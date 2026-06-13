using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace XafEasyTestAI.Module.BusinessObjects
{
    public enum OrderStatus { Draft, Confirmed, Shipped, Delivered, Cancelled }

    [DefaultClassOptions]
    [DefaultProperty(nameof(OrderNumber))]
    public class Order : BaseObject
    {
        public virtual string OrderNumber { get; set; } = string.Empty;
        public virtual DateTime OrderDate { get; set; }
        public virtual OrderStatus Status { get; set; }

        public virtual Guid? CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }

        [Aggregated]
        public virtual IList<OrderLine> OrderLines { get; set; } = new ObservableCollection<OrderLine>();

        [NotMapped]
        public decimal Total => OrderLines?.Sum(l => l.LineTotal) ?? 0m;
    }
}
