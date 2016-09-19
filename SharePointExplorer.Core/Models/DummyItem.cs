using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePointExplorer.Models
{
    public class DummyItem : TreeItem
    {
        private string _name;
        public DummyItem(TreeItem item, string name)
            :base(item)
        {
            _name = name;
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }
    }
}
