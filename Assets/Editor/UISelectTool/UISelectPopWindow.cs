using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UIEditor
{
    public class UISelectPopWindow : EditorWindow
    {
        public static void PopupWindow(GameObject target, List<GameObject> selectList)
        {
            UISelectPopWindow window = CreateInstance<UISelectPopWindow>();

            window.target = target;
            window.selectList = selectList;

            Vector2 size = UISelectSetting.size;
            Vector2 interval = UISelectSetting.interval;
            Vector2 offset = UISelectSetting.offset;
            Vector2 showAmount = UISelectSetting.showAmount;
            float scale = UISelectSetting.scale;

            Event e = Event.current;
            Vector2 mousePosition = e.mousePosition;
            Vector2 screenPosition = GUIUtility.GUIToScreenPoint(mousePosition);

            float sizeX = size.x * scale * showAmount.x + interval.x * (showAmount.x - 1);
            float sizeY = size.y * scale * showAmount.y + interval.y * (showAmount.y - 1);

            Vector2 windowSize = new Vector2(sizeX, sizeY) + offset;
            Rect rect = new Rect(screenPosition, windowSize);

            window.position = rect;

            window.ShowPopup();
            window.Focus();
        }

        [SerializeField]
        private GameObject target;

        [SerializeField]
        private List<GameObject> selectList;

        [SerializeField]
        private List<UIPreviewItem> selectItemList;

        [SerializeField]
        private Vector2 scrollPosition;

        [SerializeField]
        private List<UIPreviewItem> itemList;

        private void OnInspectorUpdate()
        {
            if (focusedWindow != this) this.Close();
            Repaint();
        }

        private void OnGUI()
        {
            if (selectItemList == null) InitSelectItemList();
            else SelectItemListDrawer();
        }

        void InitSelectItemList()
        {
            Event e = Event.current;
            if (e.type == EventType.Layout) return;
            if (itemList == null) GeneratePreview(this.target);
            this.selectItemList = GetSelectUIList(this.selectList);
            selectItemList.Reverse();
        }

        void GeneratePreview(GameObject gameObject)
        {
            this.itemList = new List<UIPreviewItem>();
            Transform[] uiTransforms = gameObject.GetComponentsInChildren<Transform>(false);
            if (uiTransforms == null) return;
            GameObject[] uiGameObjects = uiTransforms.Select((x) => x.gameObject).ToArray();
            Texture[] uiPreviews = UIPreviewHelper.GetUIAssetPreviews(uiGameObjects);
            int amount = uiGameObjects.Length;
            for (int i = 0; i < amount; i++)
            {
                Texture uiPreview = uiPreviews[i];
                GameObject uiGameObject = uiGameObjects[i];
                Graphic graphics = uiGameObject.GetComponentInChildren<Graphic>();
                if (graphics == null) continue;
                UIPreviewItem item = new UIPreviewItem();
                item.ui = uiGameObject;
                item.preview = uiPreview;
                item.uiIndex = UIPreviewHelper.GetIndex(uiGameObject, gameObject);
                this.itemList.Add(item);
            }
            this.itemList.Sort(SortItem);
        }

        int SortItem(UIPreviewItem a, UIPreviewItem b)
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

        List<UIPreviewItem> GetSelectUIList(List<GameObject> selects)
        {
            List<UIPreviewItem> selectItems = new List<UIPreviewItem>();
            int amount = selects.Count;
            for (int i = 0; i < amount; i++)
            {
                GameObject select = selects[i];
                UIPreviewItem item = this.itemList.Find(m => m.ui == select);
                if (item != null) selectItems.Add(item);
            }

            return selectItems;
        }

        void SelectItemListDrawer()
        {
            
            Vector2 size = UISelectSetting.size;
            float scale = UISelectSetting.scale;
            Vector2Int showAmount = UISelectSetting.showAmount;
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            int horizontalAmount = showAmount.x;
            int amount = this.selectItemList.Count;
            for (int i = 0; i < amount; i++)
            {
                UIPreviewItem item = this.selectItemList[i];
                if (i % horizontalAmount == 0) GUILayout.BeginHorizontal();

                float width = size.x * scale;
                float height = size.y * scale;

                GUILayout.BeginVertical();

                if (GUILayout.Button(item.preview, GUILayout.Width(width), GUILayout.Height(height)))
                {
                    Selection.activeGameObject = item.ui.gameObject;
                    EditorGUIUtility.PingObject(item.ui.gameObject);
                    this.Close();
                }

                GUILayout.Label(item.ui.name);

                GUILayout.EndVertical();

                if (i % horizontalAmount == horizontalAmount - 1) GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }
    }
}