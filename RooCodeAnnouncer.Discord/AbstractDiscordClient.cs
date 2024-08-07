﻿using System.Diagnostics;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace RooCodeAnnouncer.Discord;

public abstract class DiscordClientAbstract : IAsyncDisposable
{
    public readonly DiscordClient Client;

    private readonly List<DiscordChannel> _channels;

    private readonly List<DiscordGuild> _guilds;

    private readonly List<DiscordMember> _members;

    public virtual IReadOnlyList<DiscordChannel> Channels => this._channels;

    public virtual IReadOnlyList<DiscordGuild> Guilds => this._guilds;

    public virtual IReadOnlyList<DiscordMember> Members => this._members;

    protected DiscordClientAbstract(string token)
    {
        this._channels = new List<DiscordChannel>();
        this._guilds = new List<DiscordGuild>();
        this._members = new List<DiscordMember>();

        this.Client = new DiscordClient(new DiscordConfiguration
        {
            Token = token,
            TokenType = TokenType.Bot,
            AutoReconnect = true,
            Intents = DiscordIntents.All
        });

        this.Client.GuildDownloadCompleted += DiscordClient_GuildDownloadCompleted;
        this.Client.GuildCreated += DiscordClient_GuildCreatedCompleted;
        this.Client.GuildDeleted += DiscordClient_GuildDeletedCompleted;
        this.Client.ChannelCreated += DiscordClient_ChannelCreated;
        this.Client.ChannelDeleted += DiscordClient_ChannelDeleted;
        this.Client.GuildMemberAdded += Client_GuildMemberAdded;
        this.Client.GuildMemberRemoved += Client_GuildMemberRemoved;
    }

    private Task Client_GuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs e)
    {
        lock (this._members)
        {
            this._members.Remove(e.Member);

            return Task.CompletedTask;
        }
    }

    private Task Client_GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
    {
        lock (this._members)
        {
            this._members.Add(e.Member);

            return Task.CompletedTask;
        }
    }

    public Task ConnectAsync(CancellationToken cancellation = default)
    {
        return this.Client.ConnectAsync();
    }

    protected virtual Task DiscordClient_ChannelDeleted(DiscordClient sender, ChannelDeleteEventArgs e)
    {
        lock (this._channels)
        {
            var channel = e.Channel;

            var existingChannel = this._channels.SingleOrDefault(x => x.Id == channel.Id);

            if (existingChannel != null)
            {
                this._channels.Remove(existingChannel);
            }

            return Task.CompletedTask;
        }
    }

    protected virtual Task DiscordClient_ChannelCreated(DiscordClient sender, ChannelCreateEventArgs e)
    {
        lock (this._channels)
        {
            var channel = e.Channel;

            if (channel.Type == ChannelType.Text)
            {
                this._channels.Add(channel);
            }

            return Task.CompletedTask;
        }
    }

    protected virtual Task DiscordClient_GuildDeletedCompleted(DiscordClient sender, GuildDeleteEventArgs e)
    {
        var guild = e.Guild;

        lock (this._channels)
        {
            this.Perform(() => this._channels.RemoveAll(x => x.GuildId == guild.Id));
        }

        lock (this._members)
        {
            this.Perform(() => this._members.RemoveAll(x => x.Guild.Id == guild.Id));
        }

        lock (this._guilds)
        {
            this.Perform(() => this._guilds.Remove(guild));
        }

        return Task.CompletedTask;
    }

    protected virtual Task DiscordClient_GuildCreatedCompleted(DiscordClient sender, GuildCreateEventArgs e)
    {
        var guild = e.Guild;

        var channels = guild.Channels.Select(x => x.Value);

        lock (this._channels)
        {
            this.Perform(() => this._channels.AddRange(channels));
        }

        lock (this._members)
        {
            this.Perform(() => this._members.AddRange(guild.Members.Select(x => x.Value)));
        }

        lock (this._guilds)
        {
            this.Perform(() => this._guilds.Add(guild));
        }

        return Task.CompletedTask;
    }

    protected virtual Task DiscordClient_GuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs e)
    {
        var guilds = e.Guilds.Select(x => x.Value);

        foreach (var guild in guilds)
        {
            var channels = guild.Channels.Select(x => x.Value);

            lock (this._channels)
            {
                this.Perform(() => this._channels.AddRange(channels));
            }

            lock (this._members)
            {
                this.Perform(() => this._members.AddRange(guild.Members.Select(x => x.Value)));
            }

            lock (this._guilds)
            {
                this.Perform(() => this._guilds.Add(guild));
            }
        }

        return Task.CompletedTask;
    }

    public IEnumerable<DiscordChannel> GetDiscordChannels()
    {
        lock (this._channels)
        {
            foreach (var ch in this._channels)
            {
                yield return ch;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await this.Client.DisconnectAsync();
    }

    private void Perform(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }
}