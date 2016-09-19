using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Linq;
using System.Data.Linq.Mapping;

namespace SharePointExplorer.Models
{
    [Table(Name="backupinfo")]
    public class BackupInfo
    {
        [Column(Name = "backupid", CanBeNull = false, IsPrimaryKey = true)]
        public String BackupId { get; set; }

        [Column(Name ="localfilepath", CanBeNull = false, IsPrimaryKey = true)]
        public String LocalFilePath { get; set; }

        [Column(Name ="lastmodifieddate", CanBeNull = false)]
        public string LastModifiedString { get; set; }

        internal DateTime LastModified
        {
            get
            {
                DateTime work;
                if (DateTime.TryParseExact(LastModifiedString, "yyyy/MM/dd HH:mm:ss fff", null, System.Globalization.DateTimeStyles.None, out work))
                {
                    return DateTime.SpecifyKind(work, DateTimeKind.Utc);
                }
                return DateTime.MinValue;
            }
            set { LastModifiedString = value.ToString("yyyy/MM/dd HH:mm:ss fff"); }
        }
    }

    public class BackupFileManager :IDisposable
    {
        SQLiteConnection connection;

        public BackupFileManager()
        {
            var filename = "explorer.db";
            SQLiteConnectionStringBuilder aConnectionString = new SQLiteConnectionStringBuilder
            {
                DataSource = filename
            };
            connection = new SQLiteConnection(aConnectionString.ToString());
            connection.Open();
        }

        public string GetModifiedDate(string backupId, string localFilePath)
        {
            using (DataContext ctx = new DataContext(connection))
            {
                var table = ctx.GetTable<BackupInfo>();
                var oldData = table
                    .Where(x => x.BackupId == backupId && x.LocalFilePath == localFilePath)
                    .ToList();
                if (oldData != null && oldData.Count() > 0)
                {
                    return oldData[0].LastModifiedString;
                }
                else
                {
                    return null;
                }
            }
        }

        public void Entry(BackupInfo data)
        {
            using (DataContext ctx = new DataContext(connection))
            {
                var table = ctx.GetTable<BackupInfo>();
                var oldData = table
                    .Where(x => x.BackupId == data.BackupId && x.LocalFilePath == data.LocalFilePath)
                    .ToList();
                if (oldData != null && oldData.Count() > 0)
                {
                    oldData[0].LastModified = data.LastModified;
                }
                else
                {
                    table.InsertOnSubmit(data);
                }
                ctx.SubmitChanges();
            }
        }

        public void Dispose()
        {
            connection.Dispose();
        }
    }
}
