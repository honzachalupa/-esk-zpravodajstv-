using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace CeskeZpravodajstvi
{
    class ConverterContent : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string content = value.ToString();

            content = Regex.Replace(content, "#.+?#", "");

            return content;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
