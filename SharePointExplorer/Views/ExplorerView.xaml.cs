using SharePointExplorer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace SharePointExplorer.Views
{
    /// <summary>
    /// ExplorerView.xaml の相互作用ロジック
    /// </summary>
    public partial class ExplorerView : Window
    {
        ExplorerVM vm;

        public ExplorerView()
        {
            InitializeComponent();
            vm = new ExplorerVM();
            vm.LoadSettings();
            this.DataContext = vm;
            vm.PropertyChanged += Vm_PropertyChanged;
            vm.ViewCommandNotified += Vm_ViewCommandNotified;
        }

        private void Vm_ViewCommandNotified(object sender, ViewMaker.Core.ViewCommandEventArgs e)
        {
            if (e.Command == "Close") this.Close();
        }

        private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var vm = (ExplorerVM)this.DataContext;
                vm.SearchCommand.Execute(((TextBox)sender).Text);
            }

        }

        public void DataFormats_Click(object sender, RoutedEventArgs e)
        {
            var format = (string)((MenuItem)sender).DataContext;
            ExplorerSettings.Instance.DateFormat = format;
            ExplorerSettings.Instance.Save();
            var vm = (ExplorerVM)this.DataContext;
        }

        private void ProcessingPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }

        private void Cancel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }
    }
}
