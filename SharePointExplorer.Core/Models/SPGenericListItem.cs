using Ionic.Zip;
using Microsoft.SharePoint.Client;
using SharePointExplorer.Core;
using SharePointExplorer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ViewMaker;
using ViewMaker.Core;

namespace SharePointExplorer.Models
{
    [View("SharePointExplorer.Views.SPGenericListItemView,SharePointExplorer")]
    public class SPGenericListItem : SPTreeItem
    {

        public override string Name
        {
            get { return List.Title; }
        }

        public override string Path
        {
            get { return List.RootFolder.ServerRelativeUrl; }
        }

        public List List
        {
            get { return _list; }
        }
        private List _list;

        public ObservableCollection<SPListItem> Items { get; private set; }


        public SPListItem SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                OnPropertyChanged("SelectedItem");
            }
        }

        private SPListItem _selectedItem;

        public object CurrentContent
        {
            get { return _currentContent; }
            set { _currentContent = value; OnPropertyChanged("CurrentContent"); }
        }
        public object _currentContent;

        public SPGenericListItem(TreeItem parent, Web web, ClientContext context, List list)
            : base(parent, web, context)
        {
            Items = new ObservableCollection<SPListItem>();
            _list = list;
        }

        protected override async Task LoadChildren(int depth = 1)
        {
            Items.Clear();

            ListItemCollection li = null;
            await Task.Run(() =>
            {
                Context.Load(List, x => x.HasUniqueRoleAssignments);

                var roles = List.RoleAssignments;
                Context.Load(roles, x => x.Include(y => y.Member.Title, y => y.Member.LoginName, y => y.RoleDefinitionBindings));

                li = List.GetItems(new CamlQuery());
                Context.Load(li);
                Context.Load(li,  x => x.Include(y=>y.Id, y => y.DisplayName, y => y.AttachmentFiles, y=>y.HasUniqueRoleAssignments));
                Context.ExecuteQuery();
                //foreach (var item in li)
                //{
                //    foreach (var role in item.RoleAssignments)
                //    {
                //        Context.Load(role, x =>x.Member.LoginName, x=>x.Member.Title,  x=>x.RoleDefinitionBindings);
                //    }
                //}
                //Context.ExecuteQuery();
            });
            foreach (var item in li)
            {
                Items.Add(new SPListItem(this, this.Web, this.Context, item));
            }
        }

        public override SecurableObject SecurableItem
        {
            get
            {
                return this.List;
            }
        }

        public ICommand ShowItemAccessRight
        {
            get
            {
                return this.SelectedItem.ShowAccessRight;
            }
        }
        //public ICommand BreakRoleInheritance
        //{
        //    get
        //    {
        //        return this.CreateCommand(() => {
        //            this.SelectedItem.Item.BreakRoleInheritance(false, false);
        //            this.Context.ExecuteQuery();
        //        });
        //    }
        //}

        //public ICommand ResetRoleInheritance
        //{
        //    get
        //    {
        //        return this.CreateCommand(() => {
        //            this.SelectedItem.Item.ResetRoleInheritance();
        //            this.Context.ExecuteQuery();
        //        });
        //    }
        //}

        public override bool AvailableUploadFolder
        {
            get { return true; }
        }
        public override bool AvailableDownloadFolder
        {
            get { return true; }
        }

        public override ICommand UploadFolderCommand
        {
            get { return this.CreateCommand((x) => { ExecuteActionAsync(ImportAction(x)); }); }
        }

        public override ICommand DownloadFolderCommand
        {
            get { return this.CreateCommand((x) => { ExecuteActionAsync(ExportAction(x)); }); }
        }

        public ICommand Export
        {
            get { return this.CreateCommand((x) => { ExecuteActionAsync(ExportAction(x)); }); }
        }
        public ICommand Import
        {
            get { return this.CreateCommand((x) => { ExecuteActionAsync(ImportAction(x)); }); }
        }


        private string[] mustFields = new string[] { "ID", "Title", "Author", "Editor", "Created", "Modified", "Attachments", "Expired" };

        private string GetUserFeildValueString(FieldUserValue user)
        {
            return user.Email;
        }

        private string StringToCSVCell(string str)
        {
            bool mustQuote = (str.Contains(",") || str.Contains("\"") || str.Contains("\r") || str.Contains("\n"));
            if (mustQuote)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("\"");
                foreach (char nextChar in str)
                {
                    sb.Append(nextChar);
                    if (nextChar == '"')
                        sb.Append("\"");
                }
                sb.Append("\"");
                return sb.ToString();
            }

            return str;
        }

        private async Task ExportAction(object arg)
        {
            var filename = this.ShowSaveDialog("*.zip|*.zip");
            if (filename == null)
            {
                return;
            }
            await Task.Run(() =>
            {
                var exportFields = new List<Field>();
                var fields = List.Fields;
                this.Context.Load(fields);
                this.Context.ExecuteQuery();
                foreach (var field in fields)
                {
                    if (!mustFields.Contains(field.InternalName))
                    {
                        if (field.ReadOnlyField || field.Hidden || field.TypeAsString == "Computed")
                        {
                            continue;
                        }
                    }
                    exportFields.Add(field);
                }

                var items = List.GetItems(new CamlQuery());
                this.Context.Load(items);
                this.Context.Load(items, x => x.Include(y => y.AttachmentFiles));
                this.Context.ExecuteQuery();

                var tempFiles = new List<string>();

                try
                {
                    using (var zip = new ZipFile())
                    {
                        zip.AlternateEncoding = Encoding.GetEncoding("sjis");
                        zip.AlternateEncodingUsage = ZipOption.AsNecessary;

                        var csv = System.IO.Path.GetTempFileName();
                        tempFiles.Add(csv);

                        using (var fp = new StreamWriter(csv, false, Encoding.UTF8))
                        {
                            var fieldNames = new List<string>();
                            foreach (var f in exportFields)
                            {
                                fieldNames.Add(f.Title);
                            }
                            fp.WriteLine(string.Join(",", fieldNames));

                            foreach (var item in items)
                            {
                                var outputs = new List<string>();
                                foreach (var f in exportFields)
                                {
                                    string valueString;
                                    object value = null;
                                    if (item.FieldValues.ContainsKey(f.InternalName)){
                                        value = item.FieldValues[f.InternalName];
                                    }
                                    if (f.TypeAsString == "User")
                                    {
                                        if (value != null)
                                        {
                                            value = GetUserFeildValueString(value as FieldUserValue);
                                        }
                                    }
                                    if (f.TypeAsString == "UserMulti")
                                    {
                                        if (value != null)
                                        {
                                            value = string.Join(",", (value as FieldUserValue[]).Select(x => GetUserFeildValueString(x)));
                                        }
                                    }

                                    if (value != null)
                                    {
                                        valueString = StringToCSVCell(value.ToString());
                                    }
                                    else
                                    {
                                        valueString = "";
                                    }
                                    outputs.Add(valueString);
                                }
                                fp.WriteLine(string.Join(",", outputs));

                                var attachmentFiles = item.AttachmentFiles;
                                if (attachmentFiles.Count > 0)
                                {
                                    foreach (var attach in attachmentFiles)
                                    {
                                        var temp = System.IO.Path.GetTempFileName();
                                        var fileInfo = Microsoft.SharePoint.Client.File.OpenBinaryDirect(this.Context, attach.ServerRelativeUrl);
                                        using (var target = System.IO.File.Open(temp, FileMode.Create))
                                        {
                                            fileInfo.Stream.CopyTo(target);
                                        }
                                        var e = zip.AddFile(temp, "");
                                        e.FileName = item.Id.ToString() + "_" + attach.FileName;
                                        tempFiles.Add(temp);
                                    }
                                }

                            }
                        }
                        var c = zip.AddFile(csv);
                        c.FileName = "data.csv";
                        zip.Save(filename);
                    }
                }
                finally
                {
                    foreach (var f in tempFiles)
                    {
                        System.IO.File.Delete(f);
                    }
                }
            });
        }

        private async Task ImportAction(object arg)
        {
            var skipFields = new String[] { "ID", "Author", "Editor", "Created", "Modified", "Attachments", "ContentType" };

            var filename = this.ShowOpenDialog("*.zip|*.zip");
            if (filename == null)
            {
                return;
            }

            await Task.Run(() =>
            {
                var exportFields = new List<Field>();
                var fields = List.Fields;
                this.Context.Load(fields);
                this.Context.ExecuteQuery();
                foreach (var field in fields)
                {
                    if (!mustFields.Contains(field.InternalName))
                    {
                        if (field.ReadOnlyField || field.Hidden || field.TypeAsString == "Computed")
                        {
                            continue;
                        }
                    }
                    exportFields.Add(field);
                }

                var tempFiles = new List<string>();
                try
                {
                    using (var zip = new ZipFile(filename))
                    {
                        zip.AlternateEncoding = Encoding.GetEncoding("sjis");
                        zip.AlternateEncodingUsage = ZipOption.AsNecessary;

                        Tuple<IList<string>, IEnumerable<IList<string>>> csvText = null;
                        foreach (var entry in zip)
                        {
                            if (entry.FileName == "data.csv")
                            {
                                using (var st = entry.OpenReader())
                                {
                                    using (var st2 = new StreamReader(st))
                                    {
                                        csvText = CsvParser.ParseHeadAndTail(st2, ',', '"');


                                        if (csvText == null) throw new Exception("invalid backup zip file");

                                        var header = csvText.Item1;
                                        var lines = csvText.Item2;
                                        var idxId = header.IndexOf("ID");
                                        foreach (var line in lines)
                                        {
                                            var createInfo = new ListItemCreationInformation();
                                            var target = List.AddItem(createInfo);
                                            foreach (var f in exportFields)
                                            {
                                                if (skipFields.Contains(f.InternalName)) continue;
                                                if (f.ReadOnlyField) continue;
                                                var idx = header.IndexOf(f.Title);
                                                if (idx < 0) continue;
                                                var targetValue = ConvertValueAsType(f, line[idx], new List<FieldUserValue>());

                                                if (targetValue != null)
                                                {
                                                    target[f.InternalName] = targetValue;
                                                }
                                            }
                                            target.Update();
                                            this.Context.ExecuteQuery();
                                            if (idxId >= 0)
                                            {
                                                foreach (var attachment in zip)
                                                {
                                                    if (attachment.FileName.StartsWith(line[idxId] + "_"))
                                                    {
                                                        using (var sta = attachment.OpenReader())
                                                        {
                                                            var newAttatch = new AttachmentCreationInformation();
                                                            newAttatch.FileName = attachment.FileName.Substring(attachment.FileName.IndexOf("_"));
                                                            newAttatch.ContentStream = sta;
                                                            target.AttachmentFiles.Add(newAttatch);
                                                            this.Context.ExecuteQuery();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }
                }
                finally
                {
                    foreach (var f in tempFiles)
                    {
                        System.IO.File.Delete(f);
                    }
                }
            });
        }

        private object ConvertValueAsType(Field f, string text,List<FieldUserValue> users)
        {
            if (string.IsNullOrEmpty(text)) return null;
            switch (f.TypeAsString)
            {
                case "Note":
                case "Text":
                case "Choice":
                case "URL":
                    return text;
                case "Number":
                    return Double.Parse(text);
                case "DateTime":
                    return DateTime.Parse(text);
                case "Boolean":
                    return bool.Parse(text);
                case "User":
                    return users.Where(x=>x.Email == text).FirstOrDefault();
                case "UserMulti":
                    return  text.Split(',').Select(x=> users.Where(y => y.Email == x).FirstOrDefault()).Where(x=> x!= null).ToList();
            }
            return null;
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
                        this.SelectedItem.Item.DeleteObject();
                        try
                        {
                            Context.ExecuteQuery();
                        }
                        catch
                        {
                            //リトライあり
                            this.SelectedItem.Item.DeleteObject();
                        }
                        ExecuteUIProc(() => {
                            this.Items.Remove(item);
                        });
                    }
                }
            });

        }


        public ICommand SelectedItemChangedCommand
        {
            get { return this.CreateCommand((x) => { ExecuteActionAsync(GenerateContent(x)); }); }
        }

        private async Task GenerateContent(object arg)
        {
            //await SelectedItem.EnsureChildren();
            this.CurrentContent = ViewUtil.BuildContent(new { Id = List.Id, Title = List.Title, Url = List,SPUrl });
        }

        public override string SPUrl
        {
            get
            {
                return Context.Site.Url + List.RootFolder.ServerRelativeUrl;
            }
        }

        public string SettingUrl
        {
            get
            {
                return Context.Site.Url + Web.RootFolder.ServerRelativeUrl + "_layouts/15/listedit.aspx?List=%7B" +  List.Id.ToString().Replace("-","%2D") +"%7D";
            }
        }

        public string AccessRight
        {
            get
            {
                var access = string.Join(" | ", this.List.RoleAssignments
                    .Select(x => x.Member.Title + ":" + string.Join(",", x.RoleDefinitionBindings.Select(z => z.Name))));
                if (this.List.HasUniqueRoleAssignments)
                {
                    return "(" + access + ")";
                }
                else
                {
                    return access;
                }
            }
        }

        public override string Icon
        {
            get
            {
                return "/SharePointExplorer;Component/Images/sharepointlist.png";
            }
        }

        protected override void OpenWebSite(object obj)
        {
            Process.Start(SPUrl);
        }

    }
}
