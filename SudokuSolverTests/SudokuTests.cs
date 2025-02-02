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
            // 4 star puzzle page 12
            string[] data =
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

            var solved = new Sudoku().Solve(data);

            solved.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldSolveAFiveStarSudoku()
        {
            // 5 star puzzle page 13
            string[] data =
            [
                "     19  ",
                "   546 7 ",
                "   9  12 ",
                "  8      ",
                " 4  5   2",
                "6  7 9   ",
                " 69  4  8",
                " 7 2  4 9",
                "38       ",
            ];

            var solved = new Sudoku().Solve(data);

            solved.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldSolveXWingColumnsAndYWing()
        {
            // https://www.sudoku9x9.com/expert/
            // X-Wing columns
            // Y-Wing
            string[] data =
            [
                "      3 9",
                "    4  6 ",
                "    26  8",
                "5 1  4 2 ",
                " 7  18   ",
                "  6      ",
                "8     6  ",
                "    95  7",
                "4  3   1 ",
            ];

            var solved = new Sudoku().Solve(data);

            solved.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldSolveXWingRows()
        {
            // https://www.sudoku9x9.com/expert/
            // X-Wing rows (see previous test; turned 90 degrees)
            string[] data =
            [
                "4 8  5   ",
                "    7    ",
                "   6 1   ",
                "3        ",
                " 9  1 24 ",
                " 5  846  ",
                "  6     3",
                "1    2 6 ",
                " 7    8 9",
            ];

            var solved = new Sudoku().Solve(data);

            solved.Should().BeTrue();
        }
    }
}