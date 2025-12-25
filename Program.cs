using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RDPManager
{
    /// <summary>
    /// 程序入口
    /// </summary>
    static class Program
    {
        // 启用 Per-Monitor DPI 感知（Windows 10 1703+）
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(int value);

        // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2
        private const int DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;

        // 旧版 API 回退（Windows 8.1+）
        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(int awareness);

        private const int PROCESS_PER_MONITOR_DPI_AWARE = 2;

        /// <summary>
        /// 应用程序的主入口点
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 启用高 DPI 感知 - 这是避免模糊的关键！
            try
            {
                // 首先尝试 Windows 10 1703+ 的 Per-Monitor V2
                SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            }
            catch
            {
                try
                {
                    // 回退到 Windows 8.1+ 的 Per-Monitor
                    SetProcessDpiAwareness(PROCESS_PER_MONITOR_DPI_AWARE);
                }
                catch
                {
                    // 忽略旧系统的错误
                }
            }

            // 启用应用程序的视觉样式
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 运行新版主窗体（左右分栏布局）
            Application.Run(new MainFormNew());
        }
    }
}
