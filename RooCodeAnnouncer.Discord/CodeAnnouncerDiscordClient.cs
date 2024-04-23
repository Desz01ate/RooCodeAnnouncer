using System.Collections.Immutable;
using System.Text;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;

namespace RooCodeAnnouncer.Discord;

public class CodeAnnouncerDiscordClient : DiscordClientAbstract
{
    public CodeAnnouncerDiscordClient(string token, IServiceProvider serviceProvider)
        : base(token)
    {
        var commands = this.Client.UseSlashCommands(new SlashCommandsConfiguration
        {
            Services = serviceProvider,
        });

        commands.RegisterCommands<DiscordCommandModule>();
    }
}

public class DiscordCommandModule : ApplicationCommandModule
{
    private readonly HttpClient httpClient;

    public DiscordCommandModule(IHttpClientFactory httpClientFactory)
    {
        this.httpClient = httpClientFactory.CreateClient();
    }

    [SlashCommand("complete", "Complete the ROO's survey.")]
    public async Task CompleteSurveyAsync(InteractionContext context, [Option("url", "The survey url")] string url)
    {
        try
        {
            var uri = new Uri(url);

            var formKey = uri.AbsolutePath.Split('/').Last();
            var parameters =
                uri.Query
                    .TrimStart('?')
                    .Split('&')
                    .Select(s =>
                    {
                        var split = s.Split('=');

                        return new KeyValuePair<string, string>(split[0], split[1]);
                    })
                    .ToImmutableDictionary();

            var surveySubmitUrl =
                new Uri(
                    $"https://survey.roglobal.com/tduck-api/user/form/data/public/create?{uri.Query}");

            var payload = new
            {
                completeTime = "253053",
                formKey = formKey,
                submitOs = "Windows",
                submitBrowser = "Edge",
                submitUa = new
                {
                    ua =
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0",
                    browser = new
                    {
                        name = "Edge",
                        version = "120.0.0.0",
                        major = "120"
                    },
                    engine = new
                    {
                        name = "Blink",
                        version = "120.0.0.0"
                    },
                    os = new
                    {
                        name = "Windows",
                        version = "10"
                    },
                    device = new { },
                    cpu = new
                    {
                        architecture = "amd64"
                    }
                },
                wxUserInfo = new { },
                originalData = new { },
                extValue = parameters["ext"],
                formType = 1
            };

            using var body = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            using var resp = await this.httpClient.PostAsync(surveySubmitUrl, body);

            var respJson = await resp.Content.ReadAsStringAsync();
            var respDict = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(respJson);

            var success = respDict["code"] == 200;

            await context.CreateResponseAsync(success ? "Done!" : $"Fail with {respDict["msg"]}", true);
        }
        catch (Exception e)
        {
            await context.CreateResponseAsync("Sorry but I am unable to complete the survey for you :(", true);
        }
    }
}