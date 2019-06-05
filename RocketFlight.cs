using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class RocketFlight : Rocket
{
	[Header("Other Rocket Parts")]
	[SerializeField] GameObject _flames;
	[SerializeField] GameObject _explosions;
	[SerializeField] GameObject _smoke;
	[SerializeField] CameraShake _cameraShake;

	[Header("Flight Data")]
	[SerializeField] float _launchSpeed = 15f;
	[SerializeField] float _explosionPower;
	[NonSerialized] public bool hasMalfunctioned;
	[NonSerialized] public bool isLaunching;
	[SerializeField] float driftSpeed;
	[SerializeField] SharedFloat _lifespan;

	float _age;
	public float Age {
		get { return _age; }
		set { _age = value; }
	}
	bool _explodeBeforeReachingSpace;
	bool _explodeInSpace;
	bool _fallApartImmediately;

	[Header("Misc")]
	[SerializeField] string _resultsScene;
	[SerializeField] Transform _debrisGroup;
	[SerializeField] SharedInt _playerCount;
	[SerializeField] SharedBool _inSpace;
	[SerializeField] AudioController _audioController;
	[SerializeField] float _assemblyDuration;
	[SerializeField] int _assemblyToolCount;

	[SerializeField] float _sceneTransitionAfterMalfunctionTimer;
	public float SceneTransitionAfterMalfunctionTimer
	{
		get
		{
			return _sceneTransitionAfterMalfunctionTimer;
		}
		set
		{
			_sceneTransitionAfterMalfunctionTimer = value;
		}
	}

	const float explosionChargeUpTimer = 3.146f;
	const float waitForFireballTimer = 0f;
	const float engineStartupTimer = 2.78f;

	[NonSerialized] public new Rigidbody rigidbody;

	public delegate void OnExplodeDelegate();
	public event OnExplodeDelegate OnMalfunction;

	// Debug stuff TODO @Oane Remove
	bool cheatGodMode = false;
	

	/// <summary>
	/// To have a visual cue in the scene for the rocket spawn point
	/// </summary>
	private void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawLine(transform.position, transform.position + transform.up * (30 * transform.localScale.y));
		Gizmos.DrawLine(transform.position + transform.right * (2 * transform.localScale.x), transform.position + transform.up * (15 * transform.localScale.y) + transform.right * (2 * transform.localScale.x));
		Gizmos.DrawLine(transform.position + transform.right * (-2 * transform.localScale.x), transform.position + transform.up * (15 * transform.localScale.y) + transform.right * (-2 * transform.localScale.x));
	}

	void Start()
	{
		rigidbody = gameObject.GetComponent<Rigidbody>();
		rigidbody.useGravity = !_inSpace.Value;

		//_scoreCalculator = GetComponent<ScoreCalculator>();

		Assemble();
		StartCoroutine(_audioController.MultipleToolSounds(_assemblyDuration, _assemblyToolCount));
		//_scoreCalculator.PickRocketFailureEvent();

		if (_inSpace.Value == true)
		{
			// if we're in space, make flying sounds
			_audioController.RocketFlySound();
		}
	}

	void Update()
	{
		if (DebugKey.GetKeyDown(KeyCode.G))
		{
			cheatGodMode = !cheatGodMode;
			Debug.Log("God mode: " + cheatGodMode);
		}

		if (DebugKey.GetKeyDown(KeyCode.B))
		{
			//GamePlayer.localGamePlayer.StopNetworkTimer();
			//_explosionPower = 1;
			Explode();
			StartCoroutine(Explode());
			_lifespan.Value = 0f;
			_explodeBeforeReachingSpace = true;
			_explodeInSpace = true;
		}

		if (DebugKey.GetKeyDown(KeyCode.D))
		{
			Vector3 velocity = GetComponent<Rigidbody>().velocity;
			rigidbody.isKinematic = true;
			gameObject.GetComponent<Collider>().enabled = false;
		}

		if (DebugKey.GetKeyDown(KeyCode.I))
		{
			StopAllCoroutines();
			GamePlayer.localGamePlayer.InterruptNetworkTimer();
			StartCoroutine(TransitionToResults(0));
		}

		if (Input.GetKeyDown(KeyCode.K))
		{
			//_launchSpeed = 90;
			StartCoroutine(DebugForcedLaunch());
		}

		if (_inSpace.Value == true)
		{
			// if we're in space, fly
			FlyThroughSpace();
		}
	}

	/// <summary>
	/// Rocket falls apart right away, before the countdown has even completed, applying a force of nudge power to ensure the rocket topples over
	/// </summary>
	public void SetToFallApartImmediately(float nudge)
	{
		_explodeBeforeReachingSpace = false;
		_launchSpeed = 0;
		_explosionPower *= nudge;
		_fallApartImmediately = true;
		_lifespan.Value = 0;
	}
	
	/// <summary>
	/// Rocket will explode after delay seconds
	/// </summary>
	/// <param name="delay"></param>
	public void SetToExplodeDuringLaunch(float delay)
	{
		_lifespan.Value = delay;
		_explodeBeforeReachingSpace = true;
		_fallApartImmediately = false;
	}

	/// <summary>
	/// Rocket will not launch at all, and explode after delay seconds
	/// </summary>
	/// <param name="delay"></param>
	public void SetToNotLaunchAndExplode(float delay)
	{
		_lifespan.Value = delay;
		_launchSpeed = 0;
		_fallApartImmediately = false;
		_explodeBeforeReachingSpace = true;
	}

	/// <summary>
	/// Rocket will leave the atmosphere and travel in space for delay seconds, then explode
	/// </summary>
	/// <param name="delay"></param>
	public void SetToExplodeInSpace(float delay)
	{
		_lifespan.Value = delay;
		_explodeBeforeReachingSpace = false;
		_fallApartImmediately = false;
		_explodeInSpace = true;
	}

	/// <summary>
	/// Rocket will leave the atmosphere and travel in space for delay seconds, then stop flying and float in one spot
	/// </summary>
	/// <param name="delay"></param>
	public void SetToFlyToPointInSpace(float delay)
	{
		_lifespan.Value = delay;
		_explodeBeforeReachingSpace = false;
		_fallApartImmediately = false;
		_explodeInSpace = false;
	}

	/// <summary>
	/// Rocket will leave the atmosphere and travel indefinitely without exploding
	/// </summary>
	public void SetToFlyIndefinitely()
	{
		_lifespan.Value = Mathf.Infinity;
		_fallApartImmediately = false;
		_explodeBeforeReachingSpace = false;
	}

	/// <summary>
	/// Rocket flies through space
	/// </summary>
	void FlyThroughSpace()
	{
		if (_age < _lifespan.Value)
		{
			_age += Time.deltaTime;
			_flames.SetActive(_inSpace.Value);
		}
		else if (hasMalfunctioned == false)
		{
			if (_explodeInSpace)
			{
				StartCoroutine(Explode());
			}
			else
			{
				StopFlying();
			}
		}
	}

	/// <summary>
	/// Rocket stops flying, but wont explode
	/// </summary>
	void StopFlying()
	{
		// debug cheat
		if (cheatGodMode)
		{
			return;
		}

		hasMalfunctioned = true;

		// stop the flying sound
		_audioController.StopAllSounds();		// todo @Oane overkill: Make this only the relevant sounds
		// Call event so controller knows to stop planets from moving past
		if (OnMalfunction != null)
		{
			OnMalfunction();
		}
		_flames.SetActive(false);
		// give the rocket a slight spin to make it look like control has been lost
		Vector3 driftDirection = UnityEngine.Random.onUnitSphere * driftSpeed;
		rigidbody.angularVelocity += driftDirection;

		StartCoroutine(TransitionToResults(_sceneTransitionAfterMalfunctionTimer));
	}

	/// <summary>
	/// Rocket explodes
	/// </summary>
	IEnumerator Explode()
	{
		if (!cheatGodMode)
		{
			int explosionSoundIndex = _audioController.ExplosionSound();
			hasMalfunctioned = true;
			yield return new WaitForSeconds(explosionChargeUpTimer);
			if (OnMalfunction != null)
			{
				OnMalfunction();
			}
			_flames.SetActive(false);
			_explosions.SetActive(true);


			yield return new WaitForSeconds(waitForFireballTimer);
			_audioController.StopAllSoundsExcept(explosionSoundIndex);
			FallApart();
		}
		else
		{
			yield return null;
		}
	}

	/// <summary>
	/// Debug code to force the rocket to launch even when it shouldn't without any bells attached
	/// </summary>
	/// <returns></returns>
	public IEnumerator DebugForcedLaunch()
	{
		Debug.Log("Forced Launch with speed " + _launchSpeed);
		_audioController.RocketEngineStartupSound();
		yield return new WaitForSeconds(engineStartupTimer);
		_audioController.RocketLaunchSound();
		bool debugLaunching = true;
		while (debugLaunching)
		{
			rigidbody.AddRelativeForce(Vector3.up * _launchSpeed * Time.deltaTime, ForceMode.Force);
			if (_launchSpeed > 0)
			{
				_flames.SetActive(true);
				if (_smoke != null)
				{
					_smoke.SetActive(true);
				}
			}
			if (Input.GetKeyDown(KeyCode.K))
			{
				debugLaunching = false;
			}
			yield return null;
		}
	}

	/// <summary>
	/// Launch the rocket, explode it when it's expired.
	/// </summary>
	public IEnumerator Launch()
	{
		if (_fallApartImmediately)
		{
			FallApart();
			_audioController.RumblingSound();
			hasMalfunctioned = true;
		}
		else
		{
			_audioController.RocketEngineStartupSound();

			yield return new WaitForSeconds(engineStartupTimer);
			_audioController.RocketLaunchSound();
		}
		while (_explodeBeforeReachingSpace == false || _age < _lifespan.Value)
		{
			_age += Time.deltaTime;
			rigidbody.AddRelativeForce(Vector3.up * _launchSpeed * Time.deltaTime, ForceMode.Force);
			if (_launchSpeed > 0)
			{
				_flames.SetActive(true);
				if (_smoke != null)
				{
					_smoke.SetActive(true);
				}
			}
			yield return null;
			// debug cheat
			if (cheatGodMode && _age >= _lifespan.Value)
			{
				_age = _lifespan.Value - 1.0f;
			}
		}
		if (hasMalfunctioned == false)
		{
			StartCoroutine(Explode());
		}
	}

	/// <summary>
	/// Detaches the rocket into individual pieces which fall down
	/// </summary>
	/// <returns></returns>
	void FallApart()
	{
		// Disable rocket's physics
		Vector3 velocity = GetComponent<Rigidbody>().velocity;
		rigidbody.isKinematic = true;
		gameObject.GetComponent<Collider>().enabled = false;

		// Enable individual segment's physics
		_top.DetachObjects(_debrisGroup, velocity, _explosionPower);
		_middle.DetachObjects(_debrisGroup, velocity, _explosionPower);
		_bottom.DetachObjects(_debrisGroup, velocity, _explosionPower);
		_leftBooster.DetachObjects(_debrisGroup, velocity, _explosionPower);
		_rightBooster.DetachObjects(_debrisGroup, velocity, _explosionPower);

		StartCoroutine(TransitionToResults(_sceneTransitionAfterMalfunctionTimer));
		_cameraShake.StopShake();
	}

	IEnumerator TransitionToResults(float delay)
	{
		yield return new WaitForSeconds(delay);
		SceneManager.LoadScene(_resultsScene);
	}
}
