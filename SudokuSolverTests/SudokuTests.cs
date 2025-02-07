using FluentAssertions;
using SudokuSolver;

namespace SudokuSolverTests
{
    [TestClass]
    public class SudokuTests
    {
        // TODO Test'Diabolical Strategies' en 'Extreme Strategies': https://www.sudokuwiki.org/Finned_Swordfish, https://www.sudokuwiki.org/AIC_with_ALSs etc.

        [TestMethod("Test Strategy")]
        public void ShouldSolveStrategy()
        {
            // url
            string[] data =
            [
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
            ];

            var solved = new Sudoku().Solve(data);

            solved.Should().BeTrue();
        }

        [TestMethod ("Test first three simple strategies")]
        // Test first three simple strategies:
        // 1. Basic Candidate Elimination
        // 2. Naked Singles
        // 3. Hidden Singles
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

        [TestMethod ("Test PointingPairsTriples and Claiming Pairs/Triples")]
        // Test next simple strategies:
        // 4. Pointing Pairs/Triples
        // 5. Claiming Pairs/Triples
        public void ShouldSolvePointingPairsTriples()
        {
            // https://www.taupierbw.be/SudokuCoach/SC_PointingTriple.shtml            
            string[] data =
            [
                "  9 7    ",
                " 8 4     ",
                "  3    28",
                "1     67 ",
                " 2  13 4 ",
                " 4   78  ",
                "6   3    ",
                " 1       ",
                "      284",
            ];

            var solved = new Sudoku().Solve(data);

            solved.Should().BeTrue();
        }

        [TestMethod]
        // Test next strategies:
        // Naked Pair (n = 2)
        // Naked Triple (n = 3) 
        // Naked Quad (n = 4)
        public void ShouldSolveNakedSubsets()
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
        public void ShouldSolveXYZ()
        {
            // https://www.sudokuwiki.org/XYZ_wing
            // Y-Wing
            string[] data =
            [
                " 92  175 ",
                "5  2    8",
                "    3 2  ",
                " 75  496 ",
                "2   6  75",
                " 697   3 ",
                "  8 9  2 ",
                "7    3 89",
                "9 38   4 "
            ];

            var solved = new Sudoku().Solve(data);

            solved.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldSolveEvil()
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

        [TestMethod]
        public void ShouldSolveThisOne()
        {
            // https://www.taupierbw.be/SudokuCoach/SC_PointingTriple.shtml            
            string[] data =
            [
                "   5 3624",
                "324  7 16",
                "    24 3 ",
                "1     3  ",
                "2 6 719 4",
                "  9     1",
                "   6834  ",
                "4 271   3",
                " 834 21  ",
            ];

            var solved = new Sudoku().Solve(data);

            solved.Should().BeTrue();
        }

    }
}