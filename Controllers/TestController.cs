using Microsoft.AspNetCore.Mvc;
using RealtimeDataSamples.Services;
using System.Collections.Concurrent;
using System.Text.Json;

namespace RealtimeDataSamples.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ItemService _itemService;

        public TestController(ItemService itemService)
        {
            _itemService = itemService;
        }

        [HttpGet("event-source")]
        public async Task EventSource(CancellationToken ct)
        {
            Response.Headers.Add("Content-Type", "text/event-stream");

            while (!ct.IsCancellationRequested)
            {
                var item = await _itemService.WaitForNewItem();

                await Response.WriteAsync($"data: ");
                await JsonSerializer.SerializeAsync(Response.Body, item);
                await Response.WriteAsync($"\n\n");
                await Response.Body.FlushAsync();

                _itemService.Reset();
            }
        }

        private static readonly ConcurrentQueue<Item> _itemQueue = new ConcurrentQueue<Item>();

        [HttpPost("add-item")]
        public IActionResult AddItem([FromBody] Item newItem)
        {
            _itemQueue.Enqueue(newItem);
            return Ok();
        }

        [HttpGet("long-polling")]
        public async Task<IActionResult> LongPolling(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return NoContent();
                }

                if (_itemQueue.TryDequeue(out var newItem))
                {
                    return Ok(newItem);
                }

                await Task.Delay(100000); // Adjust the delay as needed
            }
        }

        [HttpGet("efficientLongPolling")]
        public async Task<IActionResult> EfficientLongPolling(CancellationToken userCt)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(userCt);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var timeoutTask = Task.Delay(-1, cts.Token);
            var itemArrivedTask = _itemService.WaitForNewItem();

            var completedTask = await Task.WhenAny(itemArrivedTask, timeoutTask);
            if (completedTask == itemArrivedTask)
            {
                var item = await itemArrivedTask;
                _itemService.Reset();
                return Ok(item);
            }

            return NoContent();
        }
    }
}
