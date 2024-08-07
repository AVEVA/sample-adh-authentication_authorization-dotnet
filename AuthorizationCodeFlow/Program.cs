﻿using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace AuthorizationCodeFlow
{
    public static class Program
    {
        private static IConfiguration _configuration;

        public static void Main()
        {
            bool test = false;
            try
            {
                InitConfig();

                if (SystemBrowser.OpenBrowser == null)
                {
                    SystemBrowser.OpenBrowser = new OpenSystemBrowser();
                }
                else
                {
                    test = true;
                    SystemBrowser.Password = GetConfigValue("Password");
                    SystemBrowser.UserName = GetConfigValue("UserName");
                }

                AuthorizationCode.CdsAddress = GetConfigValue("Resource");
                AuthorizationCode.RedirectHost = GetConfigValue("RedirectHost");
                AuthorizationCode.RedirectPort = int.Parse(GetConfigValue("RedirectPort"), CultureInfo.InvariantCulture);
                AuthorizationCode.RedirectPath = GetConfigValue("RedirectPath");

                string tenantId = GetConfigValue("TenantId");
                string clientId = GetConfigValue("ClientId");
                string cdsUrl = GetConfigValue("Resource");
                string apiVersion = GetConfigValue("ApiVersion");

                // Get access token.
                (string accessToken, DateTimeOffset expiration) =
                    AuthorizationCode.GetAuthorizationCodeFlowAccessToken(clientId, tenantId);
                Console.WriteLine("Access Token: " + accessToken);
                Console.WriteLine("Expires: " + expiration);

                // Make a request to Get Namespaces endpoint
                bool result1 = GetRequest($"{cdsUrl}/api/{apiVersion}/Tenants/{tenantId}/Namespaces", accessToken).Result;
                Console.WriteLine(result1
                    ? "Request succeeded"
                    : "request failed");
                if (!result1)
                    throw new Exception("Request failed");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                if (test)
                    throw;
            }

            if (!test)
                Console.ReadLine();
        }

        private static async Task<bool> GetRequest(string endpoint, string accessToken)
        {
            Console.WriteLine("Make request:");
            using HttpRequestMessage request = new ()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(endpoint),
            };

            // Attach  the access token to the Authorization header in the HTTP request.
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                using HttpClient client = new ();
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                // Uncomment this line to get the results of the calls
                // var responseBodyJson = JsonConvert.SerializeObject(response.Content.ReadAsStringAsync().Result, Formatting.Indented);
                // Console.WriteLine(responseBodyJson);
                return true;
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }

        private static void InitConfig()
        {
            try
            {
                _configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                    .Build();
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine("Config file missing: " + ex);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while initiating configuration: " + ex.ToString());
                throw;
            }
        }

        private static string GetConfigValue(string key)
        {
            try
            {
                if (_configuration == null)
                {
                    Console.WriteLine("Config Null");
                    InitConfig();
                }

                string value = _configuration.GetValue<string>(key);

                if (value == null)
                {
                    Console.WriteLine($"Missing the value for \"{key}\" in config file");
                    throw new Exception($"Missing the value for \"{key}\" in config file");
                }

                return value;
            }
            catch (Exception)
            {
                Console.WriteLine($"Configuration issue");
                throw;
            }
        }
    }
}
