using System;

// Initializing everything that needs to be spun up at the start
GameRunner.Run();

/* To Do's
*       Clean up debug outputs
*       Add text to clarify "Invalid move" and "Not in fountain room, cannot activate"
*       Communicator.Communicate() logic is pretty ugly. Look into a way to clean it up.
*/

public static class GameRunner
{
    public static void Run()
    {
        // Player intro with game header and intro text describing objective. 
        Menu.DrawHeader();
        Communicator.NarrateIntro();

        // Waits on user to specify play field before starting.
        Menu.Display<AreaSize>();
        AreaSize areaSizeSelect = Menu.UserChoiceAreaSize();
        Console.Clear();

        // Initializing all necessary game objects for start-up
        PlayArea playArea = new(areaSizeSelect);
        Player player = new(playArea);
        Menu.SetWindowTitle();      

        Options userCommand;

        do
        {
            //playArea.DrawPlayspace(player);  // Debug only

            Communicator.Communicate(player, playArea.Fountain);

            Menu.Display<Options>();

            userCommand = Menu.UserChoiceMain();

            switch (userCommand)
            {
                case Options.Move:
                    player.TriggerMoveCommand();
                    break;
                case Options.ToggleFountain:
                    player.TriggerFountainToggle(playArea.Fountain);
                    break;
                case Options.Help:
                    Communicator.ShowHelpText();
                    break;
                case Options.Quit:
                    break;
            }

        } while (userCommand != Options.Quit && !CheckForWin(player, playArea.Fountain));

        Console.ForegroundColor = ConsoleColor.Magenta;
        if (CheckForWin(player, playArea.Fountain)) Console.WriteLine("Congratulations! You won!");
        else Console.WriteLine("Goodbye o7.");

        Console.ResetColor();
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
            ConsoleKey.DownArrow => new MoveSouth(),
            ConsoleKey.RightArrow => new MoveEast(),
            ConsoleKey.UpArrow => new MoveNorth(),
            ConsoleKey.LeftArrow => new MoveWest(),
            _ => new MoveNorth()
        };

        command.Run(this);
    }

    public void TriggerFountainToggle(Fountain fountain)
    {
        if (Coordinates.X == fountain.Coordinates.X && Coordinates.Y == fountain.Coordinates.Y)
            fountain.ToggleStatus();
    
        else Console.WriteLine("You're not in the fountain room!");
    }

    public static string GetPlayerInput() => Console.ReadLine().Trim().ToLower();
}

public class PlayArea
{
    public Room[,] Playspace;
    public Coordinate GridSize { get; set; } 
    public Fountain Fountain { get; init; }
    private (int X, int Y) SmallGrid = (4, 4);
    private (int X, int Y) MediumGrid = (6, 6);
    private (int X, int Y) LargeGrid = (8, 8);

    public PlayArea(AreaSize sizeSelect)
    {
        GridSize = sizeSelect switch
        {
            AreaSize.Small => new(SmallGrid.X, SmallGrid.Y),
            AreaSize.Medium => new(MediumGrid.X, MediumGrid.Y),
            AreaSize.Large => new(LargeGrid.X, LargeGrid.Y),
            _ => new(SmallGrid.X, SmallGrid.Y)
        };
        
        Playspace = new Room[GridSize.X, GridSize.Y];
        Fountain = new(this);

        // Initializes all of the rooms
        for(int i = 0; i < GridSize.X; i++)
            for (int j = 0; j < GridSize.Y; j++)
                Playspace[i,j] = new Room(i, j, Fountain);
    }

    public void DrawPlayspace(Player player)
    {
        string GridSquare = " _ |";
        
        for(int i = 0; i < GridSize.X; i++)
            Console.Write(" ___");
            

        Console.WriteLine();

        for (int i = 0; i < GridSize.X; i++)
        {
            Console.Write($"|");

            for (int j = 0; j < GridSize.Y; j++)
            {
                // Draws player character location if conditions are met
                if (i == player.Coordinates.X && j == player.Coordinates.Y)
                    Console.Write("_C_|");

                // Draws fountain location if conditions are met (Intended for debug only)
                else if (i == Fountain.Coordinates.X && j == Fountain.Coordinates.Y)
                    Console.Write("_F_|");

                // Default state draws a standard GridSquare and defined above
                else Console.Write($"{GridSquare}");
            }
                
            Console.WriteLine();
        }

        Console.WriteLine();
    }

    public Room FindCurrentRoom(Player player)
    {
        for (int i = 0; i < GridSize.X; i++)
            for (int j = 0; j < GridSize.Y; j++)
                if (i == player.Coordinates.X && j == player.Coordinates.Y) return Playspace[i, j];
        
        return Playspace[0, 0];
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

    public Fountain(PlayArea playspace)
    {
        Random randomCoords = new();
        
        Coordinates = new(randomCoords.Next(2, playspace.GridSize.X), randomCoords.Next(2, playspace.GridSize.Y));  
        Status = false;  // Always starts with Fountain off
    }

    public void ToggleStatus() => Status = !Status;

    public string ReaderFriendlyStatus() => Status ? "The fountain is now on." : "The fountain is now off.";
}

public class Menu
{
    private static readonly string _windowTitle = "Fountain of Objects";

    // Should review the chapter on generics since I still am struggling to make them work nicely (p. 222)
    // However, this implementation may be workable to reduce the number of UserChoice() methods I have
    public static void Display<T>() where T : Enum
    {
        string[] optionsToDisplay = Enum.GetNames(typeof(T));

        // Writing a blank line before starting menu output to ensure proper spacing.
        Console.WriteLine();

        for (int i = 0; i < Enum.GetNames(typeof(T)).Length; i++)
            Console.WriteLine($" {i + 1}: {MakeFriendlyString(optionsToDisplay[i])}"); 
    }

    public static Options UserChoiceMain()
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
                    (byte)Options.Help => Options.Help,
                    (byte)Options.Quit => Options.Quit
                };

            /* Above is a complex solution that hopefully makes this more adaptable as a single menu class later on.
             * Essentially I take each enum and cast it to it's integral value and add 1 to match user input. Then
             * use that casted enum value to point directly to the enum value. Not totally sure about this solution
             * but looking to the future essentially it would mean just adding whatever enum values and their
             * associated byte + 1 values to the switch statement, rather than ensuring the index is correct in the array.
             * Potentially consider converting the enum value to string, then comparing to a string entered by a user.
            */

            else Console.WriteLine("Please enter a valid option");
        }

    }

    // Basically repeats the method above but for AreaSize instead. Really should simplify this.
    public static AreaSize UserChoiceAreaSize()
    {
        while (true)
        {
            Console.Write("\nSelect a play-area size by entering the corresponding number: ");
            byte userInput = Convert.ToByte(Console.ReadLine());

            if (userInput >= 1 && userInput < Enum.GetNames(typeof(AreaSize)).Length + 1)
                return userInput switch
                {
                    (byte)AreaSize.Small => AreaSize.Small,
                    (byte)AreaSize.Medium => AreaSize.Medium,
                    (byte)AreaSize.Large => AreaSize.Large
                };

            else Console.WriteLine("Please enter a valid option");
        }
    }

    public static void SetWindowTitle() => Console.Title = _windowTitle;

    public static void DrawHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        
        Console.WriteLine("\n+---------------------+\n" +
                          "| Fountain of Objects |\n" +
                          "+---------------------+\n");

        Console.ResetColor();
    }

    // This takes an enum and gives it a special string to output, rather than the raw enum name, unless the enum name is good enough on its own (ie doesn't have a special case assigned)
    private static string MakeFriendlyString(string optionToConvert) => optionToConvert switch { "ToggleFountain" => "Toggle Fountain", "Small" => "Small (4x4)", "Medium" => "Medium (6x6)", "Large" => "Large (8x8)",_ => optionToConvert };
}

public record Coordinate(int x, int y)
{
    public int X { get; private set; } = x;
    public int Y { get; private set; } = y;

    public void Update(int x, int y, PlayArea playspace)
    {
        // Validates move, then displays an error message and returns if the move is invalid, with red text which is reset before closing out the method
        if (InvalidMoveCheck((X+x), (Y+y), playspace))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid move.");
            Console.ResetColor();
            return;
        }

        // Proceeds if the move is determined valid (InvalidMoveCheck returns false)
        X += x;
        Y += y;
    }

    // If any of the listed conditions are met, the move is considered in invalid. (Still feels clunky by passing in PlayArea argument, but better than being handled by Player object).
    private static bool InvalidMoveCheck(int x, int y, PlayArea playspace) => (x < 0 || y < 0) || (x >= playspace.GridSize.X || y >= playspace.GridSize.Y);
}

public static class Communicator
{
    public static string EmptyRoom { get; } = "There's nothing to sense here.";
    public static string Entrance { get; } = "Bright light emanates from the cavern's mouth. You're at the entrance.";
    private static string EntranceClose { get; } = "You can see a faint light reaching out to you from the cavern's entrance.";
    private static string FountainOffClose { get; } = "You hear distant dribbles. It's getting more humid.";
    private static string FountainOnClose { get; } = "You hear rushing water. The air is damp and cool.";
    public static string FountainRoomOff { get; } = "There's a musty smell permeating this room. The air feels...dank.";
    public static string FountainRoomOn { get; } = "The sound of rushing watern fills the corridor. The Fountain of Objects has been reactivated!";
    public static string GameIntro { get; } = "You arrive at the entrance to the cavern which contains the Fountain of Objects. Your goal? To venture inside, find and enable the Fountain, and escape with your life." +
                                              "\nUse the information your senses provide to guide you to the room in which the Fountain rests.";
    public static string HelpText { get; } = "\nHow to Play:\n" +
                                             "  1. Move: Press the arrow key corresponding to the direction you want to move.\n" +
                                             "  2. Toggle Fountain: If you're in the Fountain Room, this command toggles the state of the Fountain (On/Off).\n" +
                                             "  3. Help: Displays details about available commands.\n" +
                                             "  4. Quit: Quits the game.";
    public static string CurrentRoom(Player player) => $"\nCurrent room: ({player.Coordinates.X},{player.Coordinates.Y})";

    public static void Communicate(Player player, Fountain fountain)
    {
        /* Color Key:
         *      Yellow: Player location
         *      White: Room description text
         *      Blue: Fountain On text
         *      Magenta: Narrative text
         */

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(CurrentRoom(player));

        if (player.Coordinates.X == 0 && player.Coordinates.Y == 0)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Entrance);
        }

        else if ((player.Coordinates.X == 1 && player.Coordinates.Y == 0) || (player.Coordinates.X == 0 && player.Coordinates.Y == 1))
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(EntranceClose);
        }

        else if (fountain.Status == false && CloseToFountain(player, fountain))
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(FountainOffClose);
        }

        else if (fountain.Status == true && CloseToFountain(player, fountain))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(FountainOnClose);
        }

        else if ((player.Coordinates.X == fountain.Coordinates.X && player.Coordinates.Y == fountain.Coordinates.Y) && fountain.Status == false)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(FountainRoomOff);
        }

        else if ((player.Coordinates.X == fountain.Coordinates.X && player.Coordinates.Y == fountain.Coordinates.Y) && fountain.Status == true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(FountainRoomOn);
        }

        else
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(EmptyRoom);
        }
        
        Console.ResetColor();
    }

    public static void NarrateIntro()
    {
        Console.ForegroundColor = ConsoleColor.Magenta;

        Console.WriteLine(GameIntro);

        Console.ResetColor();
    }

    // Check if the player is on any tile which is adjancent to the fountain and returns true if they are.
    private static bool CloseToFountain(Player player, Fountain fountain)
    {
        if ((player.Coordinates.X == fountain.Coordinates.X - 1 && player.Coordinates.Y == fountain.Coordinates.Y) || (player.Coordinates.X == fountain.Coordinates.X && player.Coordinates.Y == fountain.Coordinates.Y - 1) ||
            (player.Coordinates.X == fountain.Coordinates.X + 1 && player.Coordinates.Y == fountain.Coordinates.Y) || (player.Coordinates.X == fountain.Coordinates.X && player.Coordinates.Y == fountain.Coordinates.Y + 1))
                return true;
        
        else return false;
    }

    public static void ShowHelpText() => Console.WriteLine(HelpText);
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

public enum Options { Move = 1, ToggleFountain, Help, Quit}
public enum AreaSize { Small = 1, Medium, Large}