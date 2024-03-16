using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Display;
///using MathNet.Numerics.LinearAlgebra.Double;

using System.Drawing;
using System.Windows.Forms;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Rhino;
///****************************************

namespace VisualizeSelection
{
    public class VisualizeSelection : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public VisualizeSelection()
          : base("VisualizeSelection", "VisSelect",
              "Select the output result to display",
              "OpenSees", "Visualization")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_displacements", "D", "[[dxi,dyi,dzi,theta_xi,theta_yi,theta_zi,dxj,dyj,dzj,theta_xj,theta_yj,theta_zj],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("reaction_force", "reac_f", "[[Node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("section_force", "sec_f", "[[Ni,Qyi,Qzi,Mxi,Myi,Mzi,Ni,Qyi,Qzi,Mxj,Myj,Mzj,Nc,Qyc,Qzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("nodal_displacements(shell)", "D(shell)", "[[u_1,v_1,w_1,theta_x1,theta_y1,theta_z1],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("section_force(shell)", "shell_f", "[[Ni,Qyi,Qzi,Mxi,Myi,Mzi,Ni,Qyi,Qzi,Mxj,Myj,Mzj,Nk,Qyk,Qzk,Mxk,Myk,Mzk,Nl,Qyl,Qzl,Mxl,Myl,Mzl],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("KABE_W", "KABE_W", "[[No.i,No.j,No.k,No.l,kabebairitsu],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("shear_w", "shear_w", "[Q1,Q2,...](DataList)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("spring_force", "spring_f", "[[elementNo.,Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddIntegerParameter("load case", "load case", "[1,2,3...](DataList)", GH_ParamAccess.list, 1);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_displacements", "D", "[[dxi,dyi,dzi,theta_xi,theta_yi,theta_zi,dxj,dyj,dzj,theta_xj,theta_yj,theta_zj],...](DataTree)", GH_ParamAccess.tree);///0
            pManager.AddNumberParameter("reaction_force", "reac_f", "[[Node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);///1
            pManager.AddNumberParameter("section_force", "sec_f", "[[Ni,Qyi,Qzi,Mxi,Myi,Mzi,Ni,Qyi,Qzi,Mxj,Myj,Mzj,Nj,Qyc,Qzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree);///2
            pManager.AddNumberParameter("nodal_displacements(shell)", "D(shell)", "[[u_1,v_1,w_1,theta_x1,theta_y1,theta_z1],...](DataTree)", GH_ParamAccess.tree);///3
            pManager.AddNumberParameter("section_force(shell)", "shell_f", "[[Ni,Qyi,Qzi,Mxi,Myi,Mzi,Ni,Qyi,Qzi,Mxj,Myj,Mzj,Nk,Qyk,Qzk,Mxk,Myk,Mzk,Nl,Qyl,Qzl,Mxl,Myl,Mzl],...](DataTree)", GH_ParamAccess.tree);///4
            pManager.AddNumberParameter("KABE_W", "KABE_W", "[[No.i,No.j,No.k,No.l,kabebairitsu],...](DataTree)", GH_ParamAccess.tree);///5
            pManager.AddNumberParameter("shear_w", "shear_w", "[Q1,Q2,...](DataList)", GH_ParamAccess.list);///6
            pManager.AddNumberParameter("spring_force", "spring_f", "[[elementNo.,Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree);///7
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var loadcase = new List<int>(); DA.GetDataList("load case", loadcase);
            DA.GetDataTree("nodal_displacements", out GH_Structure<GH_Number> _D); var D = _D.Branches; var D_new = new GH_Structure<GH_Number>();
            DA.GetDataTree("reaction_force", out GH_Structure<GH_Number> _reac_f); var reac_f = _reac_f.Branches; var reac_f_new = new GH_Structure<GH_Number>();
            DA.GetDataTree("section_force", out GH_Structure<GH_Number> _sec_f); var sec_f = _sec_f.Branches; var sec_f_new = new GH_Structure<GH_Number>();
            DA.GetDataTree("nodal_displacements(shell)", out GH_Structure<GH_Number> _D2); var D2 = _D2.Branches; var D2_new = new GH_Structure<GH_Number>();
            DA.GetDataTree("section_force(shell)", out GH_Structure<GH_Number> _shell_f); var shell_f = _shell_f.Branches; var shell_f_new = new GH_Structure<GH_Number>();
            DA.GetDataTree("KABE_W", out GH_Structure<GH_Number> _kabe_w); var kabe_w = _kabe_w.Branches; var kabe_w_new = new GH_Structure<GH_Number>();
            var shear_w = new List<double>(); DA.GetDataList("shear_w", shear_w); var shear_w_new = new List<double>();
            DA.GetDataTree("spring_force", out GH_Structure<GH_Number> _spring_f); var spring_f = _spring_f.Branches; var spring_f_new = new GH_Structure<GH_Number>();
            if (D2[0][0].Value != -9999)
            {
                for (int e = 0; e < D2.Count; e++)
                {
                    var d = new List<GH_Number> { new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0) };
                    for (int i = 0; i < loadcase.Count; i++)
                    {
                        var k = loadcase[i];
                        for (int j = 0; j < 24; j++)
                        {
                            d[j] = new GH_Number(d[j].Value + D2[e][j + 24 * (k - 1)].Value);
                        }
                    }
                    D2_new.AppendRange(d, new GH_Path(e));
                }
                DA.SetDataTree(3, D2_new);
            }
            if (shell_f[0][0].Value != -9999)
            {
                for (int e = 0; e < shell_f.Count; e++)
                {
                    var f = new List<GH_Number> { new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0) };
                    for (int i = 0; i < loadcase.Count; i++)
                    {
                        var k = loadcase[i];
                        for (int j = 0; j < 24; j++)
                        {
                            f[j] = new GH_Number(f[j].Value + shell_f[e][j + 24 * (k - 1)].Value);
                        }
                    }
                    shell_f_new.AppendRange(f, new GH_Path(e));
                }
                DA.SetDataTree(4, shell_f_new);
            }
            if (D[0][0].Value != -9999 && sec_f[0][0].Value !=-9999)
            {
                for (int e = 0; e < D.Count; e++)
                {
                    var d = new List<GH_Number> { new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0) };
                    var f = new List<GH_Number> { new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0) };
                    for (int i = 0; i < loadcase.Count; i++)
                    {
                        var k = loadcase[i];
                        for (int j = 0; j < 12; j++)
                        {
                            d[j] = new GH_Number(d[j].Value + D[e][j + 12 * (k - 1)].Value);
                        }
                        for (int j = 0; j < 18; j++)
                        {
                            f[j] = new GH_Number(f[j].Value + sec_f[e][j + 18 * (k - 1)].Value);
                        }
                    }
                    D_new.AppendRange(d, new GH_Path(e)); sec_f_new.AppendRange(f, new GH_Path(e));
                }
                DA.SetDataTree(0, D_new); DA.SetDataTree(2, sec_f_new);
            }
            if (reac_f[0][0].Value != -9999)
            {
                for (int e = 0; e < reac_f.Count; e++)
                {
                    var f = new List<GH_Number> { new GH_Number(reac_f[e][0].Value), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0) };
                    for (int i = 0; i < loadcase.Count; i++)
                    {
                        var k = loadcase[i];
                        for (int j = 1; j < 7; j++)
                        {
                            f[j] = new GH_Number(f[j].Value + reac_f[e][j + 7 * (k - 1)].Value);
                        }
                    }
                    reac_f_new.AppendRange(f, new GH_Path(e));
                }
                DA.SetDataTree(1, reac_f_new);
            }
            if (kabe_w[0][0].Value!=-9999 && shear_w[0] != -9999)
            {
                for (int e = 0; e < kabe_w.Count; e++)
                {
                    var kw = new List<GH_Number> { new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0) };
                    for (int j = 0; j < 7; j++)
                    {
                        kw[j] = new GH_Number(kabe_w[e][j].Value);
                    }
                    kabe_w_new.AppendRange(kw, new GH_Path(e));
                    var q = 0.0;
                    for (int i = 0; i < loadcase.Count; i++)
                    {
                        var k = loadcase[i];
                        q += shear_w[e + shear_w.Count/ (kabe_w[0].Count/7) * (k - 1)];
                    }
                    shear_w_new.Add(q);
                }
                DA.SetDataTree(5, kabe_w_new); DA.SetDataList(6, shear_w_new);
            }
            if (spring_f[0][0].Value != -9999)
            {
                for (int e = 0; e < spring_f.Count; e++)
                {
                    var f = new List<GH_Number> { new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0), new GH_Number(0) };
                    for (int i = 0; i < loadcase.Count; i++)
                    {
                        var k = loadcase[i];
                        for (int j = 0; j < 6; j++)
                        {
                            f[j] = new GH_Number(f[j].Value + spring_f[e][j + 6 * (k - 1)].Value);
                        }
                    }
                    spring_f_new.AppendRange(f, new GH_Path(e));
                }
                DA.SetDataTree(7, spring_f_new);
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
                return OpenSeesUtility.Properties.Resources.visualizeselection;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("ebb513a6-ac0e-4153-aa44-6c4ab3c43974"); }
        }
    }
}