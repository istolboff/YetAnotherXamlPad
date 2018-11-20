using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static YetAnotherXamlPad.Either;

namespace YetAnotherXamlPad
{
    internal static class ViewModelAssemblyBuilder
    {
        public static ViewModelAssemblyData? TryParseViewModelCode(string viewModelCode, string xamlCode)
        {
            var parsedCode = CSharpSyntaxTree.ParseText(viewModelCode);
            var namespaceName = TryExtractViewModelNamespace(parsedCode);
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return null;
            }

            var assemblyName = TryExtractAssemblyNameForViewModelNamespace(xamlCode, namespaceName);
            return assemblyName == null ? (ViewModelAssemblyData?)null : new ViewModelAssemblyData(parsedCode, assemblyName);
        }

        public static Either<KeyValuePair<string, byte[]>, Exception>? BuildViewModelAssembly(
            ViewModelAssemblyData? viewModelAssemblyData)
        {
            if (viewModelAssemblyData == null)
            {
                return null;
            }

            var compilation = CSharpCompilation.Create(
                viewModelAssemblyData.Value.AssemblyName,
                syntaxTrees: new[] { viewModelAssemblyData.Value.SyntaxTree },
                references: new[] { MscorLib },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var memoryStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(memoryStream);
                return emitResult.Success
                    ? Left<KeyValuePair<string, byte[]>, Exception>(Make.Pair(viewModelAssemblyData.Value.AssemblyName, memoryStream.ToArray()))
                    : Right(new InvalidOperationException(string.Join(Environment.NewLine, emitResult.Diagnostics)) as Exception);
            }
        }

        private static string TryExtractAssemblyNameForViewModelNamespace(string xamlCode, string viewModelNamespaceName)
        {
            if (string.IsNullOrEmpty(xamlCode) || string.IsNullOrEmpty(viewModelNamespaceName))
            {
                return null;
            }

            var xamlMarkupRoot = XDocument.Parse(xamlCode).Root;
            if (xamlMarkupRoot == null)
            {
                return null;
            }

            var expectedPrefix = $"clr-namespace:{viewModelNamespaceName};assembly=";
            var relevantUrl = xamlMarkupRoot
                .Attributes()
                .Where(a => a.IsNamespaceDeclaration)
                .GroupBy(
                    a => a.Name.Namespace == XNamespace.None ? string.Empty : a.Name.LocalName,
                    a => XNamespace.Get(a.Value))
                .ToDictionary(g => g.Key, g => g.First())
                .Values
                .FirstOrDefault(url => url.ToString().StartsWith(expectedPrefix));

            return relevantUrl?.ToString().Substring(expectedPrefix.Length);
        }

        private static string TryExtractViewModelNamespace(SyntaxTree synaxTree)
        {
            var namespaces = synaxTree.GetRoot().DescendantNodes().OfType<NamespaceDeclarationSyntax>().ToArray();
            return (namespaces.Length == 1) ? namespaces.Single().Name.ToString() : null;
        }

        private static readonly MetadataReference MscorLib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
    }
}