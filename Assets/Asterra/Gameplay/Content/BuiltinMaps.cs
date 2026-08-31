using Asterra.Core.World;

namespace Asterra.Gameplay.Content
{
    /// <summary>Single source of truth for built-in skirmish layouts (terrain, keeps, links).</summary>
    public static class BuiltinMaps
    {
        public static MapDefinition Definition(SkirmishMapId id)
        {
            switch (id)
            {
                case SkirmishMapId.MundorCapital: return MundorCapital();
                case SkirmishMapId.OutcastCamp: return OutcastCamp();
                case SkirmishMapId.RiverCrossing: return RiverCrossing();
                case SkirmishMapId.FrozenWastes: return FrozenWastes();
                case SkirmishMapId.LushForest: return LushForest();
                case SkirmishMapId.TwinCities: return TwinCities();
                default: return AncientRelic();
            }
        }

        /// <summary>
        /// Island citadel between two north–south rivers. Seat 0 = defender (island).
        /// Seat 1 = west attacker, seat 2 = east attacker. Island defence is 1v2.
        /// </summary>
        public static MapDefinition MundorCapital()
        {
            var def = Base("mundor_capital", "Mundor Capital", 0f, 0f, DefaultTerrainCatalog.GrassShort);
            def.keeps = new[]
            {
                Keep(0, 0f, 0f),
                Keep(1, -340f, 0f),
                Keep(2, 340f, 0f),
            };
            def.terrain = new[]
            {
                Rect(-90, -90, 90, 90, DefaultTerrainCatalog.GrassBare),
                Rect(-40, -40, 40, 40, DefaultTerrainCatalog.Hill),
                Rect(-170, -450, -110, 450, DefaultTerrainCatalog.WaterDeep),
                Rect(110, -450, 170, 450, DefaultTerrainCatalog.WaterDeep),
                Rect(-180, -450, -170, 450, DefaultTerrainCatalog.Beach),
                Rect(-110, -450, -100, 450, DefaultTerrainCatalog.Beach),
                Rect(100, -450, 110, 450, DefaultTerrainCatalog.Beach),
                Rect(170, -450, 180, 450, DefaultTerrainCatalog.Beach),
                Rect(-170, -16, -110, 16, DefaultTerrainCatalog.Beach),
                Rect(-170, 140, -110, 160, DefaultTerrainCatalog.Beach),
                Rect(-170, -160, -110, -140, DefaultTerrainCatalog.Beach),
                Rect(110, -16, 170, 16, DefaultTerrainCatalog.Beach),
                Rect(110, 140, 170, 160, DefaultTerrainCatalog.Beach),
                Rect(110, -160, 170, -140, DefaultTerrainCatalog.Beach),
                Rect(-420, -50, -280, 50, DefaultTerrainCatalog.GrassBare),
                Rect(280, -50, 420, 50, DefaultTerrainCatalog.GrassBare),
                Rect(-280, 80, -180, 200, DefaultTerrainCatalog.Forest),
                Rect(180, -200, 280, -80, DefaultTerrainCatalog.Forest),
                Rect(-100, -20, 100, 20, DefaultTerrainCatalog.Road),
            };
            def.traversalLinks = new[]
            {
                Bridge(-180f, 0f, -100f, 0f),
                Bridge(-180f, 150f, -100f, 150f),
                Bridge(-180f, -150f, -100f, -150f),
                Bridge(100f, 0f, 180f, 0f),
                Bridge(100f, 150f, 180f, 150f),
                Bridge(100f, -150f, 180f, -150f),
            };
            def.territories = new[]
            {
                Territory(-140f, 0f, 28f, 8),
                Territory(140f, 0f, 28f, 8),
            };
            def.destructibles = new[]
            {
                Prop("bridge", -140f, 0f, 0),
                Prop("bridge", 140f, 0f, 3),
                Prop("crumbling_tower", -60f, 70f),
                Prop("crumbling_tower", 70f, -55f),
                Prop("shrine", 0f, 55f),
                Prop("cottage", -50f, -40f),
            };
            Script(
                def,
                Obj("hold_island", "Hold Mundor", "destroy_keeps", true, 0f, 0f, 40f),
                Obj("west_span", "Hold the west span", "optional_hold", false, -140f, 0f, 28f, 60f));
            Talk(def, Line("watch", "Watch", "West bank and east bank. They mean to take Mundor in the dark."));
            return def;
        }

        /// <summary>Host camp packed in the south-west. Seats: 0 SW camp, 1 NE, 2 NW, 3 SE. 1v1 uses 0 vs 1.</summary>
        public static MapDefinition OutcastCamp()
        {
            var def = Base("outcast_camp", "Outcast Camp", -300f, -300f, DefaultTerrainCatalog.GrassLong);
            def.keeps = new[]
            {
                Keep(0, -320f, -320f),
                Keep(1, 300f, 300f),
                Keep(2, -320f, 300f),
                Keep(3, 300f, -320f),
            };
            def.terrain = new[]
            {
                Rect(-420, -420, -180, -180, DefaultTerrainCatalog.GrassBare),
                Rect(-400, -400, -200, -200, DefaultTerrainCatalog.Hill),
                Rect(-360, -360, -280, -280, DefaultTerrainCatalog.GrassBare),
                Rect(-450, -80, -40, 40, DefaultTerrainCatalog.Forest),
                Rect(-80, -450, 40, -40, DefaultTerrainCatalog.Forest),
                Rect(80, 80, 200, 200, DefaultTerrainCatalog.Forest),
                Rect(-40, 40, 40, 120, DefaultTerrainCatalog.Swamp),
                Rect(40, -40, 120, 40, DefaultTerrainCatalog.Swamp),
                Rect(-200, -40, -80, 80, DefaultTerrainCatalog.Tree),
                Rect(160, -200, 240, -80, DefaultTerrainCatalog.Rock),
            };
            def.territories = new[]
            {
                Territory(-80f, -80f, 36f, 8),
                Territory(80f, 80f, 32f, 6),
            };
            def.destructibles = new[]
            {
                Prop("tree", -160f, 0f),
                Prop("tree", 0f, -160f),
                Prop("rock", 200f, -140f),
                Prop("cottage", -260f, -280f),
                Prop("cottage", -280f, -240f),
                Prop("barn", -220f, -300f),
                Prop("shrine", -180f, -220f),
            };
            def.objectives = new[]
            {
                new MapObjective
                {
                    id = "raze_camp",
                    title = "Break the Host camp",
                    kind = "destroy_keeps",
                    required = true,
                    x = -320f,
                    z = -320f,
                    radius = 40f,
                },
                new MapObjective
                {
                    id = "mercy_hold",
                    title = "Hold the approaches (mercy)",
                    kind = "optional_hold",
                    required = false,
                    x = -80f,
                    z = -80f,
                    radius = 36f,
                    holdSeconds = 90f,
                    onCompleteTalkId = "mercy",
                },
            };
            def.conversations = new[]
            {
                new MapConversationLine
                {
                    id = "mercy",
                    speaker = "Courier",
                    text = "They still live. The Crown will remember that you held the field without burning the camp.",
                },
            };
            return def;
        }

        /// <summary>North–south river. West vs east. Fords, one destroyable mid bridge, boats at the mouths.</summary>
        public static MapDefinition RiverCrossing()
        {
            var def = Base("river_crossing", "River Crossing", -300f, 0f, DefaultTerrainCatalog.GrassShort);
            def.keeps = new[]
            {
                Keep(0, -320f, 0f),
                Keep(1, 320f, 0f),
            };
            def.terrain = new[]
            {
                Rect(-32, -450, 32, 450, DefaultTerrainCatalog.WaterDeep),
                Rect(-32, -450, 32, -380, DefaultTerrainCatalog.WaterOcean),
                Rect(-32, 380, 32, 450, DefaultTerrainCatalog.WaterOcean),
                Rect(-32, 160, 32, 220, DefaultTerrainCatalog.WaterFast),
                Rect(-12, -220, 12, -160, DefaultTerrainCatalog.WaterShallow),
                Rect(-12, 80, 12, 120, DefaultTerrainCatalog.WaterShallow),
                Rect(-32, -14, 32, 14, DefaultTerrainCatalog.Beach),
                Rect(-48, -450, -32, 450, DefaultTerrainCatalog.Beach),
                Rect(32, -450, 48, 450, DefaultTerrainCatalog.Beach),
                Rect(-380, -40, -280, 40, DefaultTerrainCatalog.GrassBare),
                Rect(280, -40, 380, 40, DefaultTerrainCatalog.GrassBare),
                Rect(-220, -180, -80, -60, DefaultTerrainCatalog.Forest),
                Rect(80, 60, 220, 180, DefaultTerrainCatalog.Forest),
                Rect(-200, 140, -80, 240, DefaultTerrainCatalog.Swamp),
                Rect(80, -240, 200, -140, DefaultTerrainCatalog.Swamp),
            };
            def.traversalLinks = new[]
            {
                Bridge(-40f, 0f, 40f, 0f, 1.4f, 10f),
                Link(-40f, 200f, 40f, 200f, "magic", 1.1f, 10f),
                Link(-50f, -400f, 0f, -420f, "shore", 1.5f, 12f),
                Link(50f, 400f, 0f, 420f, "shore", 1.5f, 12f),
            };
            def.units = new[]
            {
                Unit(0, "boat", 0f, -400f),
                Unit(1, "boat", 0f, 400f),
                Unit(0, "builder", -280f, 0f),
                Unit(1, "builder", 280f, 0f),
            };
            def.territories = new[] { Territory(0f, 0f, 32f, 10) };
            def.destructibles = new[]
            {
                Prop("bridge", 0f, 0f, 0),
                Prop("mill", -80f, 40f),
                Prop("cottage", 90f, -50f),
                Prop("farm", -120f, -80f),
            };
            Script(
                def,
                Obj("cut_ford", "Cut the ford", "destroy_keeps", true, 0f, 0f, 40f),
                Obj("hold_span", "Hold the wooden span", "optional_hold", false, 0f, 0f, 24f, 75f, "span"));
            Talk(def, Line("span", "Scout", "If the span falls they swim. If it stands, they run."));
            Enter(def, "span", 0f, 0f, 36f);
            return def;
        }

        public static MapDefinition FrozenWastes()
        {
            var def = Base("frozen_wastes", "Frozen Wastes", -280f, 280f, DefaultTerrainCatalog.Snow);
            def.keeps = new[]
            {
                Keep(0, -320f, 300f),
                Keep(1, 320f, -300f),
            };
            def.terrain = new[]
            {
                Rect(-80, -450, 80, 450, DefaultTerrainCatalog.IceThick),
                Rect(-450, -40, 450, 40, DefaultTerrainCatalog.IceThin),
                Rect(-20, -20, 20, 20, DefaultTerrainCatalog.WaterDeep),
                Rect(-360, 240, -240, 360, DefaultTerrainCatalog.GrassBare),
                Rect(240, -360, 360, -240, DefaultTerrainCatalog.GrassBare),
                Rect(-200, 80, -80, 200, DefaultTerrainCatalog.Rock),
                Rect(80, -200, 200, -80, DefaultTerrainCatalog.Rock),
                Rect(-400, -200, -280, -80, DefaultTerrainCatalog.Hill),
                Rect(280, 80, 400, 200, DefaultTerrainCatalog.Hill),
            };
            def.traversalLinks = new[]
            {
                Bridge(-24f, 0f, 24f, 0f, 1.2f, 10f),
            };
            def.territories = new[] { Territory(0f, 0f, 36f, 7) };
            def.destructibles = new[]
            {
                Prop("rock", -140f, 140f),
                Prop("rock", 140f, -140f),
                Prop("crumbling_tower", -80f, 180f),
                Prop("shrine", 90f, -160f),
            };
            Script(
                def,
                Obj("white_march", "End the Host on the ice", "destroy_keeps", true, 0f, 0f, 40f),
                Obj("live_storm", "Weather the white", "survive", false, 0f, 0f, 80f, 90f));
            return def;
        }

        public static MapDefinition LushForest()
        {
            var def = Base("lush_forest", "Greenveil", -300f, 0f, DefaultTerrainCatalog.Forest);
            def.keeps = new[]
            {
                Keep(0, -340f, 0f),
                Keep(1, 340f, 0f),
            };
            def.terrain = new[]
            {
                Rect(-400, -60, -260, 60, DefaultTerrainCatalog.GrassBare),
                Rect(260, -60, 400, 60, DefaultTerrainCatalog.GrassBare),
                Rect(-80, -40, 80, 40, DefaultTerrainCatalog.GrassShort),
                Rect(-200, -200, -40, -40, DefaultTerrainCatalog.GrassLong),
                Rect(40, 40, 200, 200, DefaultTerrainCatalog.GrassLong),
                Rect(-120, -90, -100, -70, DefaultTerrainCatalog.Tree),
                Rect(-125, -95, -95, -65, DefaultTerrainCatalog.Tree),
                Rect(100, 70, 120, 90, DefaultTerrainCatalog.Tree),
                Rect(-60, 80, -20, 140, DefaultTerrainCatalog.Swamp),
                Rect(20, -140, 60, -80, DefaultTerrainCatalog.Swamp),
                Rect(-40, -20, 40, 20, DefaultTerrainCatalog.Road),
            };
            def.traversalLinks = new[]
            {
                Link(-125f, -95f, -95f, -65f, "treegap", 0.7f, 10f),
            };
            def.territories = new[] { Territory(0f, 0f, 40f, 8) };
            def.destructibles = new[]
            {
                Prop("tree", -110f, -80f),
                Prop("tree", 110f, 80f),
                Prop("rock", -40f, 110f),
                Prop("farm", -180f, -120f),
                Prop("farm", 160f, 140f),
                Prop("cottage", -200f, 40f),
                Prop("barn", 180f, -60f),
                Prop("mill", -40f, -140f),
            };
            Script(
                def,
                Obj("first_blood", "Break the pickets", "destroy_keeps", true, 0f, 0f, 40f),
                Obj("swamp_push", "Push the south swamp", "reach", false, 40f, -110f, 32f, 0f, "swamp"));
            Talk(def, Line("swamp", "Scout", "They mean to burn the south swamp behind them. Push it if you can."));
            Enter(def, "swamp", 40f, -110f, 36f);
            return def;
        }

        public static MapDefinition TwinCities()
        {
            var def = Base("twin_cities", "Twin Cities", -280f, 0f, DefaultTerrainCatalog.GrassShort);
            def.keeps = new[]
            {
                Keep(0, -300f, 0f),
                Keep(1, 300f, 0f),
            };
            def.terrain = new[]
            {
                Rect(-420, -180, -140, 180, DefaultTerrainCatalog.GrassBare),
                Rect(140, -180, 420, 180, DefaultTerrainCatalog.GrassBare),
                Rect(-36, -450, 36, 450, DefaultTerrainCatalog.WaterDeep),
                Rect(-50, -450, -36, 450, DefaultTerrainCatalog.Beach),
                Rect(36, -450, 50, 450, DefaultTerrainCatalog.Beach),
                Rect(-36, -200, 36, -176, DefaultTerrainCatalog.Beach),
                Rect(-36, -60, 36, -36, DefaultTerrainCatalog.Beach),
                Rect(-36, 36, 36, 60, DefaultTerrainCatalog.Beach),
                Rect(-36, 176, 36, 200, DefaultTerrainCatalog.Beach),
                Rect(-200, -40, -160, 40, DefaultTerrainCatalog.Road),
                Rect(160, -40, 200, 40, DefaultTerrainCatalog.Road),
                Rect(-380, 80, -220, 160, DefaultTerrainCatalog.Forest),
                Rect(220, -160, 380, -80, DefaultTerrainCatalog.Forest),
            };
            def.traversalLinks = new[]
            {
                Bridge(-44f, -188f, 44f, -188f),
                Bridge(-44f, -48f, 44f, -48f),
                Bridge(-44f, 48f, 44f, 48f),
                Bridge(-44f, 188f, 44f, 188f),
            };
            def.buildings = new[]
            {
                Bld(0, "tower", -180f, 80f),
                Bld(0, "tower", -180f, -80f),
                Bld(1, "tower", 180f, 80f),
                Bld(1, "tower", 180f, -80f),
            };
            def.territories = new[]
            {
                Territory(0f, 48f, 28f, 8),
                Territory(0f, -48f, 28f, 8),
            };
            def.destructibles = new[]
            {
                Prop("bridge", 0f, 48f, 2),
                Prop("bridge", 0f, -48f, 1),
                Prop("farm", -240f, 120f),
                Prop("farm", 240f, -120f),
                Prop("cottage", -220f, -80f),
                Prop("cottage", 220f, 80f),
                Prop("barn", -160f, 140f),
                Prop("mill", 160f, -140f),
            };
            Script(
                def,
                Obj("twin_keeps", "Take the far city", "destroy_keeps", true, 0f, 0f, 40f),
                Obj("mid_span", "Hold a mid span", "optional_hold", false, 0f, 48f, 26f, 70f));
            return def;
        }

        public static MapDefinition AncientRelic()
        {
            var def = Base("ancient_relic", "The Reliquary", 0f, -300f, DefaultTerrainCatalog.GrassShort);
            def.keeps = new[]
            {
                Keep(0, 0f, -340f),
                Keep(1, 0f, 340f),
            };
            def.terrain = new[]
            {
                Rect(-80, -80, 80, 80, DefaultTerrainCatalog.Rock),
                Rect(-50, -50, 50, 50, DefaultTerrainCatalog.Rubble),
                Rect(-18, -18, 18, 18, DefaultTerrainCatalog.GrassBare),
                Rect(-450, -20, -90, 20, DefaultTerrainCatalog.NoEntry),
                Rect(90, -20, 450, 20, DefaultTerrainCatalog.NoEntry),
                Rect(-20, -120, 20, -80, DefaultTerrainCatalog.Hill),
                Rect(-20, 80, 20, 120, DefaultTerrainCatalog.Hill),
                Rect(-200, -200, -80, -80, DefaultTerrainCatalog.Forest),
                Rect(80, 80, 200, 200, DefaultTerrainCatalog.Forest),
                Rect(-40, -40, -24, -24, DefaultTerrainCatalog.NoEntry),
                Rect(24, 24, 40, 40, DefaultTerrainCatalog.NoEntry),
            };
            def.traversalLinks = new[]
            {
                Link(0f, 95f, 0f, 45f, "jumpdown", 0.85f, 10f),
                Link(0f, 45f, 0f, 95f, "jumpup", 1.1f, 10f),
            };
            def.territories = new[] { Territory(0f, 0f, 42f, 12) };
            def.destructibles = new[]
            {
                Prop("rock", -32f, 32f),
                Prop("rock", 32f, -32f),
                Prop("crumbling_tower", -70f, 20f),
                Prop("crumbling_tower", 70f, -20f),
                Prop("shrine", 0f, 0f),
            };
            Script(
                def,
                Obj("end_host", "End it in the ring", "destroy_keeps", true, 0f, 0f, 40f),
                Obj("bowl", "Reach the relic bowl", "reach", false, 0f, 0f, 42f, 0f, "bowl"));
            Talk(def, Line("bowl", "Marshal", "The bowl. Jump down if you must. End it in the ring."));
            Enter(def, "bowl", 0f, 0f, 48f);
            return def;
        }

        private static MapDefinition Base(string id, string name, float camX, float camZ, ushort ground)
        {
            return new MapDefinition
            {
                id = id,
                displayName = name,
                defaultTerrain = ground,
                cameraFocusX = camX,
                cameraFocusZ = camZ,
            };
        }

        private static MapKeepSpawn Keep(int seat, float x, float z) =>
            new MapKeepSpawn { seatIndex = seat, x = x, z = z };

        private static MapTerrainPaint Rect(float minX, float minZ, float maxX, float maxZ, ushort t) =>
            new MapTerrainPaint
            {
                shape = "rect",
                minX = minX,
                minZ = minZ,
                maxX = maxX,
                maxZ = maxZ,
                terrainIndex = t,
            };

        private static MapTraversalLink Bridge(float x0, float z0, float x1, float z1, float dur = 1.25f, float approach = 8f) =>
            Link(x0, z0, x1, z1, "bridge", dur, approach);

        private static MapTraversalLink Link(
            float x0, float z0, float x1, float z1, string type, float dur, float approach) =>
            new MapTraversalLink
            {
                startX = x0,
                startZ = z0,
                endX = x1,
                endZ = z1,
                type = type,
                durationSeconds = dur,
                approachRadius = approach,
                enabled = true,
            };

        private static MapTerritory Territory(float x, float z, float r, int gold) =>
            new MapTerritory { x = x, z = z, radius = r, goldPerSecond = gold };

        private static MapDestructible Prop(string id, float x, float z, int linkIndex = -1) =>
            new MapDestructible { catalogId = id, x = x, z = z, linkedTraversalLinkId = linkIndex };

        private static MapUnitSpawn Unit(int seat, string role, float x, float z) =>
            new MapUnitSpawn { seatIndex = seat, role = role, x = x, z = z };

        private static MapBuildingSpawn Bld(int seat, string role, float x, float z) =>
            new MapBuildingSpawn { seatIndex = seat, role = role, x = x, z = z };

        private static MapObjective Obj(
            string id, string title, string kind, bool required,
            float x, float z, float radius, float holdSeconds = 0f, string talkId = null) =>
            new MapObjective
            {
                id = id,
                title = title,
                kind = kind,
                required = required,
                x = x,
                z = z,
                radius = radius,
                holdSeconds = holdSeconds,
                onCompleteTalkId = talkId ?? string.Empty,
            };

        private static MapConversationLine Line(string id, string speaker, string text) =>
            new MapConversationLine { id = id, speaker = speaker, text = text };

        private static void Script(MapDefinition def, params MapObjective[] objectives)
        {
            def.objectives = objectives;
        }

        private static void Talk(MapDefinition def, params MapConversationLine[] lines)
        {
            if (def.conversations == null || def.conversations.Length == 0)
            {
                def.conversations = lines;
                return;
            }

            var merged = new MapConversationLine[def.conversations.Length + lines.Length];
            def.conversations.CopyTo(merged, 0);
            lines.CopyTo(merged, def.conversations.Length);
            def.conversations = merged;
        }

        private static void Enter(MapDefinition def, string conversationId, float x, float z, float radius)
        {
            var trigger = new MapTalkTrigger
            {
                conversationId = conversationId,
                when = "enter",
                x = x,
                z = z,
                radius = radius,
            };
            if (def.talkTriggers == null || def.talkTriggers.Length == 0)
            {
                def.talkTriggers = new[] { trigger };
                return;
            }

            var merged = new MapTalkTrigger[def.talkTriggers.Length + 1];
            def.talkTriggers.CopyTo(merged, 0);
            merged[def.talkTriggers.Length] = trigger;
            def.talkTriggers = merged;
        }
    }
}
