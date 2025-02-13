using FluentAssertions;
using SudokuSolver.Api.Services;

namespace SudokuSolverTests
{
    [TestClass]
    public class SudokuSolverApiTests
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

        [TestMethod]
        [DataRow([" X   1 3 ", "231 9    ", " 65  31  ", "6789243  ", "1 3 5   6", "   1367  ", "  936 57 ", "  6 19843", "3        "])]
        [DataRow([" 0   1 3 ", "231 9    ", " 65  31  ", "6789243  ", "1 3 5   6", "   1367  ", "  936 57 ", "  6 19843", "3        "])]
        [DataRow([" $   1 3 ", "231 9    ", " 65  31  ", "6789243  ", "1 3 5   6", "   1367  ", "  936 57 ", "  6 19843", "3        "])]
        public void ShouldThrowExceptionInvalidPuzzleInvalidChars(string[] data)
        {
            // Prepare
            var sudoku = new SudokuService();

            // Act/Assert
            sudoku.Invoking(a => a.Solve(data))
                .Should().Throw<ArgumentException>()
                .Where(e => e.Message.Equals("Invalid puzzle. Allowed characters: 1-9 and ' ' (space)"));
        }

        [TestMethod]
        [DataRow(["12", "34"])]
        [DataRow(["     1 3 ", "231 9    ", " 65  31  ", "6789243  ", "1 3 5   6", "   1367  ", "  936 57 ", "  6 19843"])]
        [DataRow(["  1 3 ", "29    ", " 631  ", "6243  ", "15   6", " 367  ", "  936 ", " 19843", "3     "])]
        [DataRow(["     1 3 ", "231 9    ", " 65  31  ", "6789243  ", "1 3 5   6", "   1367  ", "  936 57 ", "  6 19843", "3        ", "123456789"])]
        public void ShouldThrowExceptionInvalidPuzzleDimensions(string[] data)
        {
            // Prepare
            var sudoku = new SudokuService();

            // Act/Assert
            sudoku.Invoking(a => a.Solve(data))
                .Should().Throw<ArgumentException>()
                .Where(e => e.Message.Equals("Invalid puzzle. Expected 9x9 matrix."));
        }

        [TestMethod]
        [DataRow(["44   9   ", "      3  ", "5  83 96 ", " 5   8 9 ", " 7  5    ", "6   432 7", "7       6", "8   64   ", "3 52  4 8"])] // Row
        [DataRow(["4    9   ", "      3  ", "5  83 96 ", " 5   8 9 ", " 7  5    ", "6   432 7", "7       6", "8   64   ", "4 52  4 8"])] // Column
        [DataRow(["4    9   ", "      3  ", "54 83 96 ", " 5   8 9 ", " 7  5    ", "6   432 7", "7       6", "8   64   ", "3 52  4 8"])] // Block
        public void ShouldThrowExceptionInvalidPuzzleDuplicateValues(string[] data)
        {
            // Prepare
            var sudoku = new SudokuService();

            // Act/Assert
            sudoku.Invoking(a => a.Solve(data))
                .Should().Throw<ArgumentException>()
                .Where(e => e.Message.Contains("duplicate values found."));
        }

        [TestMethod("Test first three simple strategies")]
        // Test folowing simple strategies:
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

            int[,] result = new SudokuService().Solve(data);

            IsSudokuSolved(result).Should().BeTrue();
        }

        [TestMethod("Test Naked Pairs/Triples/Quads")]
        // 4. Naked subset strategies
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

            int[,] result = new SudokuService().Solve(data);

            IsSudokuSolved(result).Should().BeTrue();
        }

        [TestMethod("Test Hidden Pairs")]
        // 5a. Hidden Pairs strategy
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

            int[,] result = new SudokuService().Solve(data);

            IsSudokuSolved(result).Should().BeTrue();
        }

        [TestMethod("Test Hidden Triple")]
        // 5b. Hidden Triples strategy
        public void ShouldSolveHiddenTriple()
        {
            // url https://hodoku.sourceforge.net/en/tech_hidden.php
            string[] data =
            [
                "5  62  37",
                "  489    ",
                "    5    ",
                "93       ",
                " 2    6 5",
                "7       3",
                "     9   ",
                "      7  ",
                "68 57   2",
            ];

            int[,] result = new SudokuService().Solve(data);

            IsSudokuSolved(result).Should().BeTrue();
        }

        [TestMethod("Test PointingPairs and PointingTriples")]
        // Test 6. Locked Candidates; Pointing Pairs/Triples
        public void ShouldSolveLockedCandidates()
        {
            // https://www.livesudoku.com/en/sudoku/evil/ 
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

            int[,] result = new SudokuService().Solve(data);

            IsSudokuSolved(result).Should().BeTrue();
        }

        [TestMethod("Test X-Wing (columns)")]
        // 7a. X-Wing strategy (columns)
        // Also tests Locked candidates; Claiming Pairs/Triples
        public void ShouldSolveXWingColumns()
        {
            // https://www.sudoku9x9.com/expert/
            // 
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

            int[,] result = new SudokuService().Solve(data);

            IsSudokuSolved(result).Should().BeTrue();
        }

        [TestMethod("Test X-Wing (rows)")]
        // 7b. X-Wing strategy (rows)
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

            int[,] result = new SudokuService().Solve(data);

            IsSudokuSolved(result).Should().BeTrue();
        }

        [TestMethod("Test Y-Wing")]
        // 8. Y-Wing strategy
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

            int[,] result = new SudokuService().Solve(data);

            IsSudokuSolved(result).Should().BeTrue();
        }

        [TestMethod("Test 5 five star puzzles")]
        [DataRow(["     1 3 ", "231 9    ", " 65  31  ", "6789243  ", "1 3 5   6", "   1367  ", "  936 57 ", "  6 19843", "3        "])]
        [DataRow(["4    9   ", "      3  ", "5  83 96 ", " 5   8 9 ", " 7  5    ", "6   432 7", "7       6", "8   64   ", "3 52  4 8"])]
        [DataRow([" 2   47  ", "  82     ", "9  6     ", "     83 6", "5 63    4", " 9 5  17 ", "      9  ", "64   1   ", "       18"])]
        [DataRow(["9 46     ", "       18", " 2  5 46 ", "5    1 4 ", "4    2   ", "    9    ", " 8    7  ", " 51  83  ", "   5    6"])]
        [DataRow(["5  96   4", "  2    8 ", "        3", "      2 7", "     2   ", " 4 75   6", "   4 9   ", "4    13 2", "    28  5"])]
        public void ShouldSolveFiveStarPuzzles(string[] data)
        {
            int[,] result = new SudokuService().Solve(data);

            IsSudokuSolved(result).Should().BeTrue();
        }

        [TestMethod("Test unsolved")]
        // Solution needs following unimplemented strategies:
        // Hidden Quads
        // X-Wing
        // XYZ-Wing
        // Color Wing
        // Almost Locked Set
        public void ShouldNotSolveThisOne()
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

            int[,] result = new SudokuService().Solve(data);

            IsSudokuSolved(result).Should().BeFalse();
        }

        private static bool IsSudokuSolved(int[,] sudokuArray)
        {
            return sudokuArray.Cast<int>().All(value => value > 0);
        }
    }
}
