using System;
using System.Collections;
using System.Collections.Generic;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.Display;
///using MathNet.Numerics.LinearAlgebra.Double;

using System.Drawing;
using System.Windows.Forms;
using System.Linq;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
///****************************************

namespace VisualizeResultShell
{
    public class VisualizeResultShell : GH_Component
    {
        public static int on_off_11 = 0; public static int on_off_12 = 0; public static int on_off_21 = 0; public static int on_off_22 = 0; public static int on_off_23 = 0;
        public static int on_off2_11 = 0; public static int on_off2_12 = 0; public static int on_off2_13 = 0; public static int on_off2_21 = 0; public static int on_off2_22 = 0; public static int on_off2_23 = 0;
        public static int on_off3_11 = 0; public static int on_off3_12 = 0; public static int on_off3_13 = 0; public static int on_off3_21 = 0; public static int on_off3_22 = 0; public static int on_off3_23 = 0;
        public static int Value = 0;
        double fontsize = double.NaN;
        string unit_of_force = "kN"; string unit_of_length = "m"; int digit = 4;
        public static void SetButton(string s, int i)
        {
            if (s == "c11")
            {
                on_off_11 = i;
            }
            else if (s == "c12")
            {
                on_off_12 = i;
            }
            else if (s == "c21")
            {
                on_off_21 = i;
            }
            else if (s == "c22")
            {
                on_off_22 = i;
            }
            else if (s == "c23")
            {
                on_off_23 = i;
            }
            else if (s == "c211")
            {
                on_off2_11 = i;
            }
            else if (s == "c212")
            {
                on_off2_12 = i;
            }
            else if (s == "c213")
            {
                on_off2_13 = i;
            }
            else if (s == "c221")
            {
                on_off2_21 = i;
            }
            else if (s == "c222")
            {
                on_off2_22 = i;
            }
            else if (s == "c223")
            {
                on_off2_23 = i;
            }
            else if (s == "c311")
            {
                on_off3_11 = i;
            }
            else if (s == "c312")
            {
                on_off3_12 = i;
            }
            else if (s == "c313")
            {
                on_off3_13 = i;
            }
            else if (s == "c321")
            {
                on_off3_21 = i;
            }
            else if (s == "c322")
            {
                on_off3_22 = i;
            }
            else if (s == "c323")
            {
                on_off3_23 = i;
            }
            else if (s == "Value")
            {
                Value = i;
            }
        }
        public VisualizeResultShell()
          : base("VisualizeAnalysisResult(Shell)", "VisualizeResult(Shell)",
              "Display analysis result by OpenSees (for shell elements)",
              "OpenSees", "Visualization")
        {
        }
        public override bool IsPreviewCapable { get { return true; } }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("element_node_relationship(shell)", "IJKL", "[[No.i,No.j,No.k,No.l(if triangle:-1),material No.,thickness],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("nodal_displacements(shell)", "D(shell)", "[[u_1,v_1,w_1,theta_x1,theta_y1,theta_z1],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("reaction_force", "reac_f", "[[Node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("section_force(shell)", "shell_f", "[[Ni,Qyi,Qzi,Mxi,Myi,Mzi,Ni,Qyi,Qzi,Mxj,Myj,Mzj,Nk,Qyk,Qzk,Mxk,Myk,Mzk,Nl,Qyl,Qzl,Mxl,Myl,Mzl],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("index(shell)", "index(shell)", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("scale_factor_for_disp", "DS", "scale factor for displacement", GH_ParamAccess.item, 100.0);///
            pManager.AddNumberParameter("scale_factor_for_N,Q", "NS", "scale factor for N,Q", GH_ParamAccess.item, 0.1);///
            pManager.AddNumberParameter("fontsize", "FS", "font size for display texts", GH_ParamAccess.item, 12.0);///
            pManager.AddNumberParameter("arcsize", "CS", "radius parameter for moment arc with arrow", GH_ParamAccess.item, 0.25);///
            pManager.AddIntegerParameter("legend parameter", "legend", "[offsetx, offsety,width,height,div]", GH_ParamAccess.list, new List<int> {50,50,50,300,5 });///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("element_node_relationship(shell)", "IJKL", "[[No.i,No.j,No.k,No.l(if triangle:-1),material No.,thickness],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("nodal_displacements(shell)", "D(shell)", "[[u_1,v_1,w_1,theta_x1,theta_y1,theta_z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("element force(shell)", "ele_f", "[[Ni,Qyi,Qzi,Mxi,Myi,Mzi,Ni,Qyi,Qzi,Mxj,Myj,Mzj,Nk,Qyk,Qzk,Mxk,Myk,Mzk,Nl,Qyl,Qzl,Mxl,Myl,Mzl],...](DataTree)", GH_ParamAccess.tree);///
        }
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r); var r = _r.Branches;
            DA.GetDataTree("element_node_relationship(shell)", out GH_Structure<GH_Number> _ijkl); var ijkl = _ijkl.Branches;
            DA.GetDataTree("nodal_displacements(shell)", out GH_Structure<GH_Number> _disp); var disp = _disp.Branches;
            DA.GetDataTree("section_force(shell)", out GH_Structure<GH_Number> _shell_f); var shell_f = _shell_f.Branches;
            var dscale = 0.0; DA.GetData("scale_factor_for_disp", ref dscale);
            var index = new List<double>(); DA.GetDataList("index(shell)", index);
            DA.GetData("fontsize", ref fontsize);
            double nscale = double.NaN; DA.GetData("scale_factor_for_N,Q", ref nscale);
            double arcsize = double.NaN; DA.GetData("arcsize", ref arcsize); List<int> legendparameter = new List<int>(); DA.GetDataList("legend parameter", legendparameter);
            if (ijkl[0][0].Value != -9999)
            {
                if (index[0] == -9999)
                {
                    index = new List<double>();
                    for (int e = 0; e < ijkl.Count; e++) { index.Add(e); }
                }
            }
            ///断面力の描画*****************************************************************************************
            if (r[0][0].Value != -9999 && ijkl[0][0].Value != -9999 && shell_f[0][0].Value != -9999)
            {
                var ele_f = new GH_Structure<GH_Number>();
                var N = new List<List<double>>(); var Qy = new List<List<double>>(); var Qz = new List<List<double>>(); var Mx = new List<List<double>>(); var My = new List<List<double>>(); var Mz = new List<List<double>>(); var Mxmax = -9999.0; var Mxmin = 9999.0; var Mymax = -9999.0; var Mymin = 9999.0;
                var xmax = -9999.0; var ymin = 9999.0;
                for (int ind = 0; ind < index.Count; ind++)
                {
                    int e = (int)index[ind];
                    if (ijkl[e][3].Value != -1)
                    {
                        int ni = (int)ijkl[e][0].Value; int nj = (int)ijkl[e][1].Value; int nk = (int)ijkl[e][2].Value; int nl = (int)ijkl[e][3].Value;
                        var ri = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value); var rj = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value); var rk = new Point3d(r[nk][0].Value, r[nk][1].Value, r[nk][2].Value); var rl = new Point3d(r[nl][0].Value, r[nl][1].Value, r[nl][2].Value); var rc = (ri + rj + rk + rl) / 4.0;
                        xmax = Math.Max(xmax, Math.Max(Math.Max(ri[0], rj[0]), Math.Max(rk[0], rl[0]))); ymin = Math.Max(ymin, Math.Min(Math.Min(ri[1], rj[1]), Math.Min(rk[1], rl[1])));
                        var rij = rj - ri; var rjk = rk - rj; var rkl = rl - rk; var rli = ri - rl;
                        var r1 = (ri + rj) / 2.0; var r2 = (rj + rk) / 2.0; var r3 = (rk + rl) / 2.0; var r4 = (rl + ri) / 2.0;
                        var Ne = new List<double>(); var Qye = new List<double>(); var Qze = new List<double>(); var Mxe = new List<double>(); var Mye = new List<double>(); var Mze = new List<double>();
                        for (int i = 0; i < 4; i++)
                        {
                            Ne.Add(shell_f[e][i * 6].Value); Qye.Add(shell_f[e][i * 6 + 1].Value); Qze.Add(shell_f[e][i * 6 + 2].Value); Mxe.Add(shell_f[e][i * 6 + 3].Value); Mye.Add(shell_f[e][i * 6 + 4].Value); Mze.Add(shell_f[e][i * 6 + 5].Value);
                        }
                        ///Mx************************************************************************************************************************
                        var Mxij = 0.0; var Mxjk = 0.0; var Mxkl = 0.0; var Mxli = 0.0; var dx = 0.0; var dy = 0.0; var Mxc = 0.0; 
                        if (Math.Max(Math.Abs(rij[0]), Math.Abs(rkl[0])) > Math.Max(Math.Abs(rjk[0]), Math.Abs(rli[0])))
                        {
                            Mxij = (Mxe[0] + Mxe[1]) / rij[0]; Mxkl = (Mxe[2] + Mxe[3]) / rkl[0];
                            Mxjk = (Mxe[1] - Mxe[2]) / ((rij[0] - rkl[0]) / 2.0); Mxli = (Mxe[3] - Mxe[0]) / ((rkl[0] - rij[0]) / 2.0);
                            Mxc = (Mxij + Mxjk + Mxkl + Mxli) / 4.0;
                            dx = (Mxjk - Mxli) / (r2[0] - r4[0]); dy = (Mxkl - Mxij) / (r3[1] - r1[1]);
                        }
                        else
                        {
                            Mxij = (Mxe[0] - Mxe[1]) / ((rjk[0] - rli[0]) / 2.0); Mxkl = (Mxe[2] - Mxe[3]) / ((rli[0]- rjk[0]) / 2.0);
                            Mxjk = (Mxe[1] + Mxe[2]) / rjk[0]; Mxli = (Mxe[3] + Mxe[0]) / rli[0];
                            Mxc = (Mxij + Mxjk + Mxkl + Mxli) / 4.0;
                            dx = (Mxkl - Mxij) / (r3[0] - r1[0]); dy = (Mxli - Mxjk) / (r4[1] - r2[1]);
                        }
                        var Mxi = Mxc + (ri - rc)[0] * dx + (ri - rc)[1] * dy; var Mxj = Mxc + (rj - rc)[0] * dx + (rj - rc)[1] * dy; var Mxk = Mxc + (rk - rc)[0] * dx + (rk - rc)[1] * dy; var Mxl = Mxc + (rl - rc)[0] * dx + (rl - rc)[1] * dy;
                        ele_f.AppendRange(new List<GH_Number> { new GH_Number(Mxi), new GH_Number(Mxj), new GH_Number(Mxk), new GH_Number(Mxl) }, new GH_Path(e));
                        //Mx.Add(new List<double> { Mxi, Mxj, Mxk, Mxl }); Mxmax = Math.Max(Mxmax, Math.Max(Math.Max(Mxi, Mxj), Math.Max(Mxk, Mxl))); Mxmin = Math.Min(Mxmin, Math.Min(Math.Min(Mxi, Mxj), Math.Min(Mxk, Mxl)));
                        Mx.Add(new List<double> { Mxc, Mxc, Mxc, Mxc }); Mxmax = Math.Max(Mxmax, Mxc); Mxmin = Math.Min(Mxmin, Mxc);
                        ///My************************************************************************************************************************
                        var Myij = 0.0; var Myjk = 0.0; var Mykl = 0.0; var Myli = 0.0; dx = 0.0; dy = 0.0; var Myc = 0.0;
                        if (Math.Max(Math.Abs(rij[1]), Math.Abs(rkl[1])) > Math.Max(Math.Abs(rjk[1]), Math.Abs(rli[1])))
                        {
                            Myij = (Mye[0] + Mye[1]) / rij[1]; Mykl = (Mye[2] + Mye[3]) / rkl[1];
                            Myjk = (Mye[1] - Mye[2]) / ((rij[1] - rkl[1]) / 2.0); Myli = (Mye[3] - Mye[0]) / ((rkl[1] - rij[1]) / 2.0);
                            Myc = (Myij + Myjk + Mykl + Myli) / 4.0;
                            dx = (Myij - Mykl) / (r1[0] - r3[0]); dy = (Myjk - Myli) / (r2[1] - r4[1]);
                        }
                        else
                        {
                            Myij = (Mye[0] - Mye[1]) / ((rli[1] - rjk[1]) / 2.0); Mykl = (Mye[2] - Mye[3]) / ((rjk[1] - rli[1]) / 2.0);
                            Myjk = (Mye[1] + Mye[2]) / rjk[1]; Myli = (Mye[3] + Mye[0]) / rli[1];
                            Myc = (Myij + Myjk + Mykl + Myli) / 4.0;
                            dx = (Myjk - Myli) / (r2[0] - r4[0]); dy = (Mykl - Myij) / (r3[1] - r1[1]);
                        }
                        var Myi = Myc + (ri - rc)[0] * dx + (ri - rc)[1] * dy; var Myj = Myc + (rj - rc)[0] * dx + (rj - rc)[1] * dy; var Myk = Myc + (rk - rc)[0] * dx + (rk - rc)[1] * dy; var Myl = Myc + (rl - rc)[0] * dx + (rl - rc)[1] * dy;
                        ele_f.AppendRange(new List<GH_Number> { new GH_Number(Myij), new GH_Number(Myjk), new GH_Number(Mykl), new GH_Number(Myli) }, new GH_Path(e));
                        //My.Add(new List<double> { Myi, Myj, Myk, Myl }); Mymax = Math.Max(Mymax, Math.Max(Math.Max(Myi, Myj), Math.Max(Myk, Myl))); Mymin = Math.Min(Mymin, Math.Min(Math.Min(Myi, Myj), Math.Min(Myk, Myl)));
                        My.Add(new List<double> { Myc, Myc, Myc, Myc }); Mymax = Math.Max(Mymax, Myc); Mymin = Math.Min(Mymin, Myc);
                    }
                }
                if (on_off3_21 == 1 || on_off3_22 == 1)
                {
                    var maxval = 0.0; var minval = 0.0; var val = new List<List<double>>();
                    if (on_off3_21 == 1) { maxval = Mxmax; minval = Mxmin; val = Mx; }
                    else if (on_off3_22 == 1) { maxval = Mymax; minval = Mymin; val = My; }
                    for (int ind = 0; ind < index.Count; ind++)
                    {
                        int e = (int)index[ind];
                        int ni = (int)ijkl[e][0].Value; int nj = (int)ijkl[e][1].Value; int nk = (int)ijkl[e][2].Value; int nl = (int)ijkl[e][3].Value;
                        var ri = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value); var rj = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value); var rk = new Point3d(r[nk][0].Value, r[nk][1].Value, r[nk][2].Value); var rl = new Point3d(r[nl][0].Value, r[nl][1].Value, r[nl][2].Value);
                        var mesh = new Mesh(); mesh.Vertices.Add(ri); mesh.Vertices.Add(rj); mesh.Vertices.Add(rk); mesh.Vertices.Add(rl);
                        var color1 = new ColorHSL((1 - (val[e][0]- minval) / (maxval - minval)) * 1.9 / 3.0, 1, 0.5); var color2 = new ColorHSL((1 - (val[e][1] - minval) / (maxval - minval)) * 1.9 / 3.0, 1, 0.5); var color3 = new ColorHSL((1 - (val[e][2] - minval) / (maxval - minval)) * 1.9 / 3.0, 1, 0.5); var color4 = new ColorHSL((1 - (val[e][3] - minval) / (maxval - minval)) * 1.9 / 3.0, 1, 0.5);
                        mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color2); mesh.VertexColors.Add(color3); mesh.VertexColors.Add(color4);
                        mesh.Faces.AddFace(0, 1, 2, 3);
                        _mesh.Add(mesh);
                    }
                    int width = legendparameter[2]; int height = legendparameter[3]; int d = legendparameter[4];
                    var legend = new Bitmap(width, height);
                    for (var x = 1; x < width; x++)
                    {
                        for (var y = 1; y < height; y++)
                        {
                            var col = new ColorHSL((1 - ((double)y / (double)height)) * 1.9 / 3.0, 1, 0.5);
                            legend.SetPixel(x, y, col);
                        }
                    }
                    _legend.Add(legend);_x.Add(legendparameter[0]); _y.Add(legendparameter[1]); _w.Add(width); _h.Add(height); _d.Add(d);
                    var texts = new List<string>();
                    for (int i = 0; i < d + 1; i++)
                    {
                        var value = (maxval - minval) / (double)d * i + minval;
                        texts.Add(Math.Round(value, Math.Min(3, (value.ToString()).Length)).ToString("F") + unit_of_force + unit_of_length + "/" + unit_of_length);
                    }
                    _text.Add(texts);
                }
                DA.SetDataTree(2, ele_f);
            }
            ///変形の描画*******************************************************************************************
            if (r[0][0].Value != -9999 && ijkl[0][0].Value != -9999 && disp[0][0].Value != -9999)
            {
                var d_v = new List<List<double>>(); var dmax = 0.0;
                for (int ind = 0; ind < index.Count; ind++)
                {
                    int e = (int)index[ind];
                    var d_e = new List<double>();
                    for (int i = 0; i < 24; i++)
                    {
                        if ((int)ijkl[e][3].Value >= 0 || i < 18) { d_e.Add(disp[e][i].Value * dscale); }
                        else { d_e.Add(disp[e][i - 6].Value * dscale); }
                    }
                    d_v.Add(d_e);
                    if (on_off_12 == 1) { d_v[e][0] = 0.0; d_v[e][1] = 0; d_v[e][6] = 0.0; d_v[e][7] = 0; d_v[e][12] = 0.0; d_v[e][13] = 0; d_v[e][18] = 0.0; d_v[e][19] = 0; }
                    if (on_off_21 == 1) { d_v[e][2] = 0.0; d_v[e][8] = 0.0; d_v[e][14] = 0.0; d_v[e][20] = 0.0; }
                    if (on_off_22 == 1) { d_v[e][1] = 0.0; d_v[e][2] = 0; d_v[e][7] = 0.0; d_v[e][8] = 0; d_v[e][13] = 0.0; d_v[e][14] = 0; d_v[e][19] = 0.0; d_v[e][20] = 0; }
                    if (on_off_23 == 1) { d_v[e][0] = 0.0; d_v[e][2] = 0; d_v[e][6] = 0.0; d_v[e][8] = 0; d_v[e][12] = 0.0; d_v[e][14] = 0; d_v[e][18] = 0.0; d_v[e][20] = 0; }
                    dmax = Math.Max(Math.Sqrt(Math.Pow(d_v[e][0], 2) + Math.Pow(d_v[e][1], 2) + Math.Pow(d_v[e][2], 2)), dmax);
                    dmax = Math.Max(Math.Sqrt(Math.Pow(d_v[e][6], 2) + Math.Pow(d_v[e][7], 2) + Math.Pow(d_v[e][8], 2)), dmax);
                    dmax = Math.Max(Math.Sqrt(Math.Pow(d_v[e][12], 2) + Math.Pow(d_v[e][13], 2) + Math.Pow(d_v[e][14], 2)), dmax);
                    dmax = Math.Max(Math.Sqrt(Math.Pow(d_v[e][18], 2) + Math.Pow(d_v[e][19], 2) + Math.Pow(d_v[e][20], 2)), dmax);
                }
                if (on_off_11 == 1 || on_off_12 == 1 || on_off_21 == 1 || on_off_22 == 1 || on_off_23 == 1)
                {
                    for (int ind = 0; ind < index.Count; ind++)
                    {
                        int e = (int)index[ind];
                        int n1 = (int)ijkl[e][0].Value; int n2 = (int)ijkl[e][1].Value; int n3 = (int)ijkl[e][2].Value; int n4 = (int)ijkl[e][3].Value;
                        var r1 = new Point3d(r[n1][0].Value, r[n1][1].Value, r[n1][2].Value); var r2 = new Point3d(r[n2][0].Value, r[n2][1].Value, r[n2][2].Value); var r3 = new Point3d(r[n3][0].Value, r[n3][1].Value, r[n3][2].Value); var r4 = new Point3d(r[n3][0].Value, r[n3][1].Value, r[n3][2].Value);
                        if ((int)ijkl[e][3].Value >= 0) { r4 = new Point3d(r[n4][0].Value, r[n4][1].Value, r[n4][2].Value); }
                        var mesh = new Mesh();
                        mesh.Vertices.Add(r1 + new Point3d(d_v[e][0], d_v[e][1], d_v[e][2])); mesh.Vertices.Add(r2 + new Point3d(d_v[e][6], d_v[e][7], d_v[e][8])); mesh.Vertices.Add(r3 + new Point3d(d_v[e][12], d_v[e][13], d_v[e][14])); mesh.Vertices.Add(r4 + new Point3d(d_v[e][18], d_v[e][19], d_v[e][20]));
                        var color1 = new ColorHSL((1 - Math.Sqrt(Math.Pow(d_v[e][0], 2) + Math.Pow(d_v[e][1], 2) + Math.Pow(d_v[e][2], 2)) / dmax) * 1.9 / 3.0, 1, 0.5);
                        var color2 = new ColorHSL((1 - Math.Sqrt(Math.Pow(d_v[e][6], 2) + Math.Pow(d_v[e][7], 2) + Math.Pow(d_v[e][8], 2)) / dmax) * 1.9 / 3.0, 1, 0.5);
                        var color3 = new ColorHSL((1 - Math.Sqrt(Math.Pow(d_v[e][12], 2) + Math.Pow(d_v[e][13], 2) + Math.Pow(d_v[e][14], 2)) / dmax) * 1.9 / 3.0, 1, 0.5);
                        var color4 = new ColorHSL((1 - Math.Sqrt(Math.Pow(d_v[e][18], 2) + Math.Pow(d_v[e][19], 2) + Math.Pow(d_v[e][20], 2)) / dmax) * 1.9 / 3.0, 1, 0.5);
                        mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color2); mesh.VertexColors.Add(color3); mesh.VertexColors.Add(color4);
                        mesh.Faces.AddFace(0, 1, 2, 3);
                        _mesh.Add(mesh);
                    }
                    int width = legendparameter[2]; int height = legendparameter[3]; int d = legendparameter[4];
                    var legend = new Bitmap(width, height);
                    for (var x = 1; x < width; x++)
                    {
                        for (var y = 1; y < height; y++)
                        {
                            var col = new ColorHSL((1 - ((double)y / (double)height)) * 1.9 / 3.0, 1, 0.5);
                            legend.SetPixel(x, y, col);
                        }
                    }
                    _legend.Add(legend); _x.Add(legendparameter[0]); _y.Add(legendparameter[1]); _w.Add(width); _h.Add(height); _d.Add(d);
                    var texts = new List<string>();
                    for (int i = 0; i < d + 1; i++)
                    {
                        var val = (dmax / dscale) / (double)d * i * 1000;//[mm]
                        texts.Add(Math.Round(val, Math.Min(4, (val.ToString()).Length)).ToString("F") + unit_of_length + unit_of_length);
                    }
                    _text.Add(texts);
                }
            }
            ///反力の描画*****************************************************************************************************
            if (!DA.GetDataTree("reaction_force", out GH_Structure<GH_Number> _reac_f)) { }
            else if (_reac_f.Branches[0][0].Value != -9999)
            {
                var reac_f = _reac_f.Branches;
                for (int i = 0; i < reac_f.Count; i++)
                {
                    int j = (int)reac_f[i][0].Value;
                    var r1 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value);
                    if (Math.Abs(reac_f[i][1].Value) > 1e-10 && on_off2_11 == 1)
                    {
                        var r2 = new Point3d(r[j][0].Value - reac_f[i][1].Value * nscale, r[j][1].Value, r[j][2].Value);
                        _arrow.Add(new Line(r2, r1));
                        if (Value == 1)
                        {
                            _value.Add(Math.Abs(reac_f[i][1].Value));
                            _pt.Add(r2);
                        }
                    }
                    if (Math.Abs(reac_f[i][2].Value) > 1e-10 && on_off2_12 == 1)
                    {
                        var r2 = new Point3d(r[j][0].Value, r[j][1].Value - reac_f[i][2].Value * nscale, r[j][2].Value);
                        _arrow.Add(new Line(r2, r1));
                        if (Value == 1)
                        {
                            _value.Add(Math.Abs(reac_f[i][2].Value));
                            _pt.Add(r2);
                        }
                    }
                    if (Math.Abs(reac_f[i][3].Value) > 1e-10 && on_off2_13 == 1)
                    {
                        var r2 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value - reac_f[i][3].Value * nscale);
                        _arrow.Add(new Line(r2, r1));
                        if (Value == 1)
                        {
                            _value.Add(Math.Abs(reac_f[i][3].Value));
                            _pt.Add(r2);
                        }
                    }
                    if (Math.Abs(reac_f[i][4].Value) > 1e-10 && on_off2_21 == 1)
                    {
                        Arc arc = new Arc();
                        if (reac_f[i][4].Value > 0)
                        {
                            r1 = new Point3d(r[j][0].Value, r[j][1].Value - arcsize, r[j][2].Value);
                            var r2 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value + arcsize);
                            var r3 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value - arcsize);
                            arc = new Arc(r1, r2, r3);
                            _arc.Add(arc);
                            _vec.Add(new Vector3d(0, -1, -0.325));
                        }
                        else
                        {
                            r1 = new Point3d(r[j][0].Value, r[j][1].Value + arcsize, r[j][2].Value);
                            var r2 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value + arcsize);
                            var r3 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value - arcsize);
                            arc = new Arc(r1, r2, r3);
                            _arc.Add(arc);
                            _vec.Add(new Vector3d(0, 1, -0.325));
                        }
                        if (Value == 1)
                        {
                            _pt0.Add(arc.StartPoint);
                            _value2.Add(Math.Abs(reac_f[i][4].Value));
                        }
                    }
                    if (Math.Abs(reac_f[i][5].Value) > 1e-10 && on_off2_22 == 1)
                    {
                        Arc arc = new Arc();
                        if (reac_f[i][5].Value > 0)
                        {
                            r1 = new Point3d(r[j][0].Value - arcsize, r[j][1].Value, r[j][2].Value);
                            var r2 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value + arcsize);
                            var r3 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value - arcsize);
                            arc = new Arc(r1, r2, r3);
                            _arc.Add(arc);
                            _vec.Add(new Vector3d(-1, 0, -0.325));
                        }
                        else
                        {
                            r1 = new Point3d(r[j][0].Value + arcsize, r[j][1].Value, r[j][2].Value);
                            var r2 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value + arcsize);
                            var r3 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value - arcsize);
                            arc = new Arc(r1, r2, r3);
                            _arc.Add(arc);
                            _vec.Add(new Vector3d(1, 0, -0.325));
                        }
                        if (Value == 1)
                        {
                            _pt0.Add(arc.StartPoint);
                            _value2.Add(Math.Abs(reac_f[i][5].Value));
                        }
                    }
                    if (Math.Abs(reac_f[i][6].Value) > 1e-10 && on_off2_23 == 1)
                    {
                        Arc arc = new Arc();
                        if (reac_f[i][6].Value > 0)
                        {
                            r1 = new Point3d(r[j][0].Value - arcsize, r[j][1].Value, r[j][2].Value);
                            var r2 = new Point3d(r[j][0].Value, r[j][1].Value + arcsize, r[j][2].Value);
                            var r3 = new Point3d(r[j][0].Value, r[j][1].Value - arcsize, r[j][2].Value);
                            arc = new Arc(r1, r2, r3);
                            _arc.Add(arc);
                            _vec.Add(new Vector3d(-1, -0.325, 0));
                        }
                        else
                        {
                            r1 = new Point3d(r[j][0].Value + arcsize, r[j][1].Value, r[j][2].Value);
                            var r2 = new Point3d(r[j][0].Value, r[j][1].Value + arcsize, r[j][2].Value);
                            var r3 = new Point3d(r[j][0].Value, r[j][1].Value - arcsize, r[j][2].Value);
                            arc = new Arc(r1, r2, r3);
                            _arc.Add(arc);
                            _vec.Add(new Vector3d(1, -0.325, 0));
                        }
                        if (Value == 1)
                        {
                            _pt0.Add(arc.StartPoint);
                            _value2.Add(Math.Abs(reac_f[i][6].Value));
                        }
                    }
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
                return OpenSeesUtility.Properties.Resources.VisResultShell;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1795b861-63db-4cdf-aba5-f3be03db9fc7"); }
        }
        ///ここからカスタム関数群********************************************************************************
        private readonly List<Mesh> _mesh = new List<Mesh>();
        private readonly List<Line> _arrow = new List<Line>();
        private readonly List<double> _value = new List<double>();
        private readonly List<double> _value2 = new List<double>();
        private readonly List<Point3d> _pt = new List<Point3d>();
        private readonly List<Point3d> _pt0 = new List<Point3d>();
        private readonly List<Arc> _arc = new List<Arc>();
        private readonly List<Vector3d> _vec = new List<Vector3d>();
        private readonly List<Bitmap> _legend = new List<Bitmap>();
        private readonly List<int> _x = new List<int>();
        private readonly List<int> _y = new List<int>();
        private readonly List<int> _w = new List<int>();
        private readonly List<int> _h = new List<int>();
        private readonly List<int> _d = new List<int>();
        private readonly List<List<string>> _text = new List<List<string>>();
        protected override void BeforeSolveInstance()
        {
            _mesh.Clear();
            _arrow.Clear();
            _value.Clear();
            _value2.Clear();
            _pt.Clear();
            _pt0.Clear();
            _arc.Clear();
            _vec.Clear();
            _legend.Clear();
            _x.Clear();
            _y.Clear();
            _w.Clear();
            _h.Clear();
            _d.Clear();
            _text.Clear();
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            args.Viewport.GetFrustumFarPlane(out Plane plane);
            RhinoViewport viewport = args.Viewport;
            for (int i = 0; i<_legend.Count; i++)
            {
                args.Display.DrawBitmap(new DisplayBitmap (_legend[i]), _x[i], _y[i]);
                for (int j = 0; j < _d[i] + 1; j++)
                {
                    args.Display.Draw2dText(_text[i][j], Color.Black, new Point2d(_x[i] + _w[i], _y[i] + _h[i] / _d[i] * j - fontsize / 2.0), false, (int)fontsize);
                }
            }
            ///変形・応力描画
            for (int i = 0; i < _mesh.Count; i++)
            {
                args.Display.DrawMeshFalseColors(_mesh[i]);
            }
            ///軸外力の描画用関数*******************************************************************************
            for (int i = 0; i < _arrow.Count; i++)
            {
                Line arrow = _arrow[i];
                args.Display.DrawLine(arrow, Color.ForestGreen, 2);
                args.Display.DrawArrowHead(arrow.To, arrow.Direction, Color.ForestGreen, 25, 0);
            }
            for (int i = 0; i < _value.Count; i++)
            {
                double size = fontsize;
                Point3d point = _pt[i];
                plane.Origin = point;
                viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                Text3d drawText = new Text3d(_value[i].ToString("F").Substring(0, digit) + unit_of_force, plane, size);
                args.Display.Draw3dText(drawText, Color.ForestGreen); drawText.Dispose();
            }
            ///*************************************************************************************************
            ///モーメント外力の描画用関数***********************************************************************
            for (int i = 0; i < _arc.Count; i++)
            {
                args.Display.DrawArc(_arc[i], Color.ForestGreen, 2);
                args.Display.DrawArrowHead(_arc[i].EndPoint, _vec[i], Color.ForestGreen, 25, 0);
            }
            for (int i = 0; i < _value2.Count; i++)
            {
                Point3d point = _pt0[i];
                double size = fontsize;
                plane.Origin = point;
                viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                Text3d drawText = new Text3d(_value2[i].ToString("F") + unit_of_force + unit_of_length, plane, size);
                args.Display.Draw3dText(drawText, Color.ForestGreen); drawText.Dispose();
            }
        }
        private DrawLegendConduit _conduit;
        private class DrawLegendConduit : DisplayConduit
        {
            public Bitmap Legend;
            protected override void DrawForeground(DrawEventArgs e)
            {
                e.Display.DrawBitmap(new DisplayBitmap(Legend), 50, 50);
            }
        }
        ///ここからGUIの作成*****************************************************************************************
        public class CustomGUI : GH_ComponentAttributes
        {
            public CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle title_rec; private Rectangle title1_rec; private Rectangle title2_rec; private Rectangle title3_rec;
            private Rectangle radio_rec; private Rectangle radio2_rec; private Rectangle radio3_rec; private Rectangle radio4_rec;
            private Rectangle radio_rec_11; private Rectangle text_rec_11; private Rectangle radio_rec_12; private Rectangle text_rec_12;
            private Rectangle radio_rec_21; private Rectangle text_rec_21; private Rectangle radio_rec_22; private Rectangle text_rec_22; private Rectangle radio_rec_23; private Rectangle text_rec_23;
            private Rectangle radio_rec2_11; private Rectangle text_rec2_11; private Rectangle radio_rec2_12; private Rectangle text_rec2_12; private Rectangle radio_rec2_13; private Rectangle text_rec2_13;
            private Rectangle radio_rec2_21; private Rectangle text_rec2_21; private Rectangle radio_rec2_22; private Rectangle text_rec2_22; private Rectangle radio_rec2_23; private Rectangle text_rec2_23;
            private Rectangle radio_rec3_11; private Rectangle text_rec3_11; private Rectangle radio_rec3_12; private Rectangle text_rec3_12; private Rectangle radio_rec3_13; private Rectangle text_rec3_13;
            private Rectangle radio_rec3_21; private Rectangle text_rec3_21; private Rectangle radio_rec3_22; private Rectangle text_rec3_22; private Rectangle radio_rec3_23; private Rectangle text_rec3_23;
            private Rectangle radio_rec4_11; private Rectangle text_rec4_11;

            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int height = 187; int subwidth = 36; int radi1 = 7; int radi2 = 4;
                int pitchx = 6; int textheight = 20; int subtitleheight = 18;
                global_rec.Height += height;
                title_rec = global_rec;
                title_rec.Y = title_rec.Bottom - height;
                title_rec.Height = 22;

                radio_rec = title_rec;
                radio_rec.Y += title_rec.Height;

                title1_rec = radio_rec;
                title1_rec.Height = subtitleheight;

                radio_rec_11 = title1_rec;
                radio_rec_11.X += radi2 - 1; radio_rec_11.Y += title1_rec.Height + radi2;
                radio_rec_11.Height = radi1; radio_rec_11.Width = radi1;

                text_rec_11 = radio_rec_11;
                text_rec_11.X += pitchx; text_rec_11.Y -= radi2;
                text_rec_11.Height = textheight; text_rec_11.Width = subwidth;

                radio_rec_12 = text_rec_11;
                radio_rec_12.X += text_rec_11.Width - radi2; radio_rec_12.Y = radio_rec_11.Y;
                radio_rec_12.Height = radi1; radio_rec_12.Width = radi1;

                text_rec_12 = radio_rec_12;
                text_rec_12.X += pitchx; text_rec_12.Y -= radi2;
                text_rec_12.Height = textheight; text_rec_12.Width = subwidth;

                radio_rec_21 = radio_rec_11;
                radio_rec_21.Y += text_rec_11.Height - radi1;
                radio_rec_21.Height = radi1; radio_rec_11.Width = radi1;

                text_rec_21 = radio_rec_21;
                text_rec_21.X += pitchx; text_rec_21.Y -= radi2;
                text_rec_21.Height = textheight; text_rec_21.Width = subwidth;

                radio_rec_22 = text_rec_21;
                radio_rec_22.X += text_rec_21.Width - radi2; radio_rec_22.Y = radio_rec_21.Y;
                radio_rec_22.Height = radi1; radio_rec_22.Width = radi1;

                text_rec_22 = radio_rec_22;
                text_rec_22.X += pitchx; text_rec_22.Y -= radi2;
                text_rec_22.Height = textheight; text_rec_22.Width = subwidth;

                radio_rec_23 = text_rec_22;
                radio_rec_23.X += text_rec_22.Width - radi2; radio_rec_23.Y = radio_rec_22.Y;
                radio_rec_23.Height = radi1; radio_rec_23.Width = radi1;

                text_rec_23 = radio_rec_23;
                text_rec_23.X += pitchx; text_rec_23.Y -= radi2;
                text_rec_23.Height = textheight; text_rec_23.Width = subwidth + 30;

                radio_rec.Height = text_rec_23.Y + textheight - radio_rec.Y - radi2;
                ///******************************************************************************************
                radio2_rec = radio_rec;
                radio2_rec.Y = radio_rec.Y + radio_rec.Height;

                title2_rec = title1_rec;
                title2_rec.Y = radio2_rec.Y;

                radio_rec2_11 = title2_rec;
                radio_rec2_11.X += radi2 - 1; radio_rec2_11.Y += title2_rec.Height + radi2;
                radio_rec2_11.Height = radi1; radio_rec2_11.Width = radi1;

                text_rec2_11 = radio_rec2_11;
                text_rec2_11.X += pitchx; text_rec2_11.Y -= radi2;
                text_rec2_11.Height = textheight; text_rec2_11.Width = subwidth;

                radio_rec2_12 = text_rec2_11;
                radio_rec2_12.X += text_rec2_11.Width - radi2; radio_rec2_12.Y = radio_rec2_11.Y;
                radio_rec2_12.Height = radi1; radio_rec2_12.Width = radi1;

                text_rec2_12 = radio_rec2_12;
                text_rec2_12.X += pitchx; text_rec2_12.Y -= radi2;
                text_rec2_12.Height = textheight; text_rec2_12.Width = subwidth;

                radio_rec2_13 = text_rec2_12;
                radio_rec2_13.X += text_rec2_12.Width - radi2; radio_rec2_13.Y = radio_rec2_12.Y;
                radio_rec2_13.Height = radi1; radio_rec2_13.Width = radi1;

                text_rec2_13 = radio_rec2_13;
                text_rec2_13.X += pitchx; text_rec2_13.Y -= radi2;
                text_rec2_13.Height = textheight; text_rec2_13.Width = subwidth + 30;

                radio_rec2_21 = radio_rec2_11;
                radio_rec2_21.Y += text_rec2_11.Height - radi1;
                radio_rec2_21.Height = radi1; radio_rec2_11.Width = radi1;

                text_rec2_21 = radio_rec2_21;
                text_rec2_21.X += pitchx; text_rec2_21.Y -= radi2;
                text_rec2_21.Height = textheight; text_rec2_21.Width = subwidth;

                radio_rec2_22 = text_rec2_21;
                radio_rec2_22.X += text_rec2_21.Width - radi2; radio_rec2_22.Y = radio_rec2_21.Y;
                radio_rec2_22.Height = radi1; radio_rec2_22.Width = radi1;

                text_rec2_22 = radio_rec2_22;
                text_rec2_22.X += pitchx; text_rec2_22.Y -= radi2;
                text_rec2_22.Height = textheight; text_rec2_22.Width = subwidth;

                radio_rec2_23 = text_rec2_22;
                radio_rec2_23.X += text_rec2_22.Width - radi2; radio_rec2_23.Y = radio_rec2_22.Y;
                radio_rec2_23.Height = radi1; radio_rec2_23.Width = radi1;

                text_rec2_23 = radio_rec2_23;
                text_rec2_23.X += pitchx; text_rec2_23.Y -= radi2;
                text_rec2_23.Height = textheight; text_rec2_23.Width = subwidth + 30;

                radio2_rec.Height = text_rec2_23.Y + textheight - radio2_rec.Y - radi2;
                ///******************************************************************************************
                radio3_rec = radio2_rec;
                radio3_rec.Y = radio2_rec.Y + radio2_rec.Height;

                title3_rec = title1_rec;
                title3_rec.Y = radio3_rec.Y;

                radio_rec3_11 = title3_rec;
                radio_rec3_11.X += radi2 - 1; radio_rec3_11.Y += title3_rec.Height + radi2;
                radio_rec3_11.Height = radi1; radio_rec3_11.Width = radi1;

                text_rec3_11 = radio_rec3_11;
                text_rec3_11.X += pitchx; text_rec3_11.Y -= radi2;
                text_rec3_11.Height = textheight; text_rec3_11.Width = subwidth;

                radio_rec3_12 = text_rec3_11;
                radio_rec3_12.X += text_rec3_11.Width - radi2; radio_rec3_12.Y = radio_rec3_11.Y;
                radio_rec3_12.Height = radi1; radio_rec3_12.Width = radi1;

                text_rec3_12 = radio_rec3_12;
                text_rec3_12.X += pitchx; text_rec3_12.Y -= radi2;
                text_rec3_12.Height = textheight; text_rec3_12.Width = subwidth;

                radio_rec3_13 = text_rec3_12;
                radio_rec3_13.X += text_rec3_12.Width - radi2; radio_rec3_13.Y = radio_rec3_12.Y;
                radio_rec3_13.Height = radi1; radio_rec3_13.Width = radi1;

                text_rec3_13 = radio_rec3_13;
                text_rec3_13.X += pitchx; text_rec3_13.Y -= radi2;
                text_rec3_13.Height = textheight; text_rec3_13.Width = subwidth + 30;

                radio_rec3_21 = radio_rec3_11;
                radio_rec3_21.Y += text_rec3_11.Height - radi1;
                radio_rec3_21.Height = radi1; radio_rec3_11.Width = radi1;

                text_rec3_21 = radio_rec3_21;
                text_rec3_21.X += pitchx; text_rec3_21.Y -= radi2;
                text_rec3_21.Height = textheight; text_rec3_21.Width = subwidth;

                radio_rec3_22 = text_rec3_21;
                radio_rec3_22.X += text_rec3_21.Width - radi2; radio_rec3_22.Y = radio_rec3_21.Y;
                radio_rec3_22.Height = radi1; radio_rec3_22.Width = radi1;

                text_rec3_22 = radio_rec3_22;
                text_rec3_22.X += pitchx; text_rec3_22.Y -= radi2;
                text_rec3_22.Height = textheight; text_rec3_22.Width = subwidth;

                radio_rec3_23 = text_rec3_22;
                radio_rec3_23.X += text_rec3_22.Width - radi2; radio_rec3_23.Y = radio_rec3_22.Y;
                radio_rec3_23.Height = radi1; radio_rec3_23.Width = radi1;

                text_rec3_23 = radio_rec3_23;
                text_rec3_23.X += pitchx; text_rec3_23.Y -= radi2;
                text_rec3_23.Height = textheight; text_rec3_23.Width = subwidth + 30;

                radio3_rec.Height = text_rec3_21.Y + textheight - radio3_rec.Y - radi2;
                ///******************************************************************************************
                radio4_rec = radio3_rec; radio4_rec.Height = textheight - radi2;
                radio4_rec.Y += radio3_rec.Height;

                radio_rec4_11 = radio4_rec;
                radio_rec4_11.X += radi2 - 1; radio_rec4_11.Y += radi2;
                radio_rec4_11.Height = radi1; radio_rec4_11.Width = radi1;

                text_rec4_11 = radio_rec4_11;
                text_rec4_11.X += pitchx; text_rec4_11.Y -= radi2;
                text_rec4_11.Height = textheight; text_rec4_11.Width = subwidth + 30;

                Bounds = global_rec;
            }
            Brush c11 = Brushes.White; Brush c12 = Brushes.White; Brush c21 = Brushes.White; Brush c22 = Brushes.White; Brush c23 = Brushes.White;
            Brush c211 = Brushes.White; Brush c212 = Brushes.White; Brush c213 = Brushes.White; Brush c221 = Brushes.White; Brush c222 = Brushes.White; Brush c223 = Brushes.White;
            Brush c311 = Brushes.White; Brush c312 = Brushes.White; Brush c313 = Brushes.White; Brush c321 = Brushes.White; Brush c322 = Brushes.White; Brush c323 = Brushes.White;
            Brush c411 = Brushes.White;
            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
            {
                base.Render(canvas, graphics, channel);
                if (channel == GH_CanvasChannel.Objects)
                {
                    StringFormat format = new StringFormat();
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    format.Trimming = StringTrimming.EllipsisCharacter;

                    GH_Capsule title = GH_Capsule.CreateCapsule(title_rec, GH_Palette.Pink, 2, 0);
                    title.Render(graphics, Selected, Owner.Locked, false);
                    title.Dispose();

                    RectangleF textRectangle = title_rec;
                    textRectangle.Height = 20;
                    graphics.DrawString("Display Option", GH_FontServer.Standard, Brushes.White, textRectangle, format);

                    GH_Capsule radio = GH_Capsule.CreateCapsule(radio_rec, GH_Palette.White, 2, 0);
                    radio.Render(graphics, Selected, Owner.Locked, false); radio.Dispose();

                    GH_Capsule title1 = GH_Capsule.CreateCapsule(title1_rec, GH_Palette.Blue, 2, 0);
                    title1.Render(graphics, Selected, Owner.Locked, false);
                    title1.Dispose();

                    RectangleF textRectangle1 = title1_rec;
                    textRectangle1.Height = 20;
                    graphics.DrawString("About displacement", GH_FontServer.Standard, Brushes.White, textRectangle1, format);


                    GH_Capsule radio_11 = GH_Capsule.CreateCapsule(radio_rec_11, GH_Palette.Black, 5, 5);
                    radio_11.Render(graphics, Selected, Owner.Locked, false); radio_11.Dispose();
                    graphics.FillEllipse(c11, radio_rec_11);
                    graphics.DrawString("dxyz", GH_FontServer.Standard, Brushes.Black, text_rec_11);

                    GH_Capsule radio_12 = GH_Capsule.CreateCapsule(radio_rec_12, GH_Palette.Black, 5, 5);
                    radio_12.Render(graphics, Selected, Owner.Locked, false); radio_12.Dispose();
                    graphics.FillEllipse(c12, radio_rec_12);
                    graphics.DrawString("dz", GH_FontServer.Standard, Brushes.Black, text_rec_12);

                    GH_Capsule radio_21 = GH_Capsule.CreateCapsule(radio_rec_21, GH_Palette.Black, 5, 5);
                    radio_21.Render(graphics, Selected, Owner.Locked, false); radio_21.Dispose();
                    graphics.FillEllipse(c21, radio_rec_21);
                    graphics.DrawString("dxy", GH_FontServer.Standard, Brushes.Black, text_rec_21);

                    GH_Capsule radio_22 = GH_Capsule.CreateCapsule(radio_rec_22, GH_Palette.Black, 5, 5);
                    radio_22.Render(graphics, Selected, Owner.Locked, false); radio_22.Dispose();
                    graphics.FillEllipse(c22, radio_rec_22);
                    graphics.DrawString("dx", GH_FontServer.Standard, Brushes.Black, text_rec_22);

                    GH_Capsule radio_23 = GH_Capsule.CreateCapsule(radio_rec_23, GH_Palette.Black, 5, 5);
                    radio_23.Render(graphics, Selected, Owner.Locked, false); radio_23.Dispose();
                    graphics.FillEllipse(c23, radio_rec_23);
                    graphics.DrawString("dy", GH_FontServer.Standard, Brushes.Black, text_rec_23);
                    ///******************************************************************************************
                    GH_Capsule radio2 = GH_Capsule.CreateCapsule(radio2_rec, GH_Palette.White, 2, 0);
                    radio2.Render(graphics, Selected, Owner.Locked, false); radio2.Dispose();

                    GH_Capsule title2 = GH_Capsule.CreateCapsule(title2_rec, GH_Palette.Blue, 2, 0);
                    title2.Render(graphics, Selected, Owner.Locked, false);
                    title2.Dispose();

                    RectangleF textRectangle2 = title2_rec;
                    textRectangle2.Height = 20;
                    graphics.DrawString("About reaction force", GH_FontServer.Standard, Brushes.White, textRectangle2, format);
                    ///******************************************************************************************

                    GH_Capsule radio2_11 = GH_Capsule.CreateCapsule(radio_rec2_11, GH_Palette.Black, 5, 5);
                    radio2_11.Render(graphics, Selected, Owner.Locked, false); radio2_11.Dispose();
                    graphics.FillEllipse(c211, radio_rec2_11);
                    graphics.DrawString("Rx", GH_FontServer.Standard, Brushes.Black, text_rec2_11);

                    GH_Capsule radio2_12 = GH_Capsule.CreateCapsule(radio_rec2_12, GH_Palette.Black, 5, 5);
                    radio2_12.Render(graphics, Selected, Owner.Locked, false); radio2_12.Dispose();
                    graphics.FillEllipse(c212, radio_rec2_12);
                    graphics.DrawString("Ry", GH_FontServer.Standard, Brushes.Black, text_rec2_12);

                    GH_Capsule radio2_13 = GH_Capsule.CreateCapsule(radio_rec2_13, GH_Palette.Black, 5, 5);
                    radio2_13.Render(graphics, Selected, Owner.Locked, false); radio2_13.Dispose();
                    graphics.FillEllipse(c213, radio_rec2_13);
                    graphics.DrawString("Rz", GH_FontServer.Standard, Brushes.Black, text_rec2_13);


                    GH_Capsule radio2_21 = GH_Capsule.CreateCapsule(radio_rec2_21, GH_Palette.Black, 5, 5);
                    radio2_21.Render(graphics, Selected, Owner.Locked, false); radio2_21.Dispose();
                    graphics.FillEllipse(c221, radio_rec2_21);
                    graphics.DrawString("Rmx", GH_FontServer.Standard, Brushes.Black, text_rec2_21);

                    GH_Capsule radio2_22 = GH_Capsule.CreateCapsule(radio_rec2_22, GH_Palette.Black, 5, 5);
                    radio2_22.Render(graphics, Selected, Owner.Locked, false); radio2_22.Dispose();
                    graphics.FillEllipse(c222, radio_rec2_22);
                    graphics.DrawString("Rmy", GH_FontServer.Standard, Brushes.Black, text_rec2_22);

                    GH_Capsule radio2_23 = GH_Capsule.CreateCapsule(radio_rec2_23, GH_Palette.Black, 5, 5);
                    radio2_23.Render(graphics, Selected, Owner.Locked, false); radio2_23.Dispose();
                    graphics.FillEllipse(c223, radio_rec2_23);
                    graphics.DrawString("Rmz", GH_FontServer.Standard, Brushes.Black, text_rec2_23);
                    ///******************************************************************************************
                    GH_Capsule radio3 = GH_Capsule.CreateCapsule(radio3_rec, GH_Palette.White, 2, 0);
                    radio3.Render(graphics, Selected, Owner.Locked, false); radio3.Dispose();

                    GH_Capsule title3 = GH_Capsule.CreateCapsule(title3_rec, GH_Palette.Blue, 2, 0);
                    title3.Render(graphics, Selected, Owner.Locked, false);
                    title3.Dispose();

                    RectangleF textRectangle3 = title3_rec;
                    textRectangle3.Height = 20;
                    graphics.DrawString("About sectional force", GH_FontServer.Standard, Brushes.White, textRectangle3, format);
                    ///******************************************************************************************

                    GH_Capsule radio3_11 = GH_Capsule.CreateCapsule(radio_rec3_11, GH_Palette.Black, 5, 5);
                    radio3_11.Render(graphics, Selected, Owner.Locked, false); radio3_11.Dispose();
                    graphics.FillEllipse(c311, radio_rec3_11);
                    graphics.DrawString("N", GH_FontServer.Standard, Brushes.Black, text_rec3_11);

                    GH_Capsule radio3_12 = GH_Capsule.CreateCapsule(radio_rec3_12, GH_Palette.Black, 5, 5);
                    radio3_12.Render(graphics, Selected, Owner.Locked, false); radio3_12.Dispose();
                    graphics.FillEllipse(c312, radio_rec3_12);
                    graphics.DrawString("Qy", GH_FontServer.Standard, Brushes.Black, text_rec3_12);

                    GH_Capsule radio3_13 = GH_Capsule.CreateCapsule(radio_rec3_13, GH_Palette.Black, 5, 5);
                    radio3_13.Render(graphics, Selected, Owner.Locked, false); radio3_13.Dispose();
                    graphics.FillEllipse(c313, radio_rec3_13);
                    graphics.DrawString("Qz", GH_FontServer.Standard, Brushes.Black, text_rec3_13);


                    GH_Capsule radio3_21 = GH_Capsule.CreateCapsule(radio_rec3_21, GH_Palette.Black, 5, 5);
                    radio3_21.Render(graphics, Selected, Owner.Locked, false); radio3_21.Dispose();
                    graphics.FillEllipse(c321, radio_rec3_21);
                    graphics.DrawString("Mx", GH_FontServer.Standard, Brushes.Black, text_rec3_21);

                    GH_Capsule radio3_22 = GH_Capsule.CreateCapsule(radio_rec3_22, GH_Palette.Black, 5, 5);
                    radio3_22.Render(graphics, Selected, Owner.Locked, false); radio3_22.Dispose();
                    graphics.FillEllipse(c322, radio_rec3_22);
                    graphics.DrawString("My", GH_FontServer.Standard, Brushes.Black, text_rec3_22);

                    GH_Capsule radio3_23 = GH_Capsule.CreateCapsule(radio_rec3_23, GH_Palette.Black, 5, 5);
                    radio3_23.Render(graphics, Selected, Owner.Locked, false); radio3_23.Dispose();
                    graphics.FillEllipse(c323, radio_rec3_23);
                    graphics.DrawString("Mz", GH_FontServer.Standard, Brushes.Black, text_rec3_23);
                    ///******************************************************************************************
                    GH_Capsule radio4 = GH_Capsule.CreateCapsule(radio4_rec, GH_Palette.White, 2, 0);
                    radio4.Render(graphics, Selected, Owner.Locked, false); radio4.Dispose();

                    GH_Capsule radio4_11 = GH_Capsule.CreateCapsule(radio_rec4_11, GH_Palette.Black, 5, 5);
                    radio4_11.Render(graphics, Selected, Owner.Locked, false); radio4_11.Dispose();
                    graphics.FillEllipse(c411, radio_rec4_11);
                    graphics.DrawString("Value", GH_FontServer.Standard, Brushes.Black, text_rec4_11);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec11 = radio_rec_11; RectangleF rec12 = radio_rec_12;
                    RectangleF rec21 = radio_rec_21; RectangleF rec22 = radio_rec_22; RectangleF rec23 = radio_rec_23;
                    RectangleF rec211 = radio_rec2_11; RectangleF rec212 = radio_rec2_12; RectangleF rec213 = radio_rec2_13;
                    RectangleF rec221 = radio_rec2_21; RectangleF rec222 = radio_rec2_22; RectangleF rec223 = radio_rec2_23;
                    RectangleF rec311 = radio_rec3_11; RectangleF rec312 = radio_rec3_12; RectangleF rec313 = radio_rec3_13;
                    RectangleF rec321 = radio_rec3_21; RectangleF rec322 = radio_rec3_22; RectangleF rec323 = radio_rec3_23;
                    RectangleF rec411 = radio_rec4_11;
                    if (rec11.Contains(e.CanvasLocation))
                    {
                        if (c11 == Brushes.Black) { c11 = Brushes.White; SetButton("c11", 0); }
                        else
                        { c11 = Brushes.Black; c12 = Brushes.White; c21 = Brushes.White; c22 = Brushes.White; c23 = Brushes.White; SetButton("c11", 1); SetButton("c12", 0); SetButton("c21", 0); SetButton("c22", 0); SetButton("c23", 0); c311 = Brushes.White; SetButton("c311", 0); c312 = Brushes.White; SetButton("c312", 0); c313 = Brushes.White; SetButton("c313", 0); c321 = Brushes.White; SetButton("c321", 0); c322 = Brushes.White; SetButton("c322", 0); c323 = Brushes.White; SetButton("c323", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec12.Contains(e.CanvasLocation))
                    {
                        if (c12 == Brushes.Black) { c12 = Brushes.White; SetButton("c12", 0); }
                        else
                        { c11 = Brushes.White; c12 = Brushes.Black; c21 = Brushes.White; c22 = Brushes.White; c23 = Brushes.White; SetButton("c11", 0); SetButton("c12", 1); SetButton("c21", 0); SetButton("c22", 0); SetButton("c23", 0); c311 = Brushes.White; SetButton("c311", 0); c312 = Brushes.White; SetButton("c312", 0); c313 = Brushes.White; SetButton("c313", 0); c321 = Brushes.White; SetButton("c321", 0); c322 = Brushes.White; SetButton("c322", 0); c323 = Brushes.White; SetButton("c323", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec21.Contains(e.CanvasLocation))
                    {
                        if (c21 == Brushes.Black) { c21 = Brushes.White; SetButton("c21", 0); }
                        else
                        { c11 = Brushes.White; c12 = Brushes.White; c21 = Brushes.Black; c22 = Brushes.White; c23 = Brushes.White; SetButton("c11", 0); SetButton("c12", 0); SetButton("c21", 1); SetButton("c22", 0); SetButton("c23", 0); c311 = Brushes.White; SetButton("c311", 0); c312 = Brushes.White; SetButton("c312", 0); c313 = Brushes.White; SetButton("c313", 0); c321 = Brushes.White; SetButton("c321", 0); c322 = Brushes.White; SetButton("c322", 0); c323 = Brushes.White; SetButton("c323", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec22.Contains(e.CanvasLocation))
                    {
                        if (c22 == Brushes.Black) { c22 = Brushes.White; SetButton("c22", 0); }
                        else
                        { c11 = Brushes.White; c12 = Brushes.White; c21 = Brushes.White; c22 = Brushes.Black; c23 = Brushes.White; SetButton("c11", 0); SetButton("c12", 0); SetButton("c21", 0); SetButton("c22", 1); SetButton("c23", 0); c311 = Brushes.White; SetButton("c311", 0); c312 = Brushes.White; SetButton("c312", 0); c313 = Brushes.White; SetButton("c313", 0); c321 = Brushes.White; SetButton("c321", 0); c322 = Brushes.White; SetButton("c322", 0); c323 = Brushes.White; SetButton("c323", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec23.Contains(e.CanvasLocation))
                    {
                        if (c23 == Brushes.Black) { c23 = Brushes.White; SetButton("c23", 0); }
                        else
                        { c11 = Brushes.White; c12 = Brushes.White; c21 = Brushes.White; c22 = Brushes.White; c23 = Brushes.Black; SetButton("c11", 0); SetButton("c12", 0); SetButton("c21", 0); SetButton("c22", 0); SetButton("c23", 1); c311 = Brushes.White; SetButton("c311", 0); c312 = Brushes.White; SetButton("c312", 0); c313 = Brushes.White; SetButton("c313", 0); c321 = Brushes.White; SetButton("c321", 0); c322 = Brushes.White; SetButton("c322", 0); c323 = Brushes.White; SetButton("c323", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    ///*************************************************************************************************************************************************
                    if (rec211.Contains(e.CanvasLocation))
                    {
                        if (c211 == Brushes.Black) { c211 = Brushes.White; SetButton("c211", 0); }
                        else { c211 = Brushes.Black; SetButton("c211", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec212.Contains(e.CanvasLocation))
                    {
                        if (c212 == Brushes.Black) { c212 = Brushes.White; SetButton("c212", 0); }
                        else { c212 = Brushes.Black; SetButton("c212", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec213.Contains(e.CanvasLocation))
                    {
                        if (c213 == Brushes.Black) { c213 = Brushes.White; SetButton("c213", 0); }
                        else { c213 = Brushes.Black; SetButton("c213", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec221.Contains(e.CanvasLocation))
                    {
                        if (c221 == Brushes.Black) { c221 = Brushes.White; SetButton("c221", 0); }
                        else { c221 = Brushes.Black; SetButton("c221", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec222.Contains(e.CanvasLocation))
                    {
                        if (c222 == Brushes.Black) { c222 = Brushes.White; SetButton("c222", 0); }
                        else { c222 = Brushes.Black; SetButton("c222", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec223.Contains(e.CanvasLocation))
                    {
                        if (c223 == Brushes.Black) { c223 = Brushes.White; SetButton("c223", 0); }
                        else { c223 = Brushes.Black; SetButton("c223", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    ///*************************************************************************************************************************************************
                    if (rec311.Contains(e.CanvasLocation))
                    {
                        if (c311 == Brushes.Black) { c311 = Brushes.White; SetButton("c311", 0); }
                        else { c311 = Brushes.Black; SetButton("c311", 1); c312 = Brushes.White; SetButton("c312", 0); c313 = Brushes.White; SetButton("c313", 0); c321 = Brushes.White; SetButton("c321", 0); c322 = Brushes.White; SetButton("c322", 0); c323 = Brushes.White; SetButton("c323", 0); c11 = Brushes.White; c12 = Brushes.White; c21 = Brushes.White; c22 = Brushes.White; c23 = Brushes.White; SetButton("c11", 0); SetButton("c12", 0); SetButton("c21", 0); SetButton("c22", 0); SetButton("c23", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec312.Contains(e.CanvasLocation))
                    {
                        if (c312 == Brushes.Black) { c312 = Brushes.White; SetButton("c312", 0); }
                        else { c312 = Brushes.Black; SetButton("c312", 1); c311 = Brushes.White; SetButton("c311", 0); c313 = Brushes.White; SetButton("c313", 0); c321 = Brushes.White; SetButton("c321", 0); c322 = Brushes.White; SetButton("c322", 0); c323 = Brushes.White; SetButton("c323", 0); c11 = Brushes.White; c12 = Brushes.White; c21 = Brushes.White; c22 = Brushes.White; c23 = Brushes.White; SetButton("c11", 0); SetButton("c12", 0); SetButton("c21", 0); SetButton("c22", 0); SetButton("c23", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec313.Contains(e.CanvasLocation))
                    {
                        if (c313 == Brushes.Black) { c313 = Brushes.White; SetButton("c313", 0); }
                        else { c313 = Brushes.Black; SetButton("c313", 1); c311 = Brushes.White; SetButton("c311", 0); c312 = Brushes.White; SetButton("c312", 0); c321 = Brushes.White; SetButton("c321", 0); c322 = Brushes.White; SetButton("c322", 0); c323 = Brushes.White; SetButton("c323", 0); c11 = Brushes.White; c12 = Brushes.White; c21 = Brushes.White; c22 = Brushes.White; c23 = Brushes.White; SetButton("c11", 0); SetButton("c12", 0); SetButton("c21", 0); SetButton("c22", 0); SetButton("c23", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec321.Contains(e.CanvasLocation))
                    {
                        if (c321 == Brushes.Black) { c321 = Brushes.White; SetButton("c321", 0);  }
                        else { c321 = Brushes.Black; SetButton("c321", 1); c311 = Brushes.White; SetButton("c311", 0); c312 = Brushes.White; SetButton("c312", 0); c313 = Brushes.White; SetButton("c313", 0); c322 = Brushes.White; SetButton("c322", 0); c323 = Brushes.White; SetButton("c323", 0); c11 = Brushes.White; c12 = Brushes.White; c21 = Brushes.White; c22 = Brushes.White; c23 = Brushes.White; SetButton("c11", 0); SetButton("c12", 0); SetButton("c21", 0); SetButton("c22", 0); SetButton("c23", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec322.Contains(e.CanvasLocation))
                    {
                        if (c322 == Brushes.Black) { c322 = Brushes.White; SetButton("c322", 0); }
                        else { c322 = Brushes.Black; SetButton("c322", 1); c311 = Brushes.White; SetButton("c311", 0); c312 = Brushes.White; SetButton("c312", 0); c313 = Brushes.White; SetButton("c313", 0); c321 = Brushes.White; SetButton("c321", 0); c323 = Brushes.White; SetButton("c323", 0); c11 = Brushes.White; c12 = Brushes.White; c21 = Brushes.White; c22 = Brushes.White; c23 = Brushes.White; SetButton("c11", 0); SetButton("c12", 0); SetButton("c21", 0); SetButton("c22", 0); SetButton("c23", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec323.Contains(e.CanvasLocation))
                    {
                        if (c323 == Brushes.Black) { c323 = Brushes.White; SetButton("c323", 0); }
                        else { c323 = Brushes.Black; SetButton("c323", 1); c311 = Brushes.White; SetButton("c311", 0); c312 = Brushes.White; SetButton("c312", 0); c313 = Brushes.White; SetButton("c313", 0); c321 = Brushes.White; SetButton("c321", 0); c322 = Brushes.White; SetButton("c322", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    ///*************************************************************************************************************************************************
                    if (rec411.Contains(e.CanvasLocation))
                    {
                        if (c411 == Brushes.Black) { c411 = Brushes.White; SetButton("Value", 0); }
                        else { c411 = Brushes.Black; SetButton("Value", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}