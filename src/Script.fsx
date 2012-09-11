#r "UIAutomationTypes"
#r "UIAutomationClient"

#load "TypeMap.fs"
#load "Query.fs"

open UIAQuery.Query
open System.Windows.Automation

#time
let y = Query AutomationElement.RootElement ">.Window[Name=ToDo List; IsEnabled=true] CheckBox"
#time