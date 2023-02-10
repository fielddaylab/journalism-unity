using BeauUtil;

namespace Journalism
{
    public struct TextNodeParams
    {
        public string NodeId;
        public string Content;
        public string Speaker;

        public TextNodeParams(string inId, string inContent, string inSpeaker) {
            NodeId = inId;
            Content = inContent;
            Speaker = inSpeaker;
        }
    }
}