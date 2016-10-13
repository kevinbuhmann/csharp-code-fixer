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

            foreach (string solutionPath in args)
            {
                if (solutionPath.EndsWith(".sln") && File.Exists(solutionPath))
                {
                    Console.WriteLine($"Processing solution {solutionPath}... ");

                    CodeFixer.FixAllFixableViolations(solutionPath);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine($"Error: Either {solutionPath} does not exit or it is not an solution file.");
                    Console.WriteLine();
                }
            }
        }
    }
}
