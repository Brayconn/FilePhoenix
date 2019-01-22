#define debug

using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Design;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using static FileSplitterExtensions.HelperMethods;

namespace FileSplitter
{
    /// <summary>
    /// Specifies how a FileFragment conforms to a given file format's specification.
    /// </summary>
    public enum Validity
    {
        /// <summary>
        /// Specifies that the FileFragment has yet to have its validity checked.
        /// </summary>
        Unchecked,
        /// <summary>
        /// Specifies that the FileFragment conforms to the given file format's specification.
        /// </summary>
        Valid,
        /// <summary>
        /// Specifies that the FileFragment does NOT conform to the given file format's specification, and will most likely prevent the file from being opened by most programs.
        /// </summary>
        HardInvalid,
        /// <summary>
        /// Specifies that the FileFragment, despite being invalid according to the file format's specification, is not going to prevent the file from being openable by most programs. (This is mostly for use with reserved space, or FileFragments specified to always be a static value)
        /// </summary>
        SoftInvalid,
        /// <summary>
        /// Specifies that it is unknown as to whether or not the FileFragment conforms to the given file format's specification.
        /// </summary>
        Unknown,
        /// <summary>
        /// Specifies that the FileFragment is, in and of itself, another file format entirley. Thus, it is unreasonable for the file type module to try and validate it.
        /// </summary>
        OutOfScope,
        /// <summary>
        /// Specifies that the FileFragment's validity can never be invalid, and should therefor be ignored in future validity checking sessions
        /// </summary>
        Irrelevant,
        /// <summary>
        /// Specifies that an error occured during the checking of the FileFragment's validity. It will be skipped for this validity checking session
        /// </summary>
        Error
    }

    /// <summary>
    /// Specifies when a FileSplitter instance will save the loaded file
    /// </summary>
    public enum AutoSaveModes
    {
        Off,
        ValidOnly,
        On
    }

    /// <summary>
    /// Specifies how a FileSplitter will generate file names when auto saving
    /// </summary>
    public enum FileNameModes
    {
        Iterate,
        Overwrite
    }

    /// <summary>
    /// Specifies when validation shouold occur
    /// </summary>
    public enum ValidationModes
    {
        Never,
        //OnChange, //RIP OnChange, stopped making sense after chronologial changeQueue parsing was dropped
        AfterChanges,
        OnSave
    }

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

    public class FileSplitterProgressInfo
    {
        public string MethodDescription { get; private set; }

        public string ProgressDescription { get; private set; }

        public int ProgressPercentage { get; private set; }

        public FileSplitterProgressInfo(string methodDescription, string progressDescription, int progressPercentage)
        {
            this.MethodDescription = methodDescription;
            this.ProgressDescription = progressDescription;
            this.ProgressPercentage = progressPercentage;
        }
    }

    public class FileSplitter : IDisposable
    {
        #region Saftey Checks

        /// <summary>
        /// Whether or not a file is currently loaded
        /// </summary>
        [Browsable(false)]
        public bool FileLoaded => VirtualFile.Count > 0;

        /// <summary>
        /// Whether or not the values needed for the program to open FILES are set
        /// </summary>
        bool LoadFileOk => !string.IsNullOrWhiteSpace(OpenedFile) && !string.IsNullOrWhiteSpace(FileFragmentExtension);
        
        /// <summary>
        /// Whether or not the values needed for the program to open ANYTHING are set
        /// </summary>
        bool LoadAnythingOk => FileTypeModule != null && !string.IsNullOrWhiteSpace(WorkingDirectory);
                
        #endregion

        #region Enabling

        private int oldModuleHash;
        private string oldWorkingDirectory; //TODO unused...
        private string oldOpenedFile;

        private bool _Enabled = false;
        [Category("General"), Description("Whether or not the program is active"), DefaultValue(false)]
        public bool Enabled
        {
            get => _Enabled;
            set
            {
                //_Enabled can only be true if BaseOk is true too, but it can be false whenever it wants
                value &= LoadAnythingOk;
                if (value != Enabled)
                {                    
                    _Enabled = value;

                    switch(Enabled)
                    {
                        case (false):
                            /* Since Enabled starts out false, and we can't set to a value we already are,
                             * reaching this point requires Enabling, then Diabling, which means that a
                             * Workingdirectorywatcher MUST have been created, so this is safe
                             * QED
                             */
                            WorkingDirectoryWatcher.EnableRaisingEvents = false;

                            //Storing the last used important stuff
                            oldModuleHash = FileTypeModule.GetHashCode(); //Logging a hash 'cause storing the entire module is lame
                            oldWorkingDirectory = WorkingDirectoryWatcher.Path; //strings are ok
                            oldOpenedFile = OpenedFile;
                            break;
                        case (true):
                            //If either the module or selected file are changed, we need to start from scratch
                            if (oldModuleHash != FileTypeModule.GetHashCode() || oldOpenedFile != OpenedFile)
                            {
                                AutoInit();
                            }
                            /*See MoveWorkingDirectory for more
                            //If only the working directory was changed, we can just move our progress over to there
                            else if (oldWorkingDirectory != WorkingDirectory)
                            {
                                
                                MoveWorkingDirectory(oldWorkingDirectory, WorkingDirectory);
                                Refresh();
                            }
                            */
                            //Otherwise try to resume from where we left off
                            else
                            {
                               Task.Run(() => Refresh());
                            }
                            break;
                    }                                   
                }
            }
        }

        #endregion
        
        #region File opening

        private class OpenFileNameEditor : UITypeEditor
        {
            public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => UITypeEditorEditStyle.Modal;

            public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
            {
                if (!(context.Instance is FileSplitter parent) || context == null || provider == null)
                    return base.EditValue(context, provider, value);

                using (OpenFileDialog ofd = new OpenFileDialog()
                {
                    Title = context.PropertyDescriptor.DisplayName,
                    Filter = string.Join("|", (parent.FileTypeModule?.OpenFileDialogFilters
                        ?? new ReadOnlyCollection<string>(new string[0])).Concat(new string[] { "All Files (*.*)|*.*" })),
                })
                {
                    ofd.FileName = value as string ?? ofd.FileName;

                    if (ofd.ShowDialog() == DialogResult.OK
                        && (parent.FileLoaded //TODO this is a mess of conditionals
                        ? MessageBox.Show("Changing this setting will result in a loss of all current progress, " +
                        "are you sure you want to change it?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes
                        : true))
                    {
                        value = ofd.FileName;
                    }
                }
                return value;
            }
        }
        string _openedFile;
        [Category("General"), Description("The opened file"), Editor(typeof(OpenFileNameEditor), typeof(UITypeEditor))]
        public string OpenedFile
        {
            get => _openedFile;
            set
            {
                //If the user is trying to enter an actual file, it has to exist
                if (!string.IsNullOrWhiteSpace(value) && !File.Exists(value))
                    return;
                CleanPreviousFlattenedTempFile();
                _openedFile = value;
                AutoInit();
            }
        }

        #endregion

        #region Module Selector

        private class FileSplitterModuleTypeConverter : ExpandableObjectConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                return (context.Instance is FileSplitter parent)
                    ? new StandardValuesCollection(parent.LoadedModules.Keys)
                    : base.GetStandardValues(context);
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                return (value is string input && context.Instance is FileSplitter parent && parent.LoadedModules.ContainsKey(input))
                        ? (IFileSplitterModule)Activator.CreateInstance(parent.LoadedModules[input])
                        : null; //TODO maybe reconsider using this again? base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return (destinationType == typeof(string)) || base.CanConvertFrom(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                return (value is IFileSplitterModule module)
                    ? module.DisplayName
                    : base.ConvertTo(context, culture, value, destinationType);
            }
        }
        private IFileSplitterModule _FileTypeModule;
        [Category("General"), Description("What file type to treat the given file as"), TypeConverter(typeof(FileSplitterModuleTypeConverter))]
        public IFileSplitterModule FileTypeModule
        {
            get => _FileTypeModule;
            set
            {
                //TODO make file type module saftey check more meaningful
                if (value != null)
                {
                    //TODO move error message out of setter
                    if (FileLoaded && MessageBox.Show("Changing this setting will result in a loss of all current progress, are you sure you want to change it?", "Warning", MessageBoxButtons.YesNo) != DialogResult.Yes)
                        return;
                    _FileTypeModule = value;
                    if(Enabled)
                        AutoInit();
                }
            }
        }

        #endregion

        #region Working Directory

        private FileSystemWatcher WorkingDirectoryWatcher;
        string _workingDirectory;
        class OpenFolderNameEditor : FolderNameEditor
        {
            public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
            {
                //TODO check ?.
                if (context?.Instance == null || provider == null || !CanUseBetterFolderBrowser)
                    return base.EditValue(context, provider, value);

                using (CommonOpenFileDialog folderPicker = new CommonOpenFileDialog()
                {
                    IsFolderPicker = true
                })
                {
                    if (value.GetType() == typeof(string))
                        folderPicker.InitialDirectory = Path.GetDirectoryName((string)value);

                    //TODO another mess of conditionals
                    if (folderPicker.ShowDialog() == CommonFileDialogResult.Ok &&
                        (Directory.EnumerateFileSystemEntries(folderPicker.FileName).Any() ?
                        MessageBox.Show("The contents of the selected folder will be deleted if/when a file is loaded. " +
                        "Are you sure you want to continue?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes
                        : true))
                    {
                        value = folderPicker.FileName;
                    }
                }
                return value;
            }
        }
        [Category("General"), Description("The directory where the contents of the opened file will be dumped"), Editor(typeof(OpenFolderNameEditor), typeof(UITypeEditor))]
        public string WorkingDirectory
        {
            get => _workingDirectory;
            set
            {
                #region Saftey Checks
                //HACK?
                /* Things we need to check for:
                 * 1 - is the value null, or white space?
                 * 2 - is the value actually a valid path?
                 * 3 - is the value the same as the one we already have stored?
                 * 4 - if the value exists, is it a system directory?
                 * 
                 * All of those have to be FALSE to set the value
                 */
                DirectoryInfo newDirInfo = new DirectoryInfo(value);
                if (value == _workingDirectory || string.IsNullOrWhiteSpace(value))
                    return;

                if (newDirInfo.Exists)
                {
                    //MessageBox.Show("In the interest of data saftey, you cannot select a working directory that already contains files (right now, anyways)");
                    //TODO seperate error to somewhere else
                    if (newDirInfo.Attributes.HasFlag(FileAttributes.System))
                        return;
                }
                #endregion

                /*See MoveWorkingDirectory's comment for more
                //We can GUARENTEE that the WorkingDirectoryWatcher is set at this point, since enabling will force it to be set
                if (Enabled)
                    MoveWorkingDirectory(WorkingDirectoryWatcher.Path, value);
                */
                /* Used to be storing the oldWorkingDirectory here too, but that's redundant, since the only way it was hit
                 * was by Enabling, then Disabling, which meant oldWorkingDirectory had already been updated
                 */                
                _workingDirectory = value;
                if (Enabled)
                    AutoInit(); //TODO this might cause issues, but I don't see many other options?
            }
        }
        /*This code was super dangerous and wouldn't work in all cases, so it has been removed.
         * If you want to move a workingdirectory, just disable, move it, redirect, and hit enable.
        /// <summary>
        /// Moves the previous at oldPath to the new one at newPath. This method assumes that WorkingDirectoryWatcher != null
        /// </summary>
        /// <param name="oldPath">the location of the old working directory</param>
        /// <param name="newPath">the location of the new working directory</param>
        private void MoveWorkingDirectory(string oldPath, string newPath)
        {
            WorkingDirectoryWatcher.EnableRaisingEvents = false;

            ClearDirectory(newPath);
            FileSystem.MoveDirectory(oldPath, newPath, true); //HACK Maybe replace with custom MoveContents() method?
            Directory.CreateDirectory(oldPath); //HACK creating the moved directory after it was moved seems messy...

            //WARNING: be sure to ALWAYS make sure _workingDirectory is up to date after running this function, otherwise things could get out of sync
            WorkingDirectoryWatcher.Path = newPath;
            WorkingDirectoryWatcher.EnableRaisingEvents = true;
        }
        */
        
            /// <summary>
        /// Initilizes the WorkingDirectoryWatcher to whatever the value of WorkingDirectory is, or updates the existing workingdirectorywatcher should one exist
        /// </summary>
        private void UpdateWorkingDirectoryWatcher()
        {
            //Update any existing watcher
            if (WorkingDirectoryWatcher != null)
                WorkingDirectoryWatcher.Path = WorkingDirectory;
            //Init if there isn't one
            else
            {
                WorkingDirectoryWatcher = new FileSystemWatcher(WorkingDirectory)
                {
                    IncludeSubdirectories = true
                };
                WorkingDirectoryWatcher.Changed += WorkingDirectoryWatcher_Triggered;
                WorkingDirectoryWatcher.Created += WorkingDirectoryWatcher_Triggered;
                WorkingDirectoryWatcher.Deleted += WorkingDirectoryWatcher_Triggered;
                WorkingDirectoryWatcher.Renamed += WorkingDirectoryWatcher_Triggered;
            }
            //If you're calling this method I would really hope you want it to be enabled
            WorkingDirectoryWatcher.EnableRaisingEvents = true;
        }
        private void WorkingDirectoryWatcher_Triggered<T>(object sender, T e) where T : FileSystemEventArgs
        {
            //TODO this might cause unwanted slowdown, but might be worth it to not trigger unnessecarily...?
            if (!Blacklist.Any(p => StrictMatchPattern(p, e.FullPath)))
            {
                UpdateTimer.Stop();
                #region Directory
                if (Directory.Exists(e.FullPath)) //If it's a directory, the only thing we care about is if it affects the working directory
                {
                    if (e.FullPath == WorkingDirectory)
                    {
                        switch (e.ChangeType)
                        {
                            case (WatcherChangeTypes.Deleted): //Stop everything if the user deletes the working directory
                                Enabled = false;
                                WorkingDirectory = null; //TODO this does nothing right now
                                break;
                            case (WatcherChangeTypes.Renamed):
                                RenamedEventArgs re = e as RenamedEventArgs;
                                if (re.OldFullPath == WorkingDirectory) //Update the working directory if it gets renamed TODO UNTESTED
                                    WorkingDirectory = WorkingDirectoryWatcher.Path = re.FullPath;
                                break;
                        }
                    }
                }
                #endregion
                #region File
                else
                {
                    switch (e.ChangeType)
                    {
                        case (WatcherChangeTypes.Changed):
                            if (VirtualFile.ContainsKey(e.FullPath))
                                goto case (WatcherChangeTypes.Created);
                            break;
                        case (WatcherChangeTypes.Created):
                            if (ChangeQueue.ContainsKey(e.FullPath))
                                ChangeQueue[e.FullPath] = e;
                            else
                                ChangeQueue.Add(e.FullPath, e);
                            break;
                        case (WatcherChangeTypes.Deleted):
                            if (VirtualFile.ContainsKey(e.FullPath)) //If the file that was deleted exists in the virtual file, add it to the queue
                            {
                                if (ChangeQueue.ContainsKey(e.FullPath))
                                    ChangeQueue[e.FullPath] = e;
                                else
                                    ChangeQueue.Add(e.FullPath, e);
                            }
                            else if (ChangeQueue.ContainsKey(e.FullPath)) //If it doesn't, but it does exist in the change queue, remove it
                            {
                                ChangeQueue.Remove(e.FullPath);
                            }
                            break;
                        case (WatcherChangeTypes.Renamed):
                            RenamedEventArgs re = e as RenamedEventArgs;
                            //If the old file exists in the virtual file, that means it's a first time rename
                            if (VirtualFile.ContainsKey(re.OldFullPath))
                            {
                                //Original filename was "deleted"
                                var deletedFileSystemEventArgs = new FileSystemEventArgs(WatcherChangeTypes.Deleted, WorkingDirectory, re.OldName);
                                if (ChangeQueue.ContainsKey(re.OldFullPath))
                                    ChangeQueue[re.OldFullPath] = deletedFileSystemEventArgs;
                                else
                                    ChangeQueue.Add(re.OldFullPath, deletedFileSystemEventArgs);
                            }
                            //If it only exists in the change queue, then this is a rename of a rename
                            else if (ChangeQueue.ContainsKey(re.OldFullPath))
                            {
                                //Delete the inbetween rename
                                ChangeQueue.Remove(re.OldFullPath);
                            }
                            //New filename was "created"
                            ChangeQueue.Add(re.FullPath, new FileSystemEventArgs(WatcherChangeTypes.Created, WorkingDirectory, re.Name));
                            break;
                    }
                }
                #endregion
                UpdateTimer.Start();
            }
        }
        #endregion

        #region Auto-Save Path

        class SaveFileNameEditor : UITypeEditor
        {
            public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => UITypeEditorEditStyle.Modal;

            public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
            {
                if (!(context.Instance is FileSplitter parent) || context == null || provider == null)
                    return base.EditValue(context, provider, value);

                using (SaveFileDialog sfd = new SaveFileDialog()
                {
                    Title = context.PropertyDescriptor.DisplayName,
                    //Join the SaveFileDialogFilters if they exist, either way, add "All Files (*.*)|*.* to the end
                    Filter = string.Join("|", (parent.FileTypeModule?.SaveFileDialogFilters
                        ?? new ReadOnlyCollection<string>(new string[0])).Concat(new string[] { "All Files (*.*)|*.*" })),
                })
                {
                    sfd.FileName = value as string ?? sfd.FileName;

                    if (sfd.ShowDialog() == DialogResult.OK)
                        value = sfd.FileName;
                }

                return value;
            }
        }
        [Category("Auto-saving"), Description("The file that will be auto-saved to"), Editor(typeof(SaveFileNameEditor), typeof(UITypeEditor))]
        public string AutoSavePath { get; set; } //TODO consider limiting to anything but the working directory (startswith)

        #endregion

        #region File naming

        private static Regex iterationRegex = new Regex(@"\((\d+)\)$");
        [Category("Auto-saving"), Description("The file naming mode to use for auto-saving"), DefaultValue(FileNameModes.Iterate)]
        public FileNameModes FileNamingMode { get; set; } = FileNameModes.Iterate;

        #endregion

        #region Blacklist

        class BlackListTypeConverter : TypeConverter
        {
            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                return (destinationType == typeof(string) && value is IList<string> input)
                ? string.Join(", ", input)
                : base.ConvertTo(context, culture, value, destinationType);
            }
        }

        //TODO implement other platforms
        private static readonly ReadOnlyDictionary<PlatformID, string[]> defaultBlacklists = new ReadOnlyDictionary<PlatformID, string[]>(new Dictionary<PlatformID, string[]>()
        {
            { PlatformID.Win32NT, new[] { @"*\Thumbs.db", @"*\Desktop.ini" } },
            { PlatformID.MacOSX, new[] {@"*\.DS_STORE"} }
        });
        [Category("General"), Description("Filters to apply to any files changed in the WorkingDirectory"), TypeConverter(typeof(BlackListTypeConverter))]
        public BindingList<string> Blacklist { get; set; }

        #endregion

        #region Timer Stuff

        [Category("General"), Description("How long to wait before reflecting changes in the WorkingDirectory (milliseconds)"), DefaultValue(1000)]
        public uint UpdateDelay { get => (uint)UpdateTimer.Interval; set => UpdateTimer.Interval = value; }

        private System.Timers.Timer UpdateTimer;
        public event Action VirtualFileUpdated = new Action(() => { });
        private void UpdateTimer_Elapsed(object sender, EventArgs e)
        {
            //Stopping any accidental triggers (usually from folders being updated)
            if (!Enabled || !ChangeQueue.Any())
                return;

            //TODO check cast vs ToList
            //HACK this isn't async safe (most of this code isn't but still)
            List<FileFragment> originalOrder = VirtualFile.Values.ToList();
            List<FileFragment> changedFileFragments = new List<FileFragment>();

            //TODO maybe move this stuff to its own function?
            int i = 100;
            foreach (var change in ChangeQueue)
            {
                switch (change.Value.ChangeType)
                {
                    case (WatcherChangeTypes.Changed):
                        VirtualFile[change.Key].Validity = Validity.Unchecked;
                        changedFileFragments.Add(VirtualFile[change.Key]);
                        break;
                    case (WatcherChangeTypes.Created):
                        VirtualFile.Add(change.Key, new FileFragment(change.Key));
                        break;
                    case (WatcherChangeTypes.Deleted):
                        VirtualFile.Remove(change.Key);
                        break;
                    case (WatcherChangeTypes.Renamed):
                        //TODO this should never be hit, but still...
                        throw new NotImplementedException("Renamed files should not appear in the changeQueue, and yet here one is... " + change.Key);
                }
                progressReporter?.Report(new FileSplitterProgressInfo("Updating Virtual File...", $"{change.Key} {change.Value.ChangeType}", i / ChangeQueue.Count));
                i += 100;
            }
            
            //Using a seperate list of only the indexes of changes
            List<int> fileFragmentsToUpdate = VirtualFile.Values
                //!contains covers added FileFragments, != covers any shifting around that happens
                .Where(x => !originalOrder.Contains(x) || VirtualFile.IndexOfValue(x) != originalOrder.IndexOf(x))
                //TODO might be slow?
                .Concat(changedFileFragments)
                .Select(x => VirtualFile.IndexOfValue(x))
                .Distinct()
                .ToList();
                        
            fileFragmentsToUpdate = UpdateVariables(fileFragmentsToUpdate);
            if (ValidationMode == ValidationModes.AfterChanges)
                ReValidate(fileFragmentsToUpdate);
            else
                Invalidate(fileFragmentsToUpdate);

            ChangeQueue.Clear();
            VirtualFileUpdated();

            #region Auto-saving
            switch (AutoSave)
            {
                case (AutoSaveModes.ValidOnly):
                    if (VirtualFile.Values.All(x => x.Validity == Validity.Valid || x.Validity == Validity.Unknown))
                        goto case (AutoSaveModes.On);
                    break;
                case (AutoSaveModes.On):                    
                    if (FileNamingMode == FileNameModes.Iterate)
                    {
                        
                        string iterationfile = AutoSavePath;
                        string extension = Path.GetExtension(iterationfile);
                        string fullpathWithoutExtension = Path.ChangeExtension(iterationfile, null);

                        //As long as the file exists, keep iterating through numbers
                        while (File.Exists(iterationfile))
                        {
                            Match result = iterationRegex.Match(fullpathWithoutExtension);
                            //Replace the existing number
                            if (result.Success)
                                fullpathWithoutExtension = iterationRegex.Replace(fullpathWithoutExtension, $"({int.Parse(result.Groups[1].Value) + 1})");
                            //Add a new number
                            else
                                fullpathWithoutExtension += $"(1)";
                            iterationfile = fullpathWithoutExtension + extension;
                        }

                        AutoSavePath = iterationfile;
                    }
                    _Save(AutoSavePath);
                    break;
            }
            #endregion
        }

        #endregion

        #region Auto-save

        
        [Category("Auto-saving"), Description("Whether or not changing the contents of the folder triggers an auto-save"), DefaultValue(AutoSaveModes.Off)]
        public AutoSaveModes AutoSave { get; set; } = AutoSaveModes.Off;

        #endregion

        #region File Fragment Extension

        [Category("General"), Description("Forces the given FileFragmentExtension to be used, even when the module specifies one")]
        public bool ForceExtension { get; set; } = false;

        [Category("General"), Description("What file extension to use when splitting a file into File Fragments")]
        public string FileFragmentExtension { get; set; }

        #endregion

        #region Validation Mode
                
        [Category("General"), Description("At what point to validate the file"), DefaultValue(ValidationModes.AfterChanges)]
        public ValidationModes ValidationMode { get; set; } = ValidationModes.AfterChanges;

        #endregion

        #region Validation


        private void Invalidate()
        {
            Invalidate(Enumerable.Range(0, VirtualFile.Count - 1).ToList());
        }
        /// <summary>
        /// Invalidates the FileFragments at the given indecies
        /// </summary>
        /// <param name="ffs">The indexes in the VirtualFile to invalidate</param>
        private void Invalidate(IList<int> ffs)
        {
            for (int i = 0; i < ffs.Count; i++)
            {
                progressReporter?.Report(new FileSplitterProgressInfo("Invalidating...", VirtualFile.Values[i].Path, (i + 1) * 100 / ffs.Count));
                VirtualFile.Values[ffs[i]].Validity = Validity.Unchecked;
            }
        }

        private void ReValidate()
        {
            ReValidate(Enumerable.Range(0, VirtualFile.Count - 1).ToList());
        }
        /// <summary>
        /// Updates the validity of any FileFragments in the VirtualFile of the given type(s)
        /// </summary>
        /// <param name="ffs">FileFragments to validate</param>
        private void ReValidate(IList<int> ffs)
        {
            for (int i = 0; i < ffs.Count; i++)
            {
                if (WorkingDirectoryWatcher != null)
                    WorkingDirectoryWatcher.EnableRaisingEvents = false;
                bool succeeded = false;
                //HACK? The idea is that if something goes wrong here the program needn't nessecarily crash...
#if !debug
                try
                {
#endif
                    progressReporter?.Report(new FileSplitterProgressInfo("Re-Validating...", $"{VirtualFile.Values[i].Path} In Progress...", (i + 1) * 100 / ffs.Count));
                    FileTypeModule.UpdateValidity(VirtualFile.Values, ffs[i]);
                    succeeded = true;
#if !debug
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
#endif
                progressReporter?.Report(new FileSplitterProgressInfo("Re-Validating...", $"{VirtualFile.Values[i].Path} {(succeeded ? "Succeeded!" : "Failed")}", (i + 1) * 100 / ffs.Count));
                if(WorkingDirectoryWatcher != null)
                    WorkingDirectoryWatcher.EnableRaisingEvents = true;
            }
        }

        private void UpdateVariables()
        {
            if(FileTypeModule.UsesVariables)
                UpdateVariables(Enumerable.Range(0, VirtualFile.Count - 1).ToList());
        }
        private List<int> UpdateVariables(IList<int> indexesToEdit)
        {
            if (!FileTypeModule.UsesVariables)
                return indexesToEdit.ToList();

            List<int> newIndexes = indexesToEdit.ToList();
            for (int i = 0; i < indexesToEdit.Count; /*Nothing to do here?*/)
            {
                //Init and progress report
                List<int> updatedIndexes = new List<int>();
                int indexToEdit = indexesToEdit[i];
                progressReporter?.Report(new FileSplitterProgressInfo("Updating Variables...", VirtualFile.Values[i].Path, (i + 1) * 100 / indexesToEdit.Count));
                
                //Keep letting the module update variables until it says it's done (returns false)
                while (FileTypeModule.UpdateVariables(VirtualFile.Values, ref indexToEdit, ref updatedIndexes)) ;

                //Add only the unique entries to newIndex
                for (int j = 0; j < updatedIndexes.Count; j++)
                    if (!newIndexes.Contains(updatedIndexes[j]))
                        newIndexes.Add(updatedIndexes[j]);

                //Put the new entries in the right place
                newIndexes.Sort();

                //Iterate through ffs until we find something greater than the last thing UpdateVariables edited
                for (int highestIndex = (updatedIndexes.Count > 0) ? updatedIndexes.Max() : int.MaxValue;
                    i < indexesToEdit.Count && indexesToEdit[i] < highestIndex; i++);                
            }
            return newIndexes;
        }

#endregion

        private Dictionary<string, FileSystemEventArgs> ChangeQueue = new Dictionary<string, FileSystemEventArgs>();
        [Browsable(false)]
        public SortedList<string, FileFragment> VirtualFile { get; private set; } = new SortedList<string, FileFragment>();

        /// <summary>
        /// Automatically decides what mode to initilize by, and does it if enabled
        /// </summary>
        private void AutoInit()
        {
            if (Enabled && LoadAnythingOk)
            {
                if (LoadFileOk)
                    Task.Run(() => InitFromFile(OpenedFile));
                else
                    Task.Run(() => InitFromDirectory(WorkingDirectory));
            }
        }
        /// <summary>
        /// Clears an entire directory of all its contents.
        /// </summary>
        /// <param name="target">Folder to clear</param>
        private static void ClearDirectory(string target)
        {
            if (new DirectoryInfo(target).Attributes.HasFlag(FileAttributes.System))
                throw new FieldAccessException("No, you can't delete System32 using FilePhoenix.");
            foreach (var directory in Directory.EnumerateDirectories(target))
                Directory.Delete(directory);
            foreach (var file in Directory.EnumerateFiles(target))
                File.Delete(file);
        }
        
        /// <summary>
        /// Loads the given file as input, discarding any current file
        /// </summary>
        /// <param name="inputFile">The file to load</param>
        private void InitFromFile(string inputFile)
        {
            if (Enabled && LoadAnythingOk && LoadFileOk)
            {
                if (Directory.Exists(WorkingDirectory) && Directory.EnumerateFileSystemEntries(WorkingDirectory).Any())
                {
                    if (MessageBox.Show("The selected working directory is not empty, all files in it will be deleted, are you sure you want to continue?", "Warning", MessageBoxButtons.YesNo) != DialogResult.Yes)
                        return;
                    else
                        ClearDirectory(WorkingDirectory);
                }
                //Copying the input file to a temp file, just in case the later ParseTo() call needs to edit the file
                string tempFile = Path.GetTempFileName();
                File.Copy(inputFile, tempFile, true);

                //Clear the preivously loaded file and init the new file list
                VirtualFile = new SortedList<string, FileFragment>();
                List<FileFragmentReference> files = new List<FileFragmentReference>();

                //Parse the file
                progressReporter?.Report(new FileSplitterProgressInfo("Initializing from file...", "Parsing...", 0));
                FileTypeModule.ParseTo(tempFile, ref files);

                //Number the output
                progressReporter?.Report(new FileSplitterProgressInfo("Initializing from file...", "Numbering...", 100));
                List<string> filenames = FilenameManager.Number(WorkingDirectory, files, FileFragmentExtension, ForceExtension);
                if (files.Count != filenames.Count)
                    throw new Exception($"File count changed during the numbering process! ({files.Count} -> {filenames.Count})\n Check that your module is putting its file fragments into the right folders.");

                //Prevent any of the usual updates from happening while the working directory is still being filled
                if (WorkingDirectoryWatcher != null) //Not safe to simply set, since this starts out null before anything's been loaded
                    WorkingDirectoryWatcher.EnableRaisingEvents = false;

                using (BinaryReader br = new BinaryReader(new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        progressReporter?.Report(new FileSplitterProgressInfo("Saving files to working directory...", filenames[i], (i + 1) * 100 / files.Count));

                        //Automatically creates any subdirectories needed (including the working directory, if it doesn't already exist)
                        Directory.CreateDirectory(Path.GetDirectoryName(filenames[i]));

                        //HACK trying to account for ulongs is annoying
                        //ulong seek
                        while ((ulong)br.BaseStream.Position != files[i].offset)
                            br.BaseStream.Seek(files[i].offset.CompareTo((ulong)br.BaseStream.Position), SeekOrigin.Current);
                        //ulong read/write
                        using (BinaryWriter bw = new BinaryWriter(new FileStream(filenames[i], FileMode.CreateNew, FileAccess.Write)))
                            for(ulong readBytes = 0; readBytes < files[i].length; readBytes++)
                                bw.Write(br.ReadByte());

                        //Add the file
                        VirtualFile.Add(filenames[i], new FileFragment(filenames[i], files[i].description, files[i].validity) { (ExpandoObject)files[i].variables });
                    }
                }
                //Don't need to keep the temp file anymore
                File.Delete(tempFile);
                //Final updates
                UpdateWorkingDirectoryWatcher(); //WorkingDirectoryWatcher.EnableRaisingEvents = true;
                VirtualFileUpdated();
            }
        }
        /// <summary>
        /// Loads the given directory as input, clearing any existing file
        /// </summary>
        /// <param name="directory">The directory to load</param>
        private void InitFromDirectory(string directory)
        {
            if (Enabled && LoadAnythingOk)
            {
                if (WorkingDirectoryWatcher != null)
                    WorkingDirectoryWatcher.EnableRaisingEvents = false;

                VirtualFile = new SortedList<string, FileFragment>();
                string[] input = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
                //It's important that we're adding things in the order they will appear in the virtualfile,
                //to make sure that FixFileFragment can be sure that the filefragments it's checking aren't going to shift around
                Array.Sort(input); //TODO investigate sorting order of GetFiles()
                //Adds placeholders for every file in the folder
                for (int i = 0; i < input.Length; i++)
                {
                    VirtualFile.Add(input[i], new FileFragment(input[i]));
                    progressReporter?.Report(new FileSplitterProgressInfo("Initializing from directory...", input[i], (i + 1) * 100 / input.Length));
                }
                //Updates their variables, then validates
                UpdateVariables();
                ReValidate();
                UpdateWorkingDirectoryWatcher();
                VirtualFileUpdated();
            }
        }

        /// <summary>
        /// Refreshes the VirtualFile to make sure it reflects the current WorkingDirectory
        /// </summary>
        private void Refresh()
        {
            if (Enabled && LoadAnythingOk)
            {
                List<int> differences = new List<int>();
                SortedList<string,FileFragment> tempFile = new SortedList<string, FileFragment>();

                string[] input = Directory.GetFiles(WorkingDirectory, "*.*", SearchOption.AllDirectories);
                Array.Sort(input);

                for (int i = 0; i < input.Length; i++)
                {
                    progressReporter?.Report(new FileSplitterProgressInfo("Refreshing the WorkingDirectory...", input[i], (i + 1) * 100 / input.Length));
                    if (VirtualFile.ContainsKey(input[i]))
                    {
                        tempFile.Add(input[i], VirtualFile[input[i]]);
                        if (i != VirtualFile.IndexOfKey(input[i]))
                            differences.Add(i);
                    }
                    else
                    {
                        tempFile.Add(input[i], new FileFragment(input[i]));
                        differences.Add(i);
                    }
                }
                VirtualFile = tempFile;
                //TODO maybe store hash codes for every file's data to catch differences in files that were edited while disabled?

                differences = UpdateVariables(differences);
                if (ValidationMode != ValidationModes.Never)
                    ReValidate(differences);
                else
                    Invalidate(differences);

                //Safe to set this because Refresh() is only called once a WorkingDirectoryWatcher has been set
                WorkingDirectoryWatcher.EnableRaisingEvents = true;
                VirtualFileUpdated();
            }
        }

        #region Flatten And Reload

        public void FlattenAndReload() => Task.Run(() => _FlattenAndReload());
        private string previousFlattenedTempFile = null;
        /// <summary>
        /// Saves the currently loaded file to a temp file, then opens that file anew
        /// </summary>
        private void _FlattenAndReload()
        {
            CleanPreviousFlattenedTempFile();
            previousFlattenedTempFile = Path.GetTempFileName();
            _Save(previousFlattenedTempFile); //Probably can't merge any of these lines, since OpenedFile must be set last...
            OpenedFile = previousFlattenedTempFile;
        }
        /// <summary>
        /// Deletes any left over temp file used for FlattenAndReload()
        /// </summary>
        private void CleanPreviousFlattenedTempFile()
        {
            if (!string.IsNullOrWhiteSpace(previousFlattenedTempFile))
            {
                try
                {
                    File.Delete(previousFlattenedTempFile);
                    previousFlattenedTempFile = null;
                }
                catch { } //TODO If the temp file doesn't exist for some reason... idk, we don't care I guess?
            }
        }

        #endregion

        public void Save(string filename) => Task.Run(() => _Save(filename));

        /// <summary>
        /// Saves the loaded file to the given path
        /// </summary>
        /// <param name="filename">Filename to save to</param>
        private void _Save(string filename)
        {
            if (ValidationMode == ValidationModes.OnSave)
                ReValidate();
            string tempFile = Path.GetTempFileName();
            using (BinaryWriter bw = new BinaryWriter(new FileStream(tempFile, FileMode.Create, FileAccess.Write)))
            {
                for(int i = 0; i < VirtualFile.Count; i++)
                {
                    //HACK probably not good practice...?
                    bool succeeded = false;
                    for (int waitCoeffeciant = 0; !succeeded; waitCoeffeciant++)
                    {
                        progressReporter?.Report(new FileSplitterProgressInfo("Saving...", filename, (i + 1) * 100 / VirtualFile.Count));
                        succeeded = false;
                        try
                        {
                            bw.Write(File.ReadAllBytes(VirtualFile.Values[i].Path));
                            succeeded = true;
                        }
                        catch (IOException)
                        {
                            System.Threading.Thread.Sleep(waitCoeffeciant);
                        }                        
                    }
                }
                //TODO Remember to check what causes the filesystemwatcher to trigger
            }
            FileTypeModule.PostSave(tempFile);
            File.Move(tempFile, filename);
        }

        #region Constructors/Module Loading

        private IProgress<FileSplitterProgressInfo> progressReporter;
        private Progress<FileSplitterProgressInfo> _Progress; //TODO see about removing this one?
        [Browsable(false)]
        public Progress<FileSplitterProgressInfo> Progress { get => _Progress; private set => progressReporter = _Progress = value; }

        /// <summary>
        /// The combination of every Module's OpenFileDialog
        /// </summary>
        private readonly List<string> allOpenFileDialogFilters;
        [Browsable(false)]
        public ReadOnlyCollection<string> AllOpenFileDialogFilters { get => allOpenFileDialogFilters.AsReadOnly(); }

        /// <summary>
        /// The module that corresponds to a given FileDialog.Index
        /// </summary>
        private readonly List<string> indexToModule;
        [Browsable(false)]
        public ReadOnlyCollection<string> IndexToModule { get => indexToModule.AsReadOnly(); }

        
        [Browsable(false)]
        public ReadOnlyDictionary<string, Type> LoadedModules { get; private set; }

        private static bool TypeIsModule(Type m) => m.IsClass && !m.IsAbstract && typeof(IFileSplitterModule).IsAssignableFrom(m);

        /// <summary>
        /// Initilizes a FileSplitter using only modules from the exe
        /// </summary>
        public FileSplitter(Progress<FileSplitterProgressInfo> progress = null) : this(true, null, null, progress) { } /*HACK that cast tho...*/
        public FileSplitter(bool autoLoad, Type[] modules, Progress<FileSplitterProgressInfo> progress = null) : this(autoLoad, modules, null, progress) { }
        public FileSplitter(bool autoLoad, string[] pluginFolders, Progress<FileSplitterProgressInfo> progress = null) : this(autoLoad, null, pluginFolders, progress) { }
        /// <summary>
        /// Mega constructor, shared by everything
        /// </summary>
        /// <param name="autoLoad">Whether or not to load modules from the exe</param>
        /// <param name="modules">What modules to load directly</param>
        /// <param name="pluginFolders">What folders to search for/load external modules from</param>
        /// <param name="progress">A Progress class to report to</param>
        public FileSplitter(bool autoLoad, Type[] modules, string[] pluginFolders, Progress<FileSplitterProgressInfo> progress = null)
        {
            List<Type> loadedTypes = new List<Type>();

            //TODO optimize all this stuff for performance (by removing linq?)
            //Exe loading
            if (autoLoad)
            {
                loadedTypes.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(x => x.GetTypes())
                    .Where(x => TypeIsModule(x)));
            }
            //Raw module lodaing
            if (modules != null)
            { 
                //Should be ok loading modules like this, since AddRange can add 0
                loadedTypes.AddRange(modules.Where(x => TypeIsModule(x)));
            }
            //Folder module loading
            if (pluginFolders != null)
            {
                foreach (var folder in pluginFolders)
                {
                    foreach (var file in Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories))
                    {
                        try
                        {
                            //Load all modules in the dll
                            loadedTypes.AddRange(Assembly.LoadFile(file).GetTypes().Where(x => TypeIsModule(x)));
                        }
                        catch (FileLoadException fle)
                        {
                            throw new FileLoadException($"Unable to load module {file}. Check that the dll hasn't been blocked by windows or your anti-virus.", file, fle);
                        }
                    }
                }
            }
            if (loadedTypes.Count <= 0) //Just in case...
                throw new Exception($"FilePhoenix is almost completly useless without loading at least one module, and you just tried to load {loadedTypes.Count}.");

            //Sorts modules alphabetically
            loadedTypes.Sort((x, y) => ((IFileSplitterModule)Activator.CreateInstance(x)).DisplayName.CompareTo(((IFileSplitterModule)Activator.CreateInstance(y)).DisplayName));

            //Lists every module by its display name
            LoadedModules = new ReadOnlyDictionary<string, Type>(loadedTypes.ToDictionary(x => ((IFileSplitterModule)Activator.CreateInstance(x)).DisplayName, y => y));

            //Sorts the list of filters alphabetically, and pairs them with the module they belong to
            SortedList<string, string> thaSuperComboList = new SortedList<string, string>();
            foreach (var module in loadedTypes.Select(x => (IFileSplitterModule)Activator.CreateInstance(x)))
                foreach (var filter in module.OpenFileDialogFilters)
                    thaSuperComboList.Add(filter, module.DisplayName);
            
            //Then splits them up into a proper FileDialog.Filter, and a list that turns a FileDialog.Index into the module's name
            allOpenFileDialogFilters = thaSuperComboList.Keys.ToList();
            indexToModule = thaSuperComboList.Values.ToList();

            //init Blacklist
            Blacklist = new BindingList<string>(defaultBlacklists.ContainsKey(Environment.OSVersion.Platform)
                ? defaultBlacklists[Environment.OSVersion.Platform] : new string[0]);

            //init Timer
            UpdateTimer = new System.Timers.Timer
            {
                AutoReset = false,
                Interval = 1000
            };
            UpdateTimer.Elapsed += UpdateTimer_Elapsed;

            //init Progress
            this.Progress = progress;
        }

        #endregion

        #region Disposing

        public bool IsDisposed { get; private set; }

        ~FileSplitter()
        {
            Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            if(!IsDisposed)
            {
                if(disposing)
                {
                    if (WorkingDirectory != null)
                    {
                        WorkingDirectoryWatcher.Changed -= WorkingDirectoryWatcher_Triggered;
                        WorkingDirectoryWatcher.Renamed -= WorkingDirectoryWatcher_Triggered;
                        WorkingDirectoryWatcher.Deleted -= WorkingDirectoryWatcher_Triggered;
                        WorkingDirectoryWatcher.Created -= WorkingDirectoryWatcher_Triggered;
                        WorkingDirectoryWatcher.Dispose();
                    }
                    UpdateTimer.Elapsed -= UpdateTimer_Elapsed;
                    UpdateTimer.Dispose();
                }
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
