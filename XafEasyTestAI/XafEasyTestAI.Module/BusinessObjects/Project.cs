using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XafEasyTestAI.Module.BusinessObjects
{
    public enum ProjectStatus { Planned, Active, OnHold, Completed, Cancelled }

    [DefaultClassOptions]
    [DefaultProperty(nameof(Name))]
    public class Project : BaseObject
    {
        [Required]
        public virtual string Name { get; set; } = string.Empty;
        public virtual string? Code { get; set; }
        public virtual DateTime? StartDate { get; set; }
        public virtual DateTime? EndDate { get; set; }
        public virtual ProjectStatus Status { get; set; }

        public virtual Guid? CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }

        [Aggregated]
        public virtual IList<ProjectTask> Tasks { get; set; } = new ObservableCollection<ProjectTask>();

        // Validation rule (fires on Save): a Project can't be Completed while any Task is still open.
        // Returns true when the project is valid. [NotMapped] = not a DB column; [Browsable(false)] = hidden in UI.
        [Browsable(false)]
        [NotMapped]
        [RuleFromBoolProperty("Project_CannotCompleteWithOpenTasks", DefaultContexts.Save,
            "A project cannot be marked Completed while it has open (incomplete) tasks.",
            SkipNullOrEmptyValues = false)]
        public virtual bool CanBeCompleted =>
            Status != ProjectStatus.Completed || Tasks.All(t => t.IsCompleted);
    }
}
