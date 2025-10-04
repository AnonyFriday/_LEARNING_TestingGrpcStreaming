using Grpc.Core;

namespace GrpcServerClientStreaming.Services
{
    public class CalculatorService : Calculator.CalculatorBase
    {
        public override async Task<AddResponse> AddStream(IAsyncStreamReader<AddRequest> requestStream, ServerCallContext context)
        {
            uint sum = 0;

            // when the stream ends, (client calls CompleteAsync) the MoveNext() will return false
            while (await requestStream.MoveNext())
            {
                sum += requestStream.Current.Number;
                Console.WriteLine($"Received number: {requestStream.Current.Number}, current sum: {sum}");
            }

            // return the final sum
            Console.WriteLine($"Final sum: {sum}, Received from Client: {context.Peer}");
            return new AddResponse { Result = sum };
        }
    }
}
