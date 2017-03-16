using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EnvDTE;

namespace OlegShilo.CSScript
{
    class RecentFilesHelper
    {
        public event Action RecentFilesChanged;
        static RecentFilesHelper instance;

        public Action<Action> ExecuteInGUIThread;

        static public RecentFilesHelper Instance
        {
            get
            {
                if (instance == null)
                    instance = new RecentFilesHelper();
                return instance;
            }
        }

        public void Synch(Project project, string script)
        {
            if (currentVSProject != project.FullName)
            {
                currentVSProject = project.FullName;
                currentScript = script;

                Reset();
            }
            Synch(script);
        }

        public void RemoveFromRecentList(RecentScript script)
        {
            var scripts = from item in GetRecentFiles()
                          where item.File != script.File
                          select item;

            SetRecentFiles(scripts.ToArray());
        }

        public void UpdateInRecentList(RecentScript script)
        {
            var entries = GetRecentFiles().Select(entry =>
                                                    {
                                                        if (entry.File == script.File)
                                                            entry.Pinned = script.Pinned;

                                                        return entry;
                                                    }).ToArray();

            SetRecentFiles(entries);
        }

        string currentVSProject = "";
        string currentScript = "";
        List<string> CurrentReferences { get; set; }
        List<string> CurrentIncludes { get; set; }

        List<string> LastCheckReferences { get; set; }
        List<string> LastCheckIncludes { get; set; }

        FileSystemWatcher recentFileWatcher;

        private RecentFilesHelper()
        {
            CurrentReferences = new List<string>();
            CurrentIncludes = new List<string>();
            LastCheckReferences = new List<string>();
            LastCheckIncludes = new List<string>();

            var dir = Path.GetDirectoryName(RecentFilesHistoryFile);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(RecentFilesHistoryFile))
                File.Create(RecentFilesHistoryFile);

            recentFileWatcher = new FileSystemWatcher(Path.GetDirectoryName(RecentFilesHistoryFile));
            recentFileWatcher.Filter = Path.GetFileName(RecentFilesHistoryFile);
            recentFileWatcher.EnableRaisingEvents = true;
            recentFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime;
            recentFileWatcher.Created += RecentFilesHistoryChanged;
            recentFileWatcher.Changed += RecentFilesHistoryChanged;
        }

        void RecentFilesHistoryChanged(object sender, FileSystemEventArgs e)
        {
            ExecuteInGUIThread(() =>
                {
                    if (RecentFilesChanged != null)
                        RecentFilesChanged();
                });
        }

        void Reset()
        {
            ExecuteInGUIThread(() =>
                {
                    CurrentReferences.Clear();
                    CurrentIncludes.Clear();
                    LastCheckReferences.Clear();
                    LastCheckIncludes.Clear();
                });
        }

        void Synch(string scriptFile)
        {
            ExecuteInGUIThread(() =>
            {
                Project project = Utils.GetActiveProject();

                Dictionary<string, List<string>> expectedDependencies = CSScriptViewModel.GetScriptDependencies(scriptFile);

                string[] expectedSources = expectedDependencies["src"]
                                                            .ToArray();

                var expectedReferences = expectedDependencies["asm"]
                                                            .Select((file) => new
                                                                                {
                                                                                    Name = Path.GetFileName(file).ToLower(),
                                                                                    File = file
                                                                                })
                                                            .ToArray();

                string[] actualSources = project.GetSources();
                string[] actualReferences = project.GetReferences()
                                                   .Select((file) => Path.GetFileName(file).ToLower())
                                                   .ToArray();

                //source files
                foreach (string expectsdSource in expectedSources)
                    if (!actualSources.Contains(expectsdSource))
                        project.AddFile(expectsdSource);

                foreach (string actualSource in actualSources)
                    if (!expectedSources.Contains(actualSource))
                        project.RemoveFile(actualSource);

                //referenced assemblies
                foreach (var expectsdAsm in expectedReferences)
                    if (!actualReferences.Contains(expectsdAsm.Name))
                        project.AddReference(expectsdAsm.File);

                foreach (string actualAsm in actualReferences)
                {
                    if (string.Compare(actualAsm, "System.dll", true) == 0 ||
                        string.Compare(actualAsm, "mscorlib.dll", true) == 0 ||
                        string.Compare(actualAsm, "System.Core.dll", true) == 0)
                        continue;

                    bool found = false;
                    foreach (var item in expectedReferences)
                        if (item.Name == actualAsm)
                            found = true;

                    if (!found)
                        project.RemoveReference(actualAsm);
                }
            });
        }

        public static string RecentFilesHistoryFile
        {
            get
            {
                return Path.Combine(Path.Combine(Path.GetTempPath(), "CSSCRIPT"), "VS2012_recent.txt");
            }
        }

        public RecentScript[] GetRecentFiles()
        {
            RecentScript[] retval = new RecentScript[0];

            SynchFileAccess(() =>
                {
                    retval = File.ReadAllLines(RecentFilesHistoryFile)
                                 .Select(x => new RecentScript(x))
                                 .ToArray();
                });

            return retval;
        }

        public void SetRecentFiles(RecentScript[] scripts)
        {
            SynchFileAccess(() =>
                {
                    using (StreamWriter sw = new StreamWriter(RecentFilesHistoryFile))
                        foreach (RecentScript script in scripts)
                            sw.WriteLine(script.ToString());
                });
        }

        void SynchFileAccess(Action action)
        {
            using (Mutex mutex = new Mutex(false, "CS-Script_RecentFilesList"))
            {
                mutex.WaitOne(-1);

                action();

                mutex.ReleaseMutex();
            }
        }

        //string[] GetGlobalSearchDirs(string scriptFile)
        //{
        //    var retval = new List<string>();

        //    if (scriptFile != null)
        //        retval.Add(Path.GetDirectoryName(Path.GetFullPath(scriptFile)));

        //    retval.Add(Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\lib"));

        //    Settings settings = GetSystemWideSettings();
        //    if (settings != null)
        //    {
        //        foreach (string dir in settings.SearchDirs.Split(';'))
        //            if (dir != "")
        //                retval.Add(Environment.ExpandEnvironmentVariables(dir));
        //    }

        //    if (CSScript.GlobalSettings != null && CSScript.GlobalSettings.HideAutoGeneratedFiles == Settings.HideOptions.HideAll)
        //        retval.Add(CSSEnvironment.GetCacheDirectory(Path.GetFullPath(scriptFile)));

        //    return (string[])retval.ToArray(typeof(string));

        //}
        public void Analyse(string scriptFile, List<string> includes, List<string> references)
        {
            includes.Clear();
            references.Clear();

            ExecuteInGUIThread(() =>
            {
            });
        }
    }
}