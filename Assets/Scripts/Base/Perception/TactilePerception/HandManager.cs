using System.Collections.Generic;
using UnityEngine;

namespace TactilePerception
{
    [RequireComponent(typeof(Rigidbody))]

    // This component deals with the tracked object corresponding to the hand.
    public class HandManager : HandPerceptionManagerProto
    {
        private const float DefaultPreviewWidth = 0.01f;

        [SerializeField] [Tooltip("Time between two saved position, is used for the preview updates.")]
        private float _positionDeltaTime = 0.01f;

        [Space] [Header("Preview Setup")] [SerializeField] [Tooltip("Activate the preview of the hand position")]
        private bool _usePreview;

        [SerializeField] [Tooltip("Prefab used for the position preview.")]
        private GameObject _handPreviewPrefab;

        [SerializeField] [Tooltip("Color of the preview.")]
        private Color _previewColor = Color.white;

        [SerializeField] [Tooltip("Color of the preview in etheral body.")]
        private Color _previewEtheralColor = Color.yellow;

        [SerializeField] [Tooltip("Color of the preview during collision.")]
        private Color _previewCollisionColor = Color.red;

        [SerializeField] [Tooltip("Maximum number of lines allowed.")]
        private int _maxNumberOfLines = 1000;

        private int _emissionId;
        private bool _isPreviewing;

        private Vector3 _lastPertinentPosition;
        private LineRenderer _lineRenderer;

        private GameObject _previewContainer;
        private List<GameObject> _previewObjects;

        private float _previousTime;


        #region Unity Methods

        protected override void Start()
        {
            // Initializing the variables
            base.Start();
            GetComponent<Collider>().isTrigger = true;
            _emissionId = Shader.PropertyToID("_EmissionColor");
        }

        private void LateUpdate()
        {
            var currentTime = Time.realtimeSinceStartup;
            if (currentTime - _previousTime >= _positionDeltaTime)
            {
                // Saving the current position
                positionList.Add(_previousPosition);
                // Updating the preview
                if (_usePreview) UpdatePreview();
            }

            _previousTime = currentTime;
        }

        #endregion

        #region Event Methods

        private void OnTriggerEnter(Collider other)
        {
            //_touchManager.EtherealTriggerEnter(other);
        }

        private void OnTriggerStay(Collider other)
        {
            //_touchManager.HandTriggerStay(other);
        }

        private void OnTriggerExit(Collider other)
        {
            //_touchManager.HandTriggerExit(other);
        }

        #endregion

        #region Preview Method

        public List<GameObject> StartPreviewPosition()
        {
            _isPreviewing = true;
            _previewObjects = new List<GameObject>();


            // Checking the prefab and creating the preview accordingly
            if (_handPreviewPrefab == null)
            {
                Debug.LogError("The preview prefab is null, can not create the preview");
                return null;
            }

            var instantiationTest = Instantiate(_handPreviewPrefab);
            _lineRenderer = instantiationTest.GetComponent<LineRenderer>();

            // if the prefab does not contain a line renderer, we set it to default values.
            if (_lineRenderer == null)
            {
                Debug.LogError("The preview prefab does not contain a line renderer, can not create the preview");
                return null;
            }

            Destroy(instantiationTest);
            _lineRenderer = null;
            _previewContainer = new GameObject(name + " Preview Container");
            return _previewObjects;
        }

        public void UpdatePreview()
        {
            if (!_isPreviewing) StartPreviewPosition();

            var positionNb = positionList.Count;
            var currentPosition = positionList[positionNb - 1]; // we always add a position before calling the update
            if (positionNb < 2)
            {
                _lastPertinentPosition = currentPosition;
                // we need at least two positions to use a line renderer
                return;
            }


            if ((currentPosition - _lastPertinentPosition).magnitude < 0.01f
            ) // Almost no translation was detected, there is no need to instantiate a new line
                return;


            var previewObject = Instantiate(_handPreviewPrefab, _previewContainer.transform);

            _lineRenderer = previewObject.GetComponent<LineRenderer>();
            previewObject.name = "LinePreview " + Time.realtimeSinceStartup;
            _previewObjects.Add(previewObject);
            var lineColor = _previewColor;
            if (_touchManager.IsInEtherealBody) lineColor = _previewEtheralColor;
            if (_touchManager.IsColliding) lineColor = _previewCollisionColor;
            _lineRenderer.material.SetColor(_emissionId, lineColor);
            _lineRenderer.positionCount = 2;
            _lineRenderer.endWidth = DefaultPreviewWidth;
            _lineRenderer.startWidth = DefaultPreviewWidth;
            _lineRenderer.SetPositions(new[] {currentPosition, _lastPertinentPosition});
            _lastPertinentPosition = currentPosition;

            if (_previewObjects.Count > _maxNumberOfLines)
            {
                Destroy(_previewObjects[0]);
                _previewObjects.RemoveAt(0);
            }
        }

        public void StopPreview()
        {
            if (_isPreviewing)
            {
                for (var i = 0; i < _previewObjects.Count; i++) Destroy(_previewObjects[i]);
                Destroy(_previewContainer);
                _previewObjects = null;
                _lineRenderer = null;
                _lastPertinentPosition = Vector3.zero;
                _isPreviewing = false;
            }
        }

        #endregion
    }
}