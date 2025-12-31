using System;
using System.Drawing;
using System.Windows.Forms;
using RDPManager.Utils;

namespace RDPManager
{
    public class SettingsForm : Form
    {
        private ComboBox comboTheme;
        private Button btnSave;
        private Button btnCancel;

        public string SelectedTheme { get; private set; }

        public SettingsForm(string currentTheme)
        {
            InitializeComponent();
            ApplyTheme();
            
            // 初始化选项
            comboTheme.Items.Add("Default (Light)");
            comboTheme.Items.Add("Sky Blue");
            comboTheme.Items.Add("Dark Mode");
            
            // 选中当前主题
            int index = 0;
            if (currentTheme == "Sky Blue") index = 1;
            else if (currentTheme == "Dark Mode") index = 2;
            comboTheme.SelectedIndex = index;
        }

        private void InitializeComponent()
        {
            this.Text = "设置";
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblTheme = new Label();
            lblTheme.Text = "界面主题:";
            lblTheme.Location = new Point(30, 30);
            lblTheme.AutoSize = true;

            comboTheme = new ComboBox();
            comboTheme.DropDownStyle = ComboBoxStyle.DropDownList;
            comboTheme.Location = new Point(120, 27);
            comboTheme.Width = 200;

            btnSave = new Button();
            btnSave.Text = "保存";
            btnSave.DialogResult = DialogResult.OK;
            btnSave.Location = new Point(190, 150);
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(280, 150);

            this.Controls.Add(lblTheme);
            this.Controls.Add(comboTheme);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
            
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }
        
        private void ApplyTheme()
        {
            this.BackColor = UIHelper.ColorBackground;
            this.ForeColor = UIHelper.ColorTextMain;
            this.Font = UIHelper.MainFont;
            
            foreach(Control c in this.Controls)
            {
                c.Font = UIHelper.MainFont;
                if(c is Button)
                {
                    c.BackColor = UIHelper.ColorPanelLeft;
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SelectedTheme = comboTheme.SelectedItem.ToString();
        }
    }
}