using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ViewMaker.Core;

namespace SharePointExplorer.Models
{
    public class SPTopicItem : TreeItem
    {
        public override string Name
        {
            get { return Topic.DisplayName; }
        }

        public string Contents
        {
            get
            {
                return Body + "\n" + string.Join("\n", Items.Select(x => x.Body));
            }
        }

        public string Body
        {
            get { return Topic["Body"] as string; }
        }

        public ClientContext Context
        {
            get { return _context; }
        }
        private ClientContext _context;

        public List List
        {
            get { return _list; }
        }
        private List _list;

        public ListItem Topic
        {
            get { return _topic; }
        }
        private ListItem _topic;

        public ObservableCollection<SPTopicReplayItem> Items { get; private set; }


        public SPTopicItem(TreeItem parent, ClientContext context, List list, ListItem topic)
            : base(parent)
        {
            Items = new ObservableCollection<SPTopicReplayItem>();
            _topic = topic;
            _list = list;
            _context = context;
        }

        protected override async Task LoadChildren()
        {
            Items.Clear();
            LoadChildren(Topic["FileRef"].ToString());
            await Task.Delay(0);
        }

        private void LoadChildren(string fileRef)
        {
            var q = CamlQuery.CreateAllItemsQuery(100, "Title", "FileRef", "Body");
            q.FolderServerRelativeUrl = fileRef;
            ListItemCollection replies = List.GetItems(q);
            Context.Load(replies);
            Context.ExecuteQueryWithIncrementalRetry();

            foreach (var replay in replies)
            {
                Items.Add(new SPTopicReplayItem(this, Context, replay));
                LoadChildren(replay["FileRef"].ToString());
            }
        }
    }
}
