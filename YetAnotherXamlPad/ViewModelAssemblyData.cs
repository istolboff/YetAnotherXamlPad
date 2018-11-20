using Microsoft.CodeAnalysis;
using JetBrains.Annotations;

namespace YetAnotherXamlPad
{
    internal struct ViewModelAssemblyData
    {
        public ViewModelAssemblyData([NotNull] SyntaxTree syntaxTree, [NotNull] string assemblyName)
            : this()
        {
            SyntaxTree = syntaxTree;
            AssemblyName = assemblyName;
        }

        [NotNull]
        public SyntaxTree SyntaxTree { get; }

        [NotNull]
        public string AssemblyName { get; }
    }
}
