using System.ComponentModel;
using OpenTK.Mathematics;
using PathTracer.Helpers;

namespace PathTracer.BVH;

public class BVHNodeSpatialSplit : BVHNode {
    public BVHNodeSpatialSplit(AABB boundingBox, int maxNumTriangles) : base(boundingBox, maxNumTriangles) { }
    public BVHNodeSpatialSplit(AABB boundingBox, BVHNode node1, BVHNode node2, int numTriangles) : base(boundingBox, node1, node2, numTriangles) { }

    public override void Split() {
        var splitPoint = BoundingBox.Min;
        var longestAxis = BoundingBox.GetLongestAxis();
        SplitAxis = longestAxis;
        Vector3 min;

        var trianglesLeft = new List<Triangle>();
        var trianglesRight = new List<Triangle>();

        // Calculate split plane perpendicular to vector from BB min
        // splitPoint always lies on one of the basis vectors of the BB
        if (longestAxis == Axis.X) {
            splitPoint.X = BoundingBox.Min.X + BoundingBox.LengthX / 2.0f;
            min = new Vector3(BoundingBox.Min.X, BoundingBox.Max.Y, BoundingBox.Max.Z);
            foreach (var triangle in Triangles) // Add triangles to either left or right subnode according to barycenter
                if (triangle.Center.X < splitPoint.X) trianglesLeft.Add(triangle);
                else trianglesRight.Add(triangle);
        }
        else if (longestAxis == Axis.Y) {
            splitPoint.Y = BoundingBox.Min.Y + BoundingBox.LengthY / 2.0f;
            min = new Vector3(BoundingBox.Max.X, BoundingBox.Min.Y, BoundingBox.Max.Z);
            foreach (var triangle in Triangles)
                if (triangle.Center.Y < splitPoint.Y) trianglesLeft.Add(triangle);
                else trianglesRight.Add(triangle);
        }
        else if (longestAxis == Axis.Z) {
            splitPoint.Z = BoundingBox.Min.Z + BoundingBox.LengthZ / 2.0f;
            min = new Vector3(BoundingBox.Max.X, BoundingBox.Max.Y, BoundingBox.Min.Z);
            foreach (var triangle in Triangles)
                if (triangle.Center.Z < splitPoint.Z) trianglesLeft.Add(triangle);
                else trianglesRight.Add(triangle);
        }
        else {
            throw new InvalidEnumArgumentException("Invalid split axis specified");
        }

        var left = new BVHNodeSpatialSplit(new AABB(min, splitPoint), MaxNumTriangles);
        left.AddTriangles(trianglesLeft);
        var right = new BVHNodeSpatialSplit(new AABB(splitPoint, BoundingBox.Max), MaxNumTriangles);
        right.AddTriangles(trianglesRight);

        SetChildren(left, right);
        Triangles.Clear();
    }
}