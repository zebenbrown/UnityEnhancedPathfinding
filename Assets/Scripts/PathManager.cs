using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PathManager : MonoBehaviour
{
    private NavMeshSurface surface;
    private NavMeshAgent agent;
    private GameObject capsule;
    [SerializeField] private Vector3 targetPositon;
    [SerializeField] private Button button;
    private bool startMoving = false;
    
    // Start is called before the first frame update
    void Start()
    {
        button.onClick.AddListener(StartMoving);
        StartCoroutine(SpawnAgent());
    }

    // Update is called once per frame
    void Update()
    {
        if (startMoving)
        {
            PathSmoothing();
            agent.SetDestination(targetPositon);   
        }
    }

    IEnumerator SpawnAgent()
    {
        capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.transform.localScale = new Vector3(5f, 3f, 5f);
        capsule.transform.position = new Vector3(5f, 5f, 3f);
        capsule.AddComponent<CapsuleCollider>();
        agent = capsule.AddComponent<NavMeshAgent>();
        agent.autoRepath = true;
        agent.speed = 10f;
        
        
        yield return new WaitForEndOfFrame();
    }

    void PathSmoothing()
    {
        Vector3 agentCurrentPosition = agent.transform.position;
        Vector3 agentNextPosition = agent.nextPosition;
        Debug.Log("Current Next Position: " + agentNextPosition);
        agent.nextPosition = new Vector3(
                                        Mathf.Lerp(agentCurrentPosition.x, agentNextPosition.x, 0.65f),  
                                        Mathf.Lerp(agentCurrentPosition.y, agentNextPosition.y, 0.65f), 
                                        Mathf.Lerp(agentCurrentPosition.z, agentNextPosition.z, 0.65f));
        
        Debug.Log("New Next Position: " + agent.nextPosition);
    }

    public void StartMoving()
    {
        PathSmoothing();
        agent.SetDestination(targetPositon);
        startMoving = true;
    }
}
