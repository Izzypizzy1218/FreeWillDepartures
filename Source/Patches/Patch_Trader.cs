// Patches/Patch_Trader.cs
// Harmony Postfix 패치: Dialog_Trade.Close
// 거래 다이얼로그가 닫힐 때 호출됨 (거래 성사/취소 모두 해당).
// 상인과 대화 자체가 트리거 — 거래 여부 무관.

using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FreeWillDepartures.Patches
{
    /// <summary>
    /// Dialog_Trade.Close 에 Postfix 패치 적용.
    /// 거래 창이 닫힐 때 (성사/취소 무관) 발동.
    /// 상인과 대화를 나눴다는 사실 자체가 영감의 원천.
    /// </summary>
    [HarmonyPatch(typeof(Dialog_Trade), nameof(Dialog_Trade.Close))]
    public static class Patch_DialogTradeClose
    {
        // 같은 거래 세션에서 중복 발동 방지용 쿨다운
        private static int lastTradeCheckTick = -9999;
        private const int COOLDOWN_TICKS = 600; // 10초 쿨다운

        [HarmonyPostfix]
        public static void Postfix()
        {
            // 쿨다운 체크
            int currentTick = Find.TickManager?.TicksGame ?? 0;
            if (currentTick - lastTradeCheckTick < COOLDOWN_TICKS) return;
            lastTradeCheckTick = currentTick;

            // 현재 맵 확인
            Map map = Find.CurrentMap;
            if (map == null) return;

            // 맵의 모든 자유 정착민 가져오기
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            if (colonists == null || colonists.Count == 0) return;

            // 각 정착민에 대해 확률 체크
            foreach (Pawn pawn in colonists)
            {
                if (pawn == null || pawn.Dead || !pawn.Spawned) continue;
                if (!pawn.IsFreeColonist) continue;
                if (pawn.IsQuestLodger()) continue;
                if (pawn.MentalStateDef != null) continue;

                // 확률 체크 (기본 2%)
                if (Rand.Value > FWD_Mod.Settings.traderChance) continue;

                Log.Message($"[FreeWillDepartures] {pawn.Name.ToStringShort}이(가) 상인과의 대화 후 출발 확률 통과!");

                // 한 번에 한 명만
                DepartureUtility.TriggerDeparture(pawn, DepartureReason.Trader);
                break;
            }
        }
    }
}
