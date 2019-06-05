using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class Rocket : MonoBehaviour
{
	[Header("Rocket Segments")]
	[SerializeField] protected RocketSegment _top;
	[SerializeField] protected RocketSegment _middle;
	[SerializeField] protected RocketSegment _bottom;
	[SerializeField] protected RocketSegment _leftBooster;
	[SerializeField] protected RocketSegment _rightBooster;

	/// <summary>
	/// To have a visual cue in the scene for the rocket spawn point
	/// </summary>
	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(transform.position, transform.position + transform.up * (30 * transform.localScale.y));
		Gizmos.DrawLine(transform.position + transform.right * 2, transform.position + transform.up * (15 * transform.localScale.y) + transform.right * (2 * transform.localScale.x));
		Gizmos.DrawLine(transform.position + transform.right * -2, transform.position + transform.up * (15 * transform.localScale.y) + transform.right * (-2 * transform.localScale.x));
	}

	void Start()
	{
		Assemble();
	}

	/// <summary>
	/// Assembles the five segments of the rocket into one
	/// </summary>
	protected void Assemble()
	{
		_bottom.transform.localPosition = Vector3.zero;
		_middle.transform.localPosition = new Vector3(0, _bottom.height, 0);
		_top.transform.localPosition = new Vector3(0, _bottom.height + _middle.height, 0);
		_leftBooster.transform.localPosition = new Vector3(-_bottom.width - _leftBooster.width, 0, 0);
		_rightBooster.transform.localPosition = new Vector3(_bottom.width + _rightBooster.width, 0, 0);

		_bottom.transform.localScale = Vector3.one;
		_middle.transform.localScale = Vector3.one;
		_top.transform.localScale = Vector3.one;
		_leftBooster.transform.localScale = Vector3.one;
		_rightBooster.transform.localScale = Vector3.one;
	}
}
