using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DDS3ModelLibrary.Models.Processing
{
    internal class TagNameParser
    {
        private enum State
        {
            None,
            ParsingName,
            ParsingPropertyName,
            ParsingPropertyArguments,
            ParsingPropertyDone
        }

        private static readonly Regex sPattern = new Regex(@"@(?<tagName>.+?)\((?<tagValues>.+?)?\)", RegexOptions.Compiled);
        private static readonly Regex sVerbosePattern = new Regex(@"(_dds3tag_(?<tagName>.+?)_(?<tagValues>.+?)?_dds3tagend)", RegexOptions.Compiled);

        public TagName Parse(string input)
        {
            var matches = sPattern.Matches(input);
            var verbosePattern = false;
            if (!matches.Any())
            {
                matches = sVerbosePattern.Matches(input);
                verbosePattern = true;
            }

            var state = State.ParsingName;

            var tagName = new TagName()
            {
                Name = input.Substring(0, matches.First().Index)
            };
            foreach (Match match in matches)
            {
                var seperator = verbosePattern ?
                    "dds3tagsep" :
                    ",";
                var name = match.Groups["tagName"].Value;
                var values = match.Groups["tagValues"].Value.Split(seperator);
                tagName.Properties.Add(name, values);
            }

            return tagName;
        }
    }
}