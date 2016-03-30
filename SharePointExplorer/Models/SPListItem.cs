using Microsoft.SharePoint.Client;
using SharePointExplorer.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewMaker.Core;

namespace SharePointExplorer.Models
{
    public class SPListItem : SPTreeItem
    {
        public override string Name
        {
            get { return Item.DisplayName; }
        }

        public override string Path
        {
            get { return null; }
        }

        public ListItem Item
        {
            get { return _item; }
        }
        private ListItem _item;

        public SPListItem(TreeItem parent, ClientContext context, ListItem item)
            : base(parent, context)

        {
            this._item = item;
        }
    }
}
