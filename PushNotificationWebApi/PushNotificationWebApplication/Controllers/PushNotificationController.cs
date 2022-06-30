using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using PushNotificationWebAPIApplication.Context;
using PushNotificationWebAPIApplication.Models;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PushSharp.Core;
using PushSharp.Google;
using System.Configuration;

namespace PushNotificationWebAPIApplication.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/pushNotification")]
    public class PushNotificationController : ApiController
    {
        public string TokenRegisterMessage = "Token was registered successfully";
        public string SendPushNotificationMessage = "Push notification was send successfully";

        private string serverKeyForApp = ConfigurationManager.AppSettings["FCMServerKeyForApp"];
        private string senderIdForApp = ConfigurationManager.AppSettings["FCMSenderIdForApp"];
        private string fcmNotificationSendURL = ConfigurationManager.AppSettings["FCMNotificationSendURL"];

        private string appleKeyId = ConfigurationManager.AppSettings["AppleKeyId"];
        private string appleTeamId = ConfigurationManager.AppSettings["AppleTeamId"];
        private string appId = ConfigurationManager.AppSettings["AppId"];
        private string appleAuthKeyFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["AppleAuthKeyFile"]);

        private PushNotificationDbContext dbContext = new PushNotificationDbContext();
        public PushNotificationController() 
        {
        }

        /// <summary>
        /// Add Push Notification
        /// </summary>
        /// <param name="pushNotification"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("addPushNotification")]
        public HttpResponseMessage AddPushNotification(PushNotification pushNotification)
        {
            var pushNotificationAddResponse = new PushNotificationAddResponse();
            try
            {
                var PushNotification = new PushNotification
                {
                    DeviceId = pushNotification.DeviceId,
                    DeviceType = pushNotification.DeviceType,
                    CreatedDate = DateTime.Now,
                    IsDeleted = false
                };

                var pushnotification = (from push in dbContext.PushNotifications
                                                    where push.DeviceId == pushNotification.DeviceId &&
                                                    push.IsDeleted == false
                                            select push).FirstOrDefault();

                if (pushnotification != null)
                {
                    pushnotification.IsDeleted = true;
                    dbContext.SaveChanges();
                }

                dbContext.PushNotifications.Add(PushNotification);
                dbContext.SaveChanges();

                pushNotificationAddResponse.IsSuccess = true;
                pushNotificationAddResponse.Message = TokenRegisterMessage;

                return Request.CreateResponse(HttpStatusCode.OK, pushNotificationAddResponse);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, ex.Message);
            }
        }

        /// <summary>
        /// Send Push Notification
        /// </summary>
        /// <param name="pushMessage"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("sendPushNotification")]
        public HttpResponseMessage SendPushNotification(Notification notificationList)
        {
            var pushNotificationAddResponse = new PushNotificationAddResponse();
            try
            {
                var responseStatus = new ResponseData();

                var androidDeviceIdList = new List<string>();
                var iosDeviceIdList = new List<string>();

                var pushNotificationList = GetPushNotification();

                if (pushNotificationList.Count > 0)
                {
                    foreach (var notification in pushNotificationList)
                    {
                        if (notification.DeviceType == "1")
                        {
                            androidDeviceIdList.Add(notification.DeviceId);
                        }
                        else if (notification.DeviceType == "2")
                        {
                            iosDeviceIdList.Add(notification.DeviceId);
                        }
                    }

                    if (androidDeviceIdList.Count > 0)
                    {
                        SendToAndroid(androidDeviceIdList, notificationList.NotificationBody, notificationList.NotificationTitle, serverKeyForApp, senderIdForApp);
                    }

                    if (iosDeviceIdList.Count > 0)
                    {
                        var apns = new IOSPushNotificationHandler(appleKeyId, appleTeamId, appId, appleAuthKeyFile, true);

                        foreach (var token in iosDeviceIdList)
                        {
                            apns.JwtAPNsPush(token, notificationList.NotificationTitle, notificationList.NotificationBody);
                        }
                    }
                }

                pushNotificationAddResponse.IsSuccess = true;
                pushNotificationAddResponse.Message = SendPushNotificationMessage;

                return Request.CreateResponse(HttpStatusCode.OK, pushNotificationAddResponse);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, ex.Message);
            }
        }

        private List<PushNotificationData> GetPushNotification()
        {
            var responseStatus = new PushNotificationResponseList();
            var pushNotificationList = new List<PushNotificationData>();
            var pushnotification = (from push in dbContext.PushNotifications
                                    where push.IsDeleted ==false
                                    select push).ToList();

            foreach (var notify in pushnotification)
            {
                var pushNotificationData = new PushNotificationData();
                pushNotificationData.Id = notify.Id;
                pushNotificationData.DeviceId = notify.DeviceId;
                pushNotificationData.DeviceType = notify.DeviceType;

                pushNotificationList.Add(pushNotificationData);
            }
            return pushNotificationList;
        }

        public void SendToAndroid(List<string> deviceIdList, string NotificationBody, string NotificationTitle, string ServerKeyForApp, string SenderIdForApp)
        {
            var config = new GcmConfiguration(SenderIdForApp, ServerKeyForApp, null);
            config.GcmUrl = fcmNotificationSendURL;
            var broker = new GcmServiceBroker(config);
            broker.OnNotificationFailed += (notification, aggregateEx) => GcmNotificationFailed(aggregateEx);
            broker.OnNotificationSucceeded += (notification) => GcmNotificationSucceeded();
            broker.Start();

            broker.QueueNotification(new GcmNotification
            {
                RegistrationIds = deviceIdList,
                Notification = JObject.FromObject(new
                {
                    body = NotificationBody,
                    title = NotificationTitle,
                    badge = 1
                }),
                Priority = GcmNotificationPriority.High,
                ContentAvailable = true
            });

            broker.Stop();
        }

        private void GcmNotificationSucceeded()
        {
            GetDevicePushStatus("Notification has been sent");
        }

        private void GcmNotificationFailed(AggregateException aggregateEx)
        {
            string errorMessage = "";
            aggregateEx.Handle(ex =>
            {
                if (ex is GcmNotificationException)
                {
                    var notificationException = (GcmNotificationException)ex;
                    var gcmNotification = notificationException.Notification;
                    var description = notificationException.Description;
                    if (description != null)
                        errorMessage = "Notification Failed: Desc={" + description + "}";
                    else
                        errorMessage = "Notification Failed: Desc={" + notificationException.Message + "}";
                }
                else if (ex is GcmMulticastResultException)
                {
                    var multicastException = (GcmMulticastResultException)ex;

                    foreach (var succeededNotification in multicastException.Succeeded)
                    {
                    }

                    foreach (var failedKvp in multicastException.Failed)
                    {
                        var n = failedKvp.Key;
                        var e = failedKvp.Value;
                        errorMessage += "Notification Failed: Desc={" + e.Message + "}";
                    }

                }
                else if (ex is DeviceSubscriptionExpiredException)
                {
                    var expiredException = (DeviceSubscriptionExpiredException)ex;

                    var oldId = expiredException.OldSubscriptionId;
                    var newId = expiredException.NewSubscriptionId;

                    errorMessage = "Device RegistrationId Expired: {" + oldId + "}";

                    if (!string.IsNullOrWhiteSpace(newId))
                    {
                        errorMessage = "Device RegistrationId Changed To: {" + newId + "}";
                    }
                }
                else if (ex is RetryAfterException)
                {
                    var retryException = (RetryAfterException)ex;
                    errorMessage = "FCM Rate Limited, don't send more until after {" + retryException.RetryAfterUtc + "}";
                }
                else if (ex is UnauthorizedAccessException)
                {
                    var unauthorizedexception = (UnauthorizedAccessException)ex;
                    errorMessage = "FCM Authorization Failed.";
                }
                else
                {
                    errorMessage = "Notification Failed for some unknown reason";
                }

                return true;
            });
            GetDevicePushStatus(errorMessage);
        }

        private string GetDevicePushStatus(string statusText)
        {
            return statusText;
        }   
    }
}