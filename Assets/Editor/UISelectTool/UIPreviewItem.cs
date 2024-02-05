using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIEditor
{
    [Serializable]
    public class UIPreviewItem
    {
        public GameObject ui;
        public Texture preview;
        public List<int> uiIndex;
    }
}