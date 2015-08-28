using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour {
	public enum Mode { FollowClick, AddToList };
	public Mode MoveMode = Mode.FollowClick;

	public enum Constraint { Time, Speed };
	public Constraint movementConstraint;

	public float movementSpeed;
	public float movementTime;

	public float currentSpeed;
	public float maxSpeed;

	public float minDistanceBetweenPoints;
	public List<Vector3> pathNodes;

	public Collider fieldBounds;
	private bool inField = true;
	private iTween.EaseType fadeEaseType = iTween.EaseType.easeOutCubic;

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
		currentSpeed = (movementConstraint == Constraint.Speed) ? movementSpeed : pathLength / movementTime;
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
			"easetype", fadeEaseType
		));
	}

	private void FadeIn()
	{
		iTween.FadeTo (meshRenderer, iTween.Hash (
			"a", 1f,
			"time", 0.5f,
			"easetype", fadeEaseType
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