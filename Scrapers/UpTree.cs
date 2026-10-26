using System.Net.Http; //using the httpClient class
using System.Net;  //used when catching http exception
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.ComponentModel; 


HttpClient client = new HttpClient();   //object used for the request later
int i = 1; //keep track of page number
int totalEvents = 0;
int MaxRequest = 0;  //this is to stop infinite loop when http request fail but is not 404
List<int> FailedPages = new List<int>(); //store the page number of failed pages
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
            
            
            Console.WriteLine(NameInfo.TextContent.Trim()); //textcontent to remove html headers and get the content between the headers and trim to remove whitespaces
            Console.WriteLine(DateInfo.TextContent.Trim());
            Console.WriteLine(link);
            Console.WriteLine(); //better spacing
            totalEvents++;
        }
        catch
        {
            throw new Exception("Error");
        }
        
    }
    i++;  //increase i to move to next page
    MaxRequest = 0; //reset max request so each page can have three reload
}
Console.WriteLine($"Total number of events {totalEvents}");
if (FailedPages.Count != 0)
{
    Console.WriteLine(string.Join(",",FailedPages));
}
