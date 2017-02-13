using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ViewMaker;
using ViewMaker.Core;

namespace SharePointExplorer.Models
{
    [View("SharePointExplorer.Views.SPSiteView,SharePointExplorer")]
    public class SPSiteItem : SPTreeItem, IDisposable
    {
        private string userScriptName = "SharepointExplorerUserScript";

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
                    _context = CreateContext(siteUrl, User, Password);   
                    OnPropertyChanged("AvailableClearCache");
                }
                return _context;
            }
        }
        private ClientContext _context;
        private string siteUrl;
        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        public string User
        {
            get { return user; }
            set { user = value; }
        }
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

        public Tuple<ClientContext, Web> GenerateContext(string webPath)
        {
            var ret = CreateContext(siteUrl, User, Password);
            var web = ret.Web;
            ret.Load(web);
            ret.ExecuteQueryWithIncrementalRetry();
            var webName = "/";
            foreach (var webNameWork in webPath.Trim('/').Split('/'))
            {
                if (string.IsNullOrEmpty(webNameWork)) continue;
                webName = webName.TrimEnd('/') + "/" + webNameWork;
                ret.Load(web.Webs);
                ret.ExecuteQueryWithIncrementalRetry();
                web = web.Webs.FirstOrDefault(x => x.ServerRelativeUrl == webName);
            }
            return new Tuple<ClientContext, Web>(ret,web);
        }


        public SPSiteItem(ExplorerVM parent, string siteUrl, string user, string password)
            : base(null, null, null)
        {
            this.siteUrl = siteUrl.TrimEnd('/');
            this.User = user;
            this.Password = password;
            this.explorer = parent;
        }


        protected override async Task LoadChildren(int depth=1)
        {
            ListCollection lists = null;
            WebCollection webs = null;
            await Task.Run(() => {
                var web = Context.Web;
                _web = web;
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


                webs = web.Webs;
                Context.Load(webs, x => x.Include(
                    y => y.Title,
                    y => y.Url,
                    y => y.RootFolder.Name,
                    y => y.RootFolder.ServerRelativeUrl));


                Context.ExecuteQueryWithIncrementalRetry();
            });
            foreach (var list in lists.Where(x => !x.IsApplicationList && !x.Hidden && x.Title != "Form Templates" && x.Title != "Customized Reports" && x.Title != "Site Collection Documents" && x.Title != "Site Collection Images" && x.Title != "Images"))
            {
                if (list.BaseType == Microsoft.SharePoint.Client.BaseType.DocumentLibrary)
                {
                    Children.Add(new SPDocumentLibraryItem(this, Web, Context, list));
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
            foreach (var web in webs)
            {
                Children.Add(new SPSubSiteItem(this, web, Context));
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

        protected override void EditUserScript(object obj)
        {
            var dialog = new ScriptEditorVM();
            dialog.Code = GetUserScript();
            this.ShowDialog(dialog, "Editor");
            if (dialog.DialogResult)
            {
                if (!string.IsNullOrWhiteSpace(dialog.Code) && this.Confirm("Confirm", Properties.Resources.MsgConfirmSaveScript))
                {
                    SetUserScript(dialog.Code);
                }
            }
        }

        private string GetUserScript()
        {
            //var webObj = _context.Web;// _context.Site.RootWeb;
            //Context.Load(webObj, w => w.EffectiveBasePermissions);
            //Context.ExecuteQuery();
            var userCustomActions = Context.Site.UserCustomActions;

            Context.Load(userCustomActions);
            Context.ExecuteQuery();
            var action = userCustomActions.Where(x => x.Title == userScriptName).FirstOrDefault();
            if (action == null) return null;
            return action.ScriptBlock;
        }

        private void SetUserScript(string script)
        {
            //var webObj = _context.Web;// _context.Site.RootWeb;
            //Context.Load(webObj, w => w.EffectiveBasePermissions);
            //Context.ExecuteQuery();
            var userCustomActions = Context.Site.UserCustomActions;

            Context.Load(userCustomActions);
            Context.ExecuteQuery();
            var action = userCustomActions.Where(x => x.Title == userScriptName).FirstOrDefault();
            if (action == null)
            {
                action = userCustomActions.Add();
                action.Location = "ScriptLink";
                action.Title = userScriptName; ;
            }
            else
            {
                //空の場合削除する
                if (string.IsNullOrWhiteSpace(script))
                {

                    action.DeleteObject();
                    Context.Load(action);
                    Context.ExecuteQuery();
                    return;
                }
            }
            action.ScriptBlock = script;
            action.Sequence = 1000;
            action.Update();
            Context.ExecuteQuery();
        }

        public override bool AvailableEditUserScript { get { return true; } }

        public void Dispose()
        {
            Context.Dispose();
        }

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
