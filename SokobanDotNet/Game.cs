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
        Player = 8,
        Stepable = TileType.Ground | TileType.Hole
    }

    enum PlayerAction { Undefined, Transported, Left, Right, Up, Down }

    enum GameStatus { OnGoing, Stuck, End }

    [Flags]
    enum MoveResult
    {
        FailedMove = 1, FailedPush = 2, Failed = 3,
        SuccessfulMove = 4, SuccessPush = 8, Success = 12
    }

    internal class SokobanGame // : ICloneable
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
        public List<Tuple<int, int>> BoxLocations { get => _BoxLocations; }

        private SokobanGame? ParentGame = null;

        public int StepsCount = 0;

        /// <summary>
        /// Game map.
        /// </summary>
        private List<List<TileType>> Map = new();

        private string[]? _MapData;

        private PlayerAction LastPlayerAction = PlayerAction.Undefined;

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

        private string Instruction = "=================== CONTROL ===================\n" +
                                     " Arrow - Move  Q - quit  R - Reset  S - Search \n" +
                                     "================= INSTRUCTION =================\n" +
                                     "  Push the stones \"o\" into the holes \"口\"! \n" +
                                     "===============================================\n";

        public bool Equals(ref SokobanGame game)
        {
            if (game.PlayerCol != PlayerCol || game.PlayerRow != PlayerRow) return false;
            for (int i = 0; i < BoxLocations.Count; i++) if (game.BoxLocations[i] != BoxLocations[i]) return false;

            return true;
        }

        public bool NotInChain(ref SokobanGame game)
        {
            //if (this == game) return false;
            if (game.PlayerCol != PlayerCol || game.PlayerRow != PlayerRow) return this.ParentGame is null ? true : this.ParentGame.NotInChain(ref game);
            for (int i = 0; i < BoxLocations.Count; i++)
            {
                if (game.BoxLocations[i] != BoxLocations[i]) return this.ParentGame is null ? true : this.ParentGame.NotInChain(ref game);
            }

            return false;
        }

        public List<SokobanGame> ExecutePossibleActions()
        {
            List<SokobanGame> possibleGames = new();
            foreach (var location in BoxLocations)
            {
                foreach (var action in new[] { PlayerAction.Up, PlayerAction.Down, PlayerAction.Left, PlayerAction.Right })
                {
                    var xyDelta = ActionToDeltas[action];
                    int targetRow = location.Item1 - xyDelta.Item1;
                    int targetCol = location.Item2 - xyDelta.Item2;

                    var movedAndPushedGame = MoveToAndPush(new(targetRow, targetCol), action);
                    if (movedAndPushedGame is not null && !movedAndPushedGame.HasStuck()) possibleGames.Add(movedGame);
                }
            }

            //foreach(var action in new [] { PlayerAction.Up, PlayerAction.Down, PlayerAction.Left, PlayerAction.Right })
            //{
            //    SokobanGame movedGame = this.Birth();
            //    if (((movedGame.Move(action) & MoveResult.Success) > 0) && !movedGame.HasStuck())
            //    {
            //        possibleGames.Add(movedGame);
            //    }
            //}

            return possibleGames;
        }

        public void ParseMapFromMap(List<List<TileType>> map)
        {
            Height = map.Count;
            Width = map[0].Count;

            PlayerRow = -1;
            PlayerCol = -1;
            int NumBoxes = 0, NumHoles = 0;

            _BoxLocations = new();
            _HoleLocations = new();
            Map = new();

            for (int h = 0; h < Height; h++)
            {
                List<TileType> row = new();
                for (int w = 0; w < Width; w++)
                {
                    row.Add(map[h][w]);
                    if ((map[h][w] & TileType.Player) > 0)
                    {
                        PlayerCol = w;
                        PlayerRow = h;
                    }
                    if ((map[h][w] & TileType.Box) > 0)
                    {
                        NumBoxes++;
                        _BoxLocations.Add(new(h, w));
                    }
                    if ((map[h][w] & TileType.Hole) > 0)
                    {
                        NumHoles++;
                        _HoleLocations.Add(new(h, w));
                    }
                }
                Map.Add(row);
            }
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

            _BoxLocations = new();
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
                        if (PlayerCol != -1 || PlayerRow != -1) throw new Exception("There can only be one player in the map!");
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

            for (int h = 0; h < Height; h++)
            {
                if ((mapData[h][0] != '0' + (int)TileType.Blocked) || (mapData[h][Width - 1]) != '0' + (int)TileType.Blocked) throw new Exception("The Border must be blocked!");
            }

            for (int w = 0; w < Width; w++)
            {
                if ((mapData[0][w] != '0' + (int)TileType.Blocked) || (mapData[Height-1][w]) != '0' + (int)TileType.Blocked) throw new Exception("The Border must be blocked!");
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
                            toString += "  ";
                            break;
                        case TileType.Blocked:
                            toString += "墙";
                            break;
                        case TileType.Hole:
                            toString += "口";
                            break;
                        case TileType.Box:
                            toString += "ｏ";
                            break;
                        case TileType.Player:
                            toString += "你";
                            break;
                        case TileType.Player | TileType.Hole:
                            toString += "你";
                            break;
                        case TileType.Box | TileType.Hole:
                            toString += "回";
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
            this.StepsCount = 0;
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

        private bool CanTransportToAndPush(Tuple<int, int> targetLocation, PlayerAction action)
        {
            if (IsOutside(targetLocation.Item1, targetLocation.Item2)) return false;
            if ((Map[targetLocation.Item1][targetLocation.Item2] | (TileType.Box | TileType.Blocked)) > 0) return false;

            var xyDelta = ActionToDeltas[action];
            int boxTargetRow = targetLocation.Item1 + xyDelta.Item1;
            int boxTargetCol = targetLocation.Item2 + xyDelta.Item2;


            return false;
        }

        /// <summary>
        /// Plan a path for a Transported node, from the last position to current without moving the boxes.
        /// </summary>
        /// <returns>Action for the path.</returns>
        private List<PlayerAction> PlanPath()
        {
            Tuple<int, int> from = new(this.ParentGame.PlayerRow, this.ParentGame.PlayerCol);
            Tuple<int, int> to = new(this.PlayerRow, this.PlayerCol);

            // TODO: Implementation
            PriorityQueue<PlannerNode, int> pathSearchList = new();
            pathSearchList.Enqueue(new(from, PlayerAction.Undefined, null), Utils.ManhattanDistance(from, to));

            List<List<TileType>> auxMap = new(Map);

            while (pathSearchList.Count > 0)
            {
                var head = pathSearchList.Dequeue();
                if (head.Position == to) return head.GetActionChain();

                foreach (var action in new[] { PlayerAction.Up, PlayerAction.Down, PlayerAction.Left, PlayerAction.Right })
                {
                    var xyDelta = ActionToDeltas[action];
                    int targetRow = head.Position.Item1 + xyDelta.Item1;
                    int targetCol = head.Position.Item2 + xyDelta.Item2;

                    if (IsOutside(targetRow, targetCol)) continue;
                    if ((auxMap[targetRow][targetCol] | TileType.Stepable) == 0) continue;

                    pathSearchList.Enqueue(new(new(targetRow, targetCol), action, ref head), Utils.ManhattanDistance(new(targetRow, targetCol), to));
                    auxMap[targetRow][targetCol] = TileType.Blocked;
                }
            }

            return new();
        }

        /// <summary>
        /// Whether the player can move to a target location without pushing any box
        /// </summary>
        /// <param name="targetLocation">Target location</param>
        /// <returns></returns>
        public bool CanMoveTo(Tuple<int, int> targetLoation)
        {
            return false;
        }

        /// <summary>
        /// Move to a location next to a box, and attempt to push it.
        /// </summary>
        /// <param name="targetLocation">Target location</param>
        /// <param name="action">Target action</param>
        /// <returns>Moved-and-pushed game. Returns null if the action is not possible.</returns>
        public SokobanGame? MoveToAndPush(Tuple<int, int> targetLocation, PlayerAction action)
        {
            if (!CanMoveTo(targetLocation, action)) return null;

            PlayerRow = targetLocation.Item1;
            PlayerCol = targetLocation.Item2;
            LastPlayerAction = PlayerAction.Transported;

            var game = this.Birth();
            return (game.Move(action) & MoveResult.Success) > 0 ? game : null;
        }

        /// <summary>
        /// Execute an action
        /// </summary>
        /// <param name="action">Action to be executed</param>
        /// <returns>Execution result</returns>
        private MoveResult Move(PlayerAction action)
        {
            if (action == PlayerAction.Undefined) throw new Exception("Player action undefined!");
            int targetDeltaRow = ActionToDeltas[action].Item1;
            int targetDeltaCol = ActionToDeltas[action].Item2;

            int targetRow = PlayerRow + targetDeltaRow;
            int targetCol = PlayerCol + targetDeltaCol;

            if (IsOutside(targetRow, targetCol)) return MoveResult.FailedMove;
            if (Map[targetRow][targetCol] == TileType.Blocked) return MoveResult.FailedMove;


            MoveResult result;

            if ((Map[targetRow][targetCol] == TileType.Ground) || (Map[targetRow][targetCol] == TileType.Hole))
            {
                Map[PlayerRow][PlayerCol] &= ~TileType.Player;
                Map[targetRow][targetCol] |= TileType.Player;
                PlayerRow = targetRow;
                PlayerCol = targetCol;
                result = MoveResult.SuccessfulMove;
            }
            else if ((Map[targetRow][targetCol] & TileType.Box) > 0)
            {
                int boxTargetRow = targetRow + targetDeltaRow;
                int boxTargetCol = targetCol + targetDeltaCol;
                if (IsOutside(boxTargetRow, boxTargetCol)) return MoveResult.FailedPush;
                if (((Map[boxTargetRow][boxTargetCol] & TileType.Blocked) | (Map[boxTargetRow][boxTargetCol] & TileType.Box)) > 0) return MoveResult.FailedMove;

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
                result = MoveResult.SuccessPush;
            }
            else return MoveResult.FailedMove;

            StepsCount++;
            LastPlayerAction = action;
            return result;
        }

        public List<PlayerAction> GetActionChain()
        {
            if (this.ParentGame is null) return new();
            var actionChain = this.ParentGame.GetActionChain();

            if (this.LastPlayerAction == PlayerAction.Transported) actionChain.AddRange(this.PlanPath());
            else actionChain.Add(this.LastPlayerAction);
            return actionChain;
        }

        private List<PlayerAction> Solve() => new GameSolver(this).SolveGame();

        /// <summary>
        /// Handle user key input.
        /// </summary>
        /// <param name="key">User input</param>
        /// <returns>String to show on the screen as the tip</returns>
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
                    Console.WriteLine("Searching for the solution...");

                    var actionChain = this.Solve();
                    if (actionChain.Count == 0) return "Found no solution.";

                    this.ShowResult(actionChain);
                    this.Status = GameStatus.End;
                    return "Found a solution with " + actionChain.Count.ToString() + " step(s).\n" +
                          $"Actions: {Utils.ActionsToString(actionChain)}";
                case ConsoleKey.Q:
                    this.Status = GameStatus.End;
                    return "Quitted the game.";
                default: return "";
            }

            MoveResult result = Move(action);

            if ((result & MoveResult.Success) > 0)
            {
                if (CheckWin())
                {
                    this.Status = GameStatus.End;
                    return "You win!";
                }
                if (((result & MoveResult.Success) > 0) && this.HasStuck())
                {
                    this.Status = GameStatus.Stuck;
                    return "Got stuck!";
                }

                else this.Status = GameStatus.OnGoing;
            }
            return "";
        }

        /// <summary>
        /// Check if a game hits the goal.
        /// </summary>
        /// <returns></returns>
        public bool CheckWin()
        {
            // TODO: Paired box-hole check
            foreach(var location in HoleLocations)
            {
                if (Map[location.Item1][location.Item2] != (TileType.Box | TileType.Hole)) return false;
            }
            return true;
        }

        /// <summary>
        /// Check if the game is stuck, i.e. games from this state will never win.
        /// This function will control the trimming.
        /// Should trade-off between computation efficiency and trimming amout.
        /// </summary>
        /// <returns>Whether the game is definitely stuck.</returns>
        private bool HasStuck()
        {
            // In a valid map, the box will not be on the boarder, so no boarder check is needed.
            foreach (var location in _BoxLocations)
            {
                if (Map[location.Item1][location.Item2] == (TileType.Hole | TileType.Box)) continue;
                if (((Map[location.Item1    ][location.Item2 + 1] & TileType.Blocked) > 0) && ((Map[location.Item1 + 1][location.Item2    ] & TileType.Blocked) > 0)) return true;
                if (((Map[location.Item1 + 1][location.Item2    ] & TileType.Blocked) > 0) && ((Map[location.Item1    ][location.Item2 - 1] & TileType.Blocked) > 0)) return true;
                if (((Map[location.Item1    ][location.Item2 - 1] & TileType.Blocked) > 0) && ((Map[location.Item1 - 1][location.Item2    ] & TileType.Blocked) > 0)) return true;
                if (((Map[location.Item1 - 1][location.Item2    ] & TileType.Blocked) > 0) && ((Map[location.Item1    ][location.Item2 + 1] & TileType.Blocked) > 0)) return true;
            }
            return false;
        }

        /// <summary>
        /// Demonstrate the searched solution.
        /// </summary>
        /// <param name="actions"></param>
        public void ShowResult(List<PlayerAction> actions)
        {
            var actionString = Utils.ActionsToString(actions);
            for (int i = 0; i < actions.Count; i++)
            {
                Move(actions[i]);
                Console.Clear();
                Console.WriteLine(this.Instruction);
                Console.WriteLine("Executed " + this.StepsCount.ToString() + " steps\n");
                Console.WriteLine(this.ToString());
                Console.WriteLine($"Showing solution with { i } / {actions.Count} steps.");
                Console.WriteLine($"Actions: { actionString }");

                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Run the game.
        /// </summary>
        public void Run()
        {
            string returnedString = "";
            while (true)
            {
                Console.Clear();
                Console.WriteLine(this.Instruction);
                Console.WriteLine(this.ToString());
                Console.WriteLine("Executed steps: " + this.StepsCount.ToString());
                Console.WriteLine(returnedString);
                if (this.Status == GameStatus.End) return;

                var PlayerInput = Console.ReadKey(true);
                returnedString = HandlePlayerInput(PlayerInput);
            }
        }

        // Shadow copy is not enough.
        //public object Clone() => this.MemberwiseClone();

        /// <summary>
        /// Parse the game map from strings.
        /// </summary>
        /// <param name="MapData"></param>
        public SokobanGame(string[] MapData) => ParseMapFromStrings(MapData);

        /// <summary>
        /// Build a child game.
        /// </summary>
        /// <param name="game"></param>
        public SokobanGame(SokobanGame game)
        {
            ParseMapFromMap(game.Map);
            ParentGame = game;
            StepsCount = game.StepsCount;
            LastPlayerAction = game.LastPlayerAction;
        }

        /// <summary>
        /// Give birth to a child node. This is for generating search nodes.
        /// </summary>
        /// <returns>A copied child SokobanGame instance with ParentGame pointing to this.</returns>
        public SokobanGame Birth()
        {
            SokobanGame child = new(this);
            child.ParentGame = this;
            return child;
        }

    }
}
