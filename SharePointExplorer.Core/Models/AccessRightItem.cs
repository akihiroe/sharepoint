using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewMaker;

namespace SharePointExplorer.Models
{
    public class AccessRightItem : ViewModelBase
    {
        public RoleAssignment Assignment  { get; set; }
        public AccessRightVM Parent { get; set; }
        public List<RoleDefinition> WebRoleDefinitions { get { return this.Parent.WebRoleDefinitions; } }

        public string Title
        {
            get { return Assignment.Member.Title; }
        }
        public string[] RoleDefinitions
        {
            get { return WebRoleDefinitions.Select(x => x.Name).ToArray(); }
        }
        public string SelectedRoleDefinition { get; set; }

        public string[] Bindings
        {
            get { return Assignment.RoleDefinitionBindings.Select(x=>x.Name).ToArray(); }
        }

        public string SelectedBinding { get; set; }

        public string BindingsString
        {
            get { return string.Join(",", Bindings); }
        }

        public System.Windows.Input.ICommand DeleteRight
        {
            get
            {
                return this.CreateCommand(() => {
                    if (string.IsNullOrEmpty(this.SelectedBinding)) return;
                    var selected = Assignment.RoleDefinitionBindings.Where(x => x.Name == this.SelectedBinding).FirstOrDefault();
                    if (selected != null)
                    {
                        Assignment.RoleDefinitionBindings.Remove(selected);
                        Assignment.Update();
                        this.Parent.Context.Load(Assignment);
                        this.Parent.Context.ExecuteQuery();
                        OnPropertyChanged(null);
                    }
                });
            }
        }
        public System.Windows.Input.ICommand AddRight
        {
            get
            {
                return this.CreateCommand(() => {
                    if (string.IsNullOrEmpty(this.SelectedRoleDefinition)) return;
                    var selected = WebRoleDefinitions.Where(x => x.Name == this.SelectedRoleDefinition).FirstOrDefault();
                    if (selected != null)
                    {
                        Assignment.RoleDefinitionBindings.Add(selected);
                        Assignment.Update();
                        this.Parent.Context.Load(Assignment);
                        this.Parent.Context.ExecuteQuery();
                        OnPropertyChanged(null);
                    }
                });
            }
        }

        public AccessRightItem(RoleAssignment assignment,  AccessRightVM parent)
        {
            this.Assignment = assignment;
            this.Parent = parent;
        }
    }
}
