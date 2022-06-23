using System;
using System.Collections.Generic;

namespace SPO4
{
    #region Main

    public class NodeBase : LocationEntity
    {
        public virtual string Resolve(string prefix = "")
        {
            return string.Empty;
        }
    }

    public class StmtNode : NodeBase
    {
        public StmtNode(List<NodeBase> nodes)
        {
            Nodes = nodes;
        }

        public List<NodeBase> Nodes { get; set; }

        public override string Resolve(string prefix = "")
        {
            string result = string.Empty;
            foreach (var node in Nodes)
                result += prefix + node.Resolve() + '\n';

            return result;
        }
    }

    #endregion

    #region Literals

    public abstract class LiteralNodeBase<T> : NodeBase
    {
        public T Value { get; set; }

        public Type LiteralType => typeof(T);

        public override string Resolve(string prefix = "")
        {
            return Value.ToString();
        }
    }

    public class IntNode : LiteralNodeBase<int>
    {
        public IntNode(int value = 0)
        {
            Value = value;
        }
    }

    public class FloatNode : LiteralNodeBase<float>
    {
        public FloatNode(float value = 0)
        {
            Value = value;
        }
    }

    public class DoubleNode : LiteralNodeBase<double>
    {
        public DoubleNode(double value = 0)
        {
            Value = value;
        }
    }

    public class BooleanNode : LiteralNodeBase<bool>
    {
        public BooleanNode(bool value = false)
        {
            Value = value;
        }
    }

    #endregion

    #region Indentefier

    public enum VariableKind
    {
        Integer,
        Float,
        Double,
        Boolean
    }

    public abstract class IdentifierNodeBase : NodeBase
    {
        public string Identifier { get; set; }
    }

    public class GetIdentifierNode : IdentifierNodeBase
    {
        public GetIdentifierNode(string identifier = null)
        {
            Identifier = identifier;
        }

        public override string Resolve(string prefix = "")
        {
            return $" {Identifier} ";
        }
    }

    public class SetIdentifierNode : IdentifierNodeBase
    {
        public SetIdentifierNode(string identifier = null)
        {
            Identifier = identifier;
        }

        public NodeBase Value { get; set; }

        public override string Resolve(string prefix = "")
        {
            return $"{prefix}{Identifier} = {Value.Resolve()}";
        }
    }

    public class DeclareIdentifierNode : IdentifierNodeBase
    {
        public DeclareIdentifierNode(VariableKind kind, string identifier = null)
        {
            Kind = kind;
            Identifier = identifier;
        }

        public VariableKind Kind { get; set; }
        public NodeBase Value { get; set; }

        public override string Resolve(string prefix = "")
        {
            return $"{prefix}{Identifier} = {(Value != null ? Value.Resolve() : "0")}";
        }
    }

    #endregion

    #region Operators

    public enum ComparisonOperatorKind
    {
        Equals,
        NotEquals,
        Less,
        LessEquals,
        Greater,
        GreaterEquals
    }

    public class OperatorNode : NodeBase
    {
        public string OperatorRepresentation { get; protected set; }
        public NodeBase LeftOperand { get; set; }

        public NodeBase RightOperand { get; set; }

        public OperatorNode(NodeBase left, NodeBase right)
        {
            LeftOperand = left;
            RightOperand = right;
        }

        public override string Resolve(string prefix = "")
        {
            return $"{LeftOperand.Resolve()} {OperatorRepresentation} {RightOperand.Resolve()}";
        }
    }

    public class AddNode : OperatorNode
    {
        public AddNode(NodeBase left, NodeBase right) : base(left, right)
        {
            OperatorRepresentation = "+";
        }
    }

    public class SubtractNode : OperatorNode
    {
        public SubtractNode(NodeBase left, NodeBase right) : base(left, right)
        {
            OperatorRepresentation = "-";
        }
    }

    public class DivideNode : OperatorNode
    {
        public DivideNode(NodeBase left, NodeBase right) : base(left, right)
        {
            OperatorRepresentation = "/";
        }
    }

    public class MultiplyNode : OperatorNode
    {
        public MultiplyNode(NodeBase left, NodeBase right) : base(left, right)
        {
            OperatorRepresentation = "*";
        }
    }

    public class ComparisonOperatorNode : OperatorNode
    {
        public ComparisonOperatorNode(ComparisonOperatorKind kind, NodeBase left, NodeBase right) : base(left, right)
        {
            Kind = kind;
        }

        public ComparisonOperatorKind Kind { get; set; }

        public static Dictionary<ComparisonOperatorKind, string> ComparisonOperatorRepresentations = new Dictionary<ComparisonOperatorKind, string> 
        {
            { ComparisonOperatorKind.Equals, "==" },
            { ComparisonOperatorKind.NotEquals, "!=" },
            { ComparisonOperatorKind.Greater, ">" },
            { ComparisonOperatorKind.Less, "<" },
            { ComparisonOperatorKind.GreaterEquals, ">=" },
            { ComparisonOperatorKind.LessEquals, "<=" }
        };

        public override string Resolve(string prefix = "")
        {
            return $"{LeftOperand.Resolve()} {ComparisonOperatorRepresentations[Kind]} {RightOperand.Resolve()}";
        }
    }

    #endregion

    #region Block if

    public class IfNode : NodeBase
    {
        public IfNode(NodeBase condition, NodeBase trueNode, NodeBase falseNode)
        {
            Condition = condition;
            True = trueNode;
            False = falseNode;
        }

        public NodeBase Condition { get; set; }
        public NodeBase True { get; set; }
        public NodeBase False { get; set; }

        public override string Resolve(string prefix = "")
        {
            var ifLine = $"{prefix}if {Condition.Resolve()}\n";

            string trueBlock;
            if (True is StmtNode)
                trueBlock = True.Resolve($"{prefix}\t");
            else
                trueBlock = True.Resolve($"{prefix}\t") + '\n';

            var falseBlock = string.Empty;
            if(False != null)
            {
                falseBlock = "else\n";
                if (False is StmtNode)
                    falseBlock += False.Resolve($"{prefix}\t");
                else
                    falseBlock += False.Resolve($"{prefix}\t") + '\n';
            }

            return ifLine + trueBlock + falseBlock + "end";
        }
    }

    public class TernaryIfNode : IfNode
    {
        public TernaryIfNode(NodeBase condition, NodeBase trueNode, NodeBase falseNode) : base(condition, trueNode, falseNode)
        {
        }

        public override string Resolve(string prefix = "")
        {
            return $"{Condition.Resolve()} ? {True.Resolve()} : {False.Resolve()}";
        }
    }

    #endregion
}
