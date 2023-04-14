using System;
using System.Diagnostics;

namespace SokobanDotNet
{
	internal class PlannerNode
	{
		public Tuple<int, int> Position;
        public PlayerAction LastAction;
		public PlannerNode? LastNode;
		public int StepsCount;
		public PlannerNode(Tuple<int, int> position, PlayerAction lastAction, PlannerNode? lastNode)
		{
			Position = position;
			LastAction = lastAction;
			LastNode = lastNode;
			StepsCount = lastNode is null ? 0 : lastNode.StepsCount + 1;
		}

		public List<PlayerAction> GetActionChain()
		{
			if (LastNode is null) return new() { };
			var prevActions = LastNode.GetActionChain();
			prevActions.Add(LastAction);
			return prevActions;
		}
	}

	//internal class GameGenerator
	//{
	//	public SokobanGame GenerateGame(int hMap, int wMap, int nStones, int nMaxAdditionalBlocks)
	//	{
	//		List<List<TileType>> map = new();
	//		for (int h = 0; h < hMap; h++)
	//		{
	//			List<TileType> row = new();
	//			for (int w = 0; w < wMap; w++)
	//			{
	//				if (h == 0 || w == 0 || h == hMap-1 || w == wMap - 1) row.Add(TileType.Blocked);
	//				else row.Add(TileType.Ground);
	//			}
	//			map.Add(row);
	//		}

	//		return new SokobanGame();
	//	}
	//}

	internal class GameSolver
	{
		private List<List<int>> DistIndexPermutations;

		private PriorityQueue<SokobanGame, int> SearchList;
		//private List<SokobanGame> SearchedNodes = new();
		//private PriorityQueue<SokobanGame, int> SearchedNodes = new();
		private Dictionary<int, List<SokobanGame>> SearchedNodesByH = new();

        public Stopwatch Watch = new();

        SokobanGame BaseGame;

		List<List<int>> BareMapToHoleDistances = new();

        public GameSolver(SokobanGame game)
		{
			BaseGame = new(game);
            DistIndexPermutations = Utils.Permute(game.StoneLocations.Count);
			SearchList = new();
			AppendToSearchList(BaseGame);
			PreComputeHeuristics();
        }

		private void AppendToSearchList(SokobanGame game)
		{

			//int fx = (game.PairedTarget ? PairedTargetManhattanDistance(ref game) : TargetManhattanDistance(ref game));
			int fx = (game.PairedTarget ? PairedTargetManhattanDistance(ref game) : TargetManhattanDistance(ref game)) + game.Cost;
			//int fx = (game.PairedTarget ? PairedTargetManhattanDistance(ref game) : MininalTargetManhattanDistance(ref game)) + game.Cost;
			//int fx = (game.PairedTarget ? PairedTargetManhattanDistance(ref game) : PreComputedPathDistance(ref game)) + game.Cost;
            //SearchedNodes.Enqueue(game, fx);
            //SearchedNodes.Add(game);
            int gameHashVal = game.HashCode;
			if (SearchedNodesByH.ContainsKey(gameHashVal)) SearchedNodesByH[gameHashVal].Add(game);
			else SearchedNodesByH[gameHashVal] = new() { game };
			SearchList.Enqueue(game, fx);
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

            return dist;
        }

        public int MininalTargetManhattanDistance(ref SokobanGame game)
        {
            int manhattanDists = 0;
            int _dist = 0;

            foreach (var sLoc in game.StoneLocations)
            {
                int sMD = int.MaxValue;
                foreach (var hLoc in game.HoleLocations)
                {
                    _dist = Math.Abs(sLoc.Item1 - hLoc.Item1) + Math.Abs(sLoc.Item2 - hLoc.Item2);
                    if (_dist < sMD) sMD = _dist;
                }
                manhattanDists += sMD;
            }
            return manhattanDists;
        }

		public int PreComputedPathDistance(ref SokobanGame game) => game.StoneLocations.Sum(item => BareMapToHoleDistances[item.Item1][item.Item2]);

        public int PairedTargetManhattanDistance(ref SokobanGame game) => game.StoneLocations.Zip(game.HoleLocations, (b, h) => Utils.ManhattanDistance(b, h)).Sum();

		public void PreComputeHeuristics()
		{
			BareMapToHoleDistances = new();
			for (int r = 0; r < BaseGame.Height; r++)
			{
				List<int> RowDistances = new();
				for (int c = 0; c < BaseGame.Width; c++)
				{
					if ((BaseGame.Map[r][c] & TileType.Blocked) > 0) RowDistances.Add(int.MaxValue);
					else RowDistances.Add(BaseGame.HoleLocations.ConvertAll(location => BaseGame.PlanPathOnBareMap(new(r, c), location)).Min());
				}
				BareMapToHoleDistances.Add(RowDistances);
			}
			return;
		}

		private bool IsSearched(SokobanGame game)
		{
			int hVal = game.GetHash(true);
			if (!SearchedNodesByH.ContainsKey(hVal)) return false;
			return SearchedNodesByH[hVal].Any(searchedGame => searchedGame.EqualsTo(game));
		}

        public List<PlayerAction> SolveGame()
		{
            Watch = new Stopwatch();
            Watch.Start();
            while (SearchList.Count > 0)
			{
				SokobanGame head = SearchList.Dequeue();

				Console.Clear();
				Console.WriteLine(head.ToString());
				Console.WriteLine(TargetManhattanDistance(ref head));
				Console.WriteLine(head.StepsCount);

                List<SokobanGame> childrenGames = head.ExecutePossibleActions();

				foreach (SokobanGame child in childrenGames)
				{
					if (child.CheckWin())
					{
                        Watch.Stop();
                        return child.GetActionChain();
                    }

					//if (!SearchedNodes.Any(game => game.EqualsTo(child))) AppendToSearchList(child);
					//else Console.WriteLine("DEBUG: Found visited state!");
					if (!IsSearched(child)) AppendToSearchList(child);
                }
            }

            Watch.Stop();
			return new();
		}
	}
}
