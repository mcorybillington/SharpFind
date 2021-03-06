using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace SharpFind
{
    class Program
    {
        static int Main(string[] args)
        {
            Dictionary<string, string> arguments = args.ToDictionary(
                 k => k.Split(new char[] { ':' }, 2)[0].ToLower(),
                 v => v.Split(new char[] { ':' }, 2).Count() > 1
                                                    ? v.Split(new char[] { ':' }, 2)[1]
                                                    : null); ;


            var searchPattern = arguments.ContainsKey("/e") ? arguments["/e"] : "*.*";
            var searchPath = arguments.ContainsKey("/p") ? Path.GetFullPath(arguments["/p"]) : Directory.GetCurrentDirectory();
            bool checkWritable = arguments.ContainsKey("/w");

            if (arguments.ContainsKey("/c"))
            {
                Process thisProcess = Process.GetCurrentProcess();
                switch (arguments["/c"].ToLower())
                {
                    case "r":
                        thisProcess.PriorityClass = ProcessPriorityClass.RealTime;
                        break;
                    case "h":
                        thisProcess.PriorityClass = ProcessPriorityClass.High;
                        break;
                    case "a":
                        thisProcess.PriorityClass = ProcessPriorityClass.AboveNormal;
                        break;
                    case "n":
                        thisProcess.PriorityClass = ProcessPriorityClass.Normal;
                        break;
                    case "b":
                        thisProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
                        break;
                    case "i":
                        thisProcess.PriorityClass = ProcessPriorityClass.Idle;
                        break;
                }
            }


            if ((!Directory.Exists(searchPath)) && !(File.Exists(searchPath)))
            {
                Console.WriteLine($"[-] Search path doesn't exist: {searchPath}");
                return 1;
            }

            int writeTime = parseIntFromArgs(arguments, "/m");
            Console.WriteLine(writeTime);

            


            Console.WriteLine($"[+] Search Path: {searchPath}");
            Console.WriteLine($"[+] Search Pattern: {searchPattern}{Environment.NewLine}");


            if (arguments.ContainsKey("/m"))
            {
                try
                {
                    return Convert.ToInt32(arguments["/m"]) * -1;
                }
                catch
                {
                    Console.WriteLine($"[-] Last Modified Time '/m' value: '{arguments["/m"]}' is invalid");
                    return 1;
                }
            }

            doFileChecks(searchPath, searchPattern, checkWritable, writeTime);

            return 0;
        }

        // This just opens the file to write and immediately closes it.
        // It will throw an exception if it cannot. Outputs if the file is locked, otherwise it just returns.
        private static string isWritable(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }
            try
            {
                File.OpenWrite(path).Close();
                return path;
            }
            catch (Exception ex)
            {
                if (ex is IOException)
                {
                    return $"[LOCKED] {path} [LOCKED]";
                }
                else
                {
                    return null;
                }
            }
        }

        // Compares the last file write time to the time this script is run plus the parameter passed.
        private static string hasBeenModified(string path, DateTime? mtime)
        {
            if (File.GetLastWriteTime(path) > mtime)
            {
                return path;
            }
            return null;
        }

        private static int parseIntFromArgs(Dictionary<string, string> arguments, string key)
        {
            if (arguments.ContainsKey(key))
            {
                try
                {
                    return Convert.ToInt32(arguments["/m"]) * -1;
                }
                catch
                {
                    Console.WriteLine($"[-] Value for '{key}' must be an integer.");
                    return 1;
                }
            }
            return 0;
        }

        // Credit to Marc Gravell for this from StackOverflow. https://stackoverflow.com/a/4986333
        public static void doFileChecks(string root, string pattern, bool checkWrite, int modtime)
        {

            DateTime modifiedTime = DateTime.Now.AddMinutes(modtime);
            Stack<string> pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count != 0)
            {
                var path = pending.Pop();
                string[] next = null;
                try
                {
                    next = Directory.GetFiles(path, pattern);
                }
                catch { }
                if (next != null && next.Length != 0)
                {
                    foreach (var file in next)
                    {
                        string outFile = file;
                        if (modtime != 0)
                        {
                            outFile = hasBeenModified(file, modifiedTime);
                        }
                        if (checkWrite)
                        {
                            outFile = isWritable(outFile);
                        }
                        if (outFile != null)
                        {
                            Console.WriteLine(outFile);
                        }

                    }
                }
                try
                {
                    next = Directory.GetDirectories(path);
                    foreach (var subdir in next)
                    {
                        pending.Push(subdir);
                    }
                }
                catch { }
            }
        }
    }
}

