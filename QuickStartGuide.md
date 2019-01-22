# FilePhoenix Quick Start Guide

There are a few different way to get started with FilePhoenix, but this guide will just focus on the easiest one: Quick Start.

1. Open FilePhoenix
2. File->Quick Start
3. Select the file you'd like to open
	- You should now see a folder called "\<filename> Ashes", this contains the contents of the file you opened, but split up into many .raw files.
4. Select the files you want to edit
	- Depending on the file type you chose you'll want to edit different .raw files, and now is your chance to do that!
	- While you can do this by opening each .raw file to view its contents, it's usually easier to use the list in FilePhoenix, or just look at the titles of the files.
	- See the list in [Filetypes.md](Filetypes.md) for more information.
5. Make some changes!
	- Now that you've selected what you want to edit, open them up using your favorite databending program (Notepad++, Audacity, some random python script you found on the internet, etc.) and go wild!
	- Some files can be more finicky than others however, pngs for example are noted for being very sensitive to changes, so starting out small is generally a good rule of thumb.
	- After you've made a change, the contents of the folder will automatically be reassembled into "\<filename> Reborn.\<file extension>", in the same directory as the original file you opened.
6. Open the file!
	- If everything has gone correctly, you should now have a brand new databent file!
	- Results will still vary depending on the file type and editing method, but the file should always be "valid".

## A special case: GIF

Gifs present a unique problem with FilePhoenix.
Due to their design, GIFs result in a massive amount of seperate files for the image data.
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