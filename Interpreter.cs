using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Interpreter
{
    public CanvasRenderer Canvas { get; set; }
    private Dictionary<string, object> variables = new();
    private Dictionary<string, int> labels = new();
    private Vector2I position;
    private Color currentColor = Colors.Transparent;
    private int brushSize = 1;
    private bool spawned = false;
    private int programCounter = 0;
    
    private static readonly Dictionary<string, Color> colorMap = new()
    {
        {"Red", Colors.Red}, {"Blue", Colors.Blue},
        {"Green", Colors.Green}, {"Yellow", Colors.Yellow},
        {"Orange", Colors.Orange}, {"Purple", Colors.Purple},
        {"Black", Colors.Black}, {"White", Colors.White},
        {"Transparent", Colors.Transparent}
    };

    public void Execute(ProgramNode program)
    {
        ResetState();
        PreprocessLabels(program);
        
        while (programCounter < program.Statements.Count)
        {
            try
            {
                var stmt = program.Statements[programCounter];
                HandleStatement(stmt);
                programCounter++;
            }
            catch (WallEException ex)
            {
                ex.Line = GetLineNumber(program, programCounter);
                throw;
            }
        }
    }

    private void ResetState()
    {
        variables.Clear();
        labels.Clear();
        position = Vector2I.Zero;
        currentColor = Colors.Transparent;
        brushSize = 1;
        spawned = false;
        programCounter = 0;
    }

    private void HandleStatement(ASTNode stmt)
    {
        switch (stmt)
        {
            case CommandNode cmd:
                ExecuteCommand(cmd);
                break;
            case AssignmentNode assignment:
                ExecuteAssignment(assignment);
                break;
            case GoToNode gotoNode:
                programCounter = HandleGoTo(gotoNode, programCounter);
                break;
            case LabelNode:
                break;
            default:
                throw new WallEException("Tipo de instrucción no reconocido", 
                    WallEException.ErrorType.Semantico, stmt.LineNumber);
        }
    }

    private void ExecuteCommand(CommandNode cmd)
    {
        switch (cmd.Command)
        {
            case "Spawn":
                HandleSpawn(cmd.Arguments);
                break;
            case "Color":
                HandleColor(cmd.Arguments);
                break;
            case "Size":
                HandleSize(cmd.Arguments);
                break;
            case "DrawLine":
                HandleDrawLine(cmd.Arguments);
                break;
            case "DrawCircle":
                HandleDrawCircle(cmd.Arguments);
                break;
            case "DrawRectangle":
                HandleDrawRectangle(cmd.Arguments);
                break;
            case "Fill":
                HandleFill();
                break;
            default:
                throw new WallEException($"Comando desconocido: {cmd.Command}", 
                    WallEException.ErrorType.Sintaxis, cmd.LineNumber);
        }
    }

    private void HandleSpawn(List<ExpressionNode> args)
    {
        if (spawned) throw new WallEException("Spawn solo puede llamarse una vez", 
            WallEException.ErrorType.Semantico, args[0].LineNumber);
        if (args.Count != 2) throw new WallEException("Spawn requiere 2 argumentos", 
            WallEException.ErrorType.Sintaxis, args[0].LineNumber);

        int x = Convert.ToInt32(Evaluate(args[0]));
        int y = Convert.ToInt32(Evaluate(args[1]));
        
        ValidatePosition(x, y);
        position = new Vector2I(x, y);
        spawned = true;
    }

    private void HandleColor(List<ExpressionNode> args)
    {
        if (args.Count != 1) throw new WallEException("Color requiere 1 argumento", 
            WallEException.ErrorType.Sintaxis, args[0].LineNumber);
        currentColor = ParseColor(Evaluate(args[0]).ToString());
    }

    private void HandleSize(List<ExpressionNode> args)
    {
        if (args.Count != 1) throw new WallEException("Size requiere 1 argumento", 
            WallEException.ErrorType.Sintaxis, args[0].LineNumber);
        brushSize = AdjustBrushSize(Convert.ToInt32(Evaluate(args[0])));
    }

    private void HandleDrawLine(List<ExpressionNode> args)
    {
        if (args.Count != 3) throw new WallEException("DrawLine requiere 3 argumentos", 
            WallEException.ErrorType.Sintaxis, args[0].LineNumber);
        
        int dirX = ClampDirection(Convert.ToInt32(Evaluate(args[0])));
        int dirY = ClampDirection(Convert.ToInt32(Evaluate(args[1])));
        int distance = Convert.ToInt32(Evaluate(args[2]));
        
        Vector2I direction = new Vector2I(dirX, dirY);
        Vector2I end = position + (direction * distance);
        
        ValidatePosition(end.X, end.Y);
        Canvas.DrawLine(position, end, brushSize, currentColor);
        position = end;
    }

    private void HandleDrawCircle(List<ExpressionNode> args)
    {
        if (args.Count != 3) throw new WallEException("DrawCircle requiere 3 argumentos", 
            WallEException.ErrorType.Sintaxis, args[0].LineNumber);
        
        int dirX = ClampDirection(Convert.ToInt32(Evaluate(args[0])));
        int dirY = ClampDirection(Convert.ToInt32(Evaluate(args[1])));
        int radius = Convert.ToInt32(Evaluate(args[2]));
        
        Vector2I direction = new Vector2I(dirX, dirY);
        Vector2I center = position + (direction * radius);
        
        ValidatePosition(center.X, center.Y);
        Canvas.DrawCircle(center, radius, brushSize, currentColor);
        position = center;
    }

    private void HandleDrawRectangle(List<ExpressionNode> args)
    {
        if (args.Count != 5) throw new WallEException("DrawRectangle requiere 5 argumentos", 
            WallEException.ErrorType.Sintaxis, args[0].LineNumber);
        
        int dirX = ClampDirection(Convert.ToInt32(Evaluate(args[0])));
        int dirY = ClampDirection(Convert.ToInt32(Evaluate(args[1])));
        int distance = Convert.ToInt32(Evaluate(args[2]));
        int width = Convert.ToInt32(Evaluate(args[3]));
        int height = Convert.ToInt32(Evaluate(args[4]));
        
        Vector2I direction = new Vector2I(dirX, dirY);
        Vector2I center = position + (direction * distance);
        
        ValidatePosition(center.X, center.Y);
        Canvas.DrawRectangle(center, width, height, brushSize, currentColor);
        position = center;
    }

    private void HandleFill()
    {
        if (!spawned) throw new WallEException("Ejecuta Spawn primero", 
            WallEException.ErrorType.Semantico);
        Canvas.Fill(position, currentColor);
    }

    private void ExecuteAssignment(AssignmentNode assignment)
    {
        variables[assignment.Variable] = Evaluate(assignment.Expression);
    }

    private int HandleGoTo(GoToNode gotoNode, int currentPc)
    {
        bool condition = Convert.ToBoolean(Evaluate(gotoNode.Condition));
        if (!condition) return currentPc;
        
        if (!labels.TryGetValue(gotoNode.Label, out int targetLine))
            throw new WallEException($"Etiqueta no definida: {gotoNode.Label}", 
                WallEException.ErrorType.Semantico, gotoNode.LineNumber);
        
        return targetLine - 1;
    }

    private object Evaluate(ExpressionNode node)
    {
        try
        {
            return node switch
            {
                LiteralNode lit => lit.Value,
                VariableNode var => GetVariableValue(var),
                BinaryNode bin => EvaluateBinary(bin),
                FunctionCallNode func => EvaluateFunction(func),
                _ => throw new WallEException("Tipo de expresión no soportado", 
                    WallEException.ErrorType.Sintaxis, node.LineNumber)
            };
        }
        catch (Exception ex)
        {
            throw new WallEException($"Error evaluando expresión: {ex.Message}", 
                WallEException.ErrorType.Ejecucion, node.LineNumber, "", ex);
        }
    }

    private object GetVariableValue(VariableNode var)
    {
        if (!variables.ContainsKey(var.Name))
            throw new WallEException($"Variable no definida: {var.Name}", 
                WallEException.ErrorType.Semantico, var.LineNumber);
        
        return variables[var.Name];
    }

    private object EvaluateBinary(BinaryNode bin)
    {
        dynamic left = Evaluate(bin.Left);
        dynamic right = Evaluate(bin.Right);

        ValidateOperands(bin.Operator, left, right);

        return bin.Operator switch
        {
            TokenType.Plus => left + right,
            TokenType.Minus => left - right,
            TokenType.Multiply => left * right,
            TokenType.Divide => SafeDivide(left, right),
            TokenType.Power => Math.Pow(left, right),
            TokenType.Modulo => left % right,
            TokenType.Equal => left == right,
            TokenType.Greater => left > right,
            TokenType.Less => left < right,
            TokenType.GreaterEqual => left >= right,
            TokenType.LessEqual => left <= right,
            TokenType.And => left && right,
            TokenType.Or => left || right,
            _ => throw new WallEException($"Operador no soportado: {bin.Operator}", 
                WallEException.ErrorType.Sintaxis, bin.LineNumber)
        };
    }

    private object EvaluateFunction(FunctionCallNode func)
    {
        return func.FunctionName switch
        {
            "GetActualX" => position.X,
            "GetActualY" => position.Y,
            "GetCanvasSize" => Canvas.GetSize(),
            "GetColorCount" => HandleGetColorCount(func.Arguments),
            "IsBrushColor" => HandleIsBrushColor(func.Arguments),
            "IsBrushSize" => HandleIsBrushSize(func.Arguments),
            "IsCanvasColor" => HandleIsCanvasColor(func.Arguments),
            _ => throw new WallEException($"Función no definida: {func.FunctionName}", 
                WallEException.ErrorType.Semantico, func.LineNumber)
        };
    }

    private int HandleGetColorCount(List<ExpressionNode> args)
    {
        if (args.Count != 5) throw new WallEException("GetColorCount requiere 5 argumentos", 
            WallEException.ErrorType.Sintaxis, args[0].LineNumber);
        
        string color = Evaluate(args[0]).ToString();
        int x1 = Convert.ToInt32(Evaluate(args[1]));
        int y1 = Convert.ToInt32(Evaluate(args[2]));
        int x2 = Convert.ToInt32(Evaluate(args[3]));
        int y2 = Convert.ToInt32(Evaluate(args[4]));
        
        return Canvas.GetColorCount(ParseColor(color), x1, y1, x2, y2);
    }

    private int HandleIsBrushColor(List<ExpressionNode> args)
    {
        if (args.Count != 1) throw new WallEException("IsBrushColor requiere 1 argumento", 
            WallEException.ErrorType.Sintaxis, args[0].LineNumber);
        return currentColor == ParseColor(Evaluate(args[0]).ToString()) ? 1 : 0;
    }

    private int HandleIsBrushSize(List<ExpressionNode> args)
    {
        if (args.Count != 1) throw new WallEException("IsBrushSize requiere 1 argumento", 
            WallEException.ErrorType.Sintaxis, args[0].LineNumber);
        return brushSize == Convert.ToInt32(Evaluate(args[0])) ? 1 : 0;
    }

    private int HandleIsCanvasColor(List<ExpressionNode> args)
    {
        if (args.Count != 3) throw new WallEException("IsCanvasColor requiere 3 argumentos", 
            WallEException.ErrorType.Sintaxis, args[0].LineNumber);
        
        string color = Evaluate(args[0]).ToString();
        int vertical = Convert.ToInt32(Evaluate(args[1]));
        int horizontal = Convert.ToInt32(Evaluate(args[2]));
        
        Vector2I checkPos = position + new Vector2I(horizontal, vertical);
        return Canvas.CheckColor(checkPos, ParseColor(color)) ? 1 : 0;
    }

    private void PreprocessLabels(ProgramNode program)
    {
        labels.Clear();
        for (int i = 0; i < program.Statements.Count; i++)
        {
            if (program.Statements[i] is LabelNode label)
            {
                if (labels.ContainsKey(label.Name))
                    throw new WallEException($"Etiqueta duplicada: {label.Name}", 
                        WallEException.ErrorType.Semantico, i);
                labels[label.Name] = i;
            }
        }
    }

    private void ValidatePosition(int x, int y)
    {
        if (!Canvas.IsPositionValid(x, y))
            throw new WallEException($"Posición inválida: ({x}, {y})", 
                WallEException.ErrorType.Ejecucion);
    }

    private Color ParseColor(string color)
    {
        if (!colorMap.TryGetValue(color, out Color value))
            throw new WallEException($"Color inválido: {color}", 
                WallEException.ErrorType.Semantico);
        return value;
    }

    private int AdjustBrushSize(int size)
    {
        if (size <= 0) throw new WallEException("Tamaño de pincel debe ser positivo", 
            WallEException.ErrorType.Semantico);
        return size % 2 == 0 ? size - 1 : size;
    }

    private int ClampDirection(int value)
    {
        return Math.Clamp(value, -1, 1);
    }

    private void ValidateOperands(TokenType op, dynamic left, dynamic right)
    {
        if (op == TokenType.Divide && right == 0)
            throw new WallEException("División por cero", WallEException.ErrorType.Ejecucion);
        
        if (op == TokenType.Modulo && right == 0)
            throw new WallEException("Módulo por cero", WallEException.ErrorType.Ejecucion);
    }

    private dynamic SafeDivide(dynamic a, dynamic b)
    {
        if (b == 0) throw new WallEException("División por cero", 
            WallEException.ErrorType.Ejecucion);
        return a / b;
    }

    private int GetLineNumber(ProgramNode program, int pc)
    {
        if (pc < 0 || pc >= program.Statements.Count) return -1;
        return program.Statements[pc].LineNumber;
    }
}