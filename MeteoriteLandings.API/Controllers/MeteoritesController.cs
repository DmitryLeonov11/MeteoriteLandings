using Microsoft.AspNetCore.Mvc;
using MeteoriteLandings.Application.DTOs;
using MeteoriteLandings.Application.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MeteoriteLandings.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MeteoritesController : ControllerBase
    {
        private readonly IMeteoriteService _meteoriteService;

        public MeteoritesController(IMeteoriteService meteoriteService)
        {
            _meteoriteService = meteoriteService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<MeteoriteLandingGroupedByYearDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetFilteredAndGrouped([FromQuery] MeteoriteLandingFilterDto filter)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var serviceErrors = await _meteoriteService.ValidateFilterAsync(filter);
            if (serviceErrors.Any())
            {
                foreach (var error in serviceErrors)
                {
                    ModelState.AddModelError("Filter", error);
                }
                return BadRequest(ModelState);
            }

            var result = await _meteoriteService.GetFilteredAndGroupedLandingsAsync(filter);

            return Ok(result);
        }

        [HttpGet("recclasses")]
        public async Task<ActionResult<IEnumerable<string>>> GetUniqueRecClasses()
        {
            var result = await _meteoriteService.GetUniqueRecClassesAsync();
            return Ok(result);
        }
    }
}
