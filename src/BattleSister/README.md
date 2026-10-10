# 战斗修女 Mod

> 当前状态：已编译并安装到本机游戏的 `Mods/Battle Sister/`。首次启动游戏后，在 Mod 菜单启用。尚未进行游戏内加载/战斗验证，详细状态见 [STATUS.md](STATUS.md)。

这是一个战斗修女的数值与内容草案，当前还不是可加载的成品。`Content/BattleSisterPack` 下的 JSON 只适用于 TABContentFramework 数据包；本机没有该框架，且检查到的框架源码与本机游戏程序集不兼容。

## 本机兼容性检查（2026-10-10）

- 本机游戏程序集版本：`1.0.14.29`。
- 游戏安装目录目前没有 `Mods/`，也没有 TABModLoader。
- 公开 TABContentFramework 源码的运行时代码要求 `ZX.ZXSystem_GameLevel` 与 `ZX.Components.CLife.AddDamage` 等入口；本机游戏程序集内找不到 `ZX.ZXSystem_GameLevel`，其 `CLife` 受击入口也使用混淆名称。因此该框架不能直接视为兼容此安装。
- 在找到经确认支持本机游戏程序集的 Mod Loader/实体框架之前，不把本草案部署到游戏目录，也不声称生命光环或常驻染色已实装。
- 更贴近此功能需求的公开工具是 Nexus Mods 的 **Entity Creation Tools 1.02**：说明中包含新单位注册、单位颜色、定制行为与 C# 示例工程；依赖 JKstring Mod Loader、Entity Registry Tools 和 Custom Hit Effect Tools。本机尚未安装这些依赖；Nexus 文件页要求登录后访问示例工程与依赖包。应以该工具链重新实现本 MOD，而不是继续扩展上面的 JSON 草案。

## 已配置部分

- 克隆原版 `Ranger`，训练地点为 `SoldiersCenter`。
- 生命暂设为 400（按游侠 80 的五倍）。
- 普通攻击暂设为 15（按游侠 10 的 1.5 倍）；射程暂设为 7（按游侠 6 加一格）。
- 费用暂按游侠 120 金、4 木、1 食物，改成 220 金、4 木、1 食物、10 铁。
- 移动速度、护甲、动画、投射物等继承游侠。

## 待实现 / 待核验

1. **生命光环**：6 格内游侠 +100 临时生命、攻击力 ×1.2；临时生命优先承伤。当前数据包 schema 没有光环或临时生命字段，需要在运行时框架加自定义行为，或另开发依赖 Mod Loader 的代码扩展。临时生命池必须独立追踪并在受击时先扣除，过期/离开范围时移除未消耗部分；若简单提高原生 `Life`，会造成永久生命或治疗行为错误。
2. **整单位染色**：目标改为整单位红色染色。当前内容包 schema 没有常驻实体色相字段；框架的运行时代码能对受击目标暂时设置 `SceneObject.ColorHue`，但这不是单位出生后一直生效的染色。需要给框架加一个按实体 ID（`BattleSister`）设置并在场景对象重建时重应用的常驻色相行为。目标色暂定为偏深的红色，实际观感需进游戏调试；单纯用红色通道相乘会令原模型绿色部分变暗，建议用红色整体色调而非纯红色滤镜。
3. **当前版本实测**：基值来自游侠常见数据值，尚未从本机当前版本的数据表确认。DAT 中已知的社区解包口令在本机 `ZXRules.dat` 上无效；部署前核实 `Life`、`Damage`、`ActionRange`、`GoldCost`、`WoodCost`、`FoodNeeded` 和铁资源字段。
4. **老兵攻击**：当前配置将老兵伤害暂设为 30，假定游侠老兵攻击为 20；若实际值不同，需按实际游侠老兵攻击 ×1.5 重算。

## 文件

- `Content/BattleSisterPack/manifest.json`：内容包清单。
- `Content/BattleSisterPack/units/battle_sister.json`：单位定义、克隆基底、数值和训练费用。
- `Content/BattleSisterPack/strings/strings.csv`：单位名称和说明。

## 当前交付状态

- JSON 单位数据草案已写入，用于后续数值迁移和比对，不能被本机游戏直接读取。
- 战斗修女的常驻整单位染色与 6 格生命光环尚未实现。
- 继续实装需要取得 Entity Creation Tools 及其 DLL/示例工程，并先确认对本机 `1.0.14.29` 的兼容性，然后编译依赖其自定义实体 API 的 C# 模组。

参考：<https://www.nexusmods.com/theyarebillions/mods/34?tab=description>；源码和可选示例文件见需登录的 Nexus Files 页：<https://www.nexusmods.com/theyarebillions/mods/34?tab=files>。

## Campaign starter unit

The mod now grants one Battle Sister during initialization of the campaign's initial mission. It places her on a free map tile near the Command Center, skips the grant if a Battle Sister is already present (including loaded saves), and logs placement failures to `zxlog.txt`. This change compiled and the deployed DLL hash matches the build. It still needs an in-game launch to verify the loader hook and placement behavior.

## Barracks train option correction

The Entity Registry Tools source confirms it creates a `Train` command for every registered unit. The previous mod code added a duplicate and selected the first generic `Train` command rather than the Battle Sister command. The mod now configures `Train.For(typeof(BattleSister))`, ensures `SoldiersCenter` is allowed, and assigns the unit an unused Soldiers Center panel slot (while reserving the rally point slot). The build succeeded and the deployed DLL hash matches. In-game confirmation is still needed.
