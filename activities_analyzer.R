require(data.table)
require(stringi)

#point to activites.csv
activites <- fread("C:/Users/Max/Documents/Programs/LightActivityTracker/activities.csv", encoding = "UTF-8")
activites[, dt := as.POSIXct(dt)]
activites[, time_spent := shift(dt,-1)-dt]
activites[, time_spent := time_spent * !((shift(IsAFK_Event,-1)==T) & (IsAFK_Event==T))]

activites[, Title := stri_replace_last(Title, "$1", regex = "(\\.[A-Za-z0-9]+?)\\*")]

activites[Title != "Shut Down Windows" & time_spent > 1 & IsAFK_Event == FALSE, .(time_spent = as.numeric(sum(time_spent), units = "mins")), by = .(Process)][order(time_spent)]

