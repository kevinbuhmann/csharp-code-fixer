using System;
using System.IO;
using Vstack.Common.Extensions;

namespace CSharpCodeFixer
{
    public static class Program
    {
        public const string ApplicationName = "C# Code Fixer";

        public static void Main(string[] args)
        {
            args.ValidateNotNull();

            string oldTitle = Console.Title;
            Console.Title = ApplicationName;

            DateTime start = DateTime.Now;

            foreach (string solutionPath in args)
            {
                if (solutionPath.EndsWith(".sln") && File.Exists(solutionPath))
                {
                    Console.WriteLine($"Processing solution {solutionPath}... ");

                    int count = CodeFixer.FixAllFixableViolations(solutionPath);

                    Console.WriteLine();
                    Console.WriteLine($"Fixed {count} violations in solution {solutionPath}... ");
                }
                else
                {
                    Console.WriteLine($"Error: Either {solutionPath} does not exit or it is not an solution file.");
                    Console.WriteLine();
                }
            }

            DateTime end = DateTime.Now;
            TimeSpan runTime = end - start;

            Console.WriteLine();
            Console.WriteLine($"Finished in {runTime.TotalSeconds} seconds");
            Console.Write("Press any key to continue...");
            Console.ReadKey();

            Console.Title = oldTitle;
        }
    }
}
