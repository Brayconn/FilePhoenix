namespace FileSplitter
{
    /// <summary>
    /// Contains information about an exported file fragment
    /// </summary>
    public class FileFragment
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

        public FileFragment(string path)
            : this(path, "", Validity.Unchecked) { }

        public FileFragment(string path, string description)
            : this(path, description, Validity.Unchecked) { }

        public FileFragment(string path, string description, Validity validity)
        {
            this.Path = path;
            this.Description = description;
            this.Validity = validity;
        }
    }

    /// <summary>
    /// Contains all information to export a part of the loaded file, and create a FileFragment
    /// </summary>
    public class FileFragmentReference : FileFragment
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
                              : base("",description,validity)
        {
            this.filename = filename;
            this.offset = offset;
            this.length = length;
        }
        #endregion
    }
}
