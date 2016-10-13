using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CSharpCodeFixer
{
    public static class CodeFixer
    {
        public static void FixAll(string filePath)
        {
            FixSA1101PrefixLocallCallsWithThis(filePath);
            FixSA1005SingleLineCommentMustBeginWithASpace(filePath);
        }

        public static void FixSA1101PrefixLocallCallsWithThis(string filePath)
        {
            string code = File.ReadAllText(filePath);

            IEnumerable<Diagnostic> diagnostics = Analyzer.GetDiagnostics(filePath, code)
                .Where(diagnostic => diagnostic.Id == "SA1101")
                .OrderByDescending(diagnostic => diagnostic.Location.SourceSpan.Start)
                .ToList();

            foreach (Diagnostic diagnostic in diagnostics)
            {
                string codeBeforeViolation = code.Substring(0, diagnostic.Location.SourceSpan.Start);
                string codeAfterViolation = code.Substring(diagnostic.Location.SourceSpan.Start);
                code = $"{codeBeforeViolation}this.{codeAfterViolation}";
            }

            File.WriteAllText(filePath, code, Encoding.UTF8);
        }

        public static void FixSA1005SingleLineCommentMustBeginWithASpace(string filePath)
        {
            string code = File.ReadAllText(filePath);

            IEnumerable<Diagnostic> diagnostics = Analyzer.GetDiagnostics(filePath, code)
                .Where(diagnostic => diagnostic.Id == "SA1005")
                .OrderByDescending(diagnostic => diagnostic.Location.SourceSpan.Start)
                .ToList();

            foreach (Diagnostic diagnostic in diagnostics)
            {
                string codeBeforeViolation = code.Substring(0, diagnostic.Location.SourceSpan.Start + 2);
                string codeAfterViolation = code.Substring(diagnostic.Location.SourceSpan.Start + 2);
                code = $"{codeBeforeViolation} {codeAfterViolation}";
            }

            File.WriteAllText(filePath, code, Encoding.UTF8);
        }
    }
}
