using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RDPManager.Models;

namespace RDPManager.Utils
{
    /// <summary>
    /// 数据存储容器
    /// </summary>
    public class DataStore
    {
        public List<RdpConnection> Connections { get; set; } = new List<RdpConnection>();
        public List<ConnectionFolder> Folders { get; set; } = new List<ConnectionFolder>();
    }

    /// <summary>
    /// 数据存储管理类
    /// 使用 JSON 文件存储连接信息和文件夹
    /// </summary>
    public class DataManager
    {
        // 配置文件保存到程序所在目录
        private static readonly string DataFilePath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
            "connections.json"
        );

        private List<RdpConnection> _connections = new List<RdpConnection>();
        private List<ConnectionFolder> _folders = new List<ConnectionFolder>();

        public DataManager()
        {
            LoadData();
        }

        #region 连接管理

        /// <summary>
        /// 获取所有连接
        /// </summary>
        public List<RdpConnection> GetAllConnections()
        {
            return _connections.OrderByDescending(c => c.LastModifiedTime).ToList();
        }

        /// <summary>
        /// 获取指定文件夹下的连接
        /// </summary>
        public List<RdpConnection> GetConnectionsByFolder(string folderId)
        {
            return _connections
                .Where(c => c.FolderId == (folderId ?? string.Empty))
                .OrderBy(c => c.Name)
                .ToList();
        }

        /// <summary>
        /// 添加连接
        /// </summary>
        public void AddConnection(RdpConnection connection)
        {
            connection.CreatedTime = DateTime.Now;
            connection.LastModifiedTime = DateTime.Now;
            _connections.Add(connection);
            SaveData();
        }

        /// <summary>
        /// 更新连接
        /// </summary>
        public void UpdateConnection(RdpConnection connection)
        {
            var existing = _connections.FirstOrDefault(c => c.Id == connection.Id);
            if (existing != null)
            {
                _connections.Remove(existing);
            }
            connection.LastModifiedTime = DateTime.Now;
            _connections.Add(connection);
            SaveData();
        }

        /// <summary>
        /// 删除连接
        /// </summary>
        public void DeleteConnection(string id)
        {
            _connections.RemoveAll(c => c.Id == id);
            SaveData();
        }

        /// <summary>
        /// 根据 ID 获取连接
        /// </summary>
        public RdpConnection GetConnectionById(string id)
        {
            return _connections.FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// 移动连接到文件夹
        /// </summary>
        public void MoveConnectionToFolder(string connectionId, string folderId)
        {
            var conn = _connections.FirstOrDefault(c => c.Id == connectionId);
            if (conn != null)
            {
                conn.FolderId = folderId ?? string.Empty;
                conn.LastModifiedTime = DateTime.Now;
                SaveData();
            }
        }

        #endregion

        #region 文件夹管理

        /// <summary>
        /// 移动文件夹到另一个文件夹
        /// </summary>
        public void MoveFolderToFolder(string folderId, string targetParentId)
        {
            var folder = _folders.FirstOrDefault(f => f.Id == folderId);
            if (folder != null)
            {
                // 检查循环引用（目标文件夹不能是当前文件夹或其子文件夹）
                if (IsSubFolder(targetParentId, folderId))
                {
                    throw new InvalidOperationException("不能将文件夹移动到其自身的子文件夹中。");
                }

                folder.ParentId = targetParentId ?? string.Empty;
                SaveData();
            }
        }

        /// <summary>
        /// 检查 folderId 是否是 potentialParentId 的子文件夹（递归）
        /// </summary>
        private bool IsSubFolder(string folderId, string potentialParentId)
        {
            if (string.IsNullOrEmpty(folderId) || string.IsNullOrEmpty(potentialParentId))
                return false;

            if (folderId == potentialParentId)
                return true;

            var folder = GetFolderById(folderId);
            if (folder != null && !string.IsNullOrEmpty(folder.ParentId))
            {
                return IsSubFolder(folder.ParentId, potentialParentId);
            }

            return false;
        }

        /// <summary>
        /// 获取所有文件夹
        /// </summary>
        public List<ConnectionFolder> GetAllFolders()
        {
            return _folders.OrderBy(f => f.SortOrder).ThenBy(f => f.Name).ToList();
        }

        /// <summary>
        /// 获取子文件夹
        /// </summary>
        public List<ConnectionFolder> GetSubFolders(string parentId)
        {
            return _folders
                .Where(f => f.ParentId == (parentId ?? string.Empty))
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.Name)
                .ToList();
        }

        /// <summary>
        /// 添加文件夹
        /// </summary>
        public void AddFolder(ConnectionFolder folder)
        {
            folder.CreatedTime = DateTime.Now;
            _folders.Add(folder);
            SaveData();
        }

        /// <summary>
        /// 更新文件夹
        /// </summary>
        public void UpdateFolder(ConnectionFolder folder)
        {
            var existing = _folders.FirstOrDefault(f => f.Id == folder.Id);
            if (existing != null)
            {
                existing.Name = folder.Name;
                existing.ParentId = folder.ParentId;
                existing.SortOrder = folder.SortOrder;
                existing.IsExpanded = folder.IsExpanded;
                SaveData();
            }
        }

        /// <summary>
        /// 删除文件夹（同时删除子文件夹和连接）
        /// </summary>
        public void DeleteFolder(string folderId)
        {
            // 递归删除子文件夹
            var subFolders = _folders.Where(f => f.ParentId == folderId).ToList();
            foreach (var sub in subFolders)
            {
                DeleteFolder(sub.Id);
            }

            // 删除文件夹内的连接
            _connections.RemoveAll(c => c.FolderId == folderId);

            // 删除文件夹本身
            _folders.RemoveAll(f => f.Id == folderId);

            SaveData();
        }

        /// <summary>
        /// 根据 ID 获取文件夹
        /// </summary>
        public ConnectionFolder GetFolderById(string id)
        {
            return _folders.FirstOrDefault(f => f.Id == id);
        }

        #endregion

        #region 数据持久化

        /// <summary>
        /// 加载数据
        /// </summary>
        private void LoadData()
        {
            try
            {
                if (File.Exists(DataFilePath))
                {
                    string json = File.ReadAllText(DataFilePath);

                    // 尝试读取新格式
                    try
                    {
                        var store = JsonConvert.DeserializeObject<DataStore>(json);
                        if (store != null)
                        {
                            _connections = store.Connections ?? new List<RdpConnection>();
                            _folders = store.Folders ?? new List<ConnectionFolder>();
                            return;
                        }
                    }
                    catch
                    {
                        // 可能是旧格式，尝试读取为连接列表
                    }

                    // 兼容旧格式（只有连接列表）
                    var oldResult = JsonConvert.DeserializeObject<List<RdpConnection>>(json);
                    _connections = oldResult ?? new List<RdpConnection>();
                    _folders = new List<ConnectionFolder>();
                }
            }
            catch
            {
                _connections = new List<RdpConnection>();
                _folders = new List<ConnectionFolder>();
            }
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        private void SaveData()
        {
            try
            {
                string directory = Path.GetDirectoryName(DataFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var store = new DataStore
                {
                    Connections = _connections,
                    Folders = _folders
                };

                string json = JsonConvert.SerializeObject(store, Formatting.Indented);
                File.WriteAllText(DataFilePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("保存数据失败: {0}", ex.Message));
            }
        }

        #endregion
    }
}
