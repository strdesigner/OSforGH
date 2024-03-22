using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace OpenSeesUtility
{
    public class kabeQ : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public kabeQ()
          : base("KabeQ", "KabeQ",
              "Checking Allowable and Existing Shear Force",
              "OpenSees", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree, -9999);///0
            pManager.AddNumberParameter("KABE_W", "KABE_W", "[[No.i,No.j,No.k,No.l,kabebairitsu],...](DataTree)", GH_ParamAccess.tree, -9999);///1
            pManager.AddNumberParameter("shear_w(X)", "shear_w(X)", "[Q1,Q2,...](DataList)", GH_ParamAccess.list, -9999);///2
            pManager.AddNumberParameter("shear_w(Y)", "shear_w(Y)", "[Q1,Q2,...](DataList)", GH_ParamAccess.list, -9999);///3
            pManager.AddIntegerParameter("index1", "index1", "[wall1 element No.,...](Datalist)", GH_ParamAccess.list);//4
            pManager.AddIntegerParameter("index2", "index2", "[wall2 element No.,...](Datalist)", GH_ParamAccess.list);//5
            pManager.AddIntegerParameter("index3", "index3", "[wall3 element No.,...](Datalist)", GH_ParamAccess.list);//6
            pManager[4].Optional = true; pManager[5].Optional = true; pManager[6].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Qx", "Qx", "total existing shear force for X direction", GH_ParamAccess.list);///
            pManager.AddNumberParameter("Qy", "Qy", "total existing shear force for Y direction", GH_ParamAccess.list);///
            pManager.AddNumberParameter("Qax", "Qax", "total allowable shear force for X direction", GH_ParamAccess.list);///
            pManager.AddNumberParameter("Qay", "Qay", "total allowable shear force for Y direction", GH_ParamAccess.list);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r); var r = _r.Branches;
            DA.GetDataTree("KABE_W", out GH_Structure<GH_Number> _KABE_W); var KABE_W = _KABE_W.Branches;
            var shear_wx = new List<double>(); DA.GetDataList("shear_w(X)", shear_wx);
            var shear_wy = new List<double>(); DA.GetDataList("shear_w(Y)", shear_wy);
            var index1 = new List<int>(); if (!DA.GetDataList("index1", index1)) { index1 = new List<int>(); };
            var index2 = new List<int>(); if (!DA.GetDataList("index2", index2)) { index2 = new List<int>(); };
            var index3 = new List<int>(); if (!DA.GetDataList("index3", index3)) { index3 = new List<int>(); };
            var Qax = new List<double>(); var Qay = new List<double>(); var Qx = new List<double>(); var Qy = new List<double>();
            if (index1.Count != 0)
            {
                var qax = 0.0; var qay = 0.0; var qx = 0.0; var qy = 0.0;
                for (int i = 0; i < index1.Count; i++)
                {
                    int e = index1[i];
                    var n1 = (int)KABE_W[e][0].Value; var n2 = (int)KABE_W[e][1].Value; var n3 = (int)KABE_W[e][2].Value; var n4 = (int)KABE_W[e][3].Value;
                    var r1 = new Point3d(r[n1][0].Value, r[n1][1].Value, r[n1][2].Value); var r2 = new Point3d(r[n2][0].Value, r[n2][1].Value, r[n2][2].Value); var r3 = new Point3d(r[n3][0].Value, r[n3][1].Value, r[n3][2].Value); var r4 = new Point3d(r[n4][0].Value, r[n4][1].Value, r[n4][2].Value);
                    var c = (r1 + r2 + r3 + r4) / 4.0; var x = c[0]; var y = c[1]; var alpha = KABE_W[e][4].Value;
                    var width = 0.0; var theta1 = 90.0; var theta2 = 0.0;
                    if (Math.Abs(r1[2] - r2[2]) < Math.Abs(r2[2] - r3[2]))//ij辺が幅方向の時
                    {
                        width = ((r2 - r1).Length + (r4 - r3).Length) / 2.0;
                        if ((new Point3d(r1[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length != 0)
                        {
                            theta1 = Math.Acos((new Point3d(r1[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length / (r2 - r1).Length) / Math.PI * 180.0;
                        }
                        theta2 = Math.Acos(Math.Sqrt(Math.Pow(r2[2] - r3[2], 2)) / (r2 - r3).Length) / Math.PI * 180.0;
                    }
                    else
                    {
                        width = ((r4 - r1).Length + (r2 - r3).Length) / 2.0;
                        if ((new Point3d(r3[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length != 0)
                        {
                            theta1 = Math.Acos((new Point3d(r3[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length / (r2 - r3).Length) / Math.PI * 180.0;
                        }
                        theta2 = Math.Acos(Math.Sqrt(Math.Pow(r1[2] - r2[2], 2)) / (r1 - r2).Length) / Math.PI * 180.0;
                    }
                    var q_x = Math.Abs(shear_wx[e]); var q_y = Math.Abs(shear_wy[e]);
                    qax += alpha * width * Math.Abs(Math.Cos(theta1 / 180 * Math.PI)) * 1.96; qay += alpha * width * Math.Abs(Math.Sin(theta1 / 180 * Math.PI)) * 1.96;
                    qx += q_x * Math.Abs(Math.Cos(theta1 / 180 * Math.PI)); qy += q_y * Math.Abs(Math.Sin(theta1 / 180 * Math.PI));
                }
                Qx.Add(qx); Qy.Add(qy); Qax.Add(qax); Qay.Add(qay);
            }
            if (index2.Count != 0)
            {
                var qax = 0.0; var qay = 0.0; var qx = 0.0; var qy = 0.0;
                for (int i = 0; i < index2.Count; i++)
                {
                    int e = index2[i];
                    var n1 = (int)KABE_W[e][0].Value; var n2 = (int)KABE_W[e][1].Value; var n3 = (int)KABE_W[e][2].Value; var n4 = (int)KABE_W[e][3].Value;
                    var r1 = new Point3d(r[n1][0].Value, r[n1][1].Value, r[n1][2].Value); var r2 = new Point3d(r[n2][0].Value, r[n2][1].Value, r[n2][2].Value); var r3 = new Point3d(r[n3][0].Value, r[n3][1].Value, r[n3][2].Value); var r4 = new Point3d(r[n4][0].Value, r[n4][1].Value, r[n4][2].Value);
                    var c = (r1 + r2 + r3 + r4) / 4.0; var x = c[0]; var y = c[1]; var alpha = KABE_W[e][4].Value;
                    var width = 0.0; var theta1 = 90.0; var theta2 = 0.0;
                    if (Math.Abs(r1[2] - r2[2]) < Math.Abs(r2[2] - r3[2]))//ij辺が幅方向の時
                    {
                        width = ((r2 - r1).Length + (r4 - r3).Length) / 2.0;
                        if ((new Point3d(r1[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length != 0)
                        {
                            theta1 = Math.Acos((new Point3d(r1[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length / (r2 - r1).Length) / Math.PI * 180.0;
                        }
                        theta2 = Math.Acos(Math.Sqrt(Math.Pow(r2[2] - r3[2], 2)) / (r2 - r3).Length) / Math.PI * 180.0;
                    }
                    else
                    {
                        width = ((r4 - r1).Length + (r2 - r3).Length) / 2.0;
                        if ((new Point3d(r3[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length != 0)
                        {
                            theta1 = Math.Acos((new Point3d(r3[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length / (r2 - r3).Length) / Math.PI * 180.0;
                        }
                        theta2 = Math.Acos(Math.Sqrt(Math.Pow(r1[2] - r2[2], 2)) / (r1 - r2).Length) / Math.PI * 180.0;
                    }
                    var q_x = Math.Abs(shear_wx[e]); var q_y = Math.Abs(shear_wy[e]);
                    qax += alpha * width * Math.Abs(Math.Cos(theta1 / 180 * Math.PI)) * 1.96; qay += alpha * width * Math.Abs(Math.Sin(theta1 / 180 * Math.PI)) * 1.96;
                    qx += q_x * Math.Abs(Math.Cos(theta1 / 180 * Math.PI)); qy += q_y * Math.Abs(Math.Sin(theta1 / 180 * Math.PI));
                }
                Qx.Add(qx); Qy.Add(qy); Qax.Add(qax); Qay.Add(qay);
            }
            if (index3.Count != 0)
            {
                var qax = 0.0; var qay = 0.0; var qx = 0.0; var qy = 0.0;
                for (int i = 0; i < index3.Count; i++)
                {
                    int e = index3[i];
                    var n1 = (int)KABE_W[e][0].Value; var n2 = (int)KABE_W[e][1].Value; var n3 = (int)KABE_W[e][2].Value; var n4 = (int)KABE_W[e][3].Value;
                    var r1 = new Point3d(r[n1][0].Value, r[n1][1].Value, r[n1][2].Value); var r2 = new Point3d(r[n2][0].Value, r[n2][1].Value, r[n2][2].Value); var r3 = new Point3d(r[n3][0].Value, r[n3][1].Value, r[n3][2].Value); var r4 = new Point3d(r[n4][0].Value, r[n4][1].Value, r[n4][2].Value);
                    var c = (r1 + r2 + r3 + r4) / 4.0; var x = c[0]; var y = c[1]; var alpha = KABE_W[e][4].Value;
                    var width = 0.0; var theta1 = 90.0; var theta2 = 0.0;
                    if (Math.Abs(r1[2] - r2[2]) < Math.Abs(r2[2] - r3[2]))//ij辺が幅方向の時
                    {
                        width = ((r2 - r1).Length + (r4 - r3).Length) / 2.0;
                        if ((new Point3d(r1[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length != 0)
                        {
                            theta1 = Math.Acos((new Point3d(r1[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length / (r2 - r1).Length) / Math.PI * 180.0;
                        }
                        theta2 = Math.Acos(Math.Sqrt(Math.Pow(r2[2] - r3[2], 2)) / (r2 - r3).Length) / Math.PI * 180.0;
                    }
                    else
                    {
                        width = ((r4 - r1).Length + (r2 - r3).Length) / 2.0;
                        if ((new Point3d(r3[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length != 0)
                        {
                            theta1 = Math.Acos((new Point3d(r3[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length / (r2 - r3).Length) / Math.PI * 180.0;
                        }
                        theta2 = Math.Acos(Math.Sqrt(Math.Pow(r1[2] - r2[2], 2)) / (r1 - r2).Length) / Math.PI * 180.0;
                    }
                    var q_x = Math.Abs(shear_wx[e]); var q_y = Math.Abs(shear_wy[e]);
                    qax += alpha * width * Math.Abs(Math.Cos(theta1 / 180 * Math.PI)) * 1.96; qay += alpha * width * Math.Abs(Math.Sin(theta1 / 180 * Math.PI)) * 1.96;
                    qx += q_x * Math.Abs(Math.Cos(theta1 / 180 * Math.PI)); qy += q_y * Math.Abs(Math.Sin(theta1 / 180 * Math.PI));
                }
                Qx.Add(qx); Qy.Add(qy); Qax.Add(qax); Qay.Add(qay);
            }
            DA.SetDataList("Qax", Qax); DA.SetDataList("Qay", Qay);
            DA.SetDataList("Qx", Qx); DA.SetDataList("Qy", Qy);
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
                return OpenSeesUtility.Properties.Resources.kabeq;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b1f0fa9f-6a63-4880-b51f-5a69caf0395d"); }
        }
    }
}