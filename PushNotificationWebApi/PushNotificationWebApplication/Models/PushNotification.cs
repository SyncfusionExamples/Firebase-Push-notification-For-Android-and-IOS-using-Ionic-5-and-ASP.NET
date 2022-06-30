using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Web;

namespace PushNotificationWebAPIApplication.Models
{
    public class PushNotification
    {
        [Key]
        public int Id { get; set; }
        public string DeviceId { get; set; }
        public string DeviceType { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class PushNotificationAddResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    public class ResponseData
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }

    public class PushNotificationData
    {
        public int Id { get; set; }
        public string DeviceId { get; set; }
        public string DeviceType { get; set; }
    }

    public class PushNotificationResponseList : ResponseData
    {
        public List<PushNotificationData> PushNotification { get; set; }
    }

    public class Notification
    {
        public string NotificationTitle { get; set; }
        public string NotificationBody { get; set; }
    }
}