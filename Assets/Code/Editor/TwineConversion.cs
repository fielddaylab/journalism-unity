using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEditor;
using UnityEngine;

namespace Journalism {
    static public class TwineConversion {
        [MenuItem("Journalism/Convert Twine File")]
        static private void Menu_ConvertTwine() {
            StringSlice twinePath = EditorUtility.OpenFilePanel("Select Twine Proofing Copy", string.Empty, "html");
            if (twinePath.IsEmpty) {
                return;
            }

            StringSlice leafPath = EditorUtility.SaveFilePanel("Save Leaf File To...", string.Empty, Path.GetFileNameWithoutExtension(twinePath.ToString()), "leaf");
            if (leafPath.IsEmpty) {
                return;
            }

            ConvertToLeaf(twinePath.ToString(), leafPath.ToString());
        }

        static public bool ConvertToLeaf(string twineFilePath, string leafFilePath) {
            try {
                XmlDocument document = new XmlDocument();
                using(FileStream fileStream = File.OpenRead(twineFilePath)) {
                    document.Load(fileStream);
                }
                using(FileStream writeStream = File.Create(leafFilePath))
                using(TextWriter write = new StreamWriter(writeStream)) {
                    if (!ConvertStoryToLeaf(document, write)) {
                        Log.Msg("[TwineConversion] Unable to convert from Twine file '{0}' to leaf", twineFilePath);
                        return false;
                    }
                }

                EditorUtility.OpenWithDefaultApp(leafFilePath);
                return true;
            } catch(Exception e) {
                Debug.LogException(e);
                Log.Msg("[TwineConversion] Unable to convert from Twine file '{0}' to leaf", twineFilePath);
                return false;
            }
        }

        static private bool ConvertStoryToLeaf(XmlDocument document, TextWriter leafBuilder) {
            XmlElement storyNode = document.DocumentElement["body"]["tw-storydata"];

            string name = storyNode.GetAttribute("name");
            string creator = storyNode.GetAttribute("creator");
            string format = storyNode.GetAttribute("format");
            string startNodeId = storyNode.GetAttribute("startnode");

            if (format != "Harlowe") {
                Log.Msg("[TwineConversion] Format '{0}' is unsupported - only Harlowe", format);
                return false;
            }

            leafBuilder.Write("// Name: ");
            leafBuilder.Write(name);
            leafBuilder.WriteLine();

            leafBuilder.Write("// Creator: ");
            leafBuilder.Write(creator);
            leafBuilder.WriteLine();

            leafBuilder.WriteLine();

            foreach(var node in storyNode.GetElementsByTagName("tw-passagedata")) {
                if (!ConvertNodeToLeaf((XmlElement) node, leafBuilder, startNodeId)) {
                    return false;
                }
            }

            // TODO: Implement
            return true;
        }

        static private bool ConvertNodeToLeaf(XmlElement element, TextWriter leafBuilder, string startNodeId) {
            string pid = element.GetAttribute("pid");
            string name = element.GetAttribute("name");
            string tags = element.GetAttribute("tags");
            tags = tags.Replace(" ", ", ");

            string leafId = ProcessId(name);

            if (pid == startNodeId) {
                leafBuilder.Write("# start ");
                leafBuilder.Write(leafId);
                leafBuilder.WriteLine();
                leafBuilder.WriteLine();
            }

            string text = element.InnerText;

            leafBuilder.Write("// Original Name: ");
            leafBuilder.Write(name);
            leafBuilder.WriteLine();

            leafBuilder.Write(":: ");
            leafBuilder.Write(leafId);
            leafBuilder.WriteLine();

            if (tags.Length > 0) {
                leafBuilder.Write("@tags ");
                leafBuilder.Write(tags);
                leafBuilder.WriteLine();
            }

            text = ItalicsFormat.Replace(text, ItalicsReplace);
            text = BoldFormat.Replace(text, BoldReplace);

            text = ComplexLinkRightFormat.Replace(text, ComplexLinkRightReplace);
            text = ComplexLinkLeftFormat.Replace(text, ComplexLinkLeftReplace);
            text = SimpleLinkFormat.Replace(text, SimpleLinkReplace);
            text = DisplayPassageFormat.Replace(text, DisplayPassageReplace);
            text = GotoPassageFormat.Replace(text, GotoPassageReplace);

            text = HubChoiceFormat.Replace(text, HubChoiceReplace);
            text = HistoryVisitedFormat.Replace(text, HistoryVisitedReplace);
            text = DecreaseTimeFormat.Replace(text, DecreaseTimeReplace);
            text = SetTimeFormat.Replace(text, SetTimeReplace);
            text = AdjustStatFormat.Replace(text, AdjustStatReplace);
            text = GiveSnippetFormat.Replace(text, GiveSnippetReplace);

            text = IsCheckFormat.Replace(text, IsCheckReplace);
            
            text = AdjustVarFormat.Replace(text, AdjustVarReplace);
            text = SimpleSetVarFormat.Replace(text, SimpleSetVarReplace);

            text = TechieFormat.Replace(text, TechieReplace);
            text = TrustFormat.Replace(text, TrustReplace);
            text = SocialFormat.Replace(text, SocialReplace);
            text = ResourcefulFormat.Replace(text, ResourcefulReplace);
            text = BookwormFormat.Replace(text, BookwormReplace);
            text = EnduranceFormat.Replace(text, EnduranceReplace);

            leafBuilder.Write(text);
            leafBuilder.WriteLine();

            leafBuilder.WriteLine();
            return true;
        }

        static private string ProcessId(string twineName) {
            StringBuilder builder = new StringBuilder(twineName.Length);
            bool newWord = true;
            foreach(char src in twineName) {
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

        static private readonly char[] IdTrimChars = new char[] { '.', '-', '_' };

        static private Regex AdjustVarFormat = new Regex(@"^\(set: \$(\w+) to it (.+)\)", RegexOptions.Multiline);
        static private MatchEvaluator AdjustVarReplace = (m) => {
            return string.Format("$set {0} {1}", m.Groups[1].Value, m.Groups[2].Value);
        };

        static private Regex SimpleSetVarFormat = new Regex(@"^\(set: \$(\w+) to (.+)\)", RegexOptions.Multiline);
        static private MatchEvaluator SimpleSetVarReplace = (m) => {
            return string.Format("$set {0} = {1}", m.Groups[1].Value, m.Groups[2].Value);
        };

        static private Regex ComplexLinkRightFormat = new Regex(@"\[\[(.+?)->(.+?)\]\]");
        static private MatchEvaluator ComplexLinkRightReplace = (m) => {
            string text = m.Groups[1].Value;
            string target = m.Groups[2].Value;
            target = ProcessId(target);
            return string.Format("$choice {0}; {1}", target, text);
        };

        static private Regex ComplexLinkLeftFormat = new Regex(@"\[\[(.+?)<-(.+?)\]\]");
        static private MatchEvaluator ComplexLinkLeftReplace = (m) => {
            string text = m.Groups[2].Value;
            string target = m.Groups[1].Value;
            target = ProcessId(target);
            return string.Format("$choice {0}; {1}", target, text);
        };

        static private Regex SimpleLinkFormat = new Regex(@"\[\[(.+?)\]\]");
        static private MatchEvaluator SimpleLinkReplace = (m) => {
            string text = m.Groups[1].Value;
            string target = ProcessId(text);
            return string.Format("$choice {0}; {1}", target, text);
        };

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

        #region Is

        static private Regex IsCheckFormat = new Regex("\\bis\\b\\s*(\\d+)\\)");
        static private MatchEvaluator IsCheckReplace = (m) => {
            return string.Format("== {0}", m.Groups[1].Value);
        };

        #endregion // Is

        #region Macros and Functions

        static private Regex HistoryVisitedFormat = new Regex("\\(history:\\)\\s*contains \"([^\"]+?)\"");
        static private MatchEvaluator HistoryVisitedReplace = (m) => {
            string target = ProcessId(m.Groups[1].Value);
            return string.Format("Visited({0})", target);
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
            string name = string.Empty;;
            switch(m.Groups[1].Value) {
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

        static private Regex DecreaseTimeFormat = new Regex("\\(set: \\$currentTime to it - (\\d+)\\)");
        static private MatchEvaluator DecreaseTimeReplace = (m) => {
            return string.Format("$call DecreaseTime({0})", StringParser.ParseFloat(m.Groups[1].Value) / 60f);
        };

        static private Regex GiveSnippetFormat = new Regex("\\(\\$gainSnippet:\\s*\"\\w+\",\\s*\"([\\w\\s]+)\"\\)");
        static private MatchEvaluator GiveSnippetReplace = (m) => {
            string target = ProcessId(m.Groups[1].Value);
            return string.Format("$call GiveSnippet({0})", target);
        };

        #endregion // Macros and Functions

        #region Stats

        static private Regex TechieFormat = new Regex("\\$techie\\b");
        static private MatchEvaluator TechieReplace = (m) => {
            return "Stat(Tech)";
        };

        static private Regex ResourcefulFormat = new Regex("\\$resourceful\\b");
        static private MatchEvaluator ResourcefulReplace = (m) => {
            return "Stat(Resourceful)";
        };

        static private Regex SocialFormat = new Regex("\\$social\\b");
        static private MatchEvaluator SocialReplace = (m) => {
            return "Stat(Social)";
        };

        static private Regex TrustFormat = new Regex("\\$trust\\b");
        static private MatchEvaluator TrustReplace = (m) => {
            return "Stat(Trust)";
        };

        static private Regex BookwormFormat = new Regex("\\$bookworm\\b");
        static private MatchEvaluator BookwormReplace = (m) => {
            return "Stat(Research)";
        };

        static private Regex EnduranceFormat = new Regex("\\$endurance\\b");
        static private MatchEvaluator EnduranceReplace = (m) => {
            return "Stat(Endurance)";
        };

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

        static private string EscapeRegex(string pattern) {
            string regex = Regex.Escape(pattern);
            if (pattern.Length > 0 && !char.IsLetterOrDigit(pattern[0]) && regex[0] == '\\') {
                regex = regex.Substring(1);
            }
            return regex;
        }
    }
}