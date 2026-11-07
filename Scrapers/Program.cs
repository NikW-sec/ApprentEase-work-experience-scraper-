using AngleSharp.Dom.Events;
using Microsoft.VisualBasic;
using System.Net.Http.Json;
using System.Net.Mime;
UpTreeScraper uptree = new UpTreeScraper(); //object for the UpTree scraper
await uptree.main();
List<UpTreeScraper.Events> uptreeEvents = uptree.ListOfEvents;

FuturesForAllScraper futures = new FuturesForAllScraper();
await futures.main();
List<FuturesForAllScraper.Event> futuresEvents = futures.listOfEvents;


string webhookUrl;
try
{
     webhookUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL");
     // as I entered the $env:DISCORD_WEBHOOK_URL = "TheURL" in ther terminal eariler, the url is stored as an variable and the envrironment line uses the value
     //this is to improve security, so the webhook url stays local and others can't see it 
     if (string.IsNullOrWhiteSpace(webhookUrl))
    {
        throw new Exception ("no url is provided");
    }
}
        
catch (Exception ex)
{
  throw new Exception("Error with webhook url ");
}



// Create the object that sends HTTP requests.
using HttpClient client = new HttpClient();

async Task Sending(string platform,string name,string date,string link, string? organisation= null,string? type = null, string? sector = null,string?days = null) // the first 4 parameters are common for both but the rest will be null for uptree
{
    //can't send all at once because discord have limit on character per message
    bool sent = false; //used to show if the current event is sucessfully sent or not
    
    var message = new{content = ""};
    if(platform.ToLower() == "uptree")
    {
        message = new
        {
            content = $"{name}\n{date}\n{link}"
            
            
        };
    }
    else
    {
        message = new
        {
            content = $"{name}\n{date}\n{link}\n{organisation}\n{type}\n{sector}\n{days}\n"
            
        };
        

    }
    
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
            
            Console.Write(ex);
            await Task.Delay(10000); //10 seconds delay to avoid rate limit

        }
    
    }
    if (sent == false)
    {
        using (StreamWriter writer = new StreamWriter("failsureEvents.txt",true)) //append mode
        {
            writer.WriteLine(platform);
            writer.WriteLine(message.content); //.content allow to store only the text  , not including the object's wrapper
            writer.WriteLine(DateTime.Now.ToString()); //also save the time of failure
            writer.WriteLine();

        }
    }
}

async Task UpTree()
{
    foreach (var k in uptreeEvents)
    {
        await Sending("UpTree",k.Name,k.Date,k.Link);
    }
}
async Task FuturesForAll()
{
    foreach(var k in futuresEvents)
    {
        await Sending("FuturesForAll",k.EventName,k.OrganisationName,k.Type,k.Sector,k.StartDate,k.Days,k.Link);
    }
}


await UpTree();

