using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace OlegShilo.CSScript
{
    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    public partial class MyControl : UserControl
    {
        private void SafeInvoke(Action action)
        {
            try
            {
                this.Refresh();
                
                if (Dispatcher.CheckAccess())
                {
                    Model.Busy = true;
                    this.Refresh();
                    action();
                }
                else
                {
                    Model.Busy = true;
                    this.Refresh();
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                try
                {
                    Thread.Sleep(500);
                    if (Dispatcher.CheckAccess())
                        Model.Busy = false;
                    else
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { Model.Busy = false; });
                }
                catch { }
            }
        }

        public IHostApp HostApp { get; private set; }

        public MyControl()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            try
            {
                HostApp = Factory.Create<IHostApp>();

                InitializeComponent();

                RecentFilesHelper.Instance.ExecuteInGUIThread = this.ExecuteInGUIThread;
                this.Model.ExecuteInGUIThread = this.ExecuteInGUIThread;

                this.Model.FetchRecentFiles();
            }
            catch (CSScriptViewModel.CSSNotFoundException)
            {
                //the control already has the status label properly set
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "CS-Script Tools");
            }
        }

        public void ExecuteInGUIThread(Action action)
        {
            if (Dispatcher.CheckAccess())
                action();
            else
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
        }

        private void HistoryItem_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SafeInvoke(() =>
               {
                   dynamic control = sender;
                   var script = (RecentScript)control.DataContext;

                   script.Busy = true;
                   this.Refresh();

                   if (File.Exists(script.File))
                       HostApp.OpenScript(script.File);
                   else if (MessageBox.Show("Script file '" + Path.GetFileName(script.File) + "' does not exist.\nDo you want to delete the reference to it from the Recent list(s)?", "CS-Script", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                       HostApp.RemoveFromRecentList(script);

                   script.Busy = false;
               });
        }

        private void Refresh_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SafeInvoke(() =>
                HostApp.RefreshCurrentScript());
        }

        private void Open_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SafeInvoke(() =>
               {
                   var dlg = new Microsoft.Win32.OpenFileDialog();
                   dlg.RestoreDirectory = true;
                   dlg.DefaultExt = ".cs";
                   dlg.Filter = "CS-Script documents (*.cs)|*.cs";

                   if (dlg.ShowDialog() == true)
                       HostApp.OpenScript(dlg.FileName);
               });
        }

        private void Create_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SafeInvoke(() =>
                HostApp.CreateAndLoadScript());
        }

        private void HomeDir_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SafeInvoke(() =>
                 System.Diagnostics.Process.Start("explorer.exe", "/e,/select,\"" + CSScriptViewModel.EngineFile + "\""));
        }

        private void Samples_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SafeInvoke(() =>
                 System.Diagnostics.Process.Start("explorer.exe", "/e,/select,\"" + System.IO.Path.Combine(CSScriptViewModel.GetCSScriptHomeDir(), "Samples\\Hello.cs") + "\""));
        }

        private void MyScripts_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SafeInvoke(() =>
                 System.Diagnostics.Process.Start("explorer.exe", "/e,\"" + CSScriptViewModel.GetMyScriptsDir() + "\""));
        }

        private void AppData_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SafeInvoke(() =>
                 System.Diagnostics.Process.Start("explorer.exe", "/e,\"" + CSScriptViewModel.GetTempScriptsDir() + "\""));
        }

        private void Config_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SafeInvoke(() =>
                System.Diagnostics.Process.Start(System.IO.Path.Combine(CSScriptViewModel.GetCSScriptHomeDir(), "css_config.exe")));
        }

        private void WebHomePage_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SafeInvoke(() =>
               System.Diagnostics.Process.Start("http://www.csscript.net"));
        }
        private void DownloadCss_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SafeInvoke(() =>
               System.Diagnostics.Process.Start("http://www.csscript.net/CurrentRelease.html"));
        }

        private void Feedback_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SafeInvoke(() =>
               System.Diagnostics.Process.Start("mailto:csscript.support@gmail.com?subject=Feedback"));
        }

        private void BugReport_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SafeInvoke(() =>
               System.Diagnostics.Process.Start("mailto:csscript.support@gmail.com?subject=Bug report"));
        }

        private void Help_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SafeInvoke(() =>
               System.Diagnostics.Process.Start("csws.exe", "help"));
        }

        private void OnlineHelp_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SafeInvoke(() =>
                System.Diagnostics.Process.Start("http://www.csscript.net/Documentation.html"));
        }

        private void Codeproject_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SafeInvoke(() =>
                System.Diagnostics.Process.Start("http://www.codeproject.com/KB/cs/cs-script_for_cp.aspx"));
        }

        private void On_Drop(object sender, System.Windows.DragEventArgs e)
        {
            SafeInvoke(() =>
                {
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        string[] droppedFilePaths = (string[])e.Data.GetData(DataFormats.FileDrop, true);

                        if (droppedFilePaths.Length == 1)
                            HostApp.OpenScript(droppedFilePaths.First());
                    }
                });
        }

        private void On_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
        }

        private void EditRecent_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SafeInvoke(() =>
               System.Diagnostics.Process.Start(RecentFilesHelper.RecentFilesHistoryFile));
        }

        private void OpenScriptFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SafeInvoke(() =>
            {
                dynamic control = sender;
                string scriptFileFolder = Path.GetDirectoryName(control.DataContext.File);

                if (Directory.Exists(scriptFileFolder))
                    System.Diagnostics.Process.Start("explorer.exe", "/e,\"" + scriptFileFolder + "\"");
                else if (MessageBox.Show("Script file folder '" + scriptFileFolder + "' does not exist.\nDo you want to delete the reference to the script from the Recent list(s)?", "CS-Script", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    HostApp.RemoveFromRecentList((RecentScript)control.DataContext);
            });
        }

        private void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SafeInvoke(() =>
            {
                dynamic control = sender;
                HostApp.RemoveFromRecentList((RecentScript)control.DataContext);
            });
        }

        private void OpenScriptMenuItem_Click(object sender, RoutedEventArgs e)
        {
            HistoryItem_MouseDown(sender, null);
        }

        private void NotPinnedGroup_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
            SafeInvoke(() =>
            {
                dynamic control = sender;
                var script = (RecentScript)control.DataContext;
                script.Pinned = true;
                HostApp.UpdateInRecentList(script);
            });
        }

        private void PinnedGroup_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
            SafeInvoke(() =>
            {
                dynamic control = sender;
                var script = (RecentScript)control.DataContext;
                script.Pinned = false;
                HostApp.UpdateInRecentList(script);
            });
        }

        ///// <summary>
        ///// Returns an IVsTextView for the given file path, if the given file is open in Visual Studio.
        ///// </summary>
        ///// <param name="filePath">Full Path of the file you are looking for.</param>
        ///// <returns>The IVsTextView for this file, if it is open, null otherwise.</returns>
        //internal static Microsoft.VisualStudio.TextManager.Interop.IVsTextView GetIVsTextView(string filePath)
        //{
        //    var dte2 = (EnvDTE80.DTE2)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE));
        //    Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte2;
        //    Microsoft.VisualStudio.Shell.ServiceProvider serviceProvider = new Microsoft.VisualStudio.Shell.ServiceProvider(sp);

        //    Microsoft.VisualStudio.Shell.Interop.IVsUIHierarchy uiHierarchy;
        //    uint itemID;
        //    Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame windowFrame;
        //    Microsoft.VisualStudio.Text.Editor.IWpfTextView wpfTextView = null;
        //    if (Microsoft.VisualStudio.Shell.VsShellUtilities.IsDocumentOpen(serviceProvider, filePath, Guid.Empty,
        //                                    out uiHierarchy, out itemID, out windowFrame))
        //    {
        //        // Get the IVsTextView from the windowFrame.
        //        return Microsoft.VisualStudio.Shell.VsShellUtilities.GetTextView(windowFrame);
        //    }

        //    return null;
        //}

        public CSScriptViewModel Model { get { return (CSScriptViewModel)(root.DataContext as ObjectDataProvider).Data; } }

        private void Grid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }

        private void Test_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //Model.Test();
        }
    }
}