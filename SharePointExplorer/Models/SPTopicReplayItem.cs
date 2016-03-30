using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePointExplorer.Models
{
    public class SPTopicReplayItem : TreeItem
    {
        public override string Name
        {
            get { return Item.DisplayName; }
        }

        public string Body
        {
            get { return Item["Body"] as string; }
        }

        public ListItem Item
        {
            get { return _item; }
        }
        private ListItem _item;

        public ClientContext Context
        {
            get { return _context; }
        }
        private ClientContext _context;

        public SPTopicReplayItem(TreeItem parent, ClientContext context, ListItem item) 
            :base(parent)

        {
            this._item = item;
            this._context = context;
        }
    }
}
