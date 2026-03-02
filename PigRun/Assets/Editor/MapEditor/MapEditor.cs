using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Map))]
public class MapEditor : Editor
{
    Map map;
    MapItem selected;
    int selectedIndex;
    bool isDragging;

    void OnEnable()
    {
        map = (Map)target;
    }

    // Inspector：基础参数 + 预制体目录编辑与拖拽导入
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        map.dataAsset = (MapData)EditorGUILayout.ObjectField("MapData", map.dataAsset, typeof(MapData), false);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("创建 MapData", GUILayout.Width(120)))
        {
            var folder = "Assets/Maps";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Maps");
            var asset = ScriptableObject.CreateInstance<MapData>();
            asset.rows = map.rows; asset.cols = map.cols; asset.cellSize = map.cellSize; asset.origin = map.origin;
            var path = AssetDatabase.GenerateUniqueAssetPath(folder + "/MapData.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            map.dataAsset = asset;
            EditorUtility.SetDirty(map);
        }
        GUI.enabled = map.dataAsset != null;
        if (GUILayout.Button("保存地图", GUILayout.Width(120)))
        {
            map.SaveToAsset(map.dataAsset);
        }
        if (GUILayout.Button("加载地图", GUILayout.Width(120)))
        {
            map.LoadFromAsset(map.dataAsset, true);
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("清空地图", GUILayout.Width(120)))
        {
            map.ClearAllItems();
            SceneView.RepaintAll();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        if (map.prefabCatalog != null && map.prefabCatalog.Count > 0)
        {
            var names = new List<string>();
            for (int i = 0; i < map.prefabCatalog.Count; i++)
            {
                var p = map.prefabCatalog[i];
                names.Add(p != null && p.prefab != null ? p.prefab.name : "None");
            }
            selectedIndex = EditorGUILayout.Popup("Active Prefab", selectedIndex, names.ToArray());
        }

        var dropRect = GUILayoutUtility.GetRect(0, 48, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, "拖拽 Prefab 或 PrefabInfo 到此添加");
        var e = Event.current;
        if (dropRect.Contains(e.mousePosition))
        {
            if (e.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                e.Use();
            }
            else if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                EnsurePrefabInfoFolder();
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is PrefabInfo pi)
                    {
                        map.prefabCatalog.Add(pi);
                        EditorUtility.SetDirty(map);
                    }
                    else if (obj is GameObject go)
                    {
                        if (!PrefabUtility.IsPartOfPrefabAsset(go)) continue;
                        var info = ScriptableObject.CreateInstance<PrefabInfo>();
                        info.prefab = go;
                        info.rows = 1;
                        info.cols = 1;
                        var path = AssetDatabase.GenerateUniqueAssetPath("Assets/MapPrefabInfos/" + go.name + "_PrefabInfo.asset");
                        AssetDatabase.CreateAsset(info, path);
                        AssetDatabase.SaveAssets();
                        map.prefabCatalog.Add(info);
                        EditorUtility.SetDirty(map);
                    }
                }
                e.Use();
                Repaint();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("PrefabCatalog 表格");
        if (map.prefabCatalog != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Prefab", GUILayout.Width(200));
            EditorGUILayout.LabelField("Rows", GUILayout.Width(60));
            EditorGUILayout.LabelField("Cols", GUILayout.Width(60));
            EditorGUILayout.LabelField("操作", GUILayout.Width(140));
            EditorGUILayout.EndHorizontal();

            int removeIndex = -1;
            for (int i = 0; i < map.prefabCatalog.Count; i++)
            {
                var pi = map.prefabCatalog[i];
                EditorGUILayout.BeginHorizontal();
                var newPrefab = (GameObject)EditorGUILayout.ObjectField(pi != null ? pi.prefab : null, typeof(GameObject), false, GUILayout.Width(200));
                if (pi != null && newPrefab != pi.prefab)
                {
                    pi.prefab = newPrefab;
                    EditorUtility.SetDirty(pi);
                }
                int newRows = pi != null ? EditorGUILayout.IntField(pi.rows, GUILayout.Width(60)) : 1;
                int newCols = pi != null ? EditorGUILayout.IntField(pi.cols, GUILayout.Width(60)) : 1;
                if (pi != null)
                {
                    newRows = Mathf.Max(1, newRows);
                    newCols = Mathf.Max(1, newCols);
                    if (newRows != pi.rows || newCols != pi.cols)
                    {
                        pi.rows = newRows;
                        pi.cols = newCols;
                        EditorUtility.SetDirty(pi);
                    }
                }
                if (GUILayout.Button("定位资产", GUILayout.Width(70)))
                {
                    if (pi != null) EditorGUIUtility.PingObject(pi);
                }
                if (GUILayout.Button("移除", GUILayout.Width(70)))
                {
                    removeIndex = i;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (removeIndex >= 0)
            {
                map.prefabCatalog.RemoveAt(removeIndex);
                EditorUtility.SetDirty(map);
            }
        }
    }

    // 尺寸计算与锚点/旋转映射，与运行时保持一致
    Vector2Int FootprintDims(PrefabInfo info, int rotIndex)
    {
        if (rotIndex % 2 == 0) return new Vector2Int(info.rows, info.cols);
        return new Vector2Int(info.cols, info.rows);
    }
    Vector2Int PivotRC(PrefabInfo info)
    {
        int pr = info.pivotRow >= 0 ? info.pivotRow : Mathf.FloorToInt((info.rows - 1) * 0.5f);
        int pc = info.pivotCol >= 0 ? info.pivotCol : Mathf.FloorToInt((info.cols - 1) * 0.5f);
        return new Vector2Int(pr, pc);
    }
    Vector2Int RotatedPivot(Vector2Int pivot, int rotIndex, Vector2Int dims0)
    {
        rotIndex = ((rotIndex % 4) + 4) % 4;
        if (rotIndex == 0) return pivot;
        if (rotIndex == 1) return new Vector2Int(pivot.y, pivot.x);
        if (rotIndex == 2) return new Vector2Int(dims0.x - 1 - pivot.x, dims0.y - 1 - pivot.y);
        return new Vector2Int(dims0.y - 1 - pivot.y, dims0.x - 1 - pivot.x);
    }
    Vector2Int StartFromPivot(Vector2Int pivotGrid, PrefabInfo info, int rotIndex)
    {
        var dims0 = new Vector2Int(info.rows, info.cols);
        var piv0 = PivotRC(info);
        var pivR = RotatedPivot(piv0, rotIndex, dims0);
        return new Vector2Int(pivotGrid.x - pivR.x, pivotGrid.y - pivR.y);
    }

    // 网格中心到世界坐标（编辑器绘制/定位）
    Vector3 GridToWorld(Vector2Int grid)
    {
        float lx = (grid.y + 0.5f) * map.cellSize + map.origin.x;
        float lz = (grid.x + 0.5f) * map.cellSize + map.origin.z;
        var local = new Vector3(lx, map.origin.y, lz);
        return map.transform.TransformPoint(local);
    }

    Vector3 FootprintWorldCenter(Vector2Int anchor, Vector2Int dims)
    {
        float lx = (anchor.y + dims.y * 0.5f) * map.cellSize + map.origin.x;
        float lz = (anchor.x + dims.x * 0.5f) * map.cellSize + map.origin.z;
        var local = new Vector3(lx, map.origin.y, lz);
        return map.transform.TransformPoint(local);
    }

    // Scene 视图拾取网格坐标（随 MapEditor transform 变化）
    bool TryGetMouseGrid(out Vector2Int cell)
    {
        cell = default;
        var e = Event.current;
        var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        var dynPlane = new Plane(map.transform.up, map.transform.TransformPoint(map.origin));
        if (!dynPlane.Raycast(ray, out float enter)) return false;
        var pt = ray.GetPoint(enter);
        var local = map.transform.InverseTransformPoint(pt) - map.origin;
        int c = Mathf.FloorToInt(local.x / map.cellSize);
        int r = Mathf.FloorToInt(local.z / map.cellSize);
        cell = new Vector2Int(r, c);
        return r >= 0 && c >= 0 && r < map.rows && c < map.cols;
    }

    Dictionary<Vector2Int, MapItem> BuildOccupancy()
    {
        var occ = new Dictionary<Vector2Int, MapItem>();
        for (int i = 0; i < map.transform.childCount; i++)
        {
            var child = map.transform.GetChild(i);
            var item = child.GetComponent<MapItem>();
            if (item == null || item.info == null) continue;
            var dims = FootprintDims(item.info, item.rotIndex);
            var start = StartFromPivot(item.gridPos, item.info, item.rotIndex);
            for (int r = 0; r < dims.x; r++)
            {
                for (int c = 0; c < dims.y; c++)
                {
                    var cell = new Vector2Int(start.x + r, start.y + c);
                    occ[cell] = item;
                }
            }
        }
        return occ;
    }

    bool InBounds(Vector2Int start, Vector2Int dims)
    {
        if (start.x < 0 || start.y < 0) return false;
        if (start.x + dims.x > map.rows) return false;
        if (start.y + dims.y > map.cols) return false;
        return true;
    }

    bool AreaFree(Vector2Int start, Vector2Int dims, Dictionary<Vector2Int, MapItem> occ, MapItem ignore)
    {
        for (int r = 0; r < dims.x; r++)
        {
            for (int c = 0; c < dims.y; c++)
            {
                var cell = new Vector2Int(start.x + r, start.y + c);
                if (occ.TryGetValue(cell, out var hit) && hit != ignore) return false;
            }
        }
        return true;
    }

    // 放置实例（Alt+左键）：越界/空间不足将提示
    void Place(Dictionary<Vector2Int, MapItem> occ, Vector2Int cell)
    {
        if (map.prefabCatalog == null || map.prefabCatalog.Count == 0) return;
        var info = map.prefabCatalog[Mathf.Clamp(selectedIndex, 0, map.prefabCatalog.Count - 1)];
        if (info == null || info.prefab == null) return;
        var dims = FootprintDims(info, 0);
        var start = StartFromPivot(cell, info, 0);
        if (!InBounds(start, dims)) { EditorApplication.Beep(); SceneView.lastActiveSceneView?.ShowNotification(new GUIContent("无法放置：越界")); return; }
        if (!AreaFree(start, dims, occ, null)) { EditorApplication.Beep(); SceneView.lastActiveSceneView?.ShowNotification(new GUIContent("无法放置：目标区域已被占用")); return; }
        var obj = PrefabUtility.InstantiatePrefab(info.prefab, map.transform) as GameObject;
        Undo.RegisterCreatedObjectUndo(obj, "Place Map Item");
        obj.transform.position = FootprintWorldCenter(start, dims);
        var mi = obj.GetComponent<MapItem>();
        if (mi == null) mi = obj.AddComponent<MapItem>();
        mi.info = info;
        mi.gridPos = cell;
        mi.rotIndex = 0;
        mi.baseRotation = obj.transform.rotation;
        selected = mi;
    }

    void TryMoveOneCell(MapItem item, Dictionary<Vector2Int, MapItem> occ, Vector2Int delta)
    {
        var target = new Vector2Int(item.gridPos.x + delta.x, item.gridPos.y + delta.y);
        var dims = FootprintDims(item.info, item.rotIndex);
        var start = StartFromPivot(target, item.info, item.rotIndex);
        if (!InBounds(start, dims)) return;
        if (!AreaFree(start, dims, occ, item)) return;
        Undo.RecordObject(item.transform, "Move Map Item");
        item.gridPos = target;
        item.transform.position = FootprintWorldCenter(start, dims);
    }

    // 旋转（Alt+左键选中后）：越界/空间不足将提示
    void TryRotate90(MapItem item, Dictionary<Vector2Int, MapItem> occ)
    {
        int nextRot = (item.rotIndex + 1) % 4;
        var dims = FootprintDims(item.info, nextRot);
        var start = StartFromPivot(item.gridPos, item.info, nextRot);
        if (!InBounds(start, dims)) { EditorApplication.Beep(); SceneView.lastActiveSceneView?.ShowNotification(new GUIContent("无法旋转：越界")); return; }
        if (!AreaFree(start, dims, occ, item)) { EditorApplication.Beep(); SceneView.lastActiveSceneView?.ShowNotification(new GUIContent("无法旋转：空间不足")); return; }
        Undo.RecordObject(item.transform, "Rotate Map Item");
        item.rotIndex = nextRot;
        item.transform.rotation = Quaternion.AngleAxis(item.rotIndex * 90f, Vector3.up) * item.baseRotation;
        item.transform.position = FootprintWorldCenter(start, dims);
    }

    // Scene 视图交互：Alt 放置/旋转，拖拽单格移动，Alt+Delete 删除
    /// <summary>
    /// 处理 Scene 视图中的全部交互：
    /// 1. Alt+左键：放置新物件 / 选中已有物件 / 旋转已选物件  
    /// 2. 左键拖拽：单格移动已选物件  
    /// 3. Alt+Delete：删除已选物件  
    /// 4. 所有操作均伴随 Undo、日志输出与 Scene 视图重绘  
    /// </summary>
    void HandleInput()
    {
        // 如果当前有控件抢占输入（如拖动滑条），则直接退出，避免冲突
        var e = Event.current;
        if (GUIUtility.hotControl != 0) return;

        // 实时构建占用表，用于后续碰撞检测
        var occ = BuildOccupancy();

        // 如果鼠标不在有效网格内，则无法继续交互
        if (!TryGetMouseGrid(out var cell)) return;

        // 预判断两种需要抢占输入的场景：Alt+左键 或 拖拽已选物件
        bool willAltClick = e.button == 0 && e.alt && (e.type == EventType.MouseDown || e.type == EventType.MouseUp);
        bool willDrag = e.type == EventType.MouseDrag && e.button == 0 && selected != null;
        if (willAltClick || willDrag)
        {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(id);   // 让当前 Editor 窗口接管后续鼠标事件
        }

        // ---------- Alt + Delete 删除 ----------
        if (e.type == EventType.KeyDown && selected != null && e.alt && e.keyCode == KeyCode.Delete)
        {
            UnityEditor.Selection.activeObject = null;
            Undo.DestroyObjectImmediate(selected.gameObject);   // 支持 Ctrl-Z 回退
            selected = null;
            e.Use();                    // 标记事件已处理
            SceneView.RepaintAll();     // 立即重绘 Scene 视图
            return;
        }

        // ---------- 鼠标按下（仅做标记） ----------
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (e.alt)
            {
                e.Use();                // 先占用事件，避免 Unity 默认行为
                SceneView.RepaintAll();
            }
        }

        // ---------- 鼠标抬起：核心逻辑 ----------
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            if (isDragging)
            {
                isDragging = false;
                e.Use();
                SceneView.RepaintAll();
                return;
            }
            // 情况 A：鼠标下已有物件
            if (occ.TryGetValue(cell, out var item))
            {
                if (e.alt)
                {
                    // Alt+左键：若已选中则旋转，否则仅选中
                    if (selected == item)
                    {
                        TryRotate90(selected, occ);
                        var wp = GridToWorld(cell);
                        Debug.Log($"旋转实例 网格=({cell.x},{cell.y}) 世界=({wp.x:F2},{wp.y:F2},{wp.z:F2}) 名称={selected.gameObject.name}");
                    }
                    else
                    {
                        selected = item;
                        var wp = GridToWorld(cell);
                        Debug.Log($"选中实例 网格=({cell.x},{cell.y}) 世界=({wp.x:F2},{wp.y:F2},{wp.z:F2}) 名称={item.gameObject.name}");
                    }
                    e.Use();
                    SceneView.RepaintAll();
                }
                else
                {
                    // 普通左键：仅选中
                    selected = item;
                    var wp = GridToWorld(cell);
                    Debug.Log($"选中实例 网格=({cell.x},{cell.y}) 世界=({wp.x:F2},{wp.y:F2},{wp.z:F2}) 名称={item.gameObject.name}");
                    e.Use();
                    SceneView.RepaintAll();
                }
            }
            // 情况 B：空格处 + Alt → 放置新物件
            else if (e.alt)
            {
                Place(occ, cell);
                var wp = GridToWorld(cell);
                Debug.Log($"创建实例 网格=({cell.x},{cell.y}) 世界=({wp.x:F2},{wp.y:F2},{wp.z:F2})");
                e.Use();
                SceneView.RepaintAll();
            }
        }

        // ---------- 拖拽移动：单格步进 ----------
        else if (e.type == EventType.MouseDrag && e.button == 0 && selected != null)
        {
            // 计算鼠标网格与选中物件当前网格的差值
            var diff = new Vector2Int(cell.x - selected.gridPos.x, cell.y - selected.gridPos.y);
            Vector2Int step = Vector2Int.zero;
            // 优先横向移动，其次纵向
            if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y)) step = new Vector2Int(diff.x > 0 ? 1 : -1, 0);
            else if (Mathf.Abs(diff.y) > 0) step = new Vector2Int(0, diff.y > 0 ? 1 : -1);

            if (step != Vector2Int.zero)
            {
                isDragging = true;
                var wpDrag = GridToWorld(cell);
                Debug.Log($"拖拽移动 选中={selected.gameObject.name} 目标网格=({cell.x},{cell.y}) 世界=({wpDrag.x:F2},{wpDrag.y:F2},{wpDrag.z:F2}) 步进=({step.x},{step.y})");
                TryMoveOneCell(selected, occ, step); // 内部已做 Undo 记录
                e.Use();
                SceneView.RepaintAll();
            }
        }
    }

    // 网格绘制：随 MapEditor 的 transform 同步
    void DrawGrid()
    {
        var prev = Handles.matrix;
        Handles.matrix = map.transform.localToWorldMatrix;
        Handles.color = map.gridColor;
        for (int r = 0; r <= map.rows; r++)
        {
            var a = map.origin + new Vector3(0f, 0f, r * map.cellSize);
            var b = map.origin + new Vector3(map.cols * map.cellSize, 0f, r * map.cellSize);
            Handles.DrawAAPolyLine(map.gridLineWidth, a, b);
        }
        for (int c = 0; c <= map.cols; c++)
        {
            var a = map.origin + new Vector3(c * map.cellSize, 0f, 0f);
            var b = map.origin + new Vector3(c * map.cellSize, 0f, map.rows * map.cellSize);
            Handles.DrawAAPolyLine(map.gridLineWidth, a, b);
        }
        Handles.matrix = prev;
    }

    // 选中框绘制：统一线宽与颜色
    void DrawSelected()
    {
        if (selected == null || selected.info == null) return;
        var prev = Handles.matrix;
        Handles.matrix = map.transform.localToWorldMatrix;
        var dims = FootprintDims(selected.info, selected.rotIndex);
        var anchor = StartFromPivot(selected.gridPos, selected.info, selected.rotIndex);
        var min = map.origin + new Vector3(anchor.y * map.cellSize, 0f, anchor.x * map.cellSize);
        var max = map.origin + new Vector3((anchor.y + dims.y) * map.cellSize, 0f, (anchor.x + dims.x) * map.cellSize);
        var selCol = map.selectedUseComplementary ? new Color(1f - map.gridColor.r, 1f - map.gridColor.g, 1f - map.gridColor.b, 1f) : map.selectedColor;
        Handles.color = selCol;
        Handles.DrawAAPolyLine(map.gridLineWidth, min, new Vector3(max.x, 0f, min.z));
        Handles.DrawAAPolyLine(map.gridLineWidth, new Vector3(max.x, 0f, min.z), max);
        Handles.DrawAAPolyLine(map.gridLineWidth, max, new Vector3(min.x, 0f, max.z));
        Handles.DrawAAPolyLine(map.gridLineWidth, new Vector3(min.x, 0f, max.z), min);
        Handles.matrix = prev;
    }

    // 主入口：处理交互并绘制
    void OnSceneGUI()
    {
        HandleInput();
        DrawGrid();
        DrawSelected();
    }

    void EnsurePrefabInfoFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/MapPrefabInfos"))
        {
            AssetDatabase.CreateFolder("Assets", "MapPrefabInfos");
        }
    }
}

