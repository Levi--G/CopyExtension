using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CopyExtension
{
    public partial class OptionControl : UserControl
    {
        string details;
        public OptionControl(string text, string details, object[] choices)
        {
            this.details = details;
            InitializeComponent();
            label1.Text = text;
            foreach (var item in choices)
            {
                var b = new Button() { Text = item.ToString(), Margin = new Padding(50, 5, 50, 5), Size = new Size(96, 32) };
                b.Click += (s, e) =>
                {
                    OnChoose?.Invoke(item);
                };
                flowLayoutPanel1.Controls.Add(b);
            }
        }

        public event Action<object> OnChoose;

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(details);
        }
    }
}
