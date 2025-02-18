using SudokuSolver.Api.Models;

namespace SudokuSolver.Api.Extensions
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
            // Combine all peers and remove duplicates
            return field.OtherRowFields()
           .Concat(field.OtherColumnFields())
           .Concat(field.OtherBlockFields())
           .Distinct(); 
        }

        public static bool ContainsValue(this IEnumerable<Field> fields, int value)
        {
            return fields.Any(x => x.Value == value);
        }

        public static bool CandidatesContainsValue(this IEnumerable<Field> fields, int value)
        {
            return fields.Any(x => x.Candidates.Contains(value));
        }
    }
}
