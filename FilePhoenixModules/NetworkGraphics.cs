using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using FileSplitter;
using FilePhoenix.Extensions;

namespace FilePhoenix.Modules
{
    public class NetworkGraphics : IFileSplitterModule
    {
        [Browsable(false)]
        public string DisplayName => "Network Graphics";
                
        [Browsable(false)]
        public ReadOnlyCollection<string> SaveFileDialogFilters => new ReadOnlyCollection<string>(new string[]
        {
            "Portable Network Graphics (*.png)|*.png",
            "Animated Portable Network Graphics (*.apng)|*.apng",
            "Multiple Network Graphics (*.mng)|*.mng",
            "JPEG Network Graphics (*.jng)|*.jng"
        });

        [Browsable(false)]
        public ReadOnlyCollection<string> OpenFileDialogFilters => new ReadOnlyCollection<string>(new string[]
        {
            "Network Graphics Files (*.png;*.apng;*.mng;*.jng)|*.png;*.apng;*.mng;*.jng"
        }.Concat(SaveFileDialogFilters).ToArray());


        [Browsable(false)]
        public bool UsesVariables => false;

        public bool UpdateVariables(IList<FileFragment> list, ref int index, ref List<int> changed)
        {
            //Unused
            return false;
        }

        [Description("Whether or not to automatically fix/overwrite invalid chunk Lengths (only applies when editing the chunk's data)"), DefaultValue(true)]
        public bool FixLength { get; set; } = true;

        [Description("Whether or not to automatically fix/overwrite invalid chunk CRCs"), DefaultValue(true)]
        public bool FixCRC { get; set; } = true;

        public void UpdateValidity(IList<FileFragment> list, int index)
        {
            if (index < 0) //error
            {
                return;
            }
            else if (index == 0) //header
            {
                HeaderDictionary.TryGetValue(File.ReadAllBytes(list[index].Path), out HeaderType);
                list[index].Validity = (HeaderType != NetworkGraphicTypes.Other) ? Validity.Valid : Validity.HardInvalid;
                list[index].Description = $"File type = {HeaderType} Network Graphics";
                return;
            }
            else //everything else
            {
                int chunkNumber = ((index - 1) / 4) + 1;
                switch (((index - 1) % 4) + 1)  //Switch based on chunk type
                {
                    case (1): //length
                        string lengthPath = list[index].Path;
                        long lengthLength = new FileInfo(lengthPath).Length;
                        if (lengthLength != 4)
                        {
                            list[index].Validity = (lengthLength < 4) ? Validity.HardInvalid : Validity.Unknown;
                            list[index].Description = $"Chunk {chunkNumber} Length = <unkown>";
                        }
                        else if (lengthLength == 4)
                        {
                            int lengthData = BinaryFileInterpreter.ReadFileAs<int>(lengthPath, true);
                            list[index].Validity =
                                (lengthData == new FileInfo(list[index + 2].Path).Length)
                                ? Validity.Valid : Validity.HardInvalid;
                            list[index].Description = $"Chunk {chunkNumber} Length = {lengthData}";
                        }
                        break;
                    case (2): //type
                        byte[] typeData = File.ReadAllBytes(list[index].Path);
                        list[index].Validity =
                            IsValidChunkType(typeData)
                            ? Validity.Valid
                            : Validity.HardInvalid;
                        list[index].Description = $"Chunk {chunkNumber} Type = {Encoding.ASCII.GetString(typeData)}"; //HACK
                        goto case (4);
                    case (3): //data
                        string dataPath = list[index].Path;
                        long dataLength = new FileInfo(dataPath).Length;
                        if (dataLength > int.MaxValue)
                            list[index].Validity = Validity.HardInvalid;
                        else
                        {
                            int lengthData = BinaryFileInterpreter.ReadFileAs<int>(list[index - 2].Path, true);
                            if (FixLength && dataLength != lengthData)
                            {
                                File.WriteAllBytes(list[index - 2].Path, ((int)dataLength).GetBytes(true));
                                list[index - 2].Description = $"Chunk {chunkNumber} Length = {dataLength}";
                                list[index - 2].Validity = Validity.Valid;
                            }
                            list[index].Validity = (FixLength || dataLength == lengthData) ? Validity.Valid : Validity.HardInvalid;
                        }
                        list[index].Description = $"Chunk {chunkNumber} Data length = {dataLength}";
                        goto case (4);
                    case (4): //crc
                        //int chunkStart = (((index - 1) / 4) * 4) + 1;
                        int chunkStart = ((chunkNumber - 1) * 4) + 1; //Using this since this case is goto'd a few times
                        byte[] correctCRC = CalculateCRCOf(
                                File.ReadAllBytes(list[chunkStart + 1].Path)
                                .Concat(File.ReadAllBytes(list[chunkStart + 2].Path))
                                .ToArray());
                        byte[] currentCRC = File.ReadAllBytes(list[chunkStart + 3].Path);
                        if (!correctCRC.SequenceEqual(currentCRC))
                        {
                            if (FixCRC)
                                File.WriteAllBytes(list[chunkStart + 3].Path, correctCRC);
                            list[chunkStart + 3].Validity = (FixCRC)
                                ? Validity.Valid
                                : Validity.HardInvalid;
                        }
                        else
                            list[chunkStart + 3].Validity = Validity.Valid;
                        list[chunkStart + 3].Description = $"Chunk {chunkNumber} CRC (Big Endian) = {currentCRC.ConvertTo<uint>(true)}";
                        break;
                }
                return;
            }
        }

        #region Chunk types (hopefully I actually understood the docs properly 3:)

        //TODO maybe sort these by chunk type, then by header type?

        private static readonly byte[][] pngChunkTypes =
        {
            //Critical
            Encoding.ASCII.GetBytes("IHDR"),
            Encoding.ASCII.GetBytes("PLTE"),
            Encoding.ASCII.GetBytes("IDAT"),
            Encoding.ASCII.GetBytes("IEND"),

            //Ancillary
            Encoding.ASCII.GetBytes("tRNS"),
            Encoding.ASCII.GetBytes("gAMA"),
            Encoding.ASCII.GetBytes("cHRM"),
            Encoding.ASCII.GetBytes("sRGB"),
            Encoding.ASCII.GetBytes("iCCP"),
            Encoding.ASCII.GetBytes("iTXt"),
            Encoding.ASCII.GetBytes("tEXt"),
            Encoding.ASCII.GetBytes("zTXt"),
            Encoding.ASCII.GetBytes("bKGD"),
            Encoding.ASCII.GetBytes("pHYs"),
            Encoding.ASCII.GetBytes("sBIT"),
            Encoding.ASCII.GetBytes("sPLT"),
            Encoding.ASCII.GetBytes("hIST"),
            Encoding.ASCII.GetBytes("tIME"),
            Encoding.ASCII.GetBytes("sCAL"),
            Encoding.ASCII.GetBytes("oFFs"),
            Encoding.ASCII.GetBytes("hIST"),
            Encoding.ASCII.GetBytes("pCAL"),
            Encoding.ASCII.GetBytes("fRAc"),
            Encoding.ASCII.GetBytes("gIF*"),

            //APNG Support
            Encoding.ASCII.GetBytes("acTL"),
            Encoding.ASCII.GetBytes("fcTL"),
            Encoding.ASCII.GetBytes("fdAT")
        };

        private static readonly byte[][] mngChunkTypes =
        {
            //Critical
            Encoding.ASCII.GetBytes("MHDR"),
            Encoding.ASCII.GetBytes("IHDR"),
            Encoding.ASCII.GetBytes("PLTE"),
            Encoding.ASCII.GetBytes("IDAT"),
            Encoding.ASCII.GetBytes("LOOP"),
            Encoding.ASCII.GetBytes("ENDL"),
            Encoding.ASCII.GetBytes("MEND"),
            Encoding.ASCII.GetBytes("IHDR"),
            Encoding.ASCII.GetBytes("JHDR"),
            Encoding.ASCII.GetBytes("TERM"),
            Encoding.ASCII.GetBytes("BACK"),
            Encoding.ASCII.GetBytes("SAVE"),
            Encoding.ASCII.GetBytes("SEEK"),
            Encoding.ASCII.GetBytes("DEFI"),
            Encoding.ASCII.GetBytes("JDAT"),
            Encoding.ASCII.GetBytes("JDAA"),
            Encoding.ASCII.GetBytes("JSEP"),

            //Ancillary
            Encoding.ASCII.GetBytes("tRNS"),
            Encoding.ASCII.GetBytes("eXPI"),
            Encoding.ASCII.GetBytes("pHYg"),
            Encoding.ASCII.GetBytes("gAMA"),
            Encoding.ASCII.GetBytes("cHRM"),
            Encoding.ASCII.GetBytes("sRGB"),
            Encoding.ASCII.GetBytes("iCCP"),
            Encoding.ASCII.GetBytes("iTXt"),
            Encoding.ASCII.GetBytes("tEXt"),
            Encoding.ASCII.GetBytes("zTXt"),
            Encoding.ASCII.GetBytes("bKGD"),
            Encoding.ASCII.GetBytes("pHYs"),
            Encoding.ASCII.GetBytes("sBIT"),
            Encoding.ASCII.GetBytes("tIME"),
        };

        private static readonly byte[][] jngChunkTypes =
        {
            //Critical
            Encoding.ASCII.GetBytes("JHDR"),
            Encoding.ASCII.GetBytes("IDAT"),
            Encoding.ASCII.GetBytes("JDAT"),
            Encoding.ASCII.GetBytes("JDAA"),
            Encoding.ASCII.GetBytes("JSEP"),
            Encoding.ASCII.GetBytes("IEND"),

            //Ancillary
            Encoding.ASCII.GetBytes("gAMA"),
            Encoding.ASCII.GetBytes("cHRM"),
            Encoding.ASCII.GetBytes("sRGB"),
            Encoding.ASCII.GetBytes("iCCP"),
            Encoding.ASCII.GetBytes("iTXt"),
            Encoding.ASCII.GetBytes("tEXt"),
            Encoding.ASCII.GetBytes("zTXt"),
            Encoding.ASCII.GetBytes("bKGD"),
            Encoding.ASCII.GetBytes("pHYs"),
            Encoding.ASCII.GetBytes("tIME"),
            Encoding.ASCII.GetBytes("sCAL"),
            Encoding.ASCII.GetBytes("oFFs"),
            Encoding.ASCII.GetBytes("pCAL"),
        };

        #endregion

        
        private static Dictionary<byte[], NetworkGraphicTypes> HeaderDictionary = new Dictionary<byte[], NetworkGraphicTypes>(new ByteArrayComparer())
        {
            { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, NetworkGraphicTypes.Portable},
            { new byte[] { 0x8A, 0x4D, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, NetworkGraphicTypes.Multiple},
            { new byte[] { 0x8B, 0x4A, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, NetworkGraphicTypes.JPEG}
        };
        private enum NetworkGraphicTypes
        {
            Other,
            Portable,
            Multiple,
            JPEG
        }
        private NetworkGraphicTypes HeaderType = NetworkGraphicTypes.Other;

        /// <summary>
        /// Whether or not the given byte[] is a valid chunk given the current file type
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private bool IsValidChunkType(byte[] input) //TODO replace with string input
        {
            if (input.Length != 4)
                return false;
            switch (HeaderType)
            {
                case (NetworkGraphicTypes.Other): //TODO make this check only shared and unkown everything else
                case (NetworkGraphicTypes.Portable):
                    return pngChunkTypes.Any(x => x.SequenceEqual(input));
                case (NetworkGraphicTypes.Multiple):
                    return mngChunkTypes.Any(x => x.SequenceEqual(input));
                case (NetworkGraphicTypes.JPEG):
                    return jngChunkTypes.Any(x => x.SequenceEqual(input));
                default:
                    return false;
            }
        }
        public enum ScanModes
        {
            Dynamic,
            Static
        }
        [Description("Whether to follow chunk data lengths at their word, or to search for chunk's dynamically"), DefaultValue(ScanModes.Static)]
        public ScanModes ScanMode { get; set; } = ScanModes.Static;

        //Unused
        public string PreParse(string input)
        {
            return input;
        }

        public void ParseTo(string inputFilePath, ref List<FileFragmentReference> output)
        {
            using (BinaryReader br = new BinaryReader(new FileStream(inputFilePath, FileMode.Open, FileAccess.Read)))
            {
                //TODO split header more
                HeaderDictionary.TryGetValue(br.ReadBytes(8), out HeaderType); //br.BaseStream.Seek(8, SeekOrigin.Begin);
                output.Add(new FileFragmentReference(0, 8, new string[] { "header" }, (HeaderType != NetworkGraphicTypes.Other) ? Validity.Valid : Validity.HardInvalid,$"Header Type = {HeaderType}"));
                
                for (int chunkNumber = 1; br.BaseStream.Position < br.BaseStream.Length; chunkNumber++)
                {
                    long lengthOffset = br.BaseStream.Position;
                    int lengthData = br.ReadInt32M();
                    
                    long typeOffset = br.BaseStream.Position;
                    byte[] typeData = br.ReadBytes(4);
                    
                    long dataOffset = br.BaseStream.Position;
                    long dataLength = 0;

                    if (ScanMode == ScanModes.Dynamic)
                    {
                        //Seeking 8 ahead should skip over the CRC of this chunk the length of the next one
                        //(assuming this chunk's length is 0)
                        //If it isn't, then the search continues
                        br.BaseStream.Seek(8, SeekOrigin.Current);
                        byte[] buffer = br.ReadBytes(4);
                        while (!IsValidChunkType(buffer)) //HACK
                        {
                            buffer[0] = buffer[1];
                            buffer[1] = buffer[2];
                            buffer[2] = buffer[3];
                            buffer[3] = br.ReadByte();
                            dataLength++;
                        }
                    }
                    else
                    {
                        dataLength = lengthData;
                    }
                    br.BaseStream.Seek(dataOffset, SeekOrigin.Begin);
                    byte[] dataData = br.ReadBytes((int)dataLength); //Casting to int 'cause ReadBytes can't take long

                    Validity lengthDataValidity = ((lengthData == dataLength) ? Validity.Valid : Validity.HardInvalid);
                    output.Add(new FileFragmentReference(lengthOffset, 4, new string[] { $"Chunk {chunkNumber}", "Length" }, lengthDataValidity,$"Chunk {chunkNumber} Length = {lengthData}"));
                    output.Add(new FileFragmentReference(typeOffset, 4, new string[] { $"Chunk {chunkNumber}", "Type" }, ((IsValidChunkType(typeData)) ? Validity.Valid : Validity.HardInvalid),$"Chunk {chunkNumber} Type = {Encoding.ASCII.GetString(typeData)}"));
                    output.Add(new FileFragmentReference(dataOffset, dataLength, new string[] { $"Chunk {chunkNumber}", "Data" }, lengthDataValidity,$"Chunk {chunkNumber} Data length = {dataLength}"));

                    long CRCOffset = br.BaseStream.Position;
                    byte[] CRCData = br.ReadBytes(4);
                    Validity CRCValidity = ((CalculateCRCOf(typeData.Concat(dataData).ToArray()).SequenceEqual(CRCData)) ? Validity.Valid : Validity.HardInvalid);
                    output.Add(new FileFragmentReference(CRCOffset, 4, new string[] { $"Chunk {chunkNumber}", "CRC" }, CRCValidity, $"Chunk {chunkNumber} CRC (Big Endian) = {CRCData.ConvertTo<uint>(true)}"));
                }
            }
        }

        #region CRC Stuff

        //TODO have the crc table be declared inline to save on re-calculating every time the user starts the program (might be a terrible idea)
        /// <summary>
        /// Table containing the crc of all possible byte values
        /// </summary>
        private static uint[] crc_table = null;

        /// <summary>
        /// Generates a table containing the crc of every byte value
        /// </summary>
        public static void GenerateCRCTable()
        {
            crc_table = new uint[256];

            for (uint byteToCalculate = 0; byteToCalculate < 256; byteToCalculate++)
            {
                uint workingByte = byteToCalculate;
                for (int bitIndex = 0; bitIndex < 8; bitIndex++)
                {
                    if ((workingByte & 1) == 1)
                        workingByte = 0xedb88320 ^ (workingByte >> 1);
                    else
                        workingByte >>= 1;
                }
                crc_table[byteToCalculate] = workingByte;
            }
        }

        /// <summary>
        /// Returns the crc of any byte array passed as input.
        /// </summary>
        /// <param name="input">The bytes to get the crc of.</param>
        /// <returns>The 4 byte crc of the given byte array.</returns>
        public static byte[] CalculateCRCOf(byte[] input)
        {
            uint workingCRC = 0xffffffff;

            if (crc_table == null)
                GenerateCRCTable();
            for (int i = 0; i < input.Length; i++)
            {
                workingCRC = crc_table[(workingCRC ^ input[i]) & 0xff] ^ (workingCRC >> 8);
            }
            workingCRC ^= 0xffffffff;

            return workingCRC.GetBytes(true);
        }

        #endregion

        public void PostSave(string filename)
        {
            //Unused
        }
    }
}
