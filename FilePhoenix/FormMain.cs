using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FileSplitter;
using FileMerger;
using FilePhoenix.Modules;
using FilePhoenix.Extensions;

namespace FilePhoenix
{
    public partial class FormMain : Form
    {
        //TODO maybe move to a switch statement function? Don't know if the more messy code is worth the peformance increase though...
        private readonly static ReadOnlyDictionary<Validity, Color> displayColors = new ReadOnlyDictionary<Validity, Color>(new Dictionary<Validity, Color>()
        {
            { Validity.Unchecked, Color.DarkGray },
            { Validity.Valid, Color.White },
            { Validity.HardInvalid, Color.Red },
            { Validity.SoftInvalid, Color.IndianRed },
            { Validity.Unknown, Color.Tan },
            { Validity.OutOfScope, Color.LightGray },
            { Validity.Irrelevant, Color.Orange },
            { Validity.Error, Color.DarkRed }
        });

        private readonly FileSplitter.FileSplitter fs;
        private readonly BindingList<FileMerger.FileMerger> fms;

        public FormMain()
        {
            InitializeComponent();

            //Progress bar
            Progress<FileSplitterProgressInfo> progress = new Progress<FileSplitterProgressInfo>();
            progress.ProgressChanged += UpdateProgress;

            //FileSplitter setup
            fs = new FileSplitter.FileSplitter(
                //Network graphics chosen arbitrarily
                typeof(NetworkGraphics).Assembly.GetExportedTypes(),
                new[] { AppDomain.CurrentDomain.BaseDirectory },
                progress);
            fileSplitterPropertyGrid.SelectedObject = fs;
            fs.VirtualFileUpdated += UpdateList;
            /*RIP Databinding
            workingDirectoryListView.BindingContext[fs,"VirtualFile.Keys"].
            workingDirectoryListView.DataBindings.Add(new Binding("Items", fs, "VirtualFile.Keys"));
            */

            //FileMerger setup
            fms = new BindingList<FileMerger.FileMerger>();
            fileMergerListBox.DataSource = fms;
        }

        /// <summary>
        /// Updates the progress bar, and window title text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateProgress(object sender, FileSplitterProgressInfo e)
        {
            this.Text = e.ProgressPercentage < 100 ? $"{e.MethodDescription} | {e.ProgressDescription} | {e.ProgressPercentage}%" : "FilePhoenix";
            progressBar1.Value = e.ProgressPercentage < 100 ? e.ProgressPercentage : 0;
        }

        #region ToolStrip Buttons

        #region File

        /// <summary>
        /// Returns the IFileSplitterModule.DisplayName of the module at the given index
        /// </summary>
        /// <param name="index">The selected index in an</param>
        /// <returns></returns>
        private string GetModuleFromIndex(int index)
        {
            //TODO review the -1 in case module selection ever goes wrong
            if (index < (fs.FileTypeModule?.OpenFileDialogFilters.Count ?? fs.AllOpenFileDialogFilters.Count))
                return fs.FileTypeModule?.DisplayName ?? fs.IndexToModule[index - 1];
            else
                return null;
        }

        private static Regex iterationRegex = new Regex(@"\((\d+)\)$");
        private void quickStartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            #region Opened File prep
            string fileToOpen;
            string moduleToUse = null;
            using (OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = string.Join("|",
                         fs.FileTypeModule?.OpenFileDialogFilters.ToArray()
                         ?? fs.AllOpenFileDialogFilters.ToArray()
                         ) + "|All Files (*.*)|*.*"
            })
            {
                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                fileToOpen = ofd.FileName;
                moduleToUse = GetModuleFromIndex(ofd.FilterIndex);
            }
            #endregion
            #region Index/Module prep
            if (moduleToUse == null)
            {
                MessageBox.Show("Please select a module to to continue.", "Info");
                using (FormModuleSelector ms = new FormModuleSelector(fs.LoadedModules.Keys.ToArray()))
                {
                    if (ms.ShowDialog() == DialogResult.OK)
                        moduleToUse = fs.LoadedModules.ElementAt(ms.SelectedIndex).Key;
                    else
                        return;
                }
            }
            #endregion
            #region Auto-save prep
            string fileToAutoSaveTo = Path.ChangeExtension(fileToOpen, null) + " Reborn" + Path.GetExtension(fileToOpen);
            #endregion
            #region Working Directory prep
            string directoryToUse = Path.ChangeExtension(fileToOpen, null) + " Ashes";
            while (Directory.Exists(directoryToUse))
            {
                Match result = iterationRegex.Match(directoryToUse);
                //Replace the existing number
                if (result.Success)
                    directoryToUse = iterationRegex.Replace(directoryToUse, $"({int.Parse(result.Groups[1].Value) + 1})");
                //Add a new number
                else
                    directoryToUse += $"(1)";
            }
            #endregion

            #region Setting everything
            fs.Enabled = false;

            fs.OpenedFile = fileToOpen;

            fs.FileFragmentExtension = "raw";

            fs.AutoSavePath = fileToAutoSaveTo;
            fs.AutoSave = AutoSaveModes.On;
            fs.FileNamingMode = FileNameModes.Iterate;

            fs.WorkingDirectory = directoryToUse;
            fs.FileTypeModule = (IFileSplitterModule)Activator.CreateInstance(fs.LoadedModules[moduleToUse]);

            fs.Enabled = true;
            #endregion
        }

        //---

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = string.Join("|",
                         fs.FileTypeModule?.OpenFileDialogFilters.ToArray()
                         ?? fs.AllOpenFileDialogFilters.ToArray()
                         ) + "|All Files (*.*)|*.*"
            })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    fs.Enabled = false;
                    fs.OpenedFile = ofd.FileName;
                    string module = GetModuleFromIndex(ofd.FilterIndex);
                    if (module != null)
                        fs.FileTypeModule = (IFileSplitterModule)Activator.CreateInstance(fs.LoadedModules[module]);
                    tabControl.SelectedIndex = 1;
                }
            }
        }

        private void directoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string selectedFolder = null;
            if (HelperMethods.CanUseWinAPICodePackFolderBrowser)
            {
                using (CommonOpenFileDialog folderPicker = new CommonOpenFileDialog()
                {
                    IsFolderPicker = true
                })
                {
                    if (folderPicker.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        selectedFolder = folderPicker.FileName;
                    }
                }
            }
            else
            {
                using (FolderBrowserDialog fd = new FolderBrowserDialog())
                {
                    if (fd.ShowDialog() == DialogResult.OK)
                    {
                        selectedFolder = fd.SelectedPath;
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(selectedFolder))
            {
                fs.Enabled = false;
                fs.OpenedFile = null;
                fs.WorkingDirectory = selectedFolder;
                fs.Enabled = true;
                tabControl.SelectedIndex = 1;
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = ((fs.FileTypeModule != null)
                    ? string.Join("|", fs.FileTypeModule.SaveFileDialogFilters) + "|"
                    : "") + "All Files (*.*)|*.*";

                if (sfd.ShowDialog() == DialogResult.OK)
                    fs.Save(sfd.FileName);
            }
        }

        #endregion

        #region Edit

        private void flattenAndReloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to flatten and reload?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                fs.FlattenAndReload();
        }

        #endregion

        #endregion
        
        private delegate void UpdateListDelegate();
        private void UpdateList()
        {
            if (workingDirectoryListView.InvokeRequired)
                workingDirectoryListView.Invoke(new UpdateListDelegate(UpdateList));
            else
            {
                //FileSplitter butons
                flattenAndReloadToolStripMenuItem.Enabled = saveAsToolStripMenuItem.Enabled = fs.FileLoaded;
                //These don't actually need to be enabled/disabled acording to the FileSplitter, since FileMerger is a seperate entity
                //FileMerger buttons
                //addToolStripMenuItem.Enabled = removeToolStripMenuItem.Enabled = fs.Enabled;
                //TODO be more dynamic
                workingDirectoryListView.Items.Clear();
                for (int i = 0; i < fs.VirtualFile.Count; i++)
                {
                    UpdateProgress(this, new FileSplitterProgressInfo("Updating list...", $"Adding {fs.VirtualFile.Values[i].Path}", ((i + 1) * 100) / fs.VirtualFile.Count));
                    workingDirectoryListView.Items.Add(fs.VirtualFile.Values[i].Path).BackColor = displayColors[fs.VirtualFile.Values[i].Validity];
                }
            }
        }

        private void workingDirectoryListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (workingDirectoryListView.SelectedIndices.Count > 0)
            {
                var ff = fs.VirtualFile.ElementAt(workingDirectoryListView.SelectedIndices[0]).Value;
                fileFragmentInfoBox.Text =
                    $"Path: {ff.Path}\n" +
                    (!string.IsNullOrWhiteSpace(ff.Description) ? $"Description: {ff.Description}\n" : "") +
                    $"Validity: {ff.Validity}";
            }
            else
                fileFragmentInfoBox.Clear();
        }

        //Keeping things looking pretty
        private void workingDirectoryListView_Resize(object sender, EventArgs e)
        {
            workingDirectoryListView.Columns[0].Width = workingDirectoryListView.Width;
        }

        #region FileMerger

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string[] subFiles;
            using (OpenFileDialog ofd = new OpenFileDialog()
            {
                //This edit and the one...         ...are to make sure that the one person who wants to use FileMerger on its own doesn't get mad
                InitialDirectory = fs?.WorkingDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Multiselect = true,
                Filter = "Any Files (*.*)|*.*"
            })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                    subFiles = ofd.FileNames;
                else
                    return;
            }

            string masterFile;
            using (SaveFileDialog sfd = new SaveFileDialog()
            {
                //                      ...down here...
                InitialDirectory = fs != null
                                   ? Path.GetDirectoryName(fs.WorkingDirectory)
                                   : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Filter = "Any Files (*.*)|*.*"
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                    masterFile = sfd.FileName;
                else
                    return;
            }

            fms.Add(new FileMerger.FileMerger(masterFile, subFiles)
            {
                Enabled = true
            });
            fileMergerListBox.SelectedIndex = fileMergerListBox.Items.Count - 1;
            fileMergerListBox_SelectedIndexChanged(this, new EventArgs());
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //HACK this works...?????????
            fms[fileMergerListBox.SelectedIndex].Dispose();
            fms.RemoveAt(fileMergerListBox.SelectedIndex);
        }

        private void fileMergerListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            fileMergerPropertyGrid.SelectedObject = fileMergerListBox.SelectedItem;
        }

        #endregion
    }
}
