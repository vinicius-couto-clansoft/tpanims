using UnityEngine;
using System.Collections;

public class DrawCollider : MonoBehaviour {
	
	private Collider col;

	void Awake()
	{
		col = GetComponent<Collider> ();
	}

	void OnDrawGizmos()
	{
		Gizmos.color = Color.white;

		if (col != null)
		{
			Vector3 cubeSize = col.bounds.size;
			cubeSize.y = 0f;
			Gizmos.DrawWireCube (transform.position, cubeSize);
		}
	}
}
