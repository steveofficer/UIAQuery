#r "UIAutomationTypes"
#r "UIAutomationClient"

#load "TypeMap.fs"
#load "Query.fs"

open UIAQuery.Query
open System.Windows.Automation


let y = Query AutomationElement.RootElement ">.Window[Name=ToDo List] CheckBox"