// FWD_Mod.cs
// 모드 진입점(Entry Point) 및 설정(Mod Settings) 클래스.
// RimWorld가 모드를 로드할 때 이 클래스의 생성자가 호출됨.

using HarmonyLib;
using Verse;
using UnityEngine;

namespace FreeWillDepartures
{
    /// <summary>
    /// 모드 설정 데이터 클래스.
    /// ExposeData()를 통해 저장/불러오기가 가능함.
    /// </summary>
    public class FWD_Settings : ModSettings
    {
        /// <summary>성인 생일 출발 확률 (0.0 ~ 1.0)</summary>
        public float birthdayChance = 0.05f; // 기본값 5%

        /// <summary>상인 거래 후 출발 확률 (0.0 ~ 1.0)</summary>
        public float traderChance = 0.02f;   // 기본값 2%

        /// <summary>
        /// Verse의 Scribe 시스템으로 설정값을 저장하고 불러옴.
        /// 이 메서드가 없으면 게임 재시작 시 설정이 초기화됨.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref birthdayChance, "birthdayChance", 0.05f);
            Scribe_Values.Look(ref traderChance, "traderChance", 0.02f);
        }
    }

    /// <summary>
    /// 모드 메인 클래스.
    /// Mod를 상속받아야 RimWorld가 이 클래스를 모드로 인식함.
    /// </summary>
    public class FWD_Mod : Mod
    {
        /// <summary>전역 설정 인스턴스 - 패치 클래스에서 참조함.</summary>
        public static FWD_Settings Settings;

        public FWD_Mod(ModContentPack content) : base(content)
        {
            // 설정 인스턴스를 불러오거나 새로 생성
            Settings = GetSettings<FWD_Settings>();

            // Harmony 패처 초기화 및 모든 [HarmonyPatch] 어트리뷰트 자동 적용
            var harmony = new Harmony("com.freewilldepartures.mod");
            harmony.PatchAll();

            Log.Message("[FreeWillDepartures] 모드가 로드되었습니다. (생일 확률: " +
                        (Settings.birthdayChance * 100f).ToString("F1") + "%, 상인 확률: " +
                        (Settings.traderChance * 100f).ToString("F1") + "%)");
        }

        /// <summary>
        /// 모드 설정 화면 (메인 메뉴 → 옵션 → 모드 설정에서 표시됨).
        /// </summary>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // 생일 확률 슬라이더
            listingStandard.Label(
                "생일 독립 확률: " + (Settings.birthdayChance * 100f).ToString("F1") + "%",
                tooltip: "정착민이 18세 생일을 맞았을 때 떠날 확률입니다."
            );
            Settings.birthdayChance = listingStandard.Slider(Settings.birthdayChance, 0f, 1f);

            listingStandard.Gap(12f);

            // 상인 확률 슬라이더
            listingStandard.Label(
                "상인 거래 후 출발 확률: " + (Settings.traderChance * 100f).ToString("F1") + "%",
                tooltip: "상인과 거래를 마친 후 정착민이 떠날 확률입니다."
            );
            Settings.traderChance = listingStandard.Slider(Settings.traderChance, 0f, 1f);

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>모드 설정 탭에 표시될 이름.</summary>
        public override string SettingsCategory() => "Free Will Departures";
    }
}
