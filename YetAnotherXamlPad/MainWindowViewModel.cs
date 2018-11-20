using System;
using System.ComponentModel;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml;
using ICSharpCode.AvalonEdit.Document;
using static YetAnotherXamlPad.Do;
using static YetAnotherXamlPad.Either;
using static YetAnotherXamlPad.ViewModelAssemblyBuilder;

namespace YetAnotherXamlPad
{
    internal sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        public MainWindowViewModel(bool useViewModels)
        {
            _useViewModels = useViewModels;
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
                _xamlCodeChanges = value != null ? CreateTextChangeObservable(value) : null;
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
                _viewModelCodeChanges = value != null ? CreateTextChangeObservable(value) : null;
                ResubscribeToEditorsChanges();

                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ViewModelCodeDocument)));
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

            if (_xamlCodeChanges == null || _viewModelCodeChanges == null)
            {
                return;
            }

            var dispatcher = Dispatcher.CurrentDispatcher;

            var compositeDisposable = new CompositeDisposable(
                    // XAML code changes
                    _xamlCodeChanges.Throttle(TimeSpan.FromMilliseconds(500)).Subscribe(_ => TryRenderXaml(dispatcher)));

            if (UseViewModels)
            {
                compositeDisposable.Add(
                    // ViewModel code changes
                    (from viewModelCode in _viewModelCodeChanges.Throttle(TimeSpan.FromMilliseconds(1500))
                     let xamlCode = dispatcher.Invoke(() => _xamlCodeDocument.Text)
                     select Try(() => Left(TryParseViewModelCode(viewModelCode: viewModelCode, xamlCode: xamlCode))).Catch(Right))
                    .Subscribe(
                        viewModelAssemblyDataOrException => TryUseUpdatedViewModelCode(viewModelAssemblyDataOrException, ChangeSource.Csharp, dispatcher),
                        exception => ReportError(exception, dispatcher)));
            }

            _editorsChangeSubscription = compositeDisposable;

            TryRenderXaml(dispatcher);
        }

        private void TryRenderXaml(Dispatcher dispatcher)
        {
            if (dispatcher.Invoke(() => _useViewModels))
            {
                if (!GuiRunner.ViewModelAssemblyAlreadyLoaded)
                {
                    var (xamlCode, viewModelCode) = dispatcher.Invoke(() => (_xamlCodeDocument.Text, _viewModelCodeDocument.Text));
                    var viewModelAssemblyDataOrException = Try(() => Left(TryParseViewModelCode(viewModelCode: viewModelCode, xamlCode: xamlCode))).Catch(Right);
                    TryUseUpdatedViewModelCode(viewModelAssemblyDataOrException, ChangeSource.Xaml, dispatcher);
                    return;
                }
            }

            TryRenderXamlCore(dispatcher);
        }

        private void TryRenderXamlCore(Dispatcher dispatcher)
        {
            dispatcher.Invoke(() =>
                ExecuteAndReportErrors(() =>
                {
                    using (var stringReader = new StringReader(_xamlCodeDocument.Text))
                    using (var xmlreader = new XmlTextReader(stringReader))
                    {
                        ParsedXaml = XamlReader.Load(xmlreader) as FrameworkElement;
                    }
                }));
        }

        private void TryUseUpdatedViewModelCode(
            Either<ViewModelAssemblyData?, Exception> viewModelAssemblyDataOrException,
            ChangeSource changeSource,
            Dispatcher dispatcher)
        {
            viewModelAssemblyDataOrException.Fold(
                viewModelAssemblyData =>
                {
                    if (!GuiRunner.ViewModelAssemblyAlreadyLoaded)
                    {
                        var viewModelAssembly = BuildViewModelAssembly(viewModelAssemblyData);
                        GuiRunner.ReloadViewModelAssembly(viewModelAssembly);
                        if (viewModelAssembly != null)
                        {
                            viewModelAssembly.Value.Fold(
                                _ => TryRenderXamlCore(dispatcher),
                                exception => ReportError(exception, dispatcher));
                        }
                        else
                        {
                            if (changeSource == ChangeSource.Xaml)
                            {
                                TryRenderXamlCore(dispatcher);
                            }
                        }
                    }
                    else
                    {
                        dispatcher.Invoke(() => RequestGuiSessionRestart(viewModelAssemblyData));
                    }
                },
                exception => ReportError(exception, dispatcher));
        }

        private void RequestGuiSessionRestart(ViewModelAssemblyData? viewModelAssemblyData = default)
        {
            GuiRunner.RequestGuiSessionRestart(
                useViewModels: _useViewModels, 
                xamlCode: _xamlCodeDocument.Text, 
                viewModelCode: _viewModelCodeDocument.Text,
                viewModelAssemblyData: viewModelAssemblyData);
        }

        private void ExecuteAndReportErrors(Action action)
        {
            Try(() => 
            {
                action();
                ClearError();
            })
            .Catch(ReportError);
        }

        private void ClearError()
        {
            Errors = null;
            ErrorTabColor = null;
        }

        private void ReportError(Exception exception, Dispatcher dispatcher)
        {
            dispatcher.Invoke(() => ReportError(exception));
        }

        private void ReportError(Exception exception)
        {
            Errors = exception.ToString();
            ErrorTabColor = "Red";
            ParsedXaml = null;
        }

        private static IObservable<string> CreateTextChangeObservable(TextDocument textDocument)
        {
            return Observable
                    .FromEventPattern(
                        h => textDocument.TextChanged += h,
                        h => textDocument.TextChanged -= h)
                    .Select(_ => textDocument.Text);
        }

        private FrameworkElement _parsedXaml;
        private TextDocument _xamlCodeDocument;
        private bool _useViewModels;
        private TextDocument _viewModelCodeDocument;
        private IObservable<string> _xamlCodeChanges;
        private IObservable<string> _viewModelCodeChanges;
        private string _errors;
        private string _errorTabColor;
        private IDisposable _editorsChangeSubscription = Disposable.Empty;

        private enum ChangeSource { Xaml, Csharp }
    }
}