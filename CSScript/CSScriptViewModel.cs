using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Runtime.Serialization;
using System.Windows;
using Microsoft.Win32;

namespace OlegShilo.CSScript
{
    public interface IHostApp
    {
        string CreateAndLoadScript();

        void OpenScript(string file);

        void RefreshCurrentScript();

        void RemoveFromRecentList(RecentScript script);

        void UpdateInRecentList(RecentScript script);
    }

    public class CSScriptViewModel : INotifyPropertyChanged
    {
        public string IncompatibilityErrorMessage { get; set; }

        public Action<Action> ExecuteInGUIThread;

        public CSScriptViewModel()
        {
            Version minCompatibleVersion = System.Version.Parse("3.4.2.0");
            IncompatibilityErrorMessage = string.Format("CS-Script Tools Extension is disabled.\nNo compatible CS-Script version found (min v{0}).", minCompatibleVersion);
            this.VSXVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            CompatibleCSSNotFound = true;

            try
            {
                this.HomeDir = GetCSScriptHomeDir();
                this.Version = GetCSScriptVersion();

                Version installedversion = System.Version.Parse(this.Version);
                CompatibleCSSNotFound = (installedversion < minCompatibleVersion);
            }
            catch { } //will throw if CS-Script is not installed

            if (!CompatibleCSSNotFound)
            {
                RecentScripts = new ObservableCollection<RecentScript>();
                RecentFilesHelper.Instance.RecentFilesChanged += OnRecentFilesChanged;
            }
        }

        public void FetchRecentFiles()
        {
            ThreadPool.QueueUserWorkItem(x => OnRecentFilesChanged());
        }

        static int lastDebgProcessId = 0;
        public void OnDebuggerAttached(EnvDTE.dbgEventReason Reason, ref EnvDTE.dbgExecutionAction ExecutionAction)
        {
            if (Reason == EnvDTE.dbgEventReason.dbgEventReasonBreakpoint)
            {
                try
                {
                    EnvDTE.Debugger debugger = Utils.GetDTE().Debugger;

                    if (debugger.CurrentMode == EnvDTE.dbgDebugMode.dbgBreakMode)
                    {
                        if (debugger.CurrentProgram != null && debugger.CurrentProgram.Process != null && debugger.CurrentProgram.Process.Name.EndsWith("csws.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            int processId = debugger.CurrentProgram.Process.ProcessID;
                            if (processId != lastDebgProcessId)
                            {
                                lastDebgProcessId = processId;

                                bool isJustMyCodeEnabled = ((int)Registry.CurrentUser.GetValue(@"Software\Microsoft\VisualStudio\11.0\Debugger\JustMyCode", 0) == 1);

                                if (isJustMyCodeEnabled)
                                {
                                    ExecutionAction = EnvDTE.dbgExecutionAction.dbgExecutionActionStepInto;
                                }
                                else
                                {
                                    string scriptFile = ReadDebuggingMetadata(processId);
                                    if (scriptFile != null)
                                    {
                                        Utils.GetDTE().ItemOperations.OpenFile(scriptFile);
                                        ExecutionAction = EnvDTE.dbgExecutionAction.dbgExecutionActionStepInto;
                                    }
                                    else
                                    {
                                        MessageBox.Show("Debugger cannot step into the script code automatically.\nPlease either enable 'Just My Code' Debugger option or load the script file (and set the breakpoint) manually.", "CS-Script");
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        static string ReadDebuggingMetadata(int processId)
        {
            string metadataFile = Path.Combine(GetCSScriptTempDir(), @"DbgAttach\" + processId + ".txt");
            if (File.Exists(metadataFile))
            {
                string content = File.ReadAllText(metadataFile);

                //  source:<script file>
                if (content.StartsWith("source:"))
                    return content.Substring("source:".Length);
            }

            return null;
        }

        static object synchObject = new object();

        void OnRecentFilesChanged()
        {
            lock (synchObject)
            {
                if (this.ExecuteInGUIThread == null)
                    return;

                string[] lines = null;

                try
                {
                    lines = File.ReadAllLines(RecentFilesHelper.RecentFilesHistoryFile);

                    this.ExecuteInGUIThread(() =>
                        {
                            RecentScripts.Clear();

                            foreach (RecentScript script in RecentFilesHelper.Instance.GetRecentFiles())
                                RecentScripts.Add(script);
                        });
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.Message);
                }

                //if (lines == null || RecentScripts.Count != lines.Length)
                //    System.Diagnostics.Debug.Assert(false, "Recent file misreading");
            }
        }

        public ObservableCollection<RecentScript> RecentScripts { get; set; }

        public string Version { get; set; }

        public string VSXVersion { get; set; }

        public string HomeDir { get; set; }

        bool busy = false;

        public bool Busy
        {
            get { return busy; }
            set { busy = value; RaisePropertyChanged("Busy"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool CompatibleCSSNotFound { get; set; }

        internal static string GetCSScriptHomeDir()
        {
            return Environment.GetEnvironmentVariable("CSSCRIPT_DIR"); ;
        }

        internal static string GetCSScriptTempDir()
        {
            return Path.Combine(Path.GetTempPath(), "CSSCRIPT");
        }

        static Assembly engineAssembly;

        static public Assembly EngineAssembly
        {
            get
            {
                if (engineAssembly == null)
                    engineAssembly = Assembly.ReflectionOnlyLoad(File.ReadAllBytes(EngineFile));
                return engineAssembly;
            }
            set
            {
                engineAssembly = value;
            }
        }

        static public string EngineFile
        {
            get
            {
                string cssDir = Environment.GetEnvironmentVariable("CSSCRIPT_DIR");

                if (cssDir == null)
                    throw new CSSNotFoundException("CS-Script is not installed");

                return Path.Combine(cssDir, "cscs.exe");
            }
        }

        [Serializable]
        public class CSSNotFoundException : ApplicationException
        {
            public CSSNotFoundException(string message)
                : base(message)
            {
            }
        }

        internal static string GetCSScriptVersion()
        {
            if (!File.Exists(EngineFile))
                throw new Exception("Cannot find CS-Script engine file (cscs.exe)");

            int pos = EngineAssembly.FullName.IndexOf("Version=");
            return EngineAssembly.FullName.Substring(pos + "Version=".Length).Split(",".ToCharArray())[0];
        }

        static string FindDebugLauncher()
        {
            if (File.Exists(Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Lib\debugVS15.0.cs")))
                return "debugVS15.0.cs";
            if (File.Exists(Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Lib\debugVS14.0.cs")))
                return "debugVS14.0.cs";
            if (File.Exists(Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Lib\debugVS13.0.cs")))
                return "debugVS13.0.cs";
            else 
                return null;
        }

        internal static string GenerateScriptVSSolution(string scriptFile)
        {
            string cssDir = Environment.GetEnvironmentVariable("CSSCRIPT_DIR");

            if (cssDir == null)
                throw new Exception("CS-Script is not installed");

            var debug_launcher = FindDebugLauncher();
            if (debug_launcher == null)
                throw new Exception("CS-Script cannot find appropriate debug launcher. Ensure you installed the latest version of CS-Script.");

            string output = Utils.RunApp(Path.Combine(cssDir, "cscs.exe"), "/dbg /nl "+ debug_launcher + " /noide \"" + scriptFile + "\"");

            if (output.StartsWith("Solution File:"))
            {
                return output.Replace("Solution File:", "").Trim();
            }
            else
            {
                throw new Exception("CS-Script cannot generate on-fly VS project");
            }
        }

        internal static Dictionary<string, List<string>> GetScriptDependencies(string scriptFile)
        {
            string cssDir = Environment.GetEnvironmentVariable("CSSCRIPT_DIR");

            if (cssDir == null)
                throw new Exception("CS-Script is not installed");

            var debug_launcher = FindDebugLauncher();
            if (debug_launcher == null)
                throw new Exception("CS-Script cannot find appropriate debug launcher. Ensure you installed the latest version of CS-Script.");

            string output = Utils.RunApp(Path.Combine(cssDir, "cscs.exe"), "/nl /dbg "+ debug_launcher + " /print \"" + scriptFile + "\"");

            var retval = new Dictionary<string, List<string>>
                {
                    { "src", new List<string>() },
                    { "asm", new List<string>() },
                };

            foreach (string line in output.Split("\n\r".ToArray(), StringSplitOptions.RemoveEmptyEntries))
                if (line.ToLower().StartsWith("asm:"))
                {
                    retval["asm"].Add(line.Split(":".ToArray(), 2)[1]);
                }
                else if (line.ToLower().StartsWith("src:"))
                {
                    retval["src"].Add(line.Split(":".ToArray(), 2)[1]);
                }

            return retval;
        }

        internal static string GetMyScriptsDir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CS-Scripts");
        }

        internal static string GetTempScriptsDir()
        {
            return Path.Combine(Path.GetTempPath(), "CSSCRIPT");
        }

        internal static string GenerateNewScriptVSSolution()
        {
            string cssDir = Environment.GetEnvironmentVariable("CSSCRIPT_DIR");

            if (cssDir == null)
                throw new Exception("CS-Script is not installed");

            string srcFile = Path.Combine(cssDir, "lib\\new_script.template");

            var destDir = GetMyScriptsDir();
            var destFile = Path.Combine(destDir, "new script.cs");

            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            int i = 0;
            while (File.Exists(destFile))
            {
                i++;
                destFile = Path.Combine(destDir, string.Format("new script ({0}).cs", i));

                if (i > 20)
                    throw new Exception("Please cleanup '" + destDir + "' folder");
            }

            File.Copy(srcFile, destFile);

            return destFile;
        }
    }
}