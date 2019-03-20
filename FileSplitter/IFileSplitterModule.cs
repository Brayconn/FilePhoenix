using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FileSplitter
{
    /// <summary>
    /// Represents a module that can parse and validate a certain file type
    /// </summary>
    public interface IFileSplitterModule
    {
        /// <summary>
        /// The name of this Module. Should be non-browsable
        /// </summary>
        string DisplayName { get; }
        /// <summary>
        /// Any applicable SaveFileDialog filters. Should be non-browsable
        /// </summary>
        ReadOnlyCollection<string> SaveFileDialogFilters { get; }
        /// <summary>
        /// Any applicable OpenFileDialog filters. Should be non-browsable
        /// </summary>
        ReadOnlyCollection<string> OpenFileDialogFilters { get; }

        /// <summary>
        /// Whether or not this module uses Variables. If false, all operations relating to Variables will be skipped
        /// </summary>
        bool UsesVariables { get; }

        /// <summary>
        /// Parses the given file to the given output. Modules may also use this function to do holistic operations (such as decryption) on the given file
        /// </summary>
        /// <param name="filename">The file to parse</param>
        /// <param name="output">The list to parse to</param>
        void ParseTo(string filename, ref List<FileFragmentReference> output); //TODO add functionality for errors that can continue?
        /// <summary>
        /// Updates the variables of the FileFragment at the given index
        /// </summary>
        /// <param name="list">The list containing the FileFragment that needs its variables updated</param>
        /// <param name="index">The index of the FileFragment that needs its variables updated</param>
        /// <returns>How many items had their variables edited</returns>
        bool UpdateVariables(IList<FileFragment> list, ref int index, ref List<int> changed);
        /// <summary>
        /// Updates the validity of the FileFragment at the given index
        /// </summary>
        /// <param name="list">The list containing the FileFragment that needs its validity updated</param>
        /// <param name="index">The index of the FileFragment that needs its validity updated</param>
        void UpdateValidity(IList<FileFragment> list, int index); //TODO bring back ref so the module can skip ahead?
        /// <summary>
        /// Does any last holistic operation (such as encrypting) needed to make a saved file valid. 
        /// </summary>
        /// <param name="filename">The file the module can edit</param>
        void PostSave(string filename);
    }
}
