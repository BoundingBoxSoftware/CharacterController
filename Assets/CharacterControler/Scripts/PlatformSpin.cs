using UnityEngine;

[DefaultExecutionOrder(-1)]
public class PlatformSpin : MonoBehaviour
{
    
    public Vector3 spinSpeed = new Vector3(0.0f, 0.0f, 0.0f);
    Vector3 currentRotation = new Vector3(0.0f, 0.0f, 0.0f);
    Rigidbody thisRigidbody;

    void Awake() {
        thisRigidbody = GetComponent<Rigidbody>();
    }
    
    void Start() {
        currentRotation = transform.eulerAngles;
    }

    void Update() {
        if( !PhysicsManager.s_platformsUseFixedUpdate) UpdatePlatform(false, Time.deltaTime);
    }

    void FixedUpdate() {
        if( PhysicsManager.s_platformsUseFixedUpdate) UpdatePlatform(true, Time.fixedDeltaTime);
    }
    
    void SetRotation(bool fixedDeltaTime, Quaternion newRotation) {
        if (fixedDeltaTime) {
            if (thisRigidbody != null) {
                if( transform.parent != null ) newRotation *= transform.parent.rotation;
                thisRigidbody.MoveRotation(newRotation);
            } else {
                transform.localRotation = newRotation;
            }
        } else {
            transform.localRotation = newRotation;
        }
    }
    
    void UpdatePlatform(bool fixedDeltaTime, float dTime) {
        
        currentRotation += spinSpeed * Time.deltaTime;
        if( currentRotation.x > 180) currentRotation.x -= 360;
        if( currentRotation.y > 180) currentRotation.y -= 360;
        if( currentRotation.z > 180) currentRotation.z -= 360;
        if( currentRotation.x < -180) currentRotation.x += 360;
        if( currentRotation.y < -180) currentRotation.y += 360;
        if( currentRotation.z < -180) currentRotation.z += 360;
        
        Quaternion spinRotation = Quaternion.Euler(currentRotation.x, currentRotation.y, currentRotation.z);
        SetRotation(fixedDeltaTime, spinRotation);

    }
    
}
