using System;

namespace YetAnotherXamlPad
{
    public static class Startup
    {
        [STAThread]
        [LoaderOptimization(LoaderOptimization.MultiDomainHost)]
        public static void Main()
        {
            GuiRunner.Setup(AppDomain.CurrentDomain);
            GuiRunner.RunGuiSession();
        }
    }
}
