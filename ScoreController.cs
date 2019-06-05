using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using RocketVariables;
using System;

public class ScoreController : MonoBehaviour	// todo @Oane make this server side if there's time left, execute from networklist.cs or something. Also put this not on the rocket?
{
	[Header("Timer")]
	[SerializeField] SharedFloat _timeRemainder;
	[SerializeField] SharedInt _gameDuration;

	[Header("Missions")]
	[SerializeField] SharedInt _playerCount;
	[SerializeField] SegmentSelection[] _segmentSelections;
	[SerializeField] MissionContainer[] _missionContainers;
	[SerializeField] SharedInt _minimumPlayerCount;
	[SerializeField] SharedBool _rocketFinished;

	[Header("Score")]
	[SerializeField] float _missionScoreWeightSuccess;
	[SerializeField] float _timerScoreWeightSuccess;
	[SerializeField] float _missionScoreWeightFailure;
	[SerializeField] float _timerScoreWeightFailure;

	[SerializeField] AnimationCurve _timerScoreCurve;
    [SerializeField] float _fixedMultiplier;
	[SerializeField] SharedFloat _score;

	[Header("Result Effects")]
	[SerializeField] RocketEvents _rocket;

	[Header("References"), SerializeField] MissionController _missionController;

	[SerializeField] bool _calculateSuccess = true;

	private void Start()
	{
		// Calculate Score
		float maxScore = (_timerScoreWeightSuccess + _missionScoreWeightSuccess) * _fixedMultiplier;
		_score.Value = Mathf.Round(Score());

		float failureThreshold = _missionController.FailureThreshold;

		// pick effect based on both timer & missions completed
		float scoreRate = _score.Value / maxScore;

		// formula to convert the range (_successThreshold to 1) to a range of (0 to 1)
		float failureEventIndex = (scoreRate - failureThreshold) * (1 / (1 - failureThreshold));

		_rocket.PickRocketFailureEvent(failureEventIndex);
	}
	

	/// <summary>
	/// Score 
	/// </summary>
	/// <returns></returns>
	public float Score()
	{
		GamePlayer gamePlayer = GamePlayer.localGamePlayer;

		int roundIndex = gamePlayer ? gamePlayer.RoundIndex : 0;

		// initialized the missioncontroller
		_missionController.Initialize(roundIndex, _calculateSuccess, 0); // todo: if we want to make the teamcaptain dynamic at some point, change this.

		float score = _missionController.SuccessRate;

		float playerMissionScore = _missionController.PlayerSuccessRate;	// Todo @Johan these return the amount of missions completed etc. Might be worth looking into these if stuff is bugged. I dont know how these work exactly, I didnt write them.
		float captainMissionScore = _missionController.CaptainSuccessRate;


		float timerScore = _timeRemainder.Value / _gameDuration.Value;
		timerScore = _timerScoreCurve.Evaluate(timerScore);


		float failureThreshold = _missionController.FailureThreshold;

		if (_rocketFinished.Value == false || playerMissionScore < failureThreshold || captainMissionScore < failureThreshold)
		{
			// total failure.						// Todo @Johan: If score is 0, this happens. Work from here?
			score *= _missionScoreWeightFailure;
			timerScore *= _timerScoreWeightFailure;
			score += timerScore;
		}
		else
		{
			// succeeded. 
			score *= _missionScoreWeightSuccess;
			timerScore *= _timerScoreWeightSuccess;
			score += timerScore;
			score = Mathf.Round(score);     // round here so our score will be multiples of _fixedMultiplier
		}

		score *= _fixedMultiplier;

		score = Mathf.Round(score); 

		Debug.Log(string.Format("Calculated Score \nTotal Score: {0}, timerScore: {1}", score, timerScore));

		return score;
	}
}
