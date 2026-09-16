using BattleShip.Models;

namespace BattleShip.API.Engine;

public sealed class Game(FleetUnderConstruction playerFleet, Board computerBoard, Random random)
{
    // Lâchée au démarrage : sans elle, plus rien ne peut modifier la flotte du joueur.
    private FleetUnderConstruction? fleetUnderConstruction = playerFleet;

    public Board PlayerBoard { get; private set; } = playerFleet.ToBoard();
    public Board ComputerBoard { get; } = computerBoard;

    public GamePhase Phase { get; private set; } = GamePhase.Setup;

    // Null hors partie en cours : personne n'a la main avant le tirage au sort ni après la victoire.
    public Side? CurrentTurn { get; private set; }

    public Side? Winner { get; private set; }

    public IReadOnlyList<int> RemainingShipLengths => fleetUnderConstruction?.RemainingLengths ?? [];

    // Seul l'ordinateur est placé à la création : le joueur pose sa flotte lui-même, ou la tire au hasard.
    public static Game CreateWithRandomComputerFleet(Random random) =>
        new(new FleetUnderConstruction(GameRules.GridSize, GameRules.GridSize, GameRules.DefaultShipLengths),
            PlaceDefaultFleet(random),
            random);

    public PlacementRejection? TryPlaceShip(Coordinate origin, int length, Orientation orientation)
    {
        if (fleetUnderConstruction is null)
            return PlacementRejection.NotInSetup;

        var rejection = fleetUnderConstruction.TryPlace(origin, length, orientation);
        if (rejection is null)
            PlayerBoard = fleetUnderConstruction.ToBoard();

        return rejection;
    }

    // Origines valides pour la préparation : vide une fois la partie commencée, faute de flotte à modifier.
    public IReadOnlyList<Coordinate> ValidOrigins(int length, Orientation orientation) =>
        fleetUnderConstruction?.ValidOrigins(length, orientation) ?? [];

    public PlacementRejection? TryRotateShipAt(Coordinate cell)
    {
        if (fleetUnderConstruction is null)
            return PlacementRejection.NotInSetup;

        var rejection = fleetUnderConstruction.TryRotateAt(cell);
        if (rejection is null)
            PlayerBoard = fleetUnderConstruction.ToBoard();

        return rejection;
    }

    public PlacementRejection? TryRemoveShipAt(Coordinate cell)
    {
        // Deux motifs distincts : après le démarrage, dire « aucun navire ici » serait faux.
        if (fleetUnderConstruction is null)
            return PlacementRejection.NotInSetup;

        if (!fleetUnderConstruction.TryRemoveAt(cell))
            return PlacementRejection.NoShipHere;

        PlayerBoard = fleetUnderConstruction.ToBoard();
        return null;
    }

    public PlacementRejection? TryMoveShipAt(Coordinate sourceCell, Coordinate targetOrigin, Orientation targetOrientation)
    {
        if (fleetUnderConstruction is null)
            return PlacementRejection.NotInSetup;

        var rejection = fleetUnderConstruction.TryMoveAt(sourceCell, targetOrigin, targetOrientation);
        if (rejection is null)
            PlayerBoard = fleetUnderConstruction.ToBoard();

        return rejection;
    }

    public PlacementRejection? TryPlaceFleetAtRandom()
    {
        if (fleetUnderConstruction is null)
            return PlacementRejection.NotInSetup;

        // La flotte par défaut tient toujours : un échec ici est un bug, pas une situation de jeu.
        if (!fleetUnderConstruction.TryPlaceAtRandom(random))
            throw new InvalidOperationException("La flotte n'a pas pu être placée au hasard.");

        PlayerBoard = fleetUnderConstruction.ToBoard();
        return null;
    }

    public StartRejection? TryStart(Func<RevealedBoard, Coordinate> chooseComputerTarget, out IReadOnlyList<ComputerShot> computerShots)
    {
        computerShots = [];

        if (fleetUnderConstruction is null)
            return StartRejection.AlreadyStarted;

        if (!fleetUnderConstruction.IsComplete)
            return StartRejection.FleetIncomplete;

        // La flotte est figée ici : l'objet modifiable disparaît, la Board construite ne bouge plus.
        PlayerBoard = fleetUnderConstruction.ToBoard();
        fleetUnderConstruction = null;
        Phase = GamePhase.InProgress;
        CurrentTurn = random.Next(2) == 0 ? Side.Player : Side.Computer;
        // Si l'ordinateur commence, il joue tout de suite : aucune méthode publique ne rend la main avec son tour en attente.
        computerShots = PlayComputerTurn(chooseComputerTarget);
        return null;
    }

    public PlayerTurnResult PlayerFire(Coordinate target, Func<RevealedBoard, Coordinate> chooseComputerTarget)
    {
        var playerShot = Fire(Side.Player, target);
        return new PlayerTurnResult(playerShot, PlayComputerTurn(chooseComputerTarget));
    }

    // Gardé par l'état de la partie, pas par le résultat du tir du joueur : seul un tir accepté à l'eau donne la main à l'ordinateur.
    private List<ComputerShot> PlayComputerTurn(Func<RevealedBoard, Coordinate> chooseTarget)
    {
        var shots = new List<ComputerShot>();

        // Un refus ne change pas le tour : si chooseTarget propose sans cesse des cases refusées, cette boucle ne termine jamais.
        while (Phase == GamePhase.InProgress && CurrentTurn == Side.Computer)
        {
            // L'ordinateur ne voit de la grille du joueur que ce qu'un joueur humain en saurait.
            var target = chooseTarget(PlayerBoard.Reveal());
            var result = Fire(Side.Computer, target);
            if (result.IsAccepted)
                shots.Add(new ComputerShot(target, result));
        }

        return shots;
    }

    private ShotResult Fire(Side shooter, Coordinate target)
    {
        if (Phase == GamePhase.Setup)
            return ShotResult.Rejected(ShotRejection.NotStarted);

        if (Phase == GamePhase.Finished)
            return ShotResult.Rejected(ShotRejection.GameOver);

        if (CurrentTurn != shooter)
            return ShotResult.Rejected(ShotRejection.NotYourTurn);

        var targetBoard = shooter == Side.Player ? ComputerBoard : PlayerBoard;
        var result = targetBoard.ReceiveShot(target);

        // Un tir refusé n'a pas d'Outcome : aucune branche ne le concerne, donc ni tour ni phase ne bougent.
        switch (result.Outcome)
        {
            case ShotOutcome.Miss:
                CurrentTurn = shooter == Side.Player ? Side.Computer : Side.Player;
                break;

            case ShotOutcome.Sunk when targetBoard.AllShipsSunk:
                // La victoire l'emporte sur le rejeu : la partie s'arrête et plus personne n'a la main.
                Phase = GamePhase.Finished;
                Winner = shooter;
                CurrentTurn = null;
                break;
        }

        return result;
    }

    // La flotte par défaut tient toujours sur la grille par défaut : un échec ici est un bug, pas une situation de jeu.
    private static Board PlaceDefaultFleet(Random random) =>
        RandomFleetPlacer.Place(GameRules.GridSize, GameRules.GridSize, GameRules.DefaultShipLengths, random).Ships
            is { } ships
            ? new Board(GameRules.GridSize, GameRules.GridSize, ships)
            : throw new InvalidOperationException("La flotte par défaut n'a pas pu être placée.");
}
