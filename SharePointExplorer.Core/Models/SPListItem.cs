using Microsoft.SharePoint.Client;
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

        public SPListItem(TreeItem parent, Web web, ClientContext context, ListItem item)
            : base(parent, web, context)

        {
            this._item = item;
        }

        public override string SPUrl
        {
            get
            {
                throw new NotImplementedException();
            }
        }

    }
}
