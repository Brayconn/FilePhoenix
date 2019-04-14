using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using static FilePhoenix.Extensions.HelperMethods;

namespace FileSplitter
{
    public partial class FileSplitter
    {
        private class OpenFileNameEditor : UITypeEditor
        {
            public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => UITypeEditorEditStyle.Modal;

            public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
            {
                if (!(context.Instance is FileSplitter parent) || context == null || provider == null)
                    return base.EditValue(context, provider, value);

                using (OpenFileDialog ofd = new OpenFileDialog()
                {
                    Title = context.PropertyDescriptor.DisplayName,
                    Filter = string.Join("|", (parent.FileTypeModule?.OpenFileDialogFilters
                        ?? new ReadOnlyCollection<string>(new string[0])).Concat(new string[] { "All Files (*.*)|*.*" })),
                })
                {
                    ofd.FileName = value as string ?? ofd.FileName;

                    if (ofd.ShowDialog() == DialogResult.OK
                        && (parent.FileLoaded //TODO this is a mess of conditionals
                        ? MessageBox.Show("Changing this setting will result in a loss of all current progress, " +
                        "are you sure you want to change it?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes
                        : true))
                    {
                        value = ofd.FileName;
                    }
                }
                return value;
            }
        }

        private class FileSplitterModuleTypeConverter : ExpandableObjectConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                return (context.Instance is FileSplitter parent)
                    ? new StandardValuesCollection(parent.LoadedModules.Keys)
                    : base.GetStandardValues(context);
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                return (value is string input && context.Instance is FileSplitter parent && parent.LoadedModules.ContainsKey(input))
                        ? (IFileSplitterModule)Activator.CreateInstance(parent.LoadedModules[input])
                        : null; //TODO maybe reconsider using this again? base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return (destinationType == typeof(string)) || base.CanConvertFrom(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                return (value is IFileSplitterModule module)
                    ? module.DisplayName
                    : base.ConvertTo(context, culture, value, destinationType);
            }
        }

        private class OpenFolderNameEditor : FolderNameEditor
        {
            public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
            {
                //TODO check ?.
                if (context?.Instance == null || provider == null || !CanUseWinAPICodePackFolderBrowser)
                    return base.EditValue(context, provider, value);

                using (CommonOpenFileDialog folderPicker = new CommonOpenFileDialog()
                {
                    IsFolderPicker = true
                })
                {
                    if (value.GetType() == typeof(string))
                        folderPicker.InitialDirectory = Path.GetDirectoryName((string)value);

                    //TODO another mess of conditionals
                    if (folderPicker.ShowDialog() == CommonFileDialogResult.Ok &&
                        (Directory.EnumerateFileSystemEntries(folderPicker.FileName).Any() ?
                        MessageBox.Show("The contents of the selected folder will be deleted if/when a file is loaded. " +
                        "Are you sure you want to continue?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes
                        : true))
                    {
                        value = folderPicker.FileName;
                    }
                }
                return value;
            }
        }

        private class SaveFileNameEditor : UITypeEditor
        {
            public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => UITypeEditorEditStyle.Modal;

            public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
            {
                if (!(context.Instance is FileSplitter parent) || context == null || provider == null)
                    return base.EditValue(context, provider, value);

                using (SaveFileDialog sfd = new SaveFileDialog()
                {
                    Title = context.PropertyDescriptor.DisplayName,
                    //Join the SaveFileDialogFilters if they exist, either way, add "All Files (*.*)|*.* to the end
                    Filter = string.Join("|", (parent.FileTypeModule?.SaveFileDialogFilters
                        ?? new ReadOnlyCollection<string>(new string[0])).Concat(new string[] { "All Files (*.*)|*.*" })),
                })
                {
                    sfd.FileName = value as string ?? sfd.FileName;

                    if (sfd.ShowDialog() == DialogResult.OK)
                        value = sfd.FileName;
                }

                return value;
            }
        }

        private class BlackListTypeConverter : TypeConverter
        {
            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                return (destinationType == typeof(string) && value is IList<string> input)
                ? string.Join(", ", input)
                : base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}
