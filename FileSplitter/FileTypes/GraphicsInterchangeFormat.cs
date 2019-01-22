using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using FileSplitter;
using FileSplitterExtensions;

namespace FileSplitter.FileTypes
{
    class GraphicsInterchangeFormat : IFileSplitterModule
    {        
        [Browsable(false)]
        public string DisplayName => "Graphics Interchange Format";

        [Browsable(false)]
        public ReadOnlyCollection<string> SaveFileDialogFilters => new ReadOnlyCollection<string>(new string[]
        {
            "Graphics Interchange Format (*.gif)|*.gif",
            "Jeff's Image Format (*.jif)|*.jif"
        });

        [Browsable(false)]
        public ReadOnlyCollection<string> OpenFileDialogFilters => new ReadOnlyCollection<string>(new string[]
        {
            "Graphics Interchange Format Based (*.gif;*.jif)|*.gif;*.jif",
        }.Concat(SaveFileDialogFilters).ToArray());

        [Browsable(false)]
        public bool UsesVariables => true;

        [Description("Whether or not to automatically fix/overwrite invalid section Lengths (only applies when editing the section's data)"), DefaultValue(true)]
        public bool FixLength { get; set; } = true;

        private readonly static string[] headerTypes =
        {
            "GIF87a",
            "GIF89a",
            "JIF99a" //For Jeff's Image format, since it's exactly the same, just different data compression
        };

        /// <summary>
        /// Characters that mark the beginnings of recognized sections
        /// </summary>
        private readonly static byte[] SentinalTypes = new byte[]
        {
            (byte)',', //Image
            (byte)'!', //Extension
            (byte)';' //End of file
        };

        private enum SectionTypes
        {
            Header, //
            GlobalColorTablePacked,
            Sentinal, //
            LocalColorTablePacked,
            XOffset,
            YOffset,
            Width,
            Height,
            Length,
            Data,
            ExtensionType, //TODO
            ApplicationIdentifier, //
            ApplicationAuthCode, //
            GraphicsControlPacked,
            DelayTime, //
            ColorIndex, //
            PixelAspectRatio, //TODO
            ColorTable, //TODO
            Trailer, //
            EndOfFile, //
            Unknown, //
        }

        struct PackedGlobalColorTableInfo
        {
            /*
             *Exists         Sort
             *V              V
             *0  0  0  0     0  0  0  0
             *   ^-----^        ^-----^
             *   ColorRes.      BitsPer.
             */

            public bool Exists;
            public int ColorResolution;
            public bool Sort;
            public int BitsPerEntry;

            public PackedGlobalColorTableInfo(byte packed)
            {
                //TODO use something other than shift
                Exists = (byte)(packed >> 7) == 1;
                ColorResolution = ((byte)(packed << 1)) >> 5;
                Sort = ((byte)(packed << 4)) >> 7 == 1;
                BitsPerEntry = ((byte)(packed << 5)) >> 5;
            }

            public override string ToString()
            {
                return $"Packed info about the Global Color Table.\n" +
                       $"Exists = {Exists}\n" +
                       $"Color Resolution = {ColorResolution + 1}\n" +
                       $"Sorted = {Sort}\n" +
                       $"Bits Per Color Table Entry = {BitsPerEntry + 1}";
            }
        }

        struct PackedLocalColorTableInfo
        {
            /*
             *BitsPer.     LCTSort   LGCTExists
             *V-----V           V     V
             *0  0  0  0     0  0  0  0
             *         ^-----^     ^
             *         Reserved   Interlace
             */

            public byte BitsPerEntry;
            public bool Sort;
            public bool Interlace;
            public bool Exists;

            public PackedLocalColorTableInfo(byte packed)
            {
                //TODO use different/better operations than shift
                BitsPerEntry = (byte)(packed >> 5);
                //TODO maybe check reserved bits?
                Sort = (byte)(packed << 5) >> 7 == 1;
                Interlace = (byte)(packed << 6) >> 7 == 1;
                Exists = (byte)(packed << 7) >> 7 == 1;
            }

            public override string ToString()
            {
                return $"Packed info about the Local Color Table.\n" +
                       $"Exists = {Exists}\n" +
                       $"Image Data Interlaced = {Interlace}\n" +
                       $"Sorted = {Sort}\n" +
                       $"Bits Per Color Table Entry = {BitsPerEntry + 1}";
            }
        }

        struct PackedGraphicsControlInfo
        {
            private static Dictionary<byte, string> DisposalMethods = new Dictionary<byte, string>()
            {
                { 0, "Unspecified" },
                { 1, "Don't Dispose" },
                { 2, "Overwrite With Background Color" },
                //3 is invalid
                { 4, "Overwrite With Previous Image" }
            };

            /*
             *             UserInput  TransparentColor
             *                     V  V
             *0  0  0  0     0  0  0  0
             *^--------^     ^--^
             * Reserved    DisposalMethod
             */

            public byte DisposalMethod;
            public bool UserInput;
            public bool TransparentColor;

            public PackedGraphicsControlInfo(byte packed)
            {
                DisposalMethod = (byte)((byte)(packed << 4) >> 6);
                UserInput = (byte)(packed << 6) >> 7 == 1;
                TransparentColor = (byte)(packed << 7) >> 7 == 1;
            }

            public override string ToString()
            {
                return $"Packed info about the Graphics Control Extension.\n" +
                       $"Transparent Color Flag = {TransparentColor}\n" +
                       $"User Input = {UserInput}\n" +
                       $"Disposal Method = { (DisposalMethods.TryGetValue(DisposalMethod, out string d) ? d : "Invalid")}";
            }
        }

        /// <summary>
        /// Reads the Top, Left, Width, and Height int16s at the given position
        /// </summary>
        /// <param name="br">Source stream</param>
        /// <param name="folderName">Folder to assign all resulting ffrs</param>
        /// <param name="subsection">Reference to subsection counter to keep counting synced</param>
        /// <param name="MaxWidth">Max width to use during validation</param>
        /// <param name="MaxHeight">Max height to use during validation</param>
        /// <returns></returns>
        private static FileFragmentReference[] ReadLeftTopWidthHeight(BinaryReader br, string folderName, short MaxWidth, short MaxHeight)
        {
            FileFragmentReference[] output = new FileFragmentReference[4];

            //TODO review what the limits of these values actually are...
            short left = br.ReadInt16();
            short top = br.ReadInt16();
            short width = br.ReadInt16();
            short height = br.ReadInt16();

            output[0] = new FileFragmentReference(br.BaseStream.Position - 8, 2,
                new string[] { folderName, "X Offset" }, (0 <= width && left + width <= MaxWidth) ? Validity.Valid : Validity.HardInvalid)
                { { "SectionType", SectionTypes.XOffset } };
            output[1] = new FileFragmentReference(br.BaseStream.Position - 6, 2,
                new string[] { folderName, "Y Offset" }, (0 <= height && top + height <= MaxHeight) ? Validity.Valid : Validity.HardInvalid)
                { { "SectionType", SectionTypes.YOffset } };            
            output[2] = new FileFragmentReference(br.BaseStream.Position - 4, 2,
                new string[] { folderName, "Width" }, (0 <= width && left + width <= MaxWidth) ? Validity.Valid : Validity.HardInvalid)
                { { "SectionType", SectionTypes.Width } };            
            output[3] = new FileFragmentReference(br.BaseStream.Position - 2, 2,
                new string[] { folderName, "Height" }, (0 <= height && top + height <= MaxHeight) ? Validity.Valid : Validity.HardInvalid)
                { { "SectionType", SectionTypes.Height } };

            return output;
        }

        /// <summary>
        /// Returns all data inbetween the current stream position and the next valid section of data
        /// </summary>
        /// <param name="br">Stream source</param>
        /// <param name="backOff">Whether or not to exclude the last byte before the next valid section</param>
        /// <param name="fullPath">Full path to assign the file fragment reference</param>
        /// <param name="validity">Validity to assign the file fragment reference</param>
        /// <returns></returns>
        private static FileFragmentReference FindNextSection(BinaryReader br, bool backOff, string[] fullPath, Validity validity)
        {
            long StartPosition;
            for (StartPosition = br.BaseStream.Position; !SentinalTypes.Contains(br.PeekByte()); br.BaseStream.Position++) ;
            if (backOff)
                br.BaseStream.Position--;
            return new FileFragmentReference(StartPosition,br.BaseStream.Position - StartPosition, fullPath, validity) { { "SectionType", SectionTypes.Unknown } };
        }

        /// <summary>
        /// Repeatedly reads one byte as the length, then that many bytes of data, since this is done a bunch in gifs
        /// </summary>
        /// <param name="br">Stream to read from</param>
        /// <param name="folderName">Folder name for all ffrs</param>
        /// <param name="filename">The filename to be attached to each ffr</param>
        /// <param name="subsection">Reference to the subsection, so iteration continues properly</param>
        /// <param name="isDataText">Whether or not to set the description for each data chunk as a string</param>
        /// <param name="addTrailer">Whether or not to add a ffr for the 0x00 byte</param>
        /// <param name="dataValidity">What validity to assign to the data chunks</param>
        /// <returns></returns>
        private static List<FileFragmentReference> ReadDataSections(BinaryReader br, string folderName, string filename, bool isDataText, bool addTrailer, Validity dataValidity)
        {
            List<FileFragmentReference> output = new List<FileFragmentReference>();
            for(int dataNumber = 0; br.PeekByte() != 0; dataNumber++)
            {
                byte length = br.ReadByte();
                output.Add(new FileFragmentReference(br.BaseStream.Position - 1, 1,
                    new string[] { folderName, $"{filename} {dataNumber} Length" }, Validity.Valid)
                    { { "SectionType", SectionTypes.Length } } );
                output.Add(new FileFragmentReference(br.BaseStream.Position, length,
                    new string[] { folderName, $"{filename} {dataNumber} Data{(isDataText ? ".txt" : ".raw")}" }, dataValidity, isDataText ? br.ReadString(length) : "")
                    { { "SectionType", SectionTypes.Data } });
                if (!isDataText) //TODO figure out if there's any way to remove this
                    br.BaseStream.Position += length;
            }
            if (addTrailer)
            {
                output.Add(new FileFragmentReference(br.BaseStream.Position, 1,
                    new string[] { folderName, $"{filename} Trailer" }, Validity.Valid)
                    { { "SectionType", SectionTypes.Trailer } });
                br.BaseStream.Position++;
            }
            return output;
        }
        
        //TODO Anything after the end of the file is going to be ignored by a viewer, thus, their validity should be overridden with SoftInvalid(?)
        int? EndOfFileIndex;

        //I now have a newfound hatred for .gif after writing this code
        public void ParseTo(string filename, ref List<FileFragmentReference> output)
        {
            using (BinaryReader br = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read)))
            {
                //Header
                string fileHeader = br.ReadString(6);
                output.Add(new FileFragmentReference(0, 6, new string[] { "Header", "Header" },
                    headerTypes.Contains(fileHeader) ? Validity.Valid : Validity.HardInvalid, $"File header = {fileHeader}")
                    { { "SectionType", SectionTypes.Header } });
                
                //Logical Screen Descriptor
                short LSDwidth = br.ReadInt16();
                output.Add(new FileFragmentReference(6, 2, new string[] { "Header", "Logical Screen Descriptor", "Width" },
                    (LSDwidth >= 0) ? Validity.Valid : Validity.HardInvalid, $"Width = {LSDwidth}")
                    { { "SectionType", SectionTypes.Width } });
                short LSDheight = br.ReadInt16();
                output.Add(new FileFragmentReference(8, 2, new string[] { "Header", "Logical Screen Descriptor", "Height" },
                    (LSDheight >= 0) ? Validity.Valid : Validity.HardInvalid, $"Height = {LSDheight}")
                    { { "SectionType", SectionTypes.Height } });

                PackedGlobalColorTableInfo globalColorTableInfo = new PackedGlobalColorTableInfo(br.ReadByte());

                //TODO validate all of these
                output.Add(new FileFragmentReference(10, 1, new string[] { "Header", "Packed Byte" }, globalColorTableInfo.ToString())
                    { { "SectionType", SectionTypes.GlobalColorTablePacked } });

                output.Add(new FileFragmentReference(11, 1, new string[] { "Header", "Background Color Index" }, Validity.Unknown)
                    { { "SectionType", SectionTypes.ColorIndex } });

                output.Add(new FileFragmentReference(12, 1, new string[] { "Header", "Pixel Aspect Ratio" }, Validity.Unknown)
                    { { "SectionType", SectionTypes.PixelAspectRatio } });

                int GCTLength = 3 * (1 << (globalColorTableInfo.BitsPerEntry + 1));
                if (globalColorTableInfo.Exists)
                {
                    //TODO split more?
                    output.Add(new FileFragmentReference(13, (ulong)GCTLength,
                        new string[] { "Header", "Global Color Table" })
                        { { "SectionType", SectionTypes.ColorTable } });
                }
                br.BaseStream.Seek(13 + GCTLength, SeekOrigin.Begin);
                
                //Loop through sections
                //TODO seperate section counts for extensions and images
                for (int section = 1; br.BaseStream.Position < br.BaseStream.Length; section++)
                {
                    //Switch off of section header
                    switch (br.ReadByte())
                    {
                        #region Image
                        case ((byte)','):
                            output.Add(new FileFragmentReference(br.BaseStream.Position - 1, 1,
                                new string[] { $"Image {section}", "Sentinal" }, Validity.Valid)
                                { { "SectionType", SectionTypes.Sentinal } }); //I would really hope this is valid if we've made it to this case...

                            output.AddRange(ReadLeftTopWidthHeight(br, $"Image {section}", LSDwidth, LSDheight));

                            PackedLocalColorTableInfo localColorTableInfo = new PackedLocalColorTableInfo(br.ReadByte());

                            //TODO Validate?
                            output.Add(new FileFragmentReference(br.BaseStream.Position - 1, 1, new string[] { $"Image {section}", "Packed Byte" },localColorTableInfo.ToString())
                                { { "SectionType", SectionTypes.LocalColorTablePacked } });

                            if(localColorTableInfo.Exists)
                            {
                                int LCTLength = 3 * (1 << (localColorTableInfo.BitsPerEntry + 1));
                                //TODO split more?
                                output.Add(new FileFragmentReference(br.BaseStream.Position, LCTLength,
                                    new string[] { $"Image {section}", "Local Color Table" })
                                    { { "SectionType", SectionTypes.ColorTable } });
                                br.BaseStream.Seek(LCTLength, SeekOrigin.Current);
                            }

                            //TODO what even is this????????
                            output.Add(new FileFragmentReference(br.BaseStream.Position, 1, new string[] { $"Image {section}", "Unencoded Length" },
                                br.ReadByte().ToString()) { { "SectionType", SectionTypes.Unknown /*TODO uncompressed length?*/ } });

                            output.AddRange(ReadDataSections(br, $"Image {section}", "Image Data", false, true, Validity.OutOfScope));
                            break;
                        #endregion

                        #region Extension
                        case ((byte)'!'):
                            output.Add(new FileFragmentReference(br.BaseStream.Position - 1, 1,
                                new string[] { $"Extension {section}", "Sentinal" }, Validity.Valid)
                                { { "SectionType", SectionTypes.Sentinal } });

                            byte type = br.ReadByte(); //TODO Validate this by making a list of all types
                            output.Add(new FileFragmentReference(br.BaseStream.Position - 1, 1,
                                new string[] { $"Extension {section}", "Type" }, Validity.Unchecked)
                                { { "SectionType", SectionTypes.ExtensionType } });
                            switch (type)
                            {
                                #region Plain Text
                                case (0x01):
                                    byte plainTextSize = br.ReadByte(); //Should always be 0x0Ch
                                    output.Add(new FileFragmentReference(br.BaseStream.Position - 1, 1,
                                        new string[] { $"Extension {section}", "Plain Text Block Length" },
                                        plainTextSize == 0x0C ? Validity.Valid : Validity.HardInvalid)
                                        { { "SectionType", SectionTypes.Length } }); //tbh, this value not being 0x0C would be a hard invalid for filephoenix rn

                                    output.AddRange(ReadLeftTopWidthHeight(br, $"Extension {section}", LSDwidth, LSDheight));

                                    //TODO validate one byte
                                    output.Add(new FileFragmentReference(br.BaseStream.Position, 1,
                                        new string[] { $"Extension {section}", "Character Cell Width" },Validity.Valid,
                                        br.ReadByte().ToString())
                                        { { "SectionType", SectionTypes.Width } });
                                    output.Add(new FileFragmentReference(br.BaseStream.Position, 1,
                                        new string[] { $"Extension {section}", "Character Cell Height" }, Validity.Valid,
                                        br.ReadByte().ToString())
                                        { { "SectionType", SectionTypes.Height } });

                                    //TODO this is flawed since it needs to fall back to the Local Color table if !GCTExists
                                    //Also, if both the GCT and LCT aren't there? Oh boy...
                                    int maxIndex = GCTLength / 3;
                                    
                                    byte TextColorIndex = br.ReadByte();
                                    output.Add(new FileFragmentReference(br.BaseStream.Position, 1,
                                        new string[] { $"Extension {section}", "Text Color Index" },
                                        TextColorIndex < maxIndex ? Validity.Valid : Validity.HardInvalid, TextColorIndex.ToString())
                                        { { "SectionType", SectionTypes.ColorIndex } });

                                    byte BackColorIndex = br.ReadByte();
                                    output.Add(new FileFragmentReference(br.BaseStream.Position, 1,
                                        new string[] { $"Extension {section}", "Text Background Color Index" },
                                        BackColorIndex < maxIndex ? Validity.Valid : Validity.HardInvalid, BackColorIndex.ToString())
                                        { { "SectionType", SectionTypes.ColorIndex } });

                                    output.AddRange(ReadDataSections(br, $"Extension {section}", "Plain Text", true, false, Validity.Valid));
                                    break;
                                #endregion

                                #region Graphics Control
                                case (0xF9):
                                    byte graphicsControlSize = br.ReadByte(); //Should always be 0x04
                                    output.Add(new FileFragmentReference(br.BaseStream.Position - 1, 1,
                                        new string[] { $"Extension {section}", "Graphics Control Block Length" },
                                        graphicsControlSize == 0x04 ? Validity.Valid : Validity.HardInvalid)
                                        { { "SectionType", SectionTypes.Length } });

                                    PackedGraphicsControlInfo graphicsControlInfo = new PackedGraphicsControlInfo(br.ReadByte());
                                    
                                    output.Add(new FileFragmentReference(br.BaseStream.Position - 1, 1,
                                        new string[] { $"Extension {section}", "Packed Byte" }, graphicsControlInfo.ToString())
                                        { { "SectionType", SectionTypes.GraphicsControlPacked } });
                                    
                                    //In Centiseconds (hundreths of seconds)
                                    short DelayTime = br.ReadInt16();
                                    output.Add(new FileFragmentReference(br.BaseStream.Position - 2, 2,
                                        new string[] { $"Extension {section}", "Delay Time" },
                                        0 <= DelayTime ? Validity.Valid : Validity.HardInvalid) //TODO check if this is hard or soft
                                        { { "SectionType", SectionTypes.DelayTime } });

                                    output.Add(new FileFragmentReference(br.BaseStream.Position, 1,
                                        new string[] { $"Extension {section}", "Transparent Color Index" },
                                        br.ReadByte() < GCTLength / 3 ? Validity.Valid : Validity.HardInvalid) //TODO < or <=?
                                        { { "SectionType", SectionTypes.ColorIndex } }); 
                                    break;
                                    #endregion

                                #region Comment
                                case (0xFE):
                                    output.AddRange(ReadDataSections(br, $"Extension {section}", "Comment", true, false, Validity.Valid));                                  
                                    break;
                                #endregion

                                #region Application
                                case (0xFF):
                                    //TODO kind of unrealistic to parse all possible application chunks... right?
                                    byte applicationSize = br.ReadByte(); //Should always be 0x0B
                                    output.Add(new FileFragmentReference(br.BaseStream.Position - 1, 1,
                                        new string[] { $"Extension {section}", "Application Extension Length" },
                                        applicationSize == 0x0B ? Validity.Valid : Validity.HardInvalid)
                                        { { "SectionType", SectionTypes.Length } });
                                    
                                    output.Add(new FileFragmentReference(br.BaseStream.Position, 8,
                                        new string[] { $"Extension {section}", "Identifier" },
                                        Validity.Valid, br.ReadString(8))
                                        { { "SectionType", SectionTypes.ApplicationIdentifier } });

                                    output.Add(new FileFragmentReference(br.BaseStream.Position, 3,
                                        new string[] { $"Extension {section}", "Authentication Code" },
                                        Validity.Valid, string.Join(", ", br.ReadBytes(3))) //This could be human readable, or not, so byte[] jsut to be sure
                                        { { "SectionType", SectionTypes.ApplicationAuthCode } });

                                    output.AddRange(ReadDataSections(br, $"Extension {section}", "Application", false, false, Validity.Valid));
                                    break;
                                #endregion

                                #region Unknown
                                default:
                                    output.Add(FindNextSection(br, true,
                                        new string[] { $"Extension {section}", "Unknown Extension Data" }, Validity.Unknown));
                                    break;
                                    #endregion
                            }
                            output.Add(new FileFragmentReference(br.BaseStream.Position, 1,
                                            new string[] { $"Extension {section}", "Extension Block Trailer" },
                                            br.ReadByte() == 0 ? Validity.Valid : Validity.SoftInvalid)
                                            { { "SectionType", SectionTypes.Trailer } });
                            break;
                        #endregion

                        #region End of file
                        case ((byte)';'):
                            output.Add(new FileFragmentReference(br.BaseStream.Position - 1, 1,
                                new string[] { $"End Of File Marker {section}" }, 
                                (br.BaseStream.Position != br.BaseStream.Length || EndOfFileIndex != null) ? Validity.SoftInvalid : Validity.Valid)
                                { { "SectionType", SectionTypes.EndOfFile } });
                            EndOfFileIndex = output.Count - 1;
                            break;
                        #endregion

                        #region Unknown
                        default:
                            br.BaseStream.Position--;
                            output.Add(FindNextSection(br, false, new string[] { $"Unknown Section {section}" }, Validity.HardInvalid));
                            break;
                            #endregion
                    }
                }
            }
        }

        private static void UpdateOffsetOrLengthValidity(IList<FileFragment> list, int offsetIndex, int lengthIndex, int maxIndex, bool isOffset)
        {
            short max = BinaryFileInterpreter.ReadFileAs<short>(list[maxIndex].Path);
            short offset = BinaryFileInterpreter.ReadFileAs<short>(list[offsetIndex].Path);
            short length = BinaryFileInterpreter.ReadFileAs<short>(list[lengthIndex].Path);
            Validity v = (0 <= length && 0 <= offset && offset + length <= max) ? Validity.Valid : Validity.HardInvalid;
            if(v == Validity.Valid)
                list[offsetIndex].Validity = list[lengthIndex].Validity = v;
            else
                list[isOffset ? offset : length].Validity = v;
        }

        private static void UpdateLogicalScreenDescriptorValidity(IList<FileFragment> list, int index, params SectionTypes[] sections)
        {
            list[index].Validity = (0 <= BinaryFileInterpreter.ReadFileAs<short>(list[index].Path)) ? Validity.Valid : Validity.HardInvalid;
            //Need to re-validate anything that relies on the full image's width/height
            for (int i = 6; i < list.Count; i++) //items 0-5 are guarenteed to not be widths, heigths, xoffsets, or yoffsets
                if (sections.Any(x => x == list[i].variables.SectionType))
                    list[i].Validity = Validity.Unchecked;
        }

        private static readonly byte[] ValidExtensions =
        {
            0x01, //Plain Text
            0xF9, //Graphics Control
            0xFE, //Comment
            0xFF, //Application
        };

        private static readonly SectionTypes[] LengthEnders =
        {
            //Comments, and other generic "data holding sections" end on Data
            SectionTypes.Data,
            //Plain Text and Application ends on the Length (that's when all the plain text starts)
            SectionTypes.Length,
            //Graphics control ends on trailer
            SectionTypes.Trailer
        };

        //This code sucks more than BLink 😂👌
        public void UpdateValidity(IList<FileFragment> list, int index)
        {
            //Anything after the first EndOfFile marker is ignored, but could still be valid if it was removed
            if (index > EndOfFileIndex.Value)
            {
                list[index].Validity = Validity.SoftInvalid;
                return;
            }

            byte CurrentLength;
            ulong ActualLength;
            switch(list[index].variables.SectionType)
            {
                case (SectionTypes.Header):
                    list[index].Validity = headerTypes.Contains(File.ReadAllText(list[index].Path))
                        ? Validity.Valid : Validity.HardInvalid;
                    break;
                case (SectionTypes.Sentinal):
                    list[index].Validity = new FileInfo(list[index].Path).Length == 1 && SentinalTypes.Contains(BinaryFileInterpreter.ReadFileAs<byte>(list[index].Path))
                        ? Validity.Valid : Validity.HardInvalid;
                    break;

                #region Image Sizes/Offsets
                case (SectionTypes.XOffset):
                    UpdateOffsetOrLengthValidity(list, index, index + 2, 1, true);
                    break;
                case (SectionTypes.Width):
                    if (index == 1)
                        UpdateLogicalScreenDescriptorValidity(list, index, SectionTypes.Width, SectionTypes.XOffset);
                    else
                        UpdateOffsetOrLengthValidity(list, index - 2, index, 1, false);
                    break;

                case (SectionTypes.YOffset):
                    UpdateOffsetOrLengthValidity(list, index, index + 2, 2, true);
                    break;
                case (SectionTypes.Height):
                    if (index == 2)
                        UpdateLogicalScreenDescriptorValidity(list, index, SectionTypes.Height, SectionTypes.YOffset);
                    else
                        UpdateOffsetOrLengthValidity(list, index - 2, index, 2, false);
                    break;
                #endregion

                #region Packed Bytes
                case (SectionTypes.GlobalColorTablePacked):
                    list[index].Description = new PackedGlobalColorTableInfo(BinaryFileInterpreter.ReadFileAs<byte>(list[index].Path)).ToString();
                    list[index].Validity = Validity.Valid;
                    break;
                case (SectionTypes.LocalColorTablePacked):
                    list[index].Description = new PackedLocalColorTableInfo(BinaryFileInterpreter.ReadFileAs<byte>(list[index].Path)).ToString();
                    list[index].Validity = Validity.Valid;
                    break;
                case (SectionTypes.GraphicsControlPacked):
                    list[index].Description = new PackedGraphicsControlInfo(BinaryFileInterpreter.ReadFileAs<byte>(list[index].Path)).ToString();
                    list[index].Validity = Validity.Valid;
                    break;
                #endregion

                case (SectionTypes.Length):
                    byte[] filecontents = File.ReadAllBytes(list[index].Path);
                    if (filecontents.Length != 1)
                    {
                        list[index].Validity = Validity.HardInvalid;
                        break;
                    }
                    CurrentLength = filecontents[0];
                    ActualLength = 0;
                    for(int i = index + 1; !LengthEnders.Contains((SectionTypes)list[i].variables.SectionType); i++)
                        ActualLength += (ulong)new FileInfo(list[i].Path).Length;

                    list[index].Validity = CurrentLength == ActualLength ? Validity.Valid : Validity.HardInvalid;
                    break;
                case (SectionTypes.Data):
                    ActualLength = 0;
                    int lengthIndex;
                    for (lengthIndex = index; list[lengthIndex].variables.SectionType != SectionTypes.Length; lengthIndex--)
                        ActualLength += (ulong)new FileInfo(list[lengthIndex].Path).Length;
                    CurrentLength = BinaryFileInterpreter.ReadFileAs<byte>(list[lengthIndex].Path);
                    if (FixLength && (CurrentLength != ActualLength) && ActualLength <= byte.MaxValue)
                    {
                        File.WriteAllBytes(list[lengthIndex].Path, new byte[] { (byte)ActualLength });
                        list[lengthIndex].Validity = Validity.Valid;
                    }
                    list[index].Validity = CurrentLength == ActualLength ? Validity.Valid : Validity.HardInvalid;
                    break;

                case (SectionTypes.ColorIndex):
                    //TODO need to check a packed byte to validate...?
                    break;

                case (SectionTypes.DelayTime):
                    list[index].Validity = (0 <= BinaryFileInterpreter.ReadFileAs<short>(list[index].Path)) ? Validity.Valid : Validity.HardInvalid;
                    break;

                    //These could be anything, and anything could be valid, so...
                case (SectionTypes.ApplicationIdentifier):
                case (SectionTypes.ApplicationAuthCode):
                    list[index].Validity = Validity.Valid;
                    break;

                case (SectionTypes.ExtensionType):
                    //TODO need to make that list of types...
                    break;

                case (SectionTypes.Trailer):
                    if (new FileInfo(list[index].Path).Length == 1)
                        list[index].Validity = (BinaryFileInterpreter.ReadFileAs<byte>(list[index].Path) == 0x00) ? Validity.Valid : Validity.SoftInvalid;
                    else //This would cause giant collateral bad
                        list[index].Validity = Validity.HardInvalid;
                    break;

                    //TODO this might break in some cases?
                case (SectionTypes.EndOfFile):
                    //If anything about this EOF marker would make it not reconisable
                    if (new FileInfo(list[index].Path).Length == 1 && BinaryFileInterpreter.ReadFileAs<byte>(list[index].Path) == (byte)';')
                        list[index].Validity = Validity.Valid;
                    //assume the user removed it
                    else
                    {
                        list[index].Validity = Validity.Unknown;
                        //and find the next available EOF marker
                        for (int i = index + 1; i < list.Count; i++)
                        {
                            if(list[i].variables.SectionType == SectionTypes.EndOfFile)
                            {
                                //Stop once we find it
                                EndOfFileIndex = i;
                                return;
                            }
                        }
                    }
                    break;

                case (SectionTypes.PixelAspectRatio):
                case (SectionTypes.ColorTable):
                case (SectionTypes.Unknown):
                    //TODO
                    list[index].Validity = Validity.Unknown;
                    break;
            }
            return;
        }

        /// <summary>
        /// Finds the first FileFragment matching the SectionType
        /// </summary>
        /// <param name="list"></param>
        /// <param name="index"></param>
        /// <param name="stoppingPoints"></param>
        /// <returns></returns>
        private int FindSectionOfType(IList<FileFragment> list, int index, params SectionTypes[] stoppingPoints)
        {
            while (!stoppingPoints.Contains((SectionTypes)list[--index].variables.SectionType));
            return index;
        }

        private List<SectionTypes> LabelDataSections(IList<FileFragment> list, int index, bool addTrailer)
        {
            List<SectionTypes> s = new List<SectionTypes>();
            byte length;
            while ((length = BinaryFileInterpreter.ReadFileAs<byte>(list[index].Path)) != 0)
            {
                s.AddRange(new SectionTypes[]
                {
                    SectionTypes.Length,
                    SectionTypes.Data
                });
                index += 2;
            }
            if (addTrailer)
                s.Add(SectionTypes.Trailer);
            return s;
        }

        public bool UpdateVariables(IList<FileFragment> list, ref int index, ref List<int> changed)
        {
            //Starting saftey thingy
            if (index == 0)
            {
                list[index].variables.SectionType = SectionTypes.Header;
                return false;
            }

            List<SectionTypes> SectionTypeQueue = new List<SectionTypes>();
            bool exists = new PackedGlobalColorTableInfo(BinaryFileInterpreter.ReadFileAs<byte>(list[3].Path)).Exists;
            if (index <= (exists ? 6 : 5))
            {
                SectionTypeQueue.AddRange(new SectionTypes[]
                        {
                        SectionTypes.Header,
                        SectionTypes.Width,
                        SectionTypes.Height,
                        SectionTypes.GlobalColorTablePacked,
                        SectionTypes.ColorIndex,
                        SectionTypes.PixelAspectRatio
                        });
                if (exists)
                    SectionTypeQueue.Add(SectionTypes.ColorTable);
                index = 0;
            }
            else
            {
                index = FindSectionOfType(list, index, SectionTypes.Sentinal, SectionTypes.Trailer);
                switch (list[index].variables.SectionType)
                {
                    case (SectionTypes.Trailer):
                        index++;
                        goto case (SectionTypes.Sentinal);
                    case (SectionTypes.Sentinal):
                        switch(BinaryFileInterpreter.ReadFileAs<byte>(list[index].Path))
                        {
                            case ((byte)','):
                                SectionTypeQueue.AddRange(new SectionTypes[]
                                    {
                                    SectionTypes.Sentinal,
                                    SectionTypes.XOffset,
                                    SectionTypes.YOffset,
                                    SectionTypes.Width,
                                    SectionTypes.Height,
                                    SectionTypes.LocalColorTablePacked
                                    });
                                bool lctexists = new PackedLocalColorTableInfo(BinaryFileInterpreter.ReadFileAs<byte>(list[index + 5].Path)).Exists;
                                if (lctexists)
                                    SectionTypeQueue.Add(SectionTypes.ColorTable);
                                SectionTypeQueue.Add(SectionTypes.Unknown); //Unencoded length?
                                //Need to +8 if the local color table exists
                                SectionTypeQueue.AddRange(LabelDataSections(list, index + (lctexists ? 8 : 7), true));
                                break;
                            case ((byte)'!'):
                                SectionTypeQueue.AddRange(new SectionTypes[]
                                {
                                    SectionTypes.Sentinal,
                                    SectionTypes.ExtensionType
                                });
                                switch(BinaryFileInterpreter.ReadFileAs<byte>(list[index+1].Path))
                                {
                                    //Plain Text
                                    case (0x01):
                                        SectionTypeQueue.AddRange(new SectionTypes[]
                                        {
                                            SectionTypes.Length,
                                            SectionTypes.XOffset,
                                            SectionTypes.YOffset,
                                            SectionTypes.Width,
                                            SectionTypes.Height,
                                            SectionTypes.Width,
                                            SectionTypes.Height,
                                            SectionTypes.ColorIndex,
                                            SectionTypes.ColorIndex
                                        });
                                        SectionTypeQueue.AddRange(LabelDataSections(list, index + 11, false));
                                        break;

                                    //Graphics Control
                                    case (0xF9):
                                        SectionTypeQueue.AddRange(new SectionTypes[]
                                        {
                                            SectionTypes.Length,
                                            SectionTypes.GraphicsControlPacked,
                                            SectionTypes.DelayTime,
                                            SectionTypes.ColorIndex
                                        });
                                        break;

                                    //Comment
                                    case (0xFE):
                                        SectionTypeQueue.AddRange(LabelDataSections(list, index + 2, false));
                                        break;

                                    //Application
                                    case (0xFF):
                                        SectionTypeQueue.AddRange(new SectionTypes[]
                                        {
                                            SectionTypes.Length,
                                            SectionTypes.ApplicationIdentifier,
                                            SectionTypes.ApplicationAuthCode
                                        });
                                        SectionTypeQueue.AddRange(LabelDataSections(list, index + 5, false));
                                        break;
                                    
                                    //Unknown
                                    default:
                                        SectionTypeQueue.Add(SectionTypes.Unknown);
                                        break;

                                }
                                SectionTypeQueue.Add(SectionTypes.Trailer);
                                break;
                            case ((byte)';'):
                                SectionTypeQueue.Add(SectionTypes.EndOfFile);
                                EndOfFileIndex = index; //TODO check this again
                                break;
                            default:
                                SectionTypeQueue.Add(SectionTypes.Unknown);
                                break;
                        }
                        break;
                }
            }

            bool didWork = false;
            for (int i = 0; i < SectionTypeQueue.Count; i++) //TODO is this confusing having i++ all the way up here?
            {
                if (list[index].variables.SectionType != SectionTypeQueue[i])
                {
                    list[index].variables.SectionType = SectionTypeQueue[i];
                    changed.Add(index);
                    didWork = true;
                }
                index++;
            }
            return didWork;
        }

        public void PostSave(string filename)
        {
            //Unused
        }
    }
}
