using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PushNotificationWebAPIApplication.Models
{
    public class Http2CustomHandler : WinHttpHandler
    {
        //[SupportedOSPlatform("windows")]
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            request.Version = new Version("2.0");
            return base.SendAsync(request, cancellationToken);
        }
    }
}
