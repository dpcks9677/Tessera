using Tessera.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tessera.Games.Yacht.Presentation
{
    /// <summary>
    /// 요트 뱅크 저축 중 킵 칸 슬롯 0 위치에 띄우는 절차 생성 연출입니다. 칸 입구에서 바깥으로 기울어 퍼지는
    /// 뒤집힌 사각뿔대 옆면 글로우와 그 위로 떠오르는 반짝이로 이뤄집니다. 윗면·아랫면 없이 옆면 4장만 그리고,
    /// 글로우 알파는 칸 쪽에서 바깥으로 연속 감소하고, 머티리얼은 전부 가산 블렌딩입니다.
    /// </summary>
    public sealed class YachtBankKeepZoneVfx : MonoBehaviour
    {
        private static readonly Color GoldColor = new(0.898f, 0.663f, 0.235f, 1f);
        private static readonly Color PaleGoldColor = new(1f, 0.945f, 0.769f, 1f);

        private const float PulsePeriodSeconds = 2.7f;
        private const float PulseMin = 0.6f;
        private const float PulseMax = 1.0f;
        private const float SurfaceEpsilon = 0.01f;

        // 트레이 메시 실측: 소켓 앞쪽(카메라 쪽) 턱 윗면이 소켓 바닥보다 3 소스 단위 높다.
        private const float FrontLipHeight = 3f * DiceBoardMetrics.TrayScale;
        private const float BorderY = DiceBoardMetrics.KeepSurfaceY - DiceBoardMetrics.KeptDieHalfSize + FrontLipHeight + SurfaceEpsilon;

        // 소켓 바닥 위 0.005 에 깔리는 바닥면의 root 기준 로컬 Y. 소켓 바닥 = 킵 주사위 중심 높이 - 반 크기.
        private const float FloorLift = 0.005f;
        private const float FloorLocalY = FloorLift - FrontLipHeight - SurfaceEpsilon;
        private const float FloorSizeRatio = 0.96f; // 벽과 z-파이팅이 나지 않게 소켓 한 변보다 살짝 안쪽
        private const float FloorAlpha = 0.25f;

        // 옆면이 수평과 이루는 각도. Play Mode 조정 대상.
        private const float FrustumSlopeDegrees = 50f;
        private const float FrustumSlantRatio = 0.4f; // 빗변 길이 = 소켓 한 변 × 이 값

        // 주사위 카메라는 직교 투영이라 시선 축으로 옮겨도 화면 위치가 변하지 않는다.
        // 당기지 않으면 뒤쪽 면 밑동이 뒤쪽 벽(2.017)·칸막이(2.062) 안에 묻혀 깊이 테스트로 잘린다(필요 d > 약 0.27).
        // 너무 당기면 킵 주사위 윗면(2.557)보다 앞으로 나와 주사위를 덮는다(d < 약 0.6). Play Mode 조정 대상.
        private const float FrustumCameraPull = 0.4f;

        private const int FrustumTextureWidth = 4;
        private const int FrustumTextureHeight = 32;

        // 알파는 (1 - v)^지수로 연속 감소한다. 1 보다 크면 선형보다 바깥에서 빨리 사라진다.
        private const float FrustumAlphaFalloffPower = 1.5f;
        // v 가 이 값에 이를 때까지 옅은 금색에서 금색으로 보간하고, 그 바깥은 금색이다.
        private const float FrustumPaleFadeEnd = 0.3f;

        private bool visible;
        private GameObject root;
        private Transform frustum;
        private Camera diceCamera;
        private Material frustumMaterial;
        private Material sparkleMaterial;
        private Texture2D frustumTexture;
        private Mesh frustumMesh;
        private Material floorMaterial;
        private Texture2D floorTexture;
        private MeshRenderer floorRenderer;
        private MeshRenderer frustumRenderer;
        private MaterialPropertyBlock propertyBlock;
        private ParticleSystem sparkle;
        private float pulseTimer;

        /// <param name="centerX">주사위 루트의 월드 X 오프셋. 킵 좌표는 이 루트 기준이라 더해 준다.</param>
        public void SetVisible(bool visible, float centerX = 0f)
        {
            if (this.visible == visible) return;
            if (visible && root == null) CreateRoot();

            this.visible = visible;
            if (root == null) return;

            if (visible)
            {
                // Z 는 시차 보정(KeepParallaxZ) 없이 소켓 지오메트리 중심(KeepCenterZ)을 쓴다.
                Vector3 keep = DiceBoardMetrics.GetKeepPosition(0);
                root.transform.position = new Vector3(centerX + keep.x, BorderY, DiceBoardMetrics.KeepCenterZ);
                pulseTimer = 0f;
                ApplyPulse();
                AlignFrustum();
                root.SetActive(true);
                sparkle.Play(true);
            }
            else
            {
                sparkle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                root.SetActive(false);
            }
        }

        private void Update()
        {
            if (!visible || frustumMaterial == null) return;

            pulseTimer = (pulseTimer + Time.unscaledDeltaTime) % PulsePeriodSeconds;
            ApplyPulse();
        }

        private void LateUpdate()
        {
            if (!visible || frustum == null) return;
            AlignFrustum();
        }

        private void OnDestroy()
        {
            if (root != null) Destroy(root);
            if (frustumMaterial != null) Destroy(frustumMaterial);
            if (sparkleMaterial != null) Destroy(sparkleMaterial);
            if (frustumTexture != null) Destroy(frustumTexture);
            if (frustumMesh != null) Destroy(frustumMesh);
            if (floorMaterial != null) Destroy(floorMaterial);
            if (floorTexture != null) Destroy(floorTexture);
        }

        /// <summary>글로우 오브젝트를 루트 위치에서 시선 반대로 당겨 놓는다.</summary>
        private void AlignFrustum()
        {
            if (diceCamera == null) diceCamera = ResolveDiceCamera();
            if (diceCamera == null) return;

            frustum.position = root.transform.position - diceCamera.transform.forward * FrustumCameraPull;
        }

        private void ApplyPulse()
        {
            // 코사인 곡선으로 PulseMin 에서 PulseMax 까지 연속으로 오간다. 표시 시작 시점(t=0)이 최소다.
            float k = PulseMin + (PulseMax - PulseMin) * (0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * pulseTimer / PulsePeriodSeconds));
            Color tint = new(k, k, k, 1f);
            ApplyTint(frustumRenderer, frustumMaterial, tint);
            ApplyTint(floorRenderer, floorMaterial, tint);
        }

        private void ApplyTint(MeshRenderer renderer, Material material, Color tint)
        {
            propertyBlock ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            if (material.HasProperty("_BaseColor")) propertyBlock.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color")) propertyBlock.SetColor("_Color", tint);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private void CreateRoot()
        {
            float size = 17f * DiceBoardMetrics.TrayScale;
            root = new GameObject("YachtBank_KeepZoneHighlight") { layer = TesseraLayers.Dice };

            Shader unlit = FindShader("Universal Render Pipeline/Unlit", "Particles/Standard Unlit", "Sprites/Default");
            frustumTexture = CreateFrustumTexture();
            frustumMaterial = CreateAdditiveMaterial(unlit, "YachtBank_Frustum_Mat", frustumTexture);
            frustumMesh = CreateFrustumMesh(size);

            GameObject frustumObject = new("Frustum") { layer = TesseraLayers.Dice };
            frustumObject.transform.SetParent(root.transform, false);
            frustum = frustumObject.transform;
            frustumObject.AddComponent<MeshFilter>().sharedMesh = frustumMesh;
            frustumRenderer = frustumObject.AddComponent<MeshRenderer>();
            frustumRenderer.sharedMaterial = frustumMaterial;
            frustumRenderer.shadowCastingMode = ShadowCastingMode.Off;
            frustumRenderer.receiveShadows = false;

            // 바닥면은 카메라 당김을 받지 않아 킵된 주사위가 깊이 테스트로 자연스럽게 가린다.
            floorTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "YachtBank_Floor_Tex",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            floorTexture.SetPixels32(new[] { ToColor32(GoldColor, FloorAlpha) });
            floorTexture.Apply(false);
            floorMaterial = CreateAdditiveMaterial(unlit, "YachtBank_Floor_Mat", floorTexture);

            GameObject floorObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            floorObject.name = "Floor";
            // 주사위 물리에 간섭하지 않도록 기본 콜라이더를 제거한다.
            Destroy(floorObject.GetComponent<MeshCollider>());
            floorObject.layer = TesseraLayers.Dice;
            floorObject.transform.SetParent(root.transform, false);
            floorObject.transform.localPosition = new Vector3(0f, FloorLocalY, 0f);
            floorObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            floorObject.transform.localScale = new Vector3(size * FloorSizeRatio, size * FloorSizeRatio, 1f);
            floorRenderer = floorObject.GetComponent<MeshRenderer>();
            floorRenderer.sharedMaterial = floorMaterial;
            floorRenderer.shadowCastingMode = ShadowCastingMode.Off;
            floorRenderer.receiveShadows = false;

            CreateSparkle(size);
            ApplyPulse();
            root.SetActive(false);
        }

        /// <summary>
        /// 아래 테두리(한 변 size)에서 바깥으로 기울어 올라가는 옆면 4장. 면마다 정점을 따로 둬 UV 가 독립이다.
        /// 감기 순서는 안쪽(위에서 내려다보는 쪽)이 앞면이 되도록 잡는다.
        /// </summary>
        private static Mesh CreateFrustumMesh(float size)
        {
            float slant = size * FrustumSlantRatio;
            float run = slant * Mathf.Cos(FrustumSlopeDegrees * Mathf.Deg2Rad);
            float rise = slant * Mathf.Sin(FrustumSlopeDegrees * Mathf.Deg2Rad);
            float bottomHalf = size * 0.5f;
            float topHalf = bottomHalf + run;

            Vector3[] vertices = new Vector3[16];
            Vector2[] uvs = new Vector2[16];
            int[] triangles = new int[24];
            for (int i = 0; i < 4; i++)
            {
                // +Z 쪽 면을 90도씩 돌려 만든다. 안쪽에서 볼 때 왼쪽이 -X.
                Quaternion turn = Quaternion.Euler(0f, 90f * i, 0f);
                int v = i * 4;
                vertices[v] = turn * new Vector3(-bottomHalf, 0f, bottomHalf);
                vertices[v + 1] = turn * new Vector3(bottomHalf, 0f, bottomHalf);
                vertices[v + 2] = turn * new Vector3(-topHalf, rise, topHalf);
                vertices[v + 3] = turn * new Vector3(topHalf, rise, topHalf);
                uvs[v] = new Vector2(0f, 0f);
                uvs[v + 1] = new Vector2(1f, 0f);
                uvs[v + 2] = new Vector2(0f, 1f);
                uvs[v + 3] = new Vector2(1f, 1f);

                int t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 2;
                triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
            }

            Mesh mesh = new() { name = "YachtBank_Frustum_Mesh", vertices = vertices, uv = uvs, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void CreateSparkle(float size)
        {
            Shader particleShader = FindShader("Universal Render Pipeline/Particles/Unlit", "Particles/Standard Unlit", "Sprites/Default");
            sparkleMaterial = CreateAdditiveMaterial(particleShader, "YachtBank_Sparkle_Mat", null);

            GameObject particleObject = new("YachtBank_Sparkle") { layer = TesseraLayers.Dice };
            // 글로우와 같은 당김을 받아 벽·칸막이에 잘리지 않도록 Frustum 의 자식으로 둔다.
            particleObject.transform.SetParent(frustum, false);
            particleObject.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            particleObject.transform.localRotation = Quaternion.identity;

            sparkle = particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = sparkle.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = 20;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.6f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.10f, size * 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(PaleGoldColor, Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            ParticleSystem.EmissionModule emission = sparkle.emission;
            emission.rateOverTime = 6f;

            ParticleSystem.ShapeModule shape = sparkle.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(size, 0.05f, size);

            // 세 축 모두 TwoConstants 로 통일한다. 모드가 섞이면 "Particle Velocity curves must all be in the same mode" 에러가 난다.
            ParticleSystem.VelocityOverLifetimeModule vel = sparkle.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            vel.y = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

            // 상수 탄젠트(무한대)로 계단식 반짝임을 만든다.
            float step = float.PositiveInfinity;
            AnimationCurve twinkle = new(
                new Keyframe(0f, 0f, step, step),
                new Keyframe(0.25f, 1f, step, step),
                new Keyframe(0.5f, 0.4f, step, step),
                new Keyframe(0.75f, 1f, step, step),
                new Keyframe(1f, 0f, step, step));
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = sparkle.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, twinkle);

            Gradient fade = new() { mode = GradientMode.Fixed };
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f) },
                new[]
                {
                    new GradientAlphaKey(1f, 0.2f),
                    new GradientAlphaKey(0.75f, 0.4f),
                    new GradientAlphaKey(0.5f, 0.6f),
                    new GradientAlphaKey(0.25f, 0.8f),
                    new GradientAlphaKey(0f, 1f),
                });
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = sparkle.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

            ParticleSystemRenderer psRenderer = particleObject.GetComponent<ParticleSystemRenderer>();
            psRenderer.sharedMaterial = sparkleMaterial;
            psRenderer.shadowCastingMode = ShadowCastingMode.Off;
            psRenderer.receiveShadows = false;
        }

        /// <summary>v(아래 테두리 0 ~ 위 테두리 1) 방향 연속 감쇠 알파 텍스처. u 방향은 균일하다.</summary>
        private static Texture2D CreateFrustumTexture()
        {
            Texture2D texture = new(FrustumTextureWidth, FrustumTextureHeight, TextureFormat.RGBA32, false)
            {
                name = "YachtBank_Frustum_Tex",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            Color32[] pixels = new Color32[FrustumTextureWidth * FrustumTextureHeight];
            for (int y = 0; y < FrustumTextureHeight; y++)
            {
                // 끝 텍셀이 v=1 이라 알파가 정확히 0 이 되어 Bilinear 가 바깥 테두리에 선을 남기지 않는다.
                float v = (float)y / (FrustumTextureHeight - 1);
                float a = Mathf.Pow(1f - v, FrustumAlphaFalloffPower);
                Color rgb = Color.Lerp(PaleGoldColor, GoldColor, Mathf.Clamp01(v / FrustumPaleFadeEnd));
                for (int x = 0; x < FrustumTextureWidth; x++)
                {
                    pixels[y * FrustumTextureWidth + x] = ToColor32(rgb, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false);
            return texture;
        }

        private static Color32 ToColor32(Color rgb, float alpha)
        {
            return new Color32((byte)(rgb.r * 255f + 0.5f), (byte)(rgb.g * 255f + 0.5f), (byte)(rgb.b * 255f + 0.5f), (byte)(alpha * 255f + 0.5f));
        }

        private static Shader FindShader(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Shader shader = Shader.Find(names[i]);
                if (shader != null) return shader;
            }

            return null;
        }

        /// <summary>투명 가산(SrcAlpha, One), ZWrite 끔, 컬링 끔, 투명 큐 머티리얼. 속성은 머티리얼이 가진 것만 설정한다.</summary>
        private static Material CreateAdditiveMaterial(Shader shader, string materialName, Texture texture)
        {
            Material material = new(shader) { name = materialName };
            if (texture != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.One);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        /// <summary>
        /// 주사위를 그리는 카메라를 찾는다. 태그가 아니라 컬링 마스크로 고르는 이유는, 이 씬이
        /// 월드·프레젠테이션·Crisp UI 카메라를 함께 쓰고 그중 주사위 레이어를 보는 것만이 글로우의
        /// 기준이기 때문이다. DiceSmokePuffVfx.ResolveDiceCamera 와 같은 로직이다.
        /// </summary>
        private static Camera ResolveDiceCamera()
        {
            int diceMask = TesseraLayers.Mask(TesseraLayers.Dice);
            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && (cameras[i].cullingMask & diceMask) != 0) return cameras[i];
            }

            return Camera.main;
        }
    }
}
