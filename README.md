# ApprentEase Work Experience Scraper

An automated C# web scraper that collects work experience opportunities from multiple platforms using HTML parsing, API integration, Discord webhooks and GitHub Actions workflows to deliver new opportunities to the ApprentEase Discord community.

## Features

- **Multiple sources:** Currently supports Uptree and Futures For All.
- **HTML parsing:** Uses AngleSharp and CSS selectors to extract opportunity from HTML pages.
- **API :**  processes JSON data from the Futures For All API.
- **Duplicate detection:** Tracks previously collected event links using text files to identify new opportunities and avoid reposting existing ones.
- **Discord integration:** posts new opportunities through Discord webhooks using formatted embeds.
- **Error handling:** Implements request retries, failed-page tracking and failed-message recovery.
- **Automated execution:** Uses scheduled GitHub Actions workflows to run the scraper weekly.
- **Security:** Stores the Discord webhook URL securely using GitHub Secrets.

## How It Works

### 1. Data Collection

**Uptree — HTML Parsing**

`HttpClient` sends GET requests to Uptree. AngleSharp parses the HTML, and CSS selectors locate the event cards and extract their names, dates and links.

The scraper follows Uptree's paged listings to collect opportunities across multiple pages.

**Futures For All — JSON API**

`HttpClient` sends GET requests to the Futures For All opportunities API. The scraper uses `System.Text.Json` to read the event name, organisation, type, sector, start date and duration.

It requests the next page while the API's `hasNextPage` property is `true`.

### 2. Duplicate Detection

Two text files store the event links collected during the previous run.

- `EventLinks.txt` stores Uptree links
- `EventLinks2.txt` stores Futures For All links.

The scrapers check each link against the corresponding file. If a link is already there, they skip it. Otherwise, they add the opportunity to the list to post.

After scraping, the application replaces both files with the links from the current run.

### 3. Discord Integration

The application formats new opportunities as Discord embeds using the available event details. It sends each embed to the configured webhook with `HttpClient.PostAsJsonAsync()`.

It retries failed messages and records delivery failures in `failedEvents.txt` for further recovery attempts.

### 4. GitHub Actions Automation

The YAML workflow automatically runs every Monday at 10:00 am (Europe/London).

1. Checks out the `main` branch containing the source code.
2. Checks out the `data` branch containing the previous run's event links.
3. Set up .NET and puts the link files where the application can read them.
4. Runs the scraper.
5. Copy the updated link files back into the `data` branch checkout.
6. Commits and pushes the changes to the `data` branch.

The workflow reads the Discord webhook URL from the `DISCORD_WEBHOOK_URL` GitHub Actions secret to enhance security as it is separate from the source code.

## Error Handling and Limitations

The application retries failed HTTP requests and Discord messages. It keeps track of pages it couldn't fetch and records failed Discord deliveries in `failedEvents.txt`.

**Current limitations:**

- The scraper only compares links against the previous run, so an opportunity can be reposted if it disappears for a run and then returns.
- Failed events recovery has not yet been tested
- Changes to the source websites or APIs may lead to scrapers failure.

## Running Locally

You'll need the .NET 10 SDK and a Discord webhook URL.

Set the webhook URL as an environment variable in PowerShell

```powershell
$env:DISCORD_WEBHOOK_URL = "your webhook url"
```

Run the application from the repository root:

```powershell
dotnet run --project "Scrapers/ApprentEase Scraper.csproj"
```

---

Developed for the **ApprentEase Discord community**.
