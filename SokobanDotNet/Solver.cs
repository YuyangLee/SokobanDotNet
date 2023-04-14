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

	internal class GameGenerator
	{
		private SokobanGame? GenerateGame(int hMap, int wMap, int nStones)
		{
			List<List<TileType>> map = new();
			for (int h = 0; h < hMap; h++)
			{
				List<TileType> row = new();
				for (int w = 0; w < wMap; w++)
				{
					if (h == 0 || w == 0 || h == hMap - 1 || w == wMap - 1) row.Add(TileType.Blocked);
					else row.Add(TileType.Ground);
				}
				map.Add(row);
			}

            Random random = new Random();
			TileType tile = random.Next(0, 101) < 50 ? TileType.Ground : TileType.Blocked;
			TileType swapper = TileType.Ground | TileType.Blocked;

			List<Tuple<int, int>> grounds = new();
            for (int h = 1; h < hMap - 1; h++)
            {
                for (int w = 1; w < wMap - 1; w++)
                {
					map[h][w] = tile;
					if (tile == TileType.Ground) grounds.Add(new(h, w));
                    tile = random.Next(0, 101) < 30 ? tile ^ swapper : tile;
                }
            }

			if (grounds.Count < 2 * nStones + 1) return null;

			int j;
			for (int i = 0; i < nStones; i++)
            {
                j = random.Next(0, grounds.Count);
                map[grounds[j].Item1][grounds[j].Item2] |= TileType.Stone;
                grounds.Remove(grounds[j]);

                j = random.Next(0, grounds.Count);
                map[grounds[j].Item1][grounds[j].Item2] = TileType.Hole;
                grounds.Remove(grounds[j]);
            }

            j = random.Next(0, grounds.Count);
            map[grounds[j].Item1][grounds[j].Item2] |= TileType.Player;

            return new SokobanGame(map);
		}

		public SokobanGame Generate(int hMap, int wMap, int nStones)
		{
			while (true)
			{
				SokobanGame? game = GenerateGame(hMap, wMap, nStones);

				if (game is not null && !game.HasStuck())
				{
					Console.Clear();
					Console.WriteLine(game.ToString());
					var solver = new GameSolver(game);
					if (!solver.IsSolvable()) continue;
					var result = solver.SolveGame();
                    if (result.Count > 0) return game;
				}

			}
		}

    }

	internal class GameSolver
	{
		private List<List<int>> DistIndexPermutations;

		private PriorityQueue<SokobanGame, int> SearchList;
		//private List<SokobanGame> SearchedNodes = new();
		//private PriorityQueue<SokobanGame, int> SearchedNodes = new();
		private Dictionary<int, List<SokobanGame>> SearchedNodesByH = new();

		private List<int> DeadRows, DeadCols;

        public Stopwatch Watch = new();

        SokobanGame BaseGame;

		List<List<int>> BareMapToHoleDistances = new();

        public GameSolver(SokobanGame game)
		{
			BaseGame = new(game);
            DistIndexPermutations = Utils.Permute(game.StoneLocations.Count);
			SearchList = new();
			PreComputeHeuristics();
			AppendToSearchList(BaseGame);
			var deads = DeadRowsAndCols(game.Map, game.HoleLocations);
			DeadRows = deads.Item1;
			DeadCols = deads.Item2;
        }

		public bool IsSolvable()
		{
            foreach (var sLoc in BaseGame.StoneLocations)
            {
                if (BareMapToHoleDistances[sLoc.Item1][sLoc.Item2] > BaseGame.Height * BaseGame.Width) return false;
            }

            foreach (var hLoc in BaseGame.HoleLocations)
            {
                if (BareMapToHoleDistances[hLoc.Item1][hLoc.Item2] > BaseGame.Height * BaseGame.Width) return false;
            }

            if (BareMapToHoleDistances[BaseGame.PlayerRow][BaseGame.PlayerCol] > BaseGame.Height * BaseGame.Width) return false;


            // TODO: Add more rules.
            return true;
		}

		public Tuple<int, int, int, int> HolesBoundingBox(List<Tuple<int, int>> holeLocs) => new (holeLocs.Min(x => x.Item1), holeLocs.Min(x => x.Item2), holeLocs.Max(x => x.Item1), holeLocs.Max(x => x.Item2));

		public Tuple<List<int>, List<int>> DeadRowsAndCols(List<List<TileType>> map, List<Tuple<int, int>> holeLocs)
		{
			List<int> deadRows = new(), deadCols = new();

			var bb = HolesBoundingBox(holeLocs);
            for (int i = 0; i < map.Count; i++)
            {
                if (i < bb.Item1 - 1)
                {
					int j;
                    for (j = 0; j < map[0].Count; j++) if ((map[i][j] & TileType.Blocked) == 0) break;
                    if (j == map[0].Count) deadRows.Add(i + 1);
                }

                if (i > bb.Item3 + 1)
                {
					int j;
                    for (j = 0; j < map[0].Count; j++) if ((map[i][j] & TileType.Blocked) == 0) break;
                    if (j == map[0].Count) deadRows.Add(i - 1);
                }
            }


            for (int j = 0; j < map[0].Count; j++)
            {
                if (j < bb.Item2 - 1)
                {
					int i;
                    for (i = 0; i < map.Count; i++) if ((map[i][j] & TileType.Blocked) == 0) break;
                    if (i == map.Count) deadCols.Add(j + 1);
                }

                if (j > bb.Item4 + 1)
                {
					int i;
                    for (i = 0; i < map.Count; i++) if ((map[i][j] & TileType.Blocked) == 0) break;
                    if (i == map.Count) deadCols.Add(j - 1);
                }
            }

            return new(deadRows, deadCols);
		}

		private bool AppendToSearchList(SokobanGame game)
		{
			int hx = PreComputedPathDistance(ref game);
			//int fx = (game.PairedTarget ? PairedTargetManhattanDistance(ref game) : TargetManhattanDistance(ref game));
			//int fx = (game.PairedTarget ? PairedTargetManhattanDistance(ref game) : TargetManhattanDistance(ref game)) + game.Cost;
			//int fx = (game.PairedTarget ? PairedTargetManhattanDistance(ref game) : MininalTargetManhattanDistance(ref game)) + game.Cost;
			if (hx == -1)
			{
                SearchList = new();
				return false;
            }
			int fx = (game.PairedTarget ? PairedTargetManhattanDistance(ref game) : PreComputedPathDistance(ref game)) + game.Cost;
			//SearchedNodes.Enqueue(game, fx);
			//SearchedNodes.Add(game);
			int gameHashVal = game.HashCode;
			if (SearchedNodesByH.ContainsKey(gameHashVal)) SearchedNodesByH[gameHashVal].Add(game);
			else SearchedNodesByH[gameHashVal] = new() { game };
			SearchList.Enqueue(game, fx);
			return true;
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

		public int PreComputedPathDistance(ref SokobanGame game)
		{
			try
			{
                return game.StoneLocations.Sum(item => BareMapToHoleDistances[item.Item1][item.Item2]);
            }
			catch (System.OverflowException ex)
			{
				return -1;
			}
		}

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
			int hVal = game.HashCode;
			if (!SearchedNodesByH.ContainsKey(hVal)) return false;
			//return SearchedNodesByH[hVal].Any(searchedGame => (searchedGame.StepsCount < game.StepsCount && searchedGame.EqualsTo(game)));
			foreach (var searchedGame in SearchedNodesByH[hVal])
			{
				if (searchedGame.StepsCount >= game.StepsCount)
				{
					if (searchedGame.EqualsTo(game))
					{
						SearchedNodesByH[hVal].Remove(searchedGame);
						return false;
					}
				}
				else if (searchedGame.EqualsTo(game)) return true;
				//if (searchedGame.EqualsTo(game)) return true;
            }
			return false;
		}

		public bool WillStuck(SokobanGame game)
		{
			foreach (var location in game.StoneLocations)
			{
				if (DeadRows.Any(item => location.Item1 == item)) return true;
				if (DeadCols.Any(item => location.Item2 == item)) return true;
			}
			return false;
		}

        public List<PlayerAction> SolveGame(long MaxSearchSecs=1200)
		{
			long MaxSearchMS = MaxSearchSecs * 1000;
            Watch = new Stopwatch();
            Watch.Start();
            while (SearchList.Count > 0)
			{
				if (Watch.ElapsedMilliseconds > MaxSearchMS)
				{
					Watch.Stop();
                    return new();
                }

                SokobanGame head = SearchList.Dequeue();

				//Console.Clear();
				//Console.WriteLine(head.ToString());
				//Console.WriteLine(TargetManhattanDistance(ref head));
				//Console.WriteLine(head.StepsCount);

                List<SokobanGame> childrenGames = head.ExecutePossibleActions();

				foreach (SokobanGame child in childrenGames)
				{
					if (child.CheckWin())
					{
                        Watch.Stop();
                        return child.GetActionChain();
                    }

					if (!WillStuck(child) && !IsSearched(child))
					{
                        bool cont = AppendToSearchList(child);
						if (!cont) break;
                    }
                }
            }

            Watch.Stop();
			return new();
		}
	}
}
