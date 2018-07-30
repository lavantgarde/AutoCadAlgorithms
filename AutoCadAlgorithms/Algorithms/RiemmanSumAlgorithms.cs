using System;
using System.Linq;
using Dreambuild;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AutoCadAlgorithms.Algorithms
{
    public static class RiemmanSumAlgorithms
    {
        public enum RiemmanSumRule
        {
            Left = 1, Right = 2, Middle = 3
        }

        /// <summary>
        /// Gets an array of rectangles used in a Riemman sum approximation.
        /// </summary>
        /// <param name="upperboundCurve">The curve to be approximated.</param>
        /// <param name="lowerbound">The line defining the range under the upperbound curve.</param>
        /// <param name="riemmanSumRule">The approximation rule.</param>
        /// <param name="intervals">The number of intervals, or rectangles, to use. </param>
        /// <param name="width">The width of the intervals. </param>
        /// <returns></returns>
        public static Rectangle3d[] RiemmanSumRectangles(this Curve upperboundCurve, Line lowerbound, RiemmanSumRule riemmanSumRule, int intervals, double width)
        {
            try
            {
                var trueIntervals = intervals == 0 ? Math.Floor(lowerbound.Length / width) : intervals;
                var rectangles = new Rectangle3d[Convert.ToInt32(trueIntervals)];

                double x = intervals == 0 ? width : lowerbound.Length / intervals;

                for (var k = 0; k < trueIntervals; k++)
                {
                    // p2 p3
                    // p1 p4

                    var p1 = new Point3d(
                        x: lowerbound.StartPoint.X + (x * k),
                        y: lowerbound.StartPoint.Y,
                        z: lowerbound.StartPoint.Z);

                    var p4 = new Point3d(
                        x: lowerbound.StartPoint.X + (x * (k + 1)),
                        y: lowerbound.StartPoint.Y,
                        z: lowerbound.StartPoint.Z);

                    var p2Intersect = new Line(p1,
                        new Point3d(p1.X, upperboundCurve.GeometricExtents.MaxPoint.Y + 1, p1.Z));
                    var p3Intersect = new Line(p4,
                        new Point3d(p4.X, upperboundCurve.GeometricExtents.MaxPoint.Y + 1, p4.Z));

                    var p2Intersection = upperboundCurve.Intersect(p2Intersect,
                        Autodesk.AutoCAD.DatabaseServices.Intersect.ExtendArgument)?[0]; // y = f(x0)
                    var p3Intersection = upperboundCurve.Intersect(p3Intersect,
                        Autodesk.AutoCAD.DatabaseServices.Intersect.ExtendArgument)?[0]; // y = f(x1)

                    var p2 = p2Intersection ?? throw new ArgumentOutOfRangeException();
                    var p3 = p3Intersection ?? throw new ArgumentOutOfRangeException();

                    switch (riemmanSumRule)
                    {
                        case RiemmanSumRule.Left:
                            {
                                p3 = new Point3d(p3.X, p2.Y, p3.Z);
                                break;
                            }
                        case RiemmanSumRule.Right:
                            {
                                p2 = new Point3d(p2.X, p3.Y, p2.Z);
                                break;
                            }
                        case RiemmanSumRule.Middle:
                            {
                                var midx = (p4.X - p1.X) / 2;
                                var midLine = new Line(new Point3d(p1.X + midx, p1.Y, p1.Z),
                                    new Point3d(p1.X + midx, upperboundCurve.GeometricExtents.MaxPoint.Y + 1, p1.Z));

                                var midIntersection = upperboundCurve.Intersect(midLine,
                                    Autodesk.AutoCAD.DatabaseServices.Intersect.ExtendArgument)?[0];

                                var intersect = midIntersection ?? throw new ArgumentOutOfRangeException();

                                p2 = new Point3d(p1.X, intersect.Y, p1.Z);
                                p3 = new Point3d(p4.X, intersect.Y, p4.Z);

                                break;
                            }
                        default:
                            throw new ArgumentOutOfRangeException(nameof(riemmanSumRule), riemmanSumRule, null);
                    }

                    var rectangle = new Rectangle3d(p2, p3, p1, p4);
                    rectangles[k] = rectangle;
                }

                return rectangles.ToArray();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Interaction.WriteLine($"An error occured.\n{ex.Message}");
                throw;
            }

        }

        /// <summary>
        /// Gets an array of rectangles used in a Riemman sum approximation.
        /// </summary>
        /// <param name="upperboundCurve">The curve to be approximated.</param>
        /// <param name="lowerbound">The line defining the range under the upperbound curve.</param>
        /// <param name="riemmanSumRule">The approximation rule.</param>
        /// <param name="intervals">The number of intervals, or rectangles, to use.</param>
        /// <param name="intervalWidth"></param>
        /// <param name="offset">The height by which to offset the rectangles above the upperbound curve.</param>
        /// <returns></returns>
        public static Rectangle3d[] RiemmanSumRectangles(this Curve upperboundCurve, Line lowerbound,
            RiemmanSumRule riemmanSumRule, int intervals, double intervalWidth, double offset)
        {
            Curve upperCurve;

            if (Math.Abs(offset) > .000000)
            {
                var transformation = Point3d.Origin.GetVectorTo(new Point3d(0, offset, 0));
                upperCurve = (Curve)upperboundCurve.GetTransformedCopy(Matrix3d.Displacement(transformation));
            }
            else
            {
                upperCurve = upperboundCurve;
            }

            return upperCurve.RiemmanSumRectangles(lowerbound, riemmanSumRule, intervals, intervalWidth);
        }
    }
}
