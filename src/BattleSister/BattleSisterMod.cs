using AX.ModLoader;
using DXVision;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Entity_Registry_Tools;
using ZX;
using ZX.Commands;
using ZX.Components;
using ZX.Entities;

namespace BattleSisterMod
{
    public sealed class BattleSister : ShooterSoldier
    {
        public BattleSister() { }

        public override DXProjectImage GetPortraitImage()
        {
            return Images.Portraits.get__Soldier();
        }
    }

    public sealed class BattleSisterAttack : ZXCommand
    {
        protected override void OnPerformEffect(ZXEntity actor, ZXCommandTarget target)
        {
            base.OnPerformEffect(actor, target);
        }
    }

    public sealed class BattleSisterAttackVeteran : ZXCommand
    {
        protected override void OnPerformEffect(ZXEntity actor, ZXCommandTarget target)
        {
            base.OnPerformEffect(actor, target);
        }
    }

    public sealed class ModEntry : IModEntry
    {
        private const string HarmonyId = "local.battlesister.aura";
        private Harmony _harmony;

        private readonly EntityRegistry.EntityData _entityData = new EntityRegistry.EntityData
        {
            entityName = "BattleSister",
            spriteAtlasEntityName = "HumansA",
            spriteAtlasAnimationName = "Ranger",
            useSpriteParameters = true
        };

        private readonly EntityRegistry.EntityCommandData _commandData = new EntityRegistry.EntityCommandData
        {
            BaseEntityType = typeof(Ranger),
            ModdedEntityType = typeof(BattleSister),
            AttackCommandType = typeof(BattleSisterAttack),
            VeteranAttackCommandType = typeof(BattleSisterAttackVeteran),
            TechCommandType = typeof(EntityRegistry.NoCommand)
        };

        private readonly EntityRegistry.DefaultColorData _color = new EntityRegistry.DefaultColorData
        {
            colorToReplace = Color.White,
            colorToChangeTo = Color.White,
            factorTolerance = 0,
            saturation = 1f,
            brightness = 1f,
            contrast = 1f,
            hueRotation = 0,
            colorHue = Color.Firebrick,
            changePortraitColors = true
        };

        public override void OnLoad(Mod mod) { }

        public override void OnLoadResources()
        {
            ZXCommand sisterAttack;
            ZXCommand sisterVeteranAttack;
            EntityRegistry.RegisterEntity(_entityData, _commandData, out sisterAttack, out sisterVeteranAttack, null, _color);

            ConfigureRuntimeValues(sisterAttack, sisterVeteranAttack);

            DXEntityTemplate template = DXProject.Current.EntityTemplates.Values
                .FirstOrDefault(t => t.ClassType == typeof(BattleSister));
            if (template != null)
                template.Entity.AddComponent(new BattleSisterAura());

            InstallAuraPatches();
            DXLog.Write("BattleSister registered with red tint and Ranger life aura.");
        }

        public override void OnUnload()
        {
            if (_harmony != null)
                _harmony.UnpatchAll(HarmonyId);
            AuraState.Clear();
        }

        private static void ConfigureRuntimeValues(ZXCommand attack, ZXCommand veteranAttack)
        {
            DXEntityTemplate rangerTemplate = DXProject.Current.EntityTemplates.Values
                .FirstOrDefault(t => t.ClassType == typeof(Ranger));
            DXEntityTemplate sisterTemplate = DXProject.Current.EntityTemplates.Values
                .FirstOrDefault(t => t.ClassType == typeof(BattleSister));
            ZXEntity ranger = rangerTemplate == null ? null : rangerTemplate.Entity as ZXEntity;
            ZXEntity sister = sisterTemplate == null ? null : sisterTemplate.Entity as ZXEntity;
            if (ranger == null || sister == null || ranger.get_Params() == null)
            {
                DXLog.Write("BattleSister: Ranger or BattleSister template parameters were unavailable.");
                return;
            }

            ZXEntityDefaultParams unitValues = CopyProperties(ranger.get_Params(), new ZXEntityDefaultParams("BattleSister"));
            unitValues.Name = "BattleSister";
            unitValues.Life = ranger.get_Params().Life * 5;
            unitValues.WatchRange = ranger.get_Params().WatchRange + 1;
            unitValues.GoldCost = ranger.get_Params().GoldCost + 100;
            unitValues.IronCost = 10;
            unitValues.AttackCommandBasic = attack;
            unitValues.AttackCommandVeteran = veteranAttack;
            unitValues.PanelPosition = FindFreeSoldiersCenterPanelPosition();
            AddEntityDefaults("BattleSister", unitValues);
            sister.SetParams();

            CopyRangerAttackValues(attack, "BattleSisterAttack", 1.5f);
            CopyRangerAttackValues(veteranAttack, "BattleSisterAttackVeteran", 1.5f);

            RegisterTrainCommand();
        }

        private static int FindFreeSoldiersCenterPanelPosition()
        {
            HashSet<int> used = new HashSet<int>(ZXGame.get_Current().DicDefaultParamsPerTemplate.Values
                .Where(p => p.BuildingFrom == typeof(SoldiersCenter))
                .Select(p => p.PanelPosition));
            for (int slot = 1; slot <= 14; slot++)
                if (slot != 11 && !used.Contains(slot))
                    return slot;
            DXLog.Write("BattleSister: no free SoldiersCenter panel position was found; retaining the Ranger panel position.");
            ZXEntityDefaultParams ranger = ZXGame.get_Current().GetParamsFromEntityType(typeof(Ranger));
            return ranger == null ? 0 : ranger.PanelPosition;
        }

        private static void CopyRangerAttackValues(ZXCommand target, string id, float damageFactor)
        {
            if (target == null)
                return;

            ZXCommand baseCommand = ZXGame.get_Current().Commands
                .FirstOrDefault(c => c.GetType() == (id == "BattleSisterAttack"
                    ? typeof(RangerAttack) : typeof(RangerAttackVeteran)));
            ZXCommandDefaultParams source = baseCommand == null ? null : baseCommand.get_Params();
            if (source == null)
                return;

            ZXCommandDefaultParams values = CopyProperties(source, new ZXCommandDefaultParams(id));
            values.Damage = (int)Math.Round(source.Damage * damageFactor, MidpointRounding.AwayFromZero);
            values.ActionRange = source.ActionRange + 1f;
            values.AvailableFor = new List<Type> { typeof(BattleSister) };
            AddCommandDefaults(id, values);
            target.set_Params(values);
        }

        private static void RegisterTrainCommand()
        {
            // EntityRegistry.RegisterEntity already creates a Train command for every registered Unit.
            // Update that exact command; adding a second Train instance for the same unit leaves the
            // registry-created one with the unconfigured defaults used by the barracks UI.
            Train train = Train.For(typeof(BattleSister));
            Train rangerTrain = Train.For(typeof(Ranger));
            if (train == null)
            {
                train = new Train(typeof(BattleSister));
                train.ReadParams();
                ZXGame.get_Current().Commands.Add(train);
            }

            if (train.get_Params() == null && rangerTrain != null && rangerTrain.get_Params() != null)
                train.set_Params(CopyProperties(rangerTrain.get_Params(), new ZXCommandDefaultParams("Train")));

            if (train.get_Params() == null)
            {
                DXLog.Write("BattleSister: registered Train command has no parameters; barracks button cannot be configured.");
                return;
            }

            List<Type> availableFor = train.get_Params().AvailableFor;
            if (availableFor == null)
                availableFor = new List<Type>();
            if (!availableFor.Contains(typeof(SoldiersCenter)))
                availableFor.Add(typeof(SoldiersCenter));
            train.get_Params().AvailableFor = availableFor;
            train.get_Params().ShowAsCommand = true;
            if (rangerTrain != null && rangerTrain.get_Params() != null)
                train.get_Params().Order = rangerTrain.get_Params().Order;

            DXLog.Write("BattleSister: configured registered Train command. BuildingRequisite=" +
                (train.Entity == null || train.Entity.get_Params() == null
                    ? "unresolved"
                    : (train.Entity.get_Params().BuildingRequisite == null
                        ? "none"
                        : train.Entity.get_Params().BuildingRequisite.Name)) +
                "; AvailableFor=" + string.Join(",", train.get_Params().AvailableFor.Select(t => t.Name).ToArray()));
        }

        private static T CopyProperties<T>(T source, T destination) where T : class
        {
            foreach (PropertyInfo property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length != 0)
                    continue;
                try { property.SetValue(destination, property.GetValue(source, null), null); }
                catch { }
            }
            return destination;
        }

        private static void AddEntityDefaults(string id, ZXEntityDefaultParams values)
        {
            ZXEntityDefaultParams record = new ZXEntityDefaultParams(id);
            record.DefaultValues = values;
            AddDefaultRecord(id, record);
            ZXGame.get_Current().DicDefaultParamsPerTemplate[id] = record;
        }

        private static void AddCommandDefaults(string id, ZXCommandDefaultParams values)
        {
            ZXCommandDefaultParams record = new ZXCommandDefaultParams(id);
            record.DefaultValues = values;
            AddDefaultRecord(id, record);
        }

        private static void AddDefaultRecord(string id, ZXDefaultParams record)
        {
            List<ZXDefaultParams> all = ZXDefaultParams.get_All();
            for (int i = all.Count - 1; i >= 0; --i)
                if (all[i].ID == id)
                    all.RemoveAt(i);
            all.Add(record);
        }

        private void InstallAuraPatches()
        {
            _harmony = new Harmony(HarmonyId);
            MethodInfo damage = AccessTools.Method(typeof(CLife), "AddDamage",
                new[] { typeof(int), typeof(bool), typeof(ZXDamageType) });
            MethodInfo attack = AccessTools.Method(typeof(ZXCommand), "PerformAttack",
                new[] { typeof(ZXEntity), typeof(ZXEntity), typeof(PointF) });
            _harmony.Patch(damage, prefix: new HarmonyMethod(typeof(AuraPatches), "BeforeDamage"));
            _harmony.Patch(attack, prefix: new HarmonyMethod(typeof(AuraPatches), "BeforeAttack"),
                finalizer: new HarmonyMethod(typeof(AuraPatches), "RestoreAttackDamage"));

            MethodInfo setCurrentLevel = AccessTools.Method(typeof(ZXLevelState), "SetCurrentLevel",
                new[] { typeof(DXLevel) });
            _harmony.Patch(setCurrentLevel, postfix: new HarmonyMethod(typeof(CampaignStarter), "AfterSetCurrentLevel"));
        }
    }

    internal static class CampaignStarter
    {
        internal static void AfterSetCurrentLevel(DXLevel level)
        {
            try
            {
                ZXMission mission = ZXMission.get_Current();
                ZXCampaign campaign = ZXCampaign.get_Current();
                if (level == null || mission == null || campaign == null || campaign.InitialMission == null ||
                    mission.ID != campaign.InitialMission.ID)
                    return;

                if (level.Entities.OfType<BattleSister>().Any())
                    return;

                CommandCenter commandCenter = level.Entities.OfType<CommandCenter>().FirstOrDefault();
                DXEntityTemplate template = DXProject.Current.EntityTemplates.Values
                    .FirstOrDefault(t => t.ClassType == typeof(BattleSister));
                if (commandCenter == null || template == null)
                {
                    DXLog.Write("BattleSister: unable to grant campaign starter unit (missing command center or template).");
                    return;
                }

                Point origin = commandCenter.Cell;
                for (int radius = 3; radius <= 7; radius++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int dy = radius - Math.Abs(dx);
                        Point[] candidates = dy == 0
                            ? new[] { new Point(origin.X + dx, origin.Y) }
                            : new[] { new Point(origin.X + dx, origin.Y + dy), new Point(origin.X + dx, origin.Y - dy) };
                        foreach (Point cell in candidates)
                        {
                            if (!level.IsCellInPlayableArea(cell) || level.Entities.Any(e => e.MaxCellArea.Contains(cell)))
                                continue;

                            ZXEntity sister = level.CreateEntity(template, cell) as ZXEntity;
                            if (sister != null)
                            {
                                sister.Team = ZXTeamType.Player;
                                DXLog.Write("BattleSister: granted one starter unit in the campaign's initial mission.");
                            }
                            return;
                        }
                    }
                }
                DXLog.Write("BattleSister: no free tile near the command center for the campaign starter unit.");
            }
            catch (Exception ex)
            {
                DXLog.Write("BattleSister: starter unit grant failed: " + ex);
            }
        }
    }

    public sealed class BattleSisterAura : ZXComponent
    {
        private const int ScanIntervalMs = 250;
        private int _nextScan;
        private readonly HashSet<ulong> _previousTargets = new HashSet<ulong>();

        public override string Description { get { return "Grants nearby Rangers temporary life and attack damage."; } }
        public override DXTypeOfComponentAvailability Availability { get { return DXTypeOfComponentAvailability.OnlyInGame; } }

        protected override void OnEntityAttached()
        {
            base.Entity.EventOnUpdate += ScanAura;
        }

        private void ScanAura()
        {
            int now = DXGame.Current.GameTimeInt;
            if (now < _nextScan) return;
            _nextScan = now + ScanIntervalMs;

            ZXEntity sister = Entity as ZXEntity;
            if (sister == null || sister.get_CLife() == null || !sister.get_CLife().IsAlive)
            {
                AuraState.RemoveSource(Entity.ID);
                _previousTargets.Clear();
                return;
            }

            HashSet<ulong> current = new HashSet<ulong>();
            List<DXEntity> nearby = DXWorldGrid.GetEntitiesInCircle(sister.Position, 6f, (uint)ZXBodyMask.Empire);
            if (nearby != null)
            {
                foreach (DXEntity entity in nearby)
                {
                    ZXEntity ranger = entity as ZXEntity;
                    if (ranger != null && ranger.GetType() == typeof(Ranger) && ranger.get_CLife() != null && ranger.get_CLife().IsAlive)
                        current.Add(ranger.ID);
                }
            }

            AuraState.UpdateSource(Entity.ID, _previousTargets, current);
            _previousTargets.Clear();
            foreach (ulong id in current) _previousTargets.Add(id);
        }

        protected override void OnEntityDetached()
        {
            base.OnEntityDetached();
            Entity.EventOnUpdate -= ScanAura;
            AuraState.RemoveSource(Entity.ID);
            _previousTargets.Clear();
        }
    }

    internal static class AuraState
    {
        private static readonly Dictionary<ulong, HashSet<ulong>> Sources = new Dictionary<ulong, HashSet<ulong>>();
        private static readonly Dictionary<ulong, int> Shield = new Dictionary<ulong, int>();

        internal static bool HasAura(ulong rangerId) { return Sources.ContainsKey(rangerId); }

        internal static void UpdateSource(ulong sourceId, HashSet<ulong> previous, HashSet<ulong> current)
        {
            foreach (ulong id in previous)
                if (!current.Contains(id)) RemoveTargetSource(id, sourceId);
            foreach (ulong id in current)
            {
                HashSet<ulong> sources;
                if (!Sources.TryGetValue(id, out sources))
                {
                    sources = new HashSet<ulong>();
                    Sources[id] = sources;
                    Shield[id] = 100;
                }
                sources.Add(sourceId);
            }
        }

        internal static void RemoveSource(ulong sourceId)
        {
            foreach (ulong id in Sources.Keys.ToArray())
                RemoveTargetSource(id, sourceId);
        }

        private static void RemoveTargetSource(ulong rangerId, ulong sourceId)
        {
            HashSet<ulong> sources;
            if (!Sources.TryGetValue(rangerId, out sources)) return;
            sources.Remove(sourceId);
            if (sources.Count == 0)
            {
                Sources.Remove(rangerId);
                Shield.Remove(rangerId);
            }
        }

        internal static int Absorb(ulong rangerId, int incoming)
        {
            int shield;
            if (incoming <= 0 || !Shield.TryGetValue(rangerId, out shield)) return 0;
            int absorbed = Math.Min(shield, incoming);
            Shield[rangerId] = shield - absorbed;
            return absorbed;
        }

        internal static void Clear()
        {
            Sources.Clear();
            Shield.Clear();
        }
    }

    internal static class AuraPatches
    {
        internal static bool BeforeDamage(CLife __instance, ref int __0)
        {
            ZXEntity target = __instance.Entity as ZXEntity;
            if (target != null && target.GetType() == typeof(Ranger))
                __0 -= AuraState.Absorb(target.ID, __0);
            return true;
        }

        internal static void BeforeAttack(ZXCommand __instance, ZXEntity actor, ref int __state)
        {
            __state = -1;
            if (actor == null || actor.GetType() != typeof(Ranger) || !AuraState.HasAura(actor.ID)) return;
            ZXCommandDefaultParams parameters = __instance.get_Params();
            if (parameters == null) return;
            __state = parameters.Damage;
            parameters.Damage = (int)Math.Round(__state * 1.2f, MidpointRounding.AwayFromZero);
        }

        internal static Exception RestoreAttackDamage(ZXCommand __instance, int __state, Exception __exception)
        {
            if (__state >= 0 && __instance.get_Params() != null)
                __instance.get_Params().Damage = __state;
            return __exception;
        }
    }
}
