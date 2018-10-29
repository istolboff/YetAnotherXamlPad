using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Xml;
using ICSharpCode.AvalonEdit.Document;

namespace YetAnotherXamlPad
{
    internal sealed class MainWindowViewModel : INotifyPropertyChanged
    {
        public MainWindowViewModel(bool useViewModels)
        {
            _useViewModels = useViewModels;
        }

        public string Title => AppDomain.CurrentDomain.FriendlyName;

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

                if (_xamlCodeDocument != null)
                {
                    TryRenderXaml(_xamlCodeDocument.Text);
                }
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

        public bool UseViewModels
        {
            get => _useViewModels;
            set
            {
                if (_useViewModels == value)
                {
                    return;
                }

                _useViewModels = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(UseViewModels)));
                GuiRunner.RequestGuiSessionRestart(useViewModels: value, xamlCode: _xamlCodeDocument.Text, viewModelCode: _viewModelCode.Text);
            }
        }

        public TextDocument ViewModelCodeDocument
        {
            get => _viewModelCode;
            set
            {
                if (ReferenceEquals(_viewModelCode, value))
                {
                    return;
                }

                if (_viewModelCode != null)
                {
                    _viewModelCode.TextChanged -= OnViewModelCodeChanged;
                }

                _viewModelCode = value;

                if (_viewModelCode != null)
                {
                    _viewModelCode.TextChanged += OnViewModelCodeChanged;
                }

                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ViewModelCodeDocument)));

                if (UseViewModels && _viewModelCode != null)
                {
                    TryBuildCSharpCode(_viewModelCode.Text);
                }
            }
        }

        public string Errors
        {
            get => _errors;
            set
            {
                if (_errors == value)
                {
                    return;
                }

                _errors = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Errors)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = Do.Nothing;

        private void OnXamlCodeChanged(object sender, EventArgs e)
        {
            TryRenderXaml(((TextDocument)sender).Text);
        }

        private void OnViewModelCodeChanged(object sender, EventArgs e)
        {
            TryBuildCSharpCode(((TextDocument)sender).Text);
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

                    Errors = null;
                }
                catch (Exception e)
                {
                    Errors = e.ToString();
                }
            }
        }

        private void TryBuildCSharpCode(string csharpCode)
        {
        }

        private TextDocument _xamlCodeDocument;
        private FrameworkElement _parsedXaml;
        private string _errors;
        private TextDocument _viewModelCode;
        private bool _useViewModels;
    }
}
