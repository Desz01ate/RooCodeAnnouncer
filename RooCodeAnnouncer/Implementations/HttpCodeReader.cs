using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Web;
using HtmlAgilityPack;
using RooCodeAnnouncer.Abstractions;
using RooCodeAnnouncer.Models;

namespace RooCodeAnnouncer.Implementations;

public class HttpCodeReader : ICodeReader
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

            var code = left.SelectSingleNode(".//strong").InnerText;
            var isNew = left.SelectSingleNode(".//em")?.InnerText == "(New Code)";
            var item = right.InnerText;

            yield return new ItemCode(code, HttpUtility.HtmlDecode(item), isNew);
        }
    }
}