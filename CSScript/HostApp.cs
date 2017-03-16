using System;
using System.Linq;
using System.Windows;
using EnvDTE;
using IO = System.IO;

namespace OlegShilo.CSScript
{
    public class HostApp : IHostApp
    {
        public string CreateAndLoadScript()
        {
            string scriptFile = CSScriptViewModel.GenerateNewScriptVSSolution();
            string solutionFile = CSScriptViewModel.GenerateScriptVSSolution(scriptFile);
            Utils.GetDTE().Solution.Open(solutionFile);
            //Utils.GetDTE().ExecuteCommand("File.OpenFile", "\"" + scriptFile + "\"");
            Utils.GetDTE().ItemOperations.OpenFile(scriptFile); //zos
            return solutionFile;
        }

        public void OpenScript(string file)
        {
            string prevSolutionFile = Utils.GetDTE().Solution.FileName;
            string solutionFile = CSScriptViewModel.GenerateScriptVSSolution(file);
            //RecentFilesHelper.Instance.Synch(project, scriptFile);

            Utils.GetDTE().Solution.Open(solutionFile);
            //Utils.GetDTE().ExecuteCommand("File.OpenFile", "\"" + file + "\"");
            Utils.GetDTE().ItemOperations.OpenFile(file);

            ReleaseSolution(prevSolutionFile);
            LockSolution(solutionFile);
        }

        void ReleaseSolution(string solutionFile)
        {
            try
            {
                if (!string.IsNullOrEmpty(solutionFile))
                {
                    string pidFile = IO.Path.Combine(IO.Path.GetDirectoryName(solutionFile), "Host.pid");

                    if (IO.File.Exists(pidFile))
                    {
                        int pid = int.Parse(IO.File.ReadAllText(pidFile));

                        if (System.Diagnostics.Process.GetCurrentProcess().Id == pid) //our script
                        {
                            IO.Directory.Delete(IO.Path.GetDirectoryName(solutionFile), true);
                        }
                    }
                }
            }
            catch { }
        }

        void LockSolution(string solutionFile)
        {
            try
            {
                if (!string.IsNullOrEmpty(solutionFile))
                {
                    string pidFile = IO.Path.Combine(IO.Path.GetDirectoryName(solutionFile), "Host.pid");
                    IO.File.WriteAllText(pidFile, System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
                }
            }
            catch { }
        }

        public void RefreshCurrentScript()
        {
            try
            {
                //MyScript.cs -> MyScript (script).csproj

                // System.Diagnostics.Debug.Assert(false);
                Project project = Utils.GetActiveProject();
                
                foreach (var item in project.AllItems())
                    if (item.IsDirty)
                        item.Save();

                Utils.GetActiveProjectReferences();

                var scriptFile = (from item in project.AllFiles()
                                  where (System.IO.Path.GetFileName(item) + "proj") == System.IO.Path.GetFileName(project.FullName).Replace(" (script)", "")
                                        && project.FullName.Contains("CSSCRIPT")
                                  select item).FirstOrDefault();

                if (scriptFile == null)
                {
                    MessageBox.Show("No CS-Script file/project is loaded.");
                }
                else
                {
                    RecentFilesHelper.Instance.Synch(project, scriptFile);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void RemoveFromRecentList(RecentScript script)
        {
            RecentFilesHelper.Instance.RemoveFromRecentList(script);
        }

        public void UpdateInRecentList(RecentScript script)
        {
            RecentFilesHelper.Instance.UpdateInRecentList(script);
        }
    }
}