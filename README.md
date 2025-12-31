# R远程_3389管理器

一款简洁高效的 Windows 远程桌面 (RDP) 连接管理工具。

## 功能特性

- **多标签页管理** - 同时管理多个远程桌面连接，标签页可自由切换
- **内置终端** - 支持嵌入式 CMD 和 PowerShell 终端，启动即可使用
- **连接分组** - 支持文件夹分组管理连接，层级清晰
- **密码加密存储** - 使用 Windows DPAPI 安全加密保存密码
- **自动重连** - RDP 连接断开后自动重试（最多 3 次）
- **分辨率调整** - 支持多种分辨率预设，可适配窗口大小
- **全屏模式** - F11 快捷键快速进入/退出全屏
- **快捷键支持** - Ctrl+N 新建、Ctrl+W 关闭、Ctrl+Tab 切换标签
- **高 DPI 支持** - 完美适配高分辨率显示器

## 系统要求

- Windows 7/8/10/11
- .NET Framework 4.8
- 远程桌面服务 (RDP)

## 快速开始

### 直接运行

下载 Release 中的可执行文件，双击运行即可。

### 从源码编译

1. 确保安装 Visual Studio 2019 或更高版本
2. 克隆仓库：
   ```bash
   git clone https://github.com/rkuhua/3389-.git
   ```
3. 打开 `RDPManager.csproj`
4. 编译运行 (F5)

或使用命令行：
```bash
msbuild RDPManager.csproj /p:Configuration=Release /p:Platform=x86
```

## 使用说明

### 添加连接

1. 点击工具栏「新建」按钮或按 Ctrl+N
2. 填写服务器地址、用户名、密码
3. 点击「保存」

### 连接到远程桌面

- 双击左侧连接列表中的连接项
- 或右键点击选择「连接」

### 使用终端

- 点击工具栏「+ 终端」按钮
- 选择「命令提示符 (CMD)」或「Windows PowerShell」
- 终端会在新标签页中打开，支持完整交互

### 管理文件夹

- 右键「所有连接」或任意文件夹，选择「新建文件夹」
- 支持嵌套文件夹结构

### 自动重连

- 当 RDP 连接因网络问题断开时，程序会自动尝试重新连接
- 最多重试 3 次，每次间隔 2 秒
- 用户主动断开连接不会触发自动重连

## 快捷键

| 快捷键 | 功能 |
|--------|------|
| Ctrl+N | 新建连接 |
| Ctrl+W | 关闭当前标签 |
| Ctrl+Tab | 切换标签 |
| F11 | 全屏/退出全屏 |
| Esc | 退出全屏 |

## 项目结构

```
RDPManager/
├── Program.cs              # 程序入口
├── MainFormNew.cs          # 主窗口（左右分栏布局）
├── EditConnectionForm.cs   # 编辑连接对话框
├── RdpPanel.cs             # RDP 连接面板控件
├── TerminalPanel.cs        # 终端面板控件（嵌入 CMD/PowerShell）
├── RdpSessionForm.cs       # RDP 会话窗体
├── RdpTabForm.cs           # 标签页窗体
├── Models/
│   ├── RdpConnection.cs    # 连接信息模型
│   └── ConnectionFolder.cs # 文件夹模型
├── Utils/
│   ├── DataManager.cs      # 数据持久化管理
│   ├── EncryptionHelper.cs # 密码加密工具
│   └── RdpHelper.cs        # RDP 辅助工具
└── app.ico                 # 应用图标
```

## 技术栈

- C# / .NET Framework 4.8
- Windows Forms
- Microsoft Terminal Services ActiveX Control (mstscax.dll)
- Newtonsoft.Json
- Windows API (user32.dll) - 用于终端窗口嵌入

## 数据存储

连接配置保存在用户目录：`%APPDATA%\RDPManager\connections.json`

密码使用 Windows DPAPI 加密，仅当前用户可解密。

## 更新日志

### v1.1.1
- **UI 重构**：全新扁平化界面，默认采用 Sky Blue (MobaXterm 风格) 主题。
- **主题支持**：新增设置菜单，支持切换 Sky Blue、深色模式 (Dark Mode) 和默认浅色主题。
- **列表优化**：侧边栏样式升级，增加“用户会话”标题栏、连接线和图标支持，右键空白处可弹出菜单。
- **新增**：标签页拖拽排序功能。
- **新增**：默认启动 CMD 终端。
- **优化**：RDP 连接成功及切换标签时自动获取焦点，无需点击。
- **修复**：RDP 连接自动重试机制（自动识别并忽略被顶号导致的断开）。
- **修复**：标签页关闭按钮点击无效的问题。

### v1.1.0
- 新增：内置终端功能（支持 CMD 和 PowerShell）
- 新增：程序启动时默认打开 PowerShell 终端
- 新增：RDP 连接自动重试机制
- 优化：工具栏添加「+ 终端」按钮

### v1.0.0
- 初始版本发布
- 多标签页 RDP 连接管理
- 连接分组和密码加密

## 许可证

MIT License

## 作者

作者微信：rrror777
