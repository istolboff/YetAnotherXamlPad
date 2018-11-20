using System;
using JetBrains.Annotations;

namespace YetAnotherXamlPad
{
    public partial class MainWindow 
    {
        [UsedImplicitly]
        public MainWindow()
        {
            InitializeComponent();
            Closed += (_, __) => (DataContext as IDisposable)?.Dispose();
            DataContextChanged += (_, args) => (args.OldValue as IDisposable)?.Dispose();
        }
    }
}
