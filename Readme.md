# What Is FilePhoenix?!

FilePhoenix is a low level file editor designed to make databending complex file formats easier.

# How Does It Work?!

FilePhoenix works by separating the parts of the file that a user would actually want to databend (raw image/audio/video data) from the parts that would break the file if bent (metadata, chunk lengths, checksums, etc.).
This is achieved by assigning every part of a given file its own individual file inside a "working directory", which can then be edited in any fashion the user likes (FilePhoenix does NOT do any editing itself beyond what is necessary to make sure the final edited file is valid, such as writing a new checksum or fixing a chunk length).
After the user has made a change, FilePhoenix is then able to re-compile the contents of the working directory and save them to a new file.

All this means that, despite its original intent, FilePhoenix is not _only_ useful for databending, it could be used as a form of low level file type editor for ANY file type (assuming a module has been written for said file type).

# Where Do I Start?!

Want to get started in a hurry? Check out the [Quick Start Guide](QuickStartGuide.md)!
Descriptions on what each feature does can mostly be found in FilePhoenix, but if you like to read, or just want a more detailed explanation, you could also check out these [tips](Tips.md).
The current list of supported file types can be found [here](Filetypes.md).