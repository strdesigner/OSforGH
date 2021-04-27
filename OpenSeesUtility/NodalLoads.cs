using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace NodalLoads
{
    public class NodalLoads : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public NodalLoads()
          : base("SetNodalLoads", "NodalLoad",
              "Set nodal load for OpenSees",
              "OpenSees", "PreProcess")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("x coordinates", "x", "boundary point x", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("y coordinates", "y", "boundary point y", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("z coordinates", "z", "boundary point z", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("axial load of x axis direction", "Px", "double", GH_ParamAccess.item, 0.0);///
            pManager.AddNumberParameter("axial load of y axis direction", "Py", "double", GH_ParamAccess.item, 0.0);///
            pManager.AddNumberParameter("axial load of z axis direction", "Pz", "double", GH_ParamAccess.item, 0.0);///
            pManager.AddNumberParameter("moment load around x axis direction", "Mx", "double", GH_ParamAccess.item, 0.0);///
            pManager.AddNumberParameter("moment load around y axis direction", "My", "double", GH_ParamAccess.item, 0.0);///
            pManager.AddNumberParameter("moment load around z axis direction", "Mz", "double", GH_ParamAccess.item, 0.0);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal load vector", "p_load", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Number> p_load = new GH_Structure<GH_Number>();
            if (!DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r)) { }
            else if (_r.Branches[0][0].Value != -9999)
            {
                var r = _r.Branches; var n = r.Count;
                var x = new List<double>(); DA.GetDataList(1, x); var y = new List<double>(); DA.GetDataList(2, y); var z = new List<double>(); DA.GetDataList(3, z);
                var px = 0.0; DA.GetData("axial load of x axis direction", ref px); var py = 0.0; DA.GetData("axial load of y axis direction", ref py); var pz = 0.0; DA.GetData("axial load of z axis direction", ref pz); var mx = 0.0; DA.GetData("moment load around x axis direction", ref mx); var my = 0.0; DA.GetData("moment load around y axis direction", ref my); var mz = 0.0; DA.GetData("moment load around z axis direction", ref mz); int e = 0;
                for (int i = 0; i < n; i++)
                {
                    var xi = r[i][0].Value; var yi = r[i][1].Value; var zi = r[i][2].Value;
                    int k = 0;
                    for (int j = 0; j < x.Count; j++)
                    {
                        if (Math.Abs(x[j] - xi) < 5e-3 || x[j] == -9999) { k += 1; break; }
                    }
                    for (int j = 0; j < y.Count; j++)
                    {
                        if (Math.Abs(y[j] - yi) < 5e-3 || y[j] == -9999) { k += 1; break; }
                    }
                    for (int j = 0; j < z.Count; j++)
                    {
                        if (Math.Abs(z[j] - zi) < 5e-3 || z[j] == -9999) { k += 1; break; }
                    }
                    if (k == 3)
                    {
                        List<GH_Number> plist = new List<GH_Number>();
                        plist.Add(new GH_Number(i)); plist.Add(new GH_Number(px)); plist.Add(new GH_Number(py)); plist.Add(new GH_Number(pz)); plist.Add(new GH_Number(mx)); plist.Add(new GH_Number(my)); plist.Add(new GH_Number(mz));
                        p_load.AppendRange(plist, new GH_Path(e)); e += 1;
                    }
                }
            }
            DA.SetDataTree(0, p_load);
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
                return OpenSeesUtility.Properties.Resources.nodalload;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("42785d1e-cd9d-4650-81bc-7cceb1bcd71f"); }
        }
    }
}