using NUnit.Framework;
using Tessera.Core;
using Tessera.Games.Yacht.Presentation;
using Tessera.Rendering;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 월드 좌표를 픽셀 격자 칸 중심으로 옮기는 스냅 계약을 고정한다.
    /// </summary>
    [TestFixture]
    public sealed class PixelGridSnapTests
    {
        private static readonly Vector2Int Resolution = new(480, 270);

        private GameObject cameraObject;
        private Camera camera;

        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("PixelGridSnapTestCamera");
            camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = PixelFilterSettings.WorldOrthographicSize;
            camera.aspect = 16f / 9f;
            cameraObject.transform.SetPositionAndRotation(
                new Vector3(0f, 20f, -5f), Quaternion.Euler(YachtSceneAssembler.CameraPitchAngle, 0f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            if (cameraObject != null) Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void ToCellCenterPutsPointsOnCellCenters()
        {
            Vector3[] points =
            {
                DiceBoardMetrics.GetActivePosition(0, 5),
                DiceBoardMetrics.GetActivePosition(3, 5),
                DiceBoardMetrics.GetKeepPosition(0),
                DiceBoardMetrics.GetKeepPosition(4),
                new Vector3(0.123f, 1.7f, -0.456f),
            };
            float cell = 2f * PixelFilterSettings.WorldOrthographicSize / Resolution.y;
            float maxMove = Mathf.Sqrt(2f) * 0.5f * cell + 1e-4f;

            foreach (Vector3 point in points)
            {
                Vector3 snapped = PixelGridSnap.ToCellCenter(camera, Resolution, point);
                Vector3 viewport = camera.WorldToViewportPoint(snapped);
                float cellX = viewport.x * Resolution.x;
                float cellY = viewport.y * Resolution.y;

                Assert.AreEqual(0.5f, cellX - Mathf.Floor(cellX), 1e-3f);
                Assert.AreEqual(0.5f, cellY - Mathf.Floor(cellY), 1e-3f);
                Assert.LessOrEqual(Vector3.Distance(point, snapped), maxMove);
            }
        }

        [Test]
        public void ToCellCenterKeepsPointAlreadyOnCellCenter()
        {
            Vector3 once = PixelGridSnap.ToCellCenter(camera, Resolution, new Vector3(0.3f, 1.2f, 0.4f));
            Vector3 twice = PixelGridSnap.ToCellCenter(camera, Resolution, once);

            Assert.AreEqual(0f, Vector3.Distance(once, twice), 1e-4f);
        }

        [Test]
        public void ToCellCenterReturnsInputWhenCameraIsNull()
        {
            Vector3 point = new(0.3f, 1.2f, 0.4f);
            Assert.AreEqual(point, PixelGridSnap.ToCellCenter(null, Resolution, point));
        }

        [Test]
        public void ToCellCenterReturnsInputWhenResolutionIsZero()
        {
            Vector3 point = new(0.3f, 1.2f, 0.4f);
            Assert.AreEqual(point, PixelGridSnap.ToCellCenter(camera, Vector2Int.zero, point));
        }
    }
}
