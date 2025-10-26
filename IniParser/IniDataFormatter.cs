using IniParser.Configuration;
using IniParser.Format;
using IniParser.Model;
using System.Collections.Generic;
using System.Text;

namespace IniParser
{
    public class IniDataFormatter : IIniDataFormatter
    {
        public string Format(IniData iniData, IniFormattingConfiguration format)
        {
            var sb = new StringBuilder();

            // Write global properties
            this.WriteProperties(iniData.Global, sb, iniData.Scheme, format);

            //Write sections
            foreach (var section in iniData.Sections)
            {
                //Write current section
                this.WriteSection(section, sb, iniData.Scheme, format);
            }

            var newLineLength = format.NewLineString.Length;

            // Remove the last new line
            sb.Remove(sb.Length - newLineLength, newLineLength);

            return sb.ToString();
        }

        #region Template Method Design Pattern

        protected virtual void WriteSection(Section section, StringBuilder sb, IniScheme scheme, IniFormattingConfiguration format)
        {
            // Comments
            this.WriteComments(section.Comments, sb, scheme, format);

            // Write blank line before section, but not if it is the first line
            if (format.NewLineBeforeSection && sb.Length > 0)
            {
                sb.Append(format.NewLineString);
            }

            // Write section name
            sb.Append($"{scheme.SectionStartString}{section.Name}{scheme.SectionEndString}{format.NewLineString}");

            if (format.NewLineAfterSection)
            {
                sb.Append(format.NewLineString);
            }

            this.WriteProperties(section.Properties, sb, scheme, format);
        }

        protected virtual void WriteProperties(PropertyCollection properties, StringBuilder sb, IniScheme scheme, IniFormattingConfiguration format)
        {
            foreach (Property property in properties)
            {
                // Write comments
                this.WriteComments(property.Comments, sb, scheme, format);

                if (format.NewLineBeforeProperty)
                {
                    sb.Append(format.NewLineString);
                }

                //Write key and value
                sb.Append($"{property.Key}{format.SpacesBetweenKeyAndAssigment}{scheme.PropertyAssigmentString}{format.SpacesBetweenAssigmentAndValue}{property.Value}{format.NewLineString}");

                if (format.NewLineAfterProperty)
                {
                    sb.Append(format.NewLineString);
                }
            }
        }

        protected virtual void WriteComments(List<string> comments, StringBuilder sb, IniScheme scheme, IniFormattingConfiguration format)
        {
            foreach (string comment in comments)
            {
                sb.Append($"{scheme.CommentString}{comment}{format.NewLineString}");
            }
        }
        #endregion
    }
}