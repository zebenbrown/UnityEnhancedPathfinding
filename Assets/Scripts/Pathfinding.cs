using System; 
using DefaultNamespace;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Cluster
{
    public int id;
    public Rect bounds;
    public List<Vector2Int> portalNodes;
}


public class Pathfinding : MonoBehaviour
{
    public int clusterSize = 32;
    private Cluster[,] clusters;
    private GridSystem gridSystem;
    [SerializeField] private GameObject agent;
    [SerializeField] private Vector2Int start;
    [SerializeField] private Vector2Int end;
    private bool shouldMove = false;

    private void Awake()
    {
        
        gridSystem = GetComponent<GridSystem>();
        InitializeClusters();
        PrecomputePaths();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            shouldMove = true;
        }

        if (shouldMove)
        {
            List<Vector2Int> path = FindPath(start, end);
            agent.transform.position = new Vector3(path[0].x, 0, path[0].y);  
        }
    }


    //Checking if a title is walkable 

    private bool IsWalkable(Vector2Int position)
    {
        Vector3 tilePos = new Vector3(position.x, 0, position.y);
        return !gridSystem.IsPositionWalkable.Contains(tilePos); 
    }
    

    private void InitializeClusters()
    {
        int mapWidth = 1024;
        int mapHeight = 1024;

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
                // Find and store portal nodes (boundaries) for each cluster
                FindPortals(clusters[x, y]);
            }
        }
    }

    private void FindPortals(Cluster cluster)
    {

        //It Finds the top edge 
        for (int x = (int)cluster.bounds.x; x < (int)(cluster.bounds.x + cluster.bounds.width); x++)
        {
            int topY = (int)(cluster.bounds.y + cluster.bounds.height);
            Vector2Int position = new Vector2Int(x, topY);
            if(IsWalkable(position))
            {
                cluster.portalNodes.Add(position);

            }
        }
        //Finds the Bottem Edge 
        for(int x = (int)cluster.bounds.x; x <(int)(cluster.bounds.x + cluster.bounds.width); x++){
            int bottemY = (int)(cluster.bounds.y);
            Vector2Int position = new Vector2Int(x, bottemY);
            if (IsWalkable(position))
            {
                cluster.portalNodes.Add(position);
            }
        }
        //Finds The Left Edge
        for (int y = (int)cluster.bounds.y; y < (int)(cluster.bounds.y + cluster.bounds.height); y++)
        {
            int leftX = (int)(cluster.bounds.x);
            Vector2Int postion = new Vector2Int(leftX, y);
            if (IsWalkable(postion))
            {
                cluster.portalNodes.Add(postion);
            }


        }
        //Finds the right Edge
        for (int y = (int)cluster.bounds.y; y < (int)(cluster.bounds.y + cluster.bounds.height); y++) {
            int rightX = (int)(cluster.bounds.x + cluster.bounds.width) - 1;
            Vector2Int position = new Vector2Int(rightX, y);
            if (IsWalkable(position)) { 
                cluster.portalNodes.Add(position);
            }

        }
    }
   
    private void PrecomputePaths()
    {
        int numXClusters = clusters.GetLength(0);
        int numYClusters = clusters.GetLength(1);



        for (int x = 0; x < numXClusters; x++)
        {
            for(int y = 0; y < numYClusters; y++)
            {
                Cluster cluster = clusters[x, y]; 
                foreach (Vector2Int portal1 in cluster.portalNodes)
                {
                    foreach (Vector2Int portal2 in cluster.portalNodes)
                    {
                        if(portal1 == portal2) { continue; } 
                        List<Vector2Int> path = FindPath(portal1, portal2);

                        
                    }
                }
            }
        }
        

    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        // 1. Get the clusters for start and end points
        Cluster startCluster = GetClusterFromPosition(start);
        Cluster endCluster = GetClusterFromPosition(end);

        // 2. High-level pathfinding between clusters
        List<Cluster> clusterPath = FindHighLevelPath(startCluster, endCluster);

        // 3. Low-level pathfinding within each cluster
        List<Vector2Int> finalPath = new List<Vector2Int>();
        if (startCluster != endCluster)
        {
            // A* Algorithm (converted from your C++ code)
            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
            Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();

            HashSet<Vector2Int> openSet = new HashSet<Vector2Int>();
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

            // Initialize
            gScore[start] = 0;
            fScore[start] = Heuristic(start, end);
            openSet.Add(start);

            while (openSet.Count > 0)
            {
                // Find node with lowest fScore
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

                // Found the goal
                if (current == end)
                {
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                // Check all neighbors (4-directional for grid)
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

            return new List<Vector2Int>(); // No path found
        }

        // Different clusters - hierarchical (skip for now)
        return new List<Vector2Int>();
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // Manhattan distance
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        path.Add(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private Cluster GetClusterFromPosition(Vector2Int position)
    {
        int x = position.x / clusterSize;
        int y = position.y / clusterSize;
        if (x >= 0 && x < clusters.GetLength(0) && y >= 0 && y < clusters.GetLength(1))
        {
            return clusters[x, y];
        }
        return null;
    }

    private List<Cluster> FindHighLevelPath(Cluster start, Cluster end)
    {
       List<Cluster> path = new List<Cluster>();
        if (start != null) path.Add(start);
        if (end != null && start != end) path.Add(end);
        return path;
    }
}
