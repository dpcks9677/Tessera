using System;
using System.Collections.Generic;
using Tessera.Core;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>한 번의 굴림 시뮬레이션 결과. 좌표는 주사위 루트 로컬(= 재생 시 기준) 프레임이다.</summary>
public sealed class DiceSimulationResult
{
    public Vector3[][] Positions;      // [프레임][주사위]
    public Quaternion[][] Rotations;   // [프레임][주사위]
    public float SettleTime;
    public float ImpactTime;
    public bool Settled;
}

/// <summary>
/// 주사위 프리셋 베이킹용 물리 리그(M10.9-T2).
///
/// 트레이 내부 림 경계에 박스 물리벽을 세우고 주사위를 굴린다. 플레이 모드에 들어가지 않고
/// 프리뷰 씬의 격리 물리 씬을 직접 스텝하므로 도메인 리로드가 없다.
///
/// 좌표계는 주사위 루트 로컬 프레임이다. 트레이 중심을 (0, *, TrayCenterZ)에 두면 시뮬레이션
/// 좌표가 곧 BakedDiceController가 재생할 좌표가 된다.
/// </summary>
public sealed class DicePresetBakeRig : IDisposable
{
    public const float SimulationStep = 1f / 60f;
    public const int SampleInterval = 3;                 // 60Hz -> 20fps 출력
    public const int OutputFps = 20;

    // 원본 웹 베이커는 주사위 한 변 1.62 기준으로 중력 95를 썼다(preset-studio/src/presetBaker.js:33).
    // 같은 체감 낙하 속도를 유지하려고 현재 주사위 크기에 비례해 환산한다.
    private const float Gravity = -95f / DiceBoardMetrics.SourceDiceSize * DiceBoardMetrics.DieSize;

    private const float WallThickness = 0.5f;
    private const float LaneSpacing = DiceBoardMetrics.DieSize * 1.45f;

    // 회전량은 원본 웹 베이커와 같은 범위를 쓴다. 더 키우면 저해상도 화면에서 눈이 읽히지 않는다.
    private const float NormalSpinLimit = 11f;
    private const float FlipSpinLimit = 40f;

    // 6시 방향에서 들어온 주사위는 착지 후 되튀며 목표보다 0.2쯤 앞(-Z)에 멈춘다. 실측한 값이며,
    // 그만큼 목표 구간 중심을 위로 올려 착지 무게중심이 트레이 중심에 오게 한다.
    private const float TargetUndershootZ = 0.2f;
    private const float MaxSeconds = 3.0f;
    private const float RestLinearSpeed = 0.06f;
    private const float RestAngularSpeed = 0.25f;
    private const int RestStepsRequired = 6;             // 0.1초 연속 정지
    private const int PostSettleSteps = 12;              // 정지 후 여유 프레임 확보용

    // 8면체 형상 상수는 DiceShapeBaker.OctRadius(소스 단위 0.98)를 월드로 환산한 값이다.
    private const float OctaSourceRadius = 0.98f;
    public const float OctaRadius = OctaSourceRadius * DiceBoardMetrics.SourceToUnityScale;
    public static readonly float OctaRestHeight = OctaRadius / Mathf.Sqrt(3f);

    private readonly Scene scene;
    private readonly Rigidbody[] bodies;
    private readonly bool[] octa;
    private readonly Vector3 previousGravity;
    private readonly List<UnityEngine.Object> owned = new();

    public int DiceCount => bodies.Length;

    public bool IsOcta(int index) => octa[index];

    /// <summary>주사위별 기대 정지 높이(중심 Y).</summary>
    public float RestHeight(int index)
    {
        return DiceBoardMetrics.RollSurfaceY + (octa[index] ? OctaRestHeight : DiceBoardMetrics.DieHalfSize);
    }

    /// <summary>주사위별 외접 반경. 공중에서 림을 넘길 여유 높이를 잡는 데 쓴다.</summary>
    public float Circumradius(int index)
    {
        return octa[index] ? OctaRadius : DiceBoardMetrics.DieHalfSize * Mathf.Sqrt(3f);
    }

    /// <summary>면으로 착지한 주사위가 바닥에서 차지하는 반경.</summary>
    public float Footprint(int index)
    {
        return octa[index] ? OctaRadius * Mathf.Sqrt(2f / 3f) : DiceBoardMetrics.DieHalfSize * Mathf.Sqrt(2f);
    }

    public DicePresetBakeRig(int diceCount, int octaCount)
    {
        previousGravity = Physics.gravity;
        Physics.gravity = new Vector3(0f, Gravity, 0f);

        scene = EditorSceneManager.NewPreviewScene();

        PhysicsMaterial surface = new("Dice Bake Surface")
        {
            dynamicFriction = 0.65f,
            staticFriction = 0.65f,
            bounciness = 0.08f,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Average
        };
        owned.Add(surface);

        BuildBoundary(surface);

        bodies = new Rigidbody[diceCount];
        octa = new bool[diceCount];
        for (int index = 0; index < diceCount; index++)
        {
            // 8면체는 뒤쪽 슬롯을 차지한다. 재생 측 슬롯 정렬 규약과 같다.
            octa[index] = index >= diceCount - octaCount;
            bodies[index] = BuildDie(index, octa[index], surface);
        }
    }

    private void BuildBoundary(PhysicsMaterial surface)
    {
        float minX = DiceBoardMetrics.PlayBoundsMinX;
        float maxX = DiceBoardMetrics.PlayBoundsMaxX;
        float minZ = DiceBoardMetrics.PlayBoundsMinZ;
        float maxZ = DiceBoardMetrics.PlayBoundsMaxZ;
        float width = maxX - minX;
        float depth = maxZ - minZ;
        float centerX = (minX + maxX) * 0.5f;
        float centerZ = (minZ + maxZ) * 0.5f;
        float wallHeight = DiceBoardMetrics.TrayRimTopY - DiceBoardMetrics.RollSurfaceY;
        float wallCenterY = DiceBoardMetrics.RollSurfaceY + wallHeight * 0.5f;

        AddBox("Floor", surface,
            new Vector3(centerX, DiceBoardMetrics.RollSurfaceY - WallThickness * 0.5f, centerZ),
            new Vector3(width + WallThickness * 2f, WallThickness, depth + WallThickness * 2f));

        AddBox("Wall -X", surface,
            new Vector3(minX - WallThickness * 0.5f, wallCenterY, centerZ),
            new Vector3(WallThickness, wallHeight, depth));
        AddBox("Wall +X", surface,
            new Vector3(maxX + WallThickness * 0.5f, wallCenterY, centerZ),
            new Vector3(WallThickness, wallHeight, depth));
        AddBox("Wall -Z", surface,
            new Vector3(centerX, wallCenterY, minZ - WallThickness * 0.5f),
            new Vector3(width + WallThickness * 2f, wallHeight, WallThickness));
        AddBox("Wall +Z", surface,
            new Vector3(centerX, wallCenterY, maxZ + WallThickness * 0.5f),
            new Vector3(width + WallThickness * 2f, wallHeight, WallThickness));
    }

    private void AddBox(string name, PhysicsMaterial surface, Vector3 center, Vector3 size)
    {
        GameObject box = new(name, typeof(BoxCollider));
        SceneManager.MoveGameObjectToScene(box, scene);
        box.transform.position = center;
        BoxCollider collider = box.GetComponent<BoxCollider>();
        collider.size = size;
        collider.sharedMaterial = surface;
    }

    private Rigidbody BuildDie(int index, bool isOcta, PhysicsMaterial surface)
    {
        GameObject die = new($"Die {index}");
        SceneManager.MoveGameObjectToScene(die, scene);

        Collider collider;
        if (isOcta)
        {
            Mesh mesh = BuildOctahedron(OctaRadius);
            owned.Add(mesh);
            MeshCollider meshCollider = die.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = true;
            collider = meshCollider;
        }
        else
        {
            BoxCollider boxCollider = die.AddComponent<BoxCollider>();
            boxCollider.size = Vector3.one * DiceBoardMetrics.DieSize;
            collider = boxCollider;
        }
        collider.sharedMaterial = surface;

        Rigidbody body = die.AddComponent<Rigidbody>();
        body.mass = 1f;
        body.linearDamping = 0.45f;
        body.angularDamping = 0.70f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.None;
        return body;
    }

    /// <summary>축 위 여섯 꼭짓점으로 만든 정팔면체. 면 법선이 (±1,±1,±1) 방향이라 게임 형상과 같다.</summary>
    private static Mesh BuildOctahedron(float radius)
    {
        Vector3[] axes =
        {
            new(radius, 0f, 0f), new(-radius, 0f, 0f),
            new(0f, radius, 0f), new(0f, -radius, 0f),
            new(0f, 0f, radius), new(0f, 0f, -radius)
        };

        List<Vector3> vertices = new();
        List<int> triangles = new();
        for (int signX = 0; signX < 2; signX++)
        {
            for (int signY = 0; signY < 2; signY++)
            {
                for (int signZ = 0; signZ < 2; signZ++)
                {
                    Vector3 a = axes[signX];
                    Vector3 b = axes[2 + signY];
                    Vector3 c = axes[4 + signZ];
                    Vector3 outward = new(signX == 0 ? 1f : -1f, signY == 0 ? 1f : -1f, signZ == 0 ? 1f : -1f);
                    int first = vertices.Count;
                    if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) >= 0f)
                    {
                        vertices.Add(a);
                        vertices.Add(b);
                        vertices.Add(c);
                    }
                    else
                    {
                        vertices.Add(a);
                        vertices.Add(c);
                        vertices.Add(b);
                    }
                    triangles.Add(first);
                    triangles.Add(first + 1);
                    triangles.Add(first + 2);
                }
            }
        }

        Mesh mesh = new() { name = "Dice Bake Octahedron" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>한 판을 굴리고 프레임을 기록한다.</summary>
    public DiceSimulationResult Simulate(System.Random random, bool flip)
    {
        if (flip) LaunchFlip(random);
        else LaunchThrow(random);

        PhysicsScene physics = scene.GetPhysicsScene();
        List<Vector3[]> positions = new();
        List<Quaternion[]> rotations = new();
        float[] previousVerticalSpeed = new float[bodies.Length];
        for (int index = 0; index < bodies.Length; index++)
        {
            previousVerticalSpeed[index] = bodies[index].linearVelocity.y;
        }

        int maxSteps = Mathf.CeilToInt(MaxSeconds / SimulationStep);
        int restSteps = 0;
        int settleStep = -1;
        float impactTime = -1f;

        for (int step = 0; step <= maxSteps; step++)
        {
            if (step % SampleInterval == 0) Capture(positions, rotations);

            physics.Simulate(SimulationStep);
            float time = (step + 1) * SimulationStep;

            // 림을 넘어 나간 주사위는 바닥 콜라이더 밖으로 떨어져 영원히 낙하한다. 즉시 중단해
            // 남은 스텝을 낭비하지 않는다. 이 판은 어차피 인식도 평가에서 탈락한다.
            if (HasEscaped()) break;

            bool allStill = true;
            for (int index = 0; index < bodies.Length; index++)
            {
                Rigidbody body = bodies[index];
                float verticalSpeed = body.linearVelocity.y;
                if (impactTime < 0f && previousVerticalSpeed[index] < -3f && verticalSpeed > previousVerticalSpeed[index] + 6f)
                {
                    impactTime = time;
                }
                previousVerticalSpeed[index] = verticalSpeed;

                if (body.IsSleeping()) continue;
                if (body.linearVelocity.magnitude > RestLinearSpeed || body.angularVelocity.magnitude > RestAngularSpeed)
                {
                    allStill = false;
                }
            }

            restSteps = allStill ? restSteps + 1 : 0;
            if (settleStep < 0 && restSteps >= RestStepsRequired)
            {
                settleStep = step + 1 - RestStepsRequired;
            }
            if (settleStep >= 0 && step >= settleStep + PostSettleSteps)
            {
                Capture(positions, rotations);
                break;
            }
        }

        return new DiceSimulationResult
        {
            Positions = positions.ToArray(),
            Rotations = rotations.ToArray(),
            SettleTime = settleStep >= 0 ? settleStep * SimulationStep : MaxSeconds,
            ImpactTime = impactTime,
            Settled = settleStep >= 0
        };
    }

    private bool HasEscaped()
    {
        foreach (Rigidbody body in bodies)
        {
            if (body.transform.position.y < DiceBoardMetrics.RollSurfaceY - 1.0f) return true;
        }
        return false;
    }

    private void Capture(List<Vector3[]> positions, List<Quaternion[]> rotations)
    {
        Vector3[] framePositions = new Vector3[bodies.Length];
        Quaternion[] frameRotations = new Quaternion[bodies.Length];
        for (int index = 0; index < bodies.Length; index++)
        {
            framePositions[index] = bodies[index].transform.position;
            frameRotations[index] = bodies[index].transform.rotation;
        }
        positions.Add(framePositions);
        rotations.Add(frameRotations);
    }

    /// <summary>
    /// 6시 방향 트레이 밖에서 림을 넘겨 던진다. 착지 목표를 플레이 영역 지터 격자에서 뽑아
    /// 분포가 처음부터 고르게 잡히도록 한다.
    ///
    /// 발사 줄의 순서는 목표 X 순서와 같게 맞춘다. 순서가 어긋나면 궤적이 공중에서 교차해
    /// 주사위끼리 부딪히고 그 충격으로 림 밖까지 날아가 버린다.
    /// </summary>
    private void LaunchThrow(System.Random random)
    {
        Vector3[] targets = SampleTargets(random);
        int[] lanes = RankByTargetX(targets);
        float gravity = -Gravity;
        float startZ = DiceBoardMetrics.PlayBoundsMinZ - 2.0f;
        float startY = DiceBoardMetrics.TrayRimTopY - 0.4f;

        for (int index = 0; index < bodies.Length; index++)
        {
            // 발사 줄 간격은 주사위 대각선보다 넓어야 한다. 좁으면 스폰 순간 서로 파고들어
            // PhysX가 분리 충격을 주고, 그 힘으로 주사위가 트레이 밖까지 날아간다.
            float lane = lanes[index] - (bodies.Length - 1) * 0.5f;
            Vector3 start = new(
                lane * LaneSpacing + Range(random, -0.08f, 0.08f),
                startY + Range(random, -0.1f, 0.1f),
                startZ - (lanes[index] % 2) * 1.1f + Range(random, -0.15f, 0.15f));
            Vector3 target = targets[index];
            float clearance = DiceBoardMetrics.TrayRimTopY + Circumradius(index) + 0.15f;

            Vector3 velocity = Vector3.zero;
            for (float flightTime = 0.55f; flightTime <= 0.90f; flightTime += 0.05f)
            {
                float verticalSpeed = (target.y - start.y + 0.5f * gravity * flightTime * flightTime) / flightTime;
                float forwardSpeed = (target.z - start.z) / flightTime;
                velocity = new Vector3((target.x - start.x) / flightTime, verticalSpeed, forwardSpeed);

                float rimTime = (DiceBoardMetrics.PlayBoundsMinZ - start.z) / forwardSpeed;
                float rimHeight = start.y + verticalSpeed * rimTime - 0.5f * gravity * rimTime * rimTime;
                if (rimHeight >= clearance) break;
            }

            Place(index, start, RandomRotation(random), velocity, RandomSpin(random, NormalSpinLimit));
        }
    }

    /// <summary>판 뒤집기. 바닥에서 수직으로 크게 튀어오른다.</summary>
    private void LaunchFlip(System.Random random)
    {
        for (int index = 0; index < bodies.Length; index++)
        {
            float lane = bodies.Length == 1 ? 0f : index - (bodies.Length - 1) * 0.5f;
            Vector3 start = new(
                lane * DiceBoardMetrics.DieSize * 1.6f + Range(random, -0.1f, 0.1f),
                RestHeight(index) + 0.4f,
                DiceBoardMetrics.TrayCenterZ + Range(random, -0.5f, 0.5f));

            // 체공 시간이 판정 시간 상한을 넘지 않도록 속도를 잡았다. 정점은 주사위 열 배 높이쯤이다.
            Vector3 velocity = new(
                Range(random, -2.5f, 2.5f),
                Range(random, 28f, 34f),
                Range(random, -2.5f, 2.5f));

            Place(index, start, RandomRotation(random), velocity, RandomSpin(random, FlipSpinLimit));
        }
    }

    private void Place(int index, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 spin)
    {
        Rigidbody body = bodies[index];
        body.transform.SetPositionAndRotation(position, rotation);
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.WakeUp();
        body.linearVelocity = velocity;
        body.angularVelocity = spin;
    }

    /// <summary>플레이 영역을 칸으로 나눠 칸마다 목표점을 하나씩 뽑고 슬롯에 무작위 배정한다.</summary>
    private Vector3[] SampleTargets(System.Random random)
    {
        // 여유는 착지한 주사위가 차지하는 바닥 발자국 기준이다. 외접 반경으로 잡으면 너무 넓어
        // 플레이 영역 가장자리가 통째로 비고 착지가 가운데로 몰린다.
        float margin = 0f;
        for (int index = 0; index < bodies.Length; index++)
        {
            margin = Mathf.Max(margin, Footprint(index) + 0.05f);
        }
        float minX = DiceBoardMetrics.PlayBoundsMinX + margin;
        float maxX = DiceBoardMetrics.PlayBoundsMaxX - margin;

        // 플레이 영역은 킵 홈 쪽으로 치우쳐 있어 그 중심이 트레이 중심보다 아래(-Z)다. 목표 구간을
        // 영역 그대로 쓰면 착지가 화면 중앙 아래에 몰리므로, 트레이 중심을 기준으로 대칭이 되게 좁힌다.
        float aimCenterZ = DiceBoardMetrics.TrayCenterZ + TargetUndershootZ;
        float halfDepth = Mathf.Min(
            aimCenterZ - (DiceBoardMetrics.PlayBoundsMinZ + margin),
            DiceBoardMetrics.PlayBoundsMaxZ - margin - aimCenterZ);
        float minZ = aimCenterZ - halfDepth;
        float maxZ = aimCenterZ + halfDepth;

        int columns = Mathf.CeilToInt(Mathf.Sqrt(bodies.Length));
        int rows = Mathf.CeilToInt(bodies.Length / (float)columns);
        List<Vector3> cells = new();
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                float cellMinX = Mathf.Lerp(minX, maxX, column / (float)columns);
                float cellMaxX = Mathf.Lerp(minX, maxX, (column + 1) / (float)columns);
                float cellMinZ = Mathf.Lerp(minZ, maxZ, row / (float)rows);
                float cellMaxZ = Mathf.Lerp(minZ, maxZ, (row + 1) / (float)rows);
                cells.Add(new Vector3(
                    Range(random, cellMinX, cellMaxX),
                    0f,
                    Range(random, cellMinZ, cellMaxZ)));
            }
        }

        for (int index = cells.Count - 1; index > 0; index--)
        {
            int swap = random.Next(index + 1);
            (cells[index], cells[swap]) = (cells[swap], cells[index]);
        }

        Vector3[] targets = new Vector3[bodies.Length];
        for (int index = 0; index < bodies.Length; index++)
        {
            targets[index] = cells[index];
            targets[index].y = RestHeight(index);
        }
        return targets;
    }

    /// <summary>슬롯별 목표 X의 순위. 발사 줄에서 몇 번째 자리에 설지를 정한다.</summary>
    private static int[] RankByTargetX(Vector3[] targets)
    {
        int[] order = new int[targets.Length];
        for (int index = 0; index < order.Length; index++) order[index] = index;
        System.Array.Sort(order, (left, right) => targets[left].x.CompareTo(targets[right].x));

        int[] ranks = new int[targets.Length];
        for (int rank = 0; rank < order.Length; rank++) ranks[order[rank]] = rank;
        return ranks;
    }

    private static float Range(System.Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }

    private static Quaternion RandomRotation(System.Random random)
    {
        return Quaternion.Euler(Range(random, 0f, 360f), Range(random, 0f, 360f), Range(random, 0f, 360f));
    }

    /// <summary>축별 균등 분포 회전. 범위는 원본 웹 베이커(presetBaker.js:134-138, :153-157)와 같다.</summary>
    private static Vector3 RandomSpin(System.Random random, float limit)
    {
        return new Vector3(
            Range(random, -limit, limit),
            Range(random, -limit, limit),
            Range(random, -limit, limit));
    }

    public void Dispose()
    {
        foreach (UnityEngine.Object item in owned)
        {
            if (item != null) UnityEngine.Object.DestroyImmediate(item);
        }
        owned.Clear();
        if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        Physics.gravity = previousGravity;
    }
}
