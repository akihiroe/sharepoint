using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Search.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace SharePointExplorer.Models
{
    public class SPSearchResultFileItem : SPTreeItem
    {
        public override string Name
        {
            get { return Path.Split('/').LastOrDefault(); }
        }

        public override string Path
        {
            get { return (string)Result["Path"]; }
        }

        public int Size
        {
            get { return (int)Result["Size"]; }
        }

        public DateTime LastModifiedTime
        {
            get { return (DateTime)Result["LastModifiedTime"]; }
        }

        public string Author
        {
            get { return (string)Result["Author"]; }
        }

        public string SiteName
        {
            get { return (string)Result["SiteName"]; }
        }

        public object ResultTextBlock
        {
            get
            {
                var result = HitHighlightedSummary;
                result = result.Replace("<c0>", "<Span Background=\"Yellow\">");
                result = result.Replace("</c0>", "</Span>");
                result = result.Replace("<ddd/>", "...");
                return XamlReader.Parse("<TextBlock xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" TextWrapping=\"Wrap\" >" + result + "</TextBlock>");
            }
        }


        public string HitHighlightedSummary
        {
            get { return (string)Result["HitHighlightedSummary"]; }
        }


        public Dictionary<string,object> Result { get; set; }

        public SPSearchResultFileItem(TreeItem parent, ClientContext context, Dictionary<string, object> result) 
            :base(parent, context)
        {
            this.Result = result;
        }

        public ICommand OpenCommand { get { return CreateCommand((x)=>ExecuteActionAsync(Open(x), null, null, true)); } }

        private async Task Open(object obj)
        {
            var relativePath = Path.Substring(SiteName.Length);
            await Task.Run(() => {
                var pathUser = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var pathDownload = System.IO.Path.Combine(pathUser, "Downloads", relativePath.Split('/').Last());

                if (System.IO.File.Exists(pathDownload))
                {
                    for (int i = 1; i < 10000; i++)
                    {
                        pathDownload = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(pathDownload), System.IO.Path.GetFileNameWithoutExtension(pathDownload) + "(" + i.ToString() +")" + System.IO.Path.GetExtension(pathDownload)); 
                        if (!System.IO.File.Exists(pathDownload)) break;
                    }
                }

                Download(relativePath, pathDownload, Size);
                Process.Start(pathDownload);
            });

        }
    }
}
