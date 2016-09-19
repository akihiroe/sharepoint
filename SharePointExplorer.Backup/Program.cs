using SharePointExplorer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharePointExplorer.Backup
{
    class Program
    {
        static void Main(string[] args)
        {
            var dictionary = new Dictionary<string, string>();
            for (int index = 0; index < args.Length - 1; index += 2)
            {
                dictionary.Add(args[index].ToLower(), args[index + 1]);
            }

            if (dictionary.ContainsKey("/backup"))
            {
                var targetLocal = dictionary["/backup"];
                if (!dictionary.ContainsKey("/sharepoint"))
                {
                    Console.WriteLine("/sharepoint is missing");
                    Environment.Exit(-1);
                }
                if (!dictionary.ContainsKey("/user"))
                {
                    Console.WriteLine("/user is missing");
                    Environment.Exit(-1);
                }
                if (!dictionary.ContainsKey("/password"))
                {
                    Console.WriteLine("/password is missing");
                    Environment.Exit(-1);
                }
                if (!dictionary.ContainsKey("/spfolder"))
                {
                    Console.WriteLine("/spfolder is missing");
                    Environment.Exit(-1);
                }
                var spFolder = dictionary["/spfolder"].TrimEnd('/');
                var sharepoint = dictionary["/sharepoint"];
                var user = dictionary["/user"];
                var password = dictionary["/password"];
                var force = false;
                if (dictionary.ContainsKey("/force"))
                {
                    force = bool.Parse(dictionary["/force"]);
                }
                var mainVm = new CommandlineExploreVM();
                var root = new SPSiteItem(mainVm, sharepoint, user, password);
                mainVm.Children.Add(root);
                for (int i = 0; i < 6 * 60; i++)
                {
                    if (GetIsNetworkAvailable(new Uri(sharepoint).Host))
                    {
                        ExecBackup(targetLocal, spFolder, force, mainVm);
                        Environment.Exit(0);
                    }
                    Thread.Sleep(10000);
                }
                Console.WriteLine( sharepoint + " is not available");
                Environment.Exit(-1);

            }else
            {
                Console.WriteLine("option");
                Console.WriteLine("  /backup \"local file path\"");
                Console.WriteLine("  /sharepoint \"sharepoint site url\"");
                Console.WriteLine("  /user \"sharepoint user\"");
                Console.WriteLine("  /password \"password\"");
                Console.WriteLine("  /spfolder \"sharepoint folder url\"");
                Console.WriteLine("  /force true|false (optional)");
                Console.WriteLine("");
                Console.WriteLine("example");
                Console.WriteLine("  /backup C:\\Users\\username\\Documents /spfolder \"https://yourcompany-my.sharepoint.com/personal/username_yourcompany_onmicrosoft_com/Documents/Backup/Documents\" /sharepoint https://yourcompany-my.sharepoint.com/personal/username_yourcompany_onmicrosoft_com /user yourname@yourcompany.onmicrosoft.com /password \"userpassword\"");
            }

        }

        private static void ExecBackup(string targetLocal, string spFolder, bool force, CommandlineExploreVM mainVm)
        {
            var f = (SPFolderItem)mainVm.FindItemByUrl(spFolder, true).Result;
            if (f == null)
            {
                Console.WriteLine("not found " + spFolder);
                Environment.Exit(-1);
            }
            f.EnsureUploadedDb();
            Trace.Listeners.Add(new ConsoleTraceListener());
            Trace.WriteLine("Backup start " + DateTime.Now.ToString());
            f.Backup(targetLocal, !force).Wait();
            Trace.WriteLine("Backup complete " + DateTime.Now.ToString());
        }

        private static bool GetIsNetworkAvailable(string host)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return false;
            try
            {
                var testDns = System.Net.Dns.GetHostEntry(host);
                if (testDns.AddressList.Length > 0) return true;
                return false;
            }
            catch
            {
                return false;
            }
        }


        private class CommandlineExploreVM : ExplorerVM
        {
            protected override bool Confirm(string title, string message)
            {
                return true;
            }

        }

    }
}
