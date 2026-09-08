using System;
using UnityEngine;

namespace Tessera.Dice
{
    public static class DiceFaceOrientation
    {
        public static readonly Vector3[] FaceNormals =
        {
            Vector3.up,       // 1 (Top: +Y)
            Vector3.forward,  // 2 (Front: +Z)
            Vector3.left,     // 3 (Left: -X)
            Vector3.right,    // 4 (Right: +X)
            Vector3.back,     // 5 (Back: -Z)
            Vector3.down      // 6 (Bottom: -Y)
        };

        private static readonly Vector3[] FaceUpAxes =
        {
            Vector3.back,     // 1
            Vector3.right,    // 2 (Numpad 1, 9 대각선 슬래시 정렬)
            Vector3.up,       // 3 (Numpad 1, 5, 9 대각선 슬래시 정렬)
            Vector3.up,       // 4
            Vector3.up,       // 5
            Vector3.forward   // 6 (Numpad 1, 3, 4, 6, 7, 9 세로 2열 정렬)
        };

        /// <summary>
        /// 8면 주사위의 면 법선(M7-T5). 배열 위치가 곧 면 인덱스 - 1이다.
        /// 원본 preset-studio/src/geometryUtils.js:110-113의 방향 순서를 그대로 쓴다.
        /// </summary>
        public static readonly Vector3[] OctaFaceNormals =
        {
            new Vector3(1f, 1f, 1f).normalized, new Vector3(1f, -1f, 1f).normalized,
            new Vector3(1f, -1f, -1f).normalized, new Vector3(1f, 1f, -1f).normalized,
            new Vector3(-1f, 1f, -1f).normalized, new Vector3(-1f, -1f, -1f).normalized,
            new Vector3(-1f, -1f, 1f).normalized, new Vector3(-1f, 1f, 1f).normalized
        };

        public const float DefaultCameraPitch = 75.0f;
        public const float DefaultCameraTiltOffset = -15.0f; // 75f - 90f

        public static Quaternion GetTopRotation(int value)
        {
            return GetTopRotation(value, Vector3.forward);
        }

        public static Quaternion GetTopRotation(int value, Vector3 faceUpDirection)
        {
            if (value < 1 || value > FaceNormals.Length) value = 1;

            Vector3 alignedFaceUp = Vector3.ProjectOnPlane(faceUpDirection, Vector3.up).normalized;
            if (alignedFaceUp.sqrMagnitude < 0.5f) alignedFaceUp = Vector3.forward;

            Quaternion sourceBasis = Quaternion.LookRotation(FaceNormals[value - 1], FaceUpAxes[value - 1]);
            Quaternion targetBasis = Quaternion.LookRotation(Vector3.up, alignedFaceUp);
            return targetBasis * Quaternion.Inverse(sourceBasis);
        }

        /// <summary>
        /// 카메라의 틸트 각도(기본 75°)에 맞춰 주사위 눈금 상단면이 카메라 렌즈와 정면 직교하도록 회전 계산
        /// </summary>
        public static Quaternion GetCameraFacingRotation(int value, float cameraPitch = DefaultCameraPitch)
        {
            float tiltAngle = cameraPitch - 90f; // 예: 75° 카메라 기준 -15° 틸트
            Quaternion tilt = Quaternion.Euler(tiltAngle, 0f, 0f);
            return tilt * GetTopRotation(value, Vector3.forward);
        }

        /// <summary>
        /// 착지된 주사위의 윗면 눈금을 유지한 채 카메라 렌즈를 정면으로 바라보도록 회전 계산
        /// </summary>
        public static Quaternion GetCameraFacingUprightRotation(Quaternion landingRotation, float cameraPitch = DefaultCameraPitch)
        {
            int topValue = GetTopValue(landingRotation);
            return GetCameraFacingRotation(topValue, cameraPitch);
        }

        public static Vector3 GetFaceNormal(int value)
        {
            if (value < 1 || value > FaceNormals.Length) return Vector3.up;
            return FaceNormals[value - 1];
        }

        public static Vector3 GetFaceUpAxis(int value)
        {
            if (value < 1 || value > FaceUpAxes.Length) return Vector3.forward;
            return FaceUpAxes[value - 1];
        }

        public static int GetTopValue(Quaternion rotation)
        {
            Quaternion normalizedRotation = rotation.normalized;
            int topValue = 1;
            float bestUpDot = float.NegativeInfinity;
            for (int index = 0; index < FaceNormals.Length; index++)
            {
                float upDot = Vector3.Dot(normalizedRotation * FaceNormals[index], Vector3.up);
                if (upDot <= bestUpDot) continue;
                bestUpDot = upDot;
                topValue = index + 1;
            }

            return topValue;
        }

        public static Quaternion GetUprightRotation(Quaternion landingRotation, Vector3 faceUpDirection)
        {
            return GetTopRotation(GetTopValue(landingRotation), faceUpDirection);
        }

        // ------------------------------------------------------------ 8면 주사위(M7-T5)

        /// <summary>지금 위를 향하고 있는 8면 주사위의 면 인덱스(1~8).</summary>
        public static int GetOctaTopFace(Quaternion rotation)
        {
            Quaternion normalizedRotation = rotation.normalized;
            int topFace = 1;
            float bestUpDot = float.NegativeInfinity;
            for (int index = 0; index < OctaFaceNormals.Length; index++)
            {
                float upDot = Vector3.Dot(normalizedRotation * OctaFaceNormals[index], Vector3.up);
                if (upDot <= bestUpDot) continue;
                bestUpDot = upDot;
                topFace = index + 1;
            }
            return topFace;
        }

        /// <summary>
        /// 8면 주사위의 목표 면이 카메라를 정면으로 바라보게 하는 회전.
        ///
        /// 면 법선만 맞추면(<see cref="Quaternion.FromToRotation"/>) 축 둘레의 롤이 정해지지 않아
        /// 면에 새긴 숫자가 제멋대로 누워서 읽힌다. 6면과 같이 면 법선과 글자 윗방향을 함께 맞춘다.
        /// </summary>
        public static Quaternion GetOctaCameraFacingRotation(int faceIndex, float cameraPitch = DefaultCameraPitch)
        {
            Quaternion tilt = Quaternion.Euler(cameraPitch - 90f, 0f, 0f);
            Quaternion sourceBasis = Quaternion.LookRotation(GetOctaFaceNormal(faceIndex), GetOctaFaceUpAxis(faceIndex));
            Quaternion targetBasis = Quaternion.LookRotation(Vector3.up, Vector3.forward);
            return tilt * (targetBasis * Quaternion.Inverse(sourceBasis));
        }

        /// <summary>
        /// 면에 새긴 숫자의 윗방향(면 평면 위). 베이커가 숫자를 세울 때 쓴 규칙과 같아야 한다
        /// (<c>DiceShapeBaker.BakeOctahedronPrefab</c>, 원본 geometryUtils.js:125).
        ///
        /// 면 방향 성분이 ±1뿐이라 이 벡터는 면 법선과 항상 직교한다.
        /// </summary>
        public static Vector3 GetOctaFaceUpAxis(int faceIndex)
        {
            Vector3 normal = GetOctaFaceNormal(faceIndex);
            return new Vector3(-Mathf.Sign(normal.x), -Mathf.Sign(normal.y), 2f * Mathf.Sign(normal.z)).normalized;
        }

        /// <summary>
        /// 착지 회전은 그대로 두고, 목표 면이 위로 오도록 8면 주사위를 제자리에서 돌린다.
        /// 6면과 달리 면이 축과 나란하지 않아 별도 표가 필요하다.
        ///
        /// 면 법선만 맞추면(<see cref="Quaternion.FromToRotation"/>) 축 둘레의 롤이 정해지지 않는다.
        /// 그 회전은 8면체의 대칭 회전이 아니라서 몸체 실루엣이 어긋나고, 숫자도 0°/60°/180°로
        /// 제각기 누워 정렬 결과가 여러 모양으로 갈린다. 6면과 같이 면 법선과 글자 윗방향을
        /// 함께 맞춰 8면체 대칭 회전만 나오게 한다.
        /// </summary>
        public static Quaternion GetOctaVisualRemapRotation(Quaternion landingRotation, int targetFaceIndex)
        {
            int physicalTop = GetOctaTopFace(landingRotation);
            Quaternion sourceBasis = Quaternion.LookRotation(
                GetOctaFaceNormal(targetFaceIndex),
                GetOctaFaceUpAxis(targetFaceIndex));
            Quaternion targetBasis = Quaternion.LookRotation(
                GetOctaFaceNormal(physicalTop),
                GetOctaFaceUpAxis(physicalTop));
            return targetBasis * Quaternion.Inverse(sourceBasis);
        }

        private static Vector3 GetOctaFaceNormal(int faceIndex)
        {
            if (faceIndex < 1 || faceIndex > OctaFaceNormals.Length) return OctaFaceNormals[0];
            return OctaFaceNormals[faceIndex - 1];
        }

        /// <summary>
        /// FBX 3D 모델의 Pip 위치를 측정하여 모델 자체의 고유한 기울임/회전 각도를 표준 직교 기저로 보정하는 Quaternion 반환
        /// </summary>
        public static Quaternion MeasureModelBasis(Transform visualRoot)
        {
            if (visualRoot == null) return Quaternion.identity;

            if (!TryMeasureFaceNormal(visualRoot, 1, out Vector3 actualUp)) return Quaternion.identity;
            if (!TryMeasureFaceNormal(visualRoot, 2, out Vector3 actualForward)) return Quaternion.identity;

            actualForward = Vector3.ProjectOnPlane(actualForward, actualUp).normalized;
            if (actualForward.sqrMagnitude < 0.5f) return Quaternion.identity;

            Quaternion modelBasis = Quaternion.LookRotation(actualForward, actualUp);
            return Quaternion.Inverse(modelBasis);
        }

        /// <summary>
        /// 프리셋 착지 회전에서 목표 눈이 상단으로 오며 목표 지향성(FaceUpAxis)이 물리적 지향성에 일치하도록 하는 Visual 직교 기저 회전 계산
        /// </summary>
        public static Quaternion GetVisualRemapRotation(Quaternion landingRotation, int targetTopValue, Quaternion baseCorrection)
        {
            int physicalTop = GetTopValue(landingRotation);

            Quaternion sourceBasis = Quaternion.LookRotation(
                FaceNormals[targetTopValue - 1],
                GetFaceUpAxis(targetTopValue));
            Quaternion targetBasis = Quaternion.LookRotation(
                FaceNormals[physicalTop - 1],
                GetFaceUpAxis(physicalTop));

            Quaternion step = targetBasis * Quaternion.Inverse(sourceBasis);
            return step * baseCorrection;
        }

        public static int[] GetRemappedFaceValues(Quaternion landingRotation, int targetTopValue)
        {
            if (targetTopValue < 1 || targetTopValue > FaceNormals.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(targetTopValue));
            }

            int physicalTopValue = GetTopValue(landingRotation);
            Quaternion labelRotation = Quaternion.Inverse(GetTopRotation(physicalTopValue, Vector3.back))
                * GetTopRotation(targetTopValue, Vector3.back);
            Quaternion inverseLabelRotation = Quaternion.Inverse(labelRotation);
            int[] values = new int[FaceNormals.Length];
            for (int physicalFace = 0; physicalFace < FaceNormals.Length; physicalFace++)
            {
                Vector3 labelDirection = inverseLabelRotation * FaceNormals[physicalFace];
                values[physicalFace] = GetClosestFaceValue(labelDirection);
            }

            return values;
        }

        private static int GetClosestFaceValue(Vector3 direction)
        {
            int value = 1;
            float bestDot = float.NegativeInfinity;
            for (int index = 0; index < FaceNormals.Length; index++)
            {
                float dot = Vector3.Dot(direction, FaceNormals[index]);
                if (dot <= bestDot) continue;
                bestDot = dot;
                value = index + 1;
            }

            return value;
        }

        public static bool TryMeasureFaceNormal(Transform dieRoot, int value, out Vector3 normal)
        {
            normal = Vector3.zero;
            if (dieRoot == null || value < 1 || value > FaceNormals.Length) return false;

            string prefix = $"Pip_{value}";
            int count = 0;
            foreach (Transform child in dieRoot.GetComponentsInChildren<Transform>(true))
            {
                if (!child.name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                normal += dieRoot.InverseTransformPoint(child.position);
                count++;
            }

            if (count == 0 || normal.sqrMagnitude < 0.0001f)
            {
                normal = FaceNormals[value - 1];
                return true;
            }

            normal.Normalize();
            return true;
        }
    }
}
