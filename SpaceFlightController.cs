using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SpaceFlightController : MonoBehaviour
{
	[SerializeField] SharedBool _inSpace;
	[SerializeField] RocketFlight _rocket;
	[SerializeField] string _resultsScene;
	[SerializeField] float _flightTimeAfterLastPlanet;
	[SerializeField] AudioController _audioController;
	[SerializeField] SharedString _reachedPlanetName;
	[SerializeField] SharedStringGameEvent _ReachedPlanetStringGameEvent;
	[SerializeField] SharedStringGameEvent _ReachedPlanetStringHeaderGameEvent;
	[SerializeField] SharedInt _finalPlanetIndex;

	[Header("Background")]
	[SerializeField] Renderer _spaceBackdrop;
	[SerializeField] float _backdropSpeed;

	[Header("Planets")]
	[SerializeField] Planet[] _planets;

	float _travelTimer = 0f;
	int _nextPlanetIndex = 0;
	Vector2 _backdropOffset;
	Material _backdropMaterial;
	int _backdropNameID;
	bool _leavingSolarSystem = false;

	bool _cheatScreenSaverMode;

	private void Start()
	{
		_inSpace.Value = true;

		_backdropNameID = Shader.PropertyToID("_MainTex");
		_backdropOffset = _spaceBackdrop.material.GetTextureOffset(_backdropNameID);
		_backdropMaterial = _spaceBackdrop.material;

		_rocket.OnMalfunction += OnRocketMalfunction;
	}

	private void Update()
	{
		if (DebugKey.GetKeyDown(KeyCode.S))
		{
			_cheatScreenSaverMode = !_cheatScreenSaverMode;
			Debug.Log("Screensaver Mode = " + _cheatScreenSaverMode.ToString());
		}

		// do calculations for the score counting up


		// planet flyby
		_travelTimer += Time.deltaTime;
		PlanetFlyBy();

		// space background texture scrollby
		_backdropOffset.y -= Time.deltaTime * _backdropSpeed;
		_backdropMaterial.SetTextureOffset(_backdropNameID, _backdropOffset);

	}

	/// <summary>
	/// Do this when rocket explodes
	/// </summary>
	void OnRocketMalfunction()
	{
		_backdropSpeed *= 0.5f;
		// disable unvisited planets
		foreach (Planet planet in _planets)
		{
			if (!planet.flyingBy)
			{
				planet.flyBySpeed = 0;
			}
			else
			{
				planet.isBraking = true;
			}
		}
	}

	/// <summary>
	/// Send a planet across the background
	/// </summary>
	void PlanetFlyBy()
	{
		// debug: skip to next planet
		if (Input.GetKeyDown(KeyCode.Z))
		{
			_travelTimer = _planets[_nextPlanetIndex].distanceFromEarth;
			_rocket.Age = _travelTimer;
		}

		// do not do stuff if we've moved past the last planet already, or the rocket has died
		if (_leavingSolarSystem || _rocket.hasMalfunctioned)
		{
			return;
		}
		else if (_travelTimer > _planets[_nextPlanetIndex].distanceFromEarth)
		{
			// if this planet is null switch to results screen
			if (_planets[_nextPlanetIndex] == null)
			{
				StartCoroutine(SwitchToResultsScreen());
			}
			// else move the next planet past the rocket
			else
			{
				Planet currentPlanet = _planets[_nextPlanetIndex];
				_nextPlanetIndex++;

				_reachedPlanetName.Value = currentPlanet.appellation;
				currentPlanet.flyingBy = true;
				// Let the UI know that we reached a new planet and display a fact about it
				if (_nextPlanetIndex <= _finalPlanetIndex.Value)
				{
					int planetFactIndex = Random.Range(0, currentPlanet.reachedText.Length);
					_ReachedPlanetStringHeaderGameEvent.Value = currentPlanet.appellation;
					_ReachedPlanetStringGameEvent.Value = currentPlanet.reachedText[planetFactIndex];
				}
				// if there is no next planet in the array initiate scene transition
				if (_planets.Length == _nextPlanetIndex)
				{
					StartCoroutine(SwitchToResultsScreen());
				}
			}
		}
	}

	IEnumerator SwitchToResultsScreen()
	{
		_leavingSolarSystem = true;
		yield return new WaitForSeconds(_flightTimeAfterLastPlanet);
		if (_cheatScreenSaverMode)
		{
			PlanetaryReset();
		}
		else
		{
            GamePlayer.localGamePlayer.InterruptNetworkTimer();
            SceneManager.LoadScene(_resultsScene);
		}
	}

	/// <summary>
	/// Debug feature. Resets all the planets so they will flyby again, cool for looping.
	/// </summary>
	void PlanetaryReset()
	{
		_leavingSolarSystem = false;
		foreach (Planet planet in _planets)
		{
			if (planet != null)
			{
				planet.ResetPosition();
			}
		}
		_planets[0].transform.position += Vector3.up * 100 + Vector3.forward * 10; // Move earth out of the way by some arbitrary amount, specifics not important, is debug feature.
		_nextPlanetIndex = 0;
		_travelTimer = 0;
	}
}