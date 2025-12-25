using System;

namespace RDPManager.Models
{
    /// <summary>
    /// RDP 连接信息模型
    /// </summary>
    public class RdpConnection
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 连接名称（显示用）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 服务器地址或 IP
        /// </summary>
        public string ServerAddress { get; set; } = string.Empty;

        /// <summary>
        /// 端口号，默认 3389
        /// </summary>
        public int Port { get; set; } = 3389;

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 加密后的密码
        /// </summary>
        public string EncryptedPassword { get; set; } = string.Empty;

        /// <summary>
        /// 分辨率宽度（0 表示全屏）
        /// </summary>
        public int Width { get; set; } = 0;

        /// <summary>
        /// 分辨率高度（0 表示全屏）
        /// </summary>
        public int Height { get; set; } = 0;

        /// <summary>
        /// 颜色深度（8/16/24/32）
        /// </summary>
        public int ColorDepth { get; set; } = 32;

        /// <summary>
        /// 是否全屏
        /// </summary>
        public bool IsFullScreen { get; set; } = true;

        /// <summary>
        /// 是否自动适应分辨率（根据窗口大小自动调整）
        /// </summary>
        public bool AutoFitResolution { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastModifiedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; } = string.Empty;

        /// <summary>
        /// 所属文件夹ID（空表示根目录）
        /// </summary>
        public string FolderId { get; set; } = string.Empty;

        /// <summary>
        /// 显示用的完整地址
        /// </summary>
        public string FullAddress => $"{ServerAddress}:{Port}";
    }
}
