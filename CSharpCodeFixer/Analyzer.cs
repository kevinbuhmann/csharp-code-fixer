using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CSharpCodeFixer
{
    public static class Analyzer
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

        public static ImmutableArray<Diagnostic> GetDiagnostics(string filePath, string csharpSource)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(csharpSource, path: filePath);

            CSharpCompilation compilation = CSharpCompilation.Create("target")
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(tree);

            ImmutableArray<Diagnostic> parseDiagnostics = compilation.GetParseDiagnostics();

            CompilationWithAnalyzersOptions options = new CompilationWithAnalyzersOptions(
                new AnalyzerOptions(default(ImmutableArray<AdditionalText>)), OnAnalyzerException, false, false);

            CompilationWithAnalyzers compilationWithAnalyzers = new CompilationWithAnalyzers(compilation, Analyzers, options);

            Task<ImmutableArray<Diagnostic>> analyzerDiagnosticsTask = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            analyzerDiagnosticsTask.Wait();
            ImmutableArray<Diagnostic> analyzerDiagnostics = analyzerDiagnosticsTask.Result;

            return parseDiagnostics
                .Concat(analyzerDiagnostics)
                .ToImmutableArray();
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