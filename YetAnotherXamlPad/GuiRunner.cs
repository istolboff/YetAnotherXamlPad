﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using ICSharpCode.AvalonEdit.Document;

namespace YetAnotherXamlPad
{
    internal static class GuiRunner 
    {
        public static MainWindowViewModel MainWindowViewModel => CreateMainWindowViewModel(GetState());

        public static void Setup(AppDomain defaultAppDomain)
        {
            Debug.Assert(
                defaultAppDomain.IsDefaultAppDomain(),
                "Program logic error: Main() is not called from default AppDomain.");

            var defaultDomainApplication = CreateApplication();
            var defaultDomain = new DefaultDomain(defaultDomainApplication);
            var guiRunnerState = new GuiRunnerState
            {
                UseViewModels = false,
                XamlCode = DefaultXamlCode,
                ViewModelCode = DefaultViewModelCode
            };

            SetDomainData(defaultAppDomain, defaultDomain, guiRunnerState);
        }

        public static void RunGuiSession()
        {
            Debug.Assert(
                AppDomain.CurrentDomain.IsDefaultAppDomain(),
                "Program logic error: RunGuiSession() is not called from default AppDomain.");

            var guiRunnerState = GetState();
            guiRunnerState.FinishApplicationNow = true;

            if (guiRunnerState.ObsoleteDomain != null)
            {
                AppDomain.Unload(guiRunnerState.ObsoleteDomain);
                guiRunnerState.ObsoleteDomain = null;
            }

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

        public static void RequestGuiSessionRestart(bool useViewModels, string xamlCode, string viewModelCode)
        {
            var guiRunnerState = GetState();

            guiRunnerState.UseViewModels = useViewModels;
            guiRunnerState.XamlCode = xamlCode;
            guiRunnerState.ViewModelCode = viewModelCode;
            guiRunnerState.FinishApplicationNow = false;

            if (!AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                Application.Current.Shutdown();
            }

            GetDefaultDomain().InvokeOnDispatcher(RunGuiSession);
        }

        private static void RunDevotedAppDomain(DefaultDomain defaultDomain, GuiRunnerState guiRunnerState)
        {
            var devotedAppDomain = AppDomain.CreateDomain("Devoted Domain");
            SetDomainData(devotedAppDomain, defaultDomain, guiRunnerState);
            devotedAppDomain.DoCallBack(RunGuiInDevotedDomain);
            if (guiRunnerState.FinishApplicationNow)
            {
                Application.Current.MainWindow?.Close();
            }
        }

        private static void RunGuiInDevotedDomain()
        {
            var guiRunnerState = GetState();
            guiRunnerState.ObsoleteDomain = AppDomain.CurrentDomain;
            CreateApplication().Run();
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

        private const string DefaultXamlCode = 
@"<Page
    xmlns = ""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:sys = ""clr-namespace:System;assembly=mscorlib""
    xmlns:x = ""http://schemas.microsoft.com/winfx/2006/xaml"">
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
    }
}
