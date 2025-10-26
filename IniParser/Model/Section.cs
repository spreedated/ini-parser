#pragma warning disable S1450

using System;
using System.Collections.Generic;

namespace IniParser.Model
{
    /// <summary>
    ///     Information associated to a section in a INI File
    ///     Includes both the properties and the comments associated to the section.
    /// </summary>
    public class Section : IDeepCloneable<Section>
    {
        #region Initialization

        public Section(string sectionName)
            : this(sectionName, EqualityComparer<string>.Default)
        {

        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Section"/> class.
        /// </summary>
        public Section(string sectionName, IEqualityComparer<string> searchComparer)
        {
            _searchComparer = searchComparer;

            if (string.IsNullOrEmpty(sectionName))
                throw new ArgumentException("section name can not be empty", nameof(sectionName));

            this.Properties = new PropertyCollection(_searchComparer);
            this.Name = sectionName;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Section"/> class
        ///     from a previous instance of <see cref="Section"/>.
        /// </summary>
        /// <remarks>
        ///     Data is deeply copied
        /// </remarks>
        /// <param name="ori">
        ///     The instance of the <see cref="Section"/> class 
        ///     used to create the new instance.
        /// </param>
        /// <param name="searchComparer">
        ///     Search comparer.
        /// </param>
        public Section(Section ori, IEqualityComparer<string> searchComparer = null)
        {
            this.Name = ori.Name;

            this._searchComparer = searchComparer;
            this.Comments = ori.Comments;
            this.Properties = new PropertyCollection(ori.Properties, searchComparer ?? ori._searchComparer);
        }

        #endregion


        /// <summary>
        ///     Gets or sets the name of the section.
        /// </summary>
        /// <value>
        ///     The name of the section
        /// </value>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _name = value;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the comment list associated to this section.
        /// </summary>
        /// <value>
        ///     A list of strings.
        /// </value>
        public List<string> Comments
        {
            get
            {
                this._comments ??= [];

                return this._comments;
            }

            set
            {
                this._comments ??= [];
                this._comments.Clear();
                this._comments.AddRange(value);
            }
        }

        /// <summary>
        ///     Gets or sets the properties associated to this section.
        /// </summary>
        /// <value>
        ///     A collection of Property objects.
        /// </value>
        public PropertyCollection Properties { get; set; }

        /// <summary>
        ///     Deletes all comments and properties from this Section
        /// </summary>
        public void Clear()
        {
            this.ClearProperties();
            this.ClearComments();
        }

        /// <summary>
        ///     Deletes all comments in this section and in all the properties pairs it contains
        /// </summary>
        public void ClearComments()
        {
            this.Comments.Clear();
            this.Properties.ClearComments();
        }

        /// <summary>
        /// Deletes all the properties pairs in this section.
        /// </summary>
    public void ClearProperties()
        {
            this.Properties.Clear();
        }

        /// <summary>
        ///     Merges otherSection into this, adding new properties if they 
        ///     did not existed or overwriting values if the properties already 
        ///     existed.
        /// </summary>
        /// <remarks>
        ///     Comments are also merged but they are always added, not overwritten.
        /// </remarks>
        /// <param name="toMergeSection"></param>
        public void Merge(Section toMergeSection)
        {
            this.Properties.Merge(toMergeSection.Properties);

            foreach (var comment in toMergeSection.Comments)
            {
                this.Comments.Add(comment);
            }
        }

        #region IDeepCloneable<T> Members
        public Section DeepClone()
        {
            return new Section(this);
        }
        #endregion

        #region Fields
        List<string> _comments;
        private string _name;
        private readonly IEqualityComparer<string> _searchComparer;
        #endregion
    }
}