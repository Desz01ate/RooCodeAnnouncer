using System.Collections.Immutable;
using System.Web;
using RooCodeAnnouncer.Contracts;

namespace RooCodeAnnouncer.Utils;

public static class ItemCodeUtils
{
    public static IEnumerable<Reward> Parse(string text)
    {
        var regex = RegexUtils.ItemSplitterRegex();
        var normalizedItemTexts =
            regex.Split(text)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Replace("x ", string.Empty))
                .Select(s => s.Trim())
                .ToImmutableArray();
        var names =
            normalizedItemTexts.Where((_, i) => i % 2 != 0)
                .Select(HttpUtility.HtmlDecode);
        var quantities =
            normalizedItemTexts.Where((_, i) => i % 2 == 0)
                .Select(s => s.Replace(",", string.Empty));
        var rewards = names.Zip(quantities)
            .Select(pair =>
                new Reward(pair.First, int.TryParse(pair.Second, out var num) ? num : -1));

        return rewards;
    }
}