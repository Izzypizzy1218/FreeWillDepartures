// Patches/Patch_Birthday.cs
// Harmony Postfix 패치: Pawn_AgeTracker.BirthdayBiological
// 폰이 18세 생일을 맞이할 때 호출됨.
// 바닐라 생일 로직이 모두 끝난 뒤(Postfix) 우리 코드가 실행됨.

using HarmonyLib;
using RimWorld;
using Verse;

namespace FreeWillDepartures.Patches
{
    /// <summary>
    /// Pawn_AgeTracker.BirthdayBiological 메서드에 Postfix 패치를 적용.
    /// 생일 때마다 호출되므로, 18세 조건을 내부에서 필터링해야 함.
    /// </summary>
    // BirthdayBiological은 private 메서드이므로 nameof() 대신 문자열로 지정
    [HarmonyPatch(typeof(Pawn_AgeTracker), "BirthdayBiological")]
    public static class Patch_BirthdayBiological
    {
        /// <summary>
        /// 바닐라 BirthdayBiological 실행 완료 후 추가 로직 실행.
        /// </summary>
        /// <param name="__instance">패치된 Pawn_AgeTracker 인스턴스 (자동 주입)</param>
        /// <param name="birthdayAge">이번 생일의 나이 (자동 주입)</param>
        [HarmonyPostfix]
        public static void Postfix(Pawn_AgeTracker __instance, int birthdayAge)
        {
            // 성인 나이(18세)가 아니면 즉시 반환
            if (birthdayAge != 18) return;

            // pawn은 private 필드이므로 Traverse로 접근
            Pawn pawn = Traverse.Create(__instance).Field<Pawn>("pawn").Value;
            if (pawn == null) return;

            // 조건 검사: 플레이어 소속 자유 정착민이어야 함
            if (pawn.Faction != Faction.OfPlayer) return;
            if (!pawn.IsFreeColonist) return;           // 노예, 죄수 제외
            if (pawn.IsQuestLodger()) return;            // 퀘스트 방문자 제외
            if (!pawn.Spawned || pawn.Dead) return;      // 유효한 폰이어야 함

            // 확률 체크 (기본 5%)
            if (Rand.Value > FWD_Mod.Settings.birthdayChance) return;

            Log.Message($"[FreeWillDepartures] {pawn.Name.ToStringShort}이(가) 18세 생일을 맞아 독립 확률 통과!");

            // 이탈 이벤트 발동
            DepartureUtility.TriggerDeparture(pawn, DepartureReason.Birthday);
        }
    }
}
