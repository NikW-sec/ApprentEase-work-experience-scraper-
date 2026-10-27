using System.Net.Http; //using the httpClient class
using System.Net;  //used when catching http exception
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.ComponentModel;
using AngleSharp.Dom.Events;
using System.Diagnostics.CodeAnalysis;

HttpClient client = new HttpClient();   //object used for the request later
int i = 1; //keep track of page number

int MaxRequest = 0;  //this is to stop infinite loop when http request fail but is not 404

List<Events> ListOfEvents = new List<Events>(); //this stores only the new events
List<int> FailedPages = new List<int>(); //store the page number of failed pages



List<string> CurrentLinks = new List<string>();  //store links of this run



List<string> PreviousLinks = new List<string>();   //this will store all the links that is in the previous runs
void GetLastRun()
{
    try    
    {
        using (StreamReader reader = new StreamReader("EventLinks.txt"))
        {
            string PastLink = reader.ReadLine();
            while (PastLink!= null)
            {
                PreviousLinks.Add(PastLink);
                PastLink = reader.ReadLine();
            }
        

        }
    }
    catch (FileNotFoundException)  // catch error if the file does not exist, will happen in the first run
    {
        return; 
    }
}

async Task scraping()   //async to allow pause in this method
{
    while (true)  //will only stop by break once all pages are scraped
    {
        if (FailedPages.Count ==3)
        {
            throw new Exception ("This run has failed");
        }
        
        string url;
        if (i ==1)
        {
            url = $"https://uptree.co/events";
        }
        else
        {
            url= $"https://uptree.co/events/?page={i}";
        }
        //the part above is to srape across all avilable pages

        string html;
        try
        {
            html = await client.GetStringAsync(url);   //await to wait for the operation before assigning value, getstringasync is to request for the html of website
        }
        catch (HttpRequestException ex)   //the http exception is sotred as ex
        {
            if (ex.StatusCode == HttpStatusCode.NotFound) //status code is http code like 404, 100. This checks if the code is 404 which means the next page does not exist
            {
                break;  //break to stop the while loop when it already scraped all pages
            }
            else
            {
                if (MaxRequest != 3)
                {
                    MaxRequest++;
                    await Task.Delay(3000);  //pause the program for 3 seconds (3000 milli) to increase chance of recovery
                    continue; //run the loop back to request again as there are still pages left
                }
                else
                {
                    FailedPages.Add(i);
                    MaxRequest = 0; //reset max
                    i++;  //move to next page
                    continue;

                }
                
            }

            
        }
        catch (TaskCanceledException)   // timeout error
        {
             if (MaxRequest != 3)
                {
                    MaxRequest++;
                    await Task.Delay(3000);  //pause the program for 3 seconds (3000 milli) to increase chance of recovery
                    continue; //run the loop back to request again as there are still pages left
                }
                else
                {
                    FailedPages.Add(i);
                    MaxRequest = 0; //reset max
                    i++;  //move to next page
                    continue;

                }
        }
    
        HtmlParser parser = new HtmlParser(); //creating parser object to help turn html text into structured elements


        var document = parser.ParseDocument(html);  //prase the html text
        var programs = document.QuerySelectorAll(".listings-card");   // collect all class="listings-card" which represent one single program

        
        foreach (var OneProgram in programs) 
        {
            try // in case there is a null error
            {
                
                var NameInfo  = OneProgram.QuerySelector(".listings-card__title"); //collect all the info between the <h3> and </h3> which includes all name infos
                var DateInfo  = OneProgram.QuerySelector(".listings-card__subtitle"); 
                var link =  $"https://uptree.co{OneProgram.QuerySelector("a").GetAttribute("href")}"; //Get attribute looks into the same line rather than the line under it, so OneProgram.QuerySelector("a") provides the line that contains the link and get arrtibute use href to get the link
                
                
                if (!PreviousLinks.Contains(link)) //check if this is a new event
                {
                    Events Object1 = new Events(NameInfo.TextContent.Trim(),DateInfo.TextContent.Trim(),link);  //create a new events object everytime, text content to include the text only and trim and remove whitespaces
                    ListOfEvents.Add(Object1); //add the current event object to the list
                }
                CurrentLinks.Add(link); //add to the links of this current run
                
            }
        
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            
            
        }
        i++;  //increase i to move to next page
        MaxRequest = 0; //reset max request so each page can have three reload
    }
}


void displayEvents()  //for local testing only, display all new events
{
    foreach (var j in ListOfEvents)
    {
        Console.WriteLine(j.showEvent()); //j represent a single event object, and show event will display the content of that object
    }
}


void ClearLinks()
{
    using (StreamWriter writer = new StreamWriter("EventLinks.txt",false))
    {
        writer.Write("");
    } 
}


//save the links of the current run
void SaveCurrentLinks()
{
    
    
    using (StreamWriter writer = new StreamWriter("EventLinks.txt",true))  //set the stream writer to append mode everytime
    {
        foreach (var k in CurrentLinks)
        {   
         writer.WriteLine(k); //save only the link, so this can be used to filter which ones are new later
        }
    }
    
}




string NumberAndError()
{   
    if (FailedPages.Count != 0)
    {
        return $"Total number of new events {ListOfEvents.Count}\n Failed pages = {string.Join(",",FailedPages)}";
    }
    else
    {
        return $"Total number of new events {ListOfEvents.Count}. No page error";
    }
     
}


async Task main()
{
    GetLastRun();
    await scraping();  // there is pause in this method so awair allows waiting
    displayEvents();
    if (FailedPages.Count ==0)  //only clear when there is no fail pages, because if links are cleared when some pages failed, it will count the old ones that aren't loaded as new
    {
        ClearLinks();
    }
    SaveCurrentLinks();
    Console.WriteLine(NumberAndError());

}

await main();



class Events //storing event details
{
    private string name;
    private string date;
    private string link;

    public string Name{get{return name;} }  // no setter is used so  the fields to read only
    public string Date{get{return date;} }
    public string Link{get{return link;} }

    public Events(string name, string date, string link)
    {
        this.name = name;
        this.date = date;
        this.link = link;
    }
    public string showEvent()
    {
        return $"{name}\n{date}\n{link}\n";
    }

}


