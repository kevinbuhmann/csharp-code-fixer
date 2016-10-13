using System;
using System.IO;
using Vstack.Extensions;

namespace CSharpCodeFixer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            args.ValidateNotNullParameter(nameof(args));

            foreach (string path in args)
            {
                if (Directory.Exists(path))
                {
                    Console.Write($"Processing directory {path}... ");
                    string[] files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
                    Console.WriteLine($"{files.Length} files");

                    foreach (string file in files)
                    {
                        Console.Write($"Processing file {file}... ");
                        CodeFixer.FixAll(file);
                        Console.WriteLine($"done");
                    }

                    Console.WriteLine();
                }
                else if (File.Exists(path))
                {
                    Console.Write($"Processing file {path}... ");
                    CodeFixer.FixAll(path);
                    Console.WriteLine($"done");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine($"Error: {path} not found.");
                    Console.WriteLine();
                }
            }
        }
    }
}
