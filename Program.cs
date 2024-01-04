using System;

// Initializing everything that needs to be spun up at the start
GameRunner.Run();

/* To Do's
*       High Priority:
*           Maelstroms should call TriggerPlayerMove() on collision with Player
*           GameRunner.CheckForLoss() will need to be updated to handle multiple hazards
*               Need to add a loss condition for colliding with an Amarok
*           Consider implementing a base class or interface Hazard which the implements Pits, Maelstroms, and Amaroks
*           PlayArea.CreateHazards needs to be updated to create Pits
*           On move, Maelstroms and Player need to stay with PlayArea bounds (should already be handled by Coordinates.Update())
*           Pits needs to be updated to use Hazard framework (created framework)
*           Amaroks still need to be added (created framework)
*       Medium Priority:
*           Collision should be handled by Hazard class, with sub-classes checking for collision individually, instead of GameRunner class
*           Re-think how PlayArea updates Playspace[] when adding hazards (Maybe a method in Room class that facilitates updating assoc. hazard bool?)
*           Maelstrom implemented, needs testing
*               Maelstrom only triggers collision once, possibly because hazard coordinates are changing in the background?
*       Low Priority:
*           Add help text once all expansions are added
*           Communicator.Communicate() logic is pretty ugly. Look into a way to clean it up.       
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
            playArea.FindCurrentRoom(player);

            playArea.DrawPlayspace(player);  // Debug only
            
            Communicator.Communicate(player, playArea);

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

            Console.WriteLine($"Player coords before move: ({player.Coordinates.X},{player.Coordinates.Y})");

            playArea.HazaradCollisionCheck<Maelstrom>(player);

            Console.WriteLine($"Player coords after move: ({player.Coordinates.X},{player.Coordinates.Y})");

        } while (userCommand != Options.Quit && !CheckForWin(player, playArea.Fountain) && !CheckForLoss(player, playArea));

        Console.ForegroundColor = ConsoleColor.Magenta;

        if (CheckForWin(player, playArea.Fountain)) Console.WriteLine("Congratulations! You won!");
        else if (CheckForLoss(player, playArea)) Console.WriteLine("Oh no! You fell in a pit and died. GAME OVER");
        else Console.WriteLine("Thanks for playing!");

        Console.ResetColor();
    }

    private static bool CheckForWin(Player player, Fountain fountain) => player.Coordinates.X == 0 && player.Coordinates.Y == 0 && fountain.Status == true;
    private static bool CheckForLoss(Player player, PlayArea playspace) => playspace.Playspace[player.Coordinates.X, player.Coordinates.Y].ContainsPit == true;

    // Debug tool
    /*
    public static void AutomatedTestPitPlacement()
    {
        for (int i = 0; i < 20; i++)
        {
            PlayArea playArea = new(AreaSize.Large);

            Player testPlayer = new(playArea);

            playArea.DrawPlayspace(testPlayer);
        }
    }
    */
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

    public void TriggerMoveCommand(IMoveCommands moveDirection) => moveDirection.Run(this);

    public void TriggerFountainToggle(Fountain fountain)
    {
        if (Coordinates.X == fountain.Coordinates.X && Coordinates.Y == fountain.Coordinates.Y)
            fountain.ToggleStatus();

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("You're not in the fountain room!");
        Console.ResetColor();
    }

    public static string GetPlayerInput() => Console.ReadLine().Trim().ToLower();
}

public class PlayArea
{
    public Room CurrentRoom { get; private set; }
    public Room[,] Playspace;
    public Coordinate GridSize { get; set; } 
    public Fountain Fountain { get; init; }
    public Room[] PitRooms { get; private set; }
    public Maelstrom[] Maelstroms { get; private set; }
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

        // Triggers pit placement after all the rooms are initialized and fountain is assigned its room.
        PlacePits(sizeSelect);

        // Triggers creation of maelstrom(s)
        CreateHazards<Maelstrom>(sizeSelect);
    }

    /// <summary>
    /// Parses through Playspace[] until a room matches the coordinates of the Player, 
    /// then assigns the room from Playspace[] to CurrentRoom and breaks, else continues.
    /// </summary>
    /// <param name="player"></param>
    public void FindCurrentRoom(Player player)
    {
        foreach(Room room in Playspace)
        {
            if (room.Coordinates.X == player.Coordinates.X && room.Coordinates.Y == player.Coordinates.X)
            {
                CurrentRoom = Playspace[room.Coordinates.X, room.Coordinates.Y];
                break;
            }    
        }
    }

    public bool HazaradCollisionCheck<T>(Player player)
    {
        if (typeof(T) == typeof(Maelstrom))
        {
            foreach (Maelstrom maelstrom in Maelstroms)
                if (maelstrom.CheckPlayerCollision(player))
                {
                    maelstrom.TriggerMovePlayer(player);

                    return true;
                }
        }

        else if (typeof(T) == typeof(Pit))
        {
            //foreach (Room pitRoom in PitRooms)
                // ???

        }

        return false;
    }

    /// <summary>
    /// Uses the X property of GridSize property to determine how many rooms get pits. 
    /// Pits are assigned randomly, only disallowed in the entrance and the fountain room.
    /// </summary>
    private void PlacePits(AreaSize sizeSelect)
    {
        // Initialize random number generation
        Random randomNum = new();

        // Takes user-selected PlayArea size and determines the number of pits to place with some arithmetic (Total grid area divided by double of X-axis length)
        int numberOfPits = sizeSelect switch
        {
            AreaSize.Small => (SmallGrid.X * SmallGrid.Y) / (SmallGrid.X * 2),
            AreaSize.Medium => (MediumGrid.X * MediumGrid.Y) / (MediumGrid.X * 2),
            AreaSize.Large => (LargeGrid.X * LargeGrid.Y) / (LargeGrid.X * 2),
            _ => 1
        };

        // Initializing PitRooms with a new array of size numberOfPits
        PitRooms = new Room[numberOfPits];

        // Loops for numberOfPits to create all the pit rooms and assign them to PitRooms[]
        for (int i = 0; i < numberOfPits; i++)
        {
            // Create prospective coordinates
            (int x, int y) tempCoords = (randomNum.Next(1, GridSize.X - 1), randomNum.Next(1, GridSize.Y - 1));

            // Preventing the pit from being placed in the fountain room
            while(CheckForFountainOverlap(tempCoords))
                tempCoords = (randomNum.Next(1, GridSize.X - 1), randomNum.Next(1, GridSize.Y - 1));

            // Assigning the new pit room to the PitRooms[] array, then using that same assignment to match and replace the corresponding value of Playspace[]
            PitRooms[i] = new(tempCoords.x, tempCoords.y, true, false);
            Playspace[PitRooms[i].Coordinates.X, PitRooms[i].Coordinates.Y] = PitRooms[i];
        }

        int pitCheckIndex = CheckForPitOverlap();

        if (pitCheckIndex < PitRooms.Length)
            ReRollPitPlacement(randomNum, pitCheckIndex);
    }

    /// <summary>
    /// Takes a hazard type (Pit, Maelstrom, Amarok) and determines how many of the hazard should be created based on GridSize.
    /// Then runs through the number of hazards required and initializes them. Additionally the Rooms in Playspace[] are updated
    /// to contain the Rooms with hazards.
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    /// <param name="sizeSelect">AreaSize</param>
    private void CreateHazards<T>(AreaSize sizeSelect)
    {
        int numberOfHazards;

        // Is there a better, cleaner way to track the values associated with arena size? (Dictionary maybe?)
        if (typeof(T) == typeof(Maelstrom))
        {
            numberOfHazards = sizeSelect switch
            {
                AreaSize.Small => 1,
                AreaSize.Medium => 1,
                AreaSize.Large => 2,
                _ => 1
            };

            Maelstroms = new Maelstrom[numberOfHazards];

            for (int i = 0; i < Maelstroms.Length; i++)
                Maelstroms[i] = new(this);

            for(int i = 0; i < Maelstroms.Length; i++)
                Playspace[Maelstroms[i].Coordinates.X, Maelstroms[i].Coordinates.Y] = new(Maelstroms[i].Coordinates.X, Maelstroms[i].Coordinates.Y, false, true);
        }
            

        /*else if (typeof(T) == typeof(Amarok))  // Will be used when Amaroks are implemented
            numberOfHazards = sizeSelect switch
            {
                AreaSize.Small => 1,
                AreaSize.Medium => 2,
                AreaSize.Large => 3,
                _ => 1
            };
        */

    }

    private bool CheckForFountainOverlap((int x, int y) coords) => coords.x == Fountain.Coordinates.X && coords.y == Fountain.Coordinates.Y;

    /// <summary>
    /// Takes a value from PitRooms[] and compares it against each value in PitRooms[] including itself.
    /// If Coordinates of value match compared value, and memory signature does not match, returns the current index of the loop.
    /// Else, returns the current length of PitRooms[].
    /// </summary>
    /// <returns>int</returns>
    private int CheckForPitOverlap()
    {
        foreach(Room roomToCompare in PitRooms)
        {
            for (int i = 0; i < PitRooms.Length; i++)
                if ((roomToCompare.Coordinates.X == PitRooms[i].Coordinates.X && roomToCompare.Coordinates.Y == PitRooms[i].Coordinates.Y) && roomToCompare != PitRooms[i])
                    return i;
        }
        
        return PitRooms.Length;
    }

    /// <summary>
    /// Takes a previously declared Random object declared in PlacePits(), and an int returned by CheckForPitOverlap().
    /// Generates new coordinates to try for the duped pit room, and compares the new coordinates to the coordinates
    /// of the Fountain Room as well as the other pit rooms to ensure the coordinates aren't duplicated again.
    /// </summary>
    /// <param name="randomNum">Random</param>
    /// <param name="indexOfPitToReplace">int</param>
    private void ReRollPitPlacement(Random randomNum, int indexOfPitToReplace) 
    {
        // Initialize coords so they can be used outside of do loop
        (int x, int y) tempCoords;

        // Local variable to track if the new coordinates match any existing pit placements
        bool matchesExistingPitCoords = false;

        do
        {
            // Create prospective coordinates
            tempCoords = (randomNum.Next(1, GridSize.X), randomNum.Next(1, GridSize.Y));

            // Preventing the pit from being placed in the fountain room
            while (CheckForFountainOverlap(tempCoords))
                tempCoords = (randomNum.Next(1, GridSize.X), randomNum.Next(1, GridSize.Y));

            // Checking against the other existing pit rooms to make sure the value isn't duplicated again
            for (int i = 0; i < PitRooms.Length; i++)
            {
                if (tempCoords.x == PitRooms[i].Coordinates.X && tempCoords.y == PitRooms[i].Coordinates.Y)
                {
                    matchesExistingPitCoords = true;
                    continue;
                }

                else matchesExistingPitCoords = false;
            }

        } while (matchesExistingPitCoords); 

        PitRooms[indexOfPitToReplace] = new(tempCoords.x, tempCoords.y, true, false);
        Playspace[PitRooms[indexOfPitToReplace].Coordinates.X, PitRooms[indexOfPitToReplace].Coordinates.Y] = PitRooms[indexOfPitToReplace];
    }

    // Maintained for debug purposes
    public void DrawPlayspace(Player player)
    {
        string GridSquare = " _ |";

        // Making sure the grid is pushed onto its own line
        Console.WriteLine();

        // Draws top of grid squares
        for (int i = 0; i < GridSize.X; i++)
            Console.Write(" ___");
            
        // Line break to start drawing actual play space
        Console.WriteLine();

        for (int i = 0; i < GridSize.X; i++)
        {
            Console.Write($"|");

            for (int j = 0; j < GridSize.Y; j++)
            {
                // Draws player character location if conditions are met
                if (i == player.Coordinates.X && j == player.Coordinates.Y)
                    Console.Write("_C_|");

                // Draws pit locations
                else if (Playspace[i, j].ContainsPit == true)
                    Console.Write("_P_|");

                // Draws maelstrom locations
                else if (Playspace[i, j].ContainsMaelstrom == true)
                    Console.Write("_M_|");

                // Draws fountain location if conditions are met (Intended for debug only)
                else if (i == Fountain.Coordinates.X && j == Fountain.Coordinates.Y)
                    Console.Write("_F_|");

                // Default state draws a standard GridSquare and defined above
                else Console.Write($"{GridSquare}");
            }

            // Drawing X coordinate grid along right side
            Console.Write($" {i}");

            // Pushing to new row
            Console.WriteLine();
        }

        // Draws Y coordinate grid along bottom
        for (int i = 0; i < GridSize.X; i++)
            Console.Write($"  {i} ");

        // Displays what the current room is (to test CurrentRoom is correctly assigned)
        Console.Write($"\nCurrent Room: ({CurrentRoom.Coordinates.X},{CurrentRoom.Coordinates.Y})\n");

        // Adding a line break to clean up before any other displays
        Console.WriteLine();
    }
}

public class Room
{
    public Coordinate Coordinates { get; set; }
    public Hazard Hazard { get; private set; }
    public bool ContainsFountain { get; init; } = false;
    public bool ContainsPit { get; init; } = false;
    public bool ContainsMaelstrom { get; init; } = false;

    /// <summary>
    /// Creates a Room object, specifically checking if Fountain matches defined Coordinates.
    /// </summary>
    /// <param name="x">int</param>
    /// <param name="y">int</param>
    /// <param name="fountain">Fountain</param>
    public Room(int x, int y, Fountain fountain)
    {
        Coordinates = new(x, y);

        if (Coordinates == fountain.Coordinates) ContainsFountain = true;
    }

    /// <summary>
    /// Creates a Room object, specifically to assign hazards to the room.
    /// Since hazards check that they're not overlapping the Fountain when they're created/placed, 
    /// we can safely assume ContainsFountain remains false and can skip that param.
    /// </summary>
    /// <param name="x">int</param>
    /// <param name="y">int</param>
    /// <param name="pitStatus">bool</param>
    /// <param name="maelstromStatus">bool</param>
    public Room(int x, int y, bool pitStatus, bool maelstromStatus)
    {
        Coordinates = new(x, y);

        ContainsPit = pitStatus;

        ContainsMaelstrom = maelstromStatus;
    }

    public bool HasHazard() => Hazard != null;
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
            Console.WriteLine($"Invalid move. ({x},{y})");
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
    public static string FountainRoomOn { get; } = "The sound of rushing water fills the corridor. The Fountain of Objects has been reactivated!";
    public static string GameIntro { get; } = "You arrive at the entrance to the cavern which contains the Fountain of Objects. Your goal? To venture inside, find and enable the Fountain, and escape with your life." +
                                              "\nUse the information your senses provide to guide you to the room in which the Fountain rests.";
    public static string HelpText { get; } = "\nHow to Play:\n" +
                                             "  1. Move: Press the arrow key corresponding to the direction you want to move.\n" +
                                             "  2. Toggle Fountain: If you're in the Fountain Room, this command toggles the state of the Fountain (On/Off).\n" +
                                             "  3. Help: Displays details about available commands.\n" +
                                             "  4. Quit: Quits the game.";
    private static string PitClose { get; } = "You feel a draft. There is a pit in a nearby room.";
    public static string CurrentRoom(Player player) => $"\nCurrent room: ({player.Coordinates.X},{player.Coordinates.Y})";

    public static void Communicate(Player player, PlayArea playspace)
    {
        /* Color Key:
         *      Yellow: Player location
         *      White: Room description text
         *      Blue: Fountain On text
         *      Magenta: Narrative text
         */

        // Outputs current Player coordinates
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(CurrentRoom(player));

        // Entrance room description
        if (player.Coordinates.X == 0 && player.Coordinates.Y == 0)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Entrance);
        }

        // Close to Fountain, Fountain Off
        else if (playspace.Fountain.Status == false && CloseToFountain(player, playspace.Fountain))
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(FountainOffClose);
        }

        // Close to Fountain, Fountain On
        else if (playspace.Fountain.Status == true && CloseToFountain(player, playspace.Fountain))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(FountainOnClose);
        }

        // Close to Entrance
        else if ((player.Coordinates.X == 1 && player.Coordinates.Y == 0) || (player.Coordinates.X == 0 && player.Coordinates.Y == 1))
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(EntranceClose);
        }

        // In Fountain Room, Fountain On
        else if ((player.Coordinates.X == playspace.Fountain.Coordinates.X && player.Coordinates.Y == playspace.Fountain.Coordinates.Y) && playspace.Fountain.Status == true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(FountainRoomOn);
        }

        // In Fountain Room, Fountain Off
        else if ((player.Coordinates.X == playspace.Fountain.Coordinates.X && player.Coordinates.Y == playspace.Fountain.Coordinates.Y) && playspace.Fountain.Status == false)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(FountainRoomOff);
        }

        // Close to Pit description
        else if (CloseToPit(player, playspace))
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(PitClose);
        }

        // Default empty room text
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

    /// <summary>
    /// Check if the player is on any tile which is adjancent to the fountain and returns true if they are.
    /// </summary>
    /// <param name="player">Player</param>
    /// <param name="fountain">Fountain</param>
    /// <returns>bool</returns>
    private static bool CloseToFountain(Player player, Fountain fountain)
    {
        if ((player.Coordinates.X == fountain.Coordinates.X - 1 && player.Coordinates.Y == fountain.Coordinates.Y) || (player.Coordinates.X == fountain.Coordinates.X && player.Coordinates.Y == fountain.Coordinates.Y - 1) ||
            (player.Coordinates.X == fountain.Coordinates.X + 1 && player.Coordinates.Y == fountain.Coordinates.Y) || (player.Coordinates.X == fountain.Coordinates.X && player.Coordinates.Y == fountain.Coordinates.Y + 1))
                return true;
        
        else return false;
    }

    /// <summary>
    /// Looks at all the possible locations where a pit could be adjacent to a player, returning true if there is a pit in any of the 8 rooms surrounding the player.
    /// Ignores current room's coordinates since player would have already lost.
    /// </summary>
    /// <param name="player">Player</param>
    /// <param name="playspace">PlayArea</param>
    /// <returns>bool</returns>
    private static bool CloseToPit(Player player, PlayArea playspace)
    {
        // Eyes out for a way to optimize this ungodly if-block
        foreach(Room room in playspace.PitRooms)
            if ((player.Coordinates.X == room.Coordinates.X + 1 && player.Coordinates.Y == room.Coordinates.Y + 1) || (player.Coordinates.X == room.Coordinates.X - 1 && player.Coordinates.Y == room.Coordinates.Y - 1) ||  // X AND Y
                (player.Coordinates.X == room.Coordinates.X - 1 && player.Coordinates.Y == room.Coordinates.Y + 1) || (player.Coordinates.X == room.Coordinates.X + 1 && player.Coordinates.Y == room.Coordinates.Y - 1) ||  // X XOR Y
                (player.Coordinates.X == room.Coordinates.X && player.Coordinates.Y == room.Coordinates.Y + 1)     || (player.Coordinates.X == room.Coordinates.X && player.Coordinates.Y == room.Coordinates.Y - 1)     ||  // +Y OR -Y
                (player.Coordinates.X == room.Coordinates.X + 1 && player.Coordinates.Y == room.Coordinates.Y)     || (player.Coordinates.X == room.Coordinates.X - 1 && player.Coordinates.Y == room.Coordinates.Y))        // +X OR -X
                return true;

        return false;
    }

    public static void ShowHelpText() => Console.WriteLine(HelpText);
}

// Hazards //

public class Hazard
{
    public Coordinate Coordinates { get; protected set; }
    public PlayArea Playspace { get; protected set; }

    public Hazard(PlayArea playspace)
    {
        Playspace = playspace;
    }

    public bool CheckPlayerCollision(Player player) => player.Coordinates.X == Coordinates.X && player.Coordinates.Y == Coordinates.Y;

    protected bool ValidateHazardPlacement<T>((int x, int y) coords)
    {
        // Guard statement to immediately return a false result if coordinates match Fountain's coordinates
        if (coords.x == Playspace.Fountain.Coordinates.X && coords.y == Playspace.Fountain.Coordinates.Y) return false;
        
        // Checks by type and sorts through the associated container of hazards
        if (typeof(T) == typeof(Maelstrom))
             foreach(Maelstrom comparisonHazard in Playspace.Maelstroms)
                if (comparisonHazard != null && (coords.x == comparisonHazard.Coordinates.X && coords.y == comparisonHazard.Coordinates.Y))
                    return false;

        return true;
    }

    protected Coordinate GenerateValidRandomCoords<T>()
    {
        // Using same logic from pits to check that maelstrom doesn't overlap fountain
        Random randCoordGeneration = new();

        (int x, int y) tempCoords = (randCoordGeneration.Next(1, Playspace.GridSize.X), randCoordGeneration.Next(1, Playspace.GridSize.Y));

        // Continues to generate coordinates until the hazard is not placed on another hazard nor in the fountain room
        while (!ValidateHazardPlacement<T>(tempCoords))
            while (tempCoords.x == Playspace.Fountain.Coordinates.X && tempCoords.y == Playspace.Fountain.Coordinates.Y)
                tempCoords = (randCoordGeneration.Next(1, Playspace.GridSize.X), randCoordGeneration.Next(1, Playspace.GridSize.Y));

        return new(tempCoords.x, tempCoords.y);
    }

    protected void UpdateHazardCoordinates(int x, int y) => Coordinates.Update(x, y, Playspace);
}

public class Pit : Hazard
{
    public Pit(PlayArea playspace) : base(playspace)
    {
        Coordinates = GenerateValidRandomCoords<Pit>();
    }


}

public class Maelstrom : Hazard
{
    private IMoveCommands[] ThrowPlayerDirections { get; } = new IMoveCommands[] { new MoveNorth(), new MoveWest(), new MoveWest() };  // Allows Maelstrom displacement directions to be changed easily

    public Maelstrom(PlayArea playspace) : base(playspace) 
    {
        Coordinates = GenerateValidRandomCoords<Maelstrom>();
    }

    public void TriggerMovePlayer(Player player)
    {
        foreach (IMoveCommands move in ThrowPlayerDirections)
        {
            Console.WriteLine($"Triggering player mover: {move}");  // Debug only
            
            player.TriggerMoveCommand(move);

            Console.WriteLine($"{move} complete");  // Debug only
        }

        // Maelstrom should always move after moving a player (Isn't triggering, see comment in Move() below)
        Move();
    }

    // Currently not working, throwing an invalid move when attempting to update coordinates. Need to determine if newCoords is providing a bogus value,
    // or if there's something in Coordinates.Update() that would cause it to refect newCoords values
    private void Move()
    {
        Coordinate newCoords = GenerateValidRandomCoords<Maelstrom>();

        Console.WriteLine($"New coordinates for maelstrom generated: ({newCoords.x},{newCoords.y})");


        // This is returning an invalid move if the maelstrom is on the edge of the arena
        Coordinates.Update(newCoords.x, newCoords.y, Playspace);
    }
}

/*public class Amarok : Hazard
{ 

}*/


// Player Commands //

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