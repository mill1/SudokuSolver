namespace SudokuSolver
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Template
            string[] data0 =
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


            // https://www.sudoku9x9.com/expert/            
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

            // TODO zorg dat wijze van oplossen word gevolgd in sudokusolver.app
            // https://www.livesudoku.com/en/sudoku/evil/ = random
            string[] dataA =
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

            new Sudoku(true).Solve(dataA);
        }
    }
}
