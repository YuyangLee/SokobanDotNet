using System;
namespace SokobanDotNet
{
	internal class GameSolver
	{
		private static bool IsMovable(SokobanGame game, int queryRow, int QueryCol)
		{
			if (SokobanGame.IsOutSideGame(game, queryRow, QueryCol)) return false;

            // TODO: Implement this.
            return true;

        }
		public static int TargetManhattanDistance(SokobanGame game)
		{
			// TODO: Minimal Manhhatan distance
			var manhattanDists = game.HoleLocations.Zip(
				game.BoxLocations,
                (holeLoc, boxLoc) => Math.Abs(holeLoc.Item1 - boxLoc.Item1) + Math.Abs(holeLoc.Item1 - boxLoc.Item1)
            );

			return manhattanDists.Sum();
		}

		public static List<PlayerAction> SolveGame(SokobanGame game)
		{
			List<PlayerAction> solutionActions = new();

			// TODO: Solve the game.

			return solutionActions;
		}
	}
}

