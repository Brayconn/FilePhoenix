# What Is FilePhoenix?!

FilePhoenix is a low level file editor designed to make databending complex file formats easier.

# How Does It Work?!

FilePhoenix is made up of two parts: FileSplitter and FileMerger.

**FileSplitter** separates the parts of the file that a user would actually want to databend (raw image/audio/video data) from the parts that would break the file if bent (metadata, chunk lengths, checksums, etc.).
This is achieved by assigning every part of a given file its own individual file inside a "working directory", which can then be edited in any fashion the user likes.
After the user has made a change, FileSplitter is then able to re-compile the contents of the working directory and save them to a new file.
(FileSplitter does not do any editing beyond what is necessary to make sure the final edited file is valid, such as writing a new checksum or fixing a chunk length.)

**FileMerger** does the opposite, taking in many files and merging them back into one.
When the user edits the resulting "master file", the contents are divied up between the original input files.
This is useful for formats such as gif, that split the image data into hundreds(!) of byte sized chunks.

# Where Do I Start?!

Want to get started in a hurry? Check out the [Quick Start Guide](QuickStartGuide.md)!

Descriptions on what each feature does can mostly be found in FilePhoenix, but if you like to read, or just want a more detailed explanation, you could also check out these [tips](Tips.md).

The current list of supported file types can be found [here](Filetypes.md).

Got questions? Join the [Discord](https://discord.gg/TGgMFdB)!