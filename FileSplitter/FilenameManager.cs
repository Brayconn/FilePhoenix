//#define verify
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static FileSplitter.FileSplitter;

namespace FileSplitter
{
    /* Maybe useful, but has been obseleted anyways
        /// <summary>
        /// Returns the top most file or directory of a path
        /// </summary>
        /// <param name="filename">The path to parse</param>
        /// <returns>The top most file or folder</returns>
        static string GetTopDirectory(string filename)
        {
            while (!string.IsNullOrWhiteSpace(Path.GetDirectoryName(filename)))
                filename = Path.GetDirectoryName(filename);
            return filename;
        }
        */

    /// <summary>
    /// Represents a file or folder in the parent VirtualDirectory
    /// </summary>
    class VirtualFileSystemEntry
    {
        public override int GetHashCode() => filename.GetHashCode();
        public override bool Equals(object obj) => (obj is VirtualFileSystemEntry) ? filename == ((VirtualFileSystemEntry)obj).filename : base.Equals(obj);
        public override string ToString() => filename;

        public string filename;
        public List<VirtualFileSystemEntry> children = new List<VirtualFileSystemEntry>();

        public VirtualFileSystemEntry(IEnumerable<string> files) : this(files.First()) => Add(files.Skip(1));
        public VirtualFileSystemEntry(string filename) => this.filename = filename;

        public static explicit operator VirtualFileSystemEntry(string v) => new VirtualFileSystemEntry(v);

        public void Add(IEnumerable<string> filepath)
        {
            if (filepath.Count() > 0)
            {
                if (children.Contains((VirtualFileSystemEntry)filepath.First()))
                    children[children.IndexOf((VirtualFileSystemEntry)filepath.First())].Add(filepath.Skip(1));
                else
                    children.Add(new VirtualFileSystemEntry(filepath));
            }
        }

        public void NumberChildren()
        {
            //Actually equal to the digits in children.Count, not just the zeros
            int zeroCount = children.Count.ToString().Length;
            for (int i = 0; i < children.Count; i++)
            {
                children[i].filename = $"{i.ToString("D" + zeroCount)} - {children[i].filename}";
                children[i].NumberChildren();
            }
        }

#if verify
        public bool Exists(IEnumerable<string> filepath)
        {
            //If we need to continue, and we can continue down
            if (filepath.Count() > 0 && children.Count > 0)
            {
                return (children.Contains((VirtualFileSystemEntry)filepath.First()))
                    ? children[children.IndexOf((VirtualFileSystemEntry)filepath.First())].Exists(filepath.Skip(1))
                    : false;
            }
            else
                return true;
        }
#endif

        public List<string> Flatten(string appendTo = "")
        {
            if (children.Count > 0)
            {
                List<string> output = new List<string>();
                foreach (var file in children)
                    output.AddRange(file.Flatten(Path.Combine(appendTo,filename)));
                return output;
            }
            else
                return new List<string>() { Path.Combine(appendTo, filename) };
        }        
    }
    
    static class FilenameManager
    {
        public static List<string> Number(string directory, List<FileFragmentReference> list, string defaultExtension, bool forceExtension = false)
        {
            VirtualFileSystemEntry virtualDirectory = new VirtualFileSystemEntry(directory);
            //Creates a virtual directory that can then be numbered correctly
            foreach (var item in list)
            {
                int lastIndex = item.filename.Length - 1;
                if (forceExtension || string.IsNullOrWhiteSpace(Path.GetExtension(item.filename[lastIndex])))
                    item.filename[lastIndex] = Path.ChangeExtension(item.filename[lastIndex], defaultExtension);
                virtualDirectory.Add(item.filename);
            }

#if (verify)
            //Verifys that all files still exist
            //Probably only catches overlapping entries
            foreach (var item in list)
            {
                if (!virdir.Exists(item.filename))
                    throw new Exception(Path.Combine(item.filename) + " was not added to the virtual directory");
            }
#endif
            virtualDirectory.NumberChildren();
            return virtualDirectory.Flatten();
        }
    }
}
