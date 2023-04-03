using SokobanDotNet;

try
{
    Console.WriteLine("Load from file: ");
    string gameFilePath = Console.ReadLine();
    Game game = Game.LoadGameFromFile(gameFilePath);
    string? gameString = game.ToString();

    game.Run();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
