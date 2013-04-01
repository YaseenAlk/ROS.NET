﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages;
using System.IO;
using System.Threading;

namespace SecondPass
{
    class Program
    {
        static void Main(string[] args)
        {
            string output = "<MD5>\n";
            string source = args.Length > 0 ? args[0] : "..\\..\\..\\Messages\\";
            foreach (MsgTypes mt in Enum.GetValues(typeof(MsgTypes)))
            {
                if (mt == MsgTypes.Unknown) continue;
                output += mt + "\t" + MD5.Sum(mt) + "\n";
            }
            output += ("</MD5>\n");
            Console.WriteLine(output);
            Dictionary<string, string> output2 = SecondPassHelper.ParseDisString(output);
            string path;
            string lines = null;
            foreach (KeyValuePair<string, string> kvp in output2)
            {
                string md5 = kvp.Value;
                path = source + (kvp.Key.Replace("__", "\\") + ".cs");
                           
              
                try
                {
                    lines = File.ReadAllText(path);
                    lines = lines.Replace("$MYMD5SUM", md5);
                    if (lines.Contains("Request"))
                        lines = lines.Replace("$REQUESTMYMD5SUM", md5);
                    if (lines.Contains("Response"))
                        lines = lines.Replace("$RESPONSEMYMD5SUM", md5);
                    File.WriteAllText(path, lines);
                    Thread.Sleep(10);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }   
            Console.WriteLine("Done");
        }

        public class SecondPassHelper
        {
            static string[] Cases = { "MD5" };

            public static Dictionary<string, string> ParseDisString(string DaString)
            {
                Dictionary<string, string> MD5Pairs = new Dictionary<string, string>();
                string state_string = "";
                string tag = "";

                string[] lines = DaString.Split('\n');
                for (int i = 0; i < lines.Count(); i++)
                {

                    if (lines[i].StartsWith("<") && lines[i].EndsWith(">"))
                    {
                        tag = lines[i].Substring(1, lines[i].Count() - 2);
                        if (tag.StartsWith("/"))
                        {
                            tag = tag.Substring(1, tag.Count() - 1);
                            if (state_string == tag)
                            {
                                //Close out the state
                                Console.WriteLine("Tag Close found");
                                state_string = "";
                                continue;
                            }
                            else
                                Console.WriteLine("Inaproriate CLoser");
                        }
                    }
                    else if (state_string == "")
                    {
                        Console.WriteLine("Inaproriate Opener");
                    }

                    if (Cases.Contains(tag) && state_string != tag)
                    {
                        state_string = tag;
                        continue;
                    }
                    else if (Cases.Contains(state_string))
                    {
                        switch (state_string)
                        {
                            case "MD5":
                                string[] pair = lines[i].Split('\t');
                                if (pair.Count() != 2)
                                {
                                    Console.WriteLine("Invalid MD5 on line #" + i);
                                    break;
                                }
                                else
                                {
                                    MD5Pairs.Add(pair[0], pair[1]);
                                    break;
                                }
                            default:
                                Console.WriteLine("You shouldn't be here, case not accounted for");
                                break;


                        }
                    }
                }
                return MD5Pairs;
            }
        }
    }
}