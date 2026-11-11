using AngleSharp.Dom.Events;
using Microsoft.VisualBasic;
using System.Net.Http.Json;
using System.Net.Mime;

string? webhookUrl;
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


UpTreeScraper uptree = new UpTreeScraper(); //object for the UpTree scraper
await uptree.main();
List<UpTreeScraper.Events> uptreeEvents = uptree.ListOfEvents;

FuturesForAllScraper futures = new FuturesForAllScraper();
await futures.main();
List<FuturesForAllScraper.Event> futuresEvents = futures.listOfEvents;





// Create the object that sends HTTP requests.
using HttpClient client = new HttpClient();

async Task<bool> Sending(string platform,string name,string date,string link, string? organisation= null,string? type = null, string? sector = null,string?days = null) // the first 4 parameters are common for both but the rest will be null for uptree
{
    //can't send all at once because discord have limit on character per message
    bool sent = false; //used to show if the current event is sucessfully sent or not
    
    string Description;
    if (platform.ToLower() == "uptree")
    {
        Description = $"**Platform** = {platform}\n**Date** = {date}";
    }
    else
    {
        Description = $"**Platform** = {platform}\n\n**Organisation** = {organisation} \n**Date** = {date} \u2003\u2003**Duration** = {days} days\n\n **Type** = {type}\u2003\u2003**Sector** = {sector}";  // u2003 for spacing
    }
    var message = new //the variable sent to discord
    {
        embeds = new[] //holds array of cards
        {
            new
            {
                title = name,   //names like title and url are fixed because discord require json in specific format
                description = Description,
                url = link
            }
        }
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
            
            Console.Write(ex);
            await Task.Delay(10000); //10 seconds delay to avoid rate limit

        }
    
    }
    if (sent == false)
    {
        using (StreamWriter writer = new StreamWriter("failedEvents.txt",true)) //append mode
        {
          
            writer.WriteLine($"{platform}||{name}||{date}||{link}||{organisation}||{type} ||{sector}||{days}"); //|| to seperate events so it can be splited later
    
            return false;
        }
    }
    return true;
}

async Task UpTree()
{
    foreach (var k in uptreeEvents)
    {
        await Sending("UpTree",k.Name,k.Date,k.Link);
    }
    UpTreeSaveCurrentLinks();
}
async Task FuturesForAll()
{
    foreach(var k in futuresEvents)
    {
        await Sending("FuturesForAll",k.EventName,k.StartDate,k.Link,k.OrganisationName,k.Type,k.Sector,k.Days);
    }
    FuturesSaveCurrentLinks(futures.CurrentLinks); 
}


//save the links of the current run
void UpTreeSaveCurrentLinks()
    {
        
        using (StreamWriter writer = new StreamWriter("EventLinks.txt",true))  //set the stream writer to append mode everytime
        {
            foreach (var k in uptree.CurrentLinks)
            {   
            writer.WriteLine(k); //save only the link, so this can be used to filter which ones are new later
            }
        }
        
    }

void FuturesSaveCurrentLinks(List<string> listOfLinks)
    {
        using (StreamWriter writer = new StreamWriter("EventLinks2.txt",false)) //overwrite mode so it removes the exisiting links in the file
        {
            foreach (string link in listOfLinks)
            {
                writer.WriteLine(link);
            }
        }
    }


async Task Recovery()
{
    List<string> stillFails = new List<string>(); 
    try
    {
        
        string? current;
        List<string> failEvents = new List<string>();
        
        using (StreamReader reader = new StreamReader("failedEvents.txt"))
        {
            current = reader.ReadLine();
            while (current!= null)
            {
                failEvents.Add(current);
                current = reader.ReadLine();
            }
        }


            foreach(string k in failEvents)
            {
                    List<string> oneEvent = k.Split("||").ToList();
                    if (oneEvent.Count == 4)
                    {
                       for (int i=0; i<3; i ++) //resend three times for each event
                       {
                         if(await Sending(oneEvent[0],oneEvent[1],oneEvent[2],oneEvent[3]) ) //the position will match the parameters of sending
                            {
                                break;
                            }
                              if (i==2)
                            {
                                stillFails.Add(string.Join("||",oneEvent));
                            }
                       }
                    }
                    else
                    {
                        for (int i=0; i<3; i ++)
                        {
                         if(await Sending(oneEvent[0],oneEvent[1],oneEvent[2],oneEvent[3],oneEvent[4],oneEvent[5],oneEvent[6],oneEvent[7]) )
                            {
            
                                break;
                            }
                         if (i==2)
                    {
                        stillFails.Add(k);
                    }
                        }
                    }
            }
            using (StreamWriter writer = new StreamWriter("failedEvents.txt"))
            {
                foreach (string i in stillFails)
                {
                    writer.WriteLine(i);
                }
            }
                    
                               
        
    }
    
    catch(Exception ex)
    {
        Console.WriteLine("no failed events yet?");
    }


    
}

await UpTree();

await FuturesForAll();

await Recovery();




