module TypeMap
    open System.Reflection
    open System.Windows.Automation

    /// Get a sequence of all public fields from the source_type that are the same type as exposed_type
    let private static_fields<'exposed_type> (source_type : System.Type) format_name =
        seq {
            let exposed_type = typeof<'exposed_type>
            yield! source_type.FindMembers(
                MemberTypes.Field, 
                BindingFlags.Static ||| BindingFlags.Public, 
                MemberFilter(fun (mi : MemberInfo) obj -> exposed_type = (mi :?> FieldInfo).FieldType),
                null
            )
            |> Seq.map (fun mi -> (format_name mi.Name), (mi :?> FieldInfo).GetValue(null) :?> 'exposed_type)
        }

    /// A Map of all the supported ControlTypes
    let private ControlTypes = lazy (
        Map (
            static_fields<ControlType> typeof<ControlType> id
        )
    )

    /// A Map of all the supported properties
    let private PropertyNames = lazy (
        Map (
            static_fields<AutomationProperty> typeof<AutomationElement> (fun name -> name.Replace("Property", ""))
        )
    )   

    /// Transform an AutomationElementCollection into an AutomationElement list
    /// If collection is null or empty then [] is returned
    let as_list (collection : AutomationElementCollection) =
        if collection = null || collection.Count = 0
        then []
        else [ for item in collection -> item ]

    /// Retrieve the ControlType with the name of control_type
    let as_control_type control_type =
        match ControlTypes.Value.TryFind(control_type) with
            | Some value -> value
            | None -> failwithf "Unknown ControlType: %s" control_type

    /// Retrieve the AutomationProperty with the name of property_name
    let as_property property_name = 
        match PropertyNames.Value.TryFind(property_name) with
            | Some value -> value
            | None -> 
                Map.iter (fun k v -> printfn "%s" k) PropertyNames.Value
                failwithf "Unknown PropertyName: %s" property_name

    /// Convert the property_name and value into a Condition
    let as_condition property_name value = PropertyCondition((property_name |> as_property), value) :> Condition