using SharePointExplorer.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ViewMaker.Core;

namespace SharePointExplorer.Models
{
    [View(typeof(LicenseView))]
    public class LicenseVM : AppViewModel
    {
        [Required]
        public string LicenseKey { get; set; }
    
        public bool DialogResult { get; set; }


        public ICommand RegisterCommand
        {
            get
            {
                return CreateCommand((x) =>
                {
                    if (!string.IsNullOrEmpty(LicenseKey))
                    {
                        DialogResult = true;
                        this.ExecuteViewCommand(ViewCommands.Close);
                    }
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
