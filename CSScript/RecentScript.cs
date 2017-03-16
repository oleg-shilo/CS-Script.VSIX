using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using IO = System.IO;

namespace OlegShilo.CSScript
{
    public class RecentScript : INotifyPropertyChanged
    {
        const string pinnedToken = "<pinned>";

        public RecentScript(string info)
        {
            this.File = info;

            if (info.StartsWith(pinnedToken))
                this.File = info.Substring(pinnedToken.Length);

            Date = IO.File.GetLastWriteTime(this.File);
            Pinned = info.StartsWith(pinnedToken);
        }

        public override string ToString()
        {
            if (this.Pinned)
                return RecentScript.pinnedToken + this.File;
            else
                return this.File;
        }

        object shellIcon;
        public object ShellIcon
        {
            get
            {
                if (shellIcon == null)
                {
                    if (System.IO.File.Exists(this.File))
                    {
                        var shinfo = new NativeMethods.SHFILEINFO();

                        var hImgSmall = NativeMethods.SHGetFileInfo(this.File, 0, ref shinfo,
                                     (uint)Marshal.SizeOf(shinfo),
                                      NativeMethods.SHGFI_ICON |
                                      NativeMethods.SHGFI_SMALLICON);

                        shellIcon = Icon.FromHandle(shinfo.hIcon)
                                        .ToBitmap()
                                        .ToBitmapSource();
                    }
                    else
                        shellIcon = null;
                }
                return shellIcon;
            }
        }

        public string File { get; set; }
        public string Name { get { return System.IO.Path.GetFileName(File ?? ""); } }
        //public string Tooltip { get { return "Open " + File + " in Visual Studio"; } }
        public string Tooltip { get { return File; } }
        //public string Tooltip { get { return "Open file in Visual Studio\n" + File; } }
        public DateTime Date { get; set; }

        bool pinned;

        public bool Pinned
        {
            get
            {
                return pinned;
            }
            set
            {
                pinned = value;
                notPinned = !value;
                RaisePropertyChanged("Pinned");
                RaisePropertyChanged("NotPinned");
            }
        }

        bool notPinned;
        public bool NotPinned
        {
            get
            {
                return notPinned;
            }
            set
            {
                notPinned = value;
                pinned = !value;

                RaisePropertyChanged("Pinned");
                RaisePropertyChanged("NotPinned");
            }
        }

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
    }
}