namespace CopyExtension.UI;

public interface IOptionGui
{
    T? GetChoiceResult<T>(string text, string details, Dictionary<string, T> Options);
}
