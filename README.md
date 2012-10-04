UIAQuery
========

A query processor that allows for jQuery like selectors for extracting AutomationElements from the .NET UI Automation library

Query Syntax
------------
#### Query Scope ####
 Adding *>* to the start of a query scopes it to only the immediate children of the parent control. Without it, queries are scoped to all descendants of the parent control.
* ##### Direct Children Example: #####
       
        var children_checkboxes = parent_control.Query(">CheckBox");
 
* ##### Descendant Example: #####
       
        var descendant_checkboxes = parent_control.Query("CheckBox");
       
#### Select by Class #####
 To select any children where the ClassName property is 'list_item': 
       
       parent_control.Query(">.list_item")

#### Select by AutomationId #####
 To select any descendants where the AutomationId is 'focussed': 
       
       parent_control.Query("#focussed")

#### Select by ControlType ####
 To select any Buttons that are children of descendants where the ClassName is 'button_container': 
       
       parent_control.Query(".button_container >Button")