using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;


public partial class Mirror_Material_Data : EditorWindow
{
    private Vector2 scrollPosition;
    private Color fixedBackgroundColor = new Color32(87, 87, 87, 255); // #575757
    private DefaultAsset selectedFolder;

    [MenuItem("CHISENOTE/Mirror_Materials")]
    private static void ShowWindow()
    {
        var window = GetWindow<Mirror_Material_Data>("Mirror_Materials");
        window.Show();
    }

    private void OnGUI()
    {
        DisplayUI();
    }

    private void DisplayUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        GUILayout.BeginHorizontal();

        float scrollbarWidth = 15.0f;
        float halfWidth = (position.width - scrollbarWidth) / 2 - 4;

        OldMaterial(halfWidth);
        NewMaterial(halfWidth);

        GUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();

        GUILayout.BeginVertical();

        selectedFolder = (DefaultAsset)EditorGUILayout.ObjectField("Save Folder", selectedFolder, typeof(DefaultAsset), false);
        if (GUILayout.Button("Save"))
        {
            if (selectedFolder != null)
            {
                Debug.Log("Save");
            }
            else
            {
                Debug.Log("Please select a folder to save.");
            }
        }


        GUILayout.EndVertical();
    }

    private void OldMaterial(float width)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(width));
        Rect foldoutRect = EditorGUILayout.GetControlRect();
        EditorGUI.DrawRect(foldoutRect, fixedBackgroundColor);

        Rect labelRect = new Rect(foldoutRect.x, foldoutRect.y, foldoutRect.width, 20);
        EditorGUI.LabelField(labelRect, "OLD_Material", EditorStyles.whiteLabel);



        EditorGUILayout.EndVertical();
    }

    private void NewMaterial(float width)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(width));
        Rect foldoutRect = EditorGUILayout.GetControlRect();
        EditorGUI.DrawRect(foldoutRect, fixedBackgroundColor);

        Rect labelRect = new Rect(foldoutRect.x, foldoutRect.y, foldoutRect.width, 20);
        EditorGUI.LabelField(labelRect, "NEW_Material", EditorStyles.whiteLabel);



        EditorGUILayout.EndVertical();
    }
}
