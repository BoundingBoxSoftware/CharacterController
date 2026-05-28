using System;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using NUnit.Framework.Constraints;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]

[DefaultExecutionOrder(1)]
public class CharacterControllerBase : MonoBehaviour
{

	[Header("Visuals")]
	[SerializeField] GameObject visuals;
	[SerializeField] float visualsOffsetThreshold = 0.1f;
	[SerializeField] float maxVisualsOffset = 0.5f;
	[SerializeField] float visualsLerpFactor = 20f;
	
	[Header("Debug Ground")]
	[SerializeField] bool showDebugVisuals = true;
	[SerializeField] GameObject debugVisuals;
	
	[SerializeField] bool applyWorldRotation = true;
	[SerializeField] float velocityClipThreshold = 0.1f;

	enum GroundCollisionType {
		HighestPoint,
		AverageHeight,
		Capsule
	}
	
	[SerializeField] GroundCollisionType groundCollisionType = GroundCollisionType.AverageHeight;
	
	[Header("Character Size")]
	[SerializeField] float characterHeight = 2.0f;
	[SerializeField] float characterRadius = 0.5f;
	[SerializeField] float stepHeight = 0.5f;
	[SerializeField] float floorFudge = 0.1f;
	
	[Header("Slope Angles")]
	[SerializeField] float minSlope = 30.0f;
	[SerializeField] float maxSlope = 60.0f;

	[Header("Max Speeds")]
	[SerializeField] float maxWalkSpeed = 10.0f;
	[SerializeField] float maxRunSpeed = 10.0f;

	[Header("Drag for Air Movement and Falling")]
	[SerializeField] float airDrag = 1.0f;
	[SerializeField] float fallDrag = 0.25f;

	[Header("Acceleration for Ground and Air")]
	[SerializeField] float groundAcc = 7.0f;
	[SerializeField] float airAcc = 2.0f;

	[Header("Jumping")] 
	[SerializeField] bool useJumpCurve = false;
	[SerializeField] float jumpTime = 0.5f;
	float jumpTimer = 0.0f;
	[SerializeField] AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
	[SerializeField] float jumpCurveForce = 1.0f;
	[SerializeField] float jumpSpeed = 7.0f;
	[SerializeField] float jumpCoolDownTime = 0.1f;
	[SerializeField] float footstepDistance = 3.0f;
	
	[SerializeField] float coyoteTime = 0.1f;

	[Header("Add More Gravity")]
	[SerializeField] float gravityMultiply = 1.0f;
	
	[SerializeField] LayerMask worldMask;
	
	[SerializeField] CharacterAudio characterAudio;

	
	// Cached variables
	float capsuleCenterLower = 0f;
	float capsuleCenterUpper = 0f;
	float capsuleCenterMiddle = 0f;
	float capsuleHeightInner = 0f;
	float sphereCastRadiusInner = 0f;
	float sphereCastRadiusThickness = 0f;
	float sphereCastRadiusOuter = 0f;
	
	Vector3[] verticalSamplePoints;
	Vector3[] groundSamplePoints;
	int[] groundSampleTris;
	int groundSampleTriCount = 0;

	bool[] groundSampleHits;
	Vector3[] groundSampleHitPoints;

	Transform parentHelper = null;
	Vector3 parentHelperLastPos = Vector3.zero;
	Rigidbody thisRigidbody = null;
	CapsuleCollider thisCollider = null;
	
	// State variables
	
	Vector3 velocity = Vector3.zero;
	Vector3 groundNormal = Vector3.up;
	float groundHeight = 0f;
	
	bool inGround = false;
	bool inGroundPrev = false;
	
	bool touchingGround = false;
	bool touchingGroundPrev = false;
	bool grounded = false;
	float airTime = 0f;
	
	Vector3 moveVector = Vector3.zero;
	bool inputJumpIsPressed = false;
	
	bool inputJump = false;
	float lastInputJump = 0f;
	float jumpCoolDownTimer = 0.0f;
	bool canDoubleJump = false;
	bool jumping = false;
	
	float footstepDistanceTraveled = 0f;

	Vector3 worldVelocity = Vector3.zero;

	[ContextMenu("Test Issue")]
	public void TestIssue() {
		transform.position = new Vector3(6f, 2f, 10f);
		thisRigidbody.position = new Vector3(6f, 2f, 10f);
		thisRigidbody.linearVelocity = new Vector3(-20f, -4f, 20f);
		inGround = false;
		touchingGround = false;
		grounded = false;
		SetParent(null, false);
	}
	
	void OnValidate() {
		thisCollider = transform.GetComponent<CapsuleCollider>();
		thisRigidbody = transform.GetComponent<Rigidbody>();
		
		thisRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
		
		thisCollider.radius = characterRadius;
		thisCollider.height = characterHeight - stepHeight;
		
		float verticalOffset = (characterHeight - stepHeight) * 0.5f + stepHeight;
		thisCollider.center = new Vector3(0, verticalOffset, 0);

		capsuleCenterLower = stepHeight + characterRadius;
		capsuleCenterUpper = characterHeight - characterRadius;
		capsuleCenterMiddle = (capsuleCenterLower + capsuleCenterUpper) * 0.5f;
		capsuleHeightInner = capsuleCenterUpper - capsuleCenterLower;

		sphereCastRadiusInner = characterRadius * 0.9f;
		sphereCastRadiusOuter = characterRadius * 1.11111f;

		sphereCastRadiusThickness = characterRadius - sphereCastRadiusInner;
	}
	
	void Awake() {
		OnValidate();
		SetParent(null, false);

		verticalSamplePoints = new Vector3[3];
		verticalSamplePoints[0] = new Vector3(0f, capsuleCenterLower, 0f);
		verticalSamplePoints[1] = new Vector3(0f, capsuleCenterMiddle, 0f);
		verticalSamplePoints[2] = new Vector3(0f, capsuleCenterUpper, 0f);
		
		groundSamplePoints = new Vector3[13];
		groundSamplePoints[0] = new Vector3(0.0f, 0f, 1.0f);
		groundSamplePoints[1] = new Vector3(0.7f, 0f, 0.7f);
		groundSamplePoints[2] = new Vector3(1.0f, 0f, 0.0f);
		groundSamplePoints[3] = new Vector3(0.7f, 0f, -0.7f);
		groundSamplePoints[4] = new Vector3(0.0f, 0f, -1.0f);
		groundSamplePoints[5] = new Vector3(-0.7f, 0f, -0.7f);
		groundSamplePoints[6] = new Vector3(-1.0f, 0f, 0.0f);
		groundSamplePoints[7] = new Vector3(-0.7f, 0f, 0.7f);
		groundSamplePoints[8] = new Vector3(0.0f, 0f, 0.5f);
		groundSamplePoints[9] = new Vector3(0.5f, 0f, 0.0f);
		groundSamplePoints[10] = new Vector3(0.0f, 0f, -0.5f);
		groundSamplePoints[11] = new Vector3(-0.5f, 0f, 0.0f);
		groundSamplePoints[12] = new Vector3(0.0f, 0f, 0.0f);
		
		
		groundSampleHits = new bool[13];
		groundSampleHitPoints = new Vector3[13];
		for (int i = 0; i < groundSamplePoints.Length; i++) {
			groundSamplePoints[i] = groundSamplePoints[i] * sphereCastRadiusInner;
			groundSampleHits[i] = false;
			groundSampleHitPoints[i] = Vector3.zero;
			//Debug.Log(groundSamplePoints[i]);
		}

		groundSampleTriCount = 16;
		
		List<int> groundSampleTrisList = new List<int>();
		AddTriangle(groundSampleTrisList, 0, 1, 8);
		AddTriangle(groundSampleTrisList, 8, 1, 9);
		AddTriangle(groundSampleTrisList, 9, 1, 2);
		AddTriangle(groundSampleTrisList, 9, 2, 3);
		AddTriangle(groundSampleTrisList, 10, 9, 3);
		AddTriangle(groundSampleTrisList, 4, 10, 3);
		AddTriangle(groundSampleTrisList, 5, 10, 4);
		AddTriangle(groundSampleTrisList, 11, 10, 5);
		AddTriangle(groundSampleTrisList, 6, 11, 5);
		AddTriangle(groundSampleTrisList, 6, 7, 11);
		AddTriangle(groundSampleTrisList, 7, 8, 11);
		AddTriangle(groundSampleTrisList, 7, 0, 8);
		AddTriangle(groundSampleTrisList, 8, 9, 12);
		AddTriangle(groundSampleTrisList, 12, 9, 10);
		AddTriangle(groundSampleTrisList, 11, 12, 10);
		AddTriangle(groundSampleTrisList, 11, 8, 12);
		
		groundSampleTris = groundSampleTrisList.ToArray();
	}

	private void AddTriangle(List<int> tris, int a, int b, int c) {
		tris.Add(a);
		tris.Add(b);
		tris.Add(c);
	}
	
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update() {
	    float dTime = Time.deltaTime;
	    //GetInputs(dTime);
	    if (!PhysicsManager.s_characterUseFixedUpdate) {
		    WorldMovement(dTime, false);
		    CheckGround(dTime, false);
		    Movement(dTime, false);
	    }
	    FixVisualPosition(dTime);
    }

    void FixedUpdate() {
	    if (PhysicsManager.s_characterUseFixedUpdate) {
		    float dTime = Time.fixedDeltaTime;
		    WorldMovement(dTime, true);
		    CheckGround(dTime, true);
		    Movement(dTime, true);
	    }
    }

    private void FixVisualPosition(float dTime) {
	    Vector3 visualsPos = visuals.transform.localPosition;
	    visualsPos.y -= visualsPos.y * dTime * visualsLerpFactor;
	    visuals.transform.localPosition = visualsPos;
    }

    public void InputMoveVector(Vector3 newMoveVector) {
	    moveVector = newMoveVector;
    }

    public Vector3 GetMoveVector() {
	    return moveVector;
    }
    
    public void InputJump(bool jumpWasPressedThisFrame, bool jumpIsPressed) {
	    float dTime = Time.deltaTime;
	    
	    inputJump = false;
	    inputJumpIsPressed = jumpIsPressed;
	    
	    // keep track of the last time we pressed jump
	    lastInputJump += dTime;
	    if (jumpWasPressedThisFrame) {
		    lastInputJump = 0f;
	    }

	    // if we pressed jump a short enough time before we could honor that input when we can jump
	    if (jumpCoolDownTimer <= 0f) {
		    if (lastInputJump <= 0.1f) {
			    inputJump = true;
		    }
	    } else {
		    jumpCoolDownTimer -= dTime;
	    }
    }
    
    private void SetParent(Transform parentTransform, bool fixedUpdate) {

	    if (parentHelper == null) {
		    parentHelper = new GameObject("CC_Parent_Helper").transform;
		    ResetParent(fixedUpdate);
	    }

	    parentHelper.SetParent(parentTransform);

	}

	private void ResetParent(bool fixedUpdate) {

		if (fixedUpdate) {
			parentHelper.position = thisRigidbody.position;
		} else {
			parentHelper.position = transform.position;
		}

		parentHelper.localScale = Vector3.one;
		parentHelper.rotation = Quaternion.identity;
		parentHelperLastPos = parentHelper.position;
	}
	
	//======================================//
	//	add the movement from the parent	//
	//======================================//
	
    public void WorldMovement(float dTime, bool fixedUpdate) {
	    
    	if (parentHelper.parent == null) {
		    worldVelocity = Vector3.zero;
		    ResetParent(fixedUpdate);
    		return;
    	}
	    
    	// Apply rotation to the visuals
	    if (applyWorldRotation) {
		    Vector3 parentRotation = Quaternion.LookRotation(parentHelper.forward).eulerAngles;
		    Vector3 baseRotation = visuals.transform.eulerAngles;
		    baseRotation.y += parentRotation.y;
		    visuals.transform.rotation = Quaternion.Euler(baseRotation);
	    }

	    Vector3 parentMovement = parentHelper.position - parentHelperLastPos;
	    worldVelocity = parentMovement / dTime;
	    
	    if (parentMovement.Equals(Vector3.zero)) {
		    ResetParent(fixedUpdate);
		    return;
	    }
	    
	    // subtract parent XZ movement from the character if we just landed, this puts the velocity in local space
	    // subtracting Y movement can lead to the character popping up if the ground is a rigidbody that the character just applied a landing impulse to
	    if (touchingGround && touchingGroundPrev == false) {
		    Vector3 rbVelocity = thisRigidbody.linearVelocity;
		    rbVelocity.x -= worldVelocity.x;
		    rbVelocity.z -= worldVelocity.z;
		    thisRigidbody.linearVelocity = rbVelocity;
		    
		    Debug.DrawRay(transform.position + Vector3.up, worldVelocity, Color.yellow, 1f);
		    Debug.DrawRay(parentHelper.position, Vector3.up * 5f, Color.yellow, 1f);
		    Debug.DrawRay(parentHelperLastPos, Vector3.up * 5f, Color.yellow, 1f);
	    }

	    Vector3 parentMovementDir = parentMovement.normalized;
    	float parentMovementLen = parentMovement.magnitude;
	    
    	// clip the movement against the world
	    // this keeps moving platforms from moving the player through a wall
    	Ray ray = new Ray();
    	RaycastHit hit = new RaycastHit();
    	for (int i = 0; i < verticalSamplePoints.Length; i++) {
    		Vector3 newOrigin = transform.position + verticalSamplePoints[i];
    		ray.origin = newOrigin;
    		ray.direction = parentMovementDir;

    		if (Physics.SphereCast(newOrigin, sphereCastRadiusInner, parentMovementDir, out hit, parentMovementLen + sphereCastRadiusThickness, worldMask, QueryTriggerInteraction.Ignore)) {
			    parentMovementLen = hit.distance - sphereCastRadiusThickness;
    		}

    		if (parentMovementLen <= 0f) {
    			parentMovementLen = 0f;
    			break;
    		}
    	}
	    
    	// apply the clipped movement
	    if (fixedUpdate) {
		    thisRigidbody.MovePosition(thisRigidbody.position + parentMovementDir * parentMovementLen);
	    } else {
		    transform.position += parentMovementDir * parentMovementLen;
		    //thisRigidbody.position += parentMovementDir * parentMovementLen;
	    }

	    ResetParent(fixedUpdate);

    }

    private Vector3 AddGravity(Vector3 thisVelocity, float dTime) {
	    thisVelocity.y -= 9.8f * gravityMultiply * dTime;
	    return thisVelocity;
    }
    
    //==================================================//
    //	check to see if we are touching ground			//
    //	set the characters position above the ground	//
    //==================================================//
    
    private void CheckGround(float dTime, bool fixedUpdate) {

	    // Get our current position
		Vector3 currentPos = transform.position;
		if (fixedUpdate) {
			currentPos = thisRigidbody.position;
		}
		
		touchingGroundPrev = touchingGround;
		touchingGround = false;
		
		inGroundPrev = inGround;
		inGround = false;
			
		Transform groundTransform = null;
		groundNormal = Vector3.up;
		groundHeight = currentPos.y;


		int groundPoints = 0;
		float groundHeightAvg = 0f;
		Vector3 groundNormalAverage = Vector3.zero;
		float highestPoint = -999999f;
		
		Ray ray = new Ray();
		RaycastHit hit = new RaycastHit();
		Vector3 rayOrigin = currentPos + Vector3.up * (stepHeight + characterRadius);
		ray.direction = Vector3.down;
		float rayGroundLength = (stepHeight * 4f) + characterRadius + floorFudge;
		
		for (int i = 0; i < groundSamplePoints.Length; i++) {
			ray.origin = rayOrigin + groundSamplePoints[i];
			if( Physics.Raycast(ray, out hit, rayGroundLength, worldMask, QueryTriggerInteraction.Ignore) ) {
				
				// skip unwalkable slopes and let physics handle the slide
				float groundAngle = Vector3.Angle(Vector3.up, hit.normal);
				if (groundAngle > maxSlope) {
					groundSampleHits[i] = false;
					groundSampleHitPoints[i] = Vector3.zero;
					continue;
				}
				
				// store the samples for later
				groundSampleHits[i] = true;
				groundSampleHitPoints[i] = hit.point;

				// accumulate ground normal for average normal fallback
				// accumulate ground height for average height calculation
				groundNormalAverage += hit.normal;
				groundHeightAvg += hit.point.y;
				groundPoints++;
				
				Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);

				// set the touching ground flag if the ground is close enough
				if (hit.point.y >= currentPos.y - floorFudge) {
					touchingGround = true;
				}
				
				// get the highest point to use as the parent
				if (hit.point.y > highestPoint) {
					groundTransform = hit.transform;
					highestPoint = hit.point.y;
				}
			} else {
				// clear the samples if the ray didn't hit anything
				groundSampleHits[i] = false;
				groundSampleHitPoints[i] = Vector3.zero;
			}
		}
		
		// use the average ray y position as the ground height
		// average the normal as a fallback for the normal calculation
		if (groundPoints > 0) {
			groundHeight = groundHeightAvg / groundPoints;
			groundNormal = groundNormalAverage / groundPoints;
		}
		
		// get the velocity
		velocity = thisRigidbody.linearVelocity;
		
		if (touchingGround) {

			float desiredMovement = 0f;
			
			if (groundCollisionType == GroundCollisionType.HighestPoint) {
				desiredMovement = highestPoint - currentPos.y;
			}
			
			if (groundCollisionType == GroundCollisionType.AverageHeight) {
				desiredMovement = groundHeight - currentPos.y;
			}

			if (groundCollisionType == GroundCollisionType.Capsule) {
				ray.origin = currentPos + new Vector3(0f, capsuleCenterLower, 0f);
				ray.direction = Vector3.down;
				float rayRadius = sphereCastRadiusInner;
				float rayLength = stepHeight + sphereCastRadiusOuter;
			
				if (Physics.SphereCast(ray, rayRadius, out hit, rayLength, worldMask, QueryTriggerInteraction.Ignore)) {
					Debug.DrawLine(ray.origin, ray.origin + ray.direction * hit.distance, Color.red);
					desiredMovement = Mathf.Max(0.0f, rayLength - hit.distance);
				} else {
					Debug.DrawLine(ray.origin, ray.origin + ray.direction * rayLength, Color.green);
				}

			}

			// calculate the ground normal based on raycasts
			groundNormal = CalculateGroundNormal(groundNormal);
			
			// show the debug grounn visuals
			DebugGroundVisuals(true, currentPos, groundNormal, groundHeight);
			
			// move rigid body if needed
			if (desiredMovement > 0f) {

				inGround = true;
				
				ray.origin = currentPos + new Vector3(0f, capsuleCenterLower, 0f);
				ray.direction = Vector3.up;
				float rayRadius = sphereCastRadiusInner;
				float rayLength = capsuleHeightInner + desiredMovement + sphereCastRadiusOuter;
				
				// make sure we are not obstructed and clip the movement
				if (Physics.SphereCast(ray, rayRadius, out hit, rayLength, worldMask, QueryTriggerInteraction.Ignore)) {
					Debug.DrawLine(ray.origin, ray.origin + ray.direction * hit.distance, Color.red);
					float hitDif = rayLength - hit.distance;
					desiredMovement = Mathf.Max(0.0f, desiredMovement - hitDif);
				} else {
					Debug.DrawLine(ray.origin, ray.origin + ray.direction * rayLength, Color.green);
				}
				
				// move the character out of the ground
				if (fixedUpdate) {
					thisRigidbody.MovePosition(new Vector3(thisRigidbody.position.x, thisRigidbody.position.y + desiredMovement, thisRigidbody.position.z));
				} else {
					transform.position = new Vector3(transform.position.x, transform.position.y + desiredMovement, transform.position.z);
				}
				
				// move the visuals down so it does not pop up, only do this if the distance exceeds the threshold
				if (desiredMovement > visualsOffsetThreshold) {
					Vector3 visualsPos = visuals.transform.localPosition;
					visualsPos.y -= desiredMovement;
					// don't let the visuals get too far down
					if (visualsPos.y < -maxVisualsOffset) {
						visualsPos.y = -maxVisualsOffset;
					}
					visuals.transform.localPosition = visualsPos;
				}

				// get the ridged body of what we are standing on
				Rigidbody groundRigidbody = groundTransform?.GetComponent<Rigidbody>();
				if (groundRigidbody != null) {
					// push rigid bodies down when we walk on them
					groundRigidbody.AddForceAtPosition(Vector3.down * (thisRigidbody.mass * 9.8f * gravityMultiply * dTime), currentPos, ForceMode.Impulse);

					// add impact force if we just landed
					if (!inGroundPrev && velocity.y < 0f) {
						float impactForce = thisRigidbody.mass * -velocity.y;
						groundRigidbody.AddForceAtPosition(Vector3.down * impactForce, currentPos, ForceMode.Impulse);
					}
				}

				// stop vertical velocity but let it slide up or down a slope if we are moving.
				if (velocity.y < 0f && (velocity.x > 0.01f || velocity.x < -0.01f || velocity.z > 0.01f || velocity.z < -0.01f)) {
					Vector3 groundCross = Vector3.Cross(groundNormal, velocity);
					Vector3 groundMove = Vector3.Cross(groundCross, groundNormal);
					velocity.y = Mathf.Max(groundMove.y, velocity.y);
				} else {
					velocity.y = Mathf.Max(0f, velocity.y);
				}

			}
			
			// set the current parent
			SetParent(groundTransform, fixedUpdate);
			
		} else {
			// If we just left the ground add the worlds velocity to the character velocity
			if (touchingGroundPrev) {
				velocity += worldVelocity;
			}
			
			// clear the parent
			SetParent(null, fixedUpdate);
			
			// hide the debug ground
			DebugGroundVisuals(false, currentPos, groundNormal, groundHeight);
		}
		
		// always add gravity
		velocity = AddGravity(velocity, dTime); 
		
		thisRigidbody.linearVelocity = velocity;

	}
    
	RaycastHit[] nonAllocHits = new RaycastHit[100];
    
    // Clips the velocity against the world.  This is helpful when using concave colliders or when your time step gets high.
	Vector3 ClipVelocity(Vector3 thisVelocity, float dTime) {

		for (int i = 0; i < verticalSamplePoints.Length; i++) {

			// get the distance we are traveling this frame
			float velocityDist = thisVelocity.magnitude * dTime;

			// if the distance we are traveling is low just return it.
			if (velocityDist < velocityClipThreshold) return thisVelocity;

			Vector3 newOrigin = transform.position + verticalSamplePoints[i];

			if (PhysicsManager.s_characterUseFixedUpdate) {
				RaycastHit[] hits = Physics.SphereCastAll(newOrigin, sphereCastRadiusInner, thisVelocity, velocityDist + sphereCastRadiusThickness, worldMask, QueryTriggerInteraction.Ignore);

				float closestDist = velocityDist - sphereCastRadiusThickness;
				bool hitSomething = false;

				for (int j = 0; j < hits.Length; j++) {

					RaycastHit hit = hits[j];
					
					//don't clip velocity against a dynamic rigid body
					Rigidbody hitRb = hit.transform.GetComponent<Rigidbody>();
					if (hitRb != null) {
						if (!hitRb.isKinematic) continue;
					}

					if (hit.distance < closestDist) {
						hitSomething = true;
						closestDist = hit.distance;
					}

				}

				if (hitSomething) {
					thisVelocity *= (closestDist - sphereCastRadiusThickness) / velocityDist;
					Debug.DrawLine(newOrigin, newOrigin + thisVelocity, Color.red, 5.0f);
				}

			} else{

				if (Physics.SphereCast(newOrigin, sphereCastRadiusInner, thisVelocity, out RaycastHit hit, velocityDist + sphereCastRadiusThickness, worldMask, QueryTriggerInteraction.Ignore)) {
					thisVelocity *= (hit.distance - sphereCastRadiusThickness) / velocityDist;
					Debug.DrawLine(newOrigin, newOrigin + thisVelocity, Color.red, 5.0f);
				}

			}

		}
		
		return thisVelocity;
	}
    
	// a helpful math function like smooth step but linear
	float LinearStep( float min, float max, float val ){
		return Mathf.Clamp01( ( val - min ) * ( 1.0f / ( max - min ) ) );
	}

	// lowers the movement vector based on the slope
	Vector3 InhibitMovementAgainstSlope(Vector3 thisMoveVector) {
		float slopeMoveAngle = 90f - Vector3.Angle(-thisMoveVector.normalized, groundNormal);
		float slopeMoveAngleMapped = LinearStep(minSlope, maxSlope, slopeMoveAngle);
		thisMoveVector *= 1.0f - slopeMoveAngleMapped * slopeMoveAngleMapped;
		return thisMoveVector;
	}

	// conforms the movement vector to the slope
	// this keeps the character from hopping down slopes and stairs
	Vector3 ConformMovementAgainstSlope(Vector3 thisMoveVector) {
		Vector3 groundCross = Vector3.Cross(groundNormal, thisMoveVector);
		if (groundCross.sqrMagnitude < 0.001f) return thisMoveVector;  // the ground is flat
		Vector3 groundMove = Vector3.Cross(groundCross, groundNormal);
		thisMoveVector = groundMove;
				
		// normalize the XY speed
		Vector3 thisMoveVectorXY = new Vector3(thisMoveVector.x, 0f, thisMoveVector.z);
		thisMoveVector *= thisMoveVector.magnitude / thisMoveVectorXY.magnitude;
		
		return thisMoveVector;
	}

	void SetGrounded() {
		// play land sound
		if (!grounded) MovementLand();
			
		// set grounded
		grounded = true;
				
		// allow double jump
		canDoubleJump = true;

		// no longer Jumping
		jumping = false;

		// reset air time
		airTime = 0f;
	}
	
	//==========================================//
	//	all things player controlled movement.	//
	//==========================================//

	void Movement(float dTime, bool fixedUpdate) {
		
		// Get the current velocity of the rigid body
		velocity = thisRigidbody.linearVelocity;
		
		if (touchingGround) {
			// if we are jumping wait until we collide with the ground and the jump timer is done
			if (jumping) {
				if (inGround && jumpCoolDownTimer <= 0f) {
					SetGrounded();
				}
			} else {
				SetGrounded();
			}

		} else {

			// add to airTime
			airTime += dTime;
			if (airTime > coyoteTime) {
				grounded = false;
			}

		}
		
		//==========================================//
		//	build acceleration based on difference	//
		//	between current speed and top speed		//
		//==========================================//
		
		// velocityXZ is the game movement
		Vector3 velocityXZ = velocity;
		velocityXZ.y = 0;

		// set current max speed to walk speed
		float currentMaxSpeed = maxRunSpeed;
		
		// apply movement input
		if (touchingGround) {
			
			if (moveVector.sqrMagnitude > 0.01f) {
				moveVector = InhibitMovementAgainstSlope(moveVector);
				moveVector = ConformMovementAgainstSlope(moveVector);
				Debug.DrawRay(transform.position, moveVector, Color.red);
			} else {
				moveVector = Vector3.zero;
			}

			// use target velocity instead of drag and acceleration
			Vector3 targetVelocity = moveVector * currentMaxSpeed;
			targetVelocity.y = Mathf.Min(targetVelocity.y, 0f); // stop from hopping up ramps
			targetVelocity.y += velocity.y; // add current velocity to stop hopping down ramps
			velocity += (targetVelocity - velocity) * (groundAcc * dTime);

			// do footstep stuff
			footstepDistanceTraveled += velocityXZ.magnitude * dTime;
			if (footstepDistanceTraveled >= footstepDistance) {
				characterAudio?.PlayStep();
				footstepDistanceTraveled = 0f;
			}
			
			if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z)) {
				Debug.LogError(velocity);
			}

		} else {

			// acceleration in respect to current velocity
			float vDot = Vector3.Dot(moveVector, velocityXZ);
			float accDif = (moveVector.magnitude * currentMaxSpeed) - vDot;

			// add air drag
			velocity -= velocityXZ * (airDrag * dTime);
			// add fall drag
			velocity.y -= velocity.y * fallDrag * dTime;
			// add movement
			velocity += moveVector * (airAcc * accDif * dTime);
			
			// no footsteps in the air
			footstepDistanceTraveled = 0f;
			
			if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z)) {
				Debug.LogError(velocity);
			}
		}

		// jumping
		MovementJump(dTime); 
		
		if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z)) {
			Debug.LogError(velocity);
		}
		
		// clip velocity
		velocity = ClipVelocity(velocity, dTime);
		
		if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z)) {
			Debug.LogError(velocity);
		} else {
			// Update the Rigid Body velocity
			thisRigidbody.linearVelocity = velocity;
		}
	

	}


	private void MovementJump(float dTime) {
		if (useJumpCurve) {
			MovementJumpCurve(dTime);
		} else {
			MovementJumpInstant(dTime);
		}
		
	}

	private void MovementJumpInstant(float dTime) {
		if (!inputJump) return;
		
		if (grounded) {
			jumpCoolDownTimer = 0.1f;
			grounded = false;
			jumping = true;
			velocity.y = jumpSpeed;
			characterAudio?.PlayJump();
			return;
		}

		if (canDoubleJump) {
			jumpCoolDownTimer = 0.1f;
			grounded = false;
			jumping = true;
			canDoubleJump = false;
			velocity.y = jumpSpeed;
			characterAudio?.PlayJump();
			return;
		}
	}

	private void MovementJumpCurve(float dTime) {
		
		if (jumping) {
			if (!inputJumpIsPressed) {
				jumping = false;
				jumpTimer = 0f;
				return;
			}

			jumpTimer += dTime;
			if (jumpTimer > jumpTime) {
				jumping = false;
				jumpTimer = 0f;
				return;
			}
			
			float timeFraction = jumpTimer / jumpTime;
			velocity.y += jumpCurve.Evaluate(timeFraction) * jumpCurveForce * dTime;
			
			return;
		}
		
		if (!inputJump) return;
		
		if (grounded) {
			jumpCoolDownTimer = 0.1f;
			jumpTimer = 0f;
			grounded = false;
			jumping = true;
			
			velocity.y = jumpSpeed;
			characterAudio.PlayJump();
			return;
		}

		if (canDoubleJump) {
			jumpCoolDownTimer = 0.1f;
			grounded = false;
			jumping = true;
			canDoubleJump = false;
			velocity.y = jumpSpeed;
			characterAudio?.PlayJump();
			return;
		}
	}
	

	private void MovementLand() {
		// If we were in the air for more time play a land sound
		// if we were in the air for less time play a step sound
		// if we were not in the air for very long at all do not play a sound so sounds don't get spammed
		if (airTime > 0.3f) {
			characterAudio?.PlayLand();
		} else if( airTime > 0.1f){
			characterAudio?.PlayStep();
		}
	}

	Vector3 CalculateGroundNormal(Vector3 groundNormal) {

		int normalCount = 0;
		Vector3 normals = Vector3.zero;
		
		for (int i = 0; i < groundSampleTriCount; i++) {
			int tri = i * 3;
			
			if( !groundSampleHits[groundSampleTris[tri]]) continue;
			if( !groundSampleHits[groundSampleTris[tri+1]]) continue;
			if( !groundSampleHits[groundSampleTris[tri+2]]) continue;
			
			Vector3 point0 = groundSampleHitPoints[groundSampleTris[tri]];
			Vector3 point1 = groundSampleHitPoints[groundSampleTris[tri+1]];
			Vector3 point2 = groundSampleHitPoints[groundSampleTris[tri+2]];
			
			Debug.DrawLine(point0, point1, Color.green);
			Debug.DrawLine(point1, point2, Color.green);
			Debug.DrawLine(point2, point0, Color.green);
			
			Vector3 newNormal = Vector3.Cross(point1 - point0, point2 - point0);
			if (newNormal.sqrMagnitude == 0f) continue;
			if (newNormal.y < 0f) newNormal = -newNormal;
			newNormal.Normalize();
			normals += newNormal;
			normalCount++;
		}
		
		if(normalCount == 0) return groundNormal;
		
		groundNormal = (normals / normalCount).normalized;
		
		return groundNormal;
	}

	private void DebugGroundVisuals(bool isActive, Vector3 debugPos, Vector3 groundNormal, float groundHeight) {
		if (!showDebugVisuals) {
			if (debugVisuals.activeSelf) debugVisuals.SetActive(false);
			return;
		}
		
		debugVisuals.SetActive(isActive);
		
		debugPos.y = groundHeight;
		debugVisuals.transform.position = debugPos;
			
		Quaternion debugRotation = Quaternion.LookRotation(groundNormal, transform.right);
		debugVisuals.transform.rotation = debugRotation;
	}


}
