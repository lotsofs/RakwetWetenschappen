using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketElement : MonoBehaviour
{
	[NonSerialized] public new Rigidbody rigidbody;
	[NonSerialized] public new Collider collider;
	[SerializeField] SharedBool _inSpace;
	[SerializeField] bool _despawnOnMalfunction;
	public float attachmentDepth;
	

	public void Start()
	{
		rigidbody = GetComponent<Rigidbody>();
		collider = GetComponent<Collider>();
	}

	public void Update()
	{
		if (DebugKey.GetKeyDown(KeyCode.D)) {
			rigidbody.useGravity = !_inSpace.Value;
			rigidbody.isKinematic = false;
			collider.enabled = true;
			rigidbody.velocity = Vector3.one;
			Vector3 randomDirection = UnityEngine.Random.insideUnitSphere;
			rigidbody.velocity += randomDirection;
		}
	}

	/// <summary>
	/// Detach this object from the rocket
	/// </summary>
	/// <param name="debrisGroup"></param>
	/// <param name="velocity"></param>
	public void Detach(Transform debrisGroup, Vector3 velocity, float explosionPower)
	{
		if (_despawnOnMalfunction)
		{
			gameObject.SetActive(false);
		}
		else
		{
			rigidbody.useGravity = !_inSpace.Value;
			rigidbody.isKinematic = false;
			rigidbody.velocity = velocity;
			Vector3 randomDirection = UnityEngine.Random.insideUnitSphere;
			rigidbody.velocity += randomDirection * explosionPower;
			collider.enabled = true;
			transform.parent = debrisGroup;
		}
	}
}
