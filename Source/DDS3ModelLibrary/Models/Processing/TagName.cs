using System.Collections.Generic;

namespace DDS3ModelLibrary.Models.Processing
{
    public class TagName
    {
        private static readonly TagNameParser sParser = new TagNameParser();

        public string Name { get; set; }

        public Dictionary<string, IList<string>> Properties { get; }

        public IList<string> this[string key]
        {
            get
            {
                return !Properties.TryGetValue(key, out var value) ? new List<string>() : value;
            }
            set => Properties[key] = value;
        }

        public TagName()
        {
            Properties = new Dictionary<string, IList<string>>();
        }

        /// <summary>
        /// Throws <see cref="TagNameParseException"/> on failure.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static TagName Parse(string input)
        {
            return sParser.Parse(input);
        }
    }
}
