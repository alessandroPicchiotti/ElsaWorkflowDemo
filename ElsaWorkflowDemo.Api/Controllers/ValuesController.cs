using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using static NodaTime.TimeZones.TzdbZone1970Location;

namespace ElsaWorkflowDemo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        public ValuesController() { }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Country>>> GetCountries()
        {
            List<string> listaStringhe = new List<string>
            {
                "cane",
                "gatto",
                "lupo"
            };
            var list = listaStringhe;
            await Task.Delay(0);

            return Ok(list);
        }
    }
}
