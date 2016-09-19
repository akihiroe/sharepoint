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
    public class SPSubSiteItem : SPTreeItem, IDisposable
    {
        private string userScriptName = "SharepointExplorerUserScript";

        public override string Name
        {
            get { return Web.Title; }
        }

        public override string Path
        {
            get { return Web.Url; }
        }


        public SPSubSiteItem(TreeItem parent, Web web, ClientContext context)
            : base(parent, web, context)
        {
        }


        protected override async Task LoadChildren(int depth=1)
        {
            ListCollection lists = null;
            WebCollection webs = null;
            await Task.Run(() => {
                var web = Web;
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
                if (item.Path.StartsWith(Web.Url))
                {
                    newList.Add(item);
                }
            }
            return newList;
        }


        protected override void OpenWebSite(object obj)
        {
            Process.Start(Web.Url);
        }

        public void Dispose()
        {
            Context.Dispose();
        }


        public override string SPUrl
        {
            get { return Web.Url; }
        }

    }
}
