using SudokuSolver.Extensions;
using System.Collections.Concurrent;

namespace SudokuSolver
{
    internal class Sudoku
    {
        private Field[,] _fields2D;
        private List<Field> _fields = new();

        public Sudoku()
        {
            // Initialize fields
            _fields2D = new Field[9, 9];

            for (int row = 0; row < 9; row++)
                for (int col = 0; col < 9; col++)
                {
                    var field = new Field(row + 1, col + 1);
                    _fields2D[row, col] = field;
                    _fields.Add(field);
                }
        }        

        internal void Solve(string[] data)
        {
            Initialize(data);

            int solvedCount = 0;
            bool slashedValuesFound = true;
            bool eliminatedValuesFound = false;
            bool eliminatedOptionsFound = false;
            bool dualOptionsFound= false;
            bool tripleOptionsFound = false;

            while (slashedValuesFound || eliminatedValuesFound || eliminatedOptionsFound || dualOptionsFound || tripleOptionsFound)
            {
                solvedCount = _fields.Where(f => f.Value != null).Count();
                //Console.WriteLine($"Solved: {solvedCount}");
                if ( solvedCount == _fields.Count )
                    break;

                // Try to find values by 'slashing'
                slashedValuesFound = FindSlashedValues();

                // Try to find values by eliminating candidates
                eliminatedValuesFound = FindEliminatedValues();

                // Try to find values by eliminating candidates (advanced)
                eliminatedOptionsFound = FindEliminatedOptions();

                // Try find values based on two or three fields in block containing value as candidate
                if (!slashedValuesFound && !eliminatedValuesFound && !eliminatedOptionsFound) 
                {
                    // TODO vertalen
                    // Identify situations where, regarding a segment, two fields contain only two different possible values (or three in the case of three fields, etc.)
                    // For example, in block 1, fields 1 and 3 contain candidates with the possible values 4 and 8.
                    // Block 1, field 1; Candidates: 4 5 7 8
                    // Block 1, field 3; Candidates: 4 5 8
                    // In such cases, remove the other candidates (5 and 7) from fields 1 and 3.
                    // This applies to blocks, rows, and columns.

                    dualOptionsFound = false;

                    for (int i = 1; i <= 9; i++) 
                    {
                        if (CheckFieldsWithCandidates(_fields.Where(f => f.Block == i), "Block", 2))
                            dualOptionsFound = true;

                        if (CheckFieldsWithCandidates(_fields.Where(f => f.Row == i), "Row", 2))
                            dualOptionsFound = true;

                        if (CheckFieldsWithCandidates(_fields.Where(f => f.Column == i), "Column", 2))
                            dualOptionsFound = true;
                    }

                    tripleOptionsFound = false;
                }
            }

            Console.ForegroundColor = solvedCount == _fields.Count ? ConsoleColor.Green : ConsoleColor.Red;

            if (solvedCount == _fields.Count)
                Console.WriteLine("Solved:");
            else
                Console.WriteLine("Not solved:");

            Console.ResetColor();
            Console.WriteLine(this);
        }

        private bool FindSlashedValues()
        {
            bool valuesFound = false;
            foreach (var field in _fields)
            {
                if (field.Value != null)
                {
                    field.GetOtherRowFields(_fields).RemoveValueFromCandidates((int)field.Value);
                    field.GetOtherColumnFields(_fields).RemoveValueFromCandidates((int)field.Value);
                    field.GetOtherBlockFields(_fields).RemoveValueFromCandidates((int)field.Value);
                }
            }

            foreach (var field in _fields)
            {
                for (int value = 1; value <= 9; value++)
                {
                    if (field.Value == null)
                    {
                        if (!field.GetOtherRowFields(_fields).CandidatesContainsValue(value) ||
                            !field.GetOtherColumnFields(_fields).CandidatesContainsValue(value) ||
                            !field.GetOtherBlockFields(_fields).CandidatesContainsValue(value))
                        {
                            field.Value = value;
                            field.Candidates = [value];
                            valuesFound = true;
                        }
                    }
                }
            }
            return valuesFound;
        }

        private bool FindEliminatedValues()
        {
            bool valuesFound = false;

            foreach (var field in _fields)
            {
                if (field.Value == null)
                {
                    for (int value = 1; value <= 9; value++)
                    {
                        if (field.GetOtherRowFields(_fields).ContainsValue(value))
                            field.RemoveValueFromCandidates(value);

                        if (field.GetOtherColumnFields(_fields).ContainsValue(value))
                            field.RemoveValueFromCandidates(value);

                        if (field.GetOtherBlockFields(_fields).ContainsValue(value))
                            field.RemoveValueFromCandidates(value);
                    }
                }
            }

            foreach (var field in _fields.Where(f => f.Candidates.Count == 1))
            {
                if (field.Value == null)
                {
                    field.Value = field.Candidates[0];
                    valuesFound = true;
                }
            }

            return valuesFound;
        }

        // Per block try to find situations where a value can only exist in one row or column (e.g. two fields in block 2 on the same row where only a 7 can go).
        // In those cases eliminate 7 as a candidate on that row in the other horizontal blocks (= block 1 and 3) and see what happens.
        private bool FindEliminatedOptions()
        {
            var eliminatedOptionsFound = false;

            for (int block = 1; block <= 9; block++)
            {
                for (int value = 1; value <= 9; value++)
                {
                    var fieldsInBlockWithValueInCandidates = _fields.Where(f => f.Block == block && f.Candidates.Contains(value)).ToList();

                    if (fieldsInBlockWithValueInCandidates.Count <= 3)
                    {
                        // On same row?
                        if (fieldsInBlockWithValueInCandidates.GroupBy(f => f.Row).Count() == 1)
                        {
                            var result = EliminateOptions(block, value, fieldsInBlockWithValueInCandidates, true);

                            if (result)
                                eliminatedOptionsFound = true;
                        }

                        // On same column?
                        if (fieldsInBlockWithValueInCandidates.GroupBy(f => f.Column).Count() == 1)
                        {
                            var result = EliminateOptions(block, value, fieldsInBlockWithValueInCandidates, false);

                            if (result)
                                eliminatedOptionsFound = true;
                        }
                    }
                }
            }
            return eliminatedOptionsFound;
        }

        private static bool CheckFieldsWithCandidates(IEnumerable<Field> fields, string segment, int candidateCount)
        {
            var combinations = new List<ValueCombination>();
            bool candidateRemoved = false;

            // Generate candidate combinations based on the input candidateCount
            var candidateNumbers = Enumerable.Range(1, 9).ToList();
            var candidateCombinations = GetCombinations(candidateNumbers, candidateCount);

            foreach (var combination in candidateCombinations)
            {
                var matchingFields = fields.Where(f => combination.All(c => f.Candidates.Contains(c))).ToList();

                // If exactly candidateCount fields match the combination, check further
                if (matchingFields.Count == candidateCount)
                {
                    // Ensure that each candidate appears exactly candidateCount times
                    bool validCombination = true;
                    foreach (var candidate in combination)
                    {
                        if (fields.Count(f => f.Candidates.Contains(candidate)) != candidateCount)
                        {
                            validCombination = false;
                            break;
                        }
                    }

                    if (validCombination)
                    {
                        // Remove other candidates from the matching fields
                        foreach (var field in matchingFields)
                        {
                            var candidates = field.Candidates.ToList();
                            foreach (int candidate in candidates)
                            {
                                if (!combination.Contains(candidate))
                                {
                                    Console.WriteLine($"Check {segment}: {candidateCount}-candidate combination: {string.Join(" ", combination)}, Removing candidate {candidate}");
                                    Console.WriteLine(field.ToString());
                                    field.RemoveValueFromCandidates(candidate);
                                    candidateRemoved = true;
                                }
                            }
                        }
                    }
                }
            }
            return candidateRemoved;
        }

        // Helper method to generate combinations of a specific length
        private static IEnumerable<List<int>> GetCombinations(List<int> list, int length)
        {
            if (length == 0)
                return new List<List<int>> { new List<int>() };

            return list.SelectMany((item, index) =>
                GetCombinations(list.Skip(index + 1).ToList(), length - 1),
                (item, items) => new List<int> { item }.Concat(items).ToList());
        }


        private bool EliminateOptions(int block, int value, List<Field> fieldsInBlockWithValueInCandidates, bool isRow)
        {
            var eliminatedOptionsFound = false;

            var fieldsOutsideBlock = _fields.Where(f => f.Block != block); 
            fieldsOutsideBlock = isRow ?
                fieldsOutsideBlock.Where(f => f.Row == fieldsInBlockWithValueInCandidates[0].Row) :
                fieldsOutsideBlock.Where(f => f.Column == fieldsInBlockWithValueInCandidates[0].Column);

            eliminatedOptionsFound = fieldsOutsideBlock.CandidatesContainsValue(value);

            fieldsOutsideBlock.RemoveValueFromCandidates(value);
            return eliminatedOptionsFound;
        }

        // Initialize the puzzle
        private void Initialize(string[] data)
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (data[row][col] != ' ')
                    {
                        var value = int.Parse(data[row][col].ToString());
                        _fields2D[row, col].Value = value;
                        _fields2D[row, col].Candidates = [value];
                    }
                }
            }
        }

        public override string ToString()
        {
            var output = string.Empty;

            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    output += _fields2D[row, col].Value == null ? " " : _fields2D[row, col].Value.ToString();
                    //output += $"{_fields2D[row, col]}\r\n";
                }
                output += "\r\n";
            }

            return output;
        }

        private class ValueCombination
        {
            public List<int> Values { get; set; }
            public List<Field> Fields { get; set; }
        }
    }
}
