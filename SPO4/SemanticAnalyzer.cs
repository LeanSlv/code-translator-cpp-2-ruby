using System;
using System.Collections.Generic;
using System.Linq;

namespace SPO4
{
	public class SemanticAnalyzer
	{
		private readonly StmtNode _tree;

		public Dictionary<string, VariableKind> DeclaredIdentifiers { get; private set; }

		public SemanticAnalyzer(StmtNode tree)
		{
			_tree = tree;
			DeclaredIdentifiers = new Dictionary<string, VariableKind>();
		}

		public void Analyze()
		{
			AnalyzeNode(_tree);
		}

		#region Statement

		private void AnalyzeNode(StmtNode stmtNode)
		{
			foreach (var node in stmtNode.Nodes)
				AnalyzeNode(node);
		}

		private void AnalyzeNode(NodeBase node)
		{
			if (node is StmtNode)
			{
				AnalyzeNode(node as StmtNode);
			}
			else if (node is IfNode)
			{
				AnalyzeNode(node as IfNode);
			}
			else if (node is DeclareIdentifierNode)
			{
				AnalyzeNode(node as DeclareIdentifierNode);
			}
			else if (node is SetIdentifierNode)
			{
				AnalyzeNode(node as SetIdentifierNode);
			}
			else if (node is GetIdentifierNode)
			{
				AnalyzeNode(node as GetIdentifierNode);
			}
		}

		#endregion

		#region Identifier

		private void AnalyzeNode(DeclareIdentifierNode node)
		{
			if (DeclaredIdentifiers.Any(i => i.Key == node.Identifier))
				ErrorHandler.Error("Идентификатор {0} уже объявлен.", node.Identifier);

			DeclaredIdentifiers.Add(node.Identifier, node.Kind);

			if (node.Value != null)
				AnalyzeSetValue(node.Value, node.Kind);
		}

		private void AnalyzeNode(SetIdentifierNode node)
		{
			if (!DeclaredIdentifiers.Any(i => i.Key == node.Identifier))
				ErrorHandler.Error("Идентификатор {0} не объявлен.", node.Identifier);

			var idKind = DeclaredIdentifiers.GetValueOrDefault(node.Identifier);
			AnalyzeSetValue(node.Value, idKind);
		}

		private void AnalyzeNode(GetIdentifierNode node)
		{
			if (!DeclaredIdentifiers.Any(i => i.Key == node.Identifier))
				ErrorHandler.Error("Идентификатор {0} не объявлен.", node.Identifier);
		}

		private void AnalyzeNode(GetIdentifierNode node, VariableKind kind)
		{
			if (!DeclaredIdentifiers.Any(i => i.Key == node.Identifier))
				ErrorHandler.Error("Идентификатор {0} не объявлен.", node.Identifier);

			var idKind = DeclaredIdentifiers.GetValueOrDefault(node.Identifier);

			if (kind == VariableKind.Integer && kind != idKind)
			{
				ErrorHandler.Error("Присвоение нецелочисленного значения идентификатору с типом \"{0}\".", kind);
			}
			else if ((kind == VariableKind.Double || kind == VariableKind.Float) && idKind == VariableKind.Boolean)
			{
				ErrorHandler.Error("Присвоение логического значения идентификатору с типом \"{0}\".", kind);
			}
			else if (kind == VariableKind.Boolean && kind != idKind)
			{
				ErrorHandler.Error("Присвоение численного значения идентификатору с типом \"{0}\".", kind);
			}
		}

		private void AnalyzeSetValue(NodeBase valueNode, VariableKind kind)
		{
			if (valueNode is OperatorNode)
			{
				AnalyzeNode(valueNode as OperatorNode, kind);
			}
			else if (valueNode is IfNode)
			{
				AnalyzeNode(valueNode as IfNode, kind);
			}
			else if (valueNode is GetIdentifierNode)
			{
				AnalyzeNode(valueNode as GetIdentifierNode, kind);
			}
			else if (kind == VariableKind.Integer && !(valueNode is IntNode))
			{
				ErrorHandler.Error("Присвоение нецелочисленного значения идентификатору с типом \"{0}\".", kind);
			}
			else if ((kind == VariableKind.Double || kind == VariableKind.Float) && valueNode is BooleanNode)
			{
				ErrorHandler.Error("Присвоение логического значения идентификатору с типом \"{0}\".", kind);
			}
			else if (kind == VariableKind.Boolean && !(valueNode is BooleanNode))
			{
				ErrorHandler.Error("Присвоение численного значения идентификатору с типом \"{0}\".", kind);
			}
		}

		#endregion

		#region Operator

		private void AnalyzeNode(OperatorNode node, VariableKind kind)
		{
			AnalyzeOperatorNodeChildren(node.LeftOperand, kind);
			AnalyzeOperatorNodeChildren(node.RightOperand, kind);
		}

		private void AnalyzeOperatorNodeChildren(NodeBase node, VariableKind kind)
		{
			if (node is OperatorNode)
			{
				AnalyzeNode(node as OperatorNode, kind);
			}
			else if (node is GetIdentifierNode)
			{
				AnalyzeNode(node as GetIdentifierNode);
			}
			else if (kind == VariableKind.Integer && !(node is IntNode))
			{
				ErrorHandler.Error("Присвоение нецелочисленного значения идентификатору с типом \"{0}\".", kind);
			}
			else if ((kind == VariableKind.Double || kind == VariableKind.Float) && node is BooleanNode)
			{
				ErrorHandler.Error("Присвоение логического значения идентификатору с типом \"{0}\".", kind);
			}
			else if (kind == VariableKind.Boolean && !(node is BooleanNode))
			{
				ErrorHandler.Error("Присвоение численного значения идентификатору с типом \"{0}\".", kind);
			}
		}

		#endregion

		#region IfBlock

		private void AnalyzeNode(IfNode node)
		{
			AnalyzeConditionNode(node.Condition as OperatorNode);
			AnalyzeIfNodeChildren(node.True);
			AnalyzeIfNodeChildren(node.False);
		}

		private void AnalyzeNode(IfNode node, VariableKind kind)
		{
			AnalyzeConditionNode(node.Condition as OperatorNode);
			AnalyzeOperatorNodeChildren(node.True, kind);
			AnalyzeOperatorNodeChildren(node.False, kind);
		}

		private void AnalyzeConditionNode(OperatorNode node)
		{
			if (node.LeftOperand is OperatorNode)
			{
				AnalyzeConditionNode(node.LeftOperand as OperatorNode);
			}
			else if (node.LeftOperand is GetIdentifierNode)
			{
				AnalyzeNode(node.LeftOperand as GetIdentifierNode);
			}

			if (node.RightOperand is OperatorNode)
			{
				AnalyzeConditionNode(node.RightOperand as OperatorNode);
			}
			else if (node.RightOperand is GetIdentifierNode)
			{
				AnalyzeNode(node.RightOperand as GetIdentifierNode);
			}
		}

		private void AnalyzeIfNodeChildren(NodeBase node)
		{
			if (node is StmtNode)
			{
				AnalyzeNode(node as StmtNode);
			}
			else
			{
				AnalyzeNode(node);
			}
		}

		#endregion

		#region Utils

		public void PrintIdentifiersTable()
		{
			Console.WriteLine(" № | Identifier\t│ Kind\t");
			Console.WriteLine("───┼────────────┼──────────");
			int i = 1;
			foreach (var id in DeclaredIdentifiers)
			{
				Console.WriteLine($" {i} | {id.Key}\t| {id.Value}");
				i++;
			}
		}

		#endregion
	}
}
