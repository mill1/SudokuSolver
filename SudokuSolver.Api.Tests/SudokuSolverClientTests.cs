using FluentAssertions;
using SudokuSolver.Api.Services;

namespace SudokuSolverTests
{
    [TestClass]
    public class SudokuSolverClientTests
    {
        [TestMethod]
        public void ShouldThrowExceptionInvalidPuzzleMinimumClues()
        {
            // Prepare
            string[] data =
            [
                "         ",
                "  1      ",
                "    2    ",
                "      3  ",
                "        4",
                "         ",
                "         ",
                "         ",
                "         ",
            ];

            var sudoku = new SudokuService();

            // Act/Assert
            sudoku.Invoking(a => a.Solve(data))
                .Should().Throw<ArgumentException>()
                .Where(e => e.Message.Equals("Invalid puzzle. Minimum number of clues: 17"));
        }

        
    }
}