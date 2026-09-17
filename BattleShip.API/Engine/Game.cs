using BattleShip.Models;

namespace BattleShip.API.Engine;

public sealed class Game(
    FleetUnderConstruction playerFleet,
    Board computerBoard,
    Random random,
    AiDifficulty difficulty = AiDifficulty.Normal)
{
    // Lâchée au démarrage : sans elle, plus rien ne peut modifier la flotte du joueur.
    private FleetUnderConstruction? fleetUnderConstruction = playerFleet;

    public Board PlayerBoard { get; private set; } = playerFleet.ToBoard();
    public Board ComputerBoard { get; } = computerBoard;
    public AiDifficulty Difficulty { get; private set; } = difficulty;

    // Capturé une fois pour toute la partie, avant que fleetUnderConstruction ne soit lâchée au
    // démarrage : sans quoi cette information disparaîtrait dès la fin de la préparation, alors
    // que le panneau de règles peut être rouvert à tout moment de la partie.
    public bool AllowAdjacentShips { get; } = playerFleet.AllowAdjacentShips;

    public GamePhase Phase { get; private set; } = GamePhase.Setup;

    // Null hors partie en cours : personne n'a la main avant le tirage au sort ni après la victoire.
    public Side? CurrentTurn { get; private set; }

    public Side? Winner { get; private set; }

    public IReadOnlyList<int> RemainingShipLengths => fleetUnderConstruction?.RemainingLengths ?? [];

    // Jauge par camp : avance d'un cran à chaque tir normal accepté, plafonnée à l'intervalle
    // (un seul niveau, pas d'empilement). Utiliser l'attaque spéciale la remet à zéro.
    private int playerSpecialAttackProgress;
    private int computerSpecialAttackProgress;

    public int SpecialAttackProgress(Side side) =>
        side == Side.Player ? playerSpecialAttackProgress : computerSpecialAttackProgress;

    public bool HasSpecialAttackCharge(Side side) => SpecialAttackProgress(side) >= GameRules.SpecialAttackChargeInterval;

    // Seul l'ordinateur est placé à la création : le joueur pose sa flotte lui-même, ou la tire au hasard.
    public static Game CreateWithRandomComputerFleet(Random random, AiDifficulty difficulty = AiDifficulty.Normal) =>
        new(new FleetUnderConstruction(GameRules.GridSize, GameRules.GridSize, GameRules.DefaultShipLengths),
            PlaceDefaultFleet(random),
            random,
            difficulty);

    // Partie personnalisée : contrairement à la flotte par défaut, rien ne garantit qu'une
    // configuration tienne sur sa grille. null n'est pas une erreur — c'est la réponse prévue par
    // docs/REGLES.md pour une configuration que le serveur ne parvient pas à placer.
    public static Game? TryCreate(
        int width, int height, IReadOnlyList<int> shipLengths, bool allowAdjacentShips,
        Random random, AiDifficulty difficulty = AiDifficulty.Normal)
    {
        var placement = RandomFleetPlacer.Place(width, height, shipLengths, random, allowAdjacentShips);
        if (placement.Ships is null)
            return null;

        return new Game(
            new FleetUnderConstruction(width, height, shipLengths, allowAdjacentShips),
            new Board(width, height, placement.Ships),
            random,
            difficulty);
    }

    // Le niveau se choisit pendant la préparation seulement : une fois la partie commencée, le
    // changer reviendrait à changer d'adversaire en cours de route. Même garde que le placement,
    // et pour la même raison : la flotte lâchée au démarrage rend l'opération sans objet.
    public PlacementRejection? TrySetDifficulty(AiDifficulty value)
    {
        if (fleetUnderConstruction is null)
            return PlacementRejection.NotInSetup;

        Difficulty = value;
        return null;
    }

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

    public PlacementRejection? TryResetFleet()
    {
        if (fleetUnderConstruction is null)
            return PlacementRejection.NotInSetup;

        fleetUnderConstruction.Reset();
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

    public PlayerAreaTurnResult PlayerFireSpecialAttack(Coordinate center, Func<RevealedBoard, Coordinate> chooseComputerTarget)
    {
        var playerShot = FireArea(Side.Player, center);
        return new PlayerAreaTurnResult(playerShot, PlayComputerTurn(chooseComputerTarget));
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

            // Même règle que pour le joueur : dès qu'il est chargé, l'ordinateur utilise son
            // attaque spéciale — aucune stratégie de décision, la charge disponible suffit.
            if (HasSpecialAttackCharge(Side.Computer))
            {
                var area = FireArea(Side.Computer, target);
                if (area.IsAccepted)
                    shots.AddRange(area.Cells.Select(cell => new ComputerShot(cell.Target, cell.Result)));
            }
            else
            {
                var result = Fire(Side.Computer, target);
                if (result.IsAccepted)
                    shots.Add(new ComputerShot(target, result));
            }
        }

        return shots;
    }

    private ShotResult Fire(Side shooter, Coordinate target)
    {
        var guard = ValidateShooter(shooter);
        if (guard is not null)
            return ShotResult.Rejected(guard.Value);

        var targetBoard = shooter == Side.Player ? ComputerBoard : PlayerBoard;
        var result = targetBoard.ReceiveShot(target);

        if (result.IsAccepted)
        {
            IncrementSpecialAttackCharge(shooter);
            ResolveTurnAfterShots(shooter, targetBoard, [result]);
        }

        return result;
    }

    // Attaque spéciale : la case visée et ses quatre voisines directes (PlacementRules.SideNeighbours,
    // déjà utilisée pour la règle de contact — même géométrie, autre usage), résolues comme cinq
    // tirs indépendants réunis en une seule action. La case visée se comporte exactement comme un
    // tir normal pour les refus ; ses voisines, elles, sont simplement ignorées si elles sont hors
    // grille ou déjà tirées — sinon la moindre case déjà connue rendrait l'attaque inutilisable.
    private AreaShotResult FireArea(Side shooter, Coordinate center)
    {
        var guard = ValidateShooter(shooter);
        if (guard is not null)
            return new AreaShotResult(ShotResult.Rejected(guard.Value), []);

        if (!HasSpecialAttackCharge(shooter))
            return new AreaShotResult(ShotResult.Rejected(ShotRejection.SpecialAttackNotCharged), []);

        var targetBoard = shooter == Side.Player ? ComputerBoard : PlayerBoard;

        var centerResult = targetBoard.ReceiveShot(center);
        if (!centerResult.IsAccepted)
            return new AreaShotResult(centerResult, []);

        var cells = new List<AreaShot> { new(center, centerResult) };
        foreach (var neighbour in PlacementRules.SideNeighbours(center))
        {
            var result = targetBoard.ReceiveShot(neighbour);
            if (result.IsAccepted)
                cells.Add(new AreaShot(neighbour, result));
        }

        // Consommée, pas comptée comme un tir normal de plus : elle ne fait pas avancer la jauge
        // vers la prochaine charge, elle la remet à zéro.
        ResetSpecialAttackCharge(shooter);
        ResolveTurnAfterShots(shooter, targetBoard, cells.Select(cell => cell.Result));

        return new AreaShotResult(centerResult, cells);
    }

    private ShotRejection? ValidateShooter(Side shooter)
    {
        if (Phase == GamePhase.Setup)
            return ShotRejection.NotStarted;

        if (Phase == GamePhase.Finished)
            return ShotRejection.GameOver;

        if (CurrentTurn != shooter)
            return ShotRejection.NotYourTurn;

        return null;
    }

    // Commun à un tir normal (un seul résultat) et à une attaque spéciale (jusqu'à cinq) : le
    // rejeu et la victoire se décident sur l'ensemble des cases résolues, pas case par case —
    // sans ça, la première case d'une attaque spéciale déciderait du tour à la place des autres.
    private void ResolveTurnAfterShots(Side shooter, Board targetBoard, IEnumerable<ShotResult> results)
    {
        var accepted = results.ToList();

        if (accepted.Any(result => result.Outcome == ShotOutcome.Sunk) && targetBoard.AllShipsSunk)
        {
            // La victoire l'emporte sur le rejeu : la partie s'arrête et plus personne n'a la main.
            Phase = GamePhase.Finished;
            Winner = shooter;
            CurrentTurn = null;
            return;
        }

        if (!accepted.Any(result => result.Outcome is ShotOutcome.Hit or ShotOutcome.Sunk))
            CurrentTurn = shooter == Side.Player ? Side.Computer : Side.Player;
    }

    private void IncrementSpecialAttackCharge(Side shooter)
    {
        if (shooter == Side.Player)
            playerSpecialAttackProgress = Math.Min(playerSpecialAttackProgress + 1, GameRules.SpecialAttackChargeInterval);
        else
            computerSpecialAttackProgress = Math.Min(computerSpecialAttackProgress + 1, GameRules.SpecialAttackChargeInterval);
    }

    private void ResetSpecialAttackCharge(Side shooter)
    {
        if (shooter == Side.Player)
            playerSpecialAttackProgress = 0;
        else
            computerSpecialAttackProgress = 0;
    }

    // La flotte par défaut tient toujours sur la grille par défaut : un échec ici est un bug, pas une situation de jeu.
    private static Board PlaceDefaultFleet(Random random) =>
        RandomFleetPlacer.Place(GameRules.GridSize, GameRules.GridSize, GameRules.DefaultShipLengths, random).Ships
            is { } ships
            ? new Board(GameRules.GridSize, GameRules.GridSize, ships)
            : throw new InvalidOperationException("La flotte par défaut n'a pas pu être placée.");
}
