using Godot;
using System.Collections.Generic;

public static class CommandRegistry
{
    public static readonly HashSet<string> ValidCommands = new()
    {
        "Spawn", "Color", "Size", "DrawLine", "DrawCircle",
        "DrawRectangle", "Fill", "GoTo"
    };

    public static bool IsValidCommand(string command) => ValidCommands.Contains(command);
}