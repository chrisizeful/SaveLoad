using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Collections.Generic;

namespace SaveLoad;

public class IgnoreResolver : DefaultContractResolver
{

    private readonly List<string> _ignoreProps;

    public IgnoreResolver(List<string> ignoreProps)
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
