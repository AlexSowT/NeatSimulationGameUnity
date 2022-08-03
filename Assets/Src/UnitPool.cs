using System.Collections.Generic;
using System.Linq;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using UnityEngine;
using UnitySharpNEAT;

namespace Src
{
    public class UnitPool : MonoBehaviour
    {
        [Header("Unit Management")]
        [SerializeField]
        [Tooltip("The Unit Prefab, which inherits from UnitController, that should be evaluated and spawned.")]
        private UnitController _unitControllerPrefab;

        [SerializeField] [Tooltip("The parent transform which will hold the instantiated Units.")]
        private Transform _spawnParent;

        // Object pooling and Unit management
        private readonly Dictionary<IBlackBox, UnitController> _blackBoxMap =
            new Dictionary<IBlackBox, UnitController>();

        private Color[] _speciesColors;

        private readonly HashSet<UnitController> _unusedUnitsPool = new HashSet<UnitController>();

        private readonly HashSet<UnitController> _usedUnitsPool = new HashSet<UnitController>();

        public void Init(int speciesCount)
        {
            _usedUnitsPool.Clear();

            // For each specieCount create a random number and add it to the SpecieColor list
            _speciesColors = new Color[speciesCount];
            for (int i = 0; i < speciesCount; i++)
                _speciesColors[i] = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 0.2f),
                    Random.Range(0.0f, 1.0f));
        }

        #region UNIT MANAGEMENT

        public int GetActiveCount()
        {
            return _usedUnitsPool.Count();
        }
        
        /// <summary>
        ///     Get the Fitness of a Unit equipped with a IBlackBox (Neural Net).
        ///     Called after a generation has performed, to evaluate the performance of a generation and to select the best of that
        ///     generation for mating/mutation.
        /// </summary>
        public float GetFitness(IBlackBox box)
        {
            if (_blackBoxMap.ContainsKey(box)) return _blackBoxMap[box].GetFitness();
            return 0;
        }

        /// <summary>
        ///     Creates (or re-uses) a UnitController instance and assigns the Neural Net (IBlackBox) to it and activates it, so
        ///     that it starts executing the Net.
        /// </summary>
        public void ActivateUnit(IBlackBox box, int speciesIdx)
        {
            var controller = GetUnusedUnit(box);
            controller.ActivateUnit(box, speciesIdx, _speciesColors[speciesIdx]);
        }
        

        /// <summary>
        ///     Deactivates and resets a Unit. Called after a generation has performed.
        ///     Units don't get Destroyed, instead they are just reset and re-used to avoid unneccessary instantiation calls. This
        ///     process is called object pooling.
        /// </summary>
        public void DeactivateUnit(IBlackBox box)
        {
            if (_blackBoxMap.ContainsKey(box))
            {
                var controller = _blackBoxMap[box];
                controller.DeactivateUnit();

                _blackBoxMap.Remove(box);
                PoolUnit(controller, false);
            }
        }

        /// <summary>
        ///     Spawns a Unit. This means either reusing a deactivated unit from the pool or to instantiate a Unit into the pool,
        ///     in case the pool is empty.
        ///     Units don't get Destroyed, instead they are just reset to avoid unneccessary instantiation calls.
        /// </summary>
        public UnitController GetUnusedUnit(IBlackBox box)
        {
            UnitController controller;

            if (_unusedUnitsPool.Any())
            {
                controller = _unusedUnitsPool.First();
                _blackBoxMap.Add(box, controller);
            }
            else
            {
                controller = InstantiateUnit(box);
            }

            PoolUnit(controller, true);
            return controller;
        }

        /// <summary>
        ///     Instantiates a Unit in case no Unit can be drawn from the _unusedUnitPool.
        /// </summary>
        public UnitController InstantiateUnit(IBlackBox box)
        {
            UnitController controller;

            if (_spawnParent != null)
            {
                controller = Instantiate(_unitControllerPrefab, _spawnParent.transform.position,
                    _spawnParent.transform.rotation);
                /*
                controller.transform.parent = _spawnParent;
                */
                controller.SpawnLocation = _spawnParent.transform.position;
            }
            else
            {
                controller = Instantiate(_unitControllerPrefab, _unitControllerPrefab.transform.position,
                    _unitControllerPrefab.transform.rotation);
                /*
                controller.transform.parent = this.transform;
            */
            }

            controller.GetComponent<AgentController>().SetInputs(InputTypes.PositionX | InputTypes.PositionY | InputTypes.GoalPositionX | InputTypes.GoalPositionY );
            controller.GetComponent<AgentController>().SetOutputs(OutputTypes.XVelocity | OutputTypes.YVelocity | OutputTypes.Speed);

            _blackBoxMap.Add(box, controller);
            return controller;
        }

        /// <summary>
        ///     Puts Units into either the Unused or the Used object pool.
        /// </summary>
        public void PoolUnit(UnitController controller, bool markUsed)
        {
            if (markUsed)
            {
                controller.gameObject.SetActive(true);
                _unusedUnitsPool.Remove(controller);
                _usedUnitsPool.Add(controller);
            }
            else
            {
                controller.gameObject.SetActive(false);
                _unusedUnitsPool.Add(controller);
                _usedUnitsPool.Remove(controller);
            }
        }

        /// <summary>
        ///     Destroys all UnitControllers and cleans the Object Pool.
        /// </summary>
        public void DeactivateAllUnits()
        {
            var _blackBoxMapCopy = new Dictionary<IBlackBox, UnitController>(_blackBoxMap);

            foreach (var boxUnitPair in _blackBoxMapCopy) DeactivateUnit(boxUnitPair.Key);
        }

        #endregion
    }
}