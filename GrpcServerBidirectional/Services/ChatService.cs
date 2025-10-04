using AnonyFriday;
using Grpc.Core;
using System;
using System.Net;
using System.Text.Json;

namespace GrpcServerBidirectional.Services
{
    public class ChatService : AnonyFriday.ChatService.ChatServiceBase
    {
        private readonly ILogger<ChatService> _logger;
        private readonly ChatRoomManager _chatRoomManager;

        public ChatService(ILogger<ChatService> logger, ChatRoomManager chatRoomManager)
        {
            _logger = logger;
            _chatRoomManager = chatRoomManager;
        }

        public override async Task LiveChatting(IAsyncStreamReader<ChatMessage> requestStream, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
        {
            ChatMessage initialMessage = null;

            // 1. Non initial message received, close the connection
            if (!await requestStream.MoveNext() || (initialMessage = requestStream.Current) == null)
            {
                _logger.LogWarning("No initial message received. Closing connection.");
                return;
            }

            // 2. validate the room code and add the user to the chat room
            // - If invalid room code, message back to the client
            if (initialMessage.Type != ChatMessage.Types.MessageType.JoinSignal
                || !_chatRoomManager.TryAddUser(context.Peer, initialMessage.Username, initialMessage.Content, responseStream))
            {
                _logger.LogWarning($"Connection attempt from {context.Peer}: No initial message received. Closing stream.");
                return;
            }

            // 3. Send the preset.json file to the client upon joining
            var sendFileStream = Task.Run(() => SendInitialPresetFileToClientStreamAsync(responseStream));

            // 4. Listening for 1 user's incomming messages
            // - Only listening for the UserMessage type
            var readMessageStream = Task.Run(() => ReadMessagesStreamAsync(requestStream, context));

            // If the client stream close, then the connection is closed
            await Task.WhenAny(readMessageStream, readMessageStream);
            _logger.LogInformation($"Connection from {context.Peer} closed.");
        }


        public async Task ReadMessagesStreamAsync(IAsyncStreamReader<ChatMessage> requestStream, ServerCallContext context)
        {
            await foreach (var message in requestStream.ReadAllAsync())
            {
                if (message.Type == ChatMessage.Types.MessageType.UserMessage)
                {
                    _logger.LogInformation($"Received message from {message.Username} ({context.Peer}): {message.Content}");
                    _chatRoomManager.BroadcastUserMessage(message, context.Peer);
                }
            }
        }

        public async Task SendInitialPresetFileToClientStreamAsync(IAsyncStreamWriter<ChatMessage> responseStream)
        {
            while (true)
            {
                Console.WriteLine("""
                (1): Send stringified preset.json file to all clients
                (2): Send message to all clients
                """);

                string input = Console.ReadLine()?.Trim() ?? "";

                switch (input)
                {
                    // send stringtified json file to all clients
                    case "1":
                        var presetFilePath = Path.Combine(AppContext.BaseDirectory, "Files", "preset.json");
                        if (File.Exists(presetFilePath))
                        {
                            var presetContent = await File.ReadAllTextAsync(presetFilePath);

                            // stringrify the json file
                            var strintifiedJson = JsonSerializer.Serialize(presetContent, new JsonSerializerOptions { WriteIndented = false });

                            _chatRoomManager.BroadcastSystemMessage(strintifiedJson, ChatMessage.Types.MessageType.FileTransfer);
                            Console.WriteLine("preset.json file sent to clients.");
                        }
                        else
                        {
                            Console.WriteLine("preset.json file not found.");
                        }
                        break;
                    case "2":
                        {
                            Console.Write("Enter the system message to broadcast: ");
                            string systemMessage = Console.ReadLine()?.Trim() ?? "System message from server.";
                            _chatRoomManager.BroadcastSystemMessage(systemMessage, ChatMessage.Types.MessageType.ServerMessage);
                            Console.WriteLine("System message sent to clients.");
                            break;
                        }
                }
            }
        }
    }
}
