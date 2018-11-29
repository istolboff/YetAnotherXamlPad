using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis;

namespace YetAnotherXamlPad
{
    [Serializable]
    internal sealed class AssemblyBuildException : InvalidOperationException
    {
        public AssemblyBuildException(IReadOnlyCollection<Diagnostic> buildErrors)
            : base(string.Join(Environment.NewLine, buildErrors.Select(error => error.GetMessage())))
        {
            _buildErrors = buildErrors.Select(error => new SourceCodeError(error.ToString(), error.Location.GetLineSpan().Span)).ToArray();
        }

        private AssemblyBuildException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _buildErrors = (SourceCodeError[])info.GetValue(nameof(_buildErrors), typeof(SourceCodeError[]));
        }

        public IReadOnlyCollection<SourceCodeError> BuildErrors => _buildErrors;

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(_buildErrors), _buildErrors, typeof(SourceCodeError[]));
        }

        private readonly SourceCodeError[] _buildErrors;
    }
}
