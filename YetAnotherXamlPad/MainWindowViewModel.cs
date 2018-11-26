using System;
using System.ComponentModel;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml;
using JetBrains.Annotations;
using ICSharpCode.AvalonEdit.Document;
using static YetAnotherXamlPad.Do;
using static YetAnotherXamlPad.ParsedViewModelCode;
using static YetAnotherXamlPad.ViewModelAssemblyBuilder;

namespace YetAnotherXamlPad
{
    internal sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        public MainWindowViewModel(bool useViewModels)
        {
            _useViewModels = useViewModels;

            Errors = new ErrorsViewModel();

            if (GuiRunner.StartupError != null)
            {
                ReportError(GuiRunner.StartupError, ErrorSource.ViewModel);
            }
        }

        public string Title => AppDomain.CurrentDomain.FriendlyName;

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

        public TextDocument XamlCodeDocument
        {
            get => _xamlCodeDocument;
            set
            {
                if (ReferenceEquals(_xamlCodeDocument, value))
                {
                    return;
                }

                _xamlCodeDocument = value;
                ResubscribeToEditorsChanges();

                PropertyChanged(this, new PropertyChangedEventArgs(nameof(XamlCodeDocument)));
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
                RequestGuiSessionRestart();
            }
        }

        public TextDocument ViewModelCodeDocument
        {
            get => _viewModelCodeDocument;
            set
            {
                if (ReferenceEquals(_viewModelCodeDocument, value))
                {
                    return;
                }

                _viewModelCodeDocument = value;
                ResubscribeToEditorsChanges();

                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ViewModelCodeDocument)));
            }
        }

        public ErrorsViewModel Errors { get; }

        public string ErrorTabColor
        {
            get => _errorTabColor;
            set
            {
                if (_errorTabColor == value)
                {
                    return;
                }

                _errorTabColor = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ErrorTabColor)));
            }
        }

        public void Dispose()
        {
            _editorsChangeSubscription.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged = Nothing;

        private void ResubscribeToEditorsChanges()
        {
            _editorsChangeSubscription.Dispose();

            if (_xamlCodeDocument == null || _viewModelCodeDocument == null)
            {
                return;
            }

            var xamlCodeChanges = CreateTextChangeObservable(_xamlCodeDocument);

            var dispatcher = Dispatcher.CurrentDispatcher;

            if (!UseViewModels)
            {
                _editorsChangeSubscription = xamlCodeChanges
                    .Throttle(XamlChangeThrottlingInterval)
                    .Subscribe(xamlCode => dispatcher.Post(() => RenderXaml(xamlCode)));
            }
            else
            {
                var viewModelCodeChanges = CreateTextChangeObservable(_viewModelCodeDocument);

                var rawXamlChanges = xamlCodeChanges
                    .Throttle(XamlChangeThrottlingInterval)
                    .Select(xamlCode => new XamlCode(xamlCode));

                var rawViewModelChanges = viewModelCodeChanges
                    .Throttle(CsharpChangeThrottlingInterval)
                    .Select(TryParseViewModelCode);

                var codeChangesListener = new CodeChangesListener(
                    xamlCode: new XamlCode(_xamlCodeDocument.Text), 
                    viewModelCode: _viewModelCodeDocument.Text, 
                    target: this, 
                    dispatcher: dispatcher);

                _editorsChangeSubscription = new CompositeDisposable(
                    rawXamlChanges.Subscribe(xamlCode => codeChangesListener.XamlChanged(xamlCode)),
                    rawViewModelChanges.Subscribe(viewModelChange => codeChangesListener.ViewModelChanged(viewModelChange)));
            }

            RenderXaml(_xamlCodeDocument.Text);
        }

        private void RenderXaml(string xamlCode)
        {
            Try(() =>
            {
                using (var stringReader = new StringReader(xamlCode))
                using (var xmlreader = new XmlTextReader(stringReader))
                {
                    ParsedXaml = XamlReader.Load(xmlreader) as FrameworkElement;
                }

                ClearError();
            })
            .Catch(exception => ReportError(exception, ErrorSource.Xaml));
        }

        private void RequestGuiSessionRestart(ViewModelAssemblyBuilder assemblyBuilder = null)
        {
            GuiRunner.RequestGuiSessionRestart(
                useViewModels: _useViewModels, 
                xamlCode: _xamlCodeDocument.Text, 
                viewModelCode: _viewModelCodeDocument.Text,
                assemblyBuilder: assemblyBuilder);
        }

        private void ClearError()
        {
            Errors.ClearErrors();
            ErrorTabColor = null;
        }

        private void ReportError(Exception exception, ErrorSource errorSource)
        {
            switch (errorSource)
            {
                case ErrorSource.Xaml:
                    Errors.XamlError = exception.ToString();
                    break;

                case ErrorSource.ViewModel:
                    Errors.ViewModelError = exception.ToString();
                    break;

                default:
                    throw new ArgumentException("errorSource");
            }

            ErrorTabColor = "Red";
            ParsedXaml = null;
        }

        private static IObservable<string> CreateTextChangeObservable([NotNull] TextDocument textDocument)
        {
            return Observable
                    .FromEventPattern(
                        h => textDocument.TextChanged += h,
                        h => textDocument.TextChanged -= h)
                    .Select(_ => textDocument.Text);
        }

        private FrameworkElement _parsedXaml;
        private bool _useViewModels;
        private TextDocument _xamlCodeDocument;
        private TextDocument _viewModelCodeDocument;
        private string _errorTabColor;
        private IDisposable _editorsChangeSubscription = Disposable.Empty;

        private static readonly TimeSpan XamlChangeThrottlingInterval = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan CsharpChangeThrottlingInterval = TimeSpan.FromMilliseconds(1500);

        private class CodeChangesListener
        {
            public CodeChangesListener(XamlCode xamlCode, string viewModelCode, MainWindowViewModel target, Dispatcher dispatcher)
            {
                _target = target;
                _dispatcher = dispatcher;
                _xamlCode = xamlCode;
                _viewModelCode = TryParseViewModelCode(viewModelCode);
            }

            public void XamlChanged(XamlCode xamlCode)
            {
                lock (_locker)
                {
                    var mentionedAssembliesChanged = _xamlCode.MentionedAssembliesDiffer(xamlCode);
                    _xamlCode = xamlCode;

                    if (!mentionedAssembliesChanged)
                    {
                        RenderXaml();
                        return;
                    }

                    var assemblyBuilderOrException = TryCreateAssemblyBuilder(_xamlCode, _viewModelCode);
                    if (!CanViewModelAssemblyBeLoadedInCurrentAppDomain(assemblyBuilderOrException))
                    {
                        RenderXaml();
                        return;
                    }

                    BuildAndUseNewAssembly(assemblyBuilderOrException.Value);
                }
            }

            public void ViewModelChanged(ParsedViewModelCode? viewModelCode)
            {
                lock (_locker)
                {
                    if (viewModelCode == null)
                    {
                        if (_viewModelCode != null && 
                            !GuiRunner.CanViewModelAssemblyBeLoadedInCurrentAppDomain(
                                TryCreateAssemblyBuilder(_xamlCode, _viewModelCode)?.GetOrElse()?.AssemblyName))
                        { 
                            _dispatcher.Post(() => _target.RequestGuiSessionRestart());
                        }

                        _viewModelCode = null;
                        return;
                    }

                    _viewModelCode = viewModelCode;

                    var assemblyBuilderOrException = TryCreateAssemblyBuilder(_xamlCode, _viewModelCode);

                    if (assemblyBuilderOrException == null)
                    {
                        return;
                    }

                    if (CanViewModelAssemblyBeLoadedInCurrentAppDomain(assemblyBuilderOrException))
                    {
                        BuildAndUseNewAssembly(assemblyBuilderOrException.Value);
                    }
                    else
                    {
                        _dispatcher.Post(() =>
                            assemblyBuilderOrException.Value.Fold(
                                exception => _target.ReportError(exception, ErrorSource.ViewModel),
                                _target.RequestGuiSessionRestart));
                    }
                }
            }

            private void BuildAndUseNewAssembly(Either<Exception, ViewModelAssemblyBuilder> assemblyBuilderOrException)
            {
                assemblyBuilderOrException.Fold(
                    exception => ReportError(exception, ErrorSource.ViewModel),
                    assemblyBuilder =>
                    {
                        assemblyBuilder.Build().Fold(
                            exception => ReportError(exception, ErrorSource.ViewModel),
                            assemblyData =>
                            {
                                GuiRunner.SetViewModelAssembly(assemblyData);
                                RenderXaml();
                            });
                    });
            }

            private void RenderXaml()
            {
                _dispatcher.Post(() => _target.RenderXaml(_xamlCode.Text));
            }

            private void ReportError(Exception exception, ErrorSource errorSource)
            {
                _dispatcher.Post(() => _target.ReportError(exception, errorSource));
            }

            private static bool CanViewModelAssemblyBeLoadedInCurrentAppDomain(
                Either<Exception, ViewModelAssemblyBuilder>? assemblyBuilderOrException)
            {
                return GuiRunner.CanViewModelAssemblyBeLoadedInCurrentAppDomain(
                    assemblyBuilderOrException?.GetOrElse()?.AssemblyName);
            }

            private readonly MainWindowViewModel _target;
            private readonly Dispatcher _dispatcher;
            private readonly object _locker = new object();

            private XamlCode _xamlCode;
            private ParsedViewModelCode? _viewModelCode;
        }

        private enum ErrorSource { Xaml, ViewModel }
    }
}