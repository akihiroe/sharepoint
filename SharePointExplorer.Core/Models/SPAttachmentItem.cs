using Microsoft.SharePoint.Client;
using SharePointExplorer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePointExplorer.Models
{
    public class SPAttachmentItem : SPTreeItem
    {

        public override string Name
        {
            get { return attachment.FileName; }
        }

        public override string Path
        {
            get { return attachment.ServerRelativeUrl; }
        }

        private Attachment attachment;

        public SPAttachmentItem(TreeItem parent, Attachment attach, Web web, ClientContext context)
            : base(parent, web, context)
        {
            this.attachment = attach;
        }

        public void Dispose()
        {
            Context.Dispose();
        }


        public override string SPUrl
        {
            get
            {
                var uri = new Uri(Context.Url);
                var root = uri.Scheme + "://" + uri.Host;
                return root + attachment.ServerRelativeUrl;
            }
        }

    }
}
