using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RDPManager.Models;
using RDPManager.Utils;

namespace RDPManager
{
    /// <summary>
    /// 新主窗口 - 左右分栏布局
    /// </summary>
    public class MainFormNew : Form
    {
        // 数据管理
        private readonly DataManager _dataManager;

        // 左侧面板
        private Panel leftPanel;
        private TreeView treeView;
        private bool leftPanelVisible = true;
        private int leftPanelWidth = 170;

        // 右侧面板
        private Panel rightPanel;
        private TabControl tabControl;
        private ToolStrip toolStrip;

        // 工具栏按钮
        private ToolStripButton btnAdd;
        private ToolStripButton btnEdit;
        private ToolStripButton btnDelete;
        private ToolStripButton btnFullScreen;
        private ToolStripButton btnFitWindow;
        private ToolStripDropDownButton btnResolution;
        private ToolStripButton btnTogglePanel;
        private ToolStripButton btnDisconnect;
        private ToolStripLabel lblStatus;

        // 终端计数器
        private int _terminalCounter = 0;

        // 全屏相关
        private bool _isFullScreen = false;
        private FormWindowState _previousWindowState;
        private FormBorderStyle _previousBorderStyle;
        private bool _previousLeftPanelVisible;
        private SplitContainer _splitContainer;

        public MainFormNew()
        {
            _dataManager = new DataManager();
            InitializeComponent();
            LoadConnections();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 窗体设置
            this.Text = "R远程_3389管理器 - 远程桌面管理器 by作者微信：rrror777";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.KeyPreview = true;
            this.KeyDown += MainFormNew_KeyDown;
            this.FormClosing += MainFormNew_FormClosing;
            this.Load += MainFormNew_Load;

            // 加载窗体图标
            try
            {
                string iconPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location), "app.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch { /* 忽略图标加载失败 */ }

            // 创建分隔条
            _splitContainer = new SplitContainer();
            _splitContainer.Dock = DockStyle.Fill;
            _splitContainer.SplitterDistance = leftPanelWidth;
            _splitContainer.SplitterWidth = 4;
            _splitContainer.FixedPanel = FixedPanel.None; // 允许双向自由调整
            _splitContainer.Panel1MinSize = 100; // 左侧最小宽度
            _splitContainer.Panel2MinSize = 100; // 右侧最小宽度

            // === 左侧面板 ===
            leftPanel = new Panel();
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.BackColor = Color.FromArgb(250, 250, 250);

            // 左侧标题栏
            Panel leftHeader = new Panel();
            leftHeader.Dock = DockStyle.Top;
            leftHeader.Height = 30;
            leftHeader.BackColor = Color.FromArgb(240, 240, 240);

            Label lblConnections = new Label();
            lblConnections.Text = "连接列表";
            lblConnections.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
            lblConnections.Location = new Point(10, 6);
            lblConnections.AutoSize = true;
            leftHeader.Controls.Add(lblConnections);

            // 树形列表
            treeView = new TreeView();
            treeView.Dock = DockStyle.Fill;
            treeView.Font = new Font("Microsoft YaHei", 9);
            treeView.ImageList = CreateImageList();
            treeView.ShowLines = true;
            treeView.ShowPlusMinus = true;
            treeView.ShowRootLines = true;
            treeView.HideSelection = false;
            treeView.DoubleClick += TreeView_DoubleClick;
            treeView.MouseUp += TreeView_MouseUp;

            leftPanel.Controls.Add(treeView);
            leftPanel.Controls.Add(leftHeader);

            _splitContainer.Panel1.Controls.Add(leftPanel);

            // === 右侧面板 ===
            rightPanel = new Panel();
            rightPanel.Dock = DockStyle.Fill;

            // 工具栏
            toolStrip = new ToolStrip();
            toolStrip.Dock = DockStyle.Top;
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.ImageScalingSize = new Size(20, 20);

            // 新建按钮
            btnAdd = new ToolStripButton();
            btnAdd.Text = "新建";
            btnAdd.ToolTipText = "新建连接 (Ctrl+N)";
            btnAdd.Click += BtnAdd_Click;
            toolStrip.Items.Add(btnAdd);

            // 快速连接下拉按钮
            var btnQuickConnect = new ToolStripDropDownButton();
            btnQuickConnect.Text = "连接";
            btnQuickConnect.ToolTipText = "快速连接到服务器";
            btnQuickConnect.DropDownOpening += BtnQuickConnect_DropDownOpening;
            toolStrip.Items.Add(btnQuickConnect);

            // 编辑按钮
            btnEdit = new ToolStripButton();
            btnEdit.Text = "编辑";
            btnEdit.ToolTipText = "编辑选中的连接";
            btnEdit.Click += BtnEdit_Click;
            toolStrip.Items.Add(btnEdit);

            // 删除按钮
            btnDelete = new ToolStripButton();
            btnDelete.Text = "删除";
            btnDelete.ToolTipText = "删除选中的连接";
            btnDelete.Click += BtnDelete_Click;
            toolStrip.Items.Add(btnDelete);

            toolStrip.Items.Add(new ToolStripSeparator());

            // 全屏按钮
            btnFullScreen = new ToolStripButton();
            btnFullScreen.Text = "全屏";
            btnFullScreen.ToolTipText = "全屏 (F11)";
            btnFullScreen.Click += BtnFullScreen_Click;
            toolStrip.Items.Add(btnFullScreen);

            // 适合窗口按钮
            btnFitWindow = new ToolStripButton();
            btnFitWindow.Text = "适合窗口";
            btnFitWindow.ToolTipText = "调整分辨率以适合当前窗口";
            btnFitWindow.Click += BtnFitWindow_Click;
            toolStrip.Items.Add(btnFitWindow);

            // 分辨率下拉按钮
            btnResolution = new ToolStripDropDownButton();
            btnResolution.Text = "分辨率";
            btnResolution.ToolTipText = "选择分辨率";
            string[] resolutions = new string[] {
                "1920x1080", "1680x1050", "1600x900", "1440x900",
                "1366x768", "1280x1024", "1280x800", "1280x720", "1024x768"
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

            // 隐藏/显示左侧面板按钮
            btnTogglePanel = new ToolStripButton();
            btnTogglePanel.Text = "隐藏列表";
            btnTogglePanel.ToolTipText = "隐藏/显示连接列表";
            btnTogglePanel.Click += BtnTogglePanel_Click;
            toolStrip.Items.Add(btnTogglePanel);

            // 断开按钮
            btnDisconnect = new ToolStripButton();
            btnDisconnect.Text = "断开";
            btnDisconnect.ToolTipText = "断开当前连接 (Ctrl+W)";
            btnDisconnect.Click += BtnDisconnect_Click;
            toolStrip.Items.Add(btnDisconnect);

            toolStrip.Items.Add(new ToolStripSeparator());

            // "+" 新建终端按钮
            var btnNewTerminal = new ToolStripDropDownButton();
            btnNewTerminal.Text = "+ 终端";
            btnNewTerminal.ToolTipText = "打开新的终端标签页";

            var cmdItem = new ToolStripMenuItem("命令提示符 (CMD)");
            cmdItem.Click += (s, args) => OpenTerminal("cmd");
            btnNewTerminal.DropDownItems.Add(cmdItem);

            var psItem = new ToolStripMenuItem("Windows PowerShell");
            psItem.Click += (s, args) => OpenTerminal("powershell");
            btnNewTerminal.DropDownItems.Add(psItem);

            toolStrip.Items.Add(btnNewTerminal);

            toolStrip.Items.Add(new ToolStripSeparator());

            // 状态标签
            lblStatus = new ToolStripLabel();
            lblStatus.Text = "就绪";
            toolStrip.Items.Add(lblStatus);

            // 标签页控件
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.SizeMode = TabSizeMode.Fixed;
            tabControl.ItemSize = new Size(180, 25);
            tabControl.Padding = new Point(12, 3);
            tabControl.AllowDrop = true; // 允许拖拽
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.MouseClick += TabControl_MouseClick;
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            
            // 拖拽事件
            tabControl.MouseDown += TabControl_MouseDown;
            tabControl.DragOver += TabControl_DragOver;
            tabControl.DragDrop += TabControl_DragDrop;

            rightPanel.Controls.Add(tabControl);
            rightPanel.Controls.Add(toolStrip);

            _splitContainer.Panel2.Controls.Add(rightPanel);

            this.Controls.Add(_splitContainer);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        /// <summary>
        /// 创建图标列表
        /// </summary>
        private ImageList CreateImageList()
        {
            ImageList imageList = new ImageList();
            imageList.ImageSize = new Size(16, 16);
            imageList.ColorDepth = ColorDepth.Depth32Bit;
            return imageList;
        }

        /// <summary>
        /// 加载连接列表到树形视图（支持文件夹）
        /// </summary>
        private void LoadConnections()
        {
            treeView.Nodes.Clear();

            TreeNode rootNode = new TreeNode("所有连接");
            rootNode.Tag = "ROOT";
            rootNode.ImageIndex = 0;

            try
            {
                // 递归加载文件夹和连接
                LoadFolderNodes(rootNode, string.Empty);

                treeView.Nodes.Add(rootNode);
                rootNode.Expand();

                var connections = _dataManager.GetAllConnections();
                lblStatus.Text = string.Format("共 {0} 个连接", connections.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("加载连接失败: {0}", ex.Message), "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 递归加载文件夹节点
        /// </summary>
        private void LoadFolderNodes(TreeNode parentNode, string parentFolderId)
        {
            // 加载子文件夹
            var folders = _dataManager.GetSubFolders(parentFolderId);
            foreach (var folder in folders)
            {
                TreeNode folderNode = new TreeNode(folder.Name);
                folderNode.Tag = folder;
                folderNode.ImageIndex = 1;
                folderNode.SelectedImageIndex = 1;

                // 递归加载子节点
                LoadFolderNodes(folderNode, folder.Id);

                parentNode.Nodes.Add(folderNode);

                if (folder.IsExpanded)
                {
                    folderNode.Expand();
                }
            }

            // 加载该文件夹下的连接
            var connections = _dataManager.GetConnectionsByFolder(parentFolderId);
            foreach (var conn in connections)
            {
                TreeNode node = new TreeNode(conn.Name);
                node.Tag = conn;
                node.ImageIndex = 2;
                node.SelectedImageIndex = 2;
                node.ToolTipText = string.Format("{0}@{1}", conn.Username, conn.FullAddress);
                parentNode.Nodes.Add(node);
            }
        }

        #region 树形视图事件

        /// <summary>
        /// 双击连接项 - 打开连接
        /// </summary>
        private void TreeView_DoubleClick(object sender, EventArgs e)
        {
            TreeNode selectedNode = treeView.SelectedNode;
            if (selectedNode != null && selectedNode.Tag is RdpConnection)
            {
                RdpConnection conn = (RdpConnection)selectedNode.Tag;
                ConnectToRemote(conn);
            }
        }

        /// <summary>
        /// 右键菜单
        /// </summary>
        private void TreeView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeNode node = treeView.GetNodeAt(e.Location);
                if (node != null)
                {
                    treeView.SelectedNode = node;

                    ContextMenuStrip menu = new ContextMenuStrip();

                    if (node.Tag is RdpConnection)
                    {
                        // 连接节点的右键菜单
                        menu.Items.Add("连接", null, (s, args) => {
                            ConnectToRemote((RdpConnection)node.Tag);
                        });
                        menu.Items.Add("-");
                        menu.Items.Add("编辑", null, (s, args) => {
                            EditConnection((RdpConnection)node.Tag);
                        });
                        menu.Items.Add("删除", null, (s, args) => {
                            DeleteConnection((RdpConnection)node.Tag);
                        });
                    }
                    else if (node.Tag is ConnectionFolder)
                    {
                        // 文件夹节点的右键菜单
                        ConnectionFolder folder = (ConnectionFolder)node.Tag;
                        menu.Items.Add("新建连接", null, (s, args) => {
                            AddNewConnectionToFolder(folder.Id);
                        });
                        menu.Items.Add("新建子文件夹", null, (s, args) => {
                            AddNewFolder(folder.Id);
                        });
                        menu.Items.Add("-");
                        menu.Items.Add("重命名", null, (s, args) => {
                            RenameFolder(folder);
                        });
                        menu.Items.Add("删除文件夹", null, (s, args) => {
                            DeleteFolder(folder);
                        });
                    }
                    else
                    {
                        // 根节点的右键菜单
                        menu.Items.Add("新建连接", null, (s, args) => {
                            AddNewConnection();
                        });
                        menu.Items.Add("新建文件夹", null, (s, args) => {
                            AddNewFolder(string.Empty);
                        });
                        menu.Items.Add("-");
                        menu.Items.Add("刷新", null, (s, args) => {
                            LoadConnections();
                        });
                    }

                    menu.Show(treeView, e.Location);
                }
            }
        }

        #endregion

        #region 远程连接

        /// <summary>
        /// 连接到远程桌面
        /// </summary>
        private void ConnectToRemote(RdpConnection connection)
        {
            try
            {
                // 检查是否已有该连接的标签页
                foreach (TabPage tab in tabControl.TabPages)
                {
                    if (tab.Tag != null && tab.Tag.ToString() == connection.Id)
                    {
                        tabControl.SelectedTab = tab;
                        return;
                    }
                }

                // 解密密码
                string password = EncryptionHelper.Decrypt(connection.EncryptedPassword);

                // 创建标签页
                TabPage tabPage = new TabPage();
                tabPage.Text = connection.Name + "    ";
                tabPage.Tag = connection.Id;

                // 创建 RDP 面板
                RdpPanel rdpPanel = new RdpPanel(connection, password);
                rdpPanel.StatusChanged += RdpPanel_StatusChanged;
                rdpPanel.ConnectionClosed += RdpPanel_ConnectionClosed;
                rdpPanel.Disconnected += RdpPanel_Disconnected;

                tabPage.Controls.Add(rdpPanel);
                tabControl.TabPages.Add(tabPage);
                tabControl.SelectedTab = tabPage;

                // 连接
                rdpPanel.Connect();
                UpdateTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("连接失败: {0}", ex.Message), "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region RDP事件处理

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
            RdpPanel panel = sender as RdpPanel;
            if (panel != null)
            {
                foreach (TabPage tab in tabControl.TabPages)
                {
                    if (tab.Controls.Contains(panel))
                    {
                        this.BeginInvoke(new Action(() => {
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
            if (panel != null && discReason != 1 && discReason != 2)
            {
                MessageBox.Show(string.Format("{0} 连接已断开\n原因代码: {1}", panel.ConnectionName, discReason),
                    "连接断开", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion

        #region 标签页处理

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            // 索引检查，防止拖拽时越界
            if (e.Index < 0 || e.Index >= tabControl.TabPages.Count) return;

            TabPage tabPage = tabControl.TabPages[e.Index];
            Rectangle tabRect = tabControl.GetTabRect(e.Index);

            if (tabControl.SelectedIndex == e.Index)
                e.Graphics.FillRectangle(Brushes.White, tabRect);
            else
                e.Graphics.FillRectangle(SystemBrushes.Control, tabRect);

            string title = tabPage.Text.TrimEnd();
            if (title.Length > 15) title = title.Substring(0, 12) + "...";

            TextRenderer.DrawText(e.Graphics, title, e.Font,
                new Rectangle(tabRect.X + 5, tabRect.Y + 5, tabRect.Width - 25, tabRect.Height - 5),
                Color.Black, TextFormatFlags.Left);

            Rectangle closeRect = new Rectangle(tabRect.Right - 20, tabRect.Top + 5, 15, 15);
            e.Graphics.DrawString("×", new Font("Arial", 10, FontStyle.Bold), Brushes.Gray, closeRect);
        }

        private void TabControl_MouseClick(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControl.TabPages.Count; i++)
            {
                Rectangle tabRect = tabControl.GetTabRect(i);
                Rectangle closeRect = new Rectangle(tabRect.Right - 20, tabRect.Top + 5, 15, 15);

                if (closeRect.Contains(e.Location))
                {
                    TabPage tabPage = tabControl.TabPages[i];
                    RdpPanel panel = GetRdpPanel(tabPage);

                    if (panel != null && panel.IsConnected)
                    {
                        DialogResult result = MessageBox.Show(
                            string.Format("确定要断开 {0} 的连接吗？", panel.ConnectionName),
                            "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result != DialogResult.Yes) return;
                    }

                    CloseTab(tabPage);
                    break;
                }
            }
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTitle();
            
            // 切换标签页时，尝试让当前 RDP 面板获取焦点
            if (tabControl.SelectedTab != null)
            {
                RdpPanel panel = GetRdpPanel(tabControl.SelectedTab);
                if (panel != null)
                {
                    panel.FocusRdp();
                }
            }
        }

        // 记录正在拖拽的标签页
        private TabPage _draggedTab;

        private void TabControl_MouseDown(object sender, MouseEventArgs e)
        {
            _draggedTab = null;
            if (e.Button == MouseButtons.Left)
            {
                for (int i = 0; i < tabControl.TabPages.Count; i++)
                {
                    if (tabControl.GetTabRect(i).Contains(e.Location))
                    {
                        _draggedTab = tabControl.TabPages[i];
                        // 启动拖拽操作
                        tabControl.DoDragDrop(_draggedTab, DragDropEffects.Move);
                        break;
                    }
                }
            }
        }

        private void TabControl_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TabPage)))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void TabControl_DragDrop(object sender, DragEventArgs e)
        {
            if (_draggedTab == null) return;

            Point clientPoint = tabControl.PointToClient(new Point(e.X, e.Y));
            int targetIndex = -1;

            // 查找放置目标的索引
            for (int i = 0; i < tabControl.TabCount; i++)
            {
                if (tabControl.GetTabRect(i).Contains(clientPoint))
                {
                    targetIndex = i;
                    break;
                }
            }

            // 如果没有找到目标（例如拖到了空白处），且在标签栏范围内，则移到最后
            if (targetIndex == -1)
            {
                // 简单的判断：如果在控件范围内，就放到最后
                targetIndex = tabControl.TabCount - 1;
            }

            if (targetIndex != -1 && targetIndex != tabControl.TabPages.IndexOf(_draggedTab))
            {
                // 移动标签页
                tabControl.TabPages.Remove(_draggedTab);
                tabControl.TabPages.Insert(targetIndex, _draggedTab);
                tabControl.SelectedTab = _draggedTab;
            }
        }

        private void CloseTab(TabPage tabPage)
        {
            // 先检查是否是终端标签页
            TerminalPanel terminalPanel = GetTerminalPanel(tabPage);
            if (terminalPanel != null)
            {
                terminalPanel.Stop();
                tabControl.TabPages.Remove(tabPage);
                tabPage.Dispose();
                UpdateTitle();
                return;
            }

            // RDP 标签页
            RdpPanel panel = GetRdpPanel(tabPage);
            if (panel != null) panel.Disconnect();

            tabControl.TabPages.Remove(tabPage);
            tabPage.Dispose();
            UpdateTitle();
        }

        private RdpPanel GetRdpPanel(TabPage tabPage)
        {
            foreach (Control ctrl in tabPage.Controls)
            {
                RdpPanel panel = ctrl as RdpPanel;
                if (panel != null) return panel;
            }
            return null;
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
            this.Text = "R远程_3389管理器 - 远程桌面管理器";
        }

        #endregion

        #region 终端处理

        /// <summary>
        /// 打开终端标签页
        /// </summary>
        private void OpenTerminal(string terminalType)
        {
            try
            {
                _terminalCounter++;
                string title = terminalType.ToLower() == "cmd" ? "CMD" : "PowerShell";

                // 创建标签页
                TabPage tabPage = new TabPage();
                tabPage.Text = string.Format("{0} #{1}    ", title, _terminalCounter);
                tabPage.Tag = string.Format("terminal_{0}", _terminalCounter);

                // 创建终端面板
                TerminalPanel terminalPanel = new TerminalPanel(terminalType);
                terminalPanel.ProcessExited += TerminalPanel_ProcessExited;

                tabPage.Controls.Add(terminalPanel);
                tabControl.TabPages.Add(tabPage);
                tabControl.SelectedTab = tabPage;

                // 启动终端
                terminalPanel.Start();

                lblStatus.Text = string.Format("已打开 {0} 终端", title);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("打开终端失败: {0}", ex.Message), "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 终端进程退出
        /// </summary>
        private void TerminalPanel_ProcessExited(object sender, EventArgs e)
        {
            TerminalPanel panel = sender as TerminalPanel;
            if (panel != null)
            {
                foreach (TabPage tab in tabControl.TabPages)
                {
                    if (tab.Controls.Contains(panel))
                    {
                        this.BeginInvoke(new Action(() => {
                            CloseTerminalTab(tab);
                        }));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 关闭终端标签页
        /// </summary>
        private void CloseTerminalTab(TabPage tabPage)
        {
            TerminalPanel panel = GetTerminalPanel(tabPage);
            if (panel != null) panel.Stop();

            tabControl.TabPages.Remove(tabPage);
            tabPage.Dispose();
        }

        /// <summary>
        /// 获取标签页中的终端面板
        /// </summary>
        private TerminalPanel GetTerminalPanel(TabPage tabPage)
        {
            foreach (Control ctrl in tabPage.Controls)
            {
                TerminalPanel panel = ctrl as TerminalPanel;
                if (panel != null) return panel;
            }
            return null;
        }

        #endregion

        #region 工具栏按钮事件

        private void BtnQuickConnect_DropDownOpening(object sender, EventArgs e)
        {
            var btn = sender as ToolStripDropDownButton;
            if (btn == null) return;

            btn.DropDownItems.Clear();

            // 获取所有连接
            var connections = _dataManager.GetAllConnections();
            if (connections.Count == 0)
            {
                var noItem = new ToolStripMenuItem("(没有保存的连接)");
                noItem.Enabled = false;
                btn.DropDownItems.Add(noItem);
                return;
            }

            // 添加所有连接到下拉菜单
            foreach (var conn in connections)
            {
                var item = new ToolStripMenuItem();
                item.Text = string.Format("{0} ({1})", conn.Name, conn.ServerAddress);
                item.Tag = conn;
                item.Click += QuickConnectItem_Click;
                btn.DropDownItems.Add(item);
            }
        }

        private void QuickConnectItem_Click(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            if (item == null || item.Tag == null) return;

            var connection = item.Tag as RdpConnection;
            if (connection != null)
            {
                ConnectToRemote(connection);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            AddNewConnection();
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = treeView.SelectedNode;
            if (selectedNode != null && selectedNode.Tag is RdpConnection)
            {
                EditConnection((RdpConnection)selectedNode.Tag);
            }
            else
            {
                MessageBox.Show("请先在左侧选择要编辑的连接！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = treeView.SelectedNode;
            if (selectedNode != null && selectedNode.Tag is RdpConnection)
            {
                DeleteConnection((RdpConnection)selectedNode.Tag);
            }
            else
            {
                MessageBox.Show("请先在左侧选择要删除的连接！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion

        #region 连接管理

        private void AddNewConnection()
        {
            using (var dialog = new EditConnectionForm())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _dataManager.AddConnection(dialog.Connection);
                        LoadConnections();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format("添加连接失败: {0}", ex.Message), "错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void EditConnection(RdpConnection connection)
        {
            using (var dialog = new EditConnectionForm(connection))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _dataManager.UpdateConnection(dialog.Connection);
                        LoadConnections();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format("更新连接失败: {0}", ex.Message), "错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void DeleteConnection(RdpConnection connection)
        {
            var result = MessageBox.Show(string.Format("确定要删除连接 \"{0}\" 吗？", connection.Name),
                "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _dataManager.DeleteConnection(connection.Id);
                    LoadConnections();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("删除连接失败: {0}", ex.Message), "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion

        #region 文件夹管理

        /// <summary>
        /// 新建文件夹
        /// </summary>
        private void AddNewFolder(string parentId)
        {
            string folderName = ShowInputDialog("新建文件夹", "请输入文件夹名称:", "新文件夹");
            if (!string.IsNullOrEmpty(folderName))
            {
                try
                {
                    var folder = new ConnectionFolder
                    {
                        Name = folderName,
                        ParentId = parentId ?? string.Empty
                    };
                    _dataManager.AddFolder(folder);
                    LoadConnections();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("创建文件夹失败: {0}", ex.Message), "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 重命名文件夹
        /// </summary>
        private void RenameFolder(ConnectionFolder folder)
        {
            string newName = ShowInputDialog("重命名文件夹", "请输入新名称:", folder.Name);
            if (!string.IsNullOrEmpty(newName) && newName != folder.Name)
            {
                try
                {
                    folder.Name = newName;
                    _dataManager.UpdateFolder(folder);
                    LoadConnections();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("重命名失败: {0}", ex.Message), "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 删除文件夹
        /// </summary>
        private void DeleteFolder(ConnectionFolder folder)
        {
            var result = MessageBox.Show(
                string.Format("确定要删除文件夹 \"{0}\" 及其所有内容吗？\n\n此操作将删除文件夹内的所有连接和子文件夹！", folder.Name),
                "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _dataManager.DeleteFolder(folder.Id);
                    LoadConnections();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("删除文件夹失败: {0}", ex.Message), "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 在指定文件夹中新建连接
        /// </summary>
        private void AddNewConnectionToFolder(string folderId)
        {
            using (var dialog = new EditConnectionForm())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        dialog.Connection.FolderId = folderId;
                        _dataManager.AddConnection(dialog.Connection);
                        LoadConnections();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format("添加连接失败: {0}", ex.Message), "错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// 显示输入对话框
        /// </summary>
        private string ShowInputDialog(string title, string prompt, string defaultValue)
        {
            Form inputForm = new Form();
            inputForm.Text = title;
            inputForm.Size = new Size(350, 150);
            inputForm.StartPosition = FormStartPosition.CenterParent;
            inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            inputForm.MaximizeBox = false;
            inputForm.MinimizeBox = false;

            Label label = new Label();
            label.Text = prompt;
            label.Location = new Point(15, 15);
            label.AutoSize = true;

            TextBox textBox = new TextBox();
            textBox.Text = defaultValue;
            textBox.Location = new Point(15, 40);
            textBox.Size = new Size(300, 25);
            textBox.SelectAll();

            Button btnOK = new Button();
            btnOK.Text = "确定";
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Location = new Point(150, 75);

            Button btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(235, 75);

            inputForm.Controls.AddRange(new Control[] { label, textBox, btnOK, btnCancel });
            inputForm.AcceptButton = btnOK;
            inputForm.CancelButton = btnCancel;

            if (inputForm.ShowDialog() == DialogResult.OK)
            {
                return textBox.Text.Trim();
            }
            return null;
        }

        #endregion

        #region 全屏和分辨率

        private void BtnFullScreen_Click(object sender, EventArgs e)
        {
            ToggleFullScreen();
        }

        private void ToggleFullScreen()
        {
            if (!_isFullScreen)
            {
                // 进入全屏
                _previousWindowState = this.WindowState;
                _previousBorderStyle = this.FormBorderStyle;
                _previousLeftPanelVisible = leftPanelVisible;

                // 隐藏左侧面板
                if (leftPanelVisible)
                {
                    ToggleLeftPanel();
                }

                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Normal;
                this.WindowState = FormWindowState.Maximized;
                toolStrip.Visible = false;

                _isFullScreen = true;
                lblStatus.Text = "全屏模式 - 按 Esc 或 F11 退出";
            }
            else
            {
                // 退出全屏
                this.FormBorderStyle = _previousBorderStyle;
                this.WindowState = _previousWindowState;
                toolStrip.Visible = true;

                // 恢复左侧面板状态
                if (_previousLeftPanelVisible && !leftPanelVisible)
                {
                    ToggleLeftPanel();
                }

                _isFullScreen = false;
                lblStatus.Text = "就绪";
            }
        }

        private void BtnFitWindow_Click(object sender, EventArgs e)
        {
            TabPage currentTab = tabControl.SelectedTab;
            if (currentTab == null) return;

            RdpPanel panel = GetRdpPanel(currentTab);
            if (panel != null && panel.IsConnected)
            {
                panel.FitToWindow();
                lblStatus.Text = "已调整分辨率以适合窗口";
            }
            else
            {
                MessageBox.Show("请先连接到远程桌面！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ResolutionMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item == null) return;

            string resolution = item.Tag.ToString();
            string[] parts = resolution.Split('x');
            if (parts.Length != 2) return;

            int width = int.Parse(parts[0]);
            int height = int.Parse(parts[1]);

            TabPage currentTab = tabControl.SelectedTab;
            if (currentTab == null) return;

            RdpPanel panel = GetRdpPanel(currentTab);
            if (panel != null && panel.IsConnected)
            {
                panel.SetResolution(width, height);
                lblStatus.Text = string.Format("已设置分辨率: {0}", resolution);
            }
            else
            {
                MessageBox.Show("请先连接到远程桌面！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion

        #region 左侧面板切换

        private void BtnTogglePanel_Click(object sender, EventArgs e)
        {
            ToggleLeftPanel();
        }

        private void ToggleLeftPanel()
        {
            leftPanelVisible = !leftPanelVisible;

            if (leftPanelVisible)
            {
                _splitContainer.Panel1Collapsed = false;
                _splitContainer.SplitterDistance = leftPanelWidth;
                btnTogglePanel.Text = "隐藏列表";
            }
            else
            {
                _splitContainer.Panel1Collapsed = true;
                btnTogglePanel.Text = "显示列表";
            }
        }

        #endregion

        #region 断开连接

        private void BtnDisconnect_Click(object sender, EventArgs e)
        {
            TabPage currentTab = tabControl.SelectedTab;
            if (currentTab == null)
            {
                MessageBox.Show("没有活动的连接！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            RdpPanel panel = GetRdpPanel(currentTab);
            if (panel != null && panel.IsConnected)
            {
                DialogResult result = MessageBox.Show(
                    string.Format("确定要断开 {0} 的连接吗？", panel.ConnectionName),
                    "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    CloseTab(currentTab);
                }
            }
            else
            {
                CloseTab(currentTab);
            }
        }

        #endregion

        #region 键盘快捷键

        private void MainFormNew_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                ToggleFullScreen();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape && _isFullScreen)
            {
                ToggleFullScreen();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.W)
            {
                // Ctrl+W 关闭当前标签
                if (tabControl.SelectedTab != null)
                {
                    CloseTab(tabControl.SelectedTab);
                }
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.Tab)
            {
                // Ctrl+Tab 切换标签
                if (tabControl.TabCount > 1)
                {
                    int nextIndex = (tabControl.SelectedIndex + 1) % tabControl.TabCount;
                    tabControl.SelectedIndex = nextIndex;
                }
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.N)
            {
                // Ctrl+N 新建连接
                AddNewConnection();
                e.Handled = true;
            }
        }

        #endregion

        #region 窗口加载

        private void MainFormNew_Load(object sender, EventArgs e)
        {
            // 窗口加载后强制设置左侧面板宽度
            _splitContainer.SplitterDistance = leftPanelWidth;

            // 默认打开一个 PowerShell 终端标签页
            OpenTerminal("powershell");
        }

        #endregion

        #region 窗口关闭

        private void MainFormNew_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 检查是否有活动连接
            int activeConnections = 0;
            foreach (TabPage tab in tabControl.TabPages)
            {
                RdpPanel panel = GetRdpPanel(tab);
                if (panel != null && panel.IsConnected)
                {
                    activeConnections++;
                }
            }

            if (activeConnections > 0)
            {
                DialogResult result = MessageBox.Show(
                    string.Format("当前有 {0} 个活动连接，确定要退出吗？", activeConnections),
                    "确认退出", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

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

        #endregion
    }
}