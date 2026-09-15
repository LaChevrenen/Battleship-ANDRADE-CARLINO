using BattleShip.Models;

namespace BattleShip.API.Engine;

public sealed class Game(Board playerBoard, Board computerBoard, Random random)
{
    public Board PlayerBoard { get; } = playerBoard;
    public Board ComputerBoard { get; } = computerBoard;

    public GamePhase Phase { get; private set; } = GamePhase.Setup;

    // Null hors partie en cours : personne n'a la main avant le tirage au sort ni après la victoire.
    public Side? CurrentTurn { get; private set; }

    public Side? Winner { get; private set; }

    public static Game CreateWithRandomFleets(Random random) =>
        new(PlaceDefaultFleet(random), PlaceDefaultFleet(random), random);

    public bool TryStart(Func<Coordinate> chooseComputerTarget, out IReadOnlyList<ComputerShot> computerShots)
    {
        if (Phase != GamePhase.Setup)
        {
            computerShots = [];
            return false;
        }

        Phase = GamePhase.InProgress;
        CurrentTurn = random.Next(2) == 0 ? Side.Player : Side.Computer;
        // Si l'ordinateur commence, il joue tout de suite : aucune méthode publique ne rend la main avec son tour en attente.
        computerShots = PlayComputerTurn(chooseComputerTarget);
        return true;
    }

    public PlayerTurnResult PlayerFire(Coordinate target, Func<Coordinate> chooseComputerTarget)
    {
        var playerShot = Fire(Side.Player, target);
        return new PlayerTurnResult(playerShot, PlayComputerTurn(chooseComputerTarget));
    }

    // Gardé par l'état de la partie, pas par le résultat du tir du joueur : seul un tir accepté à l'eau donne la main à l'ordinateur.
    private List<ComputerShot> PlayComputerTurn(Func<Coordinate> chooseTarget)
    {
        var shots = new List<ComputerShot>();

        while (Phase == GamePhase.InProgress && CurrentTurn == Side.Computer)
        {
            var target = chooseTarget();
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
        RandomFleetPlacer.Place(GameRules.GridSize, GameRules.GridSize, GameRules.DefaultShipLengths, random).Board
        ?? throw new InvalidOperationException("La flotte par défaut n'a pas pu être placée.");
}
