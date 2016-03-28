﻿#region USINGZ

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Collections.Generic;
using FauxMessages;

#endregion

namespace YAMLParser
{
    public static class MD5
    {
        public static Dictionary<string, string> md5memo = new Dictionary<string, string>();
        public static Dictionary<string, string> srvmd5memo = new Dictionary<string, string>();

        public static string Sum(SrvsFile m)
        {
            if (!srvmd5memo.ContainsKey(m.Name))
            {
                string req = Sum(m.Request);
                string res = Sum(m.Response);
                if (req == null)
                    return null;
                if (res == null)
                    return null;
                srvmd5memo.Add(m.Name, Sum(req, res));
            }
            return srvmd5memo[m.Name];
        }

        public static string Sum(MsgsFile m)
        {
            if (!md5memo.ContainsKey(m.Name))
            {
                string hashme = PrepareToHash(m);
                if (hashme == null)
                    return null;
                md5memo[m.Name] = Sum(hashme);
            }
            return md5memo[m.Name];
        }

        #region BEWARE ALL YE WHOSE EYES GAZE UPON THESE LINES

        private static string PrepareToHash(MsgsFile irm)
        {
            string hashme = irm.Definition.Trim('\n', '\t', '\r', ' ');
            while (hashme.Contains("  "))
                hashme = hashme.Replace("  ", " ");
            while (hashme.Contains("\r\n"))
                hashme = hashme.Replace("\r\n", "\n");
            hashme = hashme.Trim();
            string[] lines = hashme.Split('\n');

            Queue<string> haves = new Queue<string>(), havenots = new Queue<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i];
                if (l.Contains("=")) haves.Enqueue(l);
                else havenots.Enqueue(l);
            }
            hashme = "";
            while (haves.Count + havenots.Count > 0) 
                hashme += (haves.Count > 0 ? haves.Dequeue() : havenots.Dequeue()) + (haves.Count + havenots.Count >= 1 ? "\n" : "");
            Dictionary<string, MsgFieldInfo> mfis = MessageFieldHelper.Instantiate(irm.Stuff);
            MsgFieldInfo[] fields = mfis.Values.ToArray();
            for(int i=0;i<fields.Length;i++)
            {
                if (fields[i].IsLiteral)
                    continue;
                MsgsFile ms = irm.Stuff[i].Definer;
                if (ms == null)
                {
                    KnownStuff.WhatItIs(irm, irm.Stuff[i]);
                    ms = irm.Stuff[i].Definer;
                }
                if (ms == null)
                {
                    Debug.WriteLine("NEEDS ANOTHER PASS: " + irm.Name + " B/C OF " + irm.Stuff[i].Type);
                    return null;
                }
                string sum = MD5.Sum(ms);
                if (sum == null)
                {
                    Debug.WriteLine("STILL NEEDS ANOTHER PASS: " + irm.Name + " B/C OF " + irm.Stuff[i].Type);
                    return null;
                }
                string[] BLADAMN = hashme.Replace(fields[i].Type,sum).Split('\n');
                hashme = "";
                for (int x = 0; x < BLADAMN.Length; x++)
                {
                    if (BLADAMN[x].Contains(fields[i].Name))
                    {
                        if (BLADAMN[x].Contains("/"))
                        {
                            BLADAMN[x] = BLADAMN[x].Split('/')[1];
                        }

                        if (BLADAMN[x].Contains("[]") && !fields[i].IsLiteral)
                        {
                            BLADAMN[x] = BLADAMN[x].Replace("[]", "");
                        }
                    }
                    hashme += BLADAMN[x];
                    if (x < BLADAMN.Length - 1)
                        hashme += "\n";
                }
            }
            Console.WriteLine("\n\n------  TYPE==={0}  ------\n{1}", irm.Name, hashme);
            return hashme;
        }

        #endregion

        public static string Sum(params string[] str)
        {
            return Sum(str.Select(s => Encoding.ASCII.GetBytes(s)).ToArray());
        }

        public static string Sum(params byte[][] data)
        {
            StringBuilder sb = new StringBuilder();
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            if (data.Length > 0)
            {
                for (int i = 0; i < data.Length - 1; i++)
                {
                    md5.TransformBlock(data[i], 0, data[i].Length, data[0], 0);
                }
                md5.TransformFinalBlock(data[data.Length - 1], 0, data[data.Length - 1].Length);
            }
            for (int i = 0; i < md5.Hash.Length; i++)
            {
                sb.AppendFormat("{0:x2}", md5.Hash[i]);
            }
            return sb.ToString();
        }
    }
}