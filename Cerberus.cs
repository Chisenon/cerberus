using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;

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

    [MenuItem("CHISENOTE/Cerberus")]
    private static void ShowWindow()
    {
        var window = GetWindow<Cerberus>("Cerberus");
        window.Show();
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
                // Debug.Log("Number of unique materials: " + materialUsage.Count);
                // Debug.Log("Number of unique Textures: " + materialTextures.Count);
                // Debug.Log("Number of unique Animator Controllers: " + animatorUsage.Count);
                // Debug.Log("Number of unique Animation Clips: " + uniqueClips.Count);
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

        float scrollbarWidth = 15.0f;  // スクロールバーの幅を定義
        float halfWidth = (position.width - scrollbarWidth) / 2 - 4;  // スクロールバーの幅を引いたサイズ

        DisplayMaterialsAndTextures(halfWidth);
        DisplayAnimatorsAndAnimations(halfWidth);

        GUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }

    private void DisplayMaterialsAndTextures(float width)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(width));
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
    }

    private void DisplayAnimatorsAndAnimations(float width)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(width));
        isFoldoutAnimator = EditorGUILayout.Foldout(isFoldoutAnimator, "Animator and Animation");
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
                            EditorGUILayout.LabelField(clip.name, GUILayout.Width(250));
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
}
