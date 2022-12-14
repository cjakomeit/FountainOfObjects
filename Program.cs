using System;

// Initializing everything that needs to be spun up at the start
GameRunner.Run();

public static class GameRunner
{
    public static void Run()
    {
        Fountain fountain = new();
        PlayArea playArea = new(fountain);
        Player player = new();
        Menu.SetWindowTitle();

        playArea.DrawPlayspace();  // Debug tool

        Options userCommand;

        do
        {
            Menu.Display();

            userCommand = Menu.UserChoice();

            player.TriggerMoveCommand(playArea);

            CheckForWin(player, fountain);
        } while (userCommand != Options.Quit);
    }

    private static bool CheckForWin(Player player, Fountain fountain) => (player.Coordinates.X == 0 && player.Coordinates.Y == 0) && fountain.Status == true;
}

public class Player
{
    public Coordinate Coordinates { get; set; } = new();

    public Player()
    {
        Coordinates.X = 0;
        Coordinates.Y = 0;
    }

    private bool VerifyMove(PlayArea playspace)
    {
        if ((Coordinates.X++ > playspace.GridSize.X || Coordinates.X++ < 0) || (Coordinates.Y++ > playspace.GridSize.X || Coordinates.Y++ < 0))
            return false;

        return true;
    }

    public void TriggerMoveCommand(PlayArea playspace)
    {   
        IMoveCommands command = Console.ReadKey(true).Key switch
        {
            ConsoleKey.NumPad2 => new MoveSouth(),
            ConsoleKey.NumPad6 => new MoveEast(),
            ConsoleKey.NumPad8 => new MoveNorth(),
            ConsoleKey.NumPad4 => new MoveWest(),
            _ => new MoveNorth()
        };

        if(VerifyMove(playspace)) command.Run(this);
    }

    public void TriggerFountainToggle(Fountain fountain) => fountain.ToggleStatus();

    //public void Repeat

    public string GetPlayerInput() => Console.ReadLine().Trim().ToLower();
}

public class PlayArea
{
    public Room[,] Playspace;
    public Coordinate GridSize { get; set; } = new();
    private (int X, int Y) SmallGrid = (4, 4);
    private (int X, int Y) MediumGrid = (6, 6);
    private (int X, int Y) LargeGrid = (8, 8);

    public PlayArea(Fountain fountain)
    {
        GridSize.X = SmallGrid.X;
        GridSize.Y = SmallGrid.Y;
        Playspace = new Room[GridSize.X, GridSize.Y];

        // Initializes all of the rooms
        for(int i = 0; i < GridSize.X; i++)
            for (int j = 0; j < GridSize.Y; j++)
                Playspace[i,j] = new Room(i, j, fountain);
    }

    public void DrawPlayspace()
    {
        string GridSquare = "__|";

        Console.WriteLine($" __ __ __ __ ");

        for (int i = 0; i < GridSize.Y; i++)
        {
            Console.Write($"|");

            for (int j = 0; j < GridSize.X; j++)
                Console.Write($"{GridSquare}");

            Console.WriteLine();
        }
    }
}

public class Room
{
    public Coordinate Coordinates { get; set; } = new();
    public bool ContainsFountain { get; init; } = false;

    public Room(int x, int y, Fountain fountain)
    {   
        Coordinates.X = x; //Object refrence not set ot instance of object
        Coordinates.Y = y;

        if (Coordinates == fountain.Coordinates) ContainsFountain = true;
    }
}

public class Fountain
{
    public Coordinate Coordinates { get; init; }
    public bool Status { get; set; }

    public Fountain()
    {
        Coordinates = new(0, 2);  // Eventually this will be randomly generated 0-4 for X and Y coords
        Status = false;  // Always starts with Fountain off
    }

    public void ToggleStatus() => Status = !Status;
}

public class Menu
{
    private static readonly string _windowTitle = "Fountain of Objects";

    public static void Display()
    {
        Console.WriteLine("\n+---------------------+\n" +
                          "| Fountain of Objects |\n" +
                          "+---------------------+\n");

        for (int i = 0; i < Enum.GetNames(typeof(Options)).Length; i++)
            Console.WriteLine($" {i + 1}: {ConvertOptionToString((Options)i)}");
    }

    public static Options UserChoice()
    {
        while (true)
        {
            Console.Write("\nSelect an option by entering the corresponding number: ");
            byte userInput = Convert.ToByte(Console.ReadLine());

            if (userInput >= 1 && userInput < Enum.GetNames(typeof(Options)).Length + 1)
                return userInput switch
                {
                    (byte)Options.Move + 1 => Options.Move,
                    (byte)Options.ToggleFountain + 1 => Options.ToggleFountain,
                    (byte)Options.RepeatInfo + 1 => Options.RepeatInfo,
                    (byte)Options.Quit + 1 => Options.Quit
                };

            /* Above is a complex solution that hopefully makes this more adaptable as a single menu class later on.
             * Essentially I take each enum and cast it to it's integral value and add 1 to match user input. Then
             * use that casted enum value to point directly to the enum value. Not totally sure about this solution
             * but looking to the future essentially it would mean just adding whatever enum values and their
             * associated byte + 1 values to the switch statement, rather than ensuring the index is correct in the array.
            */

            else Console.WriteLine("Please enter a valid option");
        }

    }

    public static void SetWindowTitle() => Console.Title = _windowTitle;

    // This takes an enum and gives it a special string to output, rather than the raw enum name, unless the enum name is good enough on its own (ie doesn't have a special case assigned)
    private static string ConvertOptionToString(Enum optionToConvert) => optionToConvert switch { Options.ToggleFountain => "Toggle Fountain", Options.RepeatInfo => "Repeat Room Info", _ => Convert.ToString(optionToConvert) };
}


public record Coordinate
{
    public int X { get; set; }
    public int Y { get; set; }

    public Coordinate()
    {
        X = 0;
        Y = 0;
    }

    public Coordinate(int x, int y)
    {
        X = x;
        Y = y;
    }
}

public record Communicator
{
    public string EmptyRoom { get; } = "There's nothing to sense here.";
    public string FountainRoomOff { get; } = "There's a musty smell permeating this room. The air feels...damp.";
    public string FountainRoomOn { get; } = "The sound of rushing watern fills the corridor. The Fountain of Objects has been reactivated!";

}

// Commands //

public interface IMoveCommands
{
    public void Run(Player player);
}

public class MoveNorth : IMoveCommands
{
    public void Run(Player player) => player.Coordinates.Y -= 1;
}

public class MoveSouth : IMoveCommands
{
    public void Run(Player player) => player.Coordinates.Y += 1;
}

public class MoveEast : IMoveCommands
{
    public void Run(Player player) => player.Coordinates.X += 1;
}

public class MoveWest : IMoveCommands
{
    public void Run(Player player) => player.Coordinates.X -= 1;
}

public enum Options { Move, ToggleFountain, RepeatInfo, Quit}