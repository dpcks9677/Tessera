using System;

namespace Tessera.Games.Yacht
{
    /// <summary>
    /// 퀘스트 증강의 공통 기반입니다.
    /// </summary>
    public abstract class QuestAugment : AugmentHandler
    {
        public abstract string DisplayName { get; }

        public abstract string Description { get; }

        public virtual bool PhaseOneOnly => false;

        public virtual string[] Conflicts => Array.Empty<string>();

        public override YachtAugmentDefinition CreateDefinition() => new()
        {
            Id = Id,
            DisplayName = DisplayName,
            Description = Description,
            Target = "Quest",
            Kind = YachtAugmentKind.Quest,
            IsQuest = true,
            PhaseOneOnly = PhaseOneOnly,
            Conflicts = Conflicts
        };
    }
}
