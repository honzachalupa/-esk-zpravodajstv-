using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace CeskeZpravodajstvi
{
    class ConverterDateToHumanReadable : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (DateTime.Parse(value.ToString()).ToString("d.M.yyyy") == DateTime.Now.ToString("d.M.yyyy"))
            {
                value = "Dnes, " + DateTime.Parse(value.ToString()).ToString("H:mm");
            }
            else if (DateTime.Parse(value.ToString()).ToString("d.M.yyyy") == DateTime.Now.AddDays(-1).ToString("d.M.yyyy"))
            {
                value = "Včera, " + DateTime.Parse(value.ToString()).ToString("H:mm");
            }
            else
            {
                value = DateTime.Parse(value.ToString()).ToString("d.M.yyyy H:mm");
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}