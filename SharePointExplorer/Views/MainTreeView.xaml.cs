using SharePointExplorer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    /// MainTreeView.xaml の相互作用ロジック
    /// </summary>
    public partial class MainTreeView : UserControl
    {
        public MainTreeView()
        {
            InitializeComponent();
        }

        private void FolderTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var item = ((FrameworkElement)e.OriginalSource).DataContext as SPFolderItem;
                if (item != null && item.IsFolderEditing) item.RenameFolderCommand.Execute(null);
            }
            if (e.Key == Key.Escape)
            {
                var item = ((FrameworkElement)e.OriginalSource).DataContext as SPFolderItem;
                if (item != null && item.IsFolderEditing) item.CancelRenameFolderCommand.Execute(null);

            }
        }

        private void FolderTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var item = ((FrameworkElement)e.OriginalSource).DataContext as SPFolderItem;
            if (item != null && item.IsFolderEditing) item.RenameFolderCommand.Execute(null);

        }

        private void FolderTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var text = (TextBox)sender;
            if (text.Visibility == System.Windows.Visibility.Visible)
            {
                text.Focus();
                text.Select(0, 0);
            }
        }

        private void Folders_Drop(object sender, DragEventArgs e)
        {
            var item = FindAnchestor<TreeViewItem>(e.OriginalSource as DependencyObject);
            if (item == null) throw new NotSupportedException();
            var vm = item.DataContext as SPFolderItem;
            if (vm == null) throw new NotSupportedException();

            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            var folder = e.Data.GetData(DataFormats.StringFormat) as string;
            var data = e.Data as IDataObject;
            if (files != null)
            {
                vm.UploadCommand.Execute(files);
            }
            else if (data != null)
            {
                var st = data.GetData(DataFormats.Serializable) as MemoryStream;
                if (st != null)
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    files = (string[])bin.Deserialize(st);
                    vm.MoveFolderCommand.Execute(files);
                }
                else if (folder != null && folder != vm.SPUrl)
                {
                    vm.MoveFolderCommand.Execute(new string[] { folder });
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (folder != null && folder != vm.SPUrl)
            {
                vm.MoveFolderCommand.Execute(new string[] { folder });
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static T FindAnchestor<T>(DependencyObject current)
            where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        private Point _foldersOrigin;
        private bool _isFoldersButtonDown;

        private void Folders_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _foldersOrigin = e.GetPosition(this);
            _isFoldersButtonDown = true;
        }

        private void Folders_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isFoldersButtonDown = false;
        }

        private void Folders_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || !_isFoldersButtonDown)
            {
                return;
            }

            var fe = e.OriginalSource as FrameworkElement;
            if (fe == null || !(fe.DataContext is SPFolderItem) || !((SPFolderItem)fe.DataContext).LoadedChildren ) return;

            var point = e.GetPosition(this);
            if (CheckDistance(point, _foldersOrigin))
            {
                if (_isFoldersButtonDown)
                {
                    var item = Folders.SelectedItem as SPFolderItem;
                    if (item != null)
                    {
                        try
                        {
                            DragDrop.DoDragDrop(sender as DependencyObject, item.SPUrl, DragDropEffects.Move);
                        }
                        catch (NotSupportedException)
                        {
                        }
                    }
                    _isFoldersButtonDown = false;
                }

                e.Handled = true;
            }
        }

        private bool CheckDistance(Point x, Point y)
        {
            return Math.Abs(x.X - y.X) >= SystemParameters.MinimumHorizontalDragDistance * 4 ||
                Math.Abs(x.Y - y.Y) >= SystemParameters.MinimumVerticalDragDistance * 4;
        }
    }
}
