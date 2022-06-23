using System;
using System.IO;

namespace SPO4
{
    public class Compiler
    {
        private readonly StmtNode _tree;
        private readonly string _outputPath;

        public Compiler(StmtNode tree, string outputPath)
        {
            _tree = tree;
            _outputPath = outputPath;
        }

        public void Compile()
        {
            var result = _tree.Resolve();
            File.WriteAllText(_outputPath, result);
        }

        public void PrintLogInfo()
        {
            var fileName = Path.GetFileName(_outputPath);
            var fullPath = Path.GetFullPath(_outputPath);
            Console.WriteLine($"Текст программы записан в файл \"{fileName}\", расположенном по пути \"{fullPath}\"");
        }
    }
}
