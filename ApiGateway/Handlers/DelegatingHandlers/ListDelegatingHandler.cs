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
            var responses = new List<HttpResponseMessage>();
            var employeeServiceUriString = "";


            // Consul Client ile tag'ler kullanılarak service bilgileri alınır.
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


            // originalContent değişkenine içerisine orijinal request body yazılır.
            var stringContent = await request.Content.ReadAsStringAsync();

            JObject originalContent = JObject.Parse(stringContent);


            // Kullanılacak requestler oluşturulup düzenlenir.
            List<string> keys = new List<string>();
            keys.Add("pageSize");
            keys.Add("pageCount");

            var request1 = CreateRequest(originalContent, employeeServiceUri, keys);

            keys = new List<string>();
            keys.Add("pageSize1");
            keys.Add("pageCount1");

            employeeServiceUri = new Uri(employeeServiceUriString.Replace("1", "2")); // test icin yazilmistir. Aynı path oldugu surece yazılması gerekmeyecek.
            var request2 = CreateRequest(originalContent, employeeServiceUri, keys);


            // Paralel işlemler oluşturmak için task'lar oluşturulur ve çağrılır. Response'lar listeye eklenir.
            var task1 = Task.Run(() => SendRequest(request1, cancellationToken));
            var task2 = Task.Run(() => SendRequest(request2, cancellationToken));
            //Task.WaitAll(task1,task2);
            var response1 = await task1;
            var response2 = await task2;


            responses.Add(response1);
            responses.Add(response2);

            // Response'lar birleştirilip döndürülür.
            return await AggregateResponses(responses);
        }

        // Verilen Json nesnelerindeki key girdilerinin value değerlerini toplayan fonksiyondur.
        private static void SumValues(JObject jsonObject, string key, ref int val)
        {
            if (jsonObject.ContainsKey(key))
            {
                val += (int)jsonObject[key];
            }
        }

        // Request'i girilen değerlere göre düzenleyen fonksiyondur.
        private HttpRequestMessage CreateRequest(JObject originalContent, Uri uri, List<string> keys = null)
        {
            var request = new HttpRequestMessage();
            var editedContent = new JObject();

            if (keys != null && keys.Any())
            {
                foreach (var key in keys)
                {
                    editedContent[key] = (int)originalContent[key];
                }
                request.Method = HttpMethod.Post;
                request.Content = new StringContent(editedContent.ToString(), Encoding.UTF8, "application/json");
            }
            else
            {
                request.Method = HttpMethod.Get;
            }

            request.Headers.Add("Accept", "application/json");
            request.RequestUri = uri;

            return request;
        }

        // Request'i gönderip response döndüren fonksiyondur.
        private async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // Verilen response listesindeki response'ları birleştiren fonksiyondur.
        private async Task<HttpResponseMessage> AggregateResponses(List<HttpResponseMessage> responses)
        {
            // Dönen response'ları istenilen şekilde birleştirilir ve tek bir response oluşturulur.
            JObject baseJsonObject = new JObject();
            JObject jsonObject = new JObject();
            int totalRowCount = 0;

            for (int i = 0; i < responses.Count; i++)
            {
                try
                {
                    var jsonString = await responses[i].Content.ReadAsStringAsync();

                    jsonObject = JObject.Parse(jsonString);

                    baseJsonObject.Merge(jsonObject, new JsonMergeSettings
                    {
                        // union array values together to avoid duplicates
                        MergeArrayHandling = MergeArrayHandling.Union
                    });

                    SumValues(jsonObject, "totalRowCount", ref totalRowCount);
                }
                catch (Exception ex)
                {
                    //_logger.Error(ex.ToString() + " - Error on Downstream Response: " + responses[i].Content.ToString());
                }
            }

            baseJsonObject["totalRowCount"] = totalRowCount;
            baseJsonObject["pageCount"] = (int)Math.Ceiling((decimal)((totalRowCount) / (float)baseJsonObject["pageSize"]));

            var responseContent = new StringContent(baseJsonObject.ToString())
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
            };

            var finalResponse = new HttpResponseMessage();

            finalResponse.Content = responseContent;

            return finalResponse;
        }
    }
}