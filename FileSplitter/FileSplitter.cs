using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static FilePhoenix.Extensions.HelperMethods;

namespace FileSplitter
{
    #region Enums

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

#endregion

    /// <summary>
    /// Used to update any progress bar attached to this FileSplitter instance
    /// </summary>
    public class FileSplitterProgressInfo
    {
        /// <summary>
        /// Description of the current method being run
        /// </summary>
        public string MethodDescription { get; private set; }

        /// <summary>
        /// Description of the specific action inside the method being run
        /// </summary>
        public string ProgressDescription { get; private set; }

        /// <summary>
        /// How far along this oprtation is, between 0 and 100
        /// </summary>
        public int ProgressPercentage { get; private set; }

        public FileSplitterProgressInfo(string methodDescription, string progressDescription, int progressPercentage)
        {
            this.MethodDescription = methodDescription;
            this.ProgressDescription = progressDescription;
            this.ProgressPercentage = progressPercentage;
        }
    }

    /// <summary>
    /// The big one. Splits an input file into many SubFiles
    /// </summary>
    public partial class FileSplitter : IDisposable
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
        //private string oldWorkingDirectory; //TODO possibly useless?
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
                            //oldWorkingDirectory = WorkingDirectoryWatcher.Path; //strings are ok
                            oldOpenedFile = OpenedFile;
                            break;
                        case (true):
                            //If either the module or selected file are changed, we need to start from scratch
                            if (oldModuleHash != FileTypeModule.GetHashCode() || oldOpenedFile != OpenedFile)
                            {
                                AutoInit();
                            }
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
                
                /* Used to be storing the oldWorkingDirectory here too, but that's redundant, since the only way it was hit
                 * was by Enabling, then Disabling, which meant oldWorkingDirectory had already been updated
                 */                
                _workingDirectory = value;
                if (Enabled)
                    AutoInit(); //TODO this might cause issues, but I don't see many other options?
            }
        }
        /*Here lies workingDirectory movement, was super dangerous and wouldn't work in all cases.
         * If you want to move a workingdirectory, just disable, move it, redirect, and hit enable.
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
                
        [Category("Auto-saving"), Description("The file that will be auto-saved to"), Editor(typeof(SaveFileNameEditor), typeof(UITypeEditor))]
        public string AutoSavePath { get; set; } //TODO consider limiting to anything but the working directory (startswith)

        #endregion

        #region File naming

        private static Regex iterationRegex = new Regex(@"\((\d+)\)$");
        [Category("Auto-saving"), Description("The file naming mode to use for auto-saving"), DefaultValue(FileNameModes.Iterate)]
        public FileNameModes FileNamingMode { get; set; } = FileNameModes.Iterate;

        #endregion

        #region Blacklist
        
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
            
            //Get a list of all indexes that have been changed in some way
            List<int> fileFragmentsToUpdate = VirtualFile.Values
                //!Contains() covers newley added FileFragments, != covers moved FileFragments
                .Where(x => !originalOrder.Contains(x) || VirtualFile.IndexOfValue(x) != originalOrder.IndexOf(x))
                //TODO concat is slow
                .Concat(changedFileFragments)
                //Getting indexes
                .Select(x => VirtualFile.IndexOfValue(x))
                //Remove duplicates
                .Distinct().ToList();
            
            //Update the variables of these FileFragments, and update the list  incase anything new crops up
            fileFragmentsToUpdate = UpdateVariables(fileFragmentsToUpdate);
            if (ValidationMode == ValidationModes.AfterChanges)
                UpdateValidation(fileFragmentsToUpdate);
            else
                ClearValidation(fileFragmentsToUpdate);

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

        /// <summary>
        /// Clears the validity of every file fragment
        /// </summary>
        private void ClearValidation()
        {
            ClearValidation(Enumerable.Range(0, VirtualFile.Count - 1).ToList());
        }
        /// <summary>
        /// Clears the validity of the FileFragments at the given indecies
        /// </summary>
        /// <param name="ffs">The indexes in the VirtualFile to invalidate</param>
        private void ClearValidation(IList<int> ffs)
        {
            for (int i = 0; i < ffs.Count; i++)
            {
                progressReporter?.Report(new FileSplitterProgressInfo("Clearing Validation...", VirtualFile.Values[i].Path, (i + 1) * 100 / ffs.Count));
                VirtualFile.Values[ffs[i]].Validity = Validity.Unchecked;
            }
        }

        /// <summary>
        /// Updates the validity of every file fragment
        /// </summary>
        private void UpdateValidation()
        {
            UpdateValidation(Enumerable.Range(0, VirtualFile.Count - 1).ToList());
        }
        /// <summary>
        /// Updates the validity of the file fragments at the given indexes
        /// </summary>
        /// <param name="ffs">FileFragment indexes to validate</param>
        private void UpdateValidation(IList<int> ffs)
        {
            for (int i = 0; i < ffs.Count; i++)
            {
                if (WorkingDirectoryWatcher != null)
                    WorkingDirectoryWatcher.EnableRaisingEvents = false;
                bool succeeded = false;
                //HACK? The idea is that if something goes wrong here the program needn't nessecarily crash...
#if !DEBUG
                try
                {
#endif
                    progressReporter?.Report(new FileSplitterProgressInfo("Updating Validation...", $"{VirtualFile.Values[i].Path} In Progress...", (i + 1) * 100 / ffs.Count));
                    FileTypeModule.UpdateValidity(VirtualFile.Values, ffs[i]);
                    succeeded = true;
#if !DEBUG
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
#endif
                progressReporter?.Report(new FileSplitterProgressInfo("Updating Validation...", $"{VirtualFile.Values[i].Path} {(succeeded ? "Succeeded!" : "Failed")}", (i + 1) * 100 / ffs.Count));
                if(WorkingDirectoryWatcher != null)
                    WorkingDirectoryWatcher.EnableRaisingEvents = true;
            }
        }

        /// <summary>
        /// Updates the variables of every FileFragment
        /// </summary>
        private void UpdateVariables()
        {
            if(FileTypeModule.UsesVariables)
                UpdateVariables(Enumerable.Range(0, VirtualFile.Count - 1).ToList());
        }
        /// <summary>
        /// Updates the variables of the FileFragments at the given indexes
        /// </summary>
        /// <param name="indexesToEdit">The indexes to update</param>
        /// <returns>The indexes that actually got updated (this can be MORE than what was originally given)</returns>
        private List<int> UpdateVariables(IList<int> indexesToEdit)
        {
            //If the module doesn't use variables, don't even bother looping, just quit immediatly
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
                throw new IOException("No, you can't delete System32 using FileSplitter.");
            foreach (var directory in Directory.EnumerateDirectories(target))
                Directory.Delete(directory,true);
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

                //Lock the original input file, then let the module pre-parse, and return the path to the edited file it wants to actually open
                string tempFile;                
                using (FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    tempFile = FileTypeModule.PreParse(inputFile);                

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
                    throw new Exception($"File count changed during the numbering process! ({files.Count} -> {filenames.Count})\n" +
                        "Check that your module is putting its file fragments into the right folders.");

                //Prevent any of the usual updates from happening while the working directory is still being filled
                if (WorkingDirectoryWatcher != null) //Not safe to simply set, since this starts out null before anything's been loaded
                    WorkingDirectoryWatcher.EnableRaisingEvents = false;

                //Locking/opening the newley made tempFile
                using (BinaryReader br = new BinaryReader(new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        progressReporter?.Report(new FileSplitterProgressInfo("Saving files to working directory...", filenames[i], (i + 1) * 100 / files.Count));

                        //Automatically creates any subdirectories needed (including the working directory, if it doesn't already exist)
                        //Also updates the (previously empty) Path property
                        Directory.CreateDirectory(Path.GetDirectoryName(files[i].Path = filenames[i]));

                        //HACK trying to account for ulongs is annoying
                        //ulong seek
                        while ((ulong)br.BaseStream.Position != files[i].offset)
                            br.BaseStream.Seek(files[i].offset.CompareTo((ulong)br.BaseStream.Position), SeekOrigin.Current);
                        //ulong read/write
                        using (BinaryWriter bw = new BinaryWriter(new FileStream(filenames[i], FileMode.CreateNew, FileAccess.Write)))
                            for(ulong readBytes = 0; readBytes < files[i].length; readBytes++)
                                bw.Write(br.ReadByte());

                        //Add the file fragment
                        VirtualFile.Add(filenames[i], files[i]);
                    }
                }
                //If an actual temp file was provided, delete it now
                if(tempFile != inputFile)
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
                UpdateValidation();
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
                    UpdateValidation(differences);
                else
                    ClearValidation(differences);

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
                UpdateValidation();
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

        //Single arg
        public FileSplitter(bool autoLoad, Progress<FileSplitterProgressInfo> progress = null) : this(autoLoad, null, null, progress) { }
        public FileSplitter(Type[] modules, Progress<FileSplitterProgressInfo> progress = null) : this(false, modules, null, progress) { }
        public FileSplitter(string[] pluginFolders, Progress<FileSplitterProgressInfo> progress = null) : this(false, null, pluginFolders, progress) { }
        //Dual arg
        public FileSplitter(bool autoLoad, Type[] modules, Progress<FileSplitterProgressInfo> progress = null) : this(autoLoad, modules, null, progress) { }
        public FileSplitter(bool autoLoad, string[] pluginFolders, Progress<FileSplitterProgressInfo> progress = null) : this(autoLoad, null, pluginFolders, progress) { }
        public FileSplitter(Type[] modules, string[] pluginFolders, Progress<FileSplitterProgressInfo> progress = null) : this(false, modules, pluginFolders, progress) { }
        /// <summary>
        /// Mega constructor, shared by everything
        /// </summary>
        /// <param name="autoLoad">Whether or not to load modules from the EntryAssembly (and all referenced assemblies)</param>
        /// <param name="modules">Modules to load directly</param>
        /// <param name="pluginFolders">Folders to search for/load external modules from</param>
        /// <param name="progress">A Progress class to report to</param>
        public FileSplitter(bool autoLoad, Type[] modules, string[] pluginFolders, Progress<FileSplitterProgressInfo> progress = null)
        {
            List<Type> loadedTypes = new List<Type>();

            //TODO optimize all this stuff for performance (by removing linq?)
            //Exe loading
            if (autoLoad)
            {
                var entry = Assembly.GetEntryAssembly();
                loadedTypes.AddRange(
                    //Get assemblies from the main assembly, plus all referenced ones
                    new List<Assembly>() { entry }.Concat(entry.GetReferencedAssemblies()
                    .Select(x => Assembly.Load(x)))
                    //Get types from all of those
                    .SelectMany(x => x.GetExportedTypes())
                    //Filter to only modules
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
                throw new ArgumentException("Must supply at least one module to FileSplitter");

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

        [Browsable(false)]
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
