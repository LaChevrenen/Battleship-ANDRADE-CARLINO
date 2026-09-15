using System.Collections.Concurrent;
using BattleShip.API.Engine;

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

    private sealed record Entry(StoredGame Game, Lock Gate);
}
