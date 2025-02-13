using Microsoft.AspNetCore.Mvc;
using System.Linq;
using SudokuSolverApi.Models;

namespace SudokuSolverApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SudokuController : ControllerBase
    {
        private readonly ILogger<SudokuController> _logger;

        public SudokuController(ILogger<SudokuController> logger)
        {
            _logger = logger;
        }

        // https://localhost:44310/Sudoku

        [HttpGet]
        public string[] Get()
        {
            string[][] sudokus = 
            {
                 [ "     1 3 ", "231 9    ", " 65  31  ", "6789243  ", "1 3 5   6", "   1367  ", "  936 57 ", "  6 19843", "3        " ],
                 [ "4    9   ", "      3  ", "5  83 96 ", " 5   8 9 ", " 7  5    ", "6   432 7", "7       6", "8   64   ", "3 52  4 8" ],
                 [ " 2   47  ", "  82     ", "9  6     ", "     83 6", "5 63    4", " 9 5  17 ", "      9  ", "64   1   ", "       18" ],
                 [ "9 46     ", "       18", " 2  5 46 ", "5    1 4 ", "4    2   ", "    9    ", " 8    7  ", " 51  83  ", "   5    6" ],
                 [ "5  96   4", "  2    8 ", "        3", "      2 7", "     2   ", " 4 75   6", "   4 9   ", "4    13 2", "    28  5" ]
            };
 
            return sudokus[Random.Shared.Next(0, 5)];
        }

        //  https://localhost:44310/Sudoku?puzzle=000001030231090000065003100678924300103050006000136700009360570006019843300000000
        [HttpPost]
        public string[] Solve([FromBody] string puzzle)
        {
            return ["a", "b", "c"];

        }
    }
}
