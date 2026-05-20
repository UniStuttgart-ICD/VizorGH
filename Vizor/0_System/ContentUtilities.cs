// This file contains utility methods for rendering content objects in the Rhino viewport.

using Rhino.Geometry;
using System;
using System.Drawing;
using VizorLibs;

namespace Vizor._2_Content
{
    /// <summary>
    /// The ContentUtilities class provides static methods for rendering AR content objects 
    /// (meshes, wireframes, and texts) in the Rhino viewport.
    /// </summary>
    public static class ContentUtilities
    {
        #region Mesh Rendering

        /// <summary>
        /// Draws a shaded mesh in the viewport with the specified display arguments.
        /// </summary>
        /// <param name="args">The preview display arguments.</param>
        /// <param name="geom">The scene geometry object containing the mesh and material.</param>
        public static void DrawMeshShaded(Grasshopper.Kernel.IGH_PreviewArgs args, SceneGeometryObject geom)
        {
            if (geom == null || geom.gMesh == null)
                return;

            Color meshColor = ParseColorFromMaterial(geom.material, Color.Gray);

            // Create display material
            Rhino.Display.DisplayMaterial displayMaterial = new Rhino.Display.DisplayMaterial(meshColor);
            displayMaterial.Transparency = 1.0 - (meshColor.A / 255.0);

            // Draw the mesh
            args.Display.DrawMeshShaded(geom.gMesh, displayMaterial);
        }

        /// <summary>
        /// Draws mesh edges in the viewport with the specified display arguments.
        /// </summary>
        /// <param name="args">The preview display arguments.</param>
        /// <param name="geom">The scene geometry object containing the mesh and material.</param>
        public static void DrawMeshWires(Grasshopper.Kernel.IGH_PreviewArgs args, SceneGeometryObject geom)
        {
            if (geom == null || geom.gMesh == null)
                return;

            Color meshColor = ParseColorFromMaterial(geom.material, Color.Gray);

            // Draw mesh edges in darker color
            Color edgeColor = Color.FromArgb(
                Math.Max(0, meshColor.R - 50),
                Math.Max(0, meshColor.G - 50),
                Math.Max(0, meshColor.B - 50)
            );

            args.Display.DrawMeshWires(geom.gMesh, edgeColor);
        }

        #endregion

        #region Wireframe Rendering

        /// <summary>
        /// Draws a wireframe object in the viewport with the specified display arguments.
        /// </summary>
        /// <param name="args">The preview display arguments.</param>
        /// <param name="wireframe">The scene wireframe object containing points, material, and width.</param>
        public static void DrawWireframe(Grasshopper.Kernel.IGH_PreviewArgs args, SceneWireframeObject wireframe)
        {
            if (wireframe == null || wireframe.points == null || wireframe.points.Length < 2)
                return;

            Color wireColor = ParseColorFromMaterial(wireframe.material, Color.Magenta);

            // Get width and scale it for viewport display (mm to pixels)
            int displayWidth = (int) Math.Max(1, Math.Min(10, wireframe.width / 2));

            // Create polyline and draw it
            Polyline polyline = new Polyline(wireframe.points);
            args.Display.DrawPolyline(polyline, wireColor, displayWidth);

            // Draw points at vertices
            foreach (Point3d pt in wireframe.points)
            {
                args.Display.DrawPoint(pt, Rhino.Display.PointStyle.Circle, 1, wireColor);
            }
        }

        #endregion

        #region Text Rendering

        /// <summary>
        /// Draws a text object in the viewport with the specified display arguments.
        /// </summary>
        /// <param name="args">The preview display arguments.</param>
        /// <param name="textObj">The scene text object containing text content, plane, and material.</param>
        public static void DrawText(Grasshopper.Kernel.IGH_PreviewArgs args, SceneTextObject textObj)
        {
            if (textObj == null)
                return;

            Color textColor = ParseColorFromMaterial(textObj.material, Color.Black);

            // Draw plane origin and axes
            args.Display.DrawPoint(textObj.plane.Origin, Rhino.Display.PointStyle.X, 3, textColor);

            //double axisLength = 0.1;
            //args.Display.DrawArrow(new Line(textObj.plane.Origin, textObj.plane.Origin + textObj.plane.XAxis * axisLength), textColor);
            //args.Display.DrawArrow(new Line(textObj.plane.Origin, textObj.plane.Origin + textObj.plane.YAxis * axisLength), textColor);
            //args.Display.DrawArrow(new Line(textObj.plane.Origin, textObj.plane.Origin + textObj.plane.ZAxis * axisLength), textColor);
            Plane displayPlane = new Plane(textObj.plane);
            displayPlane.Rotate(Math.PI, displayPlane.XAxis);
            displayPlane.Rotate(Math.PI / 2, displayPlane.ZAxis);

            // Draw text
            args.Display.Draw3dText(textObj.text, textColor, displayPlane, 36, "Arial");
        }

        #endregion

        #region Bounding Box Utilities

        /// <summary>
        /// Gets the bounding box for a mesh geometry object.
        /// </summary>
        /// <param name="geom">The scene geometry object.</param>
        /// <returns>The bounding box of the mesh, or an empty box if the mesh is null.</returns>
        public static BoundingBox GetMeshBoundingBox(SceneGeometryObject geom)
        {
            if (geom != null && geom.gMesh != null)
            {
                return geom.gMesh.GetBoundingBox(false);
            }
            return BoundingBox.Empty;
        }

        /// <summary>
        /// Gets the bounding box for a wireframe object.
        /// </summary>
        /// <param name="wireframe">The scene wireframe object.</param>
        /// <returns>The bounding box containing all wireframe points, or an empty box if points are null.</returns>
        public static BoundingBox GetWireframeBoundingBox(SceneWireframeObject wireframe)
        {
            BoundingBox bbox = BoundingBox.Empty;

            if (wireframe != null && wireframe.points != null)
            {
                foreach (Point3d pt in wireframe.points)
                {
                    bbox.Union(pt);
                }
            }

            return bbox;
        }

        /// <summary>
        /// Gets the bounding box for a text object.
        /// </summary>
        /// <param name="textObj">The scene text object.</param>
        /// <returns>The bounding box containing the text plane origin, or an empty box if the object is null.</returns>
        public static BoundingBox GetTextBoundingBox(SceneTextObject textObj)
        {
            BoundingBox bbox = BoundingBox.Empty;

            if (textObj != null)
            {
                bbox.Union(textObj.plane.Origin);
            }

            return bbox;
        }

        #endregion

        #region Color Parsing

        /// <summary>
        /// Parses a color from a material string in the format "R,G,B,A".
        /// </summary>
        /// <param name="material">The material string containing RGBA values.</param>
        /// <param name="defaultColor">The default color to use if parsing fails.</param>
        /// <returns>The parsed color, or the default color if parsing fails.</returns>
        public static Color ParseColorFromMaterial(string material, Color defaultColor)
        {
            if (string.IsNullOrEmpty(material))
                return defaultColor;

            try
            {
                string[] colorParts = material.Split(',');
                if (colorParts.Length >= 3)
                {
                    int r = int.Parse(colorParts[0]);
                    int g = int.Parse(colorParts[1]);
                    int b = int.Parse(colorParts[2]);
                    int a = colorParts.Length >= 4 ? int.Parse(colorParts[3]) : 255;
                    return Color.FromArgb(a, r, g, b);
                }
            }
            catch
            {
                // Return default color on parse failure
            }

            return defaultColor;
        }

        #endregion
    }
}