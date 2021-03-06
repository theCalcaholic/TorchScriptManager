﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using NLog;

namespace ScriptManager.Ui
{
    public class MultiplyConverter : IValueConverter
    {
        Logger Log = LogManager.GetLogger("ScriptManager");
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double factor = 1D;
            if (parameter is string fString)
                if (double.TryParse(fString, NumberStyles.Float, CultureInfo.CreateSpecificCulture("en-US"), out double factorParam))
                    factor = factorParam;
            return ((double)value) * factor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double factor = 1D;
            if (parameter is string fString)
                if (Double.TryParse(fString, NumberStyles.Float, CultureInfo.CreateSpecificCulture("en-US"), out double factorParam))
                    factor = factorParam;
            return ((double)value) / factor;
        }
    }

    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }
}
