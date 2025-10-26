using System.Collections.Generic;
using System.IO;

namespace IniParser.Parser
{
    public sealed class StringBuffer
    {
        public struct Range
        {
            int _start, _size;

            public int Start
            {
                readonly get => _start;

                set
                {
                    _start = value < 0 ? 0 : value;
                }
            }

            public int Size
            {
                readonly get => _size;

                set
                {
                    _size = value < 0 ? 0 : value;
                }
            }

            public readonly int End => _size <= 0 ? 0 : _start + (_size - 1);

            public readonly bool IsEmpty => this._size == 0;

            public void Reset()
            {
                this.Start = 0;
                this.Size = 0;
            }

            public static Range FromIndexWithSize(int start, int size)
            {
                if (start < 0 || size <= 0) return new Range();

                return new Range { Start = start, Size = size };
            }

            public static Range WithIndexes(int start, int end)
            {
                if (start < 0 || end < 0 || end - start < 0)

                {
                    return new Range();
                }

                return new Range { Start = start, Size = end - start + 1 };
            }

            public override readonly string ToString()
            {
                return string.Format("[start:{0}, end:{1} size: {2}]", this.Start, this.End, this.Size);
            }
        }

        readonly static int DefaultCapacity = 256;

        public StringBuffer() : this(StringBuffer.DefaultCapacity)
        {

        }

        public StringBuffer(int capacity)
        {
            _buffer = new List<char>(capacity);
        }

        internal StringBuffer(List<char> buffer, Range bufferIndexes)
        {
            _buffer = buffer;
            _bufferIndexes = bufferIndexes;
        }

        public int Count { get { return _bufferIndexes.Size; } }

        public bool IsEmpty
        {
            get { return _bufferIndexes.IsEmpty; }
        }

        public bool IsWhitespace
        {
            get
            {
                int startIdx = _bufferIndexes.Start;
                while (startIdx <= _bufferIndexes.End
                    && char.IsWhiteSpace(_buffer[startIdx]))
                {
                    startIdx++;
                }

                return startIdx > _bufferIndexes.End;
            }
        }

        public char this[int idx]
        {
            get
            {
                return _buffer[idx + _bufferIndexes.Start];
            }
        }

        public StringBuffer DiscardChanges()
        {
            _bufferIndexes = Range.FromIndexWithSize(0, _buffer.Count);
            return this;
        }

        public Range FindSubstring(string subString, int startingIndex = 0)
        {
            int subStringLength = subString.Length;

            if (subStringLength <= 0 || this.Count < subStringLength)
            {
                return new Range();
            }

            startingIndex += _bufferIndexes.Start;

            // Search the first char of the substring
            for (int firstCharIdx = startingIndex; firstCharIdx <= _bufferIndexes.End; ++firstCharIdx)
            {
                if (_buffer[firstCharIdx] != subString[0])
                {
                    continue;
                }

                // Fail now if the substring can't fit given the size of the
                // buffer and the search start index
                if (firstCharIdx + subStringLength - 1 > _bufferIndexes.End)
                {
                    return new Range();
                }

                bool isSubstringMismatch = false;
                // Check if the substring matches starting at the index
                for (int currentIdx = 0; currentIdx < subStringLength; ++currentIdx)
                {
                    if (_buffer[firstCharIdx + currentIdx] != subString[currentIdx])
                    {
                        isSubstringMismatch = true;
                        break;
                    }
                }

                if (isSubstringMismatch)
                {
                    continue;
                }

                return Range.FromIndexWithSize(firstCharIdx - _bufferIndexes.Start, subStringLength);
            }

            return new Range();
        }

        public bool ReadLine()
        {
            if (_dataSource == null) return false;

            _buffer.Clear();
            int c = _dataSource.Read();

            // Read until new line ('\n') or EOF (-1)
            while (c != '\n' && c != -1)
            {
                if (c != '\r')
                {
                    _buffer.Add((char)c);
                }

                c = _dataSource.Read();
            }

            _bufferIndexes = Range.FromIndexWithSize(0, _buffer.Count);

            return _buffer.Count > 0 || c != -1;
        }

        public void Reset(TextReader dataSource)
        {
            _dataSource = dataSource;
            _bufferIndexes.Reset();
            _buffer.Clear();
        }

        public void Resize(Range range)
        {
            this.Resize(range.Start, range.Size);
        }

        public void Resize(int newSize)
        {
            this.Resize(0, newSize);
        }

        public void Resize(int startIdx, int size)
        {
            if (startIdx < 0 || size < 0)
            {
                return;
            }

            var internalStartIdx = _bufferIndexes.Start + startIdx;
            var internalEndIdx = internalStartIdx + size - 1;

            if (internalEndIdx > _bufferIndexes.End)
            {
                return;
            }

            _bufferIndexes.Start = internalStartIdx;
            _bufferIndexes.Size = size;
        }

        public void ResizeBetweenIndexes(int startIdx, int endIdx)
        {
            this.Resize(startIdx, endIdx - startIdx + 1);
        }

        public StringBuffer Substring(Range range)
        {
            var copy = this.SwallowCopy();
            copy.Resize(range);
            return copy;
        }

        public StringBuffer SwallowCopy()
        {
            return new StringBuffer(_buffer, _bufferIndexes);
        }

        public void TrimStart()
        {
            if (this.IsEmpty) return;

            int startIdx = _bufferIndexes.Start;
            while (startIdx <= _bufferIndexes.End
                && char.IsWhiteSpace(_buffer[startIdx]))
            {
                startIdx++;
            }

            // We need to make a copy of this value because _bufferIndexes.end
            // is a computed property, so it will change if we modify
            // _bufferIndexes.start or _bufferIndexes.size
            int endIdx = _bufferIndexes.End;

            _bufferIndexes.Start = startIdx;
            _bufferIndexes.Size = endIdx - startIdx + 1;
        }

        public void TrimEnd()
        {
            if (this.IsEmpty) return;

            int endIdx = _bufferIndexes.End;

            while (endIdx >= _bufferIndexes.Start
                && char.IsWhiteSpace(_buffer[endIdx]))
            {
                endIdx--;
            }

            _bufferIndexes.Size = endIdx - _bufferIndexes.Start + 1;
        }
        public void Trim()
        {
            this.TrimEnd();
            this.TrimStart();
        }

        public bool StartsWith(string str)
        {
            if (string.IsNullOrEmpty(str)) return false;
            if (this.IsEmpty) return false;

            int strIdx = 0;
            int bufferIdx = _bufferIndexes.Start;

            for (; strIdx < str.Length; ++strIdx, ++bufferIdx)
            {
                if (str[strIdx] != _buffer[bufferIdx]) return false;
            }

            return true;
        }

        public override string ToString()
        {
            return new string([.. _buffer],
                               _bufferIndexes.Start,
                               _bufferIndexes.Size);
        }

        public string ToString(Range range)
        {
            if (range.IsEmpty
             || range.Start < 0
             || range.Size > _bufferIndexes.Size
             || range.Start + _bufferIndexes.Start > _bufferIndexes.End)
            {
                return string.Empty;
            }

            return new string([.. _buffer],
                              _bufferIndexes.Start + range.Start,
                              range.Size);
        }

        TextReader _dataSource;
        readonly List<char> _buffer;
        Range _bufferIndexes;
    }
}
