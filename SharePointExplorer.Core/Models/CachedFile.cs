using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePointExplorer.Models
{
    public class CachedFile
    {
        public string Id { get; set; }
        public bool IsEditing { get; set; }
        public string CacheFolder { get; set; }
        public string Path { get; set; }
        public string LocalPath { get; set; }

        /// <summary>
        /// ローカルファイル更新時間
        /// </summary>
        public DateTime? LocalFileCachedTime { get; set; }

        /// <summary>
        ///サーバ側ファイル更新時刻
        /// </summary>
        public DateTime LastModified { get; set; }


        public DateTime LastAccessTime { get; set; }

        public bool IsDirty
        {
            get
            {
                if (!System.IO.File.Exists(LocalPath)) return false;
                var info = new FileInfo(LocalPath);
                return (LocalFileCachedTime < info.LastWriteTimeUtc);
            }
        }

        public bool IsDownloaded
        {
            get
            {
                return System.IO.File.Exists(LocalPath); ;

            }
        }
    }

}
