using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Display;

using System.Drawing;
using System.Windows.Forms;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
///****************************************

namespace SteelCheck
{
    public class SteelCheck : GH_Component
    {
        public static int on_off_11 = 1; public static int on_off_12 = 0; public static double fontsize;
        public static int on_off_21 = 0; public static int on_off_22 = 0; public static int on_off_23 = 0; public static int on_off_24 = 0;
        public static void SetButton(string s, int i)
        {
            if (s == "11")
            {
                on_off_11 = i;
            }
            else if (s == "12")
            {
                on_off_12 = i;
            }
            else if (s == "21")
            {
                on_off_21 = i;
            }
            else if (s == "22")
            {
                on_off_22 = i;
            }
            else if (s == "23")
            {
                on_off_23 = i;
            }
            else if (s == "24")
            {
                on_off_24 = i;
            }
        }
        public SteelCheck()
          : base("Allowable stress design for steel elements", "SteelCheck",
              "Allowable stress design(danmensantei) for steel elements using Japanese Design Code",
              "OpenSees", "Analysis")
        {
        }

        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("Young's mudulus", "E", "[...](DataList)", GH_ParamAccess.list, new List<double> { 2.1e+8 });///
            pManager.AddNumberParameter("section area", "A", "[...](DataList)", GH_ParamAccess.list, new List<double> { 0.01 });///
            pManager.AddNumberParameter("Second moment of area around y-axis", "Iy", "[...](DataList)", GH_ParamAccess.list, new List<double> { Math.Pow(0.1, 4) / 12.0 });///
            pManager.AddNumberParameter("Second moment of area around z-axis", "Iz", "[...](DataList)", GH_ParamAccess.list, new List<double> { Math.Pow(0.1, 4) / 12.0 });///
            pManager.AddNumberParameter("Section modulus around y-axis", "Zy", "[...](DataList)", GH_ParamAccess.list, new List<double> { Math.Pow(0.01, 3) / 6.0 });///
            pManager.AddNumberParameter("Section modulus around z-axis", "Zz", "[...](DataList)", GH_ParamAccess.list, new List<double> { Math.Pow(0.01, 3) / 6.0 });///
            pManager.AddTextParameter("section type", "S", "[■●□〇HL[](DataList)", GH_ParamAccess.list, new List<string> { "□" });///
            pManager.AddNumberParameter("Yield stress[N/mm2]", "F", "[...](DataList)[N/mm2]", GH_ParamAccess.list, new List<double> { 235.0 });///
            pManager.AddNumberParameter("Lby", "Lby", "[...](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddNumberParameter("Lbz", "Lbz", "[...](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddNumberParameter("sectional_force", "sec_f", "[[Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("index", "index", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("fontsize", "FS", "font size for display texts", GH_ParamAccess.item, 12.0);///
        }
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("index", "index", "[...](element No. List to show)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("IJ", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("lambda", "lambda", "elongation ratio[...](DataList)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("A", "A", "[...](DataList)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("Zy", "Zy", "[...](DataList)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("Zz", "Zz", "[...](DataList)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("fc", "fc", "[[Long-Terrm...],[Short-Terrm...]](DataTree)[N/mm2]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("ft", "ft", "[[Long-Terrm...],[Short-Terrm...]](DataTree)[N/mm2]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("fby", "fby", "[[Long-Terrm...],[Short-Terrm...]](DataTree)[N/mm2]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("fbz", "fbz", "[[Long-Terrm...],[Short-Terrm...]](DataTree)[N/mm2]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("fs", "fs", "[[Long-Terrm...],[Short-Terrm...]](DataTree)[N/mm2]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("sec_f", "sec_f", "[[Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("kentei_hi", "kentei", "[[for Ni,for Qyi,for Qzi,for Myi,for Mzi,for Nj,for Qyj,for Qzj,for Myj,for Mzj,for Nc,for Qyc,for Qzc,for Myc,for Mzc],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddLineParameter("lines", "BEAM", "Line of elements", GH_ParamAccess.list);
            pManager.AddNumberParameter("kentei(max)", "kentei(max)", "[[element.No, long-term, short-term],...]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("kmax", "kmax", "[[ele. No.,Long-term max],[ele. No.,Short-term max]](DataTree)", GH_ParamAccess.tree);///
        }
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Line> lines = new List<Line>();
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r);
            DA.GetDataTree("element_node_relationship", out GH_Structure<GH_Number> _ij); var ij_new = new GH_Structure<GH_Number>();
            DA.GetDataTree("sectional_force", out GH_Structure<GH_Number> _sec_f); var sec_f_new = new GH_Structure<GH_Number>();
            fontsize = 20.0; DA.GetData("fontsize", ref fontsize);
            List<double> E = new List<double>(); DA.GetDataList("Young's mudulus", E);
            List<double> A = new List<double>(); DA.GetDataList("section area", A); DA.SetDataList("A", A);
            List<double> Iy = new List<double>(); DA.GetDataList("Second moment of area around y-axis", Iy);
            List<double> Iz = new List<double>(); DA.GetDataList("Second moment of area around z-axis", Iz);
            List<double> Zy = new List<double>(); DA.GetDataList("Section modulus around y-axis", Zy); DA.SetDataList("Zy", Zy);
            List<double> Zz = new List<double>(); DA.GetDataList("Section modulus around z-axis", Zz); DA.SetDataList("Zz", Zz);
            List<string> S = new List<string>(); DA.GetDataList("section type", S);
            List<double> F = new List<double>(); DA.GetDataList("Yield stress[N/mm2]", F);
            List<double> Lby = new List<double>(); DA.GetDataList("Lby", Lby);
            List<double> Lbz = new List<double>(); DA.GetDataList("Lbz", Lbz);
            List<double> lambda = new List<double>();
            var r = _r.Branches; var ij = _ij.Branches; var sec_f = _sec_f.Branches;
            List<double> fc = new List<double>(); List<double> ft = new List<double>(); List<double> fby = new List<double>(); List<double> fbz = new List<double>(); List<double> fs = new List<double>();
            List<double> fc2 = new List<double>(); List<double> ft2 = new List<double>(); List<double> fby2 = new List<double>(); List<double> fbz2 = new List<double>(); List<double> fs2 = new List<double>();
            var kentei = new GH_Structure<GH_Number>(); var kentei_max = new GH_Structure<GH_Number>(); int digit = 4;
            var unit = 1.0;///単位合わせのための係数
            unit /= 1000000.0;
            unit *= 1000.0;
            List<double> index = new List<double>(); DA.GetDataList("index", index);
            if (index[0] == -9999)
            {
                index = new List<double>();
                for (int e = 0; e < ij.Count; e++) { index.Add(e); }
            }
            DA.SetDataList("index", index);
            if (r[0][0].Value != -9999 && ij[0][0].Value != -9999 && sec_f[0][0].Value != -9999)
            {
                if (Lby[0] == -9999)//未入力の場合は座屈長さは部材長とする
                {
                    Lby = new List<double>();
                    for (int e = 0; e < ij.Count; e++)
                    {
                        var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                        var l = Math.Sqrt(Math.Pow(r[nj][0].Value - r[ni][0].Value, 2) + Math.Pow(r[nj][1].Value - r[ni][1].Value, 2) + Math.Pow(r[nj][2].Value - r[ni][2].Value, 2));
                        Lby.Add(l);
                    }
                }
                if (Lbz[0] == -9999)//未入力の場合は座屈長さは部材長とする
                {
                    Lbz = new List<double>();
                    for (int e = 0; e < ij.Count; e++)
                    {
                        var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                        var l = Math.Sqrt(Math.Pow(r[nj][0].Value - r[ni][0].Value, 2) + Math.Pow(r[nj][1].Value - r[ni][1].Value, 2) + Math.Pow(r[nj][2].Value - r[ni][2].Value, 2));
                        Lbz.Add(l);
                    }
                }
                if (sec_f[0][0].Value != -9999)
                {
                    var kmax1 = new List<double>(); var kmax2 = new List<double>();
                    int m2 = sec_f[0].Count;
                    for (int ind = 0; ind < index.Count; ind++)
                    {
                        int e = (int)index[ind]; kmax1.Add(0.0); kmax2.Add(0.0);
                        int mat = (int)ij[e][2].Value; int sec = (int)ij[e][3].Value;
                        var iy = Iy[sec]; var iz = Iz[sec]; var a = A[sec];
                        var iby = Math.Sqrt(iy / a); var ibz = Math.Sqrt(iz / a);
                        var lam = Math.Max(Lby[e] / iby, Lbz[e] / ibz); lambda.Add(lam);
                        var zy = Zy[sec]; var zz = Zz[sec];
                        var f = F[mat]; var yng = E[mat];
                        var Lam = Math.Sqrt(Math.Pow(Math.PI, 2) * yng * unit / 0.6 / f);
                        var nu = 3.0 / 2.0 + 2.0 / 3.0 * Math.Pow(lam / Lam, 2);
                        ft.Add(f / 1.5); fs.Add(f / 1.5 / Math.Sqrt(3.0)); ft2.Add(ft[ind] * 1.5); fs2.Add(fs[ind] * 1.5);
                        if (lam <= Lam) { fc.Add((1 - 0.4 * Math.Pow(lam / Lam, 2)) * f / nu); fc2.Add(fc[ind] * 1.5); }
                        else { fc.Add(0.277 * f / Math.Pow(lam / Lam, 2)); fc2.Add(fc[ind] * 1.5); }

                        if (S[sec] == "■" || S[sec] == "1" || S[sec] == "●" || S[sec] == "2" || S[sec] == "□" || S[sec] == "3" || S[sec] == "▢" || S[sec] == "〇" || S[sec] == "4" || S[sec] == "○" || S[sec] == "6" || S[sec] == "L" || S[sec] == "7" || S[sec] == "[" || S[sec] == "コ") { fby.Add(f / 1.5); fbz.Add(f / 1.5); fby2.Add(fby[ind] * 1.5); fbz2.Add(fbz[ind] * 1.5); }
                        else if (S[sec] == "H" || S[sec] == "5")
                        {
                            var h = 2 * Iy[sec] / Zy[sec]; var b = 2 * Iz[sec] / Zz[sec];
                            var tw = (-A[sec] * Math.Sqrt(12.0 * A[sec] * Iy[sec] - A[sec] * b * Math.Pow(h, 3) - 12.0 * Iy[sec] * b * h + Math.Pow(b, 2) * Math.Pow(h, 4)) + 12.0 * Iy[sec] * b - Math.Pow(b, 2) * Math.Pow(h, 3) + b * h * Math.Sqrt(12.0 * A[sec] * Iy[sec] - A[sec] * b * Math.Pow(h, 3) - 12.0 * Iy[sec] * b * h + Math.Pow(b, 2) * Math.Pow(h, 4))) / (12.0 * Iy[sec] - b * Math.Pow(h, 3));
                            var tf = 0.5 * h - 0.5 * Math.Sqrt((-A[sec] + b * h) * (-12.0 * Iy[sec] + b * Math.Pow(h, 3))) / (-A[sec] + b * h);
                            var Af = tf * b; var Aw = tw * (h - 2 * tf);
                            var C = 1.0;
                            var fb1 = (1.0 - 0.4 * Math.Pow((Lbz[e] / (ibz / 2.0)), 2) / C / Math.Pow(Lam, 2)) * f; var fb2 = 89000 / (Lbz[e] * h / Af);
                            if (Iy[sec] > Iz[sec]) { fby.Add(Math.Min(f / 1.5, Math.Max(fb1, fb2))); fbz.Add(f / 1.5); fby2.Add(fby[ind] * 1.5); fbz2.Add(fbz[ind] * 1.5); }
                            else { fby.Add(f / 1.5); fbz.Add(Math.Min(f / 1.5, Math.Max(fb1, fb2))); fby2.Add(fby[ind] * 1.5); fbz2.Add(fbz[ind] * 1.5); }
                        }
                        List<GH_Number> klist = new List<GH_Number>();//=[0:sigma_c or sigma_t, 1:tau_y, 2:tau_z, 3:sigma_by, 4:sigma_zy, 5:sigma_c or sigma_t, 6:tau_y, 7:tau_z, 8:sigma_by, 9:sigma_zy, 10:sigma_c or sigma_t, 11:tau_y, 12:tau_z, 13:sigma_by, 14:sigma_zy]
                        var Ni = -sec_f[e][0].Value; var Qyi = Math.Abs(sec_f[e][1].Value); var Qzi = Math.Abs(sec_f[e][2].Value);
                        var Myi = Math.Abs(sec_f[e][4].Value); var Mzi = Math.Abs(sec_f[e][5].Value);
                        var Nj = sec_f[e][6].Value; var Qyj = Math.Abs(sec_f[e][7].Value); var Qzj = Math.Abs(sec_f[e][8].Value);
                        var Myj = Math.Abs(sec_f[e][10].Value); var Mzj = Math.Abs(sec_f[e][11].Value);
                        var Nc = -sec_f[e][12].Value; var Qyc = Math.Abs(sec_f[e][13].Value); var Qzc = Math.Abs(sec_f[e][14].Value);
                        var Myc = Math.Abs(sec_f[e][16].Value); var Mzc = Math.Abs(sec_f[e][17].Value);

                        if (Ni < 0) { klist.Add(new GH_Number(Math.Abs(Ni) / a / fc[ind] * unit)); }
                        else { klist.Add(new GH_Number(Math.Abs(Ni) / a / ft[ind] * unit)); }
                        klist.Add(new GH_Number(Qyi / a / fs[ind] * unit)); klist.Add(new GH_Number(Qzi / a / fs[ind] * unit));
                        klist.Add(new GH_Number(Myi / zy / fby[ind] * unit)); klist.Add(new GH_Number(Mzi / zz / fbz[ind] * unit));
                        if (Nj < 0) { klist.Add(new GH_Number(Math.Abs(Nj) / a / fc[ind] * unit)); }
                        else { klist.Add(new GH_Number(Math.Abs(Nj) / a / ft[ind] * unit)); }
                        klist.Add(new GH_Number(Qyj / a / fs[ind] * unit)); klist.Add(new GH_Number(Qzj / a / fs[ind] * unit));
                        klist.Add(new GH_Number(Myj / zy / fby[ind] * unit)); klist.Add(new GH_Number(Mzj / zz / fbz[ind] * unit));
                        if (Nc < 0) { klist.Add(new GH_Number(Math.Abs(Nc) / a / fc[ind] * unit)); }
                        else { klist.Add(new GH_Number(Math.Abs(Nc) / a / ft[ind] * unit)); }
                        klist.Add(new GH_Number(Qyc / a / fs[ind] * unit)); klist.Add(new GH_Number(Qzc / a / fs[ind] * unit));
                        klist.Add(new GH_Number(Myc / zy / fby[ind] * unit)); klist.Add(new GH_Number(Mzc / zz / fbz[ind] * unit));
                        var flist = new List<GH_Number>();
                        for (int i = 0; i < 18; i++) { flist.Add(new GH_Number(sec_f[e][i].Value)); }
                        kentei.AppendRange(klist, new GH_Path(new int[] { 0, ind }));
                        sec_f_new.AppendRange(flist, new GH_Path(new int[] { 0, ind })); ij_new.AppendRange(ij[e], new GH_Path(ind));
                        var kmaxlist = new List<GH_Number>();
                        if (S[sec] == "〇" || S[sec] == "4" || S[sec] == "○" || S[sec] == "●" || S[sec] == "2")
                        {
                            var k1 = Math.Max(Math.Sqrt(klist[3].Value * klist[3].Value + klist[4].Value * klist[4].Value) + klist[0].Value, Math.Sqrt(klist[1].Value * klist[1].Value + klist[2].Value * klist[2].Value));
                            var k2 = Math.Max(Math.Sqrt(klist[8].Value * klist[8].Value + klist[9].Value * klist[9].Value) + klist[5].Value, Math.Sqrt(klist[6].Value * klist[6].Value + klist[7].Value * klist[7].Value));
                            var k3 = Math.Max(Math.Sqrt(klist[13].Value * klist[13].Value + klist[14].Value * klist[14].Value) + klist[10].Value, Math.Sqrt(klist[11].Value * klist[11].Value + klist[12].Value * klist[12].Value));
                            kmaxlist.Add(new GH_Number(k1)); kmaxlist.Add(new GH_Number(k2)); kmaxlist.Add(new GH_Number(k3));
                        }
                        else
                        {
                            var k1 = Math.Max(klist[3].Value + klist[4].Value + klist[0].Value, klist[1].Value + klist[2].Value);
                            var k2 = Math.Max(klist[8].Value + klist[9].Value + klist[5].Value, klist[6].Value + klist[7].Value);
                            var k3 = Math.Max(klist[13].Value + klist[14].Value + klist[10].Value, klist[11].Value + klist[12].Value);
                            kmaxlist.Add(new GH_Number(k1)); kmaxlist.Add(new GH_Number(k2)); kmaxlist.Add(new GH_Number(k3));
                        }
                        for (int i = 0; i < kmaxlist.Count; i++) { kmax1[ind] = Math.Max(kmax1[ind], kmaxlist[i].Value); }
                        if (on_off_11 == 1)
                        {
                            var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                            var r1 = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value); var r2 = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value);
                            var rc = (r1 + r2) / 2.0; var ri = (r1 + rc) / 2.0; var rj = (r2 + rc) / 2.0;
                            if (on_off_21 == 1)
                            {
                                if (S[sec] == "〇" || S[sec] == "4" || S[sec] == "○" || S[sec] == "●" || S[sec] == "2")
                                {
                                    var k = Math.Sqrt(klist[3].Value * klist[3].Value + klist[4].Value * klist[4].Value) + klist[0].Value;//i端の検定比
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(ri);
                                    var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Sqrt(klist[8].Value * klist[8].Value + klist[9].Value * klist[9].Value) + klist[5].Value;//j端の検定比
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(rj);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Sqrt(klist[13].Value * klist[13].Value + klist[14].Value * klist[14].Value) + klist[10].Value;//中央の検定比
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(rc);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                }
                                else
                                {
                                    var k = klist[3].Value + klist[4].Value + klist[0].Value;//i端の検定比
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(ri);
                                    var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = klist[8].Value + klist[9].Value + klist[5].Value;//j端の検定比
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(rj);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = klist[13].Value + klist[14].Value + klist[10].Value;//中央の検定比
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(rc);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                }
                            }
                            else if (on_off_22 == 1)
                            {
                                if (S[sec] == "〇" || S[sec] == "4" || S[sec] == "○" || S[sec] == "●" || S[sec] == "2")
                                {
                                    var k = Math.Sqrt(klist[1].Value * klist[1].Value + klist[2].Value * klist[2].Value);
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(ri);
                                    var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Sqrt(klist[6].Value * klist[6].Value + klist[7].Value * klist[7].Value);
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(rj);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Sqrt(klist[11].Value * klist[11].Value + klist[12].Value * klist[12].Value);
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(rc);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                }
                                else
                                {
                                    var k = klist[1].Value + klist[2].Value;
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(ri);
                                    var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = klist[6].Value + klist[7].Value;
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(rj);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = klist[11].Value + klist[12].Value;
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(rc);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                }
                            }
                            else if (on_off_23 == 1)
                            {
                                if (S[sec] == "〇" || S[sec] == "4" || S[sec] == "○" || S[sec] == "●" || S[sec] == "2")
                                {
                                    var k = Math.Max(Math.Sqrt(klist[3].Value * klist[3].Value + klist[4].Value * klist[4].Value) + klist[0].Value, Math.Sqrt(klist[1].Value * klist[1].Value + klist[2].Value * klist[2].Value));
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(ri);
                                    var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(Math.Sqrt(klist[8].Value * klist[8].Value + klist[9].Value * klist[9].Value) + klist[5].Value, Math.Sqrt(klist[6].Value * klist[6].Value + klist[7].Value * klist[7].Value));
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(rj);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(Math.Sqrt(klist[13].Value * klist[13].Value + klist[14].Value * klist[14].Value) + klist[10].Value, Math.Sqrt(klist[11].Value * klist[11].Value + klist[12].Value * klist[12].Value));
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(rc);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                }
                                else
                                {
                                    var k = Math.Max(klist[3].Value + klist[4].Value + klist[0].Value, klist[1].Value + klist[2].Value);
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(ri);
                                    var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(klist[8].Value + klist[9].Value + klist[5].Value, klist[6].Value + klist[7].Value);
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(rj);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(klist[13].Value + klist[14].Value + klist[10].Value, klist[11].Value + klist[12].Value);
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(rc);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                }
                            }
                            else if (on_off_24 == 1)
                            {
                                var k = lam;
                                _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                _p.Add(rc);
                                var color = Color.Crimson;
                                _c.Add(color);
                            }
                        }
                    }
                    if (m2 / 18 == 3)
                    {
                        for (int ind = 0; ind < index.Count; ind++)
                        {
                            int e = (int)index[ind];
                            int sec = (int)ij[e][3].Value;
                            var iy = Iy[sec]; var iz = Iz[sec]; var a = A[sec];
                            var iby = Math.Sqrt(iy / a); var ibz = Math.Sqrt(iz / a);
                            var lam = Math.Max(Lby[e] / iby, Lbz[e] / ibz);
                            var zy = Zy[sec]; var zz = Zz[sec];
                            var k1list = new List<GH_Number>(); var k2list = new List<GH_Number>(); var k3list = new List<GH_Number>(); var k4list = new List<GH_Number>();
                            //=[0:sigma_c or sigma_t, 1:tau_y, 2:tau_z, 3:sigma_by, 4:sigma_zy, 5:sigma_c or sigma_t, 6:tau_y, 7:tau_z, 8:sigma_by, 9:sigma_zy, 10:sigma_c or sigma_t, 11:tau_y, 12:tau_z, 13:sigma_by, 14:sigma_zy]
                            var Ni = -sec_f[e][0].Value; var Qyi = Math.Abs(sec_f[e][1].Value); var Qzi = Math.Abs(sec_f[e][2].Value);
                            var Myi = Math.Abs(sec_f[e][4].Value); var Mzi = Math.Abs(sec_f[e][5].Value);
                            var Nj = sec_f[e][6].Value; var Qyj = Math.Abs(sec_f[e][7].Value); var Qzj = Math.Abs(sec_f[e][8].Value);
                            var Myj = Math.Abs(sec_f[e][10].Value); var Mzj = Math.Abs(sec_f[e][11].Value);
                            var Nc = -sec_f[e][12].Value; var Qyc = Math.Abs(sec_f[e][13].Value); var Qzc = Math.Abs(sec_f[e][14].Value);
                            var Myc = Math.Abs(sec_f[e][16].Value); var Mzc = Math.Abs(sec_f[e][17].Value);
                            var NXi = -sec_f[e][18 + 0].Value; var QXyi = Math.Abs(sec_f[e][18 + 1].Value); var QXzi = Math.Abs(sec_f[e][18 + 2].Value);
                            var MXyi = Math.Abs(sec_f[e][18 + 4].Value); var MXzi = Math.Abs(sec_f[e][18 + 5].Value);
                            var NXj = sec_f[e][18 + 6].Value; var QXyj = Math.Abs(sec_f[e][18 + 7].Value); var QXzj = Math.Abs(sec_f[e][18 + 8].Value);
                            var MXyj = Math.Abs(sec_f[e][18 + 10].Value); var MXzj = Math.Abs(sec_f[e][18 + 11].Value);
                            var NXc = -sec_f[e][18 + 12].Value; var QXyc = Math.Abs(sec_f[e][18 + 13].Value); var QXzc = Math.Abs(sec_f[e][18 + 14].Value);
                            var MXyc = Math.Abs(sec_f[e][18 + 16].Value); var MXzc = Math.Abs(sec_f[e][18 + 17].Value);
                            var NYi = -sec_f[e][18 * 2 + 0].Value; var QYyi = Math.Abs(sec_f[e][18 * 2 + 1].Value); var QYzi = Math.Abs(sec_f[e][18 * 2 + 2].Value);
                            var MYyi = Math.Abs(sec_f[e][18 * 2 + 4].Value); var MYzi = Math.Abs(sec_f[e][18 * 2 + 5].Value);
                            var NYj = sec_f[e][18 * 2 + 6].Value; var QYyj = Math.Abs(sec_f[e][18 * 2 + 7].Value); var QYzj = Math.Abs(sec_f[e][18 * 2 + 8].Value);
                            var MYyj = Math.Abs(sec_f[e][18 * 2 + 10].Value); var MYzj = Math.Abs(sec_f[e][18 * 2 + 11].Value);
                            var NYc = -sec_f[e][18 * 2 + 12].Value; var QYyc = Math.Abs(sec_f[e][18 * 2 + 13].Value); var QYzc = Math.Abs(sec_f[e][18 * 2 + 14].Value);
                            var MYyc = Math.Abs(sec_f[e][18 * 2 + 16].Value); var MYzc = Math.Abs(sec_f[e][18 * 2 + 17].Value);

                            if (Ni + NXi < 0) { k1list.Add(new GH_Number(Math.Abs(Ni + NXi) / a / fc2[ind] * unit)); }
                            else { k1list.Add(new GH_Number(Math.Abs(Ni + NXi) / a / ft2[ind] * unit)); }
                            k1list.Add(new GH_Number((Qyi + QXyi) / a / fs2[ind] * unit)); k1list.Add(new GH_Number((Qzi + QXzi) / a / fs2[ind] * unit));
                            k1list.Add(new GH_Number((Myi + MXyi) / zy / fby2[ind] * unit)); k1list.Add(new GH_Number((Mzi + MXzi) / zz / fbz2[ind] * unit));
                            if (Nj + NXj < 0) { k1list.Add(new GH_Number(Math.Abs(Nj + NXj) / a / fc2[ind] * unit)); }
                            else { k1list.Add(new GH_Number(Math.Abs(Nj + NXj) / a / ft2[ind] * unit)); }
                            k1list.Add(new GH_Number((Qyj + QXyj) / a / fs2[ind] * unit)); k1list.Add(new GH_Number((Qzj + QXzj) / a / fs2[ind] * unit));
                            k1list.Add(new GH_Number((Myj + MXyj) / zy / fby2[ind] * unit)); k1list.Add(new GH_Number((Mzj + MXzj) / zz / fbz2[ind] * unit));
                            if (Nc + NXc < 0) { k1list.Add(new GH_Number(Math.Abs(Nc + NXc) / a / fc2[ind] * unit)); }
                            else { k1list.Add(new GH_Number(Math.Abs(Nc + NXc) / a / ft2[ind] * unit)); }
                            k1list.Add(new GH_Number((Qyc + QXyc) / a / fs2[ind] * unit)); k1list.Add(new GH_Number((Qzc + QXzc) / a / fs2[ind] * unit));
                            k1list.Add(new GH_Number((Myc + MXyc) / zy / fby2[ind] * unit)); k1list.Add(new GH_Number((Mzc + MXzc) / zz / fbz2[ind] * unit));
                            var flist = new List<GH_Number>(); for (int i = 0; i < 18; i++) { flist.Add(new GH_Number(sec_f[e][18 + i].Value)); }
                            kentei.AppendRange(k1list, new GH_Path(new int[] { 1, ind })); sec_f_new.AppendRange(flist, new GH_Path(new int[] { 1, ind }));

                            if (Ni + NYi < 0) { k2list.Add(new GH_Number(Math.Abs(Ni + NYi) / a / fc2[ind] * unit)); }
                            else { k2list.Add(new GH_Number(Math.Abs(Ni + NYi) / a / ft2[ind] * unit)); }
                            k2list.Add(new GH_Number((Qyi + QYyi) / a / fs2[ind] * unit)); k2list.Add(new GH_Number((Qzi + QYzi) / a / fs2[ind] * unit));
                            k2list.Add(new GH_Number((Myi + MYyi) / zy / fby2[ind] * unit)); k2list.Add(new GH_Number((Mzi + MYzi) / zz / fbz2[ind] * unit));
                            if (Nj + NYj < 0) { k2list.Add(new GH_Number(Math.Abs(Nj + NYj) / a / fc2[ind] * unit)); }
                            else { k2list.Add(new GH_Number(Math.Abs(Nj + NYj) / a / ft2[ind] * unit)); }
                            k2list.Add(new GH_Number((Qyj + QYyj) / a / fs2[ind] * unit)); k2list.Add(new GH_Number((Qzj + QYzj) / a / fs2[ind] * unit));
                            k2list.Add(new GH_Number((Myj + MYyj) / zy / fby2[ind] * unit)); k2list.Add(new GH_Number((Mzj + MYzj) / zz / fbz2[ind] * unit));
                            if (Nc + NYc < 0) { k2list.Add(new GH_Number(Math.Abs(Nc + NYc) / a / fc2[ind] * unit)); }
                            else { k2list.Add(new GH_Number(Math.Abs(Nc + NYc) / a / ft2[ind] * unit)); }
                            k2list.Add(new GH_Number((Qyc + QYyc) / a / fs2[ind] * unit)); k2list.Add(new GH_Number((Qzc + QYzc) / a / fs2[ind] * unit));
                            k2list.Add(new GH_Number((Myc + MYyc) / zy / fby2[ind] * unit)); k2list.Add(new GH_Number((Mzc + MYzc) / zz / fbz2[ind] * unit));
                            flist = new List<GH_Number>(); for (int i = 0; i < 18; i++) { flist.Add(new GH_Number(sec_f[e][18 * 2 + i].Value)); }
                            kentei.AppendRange(k2list, new GH_Path(new int[] { 2, ind })); sec_f_new.AppendRange(flist, new GH_Path(new int[] { 2, ind }));

                            if (Ni - NXi < 0) { k3list.Add(new GH_Number(Math.Abs(Ni - NXi) / a / fc2[ind] * unit)); }
                            else { k3list.Add(new GH_Number(Math.Abs(Ni - NXi) / a / ft2[ind] * unit)); }
                            k3list.Add(new GH_Number(Math.Abs(Qyi - QXyi) / a / fs2[ind] * unit)); k3list.Add(new GH_Number(Math.Abs(Qzi - QXzi) / a / fs2[ind] * unit));
                            k3list.Add(new GH_Number(Math.Abs(Myi - MXyi) / zy / fby2[ind] * unit)); k3list.Add(new GH_Number(Math.Abs(Mzi - MXzi) / zz / fbz2[ind] * unit));
                            if (Nj - NXj < 0) { k3list.Add(new GH_Number(Math.Abs(Nj - NXj) / a / fc2[ind] * unit)); }
                            else { k3list.Add(new GH_Number(Math.Abs(Nj - NXj) / a / ft2[ind] * unit)); }
                            k3list.Add(new GH_Number(Math.Abs(Qyj - QXyj) / a / fs2[ind] * unit)); k3list.Add(new GH_Number(Math.Abs(Qzj - QXzj) / a / fs2[ind] * unit));
                            k3list.Add(new GH_Number(Math.Abs(Myj - MXyj) / zy / fby2[ind] * unit)); k3list.Add(new GH_Number(Math.Abs(Mzj - MXzj) / zz / fbz2[ind] * unit));
                            if (Nc - NXc < 0) { k3list.Add(new GH_Number(Math.Abs(Nc + NXc) / a / fc2[ind] * unit)); }
                            else { k3list.Add(new GH_Number(Math.Abs(Nc - NXc) / a / ft2[ind] * unit)); }
                            k3list.Add(new GH_Number(Math.Abs(Qyc - QXyc) / a / fs2[ind] * unit)); k3list.Add(new GH_Number(Math.Abs(Qzc - QXzc) / a / fs2[ind] * unit));
                            k3list.Add(new GH_Number(Math.Abs(Myc - MXyc) / zy / fby2[ind] * unit)); k3list.Add(new GH_Number(Math.Abs(Mzc - MXzc) / zz / fbz2[ind] * unit));
                            flist = new List<GH_Number>();
                            for (int i = 0; i < 18; i++) { flist.Add(new GH_Number(-sec_f[e][18 + i].Value)); }
                            kentei.AppendRange(k3list, new GH_Path(new int[] { 3, ind })); sec_f_new.AppendRange(flist, new GH_Path(new int[] { 3, ind }));

                            if (Ni - NYi < 0) { k4list.Add(new GH_Number(Math.Abs(Ni - NYi) / a / fc2[ind] * unit)); }
                            else { k4list.Add(new GH_Number(Math.Abs(Ni - NYi) / a / ft2[ind] * unit)); }
                            k4list.Add(new GH_Number(Math.Abs(Qyi - QYyi) / a / fs2[ind] * unit)); k4list.Add(new GH_Number(Math.Abs(Qzi - QYzi) / a / fs2[ind] * unit));
                            k4list.Add(new GH_Number(Math.Abs(Myi - MYyi) / zy / fby2[ind] * unit)); k4list.Add(new GH_Number(Math.Abs(Mzi - MYzi) / zz / fbz2[ind] * unit));
                            if (Nj - NYj < 0) { k4list.Add(new GH_Number(Math.Abs(Nj - NYj) / a / fc2[ind] * unit)); }
                            else { k4list.Add(new GH_Number(Math.Abs(Nj - NYj) / a / ft2[ind] * unit)); }
                            k4list.Add(new GH_Number(Math.Abs(Qyj - QYyj) / a / fs2[ind] * unit)); k4list.Add(new GH_Number(Math.Abs(Qzj - QYzj) / a / fs2[ind] * unit));
                            k4list.Add(new GH_Number(Math.Abs(Myj - MYyj) / zy / fby2[ind] * unit)); k4list.Add(new GH_Number(Math.Abs(Mzj - MYzj) / zz / fbz2[ind] * unit));
                            if (Nc - NYc < 0) { k4list.Add(new GH_Number(Math.Abs(Nc + NYc) / a / fc2[ind] * unit)); }
                            else { k4list.Add(new GH_Number(Math.Abs(Nc - NYc) / a / ft2[ind] * unit)); }
                            k4list.Add(new GH_Number(Math.Abs(Qyc - QYyc) / a / fs2[ind] * unit)); k4list.Add(new GH_Number(Math.Abs(Qzc - QYzc) / a / fs2[ind] * unit));
                            k4list.Add(new GH_Number(Math.Abs(Myc - MYyc) / zy / fby2[ind] * unit)); k4list.Add(new GH_Number(Math.Abs(Mzc - MYzc) / zz / fbz2[ind] * unit));
                            flist = new List<GH_Number>();
                            for (int i = 0; i < 18; i++) { flist.Add(new GH_Number(-sec_f[e][18 * 2 + i].Value)); }
                            kentei.AppendRange(k4list, new GH_Path(new int[] { 4, ind })); sec_f_new.AppendRange(flist, new GH_Path(new int[] { 4, ind }));
                            var kmaxlist = new List<GH_Number>();
                            if (S[sec] == "〇" || S[sec] == "4" || S[sec] == "○" || S[sec] == "●" || S[sec] == "2")
                            {
                                var k_1 = Math.Max(Math.Sqrt(k1list[3].Value * k1list[3].Value + k1list[4].Value * k1list[4].Value) + k1list[0].Value, Math.Sqrt(k1list[1].Value * k1list[1].Value + k1list[2].Value * k1list[2].Value));
                                var k_2 = Math.Max(Math.Sqrt(k2list[3].Value * k2list[3].Value + k2list[4].Value * k2list[4].Value) + k2list[0].Value, Math.Sqrt(k2list[1].Value * k2list[1].Value + k2list[2].Value * k2list[2].Value));
                                var k_3 = Math.Max(Math.Sqrt(k3list[3].Value * k3list[3].Value + k3list[4].Value * k3list[4].Value) + k3list[0].Value, Math.Sqrt(k3list[1].Value * k3list[1].Value + k3list[2].Value * k3list[2].Value));
                                var k_4 = Math.Max(Math.Sqrt(k4list[3].Value * k4list[3].Value + k4list[4].Value * k4list[4].Value) + k4list[0].Value, Math.Sqrt(k4list[1].Value * k4list[1].Value + k4list[2].Value * k4list[2].Value));
                                var k1 = Math.Max(Math.Max(k_1, k_2), Math.Max(k_3, k_4));
                                k_1 = Math.Max(Math.Sqrt(k1list[8].Value * k1list[8].Value + k1list[9].Value * k1list[9].Value) + k1list[5].Value, Math.Sqrt(k1list[6].Value * k1list[6].Value + k1list[7].Value * k1list[7].Value));
                                k_2 = Math.Max(Math.Sqrt(k2list[8].Value * k2list[8].Value + k2list[9].Value * k2list[9].Value) + k2list[5].Value, Math.Sqrt(k2list[6].Value * k2list[6].Value + k2list[7].Value * k2list[7].Value));
                                k_3 = Math.Max(Math.Sqrt(k3list[8].Value * k3list[8].Value + k3list[9].Value * k3list[9].Value) + k3list[5].Value, Math.Sqrt(k3list[6].Value * k3list[6].Value + k3list[7].Value * k3list[7].Value));
                                k_4 = Math.Max(Math.Max(k4list[8].Value, k4list[9].Value) + k4list[5].Value, Math.Max(Math.Max(k4list[6].Value, k4list[7].Value), k4list[5].Value));
                                var k2 = Math.Max(Math.Max(k_1, k_2), Math.Max(k_3, k_4));
                                k_1 = Math.Max(Math.Sqrt(k1list[13].Value * k1list[13].Value + k1list[14].Value * k1list[14].Value) + k1list[10].Value, Math.Sqrt(k1list[11].Value * k1list[11].Value + k1list[12].Value * k1list[12].Value));
                                k_2 = Math.Max(Math.Sqrt(k2list[13].Value * k2list[13].Value + k2list[14].Value * k2list[14].Value) + k2list[10].Value, Math.Sqrt(k2list[11].Value * k2list[11].Value + k2list[12].Value * k2list[12].Value));
                                k_3 = Math.Max(Math.Sqrt(k3list[13].Value * k3list[13].Value + k3list[14].Value * k3list[14].Value) + k3list[10].Value, Math.Sqrt(k3list[11].Value * k3list[11].Value + k3list[12].Value * k3list[12].Value));
                                k_4 = Math.Max(Math.Sqrt(k4list[13].Value * k4list[13].Value + k4list[14].Value * k4list[14].Value) + k4list[10].Value, Math.Sqrt(k4list[11].Value * k4list[11].Value + k4list[12].Value * k4list[12].Value));
                                var k3 = Math.Max(Math.Max(k_1, k_2), Math.Max(k_3, k_4));
                                kmaxlist.Add(new GH_Number(k1)); kmaxlist.Add(new GH_Number(k2)); kmaxlist.Add(new GH_Number(k3));
                            }
                            else
                            {
                                var k_1 = Math.Max(k1list[3].Value + k1list[4].Value + k1list[0].Value, k1list[1].Value + k1list[2].Value);
                                var k_2 = Math.Max(k2list[3].Value + k2list[4].Value + k2list[0].Value, k2list[1].Value + k2list[2].Value);
                                var k_3 = Math.Max(k3list[3].Value + k3list[4].Value + k3list[0].Value, k3list[1].Value + k3list[2].Value);
                                var k_4 = Math.Max(k4list[3].Value + k4list[4].Value + k4list[0].Value, k4list[1].Value + k4list[2].Value);
                                var k1 = Math.Max(Math.Max(k_1, k_2), Math.Max(k_3, k_4));
                                k_1 = Math.Max(k1list[8].Value + k1list[9].Value + k1list[5].Value, k1list[6].Value + k1list[7].Value);
                                k_2 = Math.Max(k2list[8].Value + k2list[9].Value + k2list[5].Value, k2list[6].Value + k2list[7].Value);
                                k_3 = Math.Max(k3list[8].Value + k3list[9].Value + k3list[5].Value, k3list[6].Value + k3list[7].Value);
                                k_4 = Math.Max(Math.Max(k4list[8].Value, k4list[9].Value) + k4list[5].Value, Math.Max(Math.Max(k4list[6].Value, k4list[7].Value), k4list[5].Value));
                                var k2 = Math.Max(Math.Max(k_1, k_2), Math.Max(k_3, k_4));
                                k_1 = Math.Max(k1list[13].Value + k1list[14].Value + k1list[10].Value, k1list[11].Value + k1list[12].Value);
                                k_2 = Math.Max(k2list[13].Value + k2list[14].Value + k2list[10].Value, k2list[11].Value + k2list[12].Value);
                                k_3 = Math.Max(k3list[13].Value + k3list[14].Value + k3list[10].Value, k3list[11].Value + k3list[12].Value);
                                k_4 = Math.Max(k4list[13].Value + k4list[14].Value + k4list[10].Value, k4list[11].Value + k4list[12].Value);
                                var k3 = Math.Max(Math.Max(k_1, k_2), Math.Max(k_3, k_4));
                                kmaxlist.Add(new GH_Number(k1)); kmaxlist.Add(new GH_Number(k2)); kmaxlist.Add(new GH_Number(k3));
                            }
                            for (int i = 0; i < kmaxlist.Count; i++) { kmax2[ind] = Math.Max(kmax2[ind], kmaxlist[i].Value); }
                            if (on_off_12 == 1)
                            {
                                var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                                var r1 = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value); var r2 = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value);
                                var rc = (r1 + r2) / 2.0; var ri = (r1 + rc) / 2.0; var rj = (r2 + rc) / 2.0;
                                if (on_off_21 == 1)
                                {
                                    if (S[sec] == "〇" || S[sec] == "4" || S[sec] == "○" || S[sec] == "●" || S[sec] == "2")
                                    {
                                        var k1 = Math.Sqrt(k1list[3].Value * k1list[3].Value + k1list[4].Value * k1list[4].Value) + k1list[0].Value;//i端の検定比
                                        var k2 = Math.Sqrt(k2list[3].Value * k2list[3].Value + k2list[4].Value * k2list[4].Value) + k2list[0].Value;
                                        var k3 = Math.Sqrt(k3list[3].Value * k3list[3].Value + k3list[4].Value * k3list[4].Value) + k3list[0].Value;
                                        var k4 = Math.Sqrt(k4list[3].Value * k4list[3].Value + k4list[4].Value * k4list[4].Value) + k4list[0].Value;
                                        var k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(ri);
                                        var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = Math.Sqrt(k1list[8].Value * k1list[8].Value + k1list[9].Value * k1list[9].Value) + k1list[5].Value;//j端の検定比
                                        k2 = Math.Sqrt(k2list[8].Value * k2list[8].Value + k2list[9].Value * k2list[9].Value) + k2list[5].Value;
                                        k3 = Math.Sqrt(k3list[8].Value * k3list[8].Value + k3list[9].Value * k3list[9].Value) + k3list[5].Value;
                                        k4 = Math.Sqrt(k4list[8].Value * k4list[8].Value + k4list[9].Value * k4list[9].Value) + k4list[5].Value;
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(rj);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = Math.Sqrt(k1list[13].Value * k1list[13].Value + k1list[14].Value * k1list[14].Value) + k1list[10].Value;//中央の検定比
                                        k2 = Math.Sqrt(k2list[13].Value * k2list[13].Value + k2list[14].Value * k2list[14].Value) + k2list[10].Value;
                                        k3 = Math.Sqrt(k3list[13].Value * k3list[13].Value + k3list[14].Value * k3list[14].Value) + k3list[10].Value;
                                        k4 = Math.Sqrt(k4list[13].Value * k4list[13].Value + k4list[14].Value * k4list[14].Value) + k4list[10].Value;
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(rc);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                    }
                                    else
                                    {
                                        var k1 = k1list[3].Value + k1list[4].Value + k1list[0].Value;//i端の検定比
                                        var k2 = k2list[3].Value + k2list[4].Value + k2list[0].Value;
                                        var k3 = k3list[3].Value + k3list[4].Value + k3list[0].Value;
                                        var k4 = k4list[3].Value + k4list[4].Value + k4list[0].Value;
                                        var k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(ri);
                                        var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = k1list[8].Value + k1list[9].Value + k1list[5].Value;//j端の検定比
                                        k2 = k2list[8].Value + k2list[9].Value + k2list[5].Value;
                                        k3 = k3list[8].Value + k3list[9].Value + k3list[5].Value;
                                        k4 = k4list[8].Value + k4list[9].Value + k4list[5].Value;
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(rj);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = k1list[13].Value + k1list[14].Value + k1list[10].Value;//中央の検定比
                                        k2 = k2list[13].Value + k2list[14].Value + k2list[10].Value;
                                        k3 = k3list[13].Value + k3list[14].Value + k3list[10].Value;
                                        k4 = k4list[13].Value + k4list[14].Value + k4list[10].Value;
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(rc);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                    }
                                }
                                else if (on_off_22 == 1)
                                {
                                    if (S[sec] == "〇" || S[sec] == "4" || S[sec] == "○" || S[sec] == "●" || S[sec] == "2")
                                    {
                                        var k1 = Math.Sqrt(k1list[1].Value * k1list[1].Value + k1list[2].Value * k1list[2].Value);
                                        var k2 = Math.Sqrt(k2list[1].Value * k2list[1].Value + k2list[2].Value * k2list[2].Value);
                                        var k3 = Math.Sqrt(k3list[1].Value * k3list[1].Value + k3list[2].Value * k3list[2].Value);
                                        var k4 = Math.Sqrt(k4list[1].Value * k4list[1].Value + k4list[2].Value * k4list[2].Value);
                                        var k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(ri);
                                        var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = Math.Sqrt(k1list[6].Value * k1list[6].Value + k1list[7].Value * k1list[7].Value);
                                        k2 = Math.Sqrt(k2list[6].Value * k2list[6].Value + k2list[7].Value * k2list[7].Value);
                                        k3 = Math.Sqrt(k3list[6].Value * k3list[6].Value + k3list[7].Value * k3list[7].Value);
                                        k4 = Math.Sqrt(k4list[6].Value * k4list[6].Value + k4list[7].Value * k4list[7].Value);
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(rj);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = Math.Sqrt(k1list[11].Value * k1list[11].Value + k1list[12].Value * k1list[12].Value);
                                        k2 = Math.Sqrt(k2list[11].Value * k2list[11].Value + k2list[12].Value * k2list[12].Value);
                                        k3 = Math.Sqrt(k3list[11].Value * k3list[11].Value + k3list[12].Value * k3list[12].Value);
                                        k4 = Math.Sqrt(k4list[11].Value * k4list[11].Value + k4list[12].Value * k4list[12].Value);
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(rc);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                    }
                                    else
                                    {
                                        var k1 = k1list[1].Value + k1list[2].Value;
                                        var k2 = k2list[1].Value + k2list[2].Value;
                                        var k3 = k3list[1].Value + k3list[2].Value;
                                        var k4 = k4list[1].Value + k4list[2].Value;
                                        var k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(ri);
                                        var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = k1list[6].Value + k1list[7].Value;
                                        k2 = k2list[6].Value + k2list[7].Value;
                                        k3 = k3list[6].Value + k3list[7].Value;
                                        k4 = k4list[6].Value + k4list[7].Value;
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(rj);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = k1list[11].Value + k1list[12].Value;
                                        k2 = k2list[11].Value + k2list[12].Value;
                                        k3 = k3list[11].Value + k3list[12].Value;
                                        k4 = k4list[11].Value + k4list[12].Value;
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(rc);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                    }
                                }
                                else if (on_off_23 == 1)
                                {
                                    if (S[sec] == "〇" || S[sec] == "4" || S[sec] == "○" || S[sec] == "●" || S[sec] == "2")
                                    {
                                        var k1 = Math.Max(Math.Sqrt(k1list[3].Value * k1list[3].Value + k1list[4].Value * k1list[4].Value) + k1list[0].Value, Math.Sqrt(k1list[1].Value * k1list[1].Value + k1list[2].Value * k1list[2].Value));
                                        var k2 = Math.Max(Math.Sqrt(k2list[3].Value * k2list[3].Value + k2list[4].Value * k2list[4].Value) + k2list[0].Value, Math.Sqrt(k2list[1].Value * k2list[1].Value + k2list[2].Value * k2list[2].Value));
                                        var k3 = Math.Max(Math.Sqrt(k3list[3].Value * k3list[3].Value + k3list[4].Value * k3list[4].Value) + k3list[0].Value, Math.Sqrt(k3list[1].Value * k3list[1].Value + k3list[2].Value * k3list[2].Value));
                                        var k4 = Math.Max(Math.Sqrt(k4list[3].Value * k4list[3].Value + k4list[4].Value * k4list[4].Value) + k4list[0].Value, Math.Sqrt(k4list[1].Value * k4list[1].Value + k4list[2].Value * k4list[2].Value));
                                        var k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(ri);
                                        var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = Math.Max(Math.Sqrt(k1list[8].Value * k1list[8].Value + k1list[9].Value * k1list[9].Value) + k1list[5].Value, Math.Sqrt(k1list[6].Value * k1list[6].Value + k1list[7].Value * k1list[7].Value));
                                        k2 = Math.Max(Math.Sqrt(k2list[8].Value * k2list[8].Value + k2list[9].Value * k2list[9].Value) + k2list[5].Value, Math.Sqrt(k2list[6].Value * k2list[6].Value + k2list[7].Value * k2list[7].Value));
                                        k3 = Math.Max(Math.Sqrt(k3list[8].Value * k3list[8].Value + k3list[9].Value * k3list[9].Value) + k3list[5].Value, Math.Sqrt(k3list[6].Value * k3list[6].Value + k3list[7].Value * k3list[7].Value));
                                        k4 = Math.Max(Math.Max(k4list[8].Value, k4list[9].Value) + k4list[5].Value, Math.Max(Math.Max(k4list[6].Value, k4list[7].Value), k4list[5].Value));
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(rj);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = Math.Max(Math.Sqrt(k1list[13].Value * k1list[13].Value + k1list[14].Value * k1list[14].Value) + k1list[10].Value, Math.Sqrt(k1list[11].Value * k1list[11].Value + k1list[12].Value * k1list[12].Value));
                                        k2 = Math.Max(Math.Sqrt(k2list[13].Value * k2list[13].Value + k2list[14].Value * k2list[14].Value) + k2list[10].Value, Math.Sqrt(k2list[11].Value * k2list[11].Value + k2list[12].Value * k2list[12].Value));
                                        k3 = Math.Max(Math.Sqrt(k3list[13].Value * k3list[13].Value + k3list[14].Value * k3list[14].Value) + k3list[10].Value, Math.Sqrt(k3list[11].Value * k3list[11].Value + k3list[12].Value * k3list[12].Value));
                                        k4 = Math.Max(Math.Sqrt(k4list[13].Value * k4list[13].Value + k4list[14].Value * k4list[14].Value) + k4list[10].Value, Math.Sqrt(k4list[11].Value * k4list[11].Value + k4list[12].Value * k4list[12].Value));
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(rc);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                    }
                                    else
                                    {
                                        var k1 = Math.Max(k1list[3].Value + k1list[4].Value + k1list[0].Value, k1list[1].Value + k1list[2].Value);
                                        var k2 = Math.Max(k2list[3].Value + k2list[4].Value + k2list[0].Value, k2list[1].Value + k2list[2].Value);
                                        var k3 = Math.Max(k3list[3].Value + k3list[4].Value + k3list[0].Value, k3list[1].Value + k3list[2].Value);
                                        var k4 = Math.Max(k4list[3].Value + k4list[4].Value + k4list[0].Value, k4list[1].Value + k4list[2].Value);
                                        var k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(ri);
                                        var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = Math.Max(k1list[8].Value + k1list[9].Value + k1list[5].Value, k1list[6].Value + k1list[7].Value);
                                        k2 = Math.Max(k2list[8].Value + k2list[9].Value + k2list[5].Value, k2list[6].Value + k2list[7].Value);
                                        k3 = Math.Max(k3list[8].Value + k3list[9].Value + k3list[5].Value, k3list[6].Value + k3list[7].Value);
                                        k4 = Math.Max(Math.Max(k4list[8].Value, k4list[9].Value) + k4list[5].Value, Math.Max(Math.Max(k4list[6].Value, k4list[7].Value), k4list[5].Value));
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(rj);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = Math.Max(k1list[13].Value + k1list[14].Value + k1list[10].Value, k1list[11].Value + k1list[12].Value);
                                        k2 = Math.Max(k2list[13].Value + k2list[14].Value + k2list[10].Value, k2list[11].Value + k2list[12].Value);
                                        k3 = Math.Max(k3list[13].Value + k3list[14].Value + k3list[10].Value, k3list[11].Value + k3list[12].Value);
                                        k4 = Math.Max(k4list[13].Value + k4list[14].Value + k4list[10].Value, k4list[11].Value + k4list[12].Value);
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                        _p.Add(rc);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                    }
                                }
                                else if (on_off_24 == 1)
                                {
                                    var k = lam;
                                    _text.Add(k.ToString("F").Substring(0, Math.Min(digit, k.ToString("F").Length)));
                                    _p.Add(rc);
                                    var color = Color.Crimson;
                                    _c.Add(color);
                                }
                            }
                        }
                    }
                    for (int i = 0; i < index.Count; i++) { kentei_max.AppendRange(new List<GH_Number> { new GH_Number(index[i]), new GH_Number(kmax1[i]), new GH_Number(kmax2[i]) }, new GH_Path(i)); }
                    DA.SetDataTree(1, ij_new); DA.SetDataTree(11, sec_f_new); DA.SetDataTree(12, kentei);
                    var f_ctree = new GH_Structure<GH_Number>(); var f_ttree = new GH_Structure<GH_Number>(); var f_bytree = new GH_Structure<GH_Number>(); var f_bztree = new GH_Structure<GH_Number>(); var f_stree = new GH_Structure<GH_Number>();
                    var f_clist = new List<GH_Number>(); var f_tlist = new List<GH_Number>(); var f_bylist = new List<GH_Number>(); var f_bzlist = new List<GH_Number>(); var f_slist = new List<GH_Number>();
                    var f_c2list = new List<GH_Number>(); var f_t2list = new List<GH_Number>(); var f_by2list = new List<GH_Number>(); var f_bz2list = new List<GH_Number>(); var f_s2list = new List<GH_Number>();
                    for (int i = 0; i < fc.Count; i++)
                    {
                        f_clist.Add(new GH_Number(fc[i])); f_tlist.Add(new GH_Number(ft[i])); f_bylist.Add(new GH_Number(fby[i])); f_bzlist.Add(new GH_Number(fbz[i])); f_slist.Add(new GH_Number(fs[i]));
                        f_c2list.Add(new GH_Number(fc2[i])); f_t2list.Add(new GH_Number(ft2[i])); f_by2list.Add(new GH_Number(fby2[i])); f_bz2list.Add(new GH_Number(fbz2[i])); f_s2list.Add(new GH_Number(fs2[i]));
                    }
                    f_ctree.AppendRange(f_clist, new GH_Path(0)); f_ctree.AppendRange(f_c2list, new GH_Path(1));
                    f_ttree.AppendRange(f_tlist, new GH_Path(0)); f_ttree.AppendRange(f_t2list, new GH_Path(1));
                    f_bytree.AppendRange(f_bylist, new GH_Path(0)); f_bytree.AppendRange(f_by2list, new GH_Path(1));
                    f_bztree.AppendRange(f_bzlist, new GH_Path(0)); f_bztree.AppendRange(f_bz2list, new GH_Path(1));
                    f_stree.AppendRange(f_slist, new GH_Path(0)); f_stree.AppendRange(f_s2list, new GH_Path(1));
                    DA.SetDataTree(6, f_ctree); DA.SetDataTree(7, f_ttree); DA.SetDataTree(8, f_bytree); DA.SetDataTree(9, f_bztree); DA.SetDataTree(10, f_stree);
                    DA.SetDataList("lambda", lambda);
                }
                DA.SetDataList("lines", lines); DA.SetDataTree(14, kentei_max);
                var _kentei = kentei_max.Branches; var kmax = new GH_Structure<GH_Number>(); var Lmax = 0.0; int nL = 0; var Smax = 0.0; int nS = 0;
                for (int i = 0; i < _kentei.Count; i++)
                {
                    Lmax = Math.Max(Lmax, _kentei[i][1].Value);
                    if (Lmax == _kentei[i][1].Value) { nL = (int)_kentei[i][0].Value; }
                    Smax = Math.Max(Smax, _kentei[i][2].Value);
                    if (Smax == _kentei[i][2].Value) { nS = (int)_kentei[i][0].Value; }
                }
                List<GH_Number> llist = new List<GH_Number>(); List<GH_Number> slist = new List<GH_Number>();
                llist.Add(new GH_Number(nL)); llist.Add(new GH_Number(Lmax)); slist.Add(new GH_Number(nS)); slist.Add(new GH_Number(Smax));
                kmax.AppendRange(llist, new GH_Path(0)); kmax.AppendRange(slist, new GH_Path(1));
                DA.SetDataTree(15, kmax);
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
                return OpenSeesUtility.Properties.Resources.steelcheck;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("c8c32540-3351-47a4-9cb1-f69b45679154"); }
        }
        ///ここからカスタム関数群********************************************************************************
        private readonly List<string> _text = new List<string>();
        private readonly List<Point3d> _p = new List<Point3d>();
        private readonly List<Color> _c = new List<Color>();
        protected override void BeforeSolveInstance()
        {
            _text.Clear();
            _c.Clear();
            _p.Clear();
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            args.Viewport.GetFrustumFarPlane(out Plane plane);
            RhinoViewport viewport = args.Viewport;
            for (int i = 0; i < _text.Count; i++)
            {
                double size = fontsize; Point3d point = _p[i];
                plane.Origin = point;
                viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                Text3d drawText = new Text3d(_text[i], plane, size);
                args.Display.Draw3dText(drawText, _c[i]);
                drawText.Dispose();
            }
        }///ここからGUIの作成*****************************************************************************************
        public class CustomGUI : GH_ComponentAttributes
        {
            public CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle title_rec; private Rectangle title_rec2;
            private Rectangle radio_rec; private Rectangle radio_rec2;
            private Rectangle radio_rec_1; private Rectangle text_rec_1;
            private Rectangle radio_rec_2; private Rectangle text_rec_2;
            private Rectangle radio_rec2_1; private Rectangle text_rec2_1;
            private Rectangle radio_rec2_2; private Rectangle text_rec2_2;
            private Rectangle radio_rec2_3; private Rectangle text_rec2_3;
            private Rectangle radio_rec2_4; private Rectangle text_rec2_4;
            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int height = 115; int width = global_rec.Width; int radi1 = 7; int radi2 = 4; int titleheight = 20;
                int pitchx = 8; int pitchy = 11; int textheight = 20;
                global_rec.Height += height;
                global_rec.Width = width;
                title_rec = global_rec;
                title_rec.Y = title_rec.Bottom - height;
                title_rec.Height = titleheight;

                radio_rec = title_rec;
                radio_rec.Y += title_rec.Height;
                radio_rec.Height = 5;

                radio_rec_1 = radio_rec;
                radio_rec_1.X += 5; radio_rec_1.Y += 5;
                radio_rec_1.Height = radi1; radio_rec_1.Width = radi1;

                text_rec_1 = radio_rec_1;
                text_rec_1.X += pitchx; text_rec_1.Y -= radi2;
                text_rec_1.Height = textheight; text_rec_1.Width = width;
                radio_rec.Height += pitchy;

                radio_rec_2 = radio_rec_1; radio_rec_2.Y += pitchy;
                text_rec_2 = radio_rec_2;
                text_rec_2.X += pitchx; text_rec_2.Y -= radi2;
                text_rec_2.Height = textheight; text_rec_2.Width = width;
                radio_rec.Height += pitchy;

                title_rec2 = global_rec;
                title_rec2.Y = radio_rec.Bottom;
                title_rec2.Height = titleheight;

                radio_rec2 = title_rec2;
                radio_rec2.Y = title_rec2.Bottom;
                radio_rec2.Height = 5;

                radio_rec2_1 = radio_rec2;
                radio_rec2_1.X += 5; radio_rec2_1.Y += 5;
                radio_rec2_1.Height = radi1; radio_rec2_1.Width = radi1;

                text_rec2_1 = radio_rec2_1;
                text_rec2_1.X += pitchx; text_rec2_1.Y -= radi2;
                text_rec2_1.Height = textheight; text_rec2_1.Width = width;
                radio_rec2.Height += pitchy;

                radio_rec2_2 = radio_rec2_1; radio_rec2_2.Y += pitchy;
                text_rec2_2 = radio_rec2_2;
                text_rec2_2.X += pitchx; text_rec2_2.Y -= radi2;
                text_rec2_2.Height = textheight; text_rec2_2.Width = width;
                radio_rec2.Height += pitchy;

                radio_rec2_3 = radio_rec2_2; radio_rec2_3.Y += pitchy;
                text_rec2_3 = radio_rec2_3;
                text_rec2_3.X += pitchx; text_rec2_3.Y -= radi2;
                text_rec2_3.Height = textheight; text_rec2_3.Width = width;
                radio_rec2.Height += pitchy;

                radio_rec2_4 = radio_rec2_3; radio_rec2_4.Y += pitchy;
                text_rec2_4 = radio_rec2_4;
                text_rec2_4.X += pitchx; text_rec2_4.Y -= radi2;
                text_rec2_4.Height = textheight; text_rec2_4.Width = width;
                radio_rec2.Height += pitchy;

                Bounds = global_rec;
            }
            Brush c1 = Brushes.Black; Brush c2 = Brushes.White; Brush c21 = Brushes.White; Brush c22 = Brushes.White; Brush c23 = Brushes.White; Brush c24 = Brushes.White;
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
                    graphics.DrawString("Stress design", GH_FontServer.Standard, Brushes.White, textRectangle, format);

                    GH_Capsule radio = GH_Capsule.CreateCapsule(radio_rec, GH_Palette.White, 2, 0);
                    radio.Render(graphics, Selected, Owner.Locked, false); radio.Dispose();

                    GH_Capsule radio_1 = GH_Capsule.CreateCapsule(radio_rec_1, GH_Palette.Black, 5, 5);
                    radio_1.Render(graphics, Selected, Owner.Locked, false); radio_1.Dispose();
                    graphics.FillEllipse(c1, radio_rec_1);
                    graphics.DrawString("Long-term", GH_FontServer.Standard, Brushes.Black, text_rec_1);

                    GH_Capsule radio_2 = GH_Capsule.CreateCapsule(radio_rec_2, GH_Palette.Black, 5, 5);
                    radio_2.Render(graphics, Selected, Owner.Locked, false); radio_2.Dispose();
                    graphics.FillEllipse(c2, radio_rec_2);
                    graphics.DrawString("Short-term", GH_FontServer.Standard, Brushes.Black, text_rec_2);

                    GH_Capsule title2 = GH_Capsule.CreateCapsule(title_rec2, GH_Palette.Pink, 2, 0);
                    title2.Render(graphics, Selected, Owner.Locked, false);
                    title2.Dispose();

                    RectangleF textRectangle2 = title_rec2;
                    textRectangle2.Height = 20;
                    graphics.DrawString("Display option", GH_FontServer.Standard, Brushes.White, textRectangle2, format);

                    GH_Capsule radio2 = GH_Capsule.CreateCapsule(radio_rec2, GH_Palette.White, 2, 0);
                    radio2.Render(graphics, Selected, Owner.Locked, false); radio2.Dispose();

                    GH_Capsule radio2_1 = GH_Capsule.CreateCapsule(radio_rec2_1, GH_Palette.Black, 5, 5);
                    radio2_1.Render(graphics, Selected, Owner.Locked, false); radio2_1.Dispose();
                    graphics.FillEllipse(c21, radio_rec2_1);
                    graphics.DrawString("M+N kentei", GH_FontServer.Standard, Brushes.Black, text_rec2_1);

                    GH_Capsule radio2_2 = GH_Capsule.CreateCapsule(radio_rec2_2, GH_Palette.Black, 5, 5);
                    radio2_2.Render(graphics, Selected, Owner.Locked, false); radio2_2.Dispose();
                    graphics.FillEllipse(c22, radio_rec2_2);
                    graphics.DrawString("Q kentei", GH_FontServer.Standard, Brushes.Black, text_rec2_2);

                    GH_Capsule radio2_3 = GH_Capsule.CreateCapsule(radio_rec2_3, GH_Palette.Black, 5, 5);
                    radio2_3.Render(graphics, Selected, Owner.Locked, false); radio2_3.Dispose();
                    graphics.FillEllipse(c23, radio_rec2_3);
                    graphics.DrawString("MAX kentei", GH_FontServer.Standard, Brushes.Black, text_rec2_3);

                    GH_Capsule radio2_4 = GH_Capsule.CreateCapsule(radio_rec2_4, GH_Palette.Black, 5, 5);
                    radio2_4.Render(graphics, Selected, Owner.Locked, false); radio2_4.Dispose();
                    graphics.FillEllipse(c24, radio_rec2_4);
                    graphics.DrawString("λ", GH_FontServer.Standard, Brushes.Black, text_rec2_4);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec1 = radio_rec_1; RectangleF rec2 = radio_rec_2;
                    RectangleF rec21 = radio_rec2_1; RectangleF rec22 = radio_rec2_2; RectangleF rec23 = radio_rec2_3; RectangleF rec24 = radio_rec2_4;
                    if (rec1.Contains(e.CanvasLocation))
                    {
                        if (c1 == Brushes.White) { c1 = Brushes.Black; SetButton("11", 1); c2 = Brushes.White; SetButton("12", 0); }
                        else { c1 = Brushes.White; SetButton("11", 0); c2 = Brushes.Black; SetButton("12", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec2.Contains(e.CanvasLocation))
                    {
                        if (c2 == Brushes.White) { c2 = Brushes.Black; SetButton("12", 1); c1 = Brushes.White; SetButton("11", 0); }
                        else { c2 = Brushes.White; SetButton("12", 0); c1 = Brushes.Black; SetButton("11", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec21.Contains(e.CanvasLocation))
                    {
                        if (c21 == Brushes.White) { c21 = Brushes.Black; SetButton("21", 1); c22 = Brushes.White; SetButton("22", 0); c23 = Brushes.White; SetButton("23", 0); c24 = Brushes.White; SetButton("24", 0); }
                        else { c21 = Brushes.White; SetButton("21", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec22.Contains(e.CanvasLocation))
                    {
                        if (c22 == Brushes.White) { c22 = Brushes.Black; SetButton("22", 1); c21 = Brushes.White; SetButton("21", 0); c23 = Brushes.White; SetButton("23", 0); c24 = Brushes.White; SetButton("24", 0); }
                        else { c22 = Brushes.White; SetButton("22", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec23.Contains(e.CanvasLocation))
                    {
                        if (c23 == Brushes.White) { c23 = Brushes.Black; SetButton("23", 1); c21 = Brushes.White; SetButton("21", 0); c22 = Brushes.White; SetButton("22", 0); c24 = Brushes.White; SetButton("24", 0); }
                        else { c23 = Brushes.White; SetButton("23", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec24.Contains(e.CanvasLocation))
                    {
                        if (c24 == Brushes.White) { c24 = Brushes.Black; SetButton("24", 1); c21 = Brushes.White; SetButton("21", 0); c22 = Brushes.White; SetButton("22", 0); c23 = Brushes.White; SetButton("23", 0); }
                        else { c24 = Brushes.White; SetButton("24", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}