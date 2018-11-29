using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using JetBrains.Annotations;

namespace YetAnotherXamlPad
{
    public abstract class LambdaConverter<TResult> : MarkupExtension, IValueConverter
    {
        protected LambdaConverter([NotNull] Func<object, TResult> lambda)
        {
            _lambda = lambda;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return _lambda(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        private readonly Func<object, TResult> _lambda;
    }

    public class TabHeaderColorConverter : LambdaConverter<Brush>
    {
        public TabHeaderColorConverter() 
            : base(value => value != null && System.Convert.ToInt32(value) > 0 ? new SolidColorBrush(Colors.Red) : null)
        {
        }
    }

    public class ErrorListVisibilityConverter : LambdaConverter<Visibility>
    {
        public ErrorListVisibilityConverter()
            : base(value => value != null && System.Convert.ToInt32(value) > 0 ? Visibility.Visible : Visibility.Collapsed)
        {
        }
    }
}
