// Patches/Patch_Trader.cs
// Harmony Postfix 패치: Tradeable.ResolveTrade (거래 실행 시점)
// 플레이어가 상인과 거래를 확정할 때 호출됨.
// 거래가 완료된 후, 낮은 확률로 정착민 한 명이 떠나는 이벤트 발동.

using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FreeWillDepartures.Patches
{
    /// <summary>
    /// Tradeable.ResolveTrade 에 Postfix 패치 적용.
    /// 이 메서드는 거래 다이얼로그에서 각 아이템의 거래가 확정될 때 호출됨.
    /// 여러 번 호출되므로 내부에서 중복 발동 방지 필요.
    /// </summary>
    [HarmonyPatch(typeof(Tradeable), nameof(Tradeable.ResolveTrade))]
    public static class Patch_TradeableResolveTrade
    {
        // 같은 거래 세션에서 한 번만 발동하기 위한 쿨다운 틱 추적
        // (ResolveTrade는 거래된 아이템 수만큼 반복 호출될 수 있음)
        private static int lastTradeCheckTick = -9999;
        private const int COOLDOWN_TICKS = 300; // 5초 쿨다운

        /// <summary>
        /// 거래 확정 후 실행되는 Postfix.
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix()
        {
            // 쿨다운 체크 (같은 거래 세션에서 중복 호출 방지)
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
                // 기본 조건 체크
                if (pawn == null || pawn.Dead || !pawn.Spawned) continue;
                if (!pawn.IsFreeColonist) continue;
                if (pawn.IsQuestLodger()) continue;
                if (pawn.MentalStateDef != null) continue; // 이미 정신 상태 중이면 제외

                // 확률 체크 (기본 2%)
                if (Rand.Value > FWD_Mod.Settings.traderChance) continue;

                Log.Message($"[FreeWillDepartures] {pawn.Name.ToStringShort}이(가) 상인 거래 후 출발 확률 통과!");

                // 한 번에 한 명만 떠나도록 이탈 후 루프 종료
                DepartureUtility.TriggerDeparture(pawn, DepartureReason.Trader);
                break;
            }
        }
    }
}
