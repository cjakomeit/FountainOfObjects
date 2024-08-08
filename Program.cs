using System;

Menu.SetWindowTitle();

// SEND IT
GameRunner.Run();

/* To Do's
*       For Feature Complete:
*           Complete Hazard Communicator logic
*       High Priority:
*           With CurrentRoom implemented, Communicator needs a refactor
*           ShootNorth and ShootSouth don't register hazards
*       Medium Priority
*           
*       Low Priority:
*           Clean up debug text
*       
*       Learnings:
*           * If I were to rewrite this whole program I'd determine how to make Hazards static and potentially even an interface. Theoretically,
*           this would simplify their use and Hazards don't really need to contain much instance data.
*           * I've been forced to consider much more carefully how necessary many-to-many relationships are, how much complexity they add
*           compared to the value they bring.
*           * I have a much finer understanding of the logic involved in if statements. I also picked up the habit of using guard statements.
*           * Doing this project was a great way to learn how to maintain and effectively refactor a large-scale, complex program.
*       
*       Copyright: Chandler Jakomeit, August 7th, 2024
*/

public static class GameRunner
{
    public static void Run()
    {
        // Immediately initializing variable for tracking user input
        Options userCommand;

        // Keeps the player in the game so that if the game ends they're automatically restarted
        do
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
            
            // Start of actual gameplay loop
            do
            {
                // Defining CurrentRoom on outset of loop
                playArea.FindCurrentRoom(player);

                playArea.DrawPlayspace(player);  // Debug only

                // Gives player details on the room they're currently in            
                Communicator.Communicate(player, playArea);

                Menu.Display<Options>();

                userCommand = Menu.UserChoiceMain();

                // Decide what to do based on userCommand
                switch (userCommand)
                {
                    case Options.Move:
                        player.TriggerMoveCommand();
                        playArea.FindCurrentRoom(player);
                        break;
                    case Options.Shoot:
                        player.Shoot();
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

                // If the player's current room contains a hazard and it is of type Maelstrom, then run logic to move player
                if (playArea.CurrentRoom.HasHazard())
                {
                    // Debug tool
                    Console.WriteLine($"Hazard detected: {playArea.CurrentRoom.HazardType}");
                    // Wrapping in guard statement
                    if (playArea.CurrentRoom.HazardType == typeof(Maelstrom))
                        playArea.MaelstromCollision(player);
                }
            
            } while (userCommand != Options.Quit && !CheckForWin(player, playArea.Fountain) && !CheckForLoss(playArea));

            Console.ForegroundColor = ConsoleColor.Magenta;

            if (CheckForWin(player, playArea.Fountain)) Console.WriteLine("Congratulations! You won!");
            else if (CheckForLoss(playArea)) Console.WriteLine("Oh no! You died. GAME OVER");
            else Console.WriteLine("Thanks for playing!");
        } while (userCommand != Options.Quit);
        
        Console.ResetColor();
    }

    private static bool CheckForWin(Player player, Fountain fountain) => player.Coordinates.X == 0 && player.Coordinates.Y == 0 && fountain.Status == true;
    private static bool CheckForLoss(PlayArea playspace)
    {
        if (playspace.CurrentRoom.HasHazard() && playspace.CurrentRoom.HazardType == typeof(Pit))
            return true;

        else if (playspace.CurrentRoom.HasHazard() && playspace.CurrentRoom.HazardType == typeof(Amarok))
            return true;

        else return false;
    }

    // Debug tool for testing Pit placement
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
    public Coordinate Coordinates { get; set; }
    public PlayArea Playspace { get; init; }
    public byte Ammo { get; private set; } = 5;

    public Player(PlayArea playArea)
    {
        Playspace = playArea;
        Coordinates = new(0, 0);
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

    public void Shoot()
    {
        if (Ammo > 0)
        {
            IShootCommands command = Console.ReadKey(true).Key switch
            {
                ConsoleKey.DownArrow => new ShootSouth(),
                ConsoleKey.RightArrow => new ShootEast(),
                ConsoleKey.UpArrow => new ShootNorth(),
                ConsoleKey.LeftArrow => new ShootWest(),
                _ => new ShootNorth()
            };

            Console.WriteLine(command);

            if (command.Shoot(Playspace))
                Console.WriteLine("Monster killed!");
            else Console.WriteLine("Arrow missed!");
                
            Ammo--;
        }
        
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No more arrows!");
            Console.ResetColor();
        }
    }

    public void TriggerFountainToggle(Fountain fountain)
    {
        // Not using PlayArea.CurrentRoom as it's more important here to make sure the player is actually in the fountain room
        if (Coordinates.X == fountain.Coordinates.X && Coordinates.Y == fountain.Coordinates.Y)
            fountain.ToggleStatus();

        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("You're not in the fountain room!");
            Console.ResetColor();
        }
    }

    public static string GetPlayerInput() => Console.ReadLine().Trim().ToLower();
}

public class PlayArea
{
    public Room CurrentRoom { get; private set; }
    public Coordinate GridSize { get; set; }
    public Room[,] Grid;
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
        
        Grid = new Room[GridSize.X, GridSize.Y];
        Fountain = new(this);

        // Initializes all of the rooms
        for (int i = 0; i < GridSize.X; i++)
            for (int j = 0; j < GridSize.Y; j++)
                Grid[i,j] = new Room(i, j, Fountain);

        // Creating all hazards
        CreateHazards<Pit>(sizeSelect);
        CreateHazards<Maelstrom>(sizeSelect);
        CreateHazards<Amarok>(sizeSelect);

        // Defaulting CurrentRoom to entrance room
        CurrentRoom = Grid[0, 0];

        // Debug tool
        for (int i = 0; i < GridSize.X; i++)
            for (int j = 0; j < GridSize.Y; j++)
                Console.WriteLine($"Room {i},{j} hazard: {Grid[i, j].HazardType}");
    }

    /// <summary>
    /// Defines CurrentRoom property as the Room at Grid[player X, player Y].
    /// </summary>
    /// <param name="player"></param>
    public void FindCurrentRoom(Player player) => CurrentRoom = Grid[player.Coordinates.X, player.Coordinates.Y];


    public void MaelstromCollision(Player player)
    {
        Maelstrom tempMaelstrom = CurrentRoom.Hazard as Maelstrom;

        tempMaelstrom.TriggerMovePlayer(player);

        CurrentRoom.DestroyHazard();
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
        if (typeof(T) == typeof(Pit))
        {
            // I originally conceived this convoluted method to determine the number of hazards with a formula for infinitely expansive grid size
            numberOfHazards = sizeSelect switch
            {
                AreaSize.Small => (SmallGrid.X * SmallGrid.Y) / (SmallGrid.X * 2),      // 2
                AreaSize.Medium => (MediumGrid.X * MediumGrid.Y) / (MediumGrid.X * 2),  // 3
                AreaSize.Large => (LargeGrid.X * LargeGrid.Y) / (LargeGrid.X * 2),      // 4
                _ => 1
            };

            Pit[] pits = new Pit[numberOfHazards];

            for (int i = 0; i < pits.Length; i++)
            {
                pits[i] = new(this);

                Grid[pits[i].Coordinates.X, pits[i].Coordinates.Y].DefineRoomHazard<Pit>(pits[i]);
            }

            return;
        }

        if (typeof(T) == typeof(Maelstrom))
        {
            numberOfHazards = sizeSelect switch
            {
                AreaSize.Small => 1,
                AreaSize.Medium => 1,
                AreaSize.Large => 2,
                _ => 1
            };

            Maelstrom[] maelstroms = new Maelstrom[numberOfHazards];

            for (int i = 0; i < maelstroms.Length; i++)
            {
                maelstroms[i] = new(this);

                Grid[maelstroms[i].Coordinates.X, maelstroms[i].Coordinates.Y].DefineRoomHazard<Maelstrom>(maelstroms[i]);
            }

            return;
        }

        if (typeof(T) == typeof(Amarok))
        {
            numberOfHazards = sizeSelect switch
            {
                AreaSize.Small => 1,
                AreaSize.Medium => 2,
                AreaSize.Large => 3,
                _ => 1
            };

            Amarok[] amaroks = new Amarok[numberOfHazards];

            for (int i = 0; i < amaroks.Length; i++)
            {
                amaroks[i] = new(this);

                Grid[amaroks[i].Coordinates.X, amaroks[i].Coordinates.Y].DefineRoomHazard<Amarok>(amaroks[i]);
            }

            return;
        }
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
                if (j == player.Coordinates.X && i == player.Coordinates.Y)
                    Console.Write("_C_|");

                // Draws pit locations
                else if (Grid[j, i].HazardType == typeof(Pit))
                    Console.Write("_P_|");

                // Draws maelstrom locations
                else if (Grid[j, i].HazardType == typeof(Maelstrom))
                    Console.Write("_M_|");

                // Draws amarok locations
                else if (Grid[j, i].HazardType == typeof(Amarok))
                    Console.Write("_A_|");

                // Draws fountain location if conditions are met (Intended for debug only)
                else if (j == Fountain.Coordinates.X && i == Fountain.Coordinates.Y)
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

        // Adding a line break to clean up before any other displays
        Console.WriteLine();
    }
}

public class Room
{
    public Coordinate Coordinates { get; set; }
    // This might need to be hard-typed instead of casting
    public Hazard Hazard { get; private set; }
    public Type HazardType { get; private set; }
    public bool ContainsFountain { get; init; } = false;

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

    public void DefineRoomHazard<T>(Hazard roomHazard)
    {
        Hazard = roomHazard;
        HazardType = typeof(T);
    }

    public bool HasHazard() => Hazard != null;
    public void DestroyHazard() 
    {
        Hazard = null;
        HazardType = null;
    }
}

public class Fountain
{
    public Coordinate Coordinates { get; init; }
    public bool Status { get; private set; }

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
                    (byte)Options.Shoot => Options.Shoot,
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

        // Proceed if the move is determined valid (InvalidMoveCheck returns false)
        X += x;
        Y += y;
    }

    // If any of the listed conditions are met, the move is considered invalid. (Still feels clunky by passing in PlayArea argument, but better than being handled by Player object).
    public static bool InvalidMoveCheck(int x, int y, PlayArea playspace) => (x < 0 || y < 0) || (x >= playspace.GridSize.X || y >= playspace.GridSize.Y);
}

public static class Communicator
{
    public static string EmptyRoom { get; } = "There's nothing to sense here.";
    public static string Entrance { get; } = "Bright light emanates from the cavern's mouth. You're at the entrance.";
    private static string EntranceNear { get; } = "You can see a faint light reaching out to you from the cavern's entrance.";
    private static string FountainOffNear { get; } = "You hear distant dribbles. It's getting more humid.";
    private static string FountainOnNear { get; } = "You hear rushing water. The air is damp and cool.";
    public static string FountainRoomOff { get; } = "There's a musty smell permeating this room. The air feels...dank.";
    public static string FountainRoomOn { get; } = "The sound of rushing water fills the corridor. The Fountain of Objects has been reactivated!";
    public static string GameIntro { get; } = " You arrive at the entrance to the cavern which contains the Fountain of Objects. Your goal? To venture inside, find and enable the Fountain, and escape with your life." +
                                              "\n Use the information your senses provide to guide you to the room in which the Fountain rests.\n";
    public static string InstructionsIntro { get; } = " \n How to Play\n" +
                                                      " -----------\n";
    public static string Instructions { get; } = " * Move through the cavern to find the Fountain of Objects. Toggle it on, then return to the cavern entrance to win.\n" +
                                                 " * Watch out for Pits! Entering a room with a Pit will invariably lead to death.\n" +
                                                 " * Avoid Maelstroms, sentient winds that will throw you to another room then relocate themselves.\n" +
                                                 " * The undead amaroks roam the cavern halls, and will devour you whole. Avoid them to keep your hide.\n" +
                                                 " * You're armed with a longbow and 5 mighty arrows. Use them wisely to vanquish Maelstroms and Amaroks.";
    public static string HelpText { get; } = "\nHow to Play:\n" +
                                             "  1. Move: Press the arrow key corresponding to the direction you want to move.\n" +
                                             "  2. Shoot: Press the arrow key corresponding to the direction you want to fire an arrow.\n" +
                                             "  3. Toggle Fountain: If you're in the Fountain Room, this command toggles the state of the Fountain (On/Off).\n" +
                                             "  4. Help: Displays details about available commands. How you got here!\n" +
                                             "  5. Quit: Quits the game fully.";
    private static string PitNear { get; } = "You feel a foreboding draft. There is a pit in a nearby room.";
    private static string MaelstromNear { get; } = "A violent gust of wind reverberates through the cavern. There is a maelstrom in a nearby room.";
    private static string AmarokNear { get; } = "The stench of rotten flesh invades your nostrils. There is an amarok in a nearby room.";

    public static void Communicate(Player player, PlayArea playspace)
    {
        /* Color Key:
         *      Yellow: Player location
         *      White: Room description text
         *      Blue: Fountain On text
         *      Magenta: Narrative text
        */

        // Need to determine proper logic for this
        Room nearestHazardRoom = NearestHazard(playspace);

        // Outputs current Player coordinates and Ammo
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(CurrentRoom(playspace));
        Console.WriteLine($"Arrows: {player.Ammo}");

        // Entrance room description
        if (player.Coordinates.X == 0 && player.Coordinates.Y == 0)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Entrance);
        }

        else if (nearestHazardRoom.HazardType != null)
        {
            Console.ForegroundColor = ConsoleColor.White;

            // Found this trick for Type checking in a switch statement off StackOverflow (Checking just typeof(X) throws a "Constant value is expected" error)
            // https://stackoverflow.com/a/65642709
            string hazardText = nearestHazardRoom?.HazardType switch
            {
                var value when value == typeof(Pit) => PitNear,
                var value when value == typeof(Maelstrom) => MaelstromNear,
                var value when value == typeof(Amarok) => AmarokNear,
                _ => PitNear
            };

            Console.WriteLine(hazardText);
        }

        // Close to Fountain, Fountain Off
        else if (playspace.Fountain.Status == false && NearFountain(player, playspace.Fountain))
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(FountainOffNear);
        }

        // Close to Fountain, Fountain On
        else if (playspace.Fountain.Status == true && NearFountain(player, playspace.Fountain))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(FountainOnNear);
        }

        // Close to Entrance
        else if ((player.Coordinates.X == 1 && player.Coordinates.Y == 0) || (player.Coordinates.X == 0 && player.Coordinates.Y == 1))
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(EntranceNear);
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

        /* Commenting out temporarily to attempt to resolve a softlock
        // Close to Pit description
        else if (CloseToPit(player, playspace))
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(PitClose);
        }*/

        // Default empty room text
        else
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(EmptyRoom);
        }
        
        Console.ResetColor();
    }

    // Displays intro + instructions text
    public static void NarrateIntro()
    {
        Console.ForegroundColor = ConsoleColor.Magenta;

        Console.WriteLine(GameIntro + InstructionsIntro + Instructions);

        Console.ResetColor();
    }

    /// <summary>
    /// Check if the player is on any tile which is adjacent to the fountain and returns true if they are.
    /// Cardinals directions checked only.
    /// </summary>
    /// <param name="player">Player</param>
    /// <param name="fountain">Fountain</param>
    /// <returns>bool</returns>
    private static bool NearFountain(Player player, Fountain fountain)
    {
        if ((player.Coordinates.X == fountain.Coordinates.X - 1 && player.Coordinates.Y == fountain.Coordinates.Y) || (player.Coordinates.X == fountain.Coordinates.X && player.Coordinates.Y == fountain.Coordinates.Y - 1) ||
            (player.Coordinates.X == fountain.Coordinates.X + 1 && player.Coordinates.Y == fountain.Coordinates.Y) || (player.Coordinates.X == fountain.Coordinates.X && player.Coordinates.Y == fountain.Coordinates.Y + 1))
                return true;
        
        else return false;
    }

    /// <summary>
    /// Checks if rooms adjacent to current room contains a hazard, then returns the Hazard Type.
    /// </summary>
    /// <param name="playspace">PlayArea</param>
    /// <returns>Hazard</returns>
    private static Room NearestHazard(PlayArea playspace)
    {
        // Behemoth initializer forr the 4 adjacent rooms
        /*Room[] adjacentRooms = new Room[4] { playspace.Grid[playspace.CurrentRoom.Coordinates.X + 1, playspace.CurrentRoom.Coordinates.Y], playspace.Grid[playspace.CurrentRoom.Coordinates.X - 1, playspace.CurrentRoom.Coordinates.Y],
                                             playspace.Grid[playspace.CurrentRoom.Coordinates.X, playspace.CurrentRoom.Coordinates.Y + 1], playspace.Grid[playspace.CurrentRoom.Coordinates.X, playspace.CurrentRoom.Coordinates.Y - 1] };

        foreach(Room room in adjacentRooms)
        {
            if (room.HasHazard())
                return room;

            else continue;
        }*/

        return playspace.CurrentRoom;
    }
    // Commenting out temporarily to try and suss out a softlock (Pit adjacency communicator)
    /*
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
    }*/
    public static void ShowHelpText() => Console.WriteLine(HelpText);
    public static string CurrentRoom(PlayArea playspace) => $"\nCurrent room: ({playspace.CurrentRoom.Coordinates.X},{playspace.CurrentRoom.Coordinates.Y})";
}

// Hazards //

public class Hazard
{
    public Coordinate Coordinates { get; protected set; }
    public PlayArea Playarea { get; protected set; }

    public Hazard(PlayArea playspace)
    {
        Playarea = playspace;
    }

    public bool CheckPlayerCollision(Player player) => player.Coordinates.X == Coordinates.X && player.Coordinates.Y == Coordinates.Y;

    /// <summary>
    /// With the help of ValidateHazardPlacement(), random coordinates are generated here,
    /// and validated to make sure there's no overlap in hazard placement and fountain placement,
    /// or hazards placed on top of other hazards. Returns a tuple used to create a Coordinate record.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected Coordinate GenerateValidRandomCoords()
    {
        // Creating a new Random object to generate numbers
        Random randCoordGeneration = new();

        (int x, int y) tempCoords = (randCoordGeneration.Next(1, Playarea.GridSize.X), randCoordGeneration.Next(1, Playarea.GridSize.Y));
        
        // Continues to generate coordinates until the Room at tempCoords is empty and doesn't contain the fountain
        while (!ValidateHazardPlacement(tempCoords))
            tempCoords = (randCoordGeneration.Next(1, Playarea.GridSize.X), randCoordGeneration.Next(1, Playarea.GridSize.Y));

        // Returns the validated coordinates as a Coordinate record
        return new(tempCoords.x, tempCoords.y);
    }

    /// <summary>
    /// Takes in tuple of coordinates, and checks the playspace to make sure the coordinates don't match fountain coordinates.
    /// If they do match fountain coordinates, then new coordinates are generated.
    /// Then, it calls Room.HasHazard() to see if there's already a hazard in the room. If yes, returns false and new coordinates 
    /// must be generated.
    /// </summary>
    /// <param name="coords"></param>
    /// <returns></returns>
    protected bool ValidateHazardPlacement((int x, int y) coords)
    {
        // Guard statement to immediately return a false result if coordinates match Fountain's coordinates
        if (Playarea.Grid[coords.x, coords.y].ContainsFountain) return false;

        // Guard statement to return false if the room already contains a hazard
        if (Playarea.Grid[coords.x, coords.y].HasHazard()) return false;

        // If the room is empty, return true
        return true;
    }

    protected void UpdateHazardCoordinates(int x, int y) => Coordinates.Update(x, y, Playarea);
}

public class Pit : Hazard
{
    public Pit(PlayArea playspace) : base(playspace) => Coordinates = GenerateValidRandomCoords();
}

public class Maelstrom : Hazard
{
    private IMoveCommands[] ThrowPlayerDirections { get; } = new IMoveCommands[] { new MoveNorth(), new MoveWest(), new MoveWest() };  // Allows Maelstrom displacement directions to be changed easily

    public Maelstrom(PlayArea playspace) : base(playspace) => Coordinates = GenerateValidRandomCoords();

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
    // Should also clear the hazard from the room it lived in before moving, and update Room to contain it
    private void Move()
    {
        Coordinate newCoords = GenerateValidRandomCoords();

        Console.WriteLine($"New coordinates for maelstrom generated: ({newCoords.x},{newCoords.y})");

        // This is returning an invalid move if the maelstrom is on the edge of the arena
        Console.WriteLine("Has hazard?" + Playarea.Grid[newCoords.x, newCoords.y].HasHazard());

        // If the new room for the Maelstrom already has a hazard, keep regenerating coordinates until a suitable room is found
        while (!Playarea.Grid[newCoords.x, newCoords.y].HasHazard())
        {
            Console.WriteLine($"Checking if new room can accept hazard ({newCoords.x},{newCoords.y})");
            if (!Playarea.Grid[newCoords.x, newCoords.y].HasHazard())
            {
                Playarea.Grid[newCoords.x, newCoords.y].DefineRoomHazard<Maelstrom>(this);
                Console.WriteLine($"Added maelstrom to Playarea.Grid at ({newCoords.x},{newCoords.y})");
            }

            else { 
                newCoords = GenerateValidRandomCoords();
            Console.WriteLine($"Generated new coords ({newCoords.x},{newCoords.y})");
            }
        }
    }
}

public class Amarok : Hazard
{ 
    public Amarok(PlayArea playspace) : base(playspace) => Coordinates = GenerateValidRandomCoords();
}

// Player Commands //

public interface IMoveCommands
{
    public void Run(Player player);
}

public class MoveNorth : IMoveCommands
{
    public void Run(Player player) => player.Coordinates.Update(0, -1, player.Playspace);
}

public class MoveSouth : IMoveCommands
{
    public void Run(Player player) => player.Coordinates.Update(0, 1, player.Playspace);
}

public class MoveEast : IMoveCommands
{
    public void Run(Player player) => player.Coordinates.Update(1, 0, player.Playspace);
}

public class MoveWest : IMoveCommands
{
    public void Run(Player player) => player.Coordinates.Update(-1, 0, player.Playspace);
}

public interface IShootCommands
{
    public bool Shoot(PlayArea playSpace);
}

public class ShootNorth : IShootCommands
{
    public bool Shoot(PlayArea playSpace) 
    {
        // Verifies coordinates will be within bounds before checking if a hazard was hit
        if (!Coordinate.InvalidMoveCheck(playSpace.CurrentRoom.Coordinates.X, playSpace.CurrentRoom.Coordinates.Y + 1, playSpace))
        {
            // Defining the targeted Room for use 
            var targetRoom = playSpace.Grid[playSpace.CurrentRoom.Coordinates.X, playSpace.CurrentRoom.Coordinates.Y + 1];

            if (targetRoom.HasHazard() && targetRoom.HazardType != typeof(Pit))
            {
                Console.WriteLine($"Hazard: {targetRoom.HazardType}");
                playSpace.Grid[targetRoom.Coordinates.X, targetRoom.Coordinates.Y].DestroyHazard();
                return true;
            }
        }

        return false;
    }
}

public class ShootEast : IShootCommands
{
    public bool Shoot(PlayArea playSpace) 
    {
        // Verifies coordinates will be within bounds before checking if a hazard was hit
        if (!Coordinate.InvalidMoveCheck(playSpace.CurrentRoom.Coordinates.X + 1, playSpace.CurrentRoom.Coordinates.Y, playSpace))
        {
            // Defining the targeted Room for use 
            var targetRoom = playSpace.Grid[playSpace.CurrentRoom.Coordinates.X + 1, playSpace.CurrentRoom.Coordinates.Y];

            if (targetRoom.HasHazard() && targetRoom.HazardType != typeof(Pit))
            {
                Console.WriteLine($"Hazard: {targetRoom.HazardType}");
                playSpace.Grid[targetRoom.Coordinates.X, targetRoom.Coordinates.Y].DestroyHazard();
                return true;
            }
        }

        return false;
    }
}

public class ShootSouth : IShootCommands
{
    public bool Shoot(PlayArea playSpace) 
    {
        // Verifies coordinates will be within bounds before checking if a hazard was hit
        if (!Coordinate.InvalidMoveCheck(playSpace.CurrentRoom.Coordinates.X, playSpace.CurrentRoom.Coordinates.Y - 1, playSpace))
        {
            // Defining the targeted Room for use 
            var targetRoom = playSpace.Grid[playSpace.CurrentRoom.Coordinates.X, playSpace.CurrentRoom.Coordinates.Y - 1];

            if (targetRoom.HasHazard() && targetRoom.HazardType != typeof(Pit))
            {
                Console.WriteLine($"Hazard: {targetRoom.HazardType}");
                playSpace.Grid[targetRoom.Coordinates.X, targetRoom.Coordinates.Y].DestroyHazard();
                return true;
            }
        }
        
        return false;
    }
}

public class ShootWest : IShootCommands
{
    public bool Shoot(PlayArea playSpace) 
    {
        // Verifies coordinates will be within bounds before checking if a hazard was hit
        if (!Coordinate.InvalidMoveCheck(playSpace.CurrentRoom.Coordinates.X - 1, playSpace.CurrentRoom.Coordinates.Y, playSpace))
        {
            // Defining the targeted Room for use 
            var targetRoom = playSpace.Grid[playSpace.CurrentRoom.Coordinates.X - 1, playSpace.CurrentRoom.Coordinates.Y];

            if (targetRoom.HasHazard() && targetRoom.HazardType != typeof(Pit))
            {
                Console.WriteLine($"Hazard: {targetRoom.HazardType}");
                playSpace.Grid[targetRoom.Coordinates.X, targetRoom.Coordinates.Y].DestroyHazard();
                return true;
            }
        }
        
        return false;
    }
}

public enum Options { Move = 1, Shoot, ToggleFountain, Help, Quit }
public enum AreaSize { Small = 1, Medium, Large }