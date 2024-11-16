using System;

namespace SaveLoad;

#nullable enable
[AttributeUsage(AttributeTargets.Method)]
public class StartupAttribute : Attribute
{

    public object[]? Parameters { get; }

    public StartupAttribute() : this(null) {}
    public StartupAttribute(params object[]? parameters) => Parameters = parameters;
}
#nullable disable