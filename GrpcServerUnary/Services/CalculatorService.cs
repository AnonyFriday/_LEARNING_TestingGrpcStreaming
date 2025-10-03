using Grpc.Core;

namespace GrpcServerUnary.Services
{
    public class CalculatorService : Calculator.CalculatorBase
    {
        public override Task<AddResponse> Add(AddRequest request, ServerCallContext context)
        {
            Console.WriteLine("Received Add request from client: " + context.Peer);

            uint result = 0;
            foreach (uint num in request.List)
            {
                result += num;
            }

            Console.WriteLine($"Received Add request: {string.Join(", ", request.List)}. Returning result: {result}");

            return Task.FromResult(new AddResponse
            {
                Result = result
            });
        }
    }
}
