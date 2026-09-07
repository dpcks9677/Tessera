using System;

namespace Tessera.Games.Yacht
{
    /// <summary>
    /// 주사위·상시 효과·수동 행동을 포함한 강화 증강의 공통 기반입니다.
    /// </summary>
    public abstract class EnhanceAugment : AugmentHandler
    {
        public abstract string DisplayName { get; }

        public abstract string Description { get; }

        public virtual string[] Conflicts => Array.Empty<string>();

        public override YachtAugmentDefinition CreateDefinition() => new()
        {
            Id = Id,
            DisplayName = DisplayName,
            Description = Description,
            Kind = YachtAugmentKind.Enhance,
            Conflicts = Conflicts
        };
    }
}
