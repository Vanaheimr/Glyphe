/*
 * Copyright (c) 2010-2012, Achim 'ahzf' Friedland <code@ahzf.de>
 * This file is part of Glyphe <http://www.github.com/ahzf/Glyphe>
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
using System.Collections.Generic;

using de.ahzf.Glyphe;

#endregion

namespace TestApplication1
{
    public class Program
    {

        public static void Main(String[] args)
        {

            var X = "INTENTION"; var n = X.Length;
            var Y = "EXECUTION"; var m = Y.Length;

            // Edit distance between X and Y is thus D(n,m);

            //var LSD1 = new Levenshtein(X, Y);
            //LSD1.CalculateMinimumEditDistance();
            //Console.WriteLine(LSD1);

            //var LSD2 = new Levenshtein("comb", "love");
            //LSD2.CalculateMinimumEditDistance();
            //Console.WriteLine(LSD2);

            //var LSD3 = new Levenshtein("bike", "back");
            //LSD3.CalculateMinimumEditDistance();
            //Console.WriteLine(LSD3);

            var LSD4 = NeedlemanWunsch.Create("ACGTC", "AGTC");
            LSD4.CalculateMinimumEditDistance();
            Console.WriteLine(LSD4);

            var LSD5 = NeedlemanWunsch.Create("ATC", "AATC", 1, (c1, c2) => { return (c1 == c2) ? 0 : -2; });
            LSD5.CalculateMinimumEditDistance();
            Console.WriteLine(LSD5);


            var LSD6 = new Levenshtein("ATCAT", "ATTATC",
                                       MatrixInitWordA:      (character, position) => 0,
                                       MatrixInitWordB:      (character, position) => 0,
                                       InsertionCostFunc:    (character) => -1,
                                       DeletionCostFunc:     (character) => -1,
                                       SubstitutionCostFunc: (c1, c2) => { return (c1 == c2) ? 1 : -1; },
                                       BorderCosts:          0,
                                       LevenshteinGoal:      Levenshtein.LevenshteinGoals.Maximize);

            LSD6.CalculateMinimumEditDistance();
            Console.WriteLine(LSD6);


            Console.WriteLine(Levenshtein.CreateAndCalc("bane", "barn"));
            Console.WriteLine(Levenshtein.CreateAndCalc("vase", "cave"));


            var _TA = new GlypheGraph();
            _TA.ReadFile("TestText.txt");

        }

    }
}
