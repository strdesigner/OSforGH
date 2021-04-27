using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;

namespace SurfLoads
{
    public class SurfLoads : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public SurfLoads()
          : base("SetSurfLoads(old)", "SurfLoad",
              "Set surface or floor load for OpenSees",
              "OpenSees", "PreProcess")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddBrepParameter("shell", "shell", "plate of elements", GH_ParamAccess.list);
            pManager.AddNumberParameter("Sz", "Sz", "[[No.,Sz],...](Datatree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("element_node_relationship(shell)", "IJKL", "[[No.i,No.j,No.k,No.l(if triangle:-1),material No.,thickness],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("x coordinates of element center point", "x", "element center point x", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("y coordinates of element center point", "y", "element center point y", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("z coordinates of element center point", "z", "element center point z", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("surface or floor load of z axis direction", "Sz", "double", GH_ParamAccess.item, -1.0);///
            pManager[1].Optional = true; pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("surface or floor load vector", "sf_load", "[[I,J,K,L,Wz],...](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Number> s_load = new GH_Structure<GH_Number>(); List<Brep> shells = new List<Brep>();
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r);
            DA.GetDataTree("element_node_relationship(shell)", out GH_Structure<GH_Number> _ijkl);
            var x = new List<double>(); DA.GetDataList("x coordinates of element center point", x); var y = new List<double>(); DA.GetDataList("y coordinates of element center point", y); var z = new List<double>(); DA.GetDataList("z coordinates of element center point", z);
            var sz = 0.0; DA.GetData("surface or floor load of z axis direction", ref sz);
            if (_r.Branches[0][0].Value != -9999 && _ijkl.Branches[0][0].Value != -9999)
            {
                var r = _r.Branches; var ijkl = _ijkl.Branches; int k = 0;
                for (int e = 0; e < ijkl.Count; e++)
                {
                    var ni = (int)ijkl[e][0].Value; var nj = (int)ijkl[e][1].Value; var nk = (int)ijkl[e][2].Value; var nl = (int)ijkl[e][3].Value;
                    var xi = r[ni][0].Value; var yi = r[ni][1].Value; var zi = r[ni][2].Value;
                    var xj = r[nj][0].Value; var yj = r[nj][1].Value; var zj = r[nj][2].Value;
                    var xk = r[nk][0].Value; var yk = r[nk][1].Value; var zk = r[nk][2].Value;
                    var xc = 0.0; var yc = 0.0; var zc = 0.0;
                    if (nl < 0) { xc = (xi + xj + xk) / 3.0; yc = (yi + yj + yk) / 3.0; zc = (zi + zj + zk) / 3.0; }
                    else
                    {
                        var xl = r[nl][0].Value; var yl = r[nl][1].Value; var zl = r[nl][2].Value;
                        xc = (xi + xj + xk + xl) / 4.0; yc = (yi + yj + yk + yl) / 4.0; zc = (zi + zj + zk + zl) / 4.0;
                    }
                    if (x.Exists(num => Math.Abs(num - xc) < 5e-3) == true || x[0] == -9999) //if (Math.Abs(xc - x[Math.Min(j, y.Count - 1)]) < 1e-8 || x[0] == -9999)
                        {
                        if (y.Exists(num => Math.Abs(num - yc) < 5e-3) == true || y[0] == -9999) //if (Math.Abs(yc - y[Math.Min(j, y.Count - 1)]) < 1e-8 || y[0] == -9999)
                        {
                            if (z.Exists(num => Math.Abs(num - zc) < 5e-3) == true || z[0] == -9999) //if (Math.Abs(zc - z[Math.Min(j, z.Count - 1)]) < 1e-8 || z[0] == -9999)
                            {
                                List<GH_Number> elist = new List<GH_Number>();
                                elist.Add(new GH_Number(ni)); elist.Add(new GH_Number(nj)); elist.Add(new GH_Number(nk)); elist.Add(new GH_Number(nl)); elist.Add(new GH_Number(sz));
                                s_load.AppendRange(elist, new GH_Path(k));
                                k += 1;
                            }
                        }
                    }
                }
            }
            else if(_r.Branches[0][0].Value != -9999 && DA.GetDataList("shell", shells) && DA.GetDataTree("Sz", out GH_Structure<GH_Number> _Sz))
            {
                var Sz = _Sz.Branches;
                var r = _r.Branches; var xyz = new List<Point3d>(); for (int i = 0; i < r.Count; i++) { xyz.Add(new Point3d(r[i][0].Value, r[i][1].Value, r[i][2].Value)); }
                int e2 = 0; var xc = 0.0; var yc = 0.0; var zc = 0.0;
                if (Sz[0][0].Value != -9999)
                {
                    for (int a = 0; a < Sz.Count; a++)
                    {
                        int e = (int)Sz[a][0].Value;
                        var shell = shells[e]; var pts = shell.Vertices;
                        for (int i = 0; i < pts.Count; i++)
                        {
                            var ri = pts[i].Location;
                            xc += ri[0]; yc += ri[1]; zc += ri[2];
                        }
                        xc = xc / pts.Count; yc = yc / pts.Count; zc = zc / pts.Count;
                        if (x.Exists(num => Math.Abs(num - xc) < 5e-3) == true || x[0] == -9999)
                        {
                            if (y.Exists(num => Math.Abs(num - yc) < 5e-3) == true || y[0] == -9999)
                            {
                                if (z.Exists(num => Math.Abs(num - zc) < 5e-3) == true || z[0] == -9999)
                                {
                                    var r1 = pts[0].Location; var r2 = pts[1].Location; var r3 = pts[2].Location; var r4 = new Point3d(-9999, -9999, -9999);
                                    if (pts.Count == 4) { r4 = pts[3].Location; }//頂点の座標取得
                                    int n1 = 0; int n2 = 0; int n3 = 0; int n4 = -1; int k = 0;
                                    for (int i = 0; i < r.Count; i++)
                                    {
                                        if ((xyz[i] - r1).Length < 5e-3) { n1 = i; }
                                        else if ((xyz[i] - r2).Length < 5e-3) { n2 = i; k += 1; }
                                        else if ((xyz[i] - r3).Length < 5e-3) { n3 = i; k += 1; }
                                        else if ((xyz[i] - r4).Length < 5e-3) { n4 = i; k += 1; }
                                        if (k == pts.Count) { break; }
                                    }
                                    List<GH_Number> elist = new List<GH_Number>();
                                    elist.Add(new GH_Number(n1)); elist.Add(new GH_Number(n2)); elist.Add(new GH_Number(n3)); elist.Add(new GH_Number(n4)); elist.Add(new GH_Number(Sz[a][1].Value));
                                    s_load.AppendRange(elist, new GH_Path(e2));
                                    e2 += 1;
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (int e = 0; e < shells.Count; e++)
                    {
                        var shell = shells[e]; var pts = shell.Vertices;
                        xc = 0.0; yc = 0.0; zc = 0.0;
                        for (int i = 0; i < pts.Count; i++)
                        {
                            var ri = pts[i].Location;
                            xc += ri[0]; yc += ri[1]; zc += ri[2];
                        }
                        xc = xc / pts.Count; yc = yc / pts.Count; zc = zc / pts.Count;
                        if (x.Exists(num => Math.Abs(num - xc) < 5e-3) == true || x[0] == -9999)
                        {
                            if (y.Exists(num => Math.Abs(num - yc) < 5e-3) == true || y[0] == -9999)
                            {
                                if (z.Exists(num => Math.Abs(num - zc) < 5e-3) == true || z[0] == -9999)
                                {
                                    var r1 = pts[0].Location; var r2 = pts[1].Location; var r3 = pts[2].Location; var r4 = new Point3d(-9999, -9999, -9999);
                                    if (pts.Count == 4) { r4 = pts[3].Location; }//頂点の座標取得
                                    int n1 = 0; int n2 = 0; int n3 = 0; int n4 = -1; int k = 0;
                                    for (int i = 0; i < r.Count; i++)
                                    {
                                        if ((xyz[i] - r1).Length < 5e-3) { n1 = i; }
                                        else if ((xyz[i] - r2).Length < 5e-3) { n2 = i; k += 1; }
                                        else if ((xyz[i] - r3).Length < 5e-3) { n3 = i; k += 1; }
                                        else if ((xyz[i] - r4).Length < 5e-3) { n4 = i; k += 1; }
                                        if (k == pts.Count) { break; }
                                    }
                                    List<GH_Number> elist = new List<GH_Number>();
                                    elist.Add(new GH_Number(n1)); elist.Add(new GH_Number(n2)); elist.Add(new GH_Number(n3)); elist.Add(new GH_Number(n4)); elist.Add(new GH_Number(sz));
                                    s_load.AppendRange(elist, new GH_Path(e2));
                                    e2 += 1;
                                }
                            }
                        }
                    }
                }
            }
            DA.SetDataTree(0, s_load);
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
                return OpenSeesUtility.Properties.Resources.surfaceload;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("22907cac-0ad9-4a60-91ce-3a0f2e280136"); }
        }
    }
}