using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UIInspectorTool
{
    public class UIInspectorToolWindow : EditorWindow
    {
        private readonly Vector2 CellSize = new Vector2(128, 128);
        public GUIStyle tipStyle;

        [MenuItem("Tools/InspectorTool", false, 0)]
        public static void OpenWindow()
        {
            UIInspectorToolWindow window = GetWindow<UIInspectorToolWindow>("UIInspectorTool");
            window.position = new Rect(Screen.width / 2f, Screen.height / 2f, 900, 600);
            window.CenterOnMainWin();
            window.Init();
            window.Show();
        }

        private GameObject editorUI;
        private Vector2 uiListPosition;
        private float sizeRate;

        private List<UIInspectorItem> uiItemList;

        private List<GameObject> selectUIList;

        private string searchString;

        void Init()
        {
            tipStyle = new GUIStyle();
            tipStyle.normal.textColor = new Color(0.5f, 0.85f, 1f, 0.5f);
            tipStyle.fontStyle = FontStyle.Bold;
            tipStyle.fontSize = 36;
            this.tipStyle.alignment = TextAnchor.MiddleCenter;

            uiItemList = new List<UIInspectorItem>();
            this.selectUIList = new List<GameObject>();
            sizeRate = 1;
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui -= SceneGUI;
            SceneView.duringSceneGui += SceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= SceneGUI;
        }

        private void SceneGUI(SceneView sceneView)
        {
            if (Event.current.type == EventType.MouseDown
                && Event.current.button == 0)
            {
                // 当前屏幕坐标，左上角是（0，0）右下角（camera.pixelWidth，camera.pixelHeight）
                Vector2 mousePosition = Event.current.mousePosition;
                // Retina 屏幕需要拉伸值
                float mult = EditorGUIUtility.pixelsPerPoint;
                // 转换成摄像机可接受的屏幕坐标，左下角是（0，0，0）右上角是（camera.pixelWidth，camera.pixelHeight，0）
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

                this.selectUIList.Clear();
                foreach (IGrouping<string, RectTransform> group in groups)
                {
                    foreach (RectTransform rectTransform in group) { this.selectUIList.Add(rectTransform.gameObject); }
                }
            }
        }

        private static IEnumerable<Scene> GetAllScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++) yield return SceneManager.GetSceneAt(i);
        }

        void OnSelectionChange()
        {
            Repaint();
        }

        private void OnGUI()
        {
            GetEditorUI();
            if (this.editorUI == null) { TipDraw(); }
            else { UIOperateDraw(); }
        }

        void GetEditorUI()
        {
            Event e = Event.current;
            Rect eventArea = new Rect(0, 0, position.width, position.height);

            if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
            {
                if (eventArea.Contains(e.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        int amount = DragAndDrop.objectReferences.Length;
                        for (int i = 0; i < amount; i++)
                        {
                            GameObject gameObject = DragAndDrop.objectReferences[i] as GameObject;
                            if (gameObject != null)
                            {
                                SetEditor(gameObject);
                                break;
                            }
                        }
                    }
                    e.Use();
                }
            }
        }

        void SetEditor(GameObject ui)
        {
            if (ui != null)
            {
                this.editorUI = ui;
                GeneratePreview(this.editorUI);
            }
        }

        void GeneratePreview(GameObject gameObject)
        {
            uiItemList.Clear();
            Transform[] uiTransforms = gameObject.GetComponentsInChildren<Transform>(false);
            if (uiTransforms != null)
            {
                GameObject[] uiGameObjects = Array.ConvertAll(uiTransforms, t => t.gameObject);
                Texture[] uiPreviews = UIInspectorHelper.GetUIAssetPreviews(uiGameObjects);
                int amount = uiGameObjects.Length;
                for (int i = 0; i < amount; i++)
                {
                    Texture uiPreview = uiPreviews[i];
                    if (UIInspectorHelper.IsNull(uiPreview)) { continue; }
                    GameObject uiGameObject = uiGameObjects[i];
                    UIInspectorItem item = new UIInspectorItem();
                    item.ui = uiGameObject;
                    item.preview = uiPreview;
                    item.uiIndex = UIInspectorHelper.GetIndex(uiGameObject, gameObject);
                    this.uiItemList.Add(item);
                }
                this.uiItemList.Sort(SortItem);
            }
        }

        int SortItem(UIInspectorItem a, UIInspectorItem b)
        {
            int minIndex = Mathf.Min(a.uiIndex.Count, b.uiIndex.Count);
            if (a.uiIndex.Count == 0 && b.uiIndex.Count != 0) { return 1; }
            if (b.uiIndex.Count == 0 && a.uiIndex.Count != 0) { return -1; }

            for (int i = 0; i < minIndex; i++)
            {
                int aIndex = a.uiIndex[i];
                int bIndex = b.uiIndex[i];
                if (aIndex == bIndex) { continue; }
                if (aIndex > bIndex) { return -1; }
                if (aIndex < bIndex) { return 1; }
            }

            if (a.uiIndex.Count > b.uiIndex.Count) { return -1; }
            if (a.uiIndex.Count < b.uiIndex.Count) { return 1; }

            return 0;
        }

        void TipDraw()
        {
            GUILayout.Label("将UI拖拽至此", tipStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        }

        void UIOperateDraw()
        {
            GUILayout.BeginHorizontal();
            {
                string tempSearchString = EditorGUILayout.TextField("", searchString, "SearchTextField");

                if (GUILayout.Button("清空", GUILayout.Width(50)))
                {
                    this.searchString = "";
                    RefreshSearch();
                }
                if (this.searchString != tempSearchString)
                {
                    this.searchString = tempSearchString;
                    RefreshSearch();
                }

                if (GUILayout.Button("刷新", GUILayout.Width(50))) { GeneratePreview(editorUI); }

            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("清空选择", GUILayout.Width(70))) { this.selectUIList.Clear(); }
                this.sizeRate = EditorGUILayout.Slider(sizeRate, 0, 1);
            }
            GUILayout.EndHorizontal();

            List<int> indexs = new List<int>();
            if (this.selectUIList.Count > 0)
            {
                int uiAmount = this.uiItemList.Count;
                for (int j = 0; j < uiAmount; j++)
                {
                    GameObject ui = this.uiItemList[j].ui;

                    int amount = this.selectUIList.Count;
                    for (int i = 0; i < amount; i++)
                    {
                        GameObject selectUI = this.selectUIList[i];
                        if (selectUI == ui)
                        {
                            indexs.Add(j);
                            break;
                        }
                    }
                }
            }
            else if (this.uiItemList != null)
            {
                for (int i = this.uiItemList.Count - 1; i >= 0; i--) indexs.Add(i);
            }

            UIListDraw(indexs);
        }

        void RefreshSearch()
        {
            this.selectUIList.Clear();
            int amount = this.uiItemList.Count;
            for (int i = 0; i < amount; i++)
            {
                GameObject ui = this.uiItemList[i].ui;
                if (UIInspectorHelper.Search(ui.name, this.searchString)) { this.selectUIList.Add(ui); }
            }
        }

        void UIListDraw(List<int> indexs)
        {
            GUILayout.BeginVertical("frameBox");

            this.uiListPosition = EditorGUILayout.BeginScrollView(this.uiListPosition, false, false);
            {
                bool isStartLayout = false;
                float maxWidth = position.width - 100;
                float currentWidth = 0;
                int amount = indexs.Count;
                for (int i = 0; i < amount; i++)
                {
                    int index = indexs[i];
                    GameObject uiObject = this.uiItemList[index].ui;
                    Texture uiPreview = this.uiItemList[index].preview;

                    if (isStartLayout == false)
                    {
                        isStartLayout = true;
                        GUILayout.BeginHorizontal();
                    }

                    GUILayout.BeginVertical();
                    if (GUILayout.Button(uiPreview, GUILayout.Width(CellSize.x * this.sizeRate), GUILayout.Height(this.CellSize.y * sizeRate)))
                    {
                        EditorGUIUtility.PingObject(uiObject);
                        Selection.activeGameObject = uiObject;
                    }

                    GUILayout.Label(uiObject.name);
                    GUILayout.EndVertical();
                    currentWidth += CellSize.x * this.sizeRate;

                    if (currentWidth + CellSize.x * this.sizeRate > maxWidth || i == amount - 1)
                    {
                        currentWidth = 0;
                        isStartLayout = false;
                        GUILayout.EndHorizontal();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            GUILayout.EndHorizontal();
        }
    }
}