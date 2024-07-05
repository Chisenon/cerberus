using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using System.IO;

public partial class Cerberus : EditorWindow
{
    private GameObject selectedPrefab;
    private Dictionary<Material, List<GameObject>> materialUsage = new Dictionary<Material, List<GameObject>>();
    private Dictionary<Material, HashSet<Texture>> materialTextures = new Dictionary<Material, HashSet<Texture>>();
    private Vector2 scrollPosition;
    private bool isFoldoutMaterials = true;
    private Dictionary<AnimatorController, List<string>> animatorUsage = new Dictionary<AnimatorController, List<string>>();
    private Dictionary<AnimatorController, Dictionary<string, HashSet<AnimationClip>>> animatorClips = new Dictionary<AnimatorController, Dictionary<string, HashSet<AnimationClip>>>();
    private bool isFoldoutAnimator = true;
    private Color fixedBackgroundColor = new Color32(87, 87, 87, 255); // #575757
    private DefaultAsset selectedFolder;

    [MenuItem("CHISENOTE/Cerberus")]
    private static void ShowWindow()
    {
        var window = GetWindow<Cerberus>("Cerberus");
        window.Show();
    }

    private void OnEnable()
    {
        selectedFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets");
        isFoldoutMaterials = true;
        isFoldoutAnimator = true;
    }

    private void OnGUI()
    {
        selectedPrefab = (GameObject)EditorGUILayout.ObjectField("Select Prefab", selectedPrefab, typeof(GameObject), true);

        if (GUILayout.Button("Check Prefab"))
        {
            ClearDictionaries();
            HashSet<AnimationClip> uniqueClips = new HashSet<AnimationClip>();

            if (selectedPrefab != null)
            {
                Debug.Log("Selected object: " + selectedPrefab.name);
                ProcessRenderers();
                ProcessAvatarDescriptor(uniqueClips);
            }
        }

        DisplayUI();
    }

    private void ClearDictionaries()
    {
        materialUsage.Clear();
        materialTextures.Clear();
        animatorUsage.Clear();
        animatorClips.Clear();
    }

    private void ProcessRenderers()
    {
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
                AddMaterialTextures(material);
            }
        }
    }

    private void AddMaterialTextures(Material material)
    {
        foreach (var property in material.GetTexturePropertyNameIDs())
        {
            Texture texture = material.GetTexture(property);
            if (texture != null)
            {
                materialTextures[material].Add(texture);
            }
        }
    }

    private void ProcessAvatarDescriptor(HashSet<AnimationClip> uniqueClips)
    {
        VRCAvatarDescriptor avatarDescriptor = selectedPrefab.GetComponent<VRCAvatarDescriptor>();
        if (avatarDescriptor != null)
        {
            foreach (var layer in avatarDescriptor.baseAnimationLayers)
            {
                if (layer.animatorController is AnimatorController ac)
                {
                    if (!animatorUsage.ContainsKey(ac))
                    {
                        animatorUsage[ac] = new List<string>();
                        animatorClips[ac] = new Dictionary<string, HashSet<AnimationClip>>();
                    }
                    animatorUsage[ac].Add(layer.type.ToString());
                    foreach (var acl in ac.layers)
                    {
                        if (!animatorClips[ac].ContainsKey(acl.stateMachine.name))
                        {
                            animatorClips[ac][acl.stateMachine.name] = new HashSet<AnimationClip>();
                        }
                        ExploreStateMachine(acl.stateMachine, uniqueClips, animatorClips[ac][acl.stateMachine.name]);
                    }
                }
            }
        }
        else
        {
            Debug.Log("VRCAvatarDescriptor component not found");
        }
    }

    private void DisplayUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        GUILayout.BeginHorizontal();

        float scrollbarWidth = 15.0f;
        float halfWidth = (position.width - scrollbarWidth) / 2 - 4;

        DisplayMaterialsAndTextures(halfWidth);
        DisplayAnimatorsAndAnimations(halfWidth);

        GUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();

        GUILayout.BeginVertical();

        selectedFolder = (DefaultAsset)EditorGUILayout.ObjectField("Save Folder", selectedFolder, typeof(DefaultAsset), false);

        if (GUILayout.Button("Save"))
        {
            if (selectedFolder != null && selectedPrefab != null)
            {
                string folderPath = AssetDatabase.GetAssetPath(selectedFolder);
                string targetFolderPath = Path.Combine(folderPath, selectedPrefab.name);

                if (!AssetDatabase.IsValidFolder(targetFolderPath))
                {
                    AssetDatabase.CreateFolder(folderPath, selectedPrefab.name);
                    Debug.Log("Created folder: " + targetFolderPath);

                    string[] subFolders = { "texture", "material" };
                    foreach (string subFolder in subFolders)
                    {
                        AssetDatabase.CreateFolder(targetFolderPath, subFolder);
                        Debug.Log($"Created {subFolder} folder inside: {targetFolderPath}");
                    }
                }
                else
                {
                    Debug.Log($"Folder already exists: {targetFolderPath}. No new folder created.");
                }

                CopyAndRelinkMaterials(targetFolderPath);
            }
            else
            {
                Debug.Log("No folder or prefab selected.");
            }
        }

        GUILayout.EndVertical();
    }

    private void DisplayMaterialsAndTextures(float width)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(width));
        Rect foldoutRect = EditorGUILayout.GetControlRect();
        EditorGUI.DrawRect(foldoutRect, fixedBackgroundColor);
        isFoldoutMaterials = EditorGUI.Foldout(foldoutRect, isFoldoutMaterials, "Materials and Textures", true);
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
    }

    private void DisplayAnimatorsAndAnimations(float width)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(width));
        Rect foldoutRect = EditorGUILayout.GetControlRect();
        EditorGUI.DrawRect(foldoutRect, fixedBackgroundColor);
        isFoldoutAnimator = EditorGUI.Foldout(foldoutRect, isFoldoutAnimator, "Animator and Animation", true);
        if (isFoldoutAnimator)
        {
            foreach (var animatorController in animatorUsage.Keys)
            {
                EditorGUILayout.ObjectField("Animator Controller", animatorController, typeof(AnimatorController), false);
                foreach (var layerName in animatorUsage[animatorController])
                {
                    EditorGUILayout.LabelField("Used in layer: " + layerName);
                }
                foreach (var entry in animatorClips[animatorController])
                {
                    foreach (var clip in entry.Value)
                    {
                        if (!clip.name.Contains("proxy"))
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(clip.name, GUILayout.Width(170));
                            EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false);
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void ExploreStateMachine(AnimatorStateMachine stateMachine, HashSet<AnimationClip> uniqueClips, HashSet<AnimationClip> controllerClips)
    {
        foreach (var state in stateMachine.states)
        {
            if (state.state.motion is AnimationClip clip && !clip.name.Contains("proxy"))
            {
                uniqueClips.Add(clip);
                controllerClips.Add(clip);
            }
            else if (state.state.motion is BlendTree blendTree)
            {
                ExploreBlendTree(blendTree, uniqueClips, controllerClips);
            }
        }

        foreach (var subStateMachine in stateMachine.stateMachines)
        {
            ExploreStateMachine(subStateMachine.stateMachine, uniqueClips, controllerClips);
        }
    }

    private void ExploreBlendTree(BlendTree blendTree, HashSet<AnimationClip> uniqueClips, HashSet<AnimationClip> controllerClips)
    {
        foreach (var child in blendTree.children)
        {
            if (child.motion is AnimationClip clip && !clip.name.Contains("proxy"))
            {
                uniqueClips.Add(clip);
                controllerClips.Add(clip);
            }
            else if (child.motion is BlendTree childBlendTree)
            {
                ExploreBlendTree(childBlendTree, uniqueClips, controllerClips);
            }
        }
    }

    private void CopyAndRelinkMaterials(string targetFolderPath)
    {
        foreach (var material in materialUsage.Keys)
        {
            string materialPath = AssetDatabase.GetAssetPath(material);
            string materialCopyPath = Path.Combine(targetFolderPath, "material", Path.GetFileName(materialPath));
            AssetDatabase.CopyAsset(materialPath, materialCopyPath);

            foreach (var texture in materialTextures[material])
            {
                string texturePath = AssetDatabase.GetAssetPath(texture);
                string textureCopyPath = Path.Combine(targetFolderPath, "texture", Path.GetFileName(texturePath));
                AssetDatabase.CopyAsset(texturePath, textureCopyPath);
            }

            Material copiedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialCopyPath);
            foreach (var property in copiedMaterial.GetTexturePropertyNameIDs())
            {
                Texture texture = copiedMaterial.GetTexture(property);
                if (texture != null)
                {
                    string newTexturePath = Path.Combine(targetFolderPath, "texture", Path.GetFileName(AssetDatabase.GetAssetPath(texture)));
                    Texture newTexture = AssetDatabase.LoadAssetAtPath<Texture>(newTexturePath);
                    copiedMaterial.SetTexture(property, newTexture);
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
