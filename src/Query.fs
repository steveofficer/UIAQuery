namespace UIAQuery

[<System.Runtime.CompilerServices.Extension>]
module Query =
    open System.Windows.Automation
    open System.Linq
    open TypeMap

    /// Active pattern for determining if the scope of the expression is Descendants or Children
    let (|DescendantExpression|ChildExpression|) (expression : string) =
        if expression.[0] = '>'
        then ChildExpression (expression.Substring(1))
        else DescendantExpression expression

    /// Active pattern to determine if the expression is selecting elements based on class, id or control type
    let (|ClassExpression|IdExpression|ControlExpression|) (expression : string) = 
        let first = expression.[0]
        match first with
            | '#' -> IdExpression(expression.Substring(1))
            | '.' -> ClassExpression(expression.Substring(1))
            | _ -> ControlExpression(expression)
    
    /// Splits an expression containing multiple named properties into a list
    let split_named_properties (properties : string) = 
        properties.Split([|';'|]) 
        |> Array.map (fun s -> s.Trim()) 
        |> Array.toList

    let extract_expressions (query : string) = 
        let to_pair (m : System.Text.RegularExpressions.Match) =
            let initial = m.Groups.["initial"].Value
            let secondary_group = m.Groups.["secondary"]
            if secondary_group.Success
            then (initial, Some(secondary_group.Value))
            else (initial, None)

        // match specified alphanumerics followed by optional [] enclosing anything
        // each match is a "query expression"
        let regex_matches = System.Text.RegularExpressions.Regex.Matches(query, "(?<initial>[\.#>A-Za-z0-9_]+)(\[(?<secondary>.*)\])?")
        [| for m in regex_matches -> to_pair m |]

    /// Returns the Condition for the shorthand expression (.class_name, #identifier, or ControlName)
    let parse_shorthand_condition =
        function
            | ClassExpression class_name -> as_condition "ClassName" class_name
            | IdExpression identifier -> as_condition "AutomationId" identifier
            | ControlExpression control_type -> as_condition "ControlType" (control_type |> as_control_type)
        
    /// Returns a Condition for the named property expression (PropertyName=value)
    let parse_named_property (expression : string) = 
        let matches = System.Text.RegularExpressions.Regex.Match(expression, "^(?<name>.*)=(?<value>.*)$")
        let name = matches.Groups.["name"].Value
        let value = matches.Groups.["value"].Value
        as_condition name value

    let query_condition (initial_condition : string, secondary_conditions : string option) = 
        // Work out what the scope of the expression is (Child or Descendant)
        let scope, primary_expression = 
            match initial_condition with
                | DescendantExpression expression -> TreeScope.Descendants, expression
                | ChildExpression expression -> TreeScope.Children, expression
        
        // determine if there are any secondary conditions
        let secondary_conditions = 
            match secondary_conditions with
                | Some properties -> 
                    properties 
                    |> split_named_properties
                    |> List.map parse_named_property
                | None -> []
        
        // Parse the conditions and put them all into a single list
        let conditions = (parse_shorthand_condition primary_expression)::secondary_conditions
        
        (scope, conditions)

    /// Matches the conditions against each control in parent_controls and returns all of the matched elements
    let execute_query (parent_controls : AutomationElement list) (scope, conditions) = 
        let condition = 
            match conditions with
                | [condition] -> condition
                | _ -> AndCondition(conditions |> List.toArray) :> Condition
        
        parent_controls
        |> List.map (fun control -> control.FindAll(scope, condition) |> as_list)
        |> List.concat

    [<System.Runtime.CompilerServices.Extension>]
    let Query (parent : AutomationElement) (query : string) =
        // Extract each sub-query and then transform each sub-query into a condition
        let query_conditions = 
            extract_expressions query 
            |> Array.map query_condition
        
        // Execute each condition against the result of the previous condition, starting with parent
        Array.fold execute_query [parent] query_conditions |> List.toArray