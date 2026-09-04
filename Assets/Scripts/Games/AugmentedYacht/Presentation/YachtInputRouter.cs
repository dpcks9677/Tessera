using System;
using Tessera.Dice;
using Tessera.Tabletop;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 입력 장치를 읽어 의미 있는 사건으로 바꿔 알린다(M10-T1).
    ///
    /// 이 클래스는 게임 상태를 직접 바꾸지 않는다. 무엇을 가리켰고 무엇을 눌렀는지만 알리고,
    /// 그 결과 무엇을 할지는 구독하는 쪽이 정한다. 덕분에 입력 방식이 바뀌어도
    /// 턴 흐름 코드는 손대지 않는다.
    ///
    /// 예외는 장식물 반응이다. 반지를 눌렀을 때 흔들리는 정도는 게임 상태가 아니라
    /// 입력에 대한 즉각적인 시각 피드백이므로 여기서 바로 처리한다.
    /// </summary>
    public sealed class YachtInputRouter : MonoBehaviour
    {
        private const float PointerRayDistance = 50f;

        /// <summary>레이캐스트 기준 카메라. 컨트롤러가 넣어 준다.</summary>
        public Camera WorldCamera { get; set; }

        /// <summary>주사위를 가리킬 수 있는 상태인지. 굴림 결과가 없으면 꺼 둔다.</summary>
        public Func<bool> DicePointerEnabled { get; set; }

        /// <summary>증강 카드를 가리킬 수 있는 상태인지. 드래프트 중이거나 일반 모드면 꺼 둔다.</summary>
        public Func<bool> AugmentPointerEnabled { get; set; }

        public event Action RollRequested;
        public event Action<int> ResolutionPresetRequested;

        /// <summary>픽셀 엣지 필터를 켜고 끄는 요청. 기존 필터와 화면을 바로 비교할 때 쓴다.</summary>
        public event Action PixelEdgeToggleRequested;

        /// <summary>색 양자화 모드를 끔 → 단계 → 팔레트 순으로 돌리는 요청.</summary>
        public event Action PixelQuantizeCycleRequested;
        public event Action<DieType> DieTypeRequested;

        /// <summary>가리킨 주사위 번호. 없으면 -1.</summary>
        public event Action<int> DieHoverChanged;
        public event Action<int> DieClicked;

        /// <summary>굴림 오브젝트(코스믹 큐브 또는 수정구)를 가리키는 중인지.</summary>
        public event Action<bool> RollTriggerHoverChanged;
        public event Action RollTriggerClicked;

        /// <summary>가리킨 증강 카드. 없으면 null.</summary>
        public event Action<AugmentTrayCardView> AugmentCardHoverChanged;
        public event Action<AugmentTrayCardView> AugmentCardClicked;

        private int lastDieHover = -1;
        private bool lastRollTriggerHover;
        private AugmentTrayCardView lastAugmentHover;

        private void Update()
        {
            PollKeyboard();
            PollAugmentCardPointer();
            PollDicePointer();
        }

        private void PollKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.spaceKey.wasPressedThisFrame) RollRequested?.Invoke();
            if (keyboard.f1Key.wasPressedThisFrame) ResolutionPresetRequested?.Invoke(0);
            if (keyboard.f2Key.wasPressedThisFrame) ResolutionPresetRequested?.Invoke(1);
            if (keyboard.f3Key.wasPressedThisFrame) PixelEdgeToggleRequested?.Invoke();
            if (keyboard.qKey.wasPressedThisFrame) PixelQuantizeCycleRequested?.Invoke();

            // 숫자키 1~9로 주사위 색상 팔레트 실시간 전환
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) DieTypeRequested?.Invoke(DieType.Normal);
            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) DieTypeRequested?.Invoke(DieType.HeavyRed);
            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) DieTypeRequested?.Invoke(DieType.Golden);
            if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) DieTypeRequested?.Invoke(DieType.Metal);
            if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) DieTypeRequested?.Invoke(DieType.Sevens);
            if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame) DieTypeRequested?.Invoke(DieType.Couple);
            if (keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame) DieTypeRequested?.Invoke(DieType.Promotion);
            if (keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame) DieTypeRequested?.Invoke(DieType.Weird);
            if (keyboard.digit9Key.wasPressedThisFrame || keyboard.numpad9Key.wasPressedThisFrame) DieTypeRequested?.Invoke(DieType.Octahedron);
        }

        private void PollAugmentCardPointer()
        {
            Mouse mouse = Mouse.current;
            if (!TryBuildPointerRay(mouse, AugmentPointerEnabled, out Ray ray))
            {
                RaiseAugmentHover(null);
                return;
            }

            AugmentTrayCardView hitCard = null;
            RaycastHit[] hits = Physics.RaycastAll(ray, PointerRayDistance);
            for (int i = 0; i < hits.Length; i++)
            {
                AugmentTrayCardView view = hits[i].collider.GetComponentInParent<AugmentTrayCardView>();
                if (view == null) continue;
                hitCard = view;
                break;
            }

            RaiseAugmentHover(hitCard);
            if (hitCard != null && mouse.leftButton.wasPressedThisFrame)
            {
                AugmentCardClicked?.Invoke(hitCard);
            }
        }

        private void PollDicePointer()
        {
            Mouse mouse = Mouse.current;
            if (!TryBuildPointerRay(mouse, DicePointerEnabled, out Ray ray))
            {
                RaiseDieHover(-1);
                RaiseRollTriggerHover(false);
                return;
            }

            int hitIndex = -1;
            bool hitRollTrigger = false;
            if (Physics.Raycast(ray, out RaycastHit hit, PointerRayDistance))
            {
                DiceKeepTarget target = hit.collider.GetComponentInParent<DiceKeepTarget>();
                if (target != null) hitIndex = target.Index;

                hitRollTrigger = hit.collider.GetComponentInParent<RollCosmicCube>() != null
                    || hit.collider.GetComponentInParent<RollOrb>() != null;

                if (mouse.leftButton.wasPressedThisFrame) PlayDecorationFeedback(hit);
            }

            RaiseDieHover(hitIndex);
            RaiseRollTriggerHover(hitRollTrigger);

            if (!mouse.leftButton.wasPressedThisFrame) return;
            if (hitRollTrigger) RollTriggerClicked?.Invoke();
            if (hitIndex >= 0) DieClicked?.Invoke(hitIndex);
        }

        /// <summary>장식물을 누르면 반응한다. 게임 상태와 무관한 즉각 피드백이다.</summary>
        private static void PlayDecorationFeedback(RaycastHit hit)
        {
            hit.collider.GetComponentInParent<TabletopTrinketRing>()?.TriggerRattle();
            hit.collider.GetComponentInParent<TabletopTrinketBrooch>()?.TriggerRattle();
            hit.collider.GetComponentInParent<TabletopTrinketManaCrystal>()?.TriggerGlow();
        }

        private bool TryBuildPointerRay(Mouse mouse, Func<bool> gate, out Ray ray)
        {
            ray = default;
            if (mouse == null || WorldCamera == null) return false;
            if (gate != null && !gate()) return false;

            Vector2 pointer = mouse.position.ReadValue();
            Vector3 viewport = new(
                Screen.width > 0 ? pointer.x / Screen.width : 0.5f,
                Screen.height > 0 ? pointer.y / Screen.height : 0.5f,
                0f);
            ray = WorldCamera.ViewportPointToRay(viewport);
            return true;
        }

        private void RaiseDieHover(int index)
        {
            if (lastDieHover == index) return;
            lastDieHover = index;
            DieHoverChanged?.Invoke(index);
        }

        private void RaiseRollTriggerHover(bool hovered)
        {
            if (lastRollTriggerHover == hovered) return;
            lastRollTriggerHover = hovered;
            RollTriggerHoverChanged?.Invoke(hovered);
        }

        private void RaiseAugmentHover(AugmentTrayCardView card)
        {
            if (lastAugmentHover == card) return;
            lastAugmentHover = card;
            AugmentCardHoverChanged?.Invoke(card);
        }
    }
}
