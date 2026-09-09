using NUnit.Framework;
using Tessera.Tabletop;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 깃펜 깃털 외형 규칙을 고정합니다(M17-T19). 근거는 InkwellAndQuill의 FeatherSlitAlpha,
    /// FeatherAlbedo, FeatherNotchScale 순수 함수입니다.
    ///
    /// 이 세 함수는 UV 좌표에서 깃가지 사이 틈(알파 클립), 깃털 색(웜 그레이 팔레트),
    /// 노치 메시 변형(비대칭 파임)을 결정합니다. 화면 확인 없이 검증할 수 있는 유일한 지점입니다.
    /// </summary>
    [TestFixture]
    public sealed class QuillFeatherAppearanceTests
    {
        [Test]
        public void 깃대_부근은_뚫리지_않는다()
        {
            for (float u = 0.40f; u <= 0.60f; u += 0.02f)
            {
                for (float v = 0f; v <= 1f; v += 0.05f)
                {
                    float alpha = InkwellAndQuill.FeatherSlitAlpha(u, v);
                    Assert.That(alpha, Is.EqualTo(0f).Or.EqualTo(1f), $"u={u}, v={v}에서 이진값이 아닙니다.");
                    Assert.That(alpha, Is.EqualTo(1f), $"u={u}, v={v}는 깃대 부근인데 뚫렸습니다.");
                }
            }
        }

        [Test]
        public void 뿌리와_팁은_막혀_있다()
        {
            for (float u = 0f; u <= 1f; u += 0.1f)
            {
                foreach (float v in new[] { 0f, 0.02f, 0.04f, 0.90f, 0.94f, 0.98f, 1f })
                {
                    Assert.That(InkwellAndQuill.FeatherSlitAlpha(u, v), Is.EqualTo(1f), $"u={u}, v={v}");
                }
            }
        }

        [Test]
        public void 외곽은_실제로_갈라진다()
        {
            bool sawOpen = false;
            bool sawClosed = false;
            for (float v = 0.35f; v <= 0.45f; v += 0.001f)
            {
                float alpha = InkwellAndQuill.FeatherSlitAlpha(0.02f, v);
                if (alpha == 0f) sawOpen = true;
                if (alpha == 1f) sawClosed = true;
            }

            Assert.That(sawOpen, Is.True, "외곽 스캔에서 뚫린 지점(알파 0)이 하나도 없습니다.");
            Assert.That(sawClosed, Is.True, "외곽 스캔에서 막힌 지점(알파 1)이 하나도 없습니다.");
        }

        [Test]
        public void 좌우_대칭이라_Repeat_랩_이음매가_없다()
        {
            for (float u = 0f; u <= 0.5f; u += 0.03f)
            {
                for (float v = 0f; v <= 1f; v += 0.07f)
                {
                    Assert.That(
                        InkwellAndQuill.FeatherSlitAlpha(u, v),
                        Is.EqualTo(InkwellAndQuill.FeatherSlitAlpha(1f - u, v)),
                        $"u={u}, v={v}에서 좌우 대칭이 깨졌습니다.");
                }
            }
        }

        [Test]
        public void 슬릿_수가_픽셀_예산_안에_있다()
        {
            int transitions = 0;
            float previous = InkwellAndQuill.FeatherSlitAlpha(0.02f, 0.16f);
            for (float v = 0.16f; v <= 0.72f; v += 0.0005f)
            {
                float current = InkwellAndQuill.FeatherSlitAlpha(0.02f, v);
                if (previous == 1f && current == 0f) transitions++;
                previous = current;
            }

            Assert.That(transitions, Is.GreaterThanOrEqualTo(5).And.LessThanOrEqualTo(9));
        }

        [Test]
        public void 슬릿이_메시_노치와_정렬된다()
        {
            for (int n = 1; n <= 4; n++)
            {
                float t = n / 6f;
                Assert.That(InkwellAndQuill.FeatherSlitAlpha(0f, t), Is.EqualTo(0f), $"n={n}, t={t}");
            }
        }

        [Test]
        public void 팔레트에_갈색이_없다()
        {
            const float tolerance = 1e-4f;
            for (float u = 0f; u <= 1f; u += 0.1f)
            {
                for (float v = 0f; v <= 1f; v += 0.1f)
                {
                    Color color = InkwellAndQuill.FeatherAlbedo(u, v);
                    float max = Mathf.Max(color.r, color.g, color.b);
                    float min = Mathf.Min(color.r, color.g, color.b);

                    Assert.That(color.r, Is.GreaterThanOrEqualTo(color.g - tolerance), $"u={u}, v={v}");
                    Assert.That(color.g, Is.GreaterThanOrEqualTo(color.b - tolerance), $"u={u}, v={v}");
                    Assert.That(color.r, Is.GreaterThanOrEqualTo(0.75f - tolerance), $"u={u}, v={v}");
                    Assert.That(max - min, Is.LessThanOrEqualTo(0.10f + tolerance), $"u={u}, v={v}");
                }
            }
        }

        [Test]
        public void 노치가_깊어졌지만_비대칭을_뒤집지_않는다()
        {
            float min = float.MaxValue;
            for (float t = 0f; t <= 1f; t += 1f / 96f)
            {
                min = Mathf.Min(min, InkwellAndQuill.FeatherNotchScale(t, -1f));
            }

            Assert.That(min, Is.GreaterThanOrEqualTo(0.60f).And.LessThanOrEqualTo(0.68f));

            foreach (float colFactor in new[] { 0f, 0.001f, 5f })
            {
                for (float t = 0f; t <= 1f; t += 0.1f)
                {
                    Assert.That(InkwellAndQuill.FeatherNotchScale(t, colFactor), Is.EqualTo(1f), $"colFactor={colFactor}, t={t}");
                }
            }
        }

        [Test]
        public void 결정성()
        {
            const float u = 0.37f;
            const float v = 0.61f;
            const float t = 0.5f;
            const float colFactor = -1f;

            Assert.That(InkwellAndQuill.FeatherSlitAlpha(u, v), Is.EqualTo(InkwellAndQuill.FeatherSlitAlpha(u, v)));
            Assert.That(InkwellAndQuill.FeatherAlbedo(u, v), Is.EqualTo(InkwellAndQuill.FeatherAlbedo(u, v)));
            Assert.That(InkwellAndQuill.FeatherNotchScale(t, colFactor), Is.EqualTo(InkwellAndQuill.FeatherNotchScale(t, colFactor)));

            float alpha = InkwellAndQuill.FeatherSlitAlpha(u, v);
            Assert.That(alpha, Is.EqualTo(0f).Or.EqualTo(1f));
        }
    }
}
