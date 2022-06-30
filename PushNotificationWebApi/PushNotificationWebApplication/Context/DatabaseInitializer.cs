using System.Data.Entity;

namespace PushNotificationWebAPIApplication.Context
{
    public class DatabaseInitializer : DropCreateDatabaseIfModelChanges<PushNotificationDbContext>
    {
        protected override void Seed(PushNotificationDbContext context)
        {
            base.Seed(context);
        }
    }
}