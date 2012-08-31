#r "UIAutomationTypes"
#r "UIAutomationClient"

#load "Query.fs"
open UIAQuery.Query
open System.Windows.Automation

#time
let y = Query AutomationElement.RootElement ">.Window[Name=ToDo List] #is_done"
#time
