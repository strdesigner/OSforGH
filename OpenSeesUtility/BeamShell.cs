using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace BeamShell
{
    public class BeamShell : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent2 class.
        /// </summary>
        public BeamShell()
          : base("CreateBeamShell", "CreateBeamShell",
              "Create spherical shell model consisting of beams",
              "OpenSees", "Utility")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("number of points on X-axis direction", "N", "Lattice is divided into N-1 on X-axis direction", GH_ParamAccess.item, 7);///
            pManager.AddIntegerParameter("number of points on Y-axis direction", "M", "Lattice is divided into M-1 on Y-axis direction", GH_ParamAccess.item, 7);///
            pManager.AddNumberParameter("span X", "X", "X direction span of the shell", GH_ParamAccess.item, 20.0);///
            pManager.AddNumberParameter("span Y", "Y", "Y direction span of the shell", GH_ParamAccess.item, 20.0);///
            pManager.AddNumberParameter("height h", "h", "height of the shell", GH_ParamAccess.item, 6);///
            pManager.AddNumberParameter("center point of the shell bottom", "center", "[x,y,z]", GH_ParamAccess.list, new double[] { 0, 0, 0 });///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("nodes", "NOD", "Point3d of nodes", GH_ParamAccess.item);
            pManager.AddLineParameter("lines", "BEAM", "Line of elements", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var xyz = new List<Point3d>(); var lines = new List<Line>();
            int N = 7; int M = 7; double X = 20.0; double Y = 20.0; double h = 6.0; var cp = new List<double>();
            DA.GetData("number of points on X-axis direction", ref N); DA.GetData("number of points on Y-axis direction", ref M);
            DA.GetData("span X", ref X); DA.GetData("span Y", ref Y); DA.GetData("height h", ref h);
            DA.GetDataList("center point of the shell bottom", cp);
            var xx = X / (double)(N - 1); var yy = Y / (double)(M - 1);
            var l2 = Math.Sqrt(Math.Pow(X / 2.0, 2) + Math.Pow(Y / 2.0, 2));
            for(int i = 0; i < N; i++)
            {
                for(int j = 0; j < M; j++)
                {
                    var r = (Math.Pow(l2, 2) + Math.Pow(h, 2)) / (2 * h); var x = i * xx; var y = j * yy;
                    var l = Math.Sqrt(Math.Pow(x - X / 2.0, 2) + Math.Pow(y - Y / 2.0, 2));
                    var z = 0.0;
                    if (h != 0) { z = -(r - h) + Math.Sqrt(Math.Pow(r - h, 2) - (Math.Pow(l, 2) - Math.Pow(l2, 2))); }
                    xyz.Add(new Point3d(x-X/2.0+cp[0], y-Y/2.0 + cp[1], z + cp[2]));
                }
            }
            DA.SetDataList("nodes", xyz);
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M-1; j++)
                {
                    var n1 = j + i * M; var n2 = j + i * M + 1;
                    var r1 = xyz[n1]; var r2 = xyz[n2];
                    lines.Add(new Line(r1, r2));
                }
            }
            for (int i = 0; i < N-1; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    var n1 = j + i * M; var n2 = j + (i + 1) * M;
                    var r1 = xyz[n1]; var r2 = xyz[n2];
                    lines.Add(new Line(r1, r2));
                }
            }
            DA.SetDataList("lines", lines);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return OpenSeesUtility.Properties.Resources.beamshell;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("77dc2988-16f6-4c43-a9fc-a0ad90e14d7e"); }
        }
    }
}