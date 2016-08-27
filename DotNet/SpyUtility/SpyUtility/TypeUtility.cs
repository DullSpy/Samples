using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NameProInfoDic= System.Collections.Generic.Dictionary<string, System.Reflection.PropertyInfo>;

namespace SpyUtility
{
    internal sealed class TypeUtility
    {
        private static Dictionary<Type, NameProInfoDic>
            _typePropertyDictionary = new Dictionary<Type, NameProInfoDic>();

        public static NameProInfoDic GetPropertyDicByType(Type type)
        {
            if (!_typePropertyDictionary.ContainsKey(type))
                AddPropertyDicByType(type);
            return _typePropertyDictionary[type];
        }

        private static void AddPropertyDicByType(Type type)
        {
            var typePropertDic = new NameProInfoDic();
            foreach (var pro in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!typePropertDic.ContainsKey(pro.Name))
                {
                    typePropertDic.Add(pro.Name, pro);
                }
            }
            _typePropertyDictionary.Add(type, typePropertDic);
        }

        public static PropertyInfo GetPropertyInfoByTypeAndName(Type type, string proName)
        {
            var proDic = GetPropertyDicByType(type);
            if (proDic.ContainsKey(proName))
                return proDic[proName];
            return null;
        }

        public static object GetPropertyValueByPath(object obj, string path)
        {
            if (null == obj)
                return null;
            if (string.IsNullOrEmpty(path))
                return obj;
            var pros = path.Split('.');
            object back = obj;
            foreach (var pro in pros)
            {
                var proinfo = GetPropertyInfoByTypeAndName(back.GetType(), pro);
                if (null != proinfo)
                    back = proinfo.GetValue(back, null);
                else
                {
                    return null;
                }
            }
            return back;
        }
    }
}
