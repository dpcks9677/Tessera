using NUnit.Framework;
using Tessera.Games.AugmentedYacht;
using Tessera.Games.Yacht;
using UnityEngine;
using UnityEngine.UI;

public sealed class AugmentCardViewTests
{
    [Test]
    public void CommonCard_이름효과종류대상상태를_같은레이아웃에표시한다()
    {
        GameObject canvasObject = new("Augment Card Test Canvas", typeof(Canvas));
        try
        {
            AugmentCardView card = AugmentCardView.Create(
                canvasObject.transform,
                "Test Card",
                Vector2.zero,
                new Vector2(460f, 460f / AugmentCardView.TrayCardAspectRatio),
                new Vector2(0.5f, 0.5f),
                null);
            var definition = new YachtAugmentDefinition
            {
                Id = YachtAugmentRuntime.LuckySevensId,
                DisplayName = "럭키 세븐",
                Description = "눈금 총합 조건을 만족하면 15점을 얻습니다.",
                Target = "Aces",
                Kind = YachtAugmentKind.ScoreReplacement
            };

            card.Bind(definition, AugmentCardDisplayState.Available);

            Assert.That(card.NameText.text, Is.EqualTo("럭키 세븐"));
            Assert.That(card.DescriptionText.text, Does.Contain("15점"));
            Assert.That(card.KindText.text, Is.EqualTo("족보 교체"));
            Assert.That(card.TargetText.text, Is.EqualTo("대상 · 에이스"));
            Assert.That(card.StateText.text, Is.EqualTo("선택 가능"));
            Assert.That(card.Button.interactable, Is.True);
            Assert.That(card.DescriptionText.text.Length, Is.LessThanOrEqualTo(20));
            RectTransform rect = card.GetComponent<RectTransform>();
            Assert.That(rect.sizeDelta.x / rect.sizeDelta.y, Is.EqualTo(AugmentCardView.TrayCardAspectRatio).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void CommonCard_임시픽셀아이콘은_64픽셀Point필터를사용한다()
    {
        GameObject canvasObject = new("Augment Icon Test Canvas", typeof(Canvas));
        try
        {
            AugmentCardView card = AugmentCardView.Create(
                canvasObject.transform,
                "Test Card",
                Vector2.zero,
                new Vector2(460f, 460f / AugmentCardView.TrayCardAspectRatio),
                new Vector2(0.5f, 0.5f),
                null);
            card.Bind(new YachtAugmentDefinition
            {
                Id = YachtAugmentRuntime.WeightedDiceId,
                DisplayName = "묵직한 주사위",
                Description = "주사위 면 구성을 바꿉니다.",
                Kind = YachtAugmentKind.Dice
            }, AugmentCardDisplayState.Available);

            Texture2D texture = card.Icon.sprite.texture;
            Assert.That(texture.width, Is.EqualTo(64));
            Assert.That(texture.height, Is.EqualTo(64));
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }
}
