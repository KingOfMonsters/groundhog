﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace badger
{
    public class badgerContourClipComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public badgerContourClipComponent()
            : base("Contour Clip", "Contour Clip",
                "Checks contours meet a specific boundary, otherwise extend/trim them",
                "Badger", "Terrain")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Contour Curves", "C", "The contours to clip", GH_ParamAccess.list);
            pManager[0].Optional = false;
            pManager.AddCurveParameter("Boundary", "B", "The boundary rectangle to clip to", GH_ParamAccess.item);
            pManager[1].Optional = false;
            pManager.AddBooleanParameter("Create PlanarSrfs", "P", "Whether to create planar surfaces; may be slow with large quantities of contours!", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Contours", "C", "The clipped contours", GH_ParamAccess.list);
            pManager.AddCurveParameter("Edged Contours", "E", "All contours with edges following the boundary", GH_ParamAccess.list);
            pManager.AddBrepParameter("Planar Surfaces", "P", "Edge contours as planar surfaces (must be toggled on)", GH_ParamAccess.list);
         }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {            
            // Create holder variables for input parameters
            List<Curve> ALL_CONTOURS = new List<Curve>();
            Curve BOUNDARY = default(Curve);
            bool CREATE_SRFS = false;
         
            // Access and extract data from the input parameters individually
            if (!DA.GetDataList(0, ALL_CONTOURS)) return;
            if (!DA.GetData(1, ref BOUNDARY)) return;
            DA.GetData(2, ref CREATE_SRFS);
            
            // Create holder variables for ouput parameters
            List<Curve> fixedContours = new List<Curve>();
            List<Curve> edgedContours = new List<Curve>();
            List<Brep> planarSrfs = new List<Brep>();

            // Get lowest point
            List<double> heightGuages = new List<double>();
            foreach (Curve curve in ALL_CONTOURS)
            {
                heightGuages.Add(curve.PointAtEnd.Z);
            }
            heightGuages.Sort();
            
            double contourLow = heightGuages[0];
            double contourHigh = heightGuages[heightGuages.Count - 1];

            // Move plane to lowest point
            double boundaryZ = BOUNDARY.PointAtEnd.Z;
            double boundaryMove;
            if (boundaryZ < contourLow)
            {
                boundaryMove = boundaryZ - contourLow;
            }
            else if (boundaryZ > contourLow)
            {
                boundaryMove = contourLow - boundaryZ;
            }
            else
            {
                boundaryMove = 0;
            }
            
            // Extrude up to highest - lowest
            BOUNDARY.Transform(Transform.Translation(new Vector3d(0, 0, boundaryMove - 1)));

            Vector3d boundaryExtrusion = new Vector3d(0, 0, contourHigh - contourLow + 2);
            Surface boundarySrf = Surface.CreateExtrusion(BOUNDARY, boundaryExtrusion);

            // End Point Clipping
            foreach (Curve curve in ALL_CONTOURS)
            {
                if (curve.IsValid == false)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A contour curve was not valid and has been skipped. You probably want to find and fix that curve.");
                } 
                else
                {

                    Curve curveWithEndClipped;
                    if (curve.IsClosed == false)
                    {
                        Curve curveWithStartClipped = clipCurveTerminus(curve, curve.PointAtStart, BOUNDARY, boundarySrf);
                        curveWithEndClipped = clipCurveTerminus(curveWithStartClipped, curve.PointAtEnd, BOUNDARY, boundarySrf);
                    }
                    else 
                    {
                        curveWithEndClipped = curve;
                    }

                    List<Curve> curveWithMiddlesClipped = clipMeanderingCurvesToBoundary(curveWithEndClipped, BOUNDARY, boundarySrf);
                    foreach (Curve curveClip in curveWithMiddlesClipped)
                    {
                        fixedContours.Add(curveClip);
                        Curve edgedContour = getBoundedContour(curveClip, BOUNDARY); // Create the profiles matching the boundary
                        edgedContours.Add(edgedContour);
                    }

                }


                
            }
            
            if (CREATE_SRFS == true)
            {
                Curve[] allContours = edgedContours.ToArray();
                Brep[] planarSrfsArray = new Brep[allContours.Length - 1];
                System.Threading.Tasks.Parallel.For(0, allContours.Length - 1, i => // Shitty multithreading
                {
                    Brep[] planarSurfaces = Rhino.Geometry.Brep.CreatePlanarBreps(allContours[i]); // Create planar surfaces
                    planarSrfsArray[i] = planarSurfaces[0];
                });
                planarSrfs = new List<Brep>(planarSrfsArray); // Probably unecessary
            }
            
            // Assign variables to output parameters
            DA.SetDataList(0, fixedContours);
            DA.SetDataList(1, edgedContours);
            DA.SetDataList(2, planarSrfs);
        }


        private Curve clipCurveTerminus(Curve initialCurve, Point3d point, Curve BOUNDARY, Surface boundarySrf)
        {
            // Test, for a particular point where it is in relation to boundary and clip curve accordingly

            Point3d testPoint = new Point3d(point.X, point.Y, BOUNDARY.PointAtEnd.Z); // Equalise the Z's for containment check

            Rhino.Geometry.PointContainment pointContainment = BOUNDARY.Contains(testPoint);

            if (pointContainment.ToString() == "Inside")
            {
                // Extend
                Curve extendedCurve = extendCurveTerminusToBoundary(initialCurve, point, boundarySrf);
                return extendedCurve;

            }
            else
            {
                return initialCurve;
            }

        }

        private Curve clipCurveEndsToBoundary(Curve initialCurve, Point3d targetPoint, Surface boundarySrf)
        {
            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            Curve trimmedCurveEnds;

            // Find where the boundary and curve intersect
            Curve[] intersectCurves;
            Point3d[] intersectPoints;
            Rhino.Geometry.Intersect.Intersection.CurveBrep(initialCurve, boundarySrf.ToBrep(), tolerance, out intersectCurves, out intersectPoints);

            // Get closest point from intersections and its parameters
            double? minimumDistance = null;
            int index = -1;
            for (int i = 0; i < intersectPoints.Length; i++)
            {
                double thisNum = intersectPoints[i].DistanceTo(targetPoint);
                if (!minimumDistance.HasValue || thisNum < minimumDistance.Value)
                {
                    minimumDistance = thisNum;
                    index = i;
                }
            }
            Point3d closestPoint = intersectPoints[index];
            double startTrim;
            initialCurve.ClosestPoint(closestPoint, out startTrim, 0); // Get Paramter of Intersection

            // Trim the curve bit that over extends
            if (startTrim != 0.0)
            {
                // Is the intersection closer to the start or end curve parameter?
                double t0Distance = initialCurve.PointAt(initialCurve.Domain.T0).DistanceTo(targetPoint);
                double t1Distance = initialCurve.PointAt(initialCurve.Domain.T1).DistanceTo(targetPoint);

                // Trim off the useless bit
                if (t0Distance > t1Distance)
                {
                    // If its closest to the start of the curve we delete before t0, and keep after intersect
                    trimmedCurveEnds = initialCurve.Trim(initialCurve.Domain.T0, startTrim);
                }
                else
                {
                    // If its closest to the end of the curve we delete before t0, and keep after intersect
                    trimmedCurveEnds = initialCurve.Trim(startTrim, initialCurve.Domain.T1);
                }

            }
            else
            {
                trimmedCurveEnds = initialCurve;
            }

            return trimmedCurveEnds;

        }

        private Curve extendCurveTerminusToBoundary(Curve initialCurve, Point3d startPoint, Surface boundarySrf)
        {

            Brep[] boundaryCollision = new Brep[] { boundarySrf.ToBrep() };

            Curve extendedCurve;
            if (startPoint.DistanceTo(initialCurve.PointAtEnd) == 0)
            {
                extendedCurve = initialCurve.Extend(CurveEnd.End, CurveExtensionStyle.Smooth, boundaryCollision);
            }
            else
            {
                extendedCurve = initialCurve.Extend(CurveEnd.Start, CurveExtensionStyle.Smooth, boundaryCollision);
            }
            
            return extendedCurve;

        }

        private List<Curve> clipMeanderingCurvesToBoundary(Curve initialCurve, Curve BOUNDARY, Surface boundarySrf)
        {
            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            List<Curve> returnCurves = new List<Curve>();

            Curve[] splitCurves = initialCurve.Split(boundarySrf, tolerance);

            if (splitCurves.Length > 0)
            {
                for (int i = 0; i < splitCurves.Length; i = i + 1)
                {
                    Point3d testPoint = splitCurves[i].PointAt(splitCurves[i].Domain.Mid);
                    testPoint.Z = BOUNDARY.PointAtEnd.Z;

                    Rhino.Geometry.PointContainment pointContainment = BOUNDARY.Contains(testPoint);

                    if (pointContainment.ToString() == "Inside")
                    {
                        returnCurves.Add(splitCurves[i]);
                    }
                }
            }
            else
            {
                returnCurves.Add(initialCurve);
            }
            return returnCurves;

        }

        private Curve getBoundedContour(Curve initialCurve, Curve BOUNDARY)
        {
            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // Move the boundary down to the same plane so we can do a curve curve intersection
            BOUNDARY.Transform(Transform.Translation(new Vector3d(0, 0, initialCurve.PointAtEnd.Z - BOUNDARY.PointAtEnd.Z)));

            Rhino.Geometry.Intersect.CurveIntersections ccx = Rhino.Geometry.Intersect.Intersection.CurveCurve(initialCurve, BOUNDARY, tolerance, tolerance);

            if (ccx.Count > 0)
            {
                Curve innerEdgeA = BOUNDARY.Trim(ccx[0].ParameterB, ccx[1].ParameterB); // remove before t0; after t1
                Curve innerEdgeB = BOUNDARY.Trim(ccx[1].ParameterB, ccx[0].ParameterB); // remove before t0; after t1

                // This is going to be incorrect sometimes, but we want to get the shorter of the two pieces
                if (innerEdgeA.GetLength() >= innerEdgeB.GetLength())
                {
                    Curve[] innerEdge = Curve.JoinCurves(new Curve[] { innerEdgeB, initialCurve }, tolerance);
                    return innerEdge[0];
                }
                else
                {
                    Curve[] innerEdge = Curve.JoinCurves(new Curve[] { innerEdgeA, initialCurve }, tolerance);
                    return innerEdge[0];
                }
            } else 
            {
                return initialCurve;
            }

        }

           /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return badger.Properties.Resources.icon_pplacer;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{2d234adc-dcfd-4cf7-815a-c8136d1798d0}"); }
        }
    }
}