# UISelectTool 

## Unity UGUI的UGUI选择工具

为解决时常选择不到的UI所写的工具，在面对UI面板复杂的情况下能更快的检索UI

## 使用

右键场景中的UI则会弹出一个窗口并显示你所点击到的所有UI（并且按照层级进行排序）

![demo](./Doc/demonstration.gif)

## 如何接入

* 在UISelectSceneUtility脚本中修改GetUIRoot函数
* 在UISelectSceneUtility脚本中修改OnSceneGUI函数，限制在UI编辑场景中调用
* 在UISelectSetting脚本中可修改参数，可改为继承ScriptableObject将设置项抛出给使用者调
* 该工具只能在场景中使用，如果想在预制模式下请自行实现
