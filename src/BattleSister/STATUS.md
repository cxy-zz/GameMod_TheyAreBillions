# 战斗修女 Mod 实装状态

## 收到的文件

`temp_scripts/inbox/` 内目前有两份 Nexus 包：

- `Examples Source Code-34-1-01-1772214777`：六个示例 C# 工程。
- `Example Unit Mods-34-1-02-1772216200`：示例模组 DLL、示例规则/字符串数据和配置。
- `Entity Creation Tools-34-1-01-1772214471`：Entity Registry Tools、Custom Hit Effect Tools、Infinite Portraits。
- `TABModLoader-v1.1.6`：加载器仓库源码、示例和 DXVision 等编译库。

它们适合用来核对 Entity Creation Tools 的 API 用法，但不是工具本体或运行依赖。

## 本机核验结果

- 游戏版本为 `1.0.14.29`，安装目录为 `D:\Program Files (x86)\Steam\steamapps\common\They Are Billions`。
- 游戏目录中没有 `Mods/`、Mod Loader 或 Entity Registry Tools。
- Entity Registry Tools DLL 和 DXVision.dll 现已在收件目录找到。
- 当前收件目录中的 Mod Loader 是源码仓库快照；没有安装发布包 `ModLoaderUpdate.zip` / `TheyAreBillions_ModLoaderUpdate.exe`。
- 示例项目文件把引用指向作者自己的 `E:\SteamLibrary\...\Mods` 和 Documents 路径；这些路径在本机不可用。
- 示例 `ZXRules.dat` / `ZXStrings.dat` 是加密数据包，不能当作可直接编辑的文本模板。

## 当前安装结果

- 已将用户提供的 `TheyAreBillions_ModLoaderUpdate.exe` 安装到游戏目录；它的文件版本与原游戏一致（`1.0.14.29`）。原版文件备份在 `temp_scripts/backups/TheyAreBillions.exe.original-1.0.14.29`。
- 已安装 Entity Registry Tools、Custom Hit Effect Tools、Infinite Portraits、All Entities Generator、Panel Resolver 和 MapEditor 到游戏 `Mods/` 目录。
- 已构建 `src/BattleSister/bin/Release/BattleSister.dll` 并复制到游戏 `Mods/Battle Sister/`，两份 DLL 的 SHA-256 相同。
- 代码把 Ranger 作为数据基底，运行时读取其生命、射程、伤害和训练费用后应用倍率/增量；加上 6 格范围扫描、最多 100 点临时护盾（先承伤）以及攻击伤害乘 1.2。整体颜色使用红色 ColorHue。
- 尚未启动游戏确认 Mod Loader 界面或实际训练/战斗表现；首次进入游戏后需在 Mods 菜单启用 Battle Sister。若日志报错，检查游戏目录生成的 `zxlog.txt`。

## 完成可加载模组所需文件

Entity Registry Tools 随 Nexus 的 Entity Creation Tools 主文件一起提供，目前主文件已经收到。

1. 从 [JKstring Mod Loader Releases](https://codeberg.org/JKstring/TABModLoader/releases) 获取发布包 `ModLoaderUpdate.zip`。当前仓库源码快照不能代替发布安装包。
2. Entity Creation Tools 主文件已解压，工具 DLL 可用于检查/编译。游戏中仍需由 Mod Loader 加载其文件夹。
3. `temp_scripts/` 中已有 All Entities Generator 和 Panel Resolver 的下载副本，但尚未安装到游戏目录。Entity Creation Tools 页面将它们列为依赖。
4. Entity Creation Tools 说明把 Custom Hit Effect Tools 列为自定义实体前置工具；其组件应随完整主文件包提供。战斗修女不需要自定义命中效果 API。

安装发布版加载器后仍需验证本机游戏兼容性、制作 ZXRules/ZXStrings 数据、实现临时生命承伤规则、编译并安装模组。当前草案的 JSON 不能被本机游戏直接加载。

## 实现边界

示例 API 展示了自定义实体注册、Ranger/Soldier 类继承、默认调色、实体组件更新和生命组件访问。它没有展示“临时生命优先承伤”的拦截实现。该机制需要在运行时确认伤害入口/组件生命周期后实现；不能通过简单提高 Ranger 的 `Life` 安全模拟，因为会永久增加生命或造成治疗/存档问题。

在依赖和伤害接口经过验证前，不应声称战斗修女已编译、已安装或生命光环已生效。

## 2026-10-10 update

- Added a Harmony postfix on `ZXLevelState.SetCurrentLevel`.
- During the campaign's initial mission, it creates one Battle Sister on an unoccupied tile near the Command Center; it does not create another if one already exists.
- Compilation succeeded. The updated DLL was deployed to the game Mods folder and its SHA-256 matches the build.
- In-game verification remains pending.

## Barracks option fix

- Reused Entity Registry Tools' already-created Battle Sister `Train` command instead of adding a duplicate.
- Ensured its `AvailableFor` includes `SoldiersCenter` and enabled it as a command.
- Assigned a free Soldiers Center panel slot to avoid overlapping Ranger's train button.
- Build succeeded; deployed DLL SHA-256 matches local build. In-game verification remains pending.
