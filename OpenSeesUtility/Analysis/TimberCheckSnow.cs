using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Display;

using System.Drawing;
using System.Windows.Forms;
using System.IO;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Rhino;
///****************************************
using System.Diagnostics;

using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp;

namespace OpenSeesUtility
{
    public class TimberCheckSnow : GH_Component
    {
        public static int on_off_11 = 1; public static int on_off_12 = 0; public static double fontsize;
        public static int on_off_21 = 0; public static int on_off_22 = 0; public static int on_off_23 = 0; public static int on_off_24 = 0;
        static int on_off = 0;
        public static PdfCreate.JapaneseFontResolver fontresolver = new PdfCreate.JapaneseFontResolver();
        public static XGraphics gfx;
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
            else if (s == "1")
            {
                on_off = i;
            }
        }
        public TimberCheckSnow()
          : base("Allowable stress design for timber beams", "TimberCheckSnow",
              "Allowable stress design(danmensantei) for timber beams using Japanese Design Code (for Heavy Snow Area)",
              "OpenSees", "Analysis")
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
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("section area", "A", "[...](DataList)", GH_ParamAccess.list, new List<double> { 0.01 });///
            pManager.AddNumberParameter("Second moment of area around y-axis", "Iy", "[...](DataList)", GH_ParamAccess.list, new List<double> { Math.Pow(0.1, 4) / 12.0 });///
            pManager.AddNumberParameter("Second moment of area around z-axis", "Iz", "[...](DataList)", GH_ParamAccess.list, new List<double> { Math.Pow(0.1, 4) / 12.0 });///
            pManager.AddNumberParameter("Section modulus around y-axis", "Zy", "[...](DataList)", GH_ParamAccess.list, new List<double> { Math.Pow(0.01, 3) / 6.0 });///
            pManager.AddNumberParameter("Section modulus around z-axis", "Zz", "[...](DataList)", GH_ParamAccess.list, new List<double> { Math.Pow(0.01, 3) / 6.0 });///
            pManager.AddNumberParameter("Standard allowable stress (compression)[N/mm2]", "Fc", "[...](DataList)[N/mm2]", GH_ParamAccess.list, new List<double> { 17.7 });///
            pManager.AddNumberParameter("Standard allowable stress (tensile)[N/mm2]", "Ft", "[...](DataList)[N/mm2]", GH_ParamAccess.list, new List<double> { 13.5 });///
            pManager.AddNumberParameter("Standard allowable stress (bending)[N/mm2]", "Fb", "[...](DataList)[N/mm2]", GH_ParamAccess.list, new List<double> { 22.2 });///
            pManager.AddNumberParameter("Standard allowable stress (shear)[N/mm2]", "Fs", "[...](DataList)[N/mm2]", GH_ParamAccess.list, new List<double> { 1.8 });///
            pManager.AddNumberParameter("Lby", "Lby", "buckling length for local y axis[...](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddNumberParameter("Lbz", "Lbz", "buckling length for local z axis[...](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddNumberParameter("sec_f(L)", "sec_f(L)", "[[Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("sec_f(S)", "sec_f(S)", "[[Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("safe factor", "alpha", "Reduction rate taking into account cross-sectional defects", GH_ParamAccess.item, 0.75);///
            pManager.AddNumberParameter("beta", "beta", "Allowable Stress Factor for Snow Loading L and S (generally 1.3 & 0.8)", GH_ParamAccess.list, new List<double> { 1.3, 0.8 });///
            pManager.AddNumberParameter("index", "index", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddTextParameter("secname", "secname", "section name", GH_ParamAccess.list, "");///
            pManager.AddNumberParameter("fontsize", "FS", "font size for display texts", GH_ParamAccess.item, 12.0);///
            pManager.AddTextParameter("outputname", "outputname", "output file name", GH_ParamAccess.item, "TimberCheckSnow");///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("kentei(max)", "kentei(max)", "[[ele. No.,for Long-term, for Short-term],...](DataTree)", GH_ParamAccess.tree);///
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
            DA.GetDataTree("sec_f(L)", out GH_Structure<GH_Number> _sec_f); var sec_f_new = new GH_Structure<GH_Number>();
            DA.GetDataTree("sec_f(S)", out GH_Structure<GH_Number> _sec_f2);
            fontsize = 20.0; DA.GetData("fontsize", ref fontsize); List<string> secname = new List<string>(); DA.GetDataList("secname", secname);
            List<double> A = new List<double>(); DA.GetDataList("section area", A);
            List<double> Iy = new List<double>(); DA.GetDataList("Second moment of area around y-axis", Iy);
            List<double> Iz = new List<double>(); DA.GetDataList("Second moment of area around z-axis", Iz);
            List<double> Zy = new List<double>(); DA.GetDataList("Section modulus around y-axis", Zy);
            List<double> Zz = new List<double>(); DA.GetDataList("Section modulus around z-axis", Zz);
            List<double> Fc = new List<double>(); DA.GetDataList("Standard allowable stress (compression)[N/mm2]", Fc);
            List<double> Ft = new List<double>(); DA.GetDataList("Standard allowable stress (tensile)[N/mm2]", Ft);
            List<double> Fb = new List<double>(); DA.GetDataList("Standard allowable stress (bending)[N/mm2]", Fb);
            List<double> Fs = new List<double>(); DA.GetDataList("Standard allowable stress (shear)[N/mm2]", Fs);
            List<double> Lby = new List<double>(); DA.GetDataList("Lby", Lby); List<double> Lbz = new List<double>(); DA.GetDataList("Lbz", Lbz); List<double> Lambda = new List<double>();
            double alpha = 1.0; DA.GetData("safe factor", ref alpha);
            List<double> beta = new List<double>(); DA.GetDataList("beta", beta);
            var r = _r.Branches; var ij = _ij.Branches; var sec_f = _sec_f.Branches; var sec_f2 = _sec_f2.Branches;
            var fc = new List<double>(); var ft = new List<double>(); var fb = new List<double>(); var fs = new List<double>();
            var fc2 = new List<double>(); var ft2 = new List<double>(); var fb2 = new List<double>(); var fs2 = new List<double>();
            var f_c = new List<double>(); var f_t = new List<double>(); var f_b = new List<double>(); var f_s = new List<double>(); var f_k = new List<double>();
            var f_c2 = new List<double>(); var f_t2 = new List<double>(); var f_b2 = new List<double>(); var f_s2 = new List<double>(); var f_k2 = new List<double>();
            int digit = 4;
            var unit = 1.0;///単位合わせのための係数
            unit /= 1000000.0;
            unit *= 1000.0;
            var maxvalL = 0.0; var maxvalS = 0.0; var maxval = new List<double>(); var kmax1 = new List<double>(); var kmax2 = new List<double>();
            List<double> index = new List<double>();
            DA.GetDataList("index", index);
            var kentei = new GH_Structure<GH_Number>();
            if (index[0] == -9999)
            {
                index = new List<double>();
                for (int e = 0; e < ij.Count; e++) { index.Add(e); }
            }
            if (r[0][0].Value != -9999 && ij[0][0].Value != -9999 && (sec_f[0][0].Value != -9999| sec_f2[0][0].Value != -9999))
            {
                for (int i = 0; i < Fc.Count; i++)
                {
                    fc.Add(Fc[i] * beta[0] * 1.1 / 3.0 * alpha); ft.Add(Ft[i] * beta[0] * 1.1 / 3.0 * alpha); fb.Add(Fb[i] * beta[0] * 1.1 / 3.0 * alpha); fs.Add(Fs[i] * beta[0] * 1.1 / 3.0 * alpha);
                    fc2.Add(Fc[i] * beta[1] * 2.0 / 3.0 * alpha); ft2.Add(Ft[i] * beta[1] * 2.0 / 3.0 * alpha); fb2.Add(Fb[i] * beta[1] * 2.0 / 3.0 * alpha); fs2.Add(Fs[i] * beta[1] * 2.0 / 3.0 * alpha);
                }
                if (Lby[0] == -9999)
                {
                    Lby = new List<double>();
                    for (int e = 0; e < ij.Count; e++)
                    {
                        var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                        var l = Math.Sqrt(Math.Pow(r[nj][0].Value - r[ni][0].Value, 2) + Math.Pow(r[nj][1].Value - r[ni][1].Value, 2) + Math.Pow(r[nj][2].Value - r[ni][2].Value, 2));
                        Lby.Add(l);
                    }
                }
                if (Lbz[0] == -9999)
                {
                    Lbz = new List<double>();
                    for (int e = 0; e < ij.Count; e++)
                    {
                        var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                        var l = Math.Sqrt(Math.Pow(r[nj][0].Value - r[ni][0].Value, 2) + Math.Pow(r[nj][1].Value - r[ni][1].Value, 2) + Math.Pow(r[nj][2].Value - r[ni][2].Value, 2));
                        Lbz.Add(l);
                    }
                }
                var kmaxlist1 = new List<List<double>>(); var kmaxlist2 = new List<List<double>>(); var klistall= new List<List<GH_Number>>(); var klist2all = new List<List<GH_Number>>();
                for (int ind = 0; ind < index.Count; ind++)
                {
                    int e = (int)index[ind];
                    int mat = (int)ij[e][2].Value; int sec = (int)ij[e][3].Value;
                    var iy = Iy[sec]; var iz = Iz[sec]; var a = A[sec];
                    var iby = Math.Sqrt(iy / a); var ibz = Math.Sqrt(iz / a);
                    var lam = Math.Max(Lby[e] / iby, Lbz[e] / ibz);
                    Lambda.Add(lam); f_c.Add(fc[mat]); f_t.Add(ft[mat]); f_b.Add(fb[mat]); f_s.Add(fs[mat]); f_c2.Add(fc2[mat]); f_t2.Add(ft2[mat]); f_b2.Add(fb2[mat]); f_s2.Add(fs2[mat]);
                    if (lam <= 30.0) { f_k.Add(fc[mat]); f_k2.Add(fc2[mat]); }
                    else if (lam <= 100.0) { f_k.Add(fc[mat] * (1.3 - 0.01 * lam)); f_k2.Add(fc2[mat] * (1.3 - 0.01 * lam)); }
                    else { f_k.Add(fc[mat] * 3000 / Math.Pow(lam, 2)); f_k2.Add(fc2[mat] * 3000 / Math.Pow(lam, 2)); }
                }
                if (sec_f[0][0].Value != -9999)
                {
                    for (int ind = 0; ind < index.Count; ind++)
                    {
                        int e = (int)index[ind];
                        int mat = (int)ij[e][2].Value; int sec = (int)ij[e][3].Value;
                        var a = A[sec]; var zy = Zy[sec]; var zz = Zz[sec];
                        var Ni = -sec_f[e][0].Value; var Qyi = Math.Abs(sec_f[e][1].Value); var Qzi = Math.Abs(sec_f[e][2].Value);
                        var Myi = Math.Abs(sec_f[e][4].Value); var Mzi = Math.Abs(sec_f[e][5].Value);
                        var Nj = sec_f[e][6].Value; var Qyj = Math.Abs(sec_f[e][7].Value); var Qzj = Math.Abs(sec_f[e][8].Value);
                        var Myj = Math.Abs(sec_f[e][10].Value); var Mzj = Math.Abs(sec_f[e][11].Value);
                        var Nc = -sec_f[e][12].Value; var Qyc = Math.Abs(sec_f[e][13].Value); var Qzc = Math.Abs(sec_f[e][14].Value);
                        var Myc = Math.Abs(sec_f[e][16].Value); var Mzc = Math.Abs(sec_f[e][17].Value);
                        List<GH_Number> klist = new List<GH_Number>();//=[0:sigma_c or sigma_t, 1:tau_y, 2:tau_z, 3:sigma_by, 4:sigma_zy, 5:sigma_c or sigma_t, 6:tau_y, 7:tau_z, 8:sigma_by, 9:sigma_zy, 10:sigma_c or sigma_t, 11:tau_y, 12:tau_z, 13:sigma_by, 14:sigma_zy]
                        if (Ni < 0) { klist.Add(new GH_Number(Math.Abs(Ni) / a / f_k[ind] * unit)); }
                        else { klist.Add(new GH_Number(Math.Abs(Ni) / a / ft[mat] * unit)); }
                        klist.Add(new GH_Number(Qyi / a / fs[mat] * unit * 1.5)); klist.Add(new GH_Number(Qzi / a / fs[mat] * unit * 1.5));
                        klist.Add(new GH_Number(Myi / zy / fb[mat] * unit)); klist.Add(new GH_Number(Mzi / zz / fb[mat] * unit));
                        if (Nj < 0) { klist.Add(new GH_Number(Math.Abs(Nj) / a / f_k[ind] * unit)); }
                        else { klist.Add(new GH_Number(Math.Abs(Nj) / a / ft[mat] * unit)); }
                        klist.Add(new GH_Number(Qyj / a / fs[mat] * unit * 1.5)); klist.Add(new GH_Number(Qzj / a / fs[mat] * unit * 1.5));
                        klist.Add(new GH_Number(Myj / zy / fb[mat] * unit)); klist.Add(new GH_Number(Mzj / zz / fb[mat] * unit));
                        if (Nc < 0) { klist.Add(new GH_Number(Math.Abs(Nc) / a / f_k[ind] * unit)); }
                        else { klist.Add(new GH_Number(Math.Abs(Nc) / a / ft[mat] * unit)); }
                        klist.Add(new GH_Number(Qyc / a / fs[mat] * unit * 1.5)); klist.Add(new GH_Number(Qzc / a / fs[mat] * unit * 1.5));
                        klist.Add(new GH_Number(Myc / zy / fb[mat] * unit)); klist.Add(new GH_Number(Mzc / zz / fb[mat] * unit));
                        var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                        var r1 = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value); var r2 = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value);
                        var rc = (r1 + r2) / 2.0; var ri = (r1 + rc) / 2.0; var rj = (r2 + rc) / 2.0;
                        var ki1 = Math.Max(klist[3].Value, klist[4].Value) + klist[0].Value; var ki2 = Math.Max(Math.Max(klist[1].Value, klist[2].Value), klist[0].Value);
                        var ki = Math.Max(ki1, ki2);
                        var kj1 = Math.Max(klist[8].Value, klist[9].Value) + klist[5].Value; var kj2 = Math.Max(Math.Max(klist[6].Value, klist[7].Value), klist[5].Value);
                        var kj = Math.Max(kj1, kj2);
                        var kc1 = Math.Max(klist[13].Value, klist[14].Value) + klist[10].Value; var kc2 = Math.Max(Math.Max(klist[11].Value, klist[12].Value), klist[10].Value);
                        var kc = Math.Max(kc1, kc2);
                        kmax1.Add(Math.Max(Math.Max(ki, kj), kc));
                        maxvalL = Math.Max(kc, Math.Max(kj, Math.Max(maxvalL, ki)));
                        klistall.Add(klist);
                        if (on_off_11 == 1)
                        {
                            if (on_off_21 == 1)
                            {
                                _text.Add(ki1.ToString("F").Substring(0, digit));
                                _p.Add(ri);
                                var color = new ColorHSL((1 - Math.Min(ki1, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                                _text.Add(kj1.ToString("F").Substring(0, digit));
                                _p.Add(rj);
                                color = new ColorHSL((1 - Math.Min(kj1, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                                _text.Add(kc1.ToString("F").Substring(0, digit));
                                _p.Add(rc);
                                color = new ColorHSL((1 - Math.Min(kc1, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                            }
                            else if (on_off_22 == 1)
                            {
                                _text.Add(ki2.ToString("F").Substring(0, digit));
                                _p.Add(ri);
                                var color = new ColorHSL((1 - Math.Min(ki2, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                                _text.Add(kj2.ToString("F").Substring(0, digit));
                                _p.Add(rj);
                                color = new ColorHSL((1 - Math.Min(kj2, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                                _text.Add(kc2.ToString("F").Substring(0, digit));
                                _p.Add(rc);
                                color = new ColorHSL((1 - Math.Min(kc2, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                            }
                            else if (on_off_23 == 1)
                            {
                                _text.Add(ki.ToString("F").Substring(0, digit));
                                _p.Add(ri);
                                var color = new ColorHSL((1 - Math.Min(ki, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                                _text.Add(kj.ToString("F").Substring(0, digit));
                                _p.Add(rj);
                                color = new ColorHSL((1 - Math.Min(kj, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                                _text.Add(kc.ToString("F").Substring(0, digit));
                                _p.Add(rc);
                                color = new ColorHSL((1 - Math.Min(kc, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                            }
                            else if (on_off_24 == 1)
                            {
                                var k = Lambda[ind];
                                _text.Add(k.ToString("F").Substring(0, digit));
                                _p.Add(rc);
                                var color = Color.Crimson;
                                _c.Add(color);
                            }
                        }
                    }
                }
                else
                {
                    for (int ind = 0; ind < index.Count; ind++)
                    {
                        kmax1.Add(0.0);
                    }
                }
                if (sec_f2[0][0].Value != -9999)
                {
                    for (int ind = 0; ind < index.Count; ind++)
                    {
                        int e = (int)index[ind];
                        int mat = (int)ij[e][2].Value; int sec = (int)ij[e][3].Value;
                        var a = A[sec]; var zy = Zy[sec]; var zz = Zz[sec];
                        var Ni = -sec_f2[e][0].Value; var Qyi = Math.Abs(sec_f2[e][1].Value); var Qzi = Math.Abs(sec_f2[e][2].Value);
                        var Myi = Math.Abs(sec_f2[e][4].Value); var Mzi = Math.Abs(sec_f2[e][5].Value);
                        var Nj = sec_f2[e][6].Value; var Qyj = Math.Abs(sec_f2[e][7].Value); var Qzj = Math.Abs(sec_f2[e][8].Value);
                        var Myj = Math.Abs(sec_f2[e][10].Value); var Mzj = Math.Abs(sec_f2[e][11].Value);
                        var Nc = -sec_f2[e][12].Value; var Qyc = Math.Abs(sec_f2[e][13].Value); var Qzc = Math.Abs(sec_f2[e][14].Value);
                        var Myc = Math.Abs(sec_f2[e][16].Value); var Mzc = Math.Abs(sec_f2[e][17].Value);
                        List<GH_Number> klist2 = new List<GH_Number>();//=[0:sigma_c or sigma_t, 1:tau_y, 2:tau_z, 3:sigma_by, 4:sigma_zy, 5:sigma_c or sigma_t, 6:tau_y, 7:tau_z, 8:sigma_by, 9:sigma_zy, 10:sigma_c or sigma_t, 11:tau_y, 12:tau_z, 13:sigma_by, 14:sigma_zy]
                        if (Ni < 0) { klist2.Add(new GH_Number(Math.Abs(Ni) / a / f_k2[ind] * unit)); }
                        else { klist2.Add(new GH_Number(Math.Abs(Ni) / a / ft2[mat] * unit)); }
                        klist2.Add(new GH_Number(Qyi / a / fs2[mat] * unit * 1.5)); klist2.Add(new GH_Number(Qzi / a / fs2[mat] * unit * 1.5));
                        klist2.Add(new GH_Number(Myi / zy / fb2[mat] * unit)); klist2.Add(new GH_Number(Mzi / zz / fb2[mat] * unit));
                        if (Nj < 0) { klist2.Add(new GH_Number(Math.Abs(Nj) / a / f_k2[ind] * unit)); }
                        else { klist2.Add(new GH_Number(Math.Abs(Nj) / a / ft2[mat] * unit)); }
                        klist2.Add(new GH_Number(Qyj / a / fs2[mat] * unit * 1.5)); klist2.Add(new GH_Number(Qzj / a / fs2[mat] * unit * 1.5));
                        klist2.Add(new GH_Number(Myj / zy / fb2[mat] * unit)); klist2.Add(new GH_Number(Mzj / zz / fb2[mat] * unit));
                        if (Nc < 0) { klist2.Add(new GH_Number(Math.Abs(Nc) / a / f_k2[ind] * unit)); }
                        else { klist2.Add(new GH_Number(Math.Abs(Nc) / a / ft2[mat] * unit)); }
                        klist2.Add(new GH_Number(Qyc / a / fs2[mat] * unit * 1.5)); klist2.Add(new GH_Number(Qzc / a / fs2[mat] * unit * 1.5));
                        klist2.Add(new GH_Number(Myc / zy / fb2[mat] * unit)); klist2.Add(new GH_Number(Mzc / zz / fb2[mat] * unit));
                        var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                        var r1 = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value); var r2 = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value);
                        var rc = (r1 + r2) / 2.0; var ri = (r1 + rc) / 2.0; var rj = (r2 + rc) / 2.0;
                        var ki1 = Math.Max(klist2[3].Value, klist2[4].Value) + klist2[0].Value; var ki2 = Math.Max(Math.Max(klist2[1].Value, klist2[2].Value), klist2[0].Value);
                        var ki = Math.Max(ki1, ki2);
                        var kj1 = Math.Max(klist2[8].Value, klist2[9].Value) + klist2[5].Value; var kj2 = Math.Max(Math.Max(klist2[6].Value, klist2[7].Value), klist2[5].Value);
                        var kj = Math.Max(kj1, kj2);
                        var kc1 = Math.Max(klist2[13].Value, klist2[14].Value) + klist2[10].Value; var kc2 = Math.Max(Math.Max(klist2[11].Value, klist2[12].Value), klist2[10].Value);
                        var kc = Math.Max(kc1, kc2);
                        kmax2.Add(Math.Max(Math.Max(ki, kj), kc));
                        maxvalS = Math.Max(kc, Math.Max(kj, Math.Max(maxvalS, ki)));
                        klist2all.Add(klist2);
                        if (on_off_12 == 1)
                        {
                            if (on_off_21 == 1)
                            {
                                _text.Add(ki1.ToString("F").Substring(0, digit));
                                _p.Add(ri);
                                var color = new ColorHSL((1 - Math.Min(ki1, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                                _text.Add(kj1.ToString("F").Substring(0, digit));
                                _p.Add(rj);
                                color = new ColorHSL((1 - Math.Min(kj1, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                                _text.Add(kc1.ToString("F").Substring(0, digit));
                                _p.Add(rc);
                                color = new ColorHSL((1 - Math.Min(kc1, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                            }
                            else if (on_off_22 == 1)
                            {
                                _text.Add(ki2.ToString("F").Substring(0, digit));
                                _p.Add(ri);
                                var color = new ColorHSL((1 - Math.Min(ki2, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                                _text.Add(kj2.ToString("F").Substring(0, digit));
                                _p.Add(rj);
                                color = new ColorHSL((1 - Math.Min(kj2, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                                _text.Add(kc2.ToString("F").Substring(0, digit));
                                _p.Add(rc);
                                color = new ColorHSL((1 - Math.Min(kc2, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                            }
                            else if (on_off_23 == 1)
                            {
                                _text.Add(ki.ToString("F").Substring(0, digit));
                                _p.Add(ri);
                                var color = new ColorHSL((1 - Math.Min(ki, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                                _text.Add(kj.ToString("F").Substring(0, digit));
                                _p.Add(rj);
                                color = new ColorHSL((1 - Math.Min(kj, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                                _text.Add(kc.ToString("F").Substring(0, digit));
                                _p.Add(rc);
                                color = new ColorHSL((1 - Math.Min(kc, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                _c.Add(color);
                            }
                            else if (on_off_24 == 1)
                            {
                                var k = Lambda[ind];
                                _text.Add(k.ToString("F").Substring(0, digit));
                                _p.Add(rc);
                                var color = Color.Crimson;
                                _c.Add(color);
                            }
                        }
                    }
                }
                else
                {
                    for (int ind = 0; ind < index.Count; ind++)
                    {
                        kmax2.Add(0.0);
                    }
                }
                for (int ind = 0; ind < index.Count; ind++)
                {
                    int e = (int)index[ind];
                    kentei.AppendRange(new List<GH_Number> { new GH_Number(e), new GH_Number(kmax1[ind]), new GH_Number(kmax2[ind]) }, new GH_Path(ind));
                }
                DA.SetDataTree(0, kentei);
                var _kentei = kentei.Branches; var kmax = new GH_Structure<GH_Number>(); var Lmax = 0.0; var Smax = 0.0; int L = 0; int S = 0;
                for (int i = 0; i < _kentei.Count; i++)
                {
                    Lmax = Math.Max(Lmax, _kentei[i][1].Value);
                    if (Lmax == _kentei[i][1].Value) { L = (int)_kentei[i][0].Value; }
                    Smax = Math.Max(Smax, _kentei[i][2].Value);
                    if (Smax == _kentei[i][2].Value) { S = (int)_kentei[i][0].Value; }
                }
                List<GH_Number> llist = new List<GH_Number>(); List<GH_Number> slist = new List<GH_Number>();
                llist.Add(new GH_Number(L)); llist.Add(new GH_Number(Lmax)); slist.Add(new GH_Number(S)); slist.Add(new GH_Number(Smax));
                kmax.AppendRange(llist, new GH_Path(0)); kmax.AppendRange(slist, new GH_Path(1));
                DA.SetDataTree(1, kmax);
                if (on_off == 1)
                {
                    var pdfname = "TimberCheckSnow"; DA.GetData("outputname", ref pdfname);
                    int Digit(int num)//数字の桁数を求める関数
                    {
                        // Mathf.Log10(0)はNegativeInfinityを返すため、別途処理する。
                        return (num == 0) ? 1 : ((int)Math.Log10(num) + 1);
                    }
                    XColor RGB(double h, double s, double l)//convert HSL to RGB
                    {
                        var max = 0.0; var min = 0.0; var rr = 0.0; var g = 0.0; var b = 0.0;
                        if (l < 0.5)
                        {
                            max = l + l * s;
                            min = l - l * s;
                        }
                        else
                        {
                            max = l + (1 - l) * s;
                            min = l - (1 - l) * s;
                        }
                        var HUE_MAX = 360.0; var RGB_MAX = 255;
                        var hp = HUE_MAX / 6.0; h *= HUE_MAX; var q = h / hp;
                        if (q <= 1)
                        {
                            rr = max;
                            g = (h / hp) * (max - min) + min;
                            b = min;
                        }
                        else if (q <= 2)
                        {
                            rr = ((hp * 2 - h) / hp) * (max - min) + min;
                            g = max;
                            b = min;
                        }
                        else if (q <= 3)
                        {
                            rr = min;
                            g = max;
                            b = ((h - hp * 2) / hp) * (max - min) + min;
                        }
                        else if (q <= 4)
                        {
                            rr = min;
                            g = ((hp * 4 - h) / hp) * (max - min) + min;
                            b = max;
                        }
                        else if (q <= 5)
                        {
                            rr = ((h - hp * 4) / hp) * (max - min) + min;
                            g = min;
                            b = max;
                        }
                        else
                        {
                            rr = max;
                            g = min;
                            b = ((HUE_MAX - h) / hp) * (max - min) + min;
                        }
                        rr *= RGB_MAX; g *= RGB_MAX; b *= RGB_MAX;
                        return XColor.FromArgb((int)rr, (int)g, (int)b);
                    }
                    if (index[0] == -9999)
                    {
                        index = new List<double>();
                        for (int e = 0; e < ij.Count; e++) { index.Add(e); }
                    }
                    // フォントリゾルバーのグローバル登録
                    if (PdfCreate.JapaneseFontResolver.fontset == 0) { PdfSharp.Fonts.GlobalFontSettings.FontResolver = fontresolver; PdfCreate.JapaneseFontResolver.fontset = 1; }
                    // PDFドキュメントを作成。
                    PdfDocument document = new PdfDocument();
                    document.Info.Title = pdfname;
                    document.Info.Author = "Shinnosuke Fujita, Assoc. Prof., The Univ. of Kitakyushu";
                    // フォントを作成。
                    XFont font = new XFont("Gen Shin Gothic", 9, XFontStyle.Regular);
                    XFont fontbold = new XFont("Gen Shin Gothic", 9, XFontStyle.Bold);
                    var pen = XPens.Black;
                    var labels = new List<string>
                    {
                        "部材番号","部材断面","λ","A[cm2]","α(低減率)","Zy[cm3]","Zz[cm3]","","fk[N/mm2]", "ft[N/mm2]", "fb[N/mm2]", "fs[N/mm2]","", "節点番号","", "N[kN]","My[kNm]", "Mz[kNm]","Qy[kN]","Qz[kN]","軸+曲げ検定比","せん断検定比","判定"
                    };
                    if (sec_f[0][0].Value != -9999 && sec_f2[0][0].Value != -9999)
                    {
                        labels.Add(""); labels.Add("N[kN]"); labels.Add("My[kNm]"); labels.Add("Mz[kNm]"); labels.Add("Qy[kN]"); labels.Add("Qz[kN]"); labels.Add("軸+曲げ検定比"); labels.Add("せん断検定比"); labels.Add("判定");
                    }
                    var label_width = 75; var offset_x = 25; var offset_y = 25; var pitchy = 12; var text_width = 25; PdfPage page = new PdfPage(); page.Size = PageSize.A4; var move = 0.0;
                    for (int ind = 0; ind < index.Count; ind++)//
                    {
                        int e = (int)index[ind];
                        int ni = (int)ij[e][0].Value; int nj = (int)ij[e][1].Value; int sec = (int)ij[e][3].Value;
                        var ele_text = ((int)index[e]).ToString(); var ni_text = ni.ToString(); var nj_text = nj.ToString();
                        var name_text = secname[sec]; var lambda_text = Lambda[e].ToString("F").Substring(0, Digit((int)Lambda[e]));
                        var A_text = (A[sec] * 1e+4).ToString("F").Substring(0, Digit((int)(A[sec] * 1e+4)));
                        var alpha_text = alpha.ToString("F"); alpha_text = alpha_text.Substring(0, Math.Min(4, alpha_text.Length));
                        var Zy_text = (Zy[sec] * 1e+6).ToString("F"); Zy_text = Zy_text.Substring(0, Math.Min(Digit((int)(Zy[sec] * 1e+6)), Zy_text.Length));
                        var Zz_text = (Zz[sec] * 1e+6).ToString("F"); Zz_text = Zz_text.Substring(0, Math.Min(Digit((int)(Zz[sec] * 1e+6)), Zz_text.Length));
                        var fk_text = f_k[ind].ToString("F"); fk_text = fk_text.Substring(0, Math.Min(5, fk_text.Length)); var fk2_text = "";
                        var ft_text = f_t[ind].ToString("F"); ft_text = ft_text.Substring(0, Math.Min(5, ft_text.Length)); var ft2_text = "";
                        var fb_text = f_b[ind].ToString("F"); fb_text = fb_text.Substring(0, Math.Min(5, fb_text.Length)); var fb2_text = "";
                        var fs_text = f_s[ind].ToString("F"); fs_text = fs_text.Substring(0, Math.Min(5, fs_text.Length)); var fs2_text = "";
                        fk2_text = f_k2[ind].ToString("F"); fk2_text = fk2_text.Substring(0, Math.Min(5, fk2_text.Length));
                        ft2_text = f_t2[ind].ToString("F"); ft2_text = ft2_text.Substring(0, Math.Min(5, ft2_text.Length));
                        fb2_text = f_b2[ind].ToString("F"); fb2_text = fb2_text.Substring(0, Math.Min(5, fb2_text.Length));
                        fs2_text = f_s2[ind].ToString("F"); fs2_text = fs2_text.Substring(0, Math.Min(5, fs2_text.Length));
                        var Ni = 0.0; var Qyi = 0.0; var Qzi = 0.0; var Myi = 0.0; var Mzi = 0.0;
                        var Nj = 0.0; var Qyj = 0.0; var Qzj = 0.0; var Myj = 0.0; var Mzj = 0.0;
                        var Nc = 0.0; var Qyc = 0.0; var Qzc = 0.0; var Myc = 0.0; var Mzc = 0.0;
                        if (sec_f[0][0].Value == -9999 && sec_f2[0][0].Value != -9999)
                        {
                            Ni = sec_f2[e][0].Value; Qyi = Math.Abs(sec_f2[e][1].Value); Qzi = Math.Abs(sec_f2[e][2].Value);
                            Myi = Math.Abs(sec_f2[e][4].Value); Mzi = Math.Abs(sec_f2[e][5].Value);
                            Nj = -sec_f2[e][6].Value; Qyj = Math.Abs(sec_f2[e][7].Value); Qzj = Math.Abs(sec_f2[e][8].Value);
                            Myj = Math.Abs(sec_f2[e][10].Value); Mzj = Math.Abs(sec_f2[e][11].Value);
                            Nc = sec_f2[e][12].Value; Qyc = Math.Abs(sec_f2[e][13].Value); Qzc = Math.Abs(sec_f2[e][14].Value);
                            Myc = Math.Abs(sec_f2[e][16].Value); Mzc = Math.Abs(sec_f2[e][17].Value);
                        }
                        else
                        {
                            Ni = sec_f[e][0].Value; Qyi = Math.Abs(sec_f[e][1].Value); Qzi = Math.Abs(sec_f[e][2].Value);
                            Myi = Math.Abs(sec_f[e][4].Value); Mzi = Math.Abs(sec_f[e][5].Value);
                            Nj = -sec_f[e][6].Value; Qyj = Math.Abs(sec_f[e][7].Value); Qzj = Math.Abs(sec_f[e][8].Value);
                            Myj = Math.Abs(sec_f[e][10].Value); Mzj = Math.Abs(sec_f[e][11].Value);
                            Nc = sec_f[e][12].Value; Qyc = Math.Abs(sec_f[e][13].Value); Qzc = Math.Abs(sec_f[e][14].Value);
                            Myc = Math.Abs(sec_f[e][16].Value); Mzc = Math.Abs(sec_f[e][17].Value);
                        }
                        var Ni_text = Ni.ToString("F"); Ni_text = Ni_text.Substring(0, Math.Min(4, Ni_text.Length));
                        var Qyi_text = Qyi.ToString("F"); Qyi_text = Qyi_text.Substring(0, Math.Min(4, Qyi_text.Length));
                        var Qzi_text = Qzi.ToString("F"); Qzi_text = Qzi_text.Substring(0, Math.Min(4, Qzi_text.Length));
                        var Myi_text = Myi.ToString("F"); Myi_text = Myi_text.Substring(0, Math.Min(4, Myi_text.Length));
                        var Mzi_text = Mzi.ToString("F"); Mzi_text = Mzi_text.Substring(0, Math.Min(4, Mzi_text.Length));
                        var Nj_text = Nj.ToString("F"); Nj_text = Nj_text.Substring(0, Math.Min(4, Nj_text.Length));
                        var Qyj_text = Qyj.ToString("F"); Qyj_text = Qyj_text.Substring(0, Math.Min(4, Qyj_text.Length));
                        var Qzj_text = Qzj.ToString("F"); Qzj_text = Qzj_text.Substring(0, Math.Min(4, Qzj_text.Length));
                        var Myj_text = Myj.ToString("F"); Myj_text = Myj_text.Substring(0, Math.Min(4, Myj_text.Length));
                        var Mzj_text = Mzj.ToString("F"); Mzj_text = Mzj_text.Substring(0, Math.Min(4, Mzj_text.Length));
                        var Nc_text = Nc.ToString("F"); Nc_text = Nc_text.Substring(0, Math.Min(4, Nc_text.Length));
                        var Qyc_text = Qyc.ToString("F"); Qyc_text = Qyc_text.Substring(0, Math.Min(4, Qyc_text.Length));
                        var Qzc_text = Qzc.ToString("F"); Qzc_text = Qzc_text.Substring(0, Math.Min(4, Qzc_text.Length));
                        var Myc_text = Myc.ToString("F"); Myc_text = Myc_text.Substring(0, Math.Min(4, Myc_text.Length));
                        var Mzc_text = Mzc.ToString("F"); Mzc_text = Mzc_text.Substring(0, Math.Min(4, Mzc_text.Length));
                        var Mki_color = new List<XSolidBrush>(); var Mkj_color = new List<XSolidBrush>(); var Mkc_color = new List<XSolidBrush>();
                        var Qki_color = new List<XSolidBrush>(); var Qkj_color = new List<XSolidBrush>(); var Qkc_color = new List<XSolidBrush>();
                        var klist = new List<GH_Number>();
                        if (sec_f[0][0].Value == -9999 && sec_f2[0][0].Value != -9999) { klist = klist2all[ind]; }
                        else { klist = klistall[ind]; }
                        var Mki = (Math.Max(double.Parse(klist[3].ToString()), double.Parse(klist[4].ToString())) + double.Parse(klist[0].ToString()));
                        var Mki_text = Mki.ToString("F").Substring(0, 4); Mki_color.Add(new XSolidBrush(RGB((1 - Math.Min(Mki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                        var Mkj = (Math.Max(double.Parse(klist[8].ToString()), double.Parse(klist[9].ToString())) + double.Parse(klist[5].ToString()));
                        var Mkj_text = Mkj.ToString("F").Substring(0, 4); Mkj_color.Add(new XSolidBrush(RGB((1 - Math.Min(Mkj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                        var Mkc = (Math.Max(double.Parse(klist[13].ToString()), double.Parse(klist[14].ToString())) + double.Parse(klist[10].ToString()));
                        var Mkc_text = Mkc.ToString("F").Substring(0, 4); Mkc_color.Add(new XSolidBrush(RGB((1 - Math.Min(Mkc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                        var Qki = Math.Max(double.Parse(klist[1].ToString()), double.Parse(klist[2].ToString()));
                        var Qki_text = Qki.ToString("F").Substring(0, 4); Qki_color.Add(new XSolidBrush(RGB((1 - Math.Min(Qki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                        var Qkj = Math.Max(double.Parse(klist[6].ToString()), double.Parse(klist[7].ToString()));
                        var Qkj_text = Qkj.ToString("F").Substring(0, 4); Qkj_color.Add(new XSolidBrush(RGB((1 - Math.Min(Qkj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                        var Qkc = Math.Max(double.Parse(klist[11].ToString()), double.Parse(klist[12].ToString()));
                        var Qkc_text = Qkc.ToString("F").Substring(0, 4); Qkc_color.Add(new XSolidBrush(RGB((1 - Math.Min(Qkc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                        var OKi_text = "O.K."; var OKj_text = "O.K."; var OKc_text = "O.K.";
                        if (Mki > 1 || Qki > 1) { OKi_text = "N.G."; }
                        if (Mkj > 1 || Qkj > 1) { OKj_text = "N.G."; }
                        if (Mkc > 1 || Qkc > 1) { OKc_text = "N.G."; }
                        var values = new List<List<string>>();
                        values.Add(new List<string> { ele_text }); values.Add(new List<string> { name_text });
                        values.Add(new List<string> { lambda_text }); values.Add(new List<string> { A_text }); values.Add(new List<string> { alpha_text });
                        values.Add(new List<string> { Zy_text }); values.Add(new List<string> { Zz_text }); values.Add(new List<string> { "長期", "", "短期" });
                        if (sec_f[0][0].Value == -9999 && sec_f2[0][0].Value != -9999) { values.Add(new List<string> { "", "", fk2_text }); }
                        else { values.Add(new List<string> { fk_text, "", fk2_text }); }
                        if (sec_f[0][0].Value == -9999 && sec_f2[0][0].Value != -9999) { values.Add(new List<string> { "", "", ft2_text }); }
                        else { values.Add(new List<string> { ft_text, "", ft2_text }); }
                        if (sec_f[0][0].Value == -9999 && sec_f2[0][0].Value != -9999) { values.Add(new List<string> { "", "", fb2_text }); }
                        else { values.Add(new List<string> { fb_text, "", fb2_text }); }
                        if (sec_f[0][0].Value == -9999 && sec_f2[0][0].Value != -9999) { values.Add(new List<string> { "", "", fs2_text }); }
                        else { values.Add(new List<string> { fs_text, "", fs2_text }); }
                        values.Add(new List<string> { "i端", "中央", "j端" }); values.Add(new List<string> { ni_text, "", nj_text });
                        if (sec_f[0][0].Value == -9999 && sec_f2[0][0].Value != -9999) { values.Add(new List<string> { "短期検討" }); }
                        else { values.Add(new List<string> { "長期検討" }); }
                        values.Add(new List<string> { Ni_text, Nc_text, Nj_text }); values.Add(new List<string> { Myi_text, Myc_text, Myj_text }); values.Add(new List<string> { Mzi_text, Mzc_text, Mzj_text });
                        values.Add(new List<string> { Qyi_text, Qyc_text, Qyj_text }); values.Add(new List<string> { Qzi_text, Qzc_text, Qzj_text });
                        values.Add(new List<string> { Mki_text, Mkc_text, Mkj_text }); values.Add(new List<string> { Qki_text, Qkc_text, Qkj_text });
                        values.Add(new List<string> { OKi_text, OKc_text, OKj_text });
                        if (sec_f[0][0].Value != -9999 && sec_f2[0][0].Value != -9999)
                        {
                            var text = "短期検討";
                            values.Add(new List<string> { text });
                            Ni = sec_f2[e][0].Value; Qyi = Math.Abs(sec_f2[e][1].Value); Qzi = Math.Abs(sec_f2[e][2].Value);
                            Myi = Math.Abs(sec_f2[e][4].Value); Mzi = Math.Abs(sec_f2[e][5].Value);
                            Nj = -sec_f2[e][6].Value; Qyj = Math.Abs(sec_f2[e][7].Value); Qzj = Math.Abs(sec_f2[e][8].Value);
                            Myj = Math.Abs(sec_f2[e][10].Value); Mzj = Math.Abs(sec_f2[e][11].Value);
                            Nc = sec_f2[e][12].Value; Qyc = Math.Abs(sec_f2[e][13].Value); Qzc = Math.Abs(sec_f2[e][14].Value);
                            Myc = Math.Abs(sec_f2[e][16].Value); Mzc = Math.Abs(sec_f2[e][17].Value);
                            Ni_text = Ni.ToString("F"); Ni_text = Ni_text.Substring(0, Math.Min(4, Ni_text.Length));
                            Qyi_text = Qyi.ToString("F"); Qyi_text = Qyi_text.Substring(0, Math.Min(4, Qyi_text.Length));
                            Qzi_text = Qzi.ToString("F"); Qzi_text = Qzi_text.Substring(0, Math.Min(4, Qzi_text.Length));
                            Myi_text = Myi.ToString("F"); Myi_text = Myi_text.Substring(0, Math.Min(4, Myi_text.Length));
                            Mzi_text = Mzi.ToString("F"); Mzi_text = Mzi_text.Substring(0, Math.Min(4, Mzi_text.Length));
                            Nj_text = Nj.ToString("F"); Nj_text = Nj_text.Substring(0, Math.Min(4, Nj_text.Length));
                            Qyj_text = Qyj.ToString("F"); Qyj_text = Qyj_text.Substring(0, Math.Min(4, Qyj_text.Length));
                            Qzj_text = Qzj.ToString("F"); Qzj_text = Qzj_text.Substring(0, Math.Min(4, Qzj_text.Length));
                            Myj_text = Myj.ToString("F"); Myj_text = Myj_text.Substring(0, Math.Min(4, Myj_text.Length));
                            Mzj_text = Mzj.ToString("F"); Mzj_text = Mzj_text.Substring(0, Math.Min(4, Mzj_text.Length));
                            Nc_text = Nc.ToString("F"); Nc_text = Nc_text.Substring(0, Math.Min(4, Nc_text.Length));
                            Qyc_text = Qyc.ToString("F"); Qyc_text = Qyc_text.Substring(0, Math.Min(4, Qyc_text.Length));
                            Qzc_text = Qzc.ToString("F"); Qzc_text = Qzc_text.Substring(0, Math.Min(4, Qzc_text.Length));
                            Myc_text = Myc.ToString("F"); Myc_text = Myc_text.Substring(0, Math.Min(4, Myc_text.Length));
                            Mzc_text = Mzc.ToString("F"); Mzc_text = Mzc_text.Substring(0, Math.Min(4, Mzc_text.Length));
                            var klist2 = klist2all[ind];
                            Mki = (Math.Max(double.Parse(klist2[3].ToString()), double.Parse(klist2[4].ToString())) + double.Parse(klist2[0].ToString()));
                            Mki_text = Mki.ToString("F").Substring(0, 4); Mki_color.Add(new XSolidBrush(RGB((1 - Math.Min(Mki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            Mkj = (Math.Max(double.Parse(klist2[8].ToString()), double.Parse(klist2[9].ToString())) + double.Parse(klist2[5].ToString()));
                            Mkj_text = Mkj.ToString("F").Substring(0, 4); Mkj_color.Add(new XSolidBrush(RGB((1 - Math.Min(Mkj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            Mkc = (Math.Max(double.Parse(klist2[13].ToString()), double.Parse(klist2[14].ToString())) + double.Parse(klist2[10].ToString()));
                            Mkc_text = Mkc.ToString("F").Substring(0, 4); Mkc_color.Add(new XSolidBrush(RGB((1 - Math.Min(Mkc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            Qki = Math.Max(double.Parse(klist2[1].ToString()), double.Parse(klist2[2].ToString()));
                            Qki_text = Qki.ToString("F").Substring(0, 4); Qki_color.Add(new XSolidBrush(RGB((1 - Math.Min(Qki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            Qkj = Math.Max(double.Parse(klist2[6].ToString()), double.Parse(klist2[7].ToString()));
                            Qkj_text = Qkj.ToString("F").Substring(0, 4); Qkj_color.Add(new XSolidBrush(RGB((1 - Math.Min(Qkj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            Qkc = Math.Max(double.Parse(klist2[11].ToString()), double.Parse(klist2[12].ToString()));
                            Qkc_text = Qkc.ToString("F").Substring(0, 4); Qkc_color.Add(new XSolidBrush(RGB((1 - Math.Min(Qkc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            OKi_text = "O.K."; OKj_text = "O.K."; OKc_text = "O.K.";
                            if (Mki > 1 || Qki > 1) { OKi_text = "N.G."; }
                            if (Mkj > 1 || Qkj > 1) { OKj_text = "N.G."; }
                            if (Mkc > 1 || Qkc > 1) { OKc_text = "N.G."; }
                            values.Add(new List<string> { Ni_text, Nc_text, Nj_text }); values.Add(new List<string> { Myi_text, Myc_text, Myj_text }); values.Add(new List<string> { Mzi_text, Mzc_text, Mzj_text });
                            values.Add(new List<string> { Qyi_text, Qyc_text, Qyj_text }); values.Add(new List<string> { Qzi_text, Qzc_text, Qzj_text });
                            values.Add(new List<string> { Mki_text, Mkc_text, Mkj_text }); values.Add(new List<string> { Qki_text, Qkc_text, Qkj_text });
                            values.Add(new List<string> { OKi_text, OKc_text, OKj_text });
                        }
                        if (e % 12 == 0)
                        {
                            move = 0.0;
                            // 空白ページを作成。
                            page = document.AddPage();
                            // 描画するためにXGraphicsオブジェクトを取得。
                            gfx = XGraphics.FromPdfPage(page);
                            for (int i = 0; i < labels.Count; i++)//ラベル列**************************************************************************
                            {
                                gfx.DrawLine(pen, offset_x, offset_y + pitchy * i, offset_x + label_width, offset_y + pitchy * i);//横線
                                gfx.DrawLine(pen, offset_x + label_width, offset_y + pitchy * i, offset_x + label_width, offset_y + pitchy * (i + 1));//縦線
                                gfx.DrawString(labels[i], font, XBrushes.Black, new XRect(offset_x, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                                if (i == labels.Count - 1)
                                {
                                    i += 1;
                                    gfx.DrawLine(pen, offset_x, offset_y + pitchy * i, offset_x + label_width, offset_y + pitchy * i);//横線
                                }
                            }//***********************************************************************************************************************
                        }
                        else if (e % 6 == 0)
                        {
                            move = pitchy * 33;
                            for (int i = 0; i < labels.Count; i++)//ラベル列**************************************************************************
                            {
                                gfx.DrawLine(pen, offset_x, offset_y + pitchy * i + move, offset_x + label_width, offset_y + pitchy * i + move);//横線
                                gfx.DrawLine(pen, offset_x + label_width, offset_y + pitchy * i + move, offset_x + label_width, offset_y + pitchy * (i + 1) + move);//縦線
                                gfx.DrawString(labels[i], font, XBrushes.Black, new XRect(offset_x, offset_y + pitchy * i + move, label_width, offset_y + pitchy * (i + 1) + move), XStringFormats.TopCenter);
                                if (i == labels.Count - 1)
                                {
                                    i += 1;
                                    gfx.DrawLine(pen, offset_x, offset_y + pitchy * i + move, offset_x + label_width, offset_y + pitchy * i + move);//横線
                                }
                            }//***********************************************************************************************************************
                        }
                        for (int i = 0; i < values.Count; i++)
                        {
                            var j = e % 6;
                            gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i + move, offset_x + label_width + text_width * 3 * (j + 1), offset_y + pitchy * i + move);//横線
                            gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i + move, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * (i + 1) + move);//縦線
                            if (values[i].Count == 1)
                            {
                                gfx.DrawString(values[i][0], font, XBrushes.Black, new XRect(offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i + move, text_width * 3, offset_y + pitchy * (i + 1) + move), XStringFormats.TopCenter);
                            }
                            else
                            {
                                var color1 = XBrushes.Black; var color2 = XBrushes.Black; var color3 = XBrushes.Black; var f = font;
                                if (i == 20) { color1 = Mki_color[0]; color2 = Mkc_color[0]; color3 = Mkj_color[0]; f = fontbold; }
                                else if (i == 21) { color1 = Qki_color[0]; color2 = Qkc_color[0]; color3 = Qkj_color[0]; f = fontbold; }
                                else if (i == 29) { color1 = Mki_color[1]; color2 = Mkc_color[1]; color3 = Mkj_color[1]; f = fontbold; }
                                else if (i == 30) { color1 = Qki_color[1]; color2 = Qkc_color[1]; color3 = Qkj_color[1]; f = fontbold; }
                                gfx.DrawString(values[i][0], f, color1, new XRect(offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i + move, text_width, offset_y + pitchy * (i + 1) + move), XStringFormats.TopCenter);
                                gfx.DrawString(values[i][1], f, color2, new XRect(offset_x + label_width + text_width * 3 * j + text_width, offset_y + pitchy * i + move, text_width, offset_y + pitchy * (i + 1) + move), XStringFormats.TopCenter);
                                gfx.DrawString(values[i][2], f, color3, new XRect(offset_x + label_width + text_width * 3 * j + text_width * 2, offset_y + pitchy * i + move, text_width, offset_y + pitchy * (i + 1) + move), XStringFormats.TopCenter);
                            }
                            if (i == values.Count - 1)
                            {
                                i += 1;
                                gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i + move, offset_x + label_width + text_width * 3 * (j + 1), offset_y + pitchy * i + move);//横線
                            }
                        }
                    }
                    var dir = Path.GetDirectoryName(Rhino.RhinoDoc.ActiveDoc.Path);
                    // ドキュメントを保存。
                    var filename = dir + "/" + pdfname + ".pdf";
                    document.Save(filename);
                    // ビューアを起動。
                    Process.Start(new ProcessStartInfo(@filename) { UseShellExecute = true });
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
                return OpenSeesUtility.Properties.Resources.timberchecksnow;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("32329783-f711-4d9e-b9b0-03181f4671c6"); }
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
            private Rectangle radio_rec; private Rectangle radio_rec2; private Rectangle radio_rec3;
            private Rectangle radio_rec_1; private Rectangle text_rec_1;
            private Rectangle radio_rec_2; private Rectangle text_rec_2;
            private Rectangle radio_rec2_1; private Rectangle text_rec2_1;
            private Rectangle radio_rec2_2; private Rectangle text_rec2_2;
            private Rectangle radio_rec2_3; private Rectangle text_rec2_3;
            private Rectangle radio_rec2_4; private Rectangle text_rec2_4;
            private Rectangle radio_rec3_1; private Rectangle text_rec3_1;
            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int height = 137; int radi1 = 7; int radi2 = 4; int titleheight = 20;
                int pitchx = 8; int pitchy = 11; int textheight = 20;
                global_rec.Height += height;
                int width = global_rec.Width;
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

                radio_rec3 = radio_rec;
                radio_rec3.Y = radio_rec2.Y + radio_rec2.Height;
                radio_rec3.Height = textheight;

                radio_rec3_1 = radio_rec3;
                radio_rec3_1.X += 5; radio_rec3_1.Y += 5;
                radio_rec3_1.Height = radi1; radio_rec3_1.Width = radi1;

                text_rec3_1 = radio_rec3_1;
                text_rec3_1.X += pitchx; text_rec3_1.Y -= radi2;
                text_rec3_1.Height = textheight; text_rec3_1.Width = width;

                Bounds = global_rec;
            }
            Brush c1 = Brushes.Black; Brush c2 = Brushes.White; Brush c21 = Brushes.White; Brush c22 = Brushes.White; Brush c23 = Brushes.White; Brush c24 = Brushes.White; Brush c31 = Brushes.White;
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

                    GH_Capsule radio3 = GH_Capsule.CreateCapsule(radio_rec3, GH_Palette.White, 2, 0);
                    radio3.Render(graphics, Selected, Owner.Locked, false); radio3.Dispose();

                    GH_Capsule radio3_1 = GH_Capsule.CreateCapsule(radio_rec3_1, GH_Palette.Black, 5, 5);
                    radio3_1.Render(graphics, Selected, Owner.Locked, false); radio3_1.Dispose();
                    graphics.FillEllipse(c31, radio_rec3_1);
                    graphics.DrawString("PDF OUTPUT", GH_FontServer.Standard, Brushes.Black, text_rec3_1);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec1 = radio_rec_1; RectangleF rec2 = radio_rec_2;
                    RectangleF rec21 = radio_rec2_1; RectangleF rec22 = radio_rec2_2; RectangleF rec23 = radio_rec2_3; RectangleF rec24 = radio_rec2_4;
                    RectangleF rec31 = radio_rec3_1;
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
                    if (rec31.Contains(e.CanvasLocation))
                    {
                        if (c31 == Brushes.White) { c31 = Brushes.Black; SetButton("1", 1); }
                        else { c31 = Brushes.White; SetButton("1", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}