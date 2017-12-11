using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ViewMaker.Core;

namespace SharePointExplorer.Models
{
    public class SPListItem : SPTreeItem
    {
        public override string Name
        {
            get { return Item.DisplayName; }
        }

        public virtual string Owner
        {
            get
            {
                try
                {
                    var u = Item.FieldValues["Author"] as FieldUserValue;
                    return u?.LookupValue;
                }
                catch (Exception)
                {
                    return "";
                }

            }
        }

        public virtual DateTime? Modified
        {
            get { return Item.FieldValues["Modified"] as DateTime?; }
        }

        public string LocalModified
        {
            get
            {
                if (Modified.HasValue)
                {
                    return System.TimeZone.CurrentTimeZone.ToLocalTime(Modified.Value).ToString(ExplorerSettings.Instance.DateFormat);
                }
                else
                {
                    return null;
                }
            }
        }


        public override string Path
        {
            get { return ((SPGenericListItem)this.Parent).Path + "/" + Item.Id; }
        }

        public ListItem Item
        {
            get { return _item; }
        }
        private ListItem _item;

        public SPListItem(TreeItem parent, Web web, ClientContext context, ListItem item)
            : base(parent, web, context)

        {
            this._item = item;
        }

        public override string SPUrl
        {
            get
            {
                return ((SPGenericListItem)this.Parent).SPUrl + "/" + Item.Id;
            }
        }

        public override SecurableObject SecurableItem
        {
            get
            {
                return this.Item;
            }
        }


        public bool HasUniqueRoleAssignment
        {
            get { return this.Item.HasUniqueRoleAssignments; }
        }


        public string AccessRight
        {
            get
            {
                return HasUniqueRoleAssignment.ToString();
                //var access = string.Join(" | ", this.Item.RoleAssignments
                //        .Select(x => x.Member.Title + ":" + string.Join(",", x.RoleDefinitionBindings.Select(z => z.Name))));

                //if (string.IsNullOrEmpty(access)) access = "none";
                //if (this.Item.HasUniqueRoleAssignments)
                //{
                //    return "";
                //}
                //else
                //{
                //    return access;
                //}
            }
        }
    }
}
