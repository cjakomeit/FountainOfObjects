using System;

// Initializing everything that needs to be spun up at the start
GameRunner.Run();

/* To Do's
*       Implement other menu option methods
*/

public static class GameRunner
{
    public static void Run()
    {
        Fountain fountain = new();
        PlayArea playArea = new(fountain);
        Player player = new(playArea);
        Menu.SetWindowTitle();

        playArea.DrawPlayspace(player);  // Debug tool

        Options userCommand;

        do
        {
            playArea.DrawPlayspace(player);

            Menu.Display();

            userCommand = Menu.UserChoice();

            switch (userCommand)
            {
                case Options.Move:
                    player.TriggerMoveCommand();
                    break;
                case Options.RepeatInfo:  // Needs implemented
                    break;
                case Options.ToggleFountain:  // Needs implemented
                    break;
                case Options.Quit:
                    break;
            }
            

            CheckForWin(player, fountain);
        } while (userCommand != Options.Quit);
    }

    private static bool CheckForWin(Player player, Fountain fountain) => (player.Coordinates.X == 0 && player.Coordinates.Y == 0) && fountain.Status == true;
}

public class Player
{
    public Coordinate Coordinates { get; set; } = new(0,0);
    public PlayArea Playspace { get; init; }

    public Player(PlayArea playArea)
    {
        Playspace = playArea;
    }

    public void TriggerMoveCommand()
    {   
        IMoveCommands command = Console.ReadKey(true).Key switch
        {
            ConsoleKey.NumPad2 => new MoveSouth(),
            ConsoleKey.NumPad6 => new MoveEast(),
            ConsoleKey.NumPad8 => new MoveNorth(),
            ConsoleKey.NumPad4 => new MoveWest(),
            _ => new MoveNorth()
        };

        command.Run(this);
    }

    public void TriggerFountainToggle(Fountain fountain) => fountain.ToggleStatus();

    public string GetPlayerInput() => Console.ReadLine().Trim().ToLower();
}

public class PlayArea
{
    public Room[,] Playspace;
    public Coordinate GridSize { get; set; } 
    private Fountain Fountain { get; init; }
    private (int X, int Y) SmallGrid = (4, 4);
    private (int X, int Y) MediumGrid = (6, 6);
    private (int X, int Y) LargeGrid = (8, 8);

    public PlayArea(Fountain fountain)
    {
        GridSize = new(SmallGrid.X, SmallGrid.Y);
        Playspace = new Room[GridSize.X, GridSize.Y];
        Fountain = fountain;

        // Initializes all of the rooms
        for(int i = 0; i < GridSize.X; i++)
            for (int j = 0; j < GridSize.Y; j++)
                Playspace[i,j] = new Room(i, j, fountain);
    }

    public void DrawPlayspace(Player character)
    {
        string GridSquare = " _ |";

        Console.WriteLine($" ___ ___ ___ ___ ");

        for (int i = 0; i < GridSize.X; i++)
        {
            Console.Write($"|");

            for (int j = 0; j < GridSize.Y; j++)
            {
                // Draws player character location if conditions are met
                if (i == character.Coordinates.X && j == character.Coordinates.Y)
                    Console.Write("_C_|");

                // Draws fountain location if conditions are met (Intended for debug only)
                else if (i == Fountain.Coordinates.X && j == Fountain.Coordinates.Y)
                    Console.Write("_F_|");

                // Default state draws a standard GridSquare and defined above
                else Console.Write($"{GridSquare}");
            }
                
            Console.WriteLine();
        }
    }
}

public class Room
{
    public Coordinate Coordinates { get; set; }
    public bool ContainsFountain { get; init; } = false;

    public Room(int x, int y, Fountain fountain)
    {
        Coordinates = new(x, y);

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

        for (int i = 1; i <= Enum.GetNames(typeof(Options)).Length; i++)
            Console.WriteLine($" {i}: {ConvertOptionToString((Options)i)}");
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
                    (byte)Options.Move => Options.Move,
                    (byte)Options.ToggleFountain => Options.ToggleFountain,
                    (byte)Options.RepeatInfo=> Options.RepeatInfo,
                    (byte)Options.Quit => Options.Quit
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


public record Coordinate(int x, int y)
{
    public int X { get; private set; } = x;
    public int Y { get; private set; } = y;

    public void Update(int x, int y, PlayArea playspace)
    {
        Console.WriteLine($"Previous PC pos.: ({X},{Y})");
        
        // Validates move, then displays an error message and returns if the move is invalid
        if (InvalidMoveCheck((X+x), (Y+y), playspace))
        {
            Console.WriteLine("Invalid move.");
            return;
        }

        // Proceeds if the move is determined valid (InvalidMoveCheck returns false)
        X += x;
        Y += y;

        Console.WriteLine($"Current PC pos.: ({X},{Y})");
    }

    // If any of the listed conditions are met, the move is considered in invalid. (Still feels clunky by passing in PlayArea argument, but better than being handled by Player object).
    private bool InvalidMoveCheck(int x, int y, PlayArea playspace) => (x < 0 || y < 0) || (x >= playspace.GridSize.X || y >= playspace.GridSize.Y);
}

public static class Communicator
{
    public static string EmptyRoom { get; } = "There's nothing to sense here.";
    public static string FountainRoomOff { get; } = "There's a musty smell permeating this room. The air feels...damp.";
    public static string FountainRoomOn { get; } = "The sound of rushing watern fills the corridor. The Fountain of Objects has been reactivated!";

}

// Commands //

public interface IMoveCommands
{
    public void Run(Player player);
}

public class MoveNorth : IMoveCommands
{
    public void Run(Player player) => player.Coordinates.Update(-1, 0, player.Playspace);
}

public class MoveSouth : IMoveCommands
{
    public void Run(Player player) => player.Coordinates.Update(1, 0, player.Playspace);
}

public class MoveEast : IMoveCommands
{
    public void Run(Player player) => player.Coordinates.Update(0, 1, player.Playspace);
}

public class MoveWest : IMoveCommands
{
    public void Run(Player player) => player.Coordinates.Update(0, -1, player.Playspace);
}

public enum Options { Move = 1, ToggleFountain, RepeatInfo, Quit}