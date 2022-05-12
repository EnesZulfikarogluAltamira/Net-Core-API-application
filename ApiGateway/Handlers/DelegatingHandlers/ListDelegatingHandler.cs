using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using Serilog.Core;
using System.Text;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using Ocelot.Middleware;
using Consul;

namespace ApiGateway.Handlers.DelegatingHandlers
{
    public class ListDelegatingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Changes to the request should be added here 

            var responses = new List<HttpResponseMessage>();
            var employeeServiceUriString = "";

            var consulClient = new ConsulClient(c => c.Address = new Uri("http://localhost:8500"));
            var services = consulClient.Agent.Services().Result.Response;
            foreach (var service in services)
            {
                var isEmployeeService = service.Value.Tags.Any(t => t == "EmployeeService");

                if (isEmployeeService)
                {
                    employeeServiceUriString = $"http://{service.Value.Address}:{service.Value.Port}{request.RequestUri.PathAndQuery}";
                }
            }

            Uri employeeServiceUri = new Uri(employeeServiceUriString);

            var stringContent = await request.Content.ReadAsStringAsync();

            JObject originalContent = JObject.Parse(stringContent);

            List<string> keys = new List<string>();
            keys.Add("pageSize");
            keys.Add("pageCount");

            var request1 = EditRequest(request, originalContent, employeeServiceUri, keys);
            var request2 = EditRequest(request, originalContent, employeeServiceUri, keys);

            var task1 = Task.Run(() => SendRequest(request1, cancellationToken));
            var task2 = Task.Run(() => SendRequest(request2, cancellationToken));

            var response1 = await task1;
            var response2 = await task2;

            responses.Add(response1);
            responses.Add(response2);

            JObject baseJsonObject = new JObject();
            JObject jsonObject = new JObject();
            int pageSize = 0, pageCount = 0;

            for (int i = 0; i < responses.Count; i++)
            {
                try
                {
                    //if(i == 0)
                    //{
                    //    JObject baseJsonObject = new JObject(await responses[i].Items.DownstreamResponse().Content.ReadAsStringAsync());
                    //}
                    var MergeBuilder = new StringBuilder();
                    var jsonString = await responses[i].Content.ReadAsStringAsync();

                    jsonObject = JObject.Parse(jsonString);

                    baseJsonObject.Merge(jsonObject, new JsonMergeSettings
                    {
                        // union array values together to avoid duplicates
                        MergeArrayHandling = MergeArrayHandling.Union
                    });

                    SumValues(jsonObject, "pageSize", ref pageSize);
                    SumValues(jsonObject, "pageCount", ref pageCount);

                }
                catch (Exception ex)
                {
                    //_logger.Error(ex.ToString() + " - Error on Downstream Response: " + responses[i].Content.ToString());
                }
            }

            baseJsonObject["pageSize"] = pageSize;
            baseJsonObject["pageCount"] = pageCount;

            var responseContent = new StringContent(baseJsonObject.ToString())
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
            };

            var finalResponse = new HttpResponseMessage();

            finalResponse.Content = responseContent;

            return finalResponse;
        }

        private static void SumValues(JObject jsonObject, string key, ref int val)
        {
            if (jsonObject.ContainsKey(key))
            {
                val += (int)jsonObject[key];
            }
        }

        private HttpRequestMessage EditRequest(HttpRequestMessage request, JObject originalContent, Uri uri, List<string> keys)
        {
            var editedContent = new JObject();

            foreach (var key in keys)
            {
                editedContent[key] = (int)originalContent[key];
            }

            request.Method = HttpMethod.Post;
            request.Headers.Add("Accept", "application/json");
            request.Content = new StringContent(editedContent.ToString(), Encoding.UTF8, "application/json");
            request.RequestUri = uri;

            return request;
        }

        private async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await base.SendAsync(request, cancellationToken);
        }
    }
}