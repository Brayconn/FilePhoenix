using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using FileSplitter;
using FilePhoenix.Extensions;

namespace FilePhoenix.Modules
{
    public class Icon : IFileSplitterModule
    {
        [Browsable(false)]
        public string DisplayName => "Microsoft Icon/Cursor Files";

        [Browsable(false)]
        public ReadOnlyCollection<string> SaveFileDialogFilters => new ReadOnlyCollection<string>(new string[]
            {
                "Microsoft Icon File (*.ico)|*.ico",
                "Microsoft Cursor File (*.cur)|*.cur"
            });

        [Browsable(false)]
        public ReadOnlyCollection<string> OpenFileDialogFilters => new ReadOnlyCollection<string>(new string[]
        {
            "Microsoft Icon/Cursor Files (*.cur;*.ico)|*.cur;*.ico"
        }.Concat(SaveFileDialogFilters).ToArray());

        [Browsable(false)]
        public bool UsesVariables => true;

        public bool UpdateVariables(IList<FileFragment> list, ref int index, ref List<int> changed)
        {
            if (index == 2) //Even though setting this is slightly dangerous due to a lack of validation, it's more important this value always exists for everything else to run smoothly
                loadedImageCount = BinaryFileInterpreter.ReadFileAs<ushort>(list[index].Path);
            return false;
        }

        private enum FileTypes
        {
            Unknown,
            Icon,
            Cursor
        }

        //Unused
        public string PreParse(string input)
        {
            return input;
        }

        ushort loadedImageCount;

        public void ParseTo(string filename, ref List<FileFragmentReference> output)
        {
            List<FileFragmentReference> ActualImages = new List<FileFragmentReference>();
            using (BinaryReader br = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read)))
            {
                //Header
                output.Add(new FileFragmentReference(0, 2, new string[] { "Header", "Reserved" }, br.ReadInt16() == 0 ? Validity.Valid : Validity.SoftInvalid));

                ushort typeData = br.ReadUInt16();
                FileTypes type = (Enum.IsDefined(typeof(FileTypes), (int)typeData)) ? (FileTypes)typeData : FileTypes.Unknown;
                output.Add(new FileFragmentReference(2, 2, new string[] { "Header", "Type" }, (type != FileTypes.Unknown) ? Validity.Valid : Validity.HardInvalid, $"File type = {type}"));

                ushort imageCount = loadedImageCount = br.ReadUInt16();
                output.Add(new FileFragmentReference(4, 2, new string[] { "Header", "Image Count" }, Validity.Valid, $"Image count = {imageCount}")); //TODO can't properly validate until way later

                for(int i = 1; i <= imageCount; i++)
                { 
                    output.Add(new FileFragmentReference(br.BaseStream.Position, 1, new string[] { $"Image {i}", "Width" }, Validity.Valid, $"Image {i} Width = {br.ReadByte().ToString().Replace("0","256")}"));
                    output.Add(new FileFragmentReference(br.BaseStream.Position, 1, new string[] { $"Image {i}", "Height" }, Validity.Valid, $"Image {i} Height = {br.ReadByte().ToString().Replace("0", "256")}"));
                    output.Add(new FileFragmentReference(br.BaseStream.Position, 1, new string[] { $"Image {i}", "Color Palette" }, Validity.Valid, $"Image {i} Color Palette = {br.ReadByte().ToString().Replace("0","N/A")}"));

                    output.Add(new FileFragmentReference(br.BaseStream.Position, 1, new string[] { $"Image {i}", "Reserved" }, br.ReadByte() == 0 ? Validity.Valid : Validity.SoftInvalid));

                    ulong offset = (ulong)br.BaseStream.Position;
                    switch (type)
                    {
                        case (FileTypes.Icon):
                            ushort colorPlanes = br.ReadUInt16();
                            output.Add(new FileFragmentReference(offset, 2, new string[] { $"Image {i}", "Color Planes" }, (colorPlanes <= 1) ? Validity.Valid : Validity.HardInvalid, $"Image {i} Color Planes = {colorPlanes}"));

                            offset = (ulong)br.BaseStream.Position;
                            ushort bpp = br.ReadUInt16();
                            output.Add(new FileFragmentReference(offset, 2, new string[] { $"Image {i}", "Bits Per Pixel" }, Validity.Valid, $"Image {i} Bits Per Pixel = {bpp}"));
                            break;
                        case (FileTypes.Cursor):
                            ushort hotspotX = br.ReadUInt16();
                            output.Add(new FileFragmentReference(offset, 2, new string[] { $"Image {i}", "Hotspot X Offset" }, Validity.Valid, $"Image {i} Hotspot X Offset = {hotspotX}"));

                            offset = (ulong)br.BaseStream.Position;
                            ushort hotspotY = br.ReadUInt16();
                            output.Add(new FileFragmentReference(offset, 2, new string[] { $"Image {i}", "Hotspot Y Offset" }, Validity.Valid, $"Image {i} Hotspot Y Offset = {hotspotY}"));
                            break;
                        case (FileTypes.Unknown):
                            output.Add(new FileFragmentReference(offset, 2, new string[] { $"Image {i}", "Variable 1" }, Validity.Unknown, $"Image {i} Variable 1 = {string.Join(", ", br.ReadBytes(2))}"));
                            output.Add(new FileFragmentReference(br.BaseStream.Position, 2, new string[] { $"Image {i}", "Variable 2" }, Validity.Unknown, $"Image {i} Variable 2 = {string.Join(", ", br.ReadBytes(2))}"));
                            break;
                    }

                    offset = (ulong)br.BaseStream.Position;
                    uint imageLength = br.ReadUInt32();
                    output.Add(new FileFragmentReference(offset, 4, new string[] { $"Image {i}", "Image Length" }, Validity.Valid, $"Image {i} Length = {imageLength}"));

                    offset = (ulong)br.BaseStream.Position;
                    uint imageOffset = br.ReadUInt32();
                    output.Add(new FileFragmentReference(offset, 4, new string[] { $"Image {i}", "Image Offset" }, Validity.Valid, $"Image {i} Offset = {imageOffset}"));

                    offset = (ulong)br.BaseStream.Position;
                    br.BaseStream.Position = imageOffset;
                    //The part that's making this giant is just checking to see if the image starts with a png header, if it does use .png, if not, use .raw
                    ActualImages.Add(new FileFragmentReference(imageOffset, imageLength,
                        new string[] { Path.ChangeExtension($"Image {i}", /*This mess gives png files the .png extension, and anything else the .raw extension*/
                        (br.ReadBytes(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) ? "png" : "raw")) }, Validity.OutOfScope));
                    br.BaseStream.Position = (long)offset;
                }
                output.AddRange(ActualImages);
            }
        }

        [Description("Whether or not to automatically fix/overwrite invalid file lengths (only applies when editing the image)"), DefaultValue(true)]
        public bool FixLength { get; set; } = true;

        [Description("Whether or not to automatically fix/overwrite invalid file offsets (only applies when editing the image)"), DefaultValue(true)]
        public bool FixOffset { get; set; } = true;
        
        public void UpdateValidity(IList<FileFragment> list, int index)
        {
            //Error
            if (index < 0)
            {
                return;
            }
            //Header
            else if (index <= 2)
            {
                switch (index)
                {
                    case (0):
                        list[index].Validity = (new FileInfo(list[index].Path).Length == 2 &&
                            BitConverter.ToUInt16(File.ReadAllBytes(list[index].Path), 0) == 0)
                            ? Validity.Valid
                            : Validity.HardInvalid;
                        break;
                    case (1):
                        GetFileType(list);
                        break;
                    case (2):
                        list[index].Validity = (new FileInfo(list[index].Path).Length == 2) ? Validity.Valid : Validity.HardInvalid;
                        break;
                }
            }
            //Image directory entry
            else if (index <= 2 + (loadedImageCount * 8))
            {
                switch (((index - 3) % 8))
                {
                    //Width
                    case (0):
                    //Height
                    case (1):
                    //Color Palette
                    case (2):
                        list[index].Validity = Validity.Valid; //TODO how do you even validate one byte? :-|
                        break;
                    //Reserved
                    case (3):
                        byte[] reservedData = File.ReadAllBytes(list[index].Path);
                        list[index].Validity = (reservedData.Length == 1 && reservedData[0] == 0) ? Validity.Valid : Validity.SoftInvalid;
                        break;
                    //Variable 1
                    case (4):
                        switch (GetFileType(list))
                        {
                            //Color Planes
                            case (FileTypes.Icon):
                                if (new FileInfo(list[index].Path).Length == 2)
                                {
                                    ushort fileTypeValue = BinaryFileInterpreter.ReadFileAs<ushort>(list[index].Path);
                                    if (fileTypeValue == 0 || fileTypeValue == 1)
                                    {
                                        list[index].Validity = Validity.Valid;
                                        break;
                                    }
                                }
                                list[index].Validity = Validity.HardInvalid;
                                break;
                            //Hotspot X Offset
                            case (FileTypes.Cursor):
                                list[index].Validity = Validity.Valid;
                                break;
                            case (FileTypes.Unknown):
                                list[index].Validity = Validity.Unknown;
                                break;
                        }
                        break;
                    //Variable 2
                    case (5):
                        switch (GetFileType(list))
                        {
                            //Bits Per Pixel
                            case (FileTypes.Icon):
                            //Hotspot Y Offset
                            case (FileTypes.Cursor):
                                list[index].Validity = Validity.Valid;
                                break;
                            case (FileTypes.Unknown):
                                list[index].Validity = Validity.Unknown;
                                break;
                        }
                        break;
                    //Image Data Length
                    case (6):
                        if (new FileInfo(list[index].Path).Length == 4)
                        {
                            uint offset = BinaryFileInterpreter.ReadFileAs<uint>(list[index + 1].Path);
                            uint length = BinaryFileInterpreter.ReadFileAs<uint>(list[index].Path);
                            long fileSize = 0;
                            for (int i = 0; i < list.Count; i++)
                                fileSize += new FileInfo(list[i].Path).Length;
                            list[index].Validity = ((offset + length) < fileSize) ? Validity.Valid : Validity.HardInvalid;
                        }
                        else
                            list[index].Validity = Validity.HardInvalid;
                        break;
                    //Image Data Offset
                    case (7):
                        list[index].Validity = (new FileInfo(list[index].Path).Length == 4
                            && BinaryFileInterpreter.ReadFileAs<uint>(list[index].Path) > (3 + (loadedImageCount * 8)))
                            ? Validity.Valid
                            : Validity.HardInvalid;
                        break;
                }
            }
            //Image
            else
            {
                //Iterating over all later images because length/offset changes will ripple
                for (int i = index; i < list.Count; i++)
                    UpdateImage(list, i);
            }
            return;
        }

        private void UpdateImage(IList<FileFragment> list, int index)
        {
            /*
             * This equation is the simplifcation of these two equations:
             * int imageNumber = index - ((loadedImageCount*8) + 3);
             * int imageLengthIndex = 1 + ((1 + imageNumber)*8);
             */
            int imageLengthIndex = (((index - (loadedImageCount * 8)) - 2) * 8) + 1;

            //Starting off out of scope, only to be changed if something goes horribly wrong
            Validity imageValidity = Validity.OutOfScope;

            //Length
            uint currentImageLength = BinaryFileInterpreter.ReadFileAs<uint>(list[imageLengthIndex].Path);
            long actualImageLength = new FileInfo(list[index].Path).Length;
            if (actualImageLength <= uint.MaxValue) //If the length is longer than a uint can hold, the image is invalid
            {
                if (FixLength && currentImageLength != actualImageLength)
                    File.WriteAllBytes(list[imageLengthIndex].Path, BitConverter.GetBytes(currentImageLength = (uint)actualImageLength));
                list[imageLengthIndex].Validity = (currentImageLength == actualImageLength) ? Validity.Valid : Validity.HardInvalid;
            }
            else
                imageValidity = Validity.HardInvalid;

            //Offset
            uint currentImageOffset = BinaryFileInterpreter.ReadFileAs<uint>(list[imageLengthIndex + 1].Path);
            long actualImageOffset = 0;
            for (int i = 0; i < index; i++)
                actualImageOffset += new FileInfo(list[i].Path).Length;
            if (actualImageOffset <= uint.MaxValue) //Same thing with the offset
            {
                if (FixOffset && currentImageOffset != actualImageOffset)
                    File.WriteAllBytes(list[imageLengthIndex + 1].Path, BitConverter.GetBytes(currentImageOffset = (uint)actualImageOffset));
                list[imageLengthIndex + 1].Validity = (currentImageOffset == actualImageOffset) ? Validity.Valid : Validity.HardInvalid;
            }
            else
                imageValidity = Validity.HardInvalid;

            //Actual File
            list[index].Validity = imageValidity; //TODO maybe this can be known...?
        }

        private static FileTypes GetFileType(IList<FileFragment> list)
        {
            if (new FileInfo(list[1].Path).Length == 2)
            {
                ushort fileTypeValue = BinaryFileInterpreter.ReadFileAs<ushort>(list[1].Path);
                if (fileTypeValue == 1 || fileTypeValue == 2)
                {
                    list[1].Validity = Validity.Valid;
                    return (FileTypes)fileTypeValue;
                }
            }
            list[1].Validity = Validity.HardInvalid;
            return FileTypes.Unknown;
        }

        public void PostSave(string filename)
        {
            //Unused
        }
    }
}
