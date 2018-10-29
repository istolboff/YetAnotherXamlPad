using System;

namespace YetAnotherXamlPad
{
    public static class Startup
    {
        [STAThread]
        public static void Main()
        {
            GuiRunner.Setup(AppDomain.CurrentDomain);
            GuiRunner.RunGuiSession();
        }
    }
}
