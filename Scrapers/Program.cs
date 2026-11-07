using AngleSharp.Dom.Events;

UpTreeScraper uptree = new UpTreeScraper(); //object for the UpTree scraper
await uptree.main();
List<UpTreeScraper.Events> uptreeEvents = uptree.ListOfEvents;

FuturesForAllScraper futures = new FuturesForAllScraper();
await futures.main();
List<FuturesForAllScraper.Event> futuresEvents = futures.listOfEvents;
