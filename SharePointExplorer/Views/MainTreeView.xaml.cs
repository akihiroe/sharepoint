using SharePointExplorer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
