using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace LineLoads
{
    public class LineLoads : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public LineLoads()
          : base("SetLineLoads", "LineLoad",
              "Set line load for OpenSees",
              "OpenSees", "PreProcess")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("x coordinates of element center point", "x", "element center point x", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("y coordinates of element center point", "y", "element center point y", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("z coordinates of element center point", "z", "element center point z", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("distributed load of x axis direction", "Wx", "double", GH_ParamAccess.item, 0.0);///
            pManager.AddNumberParameter("distributed load of y axis direction", "Wy", "double", GH_ParamAccess.item, 0.0);///
            pManager.AddNumberParameter("distributed load of z axis direction", "Wz", "double", GH_ParamAccess.item, -1.0);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("line load vector(uniform distributed load)", "e_load", "[[element No.,Wx,Wy,Wz],...](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Number> e_load = new GH_Structure<GH_Number>();
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r);
            DA.GetDataTree("element_node_relationship", out GH_Structure<GH_Number> _ij);
            if (_r.Branches[0][0].Value != -9999 && _ij.Branches[0][0].Value != -9999) 
            {
                var r = _r.Branches; var ij = _ij.Branches;
                var x = new List<double>(); DA.GetDataList("x coordinates of element center point", x); var y = new List<double>(); DA.GetDataList("y coordinates of element center point", y); var z = new List<double>(); DA.GetDataList("z coordinates of element center point", z);
                var wx = 0.0; DA.GetData("distributed load of x axis direction", ref wx); var wy = 0.0; DA.GetData("distributed load of y axis direction", ref wy); var wz = 0.0; DA.GetData("distributed load of z axis direction", ref wz); int kk = 0;
                for (int e = 0; e < ij.Count; e++)
                {
                    var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                    var xi = r[ni][0].Value; var yi = r[ni][1].Value; var zi = r[ni][2].Value;
                    var xj = r[nj][0].Value; var yj = r[nj][1].Value; var zj = r[nj][2].Value;
                    var xc = (xi + xj) / 2.0; var yc = (yi + yj) / 2.0; var zc = (zi + zj) / 2.0;
                    int k = 0;
                    for (int j = 0; j < x.Count; j++)
                    {
                        if (Math.Abs(x[j] - xc) < 5e-3 || x[j] == -9999) { k += 1; break; }
                    }
                    for (int j = 0; j < y.Count; j++)
                    {
                        if (Math.Abs(y[j] - yc) < 5e-3 || y[j] == -9999) { k += 1; break; }
                    }
                    for (int j = 0; j < z.Count; j++)
                    {
                        if (Math.Abs(z[j] - zc) < 5e-3 || z[j] == -9999) { k += 1; break; }
                    }
                    if (k == 3)
                    {
                        List<GH_Number> elist = new List<GH_Number>();
                        elist.Add(new GH_Number(e)); elist.Add(new GH_Number(wx)); elist.Add(new GH_Number(wy)); elist.Add(new GH_Number(wz));
                        e_load.AppendRange(elist, new GH_Path(kk)); kk += 1;
                    }
                }
            }
            DA.SetDataTree(0, e_load);
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
                return OpenSeesUtility.Properties.Resources.lineload;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("46d22ceb-5ded-4447-a300-54b7a0d06da0"); }
        }
    }
}