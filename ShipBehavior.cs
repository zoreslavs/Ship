using System.Collections.Generic;
using TestingTaskFramework;
using VRageMath;

namespace TestingTask
{
    // TODO: Modify 'OnUpdate' method, find asteroids in World (property Ship.World) and shoot them.
    class ShipBehavior : IShipBehavior
    {
        const int QUERY_UPDATES_LIMIT = 5;
        const float SHOOT_DISTANCE_COEF = 1.1f;

        List<WorldObject> m_targetObjects = new List<WorldObject>();
        List<WorldObject> m_activeObjects = new List<WorldObject>();
        List<int> m_shootedObjectIds = new List<int>();

        float m_shootDistance = 0;
        int m_updatesCount = 0;

        /// <summary>
        /// The ship which has this behavior.
        /// </summary>
        public Ship Ship { get; set; }

        /// <summary>
        /// Called when ship is being updated, Ship property is never null when OnUpdate is called.
        /// </summary>
        public void OnUpdate()
        {
            m_updatesCount++;

            if (m_updatesCount < QUERY_UPDATES_LIMIT)
                return;
            m_updatesCount = 0;

            if (!Ship.CanShoot)
                return;

            if (m_shootDistance == 0)
                m_shootDistance = (float)(Ship.GunInfo.ProjectileSpeed * Ship.GunInfo.ProjectileLifetime.TotalMilliseconds / 1000) * SHOOT_DISTANCE_COEF;

            m_targetObjects.Clear();
            BoundingBox box = new BoundingBox(Ship.Position + Ship.LinearVelocity - m_shootDistance, Ship.Position + Ship.LinearVelocity + m_shootDistance);

            Ship.World.Query(box, m_activeObjects);

            foreach (var obj in m_activeObjects)
            {
                if (obj.GetType() != typeof(TestingTaskFramework.Asteroid) || m_shootedObjectIds.Contains(obj.GetHashCode()))
                    continue;

                float distanceToTarget = Vector3.Distance(Ship.Position, obj.Position);
                if (m_shootDistance >= distanceToTarget)
                    m_targetObjects.Add(obj);
            }

            if (m_targetObjects.Count > 0)
            {
                Vector3 direction = GetNextTargetDirection();

                if (!Vector3.IsZero(direction))
                    ShootTarget(direction);
            }

            if (m_shootedObjectIds.Count > 10)
                m_shootedObjectIds.Clear();
        }

        private Vector3 GetNextTargetDirection()
        {
            Vector3 direction = GetBestMovingTargetDirection();
            if (Vector3.IsZero(direction))
                direction = GetBestStaticTargetDirection();

            return direction;
        }

        private Vector3 GetBestMovingTargetDirection()
        {
            WorldObject target = null;
            Vector3 direction = Vector3.Zero;
            float minDistance = m_shootDistance;

            foreach (var obj in m_targetObjects)
            {
                bool isObjectStatic = Vector3.IsZero(obj.LinearVelocity);
                if (isObjectStatic)
                    continue;

                bool isShotEnabled = false;
                float distanceToTarget = Vector3.Distance(Ship.Position, obj.Position);
                Vector3 directionToTarget = GetTargetDirection(obj, out isShotEnabled);

                if (isShotEnabled && obj.World != null && distanceToTarget < minDistance)
                {
                    target = obj;
                    minDistance = distanceToTarget;
                    direction = directionToTarget;
                }
            }

            if (target != null)
                m_targetObjects.Remove(target);

            return direction;
        }

        private Vector3 GetBestStaticTargetDirection()
        {
            WorldObject target = null;
            Vector3 direction = Vector3.Zero;
            foreach (var obj in m_targetObjects)
            {
                if (!Vector3.IsZero(obj.LinearVelocity))
                    continue;

                bool isShotEnabled = false;
                Vector3 directionToTarget = GetTargetDirection(obj, out isShotEnabled);

                if (isShotEnabled && obj.World != null)
                {
                    target = obj;
                    direction = directionToTarget;
                    break;
                }
            }

            if (target != null)
                m_targetObjects.Remove(target);

            return direction;
        }

        private void ShootTarget(Vector3 direction)
        {
            Ship.Shoot(direction);
        }

        private Vector3 GetTargetDirection(WorldObject target, out bool isShotEnabled)
        {
            isShotEnabled = true;
            Vector3 directionToTarget = Vector3.Zero;

            if (m_shootedObjectIds.Contains(target.GetHashCode()))
            {
                isShotEnabled = false;
                return directionToTarget;
            }

            float distanceToTarget = Vector3.Distance(Ship.Position, target.Position);
            float bulletTime = distanceToTarget / Ship.GunInfo.ProjectileSpeed;
            Vector3 shipShift = Ship.LinearVelocity * bulletTime;
            Vector3 targetShift = target.LinearVelocity * bulletTime;
            Vector3 shipNextPos = Ship.Position + shipShift;
            Vector3 targetNextPos = target.Position + targetShift;
            float distanceToTargetWithShift = Vector3.Distance(shipNextPos, targetNextPos);
            bool isObjectReachable = (m_shootDistance >= distanceToTargetWithShift);
            bool isObjectMoveTowards = (distanceToTarget >= distanceToTargetWithShift);

            if (!isObjectReachable || !isObjectMoveTowards)
            {
                isShotEnabled = false;
                return directionToTarget;
            }

            if (Vector3.IsZero(Ship.LinearVelocity) && Vector3.IsZero(target.LinearVelocity))
            {
                directionToTarget = (target.Position - Ship.Position) - Vector3.Normalize(target.Position - Ship.Position) * target.BoundingRadius;
            }
            else
            {
                Vector3 targetVelocity = target.LinearVelocity - Ship.LinearVelocity;
                float bulletSpeed = Ship.GunInfo.ProjectileSpeed * Ship.GunInfo.ProjectileSpeed;
                float shipSpeed = PositionHelper.SqrMagnitude(Ship.LinearVelocity);
                float totalShipSpeed = (Vector3.IsZero(Ship.LinearVelocity)) ? bulletSpeed : (shipSpeed + bulletSpeed) * 0.5f;
                directionToTarget = PositionHelper.GetCollisionDirection(Ship.Position, target.Position, Ship.LinearVelocity, targetVelocity, totalShipSpeed, Ship.GunInfo.ProjectileRadius, target.BoundingRadius, out isShotEnabled);
            }

            m_shootedObjectIds.Add(target.GetHashCode());

            return directionToTarget;
        }
    }
}