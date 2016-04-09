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
    public abstract class TreeItem : AppViewModel
    {
        public abstract string Name { get; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; OnPropertyChanged("IsSelected"); }
        }
        private bool _isSelected;


        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; OnPropertyChanged("IsExpanded"); }
        }
        private bool _isExpanded;

        public bool LoadedChildren
        {
            get { return _loadedChildren; }
            set { _loadedChildren = value; }
        }
        private bool _loadedChildren;

        public ObservableCollection<TreeItem> Children { get; private set; }

        public TreeItem Parent { get; private set; }

        public TreeItem(TreeItem parent)
        {
            this.Parent = parent;
            this.Children = new ObservableCollection<TreeItem>();
        }

        public TreeItem()
        {
        }

        public async Task EnsureChildren(bool force = false)
        {
            if (!LoadedChildren || force)
            {
                LoadedChildren = true;
                Children.Clear();
                await LoadChildren();
                OnPropertyChanged("Children");
            }
        }

        protected virtual async Task LoadChildren()
        {
            await Task.Delay(0);
        }

        public TreeItem FindRoot()
        {
            if (this.Parent == null) return this;
            return this.Parent.FindRoot();
        }


        public TreeItem FindNode(string findStr, bool childOnly = false)
        {
            if (!childOnly && this.Parent != null && Parent.Children.IndexOf(this) < Parent.Children.Count - 1)
            {
                foreach (var item in Parent.Children.SkipWhile(x => x != this).Skip(1))
                {
                    if (item.Name.ToLower().Contains(findStr.ToLower()))
                    {
                        item.IsSelected = true;
                        return item;
                    }
                }
            }
            foreach (var child in this.Children)
            {
                if (child.Name.ToLower().Contains(findStr.ToLower()))
                {
                    child.IsSelected = true;
                    this.IsExpanded = true;
                    return child;
                }
            }
            foreach (var child in Children)
            {
                var result = child.FindNode(findStr, true);
                if (result != null)
                {
                    this.IsExpanded = true;
                    return result;
                }
            }
            if (!childOnly && this.Parent != null && Parent.Children.IndexOf(this) < Parent.Children.Count - 1)
            {
                foreach (var item in Parent.Children.SkipWhile(x => x != this).Skip(1))
                {
                    var result = item.FindNode(findStr, true);
                    if (result != null)
                    {
                        this.IsExpanded = true;
                        return result;
                    }
                }
            }
            return null;
        }

        public virtual string Icon
        {
            get
            {
                return "/SharePointExplorer;Component/Images/folder.png";
            }
        }

        public ICommand RefreshCommand { get { return CreateCommand((x)=>ExecuteActionAsync(Refresh(x))); } }
        protected virtual async Task Refresh(object obj)
        {
            await EnsureChildren(true);            
        }

        public void SetDirty()
        {
            this.LoadedChildren = false;
        }
        
    }
}
