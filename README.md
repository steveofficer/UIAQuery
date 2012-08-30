UIAQuery
========

A query processor that allows for JQuery like selectors for extracting AutomationElements from the .NET UI Automation library

Examples
--------

#### Select by Class #####
To select all the children that have a ClassName property of 'list_item' then you would use: parent_control.Query(">.list_item")

#### Select by AutomationId #####
To select any descendants that have an AutomationId property of 'focussed' then you would use: parent_control.Query("#focussed")

#### Select by ControlType ####
To select any buttons that are children of elements with a ClassName of 'button_container' then you would use: parent_control.Query(".button_container >Button")