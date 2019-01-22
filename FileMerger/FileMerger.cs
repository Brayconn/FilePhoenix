using System;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Drawing.Design;
using System.Timers;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
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
    /// Represents one section of a MasterFile, and where/how it will be exported
    /// </summary>
    public class SubFile
    {
        public static readonly ReadOnlyCollection<ulong> DefaultValues = new ReadOnlyCollection<ulong>(new ulong[]
        {
            (ulong)sbyte.MaxValue,
            byte.MaxValue,
            (ulong)short.MaxValue,
            ushort.MaxValue,
            (ulong)int.MaxValue,
            uint.MaxValue,
            (ulong)long.MaxValue,
            ulong.MaxValue
        });
        public static readonly ReadOnlyCollection<string> DefaultNames = new ReadOnlyCollection<string>(new string[]
        {
            "Signed Byte",
            "Unsigned Byte",
            "Signed 16bit int",
            "Unsigned 16bit int",
            "Signed 32bit int",
            "Unsigned 32bit int",
            "Signed 64bit int",
            "Unsigned 64bit int",
        });

        [Category("General"), Description("The file to export this section of the main file to.")]
        public string Path { get; set; }

        #region Error Handling

        [Category("Error Handling"), Description("What to do in the event the file isn't able to be filled to the specified size.")]
        public SizeErrorModes SizeErrorHandling { get; set; }
        internal byte[] fillBytes = new byte[0];
        string fillString;
        [Category("Error Handling"), Description("What value to use when filling out the file with the \"FillWithValue\" option." +
            "Surround string input with \"double quotes\", and prepend raw byte input with \"0x\", or end with \"h\".")]
        public string FillValue
        {
            get => fillString;
            set
            {
                byte[] processed = null;
                Match hex, @string;
                //Strings are in quotes
                if ((@string = Regex.Match(value, "^\"(.+)\"$")).Success)
                {
                    processed = Encoding.ASCII.GetBytes(@string.Groups[1].Value);
                }
                //Bytes begin with
                else if((hex = Regex.Match(value, "^(?:0x)?([0-9A-F]+)h?$", RegexOptions.IgnoreCase)).Success && hex.Length % 2 == 0)
                {
                    processed = new byte[hex.Groups[1].Length / 2];
                    for(int i = 0; i < processed.Length; i++)
                    {
                        processed[i] = Convert.ToByte(hex.Groups[1].Value.Substring(i * 2, 2), 16);
                    }
                }
                //Only set if the conversion actually went through
                if(processed != null)
                {
                    fillString = value;
                    fillBytes = processed;
                }
            }
        }

        #endregion

        #region Offset

        /*This was a bad idea, don't try to use strings as input and parse them differently based on a bool, it's gross
        class OffsetConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if(value is string input && context.Instance is SubFile parent)
                {
                    switch(parent.OffsetPercent)
                    {
                        case true:
                            if (decimal.TryParse(input, out decimal percent))
                                return Math.Min(Math.Max(0, percent), 100);
                            break;
                        case false:
                            if (ulong.TryParse(input, out ulong absolute))
                                return absolute;
                            break;
                    }
                    return value;
                }
                else
                    return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if(context.Instance is SubFile parent)
                {
                    return parent.OffsetPercent ? (decimal)value: (ulong)value;
                }
                else
                    return base.ConvertTo(context, culture, value, destinationType);
            }
        }
        */
        [Category("Offset"), Description("Which value to use when calculating the offset for this file's data."), DefaultValue(TypeSelections.Absolute)]
        public TypeSelections OffsetSelection { get; set; } = TypeSelections.Absolute;
        [Category("Offset"), Description("The offset of this file's data, as a percent of the total file length.")]
        public decimal OffsetPercent { get; internal set; } = 0;
        [Category("Offset"), Description("The offset of this file's data.")]
        public ulong OffsetAbsolute { get; set; }

        #endregion

        #region Length/Size

        class SizePicker : TypeConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                return (context.Instance is SubFile)
                    ? new StandardValuesCollection(DefaultNames)
                    : base.GetStandardValues(context);
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string input && context.Instance is SubFile parent)
                {
                    //Entering in nothing means there's no limit
                    if (string.IsNullOrWhiteSpace(input))
                        return null;
                    //Entering in one of the default names gies a static value
                    if (DefaultNames.Contains(input))
                        return DefaultValues[DefaultNames.IndexOf(input)];
                    //Everything else is an actual number
                    bool hex;
                    if (hex = (input.ToLower().StartsWith("0x") || input.ToLower().EndsWith("h")))
                        input = Regex.Match(input, @"^(?:0x)?([0-9A-F]+)h?$", RegexOptions.IgnoreCase).Groups[1].Value;
                    return ulong.TryParse(input, hex ? NumberStyles.HexNumber : NumberStyles.Integer, CultureInfo.CurrentCulture, out ulong output) ? output : value;
                }
                else
                    return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return (destinationType == typeof(string)) || base.CanConvertFrom(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (value == null)
                    return "None";
                if (value is ulong input && context.Instance is SubFile parent)
                {
                    return DefaultValues.Contains(input)
                        ? DefaultNames[DefaultValues.IndexOf(input)]
                        : input.ToString();
                }
                else
                    return base.ConvertTo(context, culture, value, destinationType);
            }
        }
        [Category("Length"), Description("The upper limit on how big this file should get (make sure this is bigger than SizeAbsolute, or \"None\")."), DefaultValue(null), TypeConverter(typeof(SizePicker))]
        public ulong? SizeLimit { get; set; } = null;
        [Category("Length"), Description("Which value to use when calculating the length of this file's data."), DefaultValue(TypeSelections.Absolute)]
        public TypeSelections SizeSelection { get; set; } = TypeSelections.Absolute;
        [Category("Length"), Description("The length of this file's data, as a percent of the total file length")]
        public decimal SizePercent { get; internal set; } = 0;
        [Category("Length"), Description("The length of this file's data.")]
        public ulong SizeAbsolute { get; set; }

        #endregion

        //TODO maybe add destination offsets/sizes? might be too nuanced

        public SubFile(string path)
        {
            Path = path;
            SizeAbsolute = (ulong)new FileInfo(path).Length;
            SizeLimit = DefaultValues.Concat(new[] { SizeAbsolute }).First(x => x >= SizeAbsolute);
        }

        public override string ToString() => Path;
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
            //This will not stay accurate in the rare event the total sub file length is longer than the total file, but unles I do something with remove, (Maybe adding ina a "reset" method that re-imports everything too?) it'll have to do
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
                            if (j < (ulong)br.BaseStream.Length)
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
