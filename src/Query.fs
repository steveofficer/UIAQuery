namespace UIAQuery

[<System.Runtime.CompilerServices.Extension>]
module Query =
    open System.Windows.Automation
    open System.Linq
    open TypeMap
    open System.Text.RegularExpressions

    // Active pattern to determine if the expression is selecting elements based on class, id or control type
    let (|ClassExpression|IdExpression|ControlTypeExpression|) (expression : string) = 
        match expression.[0] with
            | '#' -> IdExpression(expression.Substring(1))
            | '.' -> ClassExpression(expression.Substring(1))
            | _ -> ControlTypeExpression(expression |> as_control_type)

    let parse (query : string) = 
        let ``parse property`` (name_value_pair : string) = 
            let matches = Regex.Match(name_value_pair, "^(?<name>.*)=(?<value>.*)$")
            let name = matches.Groups.["name"].Value
            let value = matches.Groups.["value"].Value
            as_condition name value

        let (++) (primary_condition : Condition) (additional_conditions : Condition list) = 
            match additional_conditions with
                | [] -> primary_condition
                | conditions -> 
                    let all_conditions = primary_condition::conditions |> List.toArray
                    AndCondition(all_conditions) :> Condition

        let ``create condition`` (scope : string) (primary : string) (additional : string) =
            let tree_scope = match scope with
                                | ">" -> TreeScope.Children
                                | _ -> TreeScope.Descendants
            let primary_condition = match primary with
                                    | ClassExpression class_name -> as_condition "ClassName" class_name
                                    | IdExpression identifier -> as_condition "AutomationId" identifier
                                    | ControlTypeExpression control_type -> as_condition "ControlType" control_type
            let additional_conditions = if System.String.IsNullOrEmpty(additional)
                                        then []
                                        else [ for property in additional.Split([|';'|]) -> property.Trim() |> ``parse property`` ]
            let condition = primary_condition ++ additional_conditions
            (tree_scope, condition)

        // An optional '>' to denote child scope
        // Followed by a sequence of specified characters
        // Optionally followed by additional characters enclosed within []
        let query_pattern = "(?<child_scope>[>]?)(?<primary>[\.#a-zA-Z0-9_]+)(\[(?<additional>.*)\])?"
        let regex_matches = Regex.Matches(query, query_pattern)
        [| for m in regex_matches -> ``create condition`` m.Groups.["child_scope"].Value m.Groups.["primary"].Value m.Groups.["additional"].Value |]

    [<System.Runtime.CompilerServices.Extension>]
    let Query (parent : AutomationElement) (query : string) = 
        let ``find matching controls`` (parents : AutomationElement list) (scope : TreeScope, condition : Condition) = 
            parents |> List.collect (fun parent -> parent.FindAll(scope, condition) |> to_list)

        let ``execute query`` (conditions : (TreeScope * Condition)[]) = 
            Array.fold ``find matching controls`` [parent] conditions |> List.toArray
        
        parse query |> ``execute query``