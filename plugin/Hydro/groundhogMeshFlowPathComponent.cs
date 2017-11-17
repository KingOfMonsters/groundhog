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
    public class groundhogMeshFlowComponent : GH_Component
    {
        public groundhogMeshFlowComponent()
            : base("Flow Simulation (Mesh)", "Mesh Flows",
                "Construct flow paths along a mesh",
                "Groundhog", "Hydro")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override Bitmap Icon => Resources.icon_flows;

        public override Guid ComponentGuid => new Guid("{2d218bdc-ecaa-2cf7-815a-c8111d1798d3}");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Base landscape form (as mesh) for the flows", GH_ParamAccess.item);
            pManager.AddPointParameter("Points", "P", "Start points for the flow paths", GH_ParamAccess.list);
            pManager.AddNumberParameter("Fidelity", "F",
                "Amount to move for each flow iteration. Small numbers may take a long time to compute",
                GH_ParamAccess.item, 100.0);
            pManager.AddBooleanParameter("Thread", "T", "Whether to multithread the calculation", GH_ParamAccess.item,
                false);
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Flow Points", "F", "The points of each simulated point of movement",
                GH_ParamAccess.tree);
            pManager.AddCurveParameter("Flow Paths", "C", "A polyline linking each point", GH_ParamAccess.list);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Create holder variables for input parameters
            var FLOW_MESH = default(Mesh);
            var FLOW_ORIGINS = new List<Point3d>();
            var FLOW_FIDELITY = 1000.0; // Default Value
            var THREAD = false;

            // Access and extract data from the input parameters individually
            DA.GetData(0, ref FLOW_MESH);
            if (!DA.GetDataList(1, FLOW_ORIGINS)) return;
            if (!DA.GetData(2, ref FLOW_FIDELITY)) return;
            if (!DA.GetData(3, ref THREAD)) return;

            if (FLOW_FIDELITY == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Flow fidelity cannot be 0");
                return;
            }

            var startPoints = FLOW_ORIGINS.ToArray(); // Array for multithreading
            var allFlowPathPoints = new List<Point3d>[startPoints.Length]; // Array of all the paths
            var flowPoints = new List<Point3d>();

            if (THREAD)
                Parallel.For(0, startPoints.Length, i => // Shitty multithreading
                    {
                        allFlowPathPoints[i] = DispatchFlowPoints(FLOW_MESH, startPoints[i], FLOW_FIDELITY);
                    }
                );
            else
                for (var i = 0; i < startPoints.Length; i = i + 1)
                    allFlowPathPoints[i] = DispatchFlowPoints(FLOW_MESH, startPoints[i], FLOW_FIDELITY);

            var allFlowPathPointsTree = new DataTree<object>();
            var allFlowPathCurvesList = new List<Polyline>();


            for (var i = 0; i < allFlowPathPoints.Length; i++)
            {
                var path = new GH_Path(i);
                // For each flow path make the polyline
                if (allFlowPathPoints[i].Count > 1)
                {
                    var flowPath = new Polyline(allFlowPathPoints[i]);
                    allFlowPathCurvesList.Add(flowPath);
                }

                // And make a branch for the list of points
                for (var j = 0; j < allFlowPathPoints[i].Count; j++)
                    // For each flow path point
                    allFlowPathPointsTree.Add(allFlowPathPoints[i][j], path);
            }

            // Assign variables to output parameters
            DA.SetDataTree(0, allFlowPathPointsTree);
            DA.SetDataList(1, allFlowPathCurvesList);
        }

        private List<Point3d> DispatchFlowPoints(Mesh FLOW_MESH, Point3d initialStartPoint,
            double MOVE_DISTANCE)
        {
            var flowPoints = new List<Point3d>(); // Holds each step

            var startPoint = FLOW_MESH.ClosestPoint(initialStartPoint);
            flowPoints.Add(startPoint);

            while (true)
            {
                Point3d nextPoint;
                nextPoint = GetNextFlowStepOnMesh(FLOW_MESH, startPoint, MOVE_DISTANCE);

                if (nextPoint.DistanceTo(startPoint) <= RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                    break; // Test the point has actully moved
                if (nextPoint.Z >= startPoint.Z)
                    break; // Test this point is actually lower
                flowPoints.Add(nextPoint);
                startPoint = nextPoint; // Checks out; iterate on
            }

            return flowPoints;
        }

        private Point3d GetNextFlowStepOnMesh(Mesh FLOW_MESH, Point3d startPoint, double MOVE_DISTANCE)
        {
            double maximumDistance = 0; // TD: setting this as +ve speeds up the search?
            Vector3d closestNormal;
            Point3d closestPoint;

            // Get closest point
            FLOW_MESH.ClosestPoint(startPoint, out closestPoint, out closestNormal, maximumDistance);

            // Get the vector to flow down
            var flowVector = Vector3d.CrossProduct(Vector3d.ZAxis, closestNormal);
            flowVector.Unitize();
            flowVector.Reverse();
            flowVector.Transform(Transform.Rotation(Math.PI / 2, closestNormal, closestPoint));

            // Flow to the new point
            //Point3d nextFlowPoint = Point3d.Add(closestPoint, V * MOVE_DISTANCE);
            var nextFlowPoint = Point3d.Add(closestPoint, flowVector * MOVE_DISTANCE);

            // Need to snap back to the surface (the vector may be pointing off the edge)
            return FLOW_MESH.ClosestPoint(nextFlowPoint);
        }
    }
}