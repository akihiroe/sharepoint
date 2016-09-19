using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using ViewMaker.Core;

namespace SharePointExplorer.Models
{
    [View("SharePointExplorer.Views.ConnectView,SharePointExplorer")]
    public class ConnectVM : AppViewModel
    {
        public string SiteUrl { get; set; }
        public string User { get; set; }

        public bool IsNew { get; set; }

        private ExplorerVM explorer;

        protected string OldSiteUrl { get; set; }

        public ConnectVM(ExplorerVM explorer, bool isNew, string siteUrl = null, string user = null)
        {
            this.explorer = explorer;
            this.IsNew = IsNew;
            this.SiteUrl = this.OldSiteUrl = siteUrl;
            this.User = user;
        }

        public ICommand ConnectCommand
        {
            get
            {
                return CreateCommand((x) =>
                {
                    var passwordBox = x as PasswordBox;
                    explorer.Connect(SiteUrl, User, passwordBox.Password, IsNew, OldSiteUrl);
                    ExplorerSettings.Instance.Save();
                    this.ExecuteViewCommand(ViewCommands.Close);
                });
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                return CreateCommand((x) =>
                {
                    this.ExecuteViewCommand(ViewCommands.Close);
                });
            }
        }

    }
}
