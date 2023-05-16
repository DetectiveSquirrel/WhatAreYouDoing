using GameOffsets.Native;
using SharpDX;
using System.Collections.Generic;
using System.Linq;
using Vector2 = System.Numerics.Vector2;

namespace WhatAreYouDoing
{
    /// <summary>
    /// Provides extension methods for objects.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Converts a list of <see cref="Vector2i"/> objects to a list of <see cref="System.Numerics.Vector2"/> objects.
        /// </summary>
        /// <param name="vector2iList">The list of <see cref="Vector2i"/> objects to convert.</param>
        /// <returns>A list of <see cref="System.Numerics.Vector2"/> objects.</returns>
        public static List<Vector2> ConvertToVector2List(this IList<Vector2i> vector2iList)
        {
            return vector2iList.Select(v => new Vector2(v.X, v.Y)).ToList();
        }

        /// <summary>
        /// Adds an offset to each Vector2 in the list.
        /// </summary>
        /// <param name="vectorList">The list of Vector2 objects to modify.</param>
        /// <param name="offsetX">The offset value to add to the X component of each vector.</param>
        /// <param name="offsetY">The offset value to add to the Y component of each vector.</param>
        public static List<Vector2> AddOffset(this List<Vector2> vectorList, float offsetX, float offsetY)
        {
            List<Vector2> modifiedList = new List<Vector2>(vectorList.Count);

            for (int i = 0; i < vectorList.Count; i++)
            {
                Vector2 vector = vectorList[i];
                vector.X += offsetX;
                vector.Y += offsetY;
                modifiedList.Add(vector);
            }

            return modifiedList;
        }

        /// <summary>
        /// Adds an offset to each Vector2 in the list.
        /// </summary>
        /// <param name="vectorList">The list of Vector2 objects to modify.</param>
        /// <param name="offset">The offset value to add to each vector.</param>
        public static List<Vector2> AddOffset(this List<Vector2> vectorList, float offset)
        {
            List<Vector2> modifiedList = new List<Vector2>(vectorList.Count);

            for (int i = 0; i < vectorList.Count; i++)
            {
                Vector2 vector = vectorList[i];
                vector.X += offset;
                vector.Y += offset;
                modifiedList.Add(vector);
            }

            return modifiedList;
        }

        /// <summary>
        /// Adds an offset to the X and Y components of a Vector2.
        /// </summary>
        /// <param name="vector">The Vector2 to modify.</param>
        /// <param name="offsetX">The offset value to add to the X component.</param>
        /// <param name="offsetY">The offset value to add to the Y component.</param>
        /// <returns>The modified Vector2 with the added offset.</returns>
        public static Vector2 AddOffset(this Vector2 vector, float offsetX, float offsetY)
        {
            vector.X += offsetX;
            vector.Y += offsetY;
            return vector;
        }

        /// <summary>
        /// Adds an offset to the X and Y components of a Vector2.
        /// </summary>
        /// <param name="vector">The Vector2 to modify.</param>
        /// <param name="offset">The offset value to add to each vector.</param>
        /// <returns>The modified Vector2 with the added offset.</returns>
        public static Vector2 AddOffset(this Vector2 vector, float offset)
        {
            vector.X += offset;
            vector.Y += offset;
            return vector;
        }

        /// <summary>
        /// Converts a Vector2 to a List<Vector2>.
        /// </summary>
        /// <param name="vector">The Vector2 to convert.</param>
        /// <returns>A List<Vector2> containing the original Vector2.</returns>
        public static List<Vector2> ToList(this Vector2 vector)
        {
            return new List<Vector2> { vector };
        }

        /// <summary>
        /// Converts a Vector2i to a List<Vector2>.
        /// </summary>
        /// <param name="vector">The Vector2 to convert.</param>
        /// <returns>A List<Vector2> containing the original Vector2.</returns>
        public static List<Vector2> ToList(this Vector2i vector)
        {
            return new List<Vector2> { vector };
        }

        /// <summary>
        /// Sets a new alpha value for the color.
        /// </summary>
        /// <param name="color">The original color.</param>
        /// <param name="alpha">The new alpha value.</param>
        /// <returns>A new color with the specified alpha value.</returns>
        public static Color WithAlpha(this Color color, byte alpha)
        {
            return new Color(color.R, color.G, color.B, alpha);
        }
    }
}