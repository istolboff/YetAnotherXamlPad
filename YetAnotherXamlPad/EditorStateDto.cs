using System;
using System.Windows;
using YetAnotherXamlPad.EditorState;

namespace YetAnotherXamlPad
{
    [Serializable]
    internal struct EditorStateDto
    {
        public bool UseViewModel { get; set; }

        public string XamlCode { get; set; }

        public string ViewModelCode { get; set; }

        public bool ReportBindingErrors { get; set; }

        public bool ApplyViewModelChangesImmediately { get; set; }

        public WindowState WindowState { get; set; }

        public WindowPosition? MainWindowPosition { get; set; }
    }
}
