﻿#define NO_SRVS_RIGHT_NOW

#region USINGZ

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Messages;

#endregion

namespace YAMLParser
{
    internal class Program
    {
        public static List<MsgsFile> msgsFiles = new List<MsgsFile>();
        public static List<SrvsFile> srvFiles = new List<SrvsFile>();
        public static string backhalf;
        public static string fronthalf;

        public static string outputdir = "..\\..\\..\\Messages";
        public static string outputdir_firstpass = "..\\..\\..\\TempMessages";
        public static string outputdir_secondpass = "..\\..\\..\\SecondPass";

        private static void Main(string[] args)
        {
            if (args.Length >= 1)
            {
                outputdir = args[0];
            }
            List<string> paths = new List<string>();
            List<string> std = new List<string>();
#if !NO_SRVS_RIGHT_NOW
            List<string> srv = new List<string>();
            List<string> pathssrv = new List<string>();
#endif
            Console.WriteLine
                (
                    "Generatinc C# classes for ROS Messages:\n\tstd_msgs\t\t(in namespace \"Messages\")\n\tgeometry_msgs\t\t(in namespace \"Messages.geometry_msgs\")\n\tnav_msgs\t\t(in namespace \"Messages.nav_msgs\")");
            if (!Directory.Exists("ROS_MESSAGES"))
            {
                Console.WriteLine("the ROS_MESSAGES folder must be in the same folder as the executable!");
                Console.WriteLine("Press Enter");
                Console.ReadLine();
            }
            std.AddRange(Directory.GetFiles("ROS_MESSAGES", "*.msg"));
#if !NO_SRVS_RIGHT_NOW
            srv.AddRange(Directory.GetFiles("ROS_MESSAGES", "*.srv"));
#endif
            foreach (string dir in Directory.GetDirectories("ROS_MESSAGES"))
            {
                std.AddRange(Directory.GetFiles(dir, "*.msg"));
#if !NO_SRVS_RIGHT_NOW
                srv.AddRange(Directory.GetFiles(dir, "*.srv"));
#endif
            }
            if (args.Length == 1)
            {
                paths.AddRange(Directory.GetFiles(".", "*.msg"));
#if !NO_SRVS_RIGHT_NOW
                pathssrv.AddRange(Directory.GetFiles(".", "*.srv"));
#endif
            }
            else
            {
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i].Contains(".msg"))
                        paths.Add(args[i]);
#if !NO_SRVS_RIGHT_NOW
                    else if (args[i].Contains(".srv"))
                        pathssrv.Add(args[i]);
#endif
                    else
                    {
                        string[] paths2 = Directory.GetFiles(args[i], "*.msg");
                        if (paths2.Length != 0)
                            paths.AddRange(paths2);
#if !NO_SRVS_RIGHT_NOW
                        string[] paths3 = Directory.GetFiles(args[i], "*.srv");
                        if (paths3.Length != 0)
                            pathssrv.AddRange(paths3);
#endif
                    }
                }
            }
            foreach (string path in std)
            {
                msgsFiles.Add(new MsgsFile(path));
            }
#if !NO_SRVS_RIGHT_NOW
            foreach (string path in srv)
            {
                srvFiles.Add(new SrvsFile(path));
            }
#endif
            if (paths.Count > 0)
            {
                Console.WriteLine("Custom messages being parsed+generated:");
                foreach (string path in paths)
                {
                    Console.WriteLine("\t" + path.Replace(".\\", ""));
                    msgsFiles.Add(new MsgsFile(path));
                }
            }
#if !NO_SRVS_RIGHT_NOW
            if (pathssrv.Count > 0)
            {
                Console.WriteLine("Custom services being parsed+generated:");
                foreach (string path in pathssrv)
                {
                    Console.WriteLine("\t" + path.Replace(".\\", ""));
                    srvFiles.Add(new SrvsFile(path));
                }
            }
#endif
#if !NO_SRVS_RIGHT_NOW
            if (std.Count + paths.Count + srv.Count + pathssrv.Count > 0)
#else
            if (std.Count + paths.Count > 0)
#endif
            {
                MakeTempDir();
                GenerateFiles(msgsFiles, srvFiles);
                GenerateProject(msgsFiles, srvFiles);
                BuildProject();
                Finalize();
            }
            else
            {
                Console.WriteLine("YOU SUCK AND I HOPE YOU DIE!!!!");
            }
            Console.ReadLine();
        }

        public static void MakeTempDir()
        {
            if (!Directory.Exists(outputdir)) Directory.CreateDirectory(outputdir);
            else
            {
                foreach (string s in Directory.GetFiles(outputdir, "*.cs")) File.Delete(s);
                foreach (string s in Directory.GetDirectories(outputdir))
                    if (s != "Properties")
                        Directory.Delete(s, true);
            }
            if (!Directory.Exists(outputdir_firstpass)) Directory.CreateDirectory(outputdir_firstpass);
            else
            {
                foreach (string s in Directory.GetFiles(outputdir_firstpass, "*.cs")) File.Delete(s);
                foreach (string s in Directory.GetDirectories(outputdir_firstpass))
                    if (s != "Properties")
                        Directory.Delete(s, true);
            }
        }

        public static void GenerateFiles(List<MsgsFile> files, List<SrvsFile> srvfiles)
        {
            foreach (MsgsFile file in files)
            {
                file.Write(outputdir);
                file.Write(outputdir_firstpass);
            }
            foreach (SrvsFile file in srvfiles)
            {
                file.Write(outputdir);
                file.Write(outputdir_firstpass);
            }
            File.WriteAllText(outputdir_firstpass + "\\MessageTypes.cs", ToString());
            File.WriteAllText(outputdir + "\\MessageTypes.cs", ToString());
        }

        public static void GenerateProject(List<MsgsFile> files, List<SrvsFile> srvfiles)
        {
            if (!Directory.Exists(outputdir_firstpass + "\\Properties"))
                Directory.CreateDirectory(outputdir_firstpass + "\\Properties");
            if (!Directory.Exists(outputdir + "\\Properties"))
                Directory.CreateDirectory(outputdir + "\\Properties");
            File.WriteAllLines
                (outputdir_firstpass + "\\Properties\\AssemblyInfo.cs",
                 File.ReadAllLines(Environment.CurrentDirectory + "\\TemplateProject\\AssemblyInfo._cs"));
            File.WriteAllLines
                (outputdir + "\\Properties\\AssemblyInfo.cs",
                 File.ReadAllLines(Environment.CurrentDirectory + "\\TemplateProject\\AssemblyInfo._cs"));

            string[] lines = File.ReadAllLines(Environment.CurrentDirectory + "\\TemplateProject\\TempMessages._csproj");
            string output = "";
            for (int i = 0; i < lines.Length; i++)
            {
                output += "" + lines[i] + "\n";
                if (lines[i].Contains("<Compile Include="))
                {
                    foreach (MsgsFile m in files)
                    {
                        output += "\t<Compile Include=\"" + m.Name.Replace('.', '\\') + ".cs\" />\n";
                    }
                    foreach (SrvsFile m in srvFiles)
                    {
                        output += "\t<Compile Include=\"" + m.Name.Replace('.', '\\') + ".cs\" />\n";
                    }
                    output += "\t<Compile Include=\"SerializationHelper.cs\" />\n";
                    output += "\t<Compile Include=\"Interfaces.cs\" />\n";
                    output += "\t<Compile Include=\"MessageTypes.cs\" />\n";
                }
            }
            File.Copy("TemplateProject\\SerializationHelper.cs", outputdir_firstpass + "\\SerializationHelper.cs");
            File.Copy("TemplateProject\\Interfaces.cs", outputdir_firstpass + "\\Interfaces.cs");
            File.WriteAllText(outputdir_firstpass + "\\TempMessages.csproj", output);
            File.Copy("TemplateProject\\SerializationHelper.cs", outputdir + "\\SerializationHelper.cs");
            File.Copy("TemplateProject\\Interfaces.cs", outputdir + "\\Interfaces.cs");
            File.WriteAllText(outputdir + "\\Messages.csproj", output);
        }

        public static void BuildProject()
        {
            string VCDir = "";
            foreach (
                string dir in
                    Directory.GetDirectories
                        (Environment.GetFolderPath(Environment.SpecialFolder.Windows) +
                         (Environment.Is64BitOperatingSystem
                              ? "\\Microsoft.NET\\Framework64\\"
                              : "\\Microsoft.NET\\Framework")))
            {
                string[] tmp = dir.Split('\\');
                if (tmp[tmp.Length - 1].Contains("v4.0"))
                {
                    VCDir = dir;
                    break;
                }
            }
            string F = VCDir + "\\msbuild.exe";
            Console.WriteLine("\n\nBUILDING GENERATED PROJECT WITH MSBUILD!");
            string args = "/nologo \"" +outputdir_firstpass+"\\TempMessages.csproj\"";
            Process proc = new Process();
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.FileName = F;
            proc.StartInfo.Arguments = args;
            proc.Start();
            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            if (File.Exists(outputdir_firstpass + "\\bin\\Debug\\TempMessages.dll"))
            {
                Console.WriteLine("\n\nGenerated DLL has been copied to:\n\t" + outputdir_firstpass + "\\TempMessages.dll\n\n");
                if (File.Exists(outputdir_firstpass + "\\TempMessages.dll"))
                    File.Delete(outputdir_firstpass + "\\TempMessages.dll");
                File.Copy(outputdir_firstpass + "\\bin\\Debug\\TempMessages.dll", outputdir_firstpass + "\\TempMessages.dll");
            }
            else
            {
                if (output.Length > 0)
                    Console.WriteLine(output);
                if (error.Length > 0)
                    Console.WriteLine(error);
                Console.WriteLine("AMG BUILD FAIL!");
            }
        }

        public static void Finalize()
        {
            string VCDir = "";
            foreach (
                string dir in
                    Directory.GetDirectories
                        (Environment.GetFolderPath(Environment.SpecialFolder.Windows) +
                         (Environment.Is64BitOperatingSystem
                              ? "\\Microsoft.NET\\Framework64\\"
                              : "\\Microsoft.NET\\Framework")))
            {
                string[] tmp = dir.Split('\\');
                if (tmp[tmp.Length - 1].Contains("v4.0"))
                {
                    VCDir = dir;
                    break;
                }
            }
            string F = VCDir + "\\msbuild.exe";
            Console.WriteLine("\n\nBUILDING GENERATED PROJECT WITH MSBUILD!");
            string args = "/nologo \"" + outputdir_secondpass + "\\SecondPass.csproj\"";
            Process proc = new Process();
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.FileName = F;
            proc.StartInfo.Arguments = args;
            proc.Start();
            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            string output2="", error2="";
            if (File.Exists(outputdir_secondpass + "\\bin\\Debug\\SecondPass.exe"))
            {
                Process proc2 = new Process();
                proc2.StartInfo.RedirectStandardOutput = true;
                proc2.StartInfo.RedirectStandardError = true;
                proc2.StartInfo.UseShellExecute = false;
                proc2.StartInfo.CreateNoWindow = true;
                proc2.StartInfo.FileName = outputdir_secondpass + "\\bin\\Debug\\SecondPass.exe";
                proc2.Start();
                output2 = proc2.StandardOutput.ReadToEnd();
                error2 = proc2.StandardError.ReadToEnd();
            }
            else
            {
                if (output.Length > 0)
                    Console.WriteLine(output);
                if (error.Length > 0)
                    Console.WriteLine(error);
                Console.WriteLine("AMG BUILD FAIL!");
            }
            if (output2.Length > 0)
                Console.WriteLine(output2);
            if (error2.Length > 0)
                Console.WriteLine(error2);
            Console.WriteLine("DO SOMETHING HERE TO CHANGE THE FILES IN: outputdir\\Messages.csproj !!!!");
            proc = new Process { StartInfo = { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true, FileName = F, Arguments = "/nologo \"" + outputdir + "\\Messages.csproj\"" } };
            proc.Start();
            string output3 = proc.StandardOutput.ReadToEnd();
            string error3 = proc.StandardError.ReadToEnd();
            if (output3.Length > 0)
                Console.WriteLine(output3);
            if (error3.Length > 0)
                Console.WriteLine(error3);
            Console.WriteLine("FINAL PASS DONE");
        }

        private static string uberpwnage;
        public new static string ToString()
        {
            if (uberpwnage == null)
            {
                if (fronthalf == null)
                {
                    fronthalf = "using Messages;\nusing Messages.std_msgs;\nusing Messages.rosgraph_msgs;\nusing Messages.custom_msgs;\nusing Messages.geometry_msgs;\nusing Messages.nav_msgs;\nusing String=Messages.std_msgs.String;\nusing Messages.roscsharp;\n\nnamespace Messages\n{\n";
                    backhalf = "\n}";
                }

                List<MsgsFile> everything = new List<MsgsFile>(msgsFiles);
                foreach (SrvsFile sf in srvFiles)
                {
                    everything.Add(sf.Request);
                    everything.Add(sf.Response);
                }
                fronthalf += "\n\tpublic enum MsgTypes\n\t{";
                fronthalf += "\n\t\tUnknown,";
                for (int i = 0; i < everything.Count; i++)
                {
                    fronthalf += "\n\t\t";
                    if (everything[i].classname == "Request" || everything[i].classname == "Response")
                        everything[i].Name += "." + everything[i].classname;
                    fronthalf += everything[i].Name.Replace(".", "__");
                    if (i < everything.Count - 1)
                        fronthalf += ",";
                }
                fronthalf += "\n\t}\n";
                uberpwnage = fronthalf + backhalf;
            }
            return uberpwnage;
        }

        public static void GenDict
            (string dictname, string keytype, string valuetype, ref string appendto, int start, int end,
             Func<int, string> genKey, Func<int, string> genVal = null, string DEFAULT = null)
        {
            appendto +=
                string.Format("\n\t\tpublic static Dictionary<{1}, {2}> {0} = new Dictionary<{1}, {2}>()\n\t\t{{",
                              dictname, keytype, valuetype);
            if (DEFAULT != null)
                appendto += "\n\t\t\t{" + DEFAULT + ",\n";
            for (int i = start; i < end; i++)
            {
                if (genVal != null)
                    appendto += string.Format("\t\t\t{{{0}, {1}}}{2}", genKey(i), genVal(i), (i < end - 1 ? ",\n" : ""));
                else
                    appendto += string.Format("\t\t\t{{{0}}}{1}", genKey(i), (i < end - 1 ? ",\n" : ""));
            }
            appendto += "\n\t\t};";
        }
    }
}