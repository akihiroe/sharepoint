using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SharePointExplorer.Models
{
    public static class Utils
    {
        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra, [Out] StringBuilder pszOut, [In][Out] ref uint pcchOut);

        public static string FileExtentionInfo(AssocStr assocStr, string doctype)
        {
            uint pcchOut = 0;
            AssocQueryString(AssocF.Verify, assocStr, doctype, null, null, ref pcchOut);

            StringBuilder pszOut = new StringBuilder((int)pcchOut);
            AssocQueryString(AssocF.Verify, assocStr, doctype, null, pszOut, ref pcchOut);
            return pszOut.ToString();
        }

        [Flags]
        public enum AssocF
        {
            Init_NoRemapCLSID = 0x1,
            Init_ByExeName = 0x2,
            Open_ByExeName = 0x2,
            Init_DefaultToStar = 0x4,
            Init_DefaultToFolder = 0x8,
            NoUserSettings = 0x10,
            NoTruncate = 0x20,
            Verify = 0x40,
            RemapRunDll = 0x80,
            NoFixUps = 0x100,
            IgnoreBaseClass = 0x200
        }

        public enum AssocStr
        {
            Command = 1,
            Executable,
            FriendlyDocName,
            FriendlyAppName,
            NoOpen,
            ShellNewValue,
            DDECommand,
            DDEIfExec,
            DDEApplication,
            DDETopic
        }


        public static string ApplicationFolder
        {
            get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SharePointExplorer"); }
        }

        static string[] rmsOfficeNativeFile = "doc,dot,xla,xls,xlt,pps,ppt,docm,docx,dotm,dotx,xlam,xlsb,xlsm,xlsx,xltm,xltx,xps,potm,potx,ppsx,ppsm,pptm,pptx,thmx".Split(',');
        static string[] rmsSupportFile = "txt,xml,jpg,jpeg,pdf,png,tiff,bmp,gif,giff,jpe,jfif,jif".Split(',');

        public static string ConvertRmsProtectedFileName(string filename)
        {
            var ext = System.IO.Path.GetExtension(filename).ToLower();
            if (ext.Length > 0) ext = ext.Substring(1);

            if (rmsOfficeNativeFile.Contains(ext)) return filename;
            if (rmsSupportFile.Contains(ext)) return filename.Substring(0, filename.Length - ext.Length) + "p" + ext;
            return filename + ".pfile";
        }

        public static bool IsFileLocked(string filePath)
        {
            try
            {
                using (File.Open(filePath, FileMode.Open)) { }
            }
            catch (IOException e)
            {
                var errorCode = Marshal.GetHRForException(e) & ((1 << 16) - 1);

                return errorCode == 32 || errorCode == 33;
            }

            return false;
        }

        static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        public static string SizeSuffix(Int64 value)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return ""; }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
        }

        private static byte[] entropy = new byte[] { 0x88, 0xa0, 0x22, 0x04 };

        public static string EncryptedPassword(string password)
        {
            var encr = ProtectedData.Protect(Encoding.UTF8.GetBytes(password), entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encr);
        }

        public static string DecryptedPassword(string password)
        {
            var unenc = ProtectedData.Unprotect(Convert.FromBase64String(password), entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(unenc);
        }


    }

}
