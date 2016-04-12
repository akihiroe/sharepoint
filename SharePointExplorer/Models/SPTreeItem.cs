using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Search.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SharePointExplorer.Models
{
    public abstract class SPTreeItem : TreeItem
    {
        public virtual ClientContext Context
        {
            get { return _context; }
        }
        private ClientContext _context;

        public FileCacheManager FileCache
        {
            get
            {
                var key = Convert.ToBase64String(Encoding.UTF8.GetBytes(Context.Url));
                if (!_fileCaches.ContainsKey(key))
                {
                    _fileCaches.Add(key, new FileCacheManager(key));
                }
                return _fileCaches[key];
            }
        }
        private static Dictionary<string,FileCacheManager> _fileCaches = new Dictionary<string, FileCacheManager>();

        public abstract string Path { get; }

        public override bool IsBusy
        {
            get { return RootVM.IsBusy; }
            set { RootVM.IsBusy = value;  }
        }

        public override bool CanCanceled
        {
            get { return RootVM.CanCanceled; }
            set { RootVM.CanCanceled = value; }
        }

        public override bool IsCancelled
        {
            get { return RootVM.IsCancelled; }
            set { RootVM.IsCancelled = value; }
        }

        public override string CancelConfirmMessage
        {
            get { return RootVM.CancelConfirmMessage; }
            set { RootVM.CancelConfirmMessage = value; }
        }

        public ICommand OpenWebSiteCommand { get { return CreateCommand(OpenWebSite); } }
        protected virtual void OpenWebSite(object obj)
        {

        }

        public ICommand CopyUrlToClipboardCommand
        {
            get { return this.CreateCommand(CopyUrlToClipboard); }
        }


        private void CopyUrlToClipboard(object arg)
        {
            Clipboard.SetText(SPUrl);
        }

        public ICommand DisconnectCommand { get { return CreateCommand(Disconnect); } }
        protected virtual void Disconnect(object obj)
        {
        }


        public ICommand EditConnectionCommand { get { return CreateCommand(EditConnection); } }
        protected virtual void EditConnection(object obj)
        {
        }

        public ICommand ClearCacheCommand { get { return CreateCommand(ClearCache); } }
        protected virtual void ClearCache(object obj)
        {
            foreach (var item in FileCache.CacheFiles.ToArray())
            {
                if (item.Value.Path.StartsWith(Path))
                {
                    if (item.Value.IsDirty)
                    {
                        var filename = Path.Split('/').Last();
                        if (Confirm(Properties.Resources.MsgConfirm, string.Format(Properties.Resources.MsgConfirmPrurgeCache, filename)))
                        {
                            FileCache.ClearCachedFile(item.Key);
                        }
                    }
                    else
                    {
                        FileCache.ClearCachedFile(item.Key);
                    }

                }
            }
        }

        public ICommand CreateFolderCommand { get { return CreateCommand(CreateFolder); } }
        protected virtual void CreateFolder(object obj)
        {
        }

        public ICommand RenameFolderCommand { get { return CreateCommand(x=>ExecuteActionAsync(RenameFolder(x))); } }
        protected virtual Task RenameFolder(object obj)
        {
            return Task.Delay(0);
        }


        public virtual ICommand MoveFolderCommand { get { return CreateCommand((x) => { }); } } 

        public ICommand DeleteFolderCommand { get {
                return CreateCommand(x => {

                    if (Confirm(Properties.Resources.MsgConfirm, string.Format(Properties.Resources.MsgDeleteConfirm, Name)))
                    {
                        ExecuteActionAsync(DeleteFolder(x), 
                            (t) => {
                                ((SPFolderItem)Parent).Children.Remove(this);
                            });
                    }

                });}
        }
        public virtual Task DeleteFolder(object obj)
        {
            return Task.Delay(0);
        }


        public virtual ICommand DownloadFolderCommand { get { return CreateCommand((x) => { }); } }

        public virtual ICommand UploadFolderCommand { get { return CreateCommand((x) => { }); } }

        public virtual ICommand OpenAsExplorerCommand { get { return CreateCommand((x) => { }); } }

        public virtual bool AvailableRefresh { get { return true; } }
        public virtual bool AvailableOpenWebSite { get { return true; } }
        public virtual bool AvailableOpenAsExplorer { get { return true; } }
        public virtual bool AvailableDisconnect { get { return false; } }
        public virtual bool AvailableEditConnection { get { return false; } }
        public virtual bool AvailableClearCache { get { return Context != null; } }
        public virtual bool AvailableCreateFolder { get { return false; } }
        public virtual bool AvailableRenameFolder { get { return false; } }
        public virtual bool AvailableMoveFolder { get { return false; } }
        public virtual bool AvailableDeleteFolder { get { return false; } }

        public virtual bool AvailableDownloadFolder { get { return false; } }
        public virtual bool AvailableUploadFolder { get { return false; } }

        public bool IsFolderEditing
        {
            get { return _isFolderEditing; }
            set { _isFolderEditing = value; OnPropertyChanged("IsFolderEditing", "IsNotFolderEditing"); }
        }
        private bool _isFolderEditing;

        public bool IsNotFolderEditing
        {
            get { return !_isFolderEditing; }
        }

        public abstract string SPUrl { get; }

        public virtual async Task<SPTreeItem> FindNodeByUrl(string url, bool ensure)
        {
            if (this.SPUrl == url.TrimEnd('/')) return this;
            if (!url.StartsWith(this.SPUrl)) return null;
            if (ensure) await EnsureChildren();
            foreach (var child in Children.OfType<SPTreeItem>())
            {
                var target = await child.FindNodeByUrl(url, ensure);
                if (target != null) return target;
            }
            return null;
        }

        public SPTreeItem(TreeItem parent, ClientContext context):base(parent)
        {
            this._context = context;
        }

        public virtual async Task<List<SPSearchResultFileItem>> Search(object obj)
        {
            ClientResult<ResultTableCollection> results = null;
            await Task.Run(() => {
                RetryAction(() => {
                    var keywordQuery = new KeywordQuery(Context);
                    keywordQuery.QueryText = "IsDocument:1 " + (string)obj;
                    SearchExecutor searchExecutor = new SearchExecutor(Context);
                    results = searchExecutor.ExecuteQuery(keywordQuery);
                    Context.ExecuteQueryWithIncrementalRetry();
                });
            });

            var list = new List<SPSearchResultFileItem>();
            foreach (var query in results.Value)
            {
                foreach (Dictionary<string, object> item in query.ResultRows)
                {
                    list.Add(new SPSearchResultFileItem(this, Context, item));
                }
            }
            return  list;
        }


        protected void Download(string ServerRelativeUrl, string localPath, long totalSize)
        {
            var temp = System.IO.Path.GetTempFileName();
            try
            {
                using (var st = new System.IO.FileStream(temp, System.IO.FileMode.Create))
                {
                    Download(ServerRelativeUrl, st, totalSize);
                }
                System.IO.File.Delete(localPath);
                System.IO.File.Move(temp, localPath);
            }
            finally
            {
                if (System.IO.File.Exists(temp)) System.IO.File.Delete(temp);
            }
        }

        protected void Download(string ServerRelativeUrl, System.IO.Stream st, long totalSize)
        {
            var data = Microsoft.SharePoint.Client.File.OpenBinaryDirect(Context, ServerRelativeUrl);
            var bufferSize = 1024 * 1024;
            var content = new ReadSeekableStream(data.Stream, bufferSize, totalSize, new IDisposable[] { });
            var buffer = new Byte[bufferSize];
            long downloaded = 0;

            while (true)
            {
                if (IsCancelled)
                {
                    throw new OperationCanceledException();
                }
                var length = content.Read(buffer, 0, buffer.Length);
                if (length <= 0) break;

                downloaded += length;
                this.NotifyProgressMessage(string.Format("{0} {1}%", string.Format(Properties.Resources.MsgDownloading, ServerRelativeUrl.Split('/').Last()), downloaded * 100 / totalSize));
                st.Write(buffer, 0, length);
            }
        }


        public virtual ExplorerVM RootVM
        {
            get { return ((SPTreeItem)FindRoot()).RootVM; }
        }

        public void RetryAction(Action action, int maxCount = 5, int delay = 1000)
        {
            for (int retry = 1; retry < maxCount + 1; retry++)
            {
                try
                {
                    action();
                }
                catch (ServerException ex)
                {
                    Trace.WriteLine(ex);
                    if (retry == maxCount) throw;
                    System.Threading.Thread.Sleep(delay);
                }
            }
        }

        protected string GetParentFolder(string path)
        {
            return path.Substring(0, path.Length - path.Split('/').Last().Length);
        }
    }
}
