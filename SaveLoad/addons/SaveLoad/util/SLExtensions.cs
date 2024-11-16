using Newtonsoft.Json;
using System;
using System.Linq;

namespace SaveLoad;

/// <summary>
/// Contains utility extension functions.
/// </summary>
public static class SLExtensions
{

    public static T Converter<T>(this JsonSerializer serializer) where T : JsonConverter
	{
		return (T) serializer.Converters.FirstOrDefault(c => c is T);
	}
	
    public static IStringConverter GetConverter(this JsonSerializer serializer, Type type)
	{
		return (IStringConverter) serializer.Converters.FirstOrDefault(c => c is IStringConverter && c.CanConvert(type));
	}
}