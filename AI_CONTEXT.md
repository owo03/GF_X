# AI_CONTEXT

## 项目简介
- 当前仓库是一个基于 **GF_X** 框架模板开发的 Unity 游戏项目。
- 当前已落地的业务玩法是一个“**翻牌构筑**”原型，核心逻辑位于 `Assets/AAAGame/Scripts/Procedures/GameProcedure.cs`。
- 玩法特征：玩家在固定棋盘上翻开卡牌，获得金币和分数；回合结束后选择奖励卡；大回合之间进入商店购买遗物或随机卡。
- 仓库本身不仅包含该玩法，也包含 GF_X 框架层、编辑器工具、热更流程和若干 Demo/模板代码。

## 技术栈与版本线索
### 引擎与运行时
- Unity：`2022.3.62f3c1`
  - 来源：`ProjectSettings/ProjectVersion.txt`
- C# 项目：存在 `Assembly-CSharp.csproj`、`Hotfix.csproj` 等 Unity 自动生成工程文件。

### 核心框架与库
- GameFramework / UnityGameFramework
- HybridCLR
- Obfuz / Obfuz4HybridCLR
- UniTask
- DOTween
- ZString
- UGUI + TextMeshPro
- Newtonsoft Json
- Protobuf

### Unity Package 线索
来源：`Packages/manifest.json`
- `com.code-philosophy.hybridclr`
- `com.code-philosophy.obfuz`
- `com.code-philosophy.obfuz4hybridclr`
- `com.unity.render-pipelines.universal` `14.0.12`
- `com.unity.textmeshpro` `3.0.8`
- `com.unity.ugui` `1.0.0`
- `com.unity.cinemachine` `2.10.3`
- `com.unity.nuget.newtonsoft-json` `3.2.1`
- `jillejr.newtonsoft.json-for-unity.converters` `1.6.3`

### Plugins 目录线索
来源：`Assets/Plugins`
- `DOTween`
- `UniTask`
- `UnityGameFramework`
- `ZString`
- `Protobuf`
- `Android`

## 目录结构说明
```text
GF_X/
├─ Assets/
│  ├─ AAAGame/
│  │  ├─ Audio/
│  │  ├─ Config/
│  │  ├─ DataTable/
│  │  ├─ Font/
│  │  ├─ HotfixDlls/
│  │  ├─ Language/
│  │  ├─ Materials/
│  │  ├─ Models/
│  │  ├─ Prefabs/
│  │  ├─ Scene/
│  │  │  ├─ Launch.unity
│  │  │  └─ Game.unity
│  │  ├─ ScriptableAssets/
│  │  ├─ Scripts/
│  │  │  ├─ Common/
│  │  │  ├─ DataModel/
│  │  │  ├─ DataTable/
│  │  │  ├─ Demo/
│  │  │  ├─ Entity/
│  │  │  ├─ EventArgs/
│  │  │  ├─ Extension/
│  │  │  ├─ MessageBox/
│  │  │  ├─ Network/
│  │  │  ├─ Procedures/
│  │  │  ├─ ScriptableObject/
│  │  │  └─ UI/
│  │  ├─ ScriptsBuiltin/
│  │  │  ├─ Editor/
│  │  │  └─ Runtime/
│  │  ├─ Shader/
│  │  ├─ SharedMaterials/
│  │  ├─ Sprites/
│  │  └─ Textures/
│  └─ Plugins/
├─ Packages/
├─ ProjectSettings/
├─ Hotfix.csproj
├─ Assembly-CSharp.csproj
├─ README.md
└─ AI_CONTEXT.md
```

## 页面 / 接口 / 服务 / 数据库模型梳理
### 页面 / UIForm
来源：`Assets/AAAGame/Scripts/UI/Core/UIViews.cs` 与对应 UI 脚本
- `MenuUIForm`
  - 旧主菜单 UI，仍保留代码。
- `GameUIForm`
  - 当前主玩法 UI。
  - 包含状态区、棋盘、牌库面板、悬浮信息。
- `GameOverUIForm`
  - 结算界面。
- `Topbar`
  - 旧顶栏 UI。
- `SettingDialog`
  - 设置界面。
  - 当前已恢复使用 prefab 中的 `Popup08_Topbar_Divided` 结构。
  - 音乐、音效、语言、震动等基础逻辑由 `SettingDialog.cs` 绑定到旧控件。
- `RatingDialog`
  - 评分弹窗，保留在枚举中。
- `TermsOfServiceDialog`
  - 服务条款弹窗，保留在枚举中。
- `CommonDialog`
  - 通用弹窗。
- `LanguagesDialog`
  - 语言选择界面。
- `ToastTips`
  - Toast 提示界面。

### 玩法流程 / 页面关系
来源：`HotfixEntry.cs`、`PreloadProcedure.cs`、`ChangeSceneProcedure.cs`、`GameProcedure.cs`
- 启动入口：`HotfixEntry.StartHotfixLogic()`
- 预加载流程：`PreloadProcedure`
- 切场景流程：`ChangeSceneProcedure`
- 当前主链路：`Launch.unity -> PreloadProcedure -> ChangeSceneProcedure -> Game.unity -> GameProcedure`
- 旧 `MenuProcedure` 代码仍存在，但当前 `ChangeSceneProcedure` 在 `Game` 场景加载完成后会直接进入 `GameProcedure`。

### 接口 / 网络服务线索
来源：`Assets/AAAGame/Scripts/Network/`
当前项目中未发现 HTTP/REST API 封装；存在的是 **GameFramework 网络通道 + Protobuf 包协议基础设施**：
- `NetworkChannelHelper.cs`
  - 网络通道辅助器
  - 负责注册包类型、心跳、序列化/反序列化、网络事件处理
- `PacketType.cs`
  - 区分 `ClientToServer` / `ServerToClient`
- `CSPacketBase.cs` / `SCPacketBase.cs`
- `CSPacketHeader.cs` / `SCPacketHeader.cs`
- `Packet/CSHeartBeat.cs`
- `Packet/SCHeartBeat.cs`
- `PacketHandler/SCHeartBeatHandler.cs`

可确认结论：
- 网络层基础设施已存在
- 当前已看到的实际业务包仅有心跳包
- 更完整的在线服务、业务接口、鉴权接口：**待确认**

### 数据模型 / 持久化 / “数据库模型”
当前仓库未发现关系型数据库模型、ORM 实体或后端数据库迁移脚本。
可确认的数据模型与持久化方式如下：
- `PlayerDataModel.cs`
  - 当前用于玩家金币/数值同步
- `DataModelStorageBase.cs`
  - 基于 `GF.Setting` 的本地 JSON 持久化
  - 使用 `Newtonsoft.Json.JsonConvert.PopulateObject(...)` 恢复对象

可确认结论：
- 没有发现传统数据库模型
- 当前更像是 **本地存档 / 设置持久化** 而非数据库

### 数据表 / 配置模型
- `AppConfigs.cs`
  - ScriptableObject
  - 配置运行时需要加载的：
    - DataTables
    - Configs
    - Languages
    - Procedures
- `FlipRunConfig.cs`
  - ScriptableObject
  - 当前翻牌玩法的运行配置
  - 包含棋盘尺寸、基础翻牌数、目标分、商店价格、初始牌库、卡牌定义与效果模板参数
  - 卡牌定义现支持：
    - `id`
    - `code`
    - `nameKey` / `descKey`
    - `rarity`
    - `tags`
    - `effectType`
    - 即时收益 / 持续收益参数
    - `offerWeight` / `rewardWeight` / `shopWeight`
- `FlipRunConfigInspector.cs`
  - 自定义 Unity Editor Inspector
  - 用中文分区展示 `FlipRunConfig`
  - 支持卡牌列表、详情面板、标签编辑、初始牌库编辑、建议权重填充
- `LanguagesTable.cs`
  - 多语言数据表结构
  - 字段：`LanguageKey` / `AssetName` / `LanguageDisplay` / `LanguageIcon`

## 环境变量线索
### 已确认的运行时设置键
来源：`Assets/AAAGame/ScriptsBuiltin/Runtime/Common/ConstBuiltin.cs` 与相关扩展
- `Setting.Language`
- `Setting.ABTestGroup`
- `Setting.ResolutionWidth`
- `Setting.ResolutionHeight`
- `Setting.FullScreen`
- `Sound.<Group>.Mute`
- `Sound.<Group>.Volume`

### 已确认的编辑器环境变量
- `UNITY_IL2CPP_PATH`
  - 来源：`Assets/AAAGame/ScriptsBuiltin/Editor/HybridCLRExtensionTool.cs`
  - 用于 HybridCLR 编辑器工具流程

### 其他环境变量
- 未发现 `.env`、`appsettings.json`、shell env 配置文件等常见环境变量文件
- 其他部署环境变量：**待确认**

## 启动与构建方式
### 编辑器启动
1. 使用 Unity `2022.3.62f3c1` 打开项目
2. 打开场景 `Assets/AAAGame/Scene/Launch.unity`
3. 点击 Play
4. 当前代码会完成预加载、切换到 `Game.unity`，并直接进入 `GameProcedure`

### 热更入口
- `HotfixEntry.StartHotfixLogic(bool enableHotfix)`
- 该入口会读取 `AppConfigs` 中的流程类并初始化流程状态机

### 构建方式线索
来源：README 与编辑器工具代码
- README 中提到可通过 Unity 菜单 `Build App/Hotfix` 进行打包/热更资源构建
- 项目内存在较多编辑器工具与打包相关脚本
- 命令行构建脚本、CI 配置、正式发包流水线：**待确认**

## 编码规范与约定
### 可从代码确认的约定
- 业务主代码目录：`Assets/AAAGame/Scripts/`
- 内置运行时代码目录：`Assets/AAAGame/ScriptsBuiltin/Runtime/`
- 编辑器工具目录：`Assets/AAAGame/ScriptsBuiltin/Editor/`
- UI 变量文件位于 `Assets/AAAGame/Scripts/UI/UIVariables/`
  - 文件头明确标注为工具自动生成
  - 不应手改
- UI 脚本普遍采用 `partial class` + 自动生成变量文件配合的方式
- 流程管理采用 `ProcedureBase` + `ChangeState<T>()`
- UI 打开关闭通过 `GF.UI.OpenUIForm(...)` / `GF.UI.CloseUIForm(...)` 及其扩展方法
- 多语言文本通过 `GF.Localization.GetString(...)` 获取
- 本地设置和数据模型持久化通过 `GF.Setting` 完成

### 文本与资源注意事项
- `Assets/AAAGame/Language/*.json` 当前读取链路对文本编码敏感
- 已确认 `ChineseSimplified.json` 曾出现编码异常，后续修改建议统一使用 **UTF-8**

## 当前开发进度
### 已可运行部分
- 热更入口可用
- 预加载流程可用
- 场景切换流程可用
- 当前主玩法 `GameProcedure` 可运行
- 结算界面可用
- 牌库面板可查看
- 鼠标悬停卡牌信息可显示
- 多语言基础接入已存在
- 分辨率 / 全屏设置基础逻辑已接入

### 当前玩法进度
来源：`GameProcedure.cs`
- 玩法已从“代码内硬编码卡牌常量”改为“`GameProcedure` + `FlipRunConfig` 资产”组合
- 默认棋盘：`4 x 3`
- 默认基础翻牌数：`5`
- 默认大回合目标分：`40 / 100 / 190`
- 小回合数：每大回合 `3` 个小回合
- 支持内容：
  - 即时触发卡牌
  - 回合结束结算卡牌
  - 持续卡
  - 奖励三选一
  - 商店购买遗物 / 随机卡
  - 日志文本与状态栏刷新
- 卡牌定义当前存放在：
  - `Assets/AAAGame/ScriptableAssets/FlipRun/FlipRunConfig.asset`
- 当前卡牌配置方式：
  - `GameProcedure.cs` 运行时使用字符串卡牌 ID，不再依赖 `CardId` 枚举
  - 卡牌的显示信息、基础数值、稀有度、标签、掉落权重、持续回合、模板类型在 `FlipRunConfig` 中维护
  - 奖励池和商店池已支持分开权重
  - 新增“完全新的效果模板”仍需改代码中的 `FlipRunCardEffectType` 与 `GameProcedure.ResolveCard(...)`
  - 在 Unity 编辑器中，`FlipRunConfig.asset` 已有中文自定义 Inspector，日常配卡优先走该面板

### 当前 UI 进度
- `GameUIForm` 已替换掉旧 demo 主显示逻辑，能承载当前玩法
- `SettingDialog` 当前已切回 prefab 原始设置结构，未继续沿用运行时自建面板

## 待办事项
仅列出从当前代码与现状可以合理确认的事项：
- 扩充卡池、遗物和联动套路
- 继续把卡牌系统从“字符串 ID + 模板配置”推进到更彻底的数据驱动：例如连遗物、日志模板、标签联动判定也进一步配置化
- 完成设置界面交互与布局稳定化
- 清理或统一旧 `MenuProcedure` / `MenuUIForm` / `UITopbar` 与当前直接进局流程
- 将卡牌说明从当前文本式展示进一步过渡到图片化展示
- 完善商店体验（当前是基础版）
- 增补自动化测试或至少形成明确测试入口：**待确认是否已有计划**
- 补完场景加载进度显示（代码中已有 TODO）

## 风险点与待确认项
### 已知风险点
- `SettingDialog` 已恢复为旧 prefab 结构，但设置能力目前主要是音频 / 语言 / 震动；更完整的 PC 设置项是否继续保留待确认
- `MenuProcedure` 仍保留旧的“点击空白进入游戏”逻辑，但当前主链路已绕过它，双入口并存存在维护风险
- `Assets/AAAGame/Scripts/Demo/` 仍保留示例代码，并持续产生编译 warning
- `Assets/AAAGame/Scripts/UI/UIVariables/SettingDialog.Variables.cs` 中仍有未使用字段 warning，说明旧 prefab 变量与现有设置页实现未完全对齐
- `FlipRun` 卡牌配置已转移到 ScriptableObject，且局内已改为字符串卡牌 ID；但“新增全新效果模板”仍需要改代码，说明当前仍属于“半数据驱动”状态
- README 当前文本编码异常，不能作为完全可靠的唯一说明文档
- 多语言文件编码不一致会直接导致运行时乱码

### 代码中明确存在的 TODO
- `Assets/AAAGame/Scripts/Procedures/ChangeSceneProcedure.cs`
  - 场景加载进度显示未实现
- `Assets/AAAGame/Scripts/Extension/SoundExtension.cs`
  - 临时资源存在判定未完成

### 待确认项
- 正式服务器接口、账号体系、在线服务能力
- 是否存在真实后端数据库或外部服务依赖
- 正式构建发布流程与 CI 配置
- 目标平台最终范围（当前从代码可见 PC/分辨率/全屏方向，但最终发行平台仍需确认）
- 美术资源规范、命名规范文档、代码风格文档是否另有项目内文件说明
