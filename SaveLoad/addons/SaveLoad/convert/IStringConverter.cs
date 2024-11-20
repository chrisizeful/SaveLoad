namespace SaveLoad;

/// <summary>
/// Implemented by some JsonConverters that allow specifying the object as a single
/// line string (i.e. <see cref="ColorConverter"/> or <see cref="Vector2Converter"/>).
/// </summary>
public interface IStringConverter
{

    public object Convert(string input);
}