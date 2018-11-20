using System;
using System.Collections.Generic;
using System.Threading;

namespace YetAnotherXamlPad
{
    internal sealed class GuiRunnerState : MarshalByRefObject
    {
        public bool UseViewModels { get; set; }

        public string XamlCode { get; set; }

        public string ViewModelCode { get; set; }

        public bool FinishApplicationNow { get; set; }

        public Either<KeyValuePair<string, byte[]>, Exception>? ViewModelAssembly { get; set; }

        public AppDomain ObsoleteDomain { get; set; }

        public AutoResetEvent ViewModelAssemblyIsReady { get; set; }
    }
}
