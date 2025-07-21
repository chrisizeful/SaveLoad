using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Collections.Generic;

namespace SaveLoad;

/// <summary>
/// A resolver that ignores a list of properties when serializing.
/// </summary>
public class IgnoreResolver : DefaultContractResolver
{

    private readonly HashSet<string> _ignoreProps;

    public IgnoreResolver(HashSet<string> ignoreProps)
    {
        _ignoreProps = ignoreProps;
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);
        if (_ignoreProps.Contains(property.UnderlyingName))
        {
            property.ShouldSerialize = p => false;
            property.ShouldDeserialize = p => false;
            property.Writable = false;
            property.Readable = false;
            property.Ignored = true;
        }
        return property;
    }
}
