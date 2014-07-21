using Microsoft.Xna.Framework;

namespace SharpSteer2
{
    public static class StaticLocalSpace
    {
        /// <summary>
        /// Transforms a direction in global space to its equivalent in local space.
        /// </summary>
        /// <param name="basis">The basis which this should operate on</param>
        /// <param name="globalDirection">The global space direction to transform.</param>
        /// <returns>The global space direction transformed to local space .</returns>
        public static Vector3 LocalizeDirection(this ILocalSpaceBasis basis, Vector3 globalDirection)
        {
            // dot offset with local basis vectors to obtain local coordiantes
            return new Vector3(Vector3.Dot(globalDirection, basis.Side), Vector3.Dot(globalDirection, basis.Up), Vector3.Dot(globalDirection, basis.Forward));
        }

        /// <summary>
        /// Transforms a point in global space to its equivalent in local space.
        /// </summary>
        /// <param name="basis">The basis which this should operate on</param>
        /// <param name="globalPosition">The global space position to transform.</param>
        /// <returns>The global space position transformed to local space.</returns>
        public static Vector3 LocalizePosition(this ILocalSpaceBasis basis, Vector3 globalPosition)
        {
            // global offset from local origin
            Vector3 globalOffset = globalPosition - basis.Position;

            // dot offset with local basis vectors to obtain local coordiantes
            return LocalizeDirection(basis, globalOffset);
        }

        /// <summary>
        /// Transforms a point in local space to its equivalent in global space.
        /// </summary>
        /// <param name="basis">The basis which this should operate on</param>
        /// <param name="localPosition">The local space position to tranform.</param>
        /// <returns>The local space position transformed to global space.</returns>
        public static Vector3 GlobalizePosition(this ILocalSpaceBasis basis, Vector3 localPosition)
        {
            return basis.Position + GlobalizeDirection(basis, localPosition);
        }

        /// <summary>
        /// Transforms a direction in local space to its equivalent in global space.
        /// </summary>
        /// <param name="basis">The basis which this should operate on</param>
        /// <param name="localDirection">The local space direction to tranform.</param>
        /// <returns>The local space direction transformed to global space</returns>
        public static Vector3 GlobalizeDirection(this ILocalSpaceBasis basis, Vector3 localDirection)
        {
            return ((basis.Side * localDirection.X) +
                    (basis.Up * localDirection.Y) +
                    (basis.Forward * localDirection.Z));
        }

        /// <summary>
        /// Rotates, in the canonical direction, a vector pointing in the
        /// "forward" (+Z) direction to the "side" (+/-X) direction as implied
        /// by IsRightHanded.
        /// </summary>
        /// <param name="basis">The basis which this should operate on</param>
        /// <param name="value">The local space vector.</param>
        /// <returns>The rotated vector.</returns>
        public static Vector3 LocalRotateForwardToSide(this ILocalSpaceBasis basis, Vector3 value)
        {
            return new Vector3(-value.Z, value.Y, value.X);
        }

        /// <summary>
        /// Rotates, in the canonical direction, a vector pointing in the
        /// "forward" (+Z) direction to the "side" (+/-X) direction as implied
        /// by IsRightHanded.
        /// </summary>
        /// <param name="basis">The basis which this should operate on</param>
        /// <param name="value">The global space forward.</param>
        /// <returns>The rotated vector.</returns>
        public static Vector3 GlobalRotateForwardToSide(this ILocalSpaceBasis basis, Vector3 value)
        {
            Vector3 localForward = basis.LocalizeDirection(value);
            Vector3 localSide = basis.LocalRotateForwardToSide(localForward);
            return basis.GlobalizeDirection(localSide);
        }
    }
}
