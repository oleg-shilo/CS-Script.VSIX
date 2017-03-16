using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using EnvDTE;
using VSLangProj;
using System.Windows.Threading;

namespace OlegShilo.CSScript
{
    static class Extensions
    {
        private static Action EmptyDelegate = delegate() { };

        public static void Refresh(this UIElement uiElement)
        {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }

        public static string GetValue(this EnvDTE.Properties source, string name)
        {
            string result = string.Empty;

            if (source == null || System.String.IsNullOrEmpty(name))
                return result;

            EnvDTE.Property property = source.Item(name);
            if (property != null)
            {
                return property.Value.ToString();
            }
            return result;
        }

        public static bool ContainsFile(this Project project, string file)
        {
            int iterator = 0;
            var projectList = new List<ProjectItem>();

            foreach (ProjectItem item in project.ProjectItems)
                projectList.Add(item);

            while (iterator < projectList.Count)
            {
                var projectItem = projectList[iterator];
                //if (projectItem.Kind == "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}")
                {
                    try
                    {
                        if (file == projectItem.Properties.GetValue("FullPath"))
                            return true;
                    }
                    catch { }
                }
                //else
                {
                    foreach (ProjectItem item in projectItem.ProjectItems)
                        projectList.Add(item);
                }
                iterator++;
            }

            return false;
        }

        public static BitmapSource ToBitmapSource(this System.Drawing.Bitmap source)
        {
            BitmapSource bitSrc = null;

            var hBitmap = source.GetHbitmap();

            try
            {
                bitSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                                                        hBitmap,
                                                                        IntPtr.Zero,
                                                                        Int32Rect.Empty,
                                                                        BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Win32Exception)
            {
                bitSrc = null;
            }
            finally
            {
                NativeMethods.DeleteObject(hBitmap);
            }

            return bitSrc;
        }

        public static void PrintItems(this Project project)
        {
            int iterator = 0;
            var projectList = new List<ProjectItem>();

            foreach (ProjectItem item in project.ProjectItems)
                projectList.Add(item);

            while (iterator < projectList.Count)
            {
                var projectItem = projectList[iterator];
                //if (projectItem.Kind == "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}")
                {
                    try
                    {
                        Trace.WriteLine(projectItem.Properties.GetValue("FullPath"));
                    }
                    catch { Trace.WriteLine("Cannot display the item"); }
                }
                //else
                {
                    foreach (ProjectItem item in projectItem.ProjectItems)
                        projectList.Add(item);
                }
                iterator++;
            }
        }

        public static string[] AllFiles(this Project project)
        {
            var retval = new List<string>();
            foreach (var item in project.AllItems())
                try
                {
                    retval.Add(item.Properties.GetValue("FullPath"));
                }
                catch { }
            return retval.ToArray();
        }

        public static string[] GetSources(this Project project)
        {
            return project.AllFiles();
        }

        public static string[] GetReferences(this Project project)
        {
            var retval = new List<string>();

            foreach (Reference item in project.AsVSProject().References)
                retval.Add(item.Path);

            return retval.ToArray();
        }

        public static void AddReference(this Project project, string assembly)
        {
            project.AsVSProject().References.Add(assembly);
        }

        public static void RemoveReference(this Project project, string assembly)
        {
            foreach (Reference item in project.AsVSProject().References)
            {
                if (item.Path.ToLower().EndsWith(assembly))
                    item.Remove();
            }
        }

        public static void RemoveFile(this Project project, string file)
        {
            foreach (ProjectItem item in project.ProjectItems)
                try
                {
                    if (item.Properties.GetValue("FullPath") == file)
                    {
                        item.Remove();
                        break;
                    }
                }
                catch { }
        }

        public static void AddFile(this Project project, string file)
        {
            project.ProjectItems.AddFromFile(file);
        }

        public static VSProject AsVSProject(this Project project)
        {
            return (VSProject)Utils.GetActiveProject().Object;
        }

        public static ProjectItem[] AllItems(this Project project)
        {
            int iterator = 0;
            var projectList = new List<ProjectItem>();

            foreach (ProjectItem item in project.ProjectItems)
                projectList.Add(item);

            while (iterator < projectList.Count)
            {
                var projectItem = projectList[iterator];
                try
                {
                    Trace.WriteLine(projectItem.Properties.GetValue("FullPath"));
                }
                catch { }

                foreach (ProjectItem item in projectItem.ProjectItems)
                    projectList.Add(item);

                iterator++;
            }
            return projectList.ToArray();
        }
    }
}