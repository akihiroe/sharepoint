using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using SharePointExplorer.Models;

namespace SharePointExplorer
{
    public class SharePointUtil :IDisposable
    {
        private ClientContext context;

        public SharePointUtil(string siteUrl, string user, string password)
        {
            var securePassword = new SecureString();
            foreach (char c in password)
            {
                securePassword.AppendChar(c);
            }

            var onlineCredentials = new SharePointOnlineCredentials(user, securePassword);
            context =  new ClientContext(siteUrl);
            context.Credentials = onlineCredentials;
           
        }

        public ListCollection ListDocumentLibrary()
        {
            var web = context.Web;
            context.Load(web);
            context.ExecuteQueryWithIncrementalRetry();

            var lists = web.Lists;
            context.Load(lists, x => x.Include(
                y => y.IsApplicationList,
                y => y.Title,
                y => y.Hidden,
                y => y.BaseType,
                y => y.RootFolder.Name));

            context.ExecuteQueryWithIncrementalRetry();
            return lists;
        }

        public void Dispose()
        {
            context.Dispose();
        }

    }
}
