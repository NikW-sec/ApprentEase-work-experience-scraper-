using AngleSharp.Dom.Events;
using Microsoft.VisualBasic;
using System.Net.Http.Json;
UpTreeScraper uptree = new UpTreeScraper(); //object for the UpTree scraper
await uptree.main();
List<UpTreeScraper.Events> uptreeEvents = uptree.ListOfEvents;

FuturesForAllScraper futures = new FuturesForAllScraper();
//await futures.main();
//List<FuturesForAllScraper.Event> futuresEvents = futures.listOfEvents;


string webhookUrl;
try
{
     webhookUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL");
     if (string.IsNullOrWhiteSpace(webhookUrl))
    {
        throw new Exception ("no url is provided");
    }
}
        // as I entered the $env:DISCORD_WEBHOOK_URL = "TheURL" in ther terminal eariler, the url is stored as an variable and the envrironment line uses the value
        //this is to improve security, so the webhook url stays local and others can't see it 
catch (Exception ex)
{
  throw new Exception("Error with webhook url ");
}

// Create the object that sends HTTP requests.
using HttpClient client = new HttpClient();
List<string>? uptreeContent = new List<string>();
foreach (var k in uptreeEvents)
{
    uptreeContent.Add(k.name.ToString());
    uptreeContent.Add(k.date.ToString());
    uptreeContent.Add(k.link.ToString());
}
List<string> tempoEvents = new List<string>();
int counter = 0; //because each event has three component, when the counter hits 3 we push the message to discord
//can't send all at once because discord have limit on character per message
bool sent = false; //used to show if the current event is sucessfully sent or not
foreach (string k in uptreeContent)
{
    tempoEvents.Add(k);
    counter ++;
    if (counter%3==0)
    {
        sent = false;
        var message = new
        {
            content = string.Join("\n",tempoEvents) + "\n" // \n for spacing
            
        };
        for (int i=0;i<3;i++)  //max three tries per request
        {
            try
            {
                //message is like an object and content is a variable inside the object
                //the reason why this is used over pure string is because discord requires json format, when i try to send a string alone, it returns 400 bad request
                using HttpResponseMessage response = await client.PostAsJsonAsync(webhookUrl + "?wait=true", message); //response store discord's http response
                //client send the message to the webhookurl 
                //using ,automatically removes the resources in response when the current scope ends, just like the using statment in streamwriter 
                response.EnsureSuccessStatusCode(); // continues if the HTTP status code indicates success (200–299), otherwise throws an exception.
            
                Console.WriteLine("sent");
                sent = true;
                await Task.Delay(2000);
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRor because {message}");
                Console.Write(ex);
                await Task.Delay(10000); //10 seconds delay to avoid rate limit

            }
         
        }
        if (sent == false)
        {
            using (StreamWriter writer = new StreamWriter("failsureEvents.txt"))
            {
                Console.WriteLine(message);
            }
        }
        tempoEvents.Clear();


    }
}
