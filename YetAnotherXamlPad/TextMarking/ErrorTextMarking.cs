using System.ComponentModel.Design;
using ICSharpCode.AvalonEdit;

namespace YetAnotherXamlPad.TextMarking
{
    internal static class ErrorTextMarking
    {
        public static ErrorTextMarkerService RegisterErrorTextMarkerService(TextEditor textEditor)
        {
            var textMarkerService = new ErrorTextMarkerService(textEditor);
            textEditor.TextArea.TextView.BackgroundRenderers.Add(textMarkerService);
            textEditor.TextArea.TextView.LineTransformers.Add(textMarkerService);
            var services = (IServiceContainer)textEditor.Document.ServiceProvider.GetService(typeof(IServiceContainer));
            services?.AddService(typeof(ErrorTextMarkerService), textMarkerService);
            return textMarkerService;
        }
    }
}
