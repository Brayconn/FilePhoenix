//#define MatchPatternDynamic
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
//TODO invesitgate using this for faster BigEndian conversions?
//using static System.Net.IPAddress;

namespace FilePhoenixExtensions
{
    /// <summary>
    /// Equality comparer for making byte[] comparisons actually work
    /// </summary>
    public class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] x, byte[] y)
        {
            return (x == null || y == null) ? x == y : x.SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj)
        {
            if (obj == null)
                throw new ArgumentException();
            return obj.Sum(b => b);
        }
    }

    /// <summary>
    /// Parses whole files as basic binary values
    /// </summary>
    public static class BinaryFileInterpreter
    {
        public static T ReadFileAs<T>(string path, bool bigEndian = false) where T : struct
        {
            return HelperMethods.BitConverterDict[typeof(T)](ReadFile(path, bigEndian));
        }
        public static dynamic ReadFileAs(string path, Type T, bool bigEndian = false)
        {
            return HelperMethods.BitConverterDict[T](ReadFile(path, bigEndian));
        }
        public static byte[] ReadFile(string path, bool bigEndian = false)
        {
            byte[] output = File.ReadAllBytes(path);
            if (bigEndian)
                Array.Reverse(output);
            return output;
        }
    }

    /// <summary>
    /// Provides a variety of helper Methods and Properties
    /// </summary>
    public static class HelperMethods
    {
        //TODO modify to fit linux/mac
        /// <summary>
        /// Whether or not the user's system supports the better folder browser found in Microsoft.WindowsAPICodePack.Dialogs
        /// </summary>
        public static bool CanUseBetterFolderBrowser
        {
            get
            {
                return Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 6;
            }
        }

        /// <summary>
        /// Delegate matching the internal System.IO.PatternMatcher.StrictMatchPattern method
        /// </summary>
        /// <param name="expression">Filter expression to check against</param>
        /// <param name="name">Filename/Path</param>
        /// <returns>Whether or not the input matched the expression</returns>
        public delegate bool StrictMatchPatternDelegate(string expression, string name);
        /// <summary>
        /// Passthrough to the internal System.IO.Patternmatcher.StrictMatchPattern method
        /// </summary>
        public readonly static StrictMatchPatternDelegate StrictMatchPattern;

        /// <summary>
        /// Reverses the given array if bigEndian is true
        /// </summary>
        /// <param name="value">Input array</param>
        /// <param name="bigEndian">Whether or not the value is to be interpreted as Big Endian (reversed)</param>
        /// <returns>The value in the given endianness</returns>
        internal static byte[] BigEndianProcess(byte[] value, bool bigEndian)
        {
            byte[] temp = value;
            if (bigEndian)
                Array.Reverse(temp);
            return temp;
        }

        //TODO maybe replace with void function that throws exception?
        /// <summary>
        /// Checks whether the given length is equal to the size of the given Type
        /// </summary>
        /// <param name="actualLength">Input length</param>
        /// <param name="t">Type length to compare to</param>
        /// <returns>Whether or not the input length is equal to the length of the given Type</returns>
        internal static bool LengthCheck(int actualLength, Type t) => actualLength == Marshal.SizeOf(t);

        //TODO consider adding length checks?
        /// <summary>
        /// Dictionary access to all the "BitConverter.ToXXX" methods.
        /// </summary>
        internal static readonly ReadOnlyDictionary<Type, Func<byte[], dynamic>> BitConverterDict = new ReadOnlyDictionary<Type, Func<byte[], dynamic>>(new Dictionary<Type, Func<byte[], dynamic>>()
        {
            { typeof(byte), (byte[] value) => value[0] }, //TODO maybe throw exception if length != 1?
            { typeof(short), (byte[] value) => BitConverter.ToInt16(value,0) },
            { typeof(ushort), (byte[] value) => BitConverter.ToUInt16(value,0) },
            { typeof(int), (byte[] value) => BitConverter.ToInt32(value,0) },
            { typeof(uint), (byte[] value) => BitConverter.ToUInt32(value,0) },
            { typeof(long), (byte[] value) => BitConverter.ToInt64(value,0) },
            { typeof(ulong), (byte[] value) => BitConverter.ToUInt64(value,0) },
            { typeof(float), (byte[] value) => BitConverter.ToSingle(value,0) },
            { typeof(double), (byte[] value) => BitConverter.ToDouble(value,0) },
        });

        static HelperMethods()
        {
#if MatchPatternDynamic
            StrictPatternMatch = (StrictPatternMatchDelegate)AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(x => x.GetTypes())
                .Where(t => t.Name == "PatternMatcher").Single()
                .GetMethod("StrictMatchPattern")
                .CreateDelegate(typeof(StrictPatternMatchDelegate));
#else
            StrictMatchPattern = (StrictMatchPatternDelegate)Type.GetType("System.IO.PatternMatcher, " +
                "System, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b77a5c561934e089")
                .GetMethod("StrictMatchPattern").CreateDelegate(typeof(StrictMatchPatternDelegate));
#endif
        }

    }

    /// <summary>
    /// Provides extension methods to aid in parsing binary data
    /// </summary>
    public static class ExtensionMethods
    {
        #region BinaryReader
        public static string ReadString(this BinaryReader br, int count) => new string(br.ReadChars(count));

        //TODO add generic read method

        #region Little Endian Peek
        /*TODO merge code behind the scenes...?
        private static T Peek<T>() where T : struct
        {

        }
        */
        public static bool PeekBoolean(this BinaryReader br)
        {
            bool peeked = br.ReadBoolean();
            br.BaseStream.Position--;
            return peeked;
        }
        public static byte PeekByte(this BinaryReader br)
        {
            byte peeked = br.ReadByte();
            br.BaseStream.Position--;
            return peeked;
        }
        public static sbyte PeekSByte(this BinaryReader br)
        {
            sbyte peeked = br.ReadSByte();
            br.BaseStream.Position--;
            return peeked;
        }
        //PeekChar() already exists
        public static short PeekInt16(this BinaryReader br)
        {
            short peeked = br.ReadInt16();
            br.BaseStream.Position -= 2;
            return peeked;
        }
        public static ushort PeekUInt16(this BinaryReader br)
        {
            ushort peeked = br.ReadUInt16();
            br.BaseStream.Position -= 2;
            return peeked;
        }
        public static int PeekInt32(this BinaryReader br)
        {
            int peeked = br.ReadInt32();
            br.BaseStream.Position -= 4;
            return peeked;
        }
        public static uint PeekUInt32(this BinaryReader br)
        {
            uint peeked = br.ReadUInt32();
            br.BaseStream.Position -= 4;
            return peeked;
        }
        public static long PeekInt64(this BinaryReader br)
        {
            long peeked = br.ReadInt64();
            br.BaseStream.Position -= 8;
            return peeked;
        }
        public static ulong PeekUInt64(this BinaryReader br)
        {
            ulong peeked = br.ReadUInt64();
            br.BaseStream.Position -= 8;
            return peeked;
        }
        public static float PeekSingle(this BinaryReader br)
        {
            float peeked = br.ReadSingle();
            br.BaseStream.Position -= 4;
            return peeked;
        }
        public static double PeekDouble(this BinaryReader br)
        {
            double peeked = br.ReadDouble();
            br.BaseStream.Position -= 8;
            return peeked;
        }
        public static decimal PeekDecimal(this BinaryReader br)
        {
            decimal peeked = br.ReadDecimal();
            br.BaseStream.Position -= 16;
            return peeked;
        }
        #endregion

        //TODO finish Big Endian Reads/Peeks (might be done?)
        #region Big Endian Read
        //TODO Optimize BigEndian Reads
        public static short ReadInt16M(this BinaryReader br)
        {
            return BitConverter.ToInt16(br.ReadBytes(2).Reverse().ToArray(), 0);
        }
        public static ushort ReadUInt16M(this BinaryReader br)
        {
            return BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);
        }
        public static int ReadInt32M(this BinaryReader br)
        {
            return BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);
        }
        public static uint ReadUInt32M(this BinaryReader br)
        {
            return BitConverter.ToUInt32(br.ReadBytes(4).Reverse().ToArray(), 0);
        }
        public static long ReadInt64M(this BinaryReader br)
        {
            return BitConverter.ToInt64(br.ReadBytes(8).Reverse().ToArray(), 0);
        }
        public static ulong ReadUInt64M(this BinaryReader br)
        {
            return BitConverter.ToUInt64(br.ReadBytes(8).Reverse().ToArray(), 0);
        }
        public static float ReadSingleM(this BinaryReader br)
        {
            return BitConverter.ToSingle(br.ReadBytes(4).Reverse().ToArray(), 0);
        }
        public static double ReadDoubleM(this BinaryReader br)
        {
            return BitConverter.ToDouble(br.ReadBytes(8).Reverse().ToArray(), 0);
        }
        //There is no BitConverter.ToDecimal() :(
        #endregion

        #region Big Endian Peek
        public static short PeekInt16M(this BinaryReader br)
        {
            short peeked = br.ReadInt16M();
            br.BaseStream.Position -= 2;
            return peeked;
        }
        public static ushort PeekUInt16M(this BinaryReader br)
        {
            ushort peeked = br.ReadUInt16M();
            br.BaseStream.Position -= 2;
            return peeked;
        }
        public static int PeekInt32M(this BinaryReader br)
        {
            int peeked = br.ReadInt32M();
            br.BaseStream.Position -= 4;
            return peeked;
        }
        public static uint PeekUInt32M(this BinaryReader br)
        {
            uint peeked = br.ReadUInt32M();
            br.BaseStream.Position -= 4;
            return peeked;
        }
        public static long PeekInt64M(this BinaryReader br)
        {
            long peeked = br.ReadInt64M();
            br.BaseStream.Position -= 8;
            return peeked;
        }
        public static ulong PeekUInt64M(this BinaryReader br)
        {
            ulong peeked = br.ReadUInt64M();
            br.BaseStream.Position -= 8;
            return peeked;
        }
        public static float PeekSingleM(this BinaryReader br)
        {
            float peeked = br.ReadSingleM();
            br.BaseStream.Position -= 4;
            return peeked;
        }
        public static double PeekDoubleM(this BinaryReader br)
        {
            double peeked = br.ReadDoubleM();
            br.BaseStream.Position -= 8;
            return peeked;
        }
        #endregion

        #endregion

        #region BitConverter repalcements

        public static T ConvertTo<T>(this byte[] value, bool bigEndian = false) where T : struct
        {
            return HelperMethods.BitConverterDict[typeof(T)](HelperMethods.BigEndianProcess(value, bigEndian));
        }
        public static dynamic ConvertTo(this byte[] value, Type T, bool bigEndian = false)
        {
            return HelperMethods.BitConverterDict[T](HelperMethods.BigEndianProcess(value, bigEndian));
        }
        public static byte[] GetBytes<T>(this T value, bool bigEndian = false) where T : struct
        {
            return HelperMethods.BigEndianProcess(BitConverter.GetBytes((dynamic)value), bigEndian);
        }

        #endregion
    }
}
