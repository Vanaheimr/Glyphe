﻿/*
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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using de.ahzf.Styx;
using de.ahzf.Blueprints.PropertyGraphs;
using de.ahzf.Blueprints.PropertyGraphs.InMemory.Mutable;

#endregion

namespace de.ahzf.Glyphe
{

    public class GlypheGraph
    {

        private IPropertyGraph _TAGraph;

        public GlypheGraph()
        {
            _TAGraph = new PropertyGraph();
        }

        public void ReadFile(String Filename)
        {

            if (!File.Exists(Filename))
                throw new ArgumentNullException("The given Filename must not be a valid file!");

            var _SplitTokens  = new Char[] { ' ' };
            var _LineNumber   = 0UL;
            var _WordNumber   = 0UL;
            var _Pattern      = new Regex("[^a-zA-Z0-9]");
            var _FilteredWord = "";

            using (var _StreamReader = new StreamReader(Filename))
            {

                var _FileVertex = _TAGraph.AddVertex(v => v.SetProperty("Type", "file").
                                                            SetProperty("Name", Filename));

                foreach (var _Line in _StreamReader.GetLines())
                {

                    var _LineVertex = _TAGraph.AddVertex(v => v.SetProperty("Type",   "line").
                                                                SetProperty("Number", _LineNumber++).
                                                                SetProperty("Text",   _Line));

                    _TAGraph.AddEdge(_LineVertex, _FileVertex, "line2file");

                    _WordNumber = 0UL;

                    foreach (var _Word in _Line.Split(_SplitTokens, StringSplitOptions.RemoveEmptyEntries))
                    {

                        _FilteredWord = _Pattern.Replace(_Word, "").ToLower();

                        var _WVertex = _TAGraph.Vertices(vertex => ((String) vertex.GetProperty("Word")) == _FilteredWord).FirstOrDefault();
                        if (_WVertex == null)
                        {

                            var _NewWordVertex = _TAGraph.AddVertex(v => v.SetProperty("Type", "word").
                                                                           SetProperty("Word", _FilteredWord));

                            _TAGraph.AddEdge(_NewWordVertex, _LineVertex, "word2line", e => e.SetProperty("Position", _WordNumber++));

                        }
                        else
                        {
                            _TAGraph.AddEdge(_WVertex, _LineVertex, "word2line", e => e.SetProperty("Position", _WordNumber++));
                            //Console.WriteLine(_FilteredWord + " : " + _WVertex.OutEdges.Count());
                        }

                    }

                }

            }

        }

    }

}