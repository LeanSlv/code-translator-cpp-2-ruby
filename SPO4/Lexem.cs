using System.Text.RegularExpressions;

namespace SPO4
{
	/// <summary>
	/// Тип лексемы.
	/// </summary>
	public enum LexemKind
	{
		// Статические типы:
		IntType,
		FloatType,
		DoubleType,
		BooleanType,
		Plus,
		Minus,
		Multiply,
		Divide,
		Assign,
		Semicolon,
		CurlyBracesOpened,
		CurlyBracesClosed,
		BracesOpened,
		BracesClosed,
		If,
		Else,
		TernaryIf,
		TernaryElse,
		Less,
		More,
		Equally,
		NotEqually,
		LessOrEqually,
		MoreOrEqually,
		True,
		False,


		// Динамические типы:
		Identifier,
		Integer,
		Float,
		Double,
		Boolean
	}

	/// <summary>
	/// Местоположение.
	/// </summary>
	public class LocationEntity
	{
		public int Offset;
		public int Length;
	}

	/// <summary>
	/// Лексема.
	/// </summary>
	public class Lexem : LocationEntity
	{
		public LexemKind Kind;
		public string Value;
	}

	/// <summary>
	/// Определение лексемы.
	/// </summary>
	/// <typeparam name="T">Класс, которым определяется лексема.</typeparam>
	public class LexemDefinition<T>
	{
		public LexemKind Kind { get; protected set; }
		public T Representation { get; protected set; }
	}

	/// <summary>
	/// Статическая лексема (ключевые слова, операторы и т.д.).
	/// </summary>
	public class StaticLexemDefinition : LexemDefinition<string>
	{
		public bool IsKeyword;

		public StaticLexemDefinition(string rep, LexemKind kind, bool isKeyword = false)
		{
			Representation = rep;
			Kind = kind;
			IsKeyword = isKeyword;
		}
	}

	/// <summary>
	/// Динамическая лексема (идентификаторы, переменные).
	/// </summary>
	public class DynamicLexemDefinition : LexemDefinition<Regex>
	{
		public DynamicLexemDefinition(string rep, LexemKind kind)
		{
			Representation = new Regex(@"\G" + rep, RegexOptions.Compiled);
			Kind = kind;
		}
	}

	/// <summary>
	/// Определения различных типов лексем.
	/// </summary>
	public static class LexemDefinitions
	{
		public static StaticLexemDefinition[] Statics = new[]
		{
			new StaticLexemDefinition("int", LexemKind.IntType, true),
			new StaticLexemDefinition("float", LexemKind.FloatType, true),
			new StaticLexemDefinition("double", LexemKind.DoubleType, true),
			new StaticLexemDefinition("bool", LexemKind.BooleanType, true),
			new StaticLexemDefinition("=", LexemKind.Assign),
			new StaticLexemDefinition("+", LexemKind.Plus),
			new StaticLexemDefinition("-", LexemKind.Minus),
			new StaticLexemDefinition("*", LexemKind.Multiply),
			new StaticLexemDefinition("/", LexemKind.Divide),
			new StaticLexemDefinition(";", LexemKind.Semicolon),
			new StaticLexemDefinition("{", LexemKind.CurlyBracesOpened),
			new StaticLexemDefinition("}", LexemKind.CurlyBracesClosed),
			new StaticLexemDefinition("(", LexemKind.BracesOpened),
			new StaticLexemDefinition(")", LexemKind.BracesClosed),
			new StaticLexemDefinition("if", LexemKind.If),
			new StaticLexemDefinition("else", LexemKind.Else),
			new StaticLexemDefinition("?", LexemKind.TernaryIf),
			new StaticLexemDefinition(":", LexemKind.TernaryElse),
			new StaticLexemDefinition("<", LexemKind.Less),
			new StaticLexemDefinition(">", LexemKind.More),
			new StaticLexemDefinition("==", LexemKind.Equally),
			new StaticLexemDefinition("!=", LexemKind.NotEqually),
			new StaticLexemDefinition("<=", LexemKind.LessOrEqually),
			new StaticLexemDefinition(">=", LexemKind.MoreOrEqually),
			new StaticLexemDefinition("true", LexemKind.True),
			new StaticLexemDefinition("false", LexemKind.False),
		};

		public static DynamicLexemDefinition[] Dynamics = new[]
		{
			new DynamicLexemDefinition("[a-zA-Z_][a-zA-Z0-9_]*", LexemKind.Identifier),
			new DynamicLexemDefinition(@"(0|[1-9][0-9]*)\.[0-9]+", LexemKind.Double),
			new DynamicLexemDefinition(@"(0|[1-9][0-9]*)", LexemKind.Integer),
		};
	}
}
