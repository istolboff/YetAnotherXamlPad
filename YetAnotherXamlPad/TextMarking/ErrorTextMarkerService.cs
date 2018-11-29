using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xaml;
using System.Windows;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Text;
using JetBrains.Annotations;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace YetAnotherXamlPad.TextMarking
{
    //
    // The source code for this class was taken from https://github.com/siegfriedpammer/AvalonEditSamples
    //
    internal sealed class ErrorTextMarkerService : DocumentColorizingTransformer, IBackgroundRenderer, ITextViewConnect
    {
        public ErrorTextMarkerService([NotNull] TextEditor textEditor)
        {
            _textEditor = textEditor;
            _markers = new TextSegmentCollection<TextMarker>(Document);
        }

        public SourceCodeError GetSourceCodeError(XamlException exception)
        {
            return new SourceCodeError(
                exception.Message, 
                TryGetSingleLineLocation(lineNumber: exception.LineNumber - 1, positionInLine: exception.LinePosition - 1, text: Document.Text));
        }

        public SourceCodeError GetSourceCodeError(System.Windows.Markup.XamlParseException exception)
        {
            return new SourceCodeError(
                exception.Message, 
                TryGetSingleLineLocation(lineNumber: exception.LineNumber - 1, positionInLine: exception.LinePosition - 1, text: Document.Text));
        }

        public void MarkError(LinePositionSpan errorSpan)
        {
            var textMarker = Create(errorSpan);
            textMarker.MarkerTypes = TextMarkerTypes.SquigglyUnderline;
            textMarker.MarkerColor = Colors.Red;
        }

        public void SetCaretToErrorStart(LinePosition errorStart)
        {
            _textEditor.Focus();
            _textEditor.TextArea.Caret.Offset = GetOffset(errorStart);
            _textEditor.TextArea.Caret.BringCaretToView();
        }

        public void RemoveAllErrorMarkers()
        {
            foreach (var m in _markers.ToArray())
            {
                if (_markers.Remove(m))
                {
                    Redraw(m);
                }
            }
        }

        KnownLayer IBackgroundRenderer.Layer => KnownLayer.Selection;

        void IBackgroundRenderer.Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView == null)
                throw new ArgumentNullException(nameof(textView));

            if (drawingContext == null)
                throw new ArgumentNullException(nameof(drawingContext));

            if (!textView.VisualLinesValid)
                return;

            var visualLines = textView.VisualLines;
            if (visualLines.Count == 0)
                return;

            int viewStart = visualLines.First().FirstDocumentLine.Offset;
            int viewEnd = visualLines.Last().LastDocumentLine.EndOffset;
            foreach (TextMarker marker in _markers.FindOverlappingSegments(viewStart, viewEnd - viewStart))
            {
                var underlineMarkerTypes = TextMarkerTypes.SquigglyUnderline;
                if ((marker.MarkerTypes & underlineMarkerTypes) != 0)
                {
                    foreach (var r in BackgroundGeometryBuilder.GetRectsForSegment(textView, marker))
                    {
                        Point startPoint = r.BottomLeft;
                        Point endPoint = r.BottomRight;

                        Brush usedBrush = new SolidColorBrush(marker.MarkerColor);
                        usedBrush.Freeze();
                        if ((marker.MarkerTypes & TextMarkerTypes.SquigglyUnderline) != 0)
                        {
                            double offset = 2.5;

                            int count = Math.Max((int)((endPoint.X - startPoint.X) / offset) + 1, 4);

                            StreamGeometry geometry = new StreamGeometry();

                            using (StreamGeometryContext ctx = geometry.Open())
                            {
                                ctx.BeginFigure(startPoint, false, false);
                                ctx.PolyLineTo(CreatePoints(startPoint, offset, count).ToArray(), true, false);
                            }

                            geometry.Freeze();

                            Pen usedPen = new Pen(usedBrush, 1);
                            usedPen.Freeze();
                            drawingContext.DrawGeometry(Brushes.Transparent, usedPen, geometry);
                        }
                    }
                }
            }
        }

        void ITextViewConnect.AddToTextView(TextView textView)
        {
            if (textView != null && !_textViews.Contains(textView))
            {
                Debug.Assert(textView.Document == Document);
                _textViews.Add(textView);
            }
        }

        void ITextViewConnect.RemoveFromTextView(TextView textView)
        {
            if (textView != null)
            {
                Debug.Assert(textView.Document == Document);
                _textViews.Remove(textView);
            }
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStart = line.Offset;
            int lineEnd = lineStart + line.Length;
            foreach (TextMarker marker in _markers.FindOverlappingSegments(lineStart, line.Length))
            {
                ChangeLinePart(
                    Math.Max(marker.StartOffset, lineStart),
                    Math.Min(marker.EndOffset, lineEnd),
                    element => {
                        Typeface tf = element.TextRunProperties.Typeface;
                        element.TextRunProperties.SetTypeface(new Typeface(
                            tf.FontFamily,
                            tf.Style,
                            tf.Weight,
                            tf.Stretch
                        ));
                    }
                );
            }
        }

        private TextDocument Document => _textEditor.Document;

        private TextMarker Create(LinePositionSpan linePositionSpan)
        {
            var (startOffset, length) = ConvertToStartOffsetAndLength(linePositionSpan);
            var m = new TextMarker(this, startOffset, length);
            _markers.Add(m);
            return m;
        }

        private IEnumerable<Point> CreatePoints(Point start, double offset, int count)
        {
            for (int i = 0; i < count; i++)
                yield return new Point(start.X + i * offset, start.Y - ((i + 1) % 2 == 0 ? offset : 0));
        }

        private void Redraw(ISegment segment)
        {
            foreach (var view in _textViews)
            {
                view.Redraw(segment);
            }
        }

        private static LinePositionSpan? TryGetSingleLineLocation(int lineNumber, int positionInLine, string text)
        {
            var lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            return lines.Length <= lineNumber
                ? default(LinePositionSpan?)
                : new LinePositionSpan(new LinePosition(lineNumber, positionInLine), new LinePosition(lineNumber, lines[lineNumber].Length));
        }

        private (int startOffset, int length) ConvertToStartOffsetAndLength(LinePositionSpan @this)
        {
            var startOffset = GetOffset(@this.Start);
            return (startOffset, GetOffset(@this.End) - startOffset);
        }

        private int GetOffset(LinePosition linePosition)
        {
            var result = 0;
            for (var i = 0; i != linePosition.Line; ++i)
            {
                var currentLineEndOffset = Document.Text.IndexOf(Environment.NewLine, result, StringComparison.Ordinal);
                if (currentLineEndOffset < 0)
                {
                    break;
                }

                result = currentLineEndOffset + Environment.NewLine.Length;
            }

            return result + linePosition.Character;
        }

        private readonly TextSegmentCollection<TextMarker> _markers;
        private readonly List<TextView> _textViews = new List<TextView>();
        private readonly TextEditor _textEditor;

        private sealed class TextMarker : TextSegment
        {
            public TextMarker([NotNull] ErrorTextMarkerService service, int startOffset, int length)
            {
                _service = service;
                StartOffset = startOffset;
                Length = length;
                _markerTypes = TextMarkerTypes.None;
            }

            public TextMarkerTypes MarkerTypes
            {
                get => _markerTypes;
                set
                {
                    if (_markerTypes != value)
                    {
                        _markerTypes = value;
                        Redraw();
                    }
                }
            }

            public Color MarkerColor
            {
                get => _markerColor;
                set
                {
                    if (_markerColor != value)
                    {
                        _markerColor = value;
                        Redraw();
                    }
                }
            }

            private void Redraw()
            {
                _service.Redraw(this);
            }

            private readonly ErrorTextMarkerService _service;

            private TextMarkerTypes _markerTypes;
            private Color _markerColor;
        }

        [Flags]
        private enum TextMarkerTypes
        {
            None = 0x0000,
            SquigglyUnderline = 0x001,
        }
    }
}