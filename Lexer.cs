using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

public class Lexer
{
    private readonly string source;
    private int start = 0;
    private int current = 0;
    private int line = 1;

    // Diccionario seguro sin comparador
    private static readonly Dictionary<string, TokenType> keywords = 
        new Dictionary<string, TokenType>()
    {
        {"spawn", TokenType.Spawn}, 
        {"color", TokenType.Color},
        {"size", TokenType.Size}, 
        {"drawline", TokenType.DrawLine},
        {"drawcircle", TokenType.DrawCircle}, 
        {"drawrectangle", TokenType.DrawRectangle},
        {"fill", TokenType.Fill}, 
        {"goto", TokenType.GoTo},
        {"getactualx", TokenType.GetActualX}, 
        {"getactualy", TokenType.GetActualY},
        {"getcanvassize", TokenType.GetCanvasSize}, 
        {"getcolorcount", TokenType.GetColorCount},
        {"isbrushcolor", TokenType.IsBrushColor}, 
        {"isbrushsize", TokenType.IsBrushSize},
        {"iscanvascolor", TokenType.IsCanvasColor}, 
        {"and", TokenType.And}, 
        {"or", TokenType.Or}
    };

    public Lexer(string source) => this.source = source;

    public List<Token> Tokenize()
    {
        List<Token> tokens = new List<Token>();
        while (!IsAtEnd())
        {
            start = current;
            char c = Advance();
            switch (c)
            {
                case ' ': case '\r': case '\t': break;
                case '\n': AddToken(tokens, TokenType.NewLine, null); line++; break;
                case '(': AddToken(tokens, TokenType.LParen, null); break;
                case ')': AddToken(tokens, TokenType.RParen, null); break;
                case '[': AddToken(tokens, TokenType.LBracket, null); break;
                case ']': AddToken(tokens, TokenType.RBracket, null); break;
                case ',': AddToken(tokens, TokenType.Comma, null); break;
                case '+': AddToken(tokens, TokenType.Plus, null); break;
                case '-': HandleMinus(tokens); break;
                case '*': HandleAsterisk(tokens); break;
                case '/': AddToken(tokens, TokenType.Divide, null); break;
                case '%': AddToken(tokens, TokenType.Modulo, null); break;
                case '=': HandleEqual(tokens); break;
                case '>': HandleGreater(tokens); break;
                case '<': HandleLess(tokens); break;
                case '"': String(tokens); break;
                case '&' when Match('&'): AddToken(tokens, TokenType.And, null); break;
                case '|' when Match('|'): AddToken(tokens, TokenType.Or, null); break;
                default:
                    if (char.IsDigit(c)) Number(tokens);
                    else if (char.IsLetter(c)) Identifier(tokens);
                    else throw new WallEException($"Carácter inesperado: '{c}'", 
                        WallEException.ErrorType.Sintaxis, line);
                    break;
            }
        }
        tokens.Add(new Token(TokenType.EOF, "", null, line));
        return tokens;
    }

    private void HandleMinus(List<Token> tokens)
    {
        if (Match('<')) 
        {
            AddToken(tokens, TokenType.Assign, "<-");
        }
        else
        {
            AddToken(tokens, TokenType.Minus, null);
        }
    }

    private void HandleAsterisk(List<Token> tokens)
    {
        if (Match('*')) AddToken(tokens, TokenType.Power, null);
        else AddToken(tokens, TokenType.Multiply, null);
    }

    private void HandleEqual(List<Token> tokens)
    {
        if (Match('=')) AddToken(tokens, TokenType.Equal, null);
        else throw new WallEException("'=' sin usar", 
            WallEException.ErrorType.Sintaxis, line);
    }

    private void HandleGreater(List<Token> tokens)
    {
        if (Match('=')) AddToken(tokens, TokenType.GreaterEqual, null);
        else AddToken(tokens, TokenType.Greater, null);
    }

    private void HandleLess(List<Token> tokens)
    {
        if (Match('=')) AddToken(tokens, TokenType.LessEqual, null);
        else AddToken(tokens, TokenType.Less, null);
    }

    private void String(List<Token> tokens)
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n')
                throw new WallEException("Cadena no cerrada", 
                    WallEException.ErrorType.Sintaxis, line);
            Advance();
        }
        
        if (IsAtEnd()) throw new WallEException("Cadena sin terminar", 
            WallEException.ErrorType.Sintaxis, line);
        
        Advance();
        string value = source.Substring(start + 1, current - start - 2);
        AddToken(tokens, TokenType.String, value);
    }

    private void Number(List<Token> tokens)
    {
        while (char.IsDigit(Peek())) Advance();
        int value = int.Parse(source.Substring(start, current - start));
        AddToken(tokens, TokenType.Number, value);
    }

 private void Identifier(List<Token> tokens)
    {
        while (char.IsLetterOrDigit(Peek()) || Peek() == '-' || Peek() == '_' || Peek() == '.') 
            Advance();
        
        string text = source.Substring(start, current - start);
        ValidateIdentifier(text);

        // Conversión a minúsculas para comparación insensible
        string key = text.ToLowerInvariant();
        
        if (keywords.TryGetValue(key, out TokenType type))
            AddToken(tokens, type, null);
        else
            AddToken(tokens, TokenType.Identifier, text);
    }

    private void ValidateIdentifier(string text)
    {
        if (char.IsDigit(text[0]))
        {
            throw new WallEException($"Identificador inválido: '{text}'", 
                WallEException.ErrorType.Sintaxis, line);
        }
    }

    private char Advance() => source[current++];
    
    private bool Match(char expected)
    {
        if (IsAtEnd() || source[current] != expected) return false;
        current++;
        return true;
    }
    
    private char Peek() => IsAtEnd() ? '\0' : source[current];
    
    private bool IsAtEnd() => current >= source.Length;
    
    private void AddToken(List<Token> tokens, TokenType type, object literal)
    {
        string lexeme = source.Substring(start, current - start);
        tokens.Add(new Token(type, lexeme, literal, line));
    }
}