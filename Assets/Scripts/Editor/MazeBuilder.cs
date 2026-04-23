using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MazeBuilder : EditorWindow
{
    // Середньо-великий — гравцю є де блукати, але не втомлює
    private int width = 45;
    private int height = 35;
    private float cellSize = 4f;
    private int seed = 42;

    // Prefabs
    private GameObject floorPrefab;
    private GameObject[] floorVariants;
    private GameObject[] wallVariants;
    private GameObject ceilingPrefab;
    private GameObject doorPrefab;
    private GameObject wallDoorPrefab;
    private GameObject cornerPrefab;
    private GameObject columnPrefab;

    private GameObject candlePrefab;
    private GameObject torchPrefab;
    private GameObject[] furniturePrefabs;
    private GameObject[] smallDecorPrefabs;
    private GameObject statuePrefab;
    private GameObject chestPrefab;

    private float torchChance = 0.18f;
    private float decorChance = 0.08f;
    private float furnitureChance = 0.1f;
    private int maxChests = 12;

    private bool[,] maze;
    private Vector2Int startPos;
    private Vector2Int endPos;

    // Room centers
    private Vector2Int puzzleRoom1;
    private Vector2Int puzzleRoom2;

    // Door boundaries (unordered cell pairs)
    private struct DoorInfo
    {
        public Vector2Int a, b;
        public string name;
        public bool locked;
    }
    private List<DoorInfo> doorPlacements = new List<DoorInfo>();
    private HashSet<ulong> doorBoundarySet = new HashSet<ulong>();

    private List<Vector2Int> deadEnds = new List<Vector2Int>();

    private const int ROOM_SIZE = 5;

    [MenuItem("Tools/Maze Builder")]
    public static void ShowWindow() => GetWindow<MazeBuilder>("Maze Builder");

    Vector2 scroll;
    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        GUILayout.Label("Maze Settings", EditorStyles.boldLabel);
        width = EditorGUILayout.IntField("Width (odd, >=29)", width);
        height = EditorGUILayout.IntField("Height (odd, >=15)", height);
        cellSize = EditorGUILayout.FloatField("Cell Size (m)", cellSize);
        seed = EditorGUILayout.IntField("Seed (0 = random)", seed);

        GUILayout.Space(10);
        GUILayout.Label("Core Prefabs", EditorStyles.boldLabel);
        floorPrefab = (GameObject)EditorGUILayout.ObjectField("Floor (main)", floorPrefab, typeof(GameObject), false);
        ceilingPrefab = (GameObject)EditorGUILayout.ObjectField("Ceiling", ceilingPrefab, typeof(GameObject), false);
        doorPrefab = (GameObject)EditorGUILayout.ObjectField("Door", doorPrefab, typeof(GameObject), false);
        wallDoorPrefab = (GameObject)EditorGUILayout.ObjectField("Wall with Door", wallDoorPrefab, typeof(GameObject), false);
        cornerPrefab = (GameObject)EditorGUILayout.ObjectField("Corner", cornerPrefab, typeof(GameObject), false);
        columnPrefab = (GameObject)EditorGUILayout.ObjectField("Column", columnPrefab, typeof(GameObject), false);

        GUILayout.Space(10);
        DrawArray(ref wallVariants, "Wall Variants");

        GUILayout.Space(10);
        DrawArray(ref floorVariants, "Floor Variants");

        GUILayout.Space(10);
        GUILayout.Label("Lighting", EditorStyles.boldLabel);
        candlePrefab = (GameObject)EditorGUILayout.ObjectField("Candle", candlePrefab, typeof(GameObject), false);
        torchPrefab = (GameObject)EditorGUILayout.ObjectField("Wall Torch", torchPrefab, typeof(GameObject), false);

        GUILayout.Space(10);
        GUILayout.Label("Furniture & Decor", EditorStyles.boldLabel);
        statuePrefab = (GameObject)EditorGUILayout.ObjectField("Statue (puzzle center)", statuePrefab, typeof(GameObject), false);
        chestPrefab = (GameObject)EditorGUILayout.ObjectField("Chest (dead ends)", chestPrefab, typeof(GameObject), false);
        DrawArray(ref furniturePrefabs, "Furniture");
        DrawArray(ref smallDecorPrefabs, "Small Decor");

        GUILayout.Space(10);
        torchChance = EditorGUILayout.Slider("Torch Chance", torchChance, 0, 1);
        decorChance = EditorGUILayout.Slider("Decor Chance", decorChance, 0, 1);
        furnitureChance = EditorGUILayout.Slider("Furniture Chance", furnitureChance, 0, 1);
        maxChests = EditorGUILayout.IntField("Max Chests", maxChests);

        GUILayout.Space(20);
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🏰 Build Maze", GUILayout.Height(40))) BuildMaze();

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("🗑 Clear Maze", GUILayout.Height(30))) ClearMaze();

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndScrollView();
    }

    void DrawArray(ref GameObject[] array, string label)
    {
        if (array == null) array = new GameObject[0];
        int newSize = EditorGUILayout.IntField(label + " Size", array.Length);
        if (newSize != array.Length) System.Array.Resize(ref array, newSize);
        for (int i = 0; i < array.Length; i++)
            array[i] = (GameObject)EditorGUILayout.ObjectField($"  [{i}]", array[i], typeof(GameObject), false);
    }

    void ClearMaze()
    {
        GameObject existing = GameObject.Find("GeneratedMaze");
        if (existing != null) DestroyImmediate(existing);
    }

    void BuildMaze()
    {
        if (floorPrefab == null || wallVariants == null || wallVariants.Length == 0)
        {
            EditorUtility.DisplayDialog("Помилка", "Потрібно Floor і Wall Variants!", "OK");
            return;
        }

        if (width % 2 == 0) width++;
        if (height % 2 == 0) height++;

        if (width < 29 || height < 15)
        {
            EditorUtility.DisplayDialog("Помилка", "Мінімальний розмір 29x15 (щоб влізли 2 кімнати і 3 зони лабіринту)!", "OK");
            return;
        }

        ClearMaze();
        if (seed == 0) seed = Random.Range(1, 99999);
        Random.InitState(seed);

        doorPlacements.Clear();
        doorBoundarySet.Clear();
        deadEnds.Clear();

        GenerateZonedMaze();
        ComputeDeadEnds();
        BuildGeometry();

        Debug.Log($"✅ Лабіринт {width}x{height}: старт{startPos} → кімн1{puzzleRoom1} → кімн2{puzzleRoom2} → вихід{endPos}. " +
                  $"Сундуків: {Mathf.Min(maxChests, deadEnds.Count)}. Seed: {seed}");
    }

    // Головна генерація: 3 ізольовані зони + 2 кімнати з двома дверима кожна.
    // Шлях ЗАВЖДИ: zone1 → Room1 → zone2 → Room2 → zone3.
    void GenerateZonedMaze()
    {
        maze = new bool[width, height];

        // X-координати центрів кімнат (непарні)
        int cx1 = width / 3;
        if (cx1 % 2 == 0) cx1++;
        int cx2 = 2 * width / 3;
        if (cx2 % 2 == 0) cx2++;

        // Y-координати (непарні, всередині допустимого діапазону)
        int cy1 = PickOddInRange(3, height - 4);
        int cy2 = PickOddInRange(3, height - 4);

        puzzleRoom1 = new Vector2Int(cx1, cy1);
        puzzleRoom2 = new Vector2Int(cx2, cy2);

        // Межі зон (від краю, розділені суцільними стіновими колонками):
        //   zone1: [1 .. cx1-4]    |  sep: cx1-3  |  Room1: cx1±2  |  sep: cx1+3  |
        //   zone2: [cx1+4 .. cx2-4]|  sep: cx2-3  |  Room2: cx2±2  |  sep: cx2+3  |
        //   zone3: [cx2+4 .. W-2]
        int z1MinX = 1,          z1MaxX = MakeOdd(cx1 - 4, false);
        int z2MinX = MakeOdd(cx1 + 4, true), z2MaxX = MakeOdd(cx2 - 4, false);
        int z3MinX = MakeOdd(cx2 + 4, true), z3MaxX = MakeOdd(width - 2, false);

        int yMin = 1, yMax = MakeOdd(height - 2, false);

        // Кожна зона — власний perfect maze, ізольований стіновими колонками
        GenerateZoneMaze(z1MinX, z1MaxX, yMin, yMax);
        GenerateZoneMaze(z2MinX, z2MaxX, yMin, yMax);
        GenerateZoneMaze(z3MinX, z3MaxX, yMin, yMax);

        // Вирізаємо кімнати (5x5)
        CarveRoom(puzzleRoom1, ROOM_SIZE, ROOM_SIZE);
        CarveRoom(puzzleRoom2, ROOM_SIZE, ROOM_SIZE);

        // Прорубаємо ЄДИНІ проходи через роздільні колонки — це клітинки дверей
        SafeCarve(new Vector2Int(cx1 - 3, cy1)); // entry 1 sep
        SafeCarve(new Vector2Int(cx1 + 3, cy1)); // exit  1 sep
        SafeCarve(new Vector2Int(cx2 - 3, cy2)); // entry 2 sep
        SafeCarve(new Vector2Int(cx2 + 3, cy2)); // exit  2 sep

        // Гарантуємо карбовану клітинку в зоні поруч з дверима
        SafeCarve(new Vector2Int(cx1 - 4, cy1));
        SafeCarve(new Vector2Int(cx1 + 4, cy1));
        SafeCarve(new Vector2Int(cx2 - 4, cy2));
        SafeCarve(new Vector2Int(cx2 + 4, cy2));

        // Реєструємо двері як візуальні елементи на межі "клітинка дверей" ↔ "клітинка кімнати"
        RegisterDoor(new Vector2Int(cx1 - 3, cy1), new Vector2Int(cx1 - 2, cy1), "Room1_EntryDoor", false);
        RegisterDoor(new Vector2Int(cx1 + 2, cy1), new Vector2Int(cx1 + 3, cy1), "Room1_ExitDoor",  true);
        RegisterDoor(new Vector2Int(cx2 - 3, cy2), new Vector2Int(cx2 - 2, cy2), "Room2_EntryDoor", false);
        RegisterDoor(new Vector2Int(cx2 + 2, cy2), new Vector2Int(cx2 + 3, cy2), "Room2_ExitDoor",  true);

        // Старт у зоні 1, вихід у зоні 3
        startPos = FindCellInZone(z1MinX, yMin, z1MaxX, yMax);
        endPos   = FindCellInZone(z3MinX, yMin, z3MaxX, yMax);

        // Гарантуємо досяжність у межах однієї зони (на випадок, якщо старт/фініш фолбекнулися в невідвідану клітинку)
        EnsureReachable(startPos, new Vector2Int(cx1 - 4, cy1));
        EnsureReachable(new Vector2Int(cx1 + 4, cy1), new Vector2Int(cx2 - 4, cy2));
        EnsureReachable(new Vector2Int(cx2 + 4, cy2), endPos);
    }

    int MakeOdd(int v, bool roundUp)
    {
        if (v % 2 != 0) return v;
        return roundUp ? v + 1 : v - 1;
    }

    int PickOddInRange(int minInclusive, int maxInclusive)
    {
        int v = Random.Range(minInclusive, maxInclusive + 1);
        if (v % 2 == 0) v = Mathf.Clamp(v + 1, minInclusive, maxInclusive);
        if (v % 2 == 0) v = Mathf.Clamp(v - 2, minInclusive, maxInclusive);
        if (v % 2 == 0) v = minInclusive % 2 == 0 ? minInclusive + 1 : minInclusive;
        return v;
    }

    void RegisterDoor(Vector2Int a, Vector2Int b, string name, bool locked)
    {
        doorPlacements.Add(new DoorInfo { a = a, b = b, name = name, locked = locked });
        doorBoundarySet.Add(BoundaryKey(a, b));
    }

    ulong BoundaryKey(Vector2Int a, Vector2Int b)
    {
        // Canonicalize: smaller cell first
        Vector2Int lo, hi;
        if (a.x < b.x || (a.x == b.x && a.y < b.y)) { lo = a; hi = b; }
        else                                         { lo = b; hi = a; }
        return ((ulong)(uint)lo.x << 48) | ((ulong)(uint)lo.y << 32) | ((ulong)(uint)hi.x << 16) | (uint)hi.y;
    }

    void GenerateZoneMaze(int minX, int maxX, int minY, int maxY)
    {
        if (minX % 2 == 0) minX++;
        if (minY % 2 == 0) minY++;
        if (maxX % 2 == 0) maxX--;
        if (maxY % 2 == 0) maxY--;
        if (minX > maxX || minY > maxY) return;

        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        Vector2Int start = new Vector2Int(minX, minY);
        maze[start.x, start.y] = true;
        stack.Push(start);

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek();
            List<Vector2Int> neighbors = GetUnvisitedNeighborsBounded(current, minX, maxX, minY, maxY);

            if (neighbors.Count > 0)
            {
                Vector2Int next = neighbors[Random.Range(0, neighbors.Count)];
                Vector2Int wall = current + (next - current) / 2;
                maze[wall.x, wall.y] = true;
                maze[next.x, next.y] = true;
                stack.Push(next);
            }
            else stack.Pop();
        }
    }

    List<Vector2Int> GetUnvisitedNeighborsBounded(Vector2Int pos, int minX, int maxX, int minY, int maxY)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        Vector2Int[] dirs = {
            new Vector2Int(0, 2), new Vector2Int(0, -2),
            new Vector2Int(2, 0), new Vector2Int(-2, 0)
        };
        foreach (var dir in dirs)
        {
            Vector2Int n = pos + dir;
            if (n.x >= minX && n.x <= maxX && n.y >= minY && n.y <= maxY && !maze[n.x, n.y])
                result.Add(n);
        }
        return result;
    }

    void ComputeDeadEnds()
    {
        // Клітинки, адяцентні до дверей — пропустимо (щоб сундук не опинився на порозі)
        HashSet<Vector2Int> avoid = new HashSet<Vector2Int>();
        foreach (var d in doorPlacements) { avoid.Add(d.a); avoid.Add(d.b); }

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (!maze[x, y]) continue;
                Vector2Int p = new Vector2Int(x, y);
                if (p == startPos || p == endPos) continue;
                if (avoid.Contains(p)) continue;
                if (IsInRoom(x, y, puzzleRoom1, ROOM_SIZE, ROOM_SIZE)) continue;
                if (IsInRoom(x, y, puzzleRoom2, ROOM_SIZE, ROOM_SIZE)) continue;

                int n = 0;
                if (maze[x + 1, y]) n++;
                if (maze[x - 1, y]) n++;
                if (maze[x, y + 1]) n++;
                if (maze[x, y - 1]) n++;

                if (n == 1) deadEnds.Add(p);
            }
        }

        // Перемішуємо щоб сундуки розкидалися по всіх зонах
        for (int i = deadEnds.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (deadEnds[i], deadEnds[j]) = (deadEnds[j], deadEnds[i]);
        }
    }

    Vector2Int FindCellInZone(int x1, int y1, int x2, int y2)
    {
        int tries = 80;
        for (int i = 0; i < tries; i++)
        {
            int x = Random.Range(x1, x2 + 1);
            int y = Random.Range(y1, y2 + 1);
            if (x % 2 == 0) x++;
            if (y % 2 == 0) y++;
            x = Mathf.Clamp(x, x1, x2);
            y = Mathf.Clamp(y, y1, y2);
            if (maze[x, y]) return new Vector2Int(x, y);
        }
        int cx = Mathf.Clamp((x1 + x2) / 2, x1, x2);
        int cy = Mathf.Clamp((y1 + y2) / 2, y1, y2);
        if (cx % 2 == 0) cx = Mathf.Max(x1, cx - 1);
        if (cy % 2 == 0) cy = Mathf.Max(y1, cy - 1);
        SafeCarve(new Vector2Int(cx, cy));
        return new Vector2Int(cx, cy);
    }

    void EnsureReachable(Vector2Int from, Vector2Int to)
    {
        if (IsReachable(from, to)) return;
        Vector2Int current = from;
        while (current.x != to.x)
        {
            current.x += (to.x > current.x) ? 1 : -1;
            SafeCarve(current);
        }
        while (current.y != to.y)
        {
            current.y += (to.y > current.y) ? 1 : -1;
            SafeCarve(current);
        }
    }

    bool IsReachable(Vector2Int from, Vector2Int to)
    {
        if (!maze[from.x, from.y] || !maze[to.x, to.y]) return false;

        bool[,] visited = new bool[width, height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(from);
        visited[from.x, from.y] = true;

        Vector2Int[] dirs = {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1)
        };
        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            if (cur == to) return true;
            foreach (var d in dirs)
            {
                Vector2Int n = cur + d;
                if (n.x < 0 || n.x >= width || n.y < 0 || n.y >= height) continue;
                if (visited[n.x, n.y] || !maze[n.x, n.y]) continue;
                visited[n.x, n.y] = true;
                queue.Enqueue(n);
            }
        }
        return false;
    }

    void SafeCarve(Vector2Int pos)
    {
        if (pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height)
            maze[pos.x, pos.y] = true;
    }

    void CarveRoom(Vector2Int center, int w, int h)
    {
        for (int x = center.x - w / 2; x <= center.x + w / 2; x++)
            for (int y = center.y - h / 2; y <= center.y + h / 2; y++)
                if (x > 0 && x < width - 1 && y > 0 && y < height - 1)
                    maze[x, y] = true;
    }

    bool IsInRoom(int x, int y, Vector2Int center, int w, int h)
    {
        return x >= center.x - w / 2 && x <= center.x + w / 2
            && y >= center.y - h / 2 && y <= center.y + h / 2;
    }

    // ================== BUILD GEOMETRY ==================

    void BuildGeometry()
    {
        GameObject root = new GameObject("GeneratedMaze");
        GameObject floors  = MakeChild(root, "Floors");
        GameObject walls   = MakeChild(root, "Walls");
        GameObject ceils   = MakeChild(root, "Ceilings");
        GameObject doors   = MakeChild(root, "Doors");
        GameObject decors  = MakeChild(root, "Decorations");
        GameObject rooms   = MakeChild(root, "Markers");
        GameObject chests  = MakeChild(root, "Chests");

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!maze[x, y]) continue;

                Vector3 pos = new Vector3(x * cellSize, 0, y * cellSize);

                GameObject fPrefab = floorPrefab;
                if (floorVariants != null && floorVariants.Length > 0 && Random.value < 0.3f)
                {
                    var valid = System.Array.FindAll(floorVariants, p => p != null);
                    if (valid.Length > 0) fPrefab = valid[Random.Range(0, valid.Length)];
                }

                var floor = (GameObject)PrefabUtility.InstantiatePrefab(fPrefab);
                floor.transform.position = pos;
                floor.transform.rotation = Quaternion.Euler(0, Random.Range(0, 4) * 90, 0);
                floor.transform.parent = floors.transform;
                floor.name = $"Floor_{x}_{y}";

                if (ceilingPrefab != null)
                {
                    var ceil = (GameObject)PrefabUtility.InstantiatePrefab(ceilingPrefab);
                    ceil.transform.position = pos + Vector3.up * cellSize;
                    ceil.transform.rotation = Quaternion.Euler(180, 0, 0);
                    ceil.transform.parent = ceils.transform;
                    ceil.name = $"Ceiling_{x}_{y}";
                }

                PlaceWallOrDoor(x, y, new Vector2Int(0, 1),  0,   walls, doors);
                PlaceWallOrDoor(x, y, new Vector2Int(1, 0),  90,  walls, doors);
                PlaceWallOrDoor(x, y, new Vector2Int(0,-1),  180, walls, doors);
                PlaceWallOrDoor(x, y, new Vector2Int(-1,0),  270, walls, doors);

                TryPlaceFloorDecor(x, y, decors.transform);
            }
        }

        // Сундуки у тупиках
        if (chestPrefab != null && deadEnds.Count > 0)
        {
            int count = Mathf.Min(maxChests, deadEnds.Count);
            for (int i = 0; i < count; i++)
            {
                var c = deadEnds[i];
                Vector3 pos = new Vector3(c.x * cellSize, 0, c.y * cellSize);
                pos += new Vector3(Random.Range(-0.4f, 0.4f), 0, Random.Range(-0.4f, 0.4f));
                var chest = (GameObject)PrefabUtility.InstantiatePrefab(chestPrefab);
                chest.transform.position = pos;
                chest.transform.rotation = Quaternion.Euler(0, Random.Range(0, 4) * 90, 0);
                chest.transform.parent = chests.transform;
                chest.name = $"Chest_{i}";
            }
        }

        MarkPoint(puzzleRoom1, "PuzzleRoom_1", rooms.transform);
        MarkPoint(puzzleRoom2, "PuzzleRoom_2", rooms.transform);
        MarkPoint(startPos,    "StartPoint",   rooms.transform);
        MarkPoint(endPos,      "EndPoint",     rooms.transform);

        GameObject player = GameObject.Find("Player");
        if (player != null)
            player.transform.position = new Vector3(startPos.x * cellSize, 1.5f, startPos.y * cellSize);
    }

    GameObject MakeChild(GameObject parent, string name)
    {
        GameObject g = new GameObject(name);
        g.transform.parent = parent.transform;
        return g;
    }

    void PlaceWallOrDoor(int x, int y, Vector2Int dir, float rotY, GameObject wallsParent, GameObject doorsParent)
    {
        int nx = x + dir.x;
        int ny = y + dir.y;

        bool outside = nx < 0 || nx >= width || ny < 0 || ny >= height;
        bool neighborSolid = outside || !maze[nx, ny];

        Vector3 pos = new Vector3(x * cellSize, 0, y * cellSize);
        pos += new Vector3(dir.x * cellSize / 2f, 0, dir.y * cellSize / 2f);

        // Якщо обидві прохідні — це або вільний прохід, або межа з дверима
        if (!neighborSolid)
        {
            // Щоб не дублювати — обробляємо тільки з "меншої" клітинки
            Vector2Int here = new Vector2Int(x, y);
            Vector2Int there = new Vector2Int(nx, ny);
            bool isSmaller = here.x < there.x || (here.x == there.x && here.y < there.y);
            if (!isSmaller) return;

            ulong key = BoundaryKey(here, there);
            if (!doorBoundarySet.Contains(key)) return;

            // Знаходимо інфо для цих дверей
            foreach (var d in doorPlacements)
            {
                if (BoundaryKey(d.a, d.b) == key)
                {
                    PlaceDoorAtBoundary(pos, rotY, doorsParent, d.name, d.locked);
                    break;
                }
            }
            return;
        }

        // Звичайна стіна
        GameObject wallPrefabToUse = wallVariants[Random.Range(0, wallVariants.Length)];
        var wall = (GameObject)PrefabUtility.InstantiatePrefab(wallPrefabToUse);
        wall.transform.position = pos;
        wall.transform.rotation = Quaternion.Euler(0, rotY, 0);
        wall.transform.parent = wallsParent.transform;
        wall.name = $"Wall_{x}_{y}_{rotY}";

        if (torchPrefab != null && Random.value < torchChance)
        {
            var torch = (GameObject)PrefabUtility.InstantiatePrefab(torchPrefab);
            Vector3 torchPos = pos + Vector3.up * 2.2f;
            torchPos -= new Vector3(dir.x * 0.25f, 0, dir.y * 0.25f);
            torch.transform.position = torchPos;
            torch.transform.rotation = Quaternion.Euler(0, rotY + 180, 0);
            torch.transform.parent = wall.transform;
            torch.name = "Torch";
            if (torch.GetComponentInChildren<Light>() == null)
                AddFireLight(torch.transform);
        }
    }

    void PlaceDoorAtBoundary(Vector3 pos, float rotY, GameObject parent, string name, bool locked)
    {
        GameObject group = new GameObject(name);
        group.transform.position = pos;
        group.transform.rotation = Quaternion.Euler(0, rotY, 0);
        group.transform.parent = parent.transform;

        if (wallDoorPrefab != null)
        {
            var frame = (GameObject)PrefabUtility.InstantiatePrefab(wallDoorPrefab);
            frame.transform.position = pos;
            frame.transform.rotation = Quaternion.Euler(0, rotY, 0);
            frame.transform.parent = group.transform;
            frame.name = "Frame";
        }

        if (doorPrefab != null)
        {
            GameObject pivot = new GameObject("Pivot");
            Vector3 pivotOffset = Quaternion.Euler(0, rotY, 0) * new Vector3(-cellSize / 2f + 0.1f, 0, 0);
            pivot.transform.position = pos + pivotOffset;
            pivot.transform.rotation = Quaternion.Euler(0, rotY, 0);
            pivot.transform.parent = group.transform;

            var door = (GameObject)PrefabUtility.InstantiatePrefab(doorPrefab);
            door.transform.parent = pivot.transform;
            door.transform.localPosition = new Vector3(cellSize / 2f - 0.1f, 0, 0);
            door.transform.localRotation = Quaternion.identity;
            door.name = locked ? "LockedLeaf" : "UnlockedLeaf";
        }
    }

    void AddFireLight(Transform parent)
    {
        var lightObj = new GameObject("FireLight");
        lightObj.transform.parent = parent;
        lightObj.transform.localPosition = Vector3.up * 0.3f;
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.6f, 0.2f);
        light.range = 6f;
        light.intensity = 1.5f;
        light.shadows = LightShadows.Soft;
    }

    void TryPlaceFloorDecor(int x, int y, Transform parent)
    {
        if (x == startPos.x && y == startPos.y) return;
        if (x == endPos.x && y == endPos.y) return;

        bool inRoom1 = IsInRoom(x, y, puzzleRoom1, ROOM_SIZE, ROOM_SIZE);
        bool inRoom2 = IsInRoom(x, y, puzzleRoom2, ROOM_SIZE, ROOM_SIZE);

        if (x == puzzleRoom1.x && y == puzzleRoom1.y)
        {
            if (statuePrefab != null) PlaceDecor(statuePrefab, x, y, parent, "PuzzleStatue_1", centered: true);
            else if (candlePrefab != null) PlaceDecor(candlePrefab, x, y, parent, "PuzzleCandle_1", centered: true);
            return;
        }

        if (x == puzzleRoom2.x && y == puzzleRoom2.y)
        {
            if (statuePrefab != null) PlaceDecor(statuePrefab, x, y, parent, "PuzzleStatue_2", centered: true);
            else if (candlePrefab != null) PlaceDecor(candlePrefab, x, y, parent, "PuzzleCandle_2", centered: true);
            return;
        }

        if ((inRoom1 || inRoom2) && furniturePrefabs != null && furniturePrefabs.Length > 0)
        {
            if (Random.value < furnitureChance * 3f)
            {
                var valid = System.Array.FindAll(furniturePrefabs, p => p != null);
                if (valid.Length > 0)
                    PlaceDecor(valid[Random.Range(0, valid.Length)], x, y, parent, "RoomFurniture", centered: false);
            }
            return;
        }

        if (Random.value < decorChance && smallDecorPrefabs != null && smallDecorPrefabs.Length > 0)
        {
            var valid = System.Array.FindAll(smallDecorPrefabs, p => p != null);
            if (valid.Length > 0)
                PlaceDecor(valid[Random.Range(0, valid.Length)], x, y, parent, "CorridorDecor", centered: false);
        }
    }

    void PlaceDecor(GameObject prefab, int x, int y, Transform parent, string label, bool centered)
    {
        var decor = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Vector3 pos = new Vector3(x * cellSize, 0, y * cellSize);
        if (!centered)
            pos += new Vector3(Random.Range(-0.8f, 0.8f), 0, Random.Range(-0.8f, 0.8f));
        decor.transform.position = pos;
        decor.transform.rotation = Quaternion.Euler(0, centered ? 0 : Random.Range(0, 360), 0);
        decor.transform.parent = parent;
        decor.name = label;
    }

    void MarkPoint(Vector2Int center, string markerName, Transform parent)
    {
        GameObject marker = new GameObject(markerName);
        marker.transform.position = new Vector3(center.x * cellSize, 0, center.y * cellSize);
        marker.transform.parent = parent;
    }
}
