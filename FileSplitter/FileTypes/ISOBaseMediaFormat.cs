using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using FileSplitter;
using FilePhoenixExtensions;

namespace FilePhoenix.FileTypes
{
    class ISOBaseMediaFormat : IFileSplitterModule
    {
        [Browsable(false)]
        public string DisplayName => "ISO Base Media Format/Quicktime";

        [Browsable(false)]
        public ReadOnlyCollection<string> SaveFileDialogFilters => new ReadOnlyCollection<string>(new string[]
        {
            "3rd Generation Partnership Program (*.3g2;*.3gp;*.3gpp)|*.3g2;*.3gp;*.3gpp;",
            "Digital Video Broadcasting (*.dvb)|*.dvb",
            "Adobe Flash Protected Audio/Video (*.f4a;*.f4b;*.f4p;*.f4v)|*.f4a;*.f4b;*.f4p;*.f4v",
            "JPEG 2000 (*.jp2;*.jpm;*.jpx)|*.jp2;*.jpm;*.jpx",
            "MPEG-4 Audio (*.m4a;*.m4b;*.m4p)|*.m4a;*.m4b;*.m4p",
            "Apple DRM Protected Video (*.m4v)|*.m4v",
            "Motion JPEG 2000 (*.mj2;*.mjp2)|*.mj2;*.mjp2",
            "Apple Quicktime (*.mov;*.qt)|*.mov;*.qt",
            "MPEG-4 Video (*.mp4)|*.mp4",
            "Sony Movie Format (*.mqv)|*.mqv"
        });

        [Browsable(false)]
        public ReadOnlyCollection<string> OpenFileDialogFilters => new ReadOnlyCollection<string>(new string[]
        {
            "ISO Base Media File Format Based (*.3gp;*.m4a;*.m4v;*.mov;*.mp4...)|*.3g2;*.3gp;*.3gpp;*.dvb;*.f4a;*.f4b;*.f4p;*.f4v;*.jp2;*.jpm;*.jpx;*.m4a;*.m4b;*.m4p;*.m4v;*.mj2;*.mjp2;*.mov;*.qt;*.mp4;*.mqv;",
        }.Concat(SaveFileDialogFilters).ToArray());

        private enum BoxTypes
        {
            Unknown,
            Length,
            Type,
            ExtendedLength,
            ExtendedType,
            Data
        }
        
        //TODO expand to be closer to NetworkGraphics levels of completeness (dynamic parsing)
        public void ParseTo(string inputFilePath, ref List<FileFragmentReference> output)
        {
            using (BinaryReader br = new BinaryReader(new FileStream(inputFilePath, FileMode.Open, FileAccess.Read)))
            {
                for (int boxNumber = 1; br.BaseStream.Position < br.BaseStream.Length; boxNumber++)
                {
                    //The actual length of the data section of this box (again, to allow for extended type/length)
                    ulong actualDataLength;

                    long lengthOffset = br.BaseStream.Position;
                    uint lengthData = br.ReadUInt32M();
                    output.Add(new FileFragmentReference(lengthOffset, 4,
                        new string[] { $"Box {boxNumber}", "Length" }, Validity.Valid,
                        $"Box {boxNumber} Length = {lengthData}"){ { "BoxNumber", boxNumber }, { "BoxType", BoxTypes.Length } });

                    long typeOffset = br.BaseStream.Position;
                    string typeData = br.ReadString(4);
                    output.Add(new FileFragmentReference(typeOffset, 4,
                        new string[] { $"Box {boxNumber}", "Type" },
                        (IsValidBoxType(typeData) ? Validity.Valid : Validity.HardInvalid),
                        $"Box {boxNumber} Type = {typeData}") { { "BoxNumber", boxNumber }, { "BoxType", BoxTypes.Type } });

                    actualDataLength = lengthData - 8; //(Length of data) - (length of "Length" and "Type")

                    if (lengthData == 1)
                    {
                        long extLengthOffset = br.BaseStream.Position;
                        ulong extLengthData = br.ReadUInt64M();
                        output.Add(new FileFragmentReference(extLengthOffset, 4,
                            new string[] { $"Box {boxNumber}", $"Extended Length" }, Validity.Valid,
                            $"Box {boxNumber} Extended Length = {extLengthData}") { { "BoxNumber", boxNumber }, { "BoxType", BoxTypes.ExtendedLength } });

                        actualDataLength = extLengthData - 16; //Now we subtract extLength as well as Length and Type
                    }
                    else if (lengthData == 0)
                    {
                        //TODO Pretty sure this work, but I don't have any files to confirm...
                        actualDataLength = (ulong)(br.BaseStream.Length - br.BaseStream.Position);
                    }

                    if (typeData == "uuid")
                    {
                        long extTypeOffset = br.BaseStream.Position;
                        Guid extTypeData = new Guid(br.ReadBytes(16));
                        output.Add(new FileFragmentReference(extTypeOffset, 16,
                            new string[] { $"Box {boxNumber}", "Extended Type" },
                            (IsValidExtendedBoxType(extTypeData) ? Validity.Valid : Validity.HardInvalid),
                            $"Box {boxNumber} GUID = {extTypeData.ToString()}") { { "BoxNumber", boxNumber }, { "BoxType", BoxTypes.ExtendedType} });
                        actualDataLength -= 16;
                    }

                    long dataOffset = br.BaseStream.Position;
                    output.Add(new FileFragmentReference((ulong)dataOffset, actualDataLength,
                        new string[] { $"Box {boxNumber}", "Data" }, Validity.Valid,
                        $"Box {boxNumber} Data length = {actualDataLength}") { { "BoxNumber", boxNumber }, { "BoxType", BoxTypes.Data } });
                    br.BaseStream.Position += (long)actualDataLength;
                }
            }
        }

        [Description("Whether or not to automatically fix/overwrite invalid chunk Lengths (only applies when editing the chunk's data)"), DefaultValue(true)]
        public bool FixLength { get; set; } = true;

        [Browsable(false)]
        public bool UsesVariables => true;

        public bool UpdateVariables(IList<FileFragment> list, ref int index, ref List<int> changed)
        {
            //Default start
            if (index == 0)
            {
                list[index].variables.BoxType = BoxTypes.Length;
                list[index].variables.BoxNumber = 1;
                return false;
            }
            else
            {
                //Shared between both cases
                BoxTypes current = BoxTypes.Unknown;
                BoxTypes prev = list[index - 1].variables.BoxType;
                int boxNumber = list[index-1].variables.BoxNumber;

                //HACK abandon all (some) hope ye who enter here
                //HACK it might be faster to just not have this first case, since it's only used once (if a file is created while enabled)
                //If in the middle of list
                if (index + 1 < list.Count)
                {
                    BoxTypes next = list[index + 1].variables.BoxType;
                    //TODO maybe move to C# 7 and use "when"?
                    switch(prev)
                    {
                        case (BoxTypes.Length):
                            current = (next == BoxTypes.Type || next == BoxTypes.Length)
                                //If you're trying to insert a box in between a Length and a Type, I don't know what to tell ya...
                                //And if you managed to get two lengths right next to eachother...
                                ? BoxTypes.Unknown
                                : BoxTypes.Type;
                            break;
                        case (BoxTypes.Type):
                            switch(next)
                            {
                                case (BoxTypes.ExtendedType):
                                    current = BoxTypes.ExtendedLength;
                                    break;
                                case (BoxTypes.Data):
                                    switch (new FileInfo(list[index].Path).Length)
                                    {
                                        case (8):
                                            current = BoxTypes.ExtendedLength;
                                            break;
                                        case (16):
                                            current = BoxTypes.ExtendedType;
                                            break;
                                        default:
                                            current = BoxTypes.Unknown;
                                            break;
                                    }
                                    break;
                                //Only way length is gonna show up is if this box is missing data
                                case (BoxTypes.Length):
                                    current = BoxTypes.Data;
                                    break;
                                default:
                                    current = BoxTypes.Unknown;
                                    break;
                            }
                            break;
                        case (BoxTypes.ExtendedLength):
                            switch(next)
                            {
                                case (BoxTypes.Data):
                                    current = BoxTypes.ExtendedType;
                                    break;
                                case (BoxTypes.Length):
                                    current = BoxTypes.Data;
                                    break;
                                default:
                                    current = BoxTypes.Unknown;
                                    break;
                            }
                            break;
                        case (BoxTypes.ExtendedType):
                            current = (next == BoxTypes.Length)
                                ? BoxTypes.Data
                                : BoxTypes.Unknown;
                            break;
                        case (BoxTypes.Data):
                            current = (next == BoxTypes.Type)
                                ? BoxTypes.Length
                                : BoxTypes.Unknown;
                            boxNumber++; //TODO might cause issues if the BoxType was found to be unknown?
                            break;
                    }
                }
                //If at end of list
                else
                {
                    switch (prev)
                    {
                        case (BoxTypes.Length):
                            current = BoxTypes.Type;
                            break;
                        case (BoxTypes.Type):
                            string typeData = File.ReadAllText(list[index - 1].Path);
                            switch(new FileInfo(list[index].Path).Length)
                            {
                                case (8):
                                    //HACK just look at this. It basically has to validate the entire box before checking aaaaaaaaaaaaa
                                    if (index >= 2 && list[index - 2].variables.BoxType == BoxTypes.Length //TODO isn't this (vvv) big endian???????
                                        && new FileInfo(list[index - 2].Path).Length == 4 && BinaryFileInterpreter.ReadFileAs<uint>(list[index - 2].Path) == 0)
                                        current = BoxTypes.ExtendedLength;
                                    else
                                        goto default;
                                    break;
                                case (16):
                                    if (typeData != "uuid") //HACK this might not be good enough?
                                        goto default;
                                    current = BoxTypes.ExtendedType;
                                    break;
                                default:
                                    current = BoxTypes.Data;
                                    break;
                            }
                            break;
                        case (BoxTypes.ExtendedLength):
                            current = (new FileInfo(list[index].Path).Length == 16)
                                ? BoxTypes.ExtendedType
                                : BoxTypes.Data;
                            break;
                        case (BoxTypes.ExtendedType):
                            current = BoxTypes.Data;
                            break;
                        case (BoxTypes.Data):
                            current = BoxTypes.Length;
                            boxNumber++;
                            break;
                    }
                }

                list[index].variables.BoxType = current;
                list[index].variables.BoxNumber = boxNumber;
                return false;
            }
        }

        public void UpdateValidity(IList<FileFragment> list, int index)
        {
            //Switch based on box type
            switch (list[index].variables.BoxType)
            {
                //TODO length is super complicated with these, so I'll have to spend a good chunk of time to fix them
                case (BoxTypes.Length): //length (uint)
                    string lengthPath = list[index].Path;
                    long lengthLength = new FileInfo(lengthPath).Length;
                    if (lengthLength != 4)
                    {
                        list[index].Validity = (lengthLength < 4) ? Validity.HardInvalid : Validity.Unknown;
                    }
                    else
                    {
                        //TODO why did I have to rename these?!?!?!?!?!?!?!?!?!?!?!
                        uint lengthData1 = BinaryFileInterpreter.ReadFileAs<uint>(lengthPath, true);
                        
                        if (list[index + 2].variables.BoxType == BoxTypes.ExtendedLength)
                        {
                            list[index].Validity = (lengthData1 == 1) ? Validity.Valid : Validity.HardInvalid;
                            list[index].Description = $"Box {list[index].variables.BoxNumber} Length = <unknown>";
                        }
                        else
                        {
                            ulong correctLength1 = 4; //Starts at the length of "Length"
                            int i1 = 0;
                            FileFragment ff1;
                            do
                            {
                                i1++;
                                ff1 = list[index + i1];
                                correctLength1 += (ulong)new FileInfo(ff1.Path).Length;
                            } while (ff1.variables.BoxType != BoxTypes.Data);

                            //Assuming it did work, we can go as usual
                            list[index].Validity = (lengthData1 == correctLength1)
                                ? Validity.Valid : Validity.HardInvalid;
                        }
                        list[index].Description = $"Box {list[index].variables.BoxNumber} Length = {lengthData1}";
                    }
                    break;
                case (BoxTypes.Type): //type
                    string typeData = File.ReadAllText(list[index].Path);
                    list[index].Validity =
                        IsValidBoxType(typeData)
                        ? Validity.Valid
                        : Validity.HardInvalid;
                    list[index].Description = $"Box {list[index].variables.BoxNumber} Type = {typeData}";
                    break;
                case (BoxTypes.ExtendedLength): //Extended Length (ulong)
                    //TODO implement extended length validation
                    break;
                case (BoxTypes.ExtendedType): //Extended Type (GUID)
                    Guid extTypeData = new Guid(File.ReadAllBytes(list[index].Path));
                    list[index].Validity =
                        IsValidExtendedBoxType(extTypeData)
                        ? Validity.Valid
                        : Validity.HardInvalid;
                    list[index].Description = $"Box {list[index].variables.BoxNumber} GUID = {extTypeData.ToString()}";
                    break;
                case (BoxTypes.Data): //data
                    //HACK this entire code is probably messy but hopefully actually works
                    string dataPath = list[index].Path;
                    ulong correctLength, dataLength;
                    correctLength = dataLength = (ulong)new FileInfo(dataPath).Length;

                    int i = 0;
                    FileFragment ff, lengthToEdit = null;
                    do
                    {
                        i++;
                        ff = list[index - i];
                        correctLength += (ulong)new FileInfo(ff.Path).Length;

                        //Save the first length or extended length box we find
                        if (lengthToEdit == null && (ff.variables.BoxType == BoxTypes.Length || ff.variables.BoxType == BoxTypes.ExtendedLength))
                            lengthToEdit = ff;
                    } while (ff.variables.BoxType != BoxTypes.Length);

                    bool isExtended = (lengthToEdit.variables.BoxType == BoxTypes.ExtendedLength); //Storing this since it's used twice

                    /*
                    ulong lengthData = isExtended
                        ? BitConverter.ToUInt64(File.ReadAllBytes(lengthToEdit.Path).Reverse().ToArray(), 0)
                        : BitConverter.ToUInt32(File.ReadAllBytes(lengthToEdit.Path).Reverse().ToArray(), 0);
                    */
                    ulong lengthData = BinaryFileInterpreter.ReadFileAs(lengthToEdit.Path, isExtended ? typeof(ulong) : typeof(uint), true);

                    if (FixLength && lengthData != correctLength)
                    {
                        //TODO this is so dumb that this conditional operator doesn't work :angery:
                        //BitConverter.GetBytes(isExtended ? correctLength : (uint)correctLength).Reverse().ToArray();
                        byte[] bytesToWrite = isExtended ? BitConverter.GetBytes(correctLength) : BitConverter.GetBytes((uint)correctLength);
                        File.WriteAllBytes(lengthToEdit.Path, bytesToWrite.Reverse().ToArray());
                        lengthData = correctLength;
                        lengthToEdit.Validity = Validity.Valid;
                        lengthToEdit.Description = $"Box {lengthToEdit.variables.BoxNumber} Length = {correctLength}";
                    }
                    list[index].Validity = (lengthData == correctLength)
                        ? Validity.Valid
                        : Validity.HardInvalid;
                    list[index].Description = $"Box {list[index].variables.BoxNumber} Data length = {dataLength}";

                    break;
            }
            return;
        }

        //TODO might be unnessecary now...?
        /// <summary>
        /// Fixes the box at index's variables
        /// </summary>
        /// <param name="list"></param>
        /// <param name="index"></param>
        private void FixBoxAt(IList<FileFragment> list, int index)
        {
            //TODO rework to be actually be accurate, probably by searching backwards until normality
            BoxTypes box = BoxTypes.Unknown;
            switch ((index % 3) + 1) //This assumes this is a file that has no extended lengths n stuff.
            {
                case (1):
                    box = BoxTypes.Length;
                    break;
                case (2):
                    box = BoxTypes.Type;
                    break;
                case (3):
                    box = BoxTypes.Data;
                    break;
            }
            list[index].variables.BoxType = box;
            list[index].variables.BoxNumber = (index / 3) + 1;
        }

        private static bool IsValidExtendedBoxType(Guid extendedBoxType)
        {
            return true; //TODO implement extended box type validation
        }

        private static bool IsValidBoxType(string boxType)
        {
            return true; //TODO implement box type validation
        }

        public void PostSave(string filename)
        {
            //Unused
        }
    }
}
