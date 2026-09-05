using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tessera.Dice
{
    public enum DieType
    {
        Normal,      // 클래식 화이트 (흰 바탕, 검은 눈)
        HeavyRed,    // 크림슨 레드 (빨간 바탕, 흰 눈)
        Golden,      // 앤틱 골드 (골드 바탕, 다크 브라운 눈)
        Metal,       // 티타늄 실버 (실버 바탕, 미드나잇 블루 눈)
        Sevens,      // 에메랄드 시안 (민트 바탕, 딥 틸 눈)
        Couple,      // 로즈 핑크 (핑크 바탕, 딥 버건디 눈)
        Promotion,   // 다크 슬레이트 (흑요석 바탕, 엠버 골드 눈)
        Weird,       // 아케인 퍼플 (보라 바탕, 민트 라임 눈)
        // 아래로만 추가한다. 씬이 selectedDieType을 정수로 직렬화하므로 중간에 끼우면 기존 값이 밀린다.
        Octahedron   // 미드나잇 네이비 8면 (남색 바탕, 흰 눈)
    }

    public struct DiePaletteDefinition
    {
        public string DisplayName;
        public Color BodyColor;
        public Color PipColor;
        public float Metallic;
        public float Smoothness;

        public DiePaletteDefinition(string displayName, Color bodyColor, Color pipColor, float metallic, float smoothness)
        {
            DisplayName = displayName;
            BodyColor = bodyColor;
            PipColor = pipColor;
            Metallic = metallic;
            Smoothness = smoothness;
        }
    }

    public static class DicePaletteCatalog
    {
        private static readonly Dictionary<DieType, DiePaletteDefinition> Definitions = new()
        {
            // 1. 일반 주사위: 클래식 화이트 바탕 + 다크 차콜 눈
            {
                DieType.Normal,
                new DiePaletteDefinition(
                    "Normal White",
                    new Color(0.96f, 0.96f, 0.94f),
                    new Color(0.08f, 0.08f, 0.08f),
                    0.0f,
                    0.45f)
            },
            // 2. 헤비/빨간 주사위: 크림슨 레드 바탕 + 퓨어 화이트 눈
            {
                DieType.HeavyRed,
                new DiePaletteDefinition(
                    "Heavy Red",
                    new Color(0.74f, 0.05f, 0.04f),
                    new Color(0.98f, 0.98f, 0.98f),
                    0.0f,
                    0.50f)
            },
            // 3. 황금 주사위: 앤틱 골드 바탕 + 에스프레소 브라운 눈
            {
                DieType.Golden,
                new DiePaletteDefinition(
                    "Golden",
                    new Color(0.88f, 0.70f, 0.22f),
                    new Color(0.16f, 0.10f, 0.04f),
                    0.65f,
                    0.80f)
            },
            // 4. 메탈 주사위: 티타늄 실버 바탕 + 미드나잇 블루 눈
            {
                DieType.Metal,
                new DiePaletteDefinition(
                    "Metal Steel",
                    new Color(0.70f, 0.73f, 0.76f),
                    new Color(0.06f, 0.09f, 0.16f),
                    0.80f,
                    0.75f)
            },
            // 5. 세븐스 주사위: 에메랄드 시안 바탕 + 딥 틸 눈
            {
                DieType.Sevens,
                new DiePaletteDefinition(
                    "Sevens Lucky",
                    new Color(0.18f, 0.83f, 0.75f),
                    new Color(0.02f, 0.18f, 0.18f),
                    0.0f,
                    0.60f)
            },
            // 6. 커플 주사위: 로즈 핑크 바탕 + 딥 버건디 눈
            {
                DieType.Couple,
                new DiePaletteDefinition(
                    "Couple Valentine",
                    new Color(0.96f, 0.25f, 0.40f),
                    new Color(0.30f, 0.02f, 0.10f),
                    0.0f,
                    0.55f)
            },
            // 7. 승급 주사위: 다크 슬레이트 바탕 + 엠버 골드 눈
            {
                DieType.Promotion,
                new DiePaletteDefinition(
                    "Promotion Dark",
                    new Color(0.20f, 0.25f, 0.33f),
                    new Color(0.98f, 0.65f, 0.08f),
                    0.20f,
                    0.65f)
            },
            // 8. 기묘한 주사위: 아케인 퍼플 바탕 + 민트 라임 눈
            {
                DieType.Weird,
                new DiePaletteDefinition(
                    "Weird Arcane",
                    new Color(0.48f, 0.18f, 0.78f),
                    new Color(0.65f, 0.95f, 0.80f),
                    0.10f,
                    0.60f)
            },
            // 9. 8면 주사위: 미드나잇 네이비 바탕 + 퓨어 화이트 눈 (원본 diceMaterials.js:16 #002F5E / #ffffff)
            //    원본 색을 그대로 쓰면 이 씬의 앰버 조명 아래에서 반사광이 색을 덮어 회백색으로 보인다.
            //    색조는 유지하고 명도만 올려(#0F427A 계열) 남색으로 읽히게 했다.
            //    숫자는 순백 대신 옅은 하늘색으로 새긴다. 순백 자발광 획은 640x360 렌더에서 인접 픽셀까지
            //    흰색으로 채워 8면체를 통째로 흰 덩어리로 만들었다.
            {
                DieType.Octahedron,
                new DiePaletteDefinition(
                    "Octahedron Navy",
                    new Color(0.06f, 0.26f, 0.48f),
                    new Color(0.92f, 0.94f, 0.98f),
                    0.0f,
                    0.18f)
            }
        };

        private static readonly Dictionary<DieType, Material> BodyMaterialCache = new();
        private static readonly Dictionary<DieType, Material> PipMaterialCache = new();

        public static DiePaletteDefinition GetDefinition(DieType type)
        {
            return Definitions.TryGetValue(type, out var def) ? def : Definitions[DieType.Normal];
        }

        public static Material GetBodyMaterial(DieType type)
        {
            if (BodyMaterialCache.TryGetValue(type, out Material mat) && mat != null) return mat;

            DiePaletteDefinition def = GetDefinition(type);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader)
            {
                name = $"Dice_Body_{type}",
                enableInstancing = true
            };

            mat.SetColor("_BaseColor", def.BodyColor);
            mat.SetFloat("_Metallic", def.Metallic);
            mat.SetFloat("_Smoothness", def.Smoothness);
            mat.SetFloat("_SpecularHighlights", 1f);
            mat.SetFloat("_EnvironmentReflections", 1f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", def.BodyColor);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            mat.SetShaderPassEnabled("ShadowCaster", true);

            BodyMaterialCache[type] = mat;
            return mat;
        }

        public static Material GetPipMaterial(DieType type)
        {
            if (PipMaterialCache.TryGetValue(type, out Material mat) && mat != null) return mat;

            DiePaletteDefinition def = GetDefinition(type);

            // 눈은 조명을 받지 않는 평면색으로 그린다.
            //
            // 눈은 음각 홈 메시라 홈의 경사벽이 면과 다른 각도로 빛을 받는다. 조명을 받으면
            // 그 벽이 몸체와 홈 바닥 사이의 중간 밝기가 되는데, 픽셀 격자에서 한 칸을 차지하면
            // 안티앨리어싱처럼 흐릿하게 읽힌다. Unlit으로 두면 벽과 바닥이 같은 색이 되어
            // 몸체와 눈 두 값만 남고 경계가 또렷해진다.
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            mat = new Material(shader)
            {
                name = $"Dice_Pip_{type}",
                enableInstancing = true
            };

            mat.SetColor("_BaseColor", def.PipColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", def.PipColor);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            // Pip 메시는 그림자를 캐스팅하지 않음 (그림자 구멍 차단)
            mat.SetShaderPassEnabled("ShadowCaster", false);

            PipMaterialCache[type] = mat;
            return mat;
        }

        public static void ClearCache()
        {
            foreach (var mat in BodyMaterialCache.Values)
            {
                if (mat != null) Object.DestroyImmediate(mat);
            }
            foreach (var mat in PipMaterialCache.Values)
            {
                if (mat != null) Object.DestroyImmediate(mat);
            }
            BodyMaterialCache.Clear();
            PipMaterialCache.Clear();
        }
    }
}
