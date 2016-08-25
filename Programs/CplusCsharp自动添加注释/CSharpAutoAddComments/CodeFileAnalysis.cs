using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConsoleApplication2;

namespace CSharpAutoAddComments
{
    internal class Log
    {
        public static void L(string log)
        {
            Console.WriteLine(log);
        }

        public static void L(string log, params object[] param)
        {
            Console.WriteLine(log, param);
        }
    }

    internal class RH
    {
        private static Dictionary<string, Regex> _regexDic = new Dictionary<string, Regex>();

        public static Regex Get(string key)
        {
            if (!_regexDic.ContainsKey(key))
                _regexDic.Add(key, new Regex(key, RegexOptions.IgnoreCase));
            return _regexDic[key];
        }
    }

    internal abstract class CommentAddApi
    {
        public void FormatFile(string filePath)
        {
            try
            {
                Log.L("begin file format {0}", filePath);
                var filecontent = File.ReadAllText(filePath);
                var methods = GetMethodsContentFromFileContent(filecontent);
                
                foreach (var method in methods)
                {
                    if (!method.IsValid())
                        continue;
                    Log.L("format {0}", method.Name);
                    filecontent = filecontent.Replace(method.Content, method.GetMethodWithComment());
                }

                File.WriteAllText(filePath, filecontent);
                Log.L("end file format {0}", filePath);
            }
            catch (Exception ex)
            {
                Log.L(ex.Message);
            }
        }

        protected abstract IEnumerable<CodeMethodBase> GetMethodsContentFromFileContent(string fileContent);
    }

    internal class CplusApi : CommentAddApi
    {
        private static List<string> GetMatchedClassList(string content)
        {
            var matches = RH.Get(MatchStr.CplusClass).Matches(content);
            return matches.OfType<Match>().Select(c => c.Value).ToList();
        }

        private static List<string> GetPublicRegionList(string content)
        {
            var publicmatches = RH.Get(MatchStr.CplusPublic).Matches(content);
            var privatematches = RH.Get(MatchStr.CplusPrivate).Matches(content);
            var publicindexs = publicmatches.OfType<Match>().Select(c => new {index = c.Index, ispublic = true});
            var privateindexs = privatematches.OfType<Match>().Select(c => new {index = c.Index, ispublic = false});
            var indexList = publicindexs.Union(privateindexs).OrderBy(c => c.index).ToList();
            var backlist = new List<string>();
            int i = 0, s = -1;
            while (i < indexList.Count)
            {
                if (indexList[i].ispublic && s == -1)
                {
                    s = indexList[i].index;
                }
                if (!indexList[i].ispublic && s != -1)
                {
                    backlist.Add(content.Substring(s, indexList[i].index - s));
                    s = -1;
                }
                i++;
                if (i == indexList.Count && s != -1)
                    backlist.Add(content.Substring(s));
            }

            return backlist;
        }

        private static IEnumerable<CodeMethodBase> GetMethodList(string classcontent)
        {
            var matches =
                RH.Get(string.Format("(?<!{0})", MatchStr.Comment) + MatchStr.CplusMethod).Matches(classcontent);
            return matches.OfType<Match>().Select(c => new CplusCodeMethod(c)).ToList();
        }

        protected override IEnumerable<CodeMethodBase> GetMethodsContentFromFileContent(string fileContent)
        {
            var methodsList = new List<CodeMethodBase>();
            GetMatchedClassList(fileContent).ForEach(c =>
            {
                GetPublicRegionList(c).ForEach(p =>
                {
                    methodsList.AddRange(GetMethodList(p));
                });
            });
            return methodsList.Distinct(new CodeMethodEquality()).ToList();
        }
    }

    internal class CsharpApi : CommentAddApi
    {
        protected override IEnumerable<CodeMethodBase> GetMethodsContentFromFileContent(string fileContent)
        {
            var matches = RH.Get(MatchStr.CsharpMethod).Matches(fileContent);
            return matches.OfType<Match>().Select(c => new CsharpCodeMethod(c)).ToList();
        }
    }

    internal class MatchStr
    {
        public static string Comment = @"//[^{};]*";
        public static string NotComment = string.Format("(?<!{0})", Comment);
        public static string CsharpStatic = @"(\s+static){0,1}";

        public static string Brace = @"{[^{}]*((?<open>{)|(?<-open>})|[^{}])*(?(open)(?!))[^{}]*}";
        public static string CplusClass = @"class\s+[^\s{}]+[^{}]+" + Brace;
        public static string CplusPrivate = @"\s+private:\s+";
        public static string CplusPublic = @"\s+public:\s+";
        public static string CplusPublicPrivateContent = @"(?<=public:)[\s\S]+(?=\s+private:\s+?)";
        //1,2,3 indicate backtype,name,params
        public static string CplusMethod = @"[^\r\n/]+[^\r\n\S]+(\S+)[^\r\n\S]+([^\s~\->]+)[^\r\n\S]*\(([^()]*)\)[^\r\n]*;";
        //1,2,3 indicate backtype,name,params
        private static string CsharpMethodDefine = @"{0}{1}{2}\s+(\S+)\s+(\S+)\s*\(([^()]*)\)\s*";

        public static string CsharpMethod = string.Format(CsharpMethodDefine, NotComment, "public", CsharpStatic) +
                                            Brace;
        public static string CsharpException = @"(?<=throw new\s+)[^()\s]*\([^()\s]*\)";
    }

    internal class CodeMethodEquality : IEqualityComparer<CodeMethodBase>
    {
        public bool Equals(CodeMethodBase x, CodeMethodBase y)
        {
            return x.Content == y.Content;
        }

        public int GetHashCode(CodeMethodBase obj)
        {
            return obj.Name.GetHashCode();
        }
    }

    internal abstract class CodeMethodBase
    {
        public string Name { get;  set; }
        public string BackType { get; set; }
        public string Content { get; set; }
        public string Mtype { get; set; }
        public bool IsStatic { get; set; }
        public List<string> ParamList { get; set; }
        public List<string> ExceptionStrList { get; set; }

        public string GetMethodWithComment()
        {
            return GetComment() + Content;
        }

        protected abstract string GetComment();

        public virtual bool IsValid()
        {
            return true;
        }
    }

    internal class CsharpCodeMethod : CodeMethodBase
    {
        const string Cmt_LinePrefix = "        ";
        readonly string Cmt_summary = "/// <summary>";
        readonly string Cmt_slashwithname = Cmt_LinePrefix + "/// {0}";
        readonly string Cmt_exception = Cmt_LinePrefix + "/// <exception>";
        readonly string Cmt_param2 = Cmt_LinePrefix + "/// <param name=\"{0}\">{1}</param>";
        readonly string Cmt_param3 = Cmt_LinePrefix + "/// <param name=\"{0}\">{1}{2}</param>";
        readonly string Cmt_return = Cmt_LinePrefix + "/// <returns>{0}</returns>";

        public CsharpCodeMethod(Match match)
        {
            Content = match.Groups[0].ToString();
            IsStatic = string.IsNullOrWhiteSpace(match.Groups[1].ToString());
            BackType = match.Groups[2].ToString();
            Name = match.Groups[3].ToString();
            ParamList = match.Groups[4].ToString().Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Select(c => c.Replace(' ', ','))
                .ToList();
            ExceptionStrList = RH.Get(MatchStr.CsharpException).Matches(Content).OfType<Match>()
                .Select(c => c.Value.Trim())
                .ToList();
        }

        protected override string GetComment()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Cmt_summary);
            sb.AppendLine(string.Format(Cmt_slashwithname, Name));
            sb.AppendLine(Cmt_LinePrefix + Cmt_summary);
            if (null != ExceptionStrList && ExceptionStrList.Count > 0)
            {
                sb.AppendLine(Cmt_exception);
                foreach (var s in ExceptionStrList)
                {
                    sb.AppendLine(string.Format(Cmt_slashwithname, s));
                }
                sb.AppendLine(Cmt_exception);
            }

            if (null != ParamList && ParamList.Count > 0)
            {
                foreach (var s in ParamList)
                {
                    var sa = s.Split(',');
                    if (sa.Length == 2)
                        sb.AppendLine(string.Format(Cmt_param2, sa[1], sa[0]));
                    else if (sa.Length == 3)
                        sb.AppendLine(string.Format(Cmt_param3, sa[2], sa[0], sa[1]));
                }
            }
            if (!BackType.Contains("void"))
                sb.AppendLine(string.Format(Cmt_return, BackType));
            sb.Append(Cmt_LinePrefix);
            return sb.ToString();
        }
    }

    internal class CplusCodeMethod : CodeMethodBase
    {
        const string Cmt_LinePrefix = "    ";
        readonly string Cmt_beginend =   "/////////////////////////////////////////////////////////////////";
        readonly string Cmt_prefix =     Cmt_LinePrefix + "///  ";
        readonly string Cmt_methodname = Cmt_LinePrefix + "///  \\brief         {0}";
        readonly string Cmt_paramin =    Cmt_LinePrefix + "///  \\param[in]     {0}";
        readonly string Cmt_paramout =   Cmt_LinePrefix + "///  \\param[out]    {0}";
        readonly string Cmt_return = Cmt_LinePrefix + "///  \\return        {0}";
        

        public CplusCodeMethod(Match match)
        {
            BackType = match.Groups[1].ToString();
            Name = match.Groups[2].ToString();
            Content = match.Value.Trim();
            ParamList = match.Groups[3].ToString().Split(',').Select(c => c.Trim()).ToList();
        }

        protected override string GetComment()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Cmt_beginend);
            sb.AppendLine(string.Format(Cmt_methodname, Name));
            sb.AppendLine(Cmt_prefix);
            if (null != ParamList)
            {
                var paramin = string.Join(",", ParamList.Where(c => c.Contains("const")));
                if (paramin.Length > 0)
                {
                    sb.AppendLine(string.Format(Cmt_paramin, paramin));
                    sb.AppendLine(Cmt_prefix);
                }
                else
                {
                    sb.AppendLine(string.Format(Cmt_paramin, "null"));
                    sb.AppendLine(Cmt_prefix);
                }
                var paramout = string.Join(",", ParamList.Where(c => !c.Contains("const")));
                if (paramout.Length > 0)
                {
                    sb.AppendLine(string.Format(Cmt_paramout, paramout));
                    sb.AppendLine(Cmt_prefix);
                }
                else
                {
                    sb.AppendLine(string.Format(Cmt_paramout, "null"));
                    sb.AppendLine(Cmt_prefix);
                }
            }

            sb.AppendLine(string.Format(Cmt_return, BackType));
            sb.Append(Cmt_LinePrefix);
            sb.AppendLine(Cmt_beginend);
            sb.Append(Cmt_LinePrefix);
            return sb.ToString();
        }
    }
}
