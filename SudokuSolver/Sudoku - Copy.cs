using SudokuSolver.Extensions;
using System.Collections.Concurrent;

namespace SudokuSolver
{
    internal class SudokuBAK
    {
        private Field[,] _fields2D;
        private List<Field> _fields = new();

        public SudokuBAK()
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
                    // Identificeer situaties waarbij mbt een segment in twee fields slechts twee verschillende passen (of 3 in 3 etc.)
                    // Bijv. in block 1 bevatten fields 1 en 3 candidates met de mogelijke waarden 4 en 8.
                    // Block 1, field 1; Candidates: 4 5 7 8
                    // Block 1, field 3; Candidates: 4 5 8
                    // Verwijder in die gevallen de andere candidates 5 en 7 uit fields 1 en 3.
                    // Het gaat hierbij om blocks, rows en columns.

                    dualOptionsFound = false;

                    for (int i = 1; i <= 9; i++) 
                    {
                        if (FindDuos(_fields.Where(f => f.Block == i), "Block"))
                            dualOptionsFound = true;

                        if (FindDuos(_fields.Where(f => f.Row == i), "Row"))
                            dualOptionsFound = true;

                        if (FindDuos(_fields.Where(f => f.Column == i), "Column"))
                            dualOptionsFound = true;
                    }

                    tripleOptionsFound = false;

                    //for (int i = 1; i <= 9; i++)
                    //{
                    //    if (FindTriplets(_fields.Where(f => f.Block == i), "Block"))
                    //        tripleOptionsFound = true;

                    //    if (FindTriplets(_fields.Where(f => f.Row == i), "Row"))
                    //        tripleOptionsFound = true;

                    //    if (FindTriplets(_fields.Where(f => f.Column == i), "Column"))
                    //        tripleOptionsFound = true;
                    //}
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

        private static bool FindDuos(IEnumerable<Field> fields, string segment)
        {
            var combinations = new List<ValueCombination>();
            bool dualOptionsFound = false;

            for (int i = 1; i <= 9; i++)
            {
                for (int j = 1; j <= 9; j++)
                {
                    if (i < j)
                    {
                        combinations.Add(new ValueCombination
                        {
                            Values = new List<int> { i, j },
                            Fields = fields.Where(f => f.Candidates.Contains(i) && f.Candidates.Contains(j)).ToList()
                        });
                    }
                }
            }

            var duos = combinations.Where(c => c.Fields.Count == 2).ToList();
            // Resolve the duo value combination where they both occur twice in the block, row or column
            foreach (var duo in duos)
            {
                if (fields.Where(f => f.Candidates.Contains(duo.Values[0])).Count() == 2 && fields.Where(f => f.Candidates.Contains(duo.Values[1])).Count() == 2)
                {
                    // Remove other candidates from those fields
                    foreach (var field in duo.Fields)
                    {
                        var candidates = field.Candidates.ToList();

                        foreach (int candidate in candidates)
                        {
                            if (!(candidate == duo.Values[0] || candidate == duo.Values[1]))
                            {                                
                                Console.WriteLine($"Check {segment}: duo: {duo.Values[0]} {duo.Values[1]}, Removing candidate {candidate}");
                                Console.WriteLine(field.ToString());
                                field.RemoveValueFromCandidates(candidate);
                                dualOptionsFound = true;
                            }
                        }
                    }
                }
            }
            return dualOptionsFound;
        }

        private static bool FindTriplets(IEnumerable<Field> fields, string segment)
        {
            var combinations = new List<ValueCombination>();
            bool tripletOptionsFound = false;

            for (int i = 1; i <= 9; i++)
            {
                for (int j = 1; j <= 9; j++)
                {
                    for (int k = 1; k <= 9; k ++)
                    {
                        if (i < j && j < k)
                        {
                            combinations.Add(new ValueCombination
                            {
                                Values = new List<int> { i, j, k},
                                Fields = fields.Where(f => f.Candidates.Contains(i) && f.Candidates.Contains(j) && f.Candidates.Contains(k)).ToList()
                            });
                        }
                    }                    
                }
            }

            var triplets = combinations.Where(c => c.Fields.Count == 3).ToList();
            // Resolve the triplets value combination where all three exist thrice in the block, row or column
            foreach (var triplet in triplets)
            {
                if (fields.Where(f => f.Candidates.Contains(triplet.Values[0])).Count() == 3 && fields.Where(f => f.Candidates.Contains(triplet.Values[1])).Count() == 3 && fields.Where(f => f.Candidates.Contains(triplet.Values[2])).Count() == 3)
                {
                    // Remove other candidates from those fields
                    foreach (var field in triplet.Fields)
                    {
                        var candidates = field.Candidates.ToList();

                        foreach (int candidate in candidates)
                        {
                            if (!(candidate == triplet.Values[0] || candidate == triplet.Values[1] || candidate == triplet.Values[2]))
                            {
                                //Console.WriteLine($"{segment}: triplet: {triplet.Values[0]} {triplet.Values[1]} {triplet.Values[2]},  Removing candidate {candidate}");
                                //Console.WriteLine(field.ToString());
                                field.RemoveValueFromCandidates(candidate);
                                tripletOptionsFound = true;
                            }
                        }
                    }
                }
            }

            return tripletOptionsFound;
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
