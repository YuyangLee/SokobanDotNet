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
        Box = 4,
        Player = 8
    }

    enum MoveResult
    {
        Success = 1, Failed = 0
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
        /// Player position.
        /// </summary>
        private int PlayerCol = -1, PlayerRow = -1;

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
            if (!File.Exists(gameMapPath))
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

            int PlayerX = -1, PlayerY = -1;

            for (int h = 0; h < Height; h++)
            {
                if (mapData[h].Length != Width)
                {
                    throw new Exception($"Each row must have same amount of Tiles. Expected {Width} tiles but found {mapData[h].Length} in row {h + 1}.");
                }
                for (int w = 0; w < Width; w++)
                {
                    if (mapData[h][w] == '0' + (int)TileType.Player)
                    {
                        if (PlayerX != -1 || PlayerY != -1)
                        {
                            throw new Exception("There can only be one player in the map!");
                        }
                        PlayerX = w;
                        PlayerY = h;
                    }
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

            Game game = new(map) { PlayerCol = PlayerX, PlayerRow = PlayerY };

            if (!Game.IsSolvable(game))
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
                        case TileType.Box:
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

        private MoveResult Move(int targetDeltaRow, int targetDeltaCol)
        {
            int targetRow = PlayerRow + targetDeltaRow;
            int targetCol = PlayerCol + targetDeltaCol;
            switch (Map[targetRow][targetCol])
            {
                case TileType.Ground:
                case TileType.Hole:
                    Map[PlayerRow][PlayerCol] &= ~TileType.Player;
                    Map[targetRow][targetCol] |= TileType.Player;
                    PlayerRow = targetRow;
                    PlayerCol = targetCol;
                    return MoveResult.Success;
                case TileType.Blocked:
                    return MoveResult.Failed;
                case TileType.Box:
                    int boxTargetRow = targetRow + targetDeltaRow;
                    int boxTargetCol = targetCol + targetDeltaCol;
                    if (Map[boxTargetRow][boxTargetCol] == TileType.Blocked || Map[boxTargetRow][boxTargetCol] == TileType.Box)
                    {
                        return MoveResult.Failed;
                    }
                    Map[boxTargetRow][boxTargetCol] |= TileType.Box;
                    Map[targetRow][targetCol] &= ~TileType.Box;
                    Map[targetRow][targetCol] |= TileType.Player;
                    Map[PlayerRow][PlayerCol] &= ~TileType.Player;
                    PlayerRow = targetRow;
                    PlayerCol = targetCol;
                    return MoveResult.Success;
                default: return MoveResult.Failed;
            }
        }

        private void HandlePlayerInput(ConsoleKeyInfo key)
        {
            int targetDeltaRow, targetDeltaCol;
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    targetDeltaRow = -1;
                    targetDeltaCol = 0;
                    break;
                case ConsoleKey.DownArrow:
                    targetDeltaRow = 1;
                    targetDeltaCol = 0;
                    break;
                case ConsoleKey.LeftArrow:
                    targetDeltaRow = 0;
                    targetDeltaCol = -1;
                    break;
                case ConsoleKey.RightArrow:
                    targetDeltaRow = 0;
                    targetDeltaCol = 1;
                    break;
                default: return;
            }
            Move(targetDeltaRow, targetDeltaCol);
        }
        public void Run()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine(this.ToString());
                var PlayerInput = Console.ReadKey(true);
                HandlePlayerInput(PlayerInput);
            }
        }
    }
}
