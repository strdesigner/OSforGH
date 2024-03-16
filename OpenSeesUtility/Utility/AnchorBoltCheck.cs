using System;
using System.IO;
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
using Rhino.DocObjects;
using Rhino;

namespace OpenSeesUtility
{
    public class AnchorBoltCheck : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public AnchorBoltCheck()
          : base("check anchor bolt for each wick", "AnchorBoltCheck",
              "Allowable stress design for anchor bolt",
              "OpenSees", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("R", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("x", "x", "x coordinate of guide lines", GH_ParamAccess.list);///
            pManager.AddNumberParameter("y", "y", "y coordinate of guide lines", GH_ParamAccess.list);///
            pManager.AddTextParameter("xlabel", "xlabel", "[label1, label2...]", GH_ParamAccess.list);///
            pManager.AddTextParameter("ylabel", "ylabel", "[label1, label2...]", GH_ParamAccess.list);///
            pManager.AddTextParameter("xlabel for check", "xlabel for check", "[label1, label2...]", GH_ParamAccess.list);///
            pManager.AddTextParameter("ylabel for check", "ylabel for check", "[label1, label2...]", GH_ParamAccess.list);///
            pManager.AddNumberParameter("reac_f", "reac_f", "[[Node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("eps", "eps", "shear force under this value is ignored", GH_ParamAccess.item,0.1);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Qx", "Qx", "[[qx1,qx1,qx1],[qx2,qx2,qx2],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("Qy", "Qy", "[[qy1,qy1,qy1],[qy2,qy2,qy2],...](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetDataTree("R", out GH_Structure<GH_Number> _R); var R = _R.Branches;
            var x = new List<double>(); DA.GetDataList("x", x); var y = new List<double>(); DA.GetDataList("y", y);
            var xlabel = new List<string>(); DA.GetDataList("xlabel", xlabel); var ylabel = new List<string>(); DA.GetDataList("ylabel", ylabel);
            var xl = new List<string>(); DA.GetDataList("xlabel for check", xl); var yl = new List<string>(); DA.GetDataList("ylabel for check", yl);
            DA.GetDataTree("reac_f", out GH_Structure<GH_Number> _reac_f); var reac_f = _reac_f.Branches;
            var eps = 0.1; DA.GetData("eps", ref eps);
            var QX = new GH_Structure<GH_Number>(); var QY = new GH_Structure<GH_Number>();
            for (int i = 0; i < xl.Count; i++)
            {
                int ind = xlabel.IndexOf(xl[i]);
                var qy = new List<GH_Number>();
                for (int j = 0; j < reac_f.Count; j++)
                {
                    int k = (int)reac_f[j][0 + 7].Value;
                    if (Math.Abs(x[ind] - R[k][0].Value) < 5e-3 && Math.Abs(reac_f[j][2 + 7].Value) > eps) { qy.Add(new GH_Number(Math.Abs(reac_f[j][2 + 7].Value))); }
                }
                QY.AppendRange(qy, new GH_Path(i));
            }
            for (int i = 0; i < yl.Count; i++)
            {
                int ind = ylabel.IndexOf(yl[i]);
                var qx = new List<GH_Number>();
                for (int j = 0; j < reac_f.Count; j++)
                {
                    int k = (int)reac_f[j][0].Value;
                    if (Math.Abs(y[ind] - R[k][1].Value) < 5e-3 && Math.Abs(reac_f[j][1].Value) > eps) { qx.Add(new GH_Number(Math.Abs(reac_f[j][1].Value))); }
                }
                QX.AppendRange(qx, new GH_Path(i));
            }
            DA.SetDataTree(0, QX); DA.SetDataTree(1, QY);
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
                return OpenSeesUtility.Properties.Resources.boltcheckpdf;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("c4edbf3d-580f-4d66-8cc5-96bf3cea5fef"); }
        }
    }
}