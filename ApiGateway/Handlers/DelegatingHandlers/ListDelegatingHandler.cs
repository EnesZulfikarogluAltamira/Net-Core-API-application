//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Net.Http;
//using System.Threading;
//using Serilog.Core;
//using System.Text;
//using System.Net;
//using Newtonsoft.Json.Linq;

//namespace ApiGateway.Handlers.DelegatingHandlers
//{
//    public class ListDelegatingHandler : DelegatingHandler
//    {
//        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
//        {
//            // Changes to the request should be added here 

//            var stringContent = request.Content.ReadAsStringAsync().Result;

//            JObject originalContent = JObject.Parse(stringContent);

//            JObject editedContent = new JObject();

//            editedContent["pageSize"] = (int)originalContent["pageSize"];
//            editedContent["pageCount"] = (int)originalContent["pageCount"];

//            var content = new StringContent(
//            //request.Content = new ByteArrayContent(new StringContent(editedContent.ToString(), Encoding.UTF8, "application/json")));

//            var response = await base.SendAsync(request, cancellationToken);

//            // Changes to the Response should be added here

           

//            return response;
//        }
//    }
//}
