using System;
namespace SokobanDotNet
{
	internal class GameSolver
	{
		private List<List<int>> DistIndexPermutations;

		private PriorityQueue<SokobanGame, int> SearchList;

        private static PlayerAction[] UserActions = { PlayerAction.Up, PlayerAction.Down, PlayerAction.Left, PlayerAction.Right };

		SokobanGame BaseGame;

        public GameSolver(SokobanGame game)
		{
			BaseGame = (SokobanGame)game.Clone();
            DistIndexPermutations = Utils.Permutate(game.BoxLocations.Count);
			SearchList = new();
			AppendToSearchList(BaseGame);
        }

		private void AppendToSearchList(SokobanGame game) => SearchList.Enqueue(game, TargetManhattanDistance(game) + game.StepsCount);

        public int TargetManhattanDistance(SokobanGame game)
		{
			int dist = int.MaxValue, manhattanDists = 0;

			foreach (var indices in DistIndexPermutations)
			{
				manhattanDists = 0;
				for (int i = 0; i < game.BoxLocations.Count; i++) manhattanDists += Math.Abs(game.BoxLocations[i].Item1 - game.HoleLocations[indices[i]].Item1) + Math.Abs(game.BoxLocations[i].Item2 - game.HoleLocations[indices[i]].Item2);
				dist = dist < manhattanDists ? dist : manhattanDists;
            }

			return manhattanDists;
		}

		public List<PlayerAction> SolveGame()
		{
			while (SearchList.Count > 0)
			{
				var head = SearchList.Dequeue();

				var childrenGames = head.ExecutePossibleActions();
				foreach (var child in childrenGames)
				{
					if (child.CheckWin()) return child.GetActionChain();
				}
            }

			return new();
		}
	}
}

