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

namespace Rotation
{
    public class Rotation : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public Rotation()
          : base("Rotate R", "Rotation",
              "Rotate R around A vector by angle",
              "OpenSees", "Utility")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree,-9999);///
            pManager.AddPointParameter("grid points", "P", "[[p00,p10...],[p01,p11]...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddVectorParameter("A vector that is the basis for rotation", "vec", "Nodes are rotated around this vector", GH_ParamAccess.item, new Vector3d(1, 0, 0));///
            pManager.AddNumberParameter("rotate angle", "angle", "fold angle(not radian)", GH_ParamAccess.item, 90);///
            pManager.AddPointParameter("center point of rotation", "center", "center point of rotation", GH_ParamAccess.item, new Point3d(0, 0, 0));///
            pManager.AddVectorParameter("offset after rotation", "offset", "R is offsetted after rotation", GH_ParamAccess.item, new Vector3d(0, 0, 0));///
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates after rotation", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddPointParameter("grid points after rotation", "P", "[[p00,p10...],[p01,p11]...](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var vec = new Vector3d(1, 0, 0); DA.GetData("A vector that is the basis for rotation", ref vec); var angle = 90.0; DA.GetData("rotate angle", ref angle); var center = new Point3d(0, 0, 0); DA.GetData("center point of rotation", ref center); var offset = new Vector3d(0, 0, 0); DA.GetData("offset after rotation", ref offset);
            GH_Structure<GH_Number> r2 = new GH_Structure<GH_Number>(); GH_Structure<GH_Point> p2 = new GH_Structure<GH_Point>();
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
            
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r);
            if (_r.Branches[0][0].Value != -9999) 
            {
                var r = _r.Branches; var n = r.Count;
                for (int i = 0; i < n; i++)
                {
                    var rvec = new Vector3d(r[i][0].Value-center[0], r[i][1].Value - center[1], r[i][2].Value - center[2]);
                    var r2vec = rotation(rvec, vec, angle);
                    var rlist = new List<GH_Number>(); rlist.Add(new GH_Number(r2vec[0]+offset[0])); rlist.Add(new GH_Number(r2vec[1] + offset[1])); rlist.Add(new GH_Number(r2vec[2] + offset[2]));
                    r2.AppendRange(rlist, new GH_Path(i));
                }
                DA.SetDataTree(0, r2);
            }
            else
            {
                DA.GetDataTree("grid points", out GH_Structure<GH_Point> _p);
                var p = _p.Branches;
                for (int i = 0; i < p.Count; i++)
                {
                    var plist = new List<GH_Point>();
                    for (int j = 0; j < p[i].Count; j++)
                    {
                        var r = p[i][j].Value;
                        var rvec = new Vector3d(r[0] - center[0], r[1] - center[1], r[2] - center[2]);
                        var r2vec = rotation(rvec, vec, angle);
                        var pj = new Point3d(r2vec[0] + offset[0], r2vec[1] + offset[1], r2vec[2] + offset[2]);
                        plist.Add(new GH_Point(pj));
                    }
                    p2.AppendRange(plist, new GH_Path(i));
                }
                DA.SetDataTree(1, p2);
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
                return OpenSeesUtility.Properties.Resources.rotation;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("a1465780-ea9f-4d60-9462-c6ea593dd2f5"); }
        }
    }
}