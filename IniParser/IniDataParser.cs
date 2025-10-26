using IniParser.Configuration;
using IniParser.Exceptions;
using IniParser.Model;
using IniParser.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace IniParser
{
    /// <summary>
    ///   Responsible for parsing an string from an ini file, and creating
    ///   an <see cref="IniData"/> structure.
    /// </summary>
    public partial class IniDataParser
    {
        #region Initialization
        /// <summary>
        ///     Ctor
        /// </summary>
        public IniDataParser()
        {
            this.Scheme = new();
            this.Configuration = new();
            this._errorExceptions = [];
        }

        #endregion

        #region State

        public virtual IniParserConfiguration Configuration { get; protected set; }

        /// <summary>
        ///     Scheme that defines the structure for the ini file to be parsed
        /// </summary>
        public IniScheme Scheme { get; protected set; }

        /// <summary>
        /// True is the parsing operation encounter any problem
        /// </summary>
        public bool HasError { get { return _errorExceptions.Count > 0; } }

        /// <summary>
        /// Returns the list of errors found while parsing the ini file.
        /// </summary>
        /// <remarks>
        /// If the configuration option ThrowExceptionOnError is false it
        /// can contain one element for each problem found while parsing;
        /// otherwise it will only contain the very same exception that was
        /// raised.
        /// </remarks>
        public ReadOnlyCollection<Exception> Errors
        {
            get { return _errorExceptions.AsReadOnly(); }
        }
        #endregion

        /// <summary>
        ///     Parses a string containing valid ini data
        /// </summary>
        /// <param name="iniString">
        ///     String with data in INI format
        /// </param>
        public IniData Parse(string iniString)
        {
            return this.Parse(new StringReader(iniString));
        }

        /// <summary>
        ///     Parses a string containing valid ini data
        /// </summary>
        /// <param name="textReader">
        ///     Text reader for the source string contaninig the ini data
        /// </param>
        /// <returns>
        ///     An <see cref="IniData"/> instance containing the data readed
        ///     from the source
        /// </returns>
        /// <exception cref="ParsingException">
        ///     Thrown if the data could not be parsed
        /// </exception>
        public IniData Parse(TextReader textReader)
        {
            IniData iniData = this.Configuration.CaseInsensitive ?
                  new IniDataCaseInsensitive(this.Scheme)
                  : new IniData(this.Scheme);

            this.Parse(textReader, ref iniData);

            return iniData;
        }

        /// <summary>
        ///     Parses a string containing valid ini data
        /// </summary>
        /// <param name="textReader">
        ///     Text reader for the source string contaninig the ini data
        /// </param>
        /// <returns>
        ///     An <see cref="IniData"/> instance containing the data readed
        ///     from the source
        /// </returns>
        /// <exception cref="ParsingException">
        ///     Thrown if the data could not be parsed
        /// </exception>
        public void Parse(TextReader textReader, ref IniData iniData)
        {
            iniData.Clear();

            iniData.Scheme = this.Scheme.DeepClone();

            _errorExceptions.Clear();
            if (this.Configuration.ParseComments)
            {
                this.CurrentCommentListTemp.Clear();
            }
            _currentSectionNameTemp = null;
            _mBuffer.Reset(textReader);
            _currentLineNumber = 0;

            while (_mBuffer.ReadLine())
            {
                _currentLineNumber++;

                try
                {
                    this.ProcessLine(_mBuffer, iniData);
                }
                catch (Exception ex)
                {
                    _errorExceptions.Add(ex);
                    if (this.Configuration.ThrowExceptionsOnError)
                    {
                        throw;
                    }
                }
            }

            // replace the try/catch with explicit guards
            if (this.Configuration.ParseComments && this.CurrentCommentListTemp.Count > 0)
            {
                if (iniData.Sections.Count > 0)
                {
                    var section = iniData.Sections.FindByName(_currentSectionNameTemp);
                    if (section != null)
                    {
                        section.Comments.AddRange(this.CurrentCommentListTemp);
                    }
                    else
                    {
                        if (this.Configuration.ThrowExceptionsOnError)
                        {
                            throw new ParsingException("Section not found for orphan comments", _currentLineNumber);
                        }
                    }
                }
                else if (iniData.Global.Count > 0)
                {
                    var last = iniData.Global.GetLast();
                    last?.Comments.AddRange(this.CurrentCommentListTemp);
                }

                this.CurrentCommentListTemp.Clear();
            }

            if (this.HasError)
            {
                iniData.Clear();
            }
        }

        #region Template Method Design Pattern
        // All this methods controls the parsing behaviour, so it can be
        // modified in derived classes.
        // See http://www.dofactory.com/Patterns/PatternTemplate.aspx for an
        // explanation of this pattern.
        // Probably for the most common cases you can change the parsing
        // behavior using a custom configuration object rather than creating
        // derived classes.
        // See IniParserConfiguration interface, and IniDataParser constructor
        // to change the default configuration.



        /// <summary>
        ///     Processes one line and parses the data found in that line
        ///     (section or key/value pair who may or may not have comments)
        /// </summary>
        protected virtual void ProcessLine(StringBuffer currentLine, IniData iniData)
        {
            if (currentLine.IsEmpty || currentLine.IsWhitespace) return;

            // TODO: change this to a global (IniData level) array of comments
            // Extract comments from current line and store them in a tmp list

            if (this.ProcessComment(currentLine)) return;

            if (this.ProcessSection(currentLine, iniData)) return;

            if (this.ProcessProperty(currentLine, iniData)) return;

            if (this.Configuration.SkipInvalidLines) return;

            var errorFormat = "Couldn't parse text: '{0}'. Please see configuration option {1}.{2} to ignore this error.";
            var errorMsg = string.Format(errorFormat, currentLine, nameof(this.Configuration), nameof(this.Configuration.SkipInvalidLines));

            throw new ParsingException(errorMsg, _currentLineNumber, currentLine.DiscardChanges().ToString());
        }

        protected virtual bool ProcessComment(StringBuffer currentLine)
        {
            // Line is  med when it came here, so we only need to check if
            // the first characters are those of the comments
            var currentLineTrimmed = currentLine.SwallowCopy();
            currentLineTrimmed.TrimStart();

            if (!currentLineTrimmed.StartsWith(this.Scheme.CommentString))
            {
                return false;
            }

            if (!this.Configuration.ParseComments)
            {
                return true;
            }

            currentLineTrimmed.TrimEnd();

            var commentRange = currentLineTrimmed.FindSubstring(this.Scheme.CommentString);
            // Exctract the range of the string that contains the comment but not
            // the comment delimiter
            var startIdx = commentRange.Start + this.Scheme.CommentString.Length;
            var size = currentLineTrimmed.Count - this.Scheme.CommentString.Length;
            var range = StringBuffer.Range.FromIndexWithSize(startIdx, size);

            var comment = currentLineTrimmed.Substring(range);
            if (this.Configuration.TrimComments)
            {
                comment.Trim();
            }

            this.CurrentCommentListTemp.Add(comment.ToString());

            return true;
        }

        /// <summary>
        ///     Proccess a string which contains an ini section.%
        /// </summary>
        /// <param name="currentLine">
        ///     The string to be processed
        /// </param>
        protected virtual bool ProcessSection(StringBuffer currentLine, IniData iniData)
        {
            if (currentLine.Count <= 0) return false;

            var sectionStartRange = currentLine.FindSubstring(this.Scheme.SectionStartString);

            if (sectionStartRange.IsEmpty) return false;

            var sectionEndRange = currentLine.FindSubstring(this.Scheme.SectionEndString, sectionStartRange.Size);
            if (sectionEndRange.IsEmpty)
            {
                if (this.Configuration.SkipInvalidLines) return false;


                var errorFormat = "No closing section value. Please see configuration option {0}.{1} to ignore this error.";
                var errorMsg = string.Format(errorFormat,
                                             nameof(this.Configuration),
                                             nameof(this.Configuration.SkipInvalidLines));

                throw new ParsingException(errorMsg,
                                           _currentLineNumber,
                                           currentLine.DiscardChanges().ToString());
            }

            var startIdx = sectionStartRange.Start + this.Scheme.SectionStartString.Length;
            var endIdx = sectionEndRange.End - this.Scheme.SectionEndString.Length;
            currentLine.ResizeBetweenIndexes(startIdx, endIdx);

            if (this.Configuration.TrimSections)
            {
                currentLine.Trim();
            }

            var sectionName = currentLine.ToString();

            // Temporally save section name.
            _currentSectionNameTemp = sectionName;

            //Checks if the section already exists
            if (!this.Configuration.AllowDuplicateSections && iniData.Sections.Contains(sectionName))
            {
                if (this.Configuration.SkipInvalidLines) return false;

                var errorFormat = "Duplicate section with name '{0}'. Please see configuration option {1}.{2} to ignore this error.";
                var errorMsg = string.Format(errorFormat,
                                                sectionName,
                                                nameof(this.Configuration),
                                                nameof(this.Configuration.SkipInvalidLines));

                throw new ParsingException(errorMsg,
                                            _currentLineNumber,
                                            currentLine.DiscardChanges().ToString());
            }

            // If the section does not exists, add it to the ini data
            iniData.Sections.Add(sectionName);

            // Save comments read until now and assign them to this section
            if (this.Configuration.ParseComments)
            {
                var sections = iniData.Sections;
                var sectionData = sections.FindByName(sectionName);
                sectionData.Comments.AddRange(this.CurrentCommentListTemp);
                this.CurrentCommentListTemp.Clear();
            }

            return true;
        }

        protected virtual bool ProcessProperty(StringBuffer currentLine, IniData iniData)
        {
            if (currentLine.Count <= 0) return false;

            var propertyAssigmentIdx = currentLine.FindSubstring(this.Scheme.PropertyAssigmentString);

            if (propertyAssigmentIdx.IsEmpty) return false;

            var keyRange = StringBuffer.Range.WithIndexes(0, propertyAssigmentIdx.Start - 1);
            var valueStartIdx = propertyAssigmentIdx.End + 1;
            var valueSize = currentLine.Count - propertyAssigmentIdx.End - 1;
            var valueRange = StringBuffer.Range.FromIndexWithSize(valueStartIdx, valueSize);

            var key = currentLine.Substring(keyRange);
            var value = currentLine.Substring(valueRange);

            if (this.Configuration.TrimProperties)
            {
                key.Trim();
                value.Trim();
            }

            if (key.IsEmpty)
            {
                if (this.Configuration.SkipInvalidLines) return false;

                var errorFormat = "Found property without key. Please see configuration option {0}.{1} to ignore this error";
                var errorMsg = string.Format(errorFormat,
                                             nameof(this.Configuration),
                                             nameof(this.Configuration.SkipInvalidLines));

                throw new ParsingException(errorMsg,
                                           _currentLineNumber,
                                           currentLine.DiscardChanges().ToString());
            }

            // Check if we haven't read any section yet
            if (string.IsNullOrEmpty(_currentSectionNameTemp))
            {
                if (!this.Configuration.AllowKeysWithoutSection)
                {
                    var errorFormat = "Properties must be contained inside a section. Please see configuration option {0}.{1} to ignore this error.";
                    var errorMsg = string.Format(errorFormat,
                                                nameof(this.Configuration),
                                                nameof(this.Configuration.AllowKeysWithoutSection));

                    throw new ParsingException(errorMsg,
                                               _currentLineNumber,
                                               currentLine.DiscardChanges().ToString());
                }

                this.AddKeyToKeyValueCollection(key.ToString(),
                                           value.ToString(),
                                           iniData.Global,
                                           "global");
            }
            else
            {
                var currentSection = iniData.Sections.FindByName(_currentSectionNameTemp);

                this.AddKeyToKeyValueCollection(key.ToString(),
                                           value.ToString(),
                                           currentSection.Properties,
                                           _currentSectionNameTemp);
            }


            return true;
        }


        /// <summary>
        ///     Abstract Method that decides what to do in case we are trying 
        ///     to add a duplicated key to a section
        /// </summary>
        void HandleDuplicatedKeyInCollection(string key,
                                             string value,
                                             PropertyCollection keyDataCollection,
                                             string sectionName)
        {
            switch (this.Configuration.DuplicatePropertiesBehaviour)
            {
                case IniParserConfiguration.EDuplicatePropertiesBehaviour.DisallowAndStopWithError:
                    var errorMsg = string.Format("Duplicated key '{0}' found in section '{1}", key, sectionName);
                    throw new ParsingException(errorMsg, _currentLineNumber);
                case IniParserConfiguration.EDuplicatePropertiesBehaviour.AllowAndKeepFirstValue:
                    // Nothing to do here: we already have the first value assigned
                    break;
                case IniParserConfiguration.EDuplicatePropertiesBehaviour.AllowAndKeepLastValue:
                    // Override the current value when the parsing is finished we will end up
                    // with the last value.
                    keyDataCollection[key] = value;
                    break;
                case IniParserConfiguration.EDuplicatePropertiesBehaviour.AllowAndConcatenateValues:
                    keyDataCollection[key] += this.Configuration.ConcatenateDuplicatePropertiesString + value;
                    break;
            }
        }
        #endregion

        #region Helpers

        /// <summary>
        ///     Adds a key to a concrete <see cref="PropertyCollection"/> instance, checking
        ///     if duplicate keys are allowed in the configuration
        /// </summary>
        /// <param name="key">
        ///     Key name
        /// </param>
        /// <param name="value">
        ///     Key's value
        /// </param>
        /// <param name="keyDataCollection">
        ///     <see cref="Property"/> collection where the key should be inserted
        /// </param>
        /// <param name="sectionName">
        ///     Name of the section where the <see cref="PropertyCollection"/> is contained.
        ///     Used only for logging purposes.
        /// </param>
        private void AddKeyToKeyValueCollection(string key, string value, PropertyCollection keyDataCollection, string sectionName)
        {
            // Check for duplicated keys
            if (keyDataCollection.Contains(key))
            {
                // We already have a key with the same name defined in the current section
                this.HandleDuplicatedKeyInCollection(key, value, keyDataCollection, sectionName);
            }
            else
            {
                // Save the keys
                keyDataCollection.Add(key, value);
            }

            if (this.Configuration.ParseComments)
            {
                keyDataCollection.FindByKey(key).Comments = this.CurrentCommentListTemp;
                this.CurrentCommentListTemp.Clear();
            }
        }

        #endregion

        #region Fields
        uint _currentLineNumber;

        // Holds a list of the exceptions catched while parsing
        readonly List<Exception> _errorExceptions;

        // Temp list of comments
        public List<string> CurrentCommentListTemp
        {
            get
            {
                _currentCommentListTemp ??= [];

                return _currentCommentListTemp;
            }

            internal set
            {
                _currentCommentListTemp = value;
            }
        }
        List<string> _currentCommentListTemp;

        // Tmp var with the name of the seccion which is being process
        string _currentSectionNameTemp;

        // Buffer used to hold the current line being processed.
        // Saves allocating a new string
        readonly StringBuffer _mBuffer = new(256);
        #endregion
    }
}
