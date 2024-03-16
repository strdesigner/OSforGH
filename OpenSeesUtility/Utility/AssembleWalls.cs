using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace AssembleWalls
{
    public class AssembleWalls : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public AssembleWalls()
          : base("Assemble R and KABE_W", "AssembleWalls",
              "AssembleWalls(Make KABE_W from surfaces)",
              "OpenSees", "Utility")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddSurfaceParameter("Surf", "Surf", "Wall Surfaces", GH_ParamAccess.list);
            pManager.AddNumberParameter("bairitsu", "bairitsu", "[...](DataList:default=2.5)", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddNumberParameter("rad", "rad", "[...](DataList:120 or 150 default=120(yuka))", GH_ParamAccess.list, new List<double> { -9999 });///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("KABE_W", "KABE_W", "[[No.i,No.j,No.k,No.l,bairitsu,rad],...](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            IList<List<GH_Number>> r; var kabe_w = new GH_Structure<GH_Number>(); List<Surface> shells = new List<Surface>(); List<double> bairitsu = new List<double>(); List<double> rad = new List<double>();
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r); DA.GetDataList("Surf", shells); DA.GetDataList("bairitsu", bairitsu); DA.GetDataList("rad", rad);
            r = _r.Branches; var n = r.Count; var m = shells.Count; var xyz = new List<Point3d>();
            for (int i = 0; i < n; i++) { xyz.Add(new Point3d(r[i][0].Value, r[i][1].Value, r[i][2].Value)); }
            if (bairitsu[0] == -9999)
            {
                bairitsu = new List<double>();
                for (int e = 0; e < m; e++) { bairitsu.Add(2.5); }
            }
            if (rad[0] == -9999)
            {
                rad = new List<double>();
                for (int e = 0; e < m; e++) { rad.Add(120); }
            }
            int e2 = 0;
            for (int e = 0; e < shells.Count; e++)
            {
                var shell = shells[e].ToNurbsSurface();
                var p = shell.Points;
                Point3d r1; p.GetPoint(0, 0, out r1); Point3d r2; p.GetPoint(1, 0, out r2); Point3d r3; p.GetPoint(1, 1, out r3); Point3d r4; p.GetPoint(0, 1, out r4);
                int n1 = 0; int n2 = 0; int n3 = 0; int n4 = 0; int k = 0;
                for (int i = 0; i < n; i++)
                {
                    if ((xyz[i] - r1).Length < 5e-3) { n1 = i; k += 1; }
                    else if ((xyz[i] - r2).Length < 5e-3) { n2 = i; k += 1; }
                    else if ((xyz[i] - r3).Length < 5e-3) { n3 = i; k += 1; }
                    else if ((xyz[i] - r4).Length < 5e-3) { n4 = i; k += 1; }
                    if (k == 4) { break; }
                }
                if ((r4 - r1).Length > 1e-3 && (r1 - r2).Length > 1e-3 && (r2 - r3).Length > 1e-3 && (r3 - r4).Length > 1e-3)
                {
                    List<GH_Number> kabe = new List<GH_Number>();
                    kabe.Add(new GH_Number(n1)); kabe.Add(new GH_Number(n2)); kabe.Add(new GH_Number(n3)); kabe.Add(new GH_Number(n4)); kabe.Add(new GH_Number(bairitsu[e])); kabe.Add(new GH_Number(rad[e]));
                    kabe_w.AppendRange(kabe, new GH_Path(e2)); e2 += 1;
                }
                else
                {
                    List<GH_Number> kabe = new List<GH_Number>();
                    kabe.Add(new GH_Number(n1)); kabe.Add(new GH_Number(n2)); kabe.Add(new GH_Number(n3)); kabe.Add(new GH_Number(n3)); kabe.Add(new GH_Number(bairitsu[e])); kabe.Add(new GH_Number(rad[e]));
                    kabe_w.AppendRange(kabe, new GH_Path(e2)); e2 += 1;
                }
            }
            DA.SetDataTree(0, kabe_w);
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
                return OpenSeesUtility.Properties.Resources.kabew;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("ca336ea0-893e-4218-b87a-c3976e121085"); }
        }
    }
}