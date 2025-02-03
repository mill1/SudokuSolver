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
            // X-Wing columns
            // Y-Wing
            string[] dataA =
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

            // https://www.livesudoku.com/en/sudoku/evil/
            // to test 
            string[] dataB =
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

            // https://www.sudokuwiki.org/Y_Wing_Strategy
            // Y-Wing

            // https://sudoku.com/sudoku-rules/y-wing/
            // https://www.learn-sudoku.com/xy-wing.html
            string[] dataC =
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

            new Sudoku().Solve(dataA);
        }
    }
}
