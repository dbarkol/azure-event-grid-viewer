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
        public async Task<ContentResult> Post()
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

                    await this.HubContext.Clients.All.SendAsync(
                        "gridupdate", 
                        gridEvent.Id,
                        gridEvent.EventType,
                        gridEvent.Subject,
                        gridEvent.EventTime.ToLongTimeString(),
                        jsonContent.ToString());

                    // Retrieve the validation code and echo back.
                    var validationCode = gridEvent.Data["validationCode"];
                    var validationResponse =
                        JsonConvert.SerializeObject(new
                        {
                            validationResponse =
                            validationCode
                        });

                    return Content(validationResponse);                 
                }
                else if (EventTypeNotification)
                {
                    var events = JArray.Parse(jsonContent);
                    foreach (var e in events)
                    {
                        // Invoke a method on the clients for 
                        // an event grid notiification.                        
                        var details = JsonConvert.DeserializeObject<GridEvent<dynamic>>(e.ToString());                        
                        await this.HubContext.Clients.All.SendAsync(
                            "gridupdate", 
                            details.Id,
                            details.EventType,
                            details.Subject,
                            details.EventTime.ToLongTimeString(),
                            e.ToString());
                    }

                    return Content("");                  
                }
                else
                {
                    return new ContentResult{
                        StatusCode = 400,
                        Content = "Bad request",
                        ContentType = "text/plain"
                    };                    
                }
            }

        }

    }
}
