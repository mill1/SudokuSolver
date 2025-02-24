using Microsoft.AspNetCore.Mvc;
using SudokuSolver.Api.Exceptions;
using SudokuSolver.Api.Interfaces;

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
        public IActionResult Solve([FromQuery] string sudoku)
        {
            try
            {
                _logger.LogTrace("SOLVE");
                var result = _sudokuService.Solve(sudoku);
                return Ok(result);
            }
            catch (InvalidSudokuException e)
            {
                var message = $"Invalid sudoku: {e.Message}";
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
