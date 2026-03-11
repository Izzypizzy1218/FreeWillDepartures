// DepartureUtility.cs
// 실제 이탈 이벤트를 발동시키는 유틸리티 클래스.
// 편지 발송 + 커스텀 MentalState 부여를 한 곳에서 처리.

using RimWorld;
using Verse;

namespace FreeWillDepartures
{
    /// <summary>이탈 이유 열거형</summary>
    public enum DepartureReason
    {
        Birthday,   // 성인이 되어 독립
        Trader      // 상인의 이야기를 듣고 떠남
    }

    /// <summary>
    /// 이탈 이벤트를 실행하는 정적 유틸리티 클래스.
    /// 패치 클래스들이 이 클래스를 호출함.
    /// </summary>
    public static class DepartureUtility
    {
        /// <summary>
        /// 지정한 폰에게 이탈 이벤트를 발동.
        /// 1) 커스텀 MentalState 부여 → 폰이 맵 끝으로 걸어감
        /// 2) 편지(Letter) 발송 → 플레이어에게 상황 알림
        /// </summary>
        /// <param name="pawn">이탈할 폰</param>
        /// <param name="reason">이탈 이유</param>
        public static void TriggerDeparture(Pawn pawn, DepartureReason reason)
        {
            // 기본 유효성 검사
            if (pawn == null || pawn.Dead || !pawn.Spawned) return;
            if (pawn.Faction != Faction.OfPlayer) return;
            if (!pawn.IsFreeColonist) return;

            // 이미 정신 상태(Mental State) 중이면 중복 발동 방지
            if (pawn.MentalStateDef != null) return;

            // MentalStateDef를 Def DB에서 찾기
            MentalStateDef stateDef = DefDatabase<MentalStateDef>.GetNamed("FWD_LeaveForDream", errorOnFail: false);
            if (stateDef == null)
            {
                Log.Error("[FreeWillDepartures] FWD_LeaveForDream MentalStateDef를 찾을 수 없습니다! XML이 올바른지 확인하세요.");
                return;
            }

            // 편지 내용 결정
            string letterLabel;
            string letterText;

            if (reason == DepartureReason.Birthday)
            {
                // 번역 키 시도, 없으면 하드코딩된 한국어 사용
                letterLabel = TryTranslate("FWD_BirthdayDeparture_Label",
                    "꿈을 찾아 떠나다");
                letterText = TryTranslateFormatted("FWD_BirthdayDeparture_Text", pawn,
                    $"{pawn.Name.ToStringShort}이(가) 이제 어엿한 성인이 되었습니다.\n\n" +
                    $"오늘 생일을 맞은 {pawn.Name.ToStringShort}은(는) 밤새 별을 바라보며, " +
                    $"이 정착지 밖에 자신이 진정으로 원하는 삶이 있다는 것을 깨달았습니다.\n\n" +
                    $"아무도 모르게 짐을 챙긴 {pawn.Name.ToStringShort}은(는) 새벽빛이 물들 무렵, " +
                    $"더 넓은 세상을 향해 조용히 걸음을 옮겼습니다. 그 뒷모습은 씁쓸하지만, 어딘가 자유로워 보였습니다.");
            }
            else // Trader
            {
                letterLabel = TryTranslate("FWD_TraderDeparture_Label",
                    "미지의 세계로 떠나다");
                letterText = TryTranslateFormatted("FWD_TraderDeparture_Text", pawn,
                    $"{pawn.Name.ToStringShort}이(가) 정착지를 떠나기로 결심했습니다.\n\n" +
                    $"상인들이 늘어놓는 바깥 세상 이야기에 귀를 기울이던 {pawn.Name.ToStringShort}의 눈이 점점 빛나기 시작했습니다. " +
                    $"수많은 도시와 경이로운 문명, 아직 발견되지 않은 광활한 땅들...\n\n" +
                    $"결국 {pawn.Name.ToStringShort}은(는) 상단의 짐꾼에게 다가가 말했습니다. " +
                    $"\"저도 함께 가도 될까요?\" 상인은 빙그레 웃으며 고개를 끄덕였고, " +
                    $"{pawn.Name.ToStringShort}은(는) 뒤도 돌아보지 않고 상단을 따라나섰습니다.");
            }

            // 먼저 편지를 발송
            Find.LetterStack.ReceiveLetter(
                label: letterLabel,
                text: letterText,
                textLetterDef: LetterDefOf.NeutralEvent,
                lookTargets: pawn
            );

            // MentalState를 폰에게 부여 (이후 GiveUpExit 로직이 발동되어 맵 끝으로 이동)
            bool started = pawn.mindState.mentalStateHandler.TryStartMentalState(
                stateDef,
                reason: reason.ToString(),
                forceWake: true,  // 자고 있어도 깨워서 실행
                causedByMood: false
            );

            if (started)
            {
                // 부여된 MentalState에 이유 정보 기록
                if (pawn.MentalState is MentalState_LeaveForDream leaveState)
                {
                    leaveState.departureReason = reason.ToString();
                }

                Log.Message($"[FreeWillDepartures] {pawn.Name.ToStringShort}이(가) 이탈 결심. 이유: {reason}");
            }
            else
            {
                Log.Warning($"[FreeWillDepartures] {pawn.Name.ToStringShort}에게 FWD_LeaveForDream MentalState 부여 실패.");
            }
        }

        // 번역 키를 시도하고 없으면 기본값 반환
        private static string TryTranslate(string key, string fallback)
        {
            string translated = key.Translate();
            return (translated == key) ? fallback : translated;
        }

        // 폰 이름이 포함된 번역 시도
        private static string TryTranslateFormatted(string key, Pawn pawn, string fallback)
        {
            try
            {
                string translated = key.Translate(pawn.Named("PAWN"));
                return (translated == key) ? fallback : translated;
            }
            catch
            {
                return fallback;
            }
        }
    }
}
