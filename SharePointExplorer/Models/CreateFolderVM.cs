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
    [View(typeof(CreateFolderView))]
    public class CreateFolderVM : AppViewModel
    {
        public string NewFolderName { get; set; }
        public bool DialogResult { get; set; }

        private SPFolderItem parent;


        public CreateFolderVM(SPFolderItem parent)
        {
            this.parent = parent;
        }
        public ICommand CreateFolderCommand
        {
            get
            {
                return CreateCommand((x) =>
                {
                    ExecuteActionAsync(parent.CreateNewFolder(NewFolderName),(t)=>{
                        this.ExecuteViewCommand(ViewCommands.Close);
                    });
                });
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                return CreateCommand((x) =>
                {
                    this.ExecuteViewCommand(ViewCommands.Close);
                });
            }
        }
    }
}
