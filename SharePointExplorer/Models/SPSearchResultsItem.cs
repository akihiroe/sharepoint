using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Search.Query;
using SharePointExplorer.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ViewMaker.Core;

namespace SharePointExplorer.Models
{
    [View(typeof(SPSearchResultsView))]
    public class SPSearchResultsItem : SPTreeItem
    {
        private ExplorerVM explorer;

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

        public SPSearchResultsItem(ExplorerVM parent, IList<SPSearchResultFileItem> results,SPSiteItem target)
            : base(target, target == null ? null : target.Context)
        {
            Items = new ObservableCollection<SPSearchResultFileItem>(results);
            this.explorer = parent;
        }

        protected override Task LoadChildren()
        {
            return Task.Delay(0);
        }

        public override bool AvailableOpenWebSite { get { return false; } }
        public override bool AvailableRefresh { get { return false; } }

        public override string SPUrl
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ICommand CloseCommand { get { return CreateCommand(Close); } }

        private void Close(object obj)
        {
            explorer.Children.Remove(this);
        }
    }
}
