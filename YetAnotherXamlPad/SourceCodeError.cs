using System;
using Microsoft.CodeAnalysis.Text;
using JetBrains.Annotations;

namespace YetAnotherXamlPad
{
    [Serializable]
    internal sealed class SourceCodeError
    {
        private SourceCodeError()
        {
        }

        public SourceCodeError(string message, LinePositionSpan? location = default)
        {
            _message = message;
            _hasLocation = location.HasValue;
            if (location.HasValue)
            {
                _startLine = location.Value.Start.Line;
                _startCharacter = location.Value.Start.Character;
                _endLine = location.Value.End.Line;
                _endCharacter = location.Value.End.Character;
            }
        }

        [UsedImplicitly]
        public string Message => _message;

        public LinePositionSpan? Location =>
            _hasLocation 
                ? new LinePositionSpan(new LinePosition(_startLine, _startCharacter), new LinePosition(_endLine, _endCharacter)) 
                : default(LinePositionSpan?);

        public override string ToString()
        {
            return Message;
        }

#pragma warning disable IDE0044 // Add readonly modifier
// ReSharper disable FieldCanBeMadeReadOnly.Local
        private string _message;
        private bool _hasLocation;
        private int _startLine;
        private int _startCharacter;
        private int _endLine;
        private int _endCharacter;
// ReSharper enable FieldCanBeMadeReadOnly.Local
#pragma warning restore IDE0044 // Add readonly modifier
    }
}
