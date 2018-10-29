using System;

namespace YetAnotherXamlPad
{
    internal sealed class GuiRunnerState : MarshalByRefObject
    {
        public bool UseViewModels { get; set; }

        public string XamlCode { get; set; }

        public string ViewModelCode { get; set; }

        public bool FinishApplicationNow { get; set; }

        public AppDomain ObsoleteDomain { get; set; }
    }
}
