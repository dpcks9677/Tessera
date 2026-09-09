using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 증강 56 `dice-alchemy` 연기 파티클에 쓸 스프라이트를 굽는다(M17-T9-1-1).
///
/// 색은 파티클 `startColor`가 입히므로 RGB는 순백으로 고정하고, 알파만 중심에서 가장자리로
/// 4단 계단(255/192/128/64/0)으로 떨어뜨린다. 픽셀 필터에서 디더링으로 깨지지 않도록
/// 계단 사이를 보간하지 않는다.
///
/// 자동 호출하지 않는다. 메뉴로 한 번 구워 결과 PNG를 커밋한다.
/// </summary>
public static class DiceSmokeSpriteBaker
{
    private const int Size = 32;
    private const string OutputPath = "Assets/Resources/Vfx/DiceSmokePuff.png";

    [MenuItem("Tessera/Bake/Dice Smoke Puff Sprite")]
    public static void Bake()
    {
        Directory.CreateDirectory("Assets/Resources/Vfx");

        Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[Size * Size];

        float center = Size * 0.5f;
        float radius = Size * 0.5f;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float dx = (x + 0.5f) - center;
                float dy = (y + 0.5f) - center;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / radius;

                byte alpha;
                if (d < 0.35f) alpha = 255;
                else if (d < 0.60f) alpha = 192;
                else if (d < 0.80f) alpha = 128;
                else if (d < 0.95f) alpha = 64;
                else alpha = 0;

                pixels[y * Size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(OutputPath, png);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(OutputPath);
        TextureImporter importer = AssetImporter.GetAtPath(OutputPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = Size;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        Debug.Log($"[DiceSmokeSpriteBaker] 연기 퍼프 스프라이트를 구웠습니다: {OutputPath}");
    }
}
