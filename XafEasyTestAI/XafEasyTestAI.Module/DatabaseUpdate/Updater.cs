using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EF;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Microsoft.Extensions.DependencyInjection;
using XafEasyTestAI.Module.BusinessObjects;

namespace XafEasyTestAI.Module.DatabaseUpdate
{
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Updating.ModuleUpdater
    public class Updater : ModuleUpdater
    {
        public Updater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion)
        {
        }
        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();
            //string name = "MyName";
            //EntityObject1 theObject = ObjectSpace.FirstOrDefault<EntityObject1>(u => u.Name == name);
            //if(theObject == null) {
            //    theObject = ObjectSpace.CreateObject<EntityObject1>();
            //    theObject.Name = name;
            //}

            // The code below creates users and roles for testing purposes only.
            // In production code, you can create users and assign roles to them automatically, as described in the following help topic:
            // https://docs.devexpress.com/eXpressAppFramework/119064/data-security-and-safety/security-system/authentication
#if !RELEASE
            // If a role doesn't exist in the database, create this role
            var defaultRole = CreateDefaultRole();
            var adminRole = CreateAdminRole();

            ObjectSpace.CommitChanges(); //This line persists created object(s).

            UserManager userManager = ObjectSpace.ServiceProvider.GetRequiredService<UserManager>();

            // If a user named 'User' doesn't exist in the database, create this user
            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "User") == null)
            {
                // Set a password if the standard authentication type is used
                string EmptyPassword = "";
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "User", EmptyPassword, (user) =>
                {
                    // Add the Users role to the user
                    user.Roles.Add(defaultRole);
                });
            }

            // If a user named 'Admin' doesn't exist in the database, create this user
            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "Admin") == null)
            {
                // Set a password if the standard authentication type is used
                string EmptyPassword = "";
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "Admin", EmptyPassword, (user) =>
                {
                    // Add the Administrators role to the user
                    user.Roles.Add(adminRole);
                });
            }

            ObjectSpace.CommitChanges(); //This line persists created object(s).

            SeedSampleData();
            ObjectSpace.CommitChanges();
#endif
        }

        void SeedSampleData()
        {
            // Idempotent: skip if any sample data already exists.
            if (ObjectSpace.GetObjectsCount(typeof(Customer), null) > 0)
                return;

            var contoso = ObjectSpace.CreateObject<Customer>();
            contoso.Name = "Contoso Ltd";
            contoso.Email = "sales@contoso.com";
            contoso.Phone = "+1 555 0100";
            contoso.Website = "https://contoso.com";
            contoso.City = "Seattle";
            contoso.Country = "USA";

            var fabrikam = ObjectSpace.CreateObject<Customer>();
            fabrikam.Name = "Fabrikam Inc";
            fabrikam.Email = "info@fabrikam.com";
            fabrikam.Phone = "+44 20 7946 0000";
            fabrikam.Website = "https://fabrikam.com";
            fabrikam.City = "London";
            fabrikam.Country = "UK";

            var alice = ObjectSpace.CreateObject<Contact>();
            alice.FirstName = "Alice"; alice.LastName = "Johnson";
            alice.JobTitle = "Procurement Manager";
            alice.Email = "alice.johnson@contoso.com"; alice.Phone = "+1 555 0101";
            alice.Customer = contoso;

            var bob = ObjectSpace.CreateObject<Contact>();
            bob.FirstName = "Bob"; bob.LastName = "Smith";
            bob.JobTitle = "CTO";
            bob.Email = "bob.smith@fabrikam.com"; bob.Phone = "+44 20 7946 0001";
            bob.Customer = fabrikam;

            var order = ObjectSpace.CreateObject<Order>();
            order.OrderNumber = "SO-1001";
            order.OrderDate = new DateTime(2026, 1, 15);
            order.Status = OrderStatus.Confirmed;
            order.Customer = contoso;
            AddLine(order, "Widget Pro", 10, 24.99m);
            AddLine(order, "Gadget Mini", 5, 12.50m);

            var order2 = ObjectSpace.CreateObject<Order>();
            order2.OrderNumber = "SO-1002";
            order2.OrderDate = new DateTime(2026, 2, 3);
            order2.Status = OrderStatus.Shipped;
            order2.Customer = fabrikam;
            AddLine(order2, "Enterprise License", 1, 4999.00m);

            var project = ObjectSpace.CreateObject<Project>();
            project.Name = "Website Redesign";
            project.Code = "PRJ-001";
            project.StartDate = new DateTime(2026, 1, 5);
            project.EndDate = new DateTime(2026, 4, 30);
            project.Status = ProjectStatus.Active;
            project.Customer = contoso;
            AddTask(project, "Gather requirements", new DateTime(2026, 1, 12), TaskPriority.High, true, alice);
            AddTask(project, "Design mockups", new DateTime(2026, 2, 1), TaskPriority.Normal, false, alice);
            AddTask(project, "Implement frontend", new DateTime(2026, 3, 15), TaskPriority.Normal, false, null);

            var project2 = ObjectSpace.CreateObject<Project>();
            project2.Name = "Data Migration";
            project2.Code = "PRJ-002";
            project2.StartDate = new DateTime(2026, 2, 10);
            project2.Status = ProjectStatus.Planned;
            project2.Customer = fabrikam;
            AddTask(project2, "Audit legacy schema", new DateTime(2026, 2, 20), TaskPriority.Critical, false, bob);
        }

        void AddLine(Order order, string product, int qty, decimal price)
        {
            var line = ObjectSpace.CreateObject<OrderLine>();
            line.ProductName = product;
            line.Quantity = qty;
            line.UnitPrice = price;
            line.Order = order;
        }

        void AddTask(Project project, string title, DateTime due, TaskPriority priority, bool completed, Contact assignedTo)
        {
            var task = ObjectSpace.CreateObject<ProjectTask>();
            task.Title = title;
            task.DueDate = due;
            task.Priority = priority;
            task.IsCompleted = completed;
            task.AssignedTo = assignedTo;
            task.Project = project;
        }
        public override void UpdateDatabaseBeforeUpdateSchema()
        {
            base.UpdateDatabaseBeforeUpdateSchema();
        }
        PermissionPolicyRole CreateAdminRole()
        {
            PermissionPolicyRole adminRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "Administrators");
            if (adminRole == null)
            {
                adminRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                adminRole.Name = "Administrators";
                adminRole.IsAdministrative = true;
            }
            return adminRole;
        }
        PermissionPolicyRole CreateDefaultRole()
        {
            PermissionPolicyRole defaultRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(role => role.Name == "Default");
            if (defaultRole == null)
            {
                defaultRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                defaultRole.Name = "Default";

                defaultRole.AddObjectPermissionFromLambda<ApplicationUser>(SecurityOperations.Read, cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
                defaultRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/MyDetails", SecurityPermissionState.Allow);
                defaultRole.AddMemberPermissionFromLambda<ApplicationUser>(SecurityOperations.Write, "ChangePasswordOnFirstLogon", cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
                defaultRole.AddMemberPermissionFromLambda<ApplicationUser>(SecurityOperations.Write, "StoredPassword", cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<PermissionPolicyRole>(SecurityOperations.Read, SecurityPermissionState.Deny);
                defaultRole.AddObjectPermission<ModelDifference>(SecurityOperations.ReadWriteAccess, "UserId = ToStr(CurrentUserId())", SecurityPermissionState.Allow);
                defaultRole.AddObjectPermission<ModelDifferenceAspect>(SecurityOperations.ReadWriteAccess, "Owner.UserId = ToStr(CurrentUserId())", SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifference>(SecurityOperations.Create, SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifferenceAspect>(SecurityOperations.Create, SecurityPermissionState.Allow);
            }
            return defaultRole;
        }
    }
}
