using Microsoft.VisualBasic;

namespace SudokuSolver.Extensions
{
    internal static class FieldExtensions
    {
        public static IEnumerable<Field> Rows(this IEnumerable<Field> fields, int row)
        {
            return fields.Where(f => f.Row == row);
        }

        public static IEnumerable<Field> Columns(this IEnumerable<Field> fields, int column)
        {
            return fields.Where(f => f.Column == column);
        }

        public static IEnumerable<Field> Blocks(this IEnumerable<Field> fields, int block)
        {
            return fields.Where(f => f.Block == block);
        }

        public static IEnumerable<Field> WithNumberOfCandidates(this IEnumerable<Field> fields, int number)
        {
            return fields.Where(f => f.Candidates.Count == number);
        }

        public static IEnumerable<Field> OtherRowFields(this Field field)
        {
            return field.Fields.Where(f => f.Row == field.Row && f.Column != field.Column);
        }

        public static IEnumerable<Field> OtherColumnFields(this Field field)
        {
            return field.Fields.Where(f => f.Column == field.Column && f.Row != field.Row);
        }

        public static IEnumerable<Field> OtherBlockFields(this Field field)
        {
            return field.Fields.Where(f => f.Block == field.Block && f != field);
        }

        public static bool ContainsValue(this IEnumerable<Field> fields, int value)
        {
            return fields.ToList().Any(x => x.Value == value);
        }

        public static bool CandidatesContainsValue(this IEnumerable<Field> fields, int value)
        {
            return fields.ToList().Any(x => x.Candidates.Contains(value));
        }

        public static int RemoveValueFromCandidates(this IEnumerable<Field> fields, int value)
        {
            int nrOfCandidatesRemoved = 0;

            foreach (var field in fields)
            {
                nrOfCandidatesRemoved += RemoveValueFromCandidates(field, value);
            }

            return nrOfCandidatesRemoved;
        }

        public static int RemoveValueFromCandidates(this Field field, int value)
        {
            if (field.Candidates.Contains(value))
            {
                var remove = field.Candidates.Single(c => c == value);
                field.Candidates.Remove(remove);
                
                // TODO lw
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Removed candidate {value} from {field}");
                Console.ForegroundColor = ConsoleColor.White;

                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
}
