using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace SaveLoad;

/// <summary>
/// A topological graph used to enforce mod dependencies, and detect mod/def
/// cyclic references.
/// </summary>
/// <typeparam name="T">The node Type in the graph.</typeparam>
public class DependencyGraph<T>
{

    Dictionary<string, List<string>> dependencies = new();
    /// <summary>
    /// A dictionary containing the set dependencies for every node.
    /// </summary>
    public IReadOnlyDictionary<string, List<string>> Dependencies => dependencies;

    Dictionary<string, T> data = new();
    /// <summary>
    /// A dictionary mapping names to nodes.
    /// </summary>
    public IReadOnlyDictionary<string, T> Data => data;

    /// <summary>
    /// Add a node with a name.
    /// </summary>
    /// <param name="node">The name of the node.</param>
    /// <param name="data">The data of the node.</param>
    public void AddNode(string node, T data)
    {
        dependencies[node] = new();
        this.data[node] = data;
    }

    /// <summary>
    /// Adds a dependency for a node. Meaning, dependency must be loaded before node.
    /// </summary>
    /// <param name="node">The name of the node.</param>
    /// <param name="dependency">The name of the dependency node.</param>
    public void AddDependency(string node, string dependency) => dependencies[node].Add(dependency);

    /// <summary>
    /// Returns the order names of nodes should be loaded in order to accomodate their dependencies.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public List<string> NameOrder()
    {
        // Topologically sort defs and their dependencies
        List<string> order = new();
        HashSet<string> visited = new();
        HashSet<string> waiting = new();
        foreach (var item in dependencies.Keys)
            if (!visited.Contains(item))
                if (!TopologicalSort(item, visited, waiting, order))
                    throw new InvalidOperationException($"Circular dependency detected for \"{item}\"!");
        return order;
    }

    /// <summary>
    /// Returns the order nodes should be loaded in order to accomodate their dependencies.
    /// </summary>
    /// <returns></returns>
    public List<T> DataOrder()
    {
        List<string> named = NameOrder();
        List<T> ordered = new();
        foreach (string name in named)
            ordered.Add(data[name]);
        return ordered;
    }

    private bool TopologicalSort(string item, HashSet<string> visited, HashSet<string> waiting, List<string> order)
    {
        if (waiting.Contains(item))
            return false; // Circular dependency detected
        if (visited.Contains(item))
            return true; // Already visited
        waiting.Add(item);
        foreach (var dependency in dependencies[item])
            if (!TopologicalSort(dependency, visited, waiting, order))
                return false; // Circular dependency detected
        waiting.Remove(item);
        visited.Add(item);
        order.Add(item);
        return true;
    }

    /// <summary>
    /// Checks for circular dependencies, returning the first item (if any) that caused the issue.
    /// </summary>
    /// <returns>The name of the item that caused the issue.</returns>
    public string HasCircularDependencies()
    {
        HashSet<string> visited = new();
        HashSet<string> inProgress = new();
        foreach (var item in dependencies.Keys)
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
        foreach (var dependency in dependencies[item])
            if (HasCycle(dependency, visited, inProgress) != null)
                return item;
        inProgress.Remove(item);
        return null;
    }
}

/// <summary>
/// A DependencyGraph that checks the properties of a <see cref="JToken"/> to determine dependencies. 
/// </summary>
/// <typeparam name="T"></typeparam>
public class JObjectGraph<T> : DependencyGraph<T>
{

    /// <summary>
    /// Invokes an action for every <see cref="JProperty"/> present in a <see cref="JToken"/>.
    /// The action is be responsible for setting up dependencies in the graph via
    /// DependencyGraph.AddDependency.
    /// </summary>
    /// <param name="token">The token to iterate.</param>
    /// <param name="action">The action to invoke.</param>
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