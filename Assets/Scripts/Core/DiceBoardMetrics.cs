using UnityEngine;

namespace Tessera.Core
{
    public static class DiceBoardMetrics
    {
        public const float SourceDiceSize = 1.62f;

        // 던지는 주사위 크기. 480x270 픽셀 격자에서 12.8칸이라 눈이 읽히지 않아 0.78에서 15% 키웠다.
        //
        // 프리셋은 이제 Unity 물리로 직접 굽는다(Assets/Editor/DicePresetBaker.cs). 베이킹이
        // 실제 플레이 영역 안에서 이루어지므로 착지 좌표는 정의상 트레이 안에 들어온다.
        // 이 값을 다시 키우려면 플레이 영역 반폭(PlayBoundsSourceMaxX * TrayScale)과 비교해
        // 주사위 몇 개가 나란히 들어갈 여유가 남는지만 확인하면 된다.
        public const float DieSize = 0.897f;
        public const float DieHalfSize = DieSize * 0.5f;

        // 정렬 크기는 확대 전과 같게 유지한다. DieSize를 키우면 ActiveDieSize가 함께 커지므로
        // 배율을 고정하지 않고 목표 정렬 크기에서 역산한다.
        public const float ArrangedDieSize = 1.014f;
        public const float ActiveScaleMultiplier = ArrangedDieSize / DieSize;
        public const float ActiveDieSize = ArrangedDieSize;
        public const float ActiveDieHalfSize = ActiveDieSize * 0.5f;        // ~0.507f

        // 원본 프리셋 좌표(SourceDiceSize 1.62)를 현재 프로젝트 크기(DieSize)로 스케일링하는 비율
        public const float SourceToUnityScale = DieSize / SourceDiceSize;

        public const float PresetFloorY = -0.528f;
        // 트레이 STL 메시(155 소스 단위)를 월드로 줄이는 배율. M10.7에서 주사위 확대에 맞춰 0.05 -> 0.06.
        public const float TrayScale = 0.06f;
        public const float RollSurfaceY = 0.2f;
        // 트레이 비주얼의 고정 Z 오프셋. 주사위 루트는 Z = 0을 기준으로 하므로,
        // 정렬/킵 목표 좌표에 이 값을 더해 트레이 내부 기준과 일치시킨다.
        public const float TrayCenterZ = -0.3f;

        // 트레이 내부 바닥 착지 시 Y 중심 좌표 (0.2 + 0.39 = 0.59f)
        public const float FloorRestY = RollSurfaceY + DieHalfSize; // 0.59f

        // 트레이 비주얼 오브젝트의 Y 좌표. 이 값 덕분에 플레이 바닥은 TrayScale과 무관하게 항상 RollSurfaceY에 온다.
        public const float TrayVisualY = RollSurfaceY - PlayFloorSourceY * TrayScale;

        // 트레이 테두리 림(Rim) 최상단 높이 및 정렬 시 주사위 Y 위치 (트레이 높이보다 조금 더 높은 위치)
        public const float TrayRimTopSourceY = 24.0f;
        public const float TrayRimTopY = RollSurfaceY + (TrayRimTopSourceY - PlayFloorSourceY) * TrayScale; // ~2.257f
        public const float ActiveArrangedY = TrayRimTopY + ActiveDieHalfSize + 0.05f; // ~2.81f (트레이 상단 위로 띄움)

        // 트레이 상단 12시 방향 킵 홈(Keep Surface) 높이 및 위치
        public const float KeepSurfaceSourceY = 13f;
        public const float PlayFloorSourceY = -10.283531f;
        public const float KeepSurfaceY = RollSurfaceY + (KeepSurfaceSourceY - PlayFloorSourceY) * TrayScale + DieHalfSize;

        public const float KeepStartSourceX = -44f;
        public const float KeepSpacingSourceX = 22f;
        public const float KeepCenterSourceZ = 58f; // 12시 방향 (+Z)

        public const float KeepStartX = KeepStartSourceX * TrayScale;       // -2.64f
        public const float KeepSpacingX = KeepSpacingSourceX * TrayScale;   //  1.32f
        public const float KeepCenterZ = TrayCenterZ + KeepCenterSourceZ * TrayScale; // +3.18f

        // 트레이 내부 플레이 영역 경계 (소스 단위). 림 안쪽 바닥에서 주사위가 실제로 구를 수 있는 범위이며,
        // 베이킹 물리벽과 착지 판정이 이 값을 공유한다.
        //
        // 트레이 STL의 펠트 바닥 면(y = PlayFloorSourceY)을 실측한 값에서 안쪽으로 1.5 정도 좁혔다.
        // 실측 범위는 x [-51.62, 51.62], z [-53.33, 47.80]이다. +Z 쪽은 12시 킵 홈 턱에서 끝난다.
        // 원본(preset-studio/src/YachtTrayModel.js:14)의 PLAY_BOUNDS와는 Z 부호가 반대다. STL을
        // Unity로 들여올 때 Z가 뒤집혀 킵 홈이 +Z(12시)로 오기 때문이며, KeepCenterSourceZ의 부호가 그 증거다.
        public const float PlayBoundsSourceMinX = -50f;
        public const float PlayBoundsSourceMaxX = 50f;
        public const float PlayBoundsSourceMinZ = -52f;
        public const float PlayBoundsSourceMaxZ = 46f;

        public const float PlayBoundsMinX = PlayBoundsSourceMinX * TrayScale;
        public const float PlayBoundsMaxX = PlayBoundsSourceMaxX * TrayScale;
        public const float PlayBoundsMinZ = TrayCenterZ + PlayBoundsSourceMinZ * TrayScale;
        public const float PlayBoundsMaxZ = TrayCenterZ + PlayBoundsSourceMaxZ * TrayScale;

        // 트레이 전체 외곽 가로폭: 155 * 0.06 = 9.30f
        // 5개 주사위 정렬 시 총 가로폭이 트레이 가로폭과 같아지도록 하는 간격: (9.30 - 1.014) / 4 = 2.072f
        public const float TrayOuterWidth = 155f * TrayScale; // 9.30f
        public const float ActiveSpacing = 1.5f; // 요청에 따른 정렬 간격 조정 (1.5f)

        public const float ActiveCenterZ = TrayCenterZ;
        public const float KeepDieScale = 0.95f;

        /// <summary>
        /// 프리셋 로컬 좌표를 6시 -> 12시 투척 방향으로 180도 회전 변환하여 트레이 내부 좌표로 매핑
        /// </summary>
        public static Vector3 TransformPresetPosition(Vector3 presetPosition, bool isMirrored)
        {
            float px = isMirrored ? -presetPosition.x : presetPosition.x;
            float pz = presetPosition.z;
            float py = presetPosition.y;

            // 180도 회전: (-px, -pz) 및 주사위 크기 비례 스케일링
            float x = -px * SourceToUnityScale;
            float z = -pz * SourceToUnityScale;
            float y = FloorRestY + (py - PresetFloorY) * SourceToUnityScale;

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// 활성 주사위(킵되지 않은 주사위)를 트레이 중앙에 가로로 정렬할 위치
        /// 주사위 개수에 따라 동일 간격을 유지하며 비례하여 가로폭을 줄이고 중앙 정렬
        /// </summary>
        public static Vector3 GetActivePosition(int slot, int totalActive)
        {
            float startX = -(totalActive - 1) * ActiveSpacing * 0.5f;
            return new Vector3(
                startX + slot * ActiveSpacing,
                ActiveArrangedY,
                ActiveCenterZ);
        }

        /// <summary>
        /// 킵(Keep)된 주사위를 트레이 상단(12시 방향) 킵 홈(Keep Zone)에 정렬할 위치
        /// </summary>
        public static Vector3 GetKeepPosition(int slot)
        {
            return new Vector3(
                KeepStartX + slot * KeepSpacingX,
                KeepSurfaceY,
                KeepCenterZ);
        }
    }
}
