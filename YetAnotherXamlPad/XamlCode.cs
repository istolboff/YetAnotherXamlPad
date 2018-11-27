using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using YetAnotherXamlPad.Utilities;
using static YetAnotherXamlPad.Utilities.Do;
using AssemblyName = System.String;

namespace YetAnotherXamlPad
{
    internal struct XamlCode
    {
        public XamlCode(string text)
        {
            Text = text;
            MentionedAssemblies = GetAssembliesMentionedInXaml(text);
        }

        public readonly string Text;

        public readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> MentionedAssemblies;

        public bool MentionedAssembliesDiffer(XamlCode xamlCode)
        {
            return !MentionedAssemblies.OrderBy(kvp => kvp.Key)
                    .SequenceEqual(xamlCode.MentionedAssemblies.OrderBy(kvp => kvp.Key));
        }

        private static IReadOnlyDictionary<AssemblyName, IReadOnlyCollection<string>> GetAssembliesMentionedInXaml(string xamlCode)
        {
            if (string.IsNullOrEmpty(xamlCode))
            {
                return new Dictionary<AssemblyName, IReadOnlyCollection<string>>();
            }

            return Try(() =>
            {
                var xamlMarkupRoot = XDocument.Parse(xamlCode).Root;
                if (xamlMarkupRoot == null)
                {
                    return new Dictionary<AssemblyName, IReadOnlyCollection<string>>();
                }

                return xamlMarkupRoot
                    .Attributes()
                    .Where(a => a.IsNamespaceDeclaration)
                    .GroupBy(
                        a => a.Name.Namespace == XNamespace.None ? string.Empty : a.Name.LocalName,
                        a => XNamespace.Get(a.Value))
                    .ToDictionary(g => g.Key, g => g.First())
                    .Values
                    .Select(url => TryToExtractNamespaceAndAssemblyNames(url.ToString()))
                    .Where(names => !string.IsNullOrWhiteSpace(names.assemblyName))
                    .GroupBy(names => names.assemblyName)
                    .ToDictionary(g => g.Key, g => g.Select(it => it.clrNamespaceName).AsImmutable());
            })
            .Catch(_ => new Dictionary<AssemblyName, IReadOnlyCollection<string>>());
        }

        private static (string clrNamespaceName, string assemblyName) TryToExtractNamespaceAndAssemblyNames(string namespaceDefinition)
        {
            // "clr-namespace:{clrNamespaceName};assembly={assemblyName}";
            const string clrNamespacePrefix = "clr-namespace:";
            if (!(namespaceDefinition?.StartsWith(clrNamespacePrefix) ?? false))
            {
                return default;
            }

            const string assemblyNameMarker = ";assembly=";
            var assemblyNameMarkerStart = namespaceDefinition.IndexOf(assemblyNameMarker, clrNamespacePrefix.Length + 1, StringComparison.Ordinal);
            if (assemblyNameMarkerStart < 0)
            {
                return default;
            }

            var clrNamespaceName = namespaceDefinition.Substring(clrNamespacePrefix.Length, assemblyNameMarkerStart - clrNamespacePrefix.Length);
            var assemblyName = namespaceDefinition.Substring(assemblyNameMarkerStart + assemblyNameMarker.Length);
            return (clrNamespaceName, assemblyName);
        }
    }
}
