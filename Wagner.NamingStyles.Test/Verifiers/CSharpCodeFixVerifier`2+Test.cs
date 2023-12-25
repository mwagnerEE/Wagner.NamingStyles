using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Wagner.NamingStyles.Test
{
    public class CSharpCodeFixTest<TCodeFix, TVerifier> : CodeFixTest<TVerifier>
        where TCodeFix : CodeFixProvider, new()
        where TVerifier : IVerifier, new()
    {
        private static readonly LanguageVersion DefaultLanguageVersion =
            Enum.TryParse("Default", out LanguageVersion version) ? version : LanguageVersion.CSharp6;

        protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
            => new[] { new TCodeFix() };

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            => new[] { MicrosoftAnalyzerAccessor.ConstructCSharpNamingStyleDiagnosticAnalyzer() };

        protected override string DefaultFileExt => "cs";

        public override string Language => LanguageNames.CSharp;

        public override Type SyntaxKindType => typeof(SyntaxKind);

        protected override CompilationOptions CreateCompilationOptions()
            => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);

        protected override ParseOptions CreateParseOptions()
            => new CSharpParseOptions(DefaultLanguageVersion, DocumentationMode.Diagnose);


        


    }
}
