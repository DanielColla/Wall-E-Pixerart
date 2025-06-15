using System;
using System.Collections.Generic;
using Godot;

public class Parser
{
    private readonly List<Token> tokens;
    private int current = 0;
    private bool debugMode = true;

    public Parser(List<Token> tokens) => this.tokens = tokens;

    public ProgramNode ParseProgram()
    {
        ProgramNode program = new ProgramNode();
        int errorCount = 0;
        const int maxErrors = 50;

        Log("Iniciando análisis del programa...");

        while (!IsAtEnd() && errorCount < maxErrors)
        {
            try
            {
                // Manejar EOF explícitamente
                if (Check(TokenType.EOF)) break;

                // Saltar líneas vacías
                if (Match(TokenType.NewLine)) continue;

                ASTNode statement = ParseStatement();
                statement.LineNumber = Peek().Line;
                program.Statements.Add(statement);
                Log($"Declaración añadida: {statement.GetType().Name} en línea {statement.LineNumber}");

                // Manejar nueva línea al final de la declaración
                if (!IsAtEnd() && !Check(TokenType.EOF))
                {
                    Match(TokenType.NewLine);
                }
            }
            catch (WallEException e)
            {
                errorCount++;
                GD.PrintErr($"ERROR DE PARSING: {e.Message}");
                if (!string.IsNullOrEmpty(e.Context))
                    GD.PrintErr($"CONTEXTO: {e.Context}");

                // Recuperación de errores: avanzar hasta próximo punto seguro
                while (!IsAtEnd() &&
                      !Check(TokenType.NewLine) &&
                      !Check(TokenType.EOF))
                {
                    Advance();
                }

                // Consumir el token de nueva línea si existe
                Match(TokenType.NewLine);
            }
        }

        Log($"Análisis completado. Errores encontrados: {errorCount}");
        return program;
    }

    private ASTNode ParseStatement()
    {
        Log($"Analizando declaración en token: {Peek().Type} '{Peek().Lexeme}'");

        // 1. Asignaciones: identificador seguido de operador de asignación
        if (Check(TokenType.Identifier) && NextTokenIs(TokenType.Assign))
        {
            Log($"Detectada asignación: {Peek().Lexeme} <- ...");
            return ParseAssignment();
        }

        // 2. Comandos especiales (GoTo primero)
        if (Check(TokenType.GoTo))
        {
            Log("Detectado comando GoTo");
            return ParseGoTo();
        }

        // 3. Comandos regulares
        if (IsCommand(Peek().Type))
        {
            Log($"Detectado comando: {Peek().Type}");
            return ParseCommand();
        }

        // 4. Etiquetas (identificador al inicio de línea)
        if (Check(TokenType.Identifier) && IsLabelLine())
        {
            Log($"Detectada etiqueta: {Peek().Lexeme}");
            return ParseLabel();
        }

        throw ParseError("Instrucción no válida",
            $"Token inesperado: '{Peek().Lexeme}' ({Peek().Type})");
    }

    private bool NextTokenIs(TokenType type)
    {
        int nextIndex = current + 1;
        if (nextIndex < tokens.Count)
        {
            return tokens[nextIndex].Type == type;
        }
        return false;
    }

    private bool IsLabelLine()
    {
        int nextIndex = current + 1;
        if (nextIndex >= tokens.Count) return true;

        Token nextToken = tokens[nextIndex];
        return nextToken.Type == TokenType.NewLine ||
               nextToken.Type == TokenType.EOF;
    }

    private AssignmentNode ParseAssignment()
    {
        Token variable = Consume(TokenType.Identifier, "Se esperaba nombre de variable");
        Log($"Variable de asignación: {variable.Lexeme}");

        // Consumir el operador de asignación
        Consume(TokenType.Assign, "Se esperaba '<-' después del nombre de variable", variable.Line);
        Log("Operador de asignación consumido");

        ExpressionNode expr = ParseExpression();
        Log($"Expresión de asignación analizada: {expr.GetType().Name}");

        return new AssignmentNode(variable.Lexeme, expr) { LineNumber = variable.Line };
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
                do
                {
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

    

    #region Helpers
    private bool Check(TokenType type)
    {
        // Verificar si estamos al final de los tokens
        if (IsAtEnd())
        {
            // Solo EOF coincide cuando estamos al final
            return type == TokenType.EOF;
        }
        return Peek().Type == type;
    }

    private Token Advance()
    {
        // Solo avanzar si no estamos al final
        if (!IsAtEnd())
        {
            current++;
        }
        return Previous();
    }

    private bool IsAtEnd()
    {
        return current >= tokens.Count;
    }

    private Token Peek()
    {
        // Devolver token EOF si estamos al final
        if (IsAtEnd())
        {
            // Crear un token EOF si no existe uno
            return tokens.Count > 0 ? tokens[tokens.Count - 1] :
                new Token(TokenType.EOF, "", null, 0);
        }
        return tokens[current];
    }

    private Token Previous()
    {
        // Manejar caso cuando no hay tokens anteriores
        if (current <= 0)
        {
            // Si no hay tokens, crear uno vacío
            return tokens.Count > 0 ? tokens[0] :
                new Token(TokenType.EOF, "", null, 0);
        }

        // Devolver token anterior seguro
        return tokens[Math.Min(current - 1, tokens.Count - 1)];
    }
    private Token PeekNext()
    {
        if (current + 1 >= tokens.Count) return null;
        return tokens[current + 1];
    }
    private bool Match(params TokenType[] types)
    {
        foreach (TokenType type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }
    #endregion
    #region Utilidades de depuración
    private void Log(string message)
    {
        if (debugMode)
        {
            GD.Print($"[PARSER L:{Peek().Line}] {message}");
        }
    }
    private WallEException ParseError(string message, string context = "")
    {
        Token token = IsAtEnd() ? Previous() : Peek();
        return new WallEException(
            message: $"[Línea {token.Line}] {message}",
            type: WallEException.ErrorType.Sintaxis,
            line: token.Line,
            context: context
        );
    }
    #endregion
}