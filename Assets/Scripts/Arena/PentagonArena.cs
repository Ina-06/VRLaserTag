
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class PentagonArena : MonoBehaviour
{
    [Header("Arena Dimensions")]
    public float radius = 18f;
    public float wallHeight = 4f;
    public float wallThickness = 0.5f;

    [Header("Texture Settings")]
    public float textureTileSize = 3f;

    [ContextMenu("Generate Arena")]
    public void BuildArena()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            Debug.LogWarning(
                "Generate the arena outside Play mode."
            );
            return;
        }

        GenerateArena();
#else
        Debug.LogWarning(
            "Arena generation is available in the Unity Editor."
        );
#endif
    }

#if UNITY_EDITOR
    private void GenerateArena()
    {
        // Remove only previously generated walls.
        // Do not delete manually added children.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (child.name.StartsWith("Wall_"))
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // Generate five pentagon corners.
        Vector3[] vertices = new Vector3[6];

        vertices[0] = Vector3.zero;

        for (int i = 0; i < 5; i++)
        {
            float angle = Mathf.Deg2Rad * (90f + i * 72f);

            vertices[i + 1] = new Vector3(
                radius * Mathf.Cos(angle),
                0f,
                radius * Mathf.Sin(angle)
            );
        }

        // Generate floor triangles.
        int[] triangles = new int[15];

        for (int i = 0; i < 5; i++)
        {
            int index = i * 3;

            triangles[index] = 0;
            triangles[index + 1] = ((i + 1) % 5) + 1;
            triangles[index + 2] = i + 1;
        }

        // Generate UV coordinates for stone texture.
        Vector2[] uvs = new Vector2[vertices.Length];

        float tileSize = Mathf.Max(0.1f, textureTileSize);

        for (int i = 0; i < vertices.Length; i++)
        {
            uvs[i] = new Vector2(
                vertices[i].x / tileSize,
                vertices[i].z / tileSize
            );
        }

        // Create the temporary floor mesh.
        Mesh mesh = new Mesh();
        mesh.name = "PentagonFloor";

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Save the mesh as a permanent Unity asset.
        string folder = "Assets/GeneratedArena";

        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder(
                "Assets",
                "GeneratedArena"
            );
        }

        string path = folder + "/PentagonFloor.asset";

        Mesh savedMesh =
            AssetDatabase.LoadAssetAtPath<Mesh>(path);

        if (savedMesh != null)
        {
            // Clear previous vertex and UV arrays.
            savedMesh.Clear();

            // Assign arrays in the correct order.
            savedMesh.vertices = mesh.vertices;
            savedMesh.uv = mesh.uv;
            savedMesh.triangles = mesh.triangles;

            savedMesh.RecalculateNormals();
            savedMesh.RecalculateBounds();

            DestroyImmediate(mesh);

            mesh = savedMesh;

            EditorUtility.SetDirty(mesh);
        }
        else
        {
            AssetDatabase.CreateAsset(mesh, path);
        }

        // Assign the saved mesh to the floor.
        MeshFilter meshFilter =
            GetComponent<MeshFilter>();

        MeshCollider meshCollider =
            GetComponent<MeshCollider>();

        meshFilter.sharedMesh = mesh;

        // Refresh collision geometry.
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;

        // Generate the five boundary walls.
        for (int i = 0; i < 5; i++)
        {
            Vector3 start = vertices[i + 1];

            Vector3 end =
                vertices[((i + 1) % 5) + 1];

            Vector3 midpoint = (start + end) / 2f;

            float length =
                Vector3.Distance(start, end);

            GameObject wall =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            wall.name = "Wall_" + (i + 1);

            wall.transform.SetParent(
                transform,
                false
            );

            wall.transform.localPosition =
                midpoint +
                Vector3.up * wallHeight / 2f;

            wall.transform.localRotation =
                Quaternion.LookRotation(end - start);

            wall.transform.localScale = new Vector3(
                wallThickness,
                wallHeight,
                length
            );
        }

        EditorUtility.SetDirty(meshFilter);
        EditorUtility.SetDirty(meshCollider);

        AssetDatabase.SaveAssets();

        Debug.Log(
            "Pentagonal arena generated successfully."
        );
    }
#endif
}
