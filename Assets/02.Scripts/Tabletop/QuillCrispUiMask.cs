using UnityEngine;
using UnityEngine.Rendering;
using Tessera.Core;

namespace Tessera.Tabletop
{
    /// <summary>
    /// 깃펜이 족보 표 글자를 가리게 만든다.
    ///
    /// 점수표의 글자와 칸은 픽셀 필터를 피하려고 <c>CrispUI</c> 레이어의 월드 스페이스 캔버스에
    /// 있고, 그 레이어는 월드 카메라의 컬링 마스크에서 빠져 있다(M9.5). 그래서 Crisp 카메라의
    /// 깊이 버퍼에는 월드 물체가 하나도 없고, 깃펜이 종이 위에 떠 있어도 글자가 그대로 비쳐 보인다.
    ///
    /// 깃펜 파츠마다 형상이 같은 판을 <c>CrispUI</c> 레이어에 하나 더 두고, 색은 한 픽셀도 쓰지
    /// 않은 채 깊이만 남긴다. 월드 스페이스 캔버스는 <c>ZTest LEqual</c>로 그려지므로 그 부분에서
    /// 글자가 잘린다. 화면에 남는 것은 월드 카메라가 그린 깃펜이라 픽셀 필터도 그대로 받는다.
    /// 주사위 눈이 트레이 벽에 가려지게 만드는 <c>DiceVisualPool</c>의 방식과 같다.
    ///
    /// 편집 중에 씬을 더럽히지 않도록 <c>ExecuteAlways</c>를 붙이지 않고 판도 저장하지 않는다.
    /// </summary>
    [RequireComponent(typeof(InkwellAndQuill))]
    public sealed class QuillCrispUiMask : MonoBehaviour
    {
        private const string ShaderName = "DicePoC/CrispUiDepthMask";
        private const string QuillRootName = "Quill Pen Root";
        private const string MaskName = "Crisp Depth Mask";

        private static Material maskMaterial;

        private Transform quillRoot;

        private void OnEnable() => EnsureMask();

        private void LateUpdate()
        {
            // InkwellAndQuill이 기하를 다시 만들면 깃펜이 뿌리째 갈리므로 판도 함께 사라진다.
            if (quillRoot == null) EnsureMask();
        }

        private void EnsureMask()
        {
            quillRoot = transform.Find(QuillRootName);
            if (quillRoot == null) return;

            Material material = EnsureMaskMaterial();
            if (material == null) return;

            foreach (MeshFilter source in quillRoot.GetComponentsInChildren<MeshFilter>())
            {
                if (source.sharedMesh == null) continue;
                if (source.name == MaskName) continue;
                if (source.transform.Find(MaskName) != null) continue;
                AddMaskPart(source, material);
            }

            CopyAlphaCutout(quillRoot, material);
        }

        /// <summary>
        /// 판을 원본 파츠에 붙이고 로컬 변환을 항등으로 둔다. 깃펜 안쪽에 계층이 더 생겨도 자세가
        /// 저절로 따라오고, 파츠마다 위치를 베껴 두었다가 어긋날 일이 없다.
        /// </summary>
        private static void AddMaskPart(MeshFilter source, Material material)
        {
            var part = new GameObject(MaskName, typeof(MeshFilter), typeof(MeshRenderer))
            {
                layer = TesseraLayers.CrispUI,
                hideFlags = HideFlags.DontSave
            };
            part.transform.SetParent(source.transform, false);
            part.GetComponent<MeshFilter>().sharedMesh = source.sharedMesh;

            MeshRenderer renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        /// <summary>
        /// 깃펜 파츠의 알파 컷아웃 설정을 판에 옮긴다. 깃털은 실루엣을 알베도 알파로 만들기
        /// 때문에, 판이 같은 알파로 잘리지 않으면 뚫린 자리에서도 깊이를 남겨 화면에 보이지
        /// 않는 영역까지 족보 글자를 지운다.
        ///
        /// 깃펜 파츠가 머티리얼 하나를 공유하므로 먼저 찾은 것 하나만 본다.
        /// </summary>
        private static void CopyAlphaCutout(Transform quillRoot, Material material)
        {
            foreach (MeshRenderer renderer in quillRoot.GetComponentsInChildren<MeshRenderer>())
            {
                Material source = renderer.sharedMaterial;
                if (source == null || source == material) continue;
                if (!source.HasProperty("_BaseMap")) continue;

                material.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
                material.SetFloat("_Cutoff", source.HasProperty("_Cutoff") ? source.GetFloat("_Cutoff") : 0.5f);
                return;
            }
        }

        private static Material EnsureMaskMaterial()
        {
            if (maskMaterial != null) return maskMaterial;

            Shader shader = Shader.Find(ShaderName);
            if (shader == null) return null;

            maskMaterial = new Material(shader)
            {
                name = "Runtime Quill Crisp Depth Mask",
                hideFlags = HideFlags.HideAndDontSave
            };
            return maskMaterial;
        }
    }
}
