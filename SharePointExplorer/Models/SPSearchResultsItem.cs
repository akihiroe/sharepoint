using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Search.Query;
using SharePointExplorer.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewMaker.Core;

namespace SharePointExplorer.Models
{
    [View(typeof(SPSearchResultsView))]
    public class SPSearchResultsItem : SPTreeItem
    {
        public override string Name
        {
            get { return SharePointExplorer.Properties.Resources.MsgSearchResults; }
        }

        public override string Path
        {
            get { return null; }
        }

        public ObservableCollection<SPSearchResultFileItem> Items { get; private set; }


        public SPSearchResultFileItem SelectedFile
        {
            get { return _selectedFile; }
            set
            {
                _selectedFile = value;
                OnPropertyChanged("SelectedFile");
                RaiseContextMenuData();
            }
        }
        private SPSearchResultFileItem _selectedFile;

        public void RaiseContextMenuData()
        {
            OnPropertyChanged("CanOpen", "CanDelete", "CanCheckout", "CanCheckin", "CanCancelCheckout", "CanRename");
        }

        public SPSearchResultsItem(TreeItem parent, ClientContext context, IList<SPSearchResultFileItem> results)
            : base(parent, context)
        {
            Items = new ObservableCollection<SPSearchResultFileItem>(results);

        }

        protected override Task LoadChildren()
        {
            return Task.Delay(0);
        }

        public override bool AvailableOpenWebSite { get { return false; } }
        public override bool AvailableRefresh { get { return false; } }

    }
}
