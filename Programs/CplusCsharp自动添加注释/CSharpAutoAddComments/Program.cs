using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharpAutoAddComments;

namespace ConsoleApplication2
{
    class Program
    {
        static void Main(string[] args)
        {
            
            ConsoleKeyInfo ckey;
            CommentAddApi api;
            Log.L("Is Add C# Comments? y indicate add C# comments, or indicate add C++ comments.");
            ckey = Console.ReadKey();
            if (ckey.Key == ConsoleKey.Y)
            {
                Log.L("switch to C# format api");
                api = new CsharpApi();
            }
            else
            {
                Log.L("switch to C++ format api");
                api = new CplusApi();
            }

            bool isdirorfile = false;
            do
            {
                Console.WriteLine("input directory|file path you want to add comment:");
                var dirorfile = Console.ReadLine();
                isdirorfile = Directory.Exists(dirorfile) || File.Exists(dirorfile);
                if (isdirorfile)
                {
                    if (Directory.Exists(dirorfile))
                    {
                        var files = GetFiles(dirorfile);
                        foreach (var file in files)
                        {
                            api.FormatFile(file);
                        }
                    }
                    else
                    {
                        api.FormatFile(dirorfile);
                    }
                }
                else
                {
                    Log.L("Wrong Directory");
                }
            } while (!isdirorfile);

            Console.WriteLine("press any key to end!");
            Console.Read();
        }

        static List<string> GetFiles(string dir)
        {
            var list = Directory.GetFiles(dir, "*.cs|*.h", SearchOption.AllDirectories).ToList();
            return
                list.Where(c => !c.Contains("Properties") && !c.Contains("Debug") && !c.Contains("AssemblyVersionInfo"))
                    .ToList();
        }
    }
}
