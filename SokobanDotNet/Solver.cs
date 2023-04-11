using System;
using System.Diagnostics;

namespace SokobanDotNet
{
	internal class PlannerNode
	{
		public Tuple<int, int> Position;
        public PlayerAction LastAction;
		public PlannerNode? LastNode;
		public PlannerNode(Tuple<int, int> position, PlayerAction lastAction, PlannerNode? lastNode)
		{
			Position = position;
			LastAction = lastAction;
			LastNode = lastNode;
		}

		public List<PlayerAction> GetActionChain()
		{
			if (LastNode is null) return new() { };
			var prevActions = LastNode.GetActionChain();
			prevActions.Add(LastAction);
			return prevActions;
		}
	}

	internal class GameSolver
	{
		private List<List<int>> DistIndexPermutations;

		private PriorityQueue<SokobanGame, int> SearchList;
		private List<SokobanGame> SearchedNodes = new();

		public Stopwatch Watch = new();

        SokobanGame BaseGame;

        public GameSolver(SokobanGame game)
		{
			BaseGame = new(game);
            DistIndexPermutations = Utils.Permute(game.StoneLocations.Count);
			SearchList = new();
			AppendToSearchList(BaseGame);
        }

		private void AppendToSearchList(SokobanGame game)
		{
			int heuristicValue = (game.PairedTarget ? TargetManhattanDistance(ref game) : PairedTargetManhattanDistance(ref game)) + game.Cost;
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
                for (int i = 0; i < game.StoneLocations.Count; i++) manhattanDists += Math.Abs(game.StoneLocations[i].Item1 - game.HoleLocations[indices[i]].Item1) + Math.Abs(game.StoneLocations[i].Item2 - game.HoleLocations[indices[i]].Item2);
                dist = dist < manhattanDists ? dist : manhattanDists;
            }

            return manhattanDists;
        }

		public int PairedTargetManhattanDistance(ref SokobanGame game) => game.StoneLocations.Zip(game.HoleLocations, (b, h) => Utils.ManhattanDistance(b, h)).Sum();

        public List<PlayerAction> SolveGame()
		{
            Watch = new Stopwatch();
            Watch.Start();
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
                        Watch.Stop();
                        return child.GetActionChain();
                    }
                    if (!SearchedNodes.Any(game => game.Equals(child))) AppendToSearchList(child);
                    //AppendToSearchList(child);
                }
            }

            Watch.Stop();
			return new();
		}
	}
}
