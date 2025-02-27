using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Minigame
{
    [Serializable] public struct ShapeTetroPos
    {
        public Shape shape;
        [SerializeField]
        public List<TetroPos> tetroPosList;
    }
    [Serializable] public struct TetroPos
    {
        public Tetromino tetro;
        public Position position;
    }
    public class GridTrigger : MonoBehaviour
    {
        // A dictionary easily modifiable in Editor to set where the trigger is compared to each possible Tetromino for each Shape.
        [FormerlySerializedAs("_positionForShapeStruct")] [SerializeField]
        public ShapeTetroPos[] positionForShapeStruct;

        private Dictionary<Shape, Dictionary<Tetromino, Position>> _positionForShape = new Dictionary<Shape, Dictionary<Tetromino, Position>>();

        private GridManager _gridManager;
        private Shape _currentShape;
        
        // Start is called before the first frame update
        void Start()
        {
            _gridManager = GetComponentInParent<GridManager>();
            
            foreach (var shape in positionForShapeStruct)
            {
                var tempDict = shape.tetroPosList.ToDictionary(tetroPos => tetroPos.tetro, tetroPos => tetroPos.position);
                _positionForShape.Add(shape.shape, tempDict);
            }
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        void OnTriggerEnter(Collider other)
        {
            if (_currentShape == Shape.Default) return;
            if (other.gameObject.GetComponentInParent<TetroID>() == null) return;
            var tetroCollided = other.gameObject.GetComponentInParent<TetroID>().tetrotype;
            _gridManager.TetroEntered(tetroCollided, _positionForShape[_currentShape][tetroCollided], gameObject);
        }

        void OnTriggerExit(Collider other)
        {
            if (_currentShape == Shape.Default) return;
            if (other.gameObject.GetComponentInParent<TetroID>() == null) return;
            var tetroCollided = other.gameObject.GetComponentInParent<TetroID>().tetrotype;
            _gridManager.TetroExited(tetroCollided, _positionForShape[_currentShape][tetroCollided], gameObject);
        }

        public void SetCurrentShape(Shape newShape)
        {
            _currentShape = newShape;
            if (_positionForShape[_currentShape].ContainsValue(Position.Correct))
                _gridManager.AddCorrectTrigger(gameObject);
        }
    }
}

