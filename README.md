# Data branch
This branch stores EventLinks.txt and EventLinks2.txt, which contain the links collected during the most recent scraper run.

The code in the main branch reads these files to compare against newly scraped events to identify which events are new.

After each run, GitHub actions automatically updates the two text files and commits and pushes the changes back to this branch.
