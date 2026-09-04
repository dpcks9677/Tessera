using Tessera.Dice;
using Tessera.Games.Yacht;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 권위 계층의 주사위 종류를 화면 팔레트 종류로 옮긴다(M7-T5).
    ///
    /// 두 열거형이 따로 있는 이유는 계층 때문이다. <see cref="DieType"/>은 색과 재질만 아는
    /// 렌더링 쪽 개념이라 요트 규칙을 몰라야 하고, <see cref="YachtDieType"/>은 눈금 값과 증강
    /// 효과를 가르는 규칙 쪽 개념이다. 대응은 원본 augmented-dice의 타입 이름을 그대로 따른다
    /// (preset-studio/src/gameRuntime.js:64-70).
    ///
    /// <see cref="DieType.Metal"/>은 어떤 규칙 종류에도 대응하지 않는다. 디버그 키로만 쓰인다.
    /// </summary>
    public static class YachtDieVisuals
    {
        public static DieType Resolve(YachtDieType type)
        {
            return type switch
            {
                YachtDieType.Heavy => DieType.HeavyRed,
                YachtDieType.Golden => DieType.Golden,
                YachtDieType.Octahedron => DieType.Octahedron,
                YachtDieType.Weird => DieType.Weird,
                YachtDieType.Promotion => DieType.Promotion,
                YachtDieType.Couple => DieType.Couple,
                YachtDieType.Sevens => DieType.Sevens,
                _ => DieType.Normal
            };
        }
    }
}
