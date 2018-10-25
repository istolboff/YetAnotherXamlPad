using System;
using System.ComponentModel;
using ICSharpCode.AvalonEdit.Document;

namespace YetAnotherXamlPad
{
    public sealed class MainWindowViewModel : INotifyPropertyChanged
    {
        public MainWindowViewModel()
        {
            XamlCodeDocument = new TextDocument(
@"<Page
    xmlns = ""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:sys = ""clr-namespace:System;assembly=mscorlib""
    xmlns:x = ""http://schemas.microsoft.com/winfx/2006/xaml"">
</Page>");
        }

        public TextDocument XamlCodeDocument
        {
            get => _xamlCodeDocument;
            set
            {
                if (ReferenceEquals(_xamlCodeDocument, value))
                {
                    return;
                }

                if (_xamlCodeDocument != null)
                {
                    _xamlCodeDocument.TextChanged -= OnXamlCodeChanged;
                }

                _xamlCodeDocument = value;

                if (_xamlCodeDocument != null)
                {
                    _xamlCodeDocument.TextChanged += OnXamlCodeChanged;
                }

                PropertyChanged(this, new PropertyChangedEventArgs(nameof(XamlCodeDocument)));
                TryRenderXaml(_xamlCodeDocument.Text);
            }
        }

        public static readonly MainWindowViewModel Instance = new MainWindowViewModel();

        public event PropertyChangedEventHandler PropertyChanged = Do.Nothing;

        private void OnXamlCodeChanged(object sender, EventArgs e)
        {
            TryRenderXaml(((TextDocument)sender).Text);
        }

        private void TryRenderXaml(string xamlCode)
        {
            System.Diagnostics.Trace.WriteLine(xamlCode);
        }

        private TextDocument _xamlCodeDocument;
    }
}
