using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using RooCodeAnnouncer.Abstractions;
using RooCodeAnnouncer.Contracts;
using RooCodeAnnouncer.Utils;

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

        foreach (var row in rows)
        {
            var children = row.ChildNodes.ToImmutableArray();

            if (children.Length != 2)
            {
                continue;
            }

            var left = children[0];
            var right = children[1];

            var node = left.FirstChild;
            var sb = new StringBuilder(node.InnerHtml);
            while (node.NextSibling is not null)
            {
                node = node.NextSibling;
                var text = node.InnerText;

                if (text.Contains("new code", StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                sb.AppendLine(node.InnerText);
            }

            var code = sb.ToString();
            var isNew = left.InnerHtml.Contains("(New Code)");
            var item = right.InnerText;

            var rewards = ItemCodeUtils.Parse(item);

            yield return new ItemCode(code, HttpUtility.HtmlDecode(item), isNew, rewards.ToArray());
        }
    }
}