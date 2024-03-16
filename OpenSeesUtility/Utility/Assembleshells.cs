using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Assembleshells
{
    public class Assembleshells : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public Assembleshells()
          : base("Make R and IJKL from Geometry", "AssembleShells",
              "AssembleGeometries(Shells)",
              "OpenSees", "Utility")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("shells", "SHELL", "Plate of elements", GH_ParamAccess.list);
            pManager.AddNumberParameter("Material No. List", "mat", "[...](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddNumberParameter("thickness List", "thick", "[...](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("element_node_relationship(shell)", "IJKL", "[[No.i,No.j,No.k,No.l(if triangle:-1),material No.,thickness],...](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Surface> shells = new List<Surface>();
            DA.GetDataList("shells", shells);
            List<double> mat = new List<double>(); DA.GetDataList("Material No. List", mat);
            List<double> t = new List<double>(); DA.GetDataList("thickness List", t);
            var r = new GH_Structure<GH_Number>(); var ijkl = new GH_Structure<GH_Number>();
            List<Point3d> xyz = new List<Point3d>();
            for (int e = 0; e < shells.Count; e++)
            {
                var shell = shells[e].ToNurbsSurface();
                var p = shell.Points;
                Point3d r1; p.GetPoint(0, 0, out r1); Point3d r2; p.GetPoint(1, 0, out r2); Point3d r3; p.GetPoint(1, 1, out r3); Point3d r4; p.GetPoint(0, 1, out r4); var l1 = 10.0; var l2 = 10.0; var l3 = 10.0; var l4 = 10.0;
                for (int i = 0; i < xyz.Count; i++) { l1 = Math.Min(l1, (xyz[i] - r1).Length); }
                if (l1 > 5e-3) { xyz.Add(r1); }
                for (int i = 0; i < xyz.Count; i++) { l2 = Math.Min(l2, (xyz[i] - r2).Length); }
                if (l2 > 5e-3) { xyz.Add(r2); }
                //if (xyz.Exists(num => (num - r1).Length < 5e-3) == false) { xyz.Add(r1); }
                //if (xyz.Exists(num => (num - r2).Length < 5e-3) == false) { xyz.Add(r2); }
                if (shells[e].IsSingular(0)==false && shells[e].IsSingular(1) == false && shells[e].IsSingular(2) == false && shells[e].IsSingular(3) == false)
                {
                    for (int i = 0; i < xyz.Count; i++) { l3 = Math.Min(l3, (xyz[i] - r3).Length); }
                    if (l3 > 5e-3) { xyz.Add(r3); }
                    //if (xyz.Exists(num => (num - r3).Length < 5e-3) == false) { xyz.Add(r3); }
                }
                for (int i = 0; i < xyz.Count; i++) { l4 = Math.Min(l4, (xyz[i] - r4).Length); }
                if (l4 > 5e-3) { xyz.Add(r4); }
            }
            for (int i = 0; i < xyz.Count; i++)
            {
                List<GH_Number> rlist = new List<GH_Number>();
                rlist.Add(new GH_Number(xyz[i].X)); rlist.Add(new GH_Number(xyz[i].Y)); rlist.Add(new GH_Number(xyz[i].Z));
                r.AppendRange(rlist, new GH_Path(i));
            }
            for (int e = 0; e < shells.Count; e++)
            {
                int i = 0; int j = 0; int k = 0; int l = -1;
                var shell = shells[e].ToNurbsSurface();
                var p = shell.Points;
                Point3d r1; p.GetPoint(0, 0, out r1); i = xyz.FindIndex(num => (num - r1).Length < 5e-3);
                Point3d r2; p.GetPoint(1, 0, out r2); j = xyz.FindIndex(num => (num - r2).Length < 5e-3);
                Point3d r3; p.GetPoint(1, 1, out r3); Point3d r4; p.GetPoint(0, 1, out r4);
                if (shells[e].IsSingular(0) == false && shells[e].IsSingular(1) == false && shells[e].IsSingular(2) == false && shells[e].IsSingular(3) == false)
                {
                    k = xyz.FindIndex(num => (num - r3).Length < 5e-3);
                    l = xyz.FindIndex(num => (num - r4).Length < 5e-3);
                }
                else { k = xyz.FindIndex(num => (num - r4).Length < 5e-3); }
                List<GH_Number> ijkllist = new List<GH_Number>();
                ijkllist.Add(new GH_Number(i)); ijkllist.Add(new GH_Number(j)); ijkllist.Add(new GH_Number(k)); ijkllist.Add(new GH_Number(l));
                if (mat[0] == -9999) { ijkllist.Add(new GH_Number(0)); }
                else if (mat.Count == 1) { ijkllist.Add(new GH_Number((int)mat[0])); }
                else { ijkllist.Add(new GH_Number(mat[e])); }
                if (t[0] == -9999) { ijkllist.Add(new GH_Number(0)); }
                else if (t.Count == 1) { ijkllist.Add(new GH_Number(t[0])); }
                else { ijkllist.Add(new GH_Number(t[e])); }
                ijkl.AppendRange(ijkllist, new GH_Path(e));
            }
            DA.SetDataTree(0, r);
            DA.SetDataTree(1, ijkl);
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
                return OpenSeesUtility.Properties.Resources.assembleshells;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("a3296515-c63a-4a40-9ba8-fc8f858709e6"); }
        }
    }
}