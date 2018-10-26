using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Xml;
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

        public FrameworkElement ParsedXaml
        {
            get => _parsedXaml;
            set
            {
                if (ReferenceEquals(_parsedXaml, value))
                {
                    return;
                }

                _parsedXaml = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ParsedXaml)));
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
            {
                try
                {
                    using (var stringReader = new StringReader(xamlCode))
                    using (var xmlreader = new XmlTextReader(stringReader))
                    {
                        ParsedXaml = XamlReader.Load(xmlreader) as FrameworkElement;
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.WriteLine(e);
                }
            }
        }

        private TextDocument _xamlCodeDocument;
        private FrameworkElement _parsedXaml;
    }
}
