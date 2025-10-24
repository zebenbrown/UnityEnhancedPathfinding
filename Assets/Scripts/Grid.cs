using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Grid : MonoBehaviour
{
    public GameObject wall;
    public float gridSize = 10f;
    private GameObject ghostObject;
    private HashSet<Vector3> occupiedPositions = new HashSet<Vector3>();

    private void Start()
    {
        CreateGhostObject();
    }

    private void Update()
    {
        UpdateGhostPosition();

        if (Input.GetMouseButtonDown(0))
        {
            PlaceObject();
        }
    }


    void CreateGhostObject()
    {
        ghostObject = Instantiate(wall);
        ghostObject.GetComponent<Collider>().enabled = true;
        
        Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();

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

    void UpdateGhostPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 point = hit.point;
            
            Vector3 snappedPosition = new Vector3(
                Mathf.Round(point.x/gridSize) * gridSize, 
                Mathf.Round(point.y/gridSize) * gridSize, 
                Mathf.Round(point.z/gridSize) * gridSize);

            
            ghostObject.transform.position = snappedPosition;

            if (occupiedPositions.Contains(snappedPosition))
            {
                SetGhostColor(Color.red);
            }
            else
            {
                SetGhostColor(new Color(1f, 1f, 1f, 0.5f));
            }
        }
    } 

    void SetGhostColor(Color color)
    {
        Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer render in renderers)
        { 
            Material material = render.material;
            material.color = color;
        }
    }

    void PlaceObject()
    {
        Vector3 placementPosition = ghostObject.transform.position;
        if (!occupiedPositions.Contains(placementPosition))
        {
            Instantiate(wall, placementPosition, Quaternion.identity);
            
            occupiedPositions.Add(placementPosition);
        }
    }
}
