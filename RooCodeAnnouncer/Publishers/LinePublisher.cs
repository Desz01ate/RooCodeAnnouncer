using MediatR;
using Microsoft.Extensions.Configuration;
using RooCodeAnnouncer.Configurations;
using RooCodeAnnouncer.Events;

namespace RooCodeAnnouncer.Publishers;

public class LinePublisher : INotificationHandler<NewCodeNotification>
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public LinePublisher(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(LinePublisher));
        _configuration = configuration;
    }

    public async Task Handle(NewCodeNotification notification, CancellationToken cancellationToken)
    {
        var tokens =
            _configuration
                .GetSection("External:Line")
                .Get<List<LineConfiguration>>()!
                .Select(c => c.NotificationToken);

        foreach (var token in tokens)
        {
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var itemText = string.Join('\n', notification.Items.Select(r => $"{r.Name} x {r.Quantity:N0}"));

            using var formData = new MultipartFormDataContent();
            using var stringContent = new StringContent($"\n{notification.Code}\n\nItems:\n{itemText}");
            formData.Add(stringContent, "message");

            var res = await _httpClient.PostAsync(string.Empty, formData, cancellationToken);

            res.EnsureSuccessStatusCode();
        }
    }
}