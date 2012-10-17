UIAQuery
========

A wrapper around the .NET UI Automation library that allows you to query elements using syntax similar to jQuery\CSS selectors.

Query Syntax
------------
In the examples "parent_control" has the type "AutomationElement".

#### Select by Class #####
 Class selectors start with *.*
 To select elements where the ClassName property is 'list_item': 
       
       var list_items = parent_control.Query(".list_item");

#### Select by AutomationId #####
 AutomationId selectors start with *#*
 To select elements where the AutomationId is 'focused': 
       
       var focused = parent_control.Query("#focused");

#### Select by ControlType ####
 Queries that are neither by AutomationId nor by Class are treated as ControlType queries.
 To select Buttons: 
       
       var buttons = parent_control.Query("Button");
       
#### Query Scope ####
 Adding *>* to the start of a query scopes it to only the immediate children of the parent control. Without it, queries are scoped to all descendants of the parent control.
* ##### Direct Children Example: #####
       
        var children_checkboxes = parent_control.Query(">CheckBox");
 
* ##### Descendant Example: #####
       
        var descendant_checkboxes = parent_control.Query("CheckBox");
       
