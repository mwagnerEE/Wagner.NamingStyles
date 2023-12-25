using System;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Wagner.NamingStyles.Test
{
    internal static class MicrosoftAnalyzerAccessor
    {
        private delegate DiagnosticAnalyzer NamingStyleDiagnosticAnalyzerConstructor();
        private const string AnalyzerAssembly = "Microsoft.CodeAnalysis.CSharp.Features, Version=3.3.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
        private const string AnalyzerNamespace = "Microsoft.CodeAnalysis.CSharp.Diagnostics.NamingStyles";
        private const string AnalyzerName = "CSharpNamingStyleDiagnosticAnalyzer";
        private static readonly string _analyzerAssemblyQualifiedName = Assembly.CreateQualifiedName(AnalyzerAssembly, AnalyzerNamespace + "." + AnalyzerName);
        private static readonly Type _typeofCSharpNamingStyleDiagnosticAnalyzer = Type.GetType(_analyzerAssemblyQualifiedName);
        private static readonly ConstructorInfo _cachedNamingStyleDiagnosticAnalyzerConstructorInfo = _typeofCSharpNamingStyleDiagnosticAnalyzer.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null);


        public static Type CSharpNamingStyleDiagnosticAnalyzerType => _typeofCSharpNamingStyleDiagnosticAnalyzer;
        public static DiagnosticAnalyzer ConstructCSharpNamingStyleDiagnosticAnalyzer()
        {
            var namingStyleDiagnosticAnalyzer = (DiagnosticAnalyzer)_cachedNamingStyleDiagnosticAnalyzerConstructorInfo.Invoke(null);
            return namingStyleDiagnosticAnalyzer;
        }
    }
}
