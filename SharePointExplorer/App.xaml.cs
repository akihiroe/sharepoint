using SharePointExplorer.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ViewMaker;

namespace SharePointExplorer
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!Directory.Exists(Utils.ApplicationFolder)) Directory.CreateDirectory(Utils.ApplicationFolder);
            foreach (var file in Directory.GetFiles(Utils.ApplicationFolder, "SharePointExplorer_*.log"))
            {
                var dateString = Path.GetFileNameWithoutExtension(file).Replace("SharePointExplorer_", "");
                DateTime d;
                if (DateTime.TryParseExact(dateString, "yyyyMMdd", null, DateTimeStyles.None, out d))
                {
                    if (d < DateTime.Today.AddDays(-60)) File.Delete(file);
                }
            }

            DefaultTraceListener drl;
            drl = (DefaultTraceListener)Trace.Listeners["Default"];
            //LogFileNameを変更する
            drl.LogFileName = Utils.ApplicationFolder + "\\SharePointExplorer_" + DateTime.Now.ToString("yyyyMMdd") + ".log";
            Trace.WriteLine("*************************************");
            Trace.WriteLine(DateTime.Now.ToString());
            Trace.WriteLine(Environment.MachineName);
            Trace.WriteLine("*************************************");


        }
    }
}
