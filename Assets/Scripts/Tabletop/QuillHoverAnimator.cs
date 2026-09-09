using UnityEngine;

namespace Tessera.Tabletop
{
    /// <summary>
    /// 잉크통에 꽂힌 깃펜을 목표 지점으로 옮겨 닙을 대고 멈추게 한다.
    ///
    /// 월드 좌표 하나와 그 지점의 표면 축만 받는다. 점수표도 게임 규칙도 모른다.
    /// 배선은 <c>AugmentedYachtController</c>가 한다. 설계 근거는
    /// <c>docs/quill_hover_writing_animation_plan.md</c>에 있다.
    ///
    /// <see cref="InkwellAndQuill"/>과 달리 <c>ExecuteAlways</c>가 아니다.
    /// 편집 중에 깃펜이 제자리를 벗어난 채 씬에 저장되는 사고를 막는다.
    /// </summary>
    [RequireComponent(typeof(InkwellAndQuill))]
    public sealed class QuillHoverAnimator : MonoBehaviour
    {
        private enum QuillState { Docked, Lifting, Traveling, Writing, Returning }

        /// <summary>깃펜 로컬 원점이 곧 닙 끝이다. 이 트랜스폼을 옮기면 닙이 그 지점에 닿는다.</summary>
        private const string QuillRootName = "Quill Pen Root";

        /// <summary>닙 끝이 종이에 닿을락 말락 하는 높이.</summary>
        private const float NibLift = 0.02f;

        /// <summary>잉크통 림 위로 띄우는 높이. 닙이 통 안쪽 벽을 뚫고 지나가지 않게 한다.</summary>
        private const float DockLift = 0.5f;

        private const float LiftSeconds = 0.12f;
        private const float DescendSeconds = 0.12f;

        /// <summary>호버가 사라진 뒤 복귀를 미루는 시간. 인접한 칸 사이를 지날 때의 깜빡임을 막는다.</summary>
        private const float ReturnGraceSeconds = 0.18f;

        private const float TravelSmoothTime = 0.13f;
        private const float TurnDegreesPerSecond = 720f;

        /// <summary>닙이 이만큼 안에 들어오면 도착으로 본다.</summary>
        private const float ArriveDistance = 0.05f;

        // --- 필기 흉내 흔들림. 진폭은 M17-T18-5에서 화면을 보고 확정한다. ---
        private const float JitterFrequency = 7f;
        private const float JitterAlongSurface = 0.015f;
        private const float JitterAcrossSurface = 0.008f;

        /// <summary>도착하자마자 떨면 튀어 보인다. 진폭을 이 시간에 걸쳐 올린다.</summary>
        private const float JitterRampSeconds = 0.2f;

        /// <summary>
        /// 종이에 닿아 있는 동안의 깃펜 크기다. 1이면 잉크통에 꽂혔을 때와 같은 크기다.
        /// 씬의 잉크통에 2.5배가 걸려 있어 깃펜 전장(9.93)이 점수표 가로 폭(7.80)보다 길다.
        /// </summary>
        [Header("Writing Pose")]
        [SerializeField, Range(0.2f, 1f)] private float writingScale = 1f;

        /// <summary>
        /// 꽂힘 자세를 종이 세로축 기준으로 돌리는 각도다. 양수가 화면에서 시계방향이다.
        /// 카메라가 yaw 없이 내려다보므로 종이 위에서 도는 각이 곧 화면에서 도는 각이다.
        /// </summary>
        [SerializeField, Range(-90f, 90f)] private float writingYawDegrees = 15f;

        private Transform quillRoot;
        private Vector3 dockedLocalPosition;
        private Quaternion dockedLocalRotation;
        private Vector3 dockedLocalScale = Vector3.one;

        private QuillState state = QuillState.Docked;
        private float stateElapsed;

        private bool hasTarget;
        private Vector3 targetCellWorld;
        private Vector3 targetSurfaceUp = Vector3.up;

        private float graceElapsed;
        private float writingElapsed;

        private Vector3 followVelocity;
        private float scaleVelocity;
        private float scaleFactor = 1f;

        /// <summary>
        /// 이 지점에 닙을 대라고 알린다. 컨트롤러가 호버 중인 동안 매 프레임 다시 부른다.
        /// 점수표는 열이 접히고 펴지는 동안 칸 좌표가 매 프레임 바뀌기 때문이다.
        /// </summary>
        public void SetWritingTarget(Vector3 worldPoint, Vector3 surfaceUp)
        {
            targetCellWorld = worldPoint;
            targetSurfaceUp = surfaceUp;
            graceElapsed = 0f;

            if (hasTarget) return;
            hasTarget = true;

            if (state == QuillState.Docked)
            {
                state = QuillState.Lifting;
                stateElapsed = 0f;
            }
            else if (state == QuillState.Returning)
            {
                // 복귀 도중에 새 칸이 들어왔다. 잉크통까지 갔다 오지 않고 그 자리에서 되돌아간다.
                state = QuillState.Traveling;
                stateElapsed = 0f;
                followVelocity = Vector3.zero;
            }
        }

        /// <summary>포인터가 칸을 벗어났다. 유예가 지나면 잉크통으로 돌아간다.</summary>
        public void ClearWritingTarget()
        {
            if (!hasTarget) return;
            hasTarget = false;
            graceElapsed = 0f;
        }

        /// <summary>
        /// 필기 자세를 정하는 순수 함수다. 화면 없이 검증할 수 있는 유일한 지점이라 따로 뺐다.
        ///
        /// 자세를 새로 만들지 않는다. 잉크통에 꽂혀 있을 때의 기울기를 그대로 들고 와 칸 위에 놓고,
        /// 종이 세로축 기준으로 <paramref name="yawDegrees"/>만큼 돌린다. 세로축 회전이라 종이에
        /// 대한 기울기는 꽂힘 자세 그대로 남고 깃털이 향하는 방향만 바뀐다.
        ///
        /// 두 플레이어가 같은 자세를 쓴다. 좌우반전은 폐기했다.
        /// </summary>
        public static void ResolveWritingPose(
            Vector3 cellWorld,
            Quaternion dockedRotation,
            Vector3 surfaceUp,
            float yawDegrees,
            out Vector3 position,
            out Quaternion rotation)
        {
            Vector3 up = surfaceUp.normalized;
            position = cellWorld + (up * NibLift);
            rotation = Quaternion.AngleAxis(yawDegrees, up) * dockedRotation;
        }

        private void Awake() => TryBindQuillRoot();

        private void OnDisable()
        {
            // 비활성으로 들어갈 때 제자리에 돌려놓지 않으면 어긋난 자세로 굳는다.
            if (quillRoot == null) return;
            ApplyDockedPose();
            state = QuillState.Docked;
            hasTarget = false;
        }

        private void LateUpdate()
        {
            // InkwellAndQuill이 ExecuteAlways라 기하 생성 순서가 어긋날 수 있다. 될 때까지 다시 찾는다.
            if (quillRoot == null && !TryBindQuillRoot()) return;

            float dt = Time.deltaTime;
            stateElapsed += dt;

            if (!hasTarget && (state == QuillState.Traveling || state == QuillState.Writing))
            {
                graceElapsed += dt;
                if (graceElapsed >= ReturnGraceSeconds) EnterReturning();
            }

            switch (state)
            {
                case QuillState.Docked:
                    return;

                case QuillState.Lifting:
                    TickLifting();
                    break;

                case QuillState.Traveling:
                    TickTraveling(dt);
                    break;

                case QuillState.Writing:
                    TickWriting(dt);
                    break;

                case QuillState.Returning:
                    TickReturning(dt);
                    break;
            }

            ApplyScale(dt);
        }

        private void TickLifting()
        {
            // 상승 도중에 호버가 사라지면 목표 없이 떠 있게 되므로 곧바로 되돌린다.
            if (!hasTarget)
            {
                EnterReturning();
                return;
            }

            float t = Mathf.Clamp01(stateElapsed / LiftSeconds);
            quillRoot.SetPositionAndRotation(
                Vector3.Lerp(DockedWorldPosition, LiftedWorldPosition, t), DockedWorldRotation);

            if (t < 1f) return;
            state = QuillState.Traveling;
            stateElapsed = 0f;
            followVelocity = Vector3.zero;
        }

        private void TickTraveling(float dt)
        {
            ResolveWritingPose(
                targetCellWorld, DockedWorldRotation, targetSurfaceUp, writingYawDegrees,
                out Vector3 goal, out Quaternion goalRotation);

            FollowTo(goal, goalRotation, dt);

            // 목표가 도중에 바뀌어도 SmoothDamp가 이어서 따라간다. 잉크통을 거치지 않는다.
            if (Vector3.Distance(quillRoot.position, goal) > ArriveDistance) return;
            state = QuillState.Writing;
            stateElapsed = 0f;
            writingElapsed = 0f;
        }

        private void TickWriting(float dt)
        {
            writingElapsed += dt;

            ResolveWritingPose(
                targetCellWorld, DockedWorldRotation, targetSurfaceUp, writingYawDegrees,
                out Vector3 goal, out Quaternion goalRotation);

            // 흔들림은 위치에만 더한다. 회전까지 흔들면 깃털 끝이 크게 휘둘려 시선을 뺏는다.
            float amplitude = Mathf.Clamp01(writingElapsed / JitterRampSeconds);
            float phase = writingElapsed * JitterFrequency * 2f * Mathf.PI;
            // 흔들림 축은 깃펜이 선 방향에서 따온다. 종이 평면 위의 작은 타원이 된다.
            Vector3 up = targetSurfaceUp.normalized;
            Vector3 along = Vector3.ProjectOnPlane(goalRotation * Vector3.up, up).normalized;
            Vector3 across = Vector3.Cross(up, along);
            goal += along * (Mathf.Sin(phase) * JitterAlongSurface * amplitude);
            goal += across * (Mathf.Sin(phase + (Mathf.PI * 0.5f)) * JitterAcrossSurface * amplitude);

            FollowTo(goal, goalRotation, dt);
        }

        private void TickReturning(float dt)
        {
            if (stateElapsed < LiftSeconds)
            {
                // 1단계: 잉크통 위 공중으로 모인다. 자세도 여기서 꽂힘 자세로 돌려 둔다.
                FollowTo(LiftedWorldPosition, DockedWorldRotation, dt);
                return;
            }

            float t = Mathf.Clamp01((stateElapsed - LiftSeconds) / DescendSeconds);
            quillRoot.SetPositionAndRotation(
                Vector3.Lerp(LiftedWorldPosition, DockedWorldPosition, t), DockedWorldRotation);

            if (t < 1f) return;
            ApplyDockedPose();
            state = QuillState.Docked;
            stateElapsed = 0f;
        }

        private void EnterReturning()
        {
            state = QuillState.Returning;
            stateElapsed = 0f;
            followVelocity = Vector3.zero;
        }

        private void FollowTo(Vector3 goal, Quaternion goalRotation, float dt)
        {
            quillRoot.SetPositionAndRotation(
                Vector3.SmoothDamp(quillRoot.position, goal, ref followVelocity, TravelSmoothTime, Mathf.Infinity, dt),
                Quaternion.RotateTowards(quillRoot.rotation, goalRotation, TurnDegreesPerSecond * dt));
        }

        /// <summary>
        /// 종이에 닿아 있는 동안만 줄인다. 도착하는 순간 크기가 튀지 않도록 이동 중에 미리 줄여 둔다.
        /// </summary>
        private void ApplyScale(float dt)
        {
            float target = hasTarget ? writingScale : 1f;
            scaleFactor = Mathf.SmoothDamp(scaleFactor, target, ref scaleVelocity, TravelSmoothTime, Mathf.Infinity, dt);
            quillRoot.localScale = dockedLocalScale * scaleFactor;
        }

        private void ApplyDockedPose()
        {
            quillRoot.localPosition = dockedLocalPosition;
            quillRoot.localRotation = dockedLocalRotation;
            quillRoot.localScale = dockedLocalScale;
            scaleFactor = 1f;
            scaleVelocity = 0f;
            followVelocity = Vector3.zero;
        }

        private Vector3 DockedWorldPosition => transform.TransformPoint(dockedLocalPosition);
        private Quaternion DockedWorldRotation => transform.rotation * dockedLocalRotation;
        private Vector3 LiftedWorldPosition => DockedWorldPosition + (Vector3.up * DockLift);

        private bool TryBindQuillRoot()
        {
            quillRoot = transform.Find(QuillRootName);
            if (quillRoot == null) return false;

            dockedLocalPosition = quillRoot.localPosition;
            dockedLocalRotation = quillRoot.localRotation;
            dockedLocalScale = quillRoot.localScale;
            return true;
        }
    }
}
