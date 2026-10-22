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

        /// <summary>
        /// 복귀 전체에 걸리는 시간이다.
        ///
        /// 예전에는 "공중으로 모으기"와 "내려꽂기"를 따로 돌렸는데, 앞 구간이 SmoothDamp라
        /// 정해진 시간 안에 목표에 닿지 못했고 뒤 구간이 목표점에서 새로 시작하는 바람에 그
        /// 경계에서 위치가 튀었다. 지금은 잉크통 위 지점을 제어점으로 삼는 곡선 하나로 잇는다.
        /// </summary>
        private const float ReturnSeconds = 0.38f;

        /// <summary>
        /// 복귀 시간 중 회전을 끝내는 지점의 비율이다.
        ///
        /// 회전에 위치와 같은 곡선을 쓰면 <see cref="Mathf.SmoothStep"/>의 끝 기울기가 0이라
        /// 다 내려앉은 뒤에도 자세가 조금 남아, 마지막에 한 번 더 도는 것처럼 보인다. 자세를
        /// 먼저 세우고 남은 구간은 내려꽂기만 남기면 실제로 펜을 통에 꽂는 동작과도 맞는다.
        /// </summary>
        private const float ReturnRotationSettleRatio = 0.7f;

        /// <summary>호버가 사라진 뒤 복귀를 미루는 시간. 인접한 칸 사이를 지날 때의 깜빡임을 막는다.</summary>
        private const float ReturnGraceSeconds = 0.18f;

        private const float TravelSmoothTime = 0.13f;

        /// <summary>회전이 목표에 수렴하는 시간이다. 등속 회전은 시작과 끝이 딱딱하게 끊긴다.</summary>
        private const float TurnSmoothTime = 0.16f;

        /// <summary>닙이 이만큼 안에 들어오면 도착으로 본다.</summary>
        private const float ArriveDistance = 0.05f;

        // --- 필기 흉내 흔들림. 진폭은 M17-T18-5에서 화면을 보고 확정한다. ---
        private const float JitterFrequency = 7f;
        private const float JitterAlongSurface = 0.015f;
        private const float JitterAcrossSurface = 0.008f;

        /// <summary>도착하자마자 떨면 튀어 보인다. 진폭을 이 시간에 걸쳐 올린다.</summary>
        private const float JitterRampSeconds = 0.2f;

        /// <summary>픽셀 격자 스냅에 쓰는 카메라와 가상 해상도. 컨트롤러가 매 프레임 넘긴다.</summary>
        private Camera pixelCamera;
        private Vector2Int pixelResolution;

        /// <summary>
        /// 스냅 전의 연속 위치다. 화면에 놓는 자리는 격자에 맞추지만, 다음 프레임의 보간은
        /// 이 값에서 이어 간다. 스냅된 값을 되먹이면 한 칸 안에서 앞뒤로 진동한다.
        /// </summary>
        private Vector3 logicalPosition;

        /// <summary>회전 감속에 쓰는 각속도. <see cref="SmoothRotate"/>가 관리한다.</summary>
        private float turnVelocity;

        /// <summary>복귀를 시작한 자리와 그때의 자세. 복귀 곡선의 출발점이다.</summary>
        private Vector3 returnStartPosition;
        private Quaternion returnStartRotation;

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
        /// <summary>
        /// 화면 픽셀 격자를 알려 준다. 깃펜이 연속 좌표로 움직이면 업스케일 셰이더가 뽑는 셀
        /// 중심 샘플이 실루엣 경계를 스쳤다 말았다 하며 자글거린다. 놓는 자리를 같은 격자에
        /// 맞추면 그 떨림이 사라지고 이동이 칸 단위로 끊긴다.
        /// </summary>
        public void SetPixelGrid(Camera camera, Vector2Int resolution)
        {
            pixelCamera = camera;
            pixelResolution = resolution;
        }

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
            // 가속만 준다. SmoothStep으로 끝을 눕히면 속도가 0으로 떨어졌다가 다음 구간에서
            // 다시 붙어 한 번 멈칫한다.
            SetRootPose(
                Vector3.Lerp(DockedWorldPosition, LiftedWorldPosition, t * t),
                DockedWorldRotation);

            if (t < 1f) return;
            state = QuillState.Traveling;
            stateElapsed = 0f;
            // 상승 마지막 속도를 그대로 넘겨 이어 달리게 한다. t*t의 t=1 기울기가 2다.
            followVelocity = (LiftedWorldPosition - DockedWorldPosition) * (2f / LiftSeconds);
        }

        private void TickTraveling(float dt)
        {
            ResolveWritingPose(
                targetCellWorld, DockedWorldRotation, targetSurfaceUp, writingYawDegrees,
                out Vector3 goal, out Quaternion goalRotation);

            FollowTo(goal, goalRotation, dt);

            // 목표가 도중에 바뀌어도 SmoothDamp가 이어서 따라간다. 잉크통을 거치지 않는다.
            if (Vector3.Distance(logicalPosition, goal) > ArriveDistance) return;
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

        /// <summary>
        /// 복귀는 곡선 하나로 잇는다. 출발점과 잉크통을 직선으로 내려오면 펜이 점수표를 쓸고
        /// 지나가므로, 잉크통 위 지점을 제어점으로 두어 한 번 떠올랐다가 내려앉게 한다.
        /// </summary>
        private void TickReturning(float dt)
        {
            float t = Mathf.Clamp01(stateElapsed / ReturnSeconds);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            float inverse = 1f - eased;
            Vector3 position =
                (inverse * inverse * returnStartPosition)
                + (2f * inverse * eased * LiftedWorldPosition)
                + (eased * eased * DockedWorldPosition);

            float rotationT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / ReturnRotationSettleRatio));
            SetRootPose(position, Quaternion.Slerp(returnStartRotation, DockedWorldRotation, rotationT));

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
            turnVelocity = 0f;
            returnStartPosition = logicalPosition;
            returnStartRotation = quillRoot.rotation;
        }

        private void FollowTo(Vector3 goal, Quaternion goalRotation, float dt)
        {
            SetRootPose(
                Vector3.SmoothDamp(logicalPosition, goal, ref followVelocity, TravelSmoothTime, Mathf.Infinity, dt),
                SmoothRotate(quillRoot.rotation, goalRotation, dt));
        }

        /// <summary>
        /// 남은 각도를 <see cref="Mathf.SmoothDamp"/>로 줄여 회전에 가속과 감속을 준다.
        ///
        /// <c>Quaternion.RotateTowards</c>는 각속도가 일정해 돌기 시작하는 순간과 멈추는 순간이
        /// 모두 각지게 끊긴다. 남은 각도를 하나의 스칼라로 보고 감쇠시키면 축이 무엇이든 같은
        /// 곡선을 그리며 눕는다.
        /// </summary>
        private Quaternion SmoothRotate(Quaternion current, Quaternion goal, float dt)
        {
            float angle = Quaternion.Angle(current, goal);
            if (angle < 0.01f)
            {
                turnVelocity = 0f;
                return goal;
            }

            float remaining = Mathf.SmoothDamp(angle, 0f, ref turnVelocity, TurnSmoothTime, Mathf.Infinity, dt);
            return Quaternion.Slerp(goal, current, Mathf.Clamp01(remaining / angle));
        }

        /// <summary>
        /// 연속 위치를 기억하고, 멈춰 있을 때만 화면에 놓는 자리를 픽셀 격자에 맞춘다.
        ///
        /// 격자 스냅은 제자리에서 떨 때의 자글거림을 잡으려고 넣은 것이다. 옮겨 가는 중에도
        /// 걸어 두면 이동 자체가 칸 단위로 끊겨 보인다. 빠르게 지나갈 때는 자글거림이 눈에
        /// 띄지 않으므로, 닙을 대고 멈춘 동안에만 건다.
        /// </summary>
        private void SetRootPose(Vector3 position, Quaternion rotation)
        {
            logicalPosition = position;
            Vector3 placed = state == QuillState.Writing ? SnapToPixelGrid(position) : position;
            quillRoot.SetPositionAndRotation(placed, rotation);
        }

        private Vector3 SnapToPixelGrid(Vector3 world)
        {
            if (pixelCamera == null || pixelResolution.x < 1 || pixelResolution.y < 1) return world;

            Vector3 viewport = pixelCamera.WorldToViewportPoint(world);
            // 카메라 뒤로 넘어간 지점은 뷰포트 좌표가 뒤집혀 스냅이 엉뚱한 자리를 만든다.
            if (viewport.z <= 0f) return world;

            viewport.x = (Mathf.Floor(viewport.x * pixelResolution.x) + 0.5f) / pixelResolution.x;
            viewport.y = (Mathf.Floor(viewport.y * pixelResolution.y) + 0.5f) / pixelResolution.y;
            return pixelCamera.ViewportToWorldPoint(viewport);
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
            logicalPosition = quillRoot.position;
            quillRoot.localRotation = dockedLocalRotation;
            quillRoot.localScale = dockedLocalScale;
            scaleFactor = 1f;
            scaleVelocity = 0f;
            followVelocity = Vector3.zero;
            turnVelocity = 0f;
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
