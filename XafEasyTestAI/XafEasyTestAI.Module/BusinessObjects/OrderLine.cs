using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace XafEasyTestAI.Module.BusinessObjects
{
    [DefaultProperty(nameof(ProductName))]
    // Makes the nested OrderLines grid editable with a New Item Row so lines can be added inline
    // (in the UI and in EasyTest via InlineNew/FillRow/InlineUpdate). Default list views are read-only.
    [DefaultListViewOptions(true, NewItemRowPosition.Top)]
    public class OrderLine : BaseObject
    {
        public virtual string ProductName { get; set; } = string.Empty;
        public virtual int Quantity { get; set; }
        public virtual decimal UnitPrice { get; set; }

        [NotMapped]
        public decimal LineTotal => Quantity * UnitPrice;

        public virtual Guid? OrderId { get; set; }
        [ForeignKey(nameof(OrderId))]
        public virtual Order? Order { get; set; }
    }
}
