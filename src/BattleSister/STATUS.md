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

## Follow-up after player screenshot

The game source confirms barracks options are generated from entity templates whose `BuildingFrom` equals the selected building type, via `Train.For(entityType)`. The mod now explicitly sets both `BuildingFrom` and `BuildingRequisite` to `SoldiersCenter`, configures the Entity Registry-created `Train` command without registering it as a generic `ShowAsCommand`, removes duplicate train commands, and clears `CCommandable`'s cached command lists. This is compiled and deployed. The running game must be fully restarted to load a replaced DLL; after restart, inspect `zxlog.txt` for the BattleSister registration lines if the icon remains absent.

## Loader visibility follow-up (2026-10-10)

The player's `zxlog.txt` excerpt contained no `[BattleSister]` or `Entity Registry` registration messages. Added explicit `Enabled`, `IsCampaign`, and `IsGlobalEnabled` metadata plus early `OnLoad`/`OnLoadResources` markers and exception logging. Rebuilt and deployed both DLL and ModInfos.json. After a full game restart, the log should contain `[BattleSister] OnLoad reached` and `[BattleSister] OnLoadResources entered`; if not, the loader is skipping the mod or the excerpt is from a different game run.

## Log path correction

`EditorWorldGenMod.log` is the MapEditor mod's own log, not a game-wide `zxlog.txt`. The Battle Sister mod now writes its own diagnostics to `Mods/Battle Sister/BattleSister.log` under the game install folder, while also forwarding messages to `DXLog`. The updated DLL is deployed; fully restart the game to create/read the new log.

## ZXRules validation failure fixed

The BattleSister.log reported Entity Registry validation failure because `Entities` lacked a `BattleSister` row. Before calling `RegisterEntity`, the mod now clones Ranger's row into `Entities/BattleSister`, clones Ranger's normal and veteran attack rows into `Commands/BattleSisterAttack` and `Commands/BattleSisterAttackVeteran`, and points the new entity's `AttackCommand` at its custom attack row. Build succeeded and the deployed DLL hash matches. Restart and inspect `Mods/Battle Sister/BattleSister.log` for the rule-row and barracks setup messages.

## Campaign load crash isolation

Windows Application Error entries at 15:54:47 and 15:54:53 show the game crashed while launching/loading, with CLR exception code `0xe0434352` / `c000041d`. The latest BattleSister.log only reached `OnLoad`; there is no resource-stage error in the current run. Removed the `ZXLevelState.SetCurrentLevel` starter-unit spawn patch as the riskiest level-load mutation, retaining ZXRules registration, barracks training, and aura code. Safety build compiled and was deployed. The crash source is not yet proven; retest by fully restarting and loading the campaign, then check whether BattleSister.log records `OnLoadResources` and successful registration.

## Parameter-reference fix after repeat crash

The 15:57 logs show resource registration succeeded, followed by another APPCRASH/CLR20r3 during load. Disabling the SetCurrentLevel starter hook did not by itself prevent the crash. Code review found `AddEntityDefaults` replaced the `DicDefaultParamsPerTemplate` object after the template had already captured EntityRegistry's original record. It now mutates that existing record in place, preserving the template reference and applying overrides consistently. The new build compiled and was deployed. Verify by restart; expected barracks log now reports both `BuildingFrom=SoldiersCenter` and `BuildingRequisite=SoldiersCenter`.

## Campaign save crash follow-up (2026-10-10)

Windows .NET Runtime event stack identifies the crash in `ZXDefaultParams.ApplyMod` while `ZXSystem_GameLevel.ApplyCampaignMods` loads the saved campaign. Added a last-priority Harmony guard before each parameter modifier runs. It checks that the modifier's reflected property belongs to the declared entity/command parameter type; if metadata is mismatched, it resolves the property by name against the correct parameter type and records the modifier in `BattleSister.log`. If the property does not exist for that parameter class, that invalid modifier alone is skipped and logged to avoid taking down the load.

Build succeeded targeting the installed .NET Framework 4.8 reference assemblies (the .NET Framework 4.7.2 targeting pack is not installed here). Deployed `BattleSister.dll`; local and deployed SHA-256: `001767896E2A85CEC2F51C45D1D544D1A2D14D92BD91A98409F868A35DD8BE39`. Runtime verification still requires loading the affected campaign save and checking `Mods/Battle Sister/BattleSister.log` for any `Corrected campaign modifier` or `Skipped incompatible campaign modifier` line.

## Default snapshot mismatch follow-up (2026-10-10)

The player's 16:08 crash still reported System.Reflection.TargetException in ZXDefaultParams.ApplyMod_Patch2, confirming the prior property-only guard was insufficient. Expanded the guard to rebuild any DefaultValues snapshot whose runtime type does not match the entity or command parameter record, copying the record's current public parameter values into a same-type snapshot before campaign modifiers run. Corrected or rebuilt records are logged with modifier/ID details to BattleSister.log.

Build succeeded; local and deployed DLL SHA-256: 1C1D375F4978984A2EFD1EC14EA9C14530C04425CBE95D8C4BBD315DF9DA10DF. Retry the same save after fully restarting the game. The fix is not runtime-verified yet.

## Campaign training visibility fix (2026-10-10)

The player's screenshot showed the custom unit registration was loaded but its train button remained hidden. Train.IsVisibleFor checks campaign unlock state; vanilla campaign data has no BattleSister unlock entry, so it hid the command after the barracks command list was generated. Added a narrow prefix that makes only Train<BattleSister> visible. The unit remains attached to Soldiers Center through BuildingFrom, and normal Train.IsEnabledFor resource checks still apply.

Build succeeded; local/deployed SHA-256: 1EA55B20608718A9A2325CE00F68533289925C180B570D5AAA92BC9F70527910. In-game verification is pending after a full restart.

## Combat portrait, attack binding, and aura feedback fixes (2026-10-10)

From the player's screenshot: the portrait override returned the Soldier portrait; aura component was attached to the template instance rather than included in spawned units' DefaultComponents; and custom command defaults were stored in a separate record from the attack command's live Params, so campaign modifiers/UI/runtime could read different objects. Updated the portrait to Ranger, made BattleSisterAura a default component for each spawned unit, added a selected-only 6-cell radius ring, and bound each custom attack command to the same ZXCommandDefaultParams record held in ZXDefaultParams.All. Added setup diagnostics for basic/veteran attack type, damage, and range, plus a fallback for missing Registry-created attack instances.

Build succeeded; local/deployed SHA-256: 99903DE34A597161BA3F28760A22AFE9DEEA493B2D7A518A1807DACBB0721621. This build needs full game restart and in-game verification. The aura radius visualization is shown when the Battle Sister is selected.

## Chinese localization and portrait color update (2026-10-10)

Set Battle Sister's runtime Name to 战斗修女 and provided an original Chinese unit description covering its role, stats, aura, and cost. Kept the Ranger portrait as the base image with changePortraitColors=true; added explicit matching red VeteranColorData so veteran rendering uses the same red palette. The same GetPortraitImage() is used by barracks training, selection, and entity/veteran views.

Build succeeded; local and deployed SHA-256: 43B0DC900A60591BEF07E31C12DF05E6BEF67A14786FCCD3F2CB25C9502AAD6C. Full game restart and visual verification are pending.

## Portrait isolation and balance update (2026-10-10)

Disabled Entity Registry Tools' portrait-style registration for Battle Sister. Its portrait rendering uses shared static state and the previous registration caused red styling to bleed into unrelated unit portraits. Battle Sister portrait objects now receive the red tint only when tagged as Battle Sister or its Train command, with a separate veteran portrait hook; this keeps Ranger and infected portraits untouched. Updated IronCost to 2 and Life to 350. Interpreted "attack speed +1" as +1 attack per second in the UI metric 1000 / (TimeAction + TimePreAction + TimePostAction); adjusted Battle Sister TimeAction for both normal and veteran attacks accordingly.

Build succeeded; local/deployed SHA-256: 179482D75A78897885694090B1B9CC291DDB7948FFD89B7F1662F08E05555151. Requires full game restart; no in-game check yet.

## Portrait bleed correction follow-up (2026-10-10)

The player confirmed the Barracks image remained uncolored and Ranger's portrait was red. Root cause was the mod's DXProjectImage.SetImageFor patch changing pooled UI object color properties; those objects are reused across commands and entities. Removed those patches and the Entity Registry veteran portrait style path. The mod now clones Ranger's portrait bitmap, changes only green-hue pixels to red, and assigns the isolated image to Battle Sister before Entity Registry registration. Veteran units reuse the same custom portrait and attack command, avoiding the Registry's veteran portrait recolor hook. Original Ranger and infected portrait objects are not changed.

Build succeeded; local/deployed SHA-256: 871E9AE12366F9BBB28B294EBDBA3EDB68A57D3273CC64898E7B64C269A69510. Fully restart the game to clear old in-memory UI objects; runtime verification remains pending.

## Portrait image registration exception (2026-10-10)

The latest `BattleSister.log` showed `InvalidCastException` in `CreateIsolatedRedRangerPortrait`: `DXProjectImage.Image` casts assigned values to `DXImageFile`, so assigning a `DXImageBitmap` aborts all resource registration. Changed portrait creation to set the cloned image's unique name and assign its recolored bitmap through `DXProjectImage.Bitmap`; the setter writes a standalone PNG into the project's portrait image folder and refreshes the backing `DXImageFile` cache. This avoids mutating Ranger's source image and allows resource registration to continue.

Build succeeded with zero warnings/errors targeting installed .NET Framework 4.8 reference assemblies. Local DLL SHA-256: `58917A7D193E99FAFECDD2D23D00866DBCBA768E841D3F8BCC6B255354E8DACA`. Deployment copy ran while the game process had the DLL open; its file timestamp updated, but Windows then denied reading it for hash verification. Fully exit the game before retrying/validating the deployed DLL, then relaunch and verify that `OnLoadResources` completes and the Battle Sister portrait is red while Ranger remains unchanged.

## Shared portrait file reference fix (2026-10-10)

The next log confirmed that setting `Name` on the MemberwiseClone portrait caused DXProjectImage to move the shared Ranger.png file. Replaced MemberwiseClone with a fresh `DXProjectImage`, copied only the project/category metadata, and wrote the recolored bitmap under its own BattleSisterPortrait.png name. Build succeeded with zero warnings/errors. Deployed DLL SHA-256: `D348825652B71CA5A429158164D5DD18AD6B783CD9E82012CD3E2E3E0AE33AC4`. Fully restart the game and verify resources register, Battle Sister portrait is red, and Ranger/infected portraits retain their original colors.

## Campaign load rendering crash fix (2026-10-10)

Windows Application Error / .NET Runtime records for the 17:23:55 crash show `NullReferenceException` in `DXProjectImage.get_ImageArea`, called while `ZXSystem_GameLevel.UpdateArmyButtons` rendered the saved game. Cause: the runtime-created Battle Sister portrait had the optimized `Portraits` category but was not present in its atlas; atlas lookup returned null. Kept the new portrait uncategorized so it renders from its standalone generated PNG and does not query a missing atlas entry. Build succeeded with zero warnings/errors. Deployed local/game DLL hashes match: `1D22DAC7F904C6EA61FF070F7D59FBDB908B628F2D23F5A8AB115F6C0482B35F`. Restart and reload the same save to verify this crash is resolved.

## Portrait placeholder fix (2026-10-10)

The player's screenshot showed the portrait frame was blank. The generated `BattleSisterPortrait.png` was only 118 bytes, a 1x1 placeholder: in deployed mode `source.Bitmap` returned the placeholder for the atlas-backed Ranger portrait. Updated image extraction to read the atlas texture through `source.Image.GetBitmap()`, crop the Ranger rectangle from `source.ImageArea`, recolor those pixels, and then write the standalone portrait. It rejects 1x1 fallback images. Build succeeded with zero warnings/errors. Local/game DLL hashes match: `9AB663FB3FBC162AD9194CFC14E1208B2B8EA7DA9634E3C4A7F1ABFA113B5C3E`. Restart the game so the generated PNG is rebuilt, then verify the Battle Sister portrait appears and the vanilla portraits remain unchanged.

## UI portrait tint isolation (2026-10-10)

The player's screenshot showed the selected/veteran Battle Sister portrait still displayed Ranger's original colors. Runtime logs confirmed the compressed atlas texture does not expose CPU bitmap pixels (`GetBitmap()` unavailable), so removed generated portrait extraction. Battle Sister now returns the vanilla Ranger portrait and marks only the immediate draw call; a Harmony hook applies red tint to that specific rendered `DXObject`, and resets previously tinted pooled UI objects before reuse. Veteran styling is reapplied after the game's veteran portrait hook. This avoids modifying the shared Ranger portrait image. Build succeeded with zero warnings/errors. Local/game DLL hashes match: `D33EE0D6289AF366CE8BC7F41417B10A76B577D138E522398FF360A03DB8C97E`. Restart and verify barracks, selected unit, and veteran portrait; confirm Ranger and infected portraits remain unchanged.

## Ottermar Auxiliary unit (2026-10-10)

Added `OttermarAuxiliary`, based on `SoldierRegular`, to the Soldiers Center. It uses the `HumansA/Soldier_A` sprite and Soldier portrait with blue visual/portrait tint. Gold cost is Soldier +30; iron cost is Soldier +1; WalkSpeed and RunSpeed are each Soldier +0.5. Basic and veteran attacks each copy the matching Soldier attack parameters and add 20 damage, preserving the vanilla veteran difference. Added an `AuxiliaryGrenadeCharge` per-unit component initialized with one charge, a special grenade command, and a custom Throw-derived command that uses vanilla ThrowGrenade parameters without changing mission-wide grenade inventory. Extended campaign train visibility to this unit and generalized train-command setup. Updated ModInfos to version 1.1.0. Build succeeded with zero warnings/errors; local/game DLL hashes match: `D68D77CDF2E6FBF6FAB50C5E1CDAEABC249ED32855CD2EB196C3E8326ADEB7AB`. In-game confirmation is pending: verify Barracks visibility, blue appearance, cost/stat values, and that its single grenade can be used without changing global grenade count.

## Crash on saved-game load and grenade registration failure (2026-10-10)

The 18:59 `BattleSister.log` showed resource registration aborting because the Commands table has no `UseItem_Grenade` row. The unit grenade button now runs as a special command and no longer requires that nonexistent rule row; its hidden throw command copies the live vanilla `ThrowGrenade` parameters. A Windows .NET crash record also identified a null reference in vanilla `CInvisibleWhenNotWatched.SetVisibility()` while the saved-game scene was being reconstructed. Added an early Harmony guard that skips that callback until the level extension, minimap watcher, entity, and scene object are available. The guard is installed before registration so it remains active even if another setup step fails.

Build succeeded with zero warnings/errors. Deployed DLL hash matches local: `4FDFFEA7416F48FF394A87B8092A878F19BF5F3AA87FDC19C29A81CC70C6CEA2`. The game process was still open during deployment, so restart the game before loading the save; runtime confirmation is pending.

## Mod Loader reports DLL file lock (2026-10-10)

The user's follow-up attachment ends with `OnLoad reached` at 19:11:42, showing the mod entry was successfully invoked in at least one launch, while the screenshot's loader error reports `HRESULT 0x80070020` against `BattleSister.dll`. Added a byte-identical `BattleSisterMod.dll` fallback in the mod directory. The Mod Loader enumerates all top-level DLLs and continues after an individual DLL load exception, so it can load the fallback path if the original filename is temporarily locked. Fallback hash: `4FDFFEA7416F48FF394A87B8092A878F19BF5F3AA87FDC19C29A81CC70C6CEA2`. Restart and check that loading continues; the newest `BattleSister.log` should reach `OnLoadResources` and report successful registration.

## Auxiliary grenade counter displays zero (2026-10-10)

The unit could throw its grenade once and the icon disappeared afterward, but the counter displayed `0` beforehand. Decompiled `ZXSystem_GameLevel.UpdateArmyButtons` passes `_CurrentCommandCenter` to `GetCustomCountValue` even for selected units. Updated the ability to resolve the currently selected Ottermar Auxiliary (including multi-selection) when that argument is not the unit. Build succeeded with zero warnings/errors. Deployed primary and fallback DLL hashes match: `14945A06985AD3A748E7E0163F1347CFB54F7C2F7FC9041957078B1351EA7E00`. Fully restart the game and verify the counter starts at `1` and disappears after the grenade is thrown.
