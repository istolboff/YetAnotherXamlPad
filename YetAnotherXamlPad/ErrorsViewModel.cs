using System.ComponentModel;
using static YetAnotherXamlPad.Utilities.Do;

namespace YetAnotherXamlPad
{
    internal sealed class ErrorsViewModel : INotifyPropertyChanged
    {
        public string XamlError
        {
            get => _xamlError;
            set
            {
                if (value == _xamlError)
                {
                    return;
                }

                _xamlError = value;

                PropertyChanged(this, new PropertyChangedEventArgs(nameof(XamlError)));
            }
        }

        public string ViewModelError
        {
            get => _viewModelError;
            set
            {
                if (value == _viewModelError)
                {
                    return;
                }

                _viewModelError = value;

                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ViewModelError)));
            }
        }

        public void ClearErrors()
        {
            XamlError = null;
            ViewModelError = null;
        }

        public event PropertyChangedEventHandler PropertyChanged = Nothing;

        private string _xamlError;
        private string _viewModelError;
    }
}
