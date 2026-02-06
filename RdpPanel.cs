using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AxMSTSCLib;
using MSTSCLib;
using RDPManager.Models;

namespace RDPManager
{
    /// <summary>
    /// RDP 连接面板 - 用于嵌入到标签页中
    /// </summary>
    public class RdpPanel : Panel
    {
        private AxMsRdpClient9NotSafeForScripting rdpClient;
        private readonly RdpConnection _connection;
        private readonly string _password;

        // 自动重试相关
        private int _retryCount = 0;
        private const int MAX_RETRY_COUNT = 3;
        private const int RETRY_DELAY_MS = 2000;
        private Timer _retryTimer;
        private bool _isRetrying = false;
        private bool _manualDisconnect = false;

        // 用于延迟显示消息的队列（避免在不安全的时机弹窗导致卡死）
        private string _pendingMessage = null;
        private string _pendingTitle = null;
        private Timer _messageTimer;

        public event EventHandler<string> StatusChanged;
        public event EventHandler ConnectionClosed;
        public event EventHandler<int> Disconnected;

        public string ConnectionName { get { return _connection.Name; } }
        public bool IsConnected
        {
            get
            {
                try
                {
                    return rdpClient != null && !rdpClient.IsDisposed && rdpClient.Connected == 1;
                }
                catch
                {
                    return false;
                }
            }
        }

        public RdpPanel(RdpConnection connection, string password)
        {
            _connection = connection;
            _password = password;
            this.Dock = DockStyle.Fill;
        }

        public void Connect()
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

                // 设置密码
                if (!string.IsNullOrEmpty(_password))
                {
                    rdpClient.AdvancedSettings9.ClearTextPassword = _password;
                }

                // 禁用凭据提示 - 安全设置
                rdpClient.AdvancedSettings9.EnableCredSspSupport = true;
                rdpClient.AdvancedSettings9.AuthenticationLevel = 2; // 如果认证失败则不连接
                rdpClient.AdvancedSettings9.NegotiateSecurityLayer = true;

                // 网络级别身份验证 (NLA)
                try
                {
                    // 尝试设置 NLA（某些版本可能不支持）
                    ((IMsRdpClientNonScriptable3)rdpClient.GetOcx()).EnableCredSspSupport = true;
                }
                catch { /* 忽略不支持的版本 */ }

                // 分辨率设置
                int desktopWidth, desktopHeight;

                // 获取主屏幕的真实物理分辨率
                int physicalWidth = GetPhysicalScreenWidth();
                int physicalHeight = GetPhysicalScreenHeight();

                if (_connection.AutoFitResolution)
                {
                    // 自动适应：使用控件当前大小（如果有效）或屏幕分辨率
                    if (this.Width > 100 && this.Height > 100)
                    {
                        desktopWidth = this.Width;
                        desktopHeight = this.Height;
                    }
                    else
                    {
                        desktopWidth = physicalWidth > 0 ? physicalWidth : 1920;
                        desktopHeight = physicalHeight > 0 ? physicalHeight : 1080;
                    }
                }
                else if (_connection.IsFullScreen)
                {
                    // 全屏模式
                    desktopWidth = physicalWidth > 0 ? physicalWidth : 1920;
                    desktopHeight = physicalHeight > 0 ? physicalHeight : 1080;
                }
                else
                {
                    // 自定义分辨率
                    desktopWidth = _connection.Width > 0 ? _connection.Width : 1920;
                    desktopHeight = _connection.Height > 0 ? _connection.Height : 1080;
                }

                // 确保分辨率在合理范围内（RDP 支持的最大分辨率）
                desktopWidth = Math.Max(800, Math.Min(desktopWidth, 4096));
                desktopHeight = Math.Max(600, Math.Min(desktopHeight, 2160));

                // 确保分辨率是偶数（某些显卡要求）
                desktopWidth = desktopWidth - (desktopWidth % 2);
                desktopHeight = desktopHeight - (desktopHeight % 2);

                rdpClient.DesktopWidth = desktopWidth;
                rdpClient.DesktopHeight = desktopHeight;

                // 颜色深度
                rdpClient.ColorDepth = _connection.ColorDepth;

                // 性能和显示优化
                // 禁用RDP客户端自带的自动重连，使用自定义重连逻辑
                rdpClient.AdvancedSettings9.EnableAutoReconnect = false;
                rdpClient.AdvancedSettings9.MaxReconnectAttempts = 0;
                rdpClient.AdvancedSettings9.Compress = 1;
                rdpClient.AdvancedSettings9.BitmapPeristence = 1;

                // 启用智能缩放 - 让远程桌面自适应控件大小
                // 由于我们设置的分辨率与窗口大小匹配，所以不会模糊
                rdpClient.AdvancedSettings9.SmartSizing = true;

                // 提高显示质量的设置
                rdpClient.AdvancedSettings9.PerformanceFlags = 0; // 启用所有视觉效果
                rdpClient.AdvancedSettings9.RedirectDrives = false;
                rdpClient.AdvancedSettings9.RedirectPrinters = false;
                rdpClient.AdvancedSettings9.RedirectPorts = false;
                rdpClient.AdvancedSettings9.RedirectSmartCards = false;

                // 提高图像质量
                rdpClient.AdvancedSettings9.DisableCtrlAltDel = 1;
                rdpClient.AdvancedSettings9.EnableWindowsKey = 1;
                // 将 Windows 组合键发送到远程计算机
                if (rdpClient.SecuredSettings2 != null)
                {
                    rdpClient.SecuredSettings2.KeyboardHookMode = 1;
                }
                rdpClient.AdvancedSettings9.GrabFocusOnConnect = true;

                // 显示连接栏（全屏时）
                rdpClient.AdvancedSettings9.DisplayConnectionBar = true;
                rdpClient.AdvancedSettings9.PinConnectionBar = false;

                // 连接事件
                rdpClient.OnConnected += RdpClient_OnConnected;
                rdpClient.OnDisconnected += RdpClient_OnDisconnected;
                rdpClient.OnFatalError += RdpClient_OnFatalError;
                rdpClient.OnLoginComplete += RdpClient_OnLoginComplete;

                // 开始连接
                OnStatusChanged("正在连接...");
                rdpClient.Connect();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("初始化 RDP 连接失败: {0}", ex.Message), "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                OnConnectionClosed();
            }
        }

        public void Disconnect()
        {
            _manualDisconnect = true;
            StopRetryTimer();

            try
            {
                if (rdpClient != null && rdpClient.Connected == 1)
                {
                    rdpClient.Disconnect();
                }
            }
            catch
            {
                // 忽略断开连接时的错误
            }
        }

        /// <summary>
        /// 停止重试定时器
        /// </summary>
        private void StopRetryTimer()
        {
            if (_retryTimer != null)
            {
                _retryTimer.Stop();
                _retryTimer.Dispose();
                _retryTimer = null;
            }
            _isRetrying = false;
        }

        /// <summary>
        /// 适应窗口大小
        /// </summary>
        public void FitToWindow()
        {
            if (this.Width > 0 && this.Height > 0)
            {
                ChangeResolution(this.Width, this.Height);
            }
        }

        /// <summary>
        /// 设置分辨率
        /// </summary>
        public void SetResolution(int width, int height)
        {
            ChangeResolution(width, height);
        }

        /// <summary>
        /// 动态调整远程桌面分辨率
        /// 注意：RDP客户端不支持运行时直接调整分辨率，需要重新创建控件并连接
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>是否成功</returns>
        public bool ChangeResolution(int width, int height)
        {
            try
            {
                if (rdpClient == null || rdpClient.Connected != 1)
                {
                    return false;
                }

                OnStatusChanged("正在调整分辨率...");

                // 断开当前连接
                rdpClient.Disconnect();

                // 等待断开完成
                System.Threading.Thread.Sleep(300);

                // 移除旧控件
                this.Controls.Remove(rdpClient);
                rdpClient.Dispose();
                rdpClient = null;

                // 创建新的 RDP 控件
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

                if (!string.IsNullOrEmpty(_password))
                {
                    rdpClient.AdvancedSettings9.ClearTextPassword = _password;
                }

                rdpClient.AdvancedSettings9.EnableCredSspSupport = true;
                rdpClient.AdvancedSettings9.AuthenticationLevel = 0;
                rdpClient.AdvancedSettings9.NegotiateSecurityLayer = true;

                // 使用新的分辨率
                rdpClient.DesktopWidth = width;
                rdpClient.DesktopHeight = height;
                rdpClient.ColorDepth = _connection.ColorDepth;

                // 性能设置
                // 禁用RDP客户端自带的自动重连
                rdpClient.AdvancedSettings9.EnableAutoReconnect = false;
                rdpClient.AdvancedSettings9.MaxReconnectAttempts = 0;
                rdpClient.AdvancedSettings9.Compress = 1;
                rdpClient.AdvancedSettings9.BitmapPeristence = 1;
                rdpClient.AdvancedSettings9.SmartSizing = true; // 启用智能缩放
                rdpClient.AdvancedSettings9.PerformanceFlags = 0;
                rdpClient.AdvancedSettings9.RedirectDrives = false;
                rdpClient.AdvancedSettings9.RedirectPrinters = false;
                rdpClient.AdvancedSettings9.RedirectPorts = false;
                rdpClient.AdvancedSettings9.RedirectSmartCards = false;
                rdpClient.AdvancedSettings9.DisableCtrlAltDel = 1;
                rdpClient.AdvancedSettings9.EnableWindowsKey = 1;
                // 将 Windows 组合键发送到远程计算机
                if (rdpClient.SecuredSettings2 != null)
                {
                    rdpClient.SecuredSettings2.KeyboardHookMode = 1;
                }
                rdpClient.AdvancedSettings9.GrabFocusOnConnect = true;
                rdpClient.AdvancedSettings9.DisplayConnectionBar = true;
                rdpClient.AdvancedSettings9.PinConnectionBar = false;

                // 重新绑定事件
                rdpClient.OnConnected += RdpClient_OnConnected;
                rdpClient.OnDisconnected += RdpClient_OnDisconnected;
                rdpClient.OnFatalError += RdpClient_OnFatalError;
                rdpClient.OnLoginComplete += RdpClient_OnLoginComplete;

                // 重新连接
                rdpClient.Connect();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("调整分辨率失败: {0}", ex.Message), "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void RdpClient_OnConnected(object sender, EventArgs e)
        {
            // 连接成功，重置重试计数
            _retryCount = 0;
            _isRetrying = false;
            OnStatusChanged("已连接");
            
            // 连接成功后强制获取焦点
            this.BeginInvoke(new Action(() => {
                FocusRdp();
            }));
        }

        /// <summary>
        /// 强制 RDP 控件获取焦点
        /// </summary>
        public void FocusRdp()
        {
            if (this.IsConnected)
            {
                try
                {
                    rdpClient.Focus();
                }
                catch { }
            }
        }

        private void RdpClient_OnLoginComplete(object sender, EventArgs e)
        {
            OnStatusChanged("登录成功");
        }

        private void RdpClient_OnDisconnected(object sender, IMsTscAxEvents_OnDisconnectedEvent e)
        {
            int discReason = e.discReason;

            // 如果是手动断开或正在调整分辨率，不触发事件
            if (_manualDisconnect)
            {
                if (Disconnected != null)
                {
                    Disconnected(this, discReason);
                }
                OnConnectionClosed();
                return;
            }

            // 检查是否被其他地方挤掉（discReason 5 = 被其他会话替换）
            // 扩展断开码也需要检查，高16位包含扩展信息
            int extendedReason = (discReason >> 16) & 0xFFFF;
            bool isKickedByOther = (discReason == 5) ||
                                   (extendedReason == 5) ||
                                   (discReason == 3); // 3 = 服务器断开（管理员踢出）

            if (isKickedByOther)
            {
                // 被挤掉，弹窗提示，不重连
                OnStatusChanged("连接已被其他会话替换");

                // 使用安全的消息显示方法，避免在控件状态不稳定时弹窗导致卡死
                ShowMessageSafe(
                    "您的远程桌面连接已被其他位置登录挤掉。\n\n连接已断开，请重新连接。",
                    "连接已断开");

                if (Disconnected != null)
                {
                    Disconnected(this, discReason);
                }
                OnConnectionClosed();
                return;
            }

            // 判断是否应该自动重试
            // discReason 1, 2 是正常断开
            // 其他错误码可能是临时性问题，可以重试
            bool shouldRetry = ShouldAutoRetry(discReason);

            if (shouldRetry && _retryCount < MAX_RETRY_COUNT)
            {
                _retryCount++;
                _isRetrying = true;
                OnStatusChanged(string.Format("连接断开，{0}秒后自动重试 ({1}/{2})...",
                    RETRY_DELAY_MS / 1000, _retryCount, MAX_RETRY_COUNT));

                // 启动重试定时器
                _retryTimer = new Timer();
                _retryTimer.Interval = RETRY_DELAY_MS;
                _retryTimer.Tick += RetryTimer_Tick;
                _retryTimer.Start();
            }
            else
            {
                // 不重试或已达到最大重试次数
                if (Disconnected != null)
                {
                    Disconnected(this, discReason);
                }
                OnConnectionClosed();
            }
        }

        /// <summary>
        /// 判断是否应该自动重试
        /// </summary>
        private bool ShouldAutoRetry(int discReason)
        {
            // 不需要重试的断开原因：
            // 1 = 用户发起的断开
            // 2 = 用户发起的断开（管理员）
            // 3 = 服务器发起的断开
            // 5 = 连接被替换 (Connection replaced) - 被其他位置登录挤掉
            if (discReason == 1 || discReason == 2 || discReason == 5)
            {
                return false;
            }

            // 可以重试的常见临时性错误：
            // 264 = 连接超时
            // 516 = 网络问题
            // 520 = 网络问题
            // 776 = 远程计算机繁忙
            // 2308 = 套接字关闭
            // 2825 = 许可证问题（临时）
            // 其他网络相关错误也可以重试
            return true;
        }

        /// <summary>
        /// 重试定时器
        /// </summary>
        private void RetryTimer_Tick(object sender, EventArgs e)
        {
            StopRetryTimer();

            if (_manualDisconnect)
            {
                return;
            }

            OnStatusChanged(string.Format("正在重试连接 ({0}/{1})...", _retryCount, MAX_RETRY_COUNT));

            try
            {
                // 清理旧控件
                if (rdpClient != null)
                {
                    this.Controls.Remove(rdpClient);
                    rdpClient.Dispose();
                    rdpClient = null;
                }

                // 重新连接
                Connect();
            }
            catch (Exception ex)
            {
                OnStatusChanged(string.Format("重试失败: {0}", ex.Message));
                if (_retryCount >= MAX_RETRY_COUNT)
                {
                    OnConnectionClosed();
                }
            }
        }

        private void RdpClient_OnFatalError(object sender, IMsTscAxEvents_OnFatalErrorEvent e)
        {
            // 使用安全的消息显示方法，避免在控件状态不稳定时弹窗导致卡死
            ShowMessageSafe(
                string.Format("发生致命错误\n错误代码: {0}", e.errorCode),
                "错误");
            OnConnectionClosed();
        }

        private void OnStatusChanged(string status)
        {
            if (StatusChanged != null)
            {
                StatusChanged(this, status);
            }
        }

        private void OnConnectionClosed()
        {
            if (ConnectionClosed != null)
            {
                ConnectionClosed(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 安全地显示消息（延迟显示，避免在不安全的时机弹窗导致卡死）
        /// 当远程桌面进入锁屏/密码界面时，RDP控件状态可能不稳定，
        /// 直接弹窗会导致UI线程卡死
        /// </summary>
        private void ShowMessageSafe(string message, string title, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            // 如果控件已释放，直接返回
            if (this.IsDisposed || !this.IsHandleCreated)
            {
                return;
            }

            try
            {
                // 存储消息，使用定时器延迟显示
                _pendingMessage = message;
                _pendingTitle = title;

                // 停止之前的消息定时器
                if (_messageTimer != null)
                {
                    _messageTimer.Stop();
                    _messageTimer.Dispose();
                }

                // 创建新的定时器，延迟100ms显示消息
                // 这样可以让RDP控件的断开事件完全处理完毕
                _messageTimer = new Timer();
                _messageTimer.Interval = 100;
                _messageTimer.Tick += MessageTimer_Tick;
                _messageTimer.Start();
            }
            catch
            {
                // 忽略异常，避免因为弹窗导致程序崩溃
            }
        }

        /// <summary>
        /// 消息定时器回调 - 安全地显示待处理的消息
        /// </summary>
        private void MessageTimer_Tick(object sender, EventArgs e)
        {
            // 停止定时器
            if (_messageTimer != null)
            {
                _messageTimer.Stop();
                _messageTimer.Dispose();
                _messageTimer = null;
            }

            // 检查控件状态
            if (this.IsDisposed || !this.IsHandleCreated)
            {
                return;
            }

            // 获取并清空待显示的消息
            string message = _pendingMessage;
            string title = _pendingTitle;
            _pendingMessage = null;
            _pendingTitle = null;

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            try
            {
                // 获取顶层窗体作为弹窗的父窗口
                Form parentForm = this.FindForm();
                if (parentForm != null && !parentForm.IsDisposed && parentForm.IsHandleCreated)
                {
                    // 使用 BeginInvoke 确保在 UI 线程上显示，并且是非阻塞的
                    if (!parentForm.IsDisposed && parentForm.IsHandleCreated)
                    {
                        parentForm.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                // 再次检查窗体状态
                                if (!parentForm.IsDisposed)
                                {
                                    MessageBox.Show(parentForm, message, title,
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                            catch
                            {
                                // 忽略弹窗时的任何异常
                            }
                        }));
                    }
                }
            }
            catch
            {
                // 忽略异常
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 清理消息定时器
                if (_messageTimer != null)
                {
                    _messageTimer.Stop();
                    _messageTimer.Dispose();
                    _messageTimer = null;
                }

                Disconnect();
                if (rdpClient != null)
                {
                    rdpClient.Dispose();
                    rdpClient = null;
                }
            }
            base.Dispose(disposing);
        }

        #region 获取物理分辨率（绕过DPI缩放）

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        private const int DESKTOPHORZRES = 118; // 物理宽度
        private const int DESKTOPVERTRES = 117; // 物理高度

        /// <summary>
        /// 获取屏幕的真实物理宽度（不受DPI缩放影响）
        /// </summary>
        private int GetPhysicalScreenWidth()
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            int width = GetDeviceCaps(hdc, DESKTOPHORZRES);
            ReleaseDC(IntPtr.Zero, hdc);
            return width > 0 ? width : Screen.PrimaryScreen.Bounds.Width;
        }

        /// <summary>
        /// 获取屏幕的真实物理高度（不受DPI缩放影响）
        /// </summary>
        private int GetPhysicalScreenHeight()
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            int height = GetDeviceCaps(hdc, DESKTOPVERTRES);
            ReleaseDC(IntPtr.Zero, hdc);
            return height > 0 ? height : Screen.PrimaryScreen.Bounds.Height;
        }

        #endregion
    }
}
