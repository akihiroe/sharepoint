using SharePointExplorer.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ViewMaker.Core;

namespace SharePointExplorer.Models
{
    [View(typeof(MoveFolderView))]
    public class MoveFolderVM : AppViewModel
    {
       
        public string MovePath { get; set; }
        public bool DialogResult { get; set; }

        private SPFolderItem parent;


        public MoveFolderVM(SPFolderItem parent)
        {
            this.parent = parent;
        }
        public ICommand MoveFolderCommand
        {
            get
            {
                return CreateCommand((x) =>
                {
                    DialogResult = true;
                    this.ExecuteViewCommand(ViewCommands.Close);
                });
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                return CreateCommand((x) =>
                {
                    DialogResult = false;
                    this.ExecuteViewCommand(ViewCommands.Close);
                });
            }
        }
    }
}
