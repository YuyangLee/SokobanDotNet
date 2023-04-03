using SokobanDotNet;

try
{
    Console.WriteLine("Load from file: ");
    string? gameFilePath = Console.ReadLine();

    if (gameFilePath is null || !File.Exists(gameFilePath))
    {
        throw new Exception($"File {gameFilePath} doesn't exist.");
    }
    // Read from TXT file
    string[] mapData = File.ReadAllLines(gameFilePath);

    Game game = new();
    game.ParseMapFromStrings(mapData);


    if (!Game.IsSolvable(game))
    {
        throw new Exception("The game is not solvable!");
    }

    game.Run();

    Console.WriteLine("Press any key to exit...");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
