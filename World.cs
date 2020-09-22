using System;
using System.Collections.Generic;
using System.Linq;
using TestingTaskFramework;
using VRage.Collections;
using VRageMath;
using DataStructures.ViliWonka.KDTree;

namespace TestingTask
{
    // TODO: World is really slow now, optimize it.
    // TODO: Fix excessive allocations during run.
    // TODO: Write body of 'PreciseCollision' method.
    class World : IWorld
    {
        const int UPDATE_MAX_DISTANCE = 40;
        const int CAMERA_HALF_WIDTH = 80;
        const int CAMERA_HALF_HEIGHT = 60;

        KDTree m_asteroidsTree = null;
        KDQuery m_treeQuery = new KDQuery();

        List<WorldObject> m_objects = new List<WorldObject>();
        List<WorldObject> m_bullets = new List<WorldObject>();
        List<WorldObject> m_asteroids = new List<WorldObject>();
        CachingList<WorldObject> m_activeObjects = new CachingList<WorldObject>();

        bool m_isUpdateEnabled = false;
        bool m_isUpdateStarted = false;
        WorldObject m_ship = null;
        Vector3 m_lastShipPosition = Vector3.Zero;
        Vector3 m_lastShipVelocity = Vector3.Zero;

        /// <summary>
        /// Time of the world, increased with each update.
        /// </summary>
        public TimeSpan Time { get; private set; }

        /// <summary>
        /// Adds new object into world.
        /// World is responsible for calling OnAdded method on object when object is added.
        /// </summary>
        public void Add(WorldObject obj)
        {
            if (IsShipObject(obj))
            {
                m_ship = obj;
                obj.OnAdded(this);
            }
            else if (IsBulletObject(obj))
            {
                obj.OnAdded(this);
                m_bullets.Add(obj);
            }

            if (IsAsteroidObject(obj))
                m_asteroids.Add(obj);

            m_objects.Add(obj);
        }

        /// <summary>
        /// Removes object from world.
        /// World is responsible for calling OnRemoved method on object when object is removed.
        /// </summary>
        public void Remove(WorldObject obj)
        {
            m_objects.Remove(obj);
            obj.OnRemoved();

            if (m_activeObjects.Contains(obj))
                m_activeObjects.Remove(obj, !m_isUpdateStarted);

            if (IsAsteroidObject(obj) && m_asteroids.IndexOf(obj) >= 0)
            {
                int index = m_asteroids.IndexOf(obj);
                m_asteroids[index] = null;
                m_asteroidsTree.Points[index] = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                m_asteroidsTree.Rebuild();
            }
        }

        /// <summary>
        /// Called when object is moved in the world.
        /// </summary>
        public void OnObjectMoved(WorldObject obj, Vector3 displacement)
        {
        }

        /// <summary>
        /// Clears whole world and resets the time.
        /// </summary>
        public void Clear()
        {
            Time = TimeSpan.Zero;
            m_objects.Clear();
        }

        /// <summary>
        /// Queries the world for objects in a box. Matching objects are added into result list.
        /// Query should return all overlapping objects.
        /// </summary>
        public void Query(BoundingBox box, List<WorldObject> resultList)
        {
            if (m_asteroidsTree == null)
            {
                Vector3[] positions = new Vector3[m_asteroids.Count];
                for (int i = 0; i < m_asteroids.Count; i++)
                {
                    Vector3 position = m_asteroids.ElementAt(i).Position;
                    positions[i] = new Vector3(position.X, 0, position.Z);
                }
                m_asteroidsTree = new KDTree(positions);
            }

            if (box.Size.X > CAMERA_HALF_WIDTH * 2)
            {
                IEnumerable<WorldObject> objects = m_objects.Where(obj => obj.BoundingBox.Contains(box) != ContainmentType.Disjoint);
                resultList.AddRange(objects);
            }
            else
            {
                IEnumerable<WorldObject> objects = m_activeObjects.Where(obj => obj.BoundingBox.Contains(box) != ContainmentType.Disjoint);
                resultList.AddRange(objects);
            }
        }

        /// <summary>
        /// Updates the world in following order:
        /// 1. Increase time.
        /// 2. Call Update on all objects with NeedsUpdate flag.
        /// 3. Call PostUpdate on all objects with NeedsUpdate flag.
        /// PostUpdate on first object must be called when all other objects are Updated.
        /// </summary>

        public void Update(TimeSpan deltaTime)
        {
            Time += deltaTime;

            if (!m_isUpdateStarted)
                CheckActiveObjects();

            if (m_isUpdateEnabled)
                UpdateActiveObjects(deltaTime);
        }

        private BoundingBox GetCameraBox()
        {
            float velocityShiftX = (Vector3.IsZero(m_ship.LinearVelocity)) ? 0 : (m_ship.LinearVelocity.X / 100) * CAMERA_HALF_HEIGHT;
            float velocityShiftZ = (Vector3.IsZero(m_ship.LinearVelocity)) ? 0 : (m_ship.LinearVelocity.Z / 100) * CAMERA_HALF_HEIGHT;
            Vector3 min = new Vector3(m_ship.Position.X - CAMERA_HALF_WIDTH + velocityShiftX, 0, m_ship.Position.Z - CAMERA_HALF_HEIGHT + velocityShiftZ);
            Vector3 max = new Vector3(m_ship.Position.X + CAMERA_HALF_WIDTH + velocityShiftX, 0, m_ship.Position.Z + CAMERA_HALF_HEIGHT + velocityShiftZ);
            return new BoundingBox(min, max);
        }

        private void CheckActiveObjects()
        {

            if (m_activeObjects.Count == 0 || m_ship.LinearVelocity != m_lastShipVelocity || Vector3.Distance(m_ship.Position, m_lastShipPosition) > UPDATE_MAX_DISTANCE)
            {
                m_isUpdateEnabled = false;
                m_lastShipPosition = m_ship.Position;
                m_lastShipVelocity = m_ship.LinearVelocity;

                UpdateActiveObjectsList();

                m_isUpdateEnabled = (m_activeObjects.Count > 0);
            }
        }

        private void UpdateActiveObjectsList()
        {
            BoundingBox box = GetCameraBox();
            List<int> indexes = new List<int>();
            m_treeQuery.Interval(m_asteroidsTree, box.Min, box.Max, indexes);

            List<WorldObject> results = new List<WorldObject>();
            foreach (int index in indexes)
            {
                if (index < m_asteroids.Count)
                    results.Add(m_asteroids.ElementAt(index));
            }
            foreach (var obj in results)
            {
                if (obj.World == null)
                    obj.OnAdded(this);

                if (!m_activeObjects.Contains(obj))
                    m_activeObjects.Add(obj);
            }
            foreach (var obj in m_activeObjects)
            {
                if (obj.World == null || 
                    IsAsteroidObject(obj) && !Vector3.IsZero(obj.LinearVelocity) && Vector3.Distance(m_ship.Position, obj.Position) > CAMERA_HALF_WIDTH || 
                    IsAsteroidObject(obj) && Vector3.IsZero(obj.LinearVelocity) && !results.Contains(obj))
                {
                    m_activeObjects.Remove(obj);
                }
            }
            
            if (!m_activeObjects.Contains(m_ship))
                m_activeObjects.Add(m_ship);

            m_activeObjects.ApplyChanges();

            m_isUpdateEnabled = m_activeObjects.Count > 0;
        }

        private void UpdateActiveObjects(TimeSpan deltaTime)
        {
            m_isUpdateStarted = true;
            if (m_bullets.Count > 0)
            {
                foreach (var obj in m_bullets)
                {
                    if (obj.World != null)
                        m_activeObjects.Add(obj);
                }
                m_bullets.Clear();
            }

            m_activeObjects.ApplyChanges();
            foreach (var obj in m_activeObjects.Where(obj => obj.NeedsUpdate))
            {
                obj.Update(deltaTime);
            }
            foreach (var obj in m_activeObjects.Where(obj => obj.World != null && obj.NeedsUpdate))
            {
                obj.PostUpdate();
            }

            m_isUpdateStarted = false;
        }

        private bool IsShipObject(WorldObject obj)
        {
            return obj.GetType() == typeof(TestingTaskFramework.Ship);
        }

        private bool IsAsteroidObject(WorldObject obj)
        {
            return obj.GetType() == typeof(TestingTaskFramework.Asteroid);
        }

        private bool IsBulletObject(WorldObject obj)
        {
            return obj.GetType() == typeof(TestingTaskFramework.Projectile);
        }

        /// <summary>
        /// Calculates precise collision of two moving objects.
        /// Returns exact delta time of touch (e.g. 1 is one second in future from now).
        /// When objects are already touching or overlapping, returns zero. When the objects won't ever touch, returns positive infinity.
        /// </summary>
        public float PreciseCollision(WorldObject a, WorldObject b)
        {
            float result = float.PositiveInfinity;
            if (Vector3.Distance(a.Position, b.Position) <= (a.BoundingRadius + b.BoundingRadius))
            {
                return 0;
            }
            else if (Vector3.IsZero(a.LinearVelocity) && Vector3.IsZero(b.LinearVelocity))
            {
                return result;
            }
            else if (a.LinearVelocity == b.LinearVelocity)
            {
               return result;
            }
            else
            {
                float collisionTime = PositionHelper.GetCollisionTime(a.Position, b.Position, a.LinearVelocity, b.LinearVelocity, a.BoundingRadius, b.BoundingRadius);

                if (collisionTime.IsValid() && collisionTime > 0)
                {
                    result = collisionTime;
                }
            }

            return result;
        }
    }
}