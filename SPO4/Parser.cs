using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace SPO4
{
	public class Parser
	{
		private Lexem[] _lexems;
		private int LexemId;

		public StmtNode Root { get; private set; }

		public Parser(IEnumerable<Lexem> lexems)
		{
			_lexems = lexems.ToArray();
		}

		public void Parse()
		{
			Root = new StmtNode(ParseMain().ToList());
		}

		#region Main

		private IEnumerable<NodeBase> ParseMain()
		{
			yield return ParseStmt();

			while (LexemId < _lexems.Length)
			{
				yield return ParseStmt();
			}
		}

		private NodeBase ParseStmt()
		{
			/* Парсим состояния:
			 * + Объявление/инициализация переменной (int a; | int a = 10; | int c = a < 5 ? 0 : 7; ...)
			 * + Использование переменной (a = 15; | a = b; | c = a < 5 ? 0 : 7; ...)
			 * + Начало блока if ( if(a < 5) {...} )
			 */
			return Attempt(DeclaringVar)
					?? Attempt(SetIdentifier)
					?? Attempt(BlockIf);
		}

		#endregion

		#region Block if

		private NodeBase BlockIf()
		{
			if (!Check(LexemKind.If))
				return null;

			Ensure(LexemKind.BracesOpened, "После ключевого слова \"if\" ожидалась открывающаяся скобка.");
			var expr = Ensure(ParseLineOpExpr, "Ожидается выражение для блока if.");
			Ensure(LexemKind.BracesClosed, "После выражения ожидалась закрывающая скобка.");

			NodeBase trueStmt = null;
			NodeBase falseStmt = null;

			// Если есть фигурные скобки
			if (Check(LexemKind.CurlyBracesOpened))
			{
				trueStmt = new StmtNode(ParseBlockIfStmts().ToList());
				Ensure(LexemKind.CurlyBracesClosed, "Ожидалась закрывающая фигурная скобка \"}\" после блока if");
			}
			// Иначе может быть только одно действие, попадающее в блок if
			else
			{
				trueStmt = ParseStmt();
			}

			if (Check(LexemKind.Else))
			{
				if (Check(LexemKind.CurlyBracesOpened))
				{
					falseStmt = new StmtNode(ParseBlockIfStmts().ToList());
					Ensure(LexemKind.CurlyBracesClosed, "Ожидалась закрывающая фигурная скобка \"}\" после блока else");
				}
				// Иначе может быть только одно действие, попадающее в блок else
				else
				{
					falseStmt = ParseStmt();
				}
			}

			return new IfNode(expr, trueStmt, falseStmt);
		}

		private IEnumerable<NodeBase> ParseBlockIfStmts()
		{
			yield return ParseStmt();

			while (!Peek(LexemKind.CurlyBracesClosed) && LexemId < _lexems.Length - 1)
			{
				yield return ParseStmt();
			}
		}

		#endregion

		#region Declaring variable

		private NodeBase DeclaringVar()
		{
			return Attempt(DeclaringInt)
					?? Attempt(DeclaringFloat)
					?? Attempt(DeclaringDouble)
					?? Attempt(DeclaringBoolean);
		}

		private DeclareIdentifierNode DeclaringInt()
		{
			if (!Check(LexemKind.IntType))
				return null;

			var node = new DeclareIdentifierNode(VariableKind.Integer, Ensure(LexemKind.Identifier, "Имя переменной должно быть идентификатором.").Value);
			node.Value = AssigningIdentefierWithExpression();

			Ensure(LexemKind.Semicolon, "Пропустил \";\" в конце действия.");
			return node;
		}

		private DeclareIdentifierNode DeclaringFloat()
		{
			if (!Check(LexemKind.FloatType))
				return null;

			var node = new DeclareIdentifierNode(VariableKind.Float, Ensure(LexemKind.Identifier, "Имя переменной должно быть идентификатором.").Value);
			node.Value = AssigningIdentefierWithExpression();

			Ensure(LexemKind.Semicolon, "Пропустил \";\" в конце действия.");
			return node;
		}

		private DeclareIdentifierNode DeclaringDouble()
		{
			if (!Check(LexemKind.DoubleType))
				return null;

			var node = new DeclareIdentifierNode(VariableKind.Double, Ensure(LexemKind.Identifier, "Имя переменной должно быть идентификатором.").Value);
			node.Value = AssigningIdentefierWithExpression();

			Ensure(LexemKind.Semicolon, "Пропустил \";\" в конце действия.");
			return node;
		}

		private DeclareIdentifierNode DeclaringBoolean()
		{
			if (!Check(LexemKind.BooleanType))
				return null;

			var node = new DeclareIdentifierNode(VariableKind.Boolean, Ensure(LexemKind.Identifier, "Имя переменной должно быть идентификатором.").Value);
			node.Value = AssigningIdentefierWithExpression();

			Ensure(LexemKind.Semicolon, "Пропустил \";\" в конце действия.");
			return node;
		}

		#endregion

		#region Set identifier

		private SetIdentifierNode SetIdentifier()
		{
			if (!Peek(LexemKind.Identifier))
				return null;

			var node = new SetIdentifierNode(Ensure(LexemKind.Identifier, "Имя переменной должно быть идентификатором.").Value);
			node.Value = AssigningIdentefierWithExpression();

			Ensure(LexemKind.Semicolon, "Пропустил \";\" в конце действия.");
			return node;
		}

		#endregion

		#region Expression

		private NodeBase AssigningIdentefierWithExpression()
		{
			NodeBase expr = null;
			if (Check(LexemKind.Assign))
			{
				expr = Ensure(ParseLineOpExpr, "Ожидается присваеваемое выражение.");
			}

			if (Check(LexemKind.TernaryIf))
			{
				expr = ParseTernaryOpExpr(expr);
			}

			return expr;
		}

		private NodeBase ParseTernaryOpExpr(NodeBase conditionExpr)
		{
			var trueExpr = Ensure(ParseLineOpExpr, "Ожидается присваеваемое выражение после тернарного оператора \"?\"");
			Ensure(LexemKind.TernaryElse, "В тернарном операторе ожидается \":\"");
			var falseExpr = Ensure(ParseLineOpExpr, "Ожидается присваеваемое выражение после тернарного оператора \":\"");

			return new TernaryIfNode(conditionExpr, trueExpr, falseExpr);
		}

		private NodeBase ParseLineOpExpr()
		{
			return ProcessOperator(ParseLineBaseExpr);
		}

		private NodeBase ParseLineBaseExpr()
		{
			return Attempt(ParseGetExpr);
		}

		private NodeBase ParseGetExpr()
		{
			var node = Attempt(ParseAtom);
			if (node == null)
				return null;

			return node;
		}

		private NodeBase ParseAtom()
		{
			return Attempt(ParseLiteral)
				   ?? Attempt(ParseGetIdExpr);
		}

		private NodeBase ProcessOperator(Func<NodeBase> getter, int priority = 0)
		{
			if (priority == Priorities.Count)
				return getter();

			var node = ProcessOperator(getter, priority + 1);
			if (node == null)
				return null;

			var ops = Priorities[priority];
			while (PeekAny(ops.Keys.ToArray()))
			{
				foreach (var curr in ops)
					if (Check(curr.Key))
						node = curr.Value(node, Ensure(() => ProcessOperator(getter, priority + 1), "Что-то не так с выражением."));
			}

			return node;
		}


		private GetIdentifierNode ParseGetIdExpr()
		{
			var node = Attempt(ParseLvalueIdExpr);
			return node;
		}

		private GetIdentifierNode ParseLvalueIdExpr()
		{
			if (!Peek(LexemKind.Identifier))
				return null;

			return new GetIdentifierNode(GetValue());
		}

		#endregion

		#region Literals

		private NodeBase ParseLiteral()
		{
			return Attempt(ParseBool)
				   ?? Attempt(ParseInt)
				   ?? Attempt(ParseFloat)
				   ?? Attempt(ParseDouble) as NodeBase;
		}

		private BooleanNode ParseBool()
		{
			if (Check(LexemKind.True))
				return new BooleanNode(true);

			if (Check(LexemKind.False))
				return new BooleanNode();

			return null;
		}

		private IntNode ParseInt()
		{
			if (!Peek(LexemKind.Integer))
				return null;

			var value = GetValue();
			try
			{
				return new IntNode(int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture));
			}
			catch
			{
				ErrorHandler.Error("Что-то не так с int", value);
				return null;
			}
		}

		private FloatNode ParseFloat()
		{
			if (!Peek(LexemKind.Float))
				return null;

			var value = GetValue();
			try
			{
				return new FloatNode(float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture));
			}
			catch
			{
				ErrorHandler.Error("Что-то не так с float", value);
				return null;
			}
		}

		private DoubleNode ParseDouble()
		{
			if (!Peek(LexemKind.Double))
				return null;

			var value = GetValue();
			try
			{
				return new DoubleNode(double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture));
			}
			catch
			{
				ErrorHandler.Error("Что-то не так с double", value);
				return null;
			}
		}

		#endregion

		#region Utils

		[DebuggerStepThrough]
		private bool Peek(params LexemKind[] types)
		{
			var id = Math.Min(LexemId, _lexems.Length - 1);
			var lex = _lexems[id];
			return lex.Kind.IsAnyOf(types);
		}

		[DebuggerStepThrough]
		private bool PeekAny(params LexemKind[] types)
		{
			var id = Math.Min(LexemId, _lexems.Length - 1);
			var lex = _lexems[id];
			return lex.Kind.IsAnyOf(types);
		}

		[DebuggerStepThrough]
		private Lexem Ensure(LexemKind type, string msg, params object[] args)
		{
			var lex = _lexems[LexemId];

			if (lex.Kind != type)
				ErrorHandler.Error(msg, args);

			Skip();
			return lex;
		}

		[DebuggerStepThrough]
		private bool Check(LexemKind lexem)
		{
			var lex = _lexems[LexemId];

			if (lex.Kind != lexem)
				return false;

			Skip();
			return true;
		}

		[DebuggerStepThrough]
		private void Skip(int count = 1)
		{
			LexemId = Math.Min(LexemId + count, _lexems.Length);
		}

		[DebuggerStepThrough]
		private string GetValue()
		{
			var value = _lexems[LexemId].Value;
			Skip();
			return value;
		}

		// Методы для узлов.

		[DebuggerStepThrough]
		private T Attempt<T>(Func<T> getter) where T : LocationEntity
		{
			var backup = LexemId;
			var result = getter();
			if (result == null)
				LexemId = backup;

			return result;
		}

		[DebuggerStepThrough]
		private T Ensure<T>(Func<T> getter, string msg) where T : LocationEntity
		{
			var result = getter();
			if (result == null)
				throw new Exception(msg);

			return result;
		}

		private static List<Dictionary<LexemKind, Func<NodeBase, NodeBase, NodeBase>>> Priorities =
		new List<Dictionary<LexemKind, Func<NodeBase, NodeBase, NodeBase>>>
		{
			new Dictionary<LexemKind, Func<NodeBase, NodeBase, NodeBase>>
			{
				{LexemKind.Equally, (a, b) => new ComparisonOperatorNode(ComparisonOperatorKind.Equals, a, b)},
				{LexemKind.NotEqually, (a, b) => new ComparisonOperatorNode(ComparisonOperatorKind.NotEquals, a, b)},
				{LexemKind.Less, (a, b) => new ComparisonOperatorNode(ComparisonOperatorKind.Less, a, b)},
				{LexemKind.LessOrEqually, (a, b) => new ComparisonOperatorNode(ComparisonOperatorKind.LessEquals, a, b)},
				{LexemKind.More, (a, b) => new ComparisonOperatorNode(ComparisonOperatorKind.Greater, a, b)},
				{LexemKind.MoreOrEqually, (a, b) => new ComparisonOperatorNode(ComparisonOperatorKind.GreaterEquals, a, b)},
			},

			new Dictionary<LexemKind, Func<NodeBase, NodeBase, NodeBase>>
			{
				{ LexemKind.Plus, (a, b) => new AddNode(a, b) },
				{ LexemKind.Minus, (a, b) => new SubtractNode(a, b) }
			},

			new Dictionary<LexemKind, Func<NodeBase, NodeBase, NodeBase>>
			{
				{ LexemKind.Divide, (a, b) => new DivideNode(a, b) },
				{ LexemKind.Multiply, (a, b) => new MultiplyNode(a, b) }
			}
		};

		#endregion

		#region Print nodes tree

		public void PrintNodesTree()
		{
			PrintNodeType(Root);
			Console.WriteLine();
			PrintTree(Root);
		}

		private void PrintTree(NodeBase startNode, string prefix = "", int depth = 0)
		{
			if (startNode is StmtNode)
			{
				PrintStmtNode(startNode as StmtNode, prefix, depth);
			}
			else if (startNode is IfNode)
			{
				PrintBlockIf(startNode as IfNode, prefix, depth);
			}
			else if (startNode is DeclareIdentifierNode)
			{
				PrintDeclareIdentifierNode(startNode as DeclareIdentifierNode, prefix, depth);
			}
			else if (startNode is SetIdentifierNode)
			{
				PrintSetIdentifierNode(startNode as SetIdentifierNode, prefix, depth);

			}
			else if (startNode is GetIdentifierNode)
			{
				PrintGetIdentifierNode(startNode as GetIdentifierNode, prefix, depth);
			}
			else if (startNode is ComparisonOperatorNode)
			{
				PrintComparisonOperatorNode(startNode as ComparisonOperatorNode, prefix, depth);
			}
			else if (startNode is OperatorNode)
			{
				PrintOperatorNode(startNode as OperatorNode, prefix, depth);
			}
			else if (startNode is IntNode)
			{
				PrintLiteral(startNode as IntNode, prefix, depth);
			}
			else if (startNode is FloatNode)
			{
				PrintLiteral(startNode as FloatNode, prefix, depth);
			}
			else if (startNode is DoubleNode)
			{
				PrintLiteral(startNode as DoubleNode, prefix, depth);
			}
			else if (startNode is BooleanNode)
			{
				PrintLiteral(startNode as BooleanNode, prefix, depth);
			}
		}

		private void PrintStmtNode(StmtNode stmtNode, string prefix = "", int depth = 0)
		{
			foreach (var node in stmtNode.Nodes.Take(stmtNode.Nodes.Count - 1))
			{
				Console.Write(prefix + "├── ");
				PrintNodeType(node);
				Console.WriteLine();
				PrintTree(node, prefix + "│   ", depth + 1);
			}

			var lastNode = stmtNode.Nodes.LastOrDefault();
			if (lastNode != null)
			{
				Console.Write(prefix + "└── ");
				PrintNodeType(lastNode);
				Console.WriteLine();
				PrintTree(lastNode, prefix + "    ", depth + 1);
			}
		}

		private void PrintDeclareIdentifierNode(DeclareIdentifierNode declareIdentifierNode, string prefix = "", int depth = 0)
		{
			Console.Write(prefix + "├── ");
			Console.Write("Identifier: " + declareIdentifierNode.Identifier);
			Console.WriteLine();

			Console.Write(prefix + (declareIdentifierNode.Value != null ? "├── " : "└── "));
			Console.Write("Kind: " + declareIdentifierNode.Kind);
			Console.WriteLine();

			if (declareIdentifierNode.Value != null)
			{
				Console.Write(prefix + "└── ");
				Console.Write("Value: ");
				PrintNodeType(declareIdentifierNode.Value);
				Console.WriteLine();
				PrintTree(declareIdentifierNode.Value, prefix + "    ", depth + 1);
			}
		}

		private void PrintBlockIf(IfNode ifNode, string prefix = "", int depth = 0)
		{
			Console.Write(prefix + "├── ");
			Console.Write("Condition: ");
			PrintNodeType(ifNode.Condition);
			Console.WriteLine();
			PrintTree(ifNode.Condition, prefix + "│   ", depth + 1);

			Console.Write(prefix + "├── ");
			Console.Write("True: ");
			PrintNodeType(ifNode.True);
			Console.WriteLine();
			PrintTree(ifNode.True, prefix + "│   ", depth + 1);

			Console.Write(prefix + "└── ");
			Console.Write("False: ");
			PrintNodeType(ifNode.False);
			Console.WriteLine();
			PrintTree(ifNode.False, prefix + "    ", depth + 1);
		}

		private void PrintSetIdentifierNode(SetIdentifierNode setIdentifierNode, string prefix = "", int depth = 0)
		{
			Console.Write(prefix + "├── ");
			Console.Write("Identifier: " + setIdentifierNode.Identifier);
			Console.WriteLine();

			Console.Write(prefix + "└── ");
			Console.Write("Value: ");
			PrintNodeType(setIdentifierNode.Value);
			Console.WriteLine();
			PrintTree(setIdentifierNode.Value, prefix + "    ", depth + 1);
		}

		private void PrintGetIdentifierNode(GetIdentifierNode getIdentifierNode, string prefix = "", int depth = 0)
		{
			Console.Write(prefix + "└── ");
			Console.Write("Identifier: " + getIdentifierNode.Identifier);
			Console.WriteLine();
		}

		private void PrintComparisonOperatorNode(ComparisonOperatorNode comparisonOperatorNode, string prefix = "", int depth = 0)
		{
			Console.Write(prefix + "├── ");
			Console.Write("Kind: " + comparisonOperatorNode.Kind);
			Console.WriteLine();

			Console.Write(prefix + "├── ");
			Console.Write("Left: ");
			PrintNodeType(comparisonOperatorNode.LeftOperand);
			Console.WriteLine();
			PrintTree(comparisonOperatorNode.LeftOperand, prefix + "│   ", depth + 1);

			Console.Write(prefix + "└── ");
			Console.Write("Right: ");
			PrintNodeType(comparisonOperatorNode.RightOperand);
			Console.WriteLine();
			PrintTree(comparisonOperatorNode.RightOperand, prefix + "   ", depth + 1);
		}

		private void PrintOperatorNode(OperatorNode operatorNode, string prefix = "", int depth = 0)
		{
			Console.Write(prefix + "├── ");
			Console.Write("OperatorRepresentation: " + operatorNode.OperatorRepresentation);
			Console.WriteLine();

			Console.Write(prefix + "├── ");
			Console.Write("Left: ");
			PrintNodeType(operatorNode.LeftOperand);
			Console.WriteLine();
			PrintTree(operatorNode.LeftOperand, prefix + "│   ", depth + 1);

			Console.Write(prefix + "└── ");
			Console.Write("Right: ");
			PrintNodeType(operatorNode.RightOperand);
			Console.WriteLine();
			PrintTree(operatorNode.RightOperand, prefix + "    ", depth + 1);
		}

		private void PrintLiteral<T>(LiteralNodeBase<T> literalNode, string prefix = "", int depth = 0)
		{
			Console.Write(prefix + "├── ");
			Console.Write("Type: " + literalNode.LiteralType);
			Console.WriteLine();

			Console.Write(prefix + "└── ");
			Console.Write("Value: " + literalNode.Value);
			Console.WriteLine();
		}

		private void PrintNodeType(NodeBase node)
		{
			Console.Write(node.ToString().Replace("SPO4.", ""));
		}

		#endregion
	}
}
