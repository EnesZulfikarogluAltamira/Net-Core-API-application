using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Ocelot.Middleware;
using Ocelot.Multiplexer;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ApiGateway.Handlers.Aggregators
{
    public class ListAggregator : IDefinedAggregator
    {
        private readonly Logger _logger;

        public ListAggregator(Logger logger)
        {
            _logger = logger;
        }
        public async Task<DownstreamResponse> Aggregate(List<HttpContext> responses)
        {
            var contentBuilder = new StringBuilder();
            JObject baseJsonObject = new JObject();
            JObject jsonObject = new JObject();

            for (int i = 0; i < responses.Count; i++)
            {
                try
                {
                    //if(i == 0)
                    //{
                    //    JObject baseJsonObject = new JObject(await responses[i].Items.DownstreamResponse().Content.ReadAsStringAsync());
                    //}
                    var MergeBuilder = new StringBuilder();
                    var jsonString = await responses[i].Items.DownstreamResponse().Content.ReadAsStringAsync();

                    jsonObject = JObject.Parse(jsonString);

                    baseJsonObject.Merge(jsonObject, new JsonMergeSettings
                    {
                        // union array values together to avoid duplicates
                        MergeArrayHandling = MergeArrayHandling.Union
                    });

                }
                catch (Exception ex)
                {
                    _logger.Error(ex.ToString() + " - Error on Downstream Response: " + responses[i].Items.DownstreamRequest().ToString());
                }
            }


            var stringContent = new StringContent(baseJsonObject.ToString())
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
            };

            return new DownstreamResponse(stringContent, HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "OK");
        }

        private string EditContent(string content)
        {
            while (content.EndsWith(","))
            {
                content = content.Remove(content.Length - 1);
            }
            return content;
        }

        private JObject filterKeys(JObject obj)
        {
            var result = new JObject();

            foreach (var key in obj)
            {
                //if (obj.ContainsKey(key.ToString()))
                //{
                //    obj[key].Aggregate;
                //}
                //  result[key] = (key in result? result[key].concat(obj[key]) : obj[key]);
            }
            return result;
        }
    }
}
