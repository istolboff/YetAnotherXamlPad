﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using JetBrains.Annotations;
using YetAnotherXamlPad.Utilities;
using static YetAnotherXamlPad.Utilities.Either;
using System.Windows;

namespace YetAnotherXamlPad
{
    internal class ViewModelAssemblyBuilder
    {
        private ViewModelAssemblyBuilder([NotNull] string assemblyName, [NotNull] SyntaxTree syntaxTree)
        {
            AssemblyName = assemblyName;
            _syntaxTree = syntaxTree;
        }

        public string AssemblyName { get; }

        public Either<Exception, KeyValuePair<string, byte[]>> Build()
        {
            var compilation = CSharpCompilation.Create(
                AssemblyName,
                syntaxTrees: new[] { _syntaxTree },
                references: ListReferences(),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using (var memoryStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(memoryStream);
                return emitResult.Success
                    ? Right<Exception, KeyValuePair<string, byte[]>>(Make.Pair(AssemblyName, memoryStream.ToArray()))
                    : Left((Exception)new AssemblyBuildException(emitResult.Diagnostics));
            }
        }

        public static Either<Exception, ViewModelAssemblyBuilder>? TryCreateAssemblyBuilder(
            XamlCode xamlCode, 
            ParsedViewModelCode? viewModelCode)
        {
            return viewModelCode == null 
                ? default 
                : TryGetViewModelAssemblyName(xamlCode, viewModelCode.Value)
                    .Map(assemblyName => new ViewModelAssemblyBuilder(assemblyName, viewModelCode.Value.SyntaxTree));
        }

        public static void WarmUp()
        {
            var parsedCode = ParsedViewModelCode.TryParseViewModelCode(WarmUpCsharpCode);
            if (parsedCode != null)
            {
                new ViewModelAssemblyBuilder(
                    "__" + Guid.NewGuid().ToString("N"),
                    parsedCode.Value.SyntaxTree)
                .Build();
            }
        }

        private MetadataReference[] ListReferences()
        {
            return new[] { MscorLib, SystemLib, PresentationFrameworkLib };
        }

        private static Either<Exception, string>? TryGetViewModelAssemblyName(
            XamlCode xamlCode, 
            ParsedViewModelCode viewModelCode)
        {
            var namesOfAssembliesThatContainViewModelNamespaces = xamlCode.MentionedAssemblies
                    .Where(kvp => kvp.Value.Intersect(viewModelCode.NamespaceNames).Any())
                    .Select(kvp => kvp.Key)
                    .AsImmutable();


            switch (namesOfAssembliesThatContainViewModelNamespaces.Count)
            {
                case 0:
                    return default;

                case 1:
                    return Right(namesOfAssembliesThatContainViewModelNamespaces.Single());

                default:
                    return Left(
                        (Exception)new InvalidOperationException(
                            "XAML namespaces erroneousely mention ViewModel's namespaces belonging to the following assemblies: " +
                            string.Join(",", namesOfAssembliesThatContainViewModelNamespaces) +
                            ". Please use *single* assembly name for all ViewModel namespaces."));
            }
        }

        private readonly SyntaxTree _syntaxTree;

        private static readonly MetadataReference MscorLib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference SystemLib = MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location);
        private static readonly MetadataReference PresentationFrameworkLib = MetadataReference.CreateFromFile(typeof(Window).Assembly.Location);

        private const string WarmUpCsharpCode =
@"namespace ViewModels
{
    public class ViewModel
    {
        public static string Greetings = ""Hello, World!"";

        public void Method() 
        {
            Console.WriteLine(Greetings);
        }
    }
}";
    }
}
