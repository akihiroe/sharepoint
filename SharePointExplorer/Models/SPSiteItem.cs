using Microsoft.SharePoint.Client;
using SharePointExplorer.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ViewMaker.Core;

namespace SharePointExplorer.Models
{
    [View(typeof(SPSiteView))]
    public class SPSiteItem : SPTreeItem, IDisposable
    {
        public override string Name
        {
            get { return siteUrl; }
        }

        public override string Path
        {
            get { return Context.Url; }
        }

        private ExplorerVM explorer;

        public override ClientContext Context
        {
            get
            {
                if (_context == null)
                {
                    _context = CreateContext(siteUrl, user, password);
                    OnPropertyChanged("AvailableClearCache");
                }
                return _context;
            }
        }
        private ClientContext _context;
        private string siteUrl;
        private string user;
        private string password;

        private static ClientContext CreateContext(string siteUrl, string user, string password)
        {
            var securePassword = new SecureString();
            foreach (char c in password)
            {
                securePassword.AppendChar(c);
            }

            var onlineCredentials = new SharePointOnlineCredentials(user, securePassword);
            var context = new ClientContext(siteUrl);
            context.Credentials = onlineCredentials;
            return context;

        }
        public SPSiteItem(ExplorerVM parent, string siteUrl, string user, string password)
            : base(null, null)
        {
            this.siteUrl = siteUrl;
            this.user = user;
            this.password = password;
            this.explorer = parent;
        }


        protected override async Task LoadChildren()
        {
            ListCollection lists = null;
            await Task.Run(() => {
                var web = Context.Web;
                Context.Load(web);
                Context.ExecuteQueryWithIncrementalRetry();

                lists = web.Lists;
                Context.Load(lists, x => x.Include(
                    y => y.IsApplicationList,
                    y => y.Title,
                    y => y.Hidden,
                    y => y.BaseType,
                    y => y.DefaultViewUrl,
                    y => y.RootFolder.Name,
                    y => y.RootFolder.ServerRelativeUrl));

                Context.ExecuteQueryWithIncrementalRetry();
            });
            foreach (var list in lists.Where(x => !x.IsApplicationList && !x.Hidden && x.Title != "Form Templates" && x.Title != "Customized Reports" && x.Title != "Site Collection Documents" && x.Title != "Site Collection Images" && x.Title != "Images"))
            {
                if (list.BaseType == Microsoft.SharePoint.Client.BaseType.DocumentLibrary)
                {
                    Children.Add(new SPDocumentLibraryItem(this, Context, list));
                }
                //if (list.BaseType == Microsoft.SharePoint.Client.BaseType.DiscussionBoard)
                //{
                //    Children.Add(new SPDocumentLibraryItem(this, Context, list));
                //}
                //if (list.BaseType == Microsoft.SharePoint.Client.BaseType.GenericList)
                //{
                //    Children.Add(new SPDiscussionBoardItem(this, Context, list));
                //}
            }
        }

        public override string Icon
        {
            get
            {
                return "/SharePointExplorer;Component/Images/sharepointsite.png";
            }
        }

        public override async Task<List<SPSearchResultFileItem>> Search(object obj)
        {
            var list = await base.Search(obj);

            var newList = new List<SPSearchResultFileItem>();
            foreach (var item in list)
            {
                if (item.Path.StartsWith(Context.Url))
                {
                    newList.Add(item);
                }
            }
            return newList;
        }


        protected override void Disconnect(object obj)
        {
            explorer.DisconnectCommand.Execute(this);
        }

        protected override void OpenWebSite(object obj)
        {
            Process.Start(Context.Url);
        }

        public override bool AvailableDisconnect { get { return true; } }


        protected override void EditConnection(object obj)
        {
            explorer.EditConnectionCommand.Execute(this);
        }

        public override bool AvailableEditConnection { get { return true; } }

        public void Dispose()
        {
            Context.Dispose();
        }

        //public override bool IsBusy
        //{
        //    get { return base.IsBusy; }
        //    set
        //    {
        //        base.IsBusy = value;
        //        explorer.IsBusy = value;
        //    }
        //}

        //public override bool CanCanceled
        //{
        //    get { return base.CanCanceled; }
        //    set
        //    {
        //        base.CanCanceled = value;
        //        explorer.CanCanceled = value;
        //    }
        //}

        public override ExplorerVM RootVM
        {
            get { return explorer; }
        }

        public override string SPUrl
        {
            get { return siteUrl; }
        }
    }
}
