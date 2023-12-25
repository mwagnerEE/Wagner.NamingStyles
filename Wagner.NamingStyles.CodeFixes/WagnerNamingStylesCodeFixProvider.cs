/*
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors
Copyright (c) 2023 Mike Wagner

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace Wagner.NamingStyles
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WagnerNamingStylesCodeFixProvider)), Shared]
    public class WagnerNamingStylesCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(NamingStyle.NamingRuleId); }
        }


        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;

        }


        NamingStyle _style;
        CodeFixContext _context;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            _context = context;
            Diagnostic diagnostic = context.Diagnostics.First();
            string serializedNamingStyle = diagnostic.Properties[nameof(NamingStyle)];
            _style = NamingStyle.FromXElement(XElement.Parse(serializedNamingStyle));

            Document activeDocument = context.Document;
            TextSpan span = context.Span;

            SyntaxNode root = await activeDocument.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            SyntaxNode? node = root.FindNode(span);

            if (node is null)
            {
                return;
            }

            SemanticModel model = await activeDocument.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            ISymbol activeSymbol = model.GetDeclaredSymbol(node, context.CancellationToken);

            // TODO: We should always be able to find the symbol that generated this diagnostic,
            // but this cannot always be done by simply asking for the declared symbol on the node 
            // from the symbol's declaration location.
            // See https://github.com/dotnet/roslyn/issues/16588

            if (activeSymbol is null)
            {
                return;
            }
            OptionSet options = await activeDocument.GetOptionsAsync(context.CancellationToken).ConfigureAwait(false);

            var fixedNames = _style.MakeCompliant(activeSymbol.Name);
            foreach (var fixedName in fixedNames) //Not sure how multiple compliant names are possible, but the fix all code uses the first one it finds.
            {
                context.RegisterCodeFix(
                    new FixNameCodeAction(
                        activeDocument.Project.Solution, activeSymbol, fixedName,
                        string.Format(CodeFixesResources.Fix_Name_Violation_colon_0, fixedName),
                        c => FixAsync(activeDocument, activeSymbol, fixedName, c),
                        equivalenceKey: nameof(WagnerNamingStylesCodeFixProvider), options),
                    diagnostic);
            }
        }



        private static async Task<Solution> FixAsync(Document document, ISymbol symbol, string fixedName, CancellationToken cancellationToken)
        {
            return await Renamer.RenameSymbolAsync(document.Project.Solution, symbol, fixedName, await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }




        private class FixNameCodeAction : CodeAction
        {
            private readonly Solution _startingSolution;
            private readonly ISymbol _symbol;
            private readonly string _newName;
            private readonly string _title;
            private readonly Func<CancellationToken, Task<Solution>> _createChangedSolutionAsync;
            private readonly string _equivalenceKey;
            private readonly OptionSet _options;

            public FixNameCodeAction(
                Solution startingSolution, ISymbol symbol, string newName, string title, Func<CancellationToken, Task<Solution>> createChangedSolutionAsync,
                string equivalenceKey, OptionSet options)
            {
                _startingSolution = startingSolution;
                _symbol = symbol;
                _newName = newName;
                _title = title;
                _createChangedSolutionAsync = createChangedSolutionAsync;
                _equivalenceKey = equivalenceKey;
                _options = options;
            }

            public override string Title => _title;

            public override string EquivalenceKey => _equivalenceKey;

            protected static async Task<Solution> FixAsync(Solution startingSolution, OptionSet options, ISymbol symbol, string fixedName, CancellationToken cancellationToken)
            {
                return await Renamer.RenameSymbolAsync(startingSolution, symbol, fixedName, options, cancellationToken).ConfigureAwait(false);
            }

            protected override async Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                return await FixAsync(_startingSolution, _options, _symbol, _newName, cancellationToken);
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
            {
                return new ApplyChangesOperation[] { new ApplyChangesOperation(await _createChangedSolutionAsync(cancellationToken).ConfigureAwait(false)) };
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            {
                var newSolution = await _createChangedSolutionAsync(cancellationToken).ConfigureAwait(false);
                var codeAction = new ApplyChangesOperation(newSolution);

                var factory = CallGetISymbolRenamedCodeActionOperationFactoryWorkspaceService(_startingSolution.Workspace.Services);
                return new CodeActionOperation[]
                {
                    codeAction,
                    CallCreateSymbolRenamedOperation(factory, _symbol, _newName, _startingSolution, newSolution)
                };

            }




            #region Reflection
            private static System.Reflection.Assembly AssemblyResolver(System.Reflection.AssemblyName assemblyName)
            {
                assemblyName.Version = null;
                return System.Reflection.Assembly.Load(assemblyName);
            }

            private static readonly Type _ISymbolRenamedCodeActionOperationFactoryWorkspaceServiceType
                = Type.GetType("Microsoft.CodeAnalysis.CodeActions.WorkspaceServices.ISymbolRenamedCodeActionOperationFactoryWorkspaceService, Microsoft.CodeAnalysis.Features, Version=3.3.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

            private delegate IWorkspaceService GetRequiredServiceCaller(HostWorkspaceServices instance);
            private static readonly MethodInfo _cachedGetRequiredServiceMethodInfo = typeof(HostWorkspaceServices).GetMethod(nameof(HostWorkspaceServices.GetRequiredService), BindingFlags.Public | BindingFlags.Instance);

            private static readonly MethodInfo _cachedGenericGetRequiredServiceMethodInfo = _cachedGetRequiredServiceMethodInfo.MakeGenericMethod(_ISymbolRenamedCodeActionOperationFactoryWorkspaceServiceType);
            private static readonly GetRequiredServiceCaller _callerForGetRequiredService = (GetRequiredServiceCaller)Delegate.CreateDelegate(typeof(GetRequiredServiceCaller), _cachedGenericGetRequiredServiceMethodInfo);
            private static IWorkspaceService CallGetISymbolRenamedCodeActionOperationFactoryWorkspaceService(HostWorkspaceServices instance)
            {
                return _callerForGetRequiredService(instance);
            }



            private delegate CodeActionOperation CreateSymbolRenamedOperationCaller(IWorkspaceService instance, ISymbol symbol, string newName, Solution startingSolution, Solution updatedSolution);
            private static readonly MethodInfo _cachedCreateSymbolRenamedOperationMethodInfo = _ISymbolRenamedCodeActionOperationFactoryWorkspaceServiceType.GetMethod("CreateSymbolRenamedOperation", BindingFlags.Public | BindingFlags.Instance);
            //private static readonly CreateSymbolRenamedOperationCaller _callerForCreateSymbolRenamedOperation = (CreateSymbolRenamedOperationCaller)Delegate.CreateDelegate(typeof(CreateSymbolRenamedOperationCaller), _cachedCreateSymbolRenamedOperationMethodInfo);
            private static CodeActionOperation CallCreateSymbolRenamedOperation(IWorkspaceService instance, ISymbol symbol, string newName, Solution startingSolution, Solution updatedSolution)
            {
                object[] args = new object[] { symbol, newName, startingSolution, updatedSolution };
                return (CodeActionOperation)_cachedCreateSymbolRenamedOperationMethodInfo.Invoke(instance, args);
                //return _callerForCreateSymbolRenamedOperation(instance, symbol, newName, startingSolution, updatedSolution);
            }

            #endregion

        }
    }
}
