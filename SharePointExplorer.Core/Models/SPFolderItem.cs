using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Search.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Delimon.Win32.IO;
//using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ViewMaker.Core;

namespace SharePointExplorer.Models
{
    [View("SharePointExplorer.Views.FolderPanelView,SharePointExplorer")]
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

        public override ExplorerVM RootVM
        {
            get
            {
                if (_rootVM != null) return _rootVM;
                return base.RootVM; 
            }
        }
        private ExplorerVM _rootVM;

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
                lock (newFiles)
                {
                    foreach (var newFile in newFiles)
                    {
                        newFile.IsCancelled = value;
                    }
                }
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

         
        public SPFolderItem(TreeItem parent, Web web, ClientContext context, Folder folder, ExplorerVM rootVM = null)
            : base(parent, web, context)
        {
            Items = new ObservableCollection<SPFileItem>();
            _folder = folder;
            this._rootVM = rootVM;
        }


        protected override async Task LoadChildren(int depth=1)
        {
            Children.Clear();
            await Task.Run(() => {
                try
                {
                    if (depth <= 1)
                        Context.Load(Folder,
                            x => x.Name,
                            x => x.ServerRelativeUrl,
                            x => x.Files.Include(
                                y => y.UniqueId,
                                y => y.Name,
                                y => y.ServerRelativeUrl,
                                y => y.TimeLastModified,
                                y => y.ModifiedBy,
                                y => y.CheckOutType,
                                y => y.CheckedOutByUser,
                                y => y.Length
                            ),
                            x => x.Folders.Include(
                                y => y.Name,
                                y => y.ServerRelativeUrl
                             )
                        );
                    Context.ExecuteQueryWithIncrementalRetry();
                }
                catch
                {
                    if (depth <= 1)
                        Context.Load(Folder,
                            x => x.Name,
                            x => x.ServerRelativeUrl,
                            x => x.Files.Include(
                                y => y.UniqueId,
                                y => y.Name,
                                y => y.ServerRelativeUrl,
                                y => y.TimeLastModified,
                                y => y.CheckOutType,
                                y => y.Length
                            ),
                            x => x.Folders.Include(
                                y => y.Name,
                                y => y.ServerRelativeUrl
                             )
                        );
                    Context.ExecuteQueryWithIncrementalRetry();
                }
                //{
                //}
                //if (depth >= 2)
                //{
                //    Context.Load(Folder,
                //        x => x.Name,
                //        x => x.ServerRelativeUrl,
                //        x => x.Files.Include(
                //            y => y.UniqueId,
                //            y => y.Name,
                //            y => y.ServerRelativeUrl,
                //            y => y.TimeLastModified,
                //            y => y.ModifiedBy,
                //            y => y.CheckOutType,
                //            y => y.CheckedOutByUser,
                //            y => y.Length
                //        ),
                //        x => x.Folders.Include(
                //            y => y.Name,
                //            y => y.ServerRelativeUrl,
                //            y => y.Files.Include(
                //                z => z.UniqueId,
                //                z => z.Name,
                //                z => z.ServerRelativeUrl,
                //                z => z.TimeLastModified,
                //                z => z.ModifiedBy,
                //                z => z.CheckOutType,
                //                z => z.CheckedOutByUser,
                //                z => z.Length
                //            ),
                //            y => y.Folders.Include(
                //                    z => z.Name,
                //                    z => z.ServerRelativeUrl
                //                )
                //         )
                //    );
                //    Context.ExecuteQueryWithIncrementalRetry();
                //}
            });

            Items.Clear();
            foreach (var file in Folder.Files)
            {
                Items.Add(new SPFileItem(this, Web, Context, file));
            }
            foreach (var subFolder in Folder.Folders.OrderBy(x=>x.Name))
            {
                if (subFolder.Name == "Forms") continue;
                var sfo = new SPFolderItem(this, Web, Context, subFolder);
                Children.Add(sfo);
                //if (depth >= 2)
                //{
                //    sfo.Items.Clear();
                //    foreach (var sfile in subFolder.Files)
                //    {
                //        sfo.Items.Add(new SPFileItem(this, Web, Context, sfile));
                //    }
                //    foreach (var subSubFolder in subFolder.Folders)
                //    {
                //        if (subSubFolder.Name == "Forms") continue;
                //        var ssfo = new SPFolderItem(this, Web, Context, subSubFolder);
                //        sfo.Children.Add(sfo);
                //    }
                //}
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
            get
            {
                return CreateCommand((x) =>
                {

                    IgnoreError = false;
                    Throttler = new SemaphoreSlim(initialCount: 1);
                    AllTasks = new List<Task>();
                    BackupFolder = this;
                    BackupMode = false;
                    ExecuteActionAsync(Upload(x), null, null, true, true, Properties.Resources.MsgConfirmCancelUpload);
                });
            }
        }

        private List<SPFileItem> newFiles = new List<SPFileItem>();

        private async Task Upload(object arg)
        {
            var files = arg as string[];
            if (files == null) return;

            foreach (var file in files)
            {
                if (IsCancelled) break;
                var path = Folder.ServerRelativeUrl + "/" + System.IO.Path.GetFileName(file);
                if (Directory.Exists(file))
                {
                    await UploadFolder(file, false);
                }
                else
                {
                    await UploadSub(new FileInfo(file), file, path);
                }
            }
            await Task.WhenAll(AllTasks);
        }

        public async Task UploadFile(FileInfo file, string filePath, string path)
        {
            await UploadSub(file, filePath, path);
        }

        private void WriteUploadLog(FileInfo file, string filePath)
        {
            EnsureUploadedDb();
            backupFiles.Entry(new BackupInfo { BackupId = TryUploadFolderPath, LocalFilePath = filePath, LastModified = file.LastWriteTime.ToUniversalTime() });
        }

        private string GetUploadedDate(FileInfo file, string filePath)
        {
            EnsureUploadedDb();
            return backupFiles.GetModifiedDate(TryUploadFolderPath, filePath);
        }

        public void EnsureUploadedDb()
        {
            if (backupFiles == null)
            {
                backupFiles = new BackupFileManager();
            }
        }

        private static Dictionary<string, bool> confirmUpdatedDirectoryCache = new Dictionary<string, bool>();

        private bool ConfirmUpdatedDirectory(DirectoryInfo directory, string filePath)
        {
            if (confirmUpdatedDirectoryCache.ContainsKey(filePath))
                return confirmUpdatedDirectoryCache[filePath];
            try
            {
                foreach (var file in directory.GetFiles().Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden)))
                {
                    var lastDate = GetUploadedDate(file, filePath + "\\" + file.Name);
                    if (lastDate != file.LastWriteTime.ToUniversalTime().ToString("yyyy/MM/dd HH:mm:ss fff"))
                    {
                        confirmUpdatedDirectoryCache[filePath] = true;
                        return true;
                    }
                }
                foreach (var dire in directory.GetDirectories("*").Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden)))
                {
                    if (ConfirmUpdatedDirectory(dire, filePath+"\\" +dire.Name ))
                    {
                        confirmUpdatedDirectoryCache[filePath] = true;
                        return true;
                    }
                }
                confirmUpdatedDirectoryCache[filePath] = false;
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        private async Task<bool> UploadSub(FileInfo file, string filePath, string path, bool updateWithModified = false)
        {
            var fileTime = file.LastWriteTime.ToUniversalTime();
            var fileChild = this.Items.Where(x => x.Path == path).FirstOrDefault();
            if (fileChild != null)
            {
                //ミリ秒のギャップあり。ミリ秒以下を無視するため
                if (updateWithModified && fileChild.Modified.Subtract(fileTime).TotalSeconds <= 1.0)
                {
                    return false;
                }
                var cache = FileCache.GetCachedFile(fileChild.Id);
                if (cache != null) FileCache.ClearCachedFile(fileChild.Id);
            }
            var overrideFile = Items.Where(x => string.Equals(x.Name, file.Name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            await Throttler.WaitAsync();
            AllTasks.Add(Task.Run(async () =>
            {
                try
                {
                    while (BackupMode && !GetIsNetworkAvailable())
                    {
                        await Task.Delay(10000);
                    }
                    spSite = spSite ?? FindSPSite();
                    var contextAndWeb = spSite.GenerateContext(this.Web.ServerRelativeUrl);
                    try
                    {
                        
                        var newFile = new SPFileItem(this, contextAndWeb.Item2, contextAndWeb.Item1, null);
                        lock (newFiles)
                        {
                            newFiles.Add(newFile);
                        }
                        try
                        {
                            await newFile.Upload(filePath, path);
                            if (BackupMode) WriteUploadLog(file, filePath);
                            if (overrideFile != null)
                            {
                                Trace.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ",S," + fileTime.ToString("yyyy/MM/dd HH:mm:ss") + "," + overrideFile.File.TimeLastModified.ToString("yyyy/MM/dd HH:mm:ss") + "," + path);
                            }
                            else
                            {
                                Trace.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ",S," + fileTime.ToString("yyyy/MM/dd HH:mm:ss") + ",                    ," + path);
                            }

                            //ExecuteUIProc(() =>
                            //{
                            //    if (overrideFile != null)
                            //    {
                            //        Items.Remove(overrideFile);
                            //    }
                            //    Items.Add(newFile);
                            //});
                        }
                        finally
                        {
                            lock (newFiles)
                            {
                                newFiles.Remove(newFile);
                            }
                        }

                    }
                    finally
                    {
                        if (contextAndWeb != null) contextAndWeb.Item1.Dispose();
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ",E," + path + "," + ex.Message);
                    Debug.WriteLine(ex.ToString());
                    NotifyLogMessage(TopViewModel.LogMessage + DateTime.Now.ToString("HH:mm:ss") + " " + ex.Message + "\n");
                    var msg = string.Format(Properties.Resources.MsgAppErrorDisplayAndConfirm, ex.Message);
                    if (!IgnoreError && MessageBox.Show(msg, "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    {
                        IsCancelled = true;
                        CanCanceled = false;
                        ProgressMessage = Properties.Resources.MsgCanceling;
                    }
                    IgnoreError = true;
                }
                finally
                {
                    Throttler.Release();
                }
            }));
            return true;
        }


        private bool GetIsNetworkAvailable()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return false;
            try
            {
                var testDns = System.Net.Dns.GetHostEntry(new Uri(this.SPUrl).Host);
                if (testDns.AddressList.Length > 0) return true;
                return false;
            }
            catch
            {
                return false;
            }
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
                var stream = (System.IO.MemoryStream)dataObj.GetData(NativeMethods.CFSTR_PREFERREDDROPEFFECT, true);
                return stream != null;
            }
        }
        private async Task Paste(object e)
        {
            IDataObject dataObj = Clipboard.GetDataObject() as IDataObject;
            if (dataObj != null)
            {
                System.IO.MemoryStream stream = (System.IO.MemoryStream)dataObj.GetData(NativeMethods.CFSTR_PREFERREDDROPEFFECT, true);
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
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private string[] GetDataObjectAsPaths(IDataObject dataObj)
        {
            var st = dataObj.GetData(DataFormats.Serializable) as System.IO.MemoryStream;
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
                        var fileObj = Web.GetFileByServerRelativeUrl(file.File.ServerRelativeUrl);
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
                                using (var st = new System.IO.FileStream(cache.LocalPath, System.IO.FileMode.Open))
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

            System.IO.MemoryStream memo = new System.IO.MemoryStream(4);
            byte[] bytes = new byte[] { (byte)(cut ? 2 : 5), 0, 0, 0 };
            memo.Write(bytes, 0, bytes.Length);
            virtualFileDataObject.SetData((short)(DataFormats.GetDataFormat(NativeMethods.CFSTR_PREFERREDDROPEFFECT).Id), memo.ToArray());
            using (System.IO.MemoryStream mem = new System.IO.MemoryStream())
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
            var newFolderItem = new SPFolderItem(this, Web, Context, newFolder);
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
            _folder = Web.GetFolderByServerRelativeUrl(url);
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
            if (newParent.Web.Url == Web.Url)
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
            if (!url.StartsWith(this.SPUrl)) return null;
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
                    IgnoreError = false;
                    Throttler = new SemaphoreSlim(initialCount: 1);
                    AllTasks = new List<Task>();
                    spSite = FindSPSite();
                    BackupFolder = this;
                    BackupMode = false;
                    var updateWithModified = Confirm("confirm",Properties.Resources.MsgUpdateWithModified);
                    ExecuteActionAsync(UploadFolder(targetPath, updateWithModified), null, null, true, true, Properties.Resources.MsgConfirmCancelUpload);
                });
            }
        }

        public ICommand TryUploadFolderCommand
        {
            get
            {
                return CreateCommand((x) => {

                    var targetPath = (x as string) ?? ShowFolderDailog();
                    if (targetPath == null) return;
                    IgnoreError = true;
                    Throttler = new SemaphoreSlim(initialCount: 10);
                    AllTasks = new List<Task>();
                    var uploadFolder = new SPFolderItem(null, this.Web, this.Context, this.Folder, this.RootVM);
                    spSite = FindSPSite();
                    BackupFolder = uploadFolder;
                    BackupMode = true;
                    var updateWithModified = Confirm("confirm", Properties.Resources.MsgUpdateWithModified);
                    ExecuteActionAsync(uploadFolder.TryUploadFolder(targetPath, updateWithModified), null, null, true, true, Properties.Resources.MsgConfirmCancelUpload);
                });
            }
        }

        public async Task Backup(string targetPath, bool updateWithModified)
        {
            IgnoreError = true;
            Throttler = new SemaphoreSlim(initialCount: 10);
            AllTasks = new List<Task>();
            var uploadFolder = new SPFolderItem(null, this.Web, this.Context, this.Folder, this.RootVM);
            spSite = FindSPSite();
            BackupFolder = uploadFolder;
            BackupMode = true;
            NotifyLogMessage("");
            await uploadFolder.TryUploadFolder(targetPath, updateWithModified);
            Trace.WriteLine("\n\nError & Warning");
            Trace.WriteLine(TopViewModel.LogMessage);
        }

        private async Task UploadFolder(string targetFolder, bool updateWithModified)
        {
            await EnsureChildren(true);
            await UploadFolderAsync(new DirectoryInfo(targetFolder), targetFolder, updateWithModified);
            await Task.WhenAll(AllTasks);
        }

        private static string TryUploadFolderPath;

        private async Task TryUploadFolder(string targetFolder, bool updateWithModified)
        {
            await EnsureChildren(true);
            TryUploadFolderPath = Path;
            confirmUpdatedDirectoryCache = new Dictionary<string, bool>();
            await UploadFolderAsync(new DirectoryInfo(targetFolder), targetFolder, updateWithModified);
            await Task.WhenAll(AllTasks);
            //if (!string.IsNullOrEmpty(TopViewModel.LogMessage))
            //{
            //    if (Confirm("confirm", Properties.Resources.MsgConfirmSaveLog))
            //    {
            //        var file = this.ShowSaveDialog();
            //        if (file != null)
            //        {
            //            System.IO.File.WriteAllText(file, TopViewModel.LogMessage);
            //        }
            //    }
            //}
        }


        private bool IsTooLongFolder(string path)
        {
            return path.Length > 200;
        }

        private string tooLongFolder = "too Long";

        private async Task<SPFolderItem> CreateTooLongDirectory(string path)
        {
            await BackupFolder.EnsureChildren();
            var topFolder = BackupFolder.Children.OfType<SPFolderItem>().Where(x => x.Name == tooLongFolder).FirstOrDefault();
            if (topFolder == null)
            {
                topFolder = BackupFolder.CreateFolderInternal(tooLongFolder);
            }
            await topFolder.EnsureChildren();
            var work = path.Split('/');
            var direName = work.Last();
            var h = CreateHash(path);
            if (h.Length > 5) h = h.Substring(0, 5);
            h = "..." + h;

            //最大50文字
            if (direName.Length > 50-h.Length) direName = direName.Substring(0, 50-h.Length);
            direName += h;

            if (IsTooLongFolder(topFolder.Path + direName))
            {
                var p = topFolder.Path.Length + direName.Length - 200;
                if (direName.Length > p)
                {
                    direName = direName.Substring(0, direName.Length - p);
                }
                else
                {
                    direName = direName.Substring(0,1);
                }
                direName = direName + h;
            }
            var shortcut = topFolder.Children.OfType<SPFolderItem>().Where(x => x.Name == direName).FirstOrDefault();
            if (shortcut == null)
            {
                shortcut = topFolder.CreateFolderInternal(direName);
            }
            return shortcut;
        }

        private string CreateHash(string path)
        {
            var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] data = System.Text.Encoding.UTF8.GetBytes(path);
            byte[] bs = md5.ComputeHash(data);
            md5.Clear();
            var result = new System.Text.StringBuilder();
            foreach (byte b in bs)
            {
                result.Append(b.ToString("x2"));
            }
            return result.ToString();
        }
        private string ConvertUploadablePath(string path)
        {
            path = path.Replace("#", "").Replace("&", "").Replace("%", "");
            if (path.Length > 250)
            {
                var h = CreateHash(path);
                if (h.Length > 6) h = h.Substring(0, 6);
                h = "..." + h;

                var extPos = path.LastIndexOf(".");
                if (extPos >= 0)
                {
                    var extlen = path.Length - extPos;
                    path = path.Substring(0, 240 - extlen) + h + path.Substring(extPos);
                }
                else
                {
                    path = path.Substring(0, 240) + h;
                }
            }
            return path;
        }

        private static SemaphoreSlim Throttler { get; set; }

        private static List<Task> AllTasks { get; set; }

        [ThreadStatic]
        private static BackupFileManager backupFiles;

        private static SPFolderItem BackupFolder { get; set; }

        private static bool IgnoreError { get; set; }

        private static bool BackupMode { get; set; }

        private static SPSiteItem spSite { get; set; }


        private SPSiteItem FindSPSite()
        {
            var parent = Parent;
            while (parent != null)
            {
                if (parent is SPSiteItem)
                {
                    return (SPSiteItem)parent;
                }
                parent = parent.Parent;
            }
            throw new InvalidOperationException();
        }


        //TOO LONG PATHのための対策でDirectoryInfoとPathをもつ 
        private async Task UploadFolderAsync(DirectoryInfo directory, string directoryPath, bool updateWithModified)
        {
            while (BackupMode && !GetIsNetworkAvailable())
            {
                await Task.Delay(10000);
            }
            if (directory == null) return;
            NotifyProgressMessage(Properties.Resources.MsgProcessing);
            await EnsureChildren();

            foreach (var file in directory.GetFiles().Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden)))
            {
                string logMessage = null;
                if (IsCancelled) throw new OperationCanceledException();
                var relativePath = Path + "/" + file.Name;
                if (BackupMode)
                {
                    var newRelativePath = ConvertUploadablePath(relativePath);
                    if (relativePath != newRelativePath)
                    {
                        relativePath = newRelativePath;
                        logMessage = string.Format(Properties.Resources.MsgRenamedUploaded, DateTime.Now.ToString("HH:mm:ss"), relativePath, newRelativePath);
                    }
                }
                if (await UploadSub(file, directoryPath + "\\" + file.Name, relativePath, updateWithModified))
                {
                    if (logMessage != null) NotifyLogMessage(TopViewModel.LogMessage + logMessage + "\n");
                }

            }

            foreach (var dire in directory.GetDirectories("*").Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden)))
            {
                try
                {
                    if (updateWithModified && BackupMode && !ConfirmUpdatedDirectory(dire, directoryPath +"\\" + dire.Name)) continue;
                    string logMessage = null;

                    if (IsCancelled) throw new OperationCanceledException();
                    var newDirename = dire.Name;
                    var orgDire = newDirename;
                    if (BackupMode)
                    {
                        var newNewDirename = ConvertUploadablePath(newDirename);
                        if (newDirename != newNewDirename)
                        {
                            newDirename = newNewDirename;
                            logMessage = string.Format(Properties.Resources.MsgRenamedUploaded, DateTime.Now.ToString("HH:mm:ss"), orgDire, newDirename);
                        }
                    }
                    var newPath = this.Path + "/" + newDirename;
                    SPFolderItem targetChild =null;
                    if (!IsTooLongFolder(newPath))
                    {
                        targetChild = Children.OfType<SPFolderItem>().Where(x => x.Name == newDirename).FirstOrDefault();
                        if (targetChild == null)
                        {
                            targetChild = CreateFolderInternal(newDirename);
                        }
                    }
                    else
                    {
                        targetChild = await CreateTooLongDirectory(newPath);
                        logMessage = string.Format(Properties.Resources.MsgRenamedUploaded, DateTime.Now.ToString("HH:mm:ss"), orgDire, targetChild.Path);
                    }
                    if (targetChild != null)
                    {
                        var uploadFolder = new SPFolderItem(null, this.Web, this.Context, targetChild.Folder, this.RootVM);
                        await uploadFolder.UploadFolderAsync(dire, directoryPath + "\\" +dire.Name, updateWithModified);
                        //await targetChild.UploadFolderAsync(dire, updateWithModified);
                    }
                   if (logMessage != null) NotifyLogMessage(TopViewModel.LogMessage + logMessage + "\n");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ",E," + directoryPath + "," + ex.Message);
                    NotifyLogMessage(TopViewModel.LogMessage + DateTime.Now.ToString("HH:mm:ss") + " " + ex.Message + "\n");
                    var msg = string.Format(Properties.Resources.MsgAppErrorDisplayAndConfirm, ex.Message)+ " " + dire;
                    if (!IgnoreError && MessageBox.Show(msg, "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel) throw;
                    IgnoreError = true;
                }

            }

            ////メモリ節約
            //Debug.WriteLine("Memory cleanup");
            //Items.Clear();
            //Children.Clear();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            //Debug.WriteLine("Done");

            //Debug.WriteLine("Done All");
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
            await EnsureChildren();
            await source.EnsureChildren();

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
                    await UploadSub(new FileInfo(tempFile),tempFile, targetPath);
                }
                finally 
                {
                    System.IO.File.Delete(tempFile);

                }
            }
        }

        public async Task CopyFiles(string[] files)
        {
            await EnsureChildren();

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
                    await UploadSub(new FileInfo(tempFile), tempFile, targetPath);
                }
                finally
                {
                    System.IO.File.Delete(tempFile);

                }
            }
        }
    }
}
