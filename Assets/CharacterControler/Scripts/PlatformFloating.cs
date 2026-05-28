using UnityEngine;

[DefaultExecutionOrder(-1)]
public class PlatformFloating : MonoBehaviour
{

    public float speed = 0.5f;
    public float height = 0.5f;
    public float targetLerpSpeed = 1f;
    public float rotationLerpSpeed = 1f;
    
    public float rbPosLerpSpeed = 20f;
    public float rbRotLerpSpeed = 20f;
    
    Vector3 targetPosition = Vector3.zero;
    Quaternion targetRotation = Quaternion.identity;
    
    float pathLerp = 0f;
    
    Vector3 startPosition = Vector3.zero;
    Quaternion startRotation = Quaternion.identity;
    
    Rigidbody thisRigidbody;
    float randomValue = 0.0f;
    float time = 0.0f;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        thisRigidbody = GetComponent<Rigidbody>();
        
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        targetPosition = transform.position;
        targetRotation = transform.rotation;
        
        time = Random.Range(0f, 100f);
        randomValue = Random.Range(0.9f, 1.1f);

    }

    void Update() {
        if( !PhysicsManager.s_platformsUseFixedUpdate) UpdatePlatform(false, Time.deltaTime);
    }

    void FixedUpdate() {
        if( PhysicsManager.s_platformsUseFixedUpdate) UpdatePlatform(true, Time.fixedDeltaTime);
    }

    // Update is called once per frame
    void UpdatePlatform(bool fixedDeltaTime, float dTime) {
        
        time += speed * dTime * randomValue;
        
        targetPosition = startPosition + new Vector3(0.0f, Mathf.Sin(time * speed) * height, 0.0f);
        targetRotation = startRotation;

        Vector3 av = thisRigidbody.angularVelocity;
        av -= av * (dTime * 10.0f);
        Quaternion deltaRotation = targetRotation * Quaternion.Inverse(thisRigidbody.rotation);
        deltaRotation.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);
        if (angleInDegrees > 180f) angleInDegrees -= 360f;
        Vector3 targetAngularVelocity = rotationAxis * (angleInDegrees * Mathf.Deg2Rad);
 
        av += targetAngularVelocity * (dTime * rbRotLerpSpeed);
        thisRigidbody.angularVelocity = av;
        
        Vector3 v = thisRigidbody.linearVelocity;
        v -= v * (dTime * 2.0f);
        v += (targetPosition - thisRigidbody.position) * (dTime * rbPosLerpSpeed);
        thisRigidbody.linearVelocity = v;
    }


}
