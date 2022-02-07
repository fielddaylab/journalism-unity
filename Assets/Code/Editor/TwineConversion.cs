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

            leafBuilder.Write(":: ");
            leafBuilder.Write(leafId);
            leafBuilder.WriteLine();

            if (tags.Length > 0) {
                leafBuilder.Write("@tags ");
                leafBuilder.Write(tags);
                leafBuilder.WriteLine();
            }

            text = FirstIfFormat.Replace(text, FirstIfReplace);
            text = ElseIfFormat.Replace(text, ElseIfReplace);
            text = EndElseFormat.Replace(text, EndElseReplace);
            text = ComplexLinkRightFormat.Replace(text, ComplexLinkRightReplace);
            text = ComplexLinkLeftFormat.Replace(text, ComplexLinkLeftReplace);
            text = SimpleLinkFormat.Replace(text, SimpleLinkReplace);
            text = DisplayPassageFormat.Replace(text, DisplayPassageReplace);
            text = AdjustVarFormat.Replace(text, AdjustVarReplace);
            text = SimpleSetVarFormat.Replace(text, SimpleSetVarReplace);

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
        
        static private Regex FirstIfFormat = new Regex(@"\(if: \$(\w+)\b \s*?([<>=]{1,2}|(?:is))\s*([""\w\d\s]+)\)\s*?\[((?:[\s\S]|[^\]])+?)\s*?\]");
        static private MatchEvaluator FirstIfReplace = (m) => {
            return string.Format("$if {0} {1} {2}\n{3}\n$endif", m.Groups[1].Value, m.Groups[2].Value, m.Groups[3], m.Groups[4].Value);
        };

        static private Regex ElseIfFormat = new Regex(@"\$endif\s*?\(elseif: \$(\w+)\b \s*?([<>=]{1,2}|(?:is))\s*([""\w\d\s]+)\)\s*?\[((?:[\s\S]|[^\]])+?)\s*?\]");
        static private MatchEvaluator ElseIfReplace = (m) => {
            return string.Format("$elseif {0} {1} {2}\n{3}\n$endif", m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value);
        };

        static private Regex EndElseFormat = new Regex(@"\$endif\s*?\(else:\)\[((?:[\s\S]|[^\]])+?)\s*?\]");
        static private MatchEvaluator EndElseReplace = (m) => {
            return string.Format("$else\n{0}\n$endif", m.Groups[1].Value);
        };

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

        static private string EscapeRegex(string pattern) {
            string regex = Regex.Escape(pattern);
            if (pattern.Length > 0 && !char.IsLetterOrDigit(pattern[0]) && regex[0] == '\\') {
                regex = regex.Substring(1);
            }
            return regex;
        }
    }
}