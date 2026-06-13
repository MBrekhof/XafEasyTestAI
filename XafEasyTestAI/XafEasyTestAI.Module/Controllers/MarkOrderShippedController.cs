using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using XafEasyTestAI.Module.BusinessObjects;

namespace XafEasyTestAI.Module.Controllers
{
    // Shared (platform-agnostic) controller: the "Mark Shipped" action shows up in both Win and Blazor.
    // Demonstrates the action + guard pattern that EasyTest is good at exercising.
    public class MarkOrderShippedController : ViewController<ListView>
    {
        readonly SimpleAction markShipped;

        public MarkOrderShippedController()
        {
            TargetObjectType = typeof(Order);
            markShipped = new SimpleAction(this, "MarkShipped", PredefinedCategory.RecordEdit)
            {
                Caption = "Mark Shipped",
                SelectionDependencyType = SelectionDependencyType.RequireSingleObject
            };
            markShipped.Execute += MarkShipped_Execute;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            View.SelectionChanged += View_SelectionChanged;
            UpdateState();
        }

        protected override void OnDeactivated()
        {
            View.SelectionChanged -= View_SelectionChanged;
            base.OnDeactivated();
        }

        void View_SelectionChanged(object sender, EventArgs e) => UpdateState();

        // Guard: only a single Confirmed order can be shipped.
        void UpdateState() =>
            markShipped.Enabled["ConfirmedOnly"] = View.CurrentObject is Order o && o.Status == OrderStatus.Confirmed;

        void MarkShipped_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            foreach (var obj in e.SelectedObjects)
            {
                if (obj is Order order && order.Status == OrderStatus.Confirmed)
                    order.Status = OrderStatus.Shipped;
            }
            ObjectSpace.CommitChanges();
        }
    }
}
