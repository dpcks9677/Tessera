using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    /// <summary>
    /// 증강 주사위 타입별 굴림 결과 눈면 배열을 담는 표입니다.
    /// 굴림 결과 생성용 표이며, <see cref="Tessera.Dice.DiceFaceValues"/>의 시각용 면 매핑(DieType 기준)과는 별개 체계입니다.
    /// </summary>
    public static class YachtDieFaces
    {
        private static readonly Dictionary<YachtDieType, int[]> Faces = new Dictionary<YachtDieType, int[]>
        {
            { YachtDieType.Heavy, new[] { 4, 4, 5, 5, 6, 6 } },
            { YachtDieType.Octahedron, new[] { 1, 2, 3, 4, 4, 5, 5, 6 } },
            { YachtDieType.Sevens, new[] { 2, 3, 4, 5, 6, 7 } }
        };

        /// <summary>주어진 증강 주사위 타입의 눈면 배열을 찾는다. Promotion은 상태 기반이라 이 표에 없다.</summary>
        public static bool TryGetFaces(YachtDieType type, out int[] faces)
        {
            return Faces.TryGetValue(type, out faces);
        }
    }
}
