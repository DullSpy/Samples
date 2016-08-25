using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApplication2
{
    internal class Log
    {
        public static void L(string log)
        {
            Console.WriteLine(log);
        }

        public static void L(string log, params object[] para)
        {
            Console.WriteLine(log, para);
        }
    }


    internal sealed class MethodCommentApi
    {
        public MethodCommentApi(MethodType type = MethodType.All)
        {
            _methodMatchStr = RegexStr.GetMethodMatchStrByMethodType(type);
        }

        private string _methodMatchStr = null;
            
        public void FormatFile(string filePath)
        {
            try
            {
                Log.L("begin file format {0}", filePath);
                var filecontent = File.ReadAllText(filePath);
                var methods = GetMethodsContentFromFileContent(filecontent);
                //var tsbuilder = new StringBuilder();
                foreach (var method in methods)
                {
                    var methodDetail = MethodDetail.InitialByContent(method);
                  //  tsbuilder.AppendLine(methodDetail.MethodWithComment());
                    filecontent = filecontent.Replace(method, methodDetail.MethodWithComment());
                }
                File.WriteAllText(filePath, filecontent);
                //File.WriteAllText(@"C:\Users\jingang.wu\Desktop\sss.txt",tsbuilder.ToString());
                Log.L("end file format {0}", filePath);
            }
            catch (Exception ex)
            {
                Log.L(ex.Message);
            }
        }

        private List<string> GetMethodsContentFromFileContent(string fileContent)
        {
            return RegexHelper.Get(_methodMatchStr).Matches(fileContent).OfType<Match>().Select(c => c.Value).ToList();
        }
    }

    internal class MethodDetail
    {
        private MethodDetail()
        {
        }

        public static MethodDetail InitialByContent(string content)
        {
            var defineRegex = RegexHelper.Get(RegexStr.GetMethodDefineStr(MethodType.All));
            var contentRegex = RegexHelper.Get(RegexStr.METHODCONTENDSTR);
            var methoddefine = defineRegex.Match(content).Value;
            var method = new MethodDetail();
            method.Content = content;
            var maincontent = contentRegex.Match(content).Value;
            method.BackType =
                RegexHelper.Get(RegexStr.METHODBACKTYPESTR).Match(methoddefine.Replace("static", "")).Value;
            method.Name = RegexHelper.Get(RegexStr.METHODNAMESTR).Match(methoddefine).Value;
            method.Mtype = RegexHelper.Get(RegexStr.METHVISISTR).Match(methoddefine).Value;
            method.IsStatic = methoddefine.Contains("static");
            var paramstr = RegexHelper.Get(RegexStr.METHPARAMSTR).Match(methoddefine).Value;
            method.ParamList =
                paramstr.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .Select(c => c.Replace(' ', ','))
                    .ToList();
            method.ExceptionStrList =
                RegexHelper.Get(RegexStr.METHEXCEPTION)
                    .Matches(maincontent)
                    .OfType<Match>()
                    .Select(c => c.Value.Trim())
                    .ToList();
            return method;
        }

        public string Name { get; private set; }
        public string BackType { get; private set; }
        public string Content { get; private set; }
        public string Mtype { get; private set; }
        public bool IsStatic { get; private set; }
        public List<string> ParamList { get; private set; }
        public List<string> ExceptionStrList { get; private set; }

        private string SUMMARY = "/// <summary>";
        private string THREESLASH = "        /// {0}";
        private string EXCEPTION = "        /// <exception>";
        private string PARAM2= "        /// <param name=\"{0}\">{1}<//param>";
        private string PARAM3= "        /// <param name=\"{0}\">{1}{2}<//param>";
        private string RETURN = "        /// <returns>{0}<//returns>";
        private string END = "        ";
        public string MethodWithComment()
        {
            var sb = new StringBuilder();
            sb.AppendLine(SUMMARY);
            sb.AppendLine(string.Format(THREESLASH, Name));
            sb.AppendLine(END + SUMMARY);
            if (null != ExceptionStrList&& ExceptionStrList.Count>0)
            {
                sb.AppendLine(EXCEPTION);
                foreach (var s in ExceptionStrList)
                {
                    sb.AppendLine(string.Format(THREESLASH, s));
                }
                sb.AppendLine(EXCEPTION);
            }
            if (null != ParamList && ParamList.Count > 0)
            {
                foreach (var s in ParamList)
                {
                    var sa = s.Split(',');
                    if (sa.Length == 2)
                        sb.AppendLine(string.Format(PARAM2, sa[1], sa[0]));
                    else if (sa.Length == 3)
                        sb.AppendLine(string.Format(PARAM3, sa[2], sa[0], sa[1]));
                }
            }
            if (!BackType.Contains("void"))
                sb.AppendLine(string.Format(RETURN, BackType));
            sb.Append(END);
            sb.AppendLine(Content);
            return sb.ToString();
        }
    }

    [Flags]
    internal enum MethodType
    {
        Static = 0x01,
        Public = 0x02,
        Private = 0x04,
        Internal = 0x08,
        All = 0x0F
    }

    internal sealed class RegexStr
    {
        private static string COMMENTSTR = @"//[^{};]*";
        public static string STATICMETHODSTR = @"(\s+static){0,1}";
        private static string METHODDEFINESTR = @"{2}({0}){1}\s+\S+\s+\S+\s*\([^()]*\)\s*";
        public static string METHODBACKTYPESTR = @"(?<=(public|private|internal)(\s+static){0,1}\s+)\S+";
        public static string METHODNAMESTR = @"(?<=(public|private|internal)(\s+static){0,1}\s+\S+\s+)\S+(?=\s*\()";
        public static string METHODCONTENDSTR = @"{[^{}]*((?<open>{)|(?<-open>})|[^{}])*(?(open)(?!))[^{}]*}";
        public static string METHVISISTR = @"(public|private|internal)";
        public static string METHPARAMSTR = @"(?<=\()[^()]*(?=\))";
        public static string METHEXCEPTION = @"(?<=throw new\s+)[^()\s]*\([^()\s]*\)";

        public static string GetMethodDefineStr(MethodType type = MethodType.Public,bool withoutComment = true)
        {
            bool hasPublic = (type & MethodType.Public) != 0;
            bool hasPrivate = (type & MethodType.Private) != 0;
            bool hasInternal = (type & MethodType.Internal) != 0;
            bool hasStatic = (type & MethodType.Static) != 0;
            var sb = new StringBuilder();
            if (hasPublic) sb.Append("public");
            if (hasPrivate)
            {
                if (sb.Length > 0)
                    sb.Append("|");
                sb.Append("private");
            }
            if (hasInternal)
            {
                if (sb.Length > 0)
                    sb.Append("|");
                sb.Append("internal");
            }
            string comment = withoutComment ? string.Format("(?<!{0})", COMMENTSTR) : COMMENTSTR;

            return string.Format(METHODDEFINESTR, sb, hasStatic ? STATICMETHODSTR : "", comment);
        }

        public static string GetMethodMatchStrByMethodType(MethodType type = MethodType.Public,
            bool withoutComment = true)
        {
            return GetMethodDefineStr(type, withoutComment) + METHODCONTENDSTR;
        }
    }

    internal class RegexHelper
    {
        private static Dictionary<string, Regex> _regexDic = new Dictionary<string, Regex>();

        public static Regex Get(string key)
        {
            if (!_regexDic.ContainsKey(key))
                _regexDic.Add(key, new Regex(key, RegexOptions.IgnoreCase));
            return _regexDic[key];
        }
    }
}
