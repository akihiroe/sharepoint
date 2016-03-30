using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SharePointExplorer.Models
{
    public class SharePointAdapter : MarshalByRefObject, IAutoRetry
    {
        ClientContext context;

        public SharePointAdapter()
        {

        }
        public FileInformation OpenBinaryDirect(string serverRelativeUrl)
        {
            return Microsoft.SharePoint.Client.File.OpenBinaryDirect(context, serverRelativeUrl);
        }

        public void LoadQuery(Actio)

        public bool CatchError(MethodBase method, Exception ex, int count)
        {
            throw new NotImplementedException();
        }
    }
}
