using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using RDPManager.Models;

namespace RDPManager.Utils
{
    /// <summary>
    /// RDP 连接工具类
    /// 生成 .rdp 文件并启动 mstsc.exe
    /// </summary>
    public static class RdpHelper
    {
        /// <summary>
        /// 连接到远程桌面
        /// </summary>
        public static void Connect(RdpConnection connection, string password)
        {
            try
            {
                // 生成临时 RDP 文件
                string rdpFilePath = GenerateRdpFile(connection, password);

                // 启动 mstsc.exe
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "mstsc.exe",
                    Arguments = $"\"{rdpFilePath}\"",
                    UseShellExecute = true
                };

                Process.Start(startInfo);

                // 延迟删除临时文件（5秒后）
                DeleteFileAfterDelay(rdpFilePath, 5000);
            }
            catch (Exception ex)
            {
                throw new Exception($"连接失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成 RDP 配置文件
        /// </summary>
        private static string GenerateRdpFile(RdpConnection connection, string password)
        {
            string tempPath = Path.GetTempPath();
            string rdpFileName = $"rdp_{Guid.NewGuid()}.rdp";
            string rdpFilePath = Path.Combine(tempPath, rdpFileName);

            StringBuilder rdpContent = new StringBuilder();

            // 基本连接信息
            rdpContent.AppendLine($"full address:s:{connection.ServerAddress}:{connection.Port}");
            rdpContent.AppendLine($"username:s:{connection.Username}");

            // 分辨率设置
            if (connection.IsFullScreen)
            {
                rdpContent.AppendLine("screen mode id:i:2"); // 全屏模式
            }
            else
            {
                rdpContent.AppendLine("screen mode id:i:1"); // 窗口模式
                rdpContent.AppendLine($"desktopwidth:i:{connection.Width}");
                rdpContent.AppendLine($"desktopheight:i:{connection.Height}");
            }

            // 颜色深度
            rdpContent.AppendLine($"session bpp:i:{connection.ColorDepth}");

            // 简化的常用设置
            rdpContent.AppendLine("compression:i:1");
            rdpContent.AppendLine("keyboardhook:i:2");
            rdpContent.AppendLine("audiocapturemode:i:0");
            rdpContent.AppendLine("videoplaybackmode:i:1");
            rdpContent.AppendLine("displayconnectionbar:i:1");
            rdpContent.AppendLine("autoreconnection enabled:i:1");
            rdpContent.AppendLine("authentication level:i:0");
            rdpContent.AppendLine("prompt for credentials:i:1");
            rdpContent.AppendLine("negotiate security layer:i:1");

            // 写入文件 - 使用 ANSI 编码（最兼容）
            File.WriteAllText(rdpFilePath, rdpContent.ToString(), Encoding.Default);

            return rdpFilePath;
        }

        /// <summary>
        /// 加密密码供 RDP 使用
        /// 注意：这是简化版本，Windows RDP 使用 DPAPI 加密
        /// </summary>
        private static string EncryptPasswordForRdp(string password)
        {
            try
            {
                // 使用 Windows DPAPI 加密
                byte[] passwordBytes = Encoding.Unicode.GetBytes(password);
                byte[] encryptedBytes = System.Security.Cryptography.ProtectedData.Protect(
                    passwordBytes,
                    null,
                    System.Security.Cryptography.DataProtectionScope.CurrentUser
                );
                return Convert.ToBase64String(encryptedBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 延迟删除文件
        /// </summary>
        private static async void DeleteFileAfterDelay(string filePath, int delayMilliseconds)
        {
            await System.Threading.Tasks.Task.Delay(delayMilliseconds);
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // 忽略删除失败
            }
        }
    }
}
