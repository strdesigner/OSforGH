using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace OpenSeesUtility
{
    public class TriBezierSurface : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public TriBezierSurface()
          : base("Triangular Patch Bezier Surface", "TBS",
              "Create triangular patch bezier surface",
              "OpenSees", "Utility")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("P", "P", "[[[x00,y00,z00],...,[x0n,y0n,z0n]],[[x10,y10,z10],...,[x1n-1,y1n-1,z1n-1]],...,[[xn0,yn0,zn0]](DataTree)", GH_ParamAccess.tree);///
            pManager.AddIntegerParameter("UV", "UV", "number of making points on U & V direction", GH_ParamAccess.item, 10);///
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
            DA.GetDataTree("P", out GH_Structure<GH_Point> _q); var q = _q.Branches;
            var np = 10; DA.GetData("UV", ref np);
            var U = new List<List<double>>(); var V = new List<List<double>>();
            var div = 1.0 / (np - 1.0);
            for (int i = 0; i < np; i++)
            {
                var u = new List<double>(); var v = new List<double>();
                for (int j = 0; j < np - i; j++)
                {
                    u.Add(div * j); v.Add(div * i);
                }
                U.Add(u); V.Add(v);
            }
            double bernstein(double _u, double _v, int n, int i, int j)
            {
                var bern = 0.0;
                var nn = 1.0; var ii = 1.0; var jj = 1.0; var kk = 1.0;
                for (int e = 1; e <= n; e++) { nn *= e; }
                for (int e = 1; e <= i; e++) { ii *= e; }
                for (int e = 1; e <= j; e++) { jj *= e; }
                for (int e = 1; e <= (n-i-j); e++) { kk *= e; }
                bern = nn / ii / jj / kk * Math.Pow(_u, i) * Math.Pow(_v, j) * Math.Pow(1 - _u - _v, n - i - j);
                return bern;
            }
            GH_Structure<GH_Point> S = new GH_Structure<GH_Point>();
            for (int k = 0; k < np; k++)
            {
                List<GH_Point> slist = new List<GH_Point>();
                for (int l = 0; l < np - k; l++)
                {
                    var u = div * l; var v = div * k;
                    var x = 0.0; var y = 0.0; var z = 0.0;
                    for (int i = 0; i < q.Count; i++)
                    {
                        for (int j = 0; j < q.Count - i; j++)
                        {
                            var qx = q[i][j].Value[0]; var qy = q[i][j].Value[1]; var qz = q[i][j].Value[2];
                            var bern = bernstein(u, v, q.Count-1, i, j);
                            x += qx * bern; y += qy * bern; z += qz * bern;
                        }
                    }
                    slist.Add(new GH_Point(new Point3d(x,y,z)));
                }
                S.AppendRange(slist, new GH_Path(k));
            }
            DA.SetDataTree(0, S);
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
                return OpenSeesUtility.Properties.Resources.tribezier;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("86c4a5d1-5bf2-4b3a-a0cf-1b288d1243fc"); }
        }
    }
}