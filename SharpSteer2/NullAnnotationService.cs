﻿
using Microsoft.Xna.Framework;

namespace SharpSteer2
{
    class NullAnnotationService
        :IAnnotationService
    {
        public bool IsEnabled
        {
            get
            {
                return false;
            }
            set
            {
            }
        }

        public void Line(Vector3 startPoint, Vector3 endPoint, Color color, float opacity = 1)
        {

        }

        public void CircleXZ(float radius, Microsoft.Xna.Framework.Vector3 center, Microsoft.Xna.Framework.Color color, int segments)
        {

        }

        public void DiskXZ(float radius, Microsoft.Xna.Framework.Vector3 center, Microsoft.Xna.Framework.Color color, int segments)
        {

        }

        public void Circle3D(float radius, Microsoft.Xna.Framework.Vector3 center, Microsoft.Xna.Framework.Vector3 axis, Microsoft.Xna.Framework.Color color, int segments)
        {

        }

        public void Disk3D(float radius, Microsoft.Xna.Framework.Vector3 center, Microsoft.Xna.Framework.Vector3 axis, Microsoft.Xna.Framework.Color color, int segments)
        {

        }

        public void CircleOrDiskXZ(float radius, Microsoft.Xna.Framework.Vector3 center, Microsoft.Xna.Framework.Color color, int segments, bool filled)
        {

        }

        public void CircleOrDisk3D(float radius, Microsoft.Xna.Framework.Vector3 center, Microsoft.Xna.Framework.Vector3 axis, Microsoft.Xna.Framework.Color color, int segments, bool filled)
        {

        }

        public void CircleOrDisk(float radius, Microsoft.Xna.Framework.Vector3 axis, Microsoft.Xna.Framework.Vector3 center, Microsoft.Xna.Framework.Color color, int segments, bool filled, bool in3D)
        {

        }

        public void AvoidObstacle(float minDistanceToCollision)
        {

        }

        public void PathFollowing(Microsoft.Xna.Framework.Vector3 future, Microsoft.Xna.Framework.Vector3 onPath, Microsoft.Xna.Framework.Vector3 target, float outside)
        {

        }

        public void AvoidCloseNeighbor(IVehicle other, float additionalDistance)
        {

        }

        public void AvoidNeighbor(IVehicle threat, float steer, Microsoft.Xna.Framework.Vector3 ourFuture, Microsoft.Xna.Framework.Vector3 threatFuture)
        {

        }

        public void VelocityAcceleration(IVehicle vehicle)
        {

        }

        public void VelocityAcceleration(IVehicle vehicle, float maxLength)
        {

        }

        public void VelocityAcceleration(IVehicle vehicle, float maxLengthAcceleration, float maxLengthVelocity)
        {

        }
    }
}
