using System.Text.RegularExpressions;

namespace RooCodeAnnouncer.Utils;

public static partial class RegexUtils
{
    [GeneratedRegex(@"([0-9,]+(?=\sx){0,1}\s)(?!\()")]
    public static partial Regex ItemSplitterRegex();
}