using System;
using System.Collections.Generic;

namespace DDS3ModelLibrary.Modeling.Utilities
{
    public class TagName
    {
        private static readonly TagNameParser sParser = new TagNameParser();

        public string Name { get; set; }

        public List<TagProperty> Properties { get; }

        public TagName()
        {
            Properties = new List<TagProperty>();
        }

        /// <summary>
        /// Throws <see cref="TagNameParseException"/> on failure.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static TagName Parse( string input )
        {
            return sParser.Parse( input );
        }
    }

    public class TagProperty
    {
        public string Name { get; }

        public List<string> Arguments { get; }

        public TagProperty( string name, List<string> arguments )
        {
            Name = name ?? throw new ArgumentNullException( nameof( name ) );
            Arguments = arguments ?? throw new ArgumentNullException( nameof( arguments ) );
        }
    }
}
