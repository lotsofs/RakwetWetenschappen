using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RocketSegment : MonoBehaviour
{
	[SerializeField] SegmentSelection _segmentSelection;
    [SerializeField] SharedBool _rocketFinished;
	int _elementCount;
	[SerializeField] bool _assembleOnStart = true;
	// The amount and model of connectors that are to be attached if this is a booster
	[Header("Booster Only Properties")]
	[SerializeField] int _boosterConnectorCount;
	[SerializeField] GameObject _boosterConnectorModel;
	enum Sides { Left, Right }
	[SerializeField] Sides attachTo;

	[NonSerialized] public float height;
	[NonSerialized] public float width;

	GameObject _topElement;
	GameObject _centerElement;
	GameObject _lowerElement;

	RocketElement[] _rocketElements;

	void Awake()
	{
        // If the rocket is unfinished, don't put any connectors so the rocket can fall apart more properly.
        if (_rocketFinished.Value == false )
        {
            _boosterConnectorCount = 0;
        }

        _elementCount = _segmentSelection.Count - 1; // -1 because color shouldn't be counted todo @oane put a proper check here
		// Array of rocket elements
		_rocketElements = new RocketElement[_elementCount + _boosterConnectorCount];
	}

	private void Start()
	{
		if (_assembleOnStart)
			Assemble();
	}

	/// <summary>
	/// Assemble this rocket part out of all the elements
	/// </summary>
	void Assemble()
	{
		// Spawn the 3 elements
		_topElement = Instantiate(_segmentSelection.ElementAt(0).Model);
		_centerElement = Instantiate(_segmentSelection.ElementAt(1).Model);
		_lowerElement = Instantiate(_segmentSelection.ElementAt(2).Model);

		// Get the RocketElements
		_rocketElements[0] = _topElement.GetComponent<RocketElement>();
		_rocketElements[1] = _centerElement.GetComponent<RocketElement>();
		_rocketElements[2] = _lowerElement.GetComponent<RocketElement>();

		// Get the boundaries
		Bounds topBounds = _topElement.GetComponent<Renderer>().bounds;
		Bounds centerBounds = _centerElement.GetComponent<Renderer>().bounds;
		Bounds lowerBounds = _lowerElement.GetComponent<Renderer>().bounds;

		// Calculate bottom element position
		Vector3 lowerPosition = Vector3.zero;
		lowerPosition.y -= _rocketElements[2].attachmentDepth;

		// Calculate middle element position
		Vector3 centerPosition = lowerPosition;
		centerPosition.y += lowerBounds.size.y;
		centerPosition.y -= _rocketElements[1].attachmentDepth;

		// Calculate top element position
		Vector3 topPosition = centerPosition;
		topPosition.y += centerBounds.size.y;
		topPosition.y -= _rocketElements[0].attachmentDepth;

		// Setting parent here and not when instantiating because rotations are hard
		_topElement.transform.parent = transform;
		_centerElement.transform.parent = transform;
		_lowerElement.transform.parent = transform;

		// Set Scale
		_topElement.transform.localScale = Vector3.one;
		_centerElement.transform.localScale = Vector3.one;
		_lowerElement.transform.localScale = Vector3.one;

		// Set position
		_topElement.transform.localPosition = topPosition;
		_centerElement.transform.localPosition = centerPosition;
		_lowerElement.transform.localPosition = lowerPosition;

		// Setting rotation
		_topElement.transform.localRotation = _topElement.transform.rotation;
		_centerElement.transform.localRotation = _centerElement.transform.rotation;
		_lowerElement.transform.localRotation = _lowerElement.transform.rotation;

		// Calculate height and width for use in full rocket assembly
		height = lowerBounds.size.y + centerBounds.size.y + topBounds.size.y;
		height -= _rocketElements[0].attachmentDepth;
		height -= _rocketElements[1].attachmentDepth;
		height -= _rocketElements[2].attachmentDepth;
		width = Mathf.Max(lowerBounds.extents.x, topBounds.extents.x, centerBounds.extents.x);

		// Attach the connectors if it has them
		if (_boosterConnectorCount > 0)
		{
			// Get boundaries
			Bounds connectorBounds = _boosterConnectorModel.GetComponent<Renderer>().bounds;
			float protrusionDistance = connectorBounds.extents.x;
			width -= protrusionDistance;

			// Calculate the difference in height between each connector (attached evenly across the bottom half of the booster)
			float distanceBetweenConnectors = height / (_boosterConnectorCount * 2);
			float connectionHeight = 0.0f;

			// Spawn and attach connectors
			for (int i = 0; i < _boosterConnectorCount; i++)
			{
				// Calculate where the next connector is to be attached
				connectionHeight = distanceBetweenConnectors * (i + 1);     // +1 because we dont want to attach at 0 height
				Vector3 connectionLocation = Vector3.zero;
				connectionLocation.x = width - protrusionDistance;
				connectionLocation.y = connectionHeight;
				// Flip the attachment location to the other side of the booster if needed
				if (attachTo == Sides.Right)
				{
					connectionLocation.x *= -1;
				}
				// Spawn and position the connector
				GameObject connectionPiece = Instantiate(_boosterConnectorModel, transform);
				connectionPiece.transform.localPosition = connectionLocation;
				// Flip piece around if needed
				if (attachTo == Sides.Right)
				{
					Vector3 mirrorScale = connectionPiece.transform.localScale;
					mirrorScale.x *= -1;
					connectionPiece.transform.localScale = mirrorScale;
				}
				// Add connector to array of elements
				_rocketElements[i + _elementCount] = connectionPiece.GetComponent<RocketElement>();
			}
		}

		// Get materials of all objects
		Material _topColor = _topElement.GetComponent<Renderer>().material;
		Material _centerColor = _centerElement.GetComponent<Renderer>().material;
		Material _lowerColor = _lowerElement.GetComponent<Renderer>().material;

		// Set their overlay color
		int overlayColorID = Shader.PropertyToID("_OverlayColor");
		Color color = _segmentSelection.ElementAt(3).Color;
		_topColor.SetColor(overlayColorID, color);
		_centerColor.SetColor(overlayColorID, color);
		_lowerColor.SetColor(overlayColorID, color);
	}

	/// <summary>
	/// Disassemble rocket and assemble it again (in case of new parts selected).
	/// </summary>
	public void Rebuild()
	{
		Destroy(_topElement);
		Destroy(_centerElement);
		Destroy(_lowerElement);
		Assemble();
	}

	/// <summary>
	/// Detach all the elements from this part
	/// </summary>
	/// <param name="debrisGroup"></param>
	/// <param name="velocity"></param>
	public void DetachObjects(Transform debrisGroup, Vector3 velocity, float explosionPower)
	{
		foreach (RocketElement rocketElement in _rocketElements)
		{
			rocketElement.Detach(debrisGroup, velocity, explosionPower);
		}
	}
}