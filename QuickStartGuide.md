# FilePhoenix Quick Start Guide
Before starting with FilePhoenix, you should already have a good understanding of how "normal" databending works (using Notepad++, Audacity, a hex editor, etc.).

If you don't, you probably want to start off with guides like these first:
- [Databending Using Audacity](https://www.hellocatfood.com/databending-using-audacity/) 
- [The Wordpad Effect](http://blog.animalswithinanimals.com/2008/08/databending-and-glitch-art-primer-part.html)
- [Glitchet Resources](http://www.glitchet.com/resources)
- [Anything else in the sidebar of /r/glitch_art](https://www.reddit.com/r/glitch_art/)

It is also recommended that you know a bit about the file format you plan on databending.

I've had the best luck finding documentation using the following sites:
- [Wikipedia](https://en.wikipedia.org/wiki/File_format)
- [Active @ File Recovery Signatures Page](http://www.file-recovery.com/signatures.htm)
- [Archiveteam Fileformat Wiki](http://fileformats.archiveteam.org/wiki/Electronic_File_Formats)

## Using FileSplitter (with the "Quick Start" button)
There are a few different way to get started with FilePhoenix, but the easiest/most convient way is by using Quick Start.

1. Open FilePhoenix
2. File->Quick Start
3. Select the file you'd like to open
	- You should now see a folder called "\<filename> Ashes", this contains the contents of the file you opened, but split up into many .raw files.
4. Find the files you want to edit
	- Depending on the file type you chose you'll want to edit different .raw files, and now is your chance to choose them!
	- While you can do this by opening each .raw file to view its contents, it's usually easier to use the list in FilePhoenix, or just look at the filenames.
	- See the list in [Filetypes.md](Filetypes.md) for more information.
5. Make some changes!
	- Now that you've found what you want to edit, open them up using your favorite databending program (Notepad++, Audacity, some random python script you found on the internet, etc.) and go wild!
	- Some files can be more finicky than others however, pngs for example are noted for being very sensitive to changes, so starting out small is generally a good rule of thumb.
	- After you've made a change, the contents of the folder will automatically be reassembled into "\<filename> Reborn.\<file extension>", in the same directory as the original file you opened.
6. Open the file!
	- If everything has gone correctly, you should now have a brand new databent file!
	- Results will still vary depending on the file type and editing method, but the file should always be "valid".

## Using FileMerger (with GIF)
Gifs present a unique problem with FileSplitter; due to their design, GIFs result in a massive amount of seperate files for the image data.
Thankfully, there is a way to get around this: FileMerger.

1. Follow the above guide until step 4.
2. Select the "FileMerger" tab, then select "Add".
   - Now you can select all the files you want to databend.
   - In the case of GIF, you'll probably want to select anything that says "Image Data"
3. Choose an output file, and click "Save"
   - This will be the file you want to open with your databending program of choice
4. Return to the above guide at step 5.
   - There is a lot more customization that can be done with FileMerger, but in the case of GIF, it should all be done automatically.
   - For more detailed information see [Tips.md](Tips.md)