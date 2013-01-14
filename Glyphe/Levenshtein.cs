/*
 * Copyright (c) 2011-2013, Achim 'ahzf' Friedland <achim@graph-database.org>
 * This file is part of Glyphe <http://www.github.com/Vanaheimr/Glyphe>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace de.ahzf.Glyphe
{

    #region NeedlemanWunsch

    public static class NeedlemanWunsch
    {

        public static Levenshtein Create(String                  WordA,
                                         String                  WordB,
                                         Int32                   InsertionAndDeletionCosts = 1,
                                         Func<Char, Char, Int32> SubstitutionCostFunc      = null)
        {

            return new Levenshtein(WordA,
                                    WordB,
                                    (character, position) => -position * InsertionAndDeletionCosts,
                                    (character, position) => -position * InsertionAndDeletionCosts,
                                    (c) => -InsertionAndDeletionCosts,
                                    (c) => -InsertionAndDeletionCosts,
                                    (SubstitutionCostFunc != null) ? SubstitutionCostFunc : (c1, c2) => { return (c1 == c2) ? 1 : -1; },
                                    null,
                                    null,
                                    Levenshtein.LevenshteinGoals.Maximize);

        }

        public static Levenshtein CreateAndCalc(String                  WordA,
                                                String                  WordB,
                                                Int32                   InsertionAndDeletionCosts = 1,
                                                Func<Char, Char, Int32> SubstitutionCostFunc      = null)
        {

            var _NeedlemanWunsch = NeedlemanWunsch.Create(WordA, WordB, InsertionAndDeletionCosts, SubstitutionCostFunc);

            _NeedlemanWunsch.CalculateMinimumEditDistance();

            return _NeedlemanWunsch;

        }

    }

    #endregion

    //public static class SmithWaterman

    #region Levenshtein

    /// <summary>
    /// The Levenshtein distance is a string metric for measuring
    /// the amount of difference between two words.
    /// </summary>
    public class Levenshtein
    {

        #region Data

        #region (enum) LevenshteinEdits

        /// <summary>
        /// The type of Levenshtein edits.
        /// </summary>
        [Flags]
        public enum LevenshteinEdits
        {
            NotYet        = 0x00,
            DoNotBother   = 0x01,
            Insertion     = 0x02,
            Deletion      = 0x04,
            Substitution  = 0x08
        }

        #endregion

        #region (enum) LevenshteinGoal

        /// <summary>
        /// The goal of Levenshtein distance calculation.
        /// </summary>
        [Flags]
        public enum LevenshteinGoals
        {
            Minimize,
            Maximize
        }

        #endregion

        #region The two words

        /// <summary>
        /// The first word.
        /// </summary>
        public readonly String WordA;

        /// <summary>
        /// The length of the first word.
        /// </summary>
        private readonly UInt32 LengthWordA;

        /// <summary>
        /// The second word.
        /// </summary>
        public readonly String WordB;

        /// <summary>
        /// The length of the second word.
        /// </summary>
        private readonly UInt32 LengthWordB;

        #endregion

        #region Matrix initialization

        /// <summary>
        /// A delegate to init the matrix base line for the first word.
        /// </summary>
        private readonly Func<Char, Int32, Int32> MatrixInitWordA;

        /// <summary>
        /// A delegate to init the matrix base line for the second word.
        /// </summary>
        private readonly Func<Char, Int32, Int32> MatrixInitWordB;

        #endregion

        #region Cost funcs

        /// <summary>
        /// The insertion costs.
        /// </summary>
        private readonly Func<Char, Int32> InsertionCostFunc;

        /// <summary>
        /// The deletion costs.
        /// </summary>
        private readonly Func<Char, Int32> DeletionCostFunc;

        /// <summary>
        /// The substitution costs.
        /// </summary>
        private readonly Func<Char, Char, Int32> SubstitutionCostFunc;

        /// <summary>
        /// The transposition costs.
        /// </summary>
        private readonly Func<Char, Char, Int32> TranspositionCostFunc;

        /// <summary>
        /// Minimze or maximize the costs.
        /// </summary>
        private readonly LevenshteinGoals LevenshteinGoal;

        /// <summary>
        /// The costs will not get higher or lower than this value.
        /// </summary>
        private readonly Nullable<Int32> BorderCosts;

        #endregion

        #region Matrices

        /// <summary>
        /// The Levenshtein distance matrix.
        /// </summary>
        public readonly Int32[,] Matrix;

        /// <summary>
        /// The Levenshtein edit matrix.
        /// </summary>
        public readonly LevenshteinEdits[,] Edits;

        #endregion

        #endregion

        #region Constructor(s)

        #region Levenshtein(WordA, WordB, MatrixInitWordA = null, MatrixInitWordB = null,InsertionCostFunc = null, DeletionCostFunc = null, SubstitutionCostFunc = null, TranspositionCostFunc = null, BorderCosts = null, LevenshteinGoal = LevenshteinGoal.Minimize)

        /// <summary>
        /// Setup a Levenshtein matrix for the given words.
        /// </summary>
        /// <param name="WordA">The first word.</param>
        /// <param name="WordB">The second word.</param>
        /// <param name="MatrixInitWordA">A delegate to init the matrix base line for the first word. Set to zero if you want to get rid of prefixes or suffixes.</param>
        /// <param name="MatrixInitWordB">A delegate to init the matrix base line for the second word. Set to zero if you want to get rid of prefixes or suffixes.</param>
        /// <param name="DeletionCostFunc">The costs for deleting a character.</param>
        /// <param name="InsertionCostFunc">The costs for inserting a character.</param>
        /// <param name="SubstitutionCostFunc">The costs of substituting a character.</param>
        /// <param name="TranspositionCostFunc">The costs for the transposition of two adjacent letters.</param>
        /// <param name="BorderCosts">The costs might not get higher or lower than this value.</param>
        /// <param name="LevenshteinGoal">Minimze or maximize the costs.</param>
        public Levenshtein(String                   WordA,
                            String                   WordB,
                            Func<Char, Int32, Int32> MatrixInitWordA       = null,
                            Func<Char, Int32, Int32> MatrixInitWordB       = null,
                            Func<Char, Int32>        InsertionCostFunc     = null,
                            Func<Char, Int32>        DeletionCostFunc      = null,
                            Func<Char, Char, Int32>  SubstitutionCostFunc  = null,
                            Func<Char, Char, Int32>  TranspositionCostFunc = null,
                            Nullable<Int32>          BorderCosts           = null,
                            LevenshteinGoals         LevenshteinGoal       = LevenshteinGoals.Minimize)
        {

            #region Initial checks

            if (WordA == null)
                throw new ArgumentNullException("WordA", "The first word must not be null!");

            if (WordB == null)
                throw new ArgumentNullException("WordB", "The second word must not be null!");

            #endregion

            #region Setup the init and cost funcs

            this.MatrixInitWordA       = (MatrixInitWordA       != null) ? MatrixInitWordA       : (character, position) => position;
            this.MatrixInitWordB       = (MatrixInitWordB       != null) ? MatrixInitWordB       : (character, position) => position;

            this.InsertionCostFunc     = (InsertionCostFunc     != null) ? InsertionCostFunc     : (c)      => 1;
            this.DeletionCostFunc      = (DeletionCostFunc      != null) ? DeletionCostFunc      : (c)      => 1;
            this.SubstitutionCostFunc  = (SubstitutionCostFunc  != null) ? SubstitutionCostFunc  : (c1, c2) => { return (c1 == c2) ? 0 : 2; };
            this.TranspositionCostFunc = (TranspositionCostFunc != null) ? TranspositionCostFunc : (c1, c2) => { return (c1 == c2) ? 0 : 2; };

            this.BorderCosts           = (BorderCosts != null) ? BorderCosts : new Nullable<Int32>();

            #endregion

            #region Setup data

            this.WordA                 = WordA;
            this.WordB                 = WordB;
            this.LengthWordA           = (UInt32) WordA.Length;
            this.LengthWordB           = (UInt32) WordB.Length;
            this.Matrix                = new Int32[LengthWordA + 1, LengthWordB + 1];
            this.Edits                 = new LevenshteinEdits[LengthWordA + 1, LengthWordB + 1];
            this.LevenshteinGoal       = LevenshteinGoal;

            #endregion

            #region Init the matrix base lines

            Edits[0, 0] = LevenshteinEdits.DoNotBother;

            for (var i = 1; i <= LengthWordA; i++)
            {
                Matrix[i, 0] = this.MatrixInitWordA(WordA[i - 1], i);
                Edits [i, 0] = LevenshteinEdits.DoNotBother;
            }

            for (var j = 1; j <= LengthWordB; j++)
            {
                Matrix[0, j] = this.MatrixInitWordB(WordB[j - 1], j);
                Edits [0, j] = LevenshteinEdits.DoNotBother;
            }

            #endregion

        }

        #endregion

        #endregion


        #region (static) Create(WordA, WordB)

        /// <summary>
        /// Setup a Levenshtein matrix for the given words.
        /// </summary>
        /// <param name="WordA">The first word.</param>
        /// <param name="WordB">The second word.</param>
        public static Levenshtein Create(String WordA, String WordB)
        {
            return new Levenshtein(WordA, WordB);
        }

        #endregion

        #region (static) CreateAndCalc(WordA, WordB)

        /// <summary>
        /// Setup a Levenshtein matrix for the given words.
        /// </summary>
        /// <param name="WordA">The first word.</param>
        /// <param name="WordB">The second word.</param>
        public static Levenshtein CreateAndCalc(String WordA, String WordB)
        {
            var _Levenshtein = new Levenshtein(WordA, WordB);
            _Levenshtein.CalculateMinimumEditDistance();
            return _Levenshtein;
        }

        #endregion


        #region CalculateMinimumEditDistance()

        /// <summary>
        /// Calculate the minimum edit distance between the given words.
        /// </summary>
        public Int32 CalculateMinimumEditDistance()
        {
            return CalculateMinimumEditDistance(LengthWordA, LengthWordB);
        }

        #endregion

        #region CalculateMinimumEditDistance(i, j)

        /// <summary>
        /// Calculate the minimum edit distance between the given words.
        /// </summary>
        /// <param name="i">The position in the first word.</param>
        /// <param name="j">The position in the second word.</param>
        public Int32 CalculateMinimumEditDistance(UInt32 i, UInt32 j)
        {

            #region Initial checks

            if (i > LengthWordA)
                throw new ArgumentException("The first parameter is larger than the length of the first word!", "i");

            if (j > LengthWordB)
                throw new ArgumentException("The second parameter is larger than the length of the first word!", "j");

            #endregion

            if (Edits[i, j] != LevenshteinEdits.NotYet)
                return Matrix[i, j];

            var CharacterA = WordA[(Int32) i - 1];
            var CharacterB = WordB[(Int32) j - 1];

            // Deletion rule...
            var _DeletionCost     = CalculateMinimumEditDistance(i - 1, j) + DeletionCostFunc(CharacterA);

            // Insertion rule...
            var _InsertionCost    = CalculateMinimumEditDistance(i, j - 1) + InsertionCostFunc(CharacterB);

            // Substitution rule...
            var _SubstitutionCost = CalculateMinimumEditDistance(i - 1, j - 1) + SubstitutionCostFunc(CharacterA, CharacterB);

                
            // Find the minimum costs...
            var CurrentCosts = 0;
            Edits[i, j]      = LevenshteinEdits.NotYet;

            if (this.LevenshteinGoal == LevenshteinGoals.Minimize)
            {

                if (_DeletionCost     <= _InsertionCost && _DeletionCost    <= _SubstitutionCost)
                {
                    CurrentCosts  = _DeletionCost;
                    Edits[i, j]  |= LevenshteinEdits.Deletion;
                }

                if (_InsertionCost    <= _DeletionCost && _InsertionCost    <= _SubstitutionCost)
                {
                    CurrentCosts  = _InsertionCost;
                    Edits[i, j]  |= LevenshteinEdits.Insertion;
                }

                if (_SubstitutionCost <= _DeletionCost && _SubstitutionCost <= _InsertionCost)
                {
                    CurrentCosts  = _SubstitutionCost;
                    Edits[i, j]  |= LevenshteinEdits.Substitution;
                }

                Matrix[i, j] = (this.BorderCosts.HasValue && CurrentCosts > BorderCosts)
                                    ? BorderCosts.Value
                                    : CurrentCosts;

            }

            else
            {

                if (_DeletionCost     >= _InsertionCost && _DeletionCost    >= _SubstitutionCost)
                {
                    CurrentCosts  = _DeletionCost;
                    Edits[i, j]  |= LevenshteinEdits.Deletion;
                }

                if (_InsertionCost    >= _DeletionCost && _InsertionCost    >= _SubstitutionCost)
                {
                    CurrentCosts  = _InsertionCost;
                    Edits[i, j]  |= LevenshteinEdits.Insertion;
                }

                if (_SubstitutionCost >= _DeletionCost && _SubstitutionCost >= _InsertionCost)
                {
                    CurrentCosts  = _SubstitutionCost;
                    Edits[i, j]  |= LevenshteinEdits.Substitution;
                }

                Matrix[i, j] = (this.BorderCosts.HasValue && CurrentCosts < BorderCosts)
                                    ? BorderCosts.Value
                                    : CurrentCosts;

            }

            return Matrix[i, j];

        }

        #endregion


        #region ToString()

        /// <summary>
        /// Return a string representation of this object.
        /// </summary>
        public override String ToString()
        {

            var _StringBuilder = new StringBuilder("        ");

            foreach (var c in WordB)
                _StringBuilder.Append("    ").Append(c).Append(" ");

            _StringBuilder.AppendLine("").AppendLine("");

            for (var i = 0; i <= LengthWordA; i++)
            {

                if (i > 0)
                    _StringBuilder.Append(WordA[i - 1]).Append(" ");
                else
                    _StringBuilder.Append("  ");

                for (var j = 0; j <= LengthWordB; j++)
                {

                    if (Edits[i, j] != LevenshteinEdits.NotYet)
                    {

                        if ((Edits[i, j] & LevenshteinEdits.Substitution) == LevenshteinEdits.Substitution)
                            _StringBuilder.Append("\\");
                        else
                            _StringBuilder.Append(" ");

                        if ((Edits[i, j] & LevenshteinEdits.Insertion) == LevenshteinEdits.Insertion)
                            _StringBuilder.Append("<");
                        else
                            _StringBuilder.Append(" ");

                        if ((Edits[i, j] & LevenshteinEdits.Deletion) == LevenshteinEdits.Deletion)
                            _StringBuilder.Append("|");
                        else
                            _StringBuilder.Append(" ");

                        _StringBuilder.Append(Matrix[i, j].ToString().PadLeft(2, ' '));

                    }
                    else
                        _StringBuilder.Append("--");

                    _StringBuilder.Append(",");

                }
                    
                _StringBuilder.Length = _StringBuilder.Length - 1;
                _StringBuilder.AppendLine(";");

            }

            return _StringBuilder.ToString();

        }

        #endregion

    }

    #endregion

}
