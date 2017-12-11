using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ViewMaker;
using ViewMaker.Core;

namespace SharePointExplorer.Models
{
    [View("SharePointExplorer.Views.AccessRightView,SharePointExplorer")]
    public class AccessRightVM : ViewModelBase
    {
        public virtual ClientContext Context
        {
            get { return _context; }
        }
        private ClientContext _context;

        public virtual Web Web
        {
            get { return _web; }
        }
        internal Web _web;

        public List<RoleDefinition> WebRoleDefinitions
        {
            get
            {
                if (_webRoleDefinitions == null)
                {
                    _webRoleDefinitions = Web.RoleDefinitions.Select(x => x).ToList();
                }
                return _webRoleDefinitions;
            }
        }
        private List<RoleDefinition> _webRoleDefinitions;

        public List<User> Users
        {
            get
            {
                if (_users == null)
                {
                    if (string.IsNullOrEmpty(this.Filter))
                    {
                        _users = this.Web.SiteUsers.Where(x => !x.IsHiddenInUI)
                            .ToList();
                    }
                    else
                    {
                        _users = this.Web.SiteUsers.Where(x => !x.IsHiddenInUI)
                            .Where(x => (x.Title != null && x.Title.Contains(this.Filter)) || (x.Email != null && x.Email.Contains(this.Filter)))
                            .ToList();
                    }
                }
                return _users;
            }
        }
        private List<User> _users;

        public string Filter
        {
            get { return _filter; }
            set { _filter = value; _users = null; OnPropertyChanged("Filter", "Users"); }
        }
        private string _filter;

        public User SelectedUser {get; set;}


        public SecurableObject Item
        {
            get
            {
                return _item;
            }
        }
        private SecurableObject _item;

        public AccessRightItem SelectedItem
        {
            get { return _selectedItem; }
            set { _selectedItem = value; OnPropertyChanged("SelectedItem"); }
        }
        private AccessRightItem _selectedItem;

        public bool HasUniqueRoleAssignments
        {
            get
            {
                try
                {
                    return this._item.HasUniqueRoleAssignments;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            set
            {
                if (value)
                {
                    BreakRoleInheritance.Execute(null);
                }
                else
                {
                    ResetRoleInheritance.Execute(null);
                }

            }
        }

        public List<AccessRightItem> RoleAssignments
        {
            get
            {
                if (this._item.HasUniqueRoleAssignments)
                {
                    _roleAssignments = this._item.RoleAssignments.Select(x => new AccessRightItem(x, this)).ToList();
                }
                else
                {
                    _roleAssignments = new List<AccessRightItem>();
                }
                return _roleAssignments;
            }
        }

        private List<AccessRightItem> _roleAssignments;

        public ICommand BreakRoleInheritance
        {
            get
            {
                return this.CreateCommand(() => {

                    var yesNo = this.ShowOKCancel(Properties.Resources.MsgCopyRole);
                    this.Item.BreakRoleInheritance(yesNo, false);
                    this.Context.ExecuteQuery();
                    this.Context.Load(this.Item);
                    var roles = Item.RoleAssignments;
                    Context.Load(roles, x => x.Include(y => y.Member.Title, y => y.Member.LoginName, y => y.RoleDefinitionBindings));
                    Context.Load(Item, x => x.HasUniqueRoleAssignments);
                    this.Context.ExecuteQuery();
                    this.OnPropertyChanged(null);
                });
            }
        }

        public ICommand ResetRoleInheritance
        {
            get
            {
                return this.CreateCommand(() => {
                    this.Item.ResetRoleInheritance();
                    this.Context.ExecuteQuery();
                    this.Context.Load(this.Item);
                    var roles = Item.RoleAssignments;
                    Context.Load(roles, x => x.Include(y => y.Member.Title, y => y.Member.LoginName, y => y.RoleDefinitionBindings));
                    Context.Load(Item, x => x.HasUniqueRoleAssignments);
                    this.Context.ExecuteQuery();
                    this.OnPropertyChanged(null);
                });
            }
        }

        public ICommand AddUser
        {
            get
            {
                return this.CreateCommand(() => {

                    if (this.SelectedUser == null) return;

                    var guest = this.WebRoleDefinitions.Where(x => x.RoleTypeKind == RoleType.Reader).FirstOrDefault();
                    if (guest == null) guest = this.WebRoleDefinitions.Where(x => x.RoleTypeKind == RoleType.Editor).FirstOrDefault();
                    if (guest == null) guest = this.WebRoleDefinitions.Where(x => x.RoleTypeKind == RoleType.Administrator).FirstOrDefault();
                    var bindings = new RoleDefinitionBindingCollection(this.Context);
                    bindings.Add(guest);
                    this.Item.RoleAssignments.Add(this.SelectedUser, bindings);
                    this.Context.Load(this.Item.RoleAssignments);
                    var roles = Item.RoleAssignments;
                    Context.Load(roles, x => x.Include(y => y.Member.Title, y => y.Member.LoginName, y => y.RoleDefinitionBindings));
                    Context.Load(Item, x => x.HasUniqueRoleAssignments);
                    this.Context.ExecuteQuery();
                    _roleAssignments = null;
                    OnPropertyChanged(null);
                });
            }
        }

        public ICommand DeleteUser
        {
            get
            {
                return this.CreateCommand(() => {

                    if (this.SelectedItem == null) return;

                    this.SelectedItem.Assignment.DeleteObject();
                    this.Context.Load(this.Item.RoleAssignments);
                    var roles = Item.RoleAssignments;
                    Context.Load(roles, x => x.Include(y => y.Member.Title, y => y.Member.LoginName, y => y.RoleDefinitionBindings));
                    Context.Load(Item, x => x.HasUniqueRoleAssignments);
                    this.Context.ExecuteQuery();
                    _roleAssignments = null;
                    OnPropertyChanged(null);
                });
            }
        }

        public AccessRightVM(ClientContext context, Web web, SecurableObject item)
        {
            this._context = context;
            this._web = web;
            this._item = item;
            this._selectedItem = null;

            Context.Load(web.RoleDefinitions);
            Context.Load(web.SiteUsers);
            Context.Load(item);
            Context.ExecuteQuery();
            Context.Load(item,
                y => y.HasUniqueRoleAssignments,
                y => y.RoleAssignments
                .Include(x => x.Member.LoginName, x => x.Member.Title, x => x.RoleDefinitionBindings));
            Context.ExecuteQuery();
        }



    }
}
