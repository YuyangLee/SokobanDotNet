using System;
namespace SokobanDotNet
{
	internal class GameSolver
	{
		private List<List<int>> DistIndexPermutations;

		private PriorityQueue<SokobanGame, int> SearchList;
		private List<SokobanGame> SearchedNodes = new();

        private static PlayerAction[] UserActions = { PlayerAction.Up, PlayerAction.Down, PlayerAction.Left, PlayerAction.Right };

		SokobanGame BaseGame;

        public GameSolver(SokobanGame game)
		{
			BaseGame = new(game);
            DistIndexPermutations = Utils.Permute(game.BoxLocations.Count);
			SearchList = new();
			AppendToSearchList(BaseGame);
        }

		private void AppendToSearchList(SokobanGame game)
		{
			int heuristicValue = TargetManhattanDistance(ref game) + game.StepsCount;
			//Console.WriteLine("Current h = " + heuristicValue.ToString() + ". Current step = " + game.StepsCount.ToString() + ".");
			SearchedNodes.Add(game);
            SearchList.Enqueue(game, heuristicValue);
        }

        public int TargetManhattanDistance(ref SokobanGame game)
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

				//Console.Clear();
				//Console.WriteLine(head.ToString());

				var childrenGames = head.ExecutePossibleActions();

				foreach (var child in childrenGames)
				{
					if (child.CheckWin())
					{
                        var chain = child.GetActionChain();
						// TODO: Move the BaseNode out of this loop.
						return chain.GetRange(1, chain.Count - 1);
                    }
					if (!SearchedNodes.Any(game => game.Equals(child))) AppendToSearchList(child);
                    //AppendToSearchList(child);
                }
            }

			return new();
		}
	}
}
