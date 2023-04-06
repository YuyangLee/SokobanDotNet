﻿using System;
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

    enum PlayerAction
    {
        Left,
        Right,
        Up,
        Down
    }

    enum MoveResult
    {
        FailedMove,
        FailedPush,
        Invalid,
        Success
    }

    enum GameStatus
    {
        OnGoing,
        End
    }

    internal class SokobanGame : ICloneable
    {
        /// <summary>
        /// Width of the map
        /// </summary>
        public int Width;

        /// <summary>
        /// Height of the map
        /// </summary>
        public int Height;

        private List<Tuple<int, int>> _HoleLocations = new();
        private List<Tuple<int, int>> _BoxLocations = new();

        public List<Tuple<int, int>> HoleLocations { get => _HoleLocations; }
        public List<Tuple<int, int>> BoxLocations { get => _HoleLocations; }

        SokobanGame? ParentGame = null;

        /// <summary>
        /// Game map.
        /// </summary>
        private List<List<TileType>> Map = new();

        private string[]? _MapData;

        public static Dictionary<PlayerAction, Tuple<int, int>> ActionToDeltas = new()
        {
            { PlayerAction.Up,      new( -1,  0 ) },
            { PlayerAction.Down,    new(  1,  0 ) },
            { PlayerAction.Left,    new(  0, -1 ) },
            { PlayerAction.Right,   new(  0,  1 ) }
        };

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
        public static bool IsSolvable(SokobanGame game) => true;

        private GameStatus Status = GameStatus.OnGoing;

        private readonly string Instruction = "\n==================== TILES ====================\n# - Block       O - Hole\nx - Box         X - Box in the Hole\ni - Player      I - Player on the Hole\n=================== CONTROL ===================\nArrow - Move    Q - quit  R - Reset  S - Search\n===============================================\n";

        public bool NotInChain(SokobanGame game)
        {
            if (this == game) return true;
            if (this.ParentGame is not null) return this.ParentGame.NotInChain(game);
            else return false;
        }

        public List<SokobanGame> ExecutePossibleActions()
        {
            List<SokobanGame> possibleGames = new();
            foreach(var action in new [] { PlayerAction.Up, PlayerAction.Down, PlayerAction.Left, PlayerAction.Right })
            {
                SokobanGame movedGame = this.Birth();
                if (movedGame.Move(action) == MoveResult.Success && this.NotInChain(movedGame)) possibleGames.Add(movedGame);
            }
            return possibleGames;
        }

        public void ParseMapFromStrings(string[]? mapData)
        {
            if (mapData is null) return;
            if (mapData.Length < 3) throw new Exception("There must be at least 3 rows in the map!");

            Height = mapData.Length;
            Width = mapData[0].Length;

            PlayerRow = -1;
            PlayerCol = -1;
            int NumBoxes = 0, NumHoles = 0;

            _HoleLocations = new();

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
                    if (mapData[h][w] == '0' + (int)TileType.Box)
                    {
                        NumBoxes++;
                        _BoxLocations.Add(new(h, w));
                    }
                    else if (mapData[h][w] == '0' + (int)TileType.Hole)
                    {
                        NumHoles++;
                        _HoleLocations.Add(new(h, w));
                    }
                }
            }

            if (NumBoxes != NumHoles) throw new Exception($"There must be equal amount of box(es) and hole(s)!");
            if (NumBoxes <= 0) throw new Exception($"There must be at least one box and a hole");

            Map = new();

            for (int h = 0; h < Height; h++)
            {
                List<TileType> row = new();
                for (int w = 0; w < Width; w++) row.Add((TileType)mapData[h][w] - '0');
                Map.Add(row);
            }

            SetBackup(mapData);
        }

        private void SetBackup(string[] mapData) { _MapData = mapData; }

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

        /// <summary>
        /// Reset the game.
        /// </summary>
        private void Reset()
        {
            this.ParseMapFromStrings(this._MapData);
            this.Status = GameStatus.OnGoing;
        }

        /// <summary>
        /// Check is a block is out of the game region.
        /// </summary>
        /// <param name="row">Row index</param>
        /// <param name="col">Column index</param>
        /// <returns></returns>
        private bool IsOutside(int row, int col) => SokobanGame.IsOutSideGame(this, row, col);

        /// <summary>
        /// Check is a block is out of a game region.
        /// </summary>
        /// <param name="game">Game region</param>
        /// <param name="row">Row index</param>
        /// <param name="col">Column index</param>
        /// <returns></returns>
        public static bool IsOutSideGame(SokobanGame game, int row, int col) => (row < 0 || row >= game.Height || col < 0 || col >= game.Width);
        
        /// <summary>
        /// Execute an action
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private MoveResult Move(PlayerAction action)
        {
            int targetDeltaRow = ActionToDeltas[action].Item1;
            int targetDeltaCol = ActionToDeltas[action].Item2;

            int targetRow = PlayerRow + targetDeltaRow;
            int targetCol = PlayerCol + targetDeltaCol;

            if (IsOutside(targetRow, targetCol)) return MoveResult.Invalid;

            if ((Map[targetRow][targetCol] == TileType.Ground) || (Map[targetRow][targetCol] == TileType.Hole))
            {
                Map[PlayerRow][PlayerCol] &= ~TileType.Player;
                Map[targetRow][targetCol] |= TileType.Player;
                PlayerRow = targetRow;
                PlayerCol = targetCol;
                return MoveResult.Success;
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

                int i;
                for (i = 0; i < _BoxLocations.Count; i++)
                {
                    if ((_BoxLocations[i].Item1 == targetRow) && (_BoxLocations[i].Item2 == targetCol))
                    {
                        _BoxLocations[i] = new(boxTargetRow, boxTargetCol);
                        break;
                    }
                }
                if (i == _BoxLocations.Count) throw new Exception("Fatal error!");
                Map[boxTargetRow][boxTargetCol] |= TileType.Box;
                Map[targetRow][targetCol] &= ~TileType.Box;
                Map[targetRow][targetCol] |= TileType.Player;
                Map[PlayerRow][PlayerCol] &= ~TileType.Player;
                PlayerRow = targetRow;
                PlayerCol = targetCol;
                return MoveResult.Success;
            }
            else
            {
                return MoveResult.FailedMove;
            }
        }

        private string HandlePlayerInput(ConsoleKeyInfo key)
        {
            PlayerAction action;
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    action = PlayerAction.Up;
                    break;
                case ConsoleKey.DownArrow:
                    action = PlayerAction.Down;
                    break;
                case ConsoleKey.LeftArrow:
                    action = PlayerAction.Left;
                    break;
                case ConsoleKey.RightArrow:
                    action = PlayerAction.Right;
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

            MoveResult result = Move(action);

            if (result == MoveResult.Success)
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

        public object Clone() => this.MemberwiseClone();
        public SokobanGame Birth()
        {
            SokobanGame child = (SokobanGame)this.Clone();
            child.ParentGame = this;
            return child;
        }
    }
}
