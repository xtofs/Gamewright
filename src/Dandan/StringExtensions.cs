namespace Dandan;

static class StringExtensions
{
    public static string ToPascalCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        var words = str.Split(['-', '_', ' '], StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < words.Length; i++)
        {
            words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
        }
        return string.Join(string.Empty, words);
    }
}
