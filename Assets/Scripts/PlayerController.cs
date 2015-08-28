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
	[Range(0f, MAX_SPEED)]public float targetSpeed;	// How fast should the player run.
	public float targetTime;	// How long should it take to run the whole path. Speed is capped at MAX_SPEED

	[HideInInspector] public List<Vector3> pathNodes;

	[Header("Objects references")]
	public Collider fieldBounds;
	private bool inField = true;

	private Animator animator;
	public GameObject meshRenderer;

	private void Awake()
	{
		animator = GetComponent<Animator> ();
	}
	
	private void Update()
	{
		if(Input.GetMouseButton(0) && currentSpeed == 0f)
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
			if(currentSpeed == 0f)
			{
				StartCoroutine(MoveThroughPath(pathNodes));
			}
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

	private IEnumerator MoveThroughPath(List<Vector3> path)
	{
		Vector3[] waypoints = path.ToArray ();
		float pathLength = iTween.PathLength(waypoints);
		float currentPercent = 0f;
		float absOnePercent = iTween.PathLength (waypoints) * 0.01f;
		currentSpeed = (constraint == Constraint.Speed) ? targetSpeed : pathLength / targetTime; // Only changes speed if constraint is time
		currentSpeed = Mathf.Min (currentSpeed, MAX_SPEED);	// Limits speed to maxSpeed
		animator.SetFloat ("speed", currentSpeed);

		while(currentPercent < 1f)
		{
			float relativeOnePercent = Vector3.Distance(iTween.PointOnPath(waypoints, currentPercent), iTween.PointOnPath(waypoints, currentPercent + 0.01f));
			float distortion = absOnePercent/relativeOnePercent;
			currentPercent += currentSpeed * distortion * Time.deltaTime/pathLength;

			iTween.PutOnPath(gameObject, waypoints, currentPercent);
			transform.LookAt(iTween.PointOnPath(waypoints, currentPercent + 0.01f));

			yield return new WaitForEndOfFrame();
		}

		pathNodes.Clear ();

		currentSpeed = 0f;
		animator.SetFloat ("speed", currentSpeed);
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