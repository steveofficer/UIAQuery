namespace UIAQuery

[<System.Runtime.CompilerServices.Extension>]
module Query =
    open System.Windows.Automation
    open System.Reflection
    open System.Linq

    /// Get a sequence of all public fields from the source_type that are the same type as exposed_type
    let find_static_fields<'exposed_type> (source_type : System.Type) (exposed_type : System.Type) format_name =
        seq {
            yield! source_type.FindMembers(
                MemberTypes.Field, 
                BindingFlags.Static ||| BindingFlags.Public, 
                MemberFilter(fun (mi : MemberInfo) obj -> exposed_type = (mi :?> FieldInfo).FieldType),
                null
            )
            |> Seq.map (fun mi -> (format_name mi.Name), (mi :?> FieldInfo).GetValue(null) :?> 'exposed_type)
        }

    /// A Map of all the supported ControlTypes
    let control_types = lazy (
        Map (
            find_static_fields<ControlType> typeof<ControlType> typeof<ControlType> id
        )
    )

    /// A Map of all the supported properties
    let property_names = lazy (
        Map (
            find_static_fields<AutomationProperty> typeof<AutomationElement> typeof<AutomationProperty> (fun name -> name.Replace("Property", ""))
        )
    )

    /// Transform an AutomationElementCollection into an AutomationElement list
    /// If collection is null or empty then [] is returned
    let to_list (collection : AutomationElementCollection) =
        if collection = null || collection.Count = 0
        then []
        else List.map (fun x -> collection.Item(x))  [0..collection.Count-1]

    /// Retrieve the ControlType with the name of control_type
    let to_control_type control_type =
        match control_types.Value.TryFind(control_type) with
            | Some value -> value
            | None -> failwithf "Unknown ControlType: %s" control_type

    /// Retrieve the AutomationProperty with the name of property_name
    let to_property property_name = 
        match property_names.Value.TryFind(property_name) with
            | Some value -> value
            | None -> failwithf "Unknown PropertyName: %s" property_name

    /// Convert the property_name and value into a Condition
    let condition property_name value = 
        let property = to_property property_name
        PropertyCondition(property, value) :> Condition

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
    
    /// Active pattern to determine if the expression selects elements on 1 property or multiple properties
    let (|SinglePropertyExpression|MultiPropertyExpression|) (expression : string) =
        let regex_match = System.Text.RegularExpressions.Regex.Match(expression, "^(.*)\[(.*)\]")
        if regex_match.Success
        then MultiPropertyExpression (regex_match.Groups.[1].Value, regex_match.Groups.[2].Value)
        else SinglePropertyExpression expression

    /// Splits an expression containing multiple named properties into a list
    let split_named_properties properties = 
        // Split on commas, but ignore embedded commas
        [properties]

    let split_query (query : string) = 
        // Split on spaces, but ignore embedded spaces
        query.Split([|' '|])

    /// Returns the Condition for the shorthand expression (.class_name, #identifier, or ControlName)
    let parse_shorthand_condition =
        function
            | ClassExpression class_name -> condition "ClassName" class_name
            | IdExpression identifier -> condition "AutomationId" identifier
            | ControlExpression control_type -> condition "ControlType" control_type
        
    /// Returns a Condition for the named property expression (PropertyName=value)
    let parse_named_property (expression : string) = 
        let matches = System.Text.RegularExpressions.Regex.Match(expression, "^(.*)=(.*)$")
        let name = matches.Groups.[1].Value
        let value = matches.Groups.[2].Value
        printfn "Name %s\nValue %s" name value
        condition name value

    /// Parses the expression into a Condition
    let parse expression =
        // Work out what the scope of the expression is (Child or Descendant)
        let scope, expression = 
            match expression with
                | DescendantExpression expression -> TreeScope.Descendants, expression
                | ChildExpression expression -> TreeScope.Children, expression
        
        // split the expression up into the first "shorthand" condition, and the subsequent secondary conditions
        let initial_condition, secondary_properties = 
            match expression with
                | SinglePropertyExpression property -> property, []
                | MultiPropertyExpression (initial_property, other_properties) -> initial_property, other_properties |> split_named_properties
        
        // Parse the conditions and put them all into a single list
        let conditions = (parse_shorthand_condition initial_condition)::(List.map parse_named_property secondary_properties)
        
        (scope, conditions)
    
    /// Matches the conditions against each control in parent_controls and returns all of the matched elements
    let find_elements (parent_controls : AutomationElement list) (scope, conditions) = 
        let condition = 
            match conditions with
                | [condition] -> condition
                | _ -> AndCondition(conditions |> List.toArray) :> Condition
        
        printfn "%A" (condition.ToString())

        parent_controls
        |> List.map (fun control -> control.FindAll(scope, condition) |> to_list)
        |> List.concat

    [<System.Runtime.CompilerServices.Extension>]
    let Query (parent : AutomationElement) (query : string) =
            split_query query
            |> Array.map parse
            |> Array.fold find_elements [parent]
            |> List.toArray