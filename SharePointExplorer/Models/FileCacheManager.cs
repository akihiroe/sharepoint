using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SharePointExplorer.Models
{
    public class FileCacheManager
    {

        public static List<FileCacheManager> Instances { get; set; }

        protected const int CachedDays = 3;


        public string Key { get; set; }

        /// <summary>
        /// ファイル キャッシュ
        /// </summary>
        public ConcurrentDictionary<string, CachedFile> CacheFiles
        {
            get { return _CacheFiles; }
        }
        private ConcurrentDictionary<string, CachedFile> _CacheFiles = new ConcurrentDictionary<string, CachedFile>();


        public FileCacheManager(string key)
        {
            this.Key = key;
            RestoreCachedFile();
        }

        /// <summary>
        /// キャッシュを取得
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public CachedFile GetCachedFile(string id)
        {
            if (CacheFiles.Keys.Contains(id)) return CacheFiles[id];
            return null;
        }

        /// <summary>
        /// キャッシュエントリの作成
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public CachedFile CreateCacheFileEntry(string id, string path, DateTime lastModified)
        {
            var cache = GetCachedFile(id);
            if (cache != null) ClearCachedFile(id);
            var targetDire = Utils.ApplicationFolder + "\\Cache_" + id;
            var localPath = Path.Combine(targetDire, GetFileName(path));
            CacheFiles[id] = new CachedFile { Id=id, CacheFolder = targetDire, Path = path, LocalPath = localPath, LastModified = lastModified, LastAccessTime = DateTime.Now };
            SaveCachedFile();
            return CacheFiles[id];
        }

        private string GetFileName(string filepath)
        {
            return filepath.Split('/').LastOrDefault();
        }

        /// <summary>
        /// キャッシュ情報のクリア
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public CachedFile ClearCachedFile(string id)
        {
            if (!CacheFiles.Keys.Contains(id)) return null;
            var cacheFolder = CacheFiles[id].CacheFolder;
            if (cacheFolder.StartsWith(Utils.ApplicationFolder) && Directory.Exists(cacheFolder))
            {
                ForceDeleteDirectory(cacheFolder);
            }

            CachedFile cahcework;
            CacheFiles.TryRemove(id, out cahcework);
            SaveCachedFile();
            return null;
        }


        /// <summary>
        /// キャッシュ情報の保存
        /// </summary>
        public void SaveCachedFile()
        {
            lock (CacheFiles)
            {
                var seri = new XmlSerializer(typeof(List<CachedFile>));
                if (!Directory.Exists(Utils.ApplicationFolder)) Directory.CreateDirectory(Utils.ApplicationFolder);
                var file = Path.Combine(Utils.ApplicationFolder, "CachedFile_" + Key + ".xml");
                using (var st = new StreamWriter(file))
                {
                    seri.Serialize(st, CacheFiles.Values.ToList());
                }
            }
        }

        /// <summary>
        /// キャッシュ情報の読み出し
        /// </summary>
        public void RestoreCachedFile()
        {
            Exception firstException = null;
            var seri = new XmlSerializer(typeof(List<CachedFile>));
            var file = Path.Combine(Utils.ApplicationFolder, "CachedFile_" + Key +".xml");
            if (File.Exists(file))
            {
                try
                {
                    using (var st = new StreamReader(file))
                    {
                        _CacheFiles = new ConcurrentDictionary<string, CachedFile>(
                            ((List<CachedFile>)seri.Deserialize(st)).Where(x=>x.Id != null).ToDictionary(x => x.Id, x => x));
                    }

                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString());
                }
            }

            //キャッシュしたファイルが無くなっている場合は削除
            var isDirty = false;
            foreach (var keyval in CacheFiles.ToList())
            {
                if (!File.Exists(keyval.Value.LocalPath))
                {
                    try
                    {
                        ClearCachedFile(keyval.Key);
                    }
                    catch (Exception ex)
                    {
                        firstException = ex;
                    }
                }
            }
            if (isDirty)
            {
                SaveCachedFile();
            }
            if (firstException != null) throw firstException;

            //期限の切れたキャッシュも削除
            foreach (var item in CacheFiles.Where(x => !x.Value.LocalFileCachedTime.HasValue || x.Value.LocalFileCachedTime.Value.AddDays(CachedDays) < DateTime.Now))
            {
                if (item.Value.IsDirty) continue;
                try
                {
                    ClearCachedFile(item.Key);
                }
                catch (Exception ex)
                {
                    firstException = ex;
                }
            }


            if (firstException != null) throw firstException;
        }



        public CachedFile RenameCachedFile(string id, string newPath)
        {
            var cache = GetCachedFile(id);
            if (cache == null) return null;

            cache.Path = newPath;
            var newLocalPath = Path.Combine(Path.GetDirectoryName(cache.LocalPath), newPath.Split('/').Last());

            if (File.Exists(cache.LocalPath))
            {
                if (File.Exists(newLocalPath)) File.Delete(newLocalPath);
                File.Move(cache.LocalPath, newLocalPath);
            }
            cache.LocalPath = newLocalPath;
            SaveCachedFile();
            return cache;
        }

        public void ForceDeleteDirectory(string path)
        {
            var directory = new DirectoryInfo(path) { Attributes = FileAttributes.Normal };

            foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                info.Attributes = FileAttributes.Normal;
            }

            directory.Delete(true);
        }


        public void ForceDeleteFile(string path)
        {
            var file = new FileInfo(path) { Attributes = FileAttributes.Normal };
            file.Delete();
        }

        public static IEnumerable<CachedFile> GteAllCachedFile()
        {
            foreach (var file in Directory.GetFiles(Utils.ApplicationFolder, "CachedFile_*.xml"))
            {
                var key = Path.GetFileNameWithoutExtension(file).Substring("CachedFile_".Length);
                var cache = new FileCacheManager(key);
                foreach (var item in cache.CacheFiles)
                {
                    yield return item.Value;
                }
               
            }
        }
    }
}
