using System;
using System.IO;
using System.ComponentModel;
using System.Drawing.Design;
using System.Timers;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Globalization;

namespace FileMerger
{
    /// <summary>
    /// What to do in the event a SubFile's length can't be filled
    /// </summary>
    public enum LengthErrorModes
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
        ulong TotalLength
        {
            //This will not stay accurate in the rare event the total sub file length is longer than the total file,
            //but unless I do something with remove, (Maybe adding in a "reset" method that re-imports everything too?) it'll have to do
            //get => (ulong)SubFiles.Sum(x => (long)x.LengthAbsolute);
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

        class TypeSelectionTypeConverter : TypeConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) => new StandardValuesCollection(new TypeSelections[]
            {
                TypeSelections.Absolute,
                TypeSelections.Percent
            });            

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string parsed)
                {
                    parsed = parsed.ToLower();
                    switch(parsed)
                    {
                        case ("absolute"):
                            return TypeSelections.Absolute;
                        case ("percent"):
                            return TypeSelections.Percent;
                    }
                }
                return null;
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return (destinationType == typeof(string)) || base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                TypeSelections? parsed = value as TypeSelections?;
                switch (parsed)
                {
                    case (TypeSelections.Absolute):
                        return "Absolute";
                    case (TypeSelections.Percent):
                        return "Percent";
                    default:
                        return "Various";
                }
            }
        }
        //TODO maybe try and reduce copy/paste here? Pretty minor...
        [Category("General"), Description("Sets the offset type selection of ALL SubFiles"), TypeConverter(typeof(TypeSelectionTypeConverter))]
        public TypeSelections? OffsetType
        {
            get
            {
                if (SubFiles.Count <= 0)
                    return null;

                TypeSelections output = SubFiles[0].OffsetSelection;
                for(int i = 1; i < SubFiles.Count; i++)
                {
                    if (SubFiles[i].OffsetSelection != output)
                        return null;
                }
                return output;
            }
            set
            {
                if (value == null)
                    return;
                for (int i = 0; i < SubFiles.Count; i++)
                    SubFiles[i].OffsetSelection = (TypeSelections)value;
            }
        }
        [Category("General"), Description("Sets the length type selection of ALL SubFiles"), TypeConverter(typeof(TypeSelectionTypeConverter))]
        public TypeSelections? LengthType
        {
            get
            {
                if (SubFiles.Count <= 0)
                    return null;

                TypeSelections output = SubFiles[0].LengthSelection;
                for (int i = 1; i < SubFiles.Count; i++)
                {
                    if (SubFiles[i].LengthSelection != output)
                        return null;
                }
                return output;
            }
            set
            {
                if (value == null)
                    return;
                for (int i = 0; i < SubFiles.Count; i++)
                    SubFiles[i].LengthSelection = (TypeSelections)value;
            }
        }
        
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
            ulong length = TotalLength;
            for(int i = 0; i < SubFiles.Count; i++)
            {
                if (length > 0)
                {
                    SubFiles[i].OffsetPercent = SubFiles[i].OffsetAbsolute > 0 ? decimal.Divide(SubFiles[i].OffsetAbsolute, length) : 0;
                    SubFiles[i].LengthPercent = SubFiles[i].LengthAbsolute > 0 ? decimal.Divide(SubFiles[i].LengthAbsolute, length) : 0;
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
                    list[e.NewIndex].OffsetAbsolute = TotalLength;
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
        /// Calculates the absolute value of the given percent and length, and limits that value to a maximum
        /// </summary>
        /// <param name="percent"></param>
        /// <param name="length"></param>
        /// <param name="limit"></param>
        /// <returns>The new absolute value</returns>
        private ulong CalculateAbsoluteAndLimit(decimal percent, long length, ulong limit)
        {
            return Math.Min((ulong)Math.Round((percent * length) / 100, MidpointRounding.ToEven), limit);
        }
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
                        SubFiles[i].OffsetAbsolute = CalculateAbsoluteAndLimit(SubFiles[i].OffsetPercent, br.BaseStream.Length, (ulong)br.BaseStream.Length);
                    //TODO another instances where ulongs are kind of annoying...
                    //ulong seek
                    while ((ulong)br.BaseStream.Position != SubFiles[i].OffsetAbsolute)
                        br.BaseStream.Seek(SubFiles[i].OffsetAbsolute.CompareTo((ulong)br.BaseStream.Position), SeekOrigin.Current);
                    //Calculate real length
                    if(SubFiles[i].LengthSelection == TypeSelections.Percent)
                        SubFiles[i].LengthAbsolute = CalculateAbsoluteAndLimit(SubFiles[i].LengthPercent, br.BaseStream.Length, SubFiles[i].LengthLimit ?? ulong.MaxValue);
                    //ulong write
                    using (BinaryWriter bw = new BinaryWriter(new FileStream(SubFiles[i].Path, FileMode.Truncate, FileAccess.Write)))
                    {
                        int fillIncrement = 0;
                        for (ulong j = 0; j < SubFiles[i].LengthAbsolute; j++)
                        {
                            //Try to read from master file first
                            if (br.BaseStream.Position < br.BaseStream.Length)
                                bw.Write(br.ReadByte());
                            //If that fails and we're supposed to fill with some value, fill!
                            else if (SubFiles[i].LengthErrorHandling == LengthErrorModes.FillWithValue)
                                //HACK this is sort of a mess...
                                bw.Write(SubFiles[i].fillBytes[fillIncrement < SubFiles[i].fillBytes.Length
                                    ? fillIncrement++ 
                                    : fillIncrement = 0]);
                            //Otherwise, stop
                            else //if (SubFiles[i].LengthErrorHandling == LengthErrorModes.Truncate)
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
