﻿namespace Khnumdev.TwitBot
{
    using Data.Model;
    using Data.Repositories;
    using Microsoft.Bot.Connector;
    using Services;
    using System;
    using System.Threading.Tasks;
    using System.Web.Http;

    [BotAuthentication]
    public class MessagesController : ApiController
    {
        readonly IMessageMatcherProcessor _messageMatcherProcessor;
        readonly IChatRepository _chatRepository;

        public MessagesController(IMessageMatcherProcessor messageMatcherProcesor,
            IChatRepository chatRepository)
        {
            _messageMatcherProcessor = messageMatcherProcesor;
            _chatRepository = chatRepository;
        }

        [HttpPost]
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<Message> Post([FromBody]Message message)
        {
            var chat = new QueueChat
            {
                RequestTime = DateTime.UtcNow,
                From = message.From.Name,
                IsRequestFromBot = message.From.IsBot,
                RequestAddress = message.From.Address,
                Request = message.Text,
                To = message.To.Name,
                MessageType = message.Type,
                SourceLanguage = message.SourceLanguage,
                DestinationLanguage = message.Language,
            };

            Message response = null;

            try
            {
                if (message.Type == "Message")
                {
                    var selectedResponse = await _messageMatcherProcessor.ProcessAsync(message.Text);

                    chat.Response = selectedResponse;
                    chat.ResponseTime = DateTime.UtcNow;

                    // return our reply to the user
                    response = message.CreateReplyMessage(selectedResponse);
                }
                else
                {
                    response = HandleSystemMessage(message);
                }
            }
            catch (Exception ex)
            {
                chat.Error = ex.ToString();
                response = message.CreateReplyMessage("ough!");
            }
            finally
            {
                await _chatRepository.EnqeueChatAsync(chat);
            }

            return response;
        }

        private Message HandleSystemMessage(Message message)
        {
            if (message.Type == "Ping")
            {
                Message reply = message.CreateReplyMessage();
                reply.Type = "Ping";
                return reply;
            }
            else if (message.Type == "DeleteUserData")
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == "BotAddedToConversation")
            {
            }
            else if (message.Type == "BotRemovedFromConversation")
            {
            }
            else if (message.Type == "UserAddedToConversation")
            {
            }
            else if (message.Type == "UserRemovedFromConversation")
            {
            }
            else if (message.Type == "EndOfConversation")
            {
            }

            return null;
        }
    }
}