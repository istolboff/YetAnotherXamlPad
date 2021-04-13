using System;
using System.Reactive.Linq;
using JetBrains.Annotations;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using YetAnotherXamlPad.TextMarking;

namespace YetAnotherXamlPad
{
    internal sealed class CodeEditor
    {
        public CodeEditor([NotNull] TextEditor textEditor)
        {
            _textDocument = textEditor.Document;
            TextMarkerService = ErrorTextMarking.RegisterErrorTextMarkerService(textEditor);
        }

        public string Text
        {
            get => _textDocument.Text;
            set => _textDocument.Text = value;
        }

        public ErrorTextMarkerService TextMarkerService { get; }

        public IObservable<string> CreateTextChangeObservable()
        {
            return Observable
                    .FromEventPattern(
                        h => _textDocument.TextChanged += h,
                        h => _textDocument.TextChanged -= h)
                    .Select(_ => _textDocument.Text);
        }

        private readonly TextDocument _textDocument;
    }
}