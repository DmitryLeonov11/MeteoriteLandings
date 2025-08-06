using Microsoft.AspNetCore.Mvc;
using MeteoriteLandings.Application.DTOs;
using MeteoriteLandings.Application.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        public async Task<IActionResult> GetFilteredAndGrouped([FromQuery] MeteoriteLandingFilterDto filter)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
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
            catch (DbUpdateException ex)
            {
                // Database-related errors
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Error = "Database error occurred while processing your request." });
            }
            catch (TimeoutException ex)
            {
                return StatusCode(StatusCodes.Status408RequestTimeout, 
                    new { Error = "Request timed out. Please try again later." });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                // Generic error - logged by middleware
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Error = "An unexpected error occurred while processing your request." });
            }
        }

        [HttpGet("recclasses")]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<string>>> GetUniqueRecClasses()
        {
            try
            {
                var result = await _meteoriteService.GetUniqueRecClassesAsync();
                return Ok(result);
            }
            catch (DbUpdateException)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Error = "Database error occurred while fetching meteorite classes." });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Error = "An unexpected error occurred while fetching meteorite classes." });
            }
        }
    }
}
