using ExileCore2;
using ExileCore2.PoEMemory;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Helpers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Vector3N = System.Numerics.Vector3;
using Vector2N = System.Numerics.Vector2;
using ExileCore2.PoEMemory.Elements;
using ExileCore2.Shared.Enums;
using GameOffsets2;

namespace WhereTheWispsAt;

public class WhereTheWispsAt : BaseSettingsPlugin<WhereTheWispsAtSettings>
{
    public enum WispType
    {
        None,
        Rituals,
        Shrine,
        Breach,
        Custom
    }

    public Dictionary<Vector2N, Stopwatch> transitionedBreaches = new();

    public List<string> GoodShrines = new List<string>()
    {
        "Gloom Shrine", "Acceleration Shrine", "Diamond Shrine",
        "Divine Shrine", "Echoing Shrine", "Covetous Shrine" /*, "Impenetrable Shrine"*/
    };

    public List<string> BadShrines = new List<string>()
    {
        "Corrupting Shrine", "Greed Shrine" /*, "Impenetrable Shrine"*/
    };

    public WispData Wisps = new([], [], [], []);

    public WhereTheWispsAt()
    {
        Name = "Where The Wisps At";
    }

    public override bool Initialise()
    {
        Settings.PressMe.OnPressed += () =>
        {
            var t = typeof(PathfindingComponentOffsets);
            foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var offset = Marshal.OffsetOf(t, field.Name);
                DebugWindow.LogMsg($"{field.Name} offset: {offset:X}");
            }
        };
        return true;
    }

    private float breachDuration => 6000.0f / (100 + breachFasterModifier);

    private float breachFasterModifier =>
        GameController.IngameState.Data.MapStats.ContainsKey(GameStat.MapBreachTimePassedPct)
            ? GameController.IngameState.Data.MapStats[GameStat.MapBreachTimePassedPct]
            : 0;

    private int totalBreachCount = 0;
    private int transitionedBreachCount = 0;

    public override void Tick()
    {
        Wisps.Shrines.RemoveAll(s => !s.IsTargetable);
        var breachesToRemove = new List<uint>();

        foreach (var breach in Wisps.Breaches.Where(b =>
                     b.IsTransitioned && !transitionedBreaches.ContainsKey(b.GridPos)))
        {
            transitionedBreachCount++;
            transitionedBreaches[breach.GridPos] = Stopwatch.StartNew();
            breachesToRemove.Add(breach.Id);
        }


        Wisps.Breaches.RemoveAll(b => breachesToRemove.Contains(b.Id) /*||b.DistancePlayer<120&&!b.IsValid*/);
        var expiredBreaches = transitionedBreaches
            .Where(pair => pair.Value.Elapsed.TotalSeconds >= breachDuration)
            .Select(pair => pair.Key)
            .ToList();

        foreach (var breach in expiredBreaches)
        {
            transitionedBreaches.Remove(breach);
        }

        //var count = Wisps.Breaches.RemoveAll(s => s.IsTransitioned);
        Wisps.Altars.RemoveAll(s => s.TryGetComponent(out StateMachine m) && m.States[0].Value >= 2);

        /*var altarsToRemove = Wisps.Altars.Where(
                altar => altar.TryGetComponent<StateMachine>(out var stateComp) &&
                         stateComp?.States.Any(x => x.Name == "activated" && x.Value == 1) == true
            )
            .ToList();

        altarsToRemove.ForEach(altar => RemoveEntityFromList(altar, Wisps.Altars));*/
    }

    public override void EntityAdded(Entity entity)
    {
        var path = entity.TryGetComponent<Animated>(out var animatedComp)
            ? animatedComp?.BaseAnimatedObjectEntity?.Path
            : null;

        //if (entity.Metadata.Contains("Volatile"))
        //{
        //    LogMessage($"Volatile");
        //    foreach (var item in entity.Stats)
        //    {
        //        LogMessage($"{item.Key} - {item.Value}");
        //    }
        //    LogMessage($"Buffs");
        //    foreach (var item in entity.Buffs)
        //    {
        //        LogMessage($"{item.DisplayName} - {item.Name}");
        //    }
        //}

        var metadata = entity.Metadata;

        switch (metadata)
        {
            case "Metadata/Terrain/Leagues/Ritual/RitualRuneInteractable":
                Wisps.Altars.Add(entity);
                break;
            //case not null when metadata.Contains("Azmeri/AzmeriDustConverter"):
            //   Wisps.DustConverters.Add(entity);
            //  break;
            case "Metadata/Shrines/Shrine":
                //if (GoodShrines.Contains(entity.RenderName)||BadShrines.Contains(entity.RenderName))
            {
                Wisps.Shrines.Add(entity);
            }

                break;
            case "Metadata/MiscellaneousObjects/Breach/BreachObject":
                Wisps.Breaches.Add(entity);
                DebugWindow.LogMsg($"Breach id: {entity.Id} ({entity.GridPos})");
                totalBreachCount++;
                break;
        }

        //Spectral Leader t17
        //"Metadata/Monsters/WarHero/WarHeroCasterAtlasUber"

        //Heretical Guardian ruined/ravaged/torched/desecr
        //Metadata/Monsters/ReligiousTemplar/ReligiousTemplarTwoHandedKitavaHellscape_
        //Metadata/Monsters/ReligiousTemplar/ReligiousTemplarTwoHandedKitava

        //Pale seraphim
        //Metadata/Monsters/LeagueHellscape/PaleFaction/HellscapePaleElite2
        //Metadata/Monsters/LeagueHellscape/PaleFaction/HellscapePaleElite2Standalone_
        //Metadata/Monsters/LeagueHellscape/PaleFaction/HellscapePaleElite2Standalone_
        //	Metadata/Monsters/LeagueHellscape/PaleFaction/HellscapePaleElite2Spectre
        //Metadata/Monsters/LeagueKalguur/PaleFaction/DemonCopperPaleElite2

        if (!string.IsNullOrEmpty(Settings.CustomMetadata?.Value))
        {
            var split = Settings.CustomMetadata.Value.Split(',');
            if (split.Contains(metadata))
            {
                Wisps.Custom.Add(entity);
            }
        }
    }

    public override void EntityRemoved(Entity entity)
    {
        /*new[]
            {
                Wisps.Altars, Wisps.Breaches, Wisps.Shrines, Wisps.Custom
            }.ToList()
            .ForEach(list => RemoveEntityFromList(entity, list));

        Wisps.Encounters.Remove(entity);*/
    }

    private static void RemoveEntityFromList(Entity entity, List<Entity> list)
    {
        var entityToRemove = list.FirstOrDefault(wisp => wisp.Id == entity.Id);

        if (entityToRemove != null)
        {
            list.Remove(entityToRemove);
        }
    }

    public override void AreaChange(AreaInstance area)
    {
        Wisps = new WispData([], [], [], []);
        transitionedBreaches.Clear();
        totalBreachCount = 0;
        transitionedBreachCount = 0;
    }

    public override void Render()
    {
        if (!Settings.Enable.Value || !GameController.InGame)
        {
            return;
        }

        var inGameUi = GameController.Game.IngameState.IngameUi;
        var offsetY = 0;
        Graphics.DrawText($"Breaches: {transitionedBreachCount}/{totalBreachCount}",
            new Vector2N(Settings.BreachX.Value, Settings.BreachY.Value));
        offsetY += 20;
        foreach (var breach in transitionedBreaches)
        {
            //Graphics.DrawCircleOnLargeMap(breach.Key, true, Settings.BreachRadius.Value, Color.Purple,5,30);
            //var z = GameController.IngameState.Data.RawTerrainHeightData[(int)breach.Key.Y][(int)breach.Key.X];
            var z = 0;
            Graphics.DrawCircleInWorld(breach.Key.GridToWorld(z), Settings.BreachRadius.Value / 0.092f, Color.HotPink,
                15, 30);


            var time = (float)breach.Value.Elapsed.TotalSeconds;
            var k = Settings.BreachK.Value / 1000f;
            var tfast = Settings.BreachTfast.Value / 1000f;
            var maxradius = Settings.BreachRadius.Value;
            var rfast = tfast * maxradius / breachDuration *
                        k;
            var isShortBreach = time <= tfast;

            var radius = isShortBreach
                ? time * k * maxradius / breachDuration
                : rfast + (maxradius - rfast) * (time - tfast) / (breachDuration - tfast);
            if (Settings.DrawBreachExpand.Value)
            {
                Graphics.DrawCircleInWorld(breach.Key.GridToWorld(z), radius / 0.092f, Color.White, 10, 30);
            }

            Graphics.DrawText($"{breachDuration - breach.Value.Elapsed.TotalSeconds}",
                new Vector2N(Settings.BreachX.Value, Settings.BreachY.Value + offsetY));
            offsetY += 20;
        }

        if (!Settings.IgnoreFullscreenPanels && inGameUi.FullscreenPanels.Any(x => x.IsVisible))
        {
            return;
        }

        if (!Settings.IgnoreLargePanels && inGameUi.LargePanels.Any(x => x.IsVisible))
        {
            return;
        }

        foreach (var (list, color, size, text, type) in new[]
                 {
                     (Wisps.Altars, Settings.Rituals.Value, 0, "Ritual", WispType.Rituals),
                     (Wisps.Shrines.Where(s => GoodShrines.Contains(s.RenderName)).ToList(), Settings.BlueWisp.Value, 0,
                         string.Empty, WispType.Shrine),
                     (Wisps.Shrines.Where(s => BadShrines.Contains(s.RenderName)).ToList(), Color.Red, 0,
                         string.Empty, WispType.Shrine),
                     (Wisps.Shrines.Where(s => !GoodShrines.Contains(s.RenderName) && !BadShrines.Contains(s.RenderName)).ToList(),
                         Color.Gray, 0,
                         string.Empty, WispType.Shrine),
                     (Wisps.Breaches, Settings.Breach.Value, 0, "Breach", WispType.Breach),
                     (Wisps.Custom, Settings.Dealer.Value, 0, "Spectre", WispType.Custom),
                 })
            DrawWisps(list, color, size, text, type);

        return;


        void DrawWisps(List<Entity> entityList, Color color, int size, string text, WispType type = WispType.None)
        {
            // Just run this once, land looks flat.
            var groundZ = entityList.FirstOrDefault()?.GridPos is { } gridPosNum
                ? GameController.IngameState.Data.GetTerrainHeightAt(gridPosNum)
                : 0;

            entityList = entityList.OrderBy(x => x.Id).ToList();

            var screenSize = new RectangleF
            {
                X = 0,
                Y = 0,
                Width = GameController.Window.GetWindowRectangleTimeCache.Size.X,
                Height = GameController.Window.GetWindowRectangleTimeCache.Size.Y
            };

            for (var i = 0; i < entityList.Count; i++)
            {
                var entityCur = entityList[i];

                if (entityCur.IsTransitioned)
                {
                    continue;
                }

                if (type.Equals(WispType.Custom))
                {
                    text = entityCur.Metadata[(entityCur.Metadata.LastIndexOf('/') + 1)..];
                }

                var actualSize = size;

                if (Settings.DrawMap && GameController.IngameState.IngameUi.Map.LargeMap.IsVisibleLocal)
                {
                    var mapPos = GameController.IngameState.Data.GetGridMapScreenPosition(
                        entityCur.Pos.WorldToGrid()
                    );

                    if (text != null)
                    {
                        const int widthPadding = 3;
                        if (string.IsNullOrEmpty(text))
                        {
                            text = entityCur.RenderName;
                        }

                        var boxOffset = Graphics.MeasureText(text) / 2f;
                        var textOffset = boxOffset;
                        boxOffset.X += widthPadding;
                        Graphics.DrawBox(mapPos - boxOffset, mapPos + boxOffset, Color.Black);
                        Graphics.DrawText(text, mapPos - textOffset, color);
                    }
                    else
                    {
                        Graphics.DrawCircleFilled(mapPos, actualSize, color, 8);
                    }
                }

                /*if (!Settings.DrawWispsOnGround)
                {
                    continue;
                }

                var entityPos = entityCur.Pos;
                var entityPosScreen = RemoteMemoryObject.TheGame.IngameState.Camera.WorldToScreen(entityPos);

                if (IsEntityWithinScreen(entityPosScreen, screenSize, 50))
                {
                    Graphics.DrawBoundingBoxInWorld(
                        entityPos with
                        {
                            Z = groundZ
                        },
                        color with
                        {
                            //A = (byte)Settings.WispsOnGroundAlpha
                        },
                        new Vector3N(
                            Settings.WispsOnGroundWidth,
                            Settings.WispsOnGroundWidth,
                            Settings.WispsOnGroundHeight
                        ),
                        0f
                    );
                }*/
            }
        }
    }

    private static bool IsEntityWithinScreen(Vector2N entityPos, RectangleF screensize, float allowancePX)
    {
        // Check if the entity position is within the screen bounds with allowance
        var leftBound = screensize.Left - allowancePX;
        var rightBound = screensize.Right + allowancePX;
        var topBound = screensize.Top - allowancePX;
        var bottomBound = screensize.Bottom + allowancePX;

        return entityPos.X >= leftBound && entityPos.X <= rightBound && entityPos.Y >= topBound &&
               entityPos.Y <= bottomBound;
    }

    public record WispData(
        List<Entity> Altars,
        List<Entity> Shrines,
        List<Entity> Breaches,
        List<Entity> Custom);
}