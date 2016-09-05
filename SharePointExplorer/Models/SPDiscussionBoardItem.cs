using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ViewMaker;

namespace SharePointExplorer.Models
{
    public class SPDiscussionBoardItem : SPTreeItem
    {

        public override string Name
        {
            get { return List.Title; }
        }

        public override string Path
        {
            get { return null; }
        }

        public List List
        {
            get { return _list; }
        }
        private List _list;

        public ObservableCollection<SPTopicItem> Items { get; private set; }


        public SPTopicItem SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                OnPropertyChanged("SelectedItem");
            }
        }

        private SPTopicItem _selectedItem;

        public object CurrentContent
        {
            get { return _currentContent; }
            set { _currentContent = value; OnPropertyChanged("CurrentContent"); }
        }
        public object _currentContent;

        public SPDiscussionBoardItem(TreeItem parent, Web web, ClientContext context, List list)
            : base(parent, web, context)
        {
            Items = new ObservableCollection<SPTopicItem>();
            _list = list;
        }

        protected override async Task LoadChildren()
        {
            Items.Clear();

            ListItemCollection topics = null;
            await Task.Run(() => {
                var q = CamlQuery.CreateAllFoldersQuery();
                topics = List.GetItems(q);
                Context.Load(topics, x => x.Include(y => y.DisplayName, y => y["FileRef"], y => y["Body"]));
                Context.ExecuteQuery();
            });


            foreach (var topic in topics)
            {
                Items.Add(new SPTopicItem(this, Context, List, topic));
            }
        }

        public ICommand SelectedItemChangedCommand
        {
            get { return this.CreateCommand((x) => { ExecuteActionAsync(GenerateContent(x)); }); }
        }

        private async Task GenerateContent(object arg)
        {
            await SelectedItem.EnsureChildren();
            this.CurrentContent = ViewUtil.BuildContent(SelectedItem);
        }

        public override string SPUrl
        {
            get
            {
                return Context.Url + "/" + List.Title;
            }
        }
    }
}
