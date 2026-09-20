using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using Tessera.Core;
using Tessera.Games.AugmentedYacht;
using Tessera.Games.Yacht;
using Tessera.Tabletop;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UI;

namespace Tessera.Editor.Tests
{
    public sealed class AugmentCardViewTests
    {
        [Test]
        public void CommonCard_ShowsNameEffectAndKindInSameLayout()
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
                Assert.That(card.KindText.text, Is.EqualTo("변형"));
                Assert.That(card.Button.interactable, Is.True);
                // 설명은 더 이상 잘리지 않고 워드랩으로 전문이 들어간다.
                Assert.That(card.DescriptionText.text, Is.EqualTo(definition.Description));
                Assert.That(card.DescriptionText.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap));
                RectTransform rect = card.GetComponent<RectTransform>();
                Assert.That(rect.sizeDelta.x / rect.sizeDelta.y, Is.EqualTo(AugmentCardView.TrayCardAspectRatio).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [TestCase(AugmentCardDisplayState.Available, true, 1f)]
        [TestCase(AugmentCardDisplayState.Selected, false, 2f)]
        [TestCase(AugmentCardDisplayState.Owned, false, 1f)]
        [TestCase(AugmentCardDisplayState.Conflict, false, 2f)]
        [TestCase(AugmentCardDisplayState.Used, false, 1f)]
        [TestCase(AugmentCardDisplayState.Disabled, false, 1f)]
        public void CommonCard_DistinguishesHighlightAndInputStateAtOnce(
            AugmentCardDisplayState state,
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
                Assert.That(card.Button.interactable, Is.EqualTo(expectedInteractable));
                Assert.That(Mathf.Abs(card.CardOutline.effectDistance.x), Is.EqualTo(expectedOutlineDistance));
                Assert.That(card.Button.colors.disabledColor, Is.EqualTo(Color.white));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void CommonCard_SixStatesUseDistinctBackgroundColors()
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
        public void TrayCard_PlacesCrispOverlayAndParchmentInSlot()
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
                Assert.That(view.GetComponentsInChildren<MeshFilter>(true), Has.Length.GreaterThanOrEqualTo(5));
                Assert.That(view.ScrollModel.WaxRenderer, Is.Not.Null);
                Assert.That(view.ScrollModel.OverlayAnchors.Count, Is.EqualTo(4));
                Assert.That(view.ScrollModel.HasCenteredSeal, Is.True);
                Assert.That(view.ScrollModel.CubeSealMark, Is.Not.Null);
                // 하늘색 네온 테두리는 픽셀 필터 격자에 걸려 깜빡였고 양피지 디자인과도 맞지 않아 폐기했다.
                Assert.That(view.transform.Find("Parchment Visual Root/Augment Scroll Preset 0/Cyan Inner Border"), Is.Null);
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
        public void TrayCard_ExpressesHoverAndClickByHeightAndState()
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
        public void Parchment_FourPresetsHaveRectBodyCurlAndCubeSeal()
        {
            var signatures = new HashSet<string>();
            foreach (AugmentParchmentPreset preset in System.Enum.GetValues(typeof(AugmentParchmentPreset)))
            {
                Mesh body = AugmentScrollModelFactory.CreatePaperBodyMesh(preset, 4.3f, 2.3f);
                Mesh roll = AugmentScrollModelFactory.CreateRolledLayersMesh(preset, 4.3f, 2.3f);
                Mesh band = AugmentScrollModelFactory.CreateSealBandMesh(preset, 4.3f, 2.3f);
                Mesh seal = AugmentScrollModelFactory.CreateWaxSealMesh(preset, 4.3f, 2.3f);
                Mesh mark = AugmentScrollModelFactory.CreateCubeSealMarkMesh(4.3f, 2.3f);
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
                }
                finally
                {
                    Object.DestroyImmediate(body);
                    Object.DestroyImmediate(roll);
                    Object.DestroyImmediate(band);
                    Object.DestroyImmediate(seal);
                    Object.DestroyImmediate(mark);
                }
            }
            Assert.That(signatures.Count, Is.EqualTo(AugmentParchmentVisuals.PresetCount));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Parchment_StaticPrefabAndPreviewProvideFourPresets(int presetId)
        {
            GameObject prefab = Resources.Load<GameObject>($"AugmentScrolls/AugmentScrollPreset_{presetId}");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<AugmentScrollModel>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<MeshFilter>(true), Has.Length.GreaterThanOrEqualTo(5));
            Assert.That(prefab.transform.Find("Embossed Cube Seal Mark"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Cyan Inner Border"), Is.Null);
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
        public void CommonCard_UsesSafeAreaWithEmptyLeftGutter()
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
        public void CommonCard_StacksHeaderAndBodyTopToBottom()
        {
            GameObject canvasObject = new("Augment Row Order Test Canvas", typeof(Canvas));
            try
            {
                AugmentCardView card = AugmentCardView.Create(
                    canvasObject.transform, "Row Order Card", Vector2.zero,
                    new Vector2(460f, 460f / AugmentCardView.TrayCardAspectRatio),
                    new Vector2(.5f, .5f), null);

                // 이름과 종류 배지가 한 헤더 행을 이루고, 그 아래로 상태 강조선 · 설명 본문이 놓인다.
                float nameTop = TopMargin(card.NameText.rectTransform);
                float kindTop = TopMargin(card.KindText.rectTransform);
                float accentTop = TopMargin(card.StateAccent.rectTransform);
                float bodyTop = TopMargin(card.DescriptionText.rectTransform);

                Assert.That(kindTop, Is.EqualTo(nameTop).Within(.001f));
                Assert.That(nameTop, Is.LessThan(accentTop));
                Assert.That(accentTop, Is.LessThan(bodyTop));
                // 제목 행과 본문 사이 여백.
                Assert.That(bodyTop - accentTop, Is.EqualTo(16f).Within(.001f));
                // 본문 아래에는 그리는 행이 없어도 여백이 남는다.
                Assert.That(card.DescriptionText.rectTransform.offsetMin.y, Is.GreaterThan(0f));
                Assert.That(card.NameText.alignment, Is.EqualTo(TextAnchor.MiddleLeft));
                Assert.That(card.KindText.alignment, Is.EqualTo(TextAnchor.MiddleRight));
                Assert.That(card.DescriptionText.alignment, Is.EqualTo(TextAnchor.UpperLeft));
                Assert.That(card.Icon.transform.IsChildOf(card.ContentRoot), Is.True);
                // 종류 배지와 본문은 각각 기본 배지·본문보다 크게 읽힌다.
                Assert.That(card.KindText.fontSize, Is.EqualTo(17));
                Assert.That(card.DescriptionText.resizeTextMinSize, Is.EqualTo(14));
                Assert.That(card.DescriptionText.resizeTextMaxSize, Is.EqualTo(19));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        /// <summary>부모 사각형 위쪽 변에서 잰 거리. 값이 작을수록 카드 위쪽 행이다.</summary>
        private static float TopMargin(RectTransform rect) => -rect.offsetMax.y;

        [Test]
        public void CardTray_ProvidesThreeSlotAnchorsAndCardRatio()
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
        public void CommonCard_FallbackPixelIconUses64pxPointFilter()
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

        private static AugmentCardView CreateCard(out GameObject canvasObject, string name = "Progress Test Canvas")
        {
            canvasObject = new GameObject(name, typeof(Canvas));
            return AugmentCardView.Create(
                canvasObject.transform, "Test Card", Vector2.zero,
                new Vector2(460f, 460f / AugmentCardView.TrayCardAspectRatio),
                new Vector2(.5f, .5f), null);
        }

        private static YachtAugmentDefinition QuestDefinition() => new()
        {
            Id = YachtAugmentRuntime.HoldoutId,
            DisplayName = "알박기",
            Description = "9턴 이후 풀하우스에 득점하면 +7점입니다.",
            Kind = YachtAugmentKind.Quest
        };

        [Test]
        public void NonQuestCard_LeavesProgressBlockInactiveAndBodyOffsetUnchanged()
        {
            AugmentCardView card = CreateCard(out GameObject canvasObject);
            try
            {
                var definition = new YachtAugmentDefinition
                {
                    Id = YachtAugmentRuntime.LuckySevensId,
                    DisplayName = "럭키 세븐",
                    Description = "눈금 총합 조건을 만족하면 15점을 얻습니다.",
                    Target = "Aces",
                    Kind = YachtAugmentKind.Modification
                };

                card.Bind(definition, AugmentCardDisplayState.Available);

                Assert.That(card.ProgressBlockRoot.activeSelf, Is.False);
                // progress 없는 카드는 본문 bottom 여백이 footerHeight+6(=28)로 유지된다.
                Assert.That(card.DescriptionText.rectTransform.offsetMin.y, Is.EqualTo(28f).Within(.001f));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void NonQuestCard_HidesStatusLabelButQuestCard_ShowsIt()
        {
            AugmentCardView card = CreateCard(out GameObject canvasObject);
            try
            {
                var lines = new[] { new AugmentProgressLine("앞면 2개 · 리롤 +1", true) };
                var enhanceDefinition = new YachtAugmentDefinition
                {
                    Id = YachtAugmentRuntime.CoinTossId,
                    DisplayName = "코인 토스",
                    Description = "동전 3개를 던져 앞면 수에 따라 효과가 갈립니다.",
                    Kind = YachtAugmentKind.Enhance
                };
                card.Bind(enhanceDefinition, AugmentCardDisplayState.Owned,
                    progress: new AugmentProgress(AugmentProgressOutcome.Succeeded, lines));

                Assert.That(card.StatusLabel.gameObject.activeSelf, Is.False);

                var lines2 = new[] { new AugmentProgressLine("9턴 이후에 Full House 기입", false) };
                card.Bind(QuestDefinition(), AugmentCardDisplayState.Owned,
                    progress: new AugmentProgress(AugmentProgressOutcome.InProgress, lines2));

                Assert.That(card.StatusLabel.gameObject.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void NonQuestCard_FooterLineHasNoStrikeOrQuestPrefix()
        {
            AugmentCardView card = CreateCard(out GameObject canvasObject);
            try
            {
                var lines = new[] { new AugmentProgressLine("앞면 2개 · 리롤 +1", true) };
                var enhanceDefinition = new YachtAugmentDefinition
                {
                    Id = YachtAugmentRuntime.CoinTossId,
                    DisplayName = "코인 토스",
                    Description = "동전 3개를 던져 앞면 수에 따라 효과가 갈립니다.",
                    Kind = YachtAugmentKind.Enhance
                };
                card.Bind(enhanceDefinition, AugmentCardDisplayState.Owned,
                    progress: new AugmentProgress(AugmentProgressOutcome.Succeeded, lines));

                AugmentCardView.ProgressRow row = card.ProgressRows[0];
                Assert.That(row.Text.text, Does.Not.Contain("<s>"), "비퀘스트 카드 푸터에는 취소선이 없어야 한다.");
                Assert.That(row.Text.text, Does.Not.Contain("퀘스트"), "비퀘스트 카드 푸터에는 '퀘스트' 접두가 없어야 한다.");
                Assert.That(row.Text.text, Is.EqualTo("앞면 2개 · 리롤 +1"));
                Assert.That(row.Group.alpha, Is.EqualTo(1f).Within(.001f));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void QuestCard_ReservesBlockHeightWithoutOverlappingBody()
        {
            AugmentCardView card = CreateCard(out GameObject canvasObject);
            try
            {
                var lines = new[]
                {
                    new AugmentProgressLine("9턴 이후에 Full House 기입", false)
                };
                card.Bind(QuestDefinition(), AugmentCardDisplayState.Owned,
                    progress: new AugmentProgress(AugmentProgressOutcome.InProgress, lines));

                Assert.That(card.ProgressBlockRoot.activeSelf, Is.True);
                // 본문 bottom 여백이 (실측 블록 높이 - FooterBleed)와 같아야 겹치지 않는다.
                Assert.That(card.DescriptionText.rectTransform.offsetMin.y,
                    Is.EqualTo(card.ProgressBlockHeight - AugmentCardView.FooterBleed).Within(.001f));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [TestCase(AugmentProgressOutcome.InProgress, "퀘스트 진행 중")]
        [TestCase(AugmentProgressOutcome.Succeeded, "퀘스트 성공")]
        [TestCase(AugmentProgressOutcome.Failed, "퀘스트 실패")]
        public void QuestCard_ShowsWebOriginalStatusLabel(AugmentProgressOutcome outcome, string expectedLabel)
        {
            AugmentCardView card = CreateCard(out GameObject canvasObject);
            try
            {
                var lines = new[] { new AugmentProgressLine("9턴 이후에 Full House 기입", outcome == AugmentProgressOutcome.Succeeded) };
                card.Bind(QuestDefinition(), AugmentCardDisplayState.Owned, progress: new AugmentProgress(outcome, lines));

                Assert.That(card.StatusLabel.text, Is.EqualTo(expectedLabel));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void QuestCard_StrikesThroughDoneLineButNotPendingLine()
        {
            AugmentCardView card = CreateCard(out GameObject canvasObject);
            try
            {
                var lines = new[]
                {
                    new AugmentProgressLine("완료된 목표", true),
                    new AugmentProgressLine("남은 목표", false)
                };
                card.Bind(QuestDefinition(), AugmentCardDisplayState.Owned,
                    progress: new AugmentProgress(AugmentProgressOutcome.InProgress, lines));

                AugmentCardView.ProgressRow doneRow = card.ProgressRows[0];
                AugmentCardView.ProgressRow pendingRow = card.ProgressRows[1];

                Assert.That(doneRow.Text.text, Does.Contain("<s>"), "달성 줄에는 취소선이 켜져야 한다.");
                Assert.That(pendingRow.Text.text, Does.Not.Contain("<s>"), "진행 중 줄에는 취소선이 없어야 한다.");
                Assert.That(doneRow.Group.alpha, Is.EqualTo(0.7f).Within(.001f));
                Assert.That(pendingRow.Group.alpha, Is.EqualTo(1f).Within(.001f));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void QuestCard_FailedOutcomeStrikesAllLinesWithLowerOpacityOnPendingOnes()
        {
            AugmentCardView card = CreateCard(out GameObject canvasObject);
            try
            {
                var lines = new[]
                {
                    new AugmentProgressLine("완료된 목표", true),
                    new AugmentProgressLine("실패한 목표", false)
                };
                card.Bind(QuestDefinition(), AugmentCardDisplayState.Owned,
                    progress: new AugmentProgress(AugmentProgressOutcome.Failed, lines));

                AugmentCardView.ProgressRow doneRow = card.ProgressRows[0];
                AugmentCardView.ProgressRow failedRow = card.ProgressRows[1];

                Assert.That(doneRow.Text.text, Does.Contain("<s>"));
                Assert.That(failedRow.Text.text, Does.Contain("<s>"), "실패 시 미달성 줄도 취소선이 켜져야 한다.");
                Assert.That(doneRow.Group.alpha, Is.EqualTo(0.7f).Within(.001f));
                Assert.That(failedRow.Group.alpha, Is.EqualTo(0.6f).Within(.001f));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void QuestCard_RowHasNoUnderline()
        {
            AugmentCardView card = CreateCard(out GameObject canvasObject);
            try
            {
                var lines = new[] { new AugmentProgressLine("9턴 이후에 Full House 기입", false) };
                card.Bind(QuestDefinition(), AugmentCardDisplayState.Owned,
                    progress: new AugmentProgress(AugmentProgressOutcome.InProgress, lines));

                AugmentCardView.ProgressRow row = card.ProgressRows[0];
                Assert.That(row.Text.text, Does.Not.Contain("<u>"));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void QuestCard_TargetNoteRowHasNoUnderline()
        {
            AugmentCardView card = CreateCard(out GameObject canvasObject);
            try
            {
                var lines = new[]
                {
                    new AugmentProgressLine("타겟으로 지정된 족보를 3회 기입하기 (0/3)", false),
                    new AugmentProgressLine("미지정", false, isTargetNote: true)
                };
                card.Bind(QuestDefinition(), AugmentCardDisplayState.Owned,
                    progress: new AugmentProgress(AugmentProgressOutcome.InProgress, lines));

                Assert.That(card.ProgressRows[1].Text.text, Does.Not.Contain("<u>"));
                Assert.That(card.ProgressRows[1].Text.text, Does.Contain("현재 타겟"));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void QuestCard_LongestDescriptionStillFitsReservedBodyAtMinimumBestFitSize()
        {
            // Copycat이 11종 중 설명 본문이 가장 길다. best-fit 결과(레이아웃 패스 필요)를 직접 믿지 않고,
            // 최소 크기(14pt, resizeTextMinSize)로 TextGenerator를 직접 돌려 줄 수 x 줄 높이가
            // 본문 rect 높이 안에 들어오는지 본다. 레이아웃 패스와 무관하다.
            AugmentCardView card = CreateCard(out GameObject canvasObject);
            try
            {
                var definition = new YachtAugmentDefinition
                {
                    Id = YachtAugmentRuntime.CopycatId,
                    DisplayName = "카피캣",
                    Description = "상대가 이미 기입한 족보를 따라 기입합니다. 초이스 이상에서 동점 기입 시 즉시, 또는 3회 누적 시 +10점입니다.",
                    Kind = YachtAugmentKind.Quest
                };
                var lines = new[] { new AugmentProgressLine("상대방이 이미 기입한 족보와 동일한 족보 기입 (0/3)", false) };
                card.Bind(definition, AugmentCardDisplayState.Owned,
                    progress: new AugmentProgress(AugmentProgressOutcome.InProgress, lines));

                Text body = card.DescriptionText;
                var generator = new TextGenerator();
                TextGenerationSettings settings = body.GetGenerationSettings(new Vector2(body.rectTransform.rect.width, 1000f));
                settings.fontSize = body.resizeTextMinSize;
                settings.resizeTextForBestFit = false;
                generator.Populate(body.text, settings);

                float ppu = Mathf.Max(1f, body.pixelsPerUnit);
                float totalHeight = 0f;
                foreach (UILineInfo line in generator.lines) totalHeight += line.height / ppu;

                Assert.That(totalHeight, Is.LessThanOrEqualTo(body.rectTransform.rect.height),
                    "Copycat 설명 본문이 최소 폰트 크기에서도 진행 블록이 줄인 영역을 넘칩니다.");
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void ProgressRow_UsesMulmaruFontBeforeMeasuringVisualLines()
        {
            // 폰트 폴백이 일어나면 아래 시각 줄 측정이 전부 무의미해지므로 먼저 전제를 확인한다.
            AugmentCardView card = CreateCard(out GameObject canvasObject);
            try
            {
                var lines = new[] { new AugmentProgressLine("9턴 이후에 Full House 기입", false) };
                card.Bind(QuestDefinition(), AugmentCardDisplayState.Owned,
                    progress: new AugmentProgress(AugmentProgressOutcome.InProgress, lines));

                TMP_FontAsset font = card.ProgressRows[0].Text.font;
                Assert.That(font, Is.Not.Null);
                Assert.That(font.faceInfo.familyName, Does.Contain("Mulmaru"),
                    $"진행 행 폰트가 Mulmaru가 아니라 {font.faceInfo.familyName}으로 폴백됐습니다. 이후 시각 줄 측정을 신뢰할 수 없습니다.");
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void ProgressFont_StrikethroughSitsInsideHangulGlyphMiddle()
        {
            // 픽셀 폰트는 OS/2 취소선 메트릭이 없어 LoadProgressFont가 '가' 글리프 기준으로 직접 보정한다.
            // 그 보정값이 실제로 글리프 세로 중앙 부근에 있는지 확인한다.
            AugmentCardView card = CreateCard(out GameObject canvasObject);
            try
            {
                var lines = new[] { new AugmentProgressLine("완료된 목표", true) };
                card.Bind(QuestDefinition(), AugmentCardDisplayState.Owned,
                    progress: new AugmentProgress(AugmentProgressOutcome.InProgress, lines));

                TMP_FontAsset font = card.ProgressRows[0].Text.font;
                Assert.That(font.characterLookupTable.TryGetValue('가', out TMP_Character ch), Is.True);
                GlyphMetrics metrics = ch.glyph.metrics;
                float bearingY = metrics.horizontalBearingY;
                float height = metrics.height;
                float offset = font.faceInfo.strikethroughOffset;

                Assert.That(offset, Is.GreaterThan(0f));
                Assert.That(offset, Is.InRange(bearingY - height * 0.75f, bearingY - height * 0.25f));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void QuestCard_ProgressBlockHeightShrinksWithFewerRows()
        {
            AugmentCardView oneRowCard = CreateCard(out GameObject oneRowCanvas, "Progress Height Test Canvas 1");
            AugmentCardView threeRowCard = CreateCard(out GameObject threeRowCanvas, "Progress Height Test Canvas 3");
            try
            {
                var oneLine = new[] { new AugmentProgressLine("A", false) };
                var threeLines = new[]
                {
                    new AugmentProgressLine("A", false),
                    new AugmentProgressLine("B", false),
                    new AugmentProgressLine("C", false)
                };
                oneRowCard.Bind(QuestDefinition(), AugmentCardDisplayState.Owned,
                    progress: new AugmentProgress(AugmentProgressOutcome.InProgress, oneLine));
                threeRowCard.Bind(QuestDefinition(), AugmentCardDisplayState.Owned,
                    progress: new AugmentProgress(AugmentProgressOutcome.InProgress, threeLines));

                Assert.That(oneRowCard.ProgressBlockHeight, Is.GreaterThan(0f));
                Assert.That(oneRowCard.ProgressBlockHeight, Is.LessThan(threeRowCard.ProgressBlockHeight));
            }
            finally
            {
                Object.DestroyImmediate(oneRowCanvas);
                Object.DestroyImmediate(threeRowCanvas);
            }
        }

        /// <summary>
        /// 11종 각각 가장 긴 상태로 만든 <see cref="AugmentProgress"/>다. 실제 State.DescribeProgress를
        /// 그대로 호출해 만들므로 문구가 로직과 항상 일치한다.
        /// </summary>
        private static IEnumerable<TestCaseData> WorstCaseQuestProgressCases()
        {
            var query = new AugmentProgressQuery(new YachtAugmentPlayerState(), new PlayerScoreData());
            yield return new TestCaseData(YachtAugmentRuntime.FastStraightId,
                (AugmentProgress)new FastStraightState { SmallScored = true }.DescribeProgress(query));
            yield return new TestCaseData(YachtAugmentRuntime.NoTimeToWasteId,
                (AugmentProgress)new NoTimeToWasteState { RemainingTurns = 1 }.DescribeProgress(query));
            yield return new TestCaseData(YachtAugmentRuntime.StepByStepId,
                (AugmentProgress)new StepByStepState { CategoryIndex = 3 }.DescribeProgress(query));
            yield return new TestCaseData(YachtAugmentRuntime.HoldoutId,
                (AugmentProgress)new HoldoutState().DescribeProgress(query));
            yield return new TestCaseData(YachtAugmentRuntime.CautiousStraightId,
                (AugmentProgress)new CautiousStraightState { SmallScored = true }.DescribeProgress(query));
            yield return new TestCaseData(YachtAugmentRuntime.EveryLittleId,
                (AugmentProgress)new EveryLittleCountsState { Count = 3 }.DescribeProgress(query));
            // Copycat "(조건 달성!)" — Rewarded인데 3회 미만일 때만 붙는, 가장 긴 문구다.
            yield return new TestCaseData(YachtAugmentRuntime.CopycatId,
                (AugmentProgress)new CopycatState { Count = 1, Rewarded = true }.DescribeProgress(query));
            yield return new TestCaseData(YachtAugmentRuntime.DoublingId,
                (AugmentProgress)new DoublingState().DescribeProgress(query));
            yield return new TestCaseData(YachtAugmentRuntime.NozdormuId,
                (AugmentProgress)new NozdormuState { TargetTurn = 12 }.DescribeProgress(query));
            // BountyHunter 미완료 + 두 줄짜리 타겟명("4 of a Kind")이 가장 길다.
            yield return new TestCaseData(YachtAugmentRuntime.BountyHunterId,
                (AugmentProgress)new BountyHunterState { Successes = 1, TargetCategory = (int)ScoreCategory.FourOfAKind }.DescribeProgress(query));
            // Prophet 두 자리 숫자 3개.
            yield return new TestCaseData(YachtAugmentRuntime.ProphetId,
                (AugmentProgress)new ProphetState { TurnsRemaining = 1, Targets = new[] { 12, 25, 30 } }.DescribeProgress(query));
        }

        [TestCaseSource(nameof(WorstCaseQuestProgressCases))]
        public void QuestCard_WorstCaseWording_KeepsVisualLineSumWithinThree(string augmentId, AugmentProgress progress)
        {
            AugmentCardView card = CreateCard(out GameObject canvasObject);
            try
            {
                var definition = new YachtAugmentDefinition
                {
                    Id = augmentId,
                    DisplayName = augmentId,
                    Description = "설명",
                    Kind = YachtAugmentKind.Quest
                };
                card.Bind(definition, AugmentCardDisplayState.Owned, progress: progress);

                int totalVisualLines = 0;
                foreach (AugmentCardView.ProgressRow row in card.ProgressRows)
                {
                    if (!row.Rect.gameObject.activeSelf) continue;
                    totalVisualLines += AugmentCardView.CountVisualLines(row.Text);
                }

                Assert.That(totalVisualLines, Is.LessThanOrEqualTo(3),
                    $"{augmentId}: 진행 블록 시각 줄 합이 3을 넘습니다(실제 {totalVisualLines}줄). 문구를 줄이거나 슬롯을 늘려야 합니다.");
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [TestCaseSource(nameof(WorstCaseQuestProgressCases))]
        public void QuestCard_ActiveRowsStackTopToBottomWithoutOverlap(string augmentId, AugmentProgress progress)
        {
            AugmentCardView card = CreateCard(out GameObject canvasObject);
            try
            {
                var definition = new YachtAugmentDefinition { Id = augmentId, DisplayName = augmentId, Description = "설명", Kind = YachtAugmentKind.Quest };
                card.Bind(definition, AugmentCardDisplayState.Owned, progress: progress);

                float previousBottom = float.PositiveInfinity;
                foreach (AugmentCardView.ProgressRow row in card.ProgressRows)
                {
                    if (!row.Rect.gameObject.activeSelf) continue;
                    (float top, float bottom) = AugmentCardView.RowBlockLocalRange(row.Rect);
                    Assert.That(top, Is.LessThanOrEqualTo(previousBottom + .01f),
                        $"{augmentId}: 행이 위에서 아래로 순서대로 놓이지 않았거나 겹칩니다.");
                    Assert.That(bottom, Is.LessThanOrEqualTo(top),
                        $"{augmentId}: 행 bottom이 top보다 위에 있습니다.");
                    previousBottom = bottom;
                }
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void QuestCard_WrappedDoneLineStrikesThroughEveryVisualLine()
        {
            AugmentCardView card = CreateCard(out GameObject canvasObject);
            try
            {
                var query = new AugmentProgressQuery(new YachtAugmentPlayerState(), new PlayerScoreData());
                AugmentProgress progress = new CopycatState { Count = 1, Rewarded = true }.DescribeProgress(query);
                var definition = new YachtAugmentDefinition { Id = YachtAugmentRuntime.CopycatId, DisplayName = "카피캣", Description = "설명", Kind = YachtAugmentKind.Quest };
                card.Bind(definition, AugmentCardDisplayState.Owned, progress: progress);

                AugmentCardView.ProgressRow row = card.ProgressRows[0];
                Assert.That(AugmentCardView.CountVisualLines(row.Text), Is.GreaterThanOrEqualTo(2),
                    "이 케이스는 카드 폭에서 두 시각 줄로 넘어가야 줄바꿈 취소선을 검증할 수 있다.");

                // TMP는 <s> 태그 구간이 줄바꿈되면 시각 줄마다 각각 선을 긋는다.
                Assert.That(row.Text.text, Does.StartWith("<s>"));
                Assert.That(row.Text.text, Does.EndWith("</s>"));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void TrayCard_ProgressLineIsVisibleOnSameCardAndLayerAsText()
        {
            // 실제 게임 화면은 AugmentCardView.Create가 아니라 AugmentTrayCardView.Create → Bind 경로를
            // 탄다. 이 경로에서 진행도가 텍스트와 다른 카드·레이어·활성 상태에 놓이면 화면에서만
            // 선이 안 보이는 문제가 생길 수 있어, 그 경로를 그대로 재현해 확인한다.
            GameObject anchorObject = new("Tray Progress Regression Anchor");
            try
            {
                Vector2 slotSize = new(4.58f, 2.58f);
                AugmentTrayCardView view = AugmentTrayCardView.Create(anchorObject.transform, slotSize, 0);
                var query = new AugmentProgressQuery(new YachtAugmentPlayerState(), new PlayerScoreData());
                AugmentProgress progress = new FastStraightState { SmallScored = true }.DescribeProgress(query);
                var definition = new YachtAugmentDefinition
                {
                    Id = YachtAugmentRuntime.FastStraightId,
                    DisplayName = "재빠른 스트레이트",
                    Description = "설명",
                    Kind = YachtAugmentKind.Quest
                };
                view.Bind(definition, (int)AugmentParchmentPreset.GentleWave, progress: progress);
                view.SetVisible(true);

                // 트레이 카드 하나에는 AugmentCardView가 하나뿐이어야 한다. 둘 이상이면 진행도가
                // 화면에 보이지 않는 카드에 Bind됐을 수 있다.
                AugmentCardView[] cardsInHierarchy = view.GetComponentsInChildren<AugmentCardView>(true);
                Assert.That(cardsInHierarchy, Has.Length.EqualTo(1),
                    $"트레이 카드 하나에 AugmentCardView가 {cardsInHierarchy.Length}개 있습니다.");
                Assert.That(cardsInHierarchy[0], Is.SameAs(view.Card));

                AugmentCardView card = view.Card;
                Assert.That(card.ProgressBlockRoot.activeInHierarchy, Is.True,
                    "진행 블록이 활성인 카드를 찾지 못했습니다. 진행도가 다른 카드에 Bind됐을 수 있습니다.");

                AugmentCardView.ProgressRow row = card.ProgressRows[0];
                Assert.That(row.Text.enabled, Is.True, "row.Text가 비활성입니다 — 텍스트도 안 보일 것입니다.");
                Assert.That(row.Text.text, Does.Contain("<s>"), "취소선 태그가 빠졌습니다.");
                Assert.That(row.Text.gameObject.layer, Is.EqualTo(TesseraLayers.CrispUI));
                Assert.That(row.Text.color.a, Is.GreaterThan(0f), "텍스트 색 알파가 0입니다.");

                float combinedAlpha = 1f;
                foreach (CanvasGroup group in row.Text.GetComponentsInParent<CanvasGroup>(true)) combinedAlpha *= group.alpha;
                Assert.That(combinedAlpha, Is.GreaterThan(0f), "텍스트를 덮은 CanvasGroup 알파 곱이 0입니다.");
            }
            finally
            {
                Object.DestroyImmediate(anchorObject);
            }
        }

        private static YachtAugmentDefinition TableFlipDefinition() => new()
        {
            Id = YachtAugmentRuntime.TableFlipId,
            DisplayName = "판 뒤집기",
            Description = "발동 버튼 검증용 정의입니다.",
            Kind = YachtAugmentKind.Enhance
        };

        [Test]
        public void UseButton_SitsInFooterRightEdgeInsideCardMask()
        {
            AugmentCardView card = CreateCard(out GameObject canvasObject);
            try
            {
                Assert.That(card.UseActionCardRect.width, Is.EqualTo(77f).Within(.01f));
                Assert.That(card.UseActionCardRect.height, Is.EqualTo(26f).Within(.01f));
                Assert.That(card.UseActionRect.parent, Is.EqualTo(card.ContentRoot));
                // 오른쪽 끝 여백 4px.
                Assert.That(-card.UseActionRect.offsetMax.x, Is.EqualTo(4f).Within(.001f));

                // 윗변만 Target Badge(targetText)와 같은 값을 쓴다. 아랫변은 버튼을 키우며 푸터 아래
                // 양피지 여백으로 더 내려가 더 이상 같지 않다.
                Assert.That(card.UseActionRect.offsetMax.y, Is.EqualTo(card.TargetText.rectTransform.offsetMax.y).Within(.001f));

                // 본문 아랫변보다 버튼 윗변이 아래에 있어야 겹치지 않는다.
                float buttonTop = card.UseActionRect.offsetMin.y + card.UseActionCardRect.height;
                float bodyBottom = card.DescriptionText.rectTransform.offsetMin.y;
                Assert.That(buttonTop, Is.LessThanOrEqualTo(bodyBottom));

                // 카드 사각형(RectMask2D가 잘라내는 경계) 안에 완전히 들어간다.
                RectTransform cardRect = card.GetComponent<RectTransform>();
                float halfWidth = cardRect.sizeDelta.x / 2f;
                float halfHeight = cardRect.sizeDelta.y / 2f;
                Assert.That(card.UseActionCardRect.xMax, Is.LessThanOrEqualTo(halfWidth));
                Assert.That(card.UseActionCardRect.yMin, Is.GreaterThanOrEqualTo(-halfHeight));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void UseButton_StartsHiddenUntilTurnedOn()
        {
            GameObject anchorObject = new("Use Action Visibility Anchor");
            try
            {
                AugmentTrayCardView view = AugmentTrayCardView.Create(anchorObject.transform, new Vector2(4.58f, 2.58f), 0);
                Assert.That(view.Card.UseActionVisible, Is.False, "생성 직후에는 숨겨져 있어야 한다.");

                view.Bind(TableFlipDefinition(), (int)AugmentParchmentPreset.GentleWave);
                view.SetUseAction(true, true);
                Assert.That(view.Card.UseActionVisible, Is.True);

                view.Bind(TableFlipDefinition(), (int)AugmentParchmentPreset.GentleWave);
                Assert.That(view.Card.UseActionVisible, Is.False, "재바인딩 시 이전 상태가 남지 않고 숨김으로 되돌아가야 한다.");
            }
            finally
            {
                Object.DestroyImmediate(anchorObject);
            }
        }

        [Test]
        public void UseButton_UsesFlatPrintedLookWithoutShadow()
        {
            AugmentCardView card = CreateCard(out GameObject canvasObject);
            try
            {
                Assert.That(card.UseActionRect.GetComponentsInChildren<Shadow>(true), Is.Empty);
                Assert.That(card.UseActionRect.GetComponentsInChildren<Outline>(true), Is.Empty);
                Assert.That(card.UseActionLabel.text, Is.EqualTo("사용"));
                Assert.That(card.UseActionLabel.fontSize, Is.EqualTo(17));
                // Ink와 같은 값이다(AugmentCardView 내부 색 상수는 비공개라 값으로 비교한다).
                Assert.That(card.UseActionLabel.color, Is.EqualTo(new Color(0.16f, 0.10f, 0.07f, 1f)));

                // 양피지 위에서 fill(밝은 앤틱 골드 톤)이 테두리(잉크 쪽으로 누른 톤)보다 밝아야 한다.
                // 정확한 리터럴이 아니라 관계로 검사해 팔레트 값이 바뀌어도 이 테스트는 안 깨진다.
                Image fill = card.UseActionRect.Find("Use Action Fill").GetComponent<Image>();
                Color fillColor = fill.color;
                Color borderColor = card.UseActionBorderColor;
                Assert.That(fillColor.r + fillColor.g + fillColor.b, Is.GreaterThan(borderColor.r + borderColor.g + borderColor.b));

                // 테두리·fill·후광 세 장 모두 빌트인 둥근 사각형 스프라이트를 9-슬라이스로 쓴다.
                Image border = card.UseActionRect.GetComponent<Image>();
                Transform glow = card.ContentRoot.Find("Use Action Glow");
                Image glowImage = glow.GetComponent<Image>();
                foreach (Image roundedImage in new[] { border, fill, glowImage })
                {
                    Assert.That(roundedImage.sprite, Is.Not.Null);
                    Assert.That(roundedImage.type, Is.EqualTo(Image.Type.Sliced));
                    // 1이면 9-슬라이스 테두리가 100배로 부풀어 모서리만이 아니라 버튼 전체가 늘어난다.
                    Assert.That(roundedImage.pixelsPerUnit, Is.EqualTo(1f).Within(.001f));
                }

                Assert.That(glowImage.raycastTarget, Is.False);
                foreach (Graphic graphic in card.UseActionRect.GetComponentsInChildren<Graphic>(true))
                    Assert.That(graphic.raycastTarget, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void UseButton_DisabledDimsAndSuppressesGlow()
        {
            GameObject anchorObject = new("Use Action Disabled Anchor");
            try
            {
                AugmentTrayCardView view = AugmentTrayCardView.Create(anchorObject.transform, new Vector2(4.58f, 2.58f), 0);
                view.Bind(TableFlipDefinition(), (int)AugmentParchmentPreset.GentleWave);
                view.SetVisible(true);
                view.SetUseAction(true, false);

                Assert.That(view.Card.UseActionEnabled, Is.False);
                CanvasGroup group = view.Card.UseActionRect.GetComponent<CanvasGroup>();
                Assert.That(group.alpha, Is.LessThan(1f));

                Color before = view.Card.UseActionBorderColor;
                view.SetUseActionHovered(true);
                for (int i = 0; i < 4; i++) view.TickHover(0.05f);

                Assert.That(view.UseActionHoverAmount, Is.Zero, "비활성 버튼은 호버해도 호버량이 오르지 않아야 한다.");
                Assert.That(view.Card.UseActionBorderColor, Is.EqualTo(before));
            }
            finally
            {
                Object.DestroyImmediate(anchorObject);
            }
        }

        [Test]
        public void UseButton_HoverFadesBorderToNeonBlue()
        {
            GameObject anchorObject = new("Use Action Hover Anchor");
            try
            {
                AugmentTrayCardView view = AugmentTrayCardView.Create(anchorObject.transform, new Vector2(4.58f, 2.58f), 0);
                view.Bind(TableFlipDefinition(), (int)AugmentParchmentPreset.GentleWave);
                view.SetVisible(true);
                view.SetUseAction(true, true);
                Image fill = view.Card.UseActionRect.Find("Use Action Fill").GetComponent<Image>();
                Color fillBefore = fill.color;
                view.SetUseActionHovered(true);

                // dt*5f 0.2초 규칙: 0.05f씩 4회면 정확히 1에 도달한다.
                for (int i = 0; i < 4; i++) view.TickHover(0.05f);

                Assert.That(view.UseActionHoverAmount, Is.EqualTo(1f).Within(.001f));
                Color border = view.Card.UseActionBorderColor;
                Assert.That(border.b, Is.GreaterThan(border.r));
                Assert.That(border.b, Is.GreaterThan(border.g));
                Assert.That(view.Card.UseActionGlowColor.a, Is.EqualTo(0.30f).Within(.01f));
                // 이번 수정의 핵심 요구: 호버가 최대여도 fill 색은 그대로여야 한다.
                Assert.That(fill.color, Is.EqualTo(fillBefore));

                view.SetUseActionHovered(false);
                for (int i = 0; i < 4; i++) view.TickHover(0.05f);
                Assert.That(view.UseActionHoverAmount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(anchorObject);
            }
        }

        [Test]
        public void TrayCard_UseActionColliderMatchesButtonRect()
        {
            GameObject anchorObject = new("Use Action Collider Anchor");
            try
            {
                Vector2 slotSize = new(4.58f, 2.58f);
                AugmentTrayCardView view = AugmentTrayCardView.Create(anchorObject.transform, slotSize, 0);
                view.Bind(TableFlipDefinition(), (int)AugmentParchmentPreset.GentleWave);
                view.SetVisible(true);
                view.SetUseAction(true, true);

                Assert.That(view.UseActionCollider.gameObject.activeSelf, Is.True);
                Assert.That(view.UseActionCollider.transform.parent, Is.EqualTo(view.VisualRoot));
                Assert.That(view.UseActionCollider.size.y, Is.EqualTo(0.02f).Within(.0001f));

                // 카드 픽셀 → 양피지 로컬 배율. AugmentTrayCardView.CardPixelWidth(460, 비공개)와 같은 값이다.
                float cardWidth = view.OverlayRect.sizeDelta.x / 100f;
                float worldPerCardPixel = cardWidth / 460f;
                Rect local = view.Card.UseActionCardRect;
                Assert.That(view.UseActionCollider.size.x, Is.EqualTo(local.width * worldPerCardPixel).Within(.0001f));
                Assert.That(view.UseActionCollider.size.z, Is.EqualTo(local.height * worldPerCardPixel).Within(.0001f));

                // 카드 사각형 안, 오른쪽 아래 푸터에 있어야 한다. 캔버스 +y는 Euler(90,0,0)에 의해 +z로 간다.
                Vector3 overlayPosition = view.OverlayRect.localPosition;
                Assert.That(view.UseActionCollider.transform.localPosition.x, Is.GreaterThan(overlayPosition.x));
                Assert.That(view.UseActionCollider.transform.localPosition.z, Is.LessThan(overlayPosition.z));

                view.SetUseAction(false, false);
                Assert.That(view.UseActionCollider.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(anchorObject);
            }
        }

        [Test]
        public void RoundedRectSprite_CornersAreTransparentAndCenterIsOpaque()
        {
            Color32[] pixels = RoundedRectSprite.CreatePixels(
                RoundedRectSprite.DefaultSize, RoundedRectSprite.DefaultSize, RoundedRectSprite.DefaultRadius);
            int size = RoundedRectSprite.DefaultSize;

            Assert.That(pixels[(0 * size) + 0].a, Is.EqualTo(0), "좌하단 모서리는 투명해야 한다.");
            Assert.That(pixels[(0 * size) + (size - 1)].a, Is.EqualTo(0), "우하단 모서리는 투명해야 한다.");
            Assert.That(pixels[((size - 1) * size) + 0].a, Is.EqualTo(0), "좌상단 모서리는 투명해야 한다.");
            Assert.That(pixels[((size - 1) * size) + (size - 1)].a, Is.EqualTo(0), "우상단 모서리는 투명해야 한다.");

            Color32 center = pixels[((size / 2) * size) + (size / 2)];
            Assert.That(center.a, Is.EqualTo(255), "중앙은 불투명해야 한다.");
            Assert.That(center.r, Is.EqualTo(255), "색은 흰색이어야 한다 — 실제 색은 Image.color가 입힌다.");
        }
    }
}
