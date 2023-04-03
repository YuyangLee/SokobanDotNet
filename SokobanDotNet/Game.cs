using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SokobanDotNet
{
    [Flags]
    enum TileType
    {
        Ground = 0,
        Blocked = 1,
        Hole = 2,
        Target = 4,
        Player = 8
    }
    
    internal class Game
    {
        /// <summary>
        /// Width of the map
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Height of the map
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Game map.
        /// </summary>
        private List<List<TileType>> Map;

        /// <summary>
        /// Check whether a game is solvable.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        private static bool IsSolvable(Game game)
        {
            return true;
        }

        /// <summary>
        /// Instantiate a game with specified tiles.
        /// </summary>
        /// <param name="tiles">Specified tiles</param>
        public Game(List<List<TileType>> tiles)
        {
            Map = tiles;
            Height = tiles.Count;
            Width = tiles[0].Count;
        }

        /// <summary>
        /// Load the game map from a file.
        /// </summary>
        /// <param name="gameMapPath">File path</param>
        public static Game LoadGameFromFile(string gameMapPath)
        {
            if (! File.Exists(gameMapPath))
            {
                throw new Exception($"File {gameMapPath} doesn't exist.");
            }
            // Read from TXT file
            string[] mapData = File.ReadAllLines(gameMapPath);

            if (mapData.Length < 3)
            {
                throw new Exception("There must be at least 3 rows in the map!");
            }
            
            int Height = mapData.Length;
            int Width = mapData[0].Length;

            for (int h = 0; h < Height; h++)
            {
                if (mapData[h].Length != Width)
                {
                    throw new Exception($"Each row must have same amount of Tiles. Expected {Width} tiles but found {mapData[h].Length} in row {h + 1}.");
                }
            }

            List<List<TileType>> map = new();

            for (int h = 0; h < Height; h++)
            {
                List<TileType> row = new();
                for (int w = 0; w < Width; w++)
                {
                    row.Add((TileType)mapData[h][w] - '0');
                }
                map.Add(row);
            }

            Game game = new(map);

            if (! Game.IsSolvable(game))
            {
                throw new Exception("The game is not solvable!");
            }

            return game;
        }

        public override string? ToString()
        {
            string toString = "";
            foreach (var row in Map)
            {
                foreach (var tile in row)
                {
                    switch (tile)
                    {
                        case TileType.Ground:
                            toString += " ";
                            break;
                        case TileType.Blocked:
                            toString += "#";
                            break;
                        case TileType.Hole:
                            toString += "O";
                            break;
                        case TileType.Target:
                            toString += "x";
                            break;
                        case TileType.Player:
                            toString += "I";
                            break;
                        default:
                            break;
                    }
                }
                toString += "\n";
            }
            return toString;
        }
    }
}
