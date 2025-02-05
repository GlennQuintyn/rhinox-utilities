﻿using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Rhinox.Utilities
{
    /// <summary>
    /// Keeps track of a certain Component that is on objects that collide with this object.
    /// Meant for this object to have a trigger, however it will also track triggers entering this if this object only has colliders.
    /// NOTE: Objects will be tracked regardless of their 'active in hierarchy' state
    /// If you wish to manage that you will have to Use ActiveContainedObjects
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [RefactoringOldNamespace("")]
    public class TriggerTracker<T> : MonoBehaviour where T : Component
    {
        public bool TrackChildren = false;
        
        /// <summary>
        /// Dictionary that describes what objects with what colliders are inside the trigger
        /// HashSet so the same collider does not enter multiple times, which will cause issues
        /// </summary>
        protected Dictionary<T, HashSet<Collider>> _containedObjectsDict = new Dictionary<T, HashSet<Collider>>();

        [ShowInInspector, ReadOnly] public ICollection<T> ContainedObjects => _containedObjectsDict.Keys;
        public IEnumerable<T> ActiveContainedObjects => _containedObjectsDict.Keys.Where(x => x != null && x.gameObject.activeInHierarchy);

        public event Action<T> ObjectEnter;
        public event Action<T> ObjectExit;
        
        protected virtual void OnEnable()
        {

        }

        protected virtual void OnDisable()
        {
            _containedObjectsDict.Clear();
        }

        protected virtual void OnObjectEnter(T obj)
        {
            ObjectEnter?.Invoke(obj);
        }

        protected virtual void OnObjectExit(T obj)
        {
            ObjectExit?.Invoke(obj);
        }

        private void OnTriggerEnter(Collider other)
        {
            RegisterCollider(other);
        }

        private void OnTriggerExit(Collider other)
        {
            UnregisterCollider(other);
        }

        private void Update()
        {
            _containedObjectsDict.RemoveAll(x => x.Key == null);
        }

        protected virtual T GetContainer(Collider coll)
        {
            Transform container = coll.transform;
            if (coll.attachedRigidbody != null)
                container = coll.attachedRigidbody.transform;

            var actualContainer = container.GetComponentInChildren<T>(true);

            // Ensure the container is a child of the 'actualContainer'
            // Due to using the attachedRigidbody we might have traveled too far up the hierarchy
            // in which case we do not want to link those colliders to the container as they may change (ChildOfControllerGrabAttach, i.e.)
            if (actualContainer == null || (TrackChildren || !container.IsChildOf(actualContainer.transform)))
                return null;

            return actualContainer;
        }

        protected virtual bool ValidateContainer(T container)
        {
            return true;
        }

        private void RegisterCollider(Collider coll)
        {
            var container = GetContainer(coll);

            if (container == null || !ValidateContainer(container))
                return;

            if (_containedObjectsDict.ContainsKey(container))
                _containedObjectsDict[container].Add(coll);
            else
            {
                _containedObjectsDict.Add(container, new HashSet<Collider> { coll });
                OnObjectEnter(container);
            }
        }

        private void UnregisterCollider(Collider coll)
        {
            var container = GetContainer(coll);

            if (container == null) return;

            if (!_containedObjectsDict.ContainsKey(container))
                return;

            _containedObjectsDict[container].Remove(coll);

            if (_containedObjectsDict[container].Any())
                return;

            _containedObjectsDict.Remove(container);
            OnObjectExit(container);
        }
    }
}