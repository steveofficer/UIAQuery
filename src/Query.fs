namespace UIAQuery

[<System.Runtime.CompilerServices.Extension>]
module Query =
    open System.Windows.Automation
    open System.Reflection
    open System.Linq

    let control_types = lazy (
        Map(
            seq {
                yield! typeof<ControlType>.FindMembers(
                    MemberTypes.Field, 
                    BindingFlags.Static ||| BindingFlags.Public, 
                    MemberFilter(fun (mi : MemberInfo) obj -> mi.ReflectedType = typeof<ControlType>),
                    null
                )
                |> Seq.map (fun control -> (control.Name, (control :?> FieldInfo).GetValue(null) :?> ControlType))
            }
        )
    )

    type Selector =
        | Id of string
        | Class of string
        | Control of ControlType

    type Relationship =
        | Child of Selector
        | Descendant of Selector

    let to_list (collection : AutomationElementCollection) =
        if collection = null || collection.Count = 0
        then []
        else List.map (fun x -> collection.Item(x))  [0..collection.Count-1]

    [<System.Runtime.CompilerServices.Extension>]
    let Query (parent : AutomationElement) (query : string) =
            let parse expression =
                let (|ClassExpression|IdExpression|DirectChildExpression|ControlExpression|) (selector : string) = 
                    let first = selector.[0]
                    let remainder = selector.Substring(1)
                    match first with
                        | '#' -> IdExpression(remainder)
                        | '.' -> ClassExpression(remainder)
                        | '>' -> DirectChildExpression(remainder)
                        | _ -> ControlExpression(selector)
                
                let to_control_type input =
                    match control_types.Value.TryFind(input) with
                        | Some value -> value
                        | None -> failwithf "Unknown ControlType: %s" input

                let rec parser item relationship =
                    match item with
                        | ClassExpression class_name -> Class(class_name) |> relationship
                        | IdExpression identifier -> Id(identifier) |> relationship
                        | ControlExpression control_type -> Control(to_control_type control_type) |> relationship
                        | DirectChildExpression sub_expression -> parser sub_expression (fun x -> Child(x))
                parser expression (fun x -> Descendant(x))
            
            let query_evaluater (parent_controls : AutomationElement list) query = 
                let scope, condition = 
                    let scope, selector = 
                        match query with
                            | Child selector -> TreeScope.Children, selector
                            | Descendant selector -> TreeScope.Descendants, selector
                    let condition = 
                        match selector with
                            | Class class_name -> PropertyCondition(AutomationElement.ClassNameProperty, class_name)
                            | Id identifier -> PropertyCondition(AutomationElement.AutomationIdProperty, identifier)
                            | Control control_type -> PropertyCondition(AutomationElement.ControlTypeProperty, control_type)
                    (scope, condition)

                parent_controls
                |> List.map (fun control -> control.FindAll(scope, condition) |> to_list)
                |> List.concat

            query.Split([|' '|]) 
            |> Array.map parse
            |> Array.fold query_evaluater [parent]
            |> List.toArray