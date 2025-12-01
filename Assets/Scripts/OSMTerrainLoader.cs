using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using System;

public class OSMTerrainLoader : MonoBehaviour
{
    [Header("Feature Settings")]
    [SerializeField] private bool loadRoads = true;
    [SerializeField] private bool loadWater = true;
    [SerializeField] private bool loadGreenAreas = true;
    [SerializeField] private float queryRadius = 200f;

    [Header("Road Settings")]
    [SerializeField] private Material roadMaterial;
    [SerializeField] private float motorwayWidth = 12f;
    [SerializeField] private float primaryWidth = 9f;
    [SerializeField] private float secondaryWidth = 7f;
    [SerializeField] private float residentialWidth = 5f;
    [SerializeField] private float pathWidth = 2f;
    [SerializeField] private float roadY = 0.005f;

    [Header("Water Settings")]
    [SerializeField] private Material waterMaterial;
    [SerializeField] private float waterY = 0.001f;

    [Header("Land Settings")]
    [SerializeField] private Material landMaterial;
    [SerializeField] private float landHeight = 1f;

    [Header("References")]
    [SerializeField] private GPSTracker gpsTracker;
    [SerializeField] private CollectibleSpawner collectibleSpawner;

    private Dictionary<string, GameObject> loadedFeatures = new();
    private List<GreenAreaData> greenAreas = new();
    private GameObject baseLandPlane;

    private bool isInitialized = false;
    private bool isLoading = false;
    private Vector2 lastLoadedCenter = Vector2.zero;
    private float reloadDistanceThreshold = 150f;

    private string statusMessage = "OSM Terrain: Waiting...";

    void Start()
    {
        if (gpsTracker == null) gpsTracker = FindObjectOfType<GPSTracker>();
        if (collectibleSpawner == null) collectibleSpawner = FindObjectOfType<CollectibleSpawner>();

        InitializeMaterials();

        if (gpsTracker != null)
        {
            gpsTracker.OnGPSUpdate += OnGPSUpdated;
            Debug.Log("[OSM] GPS event subscribed");
        }
    }

    void InitializeMaterials()
    {
        if (roadMaterial == null)
        {
            roadMaterial = new Material(Shader.Find("Sprites/Default"));
            roadMaterial.color = new Color(0.3f, 0.3f, 0.3f);
        }

        if (waterMaterial == null)
        {
            waterMaterial = new Material(Shader.Find("Standard"));
            waterMaterial.color = new Color(0.2f, 0.5f, 0.8f, 0.8f);
            waterMaterial.SetFloat("_Glossiness", 0.7f);
        }

        if (landMaterial == null)
        {
            landMaterial = new Material(Shader.Find("Standard"));
            landMaterial.color = new Color(0.4f, 0.6f, 0.3f);
        }
    }

    void OnGPSUpdated(double lat, double lon, float heading)
    {
        if (!isInitialized)
        {
            if (!PlayerCharacter.IsGlobalOriginSet) return;

            isInitialized = true;
            Debug.Log($"[OSM] First GPS update received: {lat:F6}, {lon:F6}");
            StartCoroutine(LoadOSMTerrain(lat, lon));
            lastLoadedCenter = new Vector2((float)lat, (float)lon);
            return;
        }

        Vector2 currentPos = new Vector2((float)lat, (float)lon);
        float distance = Vector2.Distance(currentPos, lastLoadedCenter) * 111319.9f;

        if (distance > reloadDistanceThreshold && !isLoading)
        {
            Debug.Log($"[OSM] Moved {distance:F1}m, reloading...");
            StartCoroutine(LoadOSMTerrain(lat, lon));
            lastLoadedCenter = currentPos;
        }
    }

    IEnumerator LoadOSMTerrain(double lat, double lon)
    {
        if (isLoading) yield break;
        isLoading = true;
        statusMessage = "Loading OSM data...";
        Debug.Log($"[OSM] Starting load for ({lat:F6}, {lon:F6}), radius: {queryRadius}m");

        CreateBaseLandPlane();

        string query = BuildOverpassQuery(lat, lon, queryRadius);
        string url = "https://overpass-api.de/api/interpreter";

        WWWForm form = new WWWForm();
        form.AddField("data", query);

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            request.timeout = 30;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log($"[OSM] Data received: {json.Length} bytes");

                ClearAllFeatures();
                ParseOSMData(json);
                statusMessage = $"Loaded! Features: {loadedFeatures.Count}, Green: {greenAreas.Count}";
            }
            else
            {
                statusMessage = $"Error: {request.error}";
                Debug.LogError($"[OSM] Request failed: {request.error}");
            }
        }

        isLoading = false;
    }

    void CreateBaseLandPlane()
    {
        if (baseLandPlane != null)
        {
            Destroy(baseLandPlane);
        }

        baseLandPlane = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseLandPlane.name = "BaseLandPlane";
        baseLandPlane.transform.SetParent(transform);
        baseLandPlane.transform.position = new Vector3(0, -landHeight, 0);

        float diameter = queryRadius * 2f;
        baseLandPlane.transform.localScale = new Vector3(diameter, landHeight, diameter);

        Renderer renderer = baseLandPlane.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = landMaterial;
        }

        Collider col = baseLandPlane.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Debug.Log($"[OSM] Base land plane created: diameter {diameter}m at y={landHeight}");
    }

    string BuildOverpassQuery(double lat, double lon, float radius)
    {
        string query = "[out:json][timeout:25];(";

        if (loadRoads)
        {
            query += $"way[\"highway\"](around:{radius},{lat},{lon});";
        }

        if (loadWater)
        {
            query += $"way[\"natural\"=\"water\"](around:{radius},{lat},{lon});";
            query += $"way[\"waterway\"](around:{radius},{lat},{lon});";
        }

        if (loadGreenAreas)
        {
            query += $"way[\"leisure\"=\"park\"](around:{radius},{lat},{lon});";
            query += $"way[\"landuse\"=\"forest\"](around:{radius},{lat},{lon});";
            query += $"way[\"landuse\"=\"meadow\"](around:{radius},{lat},{lon});";
            query += $"way[\"natural\"=\"wood\"](around:{radius},{lat},{lon});";
        }

        query += ");out body;>;out skel qt;";

        Debug.Log($"[OSM] Query: {query}");
        return query;
    }

    void ParseOSMData(string json)
    {
        Dictionary<long, OSMNode> nodes = new Dictionary<long, OSMNode>();
        List<OSMWay> ways = new();

        try
        {
            ParseJSONManually(json, nodes, ways);
            Debug.Log($"[OSM] Parsed {nodes.Count} nodes, {ways.Count} ways");

            if (ways.Count == 0)
            {
                Debug.LogWarning("[OSM] No ways found! Check if area has OSM data.");
                statusMessage = "No data in this area";
                return;
            }

            foreach (var way in ways)
            {
                if (way.tags.ContainsKey("highway"))
                {
                    CreateRoad(way, nodes);
                }
                else if (way.tags.ContainsKey("natural") && way.tags["natural"] == "water")
                {
                    CreateWaterPolygon(way, nodes);
                }
                else if (way.tags.ContainsKey("waterway"))
                {
                    CreateWaterway(way, nodes);
                }
                else if (IsGreenArea(way.tags))
                {
                    CreateInvisibleGreenArea(way, nodes);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[OSM] Parse error: {e.Message}\n{e.StackTrace}");
        }
    }

    void ParseJSONManually(string json, Dictionary<long, OSMNode> nodes, List<OSMWay> ways)
    {
        int elementsStart = json.IndexOf("\"elements\":");
        if (elementsStart == -1)
        {
            Debug.LogWarning("[OSM] No 'elements' found in JSON");
            return;
        }

        int arrayStart = json.IndexOf('[', elementsStart);
        if (arrayStart == -1) return;

        int depth = 0;
        int elementStart = -1;

        for (int i = arrayStart; i < json.Length; i++)
        {
            char c = json[i];

            if (c == '{')
            {
                depth++;
                if (depth == 1) elementStart = i;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0 && elementStart != -1)
                {
                    string element = json.Substring(elementStart, i - elementStart + 1);
                    ParseElement(element, nodes, ways);
                    elementStart = -1;
                }
            }
        }
    }

    void ParseElement(string json, Dictionary<long, OSMNode> nodes, List<OSMWay> ways)
    {
        string type = ExtractStringValue(json, "type");
        if (string.IsNullOrEmpty(type)) return;

        long id = ExtractLongValue(json, "id");

        if (type == "node")
        {
            double lat = ExtractDoubleValue(json, "lat");
            double lon = ExtractDoubleValue(json, "lon");
            if (lat != 0 && lon != 0)
            {
                nodes[id] = new OSMNode { lat = lat, lon = lon };
            }
        }
        else if (type == "way")
        {
            OSMWay way = new OSMWay
            {
                id = id,
                nodeRefs = new List<long>(),
                tags = new Dictionary<string, string>()
            };

            int nodesStart = json.IndexOf("\"nodes\":");
            if (nodesStart != -1)
            {
                int arrayStart = json.IndexOf('[', nodesStart);
                int arrayEnd = json.IndexOf(']', arrayStart);
                if (arrayStart != -1 && arrayEnd != -1)
                {
                    string nodesArray = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                    string[] nodeIds = nodesArray.Split(',');
                    foreach (string nodeId in nodeIds)
                    {
                        string trimmed = nodeId.Trim();
                        if (long.TryParse(trimmed, out long nid))
                        {
                            way.nodeRefs.Add(nid);
                        }
                    }
                }
            }

            int tagsStart = json.IndexOf("\"tags\":");
            if (tagsStart != -1)
            {
                int objStart = json.IndexOf('{', tagsStart);
                if (objStart != -1)
                {
                    int objEnd = FindMatchingBrace(json, objStart);
                    if (objEnd != -1)
                    {
                        string tagsJson = json.Substring(objStart + 1, objEnd - objStart - 1);
                        ParseTags(tagsJson, way.tags);
                    }
                }
            }

            if (way.nodeRefs.Count > 0 && way.tags.Count > 0)
            {
                ways.Add(way);
            }
        }
    }

    void ParseTags(string tagsJson, Dictionary<string, string> tags)
    {
        Regex regex = new Regex("\"([^\"]+)\"\\s*:\\s*\"([^\"]+)\"");
        MatchCollection matches = regex.Matches(tagsJson);

        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 3)
            {
                string key = match.Groups[1].Value;
                string value = match.Groups[2].Value;
                tags[key] = value;
            }
        }
    }

    int FindMatchingBrace(string json, int start)
    {
        int depth = 1;
        for (int i = start + 1; i < json.Length; i++)
        {
            if (json[i] == '{') depth++;
            else if (json[i] == '}')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    string ExtractStringValue(string json, string key)
    {
        string pattern = "\"" + key + "\"\\s*:\\s*\"([^\"]+)\"";
        Match match = Regex.Match(json, pattern);
        return match.Success ? match.Groups[1].Value : "";
    }

    long ExtractLongValue(string json, string key)
    {
        string pattern = "\"" + key + "\"\\s*:\\s*(\\d+)";
        Match match = Regex.Match(json, pattern);
        if (match.Success && long.TryParse(match.Groups[1].Value, out long value))
        {
            return value;
        }
        return 0;
    }

    double ExtractDoubleValue(string json, string key)
    {
        string pattern = "\"" + key + "\"\\s*:\\s*([\\d.\\-]+)";
        Match match = Regex.Match(json, pattern);
        if (match.Success && double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double value))
        {
            return value;
        }
        return 0;
    }

    void CreateRoad(OSMWay way, Dictionary<long, OSMNode> nodes)
    {
        string id = $"road_{way.id}";
        if (loadedFeatures.ContainsKey(id)) return;

        List<Vector3> positions = new List<Vector3>();
        foreach (long nodeId in way.nodeRefs)
        {
            if (nodes.ContainsKey(nodeId))
            {
                Vector3 pos = GPSToUnityPosition(nodes[nodeId].lat, nodes[nodeId].lon);
                pos.y = roadY;
                positions.Add(pos);
            }
        }

        if (positions.Count < 2)
        {
            Debug.LogWarning($"[OSM] Road {way.id} has only {positions.Count} points");
            return;
        }

        float width = residentialWidth;
        if (way.tags.ContainsKey("highway"))
        {
            string type = way.tags["highway"];
            switch (type)
            {
                case "motorway":
                case "trunk":
                    width = motorwayWidth;
                    break;
                case "primary":
                    width = primaryWidth;
                    break;
                case "secondary":
                case "tertiary":
                    width = secondaryWidth;
                    break;
                case "footway":
                case "path":
                case "cycleway":
                    width = pathWidth;
                    break;
                default:
                    width = residentialWidth;
                    break;
            }
        }

        GameObject road = new GameObject($"Road_{way.id}");
        road.transform.SetParent(transform);

        MeshFilter mf = road.AddComponent<MeshFilter>();
        MeshRenderer mr = road.AddComponent<MeshRenderer>();

        Mesh mesh = CreateRoadMesh(positions, width);
        mf.mesh = mesh;
        mr.material = roadMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        loadedFeatures.Add(id, road);
        Debug.Log($"[OSM] Road created: {way.tags.GetValueOrDefault("highway", "unknown")}, {positions.Count} points, width: {width}m");

        if (collectibleSpawner != null && positions != null && positions.Count > 0)
        {
            collectibleSpawner.ProcessOSMFeature("road", positions, way.tags);
        }
    }

    Mesh CreateRoadMesh(List<Vector3> centerLine, float width)
    {
        Mesh mesh = new();
        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();

        float halfWidth = width * 0.5f;

        for (int i = 0; i < centerLine.Count; i++)
        {
            Vector3 point = centerLine[i];
            Vector3 right;

            if (i == 0)
            {
                Vector3 forward = (centerLine[i + 1] - centerLine[i]).normalized;
                right = Vector3.Cross(Vector3.up, forward).normalized;
            }
            else if (i == centerLine.Count - 1)
            {
                Vector3 forward = (centerLine[i] - centerLine[i - 1]).normalized;
                right = Vector3.Cross(Vector3.up, forward).normalized;
            }
            else
            {
                Vector3 dirIn = (centerLine[i] - centerLine[i - 1]).normalized;
                Vector3 dirOut = (centerLine[i + 1] - centerLine[i]).normalized;

                Vector3 rightIn = Vector3.Cross(Vector3.up, dirIn).normalized;
                Vector3 rightOut = Vector3.Cross(Vector3.up, dirOut).normalized;

                Vector3 miter = (rightIn + rightOut).normalized;
                float miterLength = halfWidth / Mathf.Max(0.3f, Vector3.Dot(miter, rightIn));
                miterLength = Mathf.Min(miterLength, halfWidth * 3f);

                right = miter;
                halfWidth = miterLength;
            }

            vertices.Add(point - right * halfWidth);
            vertices.Add(point + right * halfWidth);

            uvs.Add(new Vector2(0, i));
            uvs.Add(new Vector2(1, i));

            halfWidth = width * 0.5f;
        }

        for (int i = 0; i < centerLine.Count - 1; i++)
        {
            int baseIndex = i * 2;

            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 1);

            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 3);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    void CreateWaterPolygon(OSMWay way, Dictionary<long, OSMNode> nodes)
    {
        string id = $"water_{way.id}";
        if (loadedFeatures.ContainsKey(id)) return;

        List<Vector3> vertices = new();
        foreach (long nodeId in way.nodeRefs)
        {
            if (nodes.ContainsKey(nodeId))
            {
                Vector3 pos = GPSToUnityPosition(nodes[nodeId].lat, nodes[nodeId].lon);
                pos.y = waterY;
                vertices.Add(pos);
            }
        }

        if (vertices.Count < 3) return;

        GameObject obj = new GameObject($"Water_{way.id}");
        obj.transform.SetParent(transform);

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();

        Mesh mesh = CreateFlatPolygonMesh(vertices);
        mf.mesh = mesh;
        mr.material = waterMaterial;

        loadedFeatures.Add(id, obj);
        Debug.Log($"[OSM] Water created with {vertices.Count} vertices");

        if (collectibleSpawner != null && vertices != null && vertices.Count > 0)
        {
            collectibleSpawner.ProcessOSMFeature("water", vertices, way.tags);
        }
    }

    void CreateWaterway(OSMWay way, Dictionary<long, OSMNode> nodes)
    {
        string id = $"waterway_{way.id}";
        if (loadedFeatures.ContainsKey(id)) return;

        List<Vector3> positions = new();
        foreach (long nodeId in way.nodeRefs)
        {
            if (nodes.ContainsKey(nodeId))
            {
                Vector3 pos = GPSToUnityPosition(nodes[nodeId].lat, nodes[nodeId].lon);
                pos.y = waterY;
                positions.Add(pos);
            }
        }

        if (positions.Count < 2) return;

        GameObject waterway = new GameObject($"Waterway_{way.id}");
        waterway.transform.SetParent(transform);

        LineRenderer lr = waterway.AddComponent<LineRenderer>();
        lr.startWidth = 5f;
        lr.endWidth = 5f;
        lr.material = waterMaterial;
        lr.positionCount = positions.Count;
        lr.SetPositions(positions.ToArray());
        lr.useWorldSpace = true;

        loadedFeatures.Add(id, waterway);
        Debug.Log($"[OSM] Waterway created: {positions.Count} points");

        if (collectibleSpawner != null && positions != null && positions.Count > 0)
        {
            collectibleSpawner.ProcessOSMFeature("water", positions, way.tags);
        }
    }

    bool IsGreenArea(Dictionary<string, string> tags)
    {
        if (tags.ContainsKey("leisure") && tags["leisure"] == "park") return true;
        if (tags.ContainsKey("landuse"))
        {
            string landuse = tags["landuse"];
            if (landuse == "forest" || landuse == "meadow") return true;
        }
        if (tags.ContainsKey("natural") && tags["natural"] == "wood") return true;
        return false;
    }

    void CreateInvisibleGreenArea(OSMWay way, Dictionary<long, OSMNode> nodes)
    {
        List<Vector3> boundary = new();
        foreach (long nodeId in way.nodeRefs)
        {
            if (nodes.ContainsKey(nodeId))
            {
                Vector3 pos = GPSToUnityPosition(nodes[nodeId].lat, nodes[nodeId].lon);
                pos.y = 0;
                boundary.Add(pos);
            }
        }

        if (boundary.Count < 3) return;

        GreenAreaData greenArea = new()
        {
            id = way.id,
            boundary = boundary,
            tags = way.tags
        };

        greenAreas.Add(greenArea);
        Debug.Log($"[OSM] ✅ Invisible green area: {boundary.Count} points");

        if (collectibleSpawner != null && boundary != null && boundary.Count > 0)
        {
            collectibleSpawner.ProcessOSMFeature("green", boundary, way.tags);
        }
    }

    Mesh CreateFlatPolygonMesh(List<Vector3> vertices)
    {
        Mesh mesh = new();

        List<int> triangles = new();
        for (int i = 1; i < vertices.Count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    Vector3 GPSToUnityPosition(double lat, double lon)
    {
        if (!PlayerCharacter.IsGlobalOriginSet)
            return Vector3.zero;

        GPSUtils.GetTileCoordinate(lat, lon, 19, out double tileX, out double tileY);

        float xDiff = (float)((PlayerCharacter.GlobalOriginX - tileX) *
                              PlayerCharacter.GlobalTileSizeMeters);
        float zDiff = (float)((tileY - PlayerCharacter.GlobalOriginY) *
                              PlayerCharacter.GlobalTileSizeMeters);

        return new Vector3(xDiff, 0, zDiff);
    }

    public void OnWorldShift(Vector3 shiftVector)
    {
        Debug.Log($"[OSM] World shift: {shiftVector}");

        if (baseLandPlane != null)
        {
            baseLandPlane.transform.position -= shiftVector;
        }

        foreach (var feature in loadedFeatures.Values)
        {
            if (feature != null) feature.transform.position -= shiftVector;
        }

        if (collectibleSpawner != null)
        {
            collectibleSpawner.OnWorldShift(shiftVector);
        }
    }

    void ClearAllFeatures()
    {
        foreach (var item in loadedFeatures.Values)
        {
            if (item != null) Destroy(item);
        }
        loadedFeatures.Clear();
        greenAreas.Clear();

        if (collectibleSpawner != null)
        {
            collectibleSpawner.ClearAllSpawns();
        }

        Debug.Log("[OSM] All features cleared");
    }

    //void OnGUI()
    //{
    //    GUIStyle style = new()
    //    {
    //        fontSize = 30
    //    };
    //    style.normal.textColor = Color.green;

    //    string debugInfo = $"[OSM Terrain]\n{statusMessage}\n" +
    //                      $"Initialized: {isInitialized}\n" +
    //                      $"Loading: {isLoading}\n" +
    //                      $"Features: {loadedFeatures.Count}\n" +
    //                      $"Green Areas: {greenAreas.Count}";

    //    GUI.Label(new Rect(50, 1700, 1000, 300), debugInfo, style);

    //    if (GUI.Button(new Rect(50, 1900, 400, 120), "<size=35>Reload</size>"))
    //    {
    //        if (gpsTracker != null && !isLoading)
    //        {
    //            Debug.Log("[OSM] Manual reload triggered");
    //            StartCoroutine(LoadOSMTerrain(
    //                gpsTracker.CurrentLatitude,
    //                gpsTracker.CurrentLongitude
    //            ));
    //        }
    //    }
    //}

    void OnDestroy()
    {
        if (gpsTracker != null)
        {
            gpsTracker.OnGPSUpdate -= OnGPSUpdated;
        }

        if (baseLandPlane != null)
        {
            Destroy(baseLandPlane);
        }

        ClearAllFeatures();
    }
}

public class OSMWay
{
    public long id;
    public List<long> nodeRefs;
    public Dictionary<string, string> tags;
}

public struct OSMNode
{
    public double lat;
    public double lon;
}

public class GreenAreaData
{
    public long id;
    public List<Vector3> boundary;
    public Dictionary<string, string> tags;
}