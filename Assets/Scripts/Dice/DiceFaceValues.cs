using UnityEngine;

namespace Tessera.Dice
{
    /// <summary>
    /// 주사위 종류별로 "물리 면 인덱스(1부터) → 그 면에 새겨진 값" 표(M7-T5).
    ///
    /// 일반 주사위는 값과 면 인덱스가 같아서 둘을 구분할 일이 없었다. 세븐스는 2~7을,
    /// 8면체는 같은 값을 두 면에 새기므로 값만으로는 어느 면을 위로 돌릴지 정할 수 없다.
    /// 회전 계산은 면 인덱스만 다루므로, 값을 여기서 면 인덱스로 옮긴 뒤 넘긴다.
    ///
    /// 원본 preset-studio/src/diceMaterials.js:66-74 이식.
    /// 묵직한 주사위(4·4·5·5·6·6)는 값 4·5·6이 일반 주사위의 같은 번호 면과 겹쳐 표가 필요 없다.
    /// </summary>
    public static class DiceFaceValues
    {
        private static readonly int[] SevensFaces = { 2, 3, 4, 5, 6, 7 };
        private static readonly int[] OctahedronFaces = { 1, 2, 3, 4, 4, 5, 5, 6 };

        /// <summary>면 값 표. 값과 면 인덱스가 같은 종류는 null이다.</summary>
        public static int[] Get(DieType type)
        {
            return type switch
            {
                DieType.Sevens => SevensFaces,
                DieType.Octahedron => OctahedronFaces,
                _ => null
            };
        }

        public static int FaceCount(DieType type)
        {
            return type == DieType.Octahedron ? 8 : 6;
        }

        /// <summary>
        /// 이 값을 보여 주려면 어느 면을 위로 돌려야 하는지 알려 준다.
        /// 같은 값이 여러 면에 있으면 첫 면을 쓴다. 표에 없는 값은 면 범위로 잘라 낸다.
        /// </summary>
        public static int FaceIndexOf(DieType type, int value)
        {
            int[] faces = Get(type);
            if (faces == null) return Mathf.Clamp(value, 1, FaceCount(type));

            for (int i = 0; i < faces.Length; i++)
            {
                if (faces[i] == value) return i + 1;
            }
            return Mathf.Clamp(value, 1, faces.Length);
        }

        /// <summary>주사위 오브젝트에 적용된 종류. 표식이 없으면 일반 주사위로 본다.</summary>
        public static DieType TypeOf(Transform die)
        {
            if (die == null) return DieType.Normal;
            DiceKeepTarget target = die.GetComponent<DiceKeepTarget>();
            return target != null ? target.Type : DieType.Normal;
        }
    }
}
