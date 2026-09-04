using System.Collections.Generic;
using NUnit.Framework;
using Tessera.Core;
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
                Kind = YachtAugmentKind.Modification
            };

            card.Bind(definition, AugmentCardDisplayState.Available);

            Assert.That(card.NameText.text, Is.EqualTo("럭키 세븐"));
            Assert.That(card.DescriptionText.text, Does.Contain("15점"));
            Assert.That(card.KindText.text, Is.EqualTo("변형"));
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

    [TestCase(AugmentCardDisplayState.Available, "[선택 가능]", true, 1f)]
    [TestCase(AugmentCardDisplayState.Selected, "[선택됨]", false, 2f)]
    [TestCase(AugmentCardDisplayState.Owned, "[보유 중]", false, 1f)]
    [TestCase(AugmentCardDisplayState.Conflict, "[충돌]", false, 2f)]
    [TestCase(AugmentCardDisplayState.Used, "[사용 완료]", false, 1f)]
    [TestCase(AugmentCardDisplayState.Disabled, "[비활성]", false, 1f)]
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
                Kind = YachtAugmentKind.Enhance
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
        try
        {
            Vector2 slotSize = new(4.58f, 2.58f);
            AugmentTrayCardView view = AugmentTrayCardView.Create(anchorObject.transform, slotSize, 0);

            // 카드 UI는 양피지 자식의 월드 스페이스 캔버스에 있다(M9.5).
            // 트레이를 옮기거나 호버로 떠오르면 계층 관계로 따라온다.
            Canvas worldCanvas = view.GetComponentInChildren<Canvas>(true);
            Assert.That(worldCanvas, Is.Not.Null);
            Assert.That(worldCanvas.renderMode, Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(view.OverlayRect.parent, Is.EqualTo(view.VisualRoot));
            Assert.That(view.OverlayRect.gameObject.layer, Is.EqualTo(TesseraLayers.CrispUI));
            Assert.That(view.ScrollModel, Is.Not.Null);
            Assert.That(view.GetComponentsInChildren<MeshFilter>(true), Has.Length.GreaterThanOrEqualTo(6));
            Assert.That(view.ScrollModel.WaxRenderer, Is.Not.Null);
            Assert.That(view.ScrollModel.OverlayAnchors.Count, Is.EqualTo(4));
            Assert.That(view.ScrollModel.HasCenteredSeal, Is.True);
            Assert.That(view.ScrollModel.CubeSealMark, Is.Not.Null);
            Assert.That(view.transform.Find("Parchment Visual Root/Augment Scroll Preset 0/Cyan Inner Border"), Is.Not.Null);
            Assert.That(view.Card.Background.color.a, Is.Zero);
            Assert.That(view.Card.CardOutline.enabled, Is.False);
            Assert.That(view.PointerCollider.size.x, Is.EqualTo(slotSize.x).Within(.001f));
            Assert.That(view.PointerCollider.size.z, Is.EqualTo(slotSize.y).Within(.001f));
            foreach (Graphic graphic in view.Card.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(anchorObject);
        }
    }

    [Test]
    public void TrayCard_호버와클릭선택을_높이와상태로표현한다()
    {
        GameObject anchorObject = new("Interactive Tray Slot Anchor");
        try
        {
            AugmentTrayCardView view = AugmentTrayCardView.Create(
                anchorObject.transform, new Vector2(4.58f, 2.58f), 0);
            view.Bind(new YachtAugmentDefinition
            {
                Id = YachtAugmentRuntime.LuckySevensId,
                DisplayName = "럭키 세븐",
                Description = "트레이 카드 상호작용 검증",
                Kind = YachtAugmentKind.Enhance
            }, (int)AugmentParchmentPreset.BottomTear);
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
        }
    }

    [Test]
    public void Parchment_네프리셋은_직사각형본문과2점5회말림및큐브인장을가진다()
    {
        var signatures = new HashSet<string>();
        foreach (AugmentParchmentPreset preset in System.Enum.GetValues(typeof(AugmentParchmentPreset)))
        {
            Mesh body = AugmentScrollModelFactory.CreatePaperBodyMesh(preset, 4.3f, 2.3f);
            Mesh roll = AugmentScrollModelFactory.CreateRolledLayersMesh(preset, 4.3f, 2.3f);
            Mesh band = AugmentScrollModelFactory.CreateSealBandMesh(preset, 4.3f, 2.3f);
            Mesh seal = AugmentScrollModelFactory.CreateWaxSealMesh(preset, 4.3f, 2.3f);
            Mesh mark = AugmentScrollModelFactory.CreateCubeSealMarkMesh(4.3f, 2.3f);
            Mesh border = AugmentScrollModelFactory.CreateInnerBorderMesh(4.3f, 2.3f);
            try
            {
                signatures.Add(AugmentParchmentVisuals.GetOutlineSignature(preset));
                Assert.That(body.vertexCount, Is.EqualTo(
                    AugmentScrollModelFactory.PaperColumns * AugmentScrollModelFactory.PaperRows * 2));
                Assert.That(body.subMeshCount, Is.EqualTo(2));
                Assert.That(body.uv, Has.Length.EqualTo(body.vertexCount));
                Assert.That(body.normals, Has.Length.EqualTo(body.vertexCount));
                Assert.That(body.tangents, Has.Length.EqualTo(body.vertexCount));
                Assert.That(body.bounds.min.x, Is.LessThanOrEqualTo(-1.655f));

                Assert.That(roll.vertexCount, Is.EqualTo(
                    AugmentScrollModelFactory.RollAxisSegments * AugmentScrollModelFactory.RollSpiralSegments * 2));
                Assert.That(roll.subMeshCount, Is.EqualTo(2));
                Assert.That(roll.bounds.size.y, Is.GreaterThan(.35f));
                Assert.That(roll.bounds.size.z, Is.GreaterThan(.25f));
                Assert.That(AugmentScrollModelFactory.RollTurns, Is.EqualTo(2.5f));
                Assert.That(band.vertexCount, Is.EqualTo(56));

                Assert.That(seal.vertexCount, Is.GreaterThan(50));
                Assert.That(seal.bounds.size.x, Is.GreaterThan(.30f));
                Assert.That(seal.bounds.size.z, Is.GreaterThan(.30f));
                Assert.That(mark.vertexCount, Is.EqualTo(36));
                Assert.That(border.vertexCount, Is.EqualTo(16));
            }
            finally
            {
                Object.DestroyImmediate(body);
                Object.DestroyImmediate(roll);
                Object.DestroyImmediate(band);
                Object.DestroyImmediate(seal);
                Object.DestroyImmediate(mark);
                Object.DestroyImmediate(border);
            }
        }
        Assert.That(signatures.Count, Is.EqualTo(AugmentParchmentVisuals.PresetCount));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void Parchment_정적3D프리팹과선택프리뷰는_네프리셋을제공한다(int presetId)
    {
        GameObject prefab = Resources.Load<GameObject>($"AugmentScrolls/AugmentScrollPreset_{presetId}");
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent<AugmentScrollModel>(), Is.Not.Null);
        Assert.That(prefab.GetComponentsInChildren<MeshFilter>(true), Has.Length.GreaterThanOrEqualTo(6));
        Assert.That(prefab.transform.Find("Embossed Cube Seal Mark"), Is.Not.Null);
        Assert.That(prefab.transform.Find("Cyan Inner Border"), Is.Not.Null);
        Assert.That(prefab.transform.Find("Pixel Readable Roll Layers"), Is.Null);
        Transform roll = prefab.transform.Find("Left Rolled Paper 2.5 Turns");
        Transform band = prefab.transform.Find("Leather Seal Band");
        Transform wax = prefab.transform.Find("Crimson Wax Seal");
        Assert.That(roll, Is.Not.Null);
        Assert.That(band, Is.Not.Null);
        Assert.That(wax, Is.Not.Null);
        Assert.That(roll.localEulerAngles.z, Is.EqualTo(AugmentScrollModelFactory.RollRotationZ).Within(.01f));
        Assert.That(band.localPosition, Is.EqualTo(roll.localPosition));
        Assert.That(wax.localPosition, Is.EqualTo(roll.localPosition));
        Assert.That(prefab.transform.Find("Ribbon Tail"), Is.Null);
        Assert.That(prefab.transform.Find("Iron Rod"), Is.Null);
        Assert.That(prefab.transform.Find("Metal Rod"), Is.Null);
        Assert.That(Resources.Load<Sprite>($"AugmentScrolls/Previews/AugmentScrollPreview_{presetId}"), Is.Not.Null);
    }

    [Test]
    public void CommonCard_왼쪽장식거터를비운_공통안전영역을사용한다()
    {
        GameObject canvasObject = new("Augment Safe Area Test Canvas", typeof(Canvas));
        try
        {
            AugmentCardView card = AugmentCardView.Create(
                canvasObject.transform, "Safe Area Card", Vector2.zero,
                new Vector2(460f, 460f / AugmentCardView.TrayCardAspectRatio),
                new Vector2(.5f, .5f), null);
            Rect expected = AugmentParchmentVisuals.ContentSafeRect;
            Assert.That(card.ContentRoot.anchorMin.x, Is.EqualTo(expected.xMin).Within(.001f));
            Assert.That(card.ContentRoot.anchorMax.x, Is.EqualTo(expected.xMax).Within(.001f));
            Assert.That(card.ContentRoot.anchorMin.x, Is.GreaterThanOrEqualTo(.20f));
            Assert.That(card.NameText.transform.IsChildOf(card.ContentRoot), Is.True);
            Assert.That(card.DescriptionText.transform.IsChildOf(card.ContentRoot), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
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
                Kind = YachtAugmentKind.Enhance
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
