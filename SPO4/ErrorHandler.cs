using System;

namespace SPO4
{
    public static class ErrorHandler
	{
		public static void Error(string msg, params object[] args)
		{
			// TODO: Указывать позицию ошибки через _lexems[LexemId]
			throw new Exception(string.Format(msg, args));
		}
	}
}
