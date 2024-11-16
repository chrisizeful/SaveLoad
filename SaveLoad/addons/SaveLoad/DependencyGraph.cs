using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace SaveLoad;

public class DependencyGraph<T>
{

    public Dictionary<string, List<string>> Dependencies { get; private set; } = new();
    public Dictionary<string, T> Data { get; private set; } = new();

    public void AddNode(string node, T data)
    {
        Dependencies[node] = new();
        Data[node] = data;
    }

    public void AddDependency(string node, string dependency) => Dependencies[node].Add(dependency);

    public List<string> NameOrder()
    {
        // Topologically sort defs and their dependencies
        List<string> order = new();
        HashSet<string> visited = new();
        HashSet<string> waiting = new();
        foreach (var item in Dependencies.Keys)
            if (!visited.Contains(item))
                if (!TopologicalSort(item, visited, waiting, order))
                    throw new InvalidOperationException($"Circular dependency detected for \"{item}\"!");
        return order;
    }

    public List<T> DataOrder()
    {
        List<string> named = NameOrder();
        List<T> ordered = new();
        foreach (string name in named)
            ordered.Add(Data[name]);
        return ordered;
    }

    private bool TopologicalSort(string item, HashSet<string> visited, HashSet<string> waiting, List<string> order)
    {
        if (waiting.Contains(item))
            return false; // Circular dependency detected
        if (visited.Contains(item))
            return true; // Already visited
        waiting.Add(item);
        foreach (var dependency in Dependencies[item])
            if (!TopologicalSort(dependency, visited, waiting, order))
                return false; // Circular dependency detected
        waiting.Remove(item);
        visited.Add(item);
        order.Add(item);
        return true;
    }

    // Check for circular dependencies, returning item (if any) that caused it
    public string HasCircularDependencies()
    {
        HashSet<string> visited = new HashSet<string>();
        HashSet<string> inProgress = new HashSet<string>();
        foreach (var item in Dependencies.Keys)
            if (!visited.Contains(item))
                if (HasCycle(item, visited, inProgress) != null)
                    return item;
        return null;
    }

    private string HasCycle(string item, HashSet<string> visited, HashSet<string> inProgress)
    {
        if (inProgress.Contains(item))
            return item;
        if (visited.Contains(item))
            return null;
        visited.Add(item);
        inProgress.Add(item);
        foreach (var dependency in Dependencies[item])
            if (HasCycle(dependency, visited, inProgress) != null)
                return item;
        inProgress.Remove(item);
        return null;
    }
}

public class JObjectGraph<T> : DependencyGraph<T>
{

    public void CheckProperties(JToken token, Action<JProperty> action)
    {
        if (token.Type == JTokenType.Object)
        {
            foreach (JProperty property in ((JObject) token).Properties())
            {
                action.Invoke(property);
                CheckProperties(property.Value, action);
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (JToken item in token)
                CheckProperties(item, action);
        }
    }
}