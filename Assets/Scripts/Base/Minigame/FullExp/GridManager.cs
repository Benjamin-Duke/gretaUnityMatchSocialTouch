using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Leap.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Minigame
{
    public enum Shape
    {
        Default, Shape1, Shape2
    }

    public enum Position
    {
        Left, Right, Up, Down, Incorrect, Correct
    }
    
    public enum Tetromino
    {
        Angle, LongAngle, Z, LongZ, W, Square, Bar, Cross, Tri
    }

    public class GridManager : MonoBehaviour
    {
        [SerializeField] private Dictionary<Tetromino, List<Position>> _tetroPositions = new Dictionary<Tetromino, List<Position>>();

        private Dictionary<GameObject, bool> _correctTriggers = new Dictionary<GameObject, bool>();

        private Shape _currentShape;
        public InputAction shape1Action;
        public InputAction shape2Action;
        public InputAction clear;

        public bool debug;
        // Start is called before the first frame update
        void Start()
        {
            shape1Action.performed += ctx => SetShape(Shape.Shape1);
            shape2Action.performed += ctx => SetShape(Shape.Shape2);
            clear.performed += ctx => FullClear();
            
            shape1Action.Enable();
            shape2Action.Enable();
            clear.Enable();
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        public void TetroEntered(Tetromino other, Position position, GameObject trigger)
        {
            if (!_tetroPositions.ContainsKey(other))
            {
                _tetroPositions.Add(other, new List<Position>());
            }
            _tetroPositions[other].Add(position);
            if (debug) Debug.Log("Tetro " + other.ToString() + " was recorded in the grid with a relative position of "+ position + " inside the trigger "+ trigger.name +".");
            if (position != Position.Correct) return;
            if (_correctTriggers.ContainsKey(trigger))
                _correctTriggers[trigger] = true;
        }
        
        public void TetroExited(Tetromino other, Position position, GameObject trigger)
        {
            if (_tetroPositions.ContainsKey(other))
                _tetroPositions[other].Remove(position);
            if (debug) Debug.Log("Tetro " + other.ToString() + " is no longer inside the Trigger "+ trigger.name + ".");
            if (position != Position.Correct) return;
            if (_correctTriggers.ContainsKey(trigger))
                _correctTriggers[trigger] = false;
        }

        public bool IsShapeCorrect()
        {
            return _correctTriggers.Values.All(v => v.Equals(true));
        }

        public KeyValuePair<Tetromino, KeyValuePair<Position, float>> WorstPlacedTetro()
        {
            var worstTetro = new KeyValuePair<Tetromino, KeyValuePair<Position, float>>(_tetroPositions.First().Key,RatioBadPlacement(_tetroPositions.First().Value));
            foreach (var tetro in _tetroPositions)
            {
                var ratio = RatioBadPlacement(tetro.Value);
                if (ratio.Value < worstTetro.Value.Value)
                {
                    worstTetro = new KeyValuePair<Tetromino, KeyValuePair<Position, float>>(tetro.Key, ratio);
                }
            }
            if (debug) Debug.Log("The currently worst placed tetromino is  "+ worstTetro.Key + " and it is mostly placed "+ worstTetro.Value.Key +" of the place it should be.");
            return worstTetro;
        }

        public KeyValuePair<Position, float> RatioBadPlacement(List<Position> positions)
        {
            var query = positions.Where(p => p != Position.Correct).GroupBy(r => r).Select(grp => new {
                    Value = grp.Key,
                    Count = grp.Count()
                });
            var mostPositionOrDefault = query.First();
            foreach (var position in query)
            {
                if (position.Count > mostPositionOrDefault.Count)
                    mostPositionOrDefault = position;
            }

            return new KeyValuePair<Position, float>(mostPositionOrDefault.Value,
                mostPositionOrDefault.Count / positions.Count);
        }

        public void SetShape(Shape shapetoSet)
        {
            foreach (var child in transform.GetSelfAndAllChildren())
            {
                child.GetComponent<GridTrigger>()?.SetCurrentShape(shapetoSet);
            }

            _currentShape = shapetoSet;
            shape1Action.Disable();
            shape2Action.Disable();
        }

        public Shape GetCurrentShape()
        {
            return _currentShape;
        }

        public void AddCorrectTrigger(GameObject trigger)
        {
            if (_correctTriggers.ContainsKey(trigger)) return;
            _correctTriggers[trigger] = false;
        }

        public void FullClear()
        {
            if (debug) Debug.Log("Fully clearing the current grid state for the minigame.");
            _correctTriggers.Clear();
            _tetroPositions.Clear();
            _currentShape = Shape.Default;
            shape1Action.Enable();
            shape2Action.Enable();
        }

        public int EmptyOrCurrentlyCorrect()
        {
            if (_tetroPositions.Count <= 0) return 0;
            foreach (var tetro in _tetroPositions)
            {
                if (!tetro.Value.All(v => v.Equals(Position.Correct)))
                    return -1;

            }
            return 1;
        }
        
    }
}

