# FileSplitter
FileSplitter is the main component of FilePhoenix, it's the part that does the actual splitting of your file.
If you're looking at the Main or Options tab, you're dealing with FileSplitter.

## Delay Time
The update delay determines the time interval between when FileSplitter detects that a file has been changed, and when it will take action on that change.
The default time is 1 second (1000 milliseconds), but if you're editing a large file that takes a long time to save, you will need to change this to a higher value to keep things running correctly.
If saving the file takes less time than you anticipated, don't worry, just change it back to a lower value, as this will reset the current timer to that lower value.

## Opening a Folder Instead of a File
Have some work from a previously opened file you want to resume?
Want to see what your entire pictures folder looks like as a gif?
Try hitting enable with only a working directory selected!
This will load the entire contents of the selected folder into FileSplitter using the selected module for validation.

## Module Specific Options
Some file type modules may have their own options to customize, including ones that affect how files are opened.
To edit these, simply select a module in the Options tab, then click the dropdown to view that module's options (assuming it has any).
From there you can continue setting everything up manually, or use quick start.

## Flattening and Reloading
The "Flatten and Reload" button will save your file to a temporary location and parse the resulting file anew.
This can be useful in cases where FilePhoenix loses track of how a file would really be parsed when whole.
However, it is important that you set any File Type Module options that concern opening files to some version of "dynamic".
Failing to do this could, at best, make no difference at all, or, at worst, could result in the file being read incorrectly, thus leaving you with the same (or a bigger!) mess than you started with.

## Chaining Files
Fun fact: you can chain multiple instances of FilePhoenix together.
1. Open an instance of FilePhoenix (this will be referred to as the parent instance) and open the first file (the one that contains the second file you want to open).
	- The settings here don't matter much, so feel free to open how you please.
2. Open a second (child) instance of FilePhoenix with the following settings:
	- OpenedFile: The file within the parent's working directory you want to edit.
	- AutoSavePath: Same as OpenedFile.
	- FileNamingMode: Overwrite
	- WorkingDirectory: Any directory NOT in the parent's WorkingDirectory.
3. Navigate to the child's WorkingDirectory and make some edits!
	- This part's exactly the same as if you just had one instance of FilePhoenix open, so feel free to go crazy!

If you really wanted, you could even repeat step 2 multiple times to go even more layers in to a file, but I'm not sure how many file types would support that...

# FileMerger
FileMerger is the opposite, younger brother of FileSplitter.
Instead of splitting your file into many parts, it merges many files back into one.
An instance of FileMerger consists of a main file (The "MasterFile"; The one you want to edit) and many sub files (the ones that will be exported to).

## Delay Time
Each FileMerger instance has a delay time, just like FileSplitter!
Scroll up to the Delay Time section of FileSplitter for the details.

## SubFiles
The most important part of a FileMerger instance is its SubFiles.
These represent what parts of the master file will be exported where.
For the rest of this subsection, lets assume that we have two files we want to merge, each 4 bytes long, "File A" and "File B".
Both File A and File B will be merged into "File C"

## Percents vs Absolute
When choosing where in the Master File your SubFile should appear at, you have a few options.
One way is to explicitly state the offset or size you want.
This is fine if you don't plan on changing the length of your file, but what if you do?
Well, you can use percents (by swapping over SizeSelection or OffsetSelection to "Percent")!
This will calculate a new absolute value based off of the percentage listed.
The actual calculation is as follows (for each SubFile):
`Absolute value = Percent * File C Length`

## Size Limit
The SizeLimit lets you... well, limit the size of a SubFile.
This doesn't do much when using Absolute sizes, but can come in handy when using percents/messing around with the length of the Master File.
This can be set to any number you want, or "None".
You can also press the dropdown to see the list of default values- the max values for various sizes of integers.

## Size Error Mode
The Size Error Mode determines what FileMerger should do in the event that it's unable to export enough data to a SubFile.
If set to "Truncate", FileMerger will export as much as it can, then stop.
If set to "FillWithValue", it will fill any remaining space with the pattern specified in "FillValue".
Any value in double quotes ("") will be treated as a string, and any value starting with "0x" (or ending with "h") will be treated as raw bytes (the actual value must also be in hexadecimal, of course).