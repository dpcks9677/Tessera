using System;
using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    /// <summary>
    /// 퀘스트 진행 상태에서 하위 목표 한 줄입니다. 표시층이 접두("퀘스트: ")와 리치 텍스트 태그를
    /// 붙이므로 여기서는 순수 텍스트만 갖습니다. UI 비의존을 유지하기 위해서입니다.
    /// </summary>
    public readonly struct AugmentProgressLine
    {
        public AugmentProgressLine(string text, bool done, bool isTargetNote = false)
        {
            Text = text;
            Done = done;
            IsTargetNote = isTargetNote;
        }

        public readonly string Text;
        public readonly bool Done;

        /// <summary>BountyHunter의 "현재 타겟: X" 같은 보조 줄입니다. 접두·밑줄이 붙지 않습니다.</summary>
        public readonly bool IsTargetNote;
    }

    /// <summary>
    /// 증강 하나의 진행 상태입니다. <see cref="Lines"/>는 카드에 하위 목표로 한 줄씩 찍힙니다.
    /// </summary>
    public readonly struct AugmentProgress
    {
        private static readonly IReadOnlyList<AugmentProgressLine> EmptyLines = Array.Empty<AugmentProgressLine>();

        public AugmentProgress(AugmentProgressOutcome outcome, IReadOnlyList<AugmentProgressLine> lines)
        {
            Outcome = outcome;
            Lines = lines ?? EmptyLines;
        }

        public readonly AugmentProgressOutcome Outcome;
        public readonly IReadOnlyList<AugmentProgressLine> Lines;
    }
}
