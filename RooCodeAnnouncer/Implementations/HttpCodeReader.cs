using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using RooCodeAnnouncer.Abstractions;
using RooCodeAnnouncer.Contracts;

namespace RooCodeAnnouncer.Implementations;

public partial class HttpCodeReader : ICodeReader
{
    private readonly HttpClient _httpClient;

    public HttpCodeReader(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(HttpCodeReader));
    }

    public async IAsyncEnumerable<ItemCode> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var resp = await _httpClient.GetAsync(string.Empty, cancellationToken);

        var rawHtml = await resp.Content.ReadAsStringAsync(cancellationToken);

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(rawHtml);

        // skip header row
        var rows = htmlDoc.DocumentNode.SelectNodes("//tr").Skip(1);

        foreach (var row in rows.Reverse())
        {
            var children = row.ChildNodes.ToImmutableArray();

            if (children.Length != 2)
            {
                continue;
            }

            var left = children[0];
            var right = children[1];

            var code = left.SelectSingleNode(".//strong").InnerText;
            var isNew = left.InnerHtml.Contains("(New Code)");
            var item = right.InnerText;

            var regex = ItemSplitterRegex();
            var normalizedItemTexts =
                regex.Split(item)
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

            yield return new ItemCode(code, HttpUtility.HtmlDecode(item), isNew, rewards.ToArray());
        }
    }

    [GeneratedRegex(@"([0-9,]+(?=\sx){0,1}\s)(?!\()")]
    private static partial Regex ItemSplitterRegex();
}