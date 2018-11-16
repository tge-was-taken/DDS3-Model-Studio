using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DDS3ModelLibrary.Modeling.Utilities
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

        private static readonly Regex sRegex = new Regex( @"(@?[^@^\()^,]+)", RegexOptions.Compiled );

        public TagName Parse( string input )
        {
            var matches = sRegex.Matches( input );
            var state   = State.ParsingName;

            var tagName = new TagName();
            string propertyName = null;
            List<string> propertyArguments = null;

            foreach ( Match match in matches )
            {
                switch ( state )
                {
                    case State.ParsingName:
                        if ( match.Value.StartsWith( "@" ) )
                        {
                            goto case State.ParsingPropertyName;
                        }
                        else
                        {
                            if ( tagName.Name == null )
                                tagName.Name = match.Value;
                            else
                                throw new TagNameParseException( "Expected property name" );
                        }

                        break;

                    case State.ParsingPropertyName:
                        propertyName = match.Value.Substring( 1 );
                        propertyArguments = new List<string>();
                        state = State.ParsingPropertyArguments;
                        break;

                    case State.ParsingPropertyArguments:
                        if ( match.Value.StartsWith( "@" ) )
                            goto case State.ParsingPropertyDone;

                        propertyArguments.Add( match.Value );
                        break;

                    case State.ParsingPropertyDone:
                        tagName[propertyName] = propertyArguments;
                        goto case State.ParsingPropertyName;

                }
            }

            if ( state == State.ParsingPropertyArguments )
            {
                tagName[propertyName] = propertyArguments;
            }

            return tagName;
        }
    }
}