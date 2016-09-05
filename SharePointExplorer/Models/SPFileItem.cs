using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using ViewMaker;

namespace SharePointExplorer.Models
{
    public class SPFileItem : SPTreeItem
    {
        public override string Name
        {
            get { return File.Name; }
        }

        public string Id
        {
            get { return File.UniqueId.ToString("d"); }
        }

        public override string Path
        {
            get { return File.ServerRelativeUrl; }
        }

        public string NewName
        {
            get { return _newName;  }
            set { _newName = value; OnPropertyChanged("NewName"); }
        }
        private string _newName;

        public virtual string Owner
        {
            get
            {
                try
                {
                    return File.ModifiedBy?.Title;
                }
                catch (Exception)
                {
                    return "";
                } 

            }
        }

        public virtual DateTime Modified
        {
            get { return File.TimeLastModified; }
        }

        public string LocalModified
        {
            get
            {
                return  System.TimeZone.CurrentTimeZone.ToLocalTime(Modified).ToString(ExplorerSettings.Instance.DateFormat);
            }
        }


        public virtual string CheckedOut
        {
            get
            {
                if (File.CheckOutType == CheckOutType.None) return null;
                try
                {
                    if (File.CheckedOutByUser == null) return null;
                    if (File.CheckedOutByUser.Title == null) return null;
                    return File.CheckedOutByUser.Title;
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        public virtual long Size
        {
            get { return File.Length; }
        }

        public virtual string SizeString
        {
            get { return Utils.SizeSuffix(File.Length); }
        }

        public virtual string Remark
        {
            get { return _remark; }
            set { _remark = value; OnPropertyChanged("Remark");  }
        }
        private string _remark;

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

        public File File
        {
            get { return _file; }
        }
        private File _file;

        public ImageSource ExeIcon
        {
            get
            {
                var ext = System.IO.Path.GetExtension(Name);
                if (!exeIcons.ContainsKey(ext))
                {
                    try
                    {
                        var path = Utils.FileExtentionInfo(Utils.AssocStr.Executable, ext);
//                        var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
                        System.Drawing.Icon icon = IconTools.GetIconForExtension(ext, ShellIconSize.LargeIcon);
                        exeIcons[ext] = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                                    icon.Handle,
                                    new Int32Rect(0, 0, icon.Width, icon.Height),
                                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                    }
                    catch (Exception)
                    {

                        exeIcons[ext] = null;
                    }
                }
                return exeIcons[ext];
            }
        }
        private static Dictionary<string, ImageSource> exeIcons = new Dictionary<string, ImageSource>();

        public string OptionIcon
        {
            get { return IsLocalEditing ? "/SharePointExplorer;Component/Images/editing.png" : "/SharePointExplorer;Component/Images/blank.png"; }
        }

        public override string SPUrl
        {
            get
            {
                var uri = new Uri(Context.Url);
                var root = uri.Scheme + "://" + uri.Host;
                return root + File.ServerRelativeUrl;
            }
        }

        public SPFileItem(TreeItem parent, Web web, ClientContext context, File file) 
            :base(parent, web, context)

        {
            this._file = file;
        }


        public ICommand OpenCommand
        {
            get { return this.CreateCommand((x)=>ExecuteActionAsync(Open(x), null, null, true)); }
        }

        private async Task Open(object arg)
        {
            CachedFile cache = await Download();
            cache.LastAccessTime = DateTime.Now;
            FileCache.SaveCachedFile();
            Process.Start(cache.LocalPath);
            ((ExplorerVM)RootVM).UpdateJumpList();
        }

        private async Task<CachedFile> Download(bool force =false)
        {
            var cache = FileCache.GetCachedFile(Id);

            if (!force && cache != null && cache.IsDownloaded)
            {
                if (cache.IsEditing) return cache;
                if (cache.IsDirty)
                {
                    //ローカル変更有でサーバ変更なしの場合は無条件でキャッシュ
                    if (cache.LastModified == this.Modified) return cache;

                    //ローカル変更有でサーバ変更有の場合は確認してダウンロード
                    if (!Confirm(SharePointExplorer.Properties.Resources.MsgConfirm, 
                        string.Format(SharePointExplorer.Properties.Resources.MsgConfirmDownloadOnEditing, Name)))
                    {
                        return cache;
                    }
                }
                else
                {
                    if (cache.LastModified == this.Modified) return cache;
                }
            }
            if (cache == null)
            {
                cache = FileCache.CreateCacheFileEntry(Id, File.ServerRelativeUrl, File.TimeLastModified);
            }
            if (cache.CacheFolder != null && !System.IO.Directory.Exists(cache.CacheFolder)) System.IO.Directory.CreateDirectory(cache.CacheFolder);

            this.NotifyProgressMessage(string.Format(SharePointExplorer.Properties.Resources.MsgDownloading, this.Name));

            await Task.Run(() =>
            {

                var ServerRelativeUrl = File.ServerRelativeUrl;
                var localPath = cache.LocalPath;

                Download(ServerRelativeUrl, localPath, Size);
            });
            cache.LocalFileCachedTime = DateTime.UtcNow;
            cache.LastModified = Modified;
            FileCache.SaveCachedFile();
            return cache;
        }


        public void Download(string localPath)
        {
            Download(File.ServerRelativeUrl, localPath, Size);
        }


        private async Task Update(object arg)
        {
            var cache = FileCache.GetCachedFile(Id);
            if (cache == null) return;
            if (cache.CacheFolder != null && !System.IO.Directory.Exists(cache.CacheFolder)) System.IO.Directory.CreateDirectory(cache.CacheFolder);

            await Upload(cache.LocalPath, Path);
            cache.LocalFileCachedTime = DateTime.UtcNow;
            cache.LastModified = Modified;
            FileCache.SaveCachedFile();

        }

        public ICommand UploadCacheCommand
        {
            get { return this.CreateCommand((x) => ExecuteActionAsync(Update(x), null,null,true, true, Properties.Resources.MsgConfirmCancelUpload)); }
        }

        public ICommand CheckoutCommand
        {
            get { return this.CreateCommand((x)=>ExecuteActionAsync(Checkout(x))); }
        }
        private async Task Checkout(object arg)
        {
            await Task.Run(() => {
                File.CheckOut();
                Context.ExecuteQuery();
                UpdateFile(File.ServerRelativeUrl);
                var cache = FileCache.GetCachedFile(Id);
                if (cache != null)
                {
                    cache.IsEditing = true;
                    FileCache.SaveCachedFile();
                }
            });
            OnPropertyChanged(null);

            ((SPFolderItem)Parent).RaiseContextMenuData();
            await Open(arg);
        }
        public bool CanCheckout
        {
            get { return this.CheckedOut == null; }
        }

        public ICommand CheckinCommand
        {
            get { return this.CreateCommand((x)=> ExecuteActionAsync(Checkin(x))); }
        }
        private async Task Checkin(object arg)
        {
            var cache = FileCache.GetCachedFile(Id);
            if (cache != null && cache.IsDirty)
            {
                if (Confirm(Properties.Resources.MsgConfirm, Properties.Resources.MsgConfirmUpload))
                {
                    await Upload(cache.LocalPath, Path);
                    cache.LocalFileCachedTime = DateTime.UtcNow;
                    cache.LastModified = Modified;
                    FileCache.SaveCachedFile();
                }
            }
            await Task.Run(() => {
                File.CheckIn("", CheckinType.OverwriteCheckIn);
                Context.ExecuteQuery();
                UpdateFile(File.ServerRelativeUrl);
                if (cache != null)
                {
                    cache.IsEditing = false;
                    FileCache.SaveCachedFile();
                }
            });
            OnPropertyChanged(null);
            ((SPFolderItem)Parent).RaiseContextMenuData();           
        }
        public bool CanCheckin
        {
            get { return this.CheckedOut != null; }
        }

        public bool IsLocalEditing
        {
            get
            {
                var cache = FileCache.GetCachedFile(Id);
                if (cache == null) return false;
                return cache.IsDirty || cache.IsEditing;
            }
        }

        public bool HasCache
        {
            get
            {
                var cache = FileCache.GetCachedFile(Id);
                if (cache == null) return false;
                return true;
            }
        }

        public ICommand CancelCheckoutCommand
        {
            get { return this.CreateCommand((x)=>ExecuteActionAsync(CancelCheckout(x))); }
        }
        private async Task CancelCheckout(object arg)
        {
            var cache = FileCache.GetCachedFile(Id);
            if (cache != null && cache.IsDirty)
            {
                if (!Confirm(Properties.Resources.MsgConfirm, Properties.Resources.MsgCancelCheckout))
                {
                    return;
                }
            }
            await Task.Run(() => {
                File.UndoCheckOut();
                Context.ExecuteQuery();
                UpdateFile(File.ServerRelativeUrl);
                if (cache != null)
                {
                    cache.IsEditing = false;
                    FileCache.SaveCachedFile();
                }

            });
            OnPropertyChanged(null);
            ((SPFolderItem)Parent).RaiseContextMenuData();
        }
        public bool CanCancelCheckout
        {
            get { return this.CheckedOut != null; }
        }

        public ICommand DeleteCommand
        {
            get
            {
                return this.CreateCommand((x)=>
                {
                    if (Confirm(Properties.Resources.MsgConfirm, string.Format(Properties.Resources.MsgDeleteConfirm, File.Name)))
                    {
                        ExecuteActionAsync(Delete(x), (t)=> {
                            ((SPFolderItem)Parent).Items.Remove(this);

                        });
                    }
                });
            }
        }
        private async Task Delete(object arg)
        {
            await Task.Run(() => {
                File.Recycle();
                Context.ExecuteQuery();
            });
        }
        public bool CanDelete
        {
            get { return true; }
        }

        public ICommand RenameCommand
        {
            get { return this.CreateCommand((x)=> {
                if (!CanRename)
                {
                    this.ShowMessage(Properties.Resources.MsgDirtyConfirmUpoad, "ERROR");
                    return;
                }
                ExecuteActionAsync(Rename(x));

            }); }
        }

        private async Task Rename(object arg)
        {
            await Task.Run(() =>
            {
                var newUrl = GetParentFolder(File.ServerRelativeUrl) + NewName;
                File.MoveTo(newUrl, MoveOperations.None);
                Context.ExecuteQuery();
                UpdateFile(newUrl);
                FileCache.RenameCachedFile(Id, newUrl);
            });
            this.IsEditing = false;
            OnPropertyChanged(null);
        }

        private void UpdateFile(string url)
        {
            _file = Web.GetFileByServerRelativeUrl(url);
            Context.Load(_file,
                x => x.UniqueId,
                x => x.Name,
                x => x.ServerRelativeUrl,
                x => x.TimeLastModified,
                x => x.ModifiedBy,
                x => x.CheckOutType,
                x => x.CheckedOutByUser,
                x => x.Length);
            Context.ExecuteQueryWithIncrementalRetry();
        }

        public bool CanRename
        {
            get { return this.CheckedOut == null && !IsLocalEditing; }
        }


        public ICommand RenameEditCommand
        {
            get { return this.CreateCommand(RenameEdit); }
        }

        private void RenameEdit(object arg)
        {
            this.IsEditing = true;
            this.NewName = this.Name;

        }
        public ICommand CancelRenameCommand
        {
            get { return this.CreateCommand(CancelRename); }
        }


        private void CancelRename(object arg)
        {
            this.IsEditing = false;
            this.NewName = null;

        }

        public ICommand ShowUrlCommand
        {
            get { return this.CreateCommand(ShowUrl); }
        }


        private void ShowUrl(object arg)
        {
            MessageBox.Show(SPUrl);
        }

        public bool CanCopyUrlToClipboardCommand
        {
            get { return true; }
        }

        public async Task Upload(string fileName, string serverPath, int fileChunkSizeInMB = 3)
        {

            await Task<File>.Run(() =>
            {
                UploadSync(fileName, serverPath, fileChunkSizeInMB);
            });

        }

        private void UploadSync(string fileName, string serverPath, int fileChunkSizeInMB)
        {
            try
            {
                var filename = serverPath.Split('/').LastOrDefault();
                this.AddProgressMessage(serverPath, string.Format(Properties.Resources.MsgUploading, filename));
                UploadInternal(fileName, serverPath, fileChunkSizeInMB);
                //_file = Web.GetFileByServerRelativeUrl(serverPath);
                //Context.Load(_file,
                //    x => x.UniqueId,
                //    x => x.Name,
                //    x => x.ServerRelativeUrl,
                //    x => x.TimeLastModified,
                //    x => x.ModifiedBy,
                //    x => x.CheckOutType,
                //    x => x.CheckedOutByUser,
                //    x => x.Length);
                //Context.ExecuteQueryWithIncrementalRetry();
                //return _file;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                throw;
            }
            finally
            {
                this.RemoveProgressMessage(serverPath);
            }
        }

        private void UploadInternal(string fileName, string serverPath, int fileChunkSizeInMB)
        {
            var filename = serverPath.Split('/').LastOrDefault();
            Guid uploadId = Guid.NewGuid();
            //var tempFileName = filename + " uploding " +  uploadId.ToString("D");
            //var tempServerpath = GetParentFolder(serverPath) + tempFileName;
            int blockSize = fileChunkSizeInMB * 1024 * 1024;
            long fileSize = new System.IO.FileInfo(fileName).Length;

            if (fileSize <= blockSize)
            {
                // Use regular approach.
                using (var fs = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
                {
                    FileCreationInformation fileInfo = new FileCreationInformation();
                    fileInfo.ContentStream = fs;
                    fileInfo.Url = filename;
                    fileInfo.Overwrite = true;
                    var direItem = Web.GetFolderByServerRelativeUrl(GetParentFolder(serverPath));
                    var uploadFile = direItem.Files.Add(fileInfo);
                    uploadFile.ListItemAllFields["Modified"] = System.IO.File.GetLastWriteTimeUtc(fileName);
                    uploadFile.ListItemAllFields.Update();
                    Context.ExecuteQuery();
                }
            }
            else
            {
                // Use large file upload approach.
                ClientResult<long> bytesUploaded = null;

                System.IO.FileStream fs = null;
                try
                {
                    fs = System.IO.File.Open(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                    using (var br = new System.IO.BinaryReader(fs))
                    {
                        byte[] buffer = new byte[blockSize];
                        Byte[] lastBuffer = null;
                        long fileoffset = 0;
                        long totalBytesRead = 0;
                        int bytesRead;
                        bool first = true;
                        bool last = false;

                        // Read data from file system in blocks. 
                        while ((bytesRead = br.Read(buffer, 0, buffer.Length)) > 0)
                        {

                            totalBytesRead = totalBytesRead + bytesRead;

                            // You've reached the end of the file.
                            if (totalBytesRead == fileSize)
                            {
                                last = true;
                                // Copy to a new buffer that has the correct size.
                                lastBuffer = new byte[bytesRead];
                                Array.Copy(buffer, 0, lastBuffer, 0, bytesRead);
                            }

                            if (first)
                            {
                                using (var contentStream = new System.IO.MemoryStream())
                                {
                                    // Add an empty file.
                                    FileCreationInformation fileInfo = new FileCreationInformation();
                                    fileInfo.ContentStream = contentStream;
                                    fileInfo.Url = filename;
                                    fileInfo.Overwrite = true;

                                    var direItem = Web.GetFolderByServerRelativeUrl(GetParentFolder(serverPath));
                                    var uploadFile = direItem.Files.Add(fileInfo);

                                    // Start upload by uploading the first slice. 
                                    using (var s = new System.IO.MemoryStream(buffer))
                                    {
                                        // Call the start upload method on the first slice.
                                        bytesUploaded = uploadFile.StartUpload(uploadId, s);
                                        Context.ExecuteQuery();
                                        // fileoffset is the pointer where the next slice will be added.
                                        fileoffset = bytesUploaded.Value;
                                    }

                                    // You can only start the upload once.
                                    first = false;
                                }
                            }
                            else
                            {
                                // Get a reference to your file.
                                var uploadFile = Web.GetFileByServerRelativeUrl(serverPath);

                                if (last)
                                {
                                    // Is this the last slice of data?
                                    using (var s = new System.IO.MemoryStream(lastBuffer))
                                    {
                                        // End sliced upload by calling FinishUpload.
                                        uploadFile = uploadFile.FinishUpload(uploadId, fileoffset, s);
                                        uploadFile.ListItemAllFields["Modified"] = System.IO.File.GetLastWriteTimeUtc(fileName);
                                        uploadFile.ListItemAllFields.Update();
                                        Context.ExecuteQuery();
                                    }
                                    break;
                                }
                                else
                                {
                                    using (var s = new System.IO.MemoryStream(buffer))
                                    {
                                        // Continue sliced upload.
                                        bytesUploaded = uploadFile.ContinueUpload(uploadId, fileoffset, s);
                                        Context.ExecuteQuery();
                                        // Update fileoffset for the next slice.
                                        fileoffset = bytesUploaded.Value;
                                    }
                                }
                            }

                            if (IsCancelled)
                            {
                                var uploadFile = Web.GetFileByServerRelativeUrl(serverPath);
                                uploadFile.CancelUpload(uploadId);
                                Context.ExecuteQuery();
                                throw new OperationCanceledException();
                            }
                            this.NotifyProgressMessage(string.Format("{0} {1}%", string.Format(Properties.Resources.MsgUploading, filename), fileoffset * 100 / fileSize));


                        } // while ((bytesRead = br.Read(buffer, 0, buffer.Length)) > 0)
                    }
                    Remark = "";
                }
                finally
                {
                    this.NotifyProgressMessage("");
                    if (fs != null)
                    {
                        fs.Dispose();
                    }
                }
            }
        }


        public async Task<SPFolderItem> MoveToFolderAsync(string targetFolderUrl)
        {
            var newParent = await RootVM.FindItemByUrl(targetFolderUrl, true) as SPFolderItem;
            if (newParent == null)
            {
                throw new InvalidOperationException("can't load target folder");
            }
            if (newParent.Context.Url == Context.Url)
            {
                File.MoveTo(targetFolderUrl + "/" + Name, MoveOperations.Overwrite);
                Context.ExecuteQuery();
                ((SPFolderItem)Parent).Items.Remove(this);
            }
            else
            {
                var tempFile = System.IO.Path.GetTempFileName();
                try
                {
                    await Task.Run(() =>
                    {
                        Download(tempFile);
                    });
                    if (IsCancelled)
                    {
                        throw new OperationCanceledException();
                    }
                    await newParent.UploadFile(tempFile, newParent.Path + "/" + Name);
                }
                finally
                {
                    System.IO.File.Delete(tempFile);
                }
            }
            return newParent;

        }

    }
}
