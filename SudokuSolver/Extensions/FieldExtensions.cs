using Microsoft.VisualBasic;

namespace SudokuSolver.Extensions
{
    internal static class FieldExtensions
    {
        public static IEnumerable<Field> OtherRowFields(this Field field, List<Field> fields)
        {
            return fields.Where(f => f.Row == field.Row && f.Column != field.Column);
        }

        public static IEnumerable<Field> OtherColumnFields(this Field field, List<Field> fields)
        {
            return fields.Where(f => f.Column == field.Column && f.Row != field.Row);
        }

        public static IEnumerable<Field> OtherBlockFields(this Field field, List<Field> fields)
        {
            return fields.Where(f => f.Block == field.Block && f != field);
        }

        public static bool ContainsValue(this IEnumerable<Field> fields, int value)
        {
            return fields.ToList().Any(x => x.Value == value);
        }

        public static bool CandidatesContainsValue(this IEnumerable<Field> fields, int value)
        {
            return fields.ToList().Any(x => x.Candidates.Contains(value));
        }

        public static void RemoveValueFromCandidates(this IEnumerable<Field> fields, int value)
        {
            foreach (var field in fields)
            {
                RemoveValueFromCandidates(field, value);
            }
        }

        public static void RemoveValueFromCandidates(this Field field, int value)
        {
            if (field.Candidates.Contains(value))
            {
                var remove = field.Candidates.Single(c => c == value);
                field.Candidates.Remove(remove);

                // TODO lw
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Removed candidate {value} from {field}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
    }
}
