using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RDPManager
{
    /// <summary>
    /// 终端面板 - 嵌入真实的 CMD/PowerShell 控制台窗口
    /// </summary>
    public class TerminalPanel : Panel
    {
        private Process _process;
        private IntPtr _consoleHandle = IntPtr.Zero;
        private Timer _embedTimer;
        private int _embedAttempts = 0;
        private const int MAX_EMBED_ATTEMPTS = 100;
        private bool _isEmbedded = false;

        public event EventHandler ProcessExited;

        public string TerminalType { get; private set; }
        public bool IsRunning { get { return _process != null && !_process.HasExited; } }

        // Windows API
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        private const int GWL_STYLE = -16;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_CHILD = 0x40000000;
        private const int WS_BORDER = 0x00800000;
        private const int WS_DLGFRAME = 0x00400000;
        private const int WS_CAPTION = WS_BORDER | WS_DLGFRAME;
        private const int WS_SYSMENU = 0x00080000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_MINIMIZEBOX = 0x00020000;
        private const int WS_MAXIMIZEBOX = 0x00010000;
        private const int SW_SHOW = 5;
        private const int SW_HIDE = 0;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_NOOWNERZORDER = 0x0200;
        private const uint SWP_NOZORDER = 0x0004;

        public TerminalPanel(string terminalType = "powershell")
        {
            TerminalType = terminalType;
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.Black;
        }

        /// <summary>
        /// 启动终端
        /// </summary>
        public void Start()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();

                if (TerminalType.ToLower() == "cmd")
                {
                    psi.FileName = "cmd.exe";
                }
                else
                {
                    psi.FileName = "powershell.exe";
                    psi.Arguments = "-NoLogo";
                }

                psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Normal;

                _process = new Process();
                _process.StartInfo = psi;
                _process.EnableRaisingEvents = true;
                _process.Exited += Process_Exited;
                _process.Start();

                // 等待窗口创建后嵌入
                _embedAttempts = 0;
                _embedTimer = new Timer();
                _embedTimer.Interval = 50;
                _embedTimer.Tick += EmbedTimer_Tick;
                _embedTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("启动终端失败: {0}", ex.Message), "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EmbedTimer_Tick(object sender, EventArgs e)
        {
            _embedAttempts++;

            if (_process == null || _process.HasExited)
            {
                StopEmbedTimer();
                return;
            }

            try
            {
                _process.Refresh();
                IntPtr hwnd = _process.MainWindowHandle;

                if (hwnd != IntPtr.Zero && hwnd != _consoleHandle)
                {
                    _consoleHandle = hwnd;
                    StopEmbedTimer();
                    EmbedConsoleWindow();
                }
                else if (_embedAttempts >= MAX_EMBED_ATTEMPTS)
                {
                    StopEmbedTimer();
                }
            }
            catch
            {
                // 忽略异常，继续尝试
            }
        }

        private void StopEmbedTimer()
        {
            if (_embedTimer != null)
            {
                _embedTimer.Stop();
                _embedTimer.Dispose();
                _embedTimer = null;
            }
        }

        private void EmbedConsoleWindow()
        {
            if (_consoleHandle == IntPtr.Zero) return;

            try
            {
                // 确保窗口可用
                EnableWindow(_consoleHandle, true);

                // 移除窗口边框和标题栏，但保留可见和可交互
                int style = GetWindowLong(_consoleHandle, GWL_STYLE);
                style = style & ~WS_CAPTION & ~WS_THICKFRAME & ~WS_MINIMIZEBOX & ~WS_MAXIMIZEBOX & ~WS_SYSMENU;
                style = style | WS_VISIBLE;
                SetWindowLong(_consoleHandle, GWL_STYLE, style);

                // 设置父窗口
                SetParent(_consoleHandle, this.Handle);

                // 应用样式变更并调整大小
                SetWindowPos(_consoleHandle, IntPtr.Zero, 0, 0, this.Width, this.Height,
                    SWP_FRAMECHANGED | SWP_NOZORDER);

                // 显示并激活窗口
                ShowWindow(_consoleHandle, SW_SHOW);

                // 延迟设置焦点，确保窗口完全就绪
                Timer focusTimer = new Timer();
                focusTimer.Interval = 100;
                focusTimer.Tick += (s, args) => {
                    focusTimer.Stop();
                    focusTimer.Dispose();
                    if (_consoleHandle != IntPtr.Zero)
                    {
                        SetForegroundWindow(_consoleHandle);
                        SetFocus(_consoleHandle);
                    }
                };
                focusTimer.Start();

                _isEmbedded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("嵌入控制台失败: {0}", ex.Message), "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);

            if (_isEmbedded && _consoleHandle != IntPtr.Zero)
            {
                MoveWindow(_consoleHandle, 0, 0, this.Width, this.Height, true);
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            if (_consoleHandle != IntPtr.Zero)
            {
                SetFocus(_consoleHandle);
                SetForegroundWindow(_consoleHandle);
            }
        }

        // 当鼠标点击面板时，将焦点转移到控制台
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (_consoleHandle != IntPtr.Zero)
            {
                SetFocus(_consoleHandle);
                SetForegroundWindow(_consoleHandle);
            }
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            _isEmbedded = false;
            _consoleHandle = IntPtr.Zero;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnProcessExited()));
            }
            else
            {
                OnProcessExited();
            }
        }

        private void OnProcessExited()
        {
            if (ProcessExited != null)
            {
                ProcessExited(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 关闭终端
        /// </summary>
        public void Stop()
        {
            StopEmbedTimer();
            _isEmbedded = false;

            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill();
                }
            }
            catch
            {
                // 忽略关闭错误
            }

            _consoleHandle = IntPtr.Zero;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
                if (_process != null)
                {
                    _process.Dispose();
                    _process = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
