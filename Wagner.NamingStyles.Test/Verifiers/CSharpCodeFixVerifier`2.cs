using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Wagner.NamingStyles.Test
{
    public static partial class CSharpCodeFixVerifier<TCodeFix>
        where TCodeFix : CodeFixProvider, new()
    {


        private static bool IsDiagnosticMethodWithNoParameter(MethodInfo methodInfo) => methodInfo.Name == nameof(Diagnostic) && methodInfo.GetParameters().Select(p => p.ParameterType).SequenceEqual(new Type[] {  });
        private static bool IsDiagnosticMethodWithStringParameter(MethodInfo methodInfo) => methodInfo.Name == nameof(Diagnostic) && methodInfo.GetParameters().Select(p => p.ParameterType).SequenceEqual(new Type[] { typeof(string) });
        private static bool IsDiagnosticMethodWithDiagnosticDescriptorParameter(MethodInfo methodInfo) => methodInfo.Name == nameof(Diagnostic) && methodInfo.GetParameters().Select(p => p.ParameterType).SequenceEqual(new Type[] { typeof(DiagnosticDescriptor) });

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic()"/>
        public static DiagnosticResult Diagnostic()
        {
            object[] args = new object[] { };
            Func<MethodInfo, bool> predicate = IsDiagnosticMethodWithNoParameter;
            var methods = GetCSharpCodeFixVerifierMethods();
            MethodInfo methodInfo = methods.Where(predicate).FirstOrDefault();
            return (DiagnosticResult)methodInfo.Invoke(null, args);
        }


        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic(string)"/>
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            object[] args = new object[] { diagnosticId };
            Func<MethodInfo, bool> predicate = IsDiagnosticMethodWithStringParameter;
            var methods = GetCSharpCodeFixVerifierMethods();
            MethodInfo methodInfo = methods.Where(predicate).FirstOrDefault();
            return (DiagnosticResult)methodInfo.Invoke(null, args);
        }


        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic(DiagnosticDescriptor)"/>
        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        {
            object[] args = new object[] { descriptor };
            Func<MethodInfo, bool> predicate = IsDiagnosticMethodWithDiagnosticDescriptorParameter;
            var methods = GetCSharpCodeFixVerifierMethods();
            MethodInfo methodInfo = methods.Where(predicate).FirstOrDefault();
            return (DiagnosticResult)methodInfo.Invoke(null, args);
        }

        private static MethodInfo[] GetCSharpCodeFixVerifierMethods()
        {
            Type type = typeof(CSharpCodeFixVerifier<,,>);
            string assemblyQualifiedName = type.AssemblyQualifiedName;
            Type classType = Type.GetType(assemblyQualifiedName);


            Type[] genericArguments = { MicrosoftAnalyzerAccessor.CSharpNamingStyleDiagnosticAnalyzerType, typeof(TCodeFix), typeof(MSTestVerifier) };
            Type constructedType = classType.MakeGenericType(genericArguments);
            Type baseType = constructedType.BaseType;
            return baseType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        }

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
        public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new Test
            {
                TestCode = source,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, string)"/>
        public static async Task VerifyCodeFixAsync(string source, string fixedSource)
            => await VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult, string)"/>
        public static async Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
            => await VerifyCodeFixAsync(source, new[] { expected }, fixedSource);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult[], string)"/>
        public static async Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
        {
            var test = new Test
            {
                TestCode = source,
                FixedCode = fixedSource,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }
    }
}
