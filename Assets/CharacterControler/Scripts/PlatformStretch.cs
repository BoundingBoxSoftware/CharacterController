using UnityEngine;

[DefaultExecutionOrder(-1)]
public class PlatformStratch : MonoBehaviour
{
    
    public Vector2 stretch = new Vector2(0.0f, 0.0f);
    public float stretchSpeed = 1f;
    
    Vector3 originalScale = Vector3.one;
    float time = 0f;
    
    Rigidbody thisRigidbody;

    void Awake() {
        thisRigidbody = GetComponent<Rigidbody>();
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        originalScale = transform.localScale;
    }
    
    void Update() {
        if( !PhysicsManager.s_platformsUseFixedUpdate) UpdatePlatform(false, Time.deltaTime);
    }

    void FixedUpdate() {
        if( PhysicsManager.s_platformsUseFixedUpdate) UpdatePlatform(true, Time.fixedDeltaTime);
    }
    

    void SetScale(bool fixedDeltaTime, Vector3 newLocalScale) {
        if (fixedDeltaTime) {
            transform.localScale = newLocalScale;
        } else {
            transform.localScale = newLocalScale;
        }
    }

    // Update is called once per frame
    void UpdatePlatform(bool fixedDeltaTime, float dTime) {

        time += stretchSpeed * dTime;
        Vector3 localScale = transform.localScale;
        localScale.x = originalScale.x + stretch.x * Mathf.Sin(time);
        localScale.z = originalScale.z + stretch.y * Mathf.Cos(time);
        SetScale(fixedDeltaTime, localScale);
    }
    
}
