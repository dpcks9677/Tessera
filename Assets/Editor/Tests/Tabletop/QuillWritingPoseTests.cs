using NUnit.Framework;
using Tessera.Tabletop;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 깃펜 필기 자세 규칙을 고정합니다. 근거는 <c>docs/quill_hover_writing_animation_plan.md</c> §3.2입니다.
    /// 이 연출에서 화면 없이 검증할 수 있는 유일한 지점입니다.
    ///
    /// 규칙은 자세를 새로 만들지 않는다는 것입니다. 잉크통에 꽂혀 있을 때의 기울기를 그대로 들고 와
    /// 칸 위에 놓고, 종이 세로축 기준으로만 돌립니다. 두 플레이어가 같은 자세를 씁니다.
    /// </summary>
    [TestFixture]
    public sealed class QuillWritingPoseTests
    {
        private static readonly Vector3 SheetUp = Vector3.up;

        /// <summary>P1 점수 칸 중앙의 월드 좌표입니다.</summary>
        private static readonly Vector3 P1Cell = new(9.922f, -0.38f, 0f);

        /// <summary>P2 점수 칸 중앙입니다. 시트 중심 x = 9.28을 축으로 P1과 대칭입니다.</summary>
        private static readonly Vector3 P2Cell = new(8.638f, -0.38f, 0f);

        /// <summary>씬에서 잰 꽂힘 자세입니다. 깃털이 +Z에서 15.5도, 종이에서 46도로 섭니다.</summary>
        private static readonly Quaternion Docked = Quaternion.Euler(40f, -65f, 20f);

        private const float Yaw = 15f;

        [Test]
        public void ZeroRotationKeepsDockedPose()
        {
            Pose(P1Cell, 0f, out _, out Quaternion rotation);
            Assert.That(Quaternion.Angle(rotation, Docked), Is.LessThan(0.01f));
        }

        [Test]
        public void PositiveAngleIsClockwiseOnScreen()
        {
            // 카메라가 yaw 없이 내려다보므로 화면 오른쪽이 +X, 화면 위쪽이 +Z입니다.
            // 위에서 오른쪽으로 도는 것이 시계방향이고, 그것이 +Z에서 +X로 가는 방향입니다.
            float before = YawFromPlusZ(Feather(0f));
            float after = YawFromPlusZ(Feather(Yaw));

            Assert.That(after, Is.GreaterThan(before));
            Assert.That(after - before, Is.EqualTo(Yaw).Within(0.01f));
        }

        [Test]
        public void VerticalAxisRotationKeepsTiltAgainstPaper()
        {
            Assert.That(Feather(Yaw).y, Is.EqualTo(Feather(0f).y).Within(1e-4f));
        }

        [Test]
        public void BothPlayersShareSamePose()
        {
            // 좌우반전은 폐기했습니다. 자세는 칸 위치와 무관합니다.
            QuillHoverAnimator.ResolveWritingPose(P1Cell, Docked, SheetUp, Yaw, out _, out Quaternion p1);
            QuillHoverAnimator.ResolveWritingPose(P2Cell, Docked, SheetUp, Yaw, out _, out Quaternion p2);

            Assert.That(Quaternion.Angle(p1, p2), Is.LessThan(0.01f));
        }

        [Test]
        public void NibSitsDirectlyAboveCellCenter()
        {
            Pose(P1Cell, Yaw, out Vector3 position, out _);

            Assert.That(position.x, Is.EqualTo(P1Cell.x).Within(1e-4f));
            Assert.That(position.z, Is.EqualTo(P1Cell.z).Within(1e-4f));
            Assert.That(position.y - P1Cell.y, Is.EqualTo(0.02f).Within(1e-4f));
        }

        [Test]
        public void RotationAngleDoesNotMoveNibPosition()
        {
            Pose(P1Cell, 0f, out Vector3 straight, out _);
            Pose(P1Cell, Yaw, out Vector3 turned, out _);

            Assert.That(Vector3.Distance(straight, turned), Is.LessThan(1e-4f));
        }

        [Test]
        public void RotationAxisIsPaperVerticalNotWorldAxis()
        {
            // 종이를 기울이면 회전축도 따라 기웁니다.
            Vector3 tiltedUp = (Quaternion.Euler(0f, 0f, 30f) * Vector3.up).normalized;
            QuillHoverAnimator.ResolveWritingPose(P1Cell, Docked, tiltedUp, Yaw, out _, out Quaternion rotation);

            Vector3 turned = rotation * Vector3.up;
            Vector3 straight = Docked * Vector3.up;

            // 기운 축을 따라 돌았으므로 그 축에 대한 성분은 보존됩니다.
            Assert.That(Vector3.Dot(turned, tiltedUp), Is.EqualTo(Vector3.Dot(straight, tiltedUp)).Within(1e-4f));
            Assert.That(Vector3.Angle(turned, straight), Is.GreaterThan(1f));
        }

        private static void Pose(Vector3 cell, float yawDegrees, out Vector3 position, out Quaternion rotation) =>
            QuillHoverAnimator.ResolveWritingPose(cell, Docked, SheetUp, yawDegrees, out position, out rotation);

        /// <summary>펜의 로컬 +Y가 닙에서 깃털 팁으로 뻗는 축입니다.</summary>
        private static Vector3 Feather(float yawDegrees)
        {
            Pose(P1Cell, yawDegrees, out _, out Quaternion rotation);
            return rotation * Vector3.up;
        }

        /// <summary>+Z에서 +X 쪽으로 잰 각도입니다. 화면에서는 위에서 오른쪽으로 도는 방향입니다.</summary>
        private static float YawFromPlusZ(Vector3 direction) =>
            Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
    }
}
