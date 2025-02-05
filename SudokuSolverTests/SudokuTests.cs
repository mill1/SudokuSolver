using FluentAssertions;
using SudokuSolver;

namespace SudokuSolverTests
{
    [TestClass]
    public class SudokuTests
    {
        // TODO Test'Diabolical Strategies' en 'Extreme Strategies': https://www.sudokuwiki.org/Finned_Swordfish, https://www.sudokuwiki.org/AIC_with_ALSs etc.

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
        public void ShouldSolveXWingColumns()
        {
            // https://www.sudoku9x9.com/expert/
            // X-Wing columns
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

        [TestMethod]
        public void ShouldSolveYWingRows()
        {
            // https://www.sudokuwiki.org/Y_Wing_Strategy
            // Y-Wing
            string[] data =
            [
                "9  24    ",
                " 5 69 231",
                " 2  5  9 ",
                " 9 7  32 ",
                "  29356 7",
                " 7   29  ",
                " 69 2  73",
                "51  79 62",
                "2 7 86  9",
            ];

            var solved = new Sudoku().Solve(data);

            solved.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldSolveEvil0()
        {
            // https://www.livesudoku.com/en/sudoku/evil/ = random
            string[] data =
            [
                " 8     5 ",
                "    3   8",
                "  491   2",
                "23  7 1  ",
                "4  6  2 5",
                " 7      6",
                "  81 6  9",
                "941      ",
                "         ",
            ];

            var solved = new Sudoku(true).Solve(data);

            solved.Should().BeTrue();
        }

        // Is gebaseerd op to be oude volgorde
        //[TestMethod]
        //public void ShouldSolveSecond()
        //{
        //    // https://www.livesudoku.com/en/sudoku/evil/ = random
        //    string[] data =
        //    [
        //        " 8  6 95 ",
        //        " 9  35  8",
        //        "  4918  2",
        //        "236579184",
        //        "419683275",
        //        "875  1396",
        //        "  81 6  9",
        //        "941      ",
        //        "    9    ",
        //    ];

        //    var solved = new Sudoku(true).Solve(data);

        //    solved.Should().BeTrue();
        //}
    }
}