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
        
        [TestMethod]
        // Test 4. Naked Pairs/Triples/Quads
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

        [TestMethod("Test Hidden Pair")]
        // 5a. Hidden Pairs
        public void ShouldSolveHiddenPair()
        {
            // url https://www.sudokuwiki.org/hidden_candidates
            string[] data =
            [
                "         ",
                "9 46 7   ",
                " 768 41  ",
                "3 97 1 8 ",
                "7 8   3 1",
                " 513 87 2",
                "  75 261 ",
                "  54 32 8",
                "         ",
            ];

            var solved = new Sudoku().Solve(data);

            solved.Should().BeTrue();
        }

        [TestMethod("Test Hidden Triple")]
        // 5b. Hidden Triples
        public void ShouldSolveHiddenTriple()
        {
            // url https://www.sudokuwiki.org/hidden_candidates
            string[] data =
            [
                "65  87 24",
                "   649 5 ",
                " 4  25   ",
                "57 438 61",
                "   5 1   ",
                "31 9 2 85",
                "   89  1 ",
                "   213   ",
                "13 75  98",
            ];

            var solved = new Sudoku().Solve(data);

            solved.Should().BeTrue();
        }

        [TestMethod("Test Hidden Quadruple")]
        // 5c. Hidden Quads
        public void ShouldSolveHiddenQuad()
        {
            // url https://www.sudokuwiki.org/hidden_candidates
            string[] data =
            [
                "65  87 24",
                "   649 5 ",
                " 4  25   ",
                "57 438 61",
                "   5 1   ",
                "31 9 2 85",
                "   89  1 ",
                "   213   ",
                "13 75  98",
            ];

            var solved = new Sudoku().Solve(data);

            solved.Should().BeTrue();
        }

        [TestMethod("Test PointingPairs and PointingTriples")]
        // Test 6. Pointing Pairs/Triples
        public void ShouldSolvePointingPairsTriples()
        {
            // https://sudoku.com/sudoku-rules/pointing-pairs/
            // https://sudoku.com/sudoku-rules/pointing-triples/

            string[] data =
            [
                "     1 3 ",
                "231 9    ",
                " 65  31  ",
                "6789243  ",
                "1 3 5   6",
                "   1367  ",
                "  936 57 ",
                "  6 19843",
                "3        ",
            ];

            var solved = new Sudoku().Solve(data);

            solved.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldLockedCandidates()
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
        public void ShouldSolveYWing()
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