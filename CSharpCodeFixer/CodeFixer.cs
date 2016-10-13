using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CSharpCodeFixer
{
    public static class CodeFixer
    {
        private static ImmutableArray<DiagnosticAnalyzer> analyzers;

        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFile", Justification = "No other option.")]
        public static ImmutableArray<DiagnosticAnalyzer> Analyzers
        {
            get
            {
                if (analyzers == null)
                {
                    Assembly stylecopAnalyzersAssembly = Assembly.LoadFile(GetPathToFile(@"StyleCop.Analyzers.dll"));
                    analyzers = stylecopAnalyzersAssembly.GetTypes()
                        .Where(t => t.IsAbstract == false && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
                        .Select(t => Activator.CreateInstance(t) as DiagnosticAnalyzer)
                        .ToImmutableArray();
                }

                return analyzers;
            }
        }

        public static void FixAllFixableViolations(string solutionPath)
        {
            FixViolations(solutionPath, "SA1005", FixSA1005SingleLineCommentMustBeginWithASpace);
            FixViolations(solutionPath, "SA1101", FixSA1101PrefixLocallCallsWithThis);
        }

        private static void FixViolations(string solutionPath, string violationId, Func<string, IEnumerable<Diagnostic>, string> fixViolations)
        {
            Console.WriteLine();
            Console.WriteLine($"Building to fix {violationId}...");

            IEnumerable<Diagnostic> dianostics = BuildSolution(solutionPath)
                .Where(d => d.Id == violationId)
                .ToList();

            IEnumerable<IGrouping<string, Diagnostic>> groups = dianostics
                .GroupBy(d => d.Location.GetLineSpan().Path)
                .ToList();

            Console.WriteLine();
            Console.WriteLine($"Found {dianostics.Count()} {violationId} violations...");

            foreach (IGrouping<string, Diagnostic> fileDiagnostics in groups)
            {
                Console.WriteLine($"Processing {fileDiagnostics.Key}");

                string code = File.ReadAllText(fileDiagnostics.Key);
                code = fixViolations(code, fileDiagnostics);
                File.WriteAllText(fileDiagnostics.Key, code, Encoding.UTF8);
            }

            Console.WriteLine($"Done fixing {violationId}");
        }

        private static string FixSA1005SingleLineCommentMustBeginWithASpace(string code, IEnumerable<Diagnostic> diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
            {
                string codeBeforeViolation = code.Substring(0, diagnostic.Location.SourceSpan.Start + 2);
                string codeAfterViolation = code.Substring(diagnostic.Location.SourceSpan.Start + 2);
                code = $"{codeBeforeViolation} {codeAfterViolation}";
            }

            return code;
        }

        private static string FixSA1101PrefixLocallCallsWithThis(string code, IEnumerable<Diagnostic> diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
            {
                string codeBeforeViolation = code.Substring(0, diagnostic.Location.SourceSpan.Start);
                string codeAfterViolation = code.Substring(diagnostic.Location.SourceSpan.Start);
                code = $"{codeBeforeViolation}this.{codeAfterViolation}";
            }

            return code;
        }

        private static IEnumerable<Diagnostic> BuildSolution(string solutionPath)
        {
            using (MSBuildWorkspace workspace = MSBuildWorkspace.Create())
            {
                Solution solution = workspace.OpenSolutionAsync(solutionPath).Result;

                return solution
                    .GetProjectDependencyGraph()
                    .GetTopologicallySortedProjects()
                    .SelectMany(projectId =>
                    {
                        Project project = solution.GetProject(projectId);

                        Console.WriteLine($"Building {project.Name}...");
                        Task<Compilation> compilationTask = project.GetCompilationAsync();
                        compilationTask.Wait();
                        Compilation compilation = compilationTask.Result;

                        Console.WriteLine($"Running code analysis on on {project.Name}...");
                        CompilationWithAnalyzersOptions options = new CompilationWithAnalyzersOptions(
                            new AnalyzerOptions(default(ImmutableArray<AdditionalText>)), OnAnalyzerException, false, false);
                        CompilationWithAnalyzers compilationWithAnalyzers = new CompilationWithAnalyzers(compilation, Analyzers, options);

                        Task<ImmutableArray<Diagnostic>> analyzerDiagnosticsTask = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
                        analyzerDiagnosticsTask.Wait();
                        return analyzerDiagnosticsTask.Result;
                    })
                    .ToList();
            }
        }

        private static string GetPathToFile(string file)
        {
            string exeFilePath = Assembly.GetExecutingAssembly().Location;
            string exeFolder = Path.GetDirectoryName(exeFilePath);
            return Path.Combine(exeFolder, file);
        }

        private static void OnAnalyzerException(Exception exception, DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
        {
        }
    }
}
