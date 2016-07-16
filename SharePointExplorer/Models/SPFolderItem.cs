using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Search.Query;
using SharePointExplorer.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ViewMaker.Core;

namespace SharePointExplorer.Models
{
    [View(typeof(FolderPanelView))]
    public class SPFolderItem : SPTreeItem
    {
        public override string Name
        {
            get { return Folder.Name; }
        }

        public string NewName
        {
            get { return _newName; }
            set { _newName = value; OnPropertyChanged("NewName"); }
        }
        private string _newName;

        public override string Path
        {
            get { return Folder.ServerRelativeUrl; }
        }

        public Folder Folder
        {
            get { return _folder; }
        }
        private Folder _folder;

        public ObservableCollection<SPFileItem> Items { get; private set; }

        public override string SPUrl
        {
            get
            {
                var uri = new Uri(Context.Url);
                var root = uri.Scheme + "://" + uri.Host;
                return root + Folder.ServerRelativeUrl;
            }
        }


        public SPFileItem SelectedFile
        {
            get { return _selectedFile; }
            set
            {
                _selectedFile = value;
                OnPropertyChanged("SelectedFile");
                RaiseContextMenuData();
            }
        }
        private SPFileItem _selectedFile;

        public void RaiseContextMenuData()
        {
            OnPropertyChanged("CanOpen", "CanDelete", "CanCheckout", "CanCheckin", "CanCancelCheckout", "CanRename");
        }

        public override bool IsCancelled
        {
            get { return base.IsCancelled; }
            set
            {
                base.IsCancelled = value;
                foreach (var item in Items)
                {
                    item.IsCancelled = value;
                }
                if (newFile != null) newFile.IsCancelled = value;
            }
        }

        public bool IsEditing
        {
            get { return _isEditing; }
            set { _isEditing = value; OnPropertyChanged("IsEditing", "IsNotEditing"); }
        }
        private bool _isEditing;

        public bool IsNotEditing
        {
            get { return !_isEditing; }
        }


        public SPFolderItem(TreeItem parent, ClientContext context, Folder folder)
            : base(parent, context)
        {
            Items = new ObservableCollection<SPFileItem>();
            _folder = folder;
        }


        protected override async Task LoadChildren()
        {
            Children.Clear();
            await Task.Run(() => {

                RetryAction(() => {
                    Context.Load(Folder, x=>x.Name, x=>x.ServerRelativeUrl, x=>x.Folders.Include(
                        y => y.Name,
                        y => y.ServerRelativeUrl)
                    );
                    Context.ExecuteQueryWithIncrementalRetry();
                });
            });

            foreach (var subFolder in Folder.Folders.OrderBy(x=>x.Name))
            {
                if (subFolder.Name == "Forms") continue;
                Children.Add(new SPFolderItem(this, Context, subFolder));
            }

            Items.Clear();
            IEnumerable<Microsoft.SharePoint.Client.File> files = null;
            await Task.Run(() => {
                RetryAction(() => {
                    try
                    {
                        files = Context.LoadQuery(Folder.Files.Include(
                            x => x.UniqueId,
                            x => x.Name,
                            x => x.ServerRelativeUrl,
                            x => x.TimeLastModified,
                            x => x.ModifiedBy,
                            x => x.CheckOutType,
                            x => x.CheckedOutByUser,
                            x => x.Length));
                        Context.ExecuteQueryWithIncrementalRetry();
                    }
                    catch (Exception)
                    {
                        files = Context.LoadQuery(Folder.Files.Include(
                            x => x.UniqueId,
                            x => x.Name,
                            x => x.ServerRelativeUrl,
                            x => x.TimeLastModified,
                            x => x.CheckOutType,
                            x => x.Length));
                        Context.ExecuteQueryWithIncrementalRetry();
                    }
                });
            });

            foreach (var file in files)
            {
                Items.Add(new SPFileItem(this, Context, file));
            }
        }

        public ICommand ExecuteFileCommand
        {
            get { return this.CreateCommand((x) => { SelectedFile?.OpenCommand.Execute(null); }); }
        }

        protected override void OpenWebSite(object obj)
        {
            Process.Start(SPUrl);
        }

        public ICommand UploadCommand
        {
            get { return this.CreateCommand((x)=>ExecuteActionAsync(Upload(x),null,null, true, true, Properties.Resources.MsgConfirmCancelUpload)); }
        }

        private SPFileItem newFile;

        private async Task Upload(object arg)
        {
            var files = arg as string[];
            if (files == null) return;

            foreach (var file in files)
            {
                if (IsCancelled) break;
                var path = Folder.ServerRelativeUrl + "/" + System.IO.Path.GetFileName(file);
                await UploadSub(file, path);
            }
        }

        public async Task<SPFileItem> UploadFile(string file, string path)
        {
            return await UploadSub(file, path);
        }

        private async Task<SPFileItem> UploadSub(string file, string path)
        {
            var fileChild = this.Items.Where(x => x.Path == path).FirstOrDefault();
            if (fileChild != null)
            {
                var cache = FileCache.GetCachedFile(fileChild.Id);
                if (cache == null) FileCache.ClearCachedFile(fileChild.Id);
            }

            var overrideFile = Items.Where(x => string.Equals(x.Name, System.IO.Path.GetFileName(file), StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            newFile = new SPFileItem(this, Context, null);
            await newFile.Upload(file, path);
            if (overrideFile != null) Items.Remove(overrideFile);
            if (newFile.File != null)
            {
                Items.Add(newFile);
            }
            var retFile = newFile;
            newFile = null;

            return newFile;
        }

        public ICommand DeleteCommand
        {
            get
            {
                return CreateCommand((y) => {
                    var cnt = Items.Where(x => x.IsSelected).Count();
                    if (cnt == 0) return;
                    var msg = string.Format(Properties.Resources.MsgDeleteConfirm, Items.Where(x => x.IsSelected).First().Name);
                    if (cnt > 1)
                    {
                        msg = string.Format(Properties.Resources.MsgMultiDeleteConfirm, cnt);
                    }
                    if (Confirm(Properties.Resources.MsgConfirm, msg))
                    {
                        ExecuteActionAsync(Delete(y));
                    }
                });
            }
        }

        private async Task Delete(object arg)
        {
            await Task.Run(() => {
                foreach (var item in Items.ToArray())
                {
                    if (item.IsSelected)
                    {
                        item.File.Recycle();
                        Context.ExecuteQuery();
                        ExecuteUIProc(() => {
                            this.Items.Remove(item);
                        });
                    }
                }
            });

        }
        public bool CanDelete
        {
            get { return SelectedFile != null; }
        }

        public ICommand OpenCommand
        {
            get { return this.CreateCommand((x)=> { SelectedFile?.OpenCommand.Execute(null); }); }
        }
        public bool CanOpen
        {
            get { return SelectedFile != null; }
        }


        //public ICommand CopyUrlToClipboardCommand
        //{
        //    get { return this.CreateCommand((x) => { SelectedFile?.CopyUrlToClipboardCommand.Execute(null); OnPropertyChanged(null); }); }
        //}

        //public bool CanCopyUrlToClipboardCommand
        //{
        //    get { return SelectedFile != null && SelectedFile.CanCopyUrlToClipboardCommand; }
        //}

        public ICommand UploadCacheCommand
        {
            get { return this.CreateCommand((x) => { SelectedFile?.UploadCacheCommand.Execute(null); OnPropertyChanged(null); }); }
        }
        public bool CanUploadCache
        {
            get { return SelectedFile != null; }
        }

        public ICommand CheckoutCommand
        {
            get { return this.CreateCommand((x) => { SelectedFile?.CheckoutCommand.Execute(null); OnPropertyChanged(null); }); }
        }
        public bool CanCheckout
        {
            get { return SelectedFile != null && SelectedFile.CanCheckout; }
        }

        public ICommand CheckinCommand
        {
            get { return this.CreateCommand((x) => { SelectedFile?.CheckinCommand.Execute(null); OnPropertyChanged(null); }); }
        }
        public bool CanCheckin
        {
            get { return SelectedFile != null && SelectedFile.CanCheckin; }
        }

        public ICommand CancelCheckoutCommand
        {
            get { return this.CreateCommand((x) => { SelectedFile?.CancelCheckoutCommand.Execute(null); OnPropertyChanged(null); });  }
        }
        public bool CanCancelCheckout
        {
            get { return SelectedFile != null && SelectedFile.CanCancelCheckout; }
        }

        public ICommand RenameEditCommand
        {
            get { return this.CreateCommand((x) => { SelectedFile?.RenameEditCommand.Execute(null); }); }
        }
        public bool CanRename
        {
            get { return SelectedFile != null && SelectedFile.CanRename; }
        }

        public ICommand ShowUrlCommand
        {
            get { return this.CreateCommand((x) => { SelectedFile?.ShowUrlCommand.Execute(null); }); }
        }
        public bool ShowUrl
        {
            get { return SelectedFile != null; }
        }

        public ICommand CopyCommand
        {
            get { return this.CreateCommand(Copy); }
        }
        public bool CanCopy
        {
            get { return SelectedFile != null; }
        }
        private void Copy(object arg)
        {
            var dataObject = CreateFilesDataObject(false);
            Clipboard.SetDataObject(dataObject);
        }

        //public ICommand CutCommand
        //{
        //    get { return this.CreateCommand(Cut); }
        //}
        //public bool CanCut
        //{
        //    get { return SelectedFile != null; }
        //}
        //private void Cut(object arg)
        //{
        //    var dataObject = CreateFilesDataObject(true);
        //    Clipboard.SetDataObject(dataObject);
        //}

        public ICommand PasteCommand
        {
            get { return CreateCommand((x) => { ExecuteActionAsync(Paste(x)); }); }
        }
        public bool CanPaste
        {
            get
            {
                IDataObject dataObj = Clipboard.GetDataObject() as IDataObject;
                if (dataObj == null) return false;
                var stream = (MemoryStream)dataObj.GetData(NativeMethods.CFSTR_PREFERREDDROPEFFECT, true);
                return stream != null;
            }
        }
        private async Task Paste(object e)
        {
            IDataObject dataObj = Clipboard.GetDataObject() as IDataObject;
            if (dataObj != null)
            {
                MemoryStream stream = (MemoryStream)dataObj.GetData(NativeMethods.CFSTR_PREFERREDDROPEFFECT, true);
                if (stream != null)
                {
                    int flag = stream.ReadByte();
                    if (!(flag != 2 && flag != 5))
                    {
                        bool cut = (flag == 2);

                        string[] files = dataObj.GetData(DataFormats.FileDrop) as string[];
                        if (files != null)
                        {
                            await Upload(files);
                        }
                        else if (dataObj.GetDataPresent(DataFormats.Serializable))
                        {
                            files = GetDataObjectAsPaths(dataObj);
                            await CopyFiles(files);
                        }
                    }
                }
            }
        }

        private string[] GetDataObjectAsPaths(IDataObject dataObj)
        {
            var st = dataObj.GetData(DataFormats.Serializable) as MemoryStream;
            if (st != null)
            {
                BinaryFormatter bin = new BinaryFormatter();
                var files = (string[])bin.Deserialize(st);
                return files;
            }
            return null;
        }

        public ICommand SaveCommand
        {
            get
            {
                return CreateCommand((x) => {
                    var dialog = new System.Windows.Forms.FolderBrowserDialog();
                    var result = dialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        ExecuteActionAsync(Save(dialog.SelectedPath));
                    }
                });
            }
        }

        public ICommand CopyUrlToClipboardCommandForItem
        {
            get { return SelectedFile.CopyUrlToClipboardCommand; }
        }

        public bool CanSave
        {
            get { return SelectedFile != null; }
        }
        private async Task Save(string target)
        {

            await Task.Run(() => {
                foreach (var file in Items.Where(x => x.IsSelected))
                {
                    var targetFile = System.IO.Path.Combine(target, file.Name);
                    using (var st = new System.IO.FileStream(targetFile, System.IO.FileMode.Create))
                    {
                        var fileObj = Context.Web.GetFileByServerRelativeUrl(file.File.ServerRelativeUrl);
                        var data = fileObj.OpenBinaryStream();
                        Context.ExecuteQuery();
                        data.Value.CopyTo(st);
                    }
                }
            });

        }

        public override async Task<List<SPSearchResultFileItem>> Search(object obj)
        {
            var list = await base.Search(obj);

            var newList = new List<SPSearchResultFileItem>();
            foreach (var item in list)
            {
                var serverRelativeUrl = System.Uri.UnescapeDataString(new Uri(item.Path).PathAndQuery);
                if (serverRelativeUrl.StartsWith(Folder.ServerRelativeUrl))
                {
                    newList.Add(item);
                }
            }
            return newList;
        }

        public ICommand ClearFileCacheCommand
        {
            get { return this.CreateCommand((x) => { SelectedFile?.ClearCacheCommand.Execute(null); }); }
        }
        public bool CanClearFileCache
        {
            get { return SelectedFile != null && SelectedFile.HasCache; }
        }


        //public DataObjectEx CreateFilesDataObject(bool cut = false)
        //{
        //    DataObjectEx.SelectedItem[] SelectedItems = Items.Where(x => x.IsSelected).Cast<SPFileItem>()
        //    .Select(x => new DataObjectEx.SelectedItem()
        //    {
        //        Context = x.Context,
        //        FileName = x.Name,
        //        Path = x.File.ServerRelativeUrl,
        //        WriteTime = x.Modified,
        //        FileSize = x.Size,
        //    })
        //    .ToArray();
        //    if (SelectedItems.Count() == 0) return null;
        //    DataObjectEx dataObject = new DataObjectEx(SelectedItems);
        //    dataObject.SetData(NativeMethods.CFSTR_FILEDESCRIPTORW, null);
        //    dataObject.SetData(NativeMethods.CFSTR_FILECONTENTS, null);
        //    dataObject.SetData(NativeMethods.CFSTR_PERFORMEDDROPEFFECT, null);
        //    MemoryStream memo = new MemoryStream(4);
        //    byte[] bytes = new byte[] { (byte)(cut ? 2 : 5), 0, 0, 0 };
        //    memo.Write(bytes, 0, bytes.Length);
        //    dataObject.SetData(NativeMethods.CFSTR_PREFERREDDROPEFFECT, memo);
        //    dataObject.SetData(NativeMethods.CFSTR_PATHS, Items.Where(x => x.IsSelected).Cast<SPFileItem>().Select(x => x.File.ServerRelativeUrl).ToArray());
        //    return dataObject;
        //}

        //private async Task DownloadAsync(SPFileItem file, Stream stream)
        //{
        //    await Task.Run(() =>
        //    {
        //        Download(file.File.ServerRelativeUrl, stream, file.File.Length);
        //    });

        //}

        public VirtualFileDataObject CreateFilesDataObject(bool cut = false)
        {
            var virtualFileDataObject = new VirtualFileDataObject();

            var SelectedItems = Items.Where(x => x.IsSelected).Cast<SPFileItem>()
            .Select(x => new VirtualFileDataObject.FileDescriptor()
            {
                Name = x.Name,
                StreamContents = stream =>
                {
                    try
                    {
                        SetBusy(null, true, true, null);
                        var task = Task.Run(() =>
                        {
                            var cache = FileCache.GetCachedFile(x.Id);
                            if (cache != null && cache.IsDownloaded)
                            {
                                using (var st = new FileStream(cache.LocalPath, FileMode.Open))
                                {
                                    st.CopyTo(stream);
                                }
                            }
                            else
                            {
                                Download(x.File.ServerRelativeUrl, stream, x.File.Length);
                            }
                        });
                        while (!task.IsCompleted)
                        {
                            DoEvents();
                        }
                        if (task.IsFaulted)
                        {
                            if (task.Exception.InnerException != null)
                            {
                                Message = task.Exception.InnerException.Message;
                            }
                            else
                            {
                                Message = task.Exception.Message;
                            }

                            ShowMessage(Message, "Error");
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.ToString());
                        if (ex.InnerException != null)
                        {
                            Message = ex.InnerException.Message;
                        }
                        else
                        {
                            Message = ex.Message;
                        }
                        ShowMessage(Message, "Error");
                    }
                    finally
                    {
                        ResetBusy();
                    }
                }
            });

            virtualFileDataObject.SetData(SelectedItems);

            MemoryStream memo = new MemoryStream(4);
            byte[] bytes = new byte[] { (byte)(cut ? 2 : 5), 0, 0, 0 };
            memo.Write(bytes, 0, bytes.Length);
            virtualFileDataObject.SetData((short)(DataFormats.GetDataFormat(NativeMethods.CFSTR_PREFERREDDROPEFFECT).Id), memo.ToArray());
            using (MemoryStream mem = new MemoryStream())
            {
                BinaryFormatter bin = new BinaryFormatter();
                var pathslist = Items.Where(x => x.IsSelected).Cast<SPFileItem>().Select(x => x.SPUrl).ToArray();
                bin.Serialize(mem, pathslist);
                var paths = (short)(DataFormats.GetDataFormat(DataFormats.Serializable).Id);
                virtualFileDataObject.SetData(paths, mem.ToArray());
            }


            return virtualFileDataObject;
        }

        protected override void CreateFolder(object obj)
        {
            var vm = new CreateFolderVM(this);
            ShowDialog(vm, Properties.Resources.MsgCreateFolder);

        }

        public async Task CreateNewFolder(string newName)
        {
            await EnsureChildren();
            if (Folder.Folders.Any(x => x.Name == newName))
            {
                for (int i = 0; i < 10000; i++)
                {
                    newName = newName + "(" + i.ToString() + ")";
                    if (!Folder.Folders.Any(x => x.Name == newName)) break;
                }
            }
            CreateFolderInternal(newName);
        }

        public SPFolderItem CreateFolderInternal(string newName)
        {
            var newFolder = Folder.Folders.Add(newName);
            Context.ExecuteQuery();
            Context.Load(newFolder,
                x => x.Name,
                x => x.ServerRelativeUrl);
            Context.ExecuteQuery();
            var newFolderItem = new SPFolderItem(this, Context, newFolder);
            Children.Add(newFolderItem);
            return newFolderItem;
        }

        public override bool AvailableCreateFolder
        {
            get { return true; }
        }

        protected override async Task RenameFolder(object obj)
        {
            var targetFolderRelativeUrl = GetParentFolder(Path) + this.NewName;
            Folder.MoveTo(targetFolderRelativeUrl);
            Context.ExecuteQuery();
            IsFolderEditing = false;

            UpdateFolder(targetFolderRelativeUrl);
            OnPropertyChanged(null);
            await EnsureChildren(true);
        }

        private void UpdateFolder(string url)
        {
            _folder = Context.Web.GetFolderByServerRelativeUrl(url);
            Context.Load(_folder,
                x => x.Name,
                x => x.ServerRelativeUrl);
            Context.ExecuteQueryWithIncrementalRetry();
        }

        public override ICommand MoveFolderCommand
        {
            get {
                return CreateCommand(x => {
                    ExecuteActionAsync(MoveFromFolderSubAsync((string[])x), null, null, true);
                });
            }
        }

        private async Task MoveFromFolderSubAsync(string[] sourceUrls)
        {
            foreach (var sourceUrl in sourceUrls)
            {
                var source = await RootVM.FindItemByUrl(sourceUrl, false);
                var folder = source as SPFolderItem;
                if (folder != null)
                {
                    await folder.MoveToFolderAsync(this.SPUrl + "/" + source.Name);
                }
                var file = source as SPFileItem;
                if (file != null)
                {
                    var newParent = await file.MoveToFolderAsync(this.SPUrl);
                    newParent.SetDirty();
                }
            }
        }

        public async Task MoveToFolderAsync(string targetFolderUrl)
        {
            var newParent = await RootVM.FindItemByUrl(GetParentFolder(targetFolderUrl), false);
            if (newParent == null) throw new InvalidOperationException("can't load targetFolder");
            if (newParent.Context.Url == Context.Url)
            {
                Folder.MoveTo(targetFolderUrl);
                Context.ExecuteQuery();
                Parent.Children.Remove(this);
            }
            else
            {
                var executed = false;
                foreach (var site in RootVM.Children.OfType<SPSiteItem>())
                {
                    if (targetFolderUrl.StartsWith(site.Context.Url))
                    {
                        var targetSite = await site.FindNodeByUrl(targetFolderUrl, true) as SPFolderItem;
                        if (targetSite == null)
                        {
                            targetSite = await site.FindNodeByUrl(GetParentFolder(targetFolderUrl), true) as SPFolderItem;
                            var newFolder = targetFolderUrl.Split('/').LastOrDefault();
                            if (targetSite != null) targetSite = targetSite.CreateFolderInternal(newFolder);
                        }
                        if (targetSite != null)
                        {
                            await targetSite.CopyFolder(this);
                            executed = true;
                            break;
                        }
                    }
                }
                if (!executed) throw new ApplicationException(Properties.Resources.MsgInvalidMoveTargetFolder);
            }
            newParent.IsSelected = true;
            await newParent.EnsureChildren(true);
        }

        public override async Task<SPTreeItem> FindNodeByUrl(string url, bool ensure)
        {
            var target = await base.FindNodeByUrl(url, ensure);
            if (target != null) return target;
            foreach (var child in Items)
            {
                target = await child.FindNodeByUrl(url, ensure);
                if (target != null) return target;
            }
            return null;
        }

        public override bool AvailableMoveFolder
        {
            get { return true; }
        }

        public override async Task DeleteFolder(object obj)
        {
            await Task.Run(() => {
                Folder.Recycle();
                Context.ExecuteQuery();
            });
        }



        public ICommand RenameFolderEditCommand
        {
            get { return this.CreateCommand(RenameFolderEdit); }
        }

        private void RenameFolderEdit(object arg)
        {
            this.IsFolderEditing = true;
            this.NewName = this.Name;

        }
        public ICommand CancelRenameFolderCommand
        {
            get { return this.CreateCommand(CancelRenameFolder); }
        }


        private void CancelRenameFolder(object arg)
        {
            this.IsFolderEditing = false;
            this.NewName = null;

        }

        public override bool AvailableRenameFolder
        {
            get { return true; }
        }

        public override bool AvailableDeleteFolder
        {
            get
            {
                return true;
            }
        }

        public override ICommand DownloadFolderCommand
        {
            get
            {
                return CreateCommand((x) => {

                    var targetPath = (x as string) ?? ShowFolderDailog();
                    if (targetPath == null) return;
                    ExecuteActionAsync(DownloadFolderAsync(targetPath), null, null, true);

                });
            }
        }

        private async Task DownloadFolderAsync(string targetFolder)
        {
            if (targetFolder == null) return;
            if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);
            await EnsureChildren();
            await Task.Run(() => {
                foreach (var file in Items)
                {
                    if (IsCancelled) throw new OperationCanceledException();
                    file.Download(System.IO.Path.Combine(targetFolder, file.Name));
                }
            });
            foreach (var folder in Children.OfType<SPFolderItem>())
            {
                if (IsCancelled) throw new OperationCanceledException();
                await folder.DownloadFolderAsync(System.IO.Path.Combine(targetFolder, folder.Name));
            }
        }
        public override bool AvailableDownloadFolder { get { return true; } }

        public override ICommand UploadFolderCommand
        {
            get
            {
                return CreateCommand((x) => {

                    var targetPath = (x as string) ?? ShowFolderDailog();
                    if (targetPath == null) return;
                    ExecuteActionAsync(UploadFolderAsync(targetPath), null, null, true);

                });
            }
        }

        private async Task UploadFolderAsync(string targetFolder)
        {
            if (targetFolder == null) return;
            await EnsureChildren(true);
            foreach (var file in Directory.GetFiles(targetFolder))
            {
                if (IsCancelled) throw new OperationCanceledException();
                var relativePath = file.Substring(targetFolder.Length).Replace("\\", "/");
                await UploadSub(file, Path+relativePath);
            }

            foreach (var dire in Directory.GetDirectories(targetFolder))
            {
                if (IsCancelled) throw new OperationCanceledException();
                var newDirename = System.IO.Path.GetFileName(dire);
                var targetChild = Children.OfType<SPFolderItem>().Where(x => x.Name == newDirename).FirstOrDefault() ;
                if (targetChild == null)
                {
                    targetChild = CreateFolderInternal(newDirename);
                }
                await targetChild.UploadFolderAsync(dire);
            }
        }

        
        public override bool AvailableUploadFolder { get { return true; } }


        public override ICommand OpenAsExplorerCommand { get { return CreateCommand(OpenAsExplorer); } }

        private void OpenAsExplorer(object arg)
        {
            ProcessStartInfo pInfo;
            Process p;
            var root = this.FindRoot() as SPSiteItem;
            if (root == null) return;
            pInfo = new ProcessStartInfo("cmd", @"/c net use " + SPUrl + " /user:" + root.User + " " + root.Password);
            pInfo.CreateNoWindow = true; 
            pInfo.UseShellExecute = true; 
            p = Process.Start(pInfo);
            p.WaitForExit(5000); 
            p.Close();

            var spUri = new Uri(SPUrl);
            pInfo = new ProcessStartInfo("\\\\" + spUri.Host + "@SSL" + spUri.LocalPath.Replace("/", "\\"));
            pInfo.CreateNoWindow = true;
            pInfo.UseShellExecute = true;
            Process.Start(pInfo);
        }

        public override bool AvailableOpenAsExplorer { get { return true; } }



        public int FileNameWidth
        {
            get { return ExplorerSettings.Instance.FileNameWidth; }
            set
            {
                ExplorerSettings.Instance.FileNameWidth = value;
                ExplorerSettings.Instance.Save();
                OnPropertyChanged("FileNameWidth");
            }
        }

        public int ModifiedDateWidth
        {
            get { return ExplorerSettings.Instance.ModifiedDateWidth; }
            set
            {
                ExplorerSettings.Instance.ModifiedDateWidth = value;
                ExplorerSettings.Instance.Save();
                OnPropertyChanged("ModifiedDateWidth");
            }
        }

        public int SizeWidth
        {
            get { return ExplorerSettings.Instance.SizeWidth; }
            set
            {
                ExplorerSettings.Instance.SizeWidth = value;
                ExplorerSettings.Instance.Save();
                OnPropertyChanged("SizeWidth");
            }
        }

        public int OwnerWidth
        {
            get { return ExplorerSettings.Instance.OwnerWidth; }
            set
            {
                ExplorerSettings.Instance.OwnerWidth = value;
                ExplorerSettings.Instance.Save();
                OnPropertyChanged("OwnerWidth");
            }
        }

        public int CheckedOutWidth
        {
            get { return ExplorerSettings.Instance.CheckedOutWidth; }
            set
            {
                ExplorerSettings.Instance.CheckedOutWidth = value;
                ExplorerSettings.Instance.Save();
                OnPropertyChanged("CheckedOutWidth");
            }
        }

        public async Task CopyFolder(SPFolderItem source)
        {
            await EnsureChildren(true);
            await source.EnsureChildren(true);

            //folder 
            foreach (var sourceFolder in source.Children.OfType<SPFolderItem>())
            {
                var targetFolder = Children.OfType<SPFolderItem>().Where(x=> string.Equals(x.Name,sourceFolder.Name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault() ;
                if (targetFolder == null)
                {
                    targetFolder = CreateFolderInternal(sourceFolder.Name);
                }
                await targetFolder.CopyFolder(sourceFolder);
            }
            //file
            foreach (var sourceFile in source.Items.OfType<SPFileItem>())
            {
                if (IsCancelled)
                {
                    throw new OperationCanceledException();
                }
                var tempFile = System.IO.Path.GetTempFileName();
                try
                {
                    await Task.Run(() =>
                    {
                        sourceFile.Download(tempFile);
                    });
                    if (IsCancelled)
                    {
                        throw new OperationCanceledException();
                    }
                    var targetPath = Folder.ServerRelativeUrl + "/" + sourceFile.Name;
                    await UploadSub(tempFile, targetPath);
                }
                finally 
                {
                    System.IO.File.Delete(tempFile);

                }
            }
        }

        public async Task CopyFiles(string[] files)
        {
            await EnsureChildren(true);

            //file
            foreach (var file in files)
            {
                if (IsCancelled)
                {
                    throw new OperationCanceledException();
                }
                var sourceFile = await RootVM.FindItemByUrl(file, true) as SPFileItem;
                if (sourceFile == null) continue;

                var tempFile = System.IO.Path.GetTempFileName();
                try
                {
                    await Task.Run(() =>
                    {
                        sourceFile.Download(tempFile);
                    });
                    if (IsCancelled)
                    {
                        throw new OperationCanceledException();
                    }
                    var targetPath = Folder.ServerRelativeUrl + "/" + sourceFile.Name;
                    await UploadSub(tempFile, targetPath);
                }
                finally
                {
                    System.IO.File.Delete(tempFile);

                }
            }
        }
    }
}
