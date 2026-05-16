using training_service_db;
using training_service_db.Models;
using training_service_db.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Reflection;

namespace TrainingcoachApi.Controllers
{
    [Route("api/coaches")]
    [ApiController]
    public class CoachController : ControllerBase
    {
        CoachService _coachService;

        public CoachController(CoachService coachService)
        {
            _coachService = coachService;
        }


        [HttpGet]
        public async Task<List<CoachOutput>> Get() => await _coachService.GetAllAsync();


        [HttpGet("{id}")]
        public async Task<IActionResult> Get(String id)
        {
            var coach = await _coachService.GetByIdAsync(id);

            if (coach is null)
            {
                return NotFound();
            }

            return CreatedAtAction(nameof(Get), coach);
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetByFilter(String nameProperty, String value)
        {

            var coachs = await _coachService.GetByFilter(nameProperty, value);
            if (coachs is null)
            {
                return NotFound();
            }
            return CreatedAtAction(nameof(GetByFilter), coachs);
        }


        [HttpPost]
        public async Task<IActionResult> Post(CoachInput coach)
        {
            Coach _coach = await _coachService.CreateAsync(coach);
            return CreatedAtAction(nameof(Post), coach);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(String id, CoachInput updatecoach)
        {
            var currentcoach = await _coachService.GetByIdAsync(id);

            if (currentcoach is null)
            {
                return NotFound();
            }

            await _coachService.UpdateAsync(id, updatecoach);

            return NoContent();
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(String id)
        {
            var result = await _coachService.DeleteAsync(id);

            if (result.DeletedCount == 0)
            {
                return NotFound();
            }

            return NoContent();
        }


        [HttpDelete("all")]
        public async Task<IActionResult> DeleteAll()
        {
            await _coachService.DeleteAllAsync();
            return NoContent();
        }
    }
}
