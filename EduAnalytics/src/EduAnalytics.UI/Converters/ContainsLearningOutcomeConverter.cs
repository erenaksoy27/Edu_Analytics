using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using EduAnalytics.Business.Dtos;
using System.Collections.Generic;

namespace EduAnalytics.UI.Converters
{
    public class ContainsLearningOutcomeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return false;
            var list = values[0] as IEnumerable<LearningOutcomeDto>;
            var item = values[1] as LearningOutcomeDto;
            if (list == null || item == null) return false;
            return list.Any(lo => lo.Id == item.Id);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
