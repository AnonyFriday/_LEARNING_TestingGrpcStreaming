using AnonyFriday;
using Grpc.Core;

namespace GrpcServerBidirectional.Services
{
    internal record UserSession
    {
        public string Username { get; init; }
        public string IpAddress { get; init; }
        public string RoomCode { get; init; }

        // a stream to send messages to the user
        // when client connect to the server, a stream will be created, and destroy after finishing sending a message
        public IServerStreamWriter<ChatMessage> ResponseStream { get; init; }
    }

    public class ChatRoomManager
    {
        private readonly List<UserSession> _userSessions = new();
        private static string ROOM_PASSCODE = "123456";
        private readonly object _tempKey = new();

        public bool IsCodeValid(string roomCode) => roomCode == ROOM_PASSCODE;

        public bool TryAddUser(string ipAddress, string userName, string roomCode, IServerStreamWriter<ChatMessage> responseStream)
        {
            // If not valid room code, message back to the client
            if (!IsCodeValid(roomCode))
            {
                return false;
            }

            var sessionUser = new UserSession
            {
                IpAddress = ipAddress,
                Username = userName,
                RoomCode = roomCode,
                ResponseStream = responseStream
            };

            // only one thread can access this block at a time
            // If locking _userSessions directly, it may cause deadlock due toe multiple session try to access 
            // _userSessions at the same time such as debugging, or broadcasting,...

            lock (_tempKey)
            {
                _userSessions.Add(sessionUser);
            }

            // Annount the join to other users
            BroadcastSystemMessage($"{userName} has joined the chat room.", ChatMessage.Types.MessageType.ServerMessage);

            return true;
        }

        public void BroadcastUserMessage(ChatMessage message, string? notSentToIpAddress)
        {
            // Copy on Read
            // Copy on Write
            // becaus the _uesrSession is the shared, mutable resources
            List<UserSession> _userSessionsCopy;

            lock (_tempKey)
            {
                _userSessionsCopy = _userSessions.ToList();
            }

            foreach (var userSession in _userSessionsCopy)
            {
                // do not send the message back to the sender
                if (notSentToIpAddress != null && userSession.IpAddress == notSentToIpAddress) continue;

                ThreadPool.QueueUserWorkItem(async _ =>
                {
                    try
                    {
                        await userSession.ResponseStream.WriteAsync(message);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Cannot broadcasting. Please try again");
                    }
                });
            }
        }

        public void BroadcastSystemMessage(string v, ChatMessage.Types.MessageType messageType)
        {
            var systemMessage = new ChatMessage
            {
                Type = messageType,
                Username = "***System***",
                Content = v,
                Timestamp = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
            };

            BroadcastUserMessage(systemMessage, null);
        }
    }
}
