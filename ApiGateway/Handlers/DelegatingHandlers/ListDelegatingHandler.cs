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

namespace ApiGateway.Handlers.DelegatingHandlers
{
    public class ListDelegatingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Changes to the request should be added here 

            var responses = new List<HttpResponseMessage>();

            var stringContent = request.Content.ReadAsStringAsync().Result;

            JObject originalContent = JObject.Parse(stringContent);

            JObject editedContent = new JObject();

            editedContent["pageSize"] = (int)originalContent["pageSize"];
            editedContent["pageCount"] = (int)originalContent["pageCount"];

            HttpRequestMessage request1 = request;
            request1.Method = HttpMethod.Post;
            request1.Headers.Add("Accept", "application/json");
            request1.Content = new StringContent(editedContent.ToString(), Encoding.UTF8, "application/json");
            request1.RequestUri = new Uri("http://localhost:9000/api/persons/aggregate1");

            editedContent = new JObject();

            editedContent["pageSize1"] = (int)originalContent["pageSize1"];
            editedContent["pageCount1"] = (int)originalContent["pageCount1"];

            var response1 = await base.SendAsync(request1, cancellationToken);
            responses.Add(response1);


            HttpRequestMessage request2 = request;
            request2.Method = HttpMethod.Post;
            request2.Headers.Add("Accept", "application/json");
            request2.Content = new StringContent(editedContent.ToString(), Encoding.UTF8, "application/json");
            request2.RequestUri = new Uri("http://localhost:9000/api/persons/aggregate2");



            //request.Content = new StringContent(editedContent.ToString(), Encoding.UTF8, "application/json");

            //request.Content = new ByteArrayContent(content);
            var response2 = await base.SendAsync(request1, cancellationToken);
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
    }
}