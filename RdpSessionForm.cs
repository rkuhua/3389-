using System;
using System.Windows.Forms;
using AxMSTSCLib;
using MSTSCLib;
using RDPManager.Models;
using RDPManager.Utils;

namespace RDPManager
{
    /// <summary>
    /// 内嵌式 RDP 连接窗口
    /// </summary>
    public partial class RdpSessionForm : Form
    {
        private AxMsRdpClient9NotSafeForScripting rdpClient;
        private readonly RdpConnection _connection;
        private readonly string _password;

        public RdpSessionForm(RdpConnection connection, string password)
        {
            _connection = connection;
            _password = password;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 窗体设置
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "RdpSessionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = string.Format("RDP - {0}", _connection.Name);
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;

            this.FormClosing += RdpSessionForm_FormClosing;
            this.Load += RdpSessionForm_Load;

            this.ResumeLayout(false);
        }

        private void RdpSessionForm_Load(object sender, EventArgs e)
        {
            InitializeRdpClient();
        }

        private void InitializeRdpClient()
        {
            try
            {
                // 创建 RDP 控件
                rdpClient = new AxMsRdpClient9NotSafeForScripting();
                ((System.ComponentModel.ISupportInitialize)(rdpClient)).BeginInit();

                rdpClient.Dock = DockStyle.Fill;
                rdpClient.Name = "rdpClient";
                rdpClient.Enabled = true;

                this.Controls.Add(rdpClient);
                ((System.ComponentModel.ISupportInitialize)(rdpClient)).EndInit();

                // 配置连接参数
                rdpClient.Server = _connection.ServerAddress;
                rdpClient.AdvancedSettings9.RDPPort = _connection.Port;
                rdpClient.UserName = _connection.Username;

                // 设置密码 - 自动填充
                if (!string.IsNullOrEmpty(_password))
                {
                    rdpClient.AdvancedSettings9.ClearTextPassword = _password;
                }

                // 禁用凭据提示（使用保存的密码）
                rdpClient.AdvancedSettings9.EnableCredSspSupport = true;
                rdpClient.AdvancedSettings9.AuthenticationLevel = 0;
                rdpClient.AdvancedSettings9.NegotiateSecurityLayer = true;

                // 分辨率设置 - 使用窗口实际大小
                rdpClient.DesktopWidth = this.ClientSize.Width;
                rdpClient.DesktopHeight = this.ClientSize.Height;

                // 如果设置了全屏，使用 RDP 全屏模式
                if (_connection.IsFullScreen)
                {
                    rdpClient.FullScreen = true;
                }

                // 颜色深度
                rdpClient.ColorDepth = _connection.ColorDepth;

                // 其他设置
                rdpClient.AdvancedSettings9.EnableAutoReconnect = true;
                rdpClient.AdvancedSettings9.MaxReconnectAttempts = 5;
                rdpClient.AdvancedSettings9.Compress = 1;
                rdpClient.AdvancedSettings9.BitmapPeristence = 1;

                // 显示连接栏
                rdpClient.AdvancedSettings9.DisplayConnectionBar = true;
                rdpClient.AdvancedSettings9.PinConnectionBar = false;

                // 连接事件
                rdpClient.OnConnected += RdpClient_OnConnected;
                rdpClient.OnDisconnected += RdpClient_OnDisconnected;
                rdpClient.OnFatalError += RdpClient_OnFatalError;
                rdpClient.OnLoginComplete += RdpClient_OnLoginComplete;

                // 开始连接
                rdpClient.Connect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化 RDP 连接失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void RdpClient_OnConnected(object sender, EventArgs e)
        {
            this.Text = $"RDP - {_connection.Name} (已连接)";
        }

        private void RdpClient_OnLoginComplete(object sender, EventArgs e)
        {
            this.Text = $"RDP - {_connection.Name} (登录成功)";
        }

        private void RdpClient_OnDisconnected(object sender, IMsTscAxEvents_OnDisconnectedEvent e)
        {
            MessageBox.Show($"连接已断开\n原因代码: {e.discReason}", "连接断开",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private void RdpClient_OnFatalError(object sender, IMsTscAxEvents_OnFatalErrorEvent e)
        {
            MessageBox.Show($"发生致命错误\n错误代码: {e.errorCode}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            this.Close();
        }

        private void RdpSessionForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (rdpClient != null && rdpClient.Connected == 1)
                {
                    var result = MessageBox.Show("确定要断开远程连接吗？", "确认",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result == DialogResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }

                    rdpClient.Disconnect();
                }
            }
            catch
            {
                // 忽略断开连接时的错误
            }
        }
    }
}
