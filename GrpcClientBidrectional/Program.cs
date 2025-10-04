// 1. Get credentials once
using AnonyFriday;
using Grpc.Core;
using Grpc.Net.Client;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

internal class Program
{
    private static string userName = "Anonymous";
    private static readonly string exitCommand = "exit";

    private static async Task Main(string[] args)
    {
        Console.Write("Enter Username: ");
        userName = Console.ReadLine()?.Trim() ?? "Anonymous";

        Console.Write("Enter Room Code (e.g., 123456): ");
        string roomCode = Console.ReadLine()?.Trim() ?? "";

        while (string.IsNullOrWhiteSpace(roomCode) || string.IsNullOrWhiteSpace(userName))
        {
            Console.Write("Room Code cannot be empty. Please enter Room Code: ");
            roomCode = Console.ReadLine()?.Trim() ?? "";
        }

        // 2. Setup channel and client
        var channel = GrpcChannel.ForAddress("http://192.168.137.1:6000");
        var client = new ChatService.ChatServiceClient(channel);
        using var call = client.LiveChatting();

        // 3. Send the initial join message
        ChatMessage initialMessage = new()
        {
            Type = ChatMessage.Types.MessageType.JoinSignal,
            Content = roomCode,
            Username = userName,
            Timestamp = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
        };
        await call.RequestStream.WriteAsync(initialMessage);

        // 3. Start the bidirectional streaming call
        Task writeMessagesAsync = Task.Run(() => WriteMessagesStreamAsync(call, userName));
        Task readMessagesAsync = Task.Run(() => ReadMessagesStreamAsync(call));

        // When any task completes, let the main thread run
        await Task.WhenAny(readMessagesAsync, writeMessagesAsync);
        Console.WriteLine("Chat session ended.");
    }

    private static async Task ReadMessagesStreamAsync(AsyncDuplexStreamingCall<ChatMessage, ChatMessage> call)
    {
        await foreach (var message in call.ResponseStream.ReadAllAsync())
        {
            // If message type is FILE_TRANSFER, deserialize the JSON content
            if (message.Type == ChatMessage.Types.MessageType.FileTransfer)
            {
                var deserializedContent = System.Text.Json.JsonSerializer.Deserialize<string>(message.Content);
                Console.WriteLine(deserializedContent);
                continue;
            }

            Console.WriteLine($"\n---------- [{message.Timestamp.ToDateTime():HH:mm:ss}] {message.Username}: {message.Content}");
        }
    }

    private static async Task WriteMessagesStreamAsync(AsyncDuplexStreamingCall<ChatMessage, ChatMessage> call, string userName)
    {
        while (true)
        {
            Console.Write("""
                (1): Bluk sending like realtime messages
                (2): Send single message
                (exit): Exit chat
                """);

            string userInput = Console.ReadLine()?.Trim();

            switch (userInput)
            {
                case "1":
                    await SendBulkRealtimeMessageAsync(call, userName);
                    continue;
                case "2":
                    await SendSingleMessageAsync(call, userName);
                    continue;
                case $"exit":
                    await call.RequestStream.CompleteAsync();
                    return;
                default:
                    Console.WriteLine("Invalid input. Please enter '1', '2', or 'exit'.");
                    continue;
            }
        }
    }

    private static async Task SendBulkRealtimeMessageAsync(AsyncDuplexStreamingCall<ChatMessage, ChatMessage> call, string userName)
    {
        Random rand = new();
        int messageCount = 50;
        for (int i = 0; i < messageCount; i++)
        {
            var chatMessage = new ChatMessage
            {
                Type = ChatMessage.Types.MessageType.UserMessage,
                Username = userName,
                Content = $"Message {i + 1}",
                Timestamp = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
            };
            // Simulate random delay between messages
            await Task.Delay(rand.Next(50, 200));

            // Send the message
            await call.RequestStream.WriteAsync(chatMessage);
        }
    }


    private static async Task SendSingleMessageAsync(AsyncDuplexStreamingCall<ChatMessage, ChatMessage> call, string userName)
    {
        Console.Write("Enter your message: ");
        string userInput = Console.ReadLine()?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(userInput))
        {
            var chatMessage = new ChatMessage
            {
                Type = ChatMessage.Types.MessageType.UserMessage,
                Username = userName,
                Content = userInput,
                Timestamp = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
            };
            await call.RequestStream.WriteAsync(chatMessage);
        }
    }
}