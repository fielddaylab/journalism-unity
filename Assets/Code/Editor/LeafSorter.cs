using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEditor;

namespace Journalism {
    static public class LeafSorter {
        static private readonly string[] RootNodes = new string[] {
            "START", "Story2Intro", "Level2Story3TheScienceStory", "ScienceStory2Wetlands", "Level3Intro", "Level3Story2"
        };

        private const string OutputPath = "Assets/Content/Level0{0}/Level{0}";

        private class State {
            public readonly Dictionary<StringHash32, Node> AllNodes = new Dictionary<StringHash32, Node>();
            public readonly HashSet<StringHash32> VisitedSet = new HashSet<StringHash32>();
            public string Prefix;
            public readonly StringBuilder Builder = new StringBuilder(2048);
            public readonly RingBuffer<StringHash32> VisitQueue = new RingBuffer<StringHash32>(32, RingBufferMode.Expand);
        }

        private class OutputFile {
            public readonly List<Node> Nodes = new List<Node>();
            public readonly List<Node> Snippets = new List<Node>();
        }

        private class Node {
            public string Id;
            public string Text;
            public bool IsSnippet;
        }

        [MenuItem("Journalism/Split Leaf File")]
        static public void Process() {
            try
            {
                EditorUtility.DisplayProgressBar("Splitting Leaf file into levels and scraps", "Reading Leaf File", 0);
                string fileText = File.ReadAllText(TwineConversion.LeafFilePath);
                State state = new State();
                EditorUtility.DisplayProgressBar("Splitting Leaf file into levels and scraps", "Parsing Leaf File", 0.1f);
                ReadAllNodes(fileText, state);
                string revealFile = null;
                float progressRatio = 1f / RootNodes.Length;
                for(int i = 0; i < RootNodes.Length; i++) {
                    OutputFile file = new OutputFile();
                    EditorUtility.DisplayProgressBar("Splitting Leaf file into levels and scraps", "Traversing Nodes for Level " + (i + 1), 0.1f + i * progressRatio);
                    Traverse(RootNodes[i], state, file);
                    EditorUtility.DisplayProgressBar("Splitting Leaf file into levels and scraps", "Writing Files for Level " + (i + 1), 0.1f + (i + 0.5f) * progressRatio);
                    WriteToFile(file, string.Format(OutputPath, i + 1), state.Prefix, ref revealFile);
                }

                if (revealFile != null) {
                    EditorUtility.RevealInFinder(revealFile);
                }
            }
            finally {
                EditorUtility.ClearProgressBar();
            }
        }

        static private void ReadAllNodes(string fileText, State state) {
            Node currentNode = null;
            foreach(var line in fileText.Split('\n')) {
                if (line.StartsWith("// Original Name: ")) {
                    if (currentNode != null) {
                        currentNode.Text = state.Builder.Flush().TrimEnd('\n');
                        state.AllNodes.Add(currentNode.Id, currentNode);
                    } else {
                        state.Prefix = state.Builder.Flush().TrimEnd();
                    }

                    currentNode = new Node();
                } else if (line.StartsWith(":: ")) {
                    string id = line.Substring(3);
                    if (currentNode != null && currentNode.Id != null) {
                        currentNode.Text = state.Builder.Flush().TrimEnd('\n');
                        state.AllNodes.Add(currentNode.Id, currentNode);
                        currentNode = new Node();
                    } else if (currentNode == null) {
                        state.Prefix = state.Builder.Flush().TrimEnd();
                        currentNode = new Node();
                    }

                    currentNode.Id = id;
                }
                
                state.Builder.Append(line).Append('\n');
            }

            if (state.Builder.Length > 0) {
                if (currentNode != null) {
                    currentNode.Text = state.Builder.Flush().TrimEnd('\n');
                    state.AllNodes.Add(currentNode.Id, currentNode);
                } else {
                    state.Prefix = state.Builder.Flush().TrimEnd();
                }
            }
        }

        static private void Traverse(string rootId, State state, OutputFile output) {
            state.VisitQueue.Clear();
            state.VisitedSet.Clear();

            state.VisitQueue.PushBack(rootId);

            StringHash32 id;
            Node node;
            while(state.VisitQueue.TryPopFront(out id)) {
                if (id != rootId) {
                    if (ArrayUtils.Contains(RootNodes, id.ToDebugString())) {
                        continue;
                    }
                }

                if (!state.VisitedSet.Add(id)) {
                    continue;
                }

                if (!state.AllNodes.TryGetValue(id, out node)) {
                    Log.Error("[LeafSorter] Referenced node '{0}' is not present", id);
                    continue;
                }

                output.Nodes.Add(node);

                foreach(Match match in SnippetFinder.Matches(node.Text)) {
                    string snippetId = match.Groups[1].Value;
                    AddSnippet(snippetId, state, output);
                }

                foreach(Match match in RegularLinkFinder.Matches(node.Text)) {
                    StringHash32 nextId = match.Groups[1].Value;
                    if (!state.VisitedSet.Contains(nextId)) {
                        state.VisitQueue.PushBack(nextId);
                    }
                }

                foreach(Match match in MacroLinkFinder.Matches(node.Text)) {
                    StringHash32 nextId = match.Groups[1].Value;
                    if (!state.VisitedSet.Contains(nextId)) {
                        state.VisitQueue.PushBack(nextId);
                    }
                }
            }
        }

        static private void AddSnippet(string snippetId, State state, OutputFile output) {
            if (!state.VisitedSet.Add(snippetId)) {
                return;
            }

            StringHash32 id = snippetId;
            Node node;
            if (!state.AllNodes.TryGetValue(snippetId, out node)) {
                Log.Error("[LeafSorter] Referenced snippet '{0}' is not present", id);
                return;
            }

            output.Snippets.Add(node);

            if (!node.IsSnippet) {
                node.IsSnippet = true;
                node.Text = SnippetTagFinder.Replace(node.Text, SnippetTagReplace);
                node.Text = SnippetImageFinder.Replace(node.Text, SnippetImageReplace);
            }
        }

        static private void WriteToFile(OutputFile output, string fileName, string prefix, ref string fileToOpen) {
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            string leafFileName = fileName + ".leaf";
            if (fileToOpen == null) {
                fileToOpen = leafFileName;
            }

            using (FileStream writeStream = File.Create(leafFileName))
            using (TextWriter write = new StreamWriter(writeStream)) {
                write.Write(prefix);
                write.WriteLine();
                write.WriteLine();

                write.Write("# start ");
                write.Write(output.Nodes[0].Id);
                write.WriteLine();
                write.WriteLine();

                foreach(var node in output.Nodes) {
                    write.Write(node.Text);
                    write.WriteLine();
                    write.WriteLine();
                }
            }

            using (FileStream writeStream = File.Create(fileName + ".scraps"))
            using (TextWriter write = new StreamWriter(writeStream)) {
                foreach(var snippet in output.Snippets) {
                    write.Write(snippet.Text);
                    write.WriteLine();
                    write.WriteLine();
                }
            }
        }

        static private Regex SnippetFinder = new Regex("\\$call GiveSnippet\\(([\\w\\d\\+\\-_\\.]+)\\)");
        static private Regex RegularLinkFinder = new Regex("\\$(?:choice|goto|branch) ([\\w\\d\\+_\\-\\.]+)\\b");
        static private Regex MacroLinkFinder = new Regex("\\$(?:Hub|Time|Once)*Choice\\(([\\w\\d\\+\\-_\\.]+)\\b");

        static private Regex SnippetTagFinder = new Regex("@tags ([\\w\\s,]+)\\s*,\\s*(\\w+)[$\\n]");
        static private MatchEvaluator SnippetTagReplace = (m) => {
            string type = m.Groups[1].Value;
            string quality = m.Groups[2].Value;
            if (Enum.TryParse<StoryScrapQuality>(type, false, out var _)) {
                Ref.Swap(ref type, ref quality);
            }
            return string.Format("@type {0}\n@quality {1}\n", type, quality);
        };

        static private Regex SnippetImageFinder = new Regex("\\{img ([\\w\\d\\.\\-\\/]+)\\}");
        static private MatchEvaluator SnippetImageReplace = (m) => {
            return string.Format("@image {0}", m.Groups[1].Value);
        };
    }
}