using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor.AI;
using UnityEngine;
using UnityEngine.AI;

public class PathManager : MonoBehaviour
{
    private NavMeshSurface surface;
    private NavMeshAgent agent;
    private GameObject capsule;
    [SerializeField] private Vector3 targetPositon;
    
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(SpawnAgent());
    }

    // Update is called once per frame
    void Update()
    {
        agent.SetDestination(targetPositon);
    }

    IEnumerator SpawnAgent()
    {
        capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.transform.localScale = new Vector3(5f, 5f, 5f);
        capsule.transform.position = new Vector3(5f, 5f, 3f);
        capsule.AddComponent<CapsuleCollider>();
        agent = capsule.AddComponent<NavMeshAgent>();
        agent.autoRepath = true;
        agent.speed = 5f;
        
        
        yield return new WaitForEndOfFrame();
        
        agent.SetDestination(targetPositon);
    }
}
