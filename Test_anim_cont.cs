using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using VRC.SDK3.Avatars.Components;

public partial class Test_anim_cont : EditorWindow
{
    private GameObject selectedPrefab;
    private Dictionary<AnimatorController, List<string>> animatorUsage = new Dictionary<AnimatorController, List<string>>();
    private Dictionary<AnimatorController, Dictionary<string, HashSet<AnimationClip>>> animatorClips = new Dictionary<AnimatorController, Dictionary<string, HashSet<AnimationClip>>>();
    private Vector2 scrollPosition;
    private bool isFoldoutAnimator = true;

    [MenuItem("CHISENOTE/Test_window_AnimCont")]
    private static void ShowWindow()
    {
        var window = GetWindow<Test_anim_cont>("UIElements");
        window.titleContent = new GUIContent("AAA");
        window.Show();
    }

    private void OnGUI()
    {
        selectedPrefab = (GameObject)EditorGUILayout.ObjectField("Select Prefab", selectedPrefab, typeof(GameObject), true);

        if (GUILayout.Button("Check Prefab"))
        {
            animatorUsage.Clear();
            animatorClips.Clear();
            HashSet<AnimationClip> uniqueClips = new HashSet<AnimationClip>();

            if (selectedPrefab != null)
            {
                Debug.Log("Selected object: " + selectedPrefab.name);

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
                    Debug.Log("Number of unique Animator Controllers: " + animatorUsage.Count);
                }
                else
                {
                    Debug.Log("VRCAvatarDescriptor component not found");
                }
            }

            Debug.Log("Number of unique Animation Clips: " + uniqueClips.Count);
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        isFoldoutAnimator = EditorGUILayout.Foldout(isFoldoutAnimator, "Animator Controllers and Animation Clips");
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
                        // プロキシでないAnimationClipのみ表示
                        if (!clip.name.Contains("proxy"))
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(clip.name, GUILayout.Width(250)); // Display the state name with a fixed width
                            EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false); // Display the animation clip
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
            }
        }
        EditorGUILayout.EndScrollView();
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
                ExploreBlendTree(blendTree, uniqueClips, controllerClips, state.state.name);
            }
        }

        foreach (var subStateMachine in stateMachine.stateMachines)
        {
            ExploreStateMachine(subStateMachine.stateMachine, uniqueClips, controllerClips);
        }
    }

    private void ExploreBlendTree(BlendTree blendTree, HashSet<AnimationClip> uniqueClips, HashSet<AnimationClip> controllerClips, string stateName)
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
                ExploreBlendTree(childBlendTree, uniqueClips, controllerClips, stateName + "/" + childBlendTree.name);
            }
        }
    }
}
