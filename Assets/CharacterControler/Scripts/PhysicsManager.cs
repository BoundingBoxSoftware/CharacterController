using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class PhysicsManager : MonoBehaviour
{

    [Tooltip("Set The simulation mode")]
    public SimulationMode simulationMode = SimulationMode.Script;
    
    [Tooltip("Should the platforms use fixed update")]
    public bool platformsUseFixedUpdate = false;
    public static bool s_platformsUseFixedUpdate = false;
    
    [Tooltip("Should the character controller use fixed update")]
    public bool characterUseFixedUpdate = false;
    public static bool s_characterUseFixedUpdate = false;
    
    // default max time step if 50 which is Unity's default as well
    [Tooltip("The maximum amount of time that can pass in a physics update")]
    public float maxTimeStep = 0.02f;

    // The more phicis updates that happen the longer the frame will take, then the next frame may have even more physics updates
    // It's a good ide to clamp this at a reasonable value.  If you need more than 1 or 2 slices you are already having a bad time so don't make it worse.
    [Tooltip("The maximum number of physics updates that can happen in a frame")]
    public int maxStepSlices = 2;
    
    // This will turn bad performance into a sort of slow-mo for physics instead of letting things become unstable
    [Tooltip("Allow physics time to slow down instead of becoming unstable")]
    public bool allowSlowDown = true;
    
    void Awake() {
        
        // We need the physics to update every frame so that the physics time matches the game time
        // We can't use FixedUpdate because this will lead to stuttering between the physical game objects and the regular game objects
        //Physics.simulationMode = SimulationMode.FixedUpdate;

        // Using the Update mode will make the physics update AFTER the rest of the scripts update
        // we need the physics update to happen BEFORE the rest of the updates but still every frame
        //Physics.simulationMode = SimulationMode.Update;
        
        // We want the physics update to happen every frame and BEFORE everything else so call it manually
        // We don't need to use any interpolation on the rigid bodies this way
        //Physics.simulationMode = SimulationMode.Script;
        
    }

    // Update is called once per frame
    void Update() {
        
        s_platformsUseFixedUpdate = platformsUseFixedUpdate;
        s_characterUseFixedUpdate = characterUseFixedUpdate;

        if (Physics.simulationMode != simulationMode) Physics.simulationMode = simulationMode;

        if (simulationMode != SimulationMode.Script) return;
        
        
        if (Physics.simulationMode == SimulationMode.Script) {
            // Sync transforms
            Physics.SyncTransforms();
            
            // If we go over the max timeStep divide the time steps evenly
            // this will be a more stable simulation but can also lower FPS even more
            int timeSteps = Mathf.CeilToInt(Time.deltaTime / maxTimeStep);
            
            // Cap the maximum number of time slices
            if (timeSteps > maxStepSlices) timeSteps = maxStepSlices;
            float dTime = Time.deltaTime / timeSteps;
            
            // Slow down time if the max time step is exceeded
            if (allowSlowDown) dTime = Mathf.Min(dTime, maxTimeStep);
            for (int i = 0; i < timeSteps; i++) {
                Physics.Simulate(dTime);
            }
            
        }
    }
}
