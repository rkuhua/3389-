using System;

namespace RDPManager.Models
{
    /// <summary>
    /// 连接文件夹模型
    /// </summary>
    public class ConnectionFolder
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 文件夹名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 父文件夹ID（空表示根目录）
        /// </summary>
        public string ParentId { get; set; } = string.Empty;

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否展开（UI状态）
        /// </summary>
        public bool IsExpanded { get; set; } = true;
    }
}
