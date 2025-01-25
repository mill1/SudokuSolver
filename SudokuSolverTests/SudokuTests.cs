using FluentAssertions;
using SudokuSolver;

namespace SudokuSolverTests
{
    [TestClass]
    public class SudokuTests
    {
        [TestMethod]
        public void ShouldSolveAFourStarSudoku()
        {
            // 4 star page 12
            string[] data12 =
            [
                " 2  5 47 ",
                "    31   ",
                "  1   3 6",
                "   1 8   ",
                "  69  7 8",
                "     29  ",
                "     6 9 ",
                "4     6 7",
                "9 72  8  ",
            ];

            var solved = new Sudoku().Solve(data12);

            solved.Should().BeTrue();
        }
    }
}