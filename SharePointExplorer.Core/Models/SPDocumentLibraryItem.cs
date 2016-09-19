using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ViewMaker.Core;

namespace SharePointExplorer.Models
{
    [View("SharePointExplorer.Views.FolderPanelView,SharePointExplorer")]
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

        public SPDocumentLibraryItem(TreeItem parent, Web web, ClientContext context, List list)
            : base(parent, web, context, list.RootFolder)
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
                return root + List.RootFolder.ServerRelativeUrl;
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
