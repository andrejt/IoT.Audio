using System.Threading.Tasks;

namespace IoT.Audio.Controllers
{
    internal class SayController : Controller
    {
        private readonly SpeechService _speechService;
        public SayController(SpeechService speechService)
        {
            _speechService = speechService;
        }
        public async Task<WebServerResponse> Get(string text, string floor)
        {
            if (string.IsNullOrEmpty(floor))
            {
                await _speechService.SayAsync(text, 0);
            }
            else if (floor.ToLower() == "up")
            {
                await _speechService.SayAsync(text, -1);
            }
            else
            {
                await _speechService.SayAsync(text, 1);
            }
            return WebServerResponse.CreateOk("OK");
        }
    }
}
