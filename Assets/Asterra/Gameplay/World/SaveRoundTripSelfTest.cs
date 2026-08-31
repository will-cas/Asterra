using System.IO;
using System.Text;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Save;
using Asterra.Gameplay.Sim;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Sim capture / restore and JSON save round-trip (no MatchBootstrap).</summary>
    public static class SaveRoundTripSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "capture units and buildings", CaptureEntities());
            Expect(ref fails, sb, "restore restores positions", RestorePositions());
            Expect(ref fails, sb, "restore wallets via data", CaptureWalletsInData());
            Expect(ref fails, sb, "json write read roundtrip", JsonRoundTrip());
            Expect(ref fails, sb, "restore upgrades and powers", RestoreTech());
            Expect(ref fails, sb, "bad path load fails", BadPathFails());
            Expect(ref fails, sb, "format version present", FormatVersionSet());

            sb.Append(fails == 0 ? "SaveRoundTripSelfTest: OK" : $"SaveRoundTripSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool CaptureEntities()
        {
            var sim = BuildWorld(out _, out _, out var p);
            var data = new MatchSaveData { formatVersion = OfflineMatchSaveService.CurrentFormatVersion };
            sim.CaptureInto(data);
            return data.units != null
                   && data.units.Length >= 1
                   && data.buildings != null
                   && data.buildings.Length >= 1
                   && data.resources != null
                   && data.resources.Length >= 1;
        }

        private static bool RestorePositions()
        {
            var sim = BuildWorld(out var ids, out var wallet, out var p);
            var unitX = 0f;
            for (int i = 0; i < sim.Units.Count; i++)
            {
                if (sim.Units[i].Owner == p)
                {
                    unitX = sim.Units[i].X;
                    break;
                }
            }

            var data = new MatchSaveData { formatVersion = OfflineMatchSaveService.CurrentFormatVersion };
            sim.CaptureInto(data);

            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim2 = new SkirmishWorldSim(wallet, ids, defs);
            sim2.RestoreFrom(data);
            for (int i = 0; i < sim2.Units.Count; i++)
            {
                if (sim2.Units[i].Owner == p)
                    return Near(sim2.Units[i].X, unitX, 0.05f);
            }

            return false;
        }

        private static bool CaptureWalletsInData()
        {
            // CaptureInto does not write wallets — OfflineMatchSaveService.Capture does.
            // Verify we can still persist wallets on MatchSaveData manually.
            var data = new MatchSaveData
            {
                formatVersion = 2,
                wallets = new[]
                {
                    new WalletSave { player = 0, gold = 321, timber = 44 },
                },
            };
            string path = Path.Combine(Application.temporaryCachePath, "asterra_test_wallet.json");
            OfflineMatchSaveService.Write(path, data);
            bool ok = OfflineMatchSaveService.TryRead(path, out var loaded)
                      && loaded.wallets != null
                      && loaded.wallets.Length == 1
                      && loaded.wallets[0].gold == 321;
            TryDelete(path);
            return ok;
        }

        private static bool JsonRoundTrip()
        {
            var sim = BuildWorld(out _, out _, out _);
            var data = new MatchSaveData
            {
                formatVersion = OfflineMatchSaveService.CurrentFormatVersion,
                mapKey = MapCatalog.LushForestId,
                playerFaction = 0,
                enemyFaction = 1,
                aiDifficulty = 2,
                tick = 99,
            };
            sim.CaptureInto(data);
            string path = Path.Combine(Application.temporaryCachePath, "asterra_test_roundtrip.json");
            OfflineMatchSaveService.Write(path, data);
            bool ok = OfflineMatchSaveService.TryRead(path, out var loaded)
                      && loaded.mapKey == MapCatalog.LushForestId
                      && loaded.aiDifficulty == 2
                      && loaded.tick == 99
                      && loaded.units.Length == data.units.Length
                      && loaded.buildings.Length == data.buildings.Length;
            TryDelete(path);
            return ok;
        }

        private static bool RestoreTech()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 5000);
            var barracks = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneAcademyId, 0f, 0f, startActive: true);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, -40f, 0f, startActive: true);
            sim.ApplyCommands(new GameCommand[]
            {
                new ChooseUpgradeCommand
                {
                    Issuer = p,
                    BuildingId = barracks.Id,
                    UpgradeDefId = FactionDefaultContent.VeiledMailId,
                },
            });
            for (int i = 0; i < 120; i++)
                sim.Tick(0.25f);
            sim.ApplyCommands(new GameCommand[]
            {
                new UnlockPowerCommand
                {
                    Issuer = p,
                    PowerDefId = FactionDefaultContent.WrathOfSkiesAbilityId,
                },
            });

            var data = new MatchSaveData { formatVersion = OfflineMatchSaveService.CurrentFormatVersion };
            sim.CaptureInto(data);

            var sim2 = new SkirmishWorldSim(new ResourceWallet(), new SequentialIdFactory(), defs);
            sim2.RestoreFrom(data);
            return sim2.HasUpgrade(p, FactionDefaultContent.VeiledMailId)
                   && sim2.HasPower(p, FactionDefaultContent.WrathOfSkiesAbilityId);
        }

        private static bool BadPathFails()
        {
            return !OfflineMatchSaveService.TryRead("/tmp/asterra_no_such_save_file.json", out _);
        }

        private static bool FormatVersionSet()
        {
            var data = new MatchSaveData();
            return data.formatVersion == OfflineMatchSaveService.CurrentFormatVersion
                   || data.formatVersion == 2;
        }

        private static SkirmishWorldSim BuildWorld(
            out SequentialIdFactory ids,
            out ResourceWallet wallet,
            out PlayerId p)
        {
            ids = new SequentialIdFactory();
            wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 200);
            wallet.Seed(p, ResourceType.Timber, 100);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, 0f, 0f, startActive: true);
            sim.SpawnUnit(ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledApprenticeId, 15f, 5f);
            sim.AddResourceNode(ids.Next(), ResourceType.Gold, 400, 30f, 0f);
            return sim;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // ignore
            }
        }

        private static bool Near(float a, float b, float eps)
        {
            float d = a - b;
            return d <= eps && d >= -eps;
        }

        private static void Expect(ref int fails, StringBuilder sb, string name, bool ok)
        {
            if (ok)
                return;
            fails++;
            sb.AppendLine("  fail: " + name);
        }
    }
}
