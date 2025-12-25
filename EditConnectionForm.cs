using System;
using System.Windows.Forms;
using RDPManager.Models;
using RDPManager.Utils;

namespace RDPManager
{
    /// <summary>
    /// 连接编辑对话框
    /// </summary>
    public partial class EditConnectionForm : Form
    {
        public RdpConnection Connection { get; private set; }
        private bool _isEditMode = false;

        /// <summary>
        /// 新建模式构造函数
        /// </summary>
        public EditConnectionForm()
        {
            InitializeComponent();
            Connection = new RdpConnection();
            _isEditMode = false;
            this.Text = "新建连接";
            InitializeDefaultValues();
        }

        /// <summary>
        /// 编辑模式构造函数
        /// </summary>
        public EditConnectionForm(RdpConnection connection)
        {
            InitializeComponent();
            Connection = connection;
            _isEditMode = true;
            this.Text = "编辑连接";
            LoadConnectionData();
        }

        /// <summary>
        /// 初始化默认值
        /// </summary>
        private void InitializeDefaultValues()
        {
            txtPort.Text = "3389";
            txtWidth.Text = "1920";
            txtHeight.Text = "1080";
            cmbColorDepth.SelectedIndex = 3; // 32位
            chkFullScreen.Checked = true;
            chkAutoFit.Checked = true;
            UpdateResolutionFields();
        }

        /// <summary>
        /// 加载连接数据到表单
        /// </summary>
        private void LoadConnectionData()
        {
            txtName.Text = Connection.Name;
            txtServerAddress.Text = Connection.ServerAddress;
            txtPort.Text = Connection.Port.ToString();
            txtUsername.Text = Connection.Username;

            // 解密密码显示
            if (!string.IsNullOrEmpty(Connection.EncryptedPassword))
            {
                txtPassword.Text = EncryptionHelper.Decrypt(Connection.EncryptedPassword);
            }

            chkFullScreen.Checked = Connection.IsFullScreen;
            chkAutoFit.Checked = Connection.AutoFitResolution;
            txtWidth.Text = Connection.Width.ToString();
            txtHeight.Text = Connection.Height.ToString();

            // 设置颜色深度
            switch (Connection.ColorDepth)
            {
                case 8:
                    cmbColorDepth.SelectedIndex = 0;
                    break;
                case 16:
                    cmbColorDepth.SelectedIndex = 1;
                    break;
                case 24:
                    cmbColorDepth.SelectedIndex = 2;
                    break;
                case 32:
                default:
                    cmbColorDepth.SelectedIndex = 3;
                    break;
            }

            txtRemarks.Text = Connection.Remarks;
            UpdateResolutionFields();
        }

        /// <summary>
        /// 保存按钮点击
        /// </summary>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("请输入连接名称！", "验证失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtServerAddress.Text))
            {
                MessageBox.Show("请输入服务器地址！", "验证失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtServerAddress.Focus();
                return;
            }

            if (!int.TryParse(txtPort.Text, out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("请输入有效的端口号（1-65535）！", "验证失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPort.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("请输入用户名！", "验证失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return;
            }

            // 如果不是全屏，验证分辨率
            if (!chkFullScreen.Checked)
            {
                if (!int.TryParse(txtWidth.Text, out int width) || width < 640)
                {
                    MessageBox.Show("请输入有效的宽度（至少640）！", "验证失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtWidth.Focus();
                    return;
                }

                if (!int.TryParse(txtHeight.Text, out int height) || height < 480)
                {
                    MessageBox.Show("请输入有效的高度（至少480）！", "验证失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtHeight.Focus();
                    return;
                }
            }

            try
            {
                // 保存数据到 Connection 对象
                Connection.Name = txtName.Text.Trim();
                Connection.ServerAddress = txtServerAddress.Text.Trim();
                Connection.Port = int.Parse(txtPort.Text);
                Connection.Username = txtUsername.Text.Trim();

                // 加密密码
                if (!string.IsNullOrEmpty(txtPassword.Text))
                {
                    Connection.EncryptedPassword = EncryptionHelper.Encrypt(txtPassword.Text);
                }

                Connection.IsFullScreen = chkFullScreen.Checked;
                Connection.AutoFitResolution = chkAutoFit.Checked;
                Connection.Width = int.TryParse(txtWidth.Text, out int w) ? w : 1920;
                Connection.Height = int.TryParse(txtHeight.Text, out int h) ? h : 1080;

                // 获取颜色深度
                switch (cmbColorDepth.SelectedIndex)
                {
                    case 0:
                        Connection.ColorDepth = 8;
                        break;
                    case 1:
                        Connection.ColorDepth = 16;
                        break;
                    case 2:
                        Connection.ColorDepth = 24;
                        break;
                    default:
                        Connection.ColorDepth = 32;
                        break;
                }

                Connection.Remarks = txtRemarks.Text.Trim();

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 取消按钮点击
        /// </summary>
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// 全屏复选框状态改变
        /// </summary>
        private void ChkFullScreen_CheckedChanged(object sender, EventArgs e)
        {
            UpdateResolutionFields();
        }

        /// <summary>
        /// 自动适应复选框状态改变
        /// </summary>
        private void ChkAutoFit_CheckedChanged(object sender, EventArgs e)
        {
            UpdateResolutionFields();
        }

        /// <summary>
        /// 更新分辨率字段的启用状态
        /// </summary>
        private void UpdateResolutionFields()
        {
            // 全屏或自动适应时，分辨率字段禁用
            bool enableResolution = !chkFullScreen.Checked && !chkAutoFit.Checked;
            txtWidth.Enabled = enableResolution;
            txtHeight.Enabled = enableResolution;
            label6.Enabled = enableResolution;
            label7.Enabled = enableResolution;
        }
    }
}
