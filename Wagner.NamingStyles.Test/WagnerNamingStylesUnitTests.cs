using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = Wagner.NamingStyles.Test.CSharpCodeFixVerifier<
    Wagner.NamingStyles.WagnerNamingStylesCodeFixProvider>;

namespace Wagner.NamingStyles.Test
{
    [TestClass]
    public class WagnerNamingStylesUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }


        [TestMethod]
        public async Task TestMethod2()
        {
            CancellationToken cancellationToken = default;

            // This will get the current WORKING directory (i.e. \bin\Debug\netcore3.1)
            string workingDirectory = Environment.CurrentDirectory;
            // or: Directory.GetCurrentDirectory() gives the same result

            // This will get the current PROJECT directory
            string testDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
            string testSolutionPath = Path.Combine(testDirectory, @"TestSolution\Solution.sln");
            string fixedSolutionPath = Path.Combine(testDirectory, @"FixedSolution\Solution.sln");

            Solution? testSolution = await GetTestSolutionAsync(testSolutionPath, cancellationToken);
            Solution? fixedSolution = await GetFixedSolutionAsync(fixedSolutionPath, cancellationToken);

            bool result = await TestIfSolutionsAreIdenticalAsync(testSolution, fixedSolution, cancellationToken);
            Assert.IsTrue(result);
        }

        public async Task<Solution?> GetTestSolutionAsync(string solutionPath, CancellationToken cancellationToken = default)
        {
            if (!VisualStudioMSBuildResolver.Resolve(VisualStudioMSBuildResolverOption.UseLatest))
            {
                Console.WriteLine($"No version of MSBuild registered.");
                return null;
            }

            using (var workspace = MSBuildWorkspace.Create())
            {
                // Print message for WorkspaceFailed event to help diagnosing project load failures.
                workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);

                Solution solution = await workspace.OpenSolutionAsync(solutionPath);
                Project project = solution.Projects.First();
                var diagnostics =  workspace.Diagnostics;
                

                WagnerNamingStylesCodeFixProvider fixer = new WagnerNamingStylesCodeFixProvider();
                FixAllContext.DiagnosticProvider diagnosticProvider = new FixAllNamesDiagnosticProvider();
                FixAllContext fixAllContext = new FixAllContext(project, fixer, FixAllScope.Solution, nameof(WagnerNamingStylesCodeFixProvider), fixer.FixableDiagnosticIds, diagnosticProvider, cancellationToken);
                var codeFix = await fixer.GetFixAllProvider().GetFixAsync(fixAllContext);
                var operations = await codeFix.GetOperationsAsync(cancellationToken);
                foreach( var operation in operations )
                {
                    operation.Apply(workspace, cancellationToken);
                }

                return workspace.CurrentSolution;
            }
        }

        private class FixAllNamesDiagnosticProvider : FixAllContext.DiagnosticProvider
        {
            public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        public async Task<Solution?> GetFixedSolutionAsync(string solutionPath, CancellationToken cancellationToken = default)
        {
            if (!VisualStudioMSBuildResolver.Resolve(VisualStudioMSBuildResolverOption.UseLatest))
            {
                Console.WriteLine($"No version of MSBuild registered.");
                return null;
            }

            using (var workspace = MSBuildWorkspace.Create())
            {
                // Print message for WorkspaceFailed event to help diagnosing project load failures.
                workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);

                Solution solution = await workspace.OpenSolutionAsync(solutionPath);

                return solution;
            }
        }

        private async Task<bool> TestIfSolutionsAreIdenticalAsync(Solution solution1, Solution solution2, CancellationToken cancellationToken = default)
        {
            if(solution1.Projects.Count() != solution2.Projects.Count())
            {
                return false;
            }
            foreach(Project project1 in solution1.Projects)
            {
                Project project2 = solution2.Projects.FirstOrDefault(project=>project.Name == project1.Name);
                if (project2 is null || project1.Documents.Count() != project2.Documents.Count())
                {
                    return false;
                }
                foreach (Document document1 in project1.Documents)
                {
                    Document document2 = project2.Documents.FirstOrDefault(document => document.Name == document1.Name);
                    if (document2 is null || await document1.GetTextAsync() != await document2.GetTextAsync())
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }


}
