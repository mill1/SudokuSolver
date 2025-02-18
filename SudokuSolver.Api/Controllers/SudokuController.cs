using Microsoft.AspNetCore.Mvc;
using SudokuSolver.Api.Exceptions;
using SudokuSolver.Api.Interfaces;
using SudokuSolver.Api.Services;

namespace SudokuSolver.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SudokuController : ControllerBase
    {
        private readonly ILogger<SudokuController> _logger;
        private readonly ISudokuService _sudokuService;

        public SudokuController(ILogger<SudokuController> logger, ISudokuService sudokuService)
        {
            _logger = logger;
            _sudokuService = sudokuService;
        }

        [HttpGet]
        public string Get()
        {
            _logger.LogTrace("GET");
            return _sudokuService.GetSudoku();            
        }

        [HttpGet("Solve")]
        public IActionResult Solve([FromQuery] string puzzle)
        {
            try
            {
                _logger.LogTrace("SOLVE");
                var result = _sudokuService.Solve(puzzle);
                return Ok(result);
            }
            catch (InvalidPuzzleException e)
            {
                var message = $"Invalid puzzle: {e.Message}";
                _logger.LogInformation(message);
                return BadRequest(message);
            }
            catch (Exception e)
            {
                _logger.LogError($"Unhandled exception: {e}");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }        
    }
}
