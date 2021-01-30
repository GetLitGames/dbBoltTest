using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Developer: Dibbie
/// Email: mailto:strongstrenth@hotmail.com [for questions/help and inquires]
/// 
/// Simple Scene Navigator (SSN) should make it easier to switch between scenes,
/// having it as a dockable window you can always leave open to quickly switch between scenes
/// rather than having to go through your Project view and navigate out of the folder you were in
/// 
/// SSN provides the following basic features:
/// - Search scenes by name or index (i: search by index exclusive, n: search by name exclusive)
/// - Lists scenes by their index then name (scenes in Build Settings)
/// - "Load" btn to switch to that scene (with the infamous save prompt if the scene is dirty/unsaved changes)
/// - "Find" btn to ping/locate that file in your Project view
/// - Hovering over the name will display its path as a tooltip
/// - Hovering over buttons will explain exactly what they will do
/// </summary>
public class SimpleSceneNavEditor : EditorWindow
{
    string searchQuery;
    List<SceneData> knownScenes = new List<SceneData>();

    int searchResults, searchedIndex;
    string lblSceneRowDisplay;
    GUIContent lblSceneRowContent = new GUIContent();
    GUIContent btnLoadSceneContent = new GUIContent();
    GUIContent btnFindSceneContent = new GUIContent();

    class SceneData
    {
        public string name, path;
        public int index;
        public Scene scene;
        public bool isValidScene { get { return !string.IsNullOrEmpty(scene.name); } }
    }

    void GetKnownScenes()
    {
        int index = 0;
        knownScenes.Clear();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            SceneData data = new SceneData();
            data.index = index++;
            data.path = scene.path;
            data.name = scene.path.Substring(scene.path.LastIndexOf('/') + 1).Replace(".unity", string.Empty);
            data.scene = EditorSceneManager.GetSceneByPath(scene.path);
            knownScenes.Add(data);
        }
    }

    [MenuItem("Window/Scene Navigator", priority = -142)]
    static void Init()
    {
        var window = GetWindow<SimpleSceneNavEditor>("Scene Navigator");
        window.minSize = new Vector2(450, 300);
        if (window.docked) { window.position = new Rect(window.position.x, window.position.y, window.minSize.x, window.minSize.y); }
        window.Show();
    }

    private void OnEnable()
    {
        GetKnownScenes();
    }

    private void OnGUI()
    {
        //create search box
        GUILayout.Label("Search");
        searchQuery = GUILayout.TextField(searchQuery);

        //loop through all listed scenes
        if (string.IsNullOrEmpty(searchQuery))
        {
            ShowKnownScenes();
        }
        else
        {
            ShowSearchResults();
        }
    }

    void OnInspectorUpdate()
    {
        GetKnownScenes();
        Repaint();
    }

    void SwitchScene(SceneData scene)
    {
        bool userWantsSave = false;
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            userWantsSave = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        if (userWantsSave || !EditorSceneManager.GetActiveScene().isDirty) { EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single); }
    }

    void DisplaySceneDetails(int i)
    {
        GUILayout.BeginHorizontal(EditorStyles.helpBox);

        //display index:name as label
        if (knownScenes[i].isValidScene && knownScenes[i].scene.isLoaded)
        {
            lblSceneRowDisplay = knownScenes[i].scene.buildIndex < 0 ? " - : " : knownScenes[i].scene.buildIndex + " : ";
            lblSceneRowDisplay += (knownScenes[i].scene.isDirty ? "*" : "") + knownScenes[i].name;
            lblSceneRowContent.text = lblSceneRowDisplay;
            lblSceneRowContent.tooltip = knownScenes[i].scene.isLoaded ? "[Active Scene]\n" : "";
            lblSceneRowContent.tooltip += (knownScenes[i].scene.isDirty ? "*Unsaved Changes\n" : "") + knownScenes[i].path;
            GUILayout.Label(lblSceneRowContent, EditorStyles.boldLabel);
        }
        else
        {
            lblSceneRowDisplay = knownScenes[i].index + " : ";
            lblSceneRowDisplay += knownScenes[i].name;
            lblSceneRowContent.text = lblSceneRowDisplay;
            lblSceneRowContent.tooltip = knownScenes[i].path;
            GUILayout.Label(lblSceneRowContent, EditorStyles.label);
        }

        //"Load" button
        btnLoadSceneContent.text = "Load";
        btnLoadSceneContent.tooltip = "Switch scenes to '" + knownScenes[i].name + "' - you will be prompted on any unsaved scene changes";
        if (GUILayout.Button(btnLoadSceneContent, GUILayout.Width(50))) { SwitchScene(knownScenes[i]); }
        
        //"Find" button
        btnFindSceneContent.text = "Find";
        btnFindSceneContent.tooltip = "Locate the scene '" + knownScenes[i].name + "' in the Project View";
        if (GUILayout.Button(btnFindSceneContent, GUILayout.Width(50))) { EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(knownScenes[i].path)); }

        GUILayout.EndHorizontal();
    }

    void ShowKnownScenes()
    {
        for (int i = 0; i < knownScenes.Count; i++)
        {
            DisplaySceneDetails(i);
        }
    }

    void ShowSearchResults()
    {
        searchResults = 0;
        bool isSearchingIndex;

        for (int i = 0; i < knownScenes.Count; i++)
        {
            isSearchingIndex = int.TryParse(searchQuery, out searchedIndex);

            if (knownScenes[i].name.ToLower().Contains(searchQuery.ToLower()) || isSearchingIndex) //search by name or index
            {
                if (isSearchingIndex && i != searchedIndex) { continue; }

                searchResults++;
                DisplaySceneDetails(isSearchingIndex ? searchedIndex : i);
            }
            else if (searchQuery.ToLower().StartsWith("i:")) //search by index only
            {
                if (!int.TryParse(searchQuery.Substring(2).Trim(), out searchedIndex)) { return; }
                if (searchedIndex > knownScenes.Count - 1) { GUILayout.Label("Index out of range", EditorStyles.helpBox); return; }
                if (i != searchedIndex) { continue; }

                searchResults++;
                DisplaySceneDetails(searchedIndex);
            }
            else if (searchQuery.ToLower().StartsWith("n:")) //search by name only
            {
                if (!knownScenes[i].name.ToLower().Contains(searchQuery.Substring(2).Trim().ToLower())) { continue; }

                searchResults++;
                DisplaySceneDetails(i);
            }
        }

        if (searchResults == 0) { GUILayout.Space(10); GUILayout.Label("No scenes found that match your search"); }
    }
}
