using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpCodeFixer
{
    public static class CodeFixer
    {
        public static int FixAllFixableViolations(string solutionPath)
        {
            int count = 0;

            count += FixViolations(solutionPath, "SA1005", FixSA1005SingleLineCommentMustBeginWithASpace);
            count += FixViolations(solutionPath, "SA1028", FixSA1028CodeMustNotContainTrailingWhitespace);
            count += FixViolations(solutionPath, "SA1101", FixSA1101PrefixLocallCallsWithThis);
            count += FixViolations(solutionPath, "SA1119", FixSA1119StatementMustNotUseUnnecessaryParenthesis);
            count += FixViolations(solutionPath, "SA1121", FixSA1121UseBuiltInTypeAlias);
            count += FixViolations(solutionPath, "SA1122", FixSA1122UseStringDotEmptyForEmptyStrings);
            count += FixViolations(solutionPath, "SA1513", FixSA1513ClosingBraceMustBeFollowedByBlankLine);
            count += FixViolations(solutionPath, "SA1516", FixSA1516ElementsMustBeSeparatedByBlankLine);

            count += FixViolations(solutionPath, "CS8019", FixCS8019UnncessaryUsingDirective);

            return count;
        }

        private static int FixViolations(string solutionPath, string violationId, Func<string, IEnumerable<Diagnostic>, string> fixViolations)
        {
            ImmutableArray<Diagnostic> dianostics = BuildSolutionForViolation(solutionPath, violationId);

            Console.WriteLine();
            Console.WriteLine($"Found {dianostics.Count()} {violationId} violations...");

            IEnumerable<IGrouping<string, Diagnostic>> diagnosticsByFile = dianostics
                .GroupBy(d => d.Location.GetLineSpan().Path)
                .ToList();

            foreach (IGrouping<string, Diagnostic> fileDiagnostics in diagnosticsByFile)
            {
                Console.WriteLine($"Processing {fileDiagnostics.Key}");

                string code = File.ReadAllText(fileDiagnostics.Key);
                code = fixViolations(code, fileDiagnostics);
                File.WriteAllText(fileDiagnostics.Key, code, Encoding.UTF8);
            }

            Console.WriteLine($"Done fixing {violationId}");

            return dianostics.Count();
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

        private static string FixSA1028CodeMustNotContainTrailingWhitespace(string code, IEnumerable<Diagnostic> diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
            {
                string codeBeforeViolation = code.Substring(0, diagnostic.Location.SourceSpan.Start);
                string codeAfterViolation = code.Substring(diagnostic.Location.SourceSpan.End);
                code = $"{codeBeforeViolation}{codeAfterViolation}";
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

        private static string FixSA1119StatementMustNotUseUnnecessaryParenthesis(string code, IEnumerable<Diagnostic> diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
            {
                string codeBeforeViolation = code.Substring(0, diagnostic.Location.SourceSpan.Start);
                string statementWithoutParenthesis = code.Substring(diagnostic.Location.SourceSpan.Start + 1, diagnostic.Location.SourceSpan.Length - 2);
                string codeAfterViolation = code.Substring(diagnostic.Location.SourceSpan.End);
                code = $"{codeBeforeViolation}{statementWithoutParenthesis}{codeAfterViolation}";
            }

            return code;
        }

        private static string FixSA1121UseBuiltInTypeAlias(string code, IEnumerable<Diagnostic> diagnostics)
        {
            Dictionary<string, string> aliases = new Dictionary<string, string>
            {
                { "Boolean", "bool" },
                { "Byte", "byte" },
                { "Char", "char" },
                { "Decimal", "decimal" },
                { "Double", "double" },
                { "Int16", "short" },
                { "Int32", "int" },
                { "Int64", "long" },
                { "Object", "object" },
                { "SByte", "sbyte" },
                { "Single", "single" },
                { "String", "string" },
                { "UInt16", "ushort" },
                { "UInt32", "uint" },
                { "UInt64", "ulong" }
            };

            foreach (Diagnostic diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
            {
                string violation = code.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length)
                    .Replace("System.", string.Empty);

                string codeBeforeViolation = code.Substring(0, diagnostic.Location.SourceSpan.Start);
                string codeAfterViolation = code.Substring(diagnostic.Location.SourceSpan.End);
                code = $"{codeBeforeViolation}{aliases[violation]}{codeAfterViolation}";
            }

            return code;
        }

        private static string FixSA1122UseStringDotEmptyForEmptyStrings(string code, IEnumerable<Diagnostic> diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
            {
                string codeBeforeViolation = code.Substring(0, diagnostic.Location.SourceSpan.Start);
                string codeAfterViolation = code.Substring(diagnostic.Location.SourceSpan.End);
                code = $"{codeBeforeViolation}string.Empty{codeAfterViolation}";
            }

            return code;
        }

        private static string FixSA1513ClosingBraceMustBeFollowedByBlankLine(string code, IEnumerable<Diagnostic> diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
            {
                string codeBeforeViolation = code.Substring(0, diagnostic.Location.SourceSpan.Start);
                string codeAfterViolation = code.Substring(diagnostic.Location.SourceSpan.Start);
                code = $"{codeBeforeViolation}{Environment.NewLine}{codeAfterViolation}";
            }

            return code;
        }

        private static string FixSA1516ElementsMustBeSeparatedByBlankLine(string code, IEnumerable<Diagnostic> diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
            {
                string codeBeforeViolation = code.Substring(0, diagnostic.Location.SourceSpan.Start);
                string codeAfterViolation = code.Substring(diagnostic.Location.SourceSpan.Start);
                code = $"{codeBeforeViolation}{Environment.NewLine}{codeAfterViolation}";
            }

            return code;
        }

        private static string FixCS8019UnncessaryUsingDirective(string code, IEnumerable<Diagnostic> diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
            {
                string codeBeforeViolation = code.Substring(0, diagnostic.Location.SourceSpan.Start);
                string codeAfterViolation = code.Substring(diagnostic.Location.SourceSpan.End);
                code = $"{codeBeforeViolation}{codeAfterViolation.TrimStart()}";
            }

            return code;
        }

        private static ImmutableArray<Diagnostic> BuildSolutionForViolation(string solutionPath, string violationId)
        {
            Console.WriteLine();
            Console.WriteLine($"Building for {violationId}... ");

            using (MSBuildWorkspace workspace = MSBuildWorkspace.Create())
            {
                Solution solution = workspace.OpenSolutionAsync(solutionPath).Result;

                return solution
                    .GetProjectDependencyGraph()
                    .GetTopologicallySortedProjects()
                    .SelectMany(projectId => BuildProject(solution, projectId, violationId))
                    .ToImmutableArray();
            }
        }

        private static ImmutableArray<Diagnostic> BuildProject(Solution solution, ProjectId projectId, string violationId)
        {
            ImmutableArray<Diagnostic> diagnostics = default(ImmutableArray<Diagnostic>);

            Project project = solution.GetProject(projectId);

            Console.WriteLine($"Building {project.Name}...");

            Task<Compilation> compilationTask = project.GetCompilationAsync();
            compilationTask.Wait();
            Compilation compilation = compilationTask.Result;
            diagnostics = compilation.GetDiagnostics();

            ImmutableArray<DiagnosticAnalyzer> analyzers = AnalyzerCollection.GetAnalyzersForViolation(violationId);
            if (analyzers.Any())
            {
                Console.WriteLine($"Running {violationId} analyzers on {project.Name}...");

                CompilationWithAnalyzersOptions options = new CompilationWithAnalyzersOptions(
                    new AnalyzerOptions(default(ImmutableArray<AdditionalText>)), OnAnalyzerException, false, false);
                CompilationWithAnalyzers compilationWithAnalyzers = new CompilationWithAnalyzers(compilation, analyzers, options);

                Task<ImmutableArray<Diagnostic>> analyzerDiagnosticsTask = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
                analyzerDiagnosticsTask.Wait();
                diagnostics = analyzerDiagnosticsTask.Result;
            }

            ImmutableArray<Diagnostic> violationDiagnostics = diagnostics
                .Where(d => d.Id == violationId)
                .ToImmutableArray();

            return violationDiagnostics;
        }

        private static void OnAnalyzerException(Exception exception, DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
        {
        }
    }
}
