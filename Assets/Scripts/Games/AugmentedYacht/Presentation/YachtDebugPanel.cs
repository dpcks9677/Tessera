using System;
using System.Collections.Generic;
using UnityEngine;
using Tessera.Games.Yacht;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 화면 왼쪽 위의 디버그 컨트롤 패널.
    ///
    /// 접이식 그룹 둘로 나뉜다. 렌더링 그룹은 예전에 가로로 늘어서 있던 버튼 일곱 개를 그대로 이어받고,
    /// 게임 그룹은 턴 넘기기·증강 부여·주사위 눈 지정을 모아 둔다. 이 셋이 있으면 확인하려는 상황을
    /// 손으로 만들 수 있어, 매번 일회용 디버그 스크립트를 쓰지 않아도 된다.
    ///
    /// uGUI가 아니라 IMGUI인 이유는 접기·스크롤·값 선택이 전부 내장이기 때문이다. 같은 것을 uGUI로 만들면
    /// Dropdown 템플릿 계층까지 손으로 조립해야 한다. 이 패널은 게임 화면이 아니므로 아트 규칙을 따르지 않는다.
    ///
    /// 라벨은 <see cref="YachtSceneAssembler.HudActions"/>의 조회 델리게이트를 매 프레임 읽는 폴링 방식이다.
    /// 상태를 바꾼 쪽이 라벨을 밀어 넣던 예전 방식과 달리, 값이 어디서 바뀌든 다음 프레임에 반영된다.
    /// </summary>
    public sealed class YachtDebugPanel : MonoBehaviour
    {
        private const int PanelWidth = 300;
        private const int AugmentListHeight = 260;

        private AugmentedYachtController controller;
        private YachtSceneAssembler.HudActions actions;

        private bool renderGroupOpen;
        private bool gameGroupOpen;
        private bool turnSectionOpen = true;
        private bool augmentSectionOpen = true;
        private bool diceSectionOpen = true;

        private Vector2 augmentScroll;
        private string augmentFilter = string.Empty;
        private string selectedAugmentId;
        private int augmentTargetPlayer = -1;
        private string lastMessage;

        /// <summary>주사위별로 고른 눈. 화면 주사위 개수가 바뀌면 다시 맞춘다.</summary>
        private int[] forcedValues = Array.Empty<int>();

        private static readonly string[] PlayerLabels = { "P1", "P2" };

        /// <summary>
        /// 버튼이 부른 일을 GUI 패스 밖으로 미루는 자리.
        ///
        /// 증강 부여·턴 넘기기·굴리기는 주사위 개수와 위상을 바꾼다. 그 일을 OnGUI 안에서 바로 하면
        /// 같은 프레임의 뒤쪽 섹션이 앞선 레이아웃 패스와 다른 개수의 컨트롤을 그리게 되어
        /// IMGUI가 레이아웃 불일치로 어긋난다. 다음 <see cref="Update"/>에서 실행한다.
        /// </summary>
        private Action pendingAction;

        private YachtAugmentDefinition[] definitions;
        private GUIStyle headerStyle;
        private GUIStyle messageStyle;

        public void Bind(AugmentedYachtController owner, YachtSceneAssembler.HudActions hudActions)
        {
            controller = owner;
            actions = hudActions;
        }

        private void Update()
        {
            if (pendingAction == null) return;

            Action action = pendingAction;
            pendingAction = null;
            action();
        }

        private void Defer(Action action)
        {
            pendingAction = action;
        }

        private void GrantAugment(YachtGameSession session, int playerIndex, string augmentId)
        {
            if (!session.DebugGrantAugment(playerIndex, augmentId, out string error))
            {
                lastMessage = $"실패: {error}";
                return;
            }

            lastMessage = $"P{playerIndex + 1} 획득: {augmentId}";
            controller.TurnFlow?.RefreshAugmentPresentation(lastMessage);
        }

        private void OnGUI()
        {
            if (controller == null || actions == null) return;

            EnsureStyles();
            GUILayout.BeginArea(new Rect(12f, 12f, PanelWidth * 2f + 24f, Screen.height - 24f));
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(PanelWidth));
            renderGroupOpen = GUILayout.Toggle(renderGroupOpen, renderGroupOpen ? "▼ 렌더링" : "▶ 렌더링", headerStyle);
            if (renderGroupOpen) DrawRenderGroup();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(PanelWidth));
            gameGroupOpen = GUILayout.Toggle(gameGroupOpen, gameGroupOpen ? "▼ 게임" : "▶ 게임", headerStyle);
            if (gameGroupOpen) DrawGameGroup();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawRenderGroup()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            if (GUILayout.Button($"해상도: {Read(actions.ResolutionPresetLabel, "-")}")) actions.ToggleResolution?.Invoke();
            if (GUILayout.Button($"조명: {Read(actions.KeyLightPresetName, "-")}")) actions.ToggleKeyLight?.Invoke();
            bool edgeOn = actions.PixelEdgeEnabled != null && actions.PixelEdgeEnabled();
            if (GUILayout.Button($"엣지: {(edgeOn ? "ON" : "OFF")}")) actions.TogglePixelEdge?.Invoke();
            if (GUILayout.Button($"양자화: {Read(actions.QuantizeModeName, "Off")}")) actions.CycleQuantize?.Invoke();
            if (GUILayout.Button($"스타일: {Read(actions.RenderStyleName, "Baseline")}")) actions.ToggleRenderStyle?.Invoke();
            if (GUILayout.Button($"룬 점등: {Read(actions.RuneProgressText, "0/12")}")) actions.AdvanceRuneLighting?.Invoke();
            if (GUILayout.Button($"룬 스톤: {Read(actions.RuneStoneText, "0/4")}")) actions.CycleRuneStones?.Invoke();
            GUILayout.EndVertical();
        }

        private void DrawGameGroup()
        {
            GUILayout.BeginVertical(GUI.skin.box);

            YachtGameSession session = controller.GameSession;
            if (session == null)
            {
                GUILayout.Label("게임이 아직 시작되지 않았습니다.");
                GUILayout.EndVertical();
                return;
            }

            turnSectionOpen = GUILayout.Toggle(turnSectionOpen, turnSectionOpen ? "▼ 턴" : "▶ 턴", headerStyle);
            if (turnSectionOpen) DrawTurnSection(session);

            augmentSectionOpen = GUILayout.Toggle(augmentSectionOpen, augmentSectionOpen ? "▼ 증강" : "▶ 증강", headerStyle);
            if (augmentSectionOpen) DrawAugmentSection(session);

            diceSectionOpen = GUILayout.Toggle(diceSectionOpen, diceSectionOpen ? "▼ 주사위" : "▶ 주사위", headerStyle);
            if (diceSectionOpen) DrawDiceSection(session);

            if (!string.IsNullOrEmpty(lastMessage)) GUILayout.Label(lastMessage, messageStyle);
            GUILayout.EndVertical();
        }

        private void DrawTurnSection(YachtGameSession session)
        {
            GUILayout.Label($"R{session.CurrentRound} · P{session.CurrentPlayerIndex + 1} · {session.Phase}");
            GUILayout.Label($"남은 굴림 {session.RollsRemaining}");

            // 넘길 턴이 있어야 누를 수 있다. 드래프트 중에는 아직 턴이 시작되지 않았다.
            bool canSkip = controller.TurnFlow != null
                && (session.Phase == YachtGamePhase.TurnReady || session.Phase == YachtGamePhase.ScoreSelection);
            GUI.enabled = canSkip;
            if (GUILayout.Button("턴 넘기기 (최고점 자동 기입)"))
            {
                Defer(() =>
                {
                    controller.TurnFlow.SkipTurn();
                    lastMessage = "턴을 넘겼습니다.";
                });
            }
            GUI.enabled = true;
        }

        private void DrawAugmentSection(YachtGameSession session)
        {
            if (session.Mode != YachtGameMode.Augmented)
            {
                GUILayout.Label("일반 모드에서는 증강을 쓸 수 없습니다.");
                return;
            }

            EnsureDefinitions();
            int target = ResolveAugmentTarget(session);

            GUILayout.BeginHorizontal();
            GUILayout.Label("대상", GUILayout.Width(32f));
            augmentTargetPlayer = GUILayout.SelectionGrid(target, PlayerLabels, PlayerLabels.Length);
            GUILayout.EndHorizontal();

            augmentFilter = GUILayout.TextField(augmentFilter);

            augmentScroll = GUILayout.BeginScrollView(augmentScroll, GUILayout.Height(AugmentListHeight));
            for (int i = 0; i < definitions.Length; i++)
            {
                YachtAugmentDefinition definition = definitions[i];
                if (!Matches(definition, augmentFilter)) continue;
                bool selected = string.Equals(definition.Id, selectedAugmentId, StringComparison.Ordinal);
                if (GUILayout.Toggle(selected, $"[{definition.Kind}] {definition.DisplayName}", GUI.skin.button))
                    selectedAugmentId = definition.Id;
            }
            GUILayout.EndScrollView();

            GUI.enabled = !string.IsNullOrEmpty(selectedAugmentId);
            if (GUILayout.Button("획득"))
            {
                string augmentId = selectedAugmentId;
                Defer(() => GrantAugment(session, target, augmentId));
            }
            GUI.enabled = true;

            GUILayout.Label($"보유: {DescribeOwned(session, target)}");
        }

        private void DrawDiceSection(YachtGameSession session)
        {
            IReadOnlyList<IReadOnlyYachtDieState> dice = session.State.Dice;
            SyncForcedValueSlots(dice.Count);

            for (int i = 0; i < dice.Count; i++)
            {
                IReadOnlyYachtDieState die = dice[i];
                int[] faces = FacesFor(die.Type);

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{i + 1}. {die.Type}{(die.IsKept ? " (킵)" : string.Empty)}", GUILayout.Width(110f));
                // 킵된 주사위는 다시 굴리지 않으므로 지정해도 효과가 없다.
                GUI.enabled = !die.IsKept;
                int current = Mathf.Max(0, Array.IndexOf(faces, forcedValues[i]));
                int picked = GUILayout.SelectionGrid(current, FaceLabels(faces), faces.Length);
                forcedValues[i] = faces[Mathf.Clamp(picked, 0, faces.Length - 1)];
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(session.CanRoll ? "지정하고 굴리기" : "다음 굴림에 예약"))
            {
                session.DebugSetForcedRollValues(BuildForcedValues(dice));
                if (session.CanRoll)
                {
                    // 스페이스바와 같은 경로로 굴린다.
                    Defer(controller.RollDice);
                    lastMessage = "지정한 눈으로 굴렸습니다.";
                }
                else
                {
                    lastMessage = "다음 굴림에 적용됩니다.";
                }
            }
            if (GUILayout.Button("예약 해제", GUILayout.Width(80f)))
            {
                session.DebugSetForcedRollValues(null);
                lastMessage = "예약을 풀었습니다.";
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>킵된 주사위 자리는 0으로 남겨 권위가 평소대로 난수를 쓰게 한다.</summary>
        private int[] BuildForcedValues(IReadOnlyList<IReadOnlyYachtDieState> dice)
        {
            var values = new int[dice.Count];
            for (int i = 0; i < dice.Count; i++) values[i] = dice[i].IsKept ? 0 : forcedValues[i];
            return values;
        }

        private void SyncForcedValueSlots(int diceCount)
        {
            if (forcedValues.Length == diceCount) return;

            var resized = new int[diceCount];
            for (int i = 0; i < diceCount; i++) resized[i] = i < forcedValues.Length && forcedValues[i] > 0 ? forcedValues[i] : 1;
            forcedValues = resized;
        }

        /// <summary>이 주사위가 낼 수 있는 눈. 표에 없는 종류는 평범한 1~6이다.</summary>
        private static int[] FacesFor(YachtDieType type)
        {
            if (!YachtDieFaces.TryGetFaces(type, out int[] faces)) return new[] { 1, 2, 3, 4, 5, 6 };

            var distinct = new List<int>(faces.Length);
            for (int i = 0; i < faces.Length; i++)
                if (!distinct.Contains(faces[i])) distinct.Add(faces[i]);
            distinct.Sort();
            return distinct.ToArray();
        }

        private static string[] FaceLabels(int[] faces)
        {
            var labels = new string[faces.Length];
            for (int i = 0; i < faces.Length; i++) labels[i] = faces[i].ToString();
            return labels;
        }

        private int ResolveAugmentTarget(YachtGameSession session)
        {
            if (augmentTargetPlayer >= 0) return augmentTargetPlayer;
            return session.IsDrafting ? Mathf.Max(0, session.State.Draft.PlayerIndex) : session.CurrentPlayerIndex;
        }

        private static string DescribeOwned(YachtGameSession session, int playerIndex)
        {
            IReadOnlyList<IReadOnlyYachtAugmentPlayerState> players = session.State.AugmentPlayers;
            if (players == null || playerIndex < 0 || playerIndex >= players.Count) return "-";

            IReadOnlyList<string> owned = players[playerIndex].OwnedIds;
            return owned == null || owned.Count == 0 ? "없음" : string.Join(", ", owned);
        }

        private static bool Matches(YachtAugmentDefinition definition, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter)) return true;
            return definition.DisplayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || definition.Id.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void EnsureDefinitions()
        {
            if (definitions != null) return;

            IReadOnlyList<YachtAugmentDefinition> all = new YachtAugmentRuntime().GetDefinitions();
            definitions = new YachtAugmentDefinition[all.Count];
            for (int i = 0; i < all.Count; i++) definitions[i] = all[i];
        }

        private static string Read(Func<string> source, string fallback)
        {
            return source != null ? source() : fallback;
        }

        private void EnsureStyles()
        {
            if (headerStyle != null) return;

            headerStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            messageStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
        }
    }
}
