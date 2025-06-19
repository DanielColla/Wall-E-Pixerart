using Godot;
using System;
using System.Collections.Generic;

public class Interpreter
{
    public CanvasRenderer Canvas { get; set; }
    private Dictionary<string, object> variables = new Dictionary<string, object>();
    private Dictionary<string, int> labels = new Dictionary<string, int>();
    private Vector2I position;
    private Color currentColor = Colors.Transparent;
    private int brushSize = 1;
    private bool spawned = false;
    private int programCounter = 0;
    private bool debugMode = true;

    private static readonly Dictionary<string, Color> colorMap = new()
    {
        {"Red", Colors.Red}, {"Blue", Colors.Blue}, {"Green", Colors.Green},
        {"Yellow", Colors.Yellow}, {"Orange", Colors.Orange}, {"Purple", Colors.Purple},
        {"Black", Colors.Black}, {"White", Colors.White}, {"Transparent", Colors.Transparent}
    };

 public void Execute(ProgramNode program)
{
    GD.Print("=== INICIANDO EJECUCIÓN ===");
    ResetState();
    PreprocessLabels(program);
    
    while (programCounter < program.Statements.Count)
    {
        var stmt = program.Statements[programCounter];
        GD.Print($"PC: {programCounter} | Tipo: {stmt.GetType().Name}");

        // 1. Manejar etiquetas (no consumen ejecución)
        if (stmt is LabelNode)
        {
            programCounter++;
            continue;
        }

        // 2. Manejar GoTo antes que otros comandos
        if (stmt is GoToNode gotoNode)
        {
            if (HandleGoTo(gotoNode))
            {
                continue; // Saltó, reiniciar ciclo
            }
            else
            {
                programCounter++; // No saltó, continuar normalmente
                continue;
            }
        }

        // 3. Ejecutar otros comandos
        HandleStatement(stmt);
        programCounter++;
    }
}

    private void ResetState()
    {
        this.variables.Clear();
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
            case AssignmentNode assignment:
                // Actualizar el valor de la variable
                variables[assignment.Variable] = Evaluate(assignment.Expression);
                Log($"Variable asignada: {assignment.Variable} = {variables[assignment.Variable]}");
                break;
                
            case CommandNode cmd:
                ExecuteCommand(cmd);
                break;
            
                
            case GoToNode gotoNode:
                if (HandleGoTo(gotoNode)) 
                {
                    Log($"Saltando a etiqueta: {gotoNode.Label}");
                }
                else
                {
                    programCounter++;
                }
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
        
    GD.Print($"Ejecutando comando: {cmd.Command} con color: {currentColor}");
        switch (cmd.Command)
        {
            case "Spawn": HandleSpawn(cmd.Arguments); break;
            case "Color": HandleColor(cmd.Arguments); break;
            case "Size": HandleSize(cmd.Arguments); break;
            case "DrawLine": HandleDrawLine(cmd.Arguments); break;
            case "DrawCircle": HandleDrawCircle(cmd.Arguments); break;
            case "DrawRectangle": HandleDrawRectangle(cmd.Arguments); break;
            case "Fill": HandleFill(); break;
            case "GoTo": break;
            default:
                throw new WallEException($"Comando desconocido: {cmd.Command}",
                    WallEException.ErrorType.Sintaxis, cmd.LineNumber);
        }
    }

    private object Evaluate(ExpressionNode node)
    {
        try
        {
            return node switch
            {
                LiteralNode lit => lit.Value,
                VariableNode var => GetVariableValue(var.Name),
                BinaryNode bin => EvaluateBinary(bin),
                FunctionCallNode func => EvaluateFunction(func),
                _ => throw new WallEException("Tipo de expresión no soportado",
                    WallEException.ErrorType.Sintaxis, node.LineNumber)
            };
        }
        catch (WallEException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new WallEException($"Error evaluando expresión: {ex.Message}",
                WallEException.ErrorType.Ejecucion, node.LineNumber);
        }
    }

    private object GetVariableValue(string name)
    {
        if (!this.variables.ContainsKey(name))
        {
            this.variables[name] = 0;
            Log($"Variable '{name}' no definida. Inicializada a 0");
        }
        
        Log($"Accediendo a variable: {name} = {this.variables[name]}");
        return this.variables[name];
    }
    private void HandleSpawn(List<ExpressionNode> args)
    {
        if (spawned)
            throw new WallEException("Spawn solo puede llamarse una vez",
                WallEException.ErrorType.Semantico, args[0].LineNumber);
        if (args.Count != 2)
            throw new WallEException("Spawn requiere 2 argumentos",
                WallEException.ErrorType.Sintaxis, args[0].LineNumber);

        int x = Convert.ToInt32(Evaluate(args[0]));
        int y = Convert.ToInt32(Evaluate(args[1]));

        ValidatePosition(x, y);
        position = new Vector2I(x, y);
        spawned = true;
    }

   private void HandleColor(List<ExpressionNode> args)
{
    if (args.Count != 1)
        throw new WallEException("Color requiere 1 argumento",
            WallEException.ErrorType.Sintaxis, args[0].LineNumber);
    
    // Evaluar y convertir directamente a string
    string colorName = Evaluate(args[0]).ToString()!;
    currentColor = ParseColor(colorName);
    
    // Depuración
    GD.Print($"Color cambiado a: {colorName} ({currentColor})");
}
    private void HandleSize(List<ExpressionNode> args)
    {
        if (args.Count != 1)
            throw new WallEException("Size requiere 1 argumento",
                WallEException.ErrorType.Sintaxis, args[0].LineNumber);
        brushSize = AdjustBrushSize(Convert.ToInt32(Evaluate(args[0])));
    }

    private void HandleDrawLine(List<ExpressionNode> args)
    {
        if (args.Count != 3)
            throw new WallEException("DrawLine requiere 3 argumentos",
                WallEException.ErrorType.Sintaxis, args[0].LineNumber);

        int dirX = ClampDirection(Convert.ToInt32(Evaluate(args[0])));
        int dirY = ClampDirection(Convert.ToInt32(Evaluate(args[1])));
        int distance = Convert.ToInt32(Evaluate(args[2]));

        Vector2I direction = new(dirX, dirY);
        Vector2I end = position + (direction * distance);

        ValidatePosition(end.X, end.Y);
        Canvas.DrawLine(position, end, brushSize, currentColor);
        position = end;
    }

    private void HandleDrawCircle(List<ExpressionNode> args)
    {
        if (args.Count != 3)
            throw new WallEException("DrawCircle requiere 3 argumentos",
                WallEException.ErrorType.Sintaxis, args[0].LineNumber);

        int dirX = ClampDirection(Convert.ToInt32(Evaluate(args[0])));
        int dirY = ClampDirection(Convert.ToInt32(Evaluate(args[1])));
        int radius = Convert.ToInt32(Evaluate(args[2]));

        Vector2I direction = new(dirX, dirY);
        Vector2I center = position + (direction * radius);

        ValidatePosition(center.X, center.Y);
        Canvas.DrawCircle(center, radius, brushSize, currentColor);
        position = center;
    }

    private void HandleDrawRectangle(List<ExpressionNode> args)
    {
        if (args.Count != 5)
            throw new WallEException("DrawRectangle requiere 5 argumentos",
                WallEException.ErrorType.Sintaxis, args[0].LineNumber);

        int dirX = ClampDirection(Convert.ToInt32(Evaluate(args[0])));
        int dirY = ClampDirection(Convert.ToInt32(Evaluate(args[1])));
        int distance = Convert.ToInt32(Evaluate(args[2]));
        int width = Convert.ToInt32(Evaluate(args[3]));
        int height = Convert.ToInt32(Evaluate(args[4]));

        Vector2I direction = new(dirX, dirY);
        Vector2I center = position + (direction * distance);

        ValidatePosition(center.X, center.Y);
        Canvas.DrawRectangle(center, width, height, brushSize, currentColor);
        position = center;
    }

    private void HandleFill()
    {
        if (!spawned)
            throw new WallEException("Ejecuta Spawn primero",
                WallEException.ErrorType.Semantico);
        Canvas.Fill(position, currentColor);
    }

    private void ExecuteAssignment(AssignmentNode assignment)
    {
        variables[assignment.Variable] = Evaluate(assignment.Expression);
    }

   private bool HandleGoTo(GoToNode gotoNode)
    {
        // Evaluar la condición
        object conditionValue = Evaluate(gotoNode.Condition);
        bool shouldJump = false;
        
        // Convertir a booleano (considerando 1 como true, 0 como false)
        if (conditionValue is int intValue)
        {
            shouldJump = intValue != 0;
        }
        else if (conditionValue is bool boolValue)
        {
            shouldJump = boolValue;
        }
        
        if (shouldJump)
        {
            string labelKey = gotoNode.Label.ToLower();
            if (labels.ContainsKey(labelKey))
            {
                Log($"Saltando a etiqueta: {labelKey} en posición {labels[labelKey]}");
                programCounter = labels[labelKey];  // Actualizar el contador
                return true;  // Indicar que se realizó el salto
            }
            else
            {
                throw new WallEException($"Etiqueta no encontrada: {gotoNode.Label}", 
                    WallEException.ErrorType.Semantico, gotoNode.LineNumber);
            }
        }
        
        return false;  // No se realizó el salto
    }
   

    #region Utilidades de depuración
    private void Log(string message)
    {
        if (debugMode) 
        {
            GD.Print($"[INTERPRETER] {message}");
        }
    }
    #endregion

    private object EvaluateBinary(BinaryNode bin)
    {
        object left = Evaluate(bin.Left);
        object right = Evaluate(bin.Right);

        // Validación de tipos
        if (left.GetType() != right.GetType())
        {
            throw new WallEException(
                $"Tipos incompatibles: {left.GetType().Name} y {right.GetType().Name}",
                WallEException.ErrorType.Ejecucion, bin.LineNumber);
        }

        // Operaciones matemáticas
        if (bin.Operator is TokenType.Plus or TokenType.Minus or TokenType.Multiply
            or TokenType.Divide or TokenType.Power or TokenType.Modulo)
        {
            if (!(left is int) || !(right is int))
                throw new WallEException($"Operandos deben ser numéricos para '{bin.Operator}'",
                    WallEException.ErrorType.Ejecucion, bin.LineNumber);

            int leftInt = (int)left;
            int rightInt = (int)right;

            return bin.Operator switch
            {
                TokenType.Plus => leftInt + rightInt,
                TokenType.Minus => leftInt - rightInt,
                TokenType.Multiply => leftInt * rightInt,
                TokenType.Divide => SafeDivide(leftInt, rightInt),
                TokenType.Power => (int)Math.Pow(leftInt, rightInt),
                TokenType.Modulo => leftInt % rightInt,
                _ => throw new Exception("Operador no manejado")
            };
        }

        // Operaciones de comparación
        if (bin.Operator is TokenType.Equal or TokenType.Greater or TokenType.Less
            or TokenType.GreaterEqual or TokenType.LessEqual)
        {
            if (left is int leftInt && right is int rightInt)
            {
                return bin.Operator switch
                {
                    TokenType.Equal => leftInt == rightInt,
                    TokenType.Greater => leftInt > rightInt,
                    TokenType.Less => leftInt < rightInt,
                    TokenType.GreaterEqual => leftInt >= rightInt,
                    TokenType.LessEqual => leftInt <= rightInt,
                    _ => false
                };
            }
            throw new WallEException($"Comparación requiere operandos numéricos",
                WallEException.ErrorType.Ejecucion, bin.LineNumber);
        }

        // Operaciones lógicas
        if (bin.Operator is TokenType.And or TokenType.Or)
        {
            if (!(left is bool) || !(right is bool))
                throw new WallEException($"Operador '{bin.Operator}' requiere booleanos",
                    WallEException.ErrorType.Ejecucion, bin.LineNumber);

            bool leftBool = (bool)left;
            bool rightBool = (bool)right;

            return bin.Operator switch
            {
                TokenType.And => leftBool && rightBool,
                TokenType.Or => leftBool || rightBool,
                _ => false
            };
        }

        throw new WallEException($"Operador no soportado: {bin.Operator}",
            WallEException.ErrorType.Sintaxis, bin.LineNumber);
    }
   private object EvaluateFunction(FunctionCallNode func)
{
    // Evaluar todos los argumentos primero
    List<object> evaluatedArgs = new List<object>();
    foreach (var arg in func.Arguments)
    {
        evaluatedArgs.Add(Evaluate(arg));
    }

    // Pasar la lista de objetos evaluados
    return func.FunctionName switch
    {
        "GetActualX" => position.X,
        "GetActualY" => position.Y,
        "GetCanvasSize" => Canvas.GetSize(),
        "GetColorCount" => HandleGetColorCount(evaluatedArgs),
        "IsBrushColor" => HandleIsBrushColor(evaluatedArgs),
        "IsBrushSize" => HandleIsBrushSize(evaluatedArgs),
        "IsCanvasColor" => HandleIsCanvasColor(evaluatedArgs),
        _ => throw new WallEException($"Función no definida: {func.FunctionName}",
            WallEException.ErrorType.Semantico, func.LineNumber)
    };
}
private bool IsValidFunction(string name)
    {
        return name switch
        {
            "GetActualX" or "GetActualY" or "GetCanvasSize" or 
            "GetColorCount" or "IsBrushColor" or "IsBrushSize" or 
            "IsCanvasColor" => true,
            _ => false
        };
    }
    private int HandleGetColorCount(List<object> args)
{
    // Validar cantidad de argumentos
    if (args.Count != 5)
        throw new WallEException("GetColorCount requiere 5 argumentos",
            WallEException.ErrorType.Sintaxis);

    // Validar tipo de cada argumento
    if (!(args[0] is string colorName))
        throw new WallEException("Primer argumento debe ser string (nombre de color)",
            WallEException.ErrorType.Ejecucion);
    
    if (!(args[1] is int x1))
        throw new WallEException("Segundo argumento debe ser entero (x1)",
            WallEException.ErrorType.Ejecucion);
    
    if (!(args[2] is int y1))
        throw new WallEException("Tercer argumento debe ser entero (y1)",
            WallEException.ErrorType.Ejecucion);
    
    if (!(args[3] is int x2))
        throw new WallEException("Cuarto argumento debe ser entero (x2)",
            WallEException.ErrorType.Ejecucion);
    
    if (!(args[4] is int y2))
        throw new WallEException("Quinto argumento debe ser entero (y2)",
            WallEException.ErrorType.Ejecucion);

    // Convertir nombre de color a objeto Color
    Color color;
    try
    {
        color = ParseColor(colorName);
    }
    catch (WallEException)
    {
        throw new WallEException($"Color inválido: '{colorName}'",
            WallEException.ErrorType.Semantico);
    }

    // Validar coordenadas
    if (!Canvas.IsPositionValid(x1, y1))
        throw new WallEException($"Coordenada inválida: ({x1}, {y1})",
            WallEException.ErrorType.Ejecucion);
    
    if (!Canvas.IsPositionValid(x2, y2))
        throw new WallEException($"Coordenada inválida: ({x2}, {y2})",
            WallEException.ErrorType.Ejecucion);

    // Llamar al método del canvas
    return Canvas.GetColorCount(color, x1, y1, x2, y2);
}
 private int HandleIsBrushColor(List<object> args)
{
    if (args.Count != 1)
        throw new WallEException("IsBrushColor requiere 1 argumento",
            WallEException.ErrorType.Sintaxis);

    // Validar tipo del argumento
    if (!(args[0] is string colorName))
        throw new WallEException("Argumento debe ser string (nombre de color)",
            WallEException.ErrorType.Ejecucion);

    // Convertir nombre de color a objeto Color
    Color color;
    try
    {
        color = ParseColor(colorName);
    }
    catch (WallEException)
    {
        throw new WallEException($"Color inválido: '{colorName}'",
            WallEException.ErrorType.Semantico);
    }

    // Comparar con el color actual del pincel con tolerancia
    return currentColor.IsEqualApprox(color) ? 1 : 0;
}

private int HandleIsBrushSize(List<object> args)
{
    if (args.Count != 1)
        throw new WallEException("IsBrushSize requiere 1 argumento",
            WallEException.ErrorType.Sintaxis);

    // Validar tipo del argumento
    if (!(args[0] is int size))
        throw new WallEException("Argumento debe ser entero (tamaño de pincel)",
            WallEException.ErrorType.Ejecucion);

    // Comparar con el tamaño actual del pincel
    return brushSize == size ? 1 : 0;
}

private int HandleIsCanvasColor(List<object> args)
{
    if (args.Count != 3)
        throw new WallEException("IsCanvasColor requiere 3 argumentos",
            WallEException.ErrorType.Sintaxis);

    // Validar tipos de los argumentos
    if (!(args[0] is string colorName))
        throw new WallEException("Primer argumento debe ser string (nombre de color)",
            WallEException.ErrorType.Ejecucion);
    
    if (!(args[1] is int vertical))
        throw new WallEException("Segundo argumento debe ser entero (vertical)",
            WallEException.ErrorType.Ejecucion);
    
    if (!(args[2] is int horizontal))
        throw new WallEException("Tercer argumento debe ser entero (horizontal)",
            WallEException.ErrorType.Ejecucion);

    // Convertir nombre de color a objeto Color
    Color color;
    try
    {
        color = ParseColor(colorName);
    }
    catch (WallEException)
    {
        throw new WallEException($"Color inválido: '{colorName}'",
            WallEException.ErrorType.Semantico);
    }

    // Calcular posición a verificar
    Vector2I checkPos = position + new Vector2I(horizontal, vertical);
    
    // Validar posición
    if (!Canvas.IsPositionValid(checkPos.X, checkPos.Y))
        throw new WallEException($"Posición inválida: ({checkPos.X}, {checkPos.Y})",
            WallEException.ErrorType.Ejecucion);

    // Verificar color en la posición especificada
    return Canvas.CheckColor(checkPos, color) ? 1 : 0;
}
    

  private void PreprocessLabels(ProgramNode program)
    {
        labels.Clear();
        for (int i = 0; i < program.Statements.Count; i++)
        {
            if (program.Statements[i] is LabelNode label)
            {
                string key = label.Name.ToLower();
                if (labels.ContainsKey(key))
                {
                    throw new WallEException($"Etiqueta duplicada: {label.Name}", 
                        WallEException.ErrorType.Semantico, label.LineNumber);
                }
                labels[key] = i;
                Log($"Registrada etiqueta: {key} en posición {i}");
            }
        }
    }
    
    private void ValidatePosition(int x, int y)
    {
        if (!Canvas.IsPositionValid(x, y))
            throw new WallEException($"Posición inválida: ({x}, {y})",
                WallEException.ErrorType.Ejecucion);
    }

   private Color ParseColor(string colorName)
{
    if (!colorMap.TryGetValue(colorName, out Color color))
    {
        // Intentar convertir a color por código hexadecimal
        try
        {
            return Color.FromString(colorName, Colors.Transparent);
        }
        catch
        {
            throw new WallEException($"Color inválido: '{colorName}'",
                WallEException.ErrorType.Semantico);
        }
    }
    return color;
}
    private int AdjustBrushSize(int size)
    {
        if (size <= 0)
            throw new WallEException("Tamaño de pincel debe ser positivo",
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

      private int SafeDivide(int a, int b)
    {
        if (b == 0) throw new WallEException("División por cero", WallEException.ErrorType.Ejecucion);
        return a / b;
    }

    private int GetLineNumber(ProgramNode program, int pc)
    {
        if (pc < 0 || pc >= program.Statements.Count) return -1;
        return program.Statements[pc].LineNumber;
    }
}