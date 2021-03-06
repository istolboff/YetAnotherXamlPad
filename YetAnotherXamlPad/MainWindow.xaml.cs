﻿using System;
using JetBrains.Annotations;

namespace YetAnotherXamlPad
{
    public partial class MainWindow 
    {
        [UsedImplicitly]
        public MainWindow()
        {
            InitializeComponent();

            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            GuiRunner.StartupInfo.EditorState.MainWindowPosition?.ApplyTo(this);

            Loaded += (_, __) => DataContext = new MainWindowViewModel(
                                                    xamlCodeEditor: new CodeEditor(XamlEditor),
                                                    viewModelCodeEditor: new CodeEditor(ViewModelEditor));

            Closed += (_, __) => (DataContext as IDisposable)?.Dispose();

            DataContextChanged += (_, args) => (args.OldValue as IDisposable)?.Dispose();
        }
    }
}
