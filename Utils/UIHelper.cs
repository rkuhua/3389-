using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RDPManager.Utils
{
    public static class UIHelper
    {
        // === 配色方案 ===
        // 去掉 readonly 以支持动态修改
        public static Color ColorPrimary = Color.FromArgb(0, 162, 232);
        public static Color ColorBackground = Color.FromArgb(245, 250, 255);
        public static Color ColorPanelLeft = Color.FromArgb(230, 242, 255);
        public static Color ColorBorder = Color.FromArgb(180, 210, 240);
        public static Color ColorTextMain = Color.FromArgb(30, 30, 30);
        public static Color ColorTextLight = Color.FromArgb(80, 80, 80);
        
        // 标签页配色
        public static Color TabActiveBg = Color.White;
        public static Color TabInactiveBg = Color.FromArgb(235, 245, 255);
        public static Color TabHoverBg = Color.FromArgb(220, 240, 255);

        // 当前主题名称
        public static string CurrentTheme = "Sky Blue";

        /// <summary>
        /// 切换主题
        /// </summary>
        public static void SetTheme(string themeName)
        {
            CurrentTheme = themeName;
            
            if (themeName == "Dark Mode")
            {
                // VS Code Dark
                ColorPrimary = Color.FromArgb(0, 122, 204);
                ColorBackground = Color.FromArgb(30, 30, 30);
                ColorPanelLeft = Color.FromArgb(37, 37, 38);
                ColorBorder = Color.FromArgb(45, 45, 45);
                ColorTextMain = Color.FromArgb(204, 204, 204);
                ColorTextLight = Color.FromArgb(150, 150, 150);
                TabActiveBg = Color.FromArgb(30, 30, 30);
                TabInactiveBg = Color.FromArgb(45, 45, 45);
                TabHoverBg = Color.FromArgb(50, 50, 50);
            }
            else if (themeName == "Sky Blue") // MobaXterm Style
            {
                // MobaXterm 风格
                ColorPrimary = Color.FromArgb(0, 90, 160); // 深蓝选中
                ColorBackground = Color.FromArgb(248, 249, 250); // 右侧极淡灰
                ColorPanelLeft = Color.White; // 左侧纯白
                ColorBorder = Color.FromArgb(210, 210, 210); // 柔和边框
                ColorTextMain = Color.Black;
                ColorTextLight = Color.FromArgb(80, 80, 80);
                TabActiveBg = Color.FromArgb(248, 249, 250); // 选中标签与右侧背景一致
                TabInactiveBg = Color.FromArgb(230, 235, 240);
                TabHoverBg = Color.FromArgb(220, 230, 240);
            }
            else // Default (Light)
            {
                // Classic Light / Minimal
                ColorPrimary = Color.FromArgb(0, 120, 215);
                ColorBackground = Color.FromArgb(243, 243, 243);
                ColorPanelLeft = Color.FromArgb(240, 240, 240);
                ColorBorder = Color.FromArgb(200, 200, 200);
                ColorTextMain = Color.Black;
                ColorTextLight = Color.FromArgb(80, 80, 80);
                TabActiveBg = Color.White;
                TabInactiveBg = Color.FromArgb(230, 230, 230);
                TabHoverBg = Color.FromArgb(220, 220, 220);
            }
        }

        // 字体
        public static Font FontAwesome; // 如果有图标字体的话
        public static readonly Font MainFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
        public static readonly Font BoldFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);

        /// <summary>
        /// 自定义 ToolStrip 渲染器（去处渐变，扁平化）
        /// </summary>
        public class ModernToolStripRenderer : ToolStripProfessionalRenderer
        {
            public ModernToolStripRenderer() : base(new ModernColorTable()) { }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                // 不绘制默认边框，或者只绘制底边
                e.Graphics.FillRectangle(new SolidBrush(ColorBackground), e.ConnectedArea);
                e.Graphics.DrawLine(new Pen(ColorBorder), 0, e.ToolStrip.Height - 1, e.ToolStrip.Width, e.ToolStrip.Height - 1);
            }
            
            // 强制文字颜色
            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = ColorTextMain;
                base.OnRenderItemText(e);
            }
            
            // 绘制箭头颜色
            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                e.ArrowColor = ColorTextMain;
                base.OnRenderArrow(e);
            }
        }

        /// <summary>
        /// 自定义颜色表
        /// </summary>
        public class ModernColorTable : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin => ColorBackground;
            public override Color ToolStripGradientMiddle => ColorBackground;
            public override Color ToolStripGradientEnd => ColorBackground;
            
            // 按钮被按下
            public override Color ButtonPressedGradientBegin => Color.FromArgb(200, 230, 255);
            public override Color ButtonPressedGradientMiddle => Color.FromArgb(200, 230, 255);
            public override Color ButtonPressedGradientEnd => Color.FromArgb(200, 230, 255);
            public override Color ButtonPressedBorder => ColorBorder;

            // 按钮被选中（悬停）
            public override Color ButtonSelectedGradientBegin => Color.FromArgb(225, 245, 255);
            public override Color ButtonSelectedGradientMiddle => Color.FromArgb(225, 245, 255);
            public override Color ButtonSelectedGradientEnd => Color.FromArgb(225, 245, 255);
            public override Color ButtonSelectedBorder => ColorBorder;

            // 菜单栏背景
            public override Color MenuStripGradientBegin => Color.White;
            public override Color MenuStripGradientEnd => Color.White;
            public override Color ToolStripDropDownBackground => Color.White; // 下拉菜单背景 (白色更干净)
            public override Color ImageMarginGradientBegin => Color.White;
            public override Color ImageMarginGradientMiddle => Color.White;
            public override Color ImageMarginGradientEnd => Color.White;

            // 下拉菜单边框
            public override Color MenuBorder => ColorBorder;
            
            // 下拉菜单项被选中
            public override Color MenuItemSelected => Color.FromArgb(220, 240, 255); // 浅蓝选中
            public override Color MenuItemBorder => Color.Transparent;
            
            // 字体颜色需要在 Renderer 中处理，ColorTable 只能处理背景
        }

        /// <summary>
        /// 配置 TreeView 样式
        /// </summary>
        public static void StyleTreeView(TreeView tv)
        {
            tv.BackColor = ColorPanelLeft;
            tv.ForeColor = ColorTextMain;
            tv.LineColor = Color.FromArgb(180, 180, 180);
            tv.Font = MainFont;
            tv.ItemHeight = 22; // MobaXterm 比较紧凑，稍微减小行高
            tv.ShowLines = true; // 显示连接线
            tv.ShowPlusMinus = true;
            tv.ShowRootLines = true;
            tv.FullRowSelect = false; // MobaXterm 不是全行高亮，只有文字高亮
            tv.BorderStyle = BorderStyle.None;
        }
    }
}