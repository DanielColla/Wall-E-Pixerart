using System.Collections.Generic;

public abstract class ASTNode 
{
    public int LineNumber { get; set; }
}

public class ProgramNode : ASTNode
{
    public List<ASTNode> Statements { get; } = new List<ASTNode>();
}

public class CommandNode : ASTNode
{
    public string Command { get; }
    public List<ExpressionNode> Arguments { get; }

    public CommandNode(string command, List<ExpressionNode> arguments)
    {
        Command = command;
        Arguments = arguments;
    }
}

public class AssignmentNode : ASTNode
{
    public string Variable { get; }
    public ExpressionNode Expression { get; }

    public AssignmentNode(string variable, ExpressionNode expression)
    {
        Variable = variable;
        Expression = expression;
    }
}

public class LabelNode : ASTNode
{
    public string Name { get; }

    public LabelNode(string name)
    {
        Name = name;
    }
}

public class GoToNode : ASTNode
{
    public string Label { get; }
    public ExpressionNode Condition { get; }

    public GoToNode(string label, ExpressionNode condition)
    {
        Label = label;
        Condition = condition;
    }
}

public abstract class ExpressionNode : ASTNode { }

public class BinaryNode : ExpressionNode
{
    public ExpressionNode Left { get; }
    public TokenType Operator { get; }
    public ExpressionNode Right { get; }

    public BinaryNode(ExpressionNode left, TokenType op, ExpressionNode right)
    {
        Left = left;
        Operator = op;
        Right = right;
    }
}

public class LiteralNode : ExpressionNode
{
    public object Value { get; }

    public LiteralNode(object value)
    {
        Value = value;
    }
}

public class VariableNode : ExpressionNode
{
    public string Name { get; }

    public VariableNode(string name)
    {
        Name = name;
    }
}

public class FunctionCallNode : ExpressionNode
{
    public string FunctionName { get; }
    public List<ExpressionNode> Arguments { get; }

    public FunctionCallNode(string name, List<ExpressionNode> args)
    {
        FunctionName = name;
        Arguments = args;
    }
}