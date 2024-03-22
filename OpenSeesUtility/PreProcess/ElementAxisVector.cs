using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace ElementAxisVector
{
    public class ElementAxisVector : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ElementAxisVector()
          : base("Calc Element Axis Vector", "EleVec",
              "Calc Element Axis Vector from IJ and angle",
              "OpenSees", "PreProcess")
        {
        }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///0
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree);///1
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddVectorParameter("element axis vector", "l_vec", "element axis vector for each elements", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Vector3d rotation(Vector3d a, Vector3d b, double theta)
            {
                double rad = theta * Math.PI / 180;
                double s = Math.Sin(rad); double c = Math.Cos(rad);
                b /= Math.Sqrt(Vector3d.Multiply(b, b));
                double b1 = b[0]; double b2 = b[1]; double b3 = b[2];
                Vector3d m1 = new Vector3d(c + Math.Pow(b1, 2) * (1 - c), b1 * b2 * (1 - c) - b3 * s, b1 * b3 * (1 - c) + b2 * s);
                Vector3d m2 = new Vector3d(b2 * b1 * (1 - c) + b3 * s, c + Math.Pow(b2, 2) * (1 - c), b2 * b3 * (1 - c) - b1 * s);
                Vector3d m3 = new Vector3d(b3 * b1 * (1 - c) - b2 * s, b3 * b2 * (1 - c) + b1 * s, c + Math.Pow(b3, 2) * (1 - c));
                return new Vector3d(Vector3d.Multiply(m1, a), Vector3d.Multiply(m2, a), Vector3d.Multiply(m3, a));
            }
            IList<List<GH_Number>> r; IList<List<GH_Number>> ij;
            List<Vector3d> l_vec = new List<Vector3d>();
            int m; int e;
            if (!DA.GetDataTree(0, out GH_Structure<GH_Number> _r)) { }
            else
            {
                r = _r.Branches;
                if (!DA.GetDataTree(1, out GH_Structure<GH_Number> _ij)) { }
                else
                {
                    ij = _ij.Branches; m = ij.Count;
                    for (e = 0; e < m; e++)
                    {
                        int i = (int)ij[e][0].Value; int j = (int)ij[e][1].Value; double a_e = ij[e][4].Value;
                        Vector3d x = new Vector3d(r[j][0].Value - r[i][0].Value, r[j][1].Value - r[i][1].Value, r[j][2].Value - r[i][2].Value);
                        if (Math.Abs(x[0]) <= 5e-3 && Math.Abs(x[1]) <= 5e-3)
                        {
                            Vector3d y = rotation(x, new Vector3d(0, 1, 0), 90);
                            Vector3d z = rotation(y, x, 90 + a_e);
                            Vector3d l = z / Math.Sqrt(Vector3d.Multiply(z, z));
                            l_vec.Add(l);
                        }
                        else
                        {
                            Vector3d y = rotation(x, new Vector3d(0, 0, 1), 90);
                            y[2] = 0.0;
                            Vector3d z = rotation(y, x, 90 + a_e);
                            Vector3d l = z / Math.Sqrt(Vector3d.Multiply(z, z));
                            l_vec.Add(l);
                        }
                    }
                    DA.SetDataList("element axis vector", l_vec);
                }
            }
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
                return OpenSeesUtility.Properties.Resources.l_vec;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5b2632a6-7aff-4019-8ebb-d5f54a527809"); }
        }
    }
}