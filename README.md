UIAQuery
========

A query processor that allows for jQuery like selectors for extracting AutomationElements from the .NET UI Automation library

Examples
--------

#### Select by Class #####
 To select any children where the ClassName property is 'list_item': 
       
       parent_control.Query(">.list_item")

#### Select by AutomationId #####
 To select any descendants where the AutomationId is 'focussed': 
       
       parent_control.Query("#focussed")

#### Select by ControlType ####
 To select any Buttons that are children of descendants where the ClassName is 'button_container': 
       
       parent_control.Query(".button_container >Button")