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
    [View(typeof(ScriptEditorView))]
    public class ScriptEditorVM : AppViewModel
    {
        public string Code { get; set; }

        public bool DialogResult { get; set; }

        public ICommand SaveCommand {
            get
            {
                return CreateCommand(() => {
                    DialogResult = true;
                    ExecuteViewCommand(ViewCommands.Close);
                });
            }
        }
        public ICommand CloseCommand
        {
            get
            {
                return CreateCommand(() => {
                    DialogResult = false;
                    ExecuteViewCommand(ViewCommands.Close);
                });
            }
        }
    }
}
