using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace PlateFolding
{
    public class PlateFolding : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent2 class.
        /// </summary>
        public PlateFolding()
          : base("CreateFoldingStructure", "CreateFolding",
              "Create folding structure model consisting of plates",
              "OpenSees", "Utility")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("number of points on width direction", "N", "1-unit is divided into N-1 on width direction", GH_ParamAccess.item, 6);///
            pManager.AddIntegerParameter("number of points on length direction", "M", "1-unit is divided into M-1 on length direction", GH_ParamAccess.item, 32);///
            pManager.AddNumberParameter("width of 1 unit", "width", "width of 1 unit(fold direction)", GH_ParamAccess.item, 1.5);///
            pManager.AddNumberParameter("length of 1 unit", "length", "length of 1 unit", GH_ParamAccess.item, 8.0);///
            pManager.AddNumberParameter("fold angle", "angle", "fold angle(not radian,0～180)", GH_ParamAccess.item, 120);///
            pManager.AddIntegerParameter("material number", "mat No.", "material number of the plate", GH_ParamAccess.item, 0);///
            pManager.AddNumberParameter("thickness", "thickness", "thickness of folding plate", GH_ParamAccess.item, 0.090);///
            pManager.AddNumberParameter("center point of the bottom", "center", "[x,y,z]", GH_ParamAccess.list, new double[] { 0, 0, 0 });///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            ///pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            ///pManager.AddNumberParameter("element_node_relationship(shell)", "IJKL", "[[No.i,No.j,No.k,No.l(if triangle:-1),material No.,thickness],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddPointParameter("nodes", "NOD", "Point3d of nodes", GH_ParamAccess.item);
            pManager.AddSurfaceParameter("shells", "SHELL", "Plate of elements", GH_ParamAccess.list);
            pManager.AddNumberParameter("material number", "mat No.", "material number of the plate", GH_ParamAccess.list);
            pManager.AddNumberParameter("thickness", "thickness", "thickness of folding plate", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var shell = new List<Surface>();
            var xyz = new List<Point3d>(); GH_Structure<GH_Number> r = new GH_Structure<GH_Number>(); GH_Structure<GH_Number> ijkl = new GH_Structure<GH_Number>();
            int N = 6; int M = 32; double X = 1.5; double Y = 8.0; double angle = 120.0; var cp = new List<double>(); double te = 0.090; int me = 0; List<double> thickness = new List<double>(); List<int> mat = new List<int>();
            DA.GetData("number of points on width direction", ref N); DA.GetData("number of points on length direction", ref M); DA.GetData("thickness", ref te); DA.GetData("material number", ref me);
            DA.GetData("width of 1 unit", ref X); DA.GetData("length of 1 unit", ref Y); DA.GetData("fold angle", ref angle); DA.GetDataList("center point of the bottom", cp);
            var dx = X / ((double)N - 1.0); var dy = Y / ((double)M - 1.0);var theta = (180.0 - angle) / 2.0 / 180.0 * Math.PI;
            for (int j = 0; j < M; j++)
            {
                for (int i = 0; i < N * 2 - 1; i++)
                {
                    var l = dx * i;
                    var x = l * Math.Cos(theta); var z = l * Math.Sin(theta); var y = dy * j;
                    if (i >= N)
                    {
                        z = X * Math.Sin(theta) - (l - X) * Math.Sin(theta);
                    }
                    xyz.Add(new Point3d(x - X * Math.Cos(theta) + cp[0], y - Y / 2.0 + cp[1], z + cp[2]));
                    var rlist = new List<GH_Number>(); rlist.Add(new GH_Number(x - X * Math.Cos(theta) + cp[0])); rlist.Add(new GH_Number(y - Y / 2.0 + cp[1])); rlist.Add(new GH_Number(z + cp[2]));
                    r.AppendRange(rlist, new GH_Path(i+j*(N*2-1)));
                    thickness.Add(te); mat.Add(me);
                }
            }
            DA.SetDataList("nodes", xyz);
            for (int j = 0; j < M - 1; j++)
            {
                for (int i = 0; i < N * 2 - 2; i++)
                {
                    int n1 = i + (N * 2 - 1) * j; int n2 = n1 + 1;int n3 = i + (N * 2 - 1) * (j + 1) + 1; int n4 = n3 - 1;
                    shell.Add(NurbsSurface.CreateFromCorners(xyz[n1], xyz[n2], xyz[n3], xyz[n4]));
                }
            }
            DA.SetDataList("shells", shell);
            DA.SetDataList("material number", mat);
            DA.SetDataList("thickness", thickness);
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
                return OpenSeesUtility.Properties.Resources.platefolding;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("d2fa16a1-5518-4f40-a786-f2860bfeccbd"); }
        }
    }
}