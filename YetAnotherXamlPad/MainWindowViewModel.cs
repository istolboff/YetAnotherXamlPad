using System;
using System.ComponentModel;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using JetBrains.Annotations;
using YetAnotherXamlPad.EditorState;
using YetAnotherXamlPad.Utilities;
using static System.Windows.Markup.XamlReader;
using static YetAnotherXamlPad.Utilities.Do;
using static YetAnotherXamlPad.ParsedViewModelCode;
using static YetAnotherXamlPad.ViewModelAssemblyBuilder;

namespace YetAnotherXamlPad
{
    internal sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        public MainWindowViewModel(
            [NotNull] CodeEditor xamlCodeEditor,
            [NotNull] CodeEditor viewModelCodeEditor)
        {
            _xamlCodeEditor = xamlCodeEditor;
            _viewModelCodeEditor = viewModelCodeEditor;

            Errors = new ErrorsViewModel(
                xamlTextMarkerService: xamlCodeEditor.TextMarkerService,
                viewModelTextMarkerService: viewModelCodeEditor.TextMarkerService);

            var (editorState, startupError) = GuiRunner.StartupInfo;
            _useViewModels = editorState.UseViewModel;
            _xamlCodeEditor.Text = editorState.XamlCode;
            _viewModelCodeEditor.Text = editorState.ViewModelCode;
            ReportBindingErrors = editorState.ReportBindingErrors;
            ApplyViewModelChangesImmediately = editorState.ApplyViewModelChangesImmediately;

            _editorsChangeSubscription = ScribeToEditorsChanges();

            Dispatcher.CurrentDispatcher.Post(() =>
            {
                if (startupError == null)
                {
                    RenderXaml(editorState.XamlCode);
                }
                else
                {
                    ReportViewModelError(startupError);
                }
            });
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

        public bool ApplyViewModelChangesImmediately
        {
            get => _applyViewModelChangesImmediately;
            set
            {
                if (_applyViewModelChangesImmediately == value)
                {
                    return;
                }

                _applyViewModelChangesImmediately = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ApplyViewModelChangesImmediately)));
            }
        }

        public bool ReportBindingErrors
        {
            get => _reportBindingErrors;
            set
            {
                if (_reportBindingErrors == value)
                {
                    return;
                }

                _reportBindingErrors = value;
                if (value)
                {
                    BindingErrorsReporting.ErrorOccured += ReportXamlError;
                }
                else
                {
                    BindingErrorsReporting.ErrorOccured -= ReportXamlError;
                }

                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ReportBindingErrors)));
                RenderXaml(_xamlCodeEditor.Text);
            }
        }

        public ErrorsViewModel Errors { get; }

        public WindowState WindowState { get; set; }
        public double WindowLeft { get; set; }
        public double WindowTop { get; set; }
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }

        public void Reload(EditorStateDto editorState)
        {
            _useViewModels = editorState.UseViewModel;
            _xamlCodeEditor.Text = editorState.XamlCode;
            _viewModelCodeEditor.Text = editorState.ViewModelCode;
            _reportBindingErrors = editorState.ReportBindingErrors;
            _applyViewModelChangesImmediately = editorState.ApplyViewModelChangesImmediately;
            PropertyChanged(this, new PropertyChangedEventArgs(null));
        }

        public void Dispose()
        {
            _editorsChangeSubscription.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged = Nothing;

        private EditorStateDto CurrentState => 
            new EditorStateDto
            {
                UseViewModel = _useViewModels,
                XamlCode = _xamlCodeEditor.Text,
                ViewModelCode = _viewModelCodeEditor.Text,
                ReportBindingErrors = _reportBindingErrors,
                ApplyViewModelChangesImmediately = _applyViewModelChangesImmediately,
                MainWindowPosition = new WindowPosition
                                        {
                                            Location = new Rect(WindowLeft, WindowTop, WindowWidth, WindowHeight),
                                            WindowState = WindowState
                                        }
            };

        private IDisposable ScribeToEditorsChanges()
        {
            var xamlCodeChanges = _xamlCodeEditor.CreateTextChangeObservable();

            var dispatcher = Dispatcher.CurrentDispatcher;

            if (!UseViewModels)
            {
                return xamlCodeChanges
                    .Throttle(XamlChangeThrottlingInterval)
                    .Subscribe(xamlCode => dispatcher.Post(() => RenderXaml(xamlCode)));
            }

            var viewModelCodeChanges = _viewModelCodeEditor
                .CreateTextChangeObservable()
                .Where(_ => ApplyViewModelChangesImmediately);

            var rawXamlChanges = xamlCodeChanges
                .Throttle(XamlChangeThrottlingInterval)
                .Select(xamlCode => new XamlCode(xamlCode));

            var viewModelChangesBecameImmediatelyApplicable =
                Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    h => PropertyChanged += h,
                    h => PropertyChanged -= h)
                    .Where(e => e.EventArgs.PropertyName == nameof(ApplyViewModelChangesImmediately) &&
                                ApplyViewModelChangesImmediately);

            var rawViewModelChanges = viewModelCodeChanges
                .Throttle(CsharpChangeThrottlingInterval)
                .Merge(
                    viewModelChangesBecameImmediatelyApplicable
                        .Select(_ => _viewModelCodeEditor.Text)
                        .Throttle(TimeSpan.FromMilliseconds(50)))
                .Select(TryParseViewModelCode);

            var codeChangesListener = new CodeChangesListener(
                xamlCode: new XamlCode(_xamlCodeEditor.Text), 
                viewModelCode: _viewModelCodeEditor.Text, 
                target: this, 
                dispatcher: dispatcher);

            return new CompositeDisposable(
                rawXamlChanges.Subscribe(xamlCode => codeChangesListener.XamlChanged(xamlCode)),
                rawViewModelChanges.Subscribe(viewModelChange => codeChangesListener.ViewModelChanged(viewModelChange)));
        }

        private void RenderXaml(string xamlCode)
        {
            Try(() =>
            {
                using (var stringReader = new StringReader(xamlCode))
                using (var xmlreader = new XmlTextReader(stringReader))
                {
                    ParsedXaml = Load(xmlreader) as FrameworkElement;
                }

                Errors.ClearErrors(); 
            })
            .Catch(ReportXamlError);
        }

        private void RequestGuiSessionRestart(ViewModelAssemblyBuilder assemblyBuilder = null)
        {
            GuiRunner.RequestGuiSessionRestart(CurrentState, assemblyBuilder);
        }

        private void ReportXamlError(Exception exception)
        {
            Errors.ReportXamlError(exception);
            ParsedXaml = null;
        }

        private void ReportViewModelError(Exception exception)
        {
            Errors.ReportViewModelErrors(exception);
            ParsedXaml = null;
        }

        private readonly CodeEditor _xamlCodeEditor;
        private readonly CodeEditor _viewModelCodeEditor;
        private readonly IDisposable _editorsChangeSubscription;

        private bool _useViewModels;
        private bool _applyViewModelChangesImmediately;
        private bool _reportBindingErrors;
        private FrameworkElement _parsedXaml;

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
                                exception => _target.ReportViewModelError(exception),
                                _target.RequestGuiSessionRestart));
                    }
                }
            }

            private void BuildAndUseNewAssembly(Either<Exception, ViewModelAssemblyBuilder> assemblyBuilderOrException)
            {
                assemblyBuilderOrException
                    .FlatMap(assemblyBuilder => assemblyBuilder.Build())
                    .Fold(
                        exception => _dispatcher.Post(() => _target.ReportViewModelError(exception)),
                        assemblyData =>
                            {
                                GuiRunner.SetViewModelAssembly(assemblyData);
                                RenderXaml();
                            });
            }

            private void RenderXaml()
            {
                _dispatcher.Post(() => _target.RenderXaml(_xamlCode.Text));
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
    }
}