using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using viewer.Hubs;
using viewer.Models;

namespace viewer.Controllers
{
    [Route("api/[controller]")]
    public class UpdatesController : Controller
    {        
        private bool EventTypeSubcriptionValidation
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
               "SubscriptionValidation";

        private bool EventTypeNotification
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
               "Notification";

        private IHubContext<GridEventsHub> HubContext;

        public UpdatesController(IHubContext<GridEventsHub> gridEventsHubContext)
        {
            this.HubContext = gridEventsHubContext;
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var jsonContent = await reader.ReadToEndAsync();

                // Check the event type.
                // Return the validation code if it's 
                // a subscription validation request. 
                if (EventTypeSubcriptionValidation)
                {
                    var gridEvent =
                        JsonConvert.DeserializeObject<List<GridEvent<Dictionary<string, string>>>>(jsonContent)
                            .First();

                    // Retrieve the validation code and echo back.
                    var validationCode = gridEvent.Data["validationCode"];
                    var validationResponse =
                        JsonConvert.SerializeObject(new
                        {
                            validationResponse =
                            validationCode
                        });
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(validationResponse)
                    };
                }
                else if (EventTypeNotification)
                {
                    var events = JArray.Parse(jsonContent);
                    foreach (var e in events)
                    {
                        // Invoke a method on the clients for 
                        // an event grid notiification.
                        await this.HubContext.Clients.All.SendAsync(
                            "gridupdate", 
                            DateTime.Now.ToLongTimeString(), 
                            e.ToString());
                    }

                    return new HttpResponseMessage { StatusCode = HttpStatusCode.OK };
                }
                else
                {
            
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest };
                }
            }

        }

    }
}
