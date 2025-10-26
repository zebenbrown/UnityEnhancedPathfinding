using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEngine.AI;
using UnityEngine;

namespace DefaultNamespace
{
    public class GridSystem : MonoBehaviour
    {
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private Vector3 gridSize;
        [SerializeField] private float cellSize;
        [SerializeField] private Vector3 cellPadding;
        private HashSet<Vector3> occupiedTiles = new HashSet<Vector3>();
        [SerializeField] private Material[] materials;
        private GameObject previewWall;
        private bool isVerticalWall = false;
        
        private NavMeshSurface surface;

        private void Awake()
        {
            surface = GetComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.RemoveData();
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    
                    Vector3 spawnPosition = new Vector3(x *(1 + cellPadding.x) * cellSize, 0, z * (1 + cellPadding.z) * cellSize);
                    Instantiate(tilePrefab, spawnPosition, Quaternion.identity, transform);
                }
            }
            
            surface.BuildNavMesh();
        }

        void Start()
        {
            CreatePreviewWall();
        }

        private void Update()
        {
            UpdateWallPosition();

            if (Input.GetKeyDown(KeyCode.R))
            {
                if (isVerticalWall)
                {
                    previewWall.transform.rotation = Quaternion.identity;
                    isVerticalWall = false;
                }
                else
                {
                    previewWall.transform.rotation = Quaternion.Euler(0, 90, 0);
                    isVerticalWall = true;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                PlaceWall();
            }
        }

        void CreatePreviewWall()
        {
            previewWall = Instantiate(wallPrefab);
            previewWall.GetComponent<Collider>().enabled = true;
        
            Renderer[] renderers = previewWall.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                Material material = renderer.material;
                Color color = material.color;
                color.a = 0.5f;
                material.color = color;
            
                material.SetFloat("_Mode", 2);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("ALPHABLEND_ON");
                material.DisableKeyword("ALPHATEST_ON");
                material.DisableKeyword("ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
        }

        void UpdateWallPosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 spawnPosition = hit.point;

                Vector3 snappedPosition = new Vector3(
                    Mathf.Round(spawnPosition.x / cellSize) * cellSize,
                    0,
                    Mathf.Round(spawnPosition.z / cellSize) * cellSize);
                
                previewWall.transform.position = snappedPosition;
                Vector3 nextTileRight = previewWall.transform.position + new Vector3(cellSize, 0, 0);
                
                if (occupiedTiles.Contains(snappedPosition))
                {
                    SetTileColor(Color.red);
                }
                else
                {
                    SetTileColor(Color.green);
                }
            }
        }
        
        void SetTileColor(Color color)
        {
            Renderer[] renderers = previewWall.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material material = renderer.material;
                material.color = color;
            }
            //wallPrefab.GetComponent<Renderer>().material = materials[index];
        }

        void PlaceWall()
        {
            Vector3 placementPosition = previewWall.transform.position;
            Quaternion placementRotation = previewWall.transform.rotation;
            if (!occupiedTiles.Contains(placementPosition))
            {
                Instantiate(wallPrefab, placementPosition, placementRotation);
                occupiedTiles.Add(placementPosition);
            }
        }
    }
}