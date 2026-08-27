using System.Collections.Generic;
using NUnit.Framework;
using Tessera.Games.AugmentedYacht;
using Tessera.Games.Yacht;
using Tessera.Tabletop;
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
            Assert.That(card.StateText.text, Is.EqualTo("[선택 가능]"));
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

    [TestCase(AugmentCardDisplayState.Available, "[선택 가능]", true, 3f)]
    [TestCase(AugmentCardDisplayState.Selected, "[선택됨]", false, 5f)]
    [TestCase(AugmentCardDisplayState.Owned, "[보유 중]", false, 3f)]
    [TestCase(AugmentCardDisplayState.Conflict, "[충돌]", false, 5f)]
    [TestCase(AugmentCardDisplayState.Used, "[사용 완료]", false, 3f)]
    [TestCase(AugmentCardDisplayState.Disabled, "[비활성]", false, 3f)]
    public void CommonCard_상태별문구강조입력여부를_즉시구분한다(
        AugmentCardDisplayState state,
        string expectedLabel,
        bool expectedInteractable,
        float expectedOutlineDistance)
    {
        GameObject canvasObject = new("Augment State Test Canvas", typeof(Canvas));
        try
        {
            AugmentCardView card = AugmentCardView.Create(
                canvasObject.transform,
                "State Test Card",
                Vector2.zero,
                new Vector2(460f, 460f / AugmentCardView.TrayCardAspectRatio),
                new Vector2(0.5f, 0.5f),
                null);
            card.Bind(new YachtAugmentDefinition
            {
                Id = YachtAugmentRuntime.LuckySevensId,
                DisplayName = "럭키 세븐",
                Description = "상태 표현 검증용 카드입니다.",
                Kind = YachtAugmentKind.Enhancement
            }, state);

            Assert.That(card.DisplayState, Is.EqualTo(state));
            Assert.That(card.StateText.text, Is.EqualTo(expectedLabel));
            Assert.That(card.Button.interactable, Is.EqualTo(expectedInteractable));
            Assert.That(card.StateAccent.color, Is.EqualTo(card.StateText.color));
            Assert.That(Mathf.Abs(card.CardOutline.effectDistance.x), Is.EqualTo(expectedOutlineDistance));
            Assert.That(card.Button.colors.disabledColor, Is.EqualTo(Color.white));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void CommonCard_여섯상태는_서로다른배경색을사용한다()
    {
        GameObject canvasObject = new("Augment State Palette Test Canvas", typeof(Canvas));
        try
        {
            AugmentCardView card = AugmentCardView.Create(
                canvasObject.transform,
                "State Palette Test Card",
                Vector2.zero,
                new Vector2(460f, 460f / AugmentCardView.TrayCardAspectRatio),
                new Vector2(0.5f, 0.5f),
                null);
            var colors = new HashSet<Color>();

            foreach (AugmentCardDisplayState state in System.Enum.GetValues(typeof(AugmentCardDisplayState)))
            {
                card.SetState(state);
                colors.Add(card.Background.color);
            }

            Assert.That(colors.Count, Is.EqualTo(6));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void TrayCard_고해상도오버레이와3D양피지를_슬롯에배치한다()
    {
        GameObject anchorObject = new("Tray Slot Anchor");
        GameObject cameraObject = new("Tray Card Camera", typeof(Camera));
        GameObject canvasObject = new("Pixel Presentation", typeof(Canvas));
        try
        {
            Canvas overlayCanvas = canvasObject.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Vector2 slotSize = new(4.58f, 2.58f);
            AugmentTrayCardView view = AugmentTrayCardView.Create(
                anchorObject.transform,
                cameraObject.GetComponent<Camera>(),
                overlayCanvas,
                slotSize,
                0);

            Assert.That(view.GetComponentInChildren<Canvas>(true), Is.Null);
            Assert.That(view.OverlayRect.parent, Is.EqualTo(overlayCanvas.transform));
            Assert.That(view.ScrollModel, Is.Not.Null);
            Assert.That(view.GetComponentsInChildren<MeshFilter>(true), Has.Length.GreaterThanOrEqualTo(5));
            Assert.That(view.ScrollModel.WaxRenderer, Is.Not.Null);
            Assert.That(view.ScrollModel.OverlayAnchors.Count, Is.EqualTo(4));
            Assert.That(view.ScrollModel.HasCenteredSeal, Is.True);
            Assert.That(view.Card.Background.color.a, Is.Zero);
            Assert.That(view.Card.CardOutline.enabled, Is.False);
            Assert.That(view.PointerCollider.size.x, Is.LessThan(slotSize.x));
            Assert.That(view.PointerCollider.size.z, Is.LessThan(slotSize.y));
            foreach (Graphic graphic in view.Card.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(anchorObject);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void TrayCard_호버와클릭선택을_높이와상태로표현한다()
    {
        GameObject anchorObject = new("Interactive Tray Slot Anchor");
        GameObject cameraObject = new("Interactive Tray Card Camera", typeof(Camera));
        GameObject canvasObject = new("Interactive Card Overlay", typeof(Canvas));
        try
        {
            AugmentTrayCardView view = AugmentTrayCardView.Create(
                anchorObject.transform,
                cameraObject.GetComponent<Camera>(),
                canvasObject.GetComponent<Canvas>(),
                new Vector2(4.58f, 2.58f),
                0);
            view.Bind(new YachtAugmentDefinition
            {
                Id = YachtAugmentRuntime.LuckySevensId,
                DisplayName = "럭키 세븐",
                Description = "트레이 카드 상호작용 검증",
                Kind = YachtAugmentKind.Enhancement
            }, (int)AugmentParchmentPreset.Scalloped);
            view.SetVisible(true);

            float restingHeight = view.VisualRoot.localPosition.y;
            float restingScale = view.VisualRoot.localScale.x;
            view.SetHovered(true);
            view.TickHover(0.06f);
            Assert.That(view.VisualRoot.localPosition.y, Is.GreaterThan(restingHeight));
            Assert.That(view.VisualRoot.localPosition.y, Is.LessThan(0.16f));
            Assert.That(view.VisualRoot.localScale.x, Is.GreaterThan(restingScale));
            for (int i = 0; i < 20; i++) view.TickHover(0.06f);
            Assert.That(view.VisualRoot.localScale.x, Is.EqualTo(1.06f).Within(0.002f));

            view.SetSelected(true);
            Assert.That(view.IsSelected, Is.True);
            Assert.That(view.Card.DisplayState, Is.EqualTo(AugmentCardDisplayState.Selected));
            view.SetHovered(false);
            for (int i = 0; i < 20; i++) view.TickHover(0.06f);
            Assert.That(view.VisualRoot.localPosition.y, Is.EqualTo(restingHeight).Within(0.002f));
        }
        finally
        {
            Object.DestroyImmediate(anchorObject);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void Parchment_다섯프리셋은_펼친본문과다층롤및밀랍인장을가진다()
    {
        var signatures = new HashSet<string>();
        foreach (AugmentParchmentPreset preset in System.Enum.GetValues(typeof(AugmentParchmentPreset)))
        {
            Mesh body = AugmentScrollModelFactory.CreatePaperBodyMesh(preset, 4.3f, 2.3f);
            Mesh roll = AugmentScrollModelFactory.CreateRolledLayersMesh(preset, 4.3f, 2.3f);
            Mesh seal = AugmentScrollModelFactory.CreateWaxSealMesh(preset, 4.3f, 2.3f);
            try
            {
                signatures.Add(AugmentParchmentVisuals.GetOutlineSignature(preset));
                Assert.That(body.vertexCount, Is.EqualTo(
                    AugmentScrollModelFactory.PaperColumns * AugmentScrollModelFactory.PaperRows * 2));
                Assert.That(body.subMeshCount, Is.EqualTo(2));
                Assert.That(body.uv, Has.Length.EqualTo(body.vertexCount));
                Assert.That(body.normals, Has.Length.EqualTo(body.vertexCount));
                Assert.That(body.tangents, Has.Length.EqualTo(body.vertexCount));

                Assert.That(roll.vertexCount, Is.EqualTo(
                    AugmentScrollModelFactory.RollAxisSegments * AugmentScrollModelFactory.RollSpiralSegments * 2));
                Assert.That(roll.subMeshCount, Is.EqualTo(2));
                Assert.That(roll.bounds.size.y, Is.GreaterThan(.35f));
                Assert.That(roll.bounds.size.z, Is.GreaterThan(.25f));

                Assert.That(seal.vertexCount, Is.GreaterThan(50));
                Assert.That(seal.bounds.size.x, Is.GreaterThan(.30f));
                Assert.That(seal.bounds.size.z, Is.GreaterThan(.30f));
            }
            finally
            {
                Object.DestroyImmediate(body);
                Object.DestroyImmediate(roll);
                Object.DestroyImmediate(seal);
            }
        }
        Assert.That(signatures.Count, Is.EqualTo(AugmentParchmentVisuals.PresetCount));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void Parchment_정적3D프리팹은_다섯프리셋을제공한다(int presetId)
    {
        GameObject prefab = Resources.Load<GameObject>($"AugmentScrolls/AugmentScrollPreset_{presetId}");
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent<AugmentScrollModel>(), Is.Not.Null);
        Assert.That(prefab.GetComponentsInChildren<MeshFilter>(true), Has.Length.GreaterThanOrEqualTo(5));
    }

    [Test]
    public void CardTray_세슬롯앵커와카드비율을제공한다()
    {
        GameObject parent = new("Augment Tray Test Parent");
        try
        {
            AugmentCardTray tray = AugmentCardTray.Create(parent.transform, Vector3.zero);
            Assert.That(tray.SlotCount, Is.EqualTo(3));
            for (int i = 0; i < tray.SlotCount; i++)
                Assert.That(tray.GetSlotAnchor(i), Is.Not.Null);
            Assert.That(tray.CardSlotLocalSize.x / tray.CardSlotLocalSize.y,
                Is.EqualTo(tray.CardSlotAspectRatio).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(parent);
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
