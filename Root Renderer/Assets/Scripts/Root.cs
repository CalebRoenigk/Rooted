using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Root
{
    public List<RootPoint> Points = new List<RootPoint>();
    public AnimationCurve WidthCurve;
    public float Length = 0f;
    public Root Parent;
    public List<Root> Children = new List<Root>();

    public Root()
    {
        
    }

    // Adds a root point to the root
    public void AddRootPoint(RootPoint rootPoint)
    {
        // Add the point to the list
        Points.Add(rootPoint);
        
        // Get the total length of the root
        if (Points.Count > 1)
        {
            float segmentLength = Vector3.Distance(Points[Points.Count - 2].Position, Points[Points.Count - 1].Position);
            Length += segmentLength;
        }
        else
        {
            Length += Points[Points.Count - 1].Position.magnitude;
        }
        
        // Update the width curve
        WidthCurve = CalculateWidthCurve();
    }
    
    // Returns the width curve of the root
    private AnimationCurve CalculateWidthCurve()
    {
        // Create the return width curve
        AnimationCurve widthCurve = new AnimationCurve();
        
        // Iterate over the points in the root
        float iterationLength = 0f;
        Vector3 lastPoint = Points[0].Position;
        float lastWidth = 0f;
        foreach (RootPoint rootPoint in Points)
        {
            // Get the length of this segment
            float segmentLength = Vector3.Distance(lastPoint, rootPoint.Position);

            // Create a new key for the current point if the current point width is different from the previous width
            if (lastWidth != rootPoint.Width)
            {
                widthCurve.AddKey((iterationLength + segmentLength) / Length, rootPoint.Width);
                
                // Store the new width
                lastWidth = rootPoint.Width;
            }

            // Store the last position and add to the iteration length
            lastPoint = rootPoint.Position;
            iterationLength += segmentLength;
        }
        
        // Return the width curve
        return widthCurve;
    }
    
    // Adds a child root to the root
    public void AddChild(Root child)
    {
        child.Parent = this;
        this.Children.Add(child);
    }
    
    // Returns the first ungrown root in the root stack or a root with no parent if there are no other ungrown roots
    public Root GetUngrownRoot()
    {
        // First check if this root has any children that are ungrown
        foreach (Root child in Children)
        {
            if (child.Length == 0f)
            {
                return child;
            }
        }
        
        // Check if this root has a parent
        if (this.Parent == null)
        {
            return new Root();
        }
            
        // The root has no ungrown children, check up the root for ungrown children
        return Parent.GetUngrownRoot();
    }
}
