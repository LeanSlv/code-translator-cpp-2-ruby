using System;
using System.IO;

namespace SPO4
{
    class Program
    {
        private static bool isPrintLexems = false;
        private static bool isPrintNodesTree = false;
        private static bool isPrintAnalyzedIdentefiers = true;
        private static bool isPrintCompilerResult = true;

        private static string sourcePath = "../.././../input.cpp";
        private static string outputPath = "../.././../output.rb";

        static void Main(string[] args)
        {
            #region Lexer

            /// ==============================
            /// Лексический анализатор
            /// ==============================
            var source = File.ReadAllText(sourcePath);
            var lexer = new Lexer(source);
            try
            {
                lexer.Parse();

                if (isPrintLexems)
                {
                    Console.WriteLine("Список лексем\n==============================");
                    foreach (var lexem in lexer.Lexems)
                    {
                        Console.Write(lexem.Kind);
                        if (!string.IsNullOrEmpty(lexem.Value))
                            Console.Write(" = " + lexem.Value);
                        Console.WriteLine();
                    }
                    Console.WriteLine("==============================\n\n");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            #endregion

            #region Parser

            /// ==============================
            /// Синтаксический анализатор
            /// ==============================
            var parser = new Parser(lexer.Lexems);
            try
            {
                parser.Parse();

                if (isPrintNodesTree)
                {
                    Console.WriteLine("Синтаксическое дерево\n==============================");
                    parser.PrintNodesTree();
                    Console.WriteLine("==============================\n\n");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            #endregion

            #region Semantic analyzer

            /// ==============================
            /// Семантический анализатор
            /// ==============================
            var sematicAnalyzer = new SemanticAnalyzer(parser.Root);
            try
            {
                sematicAnalyzer.Analyze();

                if (isPrintAnalyzedIdentefiers)
                {
                    Console.WriteLine("Таблица переменных\n==============================");
                    sematicAnalyzer.PrintIdentifiersTable();
                    Console.WriteLine("==============================\n\n");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            #endregion

            #region Compiler

            /// ==============================
            /// Генератор кода
            /// ==============================
            var compiler = new Compiler(parser.Root, outputPath);
            try
            {
                compiler.Compile();

                if (isPrintCompilerResult)
                {
                    Console.WriteLine("Транслятор в ЯП Ruby\n==============================");
                    compiler.PrintLogInfo();
                    Console.WriteLine("==============================\n\n");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            #endregion
        }
    }
}
