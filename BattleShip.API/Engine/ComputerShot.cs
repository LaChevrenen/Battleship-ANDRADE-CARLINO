using BattleShip.Models;

namespace BattleShip.API.Engine;

public sealed record ComputerShot(Coordinate Target, ShotResult Result);
