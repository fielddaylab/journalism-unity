using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEditor;
using UnityEngine;

namespace Journalism {
    static public class TwineConversion {
        private const string TwineFilePath = "Assets/Content/Testing/Journalism Table Prototype.json";
        private const string LeafFilePath = "Assets/Content/Testing/Journalism Table Prototype.leaf";

        [MenuItem("Journalism/Convert Twine File %T")]
        static private void Menu_ConvertTwine() {
            // StringSlice twinePath = EditorUtility.OpenFilePanel("Select Exported Twine JSON File", string.Empty, "json");
            // if (twinePath.IsEmpty) {
            //     return;
            // }

            // StringSlice leafPath = EditorUtility.SaveFilePanel("Save Leaf File To...", string.Empty, Path.GetFileNameWithoutExtension(twinePath.ToString()), "leaf");
            // if (leafPath.IsEmpty) {
            //     return;
            // }

            ConvertToLeaf(TwineFilePath, LeafFilePath);
        }

        static public bool ConvertToLeaf(string twineFilePath, string leafFilePath) {
            try {
                JSON document;
                using (FileStream fileStream = File.OpenRead(twineFilePath)) {
                    document = JSON.Parse(fileStream);
                }
                using (FileStream writeStream = File.Create(leafFilePath))
                using (TextWriter write = new StreamWriter(writeStream)) {
                    if (!ConvertStoryToLeaf(document, write)) {
                        Log.Msg("[TwineConversion] Unable to convert from Twine file '{0}' to leaf", twineFilePath);
                        return false;
                    }
                }

                EditorUtility.OpenWithDefaultApp(leafFilePath);
                return true;
            } catch (Exception e) {
                Debug.LogException(e);
                Log.Msg("[TwineConversion] Unable to convert from Twine file '{0}' to leaf", twineFilePath);
                return false;
            }
        }

        static private bool ConvertStoryToLeaf(JSON document, TextWriter leafBuilder) {
            JSON storyNode = document;

            string name = storyNode["name"].AsString;
            string creator = storyNode["creator"].AsString;
            string startNodeId = storyNode["startnode"].AsString;

            leafBuilder.Write("// Name: ");
            leafBuilder.Write(name);
            leafBuilder.WriteLine();

            leafBuilder.Write("// Creator: ");
            leafBuilder.Write(creator);
            leafBuilder.WriteLine();

            leafBuilder.WriteLine();

            JSON passagesNode = storyNode["passages"];
            try {
                int total = passagesNode.Count;
                int current = 1;
                foreach (var node in passagesNode.Children) {
                    EditorUtility.DisplayProgressBar("Converting Twine to Leaf", string.Format("Current Node: {0} ({1}/{2})", node["name"].AsString, current, total), current / (float) total);
                    if (!ConvertNodeToLeaf(node, leafBuilder, startNodeId)) {
                        return false;
                    }
                    current++;
                }
            }
            finally {
                EditorUtility.ClearProgressBar();
            }

            // TODO: Implement
            return true;
        }

        static private bool ConvertNodeToLeaf(JSON element, TextWriter leafBuilder, string startNodeId) {
            string pid = element["pid"].AsString;
            string name = element["name"].AsString;
            JSON tags = element["tags"];
            string text = element["text"].AsString;

            if (IsAllUppercase(name)) {
                Debug.LogFormat("Skipping node '{0}': All uppercase title", name);
                return true;
            }

            string leafId = ProcessId(name);

            if (Array.IndexOf(SkipNodeIds, leafId) >= 0) {
                Debug.LogFormat("Skipping node '{0}': Marked as skip", name);
                return true;
            }

            if (pid == startNodeId) {
                leafBuilder.Write("# start ");
                leafBuilder.Write(leafId);
                leafBuilder.WriteLine();
                leafBuilder.WriteLine();
            }

            leafBuilder.Write("// Original Name: ");
            leafBuilder.Write(name);
            leafBuilder.WriteLine();

            leafBuilder.Write(":: ");
            leafBuilder.Write(leafId);
            leafBuilder.WriteLine();

            if (tags != null) {
                leafBuilder.Write("@tags ");
                bool first = true;
                foreach (var tag in tags.Children) {
                    if (!first) {
                        leafBuilder.Write(", ");
                    } else {
                        first = false;
                    }
                    leafBuilder.Write(tag.AsString);
                }
                leafBuilder.WriteLine();
            }

            Debug.LogFormat("Node {0}:\n{1}", leafId, text);

            text = ItalicsFormat.Replace(text, ItalicsReplace);
            Debug.LogFormat("Node {0} (Italics):\n{1}", leafId, text);
            text = BoldFormat.Replace(text, BoldReplace);
            Debug.LogFormat("Node {0} (Bold):\n{1}", leafId, text);

            text = ImageFormat.Replace(text, ImageReplace);
            Debug.LogFormat("Node {0} (Image):\n{1}", leafId, text);

            text = NumberComparisonFormat.Replace(text, NumberComparisonReplace);
            Debug.LogFormat("Node {0} (Number Comparison):\n{1}", leafId, text);
            text = IsCheckFormat.Replace(text, IsCheckReplace);
            Debug.LogFormat("Node {0} (is Check):\n{1}", leafId, text);
            text = HistoryVisitedFormat_Pre.Replace(text, HistoryVisitedReplace_Pre);
            Debug.LogFormat("Node {0} (Visited Pre):\n{1}", leafId, text);

            text = ReplaceIfs(text);
            Debug.LogFormat("Node {0} (If):\n{1}", leafId, text);
            text = ReplaceElseIf(text);
            Debug.LogFormat("Node {0} (ElseIf):\n{1}", leafId, text);
            text = ReplaceElse(text);
            Debug.LogFormat("Node {0} (Else):\n{1}", leafId, text);

            text = ReplaceLinks(text);
            Debug.LogFormat("Node {0} (Links):\n{1}", leafId, text);

            text = HistoryVisitedFormat_Post.Replace(text, HistoryVisitedReplace_Post);
            Debug.LogFormat("Node {0} (Visited Post):\n{1}", leafId, text);
            text = CurrentTimeRemainingFormat.Replace(text, CurrentTimeRemainingReplace);
            Debug.LogFormat("Node {0} (Less Time):\n{1}", leafId, text);
            text = MoreTimeRemainingFormat.Replace(text, MoreTimeRemainingReplace);
            Debug.LogFormat("Node {0} (More Time):\n{1}", leafId, text);

            text = IncrementFormat.Replace(text, IncrementReplace);
            Debug.LogFormat("Node {0} (Increment):\n{1}", leafId, text);
            text = DecrementFormat.Replace(text, DecrementReplace);
            Debug.LogFormat("Node {0} (Decrement):\n{1}", leafId, text);
            text = TwocrementFormat.Replace(text, TwocrementReplace);
            Debug.LogFormat("Node {0} (Twocrement):\n{1}", leafId, text);

            text = HubChoiceFormat.Replace(text, HubChoiceReplace);
            Debug.LogFormat("Node {0} (Hub Choice):\n{1}", leafId, text);
            text = DecreaseTimeFormat.Replace(text, DecreaseTimeReplace);
            Debug.LogFormat("Node {0} (Decrease Time):\n{1}", leafId, text);
            text = SetTimeFormat.Replace(text, SetTimeReplace);
            Debug.LogFormat("Node {0} (Set Time):\n{1}", leafId, text);
            text = AdjustStatFormat.Replace(text, AdjustStatReplace);
            Debug.LogFormat("Node {0} (Adjust Stat):\n{1}", leafId, text);
            text = GiveSnippetFormat.Replace(text, GiveSnippetReplace);
            Debug.LogFormat("Node {0} (Give Snippet):\n{1}", leafId, text);

            text = AdjustVarFormat.Replace(text, AdjustVarReplace);
            Debug.LogFormat("Node {0} (Adjust Variable):\n{1}", leafId, text);
            text = SimpleSetVarFormat.Replace(text, SimpleSetVarReplace);
            Debug.LogFormat("Node {0} (Set Variable):\n{1}", leafId, text);

            text = TechieFormat.Replace(text, TechieReplace);
            text = TrustFormat.Replace(text, TrustReplace);
            text = SocialFormat.Replace(text, SocialReplace);
            text = ResourcefulFormat.Replace(text, ResourcefulReplace);
            text = BookwormFormat.Replace(text, BookwormReplace);
            text = EnduranceFormat.Replace(text, EnduranceReplace);

            text = ComplexLinkRightFormat.Replace(text, ComplexLinkRightReplace);
            Debug.LogFormat("Node {0} (Complex Link Left):\n{1}", leafId, text);
            text = ComplexLinkLeftFormat.Replace(text, ComplexLinkLeftReplace);
            Debug.LogFormat("Node {0} (Complex Link Right):\n{1}", leafId, text);
            text = SimpleLinkFormat.Replace(text, SimpleLinkReplace);
            Debug.LogFormat("Node {0} (Simple Link):\n{1}", leafId, text);
            text = DisplayPassageFormat.Replace(text, DisplayPassageReplace);
            Debug.LogFormat("Node {0} (Display Passage):\n{1}", leafId, text);
            text = GotoPassageFormat.Replace(text, GotoPassageReplace);
            Debug.LogFormat("Node {0} (Goto Passage):\n{1}", leafId, text);

            leafBuilder.Write(text);
            leafBuilder.WriteLine();

            leafBuilder.WriteLine();
            return true;
        }

        static private string ProcessId(string twineName) {
            StringBuilder builder = new StringBuilder(twineName.Length);
            bool newWord = true;
            foreach (char src in twineName) {
                char target = src;
                if (char.IsLetterOrDigit(target)) {
                    if (char.IsLetter(target)) {
                        if (newWord) {
                            target = char.ToUpperInvariant(target);
                            newWord = false;
                        }
                    }

                    builder.Append(target);
                    newWord = false;
                } else if (target == '-' || target == '_' || target == '.') {
                    builder.Append(target);
                    newWord = true;
                } else {
                    if (target != '\'') {
                        newWord = true;
                    }
                }
            }
            builder.TrimEnd(IdTrimChars);
            return builder.Flush();
        }

        static private readonly char[] IdTrimChars = new char[] { '.', '-', '_', '\'' };

        static private readonly string[] SkipNodeIds = new string[] {
            "SubmitStory", "JudgeSnippet", "StoryTypeMultiplier"
        };

        #region Set

        static private Regex AdjustVarFormat = new Regex(@"^\(set: \$(\w+) to it (.+)\)", RegexOptions.Multiline);
        static private MatchEvaluator AdjustVarReplace = (m) => {
            return string.Format("$set {0} {1}", m.Groups[1].Value, m.Groups[2].Value);
        };

        static private Regex SimpleSetVarFormat = new Regex(@"^\(set: \$(\w+) to (.+)\)", RegexOptions.Multiline);
        static private MatchEvaluator SimpleSetVarReplace = (m) => {
            return string.Format("$set {0} = {1}", m.Groups[1].Value, m.Groups[2].Value);
        };

        #endregion // Set

        #region Expressions

        static private Regex NumberComparisonFormat = new Regex("is\\s+([><=]{1,2})(\\d+)");
        static private MatchEvaluator NumberComparisonReplace = (m) => {
            return string.Format("{0} {1}", m.Groups[1].Value, m.Groups[2].Value);
        };

        #endregion // Expressions

        #region Links

        static private Regex ComplexLinkRightFormat = new Regex(@"\[\[\[?(.+?)->(.+?)\]\]\]?");
        static private MatchEvaluator ComplexLinkRightReplace = (m) => {
            string text = m.Groups[1].Value;
            string target = m.Groups[2].Value;
            target = ProcessId(target);
            return string.Format("$choice {0}; {1}", target, text);
        };

        static private Regex ComplexLinkLeftFormat = new Regex(@"\[\[\[?(.+?)<-(.+?)\]\]\]?");
        static private MatchEvaluator ComplexLinkLeftReplace = (m) => {
            string text = m.Groups[2].Value;
            string target = m.Groups[1].Value;
            target = ProcessId(target);
            return string.Format("$choice {0}; {1}", target, text);
        };

        static private Regex SimpleLinkFormat = new Regex(@"\[\[\[?(.+?)\]\]\]?");
        static private MatchEvaluator SimpleLinkReplace = (m) => {
            string text = m.Groups[1].Value;
            string target = ProcessId(text);
            return string.Format("$choice {0}; {1}", target, text);
        };

        #endregion // Links

        #region Images

        static private Regex ImageFormat = new Regex("<\\s*img\\s+src=\"https://journalism-game-master\\.netlify\\.app/assets/(.*?)\".*?\\/>");
        static private MatchEvaluator ImageReplace = (m) => {
            string target = m.Groups[1].Value;
            return string.Format("{{image Photo/{0}}}", target);
        };

        #endregion // Images

        #region Display/Goto

        static private Regex DisplayPassageFormat = new Regex("^\\(display: \"?(.+?)\"?\\)", RegexOptions.Multiline);
        static private MatchEvaluator DisplayPassageReplace = (m) => {
            string target = ProcessId(m.Groups[1].Value);
            return string.Format("$branch {0}", target);
        };

        static private Regex GotoPassageFormat = new Regex("^\\(goto: \"?(.+?)\"?\\)");
        static private MatchEvaluator GotoPassageReplace = (m) => {
            string target = ProcessId(m.Groups[1].Value);
            return string.Format("$goto {0}", target);
        };

        #endregion // Display/Goto

        #region Is

        static private Regex IsCheckFormat = new Regex("\\bis\\b\\s*(\\d+)\\)");
        static private MatchEvaluator IsCheckReplace = (m) => {
            return string.Format("== {0}", m.Groups[1].Value);
        };

        #endregion // Is

        #region Macros and Functions

        static private Regex HistoryVisitedFormat_Pre = new Regex("\\(history:\\)\\s*contains \"([^\"]+?)\"");
        static private MatchEvaluator HistoryVisitedReplace_Pre = (m) => {
            string target = ProcessId(m.Groups[1].Value);
            return string.Format("Visited\"{0}\"", target);
        };

        static private Regex HistoryVisitedFormat_Post = new Regex("Visited\"(.*)\"");
        static private MatchEvaluator HistoryVisitedReplace_Post = (m) => {
            string target = m.Groups[1].Value;
            return string.Format("Visited({0})", target);
        };

        static private Regex CurrentTimeRemainingFormat = new Regex("\\$currentTime\\s+is\\s+<=?(\\d+)");
        static private MatchEvaluator CurrentTimeRemainingReplace = (m) => {
            float time = StringParser.ParseFloat(m.Groups[1].Value);
            return string.Format("!HasTime({0})", time);
        };

        static private Regex MoreTimeRemainingFormat = new Regex("\\$currentTime\\s+is\\s+>=?(\\d+)");
        static private MatchEvaluator MoreTimeRemainingReplace = (m) => {
            float time = StringParser.ParseFloat(m.Groups[1].Value);
            return string.Format("HasTime({0})", time);
        };

        static private Regex HubChoiceFormat = new Regex("\\(\\$hubChoice:\\s*\"(.+?)\",\\s*\"(.+?)\",\\s*(\\d+)\\)");
        static private MatchEvaluator HubChoiceReplace = (m) => {
            string target = ProcessId(m.Groups[1].Value);
            string text = m.Groups[2].Value;
            float time = StringParser.ParseFloat(m.Groups[3].Value) / 60f;
            return string.Format("$HubChoice({0}, {1}, {2})", target, text, time);
        };

        static private Regex AdjustStatFormat = new Regex("\\(\\$changeStat:\\s*\\$(\\w+),\\s*(-*)(\\d+)\\)");
        static private MatchEvaluator AdjustStatReplace = (m) => {
            string name = string.Empty; ;
            switch (m.Groups[1].Value) {
                case "techieName": {
                        name = "Tech";
                        break;
                    }
                case "socialName": {
                        name = "Social";
                        break;
                    }
                case "resourcefulName": {
                        name = "Resourceful";
                        break;
                    }
                case "enduranceName": {
                        name = "Endurance";
                        break;
                    }
                case "trustName": {
                        name = "Trust";
                        break;
                    }
                case "bookwormName": {
                        name = "Research";
                        break;
                    }
            }

            string op = m.Groups[2].Value;
            string val = m.Groups[3].Value;

            if (op == "-") {
                return string.Format("$call AdjustStats({0} - {1})", name, val);
            } else {
                return string.Format("$call AdjustStats({0} + {1})", name, val);
            }
        };

        static private Regex SetTimeFormat = new Regex("\\(set: \\$currentTime to (\\d+)\\)");
        static private MatchEvaluator SetTimeReplace = (m) => {
            return string.Format("$call SetTimeRemaining({0})", StringParser.ParseFloat(m.Groups[1].Value) / 60f);
        };

        static private Regex DecreaseTimeFormat = new Regex("\\(set\\s*:\\s*\\$currentTime to it\\s*-\\s*(\\d+)\\s*\\)");
        static private MatchEvaluator DecreaseTimeReplace = (m) => {
            return string.Format("$call DecreaseTime({0})", StringParser.ParseFloat(m.Groups[1].Value) / 60f);
        };

        static private Regex GiveSnippetFormat = new Regex("\\(\\$gainSnippet:\\s*\"\\w+\",\\s*\"([\\w\\s]+)\"\\)");
        static private MatchEvaluator GiveSnippetReplace = (m) => {
            string target = ProcessId(m.Groups[1].Value);
            return string.Format("$call GiveSnippet({0})", target);
        };

        static private Regex IncrementFormat = new Regex("\\$increment\\[.*\\]\n");
        static private string IncrementReplace = "";

        static private Regex DecrementFormat = new Regex("\\$decrement\\[.*\\]\n");
        static private string DecrementReplace = "";

        static private Regex TwocrementFormat = new Regex("\\$2crement\\[.*\\]\n");
        static private string TwocrementReplace = "";

        #endregion // Macros and Functions

        #region Stats

        static private Regex TechieFormat = new Regex("\\$techie\\b");
        static private string TechieReplace = "Stat(Tech)";

        static private Regex ResourcefulFormat = new Regex("\\$resourceful\\b");
        static private string ResourcefulReplace = "Stat(Resourceful)";

        static private Regex SocialFormat = new Regex("\\$social\\b");
        static private string SocialReplace = "Stat(Social)";

        static private Regex TrustFormat = new Regex("\\$trust\\b");
        static private string TrustReplace = "Stat(Trust)";

        static private Regex BookwormFormat = new Regex("\\$bookworm\\b");
        static private string BookwormReplace = "Stat(Research)";

        static private Regex EnduranceFormat = new Regex("\\$endurance\\b");
        static private string EnduranceReplace = "Stat(Endurance)";

        #endregion // Stats

        #region Text Formatting

        static private Regex ItalicsFormat = new Regex("//([\\s\\S]+?)//", RegexOptions.Multiline);
        static private MatchEvaluator ItalicsReplace = (m) => {
            return string.Format("<i>{0}</i>", m.Groups[1].Value);
        };

        static private Regex BoldFormat = new Regex("''([\\s\\S]+?)''", RegexOptions.Multiline);
        static private MatchEvaluator BoldReplace = (m) => {
            return string.Format("<b>{0}</b>", m.Groups[1].Value);
        };

        #endregion // Text Formatting

        #region If

        static private Regex FindIfStart = new Regex("\\(if\\s*:\\s*([\\w\\d\\(\\)\" ,/!\\$><=]*)[\\b]?\\s*\\)\\s*\\[?", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        static private Regex FindElseIfContinue = new Regex("\\$endif\\s*\\(elseif\\s*:\\s*([\\w\\d\\(\\)\" ,/!\\$><=]*)[\\b]?\\s*\\)\\s*\\[?", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        static private Regex FindElseIfErrant = new Regex("\\(elseif\\s*:\\s*([\\w\\d\\(\\)\" ,/!\\$><=]*)[\\b]?\\s*\\)\\s*\\[?", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        static private Regex FindElseStart = new Regex("\\$endif\\s*\\(else\\s*:\\s*\\)\\s*\\[?", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        static private string ReplaceIfs(string source) {
            Match m = FindIfStart.Match(source);
            while(m != null && m.Success) {
                int groupStart = m.Index + m.Length - 1;
                int groupEnd;
                int endLength = 0;
                if (source[groupStart] == '[') {
                    groupEnd = FindBlockEnd(source, groupStart, out endLength);
                } else {
                    groupEnd = FindLineBreak(source, groupStart, out endLength);
                }
                string comparison = m.Groups[1].Value;
                StringSlice contents = new StringSlice(source, groupStart + 1, (groupEnd - 1 - groupStart)).Trim();
                if (groupEnd + endLength >= source.Length) {
                    source = source.Substring(0, m.Index) + string.Format("\n$if {0}\n{1}\n$endif\n", comparison, contents);
                } else {
                    source = source.Substring(0, m.Index) + string.Format("\n$if {0}\n{1}\n$endif\n", comparison, contents) + source.Substring(groupEnd + endLength);
                }
                m = FindIfStart.Match(source);
            }
            return source;
        }

        static private string ReplaceElseIf(string source) {
            Match m = FindElseIfContinue.Match(source);
            while(m != null && m.Success) {
                int groupStart = m.Index + m.Length - 1;
                int groupEnd;
                int endLength = 0;
                if (source[groupStart] == '[') {
                    groupEnd = FindBlockEnd(source, groupStart, out endLength);
                } else {
                    groupEnd = FindLineBreak(source, groupStart, out endLength);
                }
                string comparison = m.Groups[1].Value;
                StringSlice contents = new StringSlice(source, groupStart + 1, (groupEnd - 1 - groupStart)).Trim();
                if (groupEnd + endLength >= source.Length) {
                    source = source.Substring(0, m.Index) + string.Format("$elseif {0}\n{1}\n$endif\n", comparison, contents);
                } else {
                    source = source.Substring(0, m.Index) + string.Format("$elseif {0}\n{1}\n$endif\n", comparison, contents) + source.Substring(groupEnd + endLength);
                }
                m = FindElseIfContinue.Match(source);
            }

            m = FindElseIfErrant.Match(source);
            while(m != null && m.Success) {
                int groupStart = m.Index + m.Length - 1;
                int groupEnd;
                int endLength = 0;
                if (source[groupStart] == '[') {
                    groupEnd = FindBlockEnd(source, groupStart, out endLength);
                } else {
                    groupEnd = FindLineBreak(source, groupStart, out endLength);
                }
                string comparison = m.Groups[1].Value;
                StringSlice contents = new StringSlice(source, groupStart + 1, (groupEnd - 1 - groupStart)).Trim();
                if (groupEnd + endLength >= source.Length) {
                    source = source.Substring(0, m.Index) + string.Format("\n$if {0}\n{1}\n$endif\n", comparison, contents);
                } else {
                    source = source.Substring(0, m.Index) + string.Format("\n$if {0}\n{1}\n$endif\n", comparison, contents) + source.Substring(groupEnd + endLength);
                }
                m = FindElseIfErrant.Match(source);
            }
            return source;
        }

        static private string ReplaceElse(string source) {
            Match m = FindElseStart.Match(source);
            while(m != null && m.Success) {
                int groupStart = m.Index + m.Length - 1;
                int groupEnd;
                int endLength = 0;
                if (source[groupStart] == '[') {
                    groupEnd = FindBlockEnd(source, groupStart, out endLength);
                } else {
                    groupEnd = FindLineBreak(source, groupStart, out endLength);
                }
                StringSlice contents = new StringSlice(source, groupStart + 1, (groupEnd - 1 - groupStart)).Trim();
                if (groupEnd + endLength >= source.Length) {
                    source = source.Substring(0, m.Index) + string.Format("$else\n{0}\n$endif\n", contents);
                } else {
                    source = source.Substring(0, m.Index) + string.Format("$else\n{0}\n$endif\n", contents) + source.Substring(groupEnd + endLength);
                }
                m = FindElseStart.Match(source);
            }
            return source;
        }

        #endregion // If

        #region Link

        static private Regex LinkStartFormat = new Regex("\\(link(?:-reveal)?\\s*:\\s*([\\w\"\\s'-]*)\\)\\[?", RegexOptions.IgnoreCase);

        static private string ReplaceLinks(string source) {
            Match m = LinkStartFormat.Match(source);
            while(m != null && m.Success) {
                int groupStart = m.Index + m.Length - 1;
                int groupEnd;
                int endLength = 0;
                if (source[groupStart] == '[') {
                    groupEnd = FindBlockEnd(source, groupStart, out endLength);
                } else {
                    groupEnd = FindLineBreak(source, groupStart, out endLength);
                }
                string text = m.Groups[1].Value;
                if (groupEnd + endLength < source.Length) {
                    source = source.Remove(groupEnd, 1);
                }
                source = source.Substring(0, m.Index) + string.Format("\n{{@action}} {0}\n{1}", text, source.Substring(groupStart + 1));
                m = FindIfStart.Match(source);
            }
            return source;
        }

        #endregion // Link

        static private int FindBlockEnd(string source, int startIdx, out int outEndLength) {
            int depth = 0;
            bool quoteMode = false;
            char c;
            for (int i = startIdx; i < source.Length; i++) {
                c = source[i];
                switch (c) {
                    case '[':
                        if (!quoteMode) {
                            depth++;
                        }
                        break;

                    case ']':
                        if (!quoteMode) {
                            depth--;
                            if (depth <= 0) {
                                outEndLength = 1;
                                return i;
                            }
                        }
                        break;
                    
                    case '$': {
                        if (!quoteMode) {
                            if (source.AttemptMatch(i, "$endif")) {
                                depth--;
                                if (depth <= 0) {
                                    outEndLength = 6;
                                    return i;
                                }
                            } else if (source.AttemptMatch(i, "$if")) {
                                depth++;
                                i += 2;
                            }
                        }
                        break;
                    }

                    case '"':
                        if (quoteMode) {
                            if (i > 0 && source[i - 1] == '\\') {
                                i++;
                                break;
                            } else {
                                quoteMode = !quoteMode;
                            }
                        } else {
                            quoteMode = true;
                        }
                        break;
                }
            }

            Debug.LogErrorFormat("Could not find closing bracket in {0} (depth {1}, quote {2})", source.Substring(startIdx), depth, quoteMode);
            outEndLength = 0;
            return source.Length;
        }

        static private int FindLineBreak(string source, int startIdx, out int endLength) {
            int idx = source.IndexOf('\n', startIdx);
            if (idx < 0) {
                endLength = 0;
                return source.Length;
            } else {
                endLength = 1;
                return idx;
            }
        }

        static private string EscapeRegex(string pattern) {
            string regex = Regex.Escape(pattern);
            if (pattern.Length > 0 && !char.IsLetterOrDigit(pattern[0]) && regex[0] == '\\') {
                regex = regex.Substring(1);
            }
            return regex;
        }

        static private bool IsAllUppercase(string source) {
            foreach(var c in source) {
                if (char.IsLetter(c)) {
                    if (!char.IsUpper(c)) {
                        return false;
                    }
                } else if (!char.IsWhiteSpace(c)) {
                    return false;
                }
            }
            return true;
        }
    }
}