using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using VSLangProj;
using System.Diagnostics;
using System.Collections;

namespace OlegShilo.CSScript
{
    [Serializable]
    internal class InvalidExecutionContextException : Exception
    {
        public InvalidExecutionContextException(string message) : base(message) { }
    }

    public static class Factory
    {
        static Dictionary<Type, Func<object>> creators = new Dictionary<Type, Func<object>>();

        public static T Create<T>()
        {
            Type key = typeof(T);
            if (creators.ContainsKey(key))
                return (T)creators[key]();

            throw new Exception("The type " + key.Name + " is not mapped.");
        }

        public static void Map<T>(Func<object> creator)
        {
            Type key = typeof(T);
            if (creators.ContainsKey(key))
                throw new Exception("The type " + key.Name + " is already mapped.");

            creators.Add(key, creator);
        }

        public static void Map<T1, T2>()
        {
            Type key = typeof(T1);
            if (creators.ContainsKey(key))
                throw new Exception("The type " + key.Name + " is already mapped.");

            creators.Add(key, () => Activator.CreateInstance<T2>());
        }
    }

    static class Utils
    {
        internal static DTE2 GetDTE()
        {
            return (DTE2)Package.GetGlobalService(typeof(SDTE));
        }

        internal static string RunApp(string app, string args)
        {
            var output = new StringBuilder();

            using (var p = new System.Diagnostics.Process())
            {
                p.StartInfo.FileName = app;
                p.StartInfo.Arguments = args;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();

                string line = null;
                while (null != (line = p.StandardOutput.ReadLine()))
                {
                    output.AppendLine(line);
                }
                p.WaitForExit();
                return output.ToString();
            }
        }

        static public Project GetActiveProject()
        {
            var projects = (GetDTE().ActiveSolutionProjects as Array).Cast<Project>().ToArray();
            //foreach (Project item in projects)
            //    item.PrintItems();

            if (projects.Count() == 0)
                throw new InvalidExecutionContextException("There is no project opened");

            return projects.FirstOrDefault();
        }

        static public string[] GetActiveProjectSources()
        {
            return GetActiveProject().AllFiles();
        }

        static public string[] GetActiveProjectReferences()
        {
            var retval = new List<string>();

            foreach (Reference item in GetActiveProject().AsVSProject().References)
                retval.Add(item.Path);

            return retval.ToArray();
        }
    }
}