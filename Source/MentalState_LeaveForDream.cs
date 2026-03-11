// MentalState_LeaveForDream.cs
// 꿈을 찾아 떠나는 커스텀 MentalState.
//
// [핵심 설계]
// 바닐라 ThinkTree는 defName이 "GiveUpExit"인 MentalState에만 탈출 Job을 부여함.
// 우리 커스텀 defName "FWD_LeaveForDream"은 ThinkTree에 등록되지 않으므로
// PostStart()에서 직접 맵 탈출 Job(Goto + exitMapOnArrival)을 폰에게 부여해야 함.

using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace FreeWillDepartures
{
    /// <summary>
    /// 꿈을 찾아 떠나는 커스텀 정신 상태.
    /// MentalState_GiveUpExit을 상속받아 맵 이탈 행동을 그대로 활용.
    /// 추가로: 실제로 맵을 벗어났을 때 플레이어 파벌에서 영구 제거.
    /// </summary>
    public class MentalState_LeaveForDream : MentalState_GiveUpExit
    {
        // 이 정신 상태가 편지를 이미 발송했는지 여부 (중복 발송 방지)
        private bool letterSent = false;

        // 출발 이유 ("birthday" 또는 "trader") - 저장/불러오기에 사용
        public string departureReason = "unknown";

        /// <summary>
        /// 정신 상태가 시작될 때 호출됨.
        /// 바닐라 ThinkTree가 우리 커스텀 defName을 모르므로
        /// 직접 맵 탈출 Job을 부여해 폰이 실제로 걸어나가게 함.
        /// </summary>
        public override void PostStart(string reason)
        {
            base.PostStart(reason);
            letterSent = true; // 편지는 트리거 지점에서 이미 발송됨
            GiveExitJob();
        }

        /// <summary>
        /// 폰에게 맵 탈출 Job을 직접 부여.
        /// 상인 이탈 시에는 상인 방향으로, 그 외에는 최적 탈출 지점으로 이동.
        /// </summary>
        private void GiveExitJob()
        {
            if (!pawn.Spawned || pawn.Map == null) return;

            IntVec3 exitSpot;

            // 상인 이탈인 경우: 상인(TradeSession.trader)의 위치 방향으로 탈출
            if (departureReason == "Trader" && TryGetTraderExitSpot(out IntVec3 traderSpot))
            {
                exitSpot = traderSpot;
            }
            // 그 외: 최적 탈출 지점 탐색
            else if (!RCellFinder.TryFindBestExitSpot(pawn, out exitSpot, TraverseMode.ByPawn))
            {
                return; // 탈출 경로 없음
            }

            Job job = JobMaker.MakeJob(JobDefOf.Goto, exitSpot);
            job.exitMapOnArrival = true;                      // 목적지 도달 시 맵 탈출
            job.locomotionUrgency = LocomotionUrgency.Jog;   // 뛰어서 이동
            pawn.jobs.TryTakeOrderedJob(job, JobTag.MiscWork);
        }

        /// <summary>
        /// TradeSession의 상인 위치 근처의 맵 탈출 지점을 찾음.
        /// 궤도 상인(Orbital trader)은 맵에 없으므로 false 반환.
        /// </summary>
        private bool TryGetTraderExitSpot(out IntVec3 spot)
        {
            spot = IntVec3.Invalid;

            // 상인이 맵 위의 실제 폰인 경우 (캐러밴 상인 등)
            if (TradeSession.trader is Pawn traderPawn && traderPawn.Spawned
                && traderPawn.Map == pawn.Map)
            {
                // 상인 위치 근처의 맵 가장자리 탈출 지점 탐색
                return RCellFinder.TryFindExitSpotNear(pawn, traderPawn.Position, 30f, out spot, TraverseMode.ByPawn);
            }

            return false;
        }

        /// <summary>
        /// 매 틱마다 호출됨. RimWorld 1.6에서 시그니처가 (int delta)임.
        /// </summary>
        public override void MentalStateTick(int delta)
        {
            base.MentalStateTick(delta);
            // 폰이 맵에서 사라졌고 아직 살아있으면 → 영구 이탈 처리 (안전장치)
            if (!pawn.Spawned && !pawn.Dead)
            {
                MakeDeparturePermanent();
            }
        }

        /// <summary>
        /// 정신 상태가 종료될 때 호출 (RecoverFromState 내부에서 ClearMentalStateDirect → PostEnd 순서로 실행).
        /// GiveUpExit 폰이 맵을 벗어난 뒤 RecoverFromState가 호출되면 여기서 영구 이탈 처리.
        /// </summary>
        public override void PostEnd()
        {
            base.PostEnd();
            // 폰이 맵 밖에 있으면 영구 이탈로 간주
            if (!pawn.Spawned && !pawn.Dead)
            {
                MakeDeparturePermanent();
            }
        }

        /// <summary>
        /// 폰을 플레이어 파벌에서 영구 제거하는 핵심 로직.
        /// SetFaction(null)로 플레이어 통제에서 완전히 벗어남.
        /// </summary>
        private void MakeDeparturePermanent()
        {
            if (pawn.Faction == Faction.OfPlayer)
            {
                Log.Message($"[FreeWillDepartures] {pawn.Name.ToStringShort}이(가) 정착지를 영구히 떠났습니다. (이유: {departureReason})");

                // 파벌을 null로 설정 → 플레이어 정착민에서 제거
                pawn.SetFaction(null);

                // 세계 폰으로 등록 (게임 세계에 존재는 하되 통제 불가)
                if (Find.WorldPawns != null && !Find.WorldPawns.Contains(pawn))
                {
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
                }
            }
        }

        /// <summary>
        /// 이 정신 상태의 데이터를 저장/불러오기 (게임 저장 지원).
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref letterSent, "letterSent", false);
            Scribe_Values.Look(ref departureReason, "departureReason", "unknown");
        }
    }
}
