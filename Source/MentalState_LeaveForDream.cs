// MentalState_LeaveForDream.cs
// GiveUpExit 정신 상태를 상속받아 "꿈을 찾아 떠남" 동작을 구현.
// 폰이 맵 끝으로 걸어나가는 행동은 부모 클래스(MentalState_GiveUpExit)가 처리.
// 우리는 영구 이탈 로직(파벌 제거)을 추가함.

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
        /// 편지 발송은 DepartureUtility에서 처리하므로, 여기선 부모 초기화만.
        /// </summary>
        public override void PostStart(string reason)
        {
            base.PostStart(reason);
            letterSent = true; // 편지는 트리거 지점에서 이미 발송됨
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
