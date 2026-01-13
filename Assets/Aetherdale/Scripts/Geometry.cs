
using System.Linq;
using UnityEngine;

namespace Geometry2D
{
    public class Line
    {
        public Vector2 v0;
        public Vector2 v1;

        public Line(Vector2 v0, Vector2 v1)
        {
            this.v0 = v0;
            this.v1 = v1;
        }

        public Vector2 GetMidPoint()
        {
            return v0 + ((v1 - v0) / 2);
        }

        public float GetLength()
        {
            return Vector2.Distance(v0, v1);
        }

        /// <summary>
        /// On X and Z
        /// </summary>
        /// <returns></returns>
        public Line GetPerpendicularBisector()
        {
            Vector2 midPoint = GetMidPoint();
            Vector2 direction = v1 - v0;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x).normalized;

            float length = 10f;
            Vector2 newStart = midPoint - perpendicular * length;
            Vector2 newEnd = midPoint + perpendicular * length;

            return new Line(newStart, newEnd);
        }

        public Vector2 GetIntersectionPoint(Line other)
        {
            float thisRise = v1.y - v0.y;
            float thisRun = v1.x - v0.x;

            float otherRise = other.v1.y - other.v0.y;
            float otherRun = other.v1.x - other.v0.x;

            // Now we have components of y = mx + b
            float mThis = thisRise / thisRun;
            // float bThis = v0.y;

            float mOther = otherRise / otherRun;
            // float bOther = other.v0.y;

            float bThis = v0.y - mThis * v0.x;
            float bOther = other.v0.y - mOther * other.v0.x;

            if (mOther == mThis)
            {
                throw new System.Exception("Tried to get intersection point of parallel lines");
            }

            Vector2 midpointThis = GetMidPoint();
            Vector2 midpointOther = other.GetMidPoint();
            float x;
            float y;


            if (thisRun == 0 && otherRise == 0)
            {
                // 90 degree angle - take midpoint coords
                x = midpointThis.x;
                y = midpointOther.y;
            }
            else if (thisRise == 0 && otherRun == 0)
            {
                x = midpointOther.x;
                y = midpointThis.y;
            }
            else if (thisRun == 0)
            {
                y = midpointThis.y;

                // y == Mx + b on other equation
                // y == x * mOther + bOther
                // x * mOther == y - bOther
                // x == (y - bOther) / mOther
                x = (y - bOther) / mOther;

            }
            else if (thisRise == 0)
            {
                x = midpointThis.x;

                // y == Mx + b on other equation
                // y == x + bOther
                y = mOther * x + bOther;
            }
            else if (otherRun == 0)
            {
                y = midpointOther.y;

                // y == Mx + b on this equation
                // y == x * mThis + bThis
                // x * mThis == y - bThis
                // x == (y - bThis) / bThis
                x = (y - bThis) / mThis;

            }
            else if (otherRise == 0)
            {
                x = midpointOther.x;

                // y == Mx + b on this equation
                // y == x + bThis
                y = mThis * x + bThis;
            }
            else
            {
                // Find x where:
                // x * mThis + bThis == x * mOther + bOther
                // Thus, x * (mThis - mOther) == bOther - bThis
                // Thus, x == (bOther - bThis) / (mThis - mOther)

                x = (bOther - bThis) / (mThis - mOther);

                // Now use either formula, in this case the 'This' formula, to calculate the y
                y = mThis * x + bThis;
            }

            return new Vector2(x, y);
        }

        public static bool operator ==(Line e1, Line e2)
        {
            return (e1.v0 == e2.v0 && e1.v1 == e2.v1) || (e1.v0 == e2.v1 && e1.v1 == e2.v0);
        }

        public static bool operator !=(Line e1, Line e2)
        {
            return !(e1 == e2);
        }
    }

    public class Triangle
    {
        public Vector2 v0;
        public Vector2 v1;
        public Vector2 v2;

        public Circle circumCircle;

        public Circle CalculateCircumCircle()
        {
            Vector2 A = v0;
            Vector2 B = v1;
            Vector2 C = v2;

            float d = 2 * (A.x * (B.y - C.y) +
                        B.x * (C.y - A.y) +
                        C.x * (A.y - B.y));

            if (Mathf.Abs(d) < 1e-6f)
            {
                // Points are colinear or nearly so — invalid triangle
                return new Circle(Vector2.zero, float.PositiveInfinity);
            }

            float A2 = A.sqrMagnitude;
            float B2 = B.sqrMagnitude;
            float C2 = C.sqrMagnitude;

            float ux = (A2 * (B.y - C.y) + B2 * (C.y - A.y) + C2 * (A.y - B.y)) / d;
            float uy = (A2 * (C.x - B.x) + B2 * (A.x - C.x) + C2 * (B.x - A.x)) / d;

            Vector2 center = new Vector2(ux, uy);
            float radius = Vector2.Distance(center, A);

            return new Circle(center, radius);
        }

        public Triangle(Vector2 v0, Vector2 v1, Vector2 v2)
        {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;

            this.circumCircle = CalculateCircumCircle();
        }

        public Line[] GetLines()
        {
            Line[] lines = new Line[3];
            lines[0] = new(v0, v1);
            lines[1] = new(v1, v2);
            lines[2] = new(v2, v0);

            return lines;
        }

        public bool ContainsPoint(Vector2 point)
        {
            return point == v0 || point == v1 || point == v2;
        }

        public bool ContainsLine(Line targetLine)
        {
            foreach (Line line in GetLines())
            {
                if (line == targetLine)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class Circle
    {
        public Vector3 center;
        public float radius;

        public Circle(Vector3 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }

        public bool ContainsPoint(Vector3 point)
        {
            return Vector3.Distance(center, point) <= radius;
        }
    }

    [System.Serializable]
    public class Triangle3D
    {
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;

        public Triangle3D(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;
        }

        public Vector3 GetCenter()
        {
            return new(
                (v0.x + v1.x + v2.x) / 3,
                (v0.y + v1.y + v2.y) / 3,
                (v0.z + v1.z + v2.z) / 3
            );
        }


    }

}