using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XafEasyTestAI.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(FullName))]
    public class Contact : BaseObject
    {
        [Required]
        public virtual string FirstName { get; set; } = string.Empty;
        [Required]
        public virtual string LastName { get; set; } = string.Empty;
        public virtual string? JobTitle { get; set; }
        public virtual string? Email { get; set; }
        public virtual string? Phone { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();

        public virtual Guid? CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }
    }
}
