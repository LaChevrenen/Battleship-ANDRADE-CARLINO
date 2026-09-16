using BattleShip.Models;

namespace BattleShip.API.Engine;

public static class RandomTargetStrategy
{
    public static Coordinate ChooseTarget(RevealedBoard view, Random random)
    {
        var candidates =
            (from row in Enumerable.Range(0, view.Height)
             from column in Enumerable.Range(0, view.Width)
             let cell = new Coordinate(column, row)
             where !view.Misses.Contains(cell) && !view.Hits.Contains(cell)
             select cell).ToList();

        return candidates[random.Next(candidates.Count)];
    }
}