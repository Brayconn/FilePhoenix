using System;
using System.IO;
using System.ComponentModel;
using System.Drawing.Design;
using System.Timers;
using System.ComponentModel.Design;
using System.Windows.Forms;

namespace FileMerger
{
    /// <summary>
    /// What to do in the event a SubFile's size can't be filled
    /// </summary>
    public enum SizeErrorModes
    {
        Truncate,
        FillWithValue
    }
    /// <summary>
    /// Whether to use relative, or absolute values
    /// </summary>
    public enum TypeSelections
    {
        Absolute,
        Percent
    }

    /// <summary>
    /// Merges many SubFiles into one file, then saves back to all SubFiles on save
    /// </summary>
    public class FileMerger : IDisposable
    {
        private readonly FileSystemWatcher masterWatcher;
        [Category("General"), Description("Whether or not the main file is being watched."), DefaultValue(false)]
        public bool Enabled { get => masterWatcher.EnableRaisingEvents; set => masterWatcher.EnableRaisingEvents = value; }
        [Category("General"), Description("The main file's path.")]
        public string MasterPath
        {
            get => Path.Combine(masterWatcher.Path, masterWatcher.Filter);
            set
            {
                masterWatcher.Path = Path.GetDirectoryName(value);
                masterWatcher.Filter = Path.GetFileName(value);
            }
        }
        ulong TotalSize
        {
            //This will not stay accurate in the rare event the total sub file length is longer than the total file,
            //but unless I do something with remove, (Maybe adding in a "reset" method that re-imports everything too?) it'll have to do
            //get => (ulong)SubFiles.Sum(x => (long)x.SizeAbsolute);
            get => File.Exists(MasterPath) ? (ulong)new FileInfo(MasterPath).Length : 0;      
        }

        class SubPathEditor : CollectionEditor
        {
            public SubPathEditor() : base(typeof(BindingList<SubFile>)) { }
            protected override CollectionForm CreateCollectionForm()
            {
                CollectionForm c = base.CreateCollectionForm();
                //Checkmate @ The guy on stack overflow who said this would be messy 😎
                ((PropertyGrid)c.Controls["overArchingTableLayoutPanel"].Controls["propertyBrowser"]).HelpVisible = true;                
                return c;
            }

            protected override object CreateInstance(Type itemType)
            {
                string paths;
                using (OpenFileDialog ofd = new OpenFileDialog()
                {
                    Filter = "Any Files (*.*)|*.*"
                })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                        paths = ofd.FileName;
                    else
                        return null;
                }
                return new SubFile(paths);
            }
        }
        [Category("General"), Description("The files to export this file to."), Editor(typeof(SubPathEditor),typeof(UITypeEditor))]
        public BindingList<SubFile> SubFiles { get; private set; } = new BindingList<SubFile>();

        System.Timers.Timer UpdateTimer = new System.Timers.Timer();
        [Category("General"), Description("How long to wait before exporting"), DefaultValue(1000)]
        public uint UpdateDelay { get => (uint)UpdateTimer.Interval; set => UpdateTimer.Interval = value; }

        public FileMerger(string masterPath, params string[] subPaths)
        {
            //Deleting the file initially to start off with a clean slate (otherwise the Append later on will cause weirdness)
            File.Delete(masterPath);
            //Making sure to make this first, because otherwise all calls to MasterPath crash
            masterWatcher = new FileSystemWatcher(Path.GetDirectoryName(masterPath), Path.GetFileName(masterPath));

            //This gets subscribed first because everything is dynamically updated
            SubFiles.ListChanged += SubFilesListChanged;
            for (int i = 0; i < subPaths.Length; i++)
                SubFiles.Add(new SubFile(subPaths[i]));
            //Now subscribe to all the events, since the file should be ready
            masterWatcher.Renamed += (o,e) => MasterPath = e.FullPath;
            masterWatcher.Changed += MasterWatcher_Changed;
            //TODO what to do about deletions...?
            UpdateTimer = new System.Timers.Timer()
            {
                AutoReset = false,
                Interval = 1000
            };
            UpdateTimer.Elapsed += Export;
        }

        private void MasterWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            UpdateTimer.Stop();
            UpdateTimer.Start();
        }

        /// <summary>
        /// Updates the percents of all SubFiles when a file is added
        /// </summary>
        private void UpdatePercents()
        {
            ulong length = TotalSize;
            for(int i = 0; i < SubFiles.Count; i++)
            {
                if (length > 0)
                {
                    SubFiles[i].OffsetPercent = SubFiles[i].OffsetAbsolute > 0 ? decimal.Divide(SubFiles[i].OffsetAbsolute, length) : 0;
                    SubFiles[i].SizePercent = SubFiles[i].SizeAbsolute > 0 ? decimal.Divide(SubFiles[i].SizeAbsolute, length) : 0;
                }
            }
        }

        private void SubFilesListChanged(object sender, ListChangedEventArgs e)
        {
            BindingList<SubFile> list = (BindingList<SubFile>)sender;
            Enabled = false;
            switch(e.ListChangedType)
            {
                case ListChangedType.Reset:
                    File.Delete(MasterPath);
                    break;
                case ListChangedType.ItemAdded:
                    //Updates the offset for new items
                    list[e.NewIndex].OffsetAbsolute = TotalSize;
                    //Write the files to the file
                    using (FileStream master = new FileStream(MasterPath, FileMode.Append, FileAccess.Write))
                        using (FileStream sub = new FileStream(list[e.NewIndex].Path, FileMode.Open, FileAccess.Read))
                            sub.CopyTo(master);
                    //This makes sure that the percents ALWAYS stay up to date with the absolute values
                    UpdatePercents();                    
                    break;
            }
            Enabled = true;
        }

        /// <summary>
        /// Calculates the absolute value of the given percent and length
        /// </summary>
        /// <param name="percent"></param>
        /// <param name="length"></param>
        /// <returns>The new absolute value</returns>
        private ulong CalculateAbsolute(decimal percent, long length)
        => (ulong)Math.Round((percent * length) / 100, MidpointRounding.ToEven);

        private void Export(object sender, ElapsedEventArgs e)
        {
            if (!Enabled)
                return;
            using (BinaryReader br = new BinaryReader(new FileStream(MasterPath, FileMode.Open, FileAccess.Read)))
            {
                for(int i = 0; i < SubFiles.Count; i++)
                {
                    //Calculate real offset
                    if (SubFiles[i].OffsetSelection == TypeSelections.Percent)
                        SubFiles[i].OffsetAbsolute = CalculateAbsolute(SubFiles[i].OffsetPercent, br.BaseStream.Length);
                    //TODO another instances where ulongs are kind of annoying...
                    //ulong seek
                    while ((ulong)br.BaseStream.Position != SubFiles[i].OffsetAbsolute)
                        br.BaseStream.Seek(SubFiles[i].OffsetAbsolute.CompareTo((ulong)br.BaseStream.Position), SeekOrigin.Current);
                    //Calculate real size
                    if(SubFiles[i].SizeSelection == TypeSelections.Percent)
                        SubFiles[i].SizeAbsolute = Math.Min(
                            CalculateAbsolute(SubFiles[i].SizePercent, br.BaseStream.Length),
                            SubFiles[i].SizeLimit ?? ulong.MaxValue //Limiting the size
                            );
                    //ulong write
                    using (BinaryWriter bw = new BinaryWriter(new FileStream(SubFiles[i].Path, FileMode.Truncate, FileAccess.Write)))
                    {
                        int fillIncrement = 0;
                        for (ulong j = 0; j < SubFiles[i].SizeAbsolute; j++)
                        {
                            //Try to read from master file first
                            if (br.BaseStream.Position < br.BaseStream.Length)
                                bw.Write(br.ReadByte());
                            //If that fails and we're supposed to fill with some value, fill!
                            else if (SubFiles[i].SizeErrorHandling == SizeErrorModes.FillWithValue)
                                //HACK this is sort of a mess... also untested
                                bw.Write(SubFiles[i].fillBytes[fillIncrement < SubFiles[i].fillBytes.Length
                                    ? fillIncrement++ 
                                    : fillIncrement = 0]);
                            //Otherwise, stop
                            else //if (SubFiles[i].SizeErrorHandling == SizeErrorModes.Truncate)
                                break;
                        }
                    }                    
                }
            }
        }

        public override string ToString() => MasterPath;

        #region Disposing

        [Browsable(false)]
        public bool IsDisposed { get; private set; }
        ~FileMerger()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if(!IsDisposed)
            {
                if(disposing)
                {
                    masterWatcher.Changed -= MasterWatcher_Changed;
                    masterWatcher.Dispose();

                    UpdateTimer.Elapsed -= Export;
                    UpdateTimer.Dispose();
                }
                IsDisposed = true;
            }
        }

        #endregion
    }
}
