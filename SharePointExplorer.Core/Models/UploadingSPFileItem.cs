using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePointExplorer.Models
{
    public class UploadingSPFileItem : SPFileItem
    {
        private string localPath;
        private System.IO.FileInfo info;

        public override string Name
        {
            get { return System.IO.Path.GetFileName(localPath); }
        }

        public override string Owner
        {
            get { return ""; }
        }

        public override string SizeString
        {
            get { return Utils.SizeSuffix(info.Length); }
        }

        public override string CheckedOut
        {
            get { return ""; }
        }

        public override DateTime Modified
        {
            get { return System.IO.File.GetLastWriteTimeUtc(localPath); }
        }

        public UploadingSPFileItem(TreeItem parent, Web web, ClientContext context,string localPath)
            :base(parent, web, context, null)
        {
            this.localPath = localPath;
            this.info = new System.IO.FileInfo(localPath);
        }
    }
}
