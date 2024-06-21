using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public partial class Test_mat_tex : EditorWindow
{
    private GameObject selectedPrefab;
    private Dictionary<Material, List<GameObject>> materialUsage = new Dictionary<Material, List<GameObject>>();
    private Dictionary<Material, HashSet<Texture>> materialTextures = new Dictionary<Material, HashSet<Texture>>();
    private Vector2 scrollPosition;
    private bool isFoldoutMaterials = true;
    private bool isFoldoutAnimator = true;

    [MenuItem("CHISENOTE/Test_window_MatTex")]
    private static void ShowWindow()
    {
        var window = GetWindow<Test_mat_tex>("UIElements");
        window.titleContent = new GUIContent("Cerberus");
        window.Show();
    }

    private void OnGUI()
    {
        selectedPrefab = (GameObject)EditorGUILayout.ObjectField("Select Prefab", selectedPrefab, typeof(GameObject), true);

        if (GUILayout.Button("Check Prefab"))
        {
            materialUsage.Clear();
            materialTextures.Clear();

            if (selectedPrefab != null)
            {
                Debug.Log("Selected object: " + selectedPrefab.name);

                Renderer[] renderers = selectedPrefab.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (!materialUsage.ContainsKey(material))
                        {
                            materialUsage[material] = new List<GameObject>();
                            materialTextures[material] = new HashSet<Texture>();
                        }
                        materialUsage[material].Add(renderer.gameObject);

                        foreach (var property in material.GetTexturePropertyNameIDs())
                        {
                            Texture texture = material.GetTexture(property);
                            if (texture != null)
                            {
                                materialTextures[material].Add(texture);
                            }
                        }
                    }
                }
                Debug.Log("Number of unique materials: " + materialUsage.Count);
            }
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        GUILayout.BeginHorizontal(); // 横並びの始まり

        // Materials and Textures フォルダ
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 4)); // 左側の領域
        isFoldoutMaterials = EditorGUILayout.Foldout(isFoldoutMaterials, "Materials and Textures");
        if (isFoldoutMaterials)
        {
            foreach (var material in materialUsage.Keys)
            {
                EditorGUILayout.ObjectField("Material", material, typeof(Material), false);
                foreach (var gameObject in materialUsage[material])
                {
                    EditorGUILayout.ObjectField("Used by GameObject", gameObject, typeof(GameObject), false);
                }
                foreach (var texture in materialTextures[material])
                {
                    EditorGUILayout.ObjectField("Texture", texture, typeof(Texture), false);
                }
            }
        }
        EditorGUILayout.EndVertical();

        // Animator and Animation フォルダ
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 4)); // 右側の領域
        isFoldoutAnimator = EditorGUILayout.Foldout(isFoldoutAnimator, "Animator and Animation");
        if (isFoldoutAnimator)
        {
            // Animator and Animation 関連の情報の表示
        }
        EditorGUILayout.EndVertical();

        GUILayout.EndHorizontal(); // 横並びの終わり
        EditorGUILayout.EndScrollView();
    }
}
