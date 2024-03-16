using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System.Drawing;
using System.Windows.Forms;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
///****************************************

namespace OpenSeesUtility
{
    public class TriBezierMetric : GH_Component
    {
        public TriBezierMetric()
          : base("TriBezierMetric", "TriBezierMetric",
              "Calc Gaussian curvature",
              "OpenSees", "Utility")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("P", "P", "[[[x00,y00,z00],...,[x0n,y0n,z0n]],[[x10,y10,z10],...,[x1n-1,y1n-1,z1n-1]],...,[[xn0,yn0,zn0]](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("uv", "uv", "parameter [u,v] at metric points", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("G", "G", "Gaussian curvature",GH_ParamAccess.list);
            pManager.AddNumberParameter("sum G^2", "sum G^2", "Gaussian curvature norm", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetDataTree("P", out GH_Structure<GH_Point> _q); var q = _q.Branches;
            var U = new List<double>(); var V = new List<double>();
            DA.GetDataTree("uv", out GH_Structure<GH_Number> _uv); var uv = _uv.Branches;
            double ubern(double u, double v, int n, int i, int j)
            {
                var bern = 0.0;
                var nn = 1.0; var ii = 1.0; var jj = 1.0; var kk = 1.0;
                var k = n - i - j; var w = 1 - u - v;
                for (int e = 1; e <= n; e++) { nn *= e; }
                for (int e = 1; e <= i; e++) { ii *= e; }
                for (int e = 1; e <= j; e++) { jj *= e; }
                for (int e = 1; e <= k; e++) { kk *= e; }
                bern = nn / ii / jj / kk * Math.Pow(u, i - 1) * Math.Pow(v, j) * Math.Pow(w, k - 1) * (i * w - k * u);
                return bern;
            }
            double vbern(double u, double v, int n, int i, int j)
            {
                var bern = 0.0;
                var nn = 1.0; var ii = 1.0; var jj = 1.0; var kk = 1.0;
                var k = n - i - j; var w = 1 - u - v;
                for (int e = 1; e <= n; e++) { nn *= e; }
                for (int e = 1; e <= i; e++) { ii *= e; }
                for (int e = 1; e <= j; e++) { jj *= e; }
                for (int e = 1; e <= k; e++) { kk *= e; }
                bern = nn / ii / jj / kk * Math.Pow(u, i) * Math.Pow(v, j - 1) * Math.Pow(w, k - 1) * (j * w - k * v);
                return bern;
            }
            double uubern(double u, double v, int n, int i, int j)
            {
                var bern = 0.0;
                var nn = 1.0; var ii = 1.0; var jj = 1.0; var kk = 1.0;
                var k = n - i - j; var w = 1 - u - v;
                for (int e = 1; e <= n; e++) { nn *= e; }
                for (int e = 1; e <= i; e++) { ii *= e; }
                for (int e = 1; e <= j; e++) { jj *= e; }
                for (int e = 1; e <= k; e++) { kk *= e; }
                bern = nn / ii / jj / kk * Math.Pow(u, i - 2) * Math.Pow(v, j) * Math.Pow(w, k - 2) * ((i - 1) * i * Math.Pow(w, 2) - 2 * i * k * u * w + (k - 1) * k * Math.Pow(u, 2));
                return bern;
            }
            double vvbern(double u, double v, int n, int i, int j)
            {
                var bern = 0.0;
                var nn = 1.0; var ii = 1.0; var jj = 1.0; var kk = 1.0;
                var k = n - i - j; var w = 1 - u - v;
                for (int e = 1; e <= n; e++) { nn *= e; }
                for (int e = 1; e <= i; e++) { ii *= e; }
                for (int e = 1; e <= j; e++) { jj *= e; }
                for (int e = 1; e <= k; e++) { kk *= e; }
                bern = nn / ii / jj / kk * Math.Pow(u, i) * Math.Pow(v, j - 2) * Math.Pow(w, k - 2) * ((j - 1) * j * Math.Pow(w, 2) - 2 * j * k * v * w + (k - 1) * k * Math.Pow(v, 2));
                return bern;
            }
            double uvbern(double u, double v, int n, int i, int j)
            {
                var bern = 0.0;
                var nn = 1.0; var ii = 1.0; var jj = 1.0; var kk = 1.0;
                for (int e = 1; e <= n; e++) { nn *= e; }
                for (int e = 1; e <= i; e++) { ii *= e; }
                for (int e = 1; e <= j; e++) { jj *= e; }
                for (int e = 1; e <= (n - i - j); e++) { kk *= e; }
                var k = n - i - j; var w = 1 - u - v;
                bern = nn / ii / jj / kk * Math.Pow(u, i - 1) * Math.Pow(v, j - 1) * Math.Pow(w, k - 2) * (i * j * Math.Pow(w, 2) - i * k * v * w - j * k * u * w + (k - 1) * k * u * v);
                return bern;
            }
            double[] EGFLMN(double u, double v)
            {
                var x = 0.0; var y = 0.0; var z = 0.0;
                for (int i = 0; i < q.Count; i++)
                {
                    for (int j = 0; j < q.Count - i; j++)
                    {
                        var qx = q[i][j].Value[0]; var qy = q[i][j].Value[1]; var qz = q[i][j].Value[2];
                        var bern = ubern(u, v, q.Count - 1, i, j);
                        x += qx * bern; y += qy * bern; z += qz * bern;
                    }
                }
                var uR = new Vector3d(x, y, z);
                x = 0.0; y = 0.0; z = 0.0;
                for (int i = 0; i < q.Count; i++)
                {
                    for (int j = 0; j < q.Count - i; j++)
                    {
                        var qx = q[i][j].Value[0]; var qy = q[i][j].Value[1]; var qz = q[i][j].Value[2];
                        var bern = vbern(u, v, q.Count - 1, i, j);
                        x += qx * bern; y += qy * bern; z += qz * bern;
                    }
                }
                var vR = new Vector3d(x, y, z);
                x = 0.0; y = 0.0; z = 0.0;
                for (int i = 0; i < q.Count; i++)
                {
                    for (int j = 0; j < q.Count - i; j++)
                    {
                        var qx = q[i][j].Value[0]; var qy = q[i][j].Value[1]; var qz = q[i][j].Value[2];
                        var bern = uubern(u, v, q.Count - 1, i, j);
                        x += qx * bern; y += qy * bern; z += qz * bern;
                    }
                }
                var uuR = new Vector3d(x, y, z);
                x = 0.0; y = 0.0; z = 0.0;
                for (int i = 0; i < q.Count; i++)
                {
                    for (int j = 0; j < q.Count - i; j++)
                    {
                        var qx = q[i][j].Value[0]; var qy = q[i][j].Value[1]; var qz = q[i][j].Value[2];
                        var bern = uvbern(u, v, q.Count - 1, i, j);
                        x += qx * bern; y += qy * bern; z += qz * bern;
                    }
                }
                var uvR = new Vector3d(x, y, z);
                x = 0.0; y = 0.0; z = 0.0;
                for (int i = 0; i < q.Count; i++)
                {
                    for (int j = 0; j < q.Count - i; j++)
                    {
                        var qx = q[i][j].Value[0]; var qy = q[i][j].Value[1]; var qz = q[i][j].Value[2];
                        var bern = vvbern(u, v, q.Count - 1, i, j);
                        x += qx * bern; y += qy * bern; z += qz * bern;
                    }
                }
                var vvR = new Vector3d(x, y, z);
                var _E = Vector3d.Multiply(uR, uR); var _F = Vector3d.Multiply(uR, vR); var _G = Vector3d.Multiply(vR, vR);
                var e = Vector3d.CrossProduct(uR, vR) / Vector3d.CrossProduct(uR, vR).Length;
                var _L = Vector3d.Multiply(uuR,e); var _M = Vector3d.Multiply(uvR, e); var _N = Vector3d.Multiply(vvR, e);
                var metrics = new double[6] { _E, _F, _G, _L, _M, _N };
                return metrics;
            }
            var gaussian_curvature = new List<double>(); var gaussian_norm = 0.0;
            for (int i = 0; i < uv.Count; i++)
            {
                var egflmn = EGFLMN(uv[i][0].Value, uv[i][1].Value);
                var E = egflmn[0]; var F = egflmn[1]; var G = egflmn[2]; var L = egflmn[3]; var M = egflmn[4]; var N = egflmn[5];
                var g = (L * N - Math.Pow(M, 2)) / (E * G - Math.Pow(F, 2));
                gaussian_curvature.Add(g); gaussian_norm += Math.Pow(g,2);
            }
            DA.SetDataList("G", gaussian_curvature);
            DA.SetData("sum G^2", gaussian_norm);
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
                return OpenSeesUtility.Properties.Resources.metric;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6eaa8817-2f47-4b56-9d72-8280ac9a5110"); }
        }
    }
}