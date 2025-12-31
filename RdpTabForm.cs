using System;
using System.Drawing;
using System.Windows.Forms;
using RDPManager.Models;
using RDPManager.Utils;

namespace RDPManager
{
    /// <summary>
    /// RDP 连接窗口 - 浏览器风格多标签页
    /// </summary>
    public class RdpTabForm : Form
    {
        private TabControl tabControl;
        private ToolStrip toolStrip;
        private ToolStripButton btnFullScreen;
        private ToolStripButton btnFitWindow;
        private ToolStripDropDownButton btnResolution;
        private ToolStripLabel lblStatus;

        public RdpTabForm()
        {
            InitializeComponent();
            ApplyModernStyle();
        }
        
        private void ApplyModernStyle()
        {
            // 应用 ToolStrip 样式
            toolStrip.Renderer = new UIHelper.ModernToolStripRenderer();
            toolStrip.BackColor = UIHelper.ColorBackground;
            
            this.Font = UIHelper.MainFont;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 工具栏
            toolStrip = new ToolStrip();
            toolStrip.Dock = DockStyle.Top;
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.Padding = new Padding(5); // 增加内边距

            btnFullScreen = new ToolStripButton();
            btnFullScreen.Text = "全屏 (F11)";
            btnFullScreen.Click += BtnFullScreen_Click;
            toolStrip.Items.Add(btnFullScreen);

            toolStrip.Items.Add(new ToolStripSeparator());

            // 适合窗口按钮（独立按钮）
            btnFitWindow = new ToolStripButton();
            btnFitWindow.Text = "适合窗口";
            btnFitWindow.ToolTipText = "调整分辨率以适合当前窗口大小";
            btnFitWindow.Click += BtnFitWindow_Click;
            toolStrip.Items.Add(btnFitWindow);

            // 分辨率调整下拉按钮
            btnResolution = new ToolStripDropDownButton();
            btnResolution.Text = "分辨率";
            btnResolution.ToolTipText = "选择分辨率并全屏";
            // 添加常用分辨率选项
            string[] resolutions = new string[] {
                "1920x1080", "1680x1050", "1600x900", "1440x900",
                "1366x768", "1280x1024", "1280x800", "1280x720",
                "1024x768"
            };
            foreach (string res in resolutions)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(res);
                item.Tag = res;
                item.Click += ResolutionMenuItem_Click;
                btnResolution.DropDownItems.Add(item);
            }
            toolStrip.Items.Add(btnResolution);

            toolStrip.Items.Add(new ToolStripSeparator());

            lblStatus = new ToolStripLabel();
            lblStatus.Text = "就绪";
            toolStrip.Items.Add(lblStatus);

            // 标签页控件
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.SizeMode = TabSizeMode.Fixed;
            tabControl.ItemSize = new Size(200, 32); // 增加高度
            tabControl.Padding = new Point(15, 5);
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.MouseClick += TabControl_MouseClick;
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            // 添加控件
            this.Controls.Add(tabControl);
            this.Controls.Add(toolStrip);

            // 窗体设置
            this.Text = "R远程_3389管理器";
            this.Size = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.KeyPreview = true;
            this.KeyDown += RdpTabForm_KeyDown;
            this.FormClosing += RdpTabForm_FormClosing;

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        /// <summary>
        /// 添加新的 RDP 连接标签页
        /// </summary>
        public void AddConnection(RdpConnection connection, string password)
        {
            // 创建标签页
            TabPage tabPage = new TabPage();
            tabPage.Text = connection.Name + "    "; // 留空间给关闭按钮
            tabPage.Tag = connection.Id;

            // 创建 RDP 面板
            RdpPanel rdpPanel = new RdpPanel(connection, password);
            rdpPanel.StatusChanged += RdpPanel_StatusChanged;
            rdpPanel.ConnectionClosed += RdpPanel_ConnectionClosed;
            rdpPanel.Disconnected += RdpPanel_Disconnected;

            tabPage.Controls.Add(rdpPanel);

            // 添加标签页并切换
            tabControl.TabPages.Add(tabPage);
            tabControl.SelectedTab = tabPage;

            // 连接
            rdpPanel.Connect();

            UpdateTitle();
        }

        private void RdpPanel_StatusChanged(object sender, string status)
        {
            RdpPanel panel = sender as RdpPanel;
            if (panel != null)
            {
                lblStatus.Text = string.Format("{0}: {1}", panel.ConnectionName, status);
            }
        }

        private void RdpPanel_ConnectionClosed(object sender, EventArgs e)
        {
            // 找到对应的标签页并关闭
            RdpPanel panel = sender as RdpPanel;
            if (panel != null)
            {
                foreach (TabPage tab in tabControl.TabPages)
                {
                    if (tab.Controls.Contains(panel))
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            CloseTab(tab);
                        }));
                        break;
                    }
                }
            }
        }

        private void RdpPanel_Disconnected(object sender, int discReason)
        {
            RdpPanel panel = sender as RdpPanel;
            if (panel != null)
            {
                // 非正常断开时显示提示
                if (discReason != 1 && discReason != 2)
                {
                    MessageBox.Show(string.Format("{0} 连接已断开\n原因代码: {1}", panel.ConnectionName, discReason),
                        "连接断开", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// 绘制标签页（包含关闭按钮）
        /// </summary>
        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
             // 索引检查
            if (e.Index < 0 || e.Index >= tabControl.TabPages.Count) return;

            TabPage tabPage = tabControl.TabPages[e.Index];
            Rectangle tabRect = tabControl.GetTabRect(e.Index);
            bool isSelected = (tabControl.SelectedIndex == e.Index);

            // 准备画刷和颜色
            Color bgColor = isSelected ? UIHelper.TabActiveBg : UIHelper.TabInactiveBg;
            Color textColor = isSelected ? UIHelper.ColorTextMain : UIHelper.ColorTextLight;
            Font textFont = isSelected ? UIHelper.BoldFont : UIHelper.MainFont;

            // 绘制背景
            using (SolidBrush bgBrush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(bgBrush, tabRect);
            }

            // 绘制顶部高亮条（仅选中时）
            if (isSelected)
            {
                using (Pen highlightPen = new Pen(UIHelper.ColorPrimary, 3))
                {
                    e.Graphics.DrawLine(highlightPen, tabRect.Left, tabRect.Top + 1, tabRect.Right, tabRect.Top + 1);
                }
            }
            
            // 绘制底部分隔线（非选中时）
            if (!isSelected)
            {
                 using (Pen borderPen = new Pen(UIHelper.ColorBorder))
                 {
                     e.Graphics.DrawLine(borderPen, tabRect.Left, tabRect.Bottom - 1, tabRect.Right, tabRect.Bottom - 1);
                 }
            }

            // 绘制文本
            string title = tabPage.Text.TrimEnd();
            
            // 计算文字区域，确保垂直居中
            Rectangle textRect = new Rectangle(
                tabRect.X + 8,
                tabRect.Y + 1,
                tabRect.Width - 30,
                tabRect.Height - 2
            );

            // 使用 EndEllipsis 让 GDI+ 自动处理截断和省略号
            TextRenderer.DrawText(e.Graphics, title, textFont,
                textRect,
                textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

            // 绘制关闭按钮 X
            int closeBtnSize = 16;
            int closeBtnX = tabRect.Right - 22;
            int closeBtnY = tabRect.Top + (tabRect.Height - closeBtnSize) / 2;
            Rectangle closeRect = new Rectangle(closeBtnX, closeBtnY, closeBtnSize, closeBtnSize);
            
            using (Font closeFont = new Font("Arial", 10, FontStyle.Bold))
            {
                TextRenderer.DrawText(e.Graphics, "×", closeFont, closeRect,
                    isSelected ? Color.FromArgb(120, 120, 120) : Color.FromArgb(150, 150, 150),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        /// <summary>
        /// 处理标签页点击（关闭按钮）
        /// </summary>
        private void TabControl_MouseClick(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControl.TabPages.Count; i++)
            {
                Rectangle tabRect = tabControl.GetTabRect(i);
                
                int closeBtnSize = 16;
                int closeBtnX = tabRect.Right - 22;
                int closeBtnY = tabRect.Top + (tabRect.Height - closeBtnSize) / 2;
                Rectangle closeRect = new Rectangle(closeBtnX, closeBtnY, closeBtnSize, closeBtnSize);

                if (closeRect.Contains(e.Location))
                {
                    TabPage tabPage = tabControl.TabPages[i];

                    // 确认关闭
                    RdpPanel panel = GetRdpPanel(tabPage);
                    if (panel != null && panel.IsConnected)
                    {
                        DialogResult result = MessageBox.Show(
                            string.Format("确定要断开 {0} 的连接吗？", panel.ConnectionName),
                            "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result != DialogResult.Yes)
                        {
                            return;
                        }
                    }

                    CloseTab(tabPage);
                    break;
                }
            }
        }

        private void CloseTab(TabPage tabPage)
        {
            // 断开连接并移除
            RdpPanel panel = GetRdpPanel(tabPage);
            if (panel != null)
            {
                panel.Disconnect();
            }

            tabControl.TabPages.Remove(tabPage);
            tabPage.Dispose();

            UpdateTitle();

            // 如果没有标签页了，关闭窗口
            if (tabControl.TabPages.Count == 0)
            {
                this.Close();
            }
        }

        private RdpPanel GetRdpPanel(TabPage tabPage)
        {
            foreach (Control ctrl in tabPage.Controls)
            {
                RdpPanel panel = ctrl as RdpPanel;
                if (panel != null)
                {
                    return panel;
                }
            }
            return null;
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            if (tabControl.SelectedTab != null)
            {
                RdpPanel panel = GetRdpPanel(tabControl.SelectedTab);
                if (panel != null)
                {
                    this.Text = string.Format("R远程_3389管理器 - {0}", panel.ConnectionName);
                    return;
                }
            }
            this.Text = "R远程_3389管理器";
        }

        private void BtnFullScreen_Click(object sender, EventArgs e)
        {
            ToggleFullScreen();
        }

        private void ToggleFullScreen()
        {
            if (this.FormBorderStyle == FormBorderStyle.None)
            {
                // 退出全屏
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Maximized;
                toolStrip.Visible = true;
            }
            else
            {
                // 进入全屏
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Normal;
                this.Bounds = Screen.PrimaryScreen.Bounds;
                toolStrip.Visible = false;
            }
        }

        private void RdpTabForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                ToggleFullScreen();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.W)
            {
                // Ctrl+W 关闭当前标签页
                if (tabControl.SelectedTab != null)
                {
                    CloseTab(tabControl.SelectedTab);
                }
                e.Handled = true;
            }
        }

        private void RdpTabForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 检查是否有活动连接
            int activeCount = 0;
            foreach (TabPage tab in tabControl.TabPages)
            {
                RdpPanel panel = GetRdpPanel(tab);
                if (panel != null && panel.IsConnected)
                {
                    activeCount++;
                }
            }

            if (activeCount > 0)
            {
                DialogResult result = MessageBox.Show(
                    string.Format("还有 {0} 个活动连接，确定要全部断开吗？", activeCount),
                    "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            // 断开所有连接
            foreach (TabPage tab in tabControl.TabPages)
            {
                RdpPanel panel = GetRdpPanel(tab);
                if (panel != null)
                {
                    panel.Disconnect();
                }
            }
        }

        /// <summary>
        /// 适合窗口按钮点击事件
        /// </summary>
        private void BtnFitWindow_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == null)
                return;

            RdpPanel panel = GetRdpPanel(tabControl.SelectedTab);
            if (panel == null || !panel.IsConnected)
            {
                MessageBox.Show("当前没有活动的连接！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 使用当前窗口的大小
            int width = tabControl.SelectedTab.ClientSize.Width;
            int height = tabControl.SelectedTab.ClientSize.Height;
            // 确保分辨率是偶数
            width = width - (width % 2);
            height = height - (height % 2);

            // 调整分辨率（不全屏）
            bool success = panel.ChangeResolution(width, height);
            if (success)
            {
                lblStatus.Text = string.Format("{0}: 已适配窗口 {1}x{2}", panel.ConnectionName, width, height);
            }
        }

        /// <summary>
        /// 分辨率菜单项点击事件 - 调整分辨率并进入全屏
        /// </summary>
        private void ResolutionMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
            if (menuItem == null || tabControl.SelectedTab == null)
                return;

            RdpPanel panel = GetRdpPanel(tabControl.SelectedTab);
            if (panel == null || !panel.IsConnected)
            {
                MessageBox.Show("当前没有活动的连接！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string resolution = menuItem.Tag as string;
            int width, height;

            // 解析分辨率字符串
            string[] parts = resolution.Split('x');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], out width) ||
                !int.TryParse(parts[1], out height))
            {
                return;
            }

            // 调整分辨率
            bool success = panel.ChangeResolution(width, height);
            if (success)
            {
                // 选择分辨率后进入全屏模式
                if (this.FormBorderStyle != FormBorderStyle.None)
                {
                    ToggleFullScreen();
                }
                lblStatus.Text = string.Format("{0}: 已调整为 {1}x{2}", panel.ConnectionName, width, height);
            }
        }
    }
}
