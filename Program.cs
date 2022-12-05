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

        playArea.DrawPlayspace();  // Debug tool

        do
        {

        } while (true);
    }

    private static bool CheckForWin(Player player, Fountain fountain) => (player.Coordinates.X == 0 & player.Coordinates.Y == 0) && fountain.Status == true;
}

public class Player
{
    public Coordinate Coordinates { get; set; } = new();

    public Player()
    {
        Coordinates.X = 0;
        Coordinates.Y = 0;
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

    //public void Repeat

    public string GetPlayerInput() => Console.ReadLine().Trim().ToLower();
}

public class PlayArea
{
    public Room[,] Playspace;
    public (int X, int Y) GridSize { get; set; }
    private (int X, int Y) SmallGrid = (4, 4);
    private (int X, int Y) MediumGrid = (6, 6);
    private (int X, int Y) LargeGrid = (8, 8);

    public PlayArea(Fountain fountain)
    {
        GridSize = (SmallGrid);
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

public record Communicator
{
    public string EmptyRoom { get; } = "There's nothing to sense here.";
    public string FountainRoomOff { get; } = "There's a musty smell permeating this room. The air feels...damp.";
    public string FountainRoomOn { get; } = "The sound of rushing watern fills the corridor. The Fountain of Objects has been reactivated!";

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