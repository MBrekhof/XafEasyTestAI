using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XafEasyTestAI.Module.BusinessObjects
{
    public enum TaskPriority { Low, Normal, High, Critical }

    // Class is ProjectTask to avoid clashing with System.Threading.Tasks.Task; UI caption is "Task".
    [DefaultClassOptions]
    [System.ComponentModel.DisplayName("Task")]
    [DefaultProperty(nameof(Title))]
    public class ProjectTask : BaseObject
    {
        [Required]
        public virtual string Title { get; set; } = string.Empty;
        public virtual string? Description { get; set; }
        public virtual DateTime? DueDate { get; set; }
        public virtual TaskPriority Priority { get; set; }
        public virtual bool IsCompleted { get; set; }

        public virtual Guid? ProjectId { get; set; }
        [ForeignKey(nameof(ProjectId))]
        public virtual Project? Project { get; set; }

        public virtual Guid? AssignedToId { get; set; }
        [ForeignKey(nameof(AssignedToId))]
        public virtual Contact? AssignedTo { get; set; }
    }
}
