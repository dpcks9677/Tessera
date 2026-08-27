using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>펼쳐진 종이, 다층 롤, 밀랍 인장과 오버레이 기준점을 묶는 3D 스크롤입니다.</summary>
    public sealed class AugmentScrollModel : MonoBehaviour
    {
        [SerializeField] private Transform[] overlayAnchors;
        [SerializeField] private Renderer waxRenderer;
        [SerializeField] private bool ownsRuntimeAssets;

        private readonly List<Mesh> runtimeMeshes = new();
        private readonly List<Material> runtimeMaterials = new();

        public IReadOnlyList<Transform> OverlayAnchors => overlayAnchors;
        public Renderer WaxRenderer => waxRenderer;
        public bool HasCenteredSeal => waxRenderer != null && Mathf.Abs(waxRenderer.transform.localPosition.x) < 1.2f;

        public void Configure(Transform[] anchors, Renderer sealRenderer, bool ownsAssets)
        {
            overlayAnchors = anchors;
            waxRenderer = sealRenderer;
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

    /// <summary>정적 프리팹을 우선 사용하고 누락 시 동일 구조의 런타임 3D 스크롤을 생성합니다.</summary>
    public static class AugmentScrollModelFactory
    {
        public const float ReferenceWidth = 4.30f;
        public const float ReferenceHeight = 2.30f;
        public const int PaperColumns = 21;
        public const int PaperRows = 13;
        public const int RollAxisSegments = 14;
        public const int RollSpiralSegments = 36;
        private const float PaperThickness = .022f;

        public static AugmentScrollModel Create(
            Transform parent,
            AugmentParchmentPreset preset,
            Vector2 size)
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

            Material front = CreateMaterial(
                "Runtime Scroll Paper Front", new Color(.91f, .76f, .48f, 1f), .11f, true);
            Material underside = CreateMaterial(
                "Runtime Scroll Paper Underside", new Color(.39f, .20f, .10f, 1f), .08f, true);
            Material wax = CreateMaterial(
                "Runtime Scroll Wax", new Color(.54f, .10f, .09f, 1f), .34f, false);
            Material[] materials = { front, underside, wax };
            AugmentScrollModel model = Build(parent, preset, size.x, size.y, materials, true);
            model.RegisterRuntimeAsset(front);
            model.RegisterRuntimeAsset(underside);
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

            Material front = materials != null && materials.Length > 0 ? materials[0] : null;
            Material underside = materials != null && materials.Length > 1 ? materials[1] : front;
            Material wax = materials != null && materials.Length > 2 ? materials[2] : front;

            Mesh body = CreatePaperBodyMesh(preset, width, height);
            CreateMeshPart(root.transform, "Paper Front And Underside", body, new[] { front, underside });
            Mesh roll = CreateRolledLayersMesh(preset, width, height);
            CreateMeshPart(root.transform, "Rolled Inner Layers", roll, new[] { front, underside });

            Mesh band = CreateSealBandMesh(preset, width, height);
            CreateMeshPart(root.transform, "Seal Band", band, new[] { wax });

            Mesh seal = CreateWaxSealMesh(preset, width, height);
            MeshRenderer sealRenderer = CreateMeshPart(root.transform, "Wax Seal", seal, new[] { wax });

            Mesh ribbon = CreateRibbonMesh(preset, width, height);
            CreateMeshPart(root.transform, "Ribbon Tail", ribbon, new[] { wax });

            Transform[] anchors = CreateOverlayAnchors(root.transform, width, height);
            model.Configure(anchors, sealRenderer, ownsAssets);
            if (ownsAssets)
            {
                model.RegisterRuntimeAsset(body);
                model.RegisterRuntimeAsset(roll);
                model.RegisterRuntimeAsset(band);
                model.RegisterRuntimeAsset(seal);
                model.RegisterRuntimeAsset(ribbon);
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
                    for (int xIndex = 0; xIndex < PaperColumns; xIndex++)
                    {
                        float u = (float)xIndex / (PaperColumns - 1);
                        float bottomInset = EdgeInset(key, u, false);
                        float topInset = EdgeInset(key, u, true);
                        float minZ = (-.5f + bottomInset) * height;
                        float maxZ = (.5f - topInset) * height;
                        float x = (u - .5f) * width;
                        float z = Mathf.Lerp(minZ, maxZ, v);

                        float leftLift = Mathf.Pow(Mathf.Clamp01((.13f - u) / .13f), 2f) * (.035f + key * .006f);
                        float rightLift = Mathf.Pow(Mathf.Clamp01((u - .84f) / .16f), 2f) * (.045f + key * .009f);
                        float lowerCornerWeight = Mathf.Pow(Mathf.Clamp01((.20f - v) / .20f), 2f);
                        float lowerLift = lowerCornerWeight * (
                            .09f * Mathf.Exp(-Mathf.Pow((u - .12f) / .18f, 2f))
                            + .055f * Mathf.Exp(-Mathf.Pow((u - .90f) / .14f, 2f)));
                        float crease = .012f * Mathf.Sin((u * 2.7f + v * 1.3f + key) * Mathf.PI);
                        float y = thickness + leftLift + rightLift + lowerLift + crease;
                        vertices.Add(new Vector3(x, y, z));
                        uvs.Add(new Vector2(u * 1.35f, v));
                    }
                }
            }

            AddGridSurfaces(frontTriangles, undersideTriangles, PaperColumns, PaperRows);
            AddGridSides(undersideTriangles, PaperColumns, PaperRows, PaperColumns * PaperRows);
            return BuildMesh($"Augment_Scroll_Body_{key}", vertices, uvs, frontTriangles, undersideTriangles);
        }

        public static Mesh CreateRolledLayersMesh(AugmentParchmentPreset preset, float width, float height)
        {
            int key = (int)AugmentParchmentVisuals.Normalize((int)preset);
            int layerVertexCount = RollAxisSegments * RollSpiralSegments;
            var vertices = new List<Vector3>(layerVertexCount * 2);
            var uvs = new List<Vector2>(layerVertexCount * 2);
            var frontTriangles = new List<int>();
            var undersideTriangles = new List<int>();
            float rollStartX = -.46f * width;
            float rollEndX = (.06f + key * .008f) * width;
            float centerZ = height * (.265f + key * .006f);
            float centerY = height * .115f;
            float tilt = (-.018f + key * .009f) * height;
            float innerRadius = height * (.030f + key * .0015f);
            float outerRadius = height * (.125f + key * .004f);

            for (int layer = 0; layer < 2; layer++)
            {
                for (int axisIndex = 0; axisIndex < RollAxisSegments; axisIndex++)
                {
                    float axisT = (float)axisIndex / (RollAxisSegments - 1);
                    float axisEase = Mathf.Sin(axisT * Mathf.PI);
                    float x = Mathf.Lerp(rollStartX, rollEndX, axisT);
                    float localCenterZ = centerZ + (axisT - .5f) * tilt;
                    float localCenterY = centerY + axisEase * height * .012f;
                    for (int spiralIndex = 0; spiralIndex < RollSpiralSegments; spiralIndex++)
                    {
                        float q = (float)spiralIndex / (RollSpiralSegments - 1);
                        float radius = Mathf.Lerp(innerRadius, outerRadius, q)
                            * (1f + .035f * Mathf.Sin(axisT * Mathf.PI * 3f + key));
                        if (layer == 0) radius -= PaperThickness;
                        float angle = -Mathf.PI * 4f + q * Mathf.PI * (4.45f + key * .06f);
                        float y = localCenterY + Mathf.Cos(angle) * radius * 1.48f;
                        float z = localCenterZ + Mathf.Sin(angle) * radius * .86f;
                        vertices.Add(new Vector3(x, y, z));
                        uvs.Add(new Vector2(axisT, q * 2.2f));
                    }
                }
            }

            AddGridSurfaces(frontTriangles, undersideTriangles, RollSpiralSegments, RollAxisSegments);
            AddGridSides(undersideTriangles, RollSpiralSegments, RollAxisSegments, layerVertexCount);
            return BuildMesh($"Augment_Scroll_Roll_{key}", vertices, uvs, frontTriangles, undersideTriangles);
        }

        public static Mesh CreateWaxSealMesh(AugmentParchmentPreset preset, float width, float height)
        {
            int key = (int)AugmentParchmentVisuals.Normalize((int)preset);
            const int segments = 16;
            const int rings = 3;
            float radius = height * (.087f + key * .002f);
            float centerX = width * (-.20f + key * .008f);
            float centerZ = height * (.275f + key * .006f);
            float bottomY = height * .325f;
            float topY = bottomY + height * .045f;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            for (int ring = 0; ring < rings; ring++)
            {
                float ringT = (float)ring / (rings - 1);
                float ringRadius = radius * ringT;
                float y = Mathf.Lerp(topY + height * .008f, topY, ringT);
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
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
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

            var mesh = new Mesh { name = $"Augment_Scroll_Seal_{key}" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateSealBandMesh(AugmentParchmentPreset preset, float width, float height)
        {
            int key = (int)AugmentParchmentVisuals.Normalize((int)preset);
            const int segments = 24;
            float centerX = width * (-.20f + key * .008f);
            float centerY = height * .115f;
            float centerZ = height * (.265f + key * .006f);
            float radius = height * (.137f + key * .003f);
            float halfWidth = width * .022f;
            var vertices = new List<Vector3>(segments * 2);
            var uvs = new List<Vector2>(segments * 2);
            var triangles = new List<int>();

            for (int side = 0; side < 2; side++)
            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / segments;
                float angle = t * Mathf.PI * 2f;
                vertices.Add(new Vector3(
                    centerX + (side == 0 ? -halfWidth : halfWidth),
                    centerY + Mathf.Cos(angle) * radius * 1.49f,
                    centerZ + Mathf.Sin(angle) * radius * .87f));
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
            return BuildSingleSubmesh($"Augment_Scroll_Band_{key}", vertices, uvs, triangles);
        }

        public static Mesh CreateRibbonMesh(AugmentParchmentPreset preset, float width, float height)
        {
            int key = (int)AugmentParchmentVisuals.Normalize((int)preset);
            Vector3 start = new(width * (-.19f + key * .008f), height * .385f, height * .20f);
            Vector3[] points =
            {
                start,
                start + new Vector3(width * .025f, -.05f, -height * .08f),
                start + new Vector3(width * .07f, -.10f, -height * .17f),
                start + new Vector3(width * .13f, -.13f, -height * .25f),
                start + new Vector3(width * .18f, -.15f, -height * .32f)
            };
            float halfWidth = width * .015f;
            var vertices = new List<Vector3>(points.Length * 2);
            var uvs = new List<Vector2>(points.Length * 2);
            var triangles = new List<int>();
            for (int i = 0; i < points.Length; i++)
            {
                float sway = Mathf.Sin((i + key) * 1.4f) * width * .006f;
                vertices.Add(points[i] + new Vector3(-halfWidth + sway, 0f, 0f));
                vertices.Add(points[i] + new Vector3(halfWidth + sway, 0f, 0f));
                float v = (float)i / (points.Length - 1);
                uvs.Add(new Vector2(0f, v));
                uvs.Add(new Vector2(1f, v));
            }
            for (int i = 0; i < points.Length - 1; i++)
            {
                int a = i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(b); triangles.Add(d); triangles.Add(c);
            }
            return BuildSingleSubmesh($"Augment_Scroll_Ribbon_{key}", vertices, uvs, triangles);
        }

        private static Transform[] CreateOverlayAnchors(Transform parent, float width, float height)
        {
            Vector3[] positions =
            {
                new(-width * .27f, .045f, -height * .34f),
                new( width * .43f, .045f, -height * .34f),
                new( width * .43f, .045f,  height * .15f),
                new(-width * .27f, .045f,  height * .15f)
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
                    material.mainTextureScale = new Vector2(1.25f, .9f);
                }
            }
            return material;
        }

        private static float EdgeInset(int key, float u, bool top)
        {
            float phase = top ? key * .73f : key * 1.11f + 1.4f;
            float wave = .014f + .014f * Mathf.Abs(Mathf.Sin(u * Mathf.PI * (3f + key * .35f) + phase));
            float tear = key switch
            {
                1 => .025f * Mathf.Exp(-Mathf.Pow((u - .84f) / .08f, 2f)),
                2 => .020f * Mathf.Exp(-Mathf.Pow((u - .22f) / .11f, 2f)),
                3 => .030f * Mathf.Exp(-Mathf.Pow((u - .68f) / .10f, 2f)),
                4 => .028f * (Mathf.Exp(-Mathf.Pow((u - .08f) / .08f, 2f)) + Mathf.Exp(-Mathf.Pow((u - .93f) / .07f, 2f))),
                _ => 0f
            };
            return wave + tear;
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
