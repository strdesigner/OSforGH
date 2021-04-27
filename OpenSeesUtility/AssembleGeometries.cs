using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace AssembleGeometries
{
    public class AssembleGeometries : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public AssembleGeometries()
          : base("Assemble IJ and IJKL", "AssembleGeometries",
              "AssembleGeometries(Line and Shell indexes)",
              "OpenSees", "Utility")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("lines", "BEAM", "Line of elements", GH_ParamAccess.list);
            pManager.AddNumberParameter("Material No. List", "mat", "[...](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddNumberParameter("Section No. List", "sec", "[...](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddNumberParameter("Element Angle List", "angle", "[...](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddSurfaceParameter("shells", "SHELL", "Plate of elements", GH_ParamAccess.list);
            pManager.AddNumberParameter("Material No. List(shell)", "mat(shell)", "[...](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddNumberParameter("thickness List", "thick", "[...](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddLineParameter("springs", "slines", "Line of spring elements", GH_ParamAccess.list);
            pManager.AddNumberParameter("E", "E", "[[kx+,kx-,ky+,ky-,kz+,kz-,kmx,kmy,kmz],...(DataTree)]", GH_ParamAccess.tree, -9999);///
            pManager.AddLineParameter("dampers", "dlines", "Line of viscous damper elements", GH_ParamAccess.list);
            pManager.AddNumberParameter("KCa", "KCa", "[[Kd[kN/m], ad, Cd[kN/(m/sec)^(1/ad)],...(DataTree)]", GH_ParamAccess.tree, -9999);///
            pManager[0].Optional = true; pManager[4].Optional = true; pManager[7].Optional = true; pManager[9].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("element_node_relationship(shell)", "IJKL", "[[No.i,No.j,No.k,No.l(if triangle:-1),material No.,thickness],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("element_node_relationship(spring)", "spring", "[[No.i,No.j,kx+,kx-,ky+,ky-,kz+,kz-,kmx,kmy,kmz],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("damper", "damper", "[[No.i, No.j, Kd[kN/m], ad, Cd[kN/(m/sec)^(1/ad)]],...](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Line> lines = new List<Line>(); List<Surface> shells = new List<Surface>(); List<Line> slines = new List<Line>(); List<Line> dlines = new List<Line>();
            if (!DA.GetDataList("lines", lines)) { };
            if (!DA.GetDataList("shells", shells)) { };
            if (!DA.GetDataList("springs", slines)) { };
            if (!DA.GetDataList("dampers", dlines)) { };
            List<double> mat = new List<double>(); DA.GetDataList("Material No. List", mat);
            List<double> sec = new List<double>(); DA.GetDataList("Section No. List", sec);
            List<double> angle = new List<double>(); DA.GetDataList("Element Angle List", angle);
            List<double> mat2 = new List<double>(); DA.GetDataList("Material No. List(shell)", mat2);
            List<double> t = new List<double>(); DA.GetDataList("thickness List", t);
            DA.GetDataTree("E", out GH_Structure<GH_Number> _E); var E = _E.Branches;
            DA.GetDataTree("KCa", out GH_Structure<GH_Number> _KCa); var KCa = _KCa.Branches;
            var r = new GH_Structure<GH_Number>(); var ij = new GH_Structure<GH_Number>(); var ijkl = new GH_Structure<GH_Number>(); var spring = new GH_Structure<GH_Number>(); var damper = new GH_Structure<GH_Number>();
            List<Point3d> xyz = new List<Point3d>();
            for (int e = 0; e < lines.Count; e++)
            {
                var r1 = lines[e].From; var r2 = lines[e].To;
                if(xyz.Exists(num => (num - r1).Length < 5e-3) == false) { xyz.Add(r1); }
                if (xyz.Exists(num => (num - r2).Length < 5e-3) == false) { xyz.Add(r2); }
            }
            for (int e = 0; e < shells.Count; e++)
            {
                var shell = shells[e].ToNurbsSurface();
                var p = shell.Points;
                Point3d r1; p.GetPoint(0, 0, out r1); Point3d r2; p.GetPoint(1, 0, out r2); Point3d r3; p.GetPoint(1, 1, out r3); Point3d r4; p.GetPoint(0, 1, out r4);
                if (xyz.Exists(num => (num - r1).Length < 5e-3) == false) { xyz.Add(r1); }
                if (xyz.Exists(num => (num - r2).Length < 5e-3) == false) { xyz.Add(r2); }
                if (xyz.Exists(num => (num - r3).Length < 5e-3) == false) { xyz.Add(r3); }
                if (xyz.Exists(num => (num - r4).Length < 5e-3) == false) { xyz.Add(r4); }
            }
            for (int e = 0; e < slines.Count; e++)
            {
                var r1 = slines[e].From; var r2 = slines[e].To;
                if (xyz.Exists(num => (num - r1).Length < 5e-3) == false) { xyz.Add(r1); }
                if (xyz.Exists(num => (num - r2).Length < 5e-3) == false) { xyz.Add(r2); }
            }
            for (int e = 0; e < dlines.Count; e++)
            {
                var r1 = dlines[e].From; var r2 = dlines[e].To;
                if (xyz.Exists(num => (num - r1).Length < 5e-3) == false) { xyz.Add(r1); }
                if (xyz.Exists(num => (num - r2).Length < 5e-3) == false) { xyz.Add(r2); }
            }
            for (int i = 0; i < xyz.Count; i++)
            {
                List<GH_Number> rlist = new List<GH_Number>();
                rlist.Add(new GH_Number(xyz[i].X)); rlist.Add(new GH_Number(xyz[i].Y)); rlist.Add(new GH_Number(xyz[i].Z));
                r.AppendRange(rlist, new GH_Path(i));
            }
            for (int e = 0; e < lines.Count; e++)
            {
                var r1 = lines[e].From; var r2 = lines[e].To; int i = 0; int j = 0;
                for (i = 0; i < xyz.Count; i++) { if ((xyz[i] - r1).Length <= 5e-3) { break; }; }
                for (j = 0; j < xyz.Count; j++) { if ((xyz[j] - r2).Length <= 5e-3) { break; }; }
                List<GH_Number> ijlist = new List<GH_Number>();
                ijlist.Add(new GH_Number(i)); ijlist.Add(new GH_Number(j));
                if (mat[0] == -9999) { ijlist.Add(new GH_Number(0)); }
                else { ijlist.Add(new GH_Number(mat[e])); }
                if (sec[0] == -9999) { ijlist.Add(new GH_Number(0)); }
                else { ijlist.Add(new GH_Number(sec[e])); }
                if (angle[0] == -9999) { ijlist.Add(new GH_Number(0)); }
                else { ijlist.Add(new GH_Number(angle[e])); }
                ij.AppendRange(ijlist, new GH_Path(e));
            }
            for (int e = 0; e < shells.Count; e++)
            {
                int i = 0; int j = 0; int k = 0; int l = -1;
                var shell = shells[e].ToNurbsSurface();
                var p = shell.Points;
                Point3d r1; p.GetPoint(0, 0, out r1); Point3d r2; p.GetPoint(1, 0, out r2);
                Point3d r3; p.GetPoint(1, 1, out r3); Point3d r4; p.GetPoint(0, 1, out r4);
                for (i = 0; i < xyz.Count; i++) { if ((xyz[i] - r1).Length <= 5e-3) { break; }; }
                for (j = 0; j < xyz.Count; j++) { if ((xyz[j] - r2).Length <= 5e-3) { break; }; }
                if (shells[e].IsSingular(0) == false && shells[e].IsSingular(1) == false && shells[e].IsSingular(2) == false && shells[e].IsSingular(3) == false)
                {
                    for (k = 0; k < xyz.Count; k++) { if ((xyz[k] - r3).Length <= 5e-3) { break; }; }
                    for (l = 0; l < xyz.Count; l++) { if ((xyz[l] - r4).Length <= 5e-3) { break; }; }
                }
                else
                {
                    for (int k1 = 0; k1 < xyz.Count; k1++)
                    {
                        if (((xyz[k1] - r4).Length <= 5e-3 && (r1 - r4).Length > 5e-3 && (r2 - r4).Length > 5e-3) || ((xyz[k1] - r3).Length <= 5e-3 && (r1 - r3).Length > 5e-3 && (r2 - r3).Length > 5e-3))
                        {
                            k = k1; break;
                        };
                    }
                }
                List<GH_Number> ijkllist = new List<GH_Number>();
                ijkllist.Add(new GH_Number(i)); ijkllist.Add(new GH_Number(j)); ijkllist.Add(new GH_Number(k)); ijkllist.Add(new GH_Number(l));
                if (mat2[0] == -9999) { ijkllist.Add(new GH_Number(0)); }
                else { ijkllist.Add(new GH_Number(mat2[e])); }
                if (t[0] == -9999) { ijkllist.Add(new GH_Number(0.15)); }
                else { ijkllist.Add(new GH_Number(t[e])); }
                ijkl.AppendRange(ijkllist, new GH_Path(e));
            }
            for (int e = 0; e < slines.Count; e++)
            {
                var r1 = slines[e].From; var r2 = slines[e].To; int i = 0; int j = 0;
                for (i = 0; i < xyz.Count; i++) { if ((xyz[i] - r1).Length <= 5e-3) { break; }; }
                for (j = 0; j < xyz.Count; j++) { if ((xyz[j] - r2).Length <= 5e-3) { break; }; }
                List<GH_Number> slist = new List<GH_Number>(); slist.Add(new GH_Number(i)); slist.Add(new GH_Number(j));
                if (E[0][0].Value == -9999) { slist.Add(new GH_Number(1000)); slist.Add(new GH_Number(1000)); slist.Add(new GH_Number(1000)); slist.Add(new GH_Number(1000)); slist.Add(new GH_Number(1000)); slist.Add(new GH_Number(1000)); slist.Add(new GH_Number(1000)); slist.Add(new GH_Number(1000)); slist.Add(new GH_Number(1000)); }
                else { slist.Add(new GH_Number(E[e][0])); slist.Add(new GH_Number(E[e][1])); slist.Add(new GH_Number(E[e][2])); slist.Add(new GH_Number(E[e][3])); slist.Add(new GH_Number(E[e][4])); slist.Add(new GH_Number(E[e][5])); slist.Add(new GH_Number(E[e][6])); slist.Add(new GH_Number(E[e][7])); slist.Add(new GH_Number(E[e][8])); slist.Add(new GH_Number(E[e][9]));
                if (E[e].Count == 11) { slist.Add(new GH_Number(E[e][10])); }
                }
                spring.AppendRange(slist, new GH_Path(e));
            }
            for (int e = 0; e < dlines.Count; e++)
            {
                var r1 = dlines[e].From; var r2 = dlines[e].To; int i = 0; int j = 0;
                for (i = 0; i < xyz.Count; i++) { if ((xyz[i] - r1).Length <= 5e-3) { break; }; }
                for (j = 0; j < xyz.Count; j++) { if ((xyz[j] - r2).Length <= 5e-3) { break; }; }
                List<GH_Number> dlist = new List<GH_Number>(); dlist.Add(new GH_Number(i)); dlist.Add(new GH_Number(j));
                if (KCa[0][0].Value == -9999) { dlist.Add(new GH_Number(25000)); dlist.Add(new GH_Number(450)); dlist.Add(new GH_Number(0.3));}
                else
                {
                    dlist.Add(new GH_Number(KCa[e][0])); dlist.Add(new GH_Number(KCa[e][1])); dlist.Add(new GH_Number(KCa[e][2]));
                }
                damper.AppendRange(dlist, new GH_Path(e));
            }
            DA.SetDataTree(0, r);
            if (lines.Count != 0) { DA.SetDataTree(1, ij); }
            if (shells.Count != 0){ DA.SetDataTree(2, ijkl); }
            if (slines.Count != 0) { DA.SetDataTree(3, spring); }
            if (dlines.Count != 0) { DA.SetDataTree(4, damper); }
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
                return OpenSeesUtility.Properties.Resources.assemblegeometry;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("394290a8-d815-4312-b230-3ecdb95ac2b9"); }
        }
    }
}