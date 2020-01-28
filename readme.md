# Light Activity Tracker

Light-weight activity tracker with simple filter / blacklisting system (Windows only).

This is a very straight-forward activity tracker for Windows. It detects whenever the foreground applications changes and logs this event (Timestamp/Process/WindowTitle/IdleStatus) to a simple CSV file. If you are looking for a more comprehensive (or cross-platform) activity tracker / analyzer check out https://github.com/ActivityWatch/activitywatch .

### Usage
Build the solution (only tested in VS 2019) and run LightActivityTracker.exe - it automatically minimizes to the system tray and logs a file called activities.csv in the folder as the .exe file. Right-click the tray and select Quit to close the tracker.

### AFK Detection
To detect when the user is idling / AFK, the applications monitors keyboard and mouse events system wide. However none of this sensitive data is ever logged to the CSV file, it is only used for AFK detection. No data is ever sent over the internet by this tool.

### Filtering of sensitive activity
To further enhance privacy / security a filtering system is included. Simply add your filters to the included fitlers.json file (RegEx syntax) to completely redact the filtered processes from the CSV file (this is done at log-time, so filtered events can never be recovered).

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

No visualization / analysis capabilities are included. I attached a small R script I'm using to analyze my activity.


Written in C# and built in Visual Studio 2019. No warranty or anything - use this tool only if you know what you are doing. Contributions / suggestions / bug-reports welcome.