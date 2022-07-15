using PushNotificationWebAPIApplication.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace PushNotificationWebAPIApplication.Context
{
    public class PushNotificationDbContext : DbContext
    {
        public PushNotificationDbContext() : base("name=PushNotificationContext")
        {

        }
        public DbSet<PushNotification> PushNotifications { get; set; }
    }
}