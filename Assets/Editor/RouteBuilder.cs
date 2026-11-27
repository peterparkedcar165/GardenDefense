using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Tools > Garden Defense > Route Builder (Tommy, 2026-11-27): builds a level's route from the road painted on its
// Path map, so nobody places waypoints by hand. it finds where the road leaves the map, you pick which of those
// ends is the exit (every other one is an entrance), and it walks the road from the exit back to each entrance:
// straight on through crossings, splitting at T junctions. Build route then sets, with undo:
//   the main path (PathManager's waypoints): from where the last lanes merge (with one entrance, from the
//     entrance) to a cell past the exit, one waypoint per turn
//   one spawn entry per entrance (the SpawnManager's spawnEntries): its spawn point a cell outside the map and
//     its lead in waypoints, one per turn, up to the main path
//   one wave start arrow per entrance (StartingPoint), on the road's edge cell, pointing into the map
//   and, ticked, the off map walls (the Obstacle object's boxes), rebuilt round the map with a one cell gap
//     wherever a road leaves it (as the level scripts drew them for Levels 6 to 10)
// roads are one cell wide here. while the window is open the scene view shows the route it would build: the main
// path orange, the lanes blue, the ends lettered
public class RouteBuilder : EditorWindow
{
    private const string ArrowPrefabPath = "Assets/Prefabs/UI/StartingPoint.prefab";
    private const string ArrowSprites = "Assets/Sprites/UI/arrow";
    private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    private static readonly Color MainColor = new Color(1f, 0.6f, 0.1f);
    private static readonly Color LaneColor = new Color(0.3f, 0.8f, 1f);

    [MenuItem("Tools/Garden Defense/Route Builder")]
    public static void Open() => GetWindow<RouteBuilder>("Route Builder");

    private class End
    {
        public Vector2Int cell, outward;
        public string where;
    }

    private class Plan
    {
        public End exit;
        public readonly List<End> entrances = new List<End>();
        // cells, its last one past the map
        public readonly List<Vector2Int> main = new List<Vector2Int>();
        // per entrance: its spawn cell past the map, then its lead in turns
        public readonly List<List<Vector2Int>> lanes = new List<List<Vector2Int>>();
        public readonly List<string> errors = new List<string>();
        public readonly List<string> notes = new List<string>();
        public bool Ok => errors.Count == 0 && entrances.Count > 0;
    }

    // the map: its cells (on the tilemaps' grid) and where each one's placement tile sits in the world
    private Tilemap _path;
    private Vector2Int _min, _max;
    private Vector3 _origin;
    private readonly HashSet<Vector2Int> _road = new HashSet<Vector2Int>();
    private readonly List<End> _ends = new List<End>();
    private Vector2Int? _exitCell;
    private Plan _plan;
    private string _problem;
    private bool _walls = true;
    private bool _dirty = true;
    private Vector2 _scroll;

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnScene;
        Tilemap.tilemapTileChanged += OnTiles;
        EditorApplication.hierarchyChanged += MarkDirty;
        EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
        MarkDirty();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnScene;
        Tilemap.tilemapTileChanged -= OnTiles;
        EditorApplication.hierarchyChanged -= MarkDirty;
        EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
        SceneView.RepaintAll();
    }

    private void OnTiles(Tilemap map, Tilemap.SyncTile[] tiles)
    {
        if (map != null && map.name == "Path") MarkDirty();
    }

    private void OnSceneChanged(Scene from, Scene to)
    {
        _exitCell = null;
        MarkDirty();
    }

    private void MarkDirty()
    {
        _dirty = true;
        Repaint();
        SceneView.RepaintAll();
    }

    // ---------- reading the map ----------

    private static Tilemap FindMap(string name)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (Tilemap map in FindObjectsByType<Tilemap>(FindObjectsInactive.Include))
            if (map.name == name && map.gameObject.scene == scene) return map;
        return null;
    }

    private bool Inside(Vector2Int c) => c.x >= _min.x && c.x <= _max.x && c.y >= _min.y && c.y <= _max.y;

    private Vector3 World(Vector2Int c) => _origin + new Vector3(c.x - _min.x, c.y - _min.y, 0f);

    private int Degree(Vector2Int c)
    {
        int n = 0;
        foreach (Vector2Int d in Dirs) if (_road.Contains(c + d)) n++;
        return n;
    }

    private void Rescan()
    {
        _dirty = false;
        _plan = null;
        _problem = null;
        _road.Clear();
        _ends.Clear();
        if (EditorApplication.isPlayingOrWillChangePlaymode) { _problem = "Stop play mode to build a route."; return; }
        _path = FindMap("Path");
        Tilemap ground = FindMap("Ground");
        if (_path == null || ground == null) { _problem = "Open a level: this scene has no Path or Ground tilemap."; return; }

        // the placement grid, as DaytimeGridManager lays it (GridToWorld), or else the Ground map's painted cells
        DaytimeGridManager grid = FindAnyObjectByType<DaytimeGridManager>();
        if (grid != null && grid.rows > 0 && grid.columns > 0)
        {
            _origin = grid.transform.position + new Vector3(-(grid.columns + 4) / 2f, -(grid.rows - 1) / 2f, 0f);
            Vector3Int c0 = ground.WorldToCell(_origin);
            _min = new Vector2Int(c0.x, c0.y);
            _max = _min + new Vector2Int(grid.columns - 1, grid.rows - 1);
        }
        else
        {
            bool any = false;
            foreach (Vector3Int p in ground.cellBounds.allPositionsWithin)
            {
                if (!ground.HasTile(p)) continue;
                Vector2Int c = new Vector2Int(p.x, p.y);
                if (!any) { _min = _max = c; any = true; continue; }
                _min = Vector2Int.Min(_min, c);
                _max = Vector2Int.Max(_max, c);
            }
            if (!any) { _problem = "Paint the Ground map first: it gives the map's size."; return; }
            _origin = ground.GetCellCenterWorld(new Vector3Int(_min.x, _min.y, 0));
        }

        foreach (Vector3Int p in _path.cellBounds.allPositionsWithin)
        {
            Vector2Int c = new Vector2Int(p.x, p.y);
            if (Inside(c) && _path.HasTile(p)) _road.Add(c);
        }

        // a road end at the map's edge: one road neighbour, and straight on from it is off the map
        var deadEnds = new List<Vector2Int>();
        foreach (Vector2Int c in _road)
        {
            if (Degree(c) != 1) continue;
            Vector2Int from = Dirs.Select(d => c + d).First(n => _road.Contains(n));
            Vector2Int outward = c - from;
            if (Inside(c + outward)) { deadEnds.Add(c); continue; }
            _ends.Add(new End { cell = c, outward = outward, where = Where(c, outward) });
        }
        _ends.Sort((a, b) => string.CompareOrdinal(a.where, b.where));

        if (_exitCell == null || _ends.All(e => e.cell != _exitCell.Value)) _exitCell = GuessExit();
        _plan = MakePlan(deadEnds);
    }

    private string Where(Vector2Int c, Vector2Int outward)
    {
        int column = c.x - _min.x + 1, rowFromTop = _max.y - c.y + 1;
        if (outward == Vector2Int.left) return $"left edge, row {rowFromTop} from the top";
        if (outward == Vector2Int.right) return $"right edge, row {rowFromTop} from the top";
        if (outward == Vector2Int.up) return $"top edge, column {column} from the left";
        return $"bottom edge, column {column} from the left";
    }

    // the end nearest the route's current last waypoint, so a level keeps its exit when its road is redrawn
    private Vector2Int? GuessExit()
    {
        if (_ends.Count == 0) return null;
        PathManager path = FindAnyObjectByType<PathManager>();
        Transform last = path != null && path.waypoints != null ? path.waypoints.LastOrDefault(t => t != null) : null;
        if (last == null) return _ends[0].cell;
        return _ends.OrderBy(e => Vector2.Distance(World(e.cell + e.outward), last.position)).First().cell;
    }

    // ---------- walking the road ----------

    private Plan MakePlan(List<Vector2Int> deadEnds)
    {
        var plan = new Plan();
        foreach (Vector2Int c in deadEnds)
            plan.errors.Add($"The road stops inside the map at {Describe(c)}. Every road end must reach the map's edge.");
        if (_ends.Count == 0)
        {
            plan.errors.Add("No road reaches the map's edge yet. Paint the road on the Path map out to the edge, where insects come in and where they leave.");
            return plan;
        }
        plan.exit = _ends.First(e => e.cell == _exitCell.Value);
        if (_ends.Count == 1) { plan.errors.Add("The road reaches the edge only once: it needs an entrance and an exit."); return plan; }

        // walk back from the exit: every walk that reaches another end is one lane, exit first
        var walks = new List<List<Vector2Int>>();
        var visits = new Dictionary<Vector2Int, int>();
        var todo = new Stack<(List<Vector2Int> cells, Vector2Int prev)>();
        todo.Push((new List<Vector2Int> { plan.exit.cell }, plan.exit.cell + plan.exit.outward));
        int guard = 0;
        while (todo.Count > 0 && plan.errors.Count == 0)
        {
            var (cells, prev) = todo.Pop();
            while (true)
            {
                if (++guard > 100000) { plan.errors.Add("The road couldn't be followed (it loops)."); break; }
                Vector2Int cur = cells[cells.Count - 1];
                visits.TryGetValue(cur, out int seen);
                visits[cur] = seen + 1;
                int degree = Degree(cur);
                if (seen > 0 && degree != 4 || seen > 1)
                {
                    plan.errors.Add($"The road runs in a loop through {Describe(cur)}. A route can cross itself straight over, but not join back into itself.");
                    break;
                }
                var next = Dirs.Select(d => cur + d).Where(n => _road.Contains(n) && n != prev).ToList();
                if (next.Count == 0)
                {
                    if (cells.Count > 1) walks.Add(cells);
                    break;
                }
                Vector2Int step;
                if (degree == 4) step = cur + (cur - prev);   // a crossing: straight over
                else if (degree == 3)
                {
                    // a T junction: seen from the exit the road splits here, so both other arms are walked
                    foreach (Vector2Int arm in next.Skip(1))
                        todo.Push((new List<Vector2Int>(cells) { arm }, cur));
                    step = next[0];
                }
                else step = next[0];
                prev = cur;
                cells.Add(step);
            }
        }
        if (plan.errors.Count > 0) return plan;

        foreach (End end in _ends)
        {
            if (end == plan.exit) continue;
            List<Vector2Int> walk = walks.FirstOrDefault(w => w[w.Count - 1] == end.cell);
            if (walk == null) { plan.notes.Add($"The road end at the {end.where} doesn't join the exit's road, so it's left out."); continue; }
            plan.entrances.Add(end);
        }
        int unreached = _road.Count(c => !visits.ContainsKey(c));
        if (unreached > 0) plan.notes.Add($"{unreached} road cells aren't joined to the exit's road and are left out.");
        if (plan.entrances.Count == 0) { plan.errors.Add("No entrance joins the exit's road."); return plan; }

        // walks from the exit to each entrance share a stretch next to the exit: that stretch is the main path
        var lanesFromExit = plan.entrances.Select(e => walks.First(w => w[w.Count - 1] == e.cell)).ToList();
        int shared = lanesFromExit.Min(w => w.Count);
        for (int i = 0; i < shared; i++)
            if (lanesFromExit.Any(w => w[i] != lanesFromExit[0][i])) { shared = i; break; }
        if (plan.entrances.Count > 1) shared = Mathf.Max(1, shared);

        var mainCells = lanesFromExit[0].Take(shared).Reverse().ToList();
        mainCells.Add(plan.exit.cell + plan.exit.outward);
        plan.main.AddRange(Turns(mainCells, true));

        foreach (var (walk, end) in lanesFromExit.Zip(plan.entrances, (w, e) => (w, e)))
        {
            var cells = new List<Vector2Int> { end.cell + end.outward };
            cells.AddRange(walk.Skip(shared).Reverse());
            cells.Add(mainCells[0]);
            var lane = Turns(cells, false);
            plan.lanes.Add(lane);
        }
        return plan;
    }

    // the first point, every cell where the line turns, and (keepLast) the last point
    private static List<Vector2Int> Turns(List<Vector2Int> cells, bool keepLast)
    {
        var turns = new List<Vector2Int> { cells[0] };
        for (int i = 1; i + 1 < cells.Count; i++)
            if (cells[i] - cells[i - 1] != cells[i + 1] - cells[i]) turns.Add(cells[i]);
        if (keepLast && cells.Count > 1) turns.Add(cells[cells.Count - 1]);
        return turns;
    }

    private string Describe(Vector2Int c) => $"column {c.x - _min.x + 1}, row {_max.y - c.y + 1} from the top";

    // ---------- the window ----------

    private void OnGUI()
    {
        if (_dirty) Rescan();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.LabelField("Route from the painted road", EditorStyles.boldLabel);
        if (_problem != null)
        {
            EditorGUILayout.HelpBox(_problem, MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        if (_ends.Count > 0)
        {
            int at = _ends.FindIndex(e => e.cell == _exitCell);
            int pick = EditorGUILayout.Popup(new GUIContent("Exit", "where insects leave the map; every other road end is an entrance"),
                Mathf.Max(0, at), _ends.Select(e => e.where).ToArray());
            if (pick != at) { _exitCell = _ends[pick].cell; _plan = null; _dirty = true; Repaint(); SceneView.RepaintAll(); }
        }

        if (_plan != null)
        {
            for (int i = 0; i < _plan.entrances.Count; i++)
                EditorGUILayout.LabelField($"Entrance {Letters[i % Letters.Length]}", $"{_plan.entrances[i].where}, {_plan.lanes[i].Count - 1} lead in waypoints");
            if (_plan.main.Count > 0) EditorGUILayout.LabelField("Main path", $"{_plan.main.Count} waypoints");
            foreach (string e in _plan.errors) EditorGUILayout.HelpBox(e, MessageType.Warning);
            foreach (string n in _plan.notes) EditorGUILayout.HelpBox(n, MessageType.Info);
        }

        EditorGUILayout.Space();
        _walls = EditorGUILayout.ToggleLeft(new GUIContent("Rebuild the off map walls",
            "replaces every box on the Obstacle object with walls round the map that leave a one cell gap wherever a road leaves it"), _walls);
        using (new EditorGUI.DisabledScope(_plan == null || !_plan.Ok))
            if (GUILayout.Button("Build route", GUILayout.Height(28))) Build();
        if (GUILayout.Button("Refresh")) MarkDirty();
        EditorGUILayout.EndScrollView();
    }

    private void OnScene(SceneView view)
    {
        if (_dirty) Rescan();
        if (_plan == null || _plan.exit == null || _plan.main.Count == 0) return;
        var label = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
        Handles.color = MainColor;
        Handles.DrawAAPolyLine(5f, _plan.main.Select(World).ToArray());
        foreach (Vector2Int c in _plan.main) Handles.DrawSolidDisc(World(c), Vector3.forward, 0.12f);
        for (int i = 0; i < _plan.lanes.Count; i++)
        {
            Handles.color = LaneColor;
            var points = _plan.lanes[i].Select(World).ToList();
            points.Add(World(_plan.main[0]));
            Handles.DrawAAPolyLine(4f, points.ToArray());
            foreach (Vector3 p in points.Take(points.Count - 1)) Handles.DrawSolidDisc(p, Vector3.forward, 0.1f);
            label.normal.textColor = LaneColor;
            Handles.Label(World(_plan.entrances[i].cell) + new Vector3(-0.15f, 0.35f, 0f), Letters[i % Letters.Length].ToString(), label);
        }
        label.normal.textColor = MainColor;
        Handles.Label(World(_plan.exit.cell) + new Vector3(-0.3f, 0.35f, 0f), "EXIT", label);
    }

    // ---------- writing the route ----------

    private readonly HashSet<Transform> _taken = new HashSet<Transform>();

    private void Build()
    {
        Rescan();
        if (_plan == null || !_plan.Ok) return;
        PathManager path = FindAnyObjectByType<PathManager>();
        SpawnManager spawns = FindAnyObjectByType<SpawnManager>();
        if (path == null || spawns == null)
        {
            EditorUtility.DisplayDialog("Route Builder", "This scene needs its Path (PathManager) and SpawnPoint (SpawnManager) objects.", "OK");
            return;
        }
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build route");
        int group = Undo.GetCurrentGroup();
        _taken.Clear();
        WriteMain(path);
        WriteLanes(spawns);
        WriteArrows();
        if (_walls) WriteWalls();
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        MarkDirty();
    }

    private void Place(Transform t, Vector2Int cell)
    {
        Undo.RecordObject(t, "Build route");
        Vector3 p = World(cell);
        t.position = new Vector3(p.x, p.y, t.position.z);
        PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        _taken.Add(t);
    }

    private static Transform NewObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Build route");
        if (parent != null) Undo.SetTransformParent(go.transform, parent, "Build route");
        return go.transform;
    }

    // a waypoint no longer used: gone, unless it belongs to the prefab it sits in (the Path prefab's own three),
    // which is left where the route ends
    private void Retire(Transform t, Vector2Int parkAt)
    {
        if (t == null || _taken.Contains(t)) return;
        GameObject go = t.gameObject;
        if (PrefabUtility.IsPartOfPrefabInstance(go) && !PrefabUtility.IsAddedGameObjectOverride(go))
        {
            Undo.RecordObject(t, "Build route");
            Vector3 p = World(parkAt);
            t.position = new Vector3(p.x, p.y, t.position.z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            return;
        }
        Undo.DestroyObjectImmediate(go);
    }

    private void WriteMain(PathManager path)
    {
        var pool = new List<Transform>();
        foreach (Transform t in path.waypoints ?? new Transform[0]) if (t != null && !pool.Contains(t)) pool.Add(t);
        foreach (Transform t in path.transform) if (t.name.StartsWith("Waypoint") && !pool.Contains(t)) pool.Add(t);
        var used = new Transform[_plan.main.Count];
        for (int i = 0; i < used.Length; i++)
        {
            used[i] = i < pool.Count ? pool[i] : NewObject("Waypoint" + (i + 1), path.transform);
            Place(used[i], _plan.main[i]);
        }
        Undo.RecordObject(path, "Build route");
        path.waypoints = used;
        PrefabUtility.RecordPrefabInstancePropertyModifications(path);
        for (int i = used.Length; i < pool.Count; i++) Retire(pool[i], _plan.main[_plan.main.Count - 1]);
    }

    private void WriteLanes(SpawnManager spawns)
    {
        SpawnEntry[] old = spawns.spawnEntries ?? new SpawnEntry[0];
        Transform leadRoot = null;
        var entries = new SpawnEntry[_plan.lanes.Count];
        for (int k = 0; k < entries.Length; k++)
        {
            List<Vector2Int> lane = _plan.lanes[k];
            SpawnEntry was = k < old.Length ? old[k] : null;
            string letter = Letters[k % Letters.Length].ToString();
            Transform spawn = was != null && was.spawnPoint != null && !_taken.Contains(was.spawnPoint) ? was.spawnPoint
                : k == 0 && !_taken.Contains(spawns.transform) ? spawns.transform : NewObject("SpawnPoint" + letter, null);
            Place(spawn, lane[0]);
            Transform[] wasLeads = was?.leadInWaypoints ?? new Transform[0];
            var leads = new Transform[lane.Count - 1];
            for (int i = 0; i < leads.Length; i++)
            {
                Transform t = i < wasLeads.Length && wasLeads[i] != null && !_taken.Contains(wasLeads[i]) ? wasLeads[i] : null;
                if (t == null)
                {
                    if (leadRoot == null) leadRoot = FindRoot("LeadInWaypoints") ?? NewObject("LeadInWaypoints", null);
                    t = NewObject($"Lane{letter}_{i + 1}", leadRoot);
                }
                Place(t, lane[i + 1]);
                leads[i] = t;
            }
            entries[k] = new SpawnEntry { spawnPoint = spawn, leadInWaypoints = leads };
        }
        Undo.RecordObject(spawns, "Build route");
        spawns.spawnEntries = entries;
        PrefabUtility.RecordPrefabInstancePropertyModifications(spawns);

        // the old spawn points and lead ins no lane uses now (never the spawn manager's own object)
        Vector2Int park = _plan.main[0];
        foreach (SpawnEntry was in old)
        {
            if (was == null) continue;
            if (was.spawnPoint != null && was.spawnPoint != spawns.transform) Retire(was.spawnPoint, park);
            foreach (Transform t in was.leadInWaypoints ?? new Transform[0]) Retire(t, park);
        }
    }

    private static Transform FindRoot(string name)
    {
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            if (go.name == name) return go.transform;
        return null;
    }

    private static Sprite ArrowSprite(Vector2Int into)
    {
        string name = into == Vector2Int.right ? "Right" : into == Vector2Int.left ? "Left" : into == Vector2Int.up ? "Up" : "Down";
        return AssetDatabase.LoadAllAssetsAtPath(ArrowSprites + name + ".png").OfType<Sprite>().FirstOrDefault();
    }

    private void WriteArrows()
    {
        Scene scene = SceneManager.GetActiveScene();
        var arrows = FindObjectsByType<WaveStartArrow>(FindObjectsInactive.Include)
            .Where(a => a.gameObject.scene == scene).OrderBy(a => a.transform.GetSiblingIndex()).ToList();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPrefabPath);
        for (int k = 0; k < _plan.entrances.Count; k++)
        {
            End end = _plan.entrances[k];
            Transform arrow;
            if (k < arrows.Count) arrow = arrows[k].transform;
            else if (prefab != null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                Undo.RegisterCreatedObjectUndo(go, "Build route");
                arrow = go.transform;
            }
            else break;
            Place(arrow, end.cell);
            SpriteRenderer sr = arrow.GetComponent<SpriteRenderer>();
            Sprite sprite = ArrowSprite(-end.outward);
            if (sr != null && sprite != null)
            {
                Undo.RecordObject(sr, "Build route");
                sr.sprite = sprite;
                PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
            }
        }
        for (int k = _plan.entrances.Count; k < arrows.Count; k++)
        {
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(arrows[k].gameObject);
            Undo.DestroyObjectImmediate(root != null && root.transform.parent == null ? root : arrows[k].gameObject);
        }
    }

    // walls round the map, fully off it (a wall over a placement tile would steal its clicks): left 0.7 wide,
    // right 1.5, top 1, bottom 0.9, each 0.1 off the map's edge, with a one cell gap wherever a road leaves
    private void WriteWalls()
    {
        Transform obstacle = FindRoot("Obstacle");
        if (obstacle == null) return;
        Vector3 lo = World(_min) - new Vector3(0.5f, 0.5f, 0f), hi = World(_max) + new Vector3(0.5f, 0.5f, 0f);
        float ox0 = lo.x - 0.8f, ox1 = hi.x + 1.6f, oy0 = lo.y - 1f, oy1 = hi.y + 1.1f;
        var gaps = new Dictionary<Vector2Int, List<float>>();
        foreach (Vector2Int d in Dirs) gaps[d] = new List<float>();
        foreach (End end in _plan.entrances.Append(_plan.exit))
        {
            Vector3 w = World(end.cell + end.outward);
            gaps[end.outward].Add(end.outward.x != 0 ? w.y : w.x);
        }
        var rects = new List<Rect>();
        void Strip(float from, float to, List<float> gs, System.Func<float, float, Rect> make)
        {
            float cur = from;
            foreach (float g in gs.OrderBy(v => v))
            {
                if (g - 0.5f > cur) rects.Add(make(cur, g - 0.5f));
                cur = Mathf.Max(cur, g + 0.5f);
            }
            if (to > cur) rects.Add(make(cur, to));
        }
        Strip(oy0, oy1, gaps[Vector2Int.left], (a, b) => Rect.MinMaxRect(ox0, a, lo.x - 0.1f, b));
        Strip(oy0, oy1, gaps[Vector2Int.right], (a, b) => Rect.MinMaxRect(hi.x + 0.1f, a, ox1, b));
        Strip(ox0, ox1, gaps[Vector2Int.up], (a, b) => Rect.MinMaxRect(a, hi.y + 0.1f, b, oy1));
        Strip(ox0, ox1, gaps[Vector2Int.down], (a, b) => Rect.MinMaxRect(a, oy0, b, lo.y - 0.1f));

        var boxes = obstacle.GetComponents<BoxCollider2D>().ToList();
        for (int i = 0; i < rects.Count; i++)
        {
            BoxCollider2D box = i < boxes.Count ? boxes[i] : Undo.AddComponent<BoxCollider2D>(obstacle.gameObject);
            Undo.RecordObject(box, "Build route");
            box.offset = rects[i].center - (Vector2)obstacle.position;
            box.size = rects[i].size;
            PrefabUtility.RecordPrefabInstancePropertyModifications(box);
        }
        for (int i = rects.Count; i < boxes.Count; i++) Undo.DestroyObjectImmediate(boxes[i]);
    }
}
