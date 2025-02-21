namespace SudokuSolver.Api.Exceptions
{
    public class InvalidSudokuException: Exception
    {
        public InvalidSudokuException(string? message) : base(message)
        {
        }
    }
}