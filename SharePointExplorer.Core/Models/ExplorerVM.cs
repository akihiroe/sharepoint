using SharePointExplorer.Models;
using SharePointExplorer.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using ViewMaker;
using ViewMaker.Core;

namespace SharePointExplorer.Models
{
    public class ExplorerVM : AppViewModel
    {
        public ObservableCollection<SPTreeItem> Children { get; set; }

        public SPTreeItem SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                OnPropertyChanged("SelectedItem");               
            }
        }

        private SPTreeItem _selectedItem;

        public bool TargetAll
        {
            get { return _targetAll; }
            set { _targetAll = value; OnPropertyChanged("TargetAll", "SearchModeText"); }
        }
        private bool _targetAll;

        public string SearchModeText
        {
            get { return _targetAll ? Properties.Resources.MsgSeacrhModeAll : Properties.Resources.MsgSearchModeFolder;  }
        }
        public object CurrentContent
        {
            get { return _currentContent; }
            set { _currentContent = value; OnPropertyChanged("CurrentContent"); }
        }
        public object _currentContent;

        public ExplorerVM()
        {
            AppViewModel.TopViewModelInstance = this;
            Children = new ObservableCollection<SPTreeItem>();
        }

        public void LoadSettings()
        {
            if (string.IsNullOrEmpty(ExplorerSettings.Instance.LicenseKey))
            {
                if (!ExplorerSettings.Instance.StartDate.HasValue)
                {
                    ExplorerSettings.Instance.StartDate = DateTime.Now;
                    ExplorerSettings.Instance.Save();
                }
            }
            foreach (var cnct in ExplorerSettings.Instance.Connections.ToArray())
            {
                try
                {
                    var root = new SPSiteItem(this, cnct.SiteUrl, cnct.User, string.IsNullOrEmpty(cnct.Password) ? null : Utils.DecryptedPassword(cnct.Password));
                    Children.Add(root);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }
            }
        }

        public void Connect(string siteUrl, string user, string pass, bool isNew, string oldSiteUrl)
        {
            siteUrl = siteUrl.TrimEnd('/');
            if (oldSiteUrl != null) oldSiteUrl = oldSiteUrl.TrimEnd('/');
            var root = new SPSiteItem(this, siteUrl, user, pass);
            var data = Children.OfType<SPSiteItem>().Where(x => string.Equals(x.Name, oldSiteUrl, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (data != null)
            {
                if (isNew) throw new ApplicationException(Resources.MsgDuplicateConnection);

                Children.Insert(Children.IndexOf(data), root);
                Children.Remove(data);

                var info = ExplorerSettings.Instance.Connections.Where(x => string.Equals(x.SiteUrl.TrimEnd('/'), oldSiteUrl, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                info.User = user;
                info.SiteUrl = siteUrl;
                info.Password = Utils.EncryptedPassword(pass);
            }
            else
            {
                Children.Add(root);
                ExplorerSettings.Instance.Connections.Add(new ConnectionInfo { SiteUrl = siteUrl, User = user, Password = Utils.EncryptedPassword(pass) });
            }
        }

        public ICommand ConnectCommand { get { return CreateCommand(Connect); } }
        private void Connect(object obj)
        {
            var dialog = new ConnectVM(this, true);
            ShowDialog(dialog, "Connect");
        }

        public ICommand SelectedItemChangedCommand
        {
            get { return this.CreateCommand((x) => { ExecuteActionAsync(GenerateContent(x)); }); }
        }

        private async Task GenerateContent(object arg)
        {
            var content = arg as SPTreeItem;
            this._selectedItem = content;
            if (content != null)
            {
                await content.EnsureChildren();
                content.IsExpanded = true;
                this.CurrentContent = ViewUtil.BuildContent(content, true);
            }
            else
            {
                this.CurrentContent = null;
            }
        }

        public ICommand DisconnectCommand { get { return CreateCommand(Disconnect); } }

        private void Disconnect(object obj)
        {
            var site = obj as SPSiteItem;
            if (site != null)
            {
                try
                {
                    site.Dispose();
                }
                catch (Exception)
                {
                }
                Children.Remove(site);
                var info = ExplorerSettings.Instance.Connections.Where(x=> x.SiteUrl.TrimEnd('/') == site.Name).FirstOrDefault();
                if (info != null)
                {
                    ExplorerSettings.Instance.Connections.Remove(info);
                    ExplorerSettings.Instance.Save();
                }
            }
        }


        public ICommand EditConnectionCommand { get { return CreateCommand(EditConnection); } }

        private void EditConnection(object obj)
        {
            var site = obj as SPSiteItem;
            if (site != null)
            {
                var info = ExplorerSettings.Instance.Connections.Where(x => string.Equals(x.SiteUrl.TrimEnd('/'), site.Name)).FirstOrDefault();
                var dialog = new ConnectVM(this, false, info.SiteUrl.TrimEnd('/'), info.User);
                ShowDialog(dialog, "Connect");
                ExplorerSettings.Instance.Save();
            }
        }

        public ICommand ExitCommand
        {
            get
            {
                return CreateCommand((x) =>
                {
                    this.ExecuteViewCommand(ViewCommands.Close);
                });
            }
        }

        public ICommand SearchCommand
        {
            get { return CreateCommand((x)=>ExecuteActionAsync(Search(x))); }
        }

        private async Task Search(object obj)
        {
            if (string.IsNullOrEmpty((string)obj)) return;
            var target = SelectedItem as SPTreeItem;
            if (target != null && target.Context != null)
            {
                await Search(obj, target);
            }
            else
            {
                await SearchAll(obj);
            }
        }

        private async Task Search(object obj, SPTreeItem target)
        {
            var list = await target.Search(obj);
            if (list.Count() == 0)
            {
                ShowMessage(string.Format(Resources.MsgSearchNotFound, obj), "Info");
                return;
            }
            var site = target is SPSearchResultsItem ? target.Parent : target;
            var content = new SPSearchResultsItem(this,  list, site as SPSiteItem);
            var old = this.Children.Where(x => x.Name == content.Name).FirstOrDefault();
            if (old != null) this.Children.Remove(old);
            this.Children.Insert(0, content);
            content.IsSelected = true;
            this.CurrentContent = ViewUtil.BuildContent(content, false);
        }

        public ICommand SearchAllCommand
        {
            get { return CreateCommand((x) => ExecuteActionAsync(SearchAll(x))); }
        }

        private async Task SearchAll(object obj)
        {
            if (string.IsNullOrEmpty((string)obj)) return;
            var listAll = new List<SPSearchResultFileItem>();
            foreach (SPSiteItem target in Children.OfType<SPSiteItem>())
            {
                try
                {
                    var list = await target.Search(obj);
                    listAll.AddRange(list);
                }
                catch (Exception ex)
                {

                    Message = ex.Message;
                }
                
            }
            if (listAll.Count == 0)
            {
                ShowMessage(string.Format(Resources.MsgSearchNotFound, obj), "Info");
                return;
            }
            var content = new SPSearchResultsItem(this, listAll, null);
            var old = this.Children.Where(x => x.Name == content.Name).FirstOrDefault();
            if (old != null) this.Children.Remove(old);
            this.Children.Insert(0, content);
            content.IsSelected = true;
            this.CurrentContent = ViewUtil.BuildContent(content, false);
        }



        public ICommand CancelCommand
        {
            get
            {
                return CreateCommand(() => {
                    if (this.CancelConfirmMessage != null)
                    {
                        if (!Confirm(Properties.Resources.MsgConfirm, this.CancelConfirmMessage))
                        {
                            return;
                        }
                    }
                    IsCancelled = true;
                    CanCanceled = false;
                    ProgressMessage = Resources.MsgCanceling;
                });
            }
        }

        public ICommand ClearCacheCommand
        {
            get { return SelectedItem?.ClearCacheCommand; }
        }

        public ICommand DeleteFolderCommand
        {
            get { return SelectedItem?.DeleteFolderCommand; }
        }


        public async Task<SPTreeItem> FindItemByUrl(string url, bool ensure)
        {
            foreach(var site in Children.OfType<SPSiteItem>())
            {
                if (url.StartsWith(site.SPUrl))
                {
                    var target = await site.FindNodeByUrl(url, ensure);
                    if (target != null) return target;
                }
            }
            return null;
        }

        private JumpList jumpList = new JumpList();

        public void UpdateJumpList()
        {
            jumpList.JumpItems.Clear();
            foreach (var item in FileCacheManager.GteAllCachedFile().Distinct().Where(x=>x.IsDownloaded).OrderByDescending(x=>x.LastAccessTime))
            {
                jumpList.JumpItems.Add(new JumpTask()
                {
                    Title = item.Path.Split('/').LastOrDefault(),
                    Description = item.Path,
                    Arguments = "\"" +item.LocalPath + "\"",
                    WorkingDirectory = System.IO.Path.GetDirectoryName(item.LocalPath),
                    ApplicationPath = Utils.FileExtentionInfo(Utils.AssocStr.Executable, System.IO.Path.GetExtension(item.LocalPath)),
                    IconResourcePath = Utils.FileExtentionInfo(Utils.AssocStr.Executable, System.IO.Path.GetExtension(item.LocalPath)),
                });
            }
            jumpList.Apply();
        }

        public string[] DataFormats
        {
            get { return new string[] { "yyyy/MM/dd HH:mm:ss", "yyyy年MM月dd日 HH時mm分" }; }
        }



    }
}
