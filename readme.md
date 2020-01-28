**LightActivityTracker**

Light-weight activity tracker with simple filter / blacklisting system (Windows only).

This is a very straight-forward activity tracker for Windows. It detects whenever the foreground applications changes and logs this event (Timestamp/Process/WindowTitle/IdleStatus) to a simple CSV file. 

To detect when the user is idling / AFK, the applications monitors keyboard and mouse events system wide. However none of this sensitive data is ever logged to the CSV file. No data is ever sent over the internet by this tool.
To further enhance privacy / security a filtering system is included. Simply add your filters to the included fitlers.json file to completely redact the filtered processes from the CSV file (this is done at log-time, so filtered events can never be recovered).

For example, to filter out all window titles of Private Browsing tabs in Firefox the following filter does the trick (which is included by default):

```javascript
[
  {
    "ProcessRegEx": "firefox",
    "TitleRegEx": ".*Private Browsing.*",
    "RedactedProcess": "firefox",
    "RedactedTitle": "incognito"
  }
]
```

No visualization / analysis capabilities are included. I attached a small R script I'm using to analyze a user's activity.


Written in C# and built in VS Studio 2019. Contributions / suggestions / bug-reports welcome.