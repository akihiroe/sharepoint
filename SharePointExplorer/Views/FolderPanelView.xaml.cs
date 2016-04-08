using SharePointExplorer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SharePointExplorer.Views
{
    /// <summary>
    /// FolderPanelView.xaml の相互作用ロジック
    /// </summary>
    public partial class FolderPanelView : UserControl
    {
        public FolderPanelView()
        {
            InitializeComponent();
        }

        private void Items_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = this.DataContext as SPFolderItem;
            var item = ((FrameworkElement)e.OriginalSource).DataContext;
            if (vm != null) vm.ExecuteFileCommand.Execute(item);

        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var item = ((FrameworkElement)e.OriginalSource).DataContext as SPFileItem;
                if (item != null && item.IsEditing) item.RenameCommand.Execute(null);
            }
            if (e.Key == Key.Escape)
            {
                var item = ((FrameworkElement)e.OriginalSource).DataContext as SPFileItem;
                if (item != null && item.IsEditing) item.CancelRenameCommand.Execute(null);

            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var item = ((FrameworkElement)e.OriginalSource).DataContext as SPFileItem;
            if (item != null && item.IsEditing) item.RenameCommand.Execute(null);
        }

        private void TextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var text = (TextBox)sender;
            if (text.Visibility == System.Windows.Visibility.Visible)
            {
                text.Focus();
                text.Select(0, 0);
            }
        }

        private void Items_Drop(object sender, DragEventArgs e)
        {
            if (e.Data == null) return;

            var vm = this.DataContext as SPFolderItem;
            IDataObject dataObj = e.Data as IDataObject;
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                vm.UploadCommand.Execute(files);
            }
        }

        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private Dictionary<string, string> _headerColumn;

        private Dictionary<string, string> HeaderColumn
        {
            get
            {
                if (_headerColumn == null)
                {
                    _headerColumn = new Dictionary<string, string>();
                    _headerColumn.Add(Properties.Resources.MsgName, "Name");
                    _headerColumn.Add(Properties.Resources.MsgModifiedDate, "LastModified");
                    _headerColumn.Add(Properties.Resources.MsgSize, "Size");
                    _headerColumn.Add(Properties.Resources.MsgOwner, "Owner");
                    _headerColumn.Add(Properties.Resources.MsgCheckedOutUser, "CheckedOut");

                }
                return _headerColumn;
            }
        }


        void GridViewColumnHeaderClickedHandler(object sender,
                                        RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked =
                  e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }


                    string header = headerClicked.Column.Header as string;
                    HeaderColumn.TryGetValue(header, out header);
                    Sort(header, direction);


                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }

        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView =
              CollectionViewSource.GetDefaultView(Items.ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        private Point startPoint;
        private bool isButtonDown;

        private void Items_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
            isButtonDown = true;
        }

        private void Items_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            isButtonDown = false;
        }

        private void Items_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || !isButtonDown)
            {
                return;
            }

            // Get the current mouse position
            Point mousePos = e.GetPosition(null);
            Vector diff = startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var vm = this.DataContext as SPFolderItem;
                var dragData = vm.CreateFilesDataObject(false);
                if (dragData != null)
                {
                    isButtonDown = false;
                    VirtualFileDataObject.DoDragDrop(sender as DependencyObject, dragData, DragDropEffects.Copy);
                }
            }
        }

    }
}
