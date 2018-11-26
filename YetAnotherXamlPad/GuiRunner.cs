using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using ICSharpCode.AvalonEdit.Document;
using static YetAnotherXamlPad.Do;
using static YetAnotherXamlPad.Either;
using static YetAnotherXamlPad.ParsedViewModelCode;
using static YetAnotherXamlPad.ViewModelAssemblyBuilder;
using BuiltAssemblyOrException = YetAnotherXamlPad.Either<System.Exception, System.Collections.Generic.KeyValuePair<string, byte[]>>;

namespace YetAnotherXamlPad
{
    internal static class GuiRunner
    {
        public static MainWindowViewModel MainWindowViewModel => CreateMainWindowViewModel(GetState());

        public static Exception StartupError => GetState().StartupError;

        public static void Setup(AppDomain defaultAppDomain)
        {
            Debug.Assert(
                defaultAppDomain.IsDefaultAppDomain(),
                "Program logic error: GuiRunner.Setup() is not called from default AppDomain.");

            SetDomainData(
                defaultAppDomain,
                new DefaultDomain(CreateApplication()),
                new GuiRunnerState
                {
                    UseViewModels = false,
                    XamlCode = DefaultXamlCode,
                    ViewModelCode = DefaultViewModelCode,
                });
        }

        public static void RunGuiSession()
        {
            Debug.Assert(
                AppDomain.CurrentDomain.IsDefaultAppDomain(),
                "Program logic error: RunGuiSession() is not called from default AppDomain.");

            var guiRunnerState = GetState();
            guiRunnerState.FinishApplicationNow = true;
            guiRunnerState.StartupError = null;

            var defaultDomain = GetDefaultDomain();
            if (guiRunnerState.UseViewModels)
            {
                defaultDomain.Application.MainWindow?.Hide();
                RunDevotedAppDomain(defaultDomain, guiRunnerState);
            }
            else
            {
                defaultDomain.RunApplicationIfNotAlreadyRunning();
                if (defaultDomain.Application.MainWindow != null)
                {
                    defaultDomain.Application.MainWindow.DataContext = CreateMainWindowViewModel(guiRunnerState);
                    defaultDomain.Application.MainWindow.Show();
                }
            }
        }

        public static void RequestGuiSessionRestart(
            bool useViewModels,
            string xamlCode,
            string viewModelCode,
            ViewModelAssemblyBuilder assemblyBuilder = null)
        {
            var guiRunnerState = GetState();

            guiRunnerState.UseViewModels = useViewModels;
            guiRunnerState.XamlCode = xamlCode;
            guiRunnerState.ViewModelCode = viewModelCode;
            guiRunnerState.FinishApplicationNow = false;
            guiRunnerState.ViewModelAssembly = null;

            if (useViewModels)
            {
                BuildViewModelAssemblyInBackground(xamlCode, viewModelCode, assemblyBuilder, guiRunnerState);
            }

            if (!AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                Application.Current.Shutdown();
            }

            GetDefaultDomain().InvokeOnDispatcher(RunGuiSession);
        }

        public static void SetViewModelAssembly(KeyValuePair<string, byte[]> assemblyData)
        {
            Debug.Assert(
                AppDomain.CurrentDomain.IsDefaultAppDomain() || CanViewModelAssemblyBeLoadedInCurrentAppDomain(assemblyData.Key),
                "Program logic error: SetViewModelAssembly() is called when a ViewModel's assembly is already loaded. " +
                "A new GUI Session wityh a fresh AppDomain should be requested instead.");

            GetState().ViewModelAssembly = assemblyData;
        }

        public static bool CanViewModelAssemblyBeLoadedInCurrentAppDomain(string assemblyName)
        {
            return assemblyName != default && !RequestedAssemblies.Contains(assemblyName);
        }

        private static void RunDevotedAppDomain(DefaultDomain defaultDomain, GuiRunnerState guiRunnerState)
        {
            var devotedAppDomain = AppDomain.CreateDomain("Devoted Domain");
            SetDomainData(devotedAppDomain, defaultDomain, guiRunnerState);
            devotedAppDomain.DoCallBack(RunGuiInsideDevotedDomain);
            if (guiRunnerState.FinishApplicationNow)
            {
                Application.Current.MainWindow?.Close();
            }
        }

        private static void RunGuiInsideDevotedDomain()
        {
            var guiRunnerState = GetState();

            guiRunnerState.StartupError = null;
            var assemblyDataOrException = guiRunnerState.WaitAssemblyData();

            if (guiRunnerState.ObsoleteDomain != null)
            {
                AppDomain.Unload(guiRunnerState.ObsoleteDomain);
            }

            guiRunnerState.ObsoleteDomain = AppDomain.CurrentDomain;

            assemblyDataOrException?.Fold(
                exception => guiRunnerState.StartupError = exception, 
                SetViewModelAssembly);

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            CreateApplication().Run();
        }

        private static void BuildViewModelAssemblyInBackground(
            string xamlCode,
            string viewModelCode,
            ViewModelAssemblyBuilder assemblyBuilder,
            GuiRunnerState guiRunnerState)
        {
            Task
                .Run(() => 
                    Try(() => 
                            assemblyBuilder?.Build() ??
                            TryCreateAssemblyBuilder(
                                new XamlCode(xamlCode),
                                TryParseViewModelCode(viewModelCode))
                            .FlatMap(builder => builder.Build()))
                    .Catch(exception => Left(exception)))
                .ContinueWith(task =>
                    {
                        guiRunnerState.SetAssemblyData(
                            task.IsCanceled
                                    ? Left<Exception, KeyValuePair<string, byte[]>>(new TaskCanceledException())
                                    : task.Result);
                    },
                    TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.NotOnFaulted);
        }

        private static DefaultDomain GetDefaultDomain()
        {
            var defaultDomain = (DefaultDomain)AppDomain.CurrentDomain.GetData(DefaultDomainDataName);
            Debug.Assert(
                defaultDomain != null,
                "Program logic error: DefaultDomain should have been set in current AppDomain by this time.");
            return defaultDomain;

        }

        private static GuiRunnerState GetState()
        {
            var guiRunnerState = (GuiRunnerState)AppDomain.CurrentDomain.GetData(StateDataName);
            Debug.Assert(
                guiRunnerState != null,
                "Program logic error: GuiRunnerState should have been set in current AppDomain by this time.");
            return guiRunnerState;
        }

        private static Application CreateApplication()
        {
            return new Application
            {
                StartupUri = new Uri(
                    $"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/MainWindow.xaml", 
                    UriKind.Absolute)
            };
        }

        private static MainWindowViewModel CreateMainWindowViewModel(GuiRunnerState guiRunnerState)
        {
            return new MainWindowViewModel(guiRunnerState.UseViewModels)
            {
                XamlCodeDocument = new TextDocument(guiRunnerState.XamlCode),
                ViewModelCodeDocument = new TextDocument(guiRunnerState.ViewModelCode)
            };
        }

        private static void SetDomainData(AppDomain appDomain, DefaultDomain defaultDomain, GuiRunnerState guiRunnerState)
        {
            appDomain.SetData(DefaultDomainDataName, defaultDomain);
            appDomain.SetData(StateDataName, guiRunnerState);
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            string requestedAssemblyName = args.Name;
            RequestedAssemblies.Add(requestedAssemblyName);
            var assemblyData = GetState().ViewModelAssembly;
            return requestedAssemblyName.Equals(assemblyData?.Key, StringComparison.OrdinalIgnoreCase) 
                ? Assembly.Load(assemblyData?.Value) 
                : null;
        }

        // meaningful onlhy in current App Domain
        private static readonly ICollection<string> RequestedAssemblies = new HashSet<string>();

        private const string DefaultXamlCode = 
@"<Page
    xmlns = ""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:sys = ""clr-namespace:System;assembly=mscorlib""
    xmlns:x = ""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Button Content=""OK""/>
</Page>";

        private const string DefaultViewModelCode = 
@"namespace ViewModels
{
    public class ViewModel
    {
        public static string Greetings = ""Hello, World!"";
    }
}";
        private const string DefaultDomainDataName = "YetAnotherXamlPad.GuiRunner.DefaultDomain";
        private const string StateDataName = "YetAnotherXamlPad.GuiRunner.State";

        private class GuiRunnerState : MarshalByRefObject
        {
            public bool UseViewModels { get; set; }

            public string XamlCode { get; set; }

            public string ViewModelCode { get; set; }

            public Exception StartupError { get; set; }

            public bool FinishApplicationNow { get; set; }

            public KeyValuePair<string, byte[]>? ViewModelAssembly { get; set; }

            public AppDomain ObsoleteDomain { get; set; }

            public void SetAssemblyData(BuiltAssemblyOrException? data)
            {
                _viewModelAssemblyIsReady.Set(data);
            }

            public BuiltAssemblyOrException? WaitAssemblyData()
            {
                return _viewModelAssemblyIsReady.Wait();
            }

            private readonly DataReadyEvent<BuiltAssemblyOrException?> _viewModelAssemblyIsReady = new DataReadyEvent<BuiltAssemblyOrException?>();
        }
    }
}
