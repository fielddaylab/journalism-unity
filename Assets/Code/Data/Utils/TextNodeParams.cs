using BeauUtil;

namespace Journalism
{
    public struct TextNodeParams
    {
        public string Content;
        public string Speaker;

        public TextNodeParams(string inContent, string inSpeaker) {
            Content = inContent;
            Speaker = inSpeaker;
        }
    }
}