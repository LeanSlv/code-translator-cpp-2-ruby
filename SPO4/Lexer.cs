using System.Collections.Generic;

namespace SPO4
{
	/// <summary>
	/// Лексер.
	/// </summary>
	public class Lexer
	{
		private char[] SpaceChars = new[] { ' ', '\n', '\r', '\t' };
		private string Source;
		private int Offset;

		/// <summary>
		/// Список лексем.
		/// </summary>
		public IEnumerable<Lexem> Lexems { get; private set; }

		public Lexer(string src)
		{
			Source = src;
		}

		/// <summary>
		/// Поиск лексем в программе.
		/// </summary>
		public void Parse()
		{
			var lexems = new List<Lexem>();

			while (InBounds())
			{
				SkipSpaces();
				if (!InBounds())
					break;

				var lex = ProcessStatic() ?? ProcessDynamic();
				if (lex == null)
					ErrorHandler.Error("Unknown lexem at {0}", Offset);

				lexems.Add(lex);
			}

			Lexems = lexems;
		}

		#region Search lexem process

		private Lexem ProcessStatic()
		{
			foreach (var def in LexemDefinitions.Statics)
			{
				var rep = def.Representation;
				var len = rep.Length;

				if (Offset + len > Source.Length || Source.Substring(Offset, len) != rep)
					continue;

				if (Offset + len < Source.Length && def.IsKeyword)
				{
					var nextChar = Source[Offset + len];
					if (nextChar == '_' || char.IsLetterOrDigit(nextChar))
						continue;
				}

				Offset += len;
				return new Lexem { Kind = def.Kind, Offset = Offset, Length = len };
			}

			return null;
		}

		private Lexem ProcessDynamic()
		{
			foreach (var def in LexemDefinitions.Dynamics)
			{
				var match = def.Representation.Match(Source, Offset);
				if (!match.Success)
					continue;

				Offset += match.Length;
				return new Lexem { Kind = def.Kind, Offset = Offset, Length = match.Length, Value = match.Value };
			}

			return null;
		}

		#endregion

		#region Utils

		private void SkipSpaces()
		{
			while (InBounds() && Source[Offset].IsAnyOf(SpaceChars))
				Offset++;
		}

		private bool InBounds()
		{
			return Offset < Source.Length;
		}

		#endregion
	}
}
