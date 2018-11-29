using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using JetBrains.Annotations;
using YetAnotherXamlPad.Utilities;

namespace YetAnotherXamlPad
{
    internal readonly struct ParsedViewModelCode
    {
        private ParsedViewModelCode([NotNull] SyntaxTree syntaxTree)
        {
            SyntaxTree = syntaxTree;
            NamespaceNames = syntaxTree
                                .GetRoot()
                                .DescendantNodes()
                                .OfType<NamespaceDeclarationSyntax>()
                                .Select(s => s.Name.ToString())
                                .AsImmutable();
        }

        public readonly SyntaxTree SyntaxTree;

        public readonly IReadOnlyCollection<string> NamespaceNames;

        public static ParsedViewModelCode? TryParseViewModelCode(string viewModelCode)
        {
            var parsedCode = CSharpSyntaxTree.ParseText(viewModelCode);
            return parsedCode == null ? default : new ParsedViewModelCode(parsedCode);
        }
    }
}
