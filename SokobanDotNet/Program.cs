using SokobanDotNet;

try
{
    Game game = Game.LoadGameFromFile(@"E:/Dev/SokobanDotNet/SokobanDotNet/Data/Maps/Basic.txt");
    string? gameString = game.ToString();

    Console.WriteLine(gameString);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
