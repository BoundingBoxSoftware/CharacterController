using UnityEngine;

[DefaultExecutionOrder(-1)]
public class PlatformMove : MonoBehaviour
{
    
    public Vector3 movement = new Vector3(0.0f, 0.0f, 0.0f);
    public float movementSpeed = 1f;
    public bool linearMovement = false;
    public float pauseAtEnd = 0.0f;
    
    Vector3 startPosition = Vector3.zero;
    Vector3 endPosition = Vector3.zero;
    float time = 0f;

    int stage = 0;
    float lerpAmount = 0f;
    
    Rigidbody thisRigidbody;

    void Awake() {
        thisRigidbody = GetComponent<Rigidbody>();
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        startPosition = transform.localPosition;
        endPosition = transform.localPosition + movement;
    }
    
    void Update() {
        if( !PhysicsManager.s_platformsUseFixedUpdate) UpdatePlatform(false, Time.deltaTime);
    }

    void FixedUpdate() {
        if( PhysicsManager.s_platformsUseFixedUpdate) UpdatePlatform(true, Time.fixedDeltaTime);
    }

    void SetPosition(bool fixedDeltaTime, Vector3 newPosition) {
        if (fixedDeltaTime) {
            if (thisRigidbody != null) {
                thisRigidbody.MovePosition(newPosition);
            } else {
                transform.localPosition = newPosition;
            }
        } else {
            transform.localPosition = newPosition;
        }
    }

    // Update is called once per frame
    void UpdatePlatform(bool fixedDeltaTime, float dTime) {

        if (linearMovement) {
            
            if (stage == 0) {
                lerpAmount += dTime * movementSpeed;
                SetPosition(fixedDeltaTime, Vector3.Lerp(startPosition, endPosition, lerpAmount));
                if (lerpAmount >= 1f) {
                    stage = 1;
                    lerpAmount = 0;
                }
                return;
            }
            
            if (stage == 1) {
                lerpAmount += dTime;
                if( lerpAmount >= pauseAtEnd) {
                    stage = 2;
                    lerpAmount = 0;
                }
                return;
            }
            
            if (stage == 2) {
                lerpAmount += dTime * movementSpeed;
                SetPosition(fixedDeltaTime, Vector3.Lerp(endPosition, startPosition, lerpAmount));
                if (lerpAmount >= 1f) {
                    stage = 3;
                    lerpAmount = 0;
                }
                return;
            }
            
            if (stage == 3) {
                lerpAmount += dTime;
                if( lerpAmount >= pauseAtEnd) {
                    stage = 0;
                    lerpAmount = 0;
                }
                return;
            }

        } else {
            time += movementSpeed * dTime;
            SetPosition(fixedDeltaTime, startPosition + movement * Mathf.Sin(time));
        }
    }

}
