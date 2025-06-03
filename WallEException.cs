using System;

public class WallEException : Exception
{
    public enum ErrorType { Sintaxis, Semantico, Ejecucion }
    
    public ErrorType Type { get; }
    public int Line { get; set; }
    public string Context { get; }

    public WallEException(string message, ErrorType type, int line = -1, string context = "", Exception inner = null)
        : base(FormatMessage(message, line, context), inner)
    {
        Type = type;
        Line = line;
        Context = context;
    }

    private static string FormatMessage(string message, int line, string context)
    {
        string header = line > 0 ? $"[Línea {line}] " : "";
        string footer = !string.IsNullOrEmpty(context) ? $"\nContexto: {context}" : "";
        return $"{header}{message}{footer}";
    }
}