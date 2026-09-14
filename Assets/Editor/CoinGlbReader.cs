using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// coin.glb 하나를 굽는 데 필요한 만큼만 읽는 최소 GLB 파서다(M17-T23-2).
///
/// GLTF 2.0 전체를 지원하지 않는다. 이 모델이 실제로 쓰는 어트리뷰트(POSITION, NORMAL, TEXCOORD_0)와
/// 접근자 타입(float VEC3/VEC2, ushort/uint SCALAR)만 읽고, 그 외는 <see cref="NotSupportedException"/>을 던진다.
/// 좌표계 변환(우수 → 좌수, UV V축 반전, 와인딩 반전)도 여기서 한 번에 처리해 이후 코드는 Unity 좌표만 다룬다.
/// </summary>
public sealed class CoinGlbPrimitive
{
    public string MaterialName;
    public Vector3[] Positions;
    public Vector3[] Normals;
    public Vector2[] Uvs;
    public int[] Indices;
}

public static class CoinGlbReader
{
    private const uint Magic = 0x46546C67; // "glTF"
    private const uint SupportedVersion = 2;
    private const uint JsonChunkType = 0x4E4F534A; // "JSON"
    private const uint BinChunkType = 0x004E4942; // "BIN"

    public static CoinGlbPrimitive[] Read(byte[] glb)
    {
        using var stream = new MemoryStream(glb);
        using var reader = new BinaryReader(stream);

        uint magic = reader.ReadUInt32();
        uint version = reader.ReadUInt32();
        uint length = reader.ReadUInt32();
        if (magic != Magic || version != SupportedVersion || length != glb.Length)
            throw new InvalidDataException("coin.glb: GLB 2.0 헤더가 아닙니다.");

        uint jsonChunkLength = reader.ReadUInt32();
        uint jsonChunkType = reader.ReadUInt32();
        if (jsonChunkType != JsonChunkType)
            throw new InvalidDataException("coin.glb: 첫 청크가 JSON이 아닙니다.");
        JObject root = JObject.Parse(Encoding.UTF8.GetString(reader.ReadBytes((int)jsonChunkLength)));

        uint binChunkLength = reader.ReadUInt32();
        uint binChunkType = reader.ReadUInt32();
        if (binChunkType != BinChunkType)
            throw new InvalidDataException("coin.glb: 두 번째 청크가 BIN이 아닙니다.");
        byte[] bin = reader.ReadBytes((int)binChunkLength);

        var buffers = new byte[][] { bin };
        var bufferViews = (JArray)root["bufferViews"];
        var accessors = (JArray)root["accessors"];
        var materials = root["materials"] as JArray;
        var meshes = (JArray)root["meshes"];

        var mesh = (JObject)meshes[0];
        var primitivesJson = (JArray)mesh["primitives"];
        var result = new CoinGlbPrimitive[primitivesJson.Count];
        for (int i = 0; i < primitivesJson.Count; i++)
        {
            var primitiveJson = (JObject)primitivesJson[i];
            var attributes = (JObject)primitiveJson["attributes"];

            Vector3[] positions = ReadVector3(buffers, bufferViews, accessors, (int)attributes["POSITION"]);
            Vector3[] normals = ReadVector3(buffers, bufferViews, accessors, (int)attributes["NORMAL"]);
            Vector2[] uvs = ReadVector2(buffers, bufferViews, accessors, (int)attributes["TEXCOORD_0"]);
            int[] indices = ReadIndices(buffers, bufferViews, accessors, (int)primitiveJson["indices"]);

            // Unity 좌표계 변환: 우수 좌표계(glTF) → 좌수 좌표계(Unity)는 X 부호 반전.
            for (int v = 0; v < positions.Length; v++)
            {
                positions[v].x = -positions[v].x;
                normals[v].x = -normals[v].x;
                uvs[v].y = 1f - uvs[v].y;
            }

            // 와인딩 반전: X 부호 반전으로 삼각형이 뒤집히므로 인덱스 순서를 맞춰준다.
            for (int t = 0; t < indices.Length; t += 3)
                (indices[t + 1], indices[t + 2]) = (indices[t + 2], indices[t + 1]);

            string materialName = null;
            if (primitiveJson["material"] != null && materials != null)
                materialName = (string)materials[(int)primitiveJson["material"]]["name"];

            result[i] = new CoinGlbPrimitive
            {
                MaterialName = materialName,
                Positions = positions,
                Normals = normals,
                Uvs = uvs,
                Indices = indices
            };
        }

        return result;
    }

    private static Vector3[] ReadVector3(byte[][] buffers, JArray bufferViews, JArray accessors, int accessorIndex)
    {
        var accessor = (JObject)accessors[accessorIndex];
        if ((string)accessor["type"] != "VEC3" || (int)accessor["componentType"] != 5126)
            throw new NotSupportedException("coin.glb: VEC3 float 접근자만 지원합니다.");

        int count = (int)accessor["count"];
        var result = new Vector3[count];
        (byte[] buffer, int stride, int offset) = ResolveAccessor(buffers, bufferViews, accessor, elementSize: 12);
        for (int i = 0; i < count; i++)
        {
            int baseOffset = offset + i * stride;
            result[i] = new Vector3(
                BitConverter.ToSingle(buffer, baseOffset),
                BitConverter.ToSingle(buffer, baseOffset + 4),
                BitConverter.ToSingle(buffer, baseOffset + 8));
        }
        return result;
    }

    private static Vector2[] ReadVector2(byte[][] buffers, JArray bufferViews, JArray accessors, int accessorIndex)
    {
        var accessor = (JObject)accessors[accessorIndex];
        if ((string)accessor["type"] != "VEC2" || (int)accessor["componentType"] != 5126)
            throw new NotSupportedException("coin.glb: VEC2 float 접근자만 지원합니다.");

        int count = (int)accessor["count"];
        var result = new Vector2[count];
        (byte[] buffer, int stride, int offset) = ResolveAccessor(buffers, bufferViews, accessor, elementSize: 8);
        for (int i = 0; i < count; i++)
        {
            int baseOffset = offset + i * stride;
            result[i] = new Vector2(
                BitConverter.ToSingle(buffer, baseOffset),
                BitConverter.ToSingle(buffer, baseOffset + 4));
        }
        return result;
    }

    private static int[] ReadIndices(byte[][] buffers, JArray bufferViews, JArray accessors, int accessorIndex)
    {
        var accessor = (JObject)accessors[accessorIndex];
        if ((string)accessor["type"] != "SCALAR")
            throw new NotSupportedException("coin.glb: SCALAR 접근자만 인덱스로 지원합니다.");

        int componentType = (int)accessor["componentType"];
        int elementSize = componentType switch
        {
            5123 => 2, // ushort
            5125 => 4, // uint
            _ => throw new NotSupportedException("coin.glb: ushort/uint 인덱스만 지원합니다.")
        };

        int count = (int)accessor["count"];
        var result = new int[count];
        (byte[] buffer, int stride, int offset) = ResolveAccessor(buffers, bufferViews, accessor, elementSize);
        for (int i = 0; i < count; i++)
        {
            int baseOffset = offset + i * stride;
            result[i] = componentType == 5123
                ? BitConverter.ToUInt16(buffer, baseOffset)
                : (int)BitConverter.ToUInt32(buffer, baseOffset);
        }
        return result;
    }

    /// <summary>접근자가 가리키는 버퍼, 엘리먼트 간 stride(byteStride 없으면 타이트 패킹), 시작 오프셋을 계산한다.</summary>
    private static (byte[] buffer, int stride, int offset) ResolveAccessor(
        byte[][] buffers, JArray bufferViews, JObject accessor, int elementSize)
    {
        int bufferViewIndex = (int)accessor["bufferView"];
        var bufferView = (JObject)bufferViews[bufferViewIndex];
        int bufferIndex = (int)bufferView["buffer"];
        int bufferViewOffset = bufferView["byteOffset"]?.Value<int>() ?? 0;
        int accessorOffset = accessor["byteOffset"]?.Value<int>() ?? 0;
        int stride = bufferView["byteStride"]?.Value<int>() ?? elementSize;
        return (buffers[bufferIndex], stride, bufferViewOffset + accessorOffset);
    }
}
