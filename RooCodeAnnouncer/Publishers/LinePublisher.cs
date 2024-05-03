using MediatR;
using Microsoft.Extensions.Configuration;
using RooCodeAnnouncer.Configurations;
using RooCodeAnnouncer.Contracts.Events;

namespace RooCodeAnnouncer.Publishers;

public class LinePublisher :
    INotificationHandler<NewCodeNotification>,
    INotificationHandler<ZenyShopSnapTimeNotification>,
    INotificationHandler<EndOfWeekNotification>,
    INotificationHandler<EndOfMonthNotification>
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

    public Task Handle(NewCodeNotification notification, CancellationToken cancellationToken)
    {
        var itemText = string.Join('\n', notification.Items.Select(r => $"{r.Name} x {r.Quantity:N0}"));
        var message = $"\n{notification.Code}\n\nItems:\n{itemText}";

        return this.HandleImpl(message, cancellationToken);
    }

    public Task Handle(ZenyShopSnapTimeNotification notification, CancellationToken cancellationToken)
    {
        const string message = "ได้เวลาดึงการ์ดแล้ว~";

        return this.HandleImpl(message, cancellationToken);
    }

    public Task Handle(EndOfWeekNotification notification, CancellationToken cancellationToken)
    {
        const string message = "จะหมดสัปดาห์แล้ว อย่าลืมเช็คของในร้านค้ารายสัปดาห์ด้วยนะ~";

        return this.HandleImpl(message, cancellationToken);
    }

    public Task Handle(EndOfMonthNotification notification, CancellationToken cancellationToken)
    {
        const string message = "จะหมดเดือนแล้ว อย่าลืมเช็คของในร้านค้ารายเดือนด้วยนะ~";

        return this.HandleImpl(message, cancellationToken);
    }

    private async Task HandleImpl(string message, CancellationToken cancellationToken)
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

            using var formData = new MultipartFormDataContent();
            using var stringContent = new StringContent(message);
            formData.Add(stringContent, "message");

            await _httpClient.PostAsync(string.Empty, formData, cancellationToken);
        }
    }
}