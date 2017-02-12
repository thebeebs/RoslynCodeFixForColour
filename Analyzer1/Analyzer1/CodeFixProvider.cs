using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace Analyzer1
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Analyzer1CodeFixProvider)), Shared]
    public class Analyzer1CodeFixProvider : CodeFixProvider
    {
        private const string title = "Speak English";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(Analyzer1Analyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            SyntaxNode declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            if (declaration == null) {
                declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            }
            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: c => CorrectSpelling(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Solution> CorrectSpelling(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            string identifierToken;
            if (node.IsKind(SyntaxKind.PropertyDeclaration))
            {
                identifierToken = ((PropertyDeclarationSyntax)node).Identifier.Text;
            }
            else
            {
                identifierToken = ((TypeDeclarationSyntax)node).Identifier.Text;
            }
            
            var newName = identifierToken.Replace("olor", "olour");

            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

            // Return the new solution with the now-uppercase type name.
            return newSolution;
        }
    }
}