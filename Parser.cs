using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class Parser
{
    private readonly List<Token> tokens;
    private int current = 0;

    public Parser(List<Token> tokens) => this.tokens = tokens;

    public ProgramNode ParseProgram()
    {
        ProgramNode program = new();
        
        while (Match(TokenType.NewLine)) { }
        
        while (!IsAtEnd())
        {
            try
            {
                if (Match(TokenType.NewLine)) continue;
                
                ASTNode statement = ParseStatement();
                statement.LineNumber = Previous().Line;
                program.Statements.Add(statement);
                
                if (!IsAtEnd() && !Check(TokenType.EOF))
                {
                    while (Match(TokenType.NewLine)) { }
                }
            }
            catch (WallEException e)
            {
                GD.PrintErr($"Error de parsing: {e.Message}");
                while (!IsAtEnd() && !Check(TokenType.NewLine)) Advance();
                Match(TokenType.NewLine);
            }
        }
        return program;
    }

    private ASTNode ParseStatement()
    {
        // Handle assignments FIRST
        if (Check(TokenType.Identifier) && PeekNext()?.Type == TokenType.Assign) 
            return ParseAssignment();
        
        // Then handle commands
        if (Check(TokenType.GoTo)) return ParseGoTo();
        if (IsCommand(Peek().Type)) return ParseCommand();
        
        // Then handle labels
        if (Check(TokenType.Identifier) && IsLabelLine())
            return ParseLabel();
        
        throw ParseError("Instrucción no válida", $"Token inesperado: {Peek().Lexeme} ({Peek().Type})");
    }
    private bool IsCommand(TokenType type)
    {
        return type switch
        {
            TokenType.Spawn or TokenType.Color or TokenType.Size or 
            TokenType.DrawLine or TokenType.DrawCircle or 
            TokenType.DrawRectangle or TokenType.Fill or TokenType.GoTo => true,
            _ => false
        };
    }

   /* private bool IsLabelLine()
    {
        int temp = current;
        while (temp < tokens.Count && tokens[temp].Type == TokenType.Identifier) temp++;
        return temp < tokens.Count && tokens[temp].Type == TokenType.NewLine;
    }*/
    private bool IsLabelLine()
{
    int temp = current;
    return temp + 1 < tokens.Count && tokens[temp].Type == TokenType.Identifier &&
           (tokens[temp + 1].Type == TokenType.NewLine || tokens[temp + 1].Type == TokenType.EOF);
}

    private CommandNode ParseCommand()
    {
        Token commandToken = Advance();
        string commandName = commandToken.Lexeme;
        
        try
        {
            Consume(TokenType.LParen, $"Se esperaba '(' después del comando {commandName}", commandToken.Line);
            
            List<ExpressionNode> args = ParseArguments();
            
            Consume(TokenType.RParen, $"Se esperaba ')' después de los argumentos de {commandName}", commandToken.Line);
            
            return new CommandNode(commandName, args) { LineNumber = commandToken.Line };
        }
        catch (WallEException ex)
        {
            throw new WallEException(
                message: ex.Message,
                type: WallEException.ErrorType.Sintaxis,
                line: ex.Line,
                context: $"Comando: {commandName} | {ex.Context}",
                inner: ex
            );
        }
    }

    private List<ExpressionNode> ParseArguments()
    {
        List<ExpressionNode> args = new();
        while (!Check(TokenType.RParen) && !IsAtEnd())
        {
            try
            {
                args.Add(ParseExpression());
                if (!Match(TokenType.Comma)) break;
            }
            catch (WallEException ex)
            {
                throw new WallEException(
                    message: "Error en argumentos",
                    type: WallEException.ErrorType.Sintaxis,
                    line: ex.Line,
                    context: $"Argumento {args.Count + 1}: {ex.Context}",
                    inner: ex
                );
            }
        }
        return args;
    }

    private AssignmentNode ParseAssignment()
    {
        Token variable = Consume(TokenType.Identifier, "Se esperaba nombre de variable");
        
        if (!Check(TokenType.Assign))
        {
            throw new WallEException(
                message: "Operador de asignación inválido",
                type: WallEException.ErrorType.Sintaxis,
                line: variable.Line,
                context: $"Se esperaba '<-' después de '{variable.Lexeme}'"
            );
        }
        Advance();
        
        ExpressionNode expr = ParseExpression();
        return new AssignmentNode(variable.Lexeme, expr) { LineNumber = variable.Line };
    }

    private LabelNode ParseLabel()
    {
        Token label = Consume(TokenType.Identifier, "Se esperaba nombre de etiqueta");
        return new LabelNode(label.Lexeme) { LineNumber = label.Line };
    }

private GoToNode ParseGoTo()
{
    Token gotoToken = Advance();
    try
    {
        Consume(TokenType.LBracket, "Se esperaba '[' después de GoTo", gotoToken.Line);
        Token label = Consume(TokenType.Identifier, "Se esperaba nombre de etiqueta", gotoToken.Line);
        Consume(TokenType.RBracket, "Se esperaba ']' después de la etiqueta", gotoToken.Line);
        Consume(TokenType.LParen, "Se esperaba '(' antes de la condición", gotoToken.Line);
        ExpressionNode condition = ParseExpression();
        Consume(TokenType.RParen, "Se esperaba ')' después de la condición", gotoToken.Line);
        return new GoToNode(label.Lexeme, condition) { LineNumber = gotoToken.Line };
    }
    catch (WallEException ex)
    {
        throw new WallEException(
            message: $"Error en GoTo: {ex.Message}",
            type: WallEException.ErrorType.Sintaxis,
            line: ex.Line,
            context: $"Estructura: GoTo [etiqueta] (condición)",
            inner: ex
        );
    }
}


    private ExpressionNode ParseExpression() => ParseLogicalOr();

    private ExpressionNode ParseLogicalOr()
    {
        ExpressionNode expr = ParseLogicalAnd();
        while (Match(TokenType.Or))
        {
            Token op = Previous();
            ExpressionNode right = ParseLogicalAnd();
            expr = new BinaryNode(expr, op.Type, right) { LineNumber = op.Line };
        }
        return expr;
    }

    private ExpressionNode ParseLogicalAnd()
    {
        ExpressionNode expr = ParseEquality();
        while (Match(TokenType.And))
        {
            Token op = Previous();
            ExpressionNode right = ParseEquality();
            expr = new BinaryNode(expr, op.Type, right) { LineNumber = op.Line };
        }
        return expr;
    }

    private ExpressionNode ParseEquality()
    {
        ExpressionNode expr = ParseComparison();
        while (Match(TokenType.Equal))
        {
            Token op = Previous();
            ExpressionNode right = ParseComparison();
            expr = new BinaryNode(expr, op.Type, right) { LineNumber = op.Line };
        }
        return expr;
    }

    private ExpressionNode ParseComparison()
    {
        ExpressionNode expr = ParseTerm();
        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            Token op = Previous();
            ExpressionNode right = ParseTerm();
            expr = new BinaryNode(expr, op.Type, right) { LineNumber = op.Line };
        }
        return expr;
    }

    private ExpressionNode ParseTerm()
    {
        ExpressionNode expr = ParseFactor();
        while (Match(TokenType.Plus, TokenType.Minus))
        {
            Token op = Previous();
            ExpressionNode right = ParseFactor();
            expr = new BinaryNode(expr, op.Type, right) { LineNumber = op.Line };
        }
        return expr;
    }

    private ExpressionNode ParseFactor()
    {
        ExpressionNode expr = ParsePower();
        while (Match(TokenType.Multiply, TokenType.Divide, TokenType.Modulo))
        {
            Token op = Previous();
            ExpressionNode right = ParsePower();
            expr = new BinaryNode(expr, op.Type, right) { LineNumber = op.Line };
        }
        return expr;
    }

    private ExpressionNode ParsePower()
    {
        ExpressionNode expr = ParseUnary();
        while (Match(TokenType.Power))
        {
            Token op = Previous();
            ExpressionNode right = ParseUnary();
            expr = new BinaryNode(expr, op.Type, right) { LineNumber = op.Line };
        }
        return expr;
    }

    private ExpressionNode ParseUnary()
    {
        if (Match(TokenType.Minus))
        {
            Token op = Previous();
            ExpressionNode right = ParseUnary();
            return new BinaryNode(new LiteralNode(0), op.Type, right) { LineNumber = op.Line };
        }
        return ParsePrimary();
    }

    private ExpressionNode ParsePrimary()
    {
        if (Match(TokenType.Number))
            return new LiteralNode(Previous().Literal) { LineNumber = Previous().Line };
        
        if (Match(TokenType.String))
            return new LiteralNode(Previous().Literal) { LineNumber = Previous().Line };
        
        if (Match(TokenType.LParen))
        {
            ExpressionNode expr = ParseExpression();
            Consume(TokenType.RParen, "Se esperaba ')' después de la expresión");
            return expr;
        }
        
        if (Match(TokenType.Identifier))
        {
            string identifier = Previous().Lexeme;
            if (Check(TokenType.LParen))
                return ParseFunctionCall(identifier);
            else
                return new VariableNode(identifier) { LineNumber = Previous().Line };
        }
        
        throw ParseError("Expresión primaria inválida", $"Token: {Peek().Lexeme}");
    }

    private FunctionCallNode ParseFunctionCall(string functionName)
    {
        Token funcToken = Previous();
        try
        {
            Consume(TokenType.LParen, $"Se esperaba '(' después de la función {functionName}", funcToken.Line);
            List<ExpressionNode> args = new();
            
            if (!Check(TokenType.RParen))
            {
                do {
                    args.Add(ParseExpression());
                } while (Match(TokenType.Comma));
            }
            
            Consume(TokenType.RParen, $"Se esperaba ')' después de los argumentos de {functionName}", funcToken.Line);
            return new FunctionCallNode(functionName, args) { LineNumber = funcToken.Line };
        }
        catch (WallEException ex)
        {
            throw new WallEException(
                message: ex.Message,
                type: WallEException.ErrorType.Semantico,
                line: ex.Line,
                context: $"Función: {functionName} | {ex.Context}",
                inner: ex
            );
        }
    }

    private Token Consume(TokenType type, string message, int? line = null)
    {
        if (Check(type)) return Advance();
        
        Token errorToken = Peek();
        throw new WallEException(
            message: message,
            type: WallEException.ErrorType.Sintaxis,
            line: line ?? errorToken.Line,
            context: $"Recibido: '{errorToken.Lexeme}' ({errorToken.Type})"
        );
    }

    private WallEException ParseError(string message, string context = "")
    {
        Token token = IsAtEnd() ? Previous() : Peek();
        return new WallEException(
            message: message,
            type: WallEException.ErrorType.Sintaxis,
            line: token.Line,
            context: context
        );
    }

    #region Helpers
    private bool Check(TokenType type) => !IsAtEnd() && Peek().Type == type;
    private Token Advance() => tokens[current++];
    private bool IsAtEnd() => current >= tokens.Count;
    private Token Peek() => tokens[current];
    private Token Previous() => tokens[current - 1];
    private Token PeekNext() => current + 1 < tokens.Count ? tokens[current + 1] : null;
    private bool Match(params TokenType[] types) => types.Any(Check) ? (Advance() != null) : false;
    #endregion
}