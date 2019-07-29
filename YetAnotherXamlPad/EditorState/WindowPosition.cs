using System;
using System.Windows;

namespace YetAnotherXamlPad.EditorState
{
    [Serializable]
    internal struct WindowPosition
    {
        public Rect Location { get; set; }

        public WindowState WindowState { get; set; }

        public void ApplyTo(Window window)
        {
            window.Left = Location.Left;
            window.Top = Location.Top;
            window.Width = Location.Width;
            window.Height = Location.Height;
            window.WindowState = WindowState;
        }
    }
}
