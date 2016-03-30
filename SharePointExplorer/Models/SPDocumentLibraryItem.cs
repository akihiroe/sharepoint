using Microsoft.SharePoint.Client;
using SharePointExplorer.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ViewMaker.Core;

namespace SharePointExplorer.Models
{
    [View(typeof(FolderPanelView))]
    public class SPDocumentLibraryItem : SPFolderItem
    {
        public override string Name
        {
            get { return List.Title; }
        }

        public List List
        {
            get { return _list; }
        }
        private List _list;

        public SPDocumentLibraryItem(TreeItem parent, ClientContext context, List list)
            : base(parent, context, list.RootFolder)
        {
            _list = list;
        }

        public override string Icon
        {
            get
            {
                return "/SharePointExplorer;Component/Images/sharepointdoclib.png";
            }
        }

        public override string SPUrl
        {
            get
            {
                var uri = new Uri(Context.Url);
                var root = uri.Scheme + "://" + uri.Host;
                return root + List.DefaultViewUrl;
            }
        }


        protected override void OpenWebSite(object obj)
        {
            Process.Start(SPUrl);
        }

        public override bool AvailableRenameFolder
        {
            get
            {
                return false;
            }
        }

        public override bool AvailableDeleteFolder
        {
            get
            {
                return false;
            }
        }
    }
}
