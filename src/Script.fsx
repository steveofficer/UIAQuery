#r "UIAutomationTypes"
#r "UIAutomationClient"

#load "Query.fs"
open UIQuery.Query
open System.Windows.Automation

#time
let y = Query AutomationElement.RootElement ">.Window #is_done"
#time
