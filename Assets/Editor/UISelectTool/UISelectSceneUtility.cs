using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UIEditor
{
    [InitializeOnLoad]
    public static class UISelectSceneUtility
    {
        static UISelectSceneUtility()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }
        
        public static void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 1) return;
            Vector2 mousePosition = Event.current.mousePosition;
            float mult = EditorGUIUtility.pixelsPerPoint;
            mousePosition.y = sceneView.camera.pixelHeight - mousePosition.y * mult;
            mousePosition.x *= mult;

            IEnumerable<Scene> scenes = GetAllScenes();
            IGrouping<string, RectTransform>[] groups = scenes
                .Where(m => m.isLoaded)
                .SelectMany(m => m.GetRootGameObjects())
                .Where(m => m.activeInHierarchy)
                .SelectMany(m => m.GetComponentsInChildren<RectTransform>())
                .Where(m => RectTransformUtility.RectangleContainsScreenPoint(m, mousePosition, sceneView.camera))
                .GroupBy(m => m.gameObject.scene.name)
                .ToArray();

            List<GameObject> selectUIList = new List<GameObject>();
            foreach (IGrouping<string, RectTransform> group in groups)
            {
                foreach (RectTransform rectTransform in group) { selectUIList.Add(rectTransform.gameObject); }
            }

            if (selectUIList.Count <= 0) return;

            Event.current.Use();
            UISelectPopWindow.PopupWindow(GetUIRoot(), selectUIList);
        }

        private static IEnumerable<Scene> GetAllScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++) yield return SceneManager.GetSceneAt(i);
        }

        private static GameObject GetUIRoot()
        {
            //TODO Temp
            return GameObject.Find("UIRoot");
        }
    }
}