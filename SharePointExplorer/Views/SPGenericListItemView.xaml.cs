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
using System.ComponentModel;

namespace SharePointExplorer.Views
{
    /// <summary>
    /// SPGenericListItemView.xaml の相互作用ロジック
    /// </summary>
    public partial class SPGenericListItemView : UserControl
    {
        public SPGenericListItemView()
        {
            InitializeComponent();
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
                    _headerColumn.Add(Properties.Resources.MsgOwner, "Owner");

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

    }
}
