public enum TokenType
{
    // Lista completa de tipos de token
    Spawn, Color, Size, DrawLine, DrawCircle, DrawRectangle, Fill, GoTo,
    GetActualX, GetActualY, GetCanvasSize, GetColorCount, IsBrushColor, 
    IsBrushSize, IsCanvasColor, Identifier, Number, String, Assign, 
    LParen, RParen, LBracket, RBracket, Comma, NewLine, EOF,
    Plus, Minus, Multiply, Divide, Power, Modulo, And, Or, 
    Equal, GreaterEqual, LessEqual, Greater, Less
}

public class Token
{
    public TokenType Type { get; }
    public string Lexeme { get; }
    public object Literal { get; }
    public int Line { get; }

    public Token(TokenType type, string lexeme, object literal, int line)
    {
        Type = type;
        Lexeme = lexeme;
        Literal = literal;
        Line = line;
    }

    public override string ToString() => $"{Type} '{Lexeme}' {Literal}";
}