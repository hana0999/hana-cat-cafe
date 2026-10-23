using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hana.CatCafe.Editor
{
    public static class CafeSetup
    {
        [MenuItem("Hana/Create Cat Cafe Scene")]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Cat Cafe Prototype");
            root.AddComponent<CatCafePrototype>();
            if(!AssetDatabase.IsValidFolder("Assets/HanaCatCafe/Scenes"))
                AssetDatabase.CreateFolder("Assets/HanaCatCafe", "Scenes");
            string path=AssetDatabase.GenerateUniqueAssetPath("Assets/HanaCatCafe/Scenes/CatCafe.unity");
            EditorSceneManager.SaveScene(scene,path);
            Selection.activeGameObject=root;
            Debug.Log("Cat cafe scene saved. Open the Game tab and press Play.");
        }
    }
}
