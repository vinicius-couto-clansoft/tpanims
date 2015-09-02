using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour {
	private const float MAX_SPEED = 30f;	// Defined according to "run" animation
	private iTween.EaseType FADE_EASETYPE = iTween.EaseType.easeOutCubic;

	public enum Mode { FollowClick, AddToList };
	[Header("Path building")]
	public Mode MoveMode = Mode.FollowClick;
	public float minDistanceBetweenPoints;

	public enum Constraint { Time, Speed };
	[Header("Movement")]
	public Constraint constraint;

	[Range(0f, MAX_SPEED)]public float currentSpeed;
	[Range(0f, MAX_SPEED)]public float moveSpeed;	// How fast should the player run.
	public float acceleration = 30f;
	public float moveTime;	// How long should it take to run the whole path. Speed is capped at MAX_SPEED
	public bool isPaused = false;
	public float playbackSpeed = 1f;

	[HideInInspector] public List<Vector3> pathNodes;

	[Header("Objects references")]
	public Collider fieldBounds;
	private bool inField = true;

	private Animator animator;
	public GameObject meshRenderer;

	private bool isMoving = false;
	private bool isAccelerating = false;

	private enum AccelerationConstraint { Time, Space, SetValue }
	
	private void Awake()
	{
		animator = GetComponent<Animator> ();
	}
	
	private void Update()
	{
		if(Input.GetMouseButton(0) && !isMoving && !isAccelerating)
		{
			RaycastHit rayHit;
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if(Physics.Raycast(ray, out rayHit))
			{
				ClickedPoint(rayHit.point);
			}
		}

		if(Input.GetKeyDown(KeyCode.Space))
		{
			if(!isMoving && !isAccelerating)
			{
				isPaused = false;
				StartCoroutine(MoveThroughPath(pathNodes));
			} else
			{
				isPaused = !isPaused;
				animator.enabled = !isPaused;
			}
		}

		if(Input.GetKeyDown(KeyCode.KeypadPlus))
		{
			IncreasePlaybackSpeed();
		}

		if(Input.GetKeyDown(KeyCode.KeypadMinus))
		{
			DecreasePlaybackSpeed();
		}

		if(fieldBounds.bounds.Contains(transform.position) && !inField)
		{
			FadeIn ();
			inField = true;
		} else if (!fieldBounds.bounds.Contains(transform.position) && inField)
		{
			FadeOut ();
			inField = false;
		}

		animator.speed = playbackSpeed;
		animator.SetFloat("speed", currentSpeed);
	}

	private void ClickedPoint(Vector3 point)
	{
		AddPointToList (point);
		if(MoveMode == Mode.FollowClick && pathNodes.Count > 1)
		{
			StartCoroutine(MoveThroughPath(pathNodes));
		}
	}

	private void AddPointToList(Vector3 point)
	{
		// If it´s the first point add current position to list.
		if(pathNodes.Count == 0)
		{
			pathNodes.Add(transform.position);
		}

		float distance = (pathNodes [pathNodes.Count - 1] - point).magnitude;

		// Only adds points if distance is greater than the minimal distance.
		if(distance < minDistanceBetweenPoints)
		{
			return;
		} else
		{
			pathNodes.Add (point);
		}
	}

	//TODO: tentar usar um cronometro e implementar a desaceleração de acordo com o tempo.

	public IEnumerator MoveThroughPath(List<Vector3> path)
	{
		isMoving = true;
		Vector3[] waypoints = path.ToArray ();
		float pathLength = iTween.PathLength(waypoints);
		float currentPercent = 0f;
		float absOnePercent = pathLength * 0.01f;
		bool isDeaccelerating = false;
		float targetSpeed = moveSpeed;

		// Adjusts target speed according to time, pathLength and acceleration.
		if(constraint == Constraint.Time)
		{
			targetSpeed = 30f * moveTime - Mathf.Sqrt(acceleration * acceleration * moveTime * moveTime - 2 * acceleration * pathLength);
		}
		targetSpeed = Mathf.Min (targetSpeed, MAX_SPEED);	// Limits speed to maxSpeed

		IEnumerator accelerationCoroutine = AccelerateTo (targetSpeed, AccelerationConstraint.SetValue, acceleration);
		StartCoroutine(accelerationCoroutine);

		while(currentPercent < 1f)
		{
			if(!isPaused)
			{
				float relativeOnePercent = Vector3.Distance(iTween.PointOnPath(waypoints, currentPercent), iTween.PointOnPath(waypoints, currentPercent + 0.01f));
				float distortion = absOnePercent/relativeOnePercent;
				currentPercent = (currentSpeed == 0) ? 1f : currentPercent + currentSpeed * distortion * playbackSpeed  * Time.deltaTime/pathLength;

				iTween.PutOnPath(gameObject, waypoints, currentPercent);
				transform.LookAt(iTween.PointOnPath(waypoints, currentPercent + 0.01f));

//				float brakeThresholdPercent = 1f - distortion * (targetSpeed * targetSpeed * 0.5f/30f)/pathLength;
//				Vector3 brakeThresholdPoint = iTween.PointOnPath(waypoints, brakeThresholdPercent);

//				if(Vector3.Distance(transform.position, brakeThresholdPoint) <= 0.4f && !isDeaccelerating)
//				{
//					StopCoroutine(accelerationCoroutine);
//					accelerationCoroutine = AccelerateTo (0f, AccelerationConstraint.SetValue, -30f);
//					StartCoroutine(accelerationCoroutine);
//					isDeaccelerating = true;
//				}
			}
			yield return new WaitForEndOfFrame();
		}

		isDeaccelerating = false;
		StopCoroutine(accelerationCoroutine);
		isAccelerating = false;
		currentSpeed = 0f;
		pathNodes.Clear ();
		isMoving = false;
	}

	private IEnumerator AccelerateTo(float targetSpeed, AccelerationConstraint constraint, float value)
	{
		isAccelerating = true;
		float localAcceleration = 0f;
		switch(constraint)
		{
		case AccelerationConstraint.Time:
			localAcceleration = (targetSpeed - currentSpeed)/value;
			break;
		case AccelerationConstraint.Space:
			localAcceleration = (Mathf.Pow(targetSpeed, 2f) - Mathf.Pow (currentSpeed, 2f))/(2f * value);
			break;
		case AccelerationConstraint.SetValue:
			localAcceleration = value;
			break;
		}
	
		while(currentSpeed != targetSpeed)
		{ 
			currentSpeed = (localAcceleration >= 0f) ? Mathf.Min(currentSpeed + localAcceleration * Time.deltaTime, targetSpeed) : Mathf.Max(currentSpeed + localAcceleration * Time.deltaTime, targetSpeed);
			yield return new WaitForEndOfFrame();
		}
		isAccelerating = false;
	}

	private void ForceFinishMovement()
	{
		transform.position = pathNodes[pathNodes.Count-1];
	}

	private void FadeOut()
	{
		iTween.FadeTo (meshRenderer, iTween.Hash (
			"a", 0f,
			"time", 0.5f,
			"easetype", FADE_EASETYPE
		));
	}

	private void FadeIn()
	{
		iTween.FadeTo (meshRenderer, iTween.Hash (
			"a", 1f,
			"time", 0.5f,
			"easetype", FADE_EASETYPE
		));
	}

	private void IncreasePlaybackSpeed()
	{
		playbackSpeed *= 2f;
	}

	private void DecreasePlaybackSpeed()
	{
		playbackSpeed *= 0.5f;
	}

	void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		for(int i = 0; i < pathNodes.Count; i++)
		{
			Gizmos.DrawWireSphere(pathNodes[i], 0.2f);
		}

		if(pathNodes.Count > 2)
		{
			iTween.DrawPath (pathNodes.ToArray());
		}
	}
}