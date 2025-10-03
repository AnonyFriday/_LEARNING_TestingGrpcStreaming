using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GrpcClientUnary
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalculatorController : ControllerBase
    {
        private readonly Calculator.CalculatorClient _calculatorClient;

        public CalculatorController(Calculator.CalculatorClient calculatorClient)
        {
            _calculatorClient = calculatorClient;
        }

        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody] List<uint> numbers)
        {
            Console.WriteLine("Hello World");
            var request = new AddRequest()
            {
                List = { numbers },
                Count = (uint)numbers.Count,
            };

            var response = await _calculatorClient.AddAsync(request);
            return Ok(response.Result);
        }
    }
}
