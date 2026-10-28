using System.Net.Http; 
using System.Net;  
using System.Text.Json; //for scraping Json
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.ComponentModel;
using AngleSharp.Dom.Events;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualBasic;
using System.Runtime.CompilerServices;


//https://api.futuresforall.org/portal/opportunities/search?Filter=IsGroupBookable%20ne%20true&SortBy=AdvertiseStartAt&SortDirection=Ascending&PageSize=12&PageNumber=1&Sectors=11&Sectors=1&Sectors=3&Sectors=17&SearchFields=SectorName,Organisation/OrganisationName,OpportunityName
//this url will return Json instead of html
//sectors of this url = Engineering Business & Sales  Accounting, Banking & Finance Digital, IT & Computing



async Task<string> GettingJson(string url) //this method is for requesting and getting the json file  , Task<string> means it will return a string and the Task allows pause
{

    HttpClient client = new HttpClient();
    int max = 0;
    string? json = null;
    while(max <3)
    {
        try
        {
        
         json = await client.GetStringAsync(url);
         //Console.WriteLine(json);
         break;
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                break;
            }
            max++;
            continue;
        }
    }
    if (json == null)
    {
        throw new Exception("no Json fetched");
    }
    
    return json;

}

List<string> PreviousLinks = new List<string>();
List<string> CurrentLinks = new List<string>();
List<Event> listOfEvents = new List<Event>();
bool? scrap(string json) //this method is to scrap the event details from the json file and store them in a list
{
    bool? NextPage = null;
    using (JsonDocument document =  JsonDocument.Parse(json))
    {
        var roots = document.RootElement;  // this get the outmost Json objectss of the document
        var opportunities = roots.GetProperty("items"); //get property method belongs to Json elements, "items" in the Json file here stores the data we want, so opportunities now store the Json values of the "items" object
        NextPage = roots.GetProperty("hasNextPage").GetBoolean(); //return true of false
        int OpportunitiesCount = opportunities.GetArrayLength();

            for (int i =0;i<OpportunitiesCount;i++)
            {
                var CurrentEvent = opportunities[i];
                string link = $"https://finder.futuresforall.org/opportunity-detail/{CurrentEvent.GetProperty("opportunityId")}"; //The Json file does not directly provide the url but it combines with the websites path with the id to produce the url to the event page
                CurrentLinks.Add(link);
                if (!PreviousLinks.Contains(link)) //checking the link first improves runtime 
                {
                    string eventName = CurrentEvent.GetProperty("opportunityName").ToString();
                    string orginationName = CurrentEvent.GetProperty("organisation").GetProperty("organisationName").ToString();;
                    string type = CurrentEvent.GetProperty("opportunityTypeName").ToString();
                    string sector = CurrentEvent.GetProperty("sectorName").ToString();
                    string startDate = CurrentEvent.GetProperty("opportunityStartAt").ToString();
                    string  days = CurrentEvent.GetProperty("opportunityDurationDays").ToString();
                    

                    listOfEvents.Add(new Event(eventName,orginationName,type,sector,startDate,days,link));
                }

            }
        }
    
    return NextPage;
}

void UploadNewLinks(List<string> listOfLinks)
{
    using (StreamWriter writer = new StreamWriter("EventLinks2.txt",false)) //overwrite mode so it removes the exisiting links in the file
    {
        foreach (string link in listOfLinks)
        {
            writer.WriteLine(link);
        }
    }
}




void GetLastRun()
{
   try
   {
    using (StreamReader reader = new StreamReader("EventLinks2.txt"))
    {
        string? CurrentLine = reader.ReadLine();
        while (CurrentLine != null)
        {
            PreviousLinks.Add(CurrentLine);
            CurrentLine = reader.ReadLine();
        }
    }
   }
   catch(FileNotFoundException) //cathch error when file doesn't exist, happen in every first run
    {
        return;
    }
}






async Task main()
{

    int i = 1;
    GetLastRun();
    string? json = null;
    do
    {
       json = await GettingJson($"https://api.futuresforall.org/portal/opportunities/search?Filter=IsGroupBookable%20ne%20true&SortBy=AdvertiseStartAt&SortDirection=Ascending&PageSize=12&PageNumber={i}&Sectors=11&Sectors=1&Sectors=3&Sectors=17&SearchFields=SectorName,Organisation/OrganisationName,OpportunityName");
       i++;
       //Console.WriteLine(i);
    }
    while(scrap(json) == true);
    
    UploadNewLinks(CurrentLinks);
    


  


    foreach(var k in listOfEvents)
    {
        Console.WriteLine(k.Display());
    }
    int numberOfEvent = listOfEvents.Count;
    //Console.WriteLine(numberOfEvent);

}
await main();

class Event
{
    private string eventName;
    private string organisationName;
    private string type; //in person or virtual
    private string sector;
    private string startDate;
    private string days;
    private string link;

    public string EventName {get{return eventName;}} //read only
    public string OrganisationName {get{return organisationName;}}
    public string Type {get{return type;}}
    public string Sector {get{return sector;}}
    public string StartDate {get{return startDate;}}
    public string Days {get{return days;}}
    public string Link {get{return link;}}

    public Event(string EventName,string OrganisationNmae, string Type, string Sector, string StartDate, string Days,string Links)
    {
        eventName = EventName;
        organisationName = OrganisationNmae;
        type = Type;
        sector = Sector;
        startDate = StartDate;
        days = Days;
        link = Links;

    }
    public string Display() //for local testing
    {
        return$"Event name = {eventName}\n Organisation name = {organisationName}\n type = {type}\n Sector = {sector}\n Start date = {startDate}\n Duration = {days} days\n Link = {link}\n";
    }

}
