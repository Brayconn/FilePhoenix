using System;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FileMerger
{
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
                //Bytes begin with 0x, or end with h (or neither, assuming the rest of the text is valid hex)
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

        //Pro tip: don't try to use strings as input and parse them differently based on a bool, it's gross.
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
}
