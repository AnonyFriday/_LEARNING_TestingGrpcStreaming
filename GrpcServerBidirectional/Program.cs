using GrpcServerBidirectional.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddGrpc();
builder.Services.AddSingleton<ChatRoomManager>();

var app = builder.Build();
app.MapGrpcService<ChatService>();

// Configure the HTTP request pipeline.

app.Run();
