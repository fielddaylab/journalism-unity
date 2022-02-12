using UnityEngine;

namespace StreamingAssets {
    static internal class MeshGeneration {
        #region Quad

        static private readonly Vector3[] s_QuadGeneratorVertices = new Vector3[4];
        static private readonly Color32[] s_QuadGeneratorColors = new Color32[4];

        // constants
        static private readonly Vector2[] s_QuadGeneratorUVs = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) };
        static private readonly ushort[] s_QuadGeneratorIndices = new ushort[] { 0, 1, 2, 2, 1, 3 };

        /// <summary>
        /// Generates a quad mesh.
        /// </summary>
        static public Mesh CreateQuad(Vector2 inSize, Vector2 inPivot, Color32 inColor, Rect uv, Mesh ioOverwrite = null) {
            Mesh mesh = ioOverwrite;
            if (mesh == null) {
                mesh = new Mesh();
                mesh.name = "Quad";
            }

            float left = inSize.x * -inPivot.x;
            float bottom = inSize.y * -inPivot.y;
            float right = left + inSize.x;
            float top = bottom + inSize.y;

            s_QuadGeneratorVertices[0] = new Vector3(left, bottom, 0);
            s_QuadGeneratorVertices[1] = new Vector3(left, top, 0);
            s_QuadGeneratorVertices[2] = new Vector3(right, bottom, 0);
            s_QuadGeneratorVertices[3] = new Vector3(right, top, 0);

            s_QuadGeneratorColors[0] = inColor;
            s_QuadGeneratorColors[1] = inColor;
            s_QuadGeneratorColors[2] = inColor;
            s_QuadGeneratorColors[3] = inColor;

            float u0 = uv.xMin, u1 = uv.xMax,
                v0 = uv.yMin, v1 = uv.yMax;

            s_QuadGeneratorUVs[0] = new Vector2(u0, v0);
            s_QuadGeneratorUVs[1] = new Vector2(u0, v1);
            s_QuadGeneratorUVs[2] = new Vector2(u1, v0);
            s_QuadGeneratorUVs[3] = new Vector2(u1, v1);

            mesh.SetVertices(s_QuadGeneratorVertices);
            mesh.SetColors(s_QuadGeneratorColors);
            mesh.SetUVs(0, s_QuadGeneratorUVs);
            mesh.SetIndices(s_QuadGeneratorIndices, MeshTopology.Triangles, 0, false, 0);
            
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);

            return mesh;
        }

        #endregion // Quad
    }
}