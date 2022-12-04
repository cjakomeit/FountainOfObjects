using System;

// Main will start HERE

public static class GameRunner
{

}

public class Player
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsPowered { get; set; } = false;
    public ICommands[] Commands { get; } = new ICommands[3];

    public Player()
    {

    }
}

public static class PlayArea
{

}

public static class Room
{

}

public record Communicator
{

}

public struct Fountain
{

}

// Commands

public interface ICommands
{
    void Run(Player player);
}

public class MoveNorth : ICommands
{
    public void Run(Player player) => player.Y += 1;
}

public class MoveSouth : ICommands
{
    public void Run(Player player) => player.Y -= 1;
}

public class MoveEast : ICommands
{
    public void Run(Player player) => player.X += 1;
}

public class MoveWest : ICommands
{
    public void Run(Player player) => player.X -= 1;
}

public class ToggleFountain : ICommands
{

}

public class RepeatInfo : ICommands
{

}