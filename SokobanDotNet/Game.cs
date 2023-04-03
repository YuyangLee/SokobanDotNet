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
        FailedMove,
        FailedPush,
        Invalid,
        SuccessfulMove,
        SuccessfulPush
    }

    enum GameStatus
    {
        OnGoing,
        End
    }

    internal class Game
    {
        /// <summary>
        /// Width of the map
        /// </summary>
        public int Width;

        /// <summary>
        /// Height of the map
        /// </summary>
        public int Height;

        public List<Tuple<int, int>> HoleLocations = new();

        /// <summary>
        /// Game map.
        /// </summary>
        private List<List<TileType>> Map = new();

        string[]? _MapData;

        /// <summary>
        /// Player position.
        /// </summary>
        private int PlayerCol = -1, PlayerRow = -1;

        /// <summary>
        /// Check whether a game is solvable.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        // TODO: Implement this
        public static bool IsSolvable(Game game) => true;

        private GameStatus Status = GameStatus.OnGoing;

        private readonly string Instruction = "\n==================== TILES ====================\n# - Block       O - Hole\nx - Box         X - Box in the Hole\ni - Player      I - Player on the Hole\n=================== CONTROL ===================\nArrow - Move    Q - quit  R - Reset  S - Search\n===============================================\n";

        public void ParseMapFromStrings(string[]? mapData)
        {
            if (mapData is null) return;
            if (mapData.Length < 3)
            {
                throw new Exception("There must be at least 3 rows in the map!");
            }

            Height = mapData.Length;
            Width = mapData[0].Length;

            PlayerRow = -1;
            PlayerCol = -1;
            int NumBoxes = 0, NumHoles = 0;

            HoleLocations = new();

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
                        if (PlayerCol != -1 || PlayerRow != -1)
                        {
                            throw new Exception("There can only be one player in the map!");
                        }
                        PlayerCol = w;
                        PlayerRow = h;
                    }
                    if (mapData[h][w] == '0' + (int)TileType.Box) NumBoxes ++;
                    if (mapData[h][w] == '0' + (int)TileType.Hole)
                    {
                        NumHoles++;
                        HoleLocations.Add(new(h, w));
                    }
                }
            }

            if (NumBoxes != NumHoles) throw new Exception($"There must be equal amount of box(es) and hole(s)!");
            if (NumBoxes <= 0) throw new Exception($"There must be at least one box and a hole");

            Map = new();

            for (int h = 0; h < Height; h++)
            {
                List<TileType> row = new();
                for (int w = 0; w < Width; w++)
                {
                    row.Add((TileType)mapData[h][w] - '0');
                }
                Map.Add(row);
            }

            SetBackup(mapData);
        }

        private void SetBackup(string[] mapData)
        {
            _MapData = mapData;
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
                            toString += "i";
                            break;
                        case TileType.Player | TileType.Hole:
                            toString += "I";
                            break;
                        case TileType.Box | TileType.Hole:
                            toString += "X";
                            break;
                    }
                }
                toString += "\n";
            }
            return toString;
        }

        private void Reset()
        {
            this.ParseMapFromStrings(this._MapData);
            this.Status = GameStatus.OnGoing;
        }

        private bool IsOutside(int row, int col) => (row < 0 || row >= Height || col < 0 || col >= Width);

        private MoveResult Move(int targetDeltaRow, int targetDeltaCol)
        {
            int targetRow = PlayerRow + targetDeltaRow;
            int targetCol = PlayerCol + targetDeltaCol;

            if (IsOutside(targetRow, targetCol))
            {
                return MoveResult.Invalid;
            }

            if ((Map[targetRow][targetCol] == TileType.Ground) || (Map[targetRow][targetCol] == TileType.Hole))
            {
                Map[PlayerRow][PlayerCol] &= ~TileType.Player;
                Map[targetRow][targetCol] |= TileType.Player;
                PlayerRow = targetRow;
                PlayerCol = targetCol;
                return MoveResult.SuccessfulMove;
            }
            else if (Map[targetRow][targetCol] == TileType.Blocked)
            {
                return MoveResult.FailedMove;
            }
            else if ((Map[targetRow][targetCol] & TileType.Box) > 0)
            {
                int boxTargetRow = targetRow + targetDeltaRow;
                int boxTargetCol = targetCol + targetDeltaCol;
                if (IsOutside(boxTargetRow, boxTargetCol))
                {
                    return MoveResult.FailedPush;
                }
                if (((Map[boxTargetRow][boxTargetCol] & TileType.Blocked) | (Map[boxTargetRow][boxTargetCol] & TileType.Box)) > 0)
                {
                    return MoveResult.FailedMove;
                }
                Map[boxTargetRow][boxTargetCol] |= TileType.Box;
                Map[targetRow][targetCol] &= ~TileType.Box;
                Map[targetRow][targetCol] |= TileType.Player;
                Map[PlayerRow][PlayerCol] &= ~TileType.Player;
                PlayerRow = targetRow;
                PlayerCol = targetCol;
                return MoveResult.SuccessfulPush;
            }
            else
            {
                return MoveResult.FailedMove;
            }
        }

        private string HandlePlayerInput(ConsoleKeyInfo key)
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
                case ConsoleKey.R:
                    this.Reset();
                    return "Reset the game.";
                case ConsoleKey.S:
                    return "Search for a solution...";
                case ConsoleKey.Q:
                    this.Status = GameStatus.End;
                    return "Quitted the game.";
                default: return "";
            }
            MoveResult result = Move(targetDeltaRow, targetDeltaCol);

            if (result == MoveResult.SuccessfulPush)
            {
                if (CheckWin())
                {
                    this.Status = GameStatus.End;
                    return "You win!";
                }
            }
            return "";
        }

        private bool CheckWin()
        {
            foreach(var location in HoleLocations)
            {
                if (Map[location.Item1][location.Item2] != (TileType.Box | TileType.Hole)) return false;
            }
            return true;
        }

        public void Run()
        {
            string returnedString = "";
            while (true)
            {
                Console.Clear();
                Console.WriteLine(this.Instruction);
                Console.WriteLine(this.ToString());
                Console.WriteLine(returnedString);

                var PlayerInput = Console.ReadKey(true);
                returnedString = HandlePlayerInput(PlayerInput);

                if (this.Status == GameStatus.End)
                {
                    return;
                }
            }
        }
    }
}
