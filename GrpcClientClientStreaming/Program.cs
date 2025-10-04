
var channel = Grpc.Net.Client.GrpcChannel.ForAddress("http://localhost:5286");
var client = new Calculator.CalculatorClient(channel);

bool continueAdding = true;

while (continueAdding)
{
    using (var call = client.AddStream())
    {
        for (int i = 1; i <= new Random().Next(100); i++)
        {
            // return a task that only completes when the data has been writtten to
            // the local buffer, and the stream is ready for next chunk
            // so that;s why must use await here
            await call.RequestStream.WriteAsync(new AddRequest { Number = (uint)i });

            await Task.Delay(60);

            Console.WriteLine($"Sent number: {i}");
        }

        // Testing if the stream is not close but can send more numbers
        //Console.WriteLine("Do you want to sent an individual number? (0: No, 1: Yes)");
        //var moreNumber = Console.ReadLine();
        //if (moreNumber == "1")
        //{
        //    Console.WriteLine("Enter a number to send:");
        //    int.TryParse(Console.ReadLine(), out int newNumber);
        //    await call.RequestStream.WriteAsync(new AddRequest { Number = (uint)newNumber });
        //    Console.WriteLine($"Sent number: {newNumber}");
        //}

        // Complete the request stream to indicate that no more messages will be sent
        // Once u completed, the stream cannot be reuse
        await call.RequestStream.CompleteAsync();

        var response = await call.ResponseAsync;
        Console.WriteLine($"Final sum received from server: {response.Result}");
    }

    Console.WriteLine("Do you want to add more data for computation?.(0: No, 1: Yes)");
    var answer = Console.ReadLine();
    continueAdding = answer == "1";
}



