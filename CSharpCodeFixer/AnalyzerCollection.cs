using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Vstack.Extensions;

namespace CSharpCodeFixer
{
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = SuppressionJustifications.IgnoringNamingConflict)]
    public static class AnalyzerCollection
    {
        private static ImmutableArray<DiagnosticAnalyzer> analyzers;

        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFile", Justification = SuppressionJustifications.Sorry)]
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

        public static ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForViolation(string violationId)
        {
            return Analyzers
                .Where(a => a.SupportedDiagnostics.Any(d => d.Id == violationId))
                .ToImmutableArray();
        }

        private static string GetPathToFile(string file)
        {
            string exeFilePath = Assembly.GetExecutingAssembly().Location;
            string exeFolder = Path.GetDirectoryName(exeFilePath);
            return Path.Combine(exeFolder, file);
        }
    }
}
