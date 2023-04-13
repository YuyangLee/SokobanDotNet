using SokobanDotNet;

try
{
    //Console.WriteLine("==== LOAD MAP DATA FROM FILE ====");
    //Console.Write("Load from file: ");
    //string? gameFilePath = Console.ReadLine();

    string gameFilePath = "/Users/aiden/dev/SokobanDotNet/Data/Maps/BASE.txt";

    if (gameFilePath is null || !File.Exists(gameFilePath))
    {
        throw new Exception($"File {gameFilePath} doesn't exist.");
    }
    // Read from TXT file
    string[] mapData = File.ReadAllLines(gameFilePath);

    Console.WriteLine("\n===== (STONE, HOLE) PAIRING =====");
    Console.WriteLine("If you want to pair each stone with a hole, input the ordered indices of the stones.");
    Console.WriteLine("The order of the holes/stones are the order of their appearance in the map data.");
    Console.WriteLine("E.g. \"3 1 2\" pairs the stoe #3, #1, #2 with the hole #1, #2, #3 respectively.");
    Console.WriteLine("Left blank and hit ENTER if you don't want to pair.");
    Console.Write("Paired indices: ");
    string? pairingIndices = Console.ReadLine();

    int[]? targetPairingIndices = null;

    if (!String.IsNullOrEmpty(pairingIndices)) targetPairingIndices = Array.ConvertAll(pairingIndices.Split(" "), int.Parse).Select(i => i - 1).ToArray();

    SokobanGame game = new(mapData, targetPairingIndices);

    if (!SokobanGame.IsSolvable(game)) throw new Exception("The game is not solvable!");

    game.Run();

    Console.WriteLine("Press any key to exit...");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
