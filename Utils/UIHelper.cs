using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RDPManager.Utils
{
    public static class UIHelper
    {
        // === 配色方案 (现代扁平风格) ===
        public static readonly Color ColorPrimary = Color.FromArgb(0, 122, 204);     // VS Code 蓝
        public static readonly Color ColorBackground = Color.FromArgb(243, 243, 243);  // 窗体背景
        public static readonly Color ColorPanelLeft = Color.FromArgb(240, 240, 240);   // 左侧面板背景
        public static readonly Color ColorBorder = Color.FromArgb(204, 206, 219);      // 边框颜色
        public static readonly Color ColorTextMain = Color.FromArgb(30, 30, 30);       // 主要文字
        public static readonly Color ColorTextLight = Color.FromArgb(100, 100, 100);   // 次要文字/未选中文字
        
        // 标签页配色
        public static readonly Color TabActiveBg = Color.White;
        public static readonly Color TabInactiveBg = Color.FromArgb(236, 236, 236);
        public static readonly Color TabHoverBg = Color.FromArgb(245, 245, 245);

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
            public override Color ButtonPressedGradientBegin => Color.FromArgb(200, 200, 200);
            public override Color ButtonPressedGradientMiddle => Color.FromArgb(200, 200, 200);
            public override Color ButtonPressedGradientEnd => Color.FromArgb(200, 200, 200);
            public override Color ButtonPressedBorder => Color.Transparent;

            // 按钮被选中（悬停）
            public override Color ButtonSelectedGradientBegin => Color.FromArgb(220, 220, 220);
            public override Color ButtonSelectedGradientMiddle => Color.FromArgb(220, 220, 220);
            public override Color ButtonSelectedGradientEnd => Color.FromArgb(220, 220, 220);
            public override Color ButtonSelectedBorder => Color.Transparent;

            // 菜单栏背景
            public override Color MenuStripGradientBegin => ColorBackground;
            public override Color MenuStripGradientEnd => ColorBackground;
            
            // 下拉菜单边框
            public override Color MenuBorder => ColorBorder;
            
            // 下拉菜单项被选中
            public override Color MenuItemSelected => Color.FromArgb(200, 220, 240);
            public override Color MenuItemBorder => Color.Transparent;
        }

        /// <summary>
        /// 配置 TreeView 样式
        /// </summary>
        public static void StyleTreeView(TreeView tv)
        {
            tv.BackColor = ColorPanelLeft;
            tv.ForeColor = ColorTextMain;
            tv.LineColor = Color.FromArgb(160, 160, 160);
            tv.Font = MainFont;
            tv.ItemHeight = 24; // 增加行高，不那么拥挤
            tv.ShowLines = false; // 现代风格通常不显示虚线
            tv.FullRowSelect = true; // 全行选择
            tv.BorderStyle = BorderStyle.None;
        }
    }
}