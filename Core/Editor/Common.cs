using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UIInspectorTool
{
    public static class Common
    {
        public static Type[] GetAllDerivedTypes(this AppDomain targetAppDomain, Type targetType)
        {
            List<Type> result = new List<Type>();
            Assembly[] assemblies = targetAppDomain.GetAssemblies();
            int assebliesAmount = assemblies.Length;
            for (int i = 0; i < assebliesAmount; i++)
            {
                Assembly assembly = assemblies[i];
                Type[] types = assembly.GetTypes();
                int typesAmount = types.Length;
                for (int j = 0; j < typesAmount; j++)
                {
                    Type type = types[j];
                    if (type.IsSubclassOf(targetType)) result.Add(type);
                }
            }

            return result.ToArray();
        }

        public static Rect GetEditorMainWindowPos()
        {
            Type containerWinType = AppDomain.CurrentDomain.GetAllDerivedTypes(typeof(ScriptableObject)).FirstOrDefault(t => t.Name == "ContainerWindow");
            if (containerWinType == null) throw new MissingMemberException("Can't find internal type ContainerWindow. Maybe something has changed inside Unity");
            FieldInfo showModeField = containerWinType.GetField("m_ShowMode", BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo positionProperty = containerWinType.GetProperty("position", BindingFlags.Public | BindingFlags.Instance);
            if (showModeField == null || positionProperty == null)
            {
                throw new MissingFieldException("Can't find internal fields 'm_ShowMode' or 'position'. Maybe something has changed inside Unity");
            }
            Object[] windows = Resources.FindObjectsOfTypeAll(containerWinType);
            int amount = windows.Length;
            for (int i = 0; i < amount; i++)
            {
                Object window = windows[i];
                int showmode = (int) showModeField.GetValue(window);
                if (showmode == 4) // main window
                {
                    Rect pos = (Rect) positionProperty.GetValue(window, null);
                    return pos;
                }
            }
            throw new NotSupportedException("Can't find internal main window. Maybe something has changed inside Unity");
        }

        public static void CenterOnMainWin(this EditorWindow aWin)
        {
            Rect main = GetEditorMainWindowPos();
            Rect pos = aWin.position;
            float w = (main.width - pos.width) * 0.5f;
            float h = (main.height - pos.height) * 0.5f;
            pos.x = main.x + w;
            pos.y = main.y + h;
            try { aWin.position = pos; }
            catch (Exception e)
            {
                Debug.Log("编译错误：请删除Library/ScriptAssemblies/Assembly-CSharp-Editor并重新导入Editor部分");
                Debug.Log(e);
            }
        }

        public static Texture[] GetUIAssetPreviews(GameObject[] objs)
        {
            GameObject canvasObject = new GameObject("RenderCanvas", typeof(Canvas));
            GameObject cameraObject = new GameObject("RenderCamera");

            Camera renderCamera = cameraObject.AddComponent<Camera>();
            renderCamera.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            renderCamera.clearFlags = CameraClearFlags.Color;
            renderCamera.cameraType = CameraType.Preview;
            renderCamera.cullingMask = 1 << 21;

            int amount = objs.Length;
            Texture[] textures = new Texture[amount];
            for (int i = 0; i < amount; i++)
            {
                GameObject obj = objs[i];
                GameObject clone = Object.Instantiate(obj);
                Transform cloneTransform = clone.transform;

                //如果是UGUI节点的话就要把它们放在Canvas下了
                cloneTransform.SetParent(canvasObject.transform);
                cloneTransform.localPosition = Vector3.zero;

                canvasObject.transform.position = new Vector3(-1000, -1000, -1000);
                canvasObject.layer = 21; //放在21层，摄像机也只渲染此层的，避免混入了奇怪的东西

                Transform[] all = clone.GetComponentsInChildren<Transform>();
                foreach (Transform trans in all) trans.gameObject.layer = 21;

                Bounds bounds = GetBounds(clone);
                Vector3 Min = bounds.min;
                Vector3 Max = bounds.max;

                cameraObject.transform.position = new Vector3((Max.x + Min.x) / 2f, (Max.y + Min.y) / 2f, cloneTransform.position.z - 100);
                Vector3 center = new Vector3(cloneTransform.position.x + 0.01f, (Max.y + Min.y) / 2f, cloneTransform.position.z); //+0.01f是为了去掉Unity自带的摄像机旋转角度为0的打印，太烦人了
                cameraObject.transform.LookAt(center);

                renderCamera.orthographic = true;
                float width = Max.x - Min.x;
                float height = Max.y - Min.y;
                float max_camera_size = width > height ? width : height;
                renderCamera.orthographicSize = max_camera_size / 2; //预览图要尽量少点空白

                RenderTexture texture = new RenderTexture(128, 128, 0, RenderTextureFormat.Default);
                renderCamera.targetTexture = texture;

                Undo.DestroyObjectImmediate(cameraObject);
                Undo.PerformUndo(); //不知道为什么要删掉再Undo回来后才Render得出来UI的节点，3D节点是没这个问题的，估计是Canvas创建后没那么快有效？
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

        public static bool Search(string check, string input)
        {
            if (string.IsNullOrEmpty(input)) return true;
            check = check.ToLower();
            input = input.ToLower();
            if (check.Contains(input)) return true;
            return false;
        }
    }
}