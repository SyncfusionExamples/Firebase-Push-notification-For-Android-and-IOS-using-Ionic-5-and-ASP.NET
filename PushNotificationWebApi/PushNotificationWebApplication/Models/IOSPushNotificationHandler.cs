using Jose;
using Jose.keys;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace PushNotificationWebAPIApplication.Models
{
    public class IOSPushNotificationHandler
    {
        public string Algorithm { get; set; }
        public string HostServerUrl { get; set; }
        public int HostPort { get; set; }
        public string APNsKeyId { get; set; }
        public string TeamId { get; set; }
        public string BundleAppId { get; set; }
        public string AppleAuthKeyFile { get; set; }
        public CngKey PrivateKey { get; set; }
        public string AccessToken { get; set; }

        public IOSPushNotificationHandler(string appleKeyId, string appleTeamId, string appId, string appleAuthKeyFile, bool production)
        {
            Algorithm = "ES256";

            if (production == false)
                HostServerUrl = "api.development.push.apple.com";
            else
                HostServerUrl = "api.push.apple.com";

            HostPort = 443;

            APNsKeyId = appleKeyId;
            TeamId = appleTeamId;
            BundleAppId = appId;
            AppleAuthKeyFile = appleAuthKeyFile;
        }

        private long ToUnixEpochDate(DateTime date)
        {
            return (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);
        }

        private async Task JwtAPNsPush(Uri host_uri, string access_token, byte[] payload_bytes)
        {
            var result = new ResponseData();

            try
            {
                using (var handler = new Http2CustomHandler())
                {
                    using (var http_client = new HttpClient(handler))
                    {
                        var request_message = new HttpRequestMessage();
                        {
                            request_message.RequestUri = host_uri;
                            request_message.Headers.Add("authorization", String.Format("bearer {0}", access_token));
                            request_message.Headers.Add("apns-id", Guid.NewGuid().ToString());
                            request_message.Headers.Add("apns-expiration", "0");
                            request_message.Headers.Add("apns-priority", "10");
                            request_message.Headers.Add("apns-topic", BundleAppId);
                            request_message.Method = HttpMethod.Post;
                            request_message.Content = new ByteArrayContent(payload_bytes);
                        }

                        var response_message = await http_client.SendAsync(request_message);
                        if (response_message.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var response_uuid = "";

                            IEnumerable<string> values;
                            if (response_message.Headers.TryGetValues("apns-id", out values))
                            {
                                response_uuid = values.First();

                                result.Message = $"success: '{response_uuid}'";
                                result.IsSuccess = true;
                            }
                            else
                            {
                                result.Message = "failure";
                            }
                        }
                        else
                        {
                            var response_body = await response_message.Content.ReadAsStringAsync();
                            var response_json = JObject.Parse(response_body);

                            var reason_str = response_json.Value<string>("reason");
                            result.Message = $"failure: '{reason_str}'";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Message = $"exception: '{ex.Message}'";
            }
        }

        private CngKey GetPrivateKey()
        {
            using (var reader = File.OpenText(AppleAuthKeyFile))
            {
                var ecPrivateKeyParameters = (ECPrivateKeyParameters)new PemReader(reader).ReadObject();
                var x = ecPrivateKeyParameters.Parameters.G.AffineXCoord.GetEncoded();
                var y = ecPrivateKeyParameters.Parameters.G.AffineYCoord.GetEncoded();
                var d = ecPrivateKeyParameters.D.ToByteArrayUnsigned();
                return EccKey.New(x, y, d);
            }
        }

        private void JwtAPNsPushExtend(string device_token, string title, string body)
        {
            var host_uri = new Uri($"https://{HostServerUrl}:{HostPort}/3/device/{device_token}");

            var _payload = new Dictionary<string, object>()
                    {
                        { "iss", TeamId },
                        { "iat", ToUnixEpochDate(DateTime.Now) }
                    };

            var header = new Dictionary<string, object>()
                    {
                        { "alg", Algorithm },
                        { "kid", APNsKeyId }
                    };

            var privateKey = GetPrivateKey();

            AccessToken = JWT.Encode(_payload, privateKey, JwsAlgorithm.ES256, header);

            var payload = new byte[0];
            {
                var data = JObject.FromObject(new
                {
                    aps = new
                    {
                        alert = new
                        {
                            title = title,
                            body = body
                        },
                        badge = 1
                    }
                });

                payload = System.Text.Encoding.UTF8.GetBytes(data.ToString());
            }

            Task.Run(async () => await JwtAPNsPush(host_uri, AccessToken, payload));
        }

        public void JwtAPNsPush(string device_token, string title, string body)
        {
            JwtAPNsPushExtend(device_token, title, body);
        }
    }

}
