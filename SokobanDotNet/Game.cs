using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SokobanDotNet
{
    [Flags]
    enum TileType
    {
        Blocked = 1,
        Ground = 2,
        Hole = 4,
        Stone = 8,
        Player = 16,
        Unstepable = TileType.Stone | TileType.Blocked
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
        private List<Tuple<int, int>> _StoneLocations = new();

        public List<Tuple<int, int>> HoleLocations { get => _HoleLocations; }
        public List<Tuple<int, int>> StoneLocations { get => _StoneLocations; }

        public int NumStones { get => _StoneLocations.Count; }
        public int NumHoles { get => _HoleLocations.Count; }

        private readonly  SokobanGame? ParentGame = null;

        public int StepsCount = 0;

        /// <summary>
        /// Game map.
        /// </summary>
        private List<List<TileType>> Map = new();

        private string[]? _MapData;

        private List<PlayerAction> LastPlayerActions = new();

        public int Cost { get => StepsCount; }

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
        // TODO: Implement this. But maybe it's just not implementable without actually searching for solution ???
        public static bool IsSolvable(SokobanGame game) => true;

        private GameStatus Status = GameStatus.OnGoing;

        private readonly string Instruction = "=================== CONTROL ===================\n" +
                                              " Arrow - Move  Q - quit  R - Reset  S - Search \n" +
                                              "================= INSTRUCTION =================\n" +
                                              "  Push the stones \"o\" into the holes \"口\"! \n" +
                                              "===============================================\n";

        public int[]? TargetPairingIndices = null;

        public bool PairedTarget { get => TargetPairingIndices is not null; }

        public bool NotInChain(ref SokobanGame game)
        {
            //if (this == game) return false;
            if (game.PlayerCol != PlayerCol || game.PlayerRow != PlayerRow) return ParentGame is null || ParentGame.NotInChain(ref game);
            for (int i = 0; i < StoneLocations.Count; i++)
            {
                if (game.StoneLocations[i] != StoneLocations[i]) return ParentGame is null || ParentGame.NotInChain(ref game);
            }

            return false;
        }

        public List<SokobanGame> ExecutePossibleActions()
        {
            List<SokobanGame> possibleGames = new();
            foreach (var location in StoneLocations)
            {
                foreach (var action in new[] { PlayerAction.Up, PlayerAction.Down, PlayerAction.Left, PlayerAction.Right })
                {
                    var xyDelta = ActionToDeltas[action];
                    int targetRow = location.Item1 - xyDelta.Item1;
                    int targetCol = location.Item2 - xyDelta.Item2;
                    int stoneTargetRow = location.Item1 + xyDelta.Item1;
                    int stoneTargetCol = location.Item2 + xyDelta.Item2;

                    if ((Map[targetRow][targetCol] & TileType.Unstepable) > 0) continue;
                    if ((Map[stoneTargetRow][stoneTargetCol] & TileType.Unstepable) > 0) continue;

                    SokobanGame game = new(this);

                    var movedAndPushedGame = game.MoveToAndPush(new(targetRow, targetCol), action);
                    if (movedAndPushedGame is not null && !movedAndPushedGame.HasStuck()) possibleGames.Add(movedAndPushedGame);
                }
            }

            return possibleGames;
        }

        private void ApplyTargetParingIndices(int[]? targetPairingIndices)
        {
            TargetPairingIndices = targetPairingIndices;
            if (TargetPairingIndices is not null)
            {
                if (NumHoles != TargetPairingIndices.Length) throw new Exception($"The should include 1, ..., {NumHoles} only!");
                _StoneLocations = TargetPairingIndices.Select(i => _StoneLocations[i]).ToList();
            }
        }

        public static TileType CharDigitToTileType(char c) => (TileType)(int)Math.Pow(2, (c - '0'));

        public void ParseMapFromStrings(string[]? mapData, int[]?targetPairingIndices)
        {
            if (mapData is null) return;
            if (mapData.Length < 3) throw new Exception("There must be at least 3 rows in the map!");

            Height = mapData.Length;
            Width = mapData[0].Length;

            PlayerRow = -1;
            PlayerCol = -1;

            _StoneLocations = new();
            _HoleLocations = new();

            TargetPairingIndices = targetPairingIndices;

            for (int h = 0; h < Height; h++)
            {
                if (mapData[h].Length != Width)
                {
                    throw new Exception($"Each row must have same amount of Tiles. Expected {Width} tiles but found {mapData[h].Length} in row {h + 1}.");
                }
                for (int w = 0; w < Width; w++)
                {
                    TileType tile = CharDigitToTileType(mapData[h][w]);
                    if (tile == TileType.Player)
                    {
                        if (PlayerCol != -1 || PlayerRow != -1) throw new Exception("There can only be one player in the map!");
                        PlayerCol = w;
                        PlayerRow = h;
                    }
                    else if(tile == TileType.Stone) _StoneLocations.Add(new(h, w));
                    else if (tile == TileType.Hole) _HoleLocations.Add(new(h, w));
                }
            }

            for (int h = 0; h < Height; h++)
            {
                if ((CharDigitToTileType(mapData[h][0]) != TileType.Blocked) || CharDigitToTileType(mapData[h][Width-1]) != TileType.Blocked) throw new Exception("The Border must be blocked!");
            }

            for (int w = 0; w < Width; w++)
            {
                if ((CharDigitToTileType(mapData[0][w]) != TileType.Blocked) || CharDigitToTileType(mapData[Height-1][w]) != TileType.Blocked) throw new Exception("The Border must be blocked!");
            }

            if (NumStones != NumHoles) throw new Exception($"There must be equal amount of box(es) and hole(s)!");
            if (NumStones <= 0) throw new Exception($"There must be at least one box and a hole");

            Map = new();

            for (int h = 0; h < Height; h++)
            {
                List<TileType> row = new();
                for (int w = 0; w < Width; w++) row.Add(CharDigitToTileType(mapData[h][w]));
                Map.Add(row);
            }

            Map[PlayerRow][PlayerCol] |= TileType.Ground;

            foreach (var location in StoneLocations) Map[location.Item1][location.Item2] |= TileType.Ground;

            ApplyTargetParingIndices(targetPairingIndices);
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
                        case TileType.Stone | TileType.Ground:
                            toString += "ｏ";
                            break;
                        case TileType.Player | TileType.Ground:
                            toString += "你";
                            break;
                        case TileType.Player | TileType.Hole:
                            toString += "你";
                            break;
                        case TileType.Stone | TileType.Hole:
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
            ParseMapFromStrings(_MapData, TargetPairingIndices);
            Status = GameStatus.OnGoing;
            StepsCount = 0;
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
        /// Plan a path for a Transported node, from the last position to current without moving the boxes.
        /// </summary>
        /// <returns>Action for the path.</returns>
        public bool PlanPath()
        {
            if (ParentGame is null) throw new Exception("Fatal error! Found null ParentGame！");
            Tuple<int, int> from = new(ParentGame.PlayerRow, ParentGame.PlayerCol);
            Tuple<int, int> to = new(PlayerRow, PlayerCol);

            // TODO: Implementation
            PriorityQueue<PlannerNode, int> pathSearchList = new();
            pathSearchList.Enqueue(new(from, PlayerAction.Undefined, null), Utils.ManhattanDistance(from, to));

            List<List<TileType>> auxMap = Utils.DuplicateMap(Map);
            while (pathSearchList.Count > 0)
            {
                var head = pathSearchList.Dequeue();
                if (head.Position.Item1 == to.Item1 && head.Position.Item2 == to.Item2)
                {
                    LastPlayerActions = head.GetActionChain();
                    StepsCount += LastPlayerActions.Count;
                    return true;
                }
                auxMap[from.Item1][from.Item2] = TileType.Blocked;

                foreach (var action in new[] { PlayerAction.Up, PlayerAction.Down, PlayerAction.Left, PlayerAction.Right })
                {
                    var xyDelta = ActionToDeltas[action];
                    int targetRow = head.Position.Item1 + xyDelta.Item1;
                    int targetCol = head.Position.Item2 + xyDelta.Item2;

                    if (IsOutside(targetRow, targetCol)) continue;
                    if ((auxMap[targetRow][targetCol] & TileType.Unstepable) > 0) continue;

                    pathSearchList.Enqueue(new(new(targetRow, targetCol), action, head), Utils.ManhattanDistance(new(targetRow, targetCol), to));
                    auxMap[targetRow][targetCol] = TileType.Blocked;
                }
            }

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
            Map[PlayerRow][PlayerCol] &= ~TileType.Player;
            PlayerRow = targetLocation.Item1;
            PlayerCol = targetLocation.Item2;
            Map[PlayerRow][PlayerCol] |= TileType.Player;

            if (!PlanPath()) return null;
            
            SokobanGame game = new(this);
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

            
            if ((Map[targetRow][targetCol] & TileType.Stone) > 0)
            {
                int boxTargetRow = targetRow + targetDeltaRow;
                int boxTargetCol = targetCol + targetDeltaCol;
                if (IsOutside(boxTargetRow, boxTargetCol)) return MoveResult.FailedPush;
                if (((Map[boxTargetRow][boxTargetCol] & TileType.Blocked) | (Map[boxTargetRow][boxTargetCol] & TileType.Stone)) > 0) return MoveResult.FailedMove;

                int i;
                for (i = 0; i < _StoneLocations.Count; i++)
                {
                    if ((_StoneLocations[i].Item1 == targetRow) && (_StoneLocations[i].Item2 == targetCol))
                    {
                        _StoneLocations[i] = new(boxTargetRow, boxTargetCol);
                        break;
                    }
                }
                if (i == _StoneLocations.Count) throw new Exception("Fatal error!");

                Map[boxTargetRow][boxTargetCol] |= TileType.Stone;
                Map[targetRow][targetCol] &= ~TileType.Stone;
                Map[targetRow][targetCol] |= TileType.Player;
                Map[PlayerRow][PlayerCol] &= ~TileType.Player;
                PlayerRow = targetRow;
                PlayerCol = targetCol;
                result = MoveResult.SuccessPush;
            }
            else if ((Map[targetRow][targetCol] & TileType.Unstepable) == 0)
            {
                Map[PlayerRow][PlayerCol] &= ~TileType.Player;
                Map[targetRow][targetCol] |= TileType.Player;
                PlayerRow = targetRow;
                PlayerCol = targetCol;
                result = MoveResult.SuccessfulMove;
            }
            else return MoveResult.FailedMove;

            StepsCount++;
            LastPlayerActions = new() { action };
            return result;
        }

        public List<PlayerAction> GetActionChain()
        {
            if (ParentGame is null) return new();
            var actionChain = ParentGame.GetActionChain();

            // Already planned
            // PlanPath()

            actionChain.AddRange(LastPlayerActions);
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
                    Reset();
                    return "Reset the game.";
                case ConsoleKey.S:
                    Console.WriteLine("Searching for the solution...");

                    var actionChain = Solve();
                    if (actionChain.Count == 0) return "Found no solution.";

                    ShowResult(actionChain);
                    Status = GameStatus.End;
                    return "Found a solution with " + actionChain.Count.ToString() + " step(s).\n" +
                          $"Actions: {Utils.ActionsToString(actionChain)}";
                case ConsoleKey.Q:
                    Status = GameStatus.End;
                    return "Quitted the game.";
                default: return "";
            }

            MoveResult result = Move(action);

            if ((result & MoveResult.Success) > 0)
            {
                if (CheckWin())
                {
                    Status = GameStatus.End;
                    return "You win!";
                }
                if (((result & MoveResult.Success) > 0) && HasStuck())
                {
                    Status = GameStatus.Stuck;
                    return "Got stuck!";
                }

                else Status = GameStatus.OnGoing;
            }
            return "";
        }

        /// <summary>
        /// Check if a game hits the goal.
        /// </summary>
        /// <returns></returns>
        public bool CheckWin()
        {
            if (PairedTarget) 
            {
                for (int i = 0; i < StoneLocations.Count; i++)
                {
                    if (StoneLocations[i].Item1 != HoleLocations[i].Item1) return false;
                    if (StoneLocations[i].Item2 != HoleLocations[i].Item2) return false;
                }
                return true;
            }
            else return HoleLocations.All(location => Map[location.Item1][location.Item2] == (TileType.Stone | TileType.Hole));
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
            foreach (var location in _StoneLocations)
            {
                if (Map[location.Item1][location.Item2] == (TileType.Hole | TileType.Stone)) continue;
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
                Console.WriteLine(Instruction);
                Console.WriteLine("Executed " + StepsCount.ToString() + " steps\n");
                Console.WriteLine(ToString());
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
                Console.WriteLine(Instruction);

                //if (PairedTarget) Console.WriteLine(ToString());
                //else { }

                Console.WriteLine(ToString());

                Console.WriteLine("Executed steps: " + StepsCount.ToString());
                Console.WriteLine(returnedString);
                if (Status == GameStatus.End) return;

                var PlayerInput = Console.ReadKey(true);
                returnedString = HandlePlayerInput(PlayerInput);
            }
        }

        //public ConsoleColor[] ConsoleColors = (ConsoleColor[])Enum.GetValues(typeof(ConsoleColor));

        // TODO: Fix bugs with Chinese characters indicing.
        //public void PrintToConsoleWithColors()
        //{
        //    List<Tuple<int, int>> allLocs = new();
        //    List<ConsoleColor> allColors = new();
        //    for (int i = 0; i < NumHoles; i++)
        //    {
        //        allLocs.Add(StoneLocations[i]);
        //        allLocs.Add(HoleLocations[i]);
        //        allColors.Add(ConsoleColors[i]);
        //        allColors.Add(ConsoleColors[i]);
        //    }
        //    Utils.WriteLineWithColoredChars(ToString(), )
        //}

        /// <summary>
        /// Give birth to a child node. This is for generating search nodes.
        /// </summary>
        /// <param name="MapData"></param>
        public SokobanGame(string[] MapData, int[]? targetPairingIndices) => ParseMapFromStrings(MapData, targetPairingIndices);

        /// <summary>
        /// Build a child game.
        /// </summary>
        /// <param name="game"></param>
        public SokobanGame(SokobanGame game)
        {
            //ParseMapFromMap(game.Map, game.TargetPairingIndices);
            Map = Utils.DuplicateMap(game.Map);
            Height = game.Height;
            Width = game.Width;
            PlayerRow = game.PlayerRow;
            PlayerCol = game.PlayerCol;

            TargetPairingIndices = game.TargetPairingIndices;
            _HoleLocations = Utils.DuplicateLocations(game.HoleLocations);
            _StoneLocations = Utils.DuplicateLocations(game._StoneLocations);

            ParentGame = game;
            StepsCount = game.StepsCount;
        }
    }
}
