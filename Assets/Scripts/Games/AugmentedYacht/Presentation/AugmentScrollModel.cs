using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>직사각형 종이, 왼쪽 말림, 가죽 띠, 큐브 인장과 오버레이 기준점을 묶습니다.</summary>
    public sealed class AugmentScrollModel : MonoBehaviour
    {
        [SerializeField] private Transform[] overlayAnchors;
        [SerializeField] private Renderer waxRenderer;
        [SerializeField] private Transform cubeSealMark;
        [SerializeField] private bool ownsRuntimeAssets;

        private readonly List<Mesh> runtimeMeshes = new();
        private readonly List<Material> runtimeMaterials = new();

        public IReadOnlyList<Transform> OverlayAnchors => overlayAnchors;
        public Renderer WaxRenderer => waxRenderer;
        public Transform CubeSealMark => cubeSealMark;
        public bool HasCenteredSeal => waxRenderer != null
            && Mathf.Abs(waxRenderer.transform.localPosition.z) < .05f;

        public void Configure(Transform[] anchors, Renderer sealRenderer, Transform sealMark, bool ownsAssets)
        {
            overlayAnchors = anchors;
            waxRenderer = sealRenderer;
            cubeSealMark = sealMark;
            ownsRuntimeAssets = ownsAssets;
        }

        public void RegisterRuntimeAsset(Mesh mesh)
        {
            if (mesh != null) runtimeMeshes.Add(mesh);
        }

        public void RegisterRuntimeAsset(Material material)
        {
            if (material != null) runtimeMaterials.Add(material);
        }

        public bool TryGetOverlayCorners(Vector3[] corners)
        {
            if (corners == null || corners.Length < 4 || overlayAnchors == null || overlayAnchors.Length != 4)
                return false;
            for (int i = 0; i < 4; i++)
            {
                if (overlayAnchors[i] == null) return false;
                corners[i] = overlayAnchors[i].position;
            }
            return true;
        }

        public void SetDisplayState(AugmentCardDisplayState state)
        {
            if (waxRenderer == null) return;
            Color color = state switch
            {
                AugmentCardDisplayState.Selected => new Color(.94f, .45f, .17f, 1f),
                AugmentCardDisplayState.Conflict => new Color(.34f, .055f, .055f, 1f),
                AugmentCardDisplayState.Used => new Color(.32f, .25f, .25f, 1f),
                AugmentCardDisplayState.Disabled => new Color(.23f, .19f, .18f, 1f),
                _ => new Color(.54f, .10f, .09f, 1f)
            };
            var block = new MaterialPropertyBlock();
            waxRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            waxRenderer.SetPropertyBlock(block);
        }

        private void OnDestroy()
        {
            if (!ownsRuntimeAssets) return;
            for (int i = 0; i < runtimeMeshes.Count; i++) DestroyOwned(runtimeMeshes[i]);
            for (int i = 0; i < runtimeMaterials.Count; i++) DestroyOwned(runtimeMaterials[i]);
        }

        private static void DestroyOwned(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }

    /// <summary>정적 프리팹을 우선 사용하고 누락 시 같은 구조를 런타임에 생성합니다.</summary>
    public static class AugmentScrollModelFactory
    {
        public const float ReferenceWidth = 4.30f;
        public const float ReferenceHeight = 2.30f;
        public const int PaperColumns = 25;
        public const int PaperRows = 15;
        public const int RollAxisSegments = 18;
        public const int RollSpiralSegments = 44;
        public const float RollTurns = 2.5f;
        public const float RollRotationZ = 210f;
        private const float PaperThickness = .022f;
        private const float RollCenterXNormalized = -.385f;
        private const float RollCenterYNormalized = .125f;

        public static AugmentScrollModel Create(Transform parent, AugmentParchmentPreset preset, Vector2 size)
        {
            int key = (int)AugmentParchmentVisuals.Normalize((int)preset);
            GameObject prefab = Resources.Load<GameObject>($"AugmentScrolls/AugmentScrollPreset_{key}");
            if (prefab != null)
            {
                GameObject instance = Object.Instantiate(prefab, parent, false);
                instance.name = $"Augment Scroll Preset {key}";
                instance.transform.localScale = new Vector3(
                    size.x / ReferenceWidth,
                    Mathf.Min(size.x / ReferenceWidth, size.y / ReferenceHeight),
                    size.y / ReferenceHeight);
                return instance.GetComponent<AugmentScrollModel>();
            }

            Material front = CreateMaterial("Runtime Scroll Paper Front", new Color(.98f, .97f, .95f, 1f), .10f, true);
            Material underside = CreateMaterial("Runtime Scroll Paper Underside", new Color(.97f, .96f, .93f, 1f), .08f, true);
            Material leather = CreateMaterial("Runtime Scroll Leather", new Color(.20f, .09f, .045f, 1f), .18f, false);
            Material wax = CreateMaterial("Runtime Scroll Wax", new Color(.54f, .10f, .09f, 1f), .34f, false);
            Material[] materials = { front, underside, leather, wax };
            AugmentScrollModel model = Build(parent, preset, size.x, size.y, materials, true);
            model.RegisterRuntimeAsset(front);
            model.RegisterRuntimeAsset(underside);
            model.RegisterRuntimeAsset(leather);
            model.RegisterRuntimeAsset(wax);
            return model;
        }

        public static AugmentScrollModel Build(
            Transform parent,
            AugmentParchmentPreset preset,
            float width,
            float height,
            Material[] materials,
            bool ownsAssets)
        {
            int key = (int)AugmentParchmentVisuals.Normalize((int)preset);
            GameObject root = new($"Augment Scroll Preset {key}");
            root.layer = parent != null ? parent.gameObject.layer : 0;
            root.transform.SetParent(parent, false);
            AugmentScrollModel model = root.AddComponent<AugmentScrollModel>();

            Material front = GetMaterial(materials, 0);
            Material underside = GetMaterial(materials, 1) ?? front;
            Material leather = GetMaterial(materials, 2) ?? underside;
            Material wax = GetMaterial(materials, 3) ?? leather;

            Mesh body = CreatePaperBodyMesh(preset, width, height);
            CreateMeshPart(root.transform, "Rectangular Paper Body", body, new[] { front, underside });
            Mesh roll = CreateRolledLayersMesh(preset, width, height);
            Transform rollTransform = CreateMeshPart(
                root.transform, "Left Rolled Paper 2.5 Turns", roll, new[] { front, underside }).transform;
            Vector3 rollCenter = GetRollCenter(width, height);
            rollTransform.localPosition = rollCenter;
            rollTransform.localRotation = Quaternion.Euler(0f, 0f, RollRotationZ);
            Mesh band = CreateSealBandMesh(preset, width, height);
            Transform bandTransform = CreateMeshPart(root.transform, "Leather Seal Band", band, new[] { leather }).transform;
            bandTransform.localPosition = rollCenter;
            Mesh seal = CreateWaxSealMesh(preset, width, height);
            MeshRenderer sealRenderer = CreateMeshPart(root.transform, "Crimson Wax Seal", seal, new[] { wax });
            sealRenderer.transform.localPosition = rollCenter;
            Mesh mark = CreateCubeSealMarkMesh(width, height);
            Transform markTransform = CreateMeshPart(root.transform, "Embossed Cube Seal Mark", mark, new[] { leather }).transform;
            markTransform.localPosition = rollCenter;

            Transform[] anchors = CreateOverlayAnchors(root.transform, width, height);
            model.Configure(anchors, sealRenderer, markTransform, ownsAssets);
            if (ownsAssets)
            {
                model.RegisterRuntimeAsset(body);
                model.RegisterRuntimeAsset(roll);
                model.RegisterRuntimeAsset(band);
                model.RegisterRuntimeAsset(seal);
                model.RegisterRuntimeAsset(mark);
            }
            return model;
        }

        public static Mesh CreatePaperBodyMesh(AugmentParchmentPreset preset, float width, float height)
        {
            int key = (int)AugmentParchmentVisuals.Normalize((int)preset);
            var vertices = new List<Vector3>(PaperColumns * PaperRows * 2);
            var uvs = new List<Vector2>(PaperColumns * PaperRows * 2);
            var frontTriangles = new List<int>();
            var undersideTriangles = new List<int>();

            for (int layer = 0; layer < 2; layer++)
            {
                float thickness = layer == 0 ? -PaperThickness : 0f;
                for (int zIndex = 0; zIndex < PaperRows; zIndex++)
                {
                    float v = (float)zIndex / (PaperRows - 1);
                    float sideWear = SideInset(key, v);
                    for (int xIndex = 0; xIndex < PaperColumns; xIndex++)
                    {
                        float u = (float)xIndex / (PaperColumns - 1);
                        float bottomInset = AugmentParchmentVisuals.SampleEdgeInset(preset, u, false);
                        float topInset = AugmentParchmentVisuals.SampleEdgeInset(preset, u, true);
                        float minZ = (-.5f + bottomInset) * height;
                        float maxZ = (.5f - topInset) * height;
                        // 펼친 본문을 말림의 중심까지 연장해 두 부분이 한 장의 종이처럼 겹쳐 보이게 한다.
                        float x = Mathf.Lerp(RollCenterXNormalized * width, (.5f - sideWear) * width, u);
                        float z = Mathf.Lerp(minZ, maxZ, v);
                        float joinLift = Mathf.Exp(-Mathf.Pow(u / .10f, 2f)) * height * .035f;
                        float edgeLift = Mathf.Pow(Mathf.Abs(u - .5f) * 2f, 6f) * height * .010f;
                        float broadCrease = Mathf.Sin((u * 1.7f + v * 1.15f + key * .31f) * Mathf.PI) * height * .004f;
                        vertices.Add(new Vector3(x, thickness + joinLift + edgeLift + broadCrease, z));
                        uvs.Add(new Vector2(u * 1.15f, v));
                    }
                }
            }

            AddGridSurfaces(frontTriangles, undersideTriangles, PaperColumns, PaperRows);
            AddGridSides(undersideTriangles, PaperColumns, PaperRows, PaperColumns * PaperRows);
            return BuildMesh($"Augment_Rectangular_Paper_{key}", vertices, uvs, frontTriangles, undersideTriangles);
        }

        public static Mesh CreateRolledLayersMesh(AugmentParchmentPreset preset, float width, float height)
        {
            int key = (int)AugmentParchmentVisuals.Normalize((int)preset);
            int layerVertexCount = RollAxisSegments * RollSpiralSegments;
            var vertices = new List<Vector3>(layerVertexCount * 2);
            var uvs = new List<Vector2>(layerVertexCount * 2);
            var frontTriangles = new List<int>();
            var undersideTriangles = new List<int>();
            float baseCenterX = width * key * .002f;
            float baseCenterY = 0f;
            float innerRadius = height * .030f;
            float outerRadius = height * .140f;

            for (int layer = 0; layer < 2; layer++)
            for (int axisIndex = 0; axisIndex < RollAxisSegments; axisIndex++)
            {
                float axisT = (float)axisIndex / (RollAxisSegments - 1);
                float centerPinch = 1f - .24f * Mathf.Sin(axisT * Mathf.PI);
                float z = Mathf.Lerp(-height * .515f, height * .515f, axisT);
                float centerX = baseCenterX + (axisT - .5f) * height * .018f;
                float centerY = baseCenterY + Mathf.Abs(axisT - .5f) * height * .018f;
                for (int spiralIndex = 0; spiralIndex < RollSpiralSegments; spiralIndex++)
                {
                    float q = (float)spiralIndex / (RollSpiralSegments - 1);
                    float radius = Mathf.Lerp(innerRadius, outerRadius, q) * centerPinch;
                    if (layer == 0) radius = Mathf.Max(.004f, radius - PaperThickness);
                    float angle = -RollTurns * Mathf.PI + q * RollTurns * Mathf.PI * 2f;
                    float x = centerX + Mathf.Cos(angle) * radius;
                    float y = centerY + Mathf.Sin(angle) * radius * 1.32f;
                    vertices.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(axisT, q * RollTurns));
                }
            }

            AddGridSurfaces(frontTriangles, undersideTriangles, RollSpiralSegments, RollAxisSegments);
            AddGridSides(undersideTriangles, RollSpiralSegments, RollAxisSegments, layerVertexCount);
            return BuildMesh($"Augment_Left_Roll_{key}", vertices, uvs, frontTriangles, undersideTriangles);
        }

        public static Mesh CreateSealBandMesh(AugmentParchmentPreset preset, float width, float height)
        {
            const int segments = 28;
            float centerX = 0f;
            float centerY = 0f;
            float radius = height * .140f * .78f;
            float halfWidth = height * .047f;
            var vertices = new List<Vector3>(segments * 2);
            var uvs = new List<Vector2>(segments * 2);
            var triangles = new List<int>();

            for (int side = 0; side < 2; side++)
            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / segments;
                float angle = t * Mathf.PI * 2f;
                vertices.Add(new Vector3(
                    centerX + Mathf.Cos(angle) * radius * 1.04f,
                    centerY + Mathf.Sin(angle) * radius * 1.34f,
                    side == 0 ? -halfWidth : halfWidth));
                uvs.Add(new Vector2(side, t));
            }
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int a = i;
                int b = next;
                int c = segments + i;
                int d = segments + next;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
            return BuildSingleSubmesh("Augment_Leather_Band", vertices, uvs, triangles);
        }

        public static Mesh CreateWaxSealMesh(AugmentParchmentPreset preset, float width, float height)
        {
            int key = (int)AugmentParchmentVisuals.Normalize((int)preset);
            const int segments = 18;
            const int rings = 3;
            float radius = height * .094f;
            float centerX = 0f;
            float centerZ = 0f;
            float bottomY = height * .150f;
            float topY = bottomY + height * .050f;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            for (int ring = 0; ring < rings; ring++)
            {
                float ringT = (float)ring / (rings - 1);
                float ringRadius = radius * ringT;
                float y = Mathf.Lerp(topY + height * .010f, topY, ringT);
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    float irregular = ring == rings - 1 ? 1f + .065f * Mathf.Sin(angle * 5f + key) : 1f;
                    vertices.Add(new Vector3(
                        centerX + Mathf.Cos(angle) * ringRadius * irregular,
                        y,
                        centerZ + Mathf.Sin(angle) * ringRadius * irregular));
                    uvs.Add(new Vector2(.5f + Mathf.Cos(angle) * ringT * .5f, .5f + Mathf.Sin(angle) * ringT * .5f));
                }
            }

            for (int ring = 0; ring < rings - 1; ring++)
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int a = ring * segments + i;
                int b = ring * segments + next;
                int c = (ring + 1) * segments + i;
                int d = (ring + 1) * segments + next;
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(b); triangles.Add(d); triangles.Add(c);
            }

            int outerStart = (rings - 1) * segments;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int topA = outerStart + i;
                int topB = outerStart + next;
                int bottomA = vertices.Count;
                Vector3 a = vertices[topA];
                vertices.Add(new Vector3(a.x, bottomY, a.z));
                uvs.Add(uvs[topA]);
                int bottomB = vertices.Count;
                Vector3 b = vertices[topB];
                vertices.Add(new Vector3(b.x, bottomY, b.z));
                uvs.Add(uvs[topB]);
                triangles.Add(topA); triangles.Add(bottomA); triangles.Add(topB);
                triangles.Add(topB); triangles.Add(bottomA); triangles.Add(bottomB);
            }
            return BuildSingleSubmesh($"Augment_Crimson_Seal_{key}", vertices, uvs, triangles);
        }

        public static Mesh CreateCubeSealMarkMesh(float width, float height)
        {
            float centerX = 0f;
            float centerY = height * .219f;
            float radius = height * .067f;
            Vector3[] points =
            {
                new(centerX, centerY, radius),
                new(centerX + radius * .866f, centerY, radius * .5f),
                new(centerX + radius * .866f, centerY, -radius * .5f),
                new(centerX, centerY, -radius),
                new(centerX - radius * .866f, centerY, -radius * .5f),
                new(centerX - radius * .866f, centerY, radius * .5f),
                new(centerX, centerY, 0f)
            };
            var vertices = new List<Vector3>(36);
            var uvs = new List<Vector2>(36);
            var triangles = new List<int>(54);
            float thickness = height * .010f;
            for (int i = 0; i < 6; i++)
                AddFlatStrip(vertices, uvs, triangles, points[i], points[(i + 1) % 6], thickness);
            AddFlatStrip(vertices, uvs, triangles, points[6], points[0], thickness);
            AddFlatStrip(vertices, uvs, triangles, points[6], points[2], thickness);
            AddFlatStrip(vertices, uvs, triangles, points[6], points[4], thickness);
            return BuildSingleSubmesh("Augment_Embossed_Cube_Mark", vertices, uvs, triangles);
        }

        private static Transform[] CreateOverlayAnchors(Transform parent, float width, float height)
        {
            Rect safe = AugmentParchmentVisuals.ContentSafeRect;
            float left = (safe.xMin - .5f) * width;
            float right = (safe.xMax - .5f) * width;
            float bottom = (safe.yMin - .5f) * height;
            float top = (safe.yMax - .5f) * height;
            Vector3[] positions =
            {
                new(left, .052f, bottom),
                new(right, .052f, bottom),
                new(right, .052f, top),
                new(left, .052f, top)
            };
            string[] names = { "Overlay Bottom Left", "Overlay Bottom Right", "Overlay Top Right", "Overlay Top Left" };
            Transform[] anchors = new Transform[4];
            for (int i = 0; i < anchors.Length; i++)
            {
                GameObject anchor = new(names[i]);
                anchor.transform.SetParent(parent, false);
                anchor.transform.localPosition = positions[i];
                anchors[i] = anchor.transform;
            }
            return anchors;
        }

        private static Material GetMaterial(Material[] materials, int index) =>
            materials != null && index >= 0 && index < materials.Length ? materials[index] : null;

        private static Vector3 GetRollCenter(float width, float height) =>
            new(width * RollCenterXNormalized, height * RollCenterYNormalized, 0f);

        private static MeshRenderer CreateMeshPart(Transform parent, string name, Mesh mesh, Material[] materials)
        {
            GameObject part = new(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.layer = parent.gameObject.layer;
            part.transform.SetParent(parent, false);
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
            renderer.receiveShadows = true;
            return renderer;
        }

        private static Material CreateMaterial(string name, Color color, float smoothness, bool usePaperTexture)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (usePaperTexture)
            {
                Texture2D texture = Resources.Load<Texture2D>("AugmentScrolls/parchment_scroll_albedo")
                    ?? Resources.Load<Texture2D>("Parchment/parchment_base");
                if (texture != null)
                {
                    material.mainTexture = texture;
                    if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                    material.mainTextureScale = new Vector2(1.10f, .92f);
                }
            }
            return material;
        }

        private static float SideInset(int key, float v)
        {
            float baseInset = .004f + .003f * Mathf.Abs(Mathf.Sin(v * Mathf.PI * 4f + key));
            if (key == 3) baseInset += .010f * Mathf.Pow(Mathf.Abs(v - .5f) * 2f, 5f);
            return Mathf.Min(.016f, baseInset);
        }

        private static void AddFlatStrip(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            Vector3 start,
            Vector3 end,
            float thickness)
        {
            Vector3 direction = end - start;
            Vector3 side = new(-direction.z, 0f, direction.x);
            side = side.normalized * thickness * .5f;
            int o = vertices.Count;
            vertices.Add(start - side);
            vertices.Add(start + side);
            vertices.Add(end + side);
            vertices.Add(end - side);
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(0f, 1f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(1f, 0f));
            triangles.Add(o); triangles.Add(o + 1); triangles.Add(o + 2);
            triangles.Add(o); triangles.Add(o + 2); triangles.Add(o + 3);
        }

        private static void AddGridSurfaces(
            List<int> front,
            List<int> underside,
            int columns,
            int rows)
        {
            int layerVertexCount = columns * rows;
            for (int layer = 0; layer < 2; layer++)
            {
                int offset = layer * layerVertexCount;
                List<int> target = layer == 1 ? front : underside;
                for (int row = 0; row < rows - 1; row++)
                for (int column = 0; column < columns - 1; column++)
                {
                    int a = offset + row * columns + column;
                    int b = a + 1;
                    int c = a + columns;
                    int d = c + 1;
                    if (layer == 1)
                    {
                        target.Add(a); target.Add(c); target.Add(b);
                        target.Add(b); target.Add(c); target.Add(d);
                    }
                    else
                    {
                        target.Add(a); target.Add(b); target.Add(c);
                        target.Add(b); target.Add(d); target.Add(c);
                    }
                }
            }
        }

        private static void AddGridSides(
            List<int> triangles,
            int columns,
            int rows,
            int layerVertexCount)
        {
            AddGridSide(triangles, columns, rows, layerVertexCount, true);
            AddGridSide(triangles, columns, rows, layerVertexCount, false);
        }

        private static void AddGridSide(
            List<int> triangles,
            int columns,
            int rows,
            int layerVertexCount,
            bool horizontal)
        {
            int count = horizontal ? columns : rows;
            for (int edge = 0; edge < 2; edge++)
            for (int i = 0; i < count - 1; i++)
            {
                int a = horizontal ? edge * (rows - 1) * columns + i : i * columns + edge * (columns - 1);
                int b = horizontal ? a + 1 : a + columns;
                int c = a + layerVertexCount;
                int d = b + layerVertexCount;
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(b); triangles.Add(d); triangles.Add(c);
            }
        }

        private static Mesh BuildMesh(
            string name,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> frontTriangles,
            List<int> undersideTriangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(frontTriangles, 0);
            mesh.SetTriangles(undersideTriangles, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildSingleSubmesh(
            string name,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
