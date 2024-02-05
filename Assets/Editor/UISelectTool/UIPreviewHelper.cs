using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UIEditor
{
    public static class UIPreviewHelper
    {
        public static readonly Color BackgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        public static readonly Vector2 Size = new Vector2(128, 128);

        public static Texture[] GetUIAssetPreviews(GameObject[] objs)
        {
            GameObject canvasObject = new GameObject("RenderCanvas", typeof(Canvas));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            GameObject cameraObject = new GameObject("RenderCamera");

            Camera renderCamera = cameraObject.AddComponent<Camera>();
            renderCamera.backgroundColor = BackgroundColor;
            renderCamera.clearFlags = CameraClearFlags.Color;
            renderCamera.cameraType = CameraType.Preview;
            renderCamera.cullingMask = 1 << 21;

            canvas.worldCamera = renderCamera;
            canvasObject.transform.position = new Vector3(-1000, -1000, -1000);
            canvasObject.layer = 21; //放在21层，摄像机也只渲染此层的，避免混入了奇怪的东西

            //TODO 如果不然删了再Undo的话，会渲染不出来
            Undo.DestroyObjectImmediate(cameraObject);
            Undo.PerformUndo();

            int amount = objs.Length;
            Texture[] textures = new Texture[amount];
            for (int i = 0; i < amount; i++)
            {
                GameObject obj = objs[i];
                GameObject clone = Object.Instantiate(obj, canvasObject.transform);
                Transform cloneTransform = clone.transform;

                cloneTransform.localPosition = Vector3.zero;

                Transform[] all = clone.GetComponentsInChildren<Transform>();
                foreach (Transform trans in all) trans.gameObject.layer = 21;

                Bounds bounds = GetBounds(clone);
                Vector3 Min = bounds.min;
                Vector3 Max = bounds.max;

                cameraObject.transform.position = new Vector3((Max.x + Min.x) / 2f, (Max.y + Min.y) / 2f, cloneTransform.position.z - 100);
                Vector3 center = new Vector3(cloneTransform.position.x, (Max.y + Min.y) / 2f, cloneTransform.position.z);
                cameraObject.transform.LookAt(center);

                renderCamera.orthographic = true;
                float width = Max.x - Min.x;
                float height = Max.y - Min.y;
                float maxCameraSize = width > height ? width : height;
                renderCamera.orthographicSize = maxCameraSize / 2; //预览图要尽量少点空白

                RenderTexture texture = new RenderTexture((int) Size.x, (int) Size.y, 0, RenderTextureFormat.Default);
                renderCamera.targetTexture = texture;

                renderCamera.RenderDontRestore();
                RenderTexture tex = new RenderTexture(128, 128, 0, RenderTextureFormat.Default);
                Graphics.Blit(texture, tex);
                textures[i] = tex;

                Object.DestroyImmediate(clone);
            }

            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(cameraObject);
            return textures;
        }

        public static Bounds GetBounds(GameObject obj)
        {
            Vector3 Min = new Vector3(99999, 99999, 99999);
            Vector3 Max = new Vector3(-99999, -99999, -99999);

            RectTransform[] rectTrans = obj.GetComponentsInChildren<RectTransform>();
            Vector3[] corner = new Vector3[4];
            for (int i = 0; i < rectTrans.Length; i++)
            {
                //获取节点的四个角的世界坐标，分别按顺序为左下左上，右上右下
                rectTrans[i].GetWorldCorners(corner);
                if (corner[0].x < Min.x) Min.x = corner[0].x;
                if (corner[0].y < Min.y) Min.y = corner[0].y;
                if (corner[0].z < Min.z) Min.z = corner[0].z;

                if (corner[2].x > Max.x) Max.x = corner[2].x;
                if (corner[2].y > Max.y) Max.y = corner[2].y;
                if (corner[2].z > Max.z) Max.z = corner[2].z;
            }

            Vector3 center = (Min + Max) / 2;
            Vector3 size = new Vector3(Max.x - Min.x, Max.y - Min.y, Max.z - Min.z);
            return new Bounds(center, size);
        }

        public static List<int> GetIndex(GameObject current, GameObject root)
        {
            List<int> indexList = new List<int>();
            GetIndex(indexList, current, root).Reverse();
            return indexList;
        }

        private static List<int> GetIndex(List<int> index, GameObject current, GameObject target)
        {
            if (current == target) { return index; }
            index.Add(current.transform.GetSiblingIndex());
            return GetIndex(index, current.transform.parent.gameObject, target);
        }
    }
}