//Client Side Request			
//1. Input	Client Application	Creates a PriceInput object with data (e.g., id = 101, qty = 5).	Application Layer
//2. Call	Client Stub	App calls client.CalculatePrice(input). The stub is executed.	Stub Layer
//3. Encode	Protobuf	The stub uses Protobuf to serialize the PriceInput object into binary bytes.	Stub Layer
//4. Transport	gRPC Runtime / Channel	The bytes are framed, assigned a Stream ID, and sent over the HTTP/2 Stream to the server.	Transport Layer
//Server Side Processing			
//5. Decode	Protobuf	The server's runtime receives the bytes, and Protobuf deserializes them back into a PriceInput object.	Stub Layer
//6. Execute	Server Base Class Implementation	The server's logic runs, calculates the total, and creates a PriceOutput object.	Application Layer
//Server Side Response			
//7. Encode	Protobuf	The server uses Protobuf to serialize the PriceOutput object into binary bytes.	Stub Layer
//8. Transport	gRPC Runtime / Channel	The response bytes are sent back over the same Stream to the client.	Transport Layer
//9. Output	Client Stub	The client stub deserializes the bytes back into a native PriceOutput object and returns it to the client application.	Stub Layer

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddGrpcClient<Calculator.CalculatorClient>(o =>
{
    o.Address = new Uri("http://localhost:8000"); // gRPC server address
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var calculatorClient = services.GetRequiredService<Calculator.CalculatorClient>();
    var response = await calculatorClient.AddAsync(
        new AddRequest()
        {
            List = { 1, 2, 3, 4, 5 },
            Count = 5
        });

    Console.WriteLine("Response from gRPC server: " + response.Result);
}

app.MapControllers();

app.Run();
