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

namespace OpenSeesUtility
{
    public class VisualizeResult2 : GH_Component
    {
        public static int on_off_11 = 0; public static int on_off_12 = 0; public static int on_off_21 = 0; public static int on_off_22 = 0; public static int on_off_23 = 0;
        public static int on_off2_11 = 0; public static int on_off2_12 = 0; public static int on_off2_13 = 0; public static int on_off2_21 = 0; public static int on_off2_22 = 0; public static int on_off2_23 = 0;
        public static int on_off3_11 = 0; public static int on_off3_12 = 0; public static int on_off3_13 = 0; public static int on_off3_21 = 0; public static int on_off3_22 = 0; public static int on_off3_23 = 0; public static int on_off3_31 = 0;
        public static int Value = 0; public static int Delta = 0;
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
            else if (s == "c331")
            {
                on_off3_31 = i;
            }
            else if (s == "Value")
            {
                Value = i;
            }
            else if (s == "Delta")
            {
                Delta = i;
            }
        }
        public VisualizeResult2()
          : base("VisualizeAnalysisResult2", "VisualizeResult2",
              "Display analysis result by OpenSees",
              "OpenSees", "Visualization")
        {
        }
        public override bool IsPreviewCapable { get { return true; } }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("nodal_displacements", "D", "[[dxi,dyi,dzi,theta_xi,theta_yi,theta_zi,dxj,dyj,dzj,theta_xj,theta_yj,theta_zj],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("reaction_force", "reac_f", "[[Node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("section_force", "sec_f", "[[Ni,Qyi,Qzi,Mxi,Myi,Mzi,Ni,Qyi,Qzi,Mxj,Myj,Mzj,Nj,Qyc,Qzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddVectorParameter("element axis vector", "l_vec", "element axis vector for each elements", GH_ParamAccess.list, new Vector3d(-9999, -9999, -9999));
            pManager.AddNumberParameter("KABE_W", "KABE_W", "[[No.i,No.j,No.k,No.l,kabebairitsu],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("shear_w", "shear_w", "[Q1,Q2,...](DataList)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("index", "index", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("index(kabe)", "index(kabe)", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddIntegerParameter("divide_display_points", "Div", "number of display point per element", GH_ParamAccess.item, 30);///
            pManager.AddNumberParameter("scale_factor_for_disp", "DS", "scale factor for displacement", GH_ParamAccess.item, 100.0);///
            pManager.AddNumberParameter("scale_factor_for_N,Q", "NS", "scale factor for N,Q", GH_ParamAccess.item, 0.1);///
            pManager.AddNumberParameter("scale_factor_for_M", "MS", "scale factor for M", GH_ParamAccess.item, 0.15);///
            pManager.AddNumberParameter("fontsize", "FS", "font size for display texts", GH_ParamAccess.item, 12.0);///
            pManager.AddNumberParameter("arcsize", "CS", "radius parameter for moment arc with arrow", GH_ParamAccess.item, 0.25);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("section_force", "sec_f", "[[Element No.,Ni,Qyi,Qzi,Mxi,Myi,Mzi,Ni,Qyi,Qzi,Mxj,Myj,Mzj,Nj,Qyc,Qzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("KABE_W", "KABE_W", "[[No.i,No.j,No.k,No.l,kabebairitsu],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("shear_w", "shear_w", "[Q1,Q2,...](DataList)", GH_ParamAccess.list);///
            //pManager.AddSurfaceParameter("geometry", "geometry", "geometry", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            ///**************************************************************************************************
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r); var r = _r.Branches; DA.SetDataTree(0, _r);
            DA.GetDataTree("element_node_relationship", out GH_Structure<GH_Number> _ij); var ij = _ij.Branches; List<double> index = new List<double>(); List<double> index2 = new List<double>(); List<double> index3 = new List<double>();
            var dscale = 0.0; DA.GetData("scale_factor_for_disp", ref dscale); DA.GetDataList("index", index); DA.GetDataList("index(kabe)", index3);
            double nscale = double.NaN; if (!DA.GetData("scale_factor_for_N,Q", ref nscale)) return;
            double mscale = double.NaN; if (!DA.GetData("scale_factor_for_M", ref mscale)) return;
            if (!DA.GetData("fontsize", ref fontsize)) return; int div = 10; if (!DA.GetData("divide_display_points", ref div)) return;
            double arcsize = double.NaN; if (!DA.GetData("arcsize", ref arcsize)) return;
            if (ij[0][0].Value != -9999 && r[0][0].Value != -9999)
            {
                int m = ij.Count;
                if (index[0] != 9999)
                {
                    if (index[0] == -9999)
                    {
                        index = new List<double>();
                        for (int e = 0; e < m; e++) { index.Add(e); }
                    }
                    GH_Structure<GH_Number> ij_new = new GH_Structure<GH_Number>();
                    for (int ind = 0; ind < index.Count; ind++)
                    {
                        int e = (int)index[ind];
                        ij_new.AppendRange(ij[e], new GH_Path(ind));
                    }
                    DA.SetDataTree(1, ij_new);
                }
                else { index = new List<double>(); }
                Matrix transmatrix(double l, double lx, double ly, double lz, double a)
                {
                    lx /= l; double mx = ly / l; double nx = lz / l; a = a * Math.PI / 180.0;
                    double my; var ny = 0.0; double mz; double nz = 0.0;
                    if (lx == 0.0 && ly == 0.0)
                    {
                        ly = nx * Math.Cos(a); my = Math.Sin(a);
                        lz = -nx * Math.Sin(a); mz = Math.Cos(a);
                    }
                    else
                    {
                        var ll = Math.Sqrt(Math.Pow(lx, 2) + Math.Pow(mx, 2));
                        ly = -mx * Math.Cos(a) / ll - nx * lx * Math.Sin(a) / ll;
                        my = lx * Math.Cos(a) / ll - nx * mx * Math.Sin(a) / ll;
                        ny = Math.Sin(a) * ll;
                        lz = mx * Math.Sin(a) / ll - nx * lx * Math.Cos(a) / ll;
                        mz = -lx * Math.Sin(a) / ll - nx * mx * Math.Cos(a) / ll;
                        nz = Math.Cos(a) * ll;
                    }
                    var tr = new Matrix(12, 12);
                    tr[0, 0] = lx; tr[0, 1] = mx; tr[0, 2] = nx;
                    tr[1, 0] = ly; tr[1, 1] = my; tr[1, 2] = ny;
                    tr[2, 0] = lz; tr[2, 1] = mz; tr[2, 2] = nz;
                    tr[3, 3] = lx; tr[3, 4] = mx; tr[3, 5] = nx;
                    tr[4, 3] = ly; tr[4, 4] = my; tr[4, 5] = ny;
                    tr[5, 3] = lz; tr[5, 4] = mz; tr[5, 5] = nz;
                    tr[6, 6] = lx; tr[6, 7] = mx; tr[6, 8] = nx;
                    tr[7, 6] = ly; tr[7, 7] = my; tr[7, 8] = ny;
                    tr[8, 6] = lz; tr[8, 7] = mz; tr[8, 8] = nz;
                    tr[9, 9] = lx; tr[9, 10] = mx; tr[9, 11] = nx;
                    tr[10, 9] = ly; tr[10, 10] = my; tr[10, 11] = ny;
                    tr[11, 9] = lz; tr[11, 10] = mz; tr[11, 11] = nz;
                    return tr;
                }
                Matrix N(double l, double x)
                {
                    var Nmat = new Matrix(3, 12);
                    Nmat[0, 0] = 1 - x / l; Nmat[0, 6] = x / l;
                    Nmat[1, 1] = 1 - 3 * Math.Pow(x / l, 2) + 2 * Math.Pow(x / l, 3); Nmat[1, 5] = l * (x / l - 2 * Math.Pow(x / l, 2) + Math.Pow(x / l, 3)); Nmat[1, 7] = 3 * Math.Pow(x / l, 2) - 2 * Math.Pow(x / l, 3); Nmat[1, 11] = l * (-Math.Pow(x / l, 2) + Math.Pow(x / l, 3));
                    Nmat[2, 2] = 1 - 3 * Math.Pow(x / l, 2) + 2 * Math.Pow(x / l, 3); Nmat[2, 4] = -l * (x / l - 2 * Math.Pow(x / l, 2) + Math.Pow(x / l, 3)); Nmat[2, 8] = 3 * Math.Pow(x / l, 2) - 2 * Math.Pow(x / l, 3); Nmat[2, 10] = -l * (-Math.Pow(x / l, 2) + Math.Pow(x / l, 3));
                    return Nmat;
                }
                if (!DA.GetDataTree("nodal_displacements", out GH_Structure<GH_Number> _d)) { }
                else if (_d.Branches[0][0].Value != -9999)
                {
                    var d = _d.Branches; double dmax = 0.0;
                    ///節点の描画****************************************************************************************
                    var ddivlist = new List<List<Point3d>>(); var rdivlist = new List<List<Point3d>>();
                    if (dscale == 0) { dscale = 1e-12; }
                    for (int e = 0; e < m; e++)
                    {
                        int i = (int)ij[e][0].Value; int j = (int)ij[e][1].Value; double theta = ij[e][4].Value;
                        Point3d r1 = new Point3d(r[i][0].Value, r[i][1].Value, r[i][2].Value);
                        Point3d r2 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value);
                        var d_g = new Matrix(12, 1);
                        d_g[0, 0] = d[e][0].Value * dscale; d_g[1, 0] = d[e][1].Value * dscale; d_g[2, 0] = d[e][2].Value * dscale; d_g[3, 0] = d[e][3].Value * dscale; d_g[4, 0] = d[e][4].Value * dscale; d_g[5, 0] = d[e][5].Value * dscale; d_g[6, 0] = d[e][6].Value * dscale; d_g[7, 0] = d[e][7].Value * dscale; d_g[8, 0] = d[e][8].Value * dscale; d_g[9, 0] = d[e][9].Value * dscale; d_g[10, 0] = d[e][10].Value * dscale; d_g[11, 0] = d[e][11].Value * dscale;
                        double l = Math.Sqrt(Math.Pow(r2[0] - r1[0], 2) + Math.Pow(r2[1] - r1[1], 2) + Math.Pow(r2[2] - r1[2], 2));
                        double lx = r2[0] - r1[0]; double ly = r2[1] - r1[1]; double lz = r2[2] - r1[2];
                        Matrix tr = transmatrix(l, lx, ly, lz, theta); Matrix d_e = tr * d_g; List<Point3d> rdiv = new List<Point3d>(); List<Point3d> ddiv = new List<Point3d>();
                        if (on_off_11 == 1 || on_off_12 == 1 || on_off_21 == 1 || on_off_22 == 1 || on_off_23 == 1)
                        {
                            Matrix tr2 = new Matrix(3, 3);
                            tr2[0, 0] = tr[0, 0]; tr2[0, 1] = tr[1, 0]; tr2[0, 2] = tr[2, 0];
                            tr2[1, 0] = tr[0, 1]; tr2[1, 1] = tr[1, 1]; tr2[1, 2] = tr[2, 1];
                            tr2[2, 0] = tr[0, 2]; tr2[2, 1] = tr[1, 2]; tr2[2, 2] = tr[2, 2];
                            for (int k = 0; k < div + 1; k++)
                            {
                                double x = (double)k / (double)div * l;
                                Matrix dmat = N(l, x) * d_e;
                                dmat = tr2 * dmat;
                                Point3d d_v = new Point3d(dmat[0, 0], dmat[1, 0], dmat[2, 0]);
                                if (on_off_12 == 1) { d_v[0] = 0; d_v[1] = 0; }
                                if (on_off_21 == 1) { d_v[2] = 0; }
                                if (on_off_22 == 1) { d_v[1] = 0; d_v[2] = 0; }
                                if (on_off_23 == 1) { d_v[0] = 0; d_v[2] = 0; }
                                rdiv.Add(r1 + (r2 - r1) * (double)k / (double)div + d_v); ddiv.Add(d_v);
                                dmax = Math.Max(Math.Sqrt(Math.Pow(d_v[0], 2) + Math.Pow(d_v[1], 2) + Math.Pow(d_v[2], 2)), dmax);
                            }
                            rdivlist.Add(rdiv); ddivlist.Add(ddiv);
                        }
                    }
                    if (on_off_11 == 1 || on_off_12 == 1 || on_off_21 == 1 || on_off_22 == 1 || on_off_23 == 1)
                    {
                        for (int ind = 0; ind < index.Count; ind++)
                        {
                            int e = (int)index[ind]; int i = (int)ij[e][0].Value; int j = (int)ij[e][1].Value;
                            var rdiv = rdivlist[e]; var ddiv = ddivlist[e];
                            var l = new Vector3d(r[j][0].Value - r[i][0].Value, r[j][1].Value - r[i][1].Value, r[j][2].Value - r[i][2].Value).Length;
                            for (int k = 0; k < div; k++)
                            {
                                _l.Add(new Line(rdiv[k], rdiv[k + 1]));
                                var d_ave = (ddiv[k] + ddiv[k + 1]) / 2.0;
                                var color = new ColorHSL((1 - Math.Sqrt(Math.Pow(d_ave[0], 2) + Math.Pow(d_ave[1], 2) + Math.Pow(d_ave[2], 2)) / dmax) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                            }
                            _pt1.Add(rdiv[0]); _pt1.Add(rdiv[div]); _pt1.Add(rdiv[(int)(div / 2)]);
                            var scale = 1.0;
                            if (unit_of_length == "m") { scale = 1000.0; } else if (unit_of_length == "cm") { scale = 10.0; }
                            if (on_off_11 == 1 && (Value == 1 || Delta == 1))
                            {
                                if (Value == 1)
                                {

                                    _dt.Add(Math.Round(Math.Sqrt(Math.Pow(ddiv[0][0], 2) + Math.Pow(ddiv[0][1], 2) + Math.Pow(ddiv[0][2], 2)) * scale / dscale, digit).ToString("F").Substring(0, digit) + "mm");
                                    _dt.Add(Math.Round(Math.Sqrt(Math.Pow(ddiv[div][0], 2) + Math.Pow(ddiv[div][1], 2) + Math.Pow(ddiv[div][2], 2)) * scale / dscale, digit).ToString("F").Substring(0, digit) + "mm");
                                    _dt.Add(Math.Round(Math.Sqrt(Math.Pow(ddiv[(int)(div / 2)][0], 2) + Math.Pow(ddiv[(int)(div / 2)][1], 2) + Math.Pow(ddiv[(int)(div / 2)][2], 2)) * scale / dscale, digit).ToString("F").Substring(0, digit) + "mm");
                                }
                                if (Delta == 1)
                                {
                                    var delta_min = Math.Min(Math.Min(Math.Sqrt(Math.Pow(ddiv[0][0], 2) + Math.Pow(ddiv[0][1], 2) + Math.Pow(ddiv[0][2], 2)), Math.Sqrt(Math.Pow(ddiv[div][0], 2) + Math.Pow(ddiv[div][1], 2) + Math.Pow(ddiv[div][2], 2))), Math.Sqrt(Math.Pow(ddiv[(int)(div / 2)][0], 2) + Math.Pow(ddiv[(int)(div / 2)][1], 2) + Math.Pow(ddiv[(int)(div / 2)][2], 2))) / dscale; var delta_max = Math.Max(Math.Max(Math.Sqrt(Math.Pow(ddiv[0][0], 2) + Math.Pow(ddiv[0][1], 2) + Math.Pow(ddiv[0][2], 2)), Math.Sqrt(Math.Pow(ddiv[div][0], 2) + Math.Pow(ddiv[div][1], 2) + Math.Pow(ddiv[div][2], 2))), Math.Sqrt(Math.Pow(ddiv[(int)(div / 2)][0], 2) + Math.Pow(ddiv[(int)(div / 2)][1], 2) + Math.Pow(ddiv[(int)(div / 2)][2], 2))) / dscale;
                                    var rad = l / (delta_max - delta_min);
                                    if (rad < 1000) { _rad.Add("\n" + "1/" + ((int)rad).ToString()); }//たわみ角(1/1000以上のみ描画)
                                    else { _rad.Add(""); }
                                    var color = new ColorHSL((1 - Math.Min(Math.Max(1.0 / rad, 1.0 / 1000.0), 1.0 / 200.0) / (1.0 / 200.0)) * 1.9 / 3.0, 1, 0.5); _crad.Add(color);
                                    _prad.Add(rdiv[(int)(div / 2)]);
                                }
                            }
                            else if (on_off_21 == 1 && (Value == 1 || Delta == 1))
                            {
                                if (Value == 1)
                                {
                                    _dt.Add(Math.Round(Math.Sqrt(Math.Pow(ddiv[0][0], 2) + Math.Pow(ddiv[0][1], 2)) * scale / dscale, digit).ToString("F").Substring(0, digit) + "mm");
                                    _dt.Add(Math.Round(Math.Sqrt(Math.Pow(ddiv[div][0], 2) + Math.Pow(ddiv[div][1], 2)) * scale / dscale, digit).ToString("F").Substring(0, digit) + "mm");
                                    _dt.Add(Math.Round(Math.Sqrt(Math.Pow(ddiv[(int)(div / 2)][0], 2) + Math.Pow(ddiv[(int)(div / 2)][1], 2)) * scale / dscale, digit).ToString("F").Substring(0, digit) + "mm");
                                }
                                if (Delta == 1)
                                {
                                    var delta_min = Math.Min(Math.Min(Math.Sqrt(Math.Pow(ddiv[0][0], 2) + Math.Pow(ddiv[0][1], 2)), Math.Sqrt(Math.Pow(ddiv[div][0], 2) + Math.Pow(ddiv[div][1], 2))), Math.Sqrt(Math.Pow(ddiv[(int)(div / 2)][0], 2) + Math.Pow(ddiv[(int)(div / 2)][1], 2))) / dscale; var delta_max = Math.Max(Math.Max(Math.Sqrt(Math.Pow(ddiv[0][0], 2) + Math.Pow(ddiv[0][1], 2)), Math.Sqrt(Math.Pow(ddiv[div][0], 2) + Math.Pow(ddiv[div][1], 2))), Math.Sqrt(Math.Pow(ddiv[(int)(div / 2)][0], 2) + Math.Pow(ddiv[(int)(div / 2)][1], 2))) / dscale;
                                    var rad = l / (delta_max - delta_min);
                                    if (rad < 1000) { _rad.Add("\n" + "1/" + ((int)rad).ToString()); }//たわみ角(1/1000以上のみ描画)
                                    else { _rad.Add(""); }
                                    var color = new ColorHSL((1 - Math.Min(Math.Max(1.0 / rad, 1.0 / 1000.0), 1.0 / 200.0) / (1.0 / 200.0)) * 1.9 / 3.0, 1, 0.5); _crad.Add(color);
                                    _prad.Add(rdiv[(int)(div / 2)]);
                                }
                            }
                            else if (on_off_12 == 1 && (Value == 1 || Delta == 1))
                            {
                                if (Value == 1)
                                {
                                    var fugou = ""; if (ddiv[0][2] < 0) { fugou = "-"; }
                                    _dt.Add(fugou + Math.Round(Math.Abs(ddiv[0][2] * scale / dscale), digit).ToString("F").Substring(0, digit) + "mm");
                                    if (ddiv[div][2] < 0) { fugou = "-"; } else { fugou = ""; }
                                    _dt.Add(fugou + Math.Round(Math.Abs(ddiv[div][2] * scale / dscale), digit).ToString("F").Substring(0, digit) + "mm");
                                    if (ddiv[(int)(div / 2)][2] < 0) { fugou = "-"; } else { fugou = ""; }
                                    _dt.Add(fugou + Math.Round(Math.Abs(ddiv[(int)(div / 2)][2] * scale / dscale), digit).ToString("F").Substring(0, digit) + "mm");
                                }
                                if (Delta == 1)
                                {
                                    var delta_min = Math.Min(Math.Min(ddiv[0][2], ddiv[div][2]), ddiv[(int)(div / 2)][2]) / dscale; var delta_max = Math.Max(Math.Max(ddiv[0][2], ddiv[div][2]), ddiv[(int)(div / 2)][2]) / dscale;
                                    var rad = l / (delta_max - delta_min);
                                    if (rad < 1000) { _rad.Add("\n" + "1/" + ((int)rad).ToString()); }//たわみ角(1/1000以上のみ描画)
                                    else { _rad.Add(""); }
                                    var color = new ColorHSL((1 - Math.Min(Math.Max(1.0 / rad, 1.0 / 1000.0), 1.0 / 200.0) / (1.0 / 200.0)) * 1.9 / 3.0, 1, 0.5); _crad.Add(color);
                                    _prad.Add(rdiv[(int)(div / 2)]);
                                }
                            }
                            else if (on_off_22 == 1 && (Value == 1 || Delta == 1))
                            {
                                if (Value == 1)
                                {
                                    var fugou = ""; if (ddiv[0][0] < 0) { fugou = "-"; }
                                    _dt.Add(fugou + Math.Round(Math.Abs(ddiv[0][0] * scale / dscale), digit).ToString("F").Substring(0, digit) + "mm");
                                    if (ddiv[div][0] < 0) { fugou = "-"; } else { fugou = ""; }
                                    _dt.Add(fugou + Math.Round(Math.Abs(ddiv[div][0] * scale / dscale), digit).ToString("F").Substring(0, digit) + "mm");
                                    if (ddiv[(int)(div / 2)][0] < 0) { fugou = "-"; } else { fugou = ""; }
                                    _dt.Add(fugou + Math.Round(Math.Abs(ddiv[(int)(div / 2)][0] * scale / dscale), digit).ToString("F").Substring(0, digit) + "mm");
                                }
                                if (Delta == 1)
                                {
                                    var delta_min = Math.Min(Math.Min(ddiv[0][0], ddiv[div][0]), ddiv[(int)(div / 2)][0]) / dscale; var delta_max = Math.Max(Math.Max(ddiv[0][0], ddiv[div][0]), ddiv[(int)(div / 2)][0]) / dscale;
                                    var rad = l / (delta_max - delta_min);
                                    if (rad < 1000) { _rad.Add("\n" + "1/" + ((int)rad).ToString()); }//たわみ角(1/1000以上のみ描画)
                                    else { _rad.Add(""); }
                                    var color = new ColorHSL((1 - Math.Min(Math.Max(1.0 / rad, 1.0 / 1000.0), 1.0 / 200.0) / (1.0 / 200.0)) * 1.9 / 3.0, 1, 0.5); _crad.Add(color);
                                    _prad.Add(rdiv[(int)(div / 2)]);
                                }
                            }
                            else if (on_off_23 == 1 && (Value == 1 || Delta == 1))
                            {
                                if (Value == 1)
                                {
                                    var fugou = ""; if (ddiv[0][1] < 0) { fugou = "-"; }
                                    _dt.Add(fugou + Math.Round(Math.Abs(ddiv[0][1] * scale / dscale), digit).ToString("F").Substring(0, digit) + "mm");
                                    if (ddiv[div][1] < 0) { fugou = "-"; } else { fugou = ""; }
                                    _dt.Add(fugou + Math.Round(Math.Abs(ddiv[div][1] * scale / dscale), digit).ToString("F").Substring(0, digit) + "mm");
                                    if (ddiv[(int)(div / 2)][1] < 0) { fugou = "-"; } else { fugou = ""; }
                                    _dt.Add(fugou + Math.Round(Math.Abs(ddiv[(int)(div / 2)][1] * scale / dscale), digit).ToString("F").Substring(0, digit) + "mm");
                                }
                                if (Delta == 1)
                                {
                                    var delta_min = Math.Min(Math.Min(ddiv[0][1], ddiv[div][1]), ddiv[(int)(div / 2)][1]) / dscale; var delta_max = Math.Max(Math.Max(ddiv[0][1], ddiv[div][1]), ddiv[(int)(div / 2)][1]) / dscale;
                                    var rad = l / (delta_max - delta_min);
                                    if (rad < 1000) { _rad.Add("\n" + "1/" + ((int)rad).ToString()); }//たわみ角(1/1000以上のみ描画)
                                    else { _rad.Add(""); }
                                    var color = new ColorHSL((1 - Math.Min(Math.Max(1.0 / rad, 1.0 / 1000.0), 1.0 / 200.0) / (1.0 / 200.0)) * 1.9 / 3.0, 1, 0.5); _crad.Add(color);
                                    _prad.Add(rdiv[(int)(div / 2)]);
                                }
                            }
                        }
                    }
                }
                ///反力の描画****************************************************************************************
                var qvec = new Vector3d(1, 0, 0);
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
                            _c2.Add(Color.ForestGreen);
                            _arrow.Add(new Line(r2, r1));
                            if (Value == 1)
                            {
                                _c2.Add(Color.ForestGreen);
                                _value.Add(Math.Abs(reac_f[i][1].Value));
                                _pt.Add(r2);
                            }
                        }
                        if (Math.Abs(reac_f[i][2].Value) > 1e-10 && on_off2_12 == 1)
                        {
                            var r2 = new Point3d(r[j][0].Value, r[j][1].Value - reac_f[i][2].Value * nscale, r[j][2].Value);
                            _c2.Add(Color.ForestGreen);
                            _arrow.Add(new Line(r2, r1));
                            if (Value == 1)
                            {
                                _c2.Add(Color.ForestGreen);
                                _value.Add(Math.Abs(reac_f[i][2].Value));
                                _pt.Add(r2);
                            }
                        }
                        if (Math.Abs(reac_f[i][3].Value) > 1e-10 && on_off2_13 == 1)
                        {
                            var r2 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value - reac_f[i][3].Value * nscale);
                            _c2.Add(Color.ForestGreen);
                            _arrow.Add(new Line(r2, r1));
                            if (Value == 1)
                            {
                                _c2.Add(Color.ForestGreen);
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
                                _c2.Add(Color.ForestGreen);
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
                                _c2.Add(Color.ForestGreen);
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
                                _c2.Add(Color.ForestGreen);
                            }
                        }
                        qvec[0] += -reac_f[i][1].Value; qvec[1] += -reac_f[i][2].Value;
                    }
                }
                ///木造壁の描画****************************************************************************************
                List<double> shear_w = new List<double>();
                DA.GetDataTree("KABE_W", out GH_Structure<GH_Number> _kabe_w); DA.GetDataList("shear_w", shear_w);
                if (_kabe_w[0][0].Value != -9999 && shear_w[0] != -9999)
                {
                    ///**************************************************************************************************************
                    if (index3[0] != 9999)
                    {
                        var kabe_w = _kabe_w.Branches;
                        GH_Structure<GH_Number> kabe_w_new = new GH_Structure<GH_Number>(); List<double> shear_w_new = new List<double>();//指定indexのみ格納
                        if (index3[0] == -9999)
                        {
                            index3 = new List<double>();
                            for (int e = 0; e < kabe_w.Count; e++) { index3.Add(e); }
                        }
                        if (qvec[0] > qvec[1]) { qvec = new Vector3d(1, 0, 0); }
                        else { qvec = new Vector3d(0, 1, 0); }
                        for (int ind = 0; ind < index3.Count; ind++)
                        {
                            int e = (int)index3[ind];
                            double q = shear_w[e];
                            kabe_w_new.AppendRange(kabe_w[e], new GH_Path(ind)); shear_w_new.Add(q);
                            if (on_off3_31 == 1 && kabe_w[e][4].Value > 0)
                            {
                                int i = (int)kabe_w[e][0].Value; int j = (int)kabe_w[e][1].Value; int k = (int)kabe_w[e][2].Value; int l = (int)kabe_w[e][3].Value;
                                var ri = new Point3d(r[i][0].Value, r[i][1].Value, r[i][2].Value); var rj = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value); var rk = new Point3d(r[k][0].Value, r[k][1].Value, r[k][2].Value); var rl = new Point3d(r[l][0].Value, r[l][1].Value, r[l][2].Value);
                                var rc = (ri + rj + rk + rl) / 4.0; //var r1 = new Point3d(ri[0], ri[1], rc[2]); var r2 = new Point3d(rj[0], rj[1], rc[2]);
                                var vec = (rj - ri) * q * nscale / (rj - ri).Length;
                                if ((int)kabe_w[e][6].Value == 1) { vec = (rk - rj) * q * nscale / (rk - rj).Length; ; }
                                _kabew_p1.Add(rc - vec * 0.5); _kabew_p2.Add(rc + vec * 0.5);
                                if (Value == 1) { _kabeq.Add(q); }
                            }
                        }
                        DA.SetDataTree(3, kabe_w_new); DA.SetDataList("shear_w", shear_w_new);
                    }
                    else
                    {
                        List<GH_Number> w = new List<GH_Number>(); w.Add(new GH_Number(-9999));
                        var w_tree = new GH_Structure<GH_Number>(); w_tree.AppendRange(w, new GH_Path(0));
                        DA.SetDataTree(3, w_tree);
                        DA.SetDataList("shear_w", new List<double> { -9999 });
                    }
                }
                ///断面力の描画****************************************************************************************
                if (!DA.GetDataTree("section_force", out GH_Structure<GH_Number> _sec_f)) { }
                else if (_sec_f.Branches[0][0].Value != -9999)
                {
                    GH_Structure<GH_Number> sec_f_new = new GH_Structure<GH_Number>();//indexで指定した要素のみ
                    var l_vec = new List<Vector3d>();
                    if (!DA.GetDataList(5, l_vec)) { }
                    else if (l_vec[0] != new Vector3d(-9999, -9999, -9999))
                    {
                        var sec_f = _sec_f.Branches;
                        var Nmax = 0.0; var Nmin = 0.0; var Mxmax = 0.0; var Mymax = 0.0; var Mzmax = 0.0; var Qymax = 0.0; var Qzmax = 0.0;
                        for (int ind = 0; ind < index.Count; ind++)
                        {
                            int e = (int)index[ind];
                            for (int i = 0; i < 3; i++)
                            {
                                Qymax = Math.Max(Qymax, Math.Abs(sec_f[e][i * 6 + 1].Value) * nscale); Qzmax = Math.Max(Qzmax, Math.Abs(sec_f[e][i * 6 + 2].Value) * nscale);
                                Mxmax = Math.Max(Mxmax, Math.Abs(sec_f[e][i * 6 + 3].Value) * mscale); Mymax = Math.Max(Mymax, Math.Abs(sec_f[e][i * 6 + 4].Value) * mscale); Mzmax = Math.Max(Mzmax, Math.Abs(sec_f[e][i * 6 + 5].Value) * mscale);
                            }
                            Nmax = Math.Max(Nmax, Math.Max(sec_f[e][0].Value, Math.Max(-sec_f[e][6].Value, sec_f[e][12].Value)) * nscale);
                            Nmin = Math.Min(Nmin, Math.Min(sec_f[e][0].Value, Math.Min(-sec_f[e][6].Value, sec_f[e][12].Value)) * nscale);
                        }
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
                        if (on_off3_22 == 1 && on_off3_23 == 1) { Mymax = Math.Max(Mymax, Mzmax); Mzmax = Math.Max(Mymax, Mzmax); }
                        for (int ind = 0; ind < index.Count; ind++)
                        {
                            int e = (int)index[ind];
                            sec_f_new.AppendRange(sec_f[e], new GH_Path(ind));//indexで指定した要素のみ
                            var Ni = sec_f[e][0].Value; var Qyi = sec_f[e][1].Value; var Qzi = sec_f[e][2].Value; var Mxi = sec_f[e][3].Value; var Myi = sec_f[e][4].Value; var Mzi = sec_f[e][5].Value;
                            var Nj = sec_f[e][6].Value; var Qyj = sec_f[e][7].Value; var Qzj = sec_f[e][8].Value; var Mxj = sec_f[e][9].Value; var Myj = sec_f[e][10].Value; var Mzj = sec_f[e][11].Value;
                            var Nc = sec_f[e][12].Value; var Qyc = sec_f[e][13].Value; var Qzc = sec_f[e][14].Value; var Mxc = sec_f[e][15].Value; var Myc = sec_f[e][16].Value; var Mzc = sec_f[e][17].Value;
                            int n1 = (int)ij[e][0].Value; int n2 = (int)ij[e][1].Value;
                            var r1 = new Point3d(r[n1][0].Value, r[n1][1].Value, r[n1][2].Value); var r2 = new Point3d(r[n2][0].Value, r[n2][1].Value, r[n2][2].Value); var rc = (r1 + r2) / 2.0;
                            var element = new Line(r1, r2);
                            if (on_off3_22 == 1)///My
                            {
                                var l_ve = l_vec[e];
                                var p1 = r1 - l_ve * Myi * mscale; var p2 = rc - l_ve * Myc * mscale; var p3 = r2 + l_ve * Myj * mscale;
                                var curve = NurbsCurve.CreateInterpolatedCurve(new Point3d[] { p1, p2, p3 }, 3);
                                curve.DivideByCount(div, true, out Point3d[] pts);
                                var pts2 = new List<Point3d>(); var color3 = new ColorHSL(1.9 / 3.0, 1, 0.5);
                                for (int i = 0; i < pts.Length; i++) { pts2.Add(element.ClosestPoint(pts[i], true)); }
                                for (int i = 0; i < pts.Length - 1; i++)
                                {
                                    var vec1 = pts[i] - pts2[i]; var val1 = vec1.Length;
                                    if ((vec1 / vec1.Length - l_ve).Length < 1e-5) { val1 = -val1; }
                                    var color1 = new ColorHSL((1 - Math.Abs(val1) / Math.Max(1e-10, Mymax)) * 1.9 / 3.0, 1, 0.5);
                                    var vec2 = pts[i + 1] - pts2[i + 1]; var val2 = vec2.Length;
                                    if ((vec2 / vec2.Length - l_ve).Length < 1e-5) { val2 = -val2; }
                                    var color2 = new ColorHSL((1 - Math.Abs(val2) / Math.Max(1e-10, Mymax)) * 1.9 / 3.0, 1, 0.5);
                                    if (val1 * val2 >= 0)
                                    {
                                        var ptc1 = (pts[i] + pts[i + 1]) / 2.0; var ptc2 = (pts2[i] + pts2[i + 1]) / 2.0;
                                        var surf = NurbsSurface.CreateFromCorners(pts[i], ptc1, ptc2, pts2[i]);
                                        _surf.Add(surf); _cs.Add(color1);
                                        surf = NurbsSurface.CreateFromCorners(ptc1, pts[i + 1], pts2[i + 1], ptc2);
                                        _surf.Add(surf); _cs.Add(color2);
                                    }
                                    else
                                    {
                                        var ptc = pts2[i] + (pts2[i + 1] - pts2[i]) * Math.Abs(val1) / (Math.Abs(val1) + Math.Abs(val2));
                                        var surf = NurbsSurface.CreateFromCorners(pts[i], pts2[i], ptc);
                                        _surf.Add(surf); _cs.Add(color1);
                                        surf = NurbsSurface.CreateFromCorners(ptc, pts[i + 1], pts2[i + 1]);
                                        _surf.Add(surf); _cs.Add(color2);
                                    }
                                }
                                if (Value == 1)
                                {
                                    _pt2.Add(p1); _Mvalue.Add(Math.Abs(Myi)); _lunit.Add(unit_of_length); _funit.Add(unit_of_force);
                                    _pt2.Add(p2); _Mvalue.Add(Math.Abs(Myc)); _lunit.Add(unit_of_length); _funit.Add(unit_of_force);
                                    _pt2.Add(p3); _Mvalue.Add(Math.Abs(Myj)); _lunit.Add(unit_of_length); _funit.Add(unit_of_force);
                                }
                            }
                            if (on_off3_23 == 1)///Mz
                            {
                                var l_ve = rotation(l_vec[e], r2 - r1, 90);
                                var p1 = r1 - l_ve * Mzi * mscale; var p2 = rc - l_ve * Mzc * mscale; var p3 = r2 + l_ve * Mzj * mscale;
                                var curve = NurbsCurve.CreateInterpolatedCurve(new Point3d[] { p1, p2, p3 }, 3);
                                curve.DivideByCount(div, true, out Point3d[] pts);
                                var pts2 = new List<Point3d>(); var color3 = new ColorHSL(1.9 / 3.0, 1, 0.5);
                                for (int i = 0; i < pts.Length; i++) { pts2.Add(element.ClosestPoint(pts[i], true)); }
                                for (int i = 0; i < pts.Length - 1; i++)
                                {
                                    var vec1 = pts[i] - pts2[i]; var val1 = vec1.Length;
                                    if ((vec1 / vec1.Length - l_ve).Length < 1e-5) { val1 = -val1; }
                                    var color1 = new ColorHSL((1 - Math.Abs(val1) / Math.Max(1e-10, Mzmax)) * 1.9 / 3.0, 1, 0.5);
                                    var vec2 = pts[i + 1] - pts2[i + 1]; var val2 = vec2.Length;
                                    if ((vec2 / vec2.Length - l_ve).Length < 1e-5) { val2 = -val2; }
                                    var color2 = new ColorHSL((1 - Math.Abs(val2) / Math.Max(1e-10, Mzmax)) * 1.9 / 3.0, 1, 0.5);
                                    if (val1 * val2 >= 0)
                                    {
                                        var ptc1 = (pts[i] + pts[i + 1]) / 2.0; var ptc2 = (pts2[i] + pts2[i + 1]) / 2.0;
                                        var surf = NurbsSurface.CreateFromCorners(pts[i], ptc1, ptc2, pts2[i]);
                                        _surf.Add(surf); _cs.Add(color1);
                                        surf = NurbsSurface.CreateFromCorners(ptc1, pts[i + 1], pts2[i + 1], ptc2);
                                        _surf.Add(surf); _cs.Add(color2);
                                    }
                                    else
                                    {
                                        var ptc = pts2[i] + (pts2[i + 1] - pts2[i]) * Math.Abs(val1) / (Math.Abs(val1) + Math.Abs(val2));
                                        var surf = NurbsSurface.CreateFromCorners(pts[i], pts2[i], ptc);
                                        _surf.Add(surf); _cs.Add(color1);
                                        surf = NurbsSurface.CreateFromCorners(ptc, pts[i + 1], pts2[i + 1]);
                                        _surf.Add(surf); _cs.Add(color2);
                                    }
                                }
                                if (Value == 1)
                                {
                                    _pt2.Add(p1); _Mvalue.Add(Math.Abs(Mzi)); _lunit.Add(unit_of_length); _funit.Add(unit_of_force);
                                    _pt2.Add(p2); _Mvalue.Add(Math.Abs(Mzc)); _lunit.Add(unit_of_length); _funit.Add(unit_of_force);
                                    _pt2.Add(p3); _Mvalue.Add(Math.Abs(Mzj)); _lunit.Add(unit_of_length); _funit.Add(unit_of_force);
                                }
                            }
                            if (on_off3_12 == 1)///Qy
                            {
                                var l_ve = rotation(l_vec[e], r2 - r1, 90);
                                var p1 = r1 - l_ve * Qyi * nscale; var p2 = rc - l_ve * Qyc * nscale; var p3 = r2 + l_ve * Qyj * nscale;
                                var curve = NurbsCurve.CreateInterpolatedCurve(new Point3d[] { p1, p2, p3 }, 3);
                                curve.DivideByCount(div, true, out Point3d[] pts);
                                var pts2 = new List<Point3d>(); var color3 = new ColorHSL(1.9 / 3.0, 1, 0.5);
                                for (int i = 0; i < pts.Length; i++) { pts2.Add(element.ClosestPoint(pts[i], true)); }
                                for (int i = 0; i < pts.Length - 1; i++)
                                {
                                    var vec1 = pts[i] - pts2[i]; var val1 = vec1.Length;
                                    if ((vec1 / vec1.Length - l_ve).Length < 1e-5) { val1 = -val1; }
                                    var color1 = new ColorHSL((1 - Math.Abs(val1) / Math.Max(1e-10, Qymax)) * 1.9 / 3.0, 1, 0.5);
                                    var vec2 = pts[i + 1] - pts2[i + 1]; var val2 = vec2.Length;
                                    if ((vec2 / vec2.Length - l_ve).Length < 1e-5) { val2 = -val2; }
                                    var color2 = new ColorHSL((1 - Math.Abs(val2) / Math.Max(1e-10, Qymax)) * 1.9 / 3.0, 1, 0.5);
                                    if (val1 * val2 >= 0)
                                    {
                                        var ptc1 = (pts[i] + pts[i + 1]) / 2.0; var ptc2 = (pts2[i] + pts2[i + 1]) / 2.0;
                                        var surf = NurbsSurface.CreateFromCorners(pts[i], ptc1, ptc2, pts2[i]);
                                        _surf.Add(surf); _cs.Add(color1);
                                        surf = NurbsSurface.CreateFromCorners(ptc1, pts[i + 1], pts2[i + 1], ptc2);
                                        _surf.Add(surf); _cs.Add(color2);
                                    }
                                    else
                                    {
                                        var ptc = pts2[i] + (pts2[i + 1] - pts2[i]) * Math.Abs(val1) / (Math.Abs(val1) + Math.Abs(val2));
                                        var surf = NurbsSurface.CreateFromCorners(pts[i], pts2[i], ptc);
                                        _surf.Add(surf); _cs.Add(color1);
                                        surf = NurbsSurface.CreateFromCorners(ptc, pts[i + 1], pts2[i + 1]);
                                        _surf.Add(surf); _cs.Add(color2);
                                    }
                                }
                                if (Value == 1)
                                {
                                    _pt2.Add(p1); _Mvalue.Add(Math.Abs(Qyi)); _lunit.Add(""); _funit.Add(unit_of_force);
                                    _pt2.Add(p2); _Mvalue.Add(Math.Abs(Qyc)); _lunit.Add(""); _funit.Add(unit_of_force);
                                    _pt2.Add(p3); _Mvalue.Add(Math.Abs(Qyj)); _lunit.Add(""); _funit.Add(unit_of_force);
                                }
                            }
                            if (on_off3_13 == 1)///Qz
                            {
                                var l_ve = l_vec[e];
                                var p1 = r1 - l_ve * Qzi * nscale; var p2 = rc - l_ve * Qzc * nscale; var p3 = r2 + l_ve * Qzj * nscale;
                                var curve = NurbsCurve.CreateInterpolatedCurve(new Point3d[] { p1, p2, p3 }, 3);
                                curve.DivideByCount(div, true, out Point3d[] pts);
                                var pts2 = new List<Point3d>(); var color3 = new ColorHSL(1.9 / 3.0, 1, 0.5);
                                for (int i = 0; i < pts.Length; i++) { pts2.Add(element.ClosestPoint(pts[i], true)); }
                                for (int i = 0; i < pts.Length - 1; i++)
                                {
                                    var vec1 = pts[i] - pts2[i]; var val1 = vec1.Length;
                                    if ((vec1 / vec1.Length - l_ve).Length < 1e-5) { val1 = -val1; }
                                    var color1 = new ColorHSL((1 - Math.Abs(val1) / Math.Max(1e-10, Qzmax)) * 1.9 / 3.0, 1, 0.5);
                                    var vec2 = pts[i + 1] - pts2[i + 1]; var val2 = vec2.Length;
                                    if ((vec2 / vec2.Length - l_ve).Length < 1e-5) { val2 = -val2; }
                                    var color2 = new ColorHSL((1 - Math.Abs(val2) / Math.Max(1e-10, Qzmax)) * 1.9 / 3.0, 1, 0.5);
                                    if (val1 * val2 >= 0)
                                    {
                                        var ptc1 = (pts[i] + pts[i + 1]) / 2.0; var ptc2 = (pts2[i] + pts2[i + 1]) / 2.0;
                                        var surf = NurbsSurface.CreateFromCorners(pts[i], ptc1, ptc2, pts2[i]);
                                        _surf.Add(surf); _cs.Add(color1);
                                        surf = NurbsSurface.CreateFromCorners(ptc1, pts[i + 1], pts2[i + 1], ptc2);
                                        _surf.Add(surf); _cs.Add(color2);
                                    }
                                    else
                                    {
                                        var ptc = pts2[i] + (pts2[i + 1] - pts2[i]) * Math.Abs(val1) / (Math.Abs(val1) + Math.Abs(val2));
                                        var surf = NurbsSurface.CreateFromCorners(pts[i], pts2[i], ptc);
                                        _surf.Add(surf); _cs.Add(color1);
                                        surf = NurbsSurface.CreateFromCorners(ptc, pts[i + 1], pts2[i + 1]);
                                        _surf.Add(surf); _cs.Add(color2);
                                    }
                                }
                                if (Value == 1)
                                {
                                    _pt2.Add(p1); _Mvalue.Add(Math.Abs(Qzi)); _lunit.Add(""); _funit.Add(unit_of_force);
                                    _pt2.Add(p2); _Mvalue.Add(Math.Abs(Qzc)); _lunit.Add(""); _funit.Add(unit_of_force);
                                    _pt2.Add(p3); _Mvalue.Add(Math.Abs(Qzj)); _lunit.Add(""); _funit.Add(unit_of_force);
                                }
                            }
                            if (on_off3_11 == 1)///N
                            {
                                var l_ve = l_vec[e];
                                var p1 = r1 - l_ve * Ni * nscale; var p2 = rc - l_ve * Nc * nscale; var p3 = r2 + l_ve * Nj * nscale;
                                var curve = NurbsCurve.CreateInterpolatedCurve(new Point3d[] { p1, p2, p3 }, 3);
                                curve.DivideByCount(div, true, out Point3d[] pts);
                                var pts2 = new List<Point3d>(); var color3 = new ColorHSL(1.9 / 3.0, 1, 0.5);
                                for (int i = 0; i < pts.Length; i++) { pts2.Add(element.ClosestPoint(pts[i], true)); }
                                RhinoApp.WriteLine(Nmax.ToString() + " " + Nmin.ToString());
                                for (int i = 0; i < pts.Length - 1; i++)
                                {
                                    var vec1 = pts[i] - pts2[i]; var val1 = vec1.Length;
                                    if ((vec1 / vec1.Length - l_ve).Length < 1e-5) { val1 = -val1; }
                                    var color1 = new ColorHSL(Math.Abs(val1) / Math.Max(1e-10, Nmax) * 1.9 / 6.0 + 1.9 / 6.0, 1, 0.5);
                                    if (((val1 < 0 && Ni >= 0) || (Ni < 0)))
                                    {
                                        color1 = new ColorHSL((1 - Math.Abs(val1) / Math.Max(1e-10, Math.Abs(Nmin))) * 1.9 / 6.0, 1, 0.5);
                                    }
                                    var vec2 = pts[i + 1] - pts2[i + 1]; var val2 = vec2.Length;
                                    if ((vec2 / vec2.Length - l_ve).Length < 1e-5) { val2 = -val2; }
                                    var color2 = new ColorHSL(Math.Abs(val2) / Math.Max(1e-10, Nmax) * 1.9 / 6.0 + 1.9 / 6.0, 1, 0.5);
                                    if ((val2 < 0 && Ni >= 0) || (Ni < 0))
                                    {
                                        color2 = new ColorHSL((1 - Math.Abs(val2) / Math.Max(1e-10, Math.Abs(Nmin))) * 1.9 / 6.0, 1, 0.5);
                                    }
                                    if (val1 * val2 >= 0)
                                    {
                                        var ptc1 = (pts[i] + pts[i + 1]) / 2.0; var ptc2 = (pts2[i] + pts2[i + 1]) / 2.0;
                                        var surf = NurbsSurface.CreateFromCorners(pts[i], ptc1, ptc2, pts2[i]);
                                        _surf.Add(surf); _cs.Add(color1);
                                        surf = NurbsSurface.CreateFromCorners(ptc1, pts[i + 1], pts2[i + 1], ptc2);
                                        _surf.Add(surf); _cs.Add(color2);
                                    }
                                    else
                                    {
                                        var ptc = pts2[i] + (pts2[i + 1] - pts2[i]) * Math.Abs(val1) / (Math.Abs(val1) + Math.Abs(val2));
                                        var surf = NurbsSurface.CreateFromCorners(pts[i], pts2[i], ptc);
                                        _surf.Add(surf); _cs.Add(color1);
                                        surf = NurbsSurface.CreateFromCorners(ptc, pts[i + 1], pts2[i + 1]);
                                        _surf.Add(surf); _cs.Add(color2);
                                    }
                                }
                                if (Value == 1)
                                {
                                    _pt2.Add(p1); _Mvalue.Add(Ni); _lunit.Add(""); _funit.Add(unit_of_force);
                                    _pt2.Add(p2); _Mvalue.Add(Nc); _lunit.Add(""); _funit.Add(unit_of_force);
                                    _pt2.Add(p3); _Mvalue.Add(-Nj); _lunit.Add(""); _funit.Add(unit_of_force);
                                }
                            }
                            if (on_off3_21 == 1)///Mx
                            {
                                var l_ve = l_vec[e];
                                var p1 = r1 - l_ve * Mxi * nscale; var p2 = rc - l_ve * Mxc * nscale; var p3 = r2 + l_ve * Mxj * nscale;
                                var curve = NurbsCurve.CreateInterpolatedCurve(new Point3d[] { p1, p2, p3 }, 3);
                                curve.DivideByCount(div, true, out Point3d[] pts);
                                var pts2 = new List<Point3d>(); var color3 = new ColorHSL(1.9 / 3.0, 1, 0.5);
                                for (int i = 0; i < pts.Length; i++) { pts2.Add(element.ClosestPoint(pts[i], true)); }
                                for (int i = 0; i < pts.Length - 1; i++)
                                {
                                    var vec1 = pts[i] - pts2[i]; var val1 = vec1.Length;
                                    if ((vec1 / vec1.Length - l_ve).Length < 1e-5) { val1 = -val1; }
                                    var color1 = new ColorHSL((1 - Math.Abs(val1) / Math.Max(1e-10, Mxmax)) * 1.9 / 3.0, 1, 0.5);
                                    var vec2 = pts[i + 1] - pts2[i + 1]; var val2 = vec2.Length;
                                    if ((vec2 / vec2.Length - l_ve).Length < 1e-5) { val2 = -val2; }
                                    var color2 = new ColorHSL((1 - Math.Abs(val2) / Math.Max(1e-10, Mxmax)) * 1.9 / 3.0, 1, 0.5);
                                    if (val1 * val2 >= 0)
                                    {
                                        var ptc1 = (pts[i] + pts[i + 1]) / 2.0; var ptc2 = (pts2[i] + pts2[i + 1]) / 2.0;
                                        var surf = NurbsSurface.CreateFromCorners(pts[i], ptc1, ptc2, pts2[i]);
                                        _surf.Add(surf); _cs.Add(color1);
                                        surf = NurbsSurface.CreateFromCorners(ptc1, pts[i + 1], pts2[i + 1], ptc2);
                                        _surf.Add(surf); _cs.Add(color2);
                                    }
                                    else
                                    {
                                        var ptc = pts2[i] + (pts2[i + 1] - pts2[i]) * Math.Abs(val1) / (Math.Abs(val1) + Math.Abs(val2));
                                        var surf = NurbsSurface.CreateFromCorners(pts[i], pts2[i], ptc);
                                        _surf.Add(surf); _cs.Add(color1);
                                        surf = NurbsSurface.CreateFromCorners(ptc, pts[i + 1], pts2[i + 1]);
                                        _surf.Add(surf); _cs.Add(color2);
                                    }
                                }
                                if (Value == 1)
                                {
                                    _pt2.Add(p1); _Mvalue.Add(Math.Abs(Mxi)); _lunit.Add(""); _funit.Add(unit_of_force);
                                    _pt2.Add(p2); _Mvalue.Add(Math.Abs(Mxc)); _lunit.Add(""); _funit.Add(unit_of_force);
                                    _pt2.Add(p3); _Mvalue.Add(Math.Abs(Mxj)); _lunit.Add(""); _funit.Add(unit_of_force);
                                }
                            }
                        }
                        DA.SetDataTree(2, sec_f_new);
                        //DA.SetDataList("geometry", _surf);
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
                return OpenSeesUtility.Properties.Resources.VisResult2;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("39dcef05-8da7-4257-b324-e7fc8e01a7fc"); }
        }///ここからカスタム関数群********************************************************************************
        private readonly List<Line> _l = new List<Line>();
        private readonly List<Color> _c = new List<Color>();
        private readonly List<Color> _c2 = new List<Color>();
        private readonly List<Color> _c3 = new List<Color>();
        private readonly List<Color> _cs = new List<Color>();
        private readonly List<Line> _arrow = new List<Line>();
        private readonly List<double> _value = new List<double>();
        private readonly List<double> _value2 = new List<double>();
        private readonly List<Point3d> _pt = new List<Point3d>();
        private readonly List<Point3d> _pt0 = new List<Point3d>();
        private readonly List<Point3d> _pt1 = new List<Point3d>();
        private readonly List<Point3d> _pt2 = new List<Point3d>();
        private readonly List<string> _lunit = new List<string>();
        private readonly List<string> _funit = new List<string>();
        private readonly List<Arc> _arc = new List<Arc>();
        private readonly List<Vector3d> _vec = new List<Vector3d>();
        private readonly List<Mesh> _mesh = new List<Mesh>();
        private readonly List<Surface> _surf = new List<Surface>();
        private readonly List<double> _Nvalue = new List<double>();
        private readonly List<double> _Tvalue = new List<double>();
        private readonly List<Point3d> _p1 = new List<Point3d>();
        private readonly List<Point3d> _p2 = new List<Point3d>();
        private readonly List<double> _Mvalue = new List<double>();
        private readonly List<double> _Qvalue = new List<double>();
        private readonly List<string> _dt = new List<string>();
        private readonly List<string> _rad = new List<string>();//たわみ角
        private readonly List<Color> _crad = new List<Color>();
        private readonly List<Point3d> _prad = new List<Point3d>();
        private readonly List<double> _kabeq = new List<double>();
        private readonly List<Point3d> _kabew_p1 = new List<Point3d>();
        private readonly List<Point3d> _kabew_p2 = new List<Point3d>();
        protected override void BeforeSolveInstance()
        {
            _l.Clear();
            _c.Clear();
            _c2.Clear();
            _c3.Clear();
            _cs.Clear();
            _arrow.Clear();
            _value.Clear();
            _value2.Clear();
            _pt.Clear();
            _pt1.Clear();
            _pt2.Clear();
            _lunit.Clear();
            _funit.Clear();
            _arc.Clear();
            _vec.Clear();
            _mesh.Clear();
            _surf.Clear();
            _Nvalue.Clear();
            _Tvalue.Clear();
            _Mvalue.Clear();
            _Qvalue.Clear();
            _pt0.Clear();
            _p1.Clear();
            _p2.Clear();
            _dt.Clear();
            _rad.Clear();
            _crad.Clear();
            _prad.Clear();
            _kabeq.Clear();
            _kabew_p1.Clear();
            _kabew_p2.Clear();
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            args.Viewport.GetFrustumFarPlane(out Plane plane);
            RhinoViewport viewport = args.Viewport;
            ///軸力描画用関数*******************************************************************************
            for (int i = 0; i < _Nvalue.Count; i++)
            {
                var l1 = _p2[i] - _p1[i]; var l2 = _p1[i] - _p2[i];
                args.Display.DrawLine(_p1[i], _p2[i], _c3[i]);
                if (_Nvalue[i] > 0)
                {
                    args.Display.DrawArrowHead(_p1[i], l1, _c3[i], 25, 0); args.Display.DrawArrowHead(_p2[i], l2, _c3[i], 25, 0);
                }
                else
                {
                    args.Display.DrawArrowHead(_p1[i], l2, _c3[i], 25, 0); args.Display.DrawArrowHead(_p2[i], l1, _c3[i], 25, 0);
                }
                if (Value == 1)
                {
                    double size = fontsize;
                    Point3d point = (_p1[i] + _p2[i]) / 2.0;
                    plane.Origin = point;
                    viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                    Text3d drawText = new Text3d(_Nvalue[i].ToString("F").Substring(0, digit) + _funit[i] + _lunit[i], plane, size);
                    args.Display.Draw3dText(drawText, _c3[i]); drawText.Dispose();
                }
            }
            ///モーメントとせん断力の描画用関数*************************************************************************
            for (int i = 0; i < _mesh.Count; i++)
            {
                args.Display.DrawMeshFalseColors(_mesh[i]);
            }
            for (int i = 0; i < _Mvalue.Count; i++)
            {
                double size = fontsize;
                Point3d point = _pt2[i];
                plane.Origin = point;
                viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                Text3d drawText = new Text3d(_Mvalue[i].ToString("F").Substring(0, digit) + _funit[i] + _lunit[i], plane, size);
                args.Display.Draw3dText(drawText, Color.MediumVioletRed); drawText.Dispose();
            }
            for (int i = 0; i < _surf.Count; i++)
            {
                args.Display.DrawSurface(_surf[i], _cs[i], 1);
            }
            ///木造壁せん断力の描画用関数*******************************************************************************
            for (int i = 0; i < _kabew_p1.Count; i++)
            {
                var l1 = _kabew_p2[i] - _kabew_p1[i]; var l2 = _kabew_p1[i] - _kabew_p2[i];
                args.Display.DrawLine(_kabew_p1[i], _kabew_p2[i], Color.Chocolate);
                args.Display.DrawArrowHead(_kabew_p1[i], l2, Color.Chocolate, 25, 0); args.Display.DrawArrowHead(_kabew_p2[i], l1, Color.Chocolate, 25, 0);
                if (Value == 1)
                {
                    double size = fontsize; Point3d point = (_kabew_p1[i] + _kabew_p2[i]) / 2.0; plane.Origin = point;
                    viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                    Text3d drawText = new Text3d(_kabeq[i].ToString("F").Substring(0, digit) + unit_of_force, plane, size);
                    args.Display.Draw3dText(drawText, Color.Chocolate); drawText.Dispose();
                }
            }
            ///*************************************************************************************************
            ///変位の描画用関数*******************************************************************************
            for (int i = 0; i < _l.Count; i++)
            {
                args.Display.DrawLine(_l[i], _c[i]);
            }
            for (int i = 0; i < _dt.Count; i++)
            {
                double size = fontsize;
                Point3d point = _pt1[i];
                plane.Origin = point;
                viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                Text3d drawText = new Text3d(_dt[i], plane, size);
                args.Display.Draw3dText(drawText, Color.Black); drawText.Dispose();
            }
            ///たわみの描画用関数*******************************************************************************
            for (int i = 0; i < _rad.Count; i++)
            {
                double size = fontsize;
                Point3d point = _prad[i];
                plane.Origin = point;
                viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                Text3d drawText = new Text3d(_rad[i], plane, size);
                args.Display.Draw3dText(drawText, _crad[i]); drawText.Dispose();
            }
            ///軸外力の描画用関数*******************************************************************************
            for (int i = 0; i < _arrow.Count; i++)
            {
                Line arrow = _arrow[i];
                args.Display.DrawLine(arrow, _c2[i], 2);
                args.Display.DrawArrowHead(arrow.To, arrow.Direction, _c2[i], 25, 0);
            }
            for (int i = 0; i < _value.Count; i++)
            {
                double size = fontsize;
                Point3d point = _pt[i];
                plane.Origin = point;
                viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                Text3d drawText = new Text3d(_value[i].ToString("F").Substring(0, digit) + unit_of_force, plane, size);
                args.Display.Draw3dText(drawText, _c2[i]); drawText.Dispose();
            }
            ///*************************************************************************************************
            ///モーメント外力の描画用関数***********************************************************************
            for (int i = 0; i < _arc.Count; i++)
            {
                args.Display.DrawArc(_arc[i], _c2[i], 2);
                args.Display.DrawArrowHead(_arc[i].EndPoint, _vec[i], _c2[i], 25, 0);
            }
            for (int i = 0; i < _value2.Count; i++)
            {
                Point3d point = _pt0[i];
                double size = fontsize;
                plane.Origin = point;
                viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                Text3d drawText = new Text3d(_value2[i].ToString("F") + unit_of_force + unit_of_length, plane, size);
                args.Display.Draw3dText(drawText, _c2[i]); drawText.Dispose();
            }
            ///*************************************************************************************************
        }///ここからGUIの作成*****************************************************************************************
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
            private Rectangle radio_rec3_21; private Rectangle text_rec3_21; private Rectangle radio_rec3_22; private Rectangle text_rec3_22; private Rectangle radio_rec3_23; private Rectangle text_rec3_23; private Rectangle radio_rec3_31; private Rectangle text_rec3_31;
            private Rectangle radio_rec4_11; private Rectangle text_rec4_11; private Rectangle radio_rec4_12; private Rectangle text_rec4_12;

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

                radio_rec3_31 = radio_rec3_21;
                radio_rec3_31.Y += text_rec3_21.Height - radi1;
                radio_rec3_31.Height = radi1; radio_rec3_21.Width = radi1;

                text_rec3_31 = radio_rec3_31;
                text_rec3_31.X += pitchx; text_rec3_31.Y -= radi2;
                text_rec3_31.Height = textheight; text_rec3_31.Width = subwidth;

                radio3_rec.Height = text_rec3_31.Y + textheight - radio3_rec.Y - radi2;
                ///******************************************************************************************
                radio4_rec = radio3_rec; radio4_rec.Height = textheight - radi2;
                radio4_rec.Y += radio3_rec.Height;

                radio_rec4_11 = radio4_rec;
                radio_rec4_11.X += radi2 - 1; radio_rec4_11.Y += radi2;
                radio_rec4_11.Height = radi1; radio_rec4_11.Width = radi1;

                text_rec4_11 = radio_rec4_11;
                text_rec4_11.X += pitchx; text_rec4_11.Y -= radi2;
                text_rec4_11.Height = textheight; text_rec4_11.Width = subwidth + 30;

                radio_rec4_12 = radio_rec4_11; radio_rec4_12.X = text_rec4_11.X + text_rec4_11.Width + 5;

                text_rec4_12 = radio_rec4_12;
                text_rec4_12.X += pitchx; text_rec4_12.Y -= radi2;
                text_rec4_12.Height = textheight; text_rec4_12.Width = subwidth + 30;

                Bounds = global_rec;
            }
            Brush c11 = Brushes.White; Brush c12 = Brushes.White; Brush c21 = Brushes.White; Brush c22 = Brushes.White; Brush c23 = Brushes.White;
            Brush c211 = Brushes.White; Brush c212 = Brushes.White; Brush c213 = Brushes.White; Brush c221 = Brushes.White; Brush c222 = Brushes.White; Brush c223 = Brushes.White;
            Brush c311 = Brushes.White; Brush c312 = Brushes.White; Brush c313 = Brushes.White; Brush c321 = Brushes.White; Brush c322 = Brushes.White; Brush c323 = Brushes.White;
            Brush c331 = Brushes.White;
            Brush c411 = Brushes.White; Brush c412 = Brushes.White;
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

                    GH_Capsule radio3_31 = GH_Capsule.CreateCapsule(radio_rec3_31, GH_Palette.Black, 5, 5);
                    radio3_31.Render(graphics, Selected, Owner.Locked, false); radio3_31.Dispose();
                    graphics.FillEllipse(c331, radio_rec3_31);
                    graphics.DrawString("Qkabe", GH_FontServer.Standard, Brushes.Black, text_rec3_31);
                    ///******************************************************************************************
                    GH_Capsule radio4 = GH_Capsule.CreateCapsule(radio4_rec, GH_Palette.White, 2, 0);
                    radio4.Render(graphics, Selected, Owner.Locked, false); radio4.Dispose();

                    GH_Capsule radio4_11 = GH_Capsule.CreateCapsule(radio_rec4_11, GH_Palette.Black, 5, 5);
                    radio4_11.Render(graphics, Selected, Owner.Locked, false); radio4_11.Dispose();
                    graphics.FillEllipse(c411, radio_rec4_11);
                    graphics.DrawString("Value", GH_FontServer.Standard, Brushes.Black, text_rec4_11);

                    GH_Capsule radio4_12 = GH_Capsule.CreateCapsule(radio_rec4_12, GH_Palette.Black, 5, 5);
                    radio4_12.Render(graphics, Selected, Owner.Locked, false); radio4_12.Dispose();
                    graphics.FillEllipse(c412, radio_rec4_12);
                    graphics.DrawString("D/H", GH_FontServer.Standard, Brushes.Black, text_rec4_12);
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
                    RectangleF rec331 = radio_rec3_31;
                    RectangleF rec411 = radio_rec4_11; RectangleF rec412 = radio_rec4_12;
                    if (rec11.Contains(e.CanvasLocation))
                    {
                        if (c11 == Brushes.Black) { c11 = Brushes.White; SetButton("c11", 0); }
                        else if ((c21 == Brushes.White && c22 == Brushes.White && c23 == Brushes.White) || (c12 == Brushes.White && c22 == Brushes.White && c23 == Brushes.White) || (c12 == Brushes.White && c21 == Brushes.White && c23 == Brushes.White) || (c12 == Brushes.White && c21 == Brushes.White && c22 == Brushes.White))
                        { c11 = Brushes.Black; c12 = Brushes.White; c21 = Brushes.White; c22 = Brushes.White; c23 = Brushes.White; SetButton("c11", 1); SetButton("c12", 0); SetButton("c21", 0); SetButton("c22", 0); SetButton("c23", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec12.Contains(e.CanvasLocation))
                    {
                        if (c12 == Brushes.Black) { c12 = Brushes.White; SetButton("c12", 0); }
                        else if ((c21 == Brushes.White && c22 == Brushes.White && c23 == Brushes.White) || (c11 == Brushes.White && c22 == Brushes.White && c23 == Brushes.White) || (c11 == Brushes.White && c21 == Brushes.White && c23 == Brushes.White) || (c11 == Brushes.White && c21 == Brushes.White && c22 == Brushes.White))
                        { c11 = Brushes.White; c12 = Brushes.Black; c21 = Brushes.White; c22 = Brushes.White; c23 = Brushes.White; SetButton("c11", 0); SetButton("c12", 1); SetButton("c21", 0); SetButton("c22", 0); SetButton("c23", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec21.Contains(e.CanvasLocation))
                    {
                        if (c21 == Brushes.Black) { c21 = Brushes.White; SetButton("c21", 0); }
                        else if ((c11 == Brushes.White && c22 == Brushes.White && c23 == Brushes.White) || (c12 == Brushes.White && c22 == Brushes.White && c23 == Brushes.White) || (c12 == Brushes.White && c11 == Brushes.White && c23 == Brushes.White) || (c12 == Brushes.White && c11 == Brushes.White && c22 == Brushes.White))
                        { c11 = Brushes.White; c12 = Brushes.White; c21 = Brushes.Black; c22 = Brushes.White; c23 = Brushes.White; SetButton("c11", 0); SetButton("c12", 0); SetButton("c21", 1); SetButton("c22", 0); SetButton("c23", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec22.Contains(e.CanvasLocation))
                    {
                        if (c22 == Brushes.Black) { c22 = Brushes.White; SetButton("c22", 0); }
                        else if ((c21 == Brushes.White && c11 == Brushes.White && c23 == Brushes.White) || (c12 == Brushes.White && c11 == Brushes.White && c23 == Brushes.White) || (c12 == Brushes.White && c21 == Brushes.White && c23 == Brushes.White) || (c12 == Brushes.White && c21 == Brushes.White && c11 == Brushes.White))
                        { c11 = Brushes.White; c12 = Brushes.White; c21 = Brushes.White; c22 = Brushes.Black; c23 = Brushes.White; SetButton("c11", 0); SetButton("c12", 0); SetButton("c21", 0); SetButton("c22", 1); SetButton("c23", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec23.Contains(e.CanvasLocation))
                    {
                        if (c23 == Brushes.Black) { c23 = Brushes.White; SetButton("c23", 0); }
                        else if ((c21 == Brushes.White && c22 == Brushes.White && c11 == Brushes.White) || (c12 == Brushes.White && c22 == Brushes.White && c11 == Brushes.White) || (c12 == Brushes.White && c21 == Brushes.White && c11 == Brushes.White) || (c12 == Brushes.White && c21 == Brushes.White && c22 == Brushes.White))
                        { c11 = Brushes.White; c12 = Brushes.White; c21 = Brushes.White; c22 = Brushes.White; c23 = Brushes.Black; SetButton("c11", 0); SetButton("c12", 0); SetButton("c21", 0); SetButton("c22", 0); SetButton("c23", 1); }
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
                        else { c311 = Brushes.Black; SetButton("c311", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec312.Contains(e.CanvasLocation))
                    {
                        if (c312 == Brushes.Black) { c312 = Brushes.White; SetButton("c312", 0); }
                        else { c312 = Brushes.Black; SetButton("c312", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec313.Contains(e.CanvasLocation))
                    {
                        if (c313 == Brushes.Black) { c313 = Brushes.White; SetButton("c313", 0); }
                        else { c313 = Brushes.Black; SetButton("c313", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec321.Contains(e.CanvasLocation))
                    {
                        if (c321 == Brushes.Black) { c321 = Brushes.White; SetButton("c321", 0); }
                        else { c321 = Brushes.Black; SetButton("c321", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec322.Contains(e.CanvasLocation))
                    {
                        if (c322 == Brushes.Black) { c322 = Brushes.White; SetButton("c322", 0); }
                        else { c322 = Brushes.Black; SetButton("c322", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec323.Contains(e.CanvasLocation))
                    {
                        if (c323 == Brushes.Black) { c323 = Brushes.White; SetButton("c323", 0); }
                        else { c323 = Brushes.Black; SetButton("c323", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec331.Contains(e.CanvasLocation))
                    {
                        if (c331 == Brushes.Black) { c331 = Brushes.White; SetButton("c331", 0); }
                        else { c331 = Brushes.Black; SetButton("c331", 1); }
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
                    if (rec412.Contains(e.CanvasLocation))
                    {
                        if (c412 == Brushes.Black) { c412 = Brushes.White; SetButton("Delta", 0); }
                        else { c412 = Brushes.Black; SetButton("Delta", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}