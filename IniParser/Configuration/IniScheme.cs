﻿namespace IniParser.Configuration
{
    /// <summary>
    /// This structure defines the format of the INI file by customization the characters used to define sections
    /// key/values or comments.
    /// Used IniDataParser to read INI files, and an IIniDataFormatter to write a new ini file string.
    /// </summary>
  public class IniScheme : IDeepCloneable<IniScheme>
    {
        /// <summary>
        ///     Ctor.
        /// </summary>
        /// <remarks>
        ///     By default the various delimiters for the data are setted:
        ///     <para>';' for one-line comments</para>
        ///     <para>'[' ']' for delimiting a section</para>
        ///     <para>'=' for linking key / value pairs</para>
        ///     <example>
        ///         An example of well formed data with the default values:
        ///         <para>
        ///         ;section comment<br/>
        ///         [section] ; section comment<br/>
        ///         <br/>
        ///         ; key comment<br/>
        ///         key = value ;key comment<br/>
        ///         <br/>
        ///         ;key2 comment<br/>
        ///         key2 = value<br/>
        ///         </para>
        ///     </example>
        /// </remarks>
        public IniScheme()
        {
        }

        /// <summary>
        ///     Copy ctor.
        /// </summary>
        /// <param name="ori">
        ///     Original instance to be copied.
        /// </param>
        IniScheme(IniScheme ori)
        {
            this.PropertyAssigmentString = ori.PropertyAssigmentString;
            this.SectionStartString = ori.SectionStartString;
            this.SectionEndString = ori.SectionEndString;
            this.CommentString = ori.CommentString;
        }

        /// <summary>
        ///     Sets the string that defines the start of a comment.
        ///     A comment spans from the mirst matching comment string
        ///     to the end of the line.
        /// </summary>
        /// <remarks>
        ///     Defaults to string ";". 
        ///     String returned will also be trimmed
        /// </remarks>
        public string CommentString
        {
            get => string.IsNullOrWhiteSpace(this._commentString) ? ";" : this._commentString;
            set => this._commentString = value?.Trim();
        }

        /// <summary>
        ///     Sets the string that defines the start of a section name.
        /// </summary>
        /// <remarks>
        ///     Defaults to "["
        ///     String returned will also be trimmed
        /// </remarks>
        public string SectionStartString
        {
            get => string.IsNullOrWhiteSpace(this._sectionStartString) ? "[" : this._sectionStartString;
            set => this._sectionStartString = value?.Trim();
        }


        /// <summary>
        ///     Sets the char that defines the end of a section name.
        /// </summary>
        /// <remarks>
        ///     Defaults to character ']'
        ///     String returned will also be trimmed
        /// </remarks>
        public string SectionEndString
        {
            get => string.IsNullOrWhiteSpace(this._sectionEndString) ? "]" : this._sectionEndString;
            set => this._sectionEndString = value?.Trim();
        }


        /// <summary>
        ///     Sets the string used in the ini file to denote a key / value assigment
        /// </summary>
        /// <remarks>
        ///     Defaults to character '='
        ///     String returned will also be trimmed
        /// </remarks>
        /// 
        public string PropertyAssigmentString
        {
            get => string.IsNullOrWhiteSpace(this._propertyAssigmentString) ? "=" : this._propertyAssigmentString;
            set => this._propertyAssigmentString = value?.Trim();
        }

        #region IDeepCloneable<T> Members
        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public IniScheme DeepClone()
        {
            return new IniScheme(this);
        }
        #endregion

        #region Fields
        string _commentString = ";";
        string _sectionStartString = "[";
        string _sectionEndString = "]";
        string _propertyAssigmentString = "=";
        #endregion
    }
}
