using API_sprot_training_program.Models;
using API_sprot_training_program.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Reflection;

namespace TrainingProgramApi.Controllers
{
    [Route("api/programs")]
    [ApiController]
    public class TrainingController : ControllerBase
    {
        TrainingService _service;
        public TrainingController(TrainingService service) { 
            _service = service;
        }


        [HttpGet]
        public async Task<List<TrainingOutput>> Get() =>await _service.GetAllAsync();


        [HttpGet("{id}")]
        public async Task<IActionResult> Get(String id)
        {
            var program = await _service.GetByIdAsync(id);

            if (program is null)
            {
                return NotFound();
            }

            return CreatedAtAction(nameof(Get), program);
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetByFilter(String nameProperty, String value)
        {
     
            var programs = await _service.GetByFilter(nameProperty, value);
            if (programs is null)
            {
                return NotFound();
            }
            return CreatedAtAction(nameof(GetByFilter), programs);
        }


        [HttpPost]
        public async Task<IActionResult> Post(TrainingInput program)
        {
            var resutl = await _service.CreateAsync(program);
            if (resutl is null) {
                return NotFound();
            }
            return CreatedAtAction(nameof(Post), program);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(String id, TrainingInput updateProgram)
        {
            var currentProgram = await _service.GetByIdAsync(id);

            if (currentProgram is null)
            {
                return NotFound();
            }

            await _service.UpdateAsync(id, updateProgram);

            return NoContent();
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(String id)
        {
            var result = await _service.DeleteAsync(id);

            if (result.DeletedCount == 0)
            {
                return NotFound();
            }

            return NoContent();
        }


        [HttpDelete("all")]
        public async Task<IActionResult> DeleteAll()
        {
            await _service.DeleteAllAsync();
            return NoContent();
        }
    }
}
