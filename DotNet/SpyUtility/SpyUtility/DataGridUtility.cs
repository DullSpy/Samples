using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SpyUtility
{
    internal abstract class DataGridUtilityAb
    {
        protected Dictionary<DataGridColumn, Binding> GetColumnBindingDic(DataGrid datagrid)
        {
            if (null == datagrid)
                return null;
            var dict = new Dictionary<DataGridColumn, Binding>();
            PropertyInfo prop;
            foreach (var col in datagrid.Columns.OrderBy(c => c.DisplayIndex))
            {
                if (!CoperSpecialColumn(dict, col))
                {
                    prop = col.GetType().GetProperty("Binding");
                    if (null != prop)
                    {
                        var binding = prop.GetValue(col, null) as Binding;
                        dict.Add(col, binding);
                    }
                }
            }
            return dict;
        }

        protected virtual bool CoperSpecialColumn(Dictionary<DataGridColumn, Binding> dic, DataGridColumn col)
        {
            return false;
        }
    }

    internal sealed class SpecialDataGridUtility : DataGridUtilityAb
    {
        private Dictionary<DataGridColumn, Binding> _columnBindingDic;

        public SpecialDataGridUtility(DataGrid datagrid)
        {
            _columnBindingDic = GetColumnBindingDic(datagrid);
        }

        protected override bool CoperSpecialColumn(Dictionary<DataGridColumn, Binding> dic, DataGridColumn col)
        {
            if (null != dic && null != col)
            {
                if (PAAttachedProperty.GetName(col) == "colMark")
                {
                    dic.Add(col, new Binding("Mark"));
                    return true;
                }
            }
            return false;
        }

        private IList<Binding> GetVisibilityColumnBindings()
        {
            if (null == _columnBindingDic)
                return new List<Binding>();
            return _columnBindingDic.Where(c => c.Key.Visibility == Visibility.Visible).Select(c => c.Value).ToList();
        }

        private string GetProString(object obj, Binding binding)
        {
            var pvalue = TypeUtility.GetPropertyValueByPath(obj, binding.Path.Path);
            if (null != pvalue)
            {
                if (null != binding.Converter)
                {
                    var back = binding.Converter.Convert(pvalue, typeof(object), binding.ConverterParameter, null);
                    if (null != back)
                        return back.ToString();
                }
                return pvalue.ToString();
            }
            return string.Empty;
        }

        public string ConvertDisplayProOfItemToString(object study, string split)
        {
            var visiColumns = GetVisibilityColumnBindings();
            var pros = visiColumns.Select(c => GetProString(study, c)).Where(v => v != null).ToList();
            return string.Join(split, pros);
        }
    }
}
