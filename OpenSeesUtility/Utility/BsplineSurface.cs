using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System.Drawing;
using System.Windows.Forms;

namespace OpenSeesUtility
{
    public class BsplineSurface : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public BsplineSurface()
          : base("Rational B-spline Surface", "RBS",
              "Create (Uniform) rational B-spline surface",
              "OpenSees", "Utility")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("P", "P", "[[[x00,y00,z00],...],[x10,y10,z10],...]](DataTree)", GH_ParamAccess.tree);///
            pManager.AddIntegerParameter("D", "D", "degree of surface", GH_ParamAccess.item,3);///
            pManager.AddIntegerParameter("U", "U", "number of making points on U direction", GH_ParamAccess.item,10);///
            pManager.AddIntegerParameter("V", "V", "number of making points on V direction", GH_ParamAccess.item,10);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("S", "S", "[[[x00,y00,z00],...],[x10,y10,z10],...]](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int D = 3; int divU = 10; int divV = 10;
            if (!DA.GetDataTree("P", out GH_Structure<GH_Point> _P)) { return; }
            var P = _P.Branches; int N = P.Count; int M = P[0].Count;
            DA.GetData("D", ref D); DA.GetData("U", ref divU); DA.GetData("V", ref divV);
            List<List<double>> cox_de_boor(int n, int d, double t, List<double> k)
            {
                var Ni = new List<List<double>>();
                for (int i = 0; i < d + 1; i++)
                {
                    var ni = new List<double>();
                    for (int j = 0; j < n + d + 1; j++)
                    {
                        ni.Add(0.0);
                    }
                    Ni.Add(ni);
                }
                for (int i = 0; i < n + d; i++)
                {
                    if(k[i]<=t && t < k[i + 1]) { Ni[0][i] = 1.0; break; }
                    if (Math.Abs(t - k[n + d])<1e-6) { Ni[0][n - 1] = 1.0; break; }
                }
                for(int i = 1; i < d + 1; i++)
                {
                    for (int j = 0; j < n + d - i; j++)
                    {
                        var a1 = k[i + j] - k[j]; var a2 = k[i + j + 1] - k[j + 1]; var b1 = 0.0; var b2 = 0.0;
                        if (Math.Abs(a1) > 1e-6) { b1 = (t - k[j]) / a1 * Ni[i - 1][j]; }
                        if (Math.Abs(a2) > 1e-6) { b2 = (k[i + j + 1] - t) / a2 * Ni[i - 1][j + 1]; }
                        Ni[i][j] = b1 + b2;
                    }
                }
                return Ni;
            }
            GH_Structure<GH_Point> bspline(int n, int m, IList<List<GH_Point>> cp, List<double> u, List<double> v, int du, int dv, List<double> ku, List<double> kv)
            {
                GH_Structure<GH_Point> S=new GH_Structure<GH_Point>();
                var XYZ = new List<List<List<double>>>();
                for (int k = 0; k < u.Count; k++)
                {
                    var xyz = new List<List<double>>();
                    for (int l = 0; l < v.Count; l++)
                    {
                        xyz.Add(new List<double> { 0.0, 0.0, 0.0 });
                    }
                    XYZ.Add(xyz);
                }
                for (int k = 0; k < u.Count; k++)
                {
                    var Ni = cox_de_boor(n, du, u[k], ku);
                    for (int l = 0; l < v.Count; l++)
                    {
                        var Nj = cox_de_boor(m, dv, v[l], kv);
                        for (int i = 0; i < ku.Count - du - 1; i++)
                        {
                            for (int j = 0; j < kv.Count - dv - 1; j++)
                            {
                                var r = cp[i][j].Value * Ni[du][i] * Nj[dv][j];
                                XYZ[k][l][0] += r[0]; XYZ[k][l][1] += r[1]; XYZ[k][l][2] += r[2];
                            }
                        }
                    }
                }
                for (int k = 0; k < u.Count; k++)
                {
                    List<GH_Point> slist = new List<GH_Point>();
                    for (int l = 0; l < v.Count; l++)
                    {
                        slist.Add(new GH_Point(new Point3d(XYZ[k][l][0], XYZ[k][l][1], XYZ[k][l][2])));
                    }
                    S.AppendRange(slist, new GH_Path(k));
                }
                return S;
            }
            var _u = new List<double>(); var _v = new List<double>(); var _ku = new List<double>(); var _kv = new List<double>();
            var divu = (N - D) / (double)(divU - 1); var divv = (M - D) / (double)(divV - 1);
            for (int i = 0; i < divU; i++) { _u.Add(divu * i); }
            for (int i = 0; i < divV; i++) { _v.Add(divv * i); }
            for (int i = 0; i < D; i++) { _ku.Add(0.0); _kv.Add(0.0); }
            for (int i = 0; i < N - D + 1; i++) { _ku.Add(i); }
            for (int i = 0; i < M - D + 1; i++) { _kv.Add(i); }
            for (int i = 0; i < D; i++) { _ku.Add(N - D); _kv.Add(M - D); }
            var s = bspline(N, M, P, _u, _v, D, D, _ku, _kv);
            DA.SetDataTree(0, s);
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
                return OpenSeesUtility.Properties.Resources.urbs;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("7c6bdf91-5c74-4a11-b704-d1661386aeab"); }
        }
    }
}