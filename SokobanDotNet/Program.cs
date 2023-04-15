using SokobanDotNet;

try
{
    Console.WriteLine("==== LOAD MAP DATA FROM FILE ====");
    Console.Write("Load from file: ");
    string? gameFilePath = Console.ReadLine();

    //string gameFilePath = "E:\\Dev\\SokobanDotNet\\Data\\Maps\\BASE.txt";

    if (gameFilePath.Length > 0)
    {
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
    }
    else
    {
        int H, W, S;

        Console.Write("Height ( > 3 ): ");
        H = int.Parse(Console.ReadLine());
        if (H < 4) H = 4;

        Console.Write("Width ( > 3 ): ");
        W = int.Parse(Console.ReadLine());
        if (W < 4) W = 4;

        Console.Write("Amount of Stones / Holes: ");
        S = int.Parse(Console.ReadLine());

        Console.WriteLine($"Will generate a game with size ({H}, {W}) and {S} stones/holes.");
        GameGenerator generator = new();
        SokobanGame game = generator.Generate(H, W, S);
        game.Run();
    }

    Console.WriteLine("Press any key to exit...");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
