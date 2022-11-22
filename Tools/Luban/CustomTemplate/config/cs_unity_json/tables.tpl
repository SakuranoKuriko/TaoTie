using Bright.Serialization;
using SimpleJSON;
using System.Collections.Generic;
{{
    name = x.name
    namespace = x.namespace
    tables = x.tables
}}
namespace {{namespace}}
{
   
public sealed partial class {{name}}
{
    {{~for table in tables ~}}
{{~if table.comment != '' ~}}
    /// <summary>
    /// {{table.escape_comment}}
    /// </summary>
{{~end~}}
    public {{table.full_name}} {{table.name}} { get; private set; }
    {{~end~}}

    private {{name}}() { }

    public {{name}}(System.Func<string, JSONNode> loader)
    {
        var tables = new Dictionary<string, object>();
        {{~for table in tables ~}}
        {{table.name}} = new {{table.full_name}}(loader("{{table.output_data_file}}")); 
        tables.Add("{{table.full_name}}", {{table.name}});
        {{~end~}}
        PostLoad(tables);
    }

    public static {{name}} Load(System.Func<string, JSONNode> loader)
    {
        var tables = new Dictionary<string, object>();
        var instance = new {{name}}();
        {{~for table in tables ~}}
        instance.{{table.name}} = new {{table.full_name}}(loader("{{table.output_data_file}}"));
        tables.Add("{{table.full_name}}", instance.{{table.name}});
        {{~end~}}
        instance.PostLoad(tables);
        return instance;
    }

    void PostLoad(Dictionary<string, object> tables)
    {
        PostInit();
        {{~for table in tables ~}}
        {{table.name}}.Resolve(tables); 
        {{~end~}}
        PostResolve();
    }

    public void TranslateText(System.Func<string, string, string> translator)
    {
        {{~for table in tables ~}}
        {{table.name}}.TranslateText(translator); 
        {{~end~}}
    }
    
    partial void PostInit();
    partial void PostResolve();
}

}
