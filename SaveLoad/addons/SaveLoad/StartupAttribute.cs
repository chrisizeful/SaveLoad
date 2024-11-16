using System;

namespace SaveLoad;

/// <summary>
/// A method attribute that allows a mod to specify its entry point.
/// </summary>
#nullable enable
[AttributeUsage(AttributeTargets.Method)]
public class StartupAttribute : Attribute
{

    public object[]? Parameters { get; }

    public StartupAttribute() : this(null) {}
    public StartupAttribute(params object[]? parameters) => Parameters = parameters;
}
