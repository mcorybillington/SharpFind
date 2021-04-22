using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace SharpFind
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Usage();
                return 0;
            }

            Dictionary<string, string> arguments = SetArgs(args);
            if (arguments.Count == 0)
            {
                Console.WriteLine(@"Arguments must be in colon separated form such as  '/e:searchpattern.*'");
                return 1;
            }

            if (arguments.ContainsKey("/h"))
            {
                Usage();
                return 0;
            }


            var searchPattern = arguments.ContainsKey("/e") ? arguments["/e"] : "*.*";
            var searchPath = arguments.ContainsKey("/p") ? Path.GetFullPath(arguments["/p"]) : Directory.GetCurrentDirectory();
            bool checkWritable = arguments.ContainsKey("/w");
            bool isDotNet = arguments.ContainsKey("/n");
            int writeTime = ParseIntFromArgs(arguments, "/m");
            int priority = ParseIntFromArgs(arguments, "/c");
            writeTime = (writeTime > 0) ? writeTime * -1 : writeTime;

            if (!SetPriority(priority))
            {
                Console.WriteLine($"[-] Argument '/c' value: {priority} is invalid. Integers 0-5 are valid values.");
                return 1;
            }

            if ((!Directory.Exists(searchPath)) && !(File.Exists(searchPath)))
            {
                Console.WriteLine($"[-] Search path doesn't exist: {searchPath}");
                return 1;
            }
            var watch = new Stopwatch();
            watch.Start();
            DoFileChecks(searchPath, searchPattern, checkWritable, writeTime, isDotNet);
            watch.Stop();
            TimeSpan ts = watch.Elapsed;

            Console.WriteLine("");
            Console.WriteLine($"[+] Completed search of {searchPath} in {ts.Seconds} {(ts.Seconds == 1 ? "second" : "seconds")}.");

            return 0;
        }

        // This opens the file to write and immediately closes it.
        // It will throw an exception if it cannot. Outputs if the file is locked, otherwise it just returns null.
        private static string IsWritable(string path)
        {
            try
            {
                File.OpenWrite(path).Close();
                return path;
            }
            catch (Exception ex)
            {
                var errorCode = Marshal.GetHRForException(ex) & ((1 << 16) - 1);
                if (errorCode == 32 || errorCode == 33)
                {
                    return $"[WRITE LOCKED] {path}";
                }
                else
                {
                    return null;
                }
            }
        }
        // Identifies .NET assemblies by attempting to get the assembly name. This isn't perfect as more could be done to handle
        // read locks/etc, but it seems to do well enough.
        // https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assemblyname
        private static string IsDotNet(string path)
        {
            try
            {
                var assembly = AssemblyName.GetAssemblyName(path);
                return path;
            }
            catch
            {
                return null;
            }
        }

        private static Dictionary<string, string> SetArgs(string[] args)
        {
            try
            {
                return args.ToDictionary(
                 k => k.Split(new char[] { ':' }, 2)[0].ToLower(),
                 v => v.Split(new char[] { ':' }, 2).Count() > 1
                                                    ? v.Split(new char[] { ':' }, 2)[1]
                                                    : null); ;
            }
            catch
            {
                return new Dictionary<string, string>();
            }

        }

        // Sets process priority to control OS allication of resources compared to other processes.
        private static bool SetPriority(int priority)
        {
            Process thisProcess = Process.GetCurrentProcess();
            switch (priority)
            {
                case 5:
                    thisProcess.PriorityClass = ProcessPriorityClass.RealTime;
                    return true;
                case 4:
                    thisProcess.PriorityClass = ProcessPriorityClass.High;
                    return true;
                case 3:
                    thisProcess.PriorityClass = ProcessPriorityClass.AboveNormal;
                    return true;
                case 2:
                    thisProcess.PriorityClass = ProcessPriorityClass.Normal;
                    return true;
                case 1:
                    thisProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
                    return true;
                case 0:
                    thisProcess.PriorityClass = ProcessPriorityClass.Idle;
                    return true;
                default:
                    return false;
            }
        }

        // Compares the last file write time to the time this script is run plus the parameter passed.
        private static string HasBeenModified(string path, DateTime? mtime)
        {
            if (File.GetLastWriteTime(path) > mtime)
            {
                return path;
            }
            return null;
        }

        private static int ParseIntFromArgs(Dictionary<string, string> arguments, string key)
        {
            if (arguments.ContainsKey(key))
            {
                try
                {
                    return Convert.ToInt32(arguments[key]);
                }
                catch
                {
                    Console.WriteLine($"[-] Value for '{key}' must be an integer.");
                    return -1;
                }
            }
            return 0;
        }

        // Credit to Marc Gravell for this from StackOverflow. https://stackoverflow.com/a/4986333
        public static void DoFileChecks(string root, string pattern, bool checkWrite, int modtime, bool isDotNet)
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
                            outFile = HasBeenModified(file, modifiedTime);
                        }
                        if (checkWrite)
                        {
                            outFile = IsWritable(outFile);
                            if (String.IsNullOrEmpty(outFile))
                            {
                                continue;
                            }
                            if (outFile.Contains("[WRITE LOCKED]"))
                            {
                                Console.WriteLine(outFile);
                                continue;
                            }
                        }
                        if (isDotNet)
                        {
                            outFile = IsDotNet(outFile);
                        }
                        if (!String.IsNullOrEmpty(outFile))
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

        public static void Usage()
        {
            Console.WriteLine(@"SharpFind.exe A .NET tool to mimic some functionality of the Unix find command");
            Console.WriteLine(@"such finding writable files, recently modified files, and files matching a pattern");
            Console.WriteLine(@"");
            Console.WriteLine(@"Usage: .\SharpFind.exe /p:<absolute-or-relative-path> /e:<search-pattern> /c:<cpu-priority> /m:<minutes-since-last-modification> /w");
            Console.WriteLine(@"Example: .\SharpFind.exe /p:c:\users\they /e:*lolcats.ext* /c:0 /m:10 /w");
            Console.WriteLine(@"");
            Console.WriteLine(@"/p:<path> path to search. Relative or absolute is acceptable");
            Console.WriteLine(@"/e:<search-pattern> * is wildcard");
            Console.WriteLine(@"/c:<cpu-priority> OS CPU priority compared to other proccesses. Valid values are 0-5, 5 being highest priority");
            Console.WriteLine(@"/m:<minutes-since-last-modification> Find files modified in the last n minutes");
            Console.WriteLine(@"/w Only return files wrtitable by the current user. Will show '[WRITE LOCKED]' for files that are locked for writing.");
            Console.WriteLine(@"/n Only return files that are valid.NET assemblies");
        }
    }
}

