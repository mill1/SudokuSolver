namespace SudokuSolver
{
    public class Program
    {
        static void Main(string[] args)
        {
            bool debug = false;
            if(args.Length > 0) 
                debug = true;


            // https://www.sudoku9x9.com/expert/            
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

            new Sudoku(debug).Solve(data);
        }
    }
}
