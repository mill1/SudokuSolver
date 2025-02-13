using SudokuSolverApi.Models;

namespace SudokuSolverApi.Extensions
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

        public static bool IntersectsWith(this Field sourceField, Field field)
        {
            return sourceField.OtherPeers().Contains(field);
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

        public static IEnumerable<Field> OtherPeers(this Field field)
        {
            // Combine all peers and remove duplicates using Distinct
            return field.OtherRowFields()
           .Concat(field.OtherColumnFields())
           .Concat(field.OtherBlockFields())
           .Distinct();  // Ensure no duplicates (if a field shares both row and block, for example)
        }

        public static bool ContainsValue(this IEnumerable<Field> fields, int value)
        {
            return fields.Any(x => x.Value == value);
        }

        public static bool CandidatesContainsValue(this IEnumerable<Field> fields, int value)
        {
            return fields.Any(x => x.Candidates.Contains(value));
        }

        public static int RemoveCandidateFromFields(this IEnumerable<Field> fields, int candidate)
        {
            int nrOfCandidatesRemoved = 0;

            foreach (var field in fields)
                nrOfCandidatesRemoved += RemoveCandidateFromField(field, candidate);

            return nrOfCandidatesRemoved;
        }

        public static int RemoveCandidateFromField(this Field field, int value)
        {
            if (field.Candidates.Contains(value))
            {
                var remove = field.Candidates.Single(c => c == value);
                field.Candidates.Remove(remove);

                if (field.Candidates.Count == 0)
                     throw new InvalidOperationException($"No candidates left after removing value {value}. Field: {field}");

                // TODO if (Settings.Debug)
                    Console.WriteLine($"Removed candidate {value} from {field}...");

                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
}
