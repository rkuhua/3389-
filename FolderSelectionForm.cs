using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RDPManager.Models;
using RDPManager.Utils;

namespace RDPManager
{
    public class FolderSelectionForm : Form
    {
        private TreeView treeView;
        private Button btnOk;
        private Button btnCancel;
        private ImageList imageList;

        public string SelectedFolderId { get; private set; }
        private List<ConnectionFolder> _allFolders;
        private string _excludeId; // 需要排除的文件夹 ID (自身及其子目录)

        public FolderSelectionForm(List<ConnectionFolder> folders, string excludeId = null)
        {
            _allFolders = folders;
            _excludeId = excludeId;
            InitializeComponent();
            LoadFolders();
        }

        private void InitializeComponent()
        {
            this.Text = "选择目标文件夹";
            this.Size = new Size(350, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 图标列表
            imageList = new ImageList();
            imageList.ImageSize = new Size(16, 16);
            imageList.ColorDepth = ColorDepth.Depth32Bit;
            
            // 0. 根/默认
            Bitmap bmpRoot = new Bitmap(16, 16);
            using(Graphics g = Graphics.FromImage(bmpRoot)) {
                g.FillEllipse(Brushes.Gray, 4, 4, 8, 8);
            }
            imageList.Images.Add(bmpRoot);
            
            // 1. 文件夹
            Bitmap bmpFolder = new Bitmap(16, 16);
            using(Graphics g = Graphics.FromImage(bmpFolder)) {
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 200, 80)), 2, 4, 12, 10); // 黄色文件夹
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 200, 80)), 2, 2, 6, 2);
            }
            imageList.Images.Add(bmpFolder);

            // TreeView
            treeView = new TreeView();
            treeView.Dock = DockStyle.Top;
            treeView.Height = 300;
            treeView.ImageList = imageList;
            treeView.HideSelection = false;
            treeView.Location = new Point(10, 10);
            treeView.Width = 315;
            treeView.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            // 按钮
            btnOk = new Button();
            btnOk.Text = "确定";
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(150, 320);
            btnOk.Click += BtnOk_Click;

            btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(240, 320);

            this.Controls.Add(treeView);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
            
            // 应用主题
            ApplyTheme();
        }
        
        private void ApplyTheme()
        {
            this.BackColor = UIHelper.ColorBackground;
            this.ForeColor = UIHelper.ColorTextMain;
            this.Font = UIHelper.MainFont;
            
            treeView.BackColor = UIHelper.ColorPanelLeft;
            treeView.ForeColor = UIHelper.ColorTextMain;
            treeView.LineColor = UIHelper.ColorBorder;
            
            foreach(Control c in this.Controls)
            {
                c.Font = UIHelper.MainFont;
            }
        }

        private void LoadFolders()
        {
            treeView.Nodes.Clear();

            // 根节点
            TreeNode rootNode = new TreeNode("所有连接 (根目录)");
            rootNode.Tag = "ROOT"; // 空 ID 代表根目录
            rootNode.ImageIndex = 0;
            rootNode.SelectedImageIndex = 0;
            treeView.Nodes.Add(rootNode);

            LoadSubFolders(rootNode, string.Empty);
            rootNode.Expand();
        }

        private void LoadSubFolders(TreeNode parentNode, string parentId)
        {
            foreach (var folder in _allFolders)
            {
                if (folder.ParentId == (parentId ?? string.Empty))
                {
                    // 排除自身及其子文件夹（防止循环移动）
                    if (_excludeId != null && (folder.Id == _excludeId || IsSubFolder(folder.Id, _excludeId)))
                    {
                        continue;
                    }

                    TreeNode node = new TreeNode(folder.Name);
                    node.Tag = folder;
                    node.ImageIndex = 1;
                    node.SelectedImageIndex = 1;
                    parentNode.Nodes.Add(node);

                    LoadSubFolders(node, folder.Id);
                }
            }
        }

        // 简单的递归检查
        private bool IsSubFolder(string folderId, string targetId)
        {
            // 这里只需要简单的层级判断，因为 _allFolders 是扁平列表，我们需要根据 ParentId 查找
            // 但为了简化，这里假设如果 parentId 是 targetId，那就是子文件夹
            // 实际上在 LoadSubFolders 中，我们是递归加载的，如果当前节点的祖先链中包含 _excludeId，就不加载
            // 但上面的逻辑是自顶向下的，所以只要 excludeId 不加载，它的子节点自然也不会加载
            return false; 
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (treeView.SelectedNode != null)
            {
                if (treeView.SelectedNode.Tag.ToString() == "ROOT")
                {
                    SelectedFolderId = string.Empty;
                }
                else if (treeView.SelectedNode.Tag is ConnectionFolder)
                {
                    SelectedFolderId = ((ConnectionFolder)treeView.SelectedNode.Tag).Id;
                }
            }
            else
            {
                // 默认根目录
                SelectedFolderId = string.Empty; 
            }
        }
    }
}