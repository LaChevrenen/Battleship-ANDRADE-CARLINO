using System.Collections.Concurrent;
using BattleShip.API.Engine;
using BattleShip.Models.Dtos;

namespace BattleShip.API.Storage;

public sealed class GameStore
{
    private readonly ConcurrentDictionary<Guid, Entry> games = new();

    public T Add<T>(Game game, Func<StoredGame, T> projection)
    {
        var entry = new Entry(new StoredGame(Guid.NewGuid(), game), new Lock());

        lock (entry.Gate)
        {
            games[entry.Game.Id] = entry;
            return projection(entry.Game);
        }
    }

    // Game n'est pas thread-safe : l'opération et la lecture de son résultat se font sous le même verrou,
    // sinon un GET pourrait parcourir le journal des tirs pendant qu'un tir l'écrit.
    public bool TryExecute<T>(Guid id, Func<StoredGame, T> operation, out T result)
    {
        if (!games.TryGetValue(id, out var entry))
        {
            result = default!;
            return false;
        }

        lock (entry.Gate)
        {
            result = operation(entry.Game);
            return true;
        }
    }

    public IReadOnlyList<GameSummaryDto> ListSummaries()
    {
        return [..
            games.Values
                .Select(entry =>
                {
                    lock (entry.Gate)
                    {
                        return new GameSummaryDto(
                            entry.Game.Id,
                            entry.Game.CreatedAt,
                            entry.Game.Game.Phase,
                            entry.Game.Game.Winner,
                            entry.Game.PlayerShotCount,
                            DurationSeconds(entry.Game),
                            entry.Game.Game.IsCustom);
                    }
                })
                .OrderByDescending(summary => summary.CreatedAt)];
    }

    public bool TryRemove(Guid id) => games.TryRemove(id, out _);

    private static int? DurationSeconds(StoredGame game)
    {
        if (game.StartedAt is not { } startedAt)
            return null;

        var end = game.FinishedAt ?? DateTimeOffset.UtcNow;
        return Math.Max(0, (int)(end - startedAt).TotalSeconds);
    }

    private sealed record Entry(StoredGame Game, Lock Gate);
}
