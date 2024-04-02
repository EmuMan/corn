using CornBot.Utilities;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using CornBot.Models;
using System.Diagnostics;
using CornBot.Models.Requests;
using CornBot.Models.Responses;

namespace CornBot.Handlers
{
    public class MessageHandler
    {

        private readonly IServiceProvider _services;
        private readonly DiscordSocketClient _client;

        private readonly WordDetector _cornDetector;
        private readonly WordDetector _prideDetector;

        public MessageHandler(DiscordSocketClient client, IServiceProvider services)
        {
            _client = client;
            _services = services;
            _cornDetector = new WordDetector("corn", Constants.CORN_EMOJI);
            _prideDetector = new WordDetector("pride", Constants.RAINBOW_EMOJI);
        }

        public Task Initialize()
        {
            _client.MessageReceived += MessageReceivedAsync;
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(SocketMessage messageParam)
        {
            if (messageParam is not SocketUserMessage message) return;

            if (message.Author.IsBot) return;

            if (message.Channel is not SocketGuildChannel channel) return;

            var content = message.Content;
            var api = _services.GetRequiredService<CornAPI>();

            WordDetector.DetectionLevel result = _cornDetector.Parse(content);
            WordDetector.DetectionLevel prideResult = _prideDetector.Parse(content);
            var isPride = prideResult == WordDetector.DetectionLevel.FULL &&
                        Events.GetCurrentEvent() == Constants.CornEvent.PRIDE;

            // this is a little stupid but necessary i guess
            if (message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id) ||
                result == WordDetector.DetectionLevel.FULL)
            {
                if (_services.GetRequiredService<Random>().Next(0, Constants.ANGRY_CHANCE) == 0 && !isPride)
                {
                    try { await message.Channel.SendMessageAsync(Constants.CORN_ANGRY_DIALOGUE); }
                    catch (HttpException) { }
                    await api.PostModelAsync<MessageRequest, MessageResponse>($"/message/{channel.Guild.Id}/{message.Author.Id}/add",
                        new MessageRequest(-1000));
                }
                else
                {
                    var response = isPride ?
                        Constants.CORN_PRIDE_DIALOGUE_COMBINED : Constants.CORN_NICE_DIALOGUE;
                    try { await message.Channel.SendMessageAsync(response); }
                    catch (HttpException) { }
                    await api.PostModelAsync<MessageRequest, MessageResponse>($"/message/{channel.Guild.Id}/{message.Author.Id}/add",
                        new MessageRequest(5));
                }
            }
            else if (result == WordDetector.DetectionLevel.PARTIAL)
            {
                try 
                {
                    if (Events.GetCurrentEvent() == Constants.CornEvent.PRIDE &&
                        Emote.TryParse(Constants.PRIDE_CORN_EMOJI, out var emote))
                    {
                        await message.AddReactionAsync(emote);
                    }
                    else
                    {
                        await message.AddReactionAsync(new Emoji(Constants.CORN_EMOJI));
                    }
                }
                catch (HttpException) { }
                await api.PostModelAsync<MessageRequest, MessageResponse>($"/message/{channel.Guild.Id}/{message.Author.Id}/add",
                    new MessageRequest(1));
            }
            else if (isPride)
            {
                await message.Channel.SendMessageAsync(Constants.CORN_PRIDE_DIALOGUE);
            }
        }

    }
}
