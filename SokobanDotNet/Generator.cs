using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SokobanDotNet
{

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

}
