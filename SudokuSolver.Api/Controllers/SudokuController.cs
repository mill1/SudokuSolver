using Microsoft.AspNetCore.Mvc;
using SudokuSolver.Api.Services;

namespace SudokuSolver.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SudokuController : ControllerBase
    {
        private readonly ILogger<SudokuController> _logger;
        private readonly SudokuService _sudokuService;

        public SudokuController(ILogger<SudokuController> logger, SudokuService sudokuService)
        {
            _logger = logger;
            _sudokuService = sudokuService;
        }

        [HttpGet]
        public string[] Get()
        {
            _logger.LogTrace("GET");
            return _sudokuService.GetSudoku();            
        }

        [HttpGet("Solve")]
        public int[,] Solve([FromQuery] string puzzle)
        {
            _logger.LogTrace("SOLVE");
            return _sudokuService.SolveSudoku(puzzle);
        }        
    }
}
