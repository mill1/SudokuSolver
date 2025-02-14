namespace SudokuSolver.Api.Exceptions
{
    public class InvalidPuzzleException: Exception
    {
        public InvalidPuzzleException(string? message) : base(message)
        {
        }
    }
}