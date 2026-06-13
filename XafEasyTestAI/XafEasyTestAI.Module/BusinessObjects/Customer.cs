using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace XafEasyTestAI.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(Name))]
    public class Customer : BaseObject
    {
        [Required]
        public virtual string Name { get; set; } = string.Empty;
        public virtual string? Email { get; set; }
        public virtual string? Phone { get; set; }
        public virtual string? Website { get; set; }
        public virtual string? City { get; set; }
        public virtual string? Country { get; set; }

        [Aggregated]
        public virtual IList<Contact> Contacts { get; set; } = new ObservableCollection<Contact>();
        public virtual IList<Order> Orders { get; set; } = new ObservableCollection<Order>();
        public virtual IList<Project> Projects { get; set; } = new ObservableCollection<Project>();
    }
}
