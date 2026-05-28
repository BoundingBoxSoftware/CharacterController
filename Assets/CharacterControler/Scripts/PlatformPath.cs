using UnityEngine;

[DefaultExecutionOrder(-1)]
public class PlatformPath : MonoBehaviour
{
    public PathNode startNode;

    public float speed = 1f;
    public float targetLerpSpeed = 1f;
    public float rotationLerpSpeed = 1f;
    
    public float rbPosLerpSpeed = 10f;
    public float rbRotLerpSpeed = 10f;
    
    
    PathNode currentNode;
    Vector3 pathPosition = Vector3.zero;
    Quaternion pathRotation = Quaternion.identity;
    
    Vector3 targetPosition = Vector3.zero;
    Quaternion targetRotation = Quaternion.identity;
    
    float pathLerp = 0f;
    
    Rigidbody thisRigidbody;
    
    void Awake() {
        thisRigidbody = GetComponent<Rigidbody>();
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        
        transform.position = startNode.transform.position;
        currentNode = startNode;
        
        transform.rotation = Quaternion.LookRotation(currentNode.transform.position - currentNode.nextNode.transform.position);
        
        pathPosition = transform.position;
        pathRotation = transform.rotation;
        
        targetPosition = transform.position;
        targetRotation = transform.rotation;

    }
    
    void Update() {
        if( !PhysicsManager.s_platformsUseFixedUpdate) UpdatePlatform(false, Time.deltaTime);
    }

    void FixedUpdate() {
        if( PhysicsManager.s_platformsUseFixedUpdate) UpdatePlatform(true, Time.fixedDeltaTime);
    }

    // Update is called once per frame
    void UpdatePlatform(bool fixedDeltaTime, float dTime) {
        
        pathLerp = Mathf.Clamp01(pathLerp + dTime * speed);
        if (pathLerp >= 1f) {
            currentNode = currentNode.nextNode;
            pathLerp = 0f;
        }

        pathPosition = Vector3.Lerp(currentNode.transform.position, currentNode.nextNode.transform.position, pathLerp);
        pathRotation = Quaternion.LookRotation(currentNode.transform.position - currentNode.nextNode.transform.position);
        
        targetPosition = Vector3.Lerp(targetPosition, pathPosition, dTime * targetLerpSpeed);
        targetRotation = Quaternion.Lerp(targetRotation, pathRotation, dTime * rotationLerpSpeed);

        Vector3 av = thisRigidbody.angularVelocity;
        av -= av * (dTime * 10.0f);
        Quaternion deltaRotation = targetRotation * Quaternion.Inverse(thisRigidbody.rotation);
        deltaRotation.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);
        if (angleInDegrees > 180f) angleInDegrees -= 360f;
        Vector3 targetAngularVelocity = rotationAxis * (angleInDegrees * Mathf.Deg2Rad);;
 
        av += targetAngularVelocity * (dTime * rbRotLerpSpeed);
        thisRigidbody.angularVelocity = av;
        
        Vector3 v = thisRigidbody.linearVelocity;
        v -= v * (dTime * 2.0f);
        v += (targetPosition - thisRigidbody.position) * (dTime * rbPosLerpSpeed);
        thisRigidbody.linearVelocity = v;
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        
        Gizmos.DrawSphere(targetPosition, 0.5f);
        
    }
}
