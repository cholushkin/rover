using Game.Gameplay.IslandExploration.Generation;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.IslandExploration
{
    // Not an NUnit EditMode test: a Game.Tests asmdef would compile before the default assembly and
    // could never see TerraxelGenerated/RoverIslandGenerationSettings (both default-assembly types), so this
    // is a plain static entry point instead, runnable headlessly via:
    //   Unity.exe -batchmode -nographics -quit -projectPath <proj> -executeMethod Game.Editor.IslandExploration.RoverIslandValidationRunner.RunValidation
    // Logs PASS/FAIL per check to the Editor log; does not throw or force a nonzero exit code itself.
    public static class RoverIslandValidationRunner
    {
        private const string SettingsPath = "Assets/Game/DataAndSettings/IslandExploration/RoverIslandSettings.asset";

        [MenuItem("Rover/Island/Run Validation Checks")]
        public static void RunValidation()
        {
            RoverIslandGenerationSettings settings = AssetDatabase.LoadAssetAtPath<RoverIslandGenerationSettings>(SettingsPath);
            if (settings == null)
            {
                UnityEngine.Debug.LogError($"[RoverIslandValidationRunner] Could not load settings at '{SettingsPath}'.");
                return;
            }

            bool allPassed = true;
            allPassed &= CheckDeterminism(settings);
            allPassed &= CheckIslandIsLandAtCenter(settings);
            allPassed &= CheckOceanFarOutsideIsland(settings);
            allPassed &= CheckSeedChangesTerrain(settings);

            UnityEngine.Debug.Log(allPassed
                ? "[RoverIslandValidationRunner] ALL CHECKS PASSED"
                : "[RoverIslandValidationRunner] SOME CHECKS FAILED — see preceding errors");
        }

        private static bool CheckDeterminism(RoverIslandGenerationSettings settings)
        {
            float2 pos = new float2(settings.IslandCenter.x + 50f, settings.IslandCenter.y - 30f);
            float a = TerraxelGenerated.GenerateDensity(pos, settings.Seed);
            float b = TerraxelGenerated.GenerateDensity(pos, settings.Seed);

            bool passed = Mathf.Approximately(a, b);
            LogResult(passed, $"Determinism: same seed+position returns the same height twice ({a} vs {b})");
            return passed;
        }

        private static bool CheckIslandIsLandAtCenter(RoverIslandGenerationSettings settings)
        {
            float2 center = new float2(settings.IslandCenter.x, settings.IslandCenter.y);
            float height = TerraxelGenerated.GenerateDensity(center, settings.Seed);

            bool passed = height > settings.SeaLevel;
            LogResult(passed, $"Island boundary: center height ({height}) is above sea level ({settings.SeaLevel})");
            return passed;
        }

        private static bool CheckOceanFarOutsideIsland(RoverIslandGenerationSettings settings)
        {
            float2 farAway = new float2(settings.IslandCenter.x, settings.IslandCenter.y + settings.IslandLength * 3f);
            float height = TerraxelGenerated.GenerateDensity(farAway, settings.Seed);

            float expectedCeiling = settings.SeaLevel - settings.DepthBelowSea * 0.5f;
            bool passed = height < expectedCeiling;
            LogResult(passed, $"Island boundary: far-outside height ({height}) is well below sea level (< {expectedCeiling})");
            return passed;
        }

        private static bool CheckSeedChangesTerrain(RoverIslandGenerationSettings settings)
        {
            float2 pos = new float2(settings.IslandCenter.x + 120f, settings.IslandCenter.y + 80f);
            float a = TerraxelGenerated.GenerateDensity(pos, settings.Seed);
            float b = TerraxelGenerated.GenerateDensity(pos, settings.Seed + 1);

            bool passed = !Mathf.Approximately(a, b);
            LogResult(passed, $"Seed sensitivity: different seeds produce different heights at the same position ({a} vs {b})");
            return passed;
        }

        private static void LogResult(bool passed, string message)
        {
            if (passed)
                UnityEngine.Debug.Log($"[RoverIslandValidationRunner] PASS — {message}");
            else
                UnityEngine.Debug.LogError($"[RoverIslandValidationRunner] FAIL — {message}");
        }
    }
}
