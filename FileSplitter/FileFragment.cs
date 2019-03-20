using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

namespace FileSplitter
{
    /*TODO maybe turn FileFragmentReferences into classes so this can be inherited from?
    public class Variables : IEnumerable<KeyValuePair<string,object>>
    {
        public dynamic variables;

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IDictionary<string, object>)variables).GetEnumerator();

        public void Add(string key, object value) => ((IDictionary<string, object>)variables).Add(key, value);
        public void Add(string key, object value) => ((IDictionary<string, object>)variables).Add(key, value);
    };
    */
    //TODO better idea: make both FileFragment and FileFragmentReference abstract, then let each module have its own implementation if they need.

    /// <summary>
    /// Contains information about an exported file fragment
    /// </summary>
    public class FileFragment : IEnumerable<KeyValuePair<string, object>>
    {
        /// <summary>
        /// Fullpath of the file this FileFragment represents
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// Description of the data contained in the FileFragment
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The Validity of the file
        /// </summary>
        public Validity Validity { get; set; }
        /// <summary>
        /// Any extra variables a given IFileSplitterModule may want to access when validating
        /// </summary>
        public dynamic variables;

        public FileFragment(string path)
            : this(path, "", Validity.Unchecked) { }

        public FileFragment(string path, string description)
            : this(path, description, Validity.Unchecked) { }

        public FileFragment(string path, string description, Validity validity)
        {
            this.Path = path;
            this.Description = description;
            this.Validity = validity;
            this.variables = new ExpandoObject();
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IDictionary<string, object>)variables).GetEnumerator();

        public void Add(string key, object value) => ((IDictionary<string, object>)variables).Add(key, value);
        public void Add(ExpandoObject values) => variables = values;
    }

    /// <summary>
    /// Contains all information to export a part of the loaded file, and create a FileFragment
    /// </summary>
    public struct FileFragmentReference : IEnumerable<KeyValuePair<string, object>>
    {
        /// <summary>
        /// The offset within the base file where the reference's data can be found
        /// </summary>
        public readonly ulong offset;
        /// <summary>
        /// The length of the data in the reference
        /// </summary>
        public readonly ulong length;
        /// <summary>
        /// The fullpath to the file this reference should be exported to, not including a root directory
        /// </summary>
        public readonly string[] filename;
        /// <summary>
        /// The validity of this reference
        /// </summary>
        public readonly Validity validity;
        /// <summary>
        /// Description of the data contained in the reference
        /// </summary>
        public readonly string description;
        /// <summary>
        /// Any extra variables a given IFileSplitter may want to access during validation
        /// </summary>
        public dynamic variables;

        #region Constructors

        public FileFragmentReference(long offset, long length, string[] filename)
                              : this((ulong)offset, (ulong)length, filename) { }
        public FileFragmentReference(ulong offset, ulong length, string[] filename)
                              : this(offset, length, filename, Validity.Unchecked, "") { }

        public FileFragmentReference(long offset, long length, string[] filename, Validity validity)
                              : this((ulong)offset, (ulong)length, filename, validity) { }
        public FileFragmentReference(ulong offset, ulong length, string[] filename, Validity validity)
                              : this(offset, length, filename, validity, "") { }

        public FileFragmentReference(long offset, long length, string[] filename, string description)
                              : this((ulong)offset, (ulong)length, filename, description) { }
        public FileFragmentReference(ulong offset, ulong length, string[] filename, string description)
                              : this(offset, length, filename, Validity.Unchecked, description) { }

        public FileFragmentReference(long offset, long length, string[] filename, Validity validity, string description)
                              : this((ulong)offset, (ulong)length, filename, validity, description) { }
        public FileFragmentReference(ulong offset, ulong length, string[] filename, Validity validity, string description)
        {
            this.filename = filename;
            this.offset = offset;
            this.length = length;
            this.validity = validity;
            this.description = description;
            this.variables = new ExpandoObject();
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IDictionary<string, object>)variables).GetEnumerator();

        public void Add(string key, object value) => ((IDictionary<string, object>)variables).Add(key, value);
        public void Add(ExpandoObject values) => variables = values;
        #endregion
    }
}
