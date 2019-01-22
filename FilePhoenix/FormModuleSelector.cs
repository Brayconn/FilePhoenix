using System;
using System.Windows.Forms;

//TODO make sure this form's UI is actually precise
namespace FilePhoenix
{
    public partial class FormModuleSelector : Form
    {
        public int SelectedIndex { get; private set; }

        public FormModuleSelector(string[] moduleList)
        {
            InitializeComponent();

            comboBox1.Items.AddRange(moduleList);
            comboBox1.SelectedIndex = 0;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedIndex = comboBox1.SelectedIndex;
        }
    }
}
