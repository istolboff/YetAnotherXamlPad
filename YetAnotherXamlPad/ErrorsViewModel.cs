using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Xaml;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.Text;
using YetAnotherXamlPad.TextMarking;
using YetAnotherXamlPad.Utilities;
using static YetAnotherXamlPad.Utilities.Do;

namespace YetAnotherXamlPad
{
    internal sealed class ErrorsViewModel : INotifyPropertyChanged
    {
        public ErrorsViewModel(
            [NotNull] ErrorTextMarkerService xamlTextMarkerService,
            [NotNull] ErrorTextMarkerService viewModelTextMarkerService)
        {
            _xamlTextMarkerService = xamlTextMarkerService;
            _viewModelTextMarkerService = viewModelTextMarkerService;

            XamlErrors = new ObservableCollection<SourceCodeError>();
            ViewModelErrors = new ObservableCollection<SourceCodeError>();
        }

        public ObservableCollection<SourceCodeError> XamlErrors { get; }

        public SourceCodeError SelectedXamlError
        {
            get => _selectedXamlError;
            set
            {
                if (ReferenceEquals(value, _selectedXamlError))
                {
                    return;
                }

                _selectedXamlError = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(SelectedXamlError)));
                if (value?.Location != null)
                {
                    SetCaretToErrorStart(_xamlTextMarkerService, value.Location);
                }
            }
        }

        public ObservableCollection<SourceCodeError> ViewModelErrors { get; }

        public SourceCodeError SelectedViewModelError
        {
            get => _selectedViewModelError;
            set
            {
                if (ReferenceEquals(value, _selectedViewModelError))
                {
                    return;
                }

                _selectedViewModelError = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(SelectedViewModelError)));
                SetCaretToErrorStart(_viewModelTextMarkerService, value.Location);
            }
        }

        public void ClearErrors()
        {
            ReportErrors(new SourceCodeError[] { }, XamlErrors, _xamlTextMarkerService);
            ReportErrors(new SourceCodeError[] { }, ViewModelErrors, _viewModelTextMarkerService);
        }

        public void ReportXamlError(Exception exception)
        {
            switch (exception)
            {
                case XamlException xamlException:
                    ReportXamlError(_xamlTextMarkerService.GetSourceCodeError(xamlException));
                    break;

                case System.Windows.Markup.XamlParseException xamlParseException:
                    ReportXamlError(_xamlTextMarkerService.GetSourceCodeError(xamlParseException));
                    break;

                default:
                    ReportXamlError(new SourceCodeError(exception.Message));
                    break;
            }
        }

        public void ReportViewModelErrors(Exception exception)
        {
            switch (exception)
            {
                case AssemblyBuildException assemblyBuildException:
                    ReportViewModelErrors(assemblyBuildException.BuildErrors);
                    break;

                default:
                    ReportViewModelErrors(new[] { new SourceCodeError(exception.Message) });
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = Nothing;

        private void ReportXamlError(SourceCodeError sourceCodeError)
        {
            ReportErrors(new[] { sourceCodeError }, XamlErrors, _xamlTextMarkerService);
        }

        private void ReportViewModelErrors(IReadOnlyCollection<SourceCodeError> buildErrors)
        {
            ReportErrors(buildErrors, ViewModelErrors, _viewModelTextMarkerService);
        }

        private static void ReportErrors(
            IReadOnlyCollection<SourceCodeError> newErrors,
            ICollection<SourceCodeError> observableErrors, 
            ErrorTextMarkerService errorTextMarkerService)
        {
            observableErrors.ReplaceAllWithRange(
                newErrors.OrderBy(
                    error => error.Location ?? BiggestLinePositionSpan, 
                    new LinePositionSpanComparer()));
            errorTextMarkerService?.RemoveAllErrorMarkers();
            foreach (var errorLocation in newErrors.Where(e => e.Location.HasValue).Select(e => e.Location.Value))
            {
                errorTextMarkerService?.MarkError(errorLocation);
            }
        }

        private static void SetCaretToErrorStart(ErrorTextMarkerService textMarkerService, LinePositionSpan? errorLocation)
        {
            if (errorLocation != null)
            {
                textMarkerService.SetCaretToErrorStart(errorLocation.Value.Start);
            }
        }

        private readonly ErrorTextMarkerService _xamlTextMarkerService;
        private readonly ErrorTextMarkerService _viewModelTextMarkerService;

        private SourceCodeError _selectedXamlError;
        private SourceCodeError _selectedViewModelError;

        private static readonly LinePositionSpan BiggestLinePositionSpan = new LinePositionSpan(new LinePosition(100000, 0), new LinePosition(100000, 1));

        private class LinePositionSpanComparer : IComparer<LinePositionSpan>
        {
            public int Compare(LinePositionSpan x, LinePositionSpan y)
            {
                return x.Start.Line != y.Start.Line
                        ? x.Start.Line - y.Start.Line
                        : x.Start.Character - y.Start.Character;
            }
        }
    }
}
