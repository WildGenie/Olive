﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Olive.ApiProxy
{
    class Context
    {
        public static string PublisherService, ControllerName;
        public static FileInfo AssemblyFile;
        public static DirectoryInfo TempPath, Output;
        public static Assembly Assembly;
        public static Type ControllerType;
        public static MethodGenerator[] ActionMethods;

        internal static void PrepareOutputDirectory()
        {
            if (!TempPath.Exists)
                throw new Exception("Output directory not found: " + TempPath.FullName);

            TempPath = new DirectoryInfo(Path.Combine(TempPath.FullName, ControllerName));

            try
            {
                if (TempPath.Exists)
                    TempPath.Delete(recursive: true, harshly: true).Wait();
                TempPath.Create();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to delete the previous output directory " +
                    TempPath.FullName + Environment.NewLine + ex.Message);
            }
        }

        internal static void Run(string command)
        {
            var cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = false;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            cmd.StandardInput.WriteLine(command);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            var result = cmd.StandardOutput.ReadToEnd().ToStringOrEmpty().Trim();

            if (result.StartsWith("Could not ")) throw new Exception(result);

            if (result.Contains("Build FAILED")) throw new Exception(result.TrimBefore("Build FAILED"));
        }

        internal static void LoadAssembly()
        {
            if (!AssemblyFile.Exists)
                throw new Exception("File not found: " + AssemblyFile.FullName);

            Assembly = Assembly.LoadFrom(AssemblyFile.FullName);
            ControllerType = Assembly.GetType(ControllerName)
                ?? throw new Exception(ControllerName + " was not found.");

            ActionMethods = ControllerType
                .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
                .Select(x => new MethodGenerator(x))
                .ToArray();

            if (ActionMethods.Length == 0) throw new Exception("This controller has no action method.");
        }
    }
}