# 电竞教父 修改器

电竞教父 MelonLoader Mod — 游戏内修改面板，按 F1 打开。

---

## 安装方法

### 1. 安装 MelonLoader

下载 MelonLoader.Installer：

[MelonLoader.Installer.exe 直接下载](https://github.com/LavaGang/MelonLoader.Installer/releases/download/4.3.0/MelonLoader.Installer.exe)

> 或用浏览器打开 [Release 页面](https://github.com/LavaGang/MelonLoader.Installer/releases/tag/4.3.0) 自行选择版本。

打开 MelonLoader.Installer，按以下步骤操作：

1. 在列表中找到 **Esport Godfather**（电竞教父）
2. **Install v0.7.2**（越新越好，但看体质；开发环境为 v0.7.2）
3. 选择 **MonoBleedingEdge** 版本（游戏使用 Mono，非 IL2CPP）
4. 安装完成后，**运行一次游戏**，务必进入游戏主界面再退出
5. 游戏根目录出现 `MelonLoader/` 和 `Mods/` 文件夹即安装成功

### 2. 安装 Mod

将 `DebugMod.dll` 放入游戏根目录的 `Mods/` 文件夹。

### 3. 启动

启动游戏，进入主界面后按下 **F1** 打开修改面板。

---

## Steam 游戏根目录快速定位

Steam 库 → 右键 **Esport Godfather** → 管理 → 浏览本地文件。

---

## 功能面板

| Tab | 折叠区 | 功能 |
|-----|--------|------|
| **俱乐部** | (直接显示) | 教练点/训练点/预算 读取与设置、粉丝数显示 |
| | 勋章系统 | 已解锁勋章列表、按名称搜索解锁、随机解锁、所有勋章分页浏览与一键解锁 |
| | 训练研究 | 修改训练研究等级与次数 |
| | 晋级/降级 | 俱乐部晋级/降级、教练晋升 |
| **队员** | (直接显示) | 选手翻页、名称与立绘显示、修改名称、随机换立绘 |
| | (直接显示) | 5 项能力滑块（进攻/防守/发育/反应/操作）、当前/全部选手全满 |
| | 英雄池/熟练度 | 英雄池容量显示、7 级熟练度按钮（入门→宗师）、移除英雄、全部宗师/清空 |
| | 性格/心情 | 性格列表显示、心情显示（带颜色）、指定性格分页添加、清除性格、随机/清除心情 |
| | 潜在/适应/战斗数据 | 能力潜力 D~S+、魅力 D~S、英雄适应性 6 类 D~S、英雄池容量增减、对战数据 8 项编辑、全能选手一键创建 |
| | 合同/续约 | 合同信息显示（薪资/赛季/状态）、续约、重置默认合同、设为自由合同 |
| | 装备管理 | 当前装备列表显示与移除、列出所有可用装备（战斗中可用） |
| **战斗** | (直接显示) | 双方队伍单位信息（HP/能量） |
| | 己方作弊 | 无限 HP/护盾/金钱/战术点 — 每帧自动维持、横扫/清零预设 |
| | 对方削弱 | 同上锁定开关 |
| | 强制结果/速度 | 强制获胜/强制失败（基地击杀锁定模式，有 UI 状态反馈）、战斗速度设置 |
| | 手牌控制 | 手牌/卡池数量显示、批量修改卡牌费用、补满手牌、清空手牌 |
| | BanPick 控制 | BP 锁定加速（每帧压缩倒计时至 1 秒） |
| **赛事** | 版本规则 | 锁定规则不变 |
| | 训练/难度设置 | 训练点倍率设置、恢复默认 |
| | 赛事格式 | 常规赛/季后赛/决赛 BO1/BO3/BO5 一键切换、恢复默认值 |
| **事件** | 俱乐部奖励 | 获取预算/教练点/训练点、全员能力+30、全员熟练度升至 10 级 |
| | 奖杯/勋章/成就 | 勋章显示+随机解锁、成就列表读取+进度显示、单成就解锁、一键全部成就 |
| | 速度控制 | 全局游戏速度设置/重置 |
| | 数据工具 | F2 数据快照控制台输出 |
| **调试** | 22 个功能分区 | 俱乐部/队员/战斗/赛事/事件所有功能逐项测试按钮 + 游戏原生对话框测试（BlueTip/RedTip/PurpleTip/OrangeTip 全部 5 种类型 17 个测试按钮） |

---

## 快捷键

| 按键 | 功能 |
|------|------|
| F1 | 打开/关闭修改面板 |
| F2 | 输出完整游戏数据快照到 MelonLoader 控制台 |

---

## 构建

```bash
dotnet build DebugMod/DebugMod.csproj -c Release
```

需要 .NET 6 SDK + MelonLoader v0.7.2 (MonoBleedingEdge) 依赖。项目引用：

| 依赖 | 路径 |
|------|------|
| MelonLoader.dll | `MelonLoader/net35/MelonLoader.dll` |
| 0Harmony.dll | `MelonLoader/net35/0Harmony.dll` |
| Assembly-CSharp.dll | `Managed/Assembly-CSharp.dll`（游戏主程序集） |
| UnityEngine / UnityEngine.CoreModule / 等 | `Managed/` 目录 |
| FairyGUI.dll | `Managed/FairyGUI.dll`（游戏 UI 框架） |
| Utility / Utility.GameSystem | `Managed/` 目录 |

---

**作者**: Mizuof  
**B站**: https://space.bilibili.com/516995192/dynamic  
**QQ群**: 624594852  
**网站**: www.mizu7.top  

*本修改器完全免费，请勿用于商业用途。*
