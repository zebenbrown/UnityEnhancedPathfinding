using System;
using DefaultNamespace;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Cluster
{
    public int id;
    public Rect bounds;
    public List<Vector2Int> portalNodes;
}

public class Pathfinding : MonoBehaviour
{
    public int clusterSize = 128;
    private Cluster[,] clusters;
    private GridSystem gridSystem;

    [SerializeField] private GameObject agent;
    [SerializeField] private Vector2Int start;
    [SerializeField] private Vector2Int end;

    private List<Vector2Int> currentPath;
    private int currentPathIndex = 0;
    private bool shouldMove = false;
    private float cellSize;
    private float cellPaddingX;
    private float cellPaddingZ;
    private int mapWidth;
    private int mapHeight;



    private void Awake()
    {
        gridSystem = GetComponent<GridSystem>();
        Vector3 gridSize = gridSystem.GetGridSize();
        this.cellSize = gridSystem.GetCellSize();
        Vector3 cellPadding = gridSystem.GetCellPadding();

        // Store these for use throughout the script
        this.cellPaddingX = cellPadding.x;
        this.cellPaddingZ = cellPadding.z;
        this.mapWidth = (int)gridSize.x;
        this.mapHeight = (int)gridSize.z;

        Debug.Log($"Grid config: {mapWidth}x{mapHeight}, cellSize: {cellSize}, padding: {cellPaddingX}, {cellPaddingZ}");
        InitializeClusters();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentPath = FindPath(start, end);

            if (currentPath != null && currentPath.Count > 0)
            {
                currentPathIndex = 0;
                shouldMove = true;
                Debug.Log($"Path found with {currentPath.Count} nodes");
            }
            else
            {
                Debug.Log("No path found!");
                shouldMove = false;
            }
        }

        if (shouldMove && currentPath != null && currentPathIndex < currentPath.Count)
        {
            Vector2Int gridPos = currentPath[currentPathIndex];
            agent.transform.position = GridToWorld(gridPos); 
            currentPathIndex++;

            if (currentPathIndex >= currentPath.Count)
            {
                shouldMove = false;
                Debug.Log("Reached destination!");
            }
        }
    }

    private bool IsWalkable(Vector2Int position)
    {
        if (position.x < 0 || position.x >= 256 ||
            position.y < 0 || position.y >= 256)
            return false;

        Vector3 tilePos = GridToWorld(position);
        return gridSystem.IsPositionWalkable(tilePos);
    }
    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        float worldX = gridPos.x * (1 + cellPaddingX) * cellSize;
        float worldZ = gridPos.y * (1 + cellPaddingZ) * cellSize;

        return new Vector3(worldX, 0.5f, worldZ);
    }

    private void InitializeClusters()
    {
        int mapWidth = 256;
        int mapHeight = 256;

        int numXClusters = Mathf.CeilToInt((float)mapWidth / clusterSize);
        int numYClusters = Mathf.CeilToInt((float)mapHeight / clusterSize);

        clusters = new Cluster[numXClusters, numYClusters];

        for (int x = 0; x < numXClusters; x++)
        {
            for (int y = 0; y < numYClusters; y++)
            {
                clusters[x, y] = new Cluster
                {
                    id = x * numYClusters + y,
                    bounds = new Rect(x * clusterSize, y * clusterSize, clusterSize, clusterSize),
                    portalNodes = new List<Vector2Int>()
                };
                FindPortals(clusters[x, y]);
            }
        }

        Debug.Log($"Initialized {numXClusters}x{numYClusters} clusters");
    }

    private void FindPortals(Cluster cluster)
    {
        int mapWidth = 256;
        int mapHeight = 256;

        List<Vector2Int> topEdge = new List<Vector2Int>();
        List<Vector2Int> bottomEdge = new List<Vector2Int>();
        List<Vector2Int> leftEdge = new List<Vector2Int>();
        List<Vector2Int> rightEdge = new List<Vector2Int>();

        // Top edge
        for (int x = (int)cluster.bounds.x; x < (int)(cluster.bounds.x + cluster.bounds.width); x++)
        {
            int topY = (int)(cluster.bounds.y + cluster.bounds.height);
            if (topY >= mapHeight) continue;

            Vector2Int pos = new Vector2Int(x, topY);
            if (IsWalkable(pos)) topEdge.Add(pos);
        }

        // Bottom edge
        for (int x = (int)cluster.bounds.x; x < (int)(cluster.bounds.x + cluster.bounds.width); x++)
        {
            int bottomY = (int)cluster.bounds.y;
            Vector2Int pos = new Vector2Int(x, bottomY);
            if (IsWalkable(pos)) bottomEdge.Add(pos);
        }

        // Left edge
        for (int y = (int)cluster.bounds.y; y < (int)(cluster.bounds.y + cluster.bounds.height); y++)
        {
            int leftX = (int)cluster.bounds.x;
            Vector2Int pos = new Vector2Int(leftX, y);
            if (IsWalkable(pos)) leftEdge.Add(pos);
        }

        // Right edge
        for (int y = (int)cluster.bounds.y; y < (int)(cluster.bounds.y + cluster.bounds.height); y++)
        {
            int rightX = (int)(cluster.bounds.x + cluster.bounds.width);
            if (rightX >= mapWidth) continue;

            Vector2Int pos = new Vector2Int(rightX, y);
            if (IsWalkable(pos)) rightEdge.Add(pos);
        }

        // Add limited portals from each edge
        AddLimitedPortals(cluster, topEdge);
        AddLimitedPortals(cluster, bottomEdge);
        AddLimitedPortals(cluster, leftEdge);
        AddLimitedPortals(cluster, rightEdge);
    }

    private void AddLimitedPortals(Cluster cluster, List<Vector2Int> portals)
    {
        if (portals.Count == 0) return;

        cluster.portalNodes.Add(portals[0]);

        if (portals.Count > 2)
        {
            cluster.portalNodes.Add(portals[portals.Count / 2]);
        }

        if (portals.Count > 1)
        {
            cluster.portalNodes.Add(portals[portals.Count - 1]);
        }
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        Cluster startCluster = GetClusterFromPosition(start);
        Cluster endCluster = GetClusterFromPosition(end);

        if (startCluster == null || endCluster == null)
        {
            Debug.LogError("Start or end outside valid clusters!");
            return new List<Vector2Int>();
        }

        if (startCluster == endCluster)
        {
            return AStar(start, end);
        }

        Debug.Log("Using hierarchical pathfinding...");

        List<Cluster> clusterPath = FindHighLevelPath(startCluster, endCluster);

        if (clusterPath.Count == 0)
        {
            Debug.LogError("No cluster path found!");
            return new List<Vector2Int>();
        }

        List<Vector2Int> completePath = new List<Vector2Int>();

        for (int i = 0; i < clusterPath.Count; i++)
        {
            if (i == 0)
            {
                Vector2Int exitPortal = FindBestPortal(clusterPath[0], clusterPath[1]);
                List<Vector2Int> segment = AStarWithinCluster(start, exitPortal, clusterPath[0]);
                completePath.AddRange(segment);
            }
            else if (i == clusterPath.Count - 1)
            {
                Vector2Int entryPortal = FindBestPortal(clusterPath[i], clusterPath[i - 1]);
                List<Vector2Int> segment = AStarWithinCluster(entryPortal, end, clusterPath[i]);

                if (segment.Count > 1)
                    completePath.AddRange(segment.GetRange(1, segment.Count - 1));
            }
            else
            {
                Vector2Int entryPortal = FindBestPortal(clusterPath[i], clusterPath[i - 1]);
                Vector2Int exitPortal = FindBestPortal(clusterPath[i], clusterPath[i + 1]);
                List<Vector2Int> segment = AStarWithinCluster(entryPortal, exitPortal, clusterPath[i]);

                if (segment.Count > 1)
                    completePath.AddRange(segment.GetRange(1, segment.Count - 1));
            }
        }

        Debug.Log($"Path: {completePath.Count} nodes through {clusterPath.Count} clusters");
        return completePath;
    }

    private List<Vector2Int> AStar(Vector2Int start, Vector2Int end)
    {
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
        Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();

        HashSet<Vector2Int> openSet = new HashSet<Vector2Int>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        gScore[start] = 0;
        fScore[start] = Heuristic(start, end);
        openSet.Add(start);

        while (openSet.Count > 0)
        {
            Vector2Int current = openSet.First();
            float lowestF = fScore[current];

            foreach (Vector2Int node in openSet)
            {
                if (fScore[node] < lowestF)
                {
                    current = node;
                    lowestF = fScore[current];
                }
            }

            if (current == end)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            Vector2Int[] neighbors = new Vector2Int[]
            {
                new Vector2Int(current.x + 1, current.y),
                new Vector2Int(current.x - 1, current.y),
                new Vector2Int(current.x, current.y + 1),
                new Vector2Int(current.x, current.y - 1)
            };

            foreach (Vector2Int neighbor in neighbors)
            {
                if (closedSet.Contains(neighbor) || !IsWalkable(neighbor))
                    continue;

                float tentativeG = gScore[current] + 1;

                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                else if (gScore.ContainsKey(neighbor) && tentativeG >= gScore[neighbor])
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, end);
            }
        }

        return new List<Vector2Int>();
    }

    private List<Vector2Int> AStarWithinCluster(Vector2Int start, Vector2Int end, Cluster cluster)
    {
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
        Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();

        HashSet<Vector2Int> openSet = new HashSet<Vector2Int>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        gScore[start] = 0;
        fScore[start] = Heuristic(start, end);
        openSet.Add(start);

        while (openSet.Count > 0)
        {
            Vector2Int current = openSet.First();
            float lowestF = fScore[current];

            foreach (Vector2Int node in openSet)
            {
                if (fScore[node] < lowestF)
                {
                    current = node;
                    lowestF = fScore[current];
                }
            }

            if (current == end)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            Vector2Int[] neighbors = new Vector2Int[]
            {
                new Vector2Int(current.x + 1, current.y),
                new Vector2Int(current.x - 1, current.y),
                new Vector2Int(current.x, current.y + 1),
                new Vector2Int(current.x, current.y - 1)
            };

            foreach (Vector2Int neighbor in neighbors)
            {
                if (!IsInCluster(neighbor, cluster) || closedSet.Contains(neighbor) || !IsWalkable(neighbor))
                    continue;

                float tentativeG = gScore[current] + 1;

                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                else if (gScore.ContainsKey(neighbor) && tentativeG >= gScore[neighbor])
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, end);
            }
        }

        return new List<Vector2Int>();
    }

    private bool IsInCluster(Vector2Int pos, Cluster cluster)
    {
        return pos.x >= cluster.bounds.x &&
               pos.x < cluster.bounds.x + cluster.bounds.width &&
               pos.y >= cluster.bounds.y &&
               pos.y < cluster.bounds.y + cluster.bounds.height;
    }

    private List<Cluster> FindHighLevelPath(Cluster start, Cluster end)
    {
        if (start == null || end == null) return new List<Cluster>();
        if (start == end) return new List<Cluster> { start };

        Dictionary<Cluster, Cluster> cameFrom = new Dictionary<Cluster, Cluster>();
        Dictionary<Cluster, float> gScore = new Dictionary<Cluster, float>();
        Dictionary<Cluster, float> fScore = new Dictionary<Cluster, float>();

        HashSet<Cluster> openSet = new HashSet<Cluster>();
        HashSet<Cluster> closedSet = new HashSet<Cluster>();

        gScore[start] = 0;
        fScore[start] = HeuristicCluster(start, end);
        openSet.Add(start);

        while (openSet.Count > 0)
        {
            Cluster current = openSet.First();
            float lowestF = fScore[current];

            foreach (Cluster cluster in openSet)
            {
                if (fScore[cluster] < lowestF)
                {
                    current = cluster;
                    lowestF = fScore[current];
                }
            }

            if (current == end)
                return ReconstructClusterPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Cluster neighbor in GetNeighboringClusters(current))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                float tentativeG = gScore[current] + 1;

                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                else if (gScore.ContainsKey(neighbor) && tentativeG >= gScore[neighbor])
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                fScore[neighbor] = gScore[neighbor] + HeuristicCluster(neighbor, end);
            }
        }

        return new List<Cluster>();
    }

    private Vector2Int FindBestPortal(Cluster fromCluster, Cluster toCluster)
    {
        Vector2Int bestPortal = fromCluster.portalNodes[0];
        float minDistance = float.MaxValue;

        float centerX = toCluster.bounds.x + toCluster.bounds.width / 2;
        float centerY = toCluster.bounds.y + toCluster.bounds.height / 2;

        foreach (Vector2Int portal in fromCluster.portalNodes)
        {
            if (IsPortalOnBoundary(portal, fromCluster, toCluster))
            {
                float distance = Mathf.Abs(portal.x - centerX) + Mathf.Abs(portal.y - centerY);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestPortal = portal;
                }
            }
        }

        return bestPortal;
    }

    private bool IsPortalOnBoundary(Vector2Int portal, Cluster c1, Cluster c2)
    {
        bool onEdge = portal.x == c1.bounds.x || portal.x == c1.bounds.x + c1.bounds.width ||
                      portal.y == c1.bounds.y || portal.y == c1.bounds.y + c1.bounds.height;

        bool nearC2 = (portal.x >= c2.bounds.x - 1 && portal.x <= c2.bounds.x + c2.bounds.width) &&
                      (portal.y >= c2.bounds.y - 1 && portal.y <= c2.bounds.y + c2.bounds.height);

        return onEdge && nearC2;
    }

    private List<Cluster> GetNeighboringClusters(Cluster cluster)
    {
        List<Cluster> neighbors = new List<Cluster>();
        int numX = clusters.GetLength(0);
        int numY = clusters.GetLength(1);

        for (int x = 0; x < numX; x++)
        {
            for (int y = 0; y < numY; y++)
            {
                if (clusters[x, y] == cluster)
                {
                    if (x > 0) neighbors.Add(clusters[x - 1, y]);
                    if (x < numX - 1) neighbors.Add(clusters[x + 1, y]);
                    if (y > 0) neighbors.Add(clusters[x, y - 1]);
                    if (y < numY - 1) neighbors.Add(clusters[x, y + 1]);
                    return neighbors;
                }
            }
        }

        return neighbors;
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private float HeuristicCluster(Cluster a, Cluster b)
    {
        float aX = a.bounds.x + a.bounds.width / 2;
        float aY = a.bounds.y + a.bounds.height / 2;
        float bX = b.bounds.x + b.bounds.width / 2;
        float bY = b.bounds.y + b.bounds.height / 2;

        return Mathf.Abs(aX - bX) + Mathf.Abs(aY - bY);
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> path = new List<Vector2Int> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private List<Cluster> ReconstructClusterPath(Dictionary<Cluster, Cluster> cameFrom, Cluster current)
    {
        List<Cluster> path = new List<Cluster> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private Cluster GetClusterFromPosition(Vector2Int pos)
    {
        int x = pos.x / clusterSize;
        int y = pos.y / clusterSize;

        if (x >= 0 && x < clusters.GetLength(0) && y >= 0 && y < clusters.GetLength(1))
            return clusters[x, y];

        return null;
    }
}