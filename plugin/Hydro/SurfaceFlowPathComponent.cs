﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using groundhog.Properties;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino;
using Rhino.Geometry;

namespace groundhog
{
    public class GroundhogSurfaceFlowComponent : GroundHogComponent
    {
        public GroundhogSurfaceFlowComponent()
            : base("Flow Projection (Surface)", "Srf Flows", "Construct flow paths over a surface", "Groundhog", "Hydro")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override Bitmap Icon => Resources.icon_flows_srf;

        public override Guid ComponentGuid => new Guid("{2d268bdc-ecaa-4cf7-815a-c8111d1798d1}");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "Base landscape form (as a surface) for the flow calculation", GH_ParamAccess.item);
            pManager.AddPointParameter("Points", "P", "Start points for the flow paths. These should be above the surface (they will be projected on to it)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Fidelity", "F", "Amount to move for each flow iteration. Small numbers may take a long time to compute. If not specified or set to 0 a (hopefully) sensible step size will be calculated.", GH_ParamAccess.item, 0);
            pManager[2].Optional = true;
            pManager.AddIntegerParameter("Steps", "L", "A limit to the number of flow iterations. Leave unset or to 0 for an unlimited set of iterations", GH_ParamAccess.item, 0);
            pManager[3].Optional = true;
            pManager.AddBooleanParameter("Thread", "T", "Whether to multithread the solution (this can speed up long calculations)", GH_ParamAccess.item, false);
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Flow Points", "F", "The points of each simulated flow path 'jump'", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Flow Paths", "C", "A polyline linking each of the flow points into a path", GH_ParamAccess.list);
        }

        protected override void GroundHogSolveInstance(IGH_DataAccess DA)
        {
            // Create holder variables for input parameters
            var FLOW_SURFACE = default(Surface);
            var FLOW_ORIGINS = new List<Point3d>();
            var FLOW_FIDELITY = 1000.0; // Default Value
            var FLOW_LIMIT = 0; // Default Value
            var THREAD = false;

            // Access and extract data from the input parameters individually
            DA.GetData(0, ref FLOW_SURFACE);
            if (!DA.GetDataList(1, FLOW_ORIGINS)) return;
            if (!DA.GetData(2, ref FLOW_FIDELITY)) return;
            if (!DA.GetData(3, ref FLOW_LIMIT)) return;
            if (!DA.GetData(4, ref THREAD)) return;

            // Validation
            if (FLOW_SURFACE == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A null item has been provided as the Surface input; please correct this input.");
                return;
            }
            // TODO: properly cull nulls; they come through as 0,0,0 however
            if (FLOW_ORIGINS.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid points have been provided; perhaps check that you have not provided null or invalid points?");
                return;
            }
            if (FLOW_FIDELITY == 0)
            {
                FLOW_FIDELITY = FlowCalculations.getSensibleFidelity(FLOW_ORIGINS, null, FLOW_SURFACE);
            }

            var startPoints = FLOW_ORIGINS.ToArray(); // Array for multithreading
            var allFlowPathPoints = new List<Point3d>[startPoints.Length]; // Array of all the paths
            var flowPoints = new List<Point3d>();

            var FLOW_BREP = default(Brep);
            if (FLOW_SURFACE != default(Surface)) FLOW_BREP = FLOW_SURFACE.ToBrep();

            if (THREAD)
                Parallel.For(0, startPoints.Length, i => // Shitty multithreading
                    {
                        allFlowPathPoints[i] = DispatchFlowPoints(FLOW_BREP, startPoints[i], FLOW_FIDELITY, FLOW_LIMIT);
                    }
                );
            else
                for (var i = 0; i < startPoints.Length; i = i + 1)
                    allFlowPathPoints[i] = DispatchFlowPoints(FLOW_BREP, startPoints[i], FLOW_FIDELITY, FLOW_LIMIT);

            var outputs = FlowCalculations.MakeOutputs(allFlowPathPoints);
            // Assign variables to output parameters
            DA.SetDataTree(0, outputs.Item1);
            DA.SetDataList(1, outputs.Item2);
        }

        private List<Point3d> DispatchFlowPoints(Brep FLOW_SURFACE, Point3d initialStartPoint,
                                                 double MOVE_DISTANCE, int FLOW_LIMIT)
        {
            var flowPoints = new List<Point3d>(); // Holds each step

            var startPoint = FLOW_SURFACE.ClosestPoint(initialStartPoint);
            flowPoints.Add(startPoint);

            while (true)
            {
                Point3d nextPoint;
                nextPoint = GetNextFlowStepOnSurface(FLOW_SURFACE, startPoint, MOVE_DISTANCE);

                if (nextPoint.DistanceTo(startPoint) <= RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                    break; // Test the point has actully moved
                if (nextPoint.Z >= startPoint.Z)
                    break; // Test this point is actually lower
                flowPoints.Add(nextPoint);
                if (FLOW_LIMIT != 0 && FLOW_LIMIT <= flowPoints.Count)
                    break; // Stop if iteration limit reached
                startPoint = nextPoint; // Checks out; iterate on
            }

            return flowPoints;
        }

        private Point3d GetNextFlowStepOnSurface(Brep FLOW_SURFACE, Point3d startPoint, double MOVE_DISTANCE)
        {
            double closestS, closestT;
            double maximumDistance = 0; // TD: setting this as +ve speeds up the search?
            Vector3d closestNormal;
            ComponentIndex closestCI;
            Point3d closestPoint;

            // Get closest point
            FLOW_SURFACE.ClosestPoint(startPoint, out closestPoint, out closestCI, out closestS, out closestT,
                                      maximumDistance, out closestNormal);
            // Get the next point following the vector
            var nextFlowPoint = FlowCalculations.MoveFlowPoint(closestNormal, closestPoint, MOVE_DISTANCE);
            // Need to snap back to the surface (the vector may be pointing off the edge)
            return FLOW_SURFACE.ClosestPoint(nextFlowPoint);
        }
    }
}