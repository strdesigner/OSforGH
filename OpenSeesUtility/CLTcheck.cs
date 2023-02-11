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
using Rhino;
///****************************************

namespace CLTCheck
{
    public class CLTCheck : GH_Component
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
        public CLTCheck()
          : base("Allowable stress design for CLT panels", "CLTCheck",
              "Allowable stress design(danmensantei) for CLT panels using Japanese Design Code",
              "OpenSees", "Analysis")
        {
        }
        public override bool IsPreviewCapable { get { return true; } }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
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
            pManager.AddNumberParameter("Standard allowable stress (In-plane bending)[N/mm2]", "Fb(inner)", "[...](DataList)[N/mm2]", GH_ParamAccess.list, new List<double> { 22.2 });///
            pManager.AddNumberParameter("Standard allowable stress (In-plane shear)[N/mm2]", "Fs(inner)", "[...](DataList)[N/mm2]", GH_ParamAccess.list, new List<double> { 1.8 });///
            pManager.AddNumberParameter("Lby", "Lby", "buckling length for local y axis[...](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddNumberParameter("Lbz", "Lbz", "buckling length for local z axis[...](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddNumberParameter("sectional_force", "sec_f", "[[Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("safe factor", "alpha", "Reduction rate taking into account cross-sectional defects", GH_ParamAccess.item, 0.9);///
            pManager.AddNumberParameter("Fb(inner)/Fb(outer)", "Fb(inner)/Fb(outer)", "Fb(inner)/Fb(outer) ratio", GH_ParamAccess.list, new List<double> { 10.8 / 12.67 });///9
            pManager.AddNumberParameter("Fs(inner)/Fs(outer)", "Fs(inner)/Fs(outer)", "Fs(inner)/Fs(outer) ration", GH_ParamAccess.list, new List<double> { 2.59 / 0.9 });///10
            pManager.AddNumberParameter("index", "index", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("index(burn)", "index(burn)", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("burnB", "burnB", "[double,double,...](Datalist)[m]", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("burnD", "burnD", "[double,double,...](Datalist)[m]", GH_ParamAccess.list, -9999);///
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
            pManager.AddNumberParameter("A", "A", "section area[...](DataList)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("Zy", "Zy", "section modulus around y axis[...](DataList)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("Zz", "Zz", "section modulus around z axis[...](DataList)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("fk", "fk", "[[Long-Terrm...],[Short-Terrm...]](DataTree)[N/mm2]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("ft", "ft", "[[Long-Terrm...],[Short-Terrm...]](DataTree)[N/mm2]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("fb(inner)", "fb(inner)", "[[Long-Terrm...],[Short-Terrm...]](DataTree)[N/mm2]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("fs(inner)", "fs(inner)", "[[Long-Terrm...],[Short-Terrm...]](DataTree)[N/mm2]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("fb(outer)", "fb(outer)", "[[Long-Terrm...],[Short-Terrm...]](DataTree)[N/mm2]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("fs(outer)", "fs(outer)", "[[Long-Terrm...],[Short-Terrm...]](DataTree)[N/mm2]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("safe factor", "alpha", "Reduction rate taking into account cross-sectional defects", GH_ParamAccess.item);///
            pManager.AddNumberParameter("sec_f", "sec_f", "[[Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("kentei_hi", "kentei", "[[for Ni,for Qyi,for Qzi,for Myi,for Mzi,for Nj,for Qyj,for Qzj,for Myj,for Mzj,for Nc,for Qyc,for Qzc,for Myc,for Mzc],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("burnB", "burnB", "[double,double,...](Datalist)[m]", GH_ParamAccess.list);///
            pManager.AddNumberParameter("burnD", "burnD", "[double,double,...](Datalist)[m]", GH_ParamAccess.list);///
            pManager.AddNumberParameter("kmax", "kmax", "[[ele. No.,Long-term max],[ele. No.,Short-term max]](DataTree)", GH_ParamAccess.tree);///
        }
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r);
            DA.GetDataTree("element_node_relationship", out GH_Structure<GH_Number> _ij); var ij_new = new GH_Structure<GH_Number>();
            DA.GetDataTree("sectional_force", out GH_Structure<GH_Number> _sec_f); var sec_f_new = new GH_Structure<GH_Number>();
            fontsize = 20.0; DA.GetData("fontsize", ref fontsize);
            List<double> A = new List<double>(); DA.GetDataList("section area", A); DA.SetDataList("A", A);
            List<double> Iy = new List<double>(); DA.GetDataList("Second moment of area around y-axis", Iy);
            List<double> Iz = new List<double>(); DA.GetDataList("Second moment of area around z-axis", Iz);
            List<double> Zy = new List<double>(); DA.GetDataList("Section modulus around y-axis", Zy); DA.SetDataList("Zy", Zy);
            List<double> Zz = new List<double>(); DA.GetDataList("Section modulus around z-axis", Zz); DA.SetDataList("Zz", Zz);
            List<double> Fc = new List<double>(); DA.GetDataList("Standard allowable stress (compression)[N/mm2]", Fc);
            List<double> Ft = new List<double>(); DA.GetDataList("Standard allowable stress (tensile)[N/mm2]", Ft);
            List<double> Fb = new List<double>(); DA.GetDataList("Standard allowable stress (In-plane bending)[N/mm2]", Fb);
            List<double> Fs = new List<double>(); DA.GetDataList("Standard allowable stress (In-plane shear)[N/mm2]", Fs);
            var bb = new List<double>(); DA.GetDataList("Fb(inner)/Fb(outer)", bb); var ss = new List<double>(); DA.GetDataList("Fs(inner)/Fs(outer)", ss);
            List<double> Fbo = new List<double>(); List<double> Fso = new List<double>();
            for (int i = 0; i < Fb.Count; i++)
            {
                if (bb.Count == 1 && ss.Count == 1) { Fbo.Add(Fb[i] / bb[0]); Fso.Add(Fs[i] / ss[0]); }
                else { Fbo.Add(Fb[i] / bb[i]); Fso.Add(Fs[i] / ss[i]); }
            }
            List<double> Lby = new List<double>(); DA.GetDataList("Lby", Lby); List<double> Lbz = new List<double>(); DA.GetDataList("Lbz", Lbz); List<double> Lambda = new List<double>();
            double alpha = 1.0; DA.GetData("safe factor", ref alpha); DA.SetData("safe factor", alpha);
            var r = _r.Branches; var ij = _ij.Branches; var sec_f = _sec_f.Branches;
            var fc = new List<double>(); var ft = new List<double>(); var fb = new List<double>(); var fs = new List<double>(); var fbo = new List<double>(); var fso = new List<double>();
            var fc2 = new List<double>(); var ft2 = new List<double>(); var fb2 = new List<double>(); var fs2 = new List<double>(); var fbo2 = new List<double>(); var fso2 = new List<double>();
            var f_c = new List<double>(); var f_t = new List<double>(); var f_b = new List<double>(); var f_s = new List<double>(); var f_bo = new List<double>(); var f_so = new List<double>(); var f_k = new List<double>();
            var f_c2 = new List<double>(); var f_t2 = new List<double>(); var f_b2 = new List<double>(); var f_s2 = new List<double>(); var f_k2 = new List<double>(); var f_bo2 = new List<double>(); var f_so2 = new List<double>();
            var kentei = new GH_Structure<GH_Number>(); int digit = 4;
            var unit = 1.0;///単位合わせのための係数
            unit /= 1000000.0;
            unit *= 1000.0;
            List<double> index = new List<double>(); List<double> index3 = new List<double>();
            DA.GetDataList("index", index); DA.GetDataList("index(burn)", index3);
            if (index[0] == -9999)
            {
                index = new List<double>();
                for (int e = 0; e < ij.Count; e++) { index.Add(e); }
            }
            if (r[0][0].Value != -9999 && ij[0][0].Value != -9999 && sec_f[0][0].Value != -9999)
            {
                for (int i = 0; i < Fc.Count; i++)
                {
                    fc.Add(Fc[i] * 1.1 / 3.0 * alpha); ft.Add(Ft[i] * 1.1 / 3.0 * alpha); fb.Add(Fb[i] * 1.1 / 3.0 * alpha); fs.Add(Fs[i] * 1.1 / 3.0); fbo.Add(Fbo[i] * 1.1 / 3.0 * alpha); fso.Add(Fso[i] * 1.1 / 3.0 * alpha);
                    fc2.Add(Fc[i] * 2.2 / 3.0 * alpha); ft2.Add(Ft[i] * 2.2 / 3.0 * alpha); fb2.Add(Fb[i] * 2.2 / 3.0 * alpha); fs2.Add(Fs[i] * 2.2 / 3.0 * alpha); fbo2.Add(Fbo[i] * 2.2 / 3.0 * alpha); fso2.Add(Fso[i] * 2.2 / 3.0 * alpha);
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
                if (sec_f[0][0].Value != -9999)
                {
                    if (index3[0] != -9999)//燃えしろ設計
                    {
                        var burnB = new List<double>(); var burnD = new List<double>();
                        DA.GetDataList("burnB", burnB); DA.GetDataList("burnD", burnD); DA.SetDataList("burnB", burnB); DA.SetDataList("burnD", burnD);
                        for (int ind = 0; ind < index3.Count; ind++)
                        {
                            int e = (int)index3[ind];
                            int mat = (int)ij[e][2].Value; int sec = (int)ij[e][3].Value;
                            var iy = Iy[sec]; var iz = Iz[sec];
                            var b = Math.Pow(144 * Math.Pow(iz, 3) / iy, 1.0 / 8.0) - burnB[ind];
                            var d = Math.Pow(144 * Math.Pow(iy, 3) / iz, 1.0 / 8.0) - burnD[ind];
                            iy = b * Math.Pow(d, 3) / 12.0; iz = d * Math.Pow(b, 3) / 12.0; var a = b * d; var zy = b * Math.Pow(d, 2) / 6.0; var zz = d * Math.Pow(b, 2) / 6.0;
                            var iby = Math.Sqrt(iy / a); var ibz = Math.Sqrt(iz / a);
                            var lam = Math.Max(Lby[e] / iby, Lbz[e] / ibz);
                            Lambda.Add(lam); f_c.Add(fc2[mat]); f_t.Add(ft2[mat]); f_b.Add(fb2[mat]); f_s.Add(fs2[mat]); f_bo.Add(fbo2[mat]); f_so.Add(fso2[mat]);
                            if (lam <= 30)
                            {
                                f_k.Add(fc2[mat]);
                            }
                            else if (lam <= 100)
                            {
                                f_k.Add((1.3 - 0.01 * lam) * fc2[mat]);
                            }
                            else
                            {
                                f_k.Add(fc2[mat] * (3000 / Math.Pow(lam, 2)));
                            }
                            List<GH_Number> klist = new List<GH_Number>();//=[0:sigma_c or sigma_t, 1:tau_y, 2:tau_z, 3:sigma_by, 4:sigma_zy, 5:sigma_c or sigma_t, 6:tau_y, 7:tau_z, 8:sigma_by, 9:sigma_zy, 10:sigma_c or sigma_t, 11:tau_y, 12:tau_z, 13:sigma_by, 14:sigma_zy]
                            var Ni = -sec_f[e][0].Value; var Qyi = Math.Abs(sec_f[e][1].Value); var Qzi = Math.Abs(sec_f[e][2].Value);
                            var Myi = Math.Abs(sec_f[e][4].Value); var Mzi = Math.Abs(sec_f[e][5].Value);
                            var Nj = sec_f[e][6].Value; var Qyj = Math.Abs(sec_f[e][7].Value); var Qzj = Math.Abs(sec_f[e][8].Value);
                            var Myj = Math.Abs(sec_f[e][10].Value); var Mzj = Math.Abs(sec_f[e][11].Value);
                            var Nc = -sec_f[e][12].Value; var Qyc = Math.Abs(sec_f[e][13].Value); var Qzc = Math.Abs(sec_f[e][14].Value);
                            var Myc = Math.Abs(sec_f[e][16].Value); var Mzc = Math.Abs(sec_f[e][17].Value);
                            if (Ni < 0) { klist.Add(new GH_Number(Math.Abs(Ni) / a / f_k[ind] * unit)); }
                            else { klist.Add(new GH_Number(Math.Abs(Ni) / a / ft2[mat] * unit)); }
                            klist.Add(new GH_Number(Qyi / a / fso2[mat] * unit)); klist.Add(new GH_Number(Qzi / a / fs2[mat] * unit));
                            klist.Add(new GH_Number(Myi / zy / fb2[mat] * unit)); klist.Add(new GH_Number(Mzi / zz / fbo2[mat] * unit));
                            if (Nj < 0) { klist.Add(new GH_Number(Math.Abs(Nj) / a / f_k[ind] * unit)); }
                            else { klist.Add(new GH_Number(Math.Abs(Nj) / a / ft2[mat] * unit)); }
                            klist.Add(new GH_Number(Qyj / a / fso2[mat] * unit)); klist.Add(new GH_Number(Qzj / a / fs2[mat] * unit));
                            klist.Add(new GH_Number(Myj / zy / fb2[mat] * unit)); klist.Add(new GH_Number(Mzj / zz / fbo2[mat] * unit));
                            if (Nc < 0) { klist.Add(new GH_Number(Math.Abs(Nc) / a / f_k[ind] * unit)); }
                            else { klist.Add(new GH_Number(Math.Abs(Nc) / a / ft2[mat] * unit)); }
                            klist.Add(new GH_Number(Qyc / a / fso2[mat] * unit)); klist.Add(new GH_Number(Qzc / a / fs2[mat] * unit));
                            klist.Add(new GH_Number(Myc / zy / fb2[mat] * unit)); klist.Add(new GH_Number(Mzc / zz / fbo2[mat] * unit));
                            var flist = new List<GH_Number>();
                            for (int i = 0; i < 18; i++) { flist.Add(new GH_Number(sec_f[e][i].Value)); }
                            kentei.AppendRange(klist, new GH_Path(new int[] { 0, ind })); sec_f_new.AppendRange(flist, new GH_Path(new int[] { 0, ind })); ij_new.AppendRange(ij[e], new GH_Path(ind));
                            if (on_off_12 == 1)
                            {
                                var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                                var r1 = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value); var r2 = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value);
                                var rc = (r1 + r2) / 2.0; var ri = (r1 + rc) / 2.0; var rj = (r2 + rc) / 2.0;
                                if (on_off_21 == 1)
                                {
                                    var k = Math.Max(klist[3].Value, klist[4].Value) + klist[0].Value;//i端の検定比
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(ri);
                                    var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(klist[8].Value, klist[9].Value) + klist[5].Value;//j端の検定比
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rj);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(klist[13].Value, klist[14].Value) + klist[10].Value;//中央の検定比
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rc);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                }
                                else if (on_off_22 == 1)
                                {
                                    var k = Math.Max(klist[1].Value, klist[2].Value);
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(ri);
                                    var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(klist[6].Value, klist[7].Value);
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rj);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(klist[11].Value, klist[12].Value);
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rc);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                }
                                else if (on_off_23 == 1)
                                {
                                    var k = Math.Max(Math.Max(klist[3].Value, klist[4].Value) + klist[0].Value, Math.Max(Math.Max(klist[1].Value, klist[2].Value), klist[0].Value));
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(ri);
                                    var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(Math.Max(klist[8].Value, klist[9].Value) + klist[5].Value, Math.Max(Math.Max(klist[6].Value, klist[7].Value), klist[5].Value));
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rj);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(Math.Max(klist[13].Value, klist[14].Value) + klist[10].Value, Math.Max(Math.Max(klist[11].Value, klist[12].Value), klist[10].Value));
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rc);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                }
                                else if (on_off_24 == 1)
                                {
                                    var k = lam;
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rc);
                                    var color = Color.Crimson;
                                    _c.Add(color);
                                }
                            }
                        }
                        DA.SetDataTree(14, kentei); DA.SetDataTree(1, ij_new); DA.SetDataList("index", index3); DA.SetDataList("lambda", Lambda);
                        var f_ktree = new GH_Structure<GH_Number>(); var f_ttree = new GH_Structure<GH_Number>(); var f_btree = new GH_Structure<GH_Number>(); var f_stree = new GH_Structure<GH_Number>(); var f_botree = new GH_Structure<GH_Number>(); var f_sotree = new GH_Structure<GH_Number>();
                        var f_klist = new List<GH_Number>(); var f_tlist = new List<GH_Number>(); var f_blist = new List<GH_Number>(); var f_slist = new List<GH_Number>(); var f_bolist = new List<GH_Number>(); var f_solist = new List<GH_Number>();
                        for (int i = 0; i < f_k.Count; i++)
                        {
                            f_klist.Add(new GH_Number(f_k[i])); f_tlist.Add(new GH_Number(f_t[i])); f_blist.Add(new GH_Number(f_b[i])); f_slist.Add(new GH_Number(f_s[i])); f_bolist.Add(new GH_Number(f_bo[i])); f_solist.Add(new GH_Number(f_so[i]));
                        }
                        f_ktree.AppendRange(f_klist, new GH_Path(0));
                        f_ttree.AppendRange(f_tlist, new GH_Path(0));
                        f_btree.AppendRange(f_blist, new GH_Path(0));
                        f_stree.AppendRange(f_slist, new GH_Path(0));
                        f_botree.AppendRange(f_bolist, new GH_Path(0));
                        f_sotree.AppendRange(f_solist, new GH_Path(0));
                        DA.SetDataTree(6, f_ktree); DA.SetDataTree(7, f_ttree); DA.SetDataTree(8, f_btree); DA.SetDataTree(9, f_stree); DA.SetDataTree(10, f_botree); DA.SetDataTree(11, f_sotree); DA.SetDataTree(13, sec_f_new);
                    }
                    else
                    {
                        int m2 = sec_f[0].Count;
                        for (int ind = 0; ind < index.Count; ind++)
                        {
                            int e = (int)index[ind];
                            int mat = (int)ij[e][2].Value; int sec = (int)ij[e][3].Value;
                            var iy = Iy[sec]; var iz = Iz[sec]; var a = A[sec];
                            var iby = Math.Sqrt(iy / a); var ibz = Math.Sqrt(iz / a);
                            var lam = Math.Max(Lby[e] / iby, Lbz[e] / ibz);
                            Lambda.Add(lam); f_c.Add(fc[mat]); f_t.Add(ft[mat]); f_b.Add(fb[mat]); f_s.Add(fs[mat]); f_bo.Add(fbo[mat]); f_so.Add(fso[mat]); f_c2.Add(fc2[mat]); f_t2.Add(ft2[mat]); f_b2.Add(fb2[mat]); f_s2.Add(fs2[mat]); f_bo2.Add(fbo2[mat]); f_so2.Add(fso2[mat]);
                            if (lam <= 30)
                            {
                                f_k.Add(fc[mat]); f_k2.Add(fc2[mat]);
                            }
                            else if (lam <= 100)
                            {
                                f_k.Add((1.3 - 0.01 * lam) * fc[mat]); f_k2.Add((1.3 - 0.01 * lam) * fc2[mat]);
                            }
                            else
                            {
                                f_k.Add(fc[mat] * (3000 / Math.Pow(lam, 2))); f_k2.Add(fc2[mat] * (3000 / Math.Pow(lam, 2)));
                            }
                            var zy = Zy[sec]; var zz = Zz[sec];
                            List<GH_Number> klist = new List<GH_Number>();//=[0:sigma_c or sigma_t, 1:tau_y, 2:tau_z, 3:sigma_by, 4:sigma_zy, 5:sigma_c or sigma_t, 6:tau_y, 7:tau_z, 8:sigma_by, 9:sigma_zy, 10:sigma_c or sigma_t, 11:tau_y, 12:tau_z, 13:sigma_by, 14:sigma_zy]
                            var Ni = -sec_f[e][0].Value; var Qyi = Math.Abs(sec_f[e][1].Value); var Qzi = Math.Abs(sec_f[e][2].Value);
                            var Myi = Math.Abs(sec_f[e][4].Value); var Mzi = Math.Abs(sec_f[e][5].Value);
                            var Nj = sec_f[e][6].Value; var Qyj = Math.Abs(sec_f[e][7].Value); var Qzj = Math.Abs(sec_f[e][8].Value);
                            var Myj = Math.Abs(sec_f[e][10].Value); var Mzj = Math.Abs(sec_f[e][11].Value);
                            var Nc = -sec_f[e][12].Value; var Qyc = Math.Abs(sec_f[e][13].Value); var Qzc = Math.Abs(sec_f[e][14].Value);
                            var Myc = Math.Abs(sec_f[e][16].Value); var Mzc = Math.Abs(sec_f[e][17].Value);

                            if (Ni < 0) { klist.Add(new GH_Number(Math.Abs(Ni) / a / f_k[ind] * unit)); }
                            else { klist.Add(new GH_Number(Math.Abs(Ni) / a / ft[mat] * unit)); }
                            klist.Add(new GH_Number(Qyi / a / fso[mat] * unit)); klist.Add(new GH_Number(Qzi / a / fs[mat] * unit));
                            klist.Add(new GH_Number(Myi / zy / fb[mat] * unit)); klist.Add(new GH_Number(Mzi / zz / fbo[mat] * unit));
                            if (Nj < 0) { klist.Add(new GH_Number(Math.Abs(Nj) / a / f_k[ind] * unit)); }
                            else { klist.Add(new GH_Number(Math.Abs(Nj) / a / ft[mat] * unit)); }
                            klist.Add(new GH_Number(Qyj / a / fso[mat] * unit)); klist.Add(new GH_Number(Qzj / a / fs[mat] * unit));
                            klist.Add(new GH_Number(Myj / zy / fb[mat] * unit)); klist.Add(new GH_Number(Mzj / zz / fbo[mat] * unit));
                            if (Nc < 0) { klist.Add(new GH_Number(Math.Abs(Nc) / a / f_k[ind] * unit)); }
                            else { klist.Add(new GH_Number(Math.Abs(Nc) / a / ft[mat] * unit)); }
                            klist.Add(new GH_Number(Qyc / a / fso[mat] * unit)); klist.Add(new GH_Number(Qzc / a / fs[mat] * unit));
                            klist.Add(new GH_Number(Myc / zy / fb[mat] * unit)); klist.Add(new GH_Number(Mzc / zz / fbo[mat] * unit));
                            var flist = new List<GH_Number>();
                            for (int i = 0; i < 18; i++) { flist.Add(new GH_Number(sec_f[e][i].Value)); }
                            kentei.AppendRange(klist, new GH_Path(new int[] { 0, ind })); sec_f_new.AppendRange(flist, new GH_Path(new int[] { 0, ind })); ij_new.AppendRange(ij[e], new GH_Path(ind));
                            if (on_off_11 == 1)
                            {
                                var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                                var r1 = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value); var r2 = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value);
                                var rc = (r1 + r2) / 2.0; var ri = (r1 + rc) / 2.0; var rj = (r2 + rc) / 2.0;
                                if (on_off_21 == 1)
                                {
                                    var k = Math.Max(klist[3].Value, klist[4].Value) + klist[0].Value;//i端の検定比
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(ri);
                                    var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(klist[8].Value, klist[9].Value) + klist[5].Value;//j端の検定比
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rj);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(klist[13].Value, klist[14].Value) + klist[10].Value;//中央の検定比
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rc);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                }
                                else if (on_off_22 == 1)
                                {
                                    var k = Math.Max(klist[1].Value, klist[2].Value);
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(ri);
                                    var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(klist[6].Value, klist[7].Value);
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rj);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(klist[11].Value, klist[12].Value);
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rc);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                }
                                else if (on_off_23 == 1)
                                {
                                    var k = Math.Max(Math.Max(klist[3].Value, klist[4].Value) + klist[0].Value, Math.Max(Math.Max(klist[1].Value, klist[2].Value), klist[0].Value));
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(ri);
                                    var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(Math.Max(klist[8].Value, klist[9].Value) + klist[5].Value, Math.Max(Math.Max(klist[6].Value, klist[7].Value), klist[5].Value));
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rj);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(Math.Max(klist[13].Value, klist[14].Value) + klist[10].Value, Math.Max(Math.Max(klist[11].Value, klist[12].Value), klist[10].Value));
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rc);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                }
                                else if (on_off_24 == 1)
                                {
                                    var k = lam;
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rc);
                                    var color = Color.Crimson;
                                    _c.Add(color);
                                }
                            }
                            else if (m2 == 18 && on_off_12 == 1)
                            {
                                var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                                var r1 = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value); var r2 = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value);
                                var rc = (r1 + r2) / 2.0; var ri = (r1 + rc) / 2.0; var rj = (r2 + rc) / 2.0;

                                klist = new List<GH_Number>();
                                if (Ni < 0) { klist.Add(new GH_Number(Math.Abs(Ni) / a / f_k2[ind] * unit)); }
                                else { klist.Add(new GH_Number(Math.Abs(Ni) / a / ft2[mat] * unit)); }
                                klist.Add(new GH_Number(Qyi / a / fso2[mat] * unit)); klist.Add(new GH_Number(Qzi / a / fs2[mat] * unit));
                                klist.Add(new GH_Number(Myi / zy / fb2[mat] * unit)); klist.Add(new GH_Number(Mzi / zz / fbo2[mat] * unit));
                                if (Nj < 0) { klist.Add(new GH_Number(Math.Abs(Nj) / a / f_k2[ind] * unit)); }
                                else { klist.Add(new GH_Number(Math.Abs(Nj) / a / ft2[mat] * unit)); }
                                klist.Add(new GH_Number(Qyj / a / fso2[mat] * unit)); klist.Add(new GH_Number(Qzj / a / fs2[mat] * unit));
                                klist.Add(new GH_Number(Myj / zy / fb2[mat] * unit)); klist.Add(new GH_Number(Mzj / zz / fbo2[mat] * unit));
                                if (Nc < 0) { klist.Add(new GH_Number(Math.Abs(Nc) / a / f_k2[ind] * unit)); }
                                else { klist.Add(new GH_Number(Math.Abs(Nc) / a / ft2[mat] * unit)); }
                                klist.Add(new GH_Number(Qyc / a / fso2[mat] * unit)); klist.Add(new GH_Number(Qzc / a / fs2[mat] * unit));
                                klist.Add(new GH_Number(Myc / zy / fb2[mat] * unit)); klist.Add(new GH_Number(Mzc / zz / fbo2[mat] * unit));
                                kentei.AppendRange(klist, new GH_Path(new int[] { 0, ind }));
                                if (on_off_21 == 1)
                                {
                                    var k = Math.Max(klist[3].Value, klist[4].Value) + klist[0].Value;//i端の検定比
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(ri);
                                    var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(klist[8].Value, klist[9].Value) + klist[5].Value;//j端の検定比
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rj);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(klist[13].Value, klist[14].Value) + klist[10].Value;//中央の検定比
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rc);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                }
                                else if (on_off_22 == 1)
                                {
                                    var k = Math.Max(klist[1].Value, klist[2].Value);
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(ri);
                                    var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(klist[6].Value, klist[7].Value);
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rj);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(klist[11].Value, klist[12].Value);
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rc);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                }
                                else if (on_off_23 == 1)
                                {
                                    var k = Math.Max(Math.Max(klist[3].Value, klist[4].Value) + klist[0].Value, Math.Max(Math.Max(klist[1].Value, klist[2].Value), klist[0].Value));
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(ri);
                                    var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(Math.Max(klist[8].Value, klist[9].Value) + klist[5].Value, Math.Max(Math.Max(klist[6].Value, klist[7].Value), klist[5].Value));
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rj);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                    k = Math.Max(Math.Max(klist[13].Value, klist[14].Value) + klist[10].Value, Math.Max(Math.Max(klist[11].Value, klist[12].Value), klist[10].Value));
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rc);
                                    color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                    _c.Add(color);
                                }
                                else if (on_off_24 == 1)
                                {
                                    var k = lam;
                                    _text.Add(k.ToString("F").Substring(0, digit));
                                    _p.Add(rc);
                                    var color = Color.Crimson;
                                    _c.Add(color);
                                }
                            }
                        }
                        if (m2 / 18 == 3 || m2 / 18 == 5)
                        {
                            for (int ind = 0; ind < index.Count; ind++)
                            {
                                int e = (int)index[ind];
                                int mat = (int)ij[e][2].Value; int sec = (int)ij[e][3].Value;
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
                                var NXi2 = -NXi; var QXyi2 = -QXyi; var QXzi2 = -QXzi; var MXyi2 = -MXyi; var MXzi2 = -MXzi;
                                var NXj2 = -NXj; var QXyj2 = -QXyj; var QXzj2 = -QXzj; var MXyj2 = -MXyj; var MXzj2 = -MXzj;
                                var NXc2 = -NXc; var QXyc2 = -QXyc; var QXzc2 = -QXzc; var MXyc2 = -MXyc; var MXzc2 = -MXzc;
                                var NYi2 = -NYi; var QYyi2 = -QYyi; var QYzi2 = -QYzi; var MYyi2 = -MYyi; var MYzi2 = -MYzi;
                                var NYj2 = -NYj; var QYyj2 = -QYyj; var QYzj2 = -QYzj; var MYyj2 = -MYyj; var MYzj2 = -MYzj;
                                var NYc2 = -NYc; var QYyc2 = -QYyc; var QYzc2 = -QYzc; var MYyc2 = -MYyc; var MYzc2 = -MYzc;
                                if (m2 / 18 == 5)
                                {
                                    NXi2 = -sec_f[e][18 + 0 + 18 * 2].Value; QXyi2 = Math.Abs(sec_f[e][18 + 1 + 18 * 2].Value); QXzi2 = Math.Abs(sec_f[e][18 + 2 + 18 * 2].Value);
                                    MXyi2 = Math.Abs(sec_f[e][18 + 4 + 18 * 2].Value); MXzi2 = Math.Abs(sec_f[e][18 + 5 + 18 * 2].Value);
                                    NXj2 = sec_f[e][18 + 6 + 18 * 2].Value; QXyj2 = Math.Abs(sec_f[e][18 + 7 + 18 * 2].Value); QXzj2 = Math.Abs(sec_f[e][18 + 8 + 18 * 2].Value);
                                    MXyj2 = Math.Abs(sec_f[e][18 + 10 + 18 * 2].Value); MXzj2 = Math.Abs(sec_f[e][18 + 11 + 18 * 2].Value);
                                    NXc2 = -sec_f[e][18 + 12 + 18 * 2].Value; QXyc2 = Math.Abs(sec_f[e][18 + 13 + 18 * 2].Value); QXzc2 = Math.Abs(sec_f[e][18 + 14 + 18 * 2].Value);
                                    MXyc2 = Math.Abs(sec_f[e][18 + 16 + 18 * 2].Value); MXzc2 = Math.Abs(sec_f[e][18 + 17 + 18 * 2].Value);
                                    NYi2 = -sec_f[e][18 * 2 + 0 + 18 * 2].Value; QYyi2 = Math.Abs(sec_f[e][18 * 2 + 1 + 18 * 2].Value); QYzi2 = Math.Abs(sec_f[e][18 * 2 + 2 + 18 * 2].Value);
                                    MYyi2 = Math.Abs(sec_f[e][18 * 2 + 4 + 18 * 2].Value); MYzi2 = Math.Abs(sec_f[e][18 * 2 + 5 + 18 * 2].Value);
                                    NYj2 = sec_f[e][18 * 2 + 6 + 18 * 2].Value; QYyj2 = Math.Abs(sec_f[e][18 * 2 + 7 + 18 * 2].Value); QYzj2 = Math.Abs(sec_f[e][18 * 2 + 8 + 18 * 2].Value);
                                    MYyj2 = Math.Abs(sec_f[e][18 * 2 + 10 + 18 * 2].Value); MYzj2 = Math.Abs(sec_f[e][18 * 2 + 11 + 18 * 2].Value);
                                    NYc2 = -sec_f[e][18 * 2 + 12 + 18 * 2].Value; QYyc2 = Math.Abs(sec_f[e][18 * 2 + 13 + 18 * 2].Value); QYzc2 = Math.Abs(sec_f[e][18 * 2 + 14 + 18 * 2].Value);
                                    MYyc2 = Math.Abs(sec_f[e][18 * 2 + 16 + 18 * 2].Value); MYzc2 = Math.Abs(sec_f[e][18 * 2 + 17 + 18 * 2].Value);
                                }

                                if (Ni + NXi < 0) { k1list.Add(new GH_Number(Math.Abs(Ni + NXi) / a / f_k2[ind] * unit)); }
                                else { k1list.Add(new GH_Number(Math.Abs(Ni + NXi) / a / ft2[mat] * unit)); }
                                k1list.Add(new GH_Number((Qyi + QXyi) / a / fso2[mat] * unit)); k1list.Add(new GH_Number((Qzi + QXzi) / a / fs2[mat] * unit));
                                k1list.Add(new GH_Number((Myi + MXyi) / zy / fb2[mat] * unit)); k1list.Add(new GH_Number((Mzi + MXzi) / zz / fbo2[mat] * unit));
                                if (Nj + NXj < 0) { k1list.Add(new GH_Number(Math.Abs(Nj + NXj) / a / f_k2[ind] * unit)); }
                                else { k1list.Add(new GH_Number(Math.Abs(Nj + NXj) / a / ft2[mat] * unit)); }
                                k1list.Add(new GH_Number((Qyj + QXyj) / a / fso2[mat] * unit)); k1list.Add(new GH_Number((Qzj + QXzj) / a / fs2[mat] * unit));
                                k1list.Add(new GH_Number((Myj + MXyj) / zy / fb2[mat] * unit)); k1list.Add(new GH_Number((Mzj + MXzj) / zz / fbo2[mat] * unit));
                                if (Nc + NXc < 0) { k1list.Add(new GH_Number(Math.Abs(Nc + NXc) / a / f_k2[ind] * unit)); }
                                else { k1list.Add(new GH_Number(Math.Abs(Nc + NXc) / a / ft2[mat] * unit)); }
                                k1list.Add(new GH_Number((Qyc + QXyc) / a / fso2[mat] * unit)); k1list.Add(new GH_Number((Qzc + QXzc) / a / fs2[mat] * unit));
                                k1list.Add(new GH_Number((Myc + MXyc) / zy / fb2[mat] * unit)); k1list.Add(new GH_Number((Mzc + MXzc) / zz / fbo2[mat] * unit));
                                var flist = new List<GH_Number>();
                                for (int i = 0; i < 18; i++) { flist.Add(new GH_Number(sec_f[e][18 + i].Value)); }//+X
                                kentei.AppendRange(k1list, new GH_Path(new int[] { 1, ind })); sec_f_new.AppendRange(flist, new GH_Path(new int[] { 1, ind }));

                                if (Ni + NYi < 0) { k2list.Add(new GH_Number(Math.Abs(Ni + NYi) / a / f_k2[ind] * unit)); }
                                else { k2list.Add(new GH_Number(Math.Abs(Ni + NYi) / a / ft2[mat] * unit)); }
                                k2list.Add(new GH_Number((Qyi + QYyi) / a / fso2[mat] * unit)); k2list.Add(new GH_Number((Qzi + QYzi) / a / fs2[mat] * unit));
                                k2list.Add(new GH_Number((Myi + MYyi) / zy / fb2[mat] * unit)); k2list.Add(new GH_Number((Mzi + MYzi) / zz / fbo2[mat] * unit));
                                if (Nj + NYj < 0) { k2list.Add(new GH_Number(Math.Abs(Nj + NYj) / a / f_k2[ind] * unit)); }
                                else { k2list.Add(new GH_Number(Math.Abs(Nj + NYj) / a / ft2[mat] * unit)); }
                                k2list.Add(new GH_Number((Qyj + QYyj) / a / fso2[mat] * unit)); k2list.Add(new GH_Number((Qzj + QYzj) / a / fs2[mat] * unit));
                                k2list.Add(new GH_Number((Myj + MYyj) / zy / fb2[mat] * unit)); k2list.Add(new GH_Number((Mzj + MYzj) / zz / fbo2[mat] * unit));
                                if (Nc + NYc < 0) { k2list.Add(new GH_Number(Math.Abs(Nc + NYc) / a / f_k2[ind] * unit)); }
                                else { k2list.Add(new GH_Number(Math.Abs(Nc + NYc) / a / ft2[mat] * unit)); }
                                k2list.Add(new GH_Number((Qyc + QYyc) / a / fso2[mat] * unit)); k2list.Add(new GH_Number((Qzc + QYzc) / a / fs2[mat] * unit));
                                k2list.Add(new GH_Number((Myc + MYyc) / zy / fb2[mat] * unit)); k2list.Add(new GH_Number((Mzc + MYzc) / zz / fbo2[mat] * unit));
                                flist = new List<GH_Number>();
                                for (int i = 0; i < 18; i++) { flist.Add(new GH_Number(sec_f[e][18 * 2 + i].Value)); }//+Y
                                kentei.AppendRange(k2list, new GH_Path(new int[] { 2, ind })); sec_f_new.AppendRange(flist, new GH_Path(new int[] { 2, ind }));

                                if (Ni + NXi2 < 0) { k3list.Add(new GH_Number(Math.Abs(Ni + NXi2) / a / f_k2[ind] * unit)); }
                                else { k3list.Add(new GH_Number(Math.Abs(Ni + NXi2) / a / ft2[mat] * unit)); }
                                k3list.Add(new GH_Number((Qyi + QXyi2) / a / fso2[mat] * unit)); k3list.Add(new GH_Number((Qzi + QXzi2) / a / fs2[mat] * unit));
                                k3list.Add(new GH_Number((Myi + MXyi2) / zy / fb2[mat] * unit)); k3list.Add(new GH_Number((Mzi + MXzi2) / zz / fbo2[mat] * unit));
                                if (Nj + NXj2 < 0) { k3list.Add(new GH_Number(Math.Abs(Nj + NXj2) / a / f_k2[ind] * unit)); }
                                else { k3list.Add(new GH_Number(Math.Abs(Nj + NXj2) / a / ft2[mat] * unit)); }
                                k3list.Add(new GH_Number((Qyj + QXyj2) / a / fso2[mat] * unit)); k3list.Add(new GH_Number((Qzj + QXzj2) / a / fs2[mat] * unit));
                                k3list.Add(new GH_Number((Myj + MXyj2) / zy / fb2[mat] * unit)); k3list.Add(new GH_Number((Mzj + MXzj2) / zz / fbo2[mat] * unit));
                                if (Nc + NXc2 < 0) { k3list.Add(new GH_Number(Math.Abs(Nc + NXc) / a / f_k2[ind] * unit)); }
                                else { k3list.Add(new GH_Number(Math.Abs(Nc + NXc2) / a / ft2[mat] * unit)); }
                                k3list.Add(new GH_Number((Qyc + QXyc2) / a / fso2[mat] * unit)); k3list.Add(new GH_Number((Qzc + QXzc2) / a / fs2[mat] * unit));
                                k3list.Add(new GH_Number((Myc + MXyc2) / zy / fb2[mat] * unit)); k3list.Add(new GH_Number((Mzc + MXzc2) / zz / fbo2[mat] * unit));
                                flist = new List<GH_Number>();
                                if (m2 / 18 == 3)//-X
                                {
                                    for (int i = 0; i < 18; i++) { flist.Add(new GH_Number(-sec_f[e][18 + i].Value)); }
                                }
                                else
                                {
                                    for (int i = 0; i < 18; i++) { flist.Add(new GH_Number(sec_f[e][18 * 3 + i].Value)); }
                                }
                                kentei.AppendRange(k3list, new GH_Path(new int[] { 3, ind })); sec_f_new.AppendRange(flist, new GH_Path(new int[] { 3, ind }));

                                if (Ni + NYi2 < 0) { k4list.Add(new GH_Number(Math.Abs(Ni + NYi2) / a / f_k2[ind] * unit)); }
                                else { k4list.Add(new GH_Number(Math.Abs(Ni + NYi2) / a / ft2[mat] * unit)); }
                                k4list.Add(new GH_Number((Qyi + QYyi2) / a / fso2[mat] * unit)); k4list.Add(new GH_Number((Qzi + QYzi2) / a / fs2[mat] * unit));
                                k4list.Add(new GH_Number((Myi + MYyi2) / zy / fb2[mat] * unit)); k4list.Add(new GH_Number((Mzi + MYzi2) / zz / fbo2[mat] * unit));
                                if (Nj + NYj2 < 0) { k4list.Add(new GH_Number(Math.Abs(Nj + NYj2) / a / f_k2[ind] * unit)); }
                                else { k4list.Add(new GH_Number(Math.Abs(Nj + NYj2) / a / ft2[mat] * unit)); }
                                k4list.Add(new GH_Number((Qyj + QYyj2) / a / fso2[mat] * unit)); k4list.Add(new GH_Number((Qzj + QYzj2) / a / fs2[mat] * unit));
                                k4list.Add(new GH_Number((Myj + MYyj2) / zy / fb2[mat] * unit)); k4list.Add(new GH_Number((Mzj + MYzj2) / zz / fbo2[mat] * unit));
                                if (Nc + NYc2 < 0) { k4list.Add(new GH_Number(Math.Abs(Nc + NYc) / a / f_k2[ind] * unit)); }
                                else { k4list.Add(new GH_Number(Math.Abs(Nc + NYc2) / a / ft2[mat] * unit)); }
                                k4list.Add(new GH_Number((Qyc + QYyc2) / a / fso2[mat] * unit)); k4list.Add(new GH_Number((Qzc + QYzc2) / a / fs2[mat] * unit));
                                k4list.Add(new GH_Number((Myc + MYyc2) / zy / fb2[mat] * unit)); k4list.Add(new GH_Number((Mzc + MYzc2) / zz / fbo2[mat] * unit));
                                flist = new List<GH_Number>();
                                if (m2 / 18 == 3)//-Y
                                {
                                    for (int i = 0; i < 18; i++) { flist.Add(new GH_Number(-sec_f[e][18 * 2 + i].Value)); }
                                }
                                else
                                {
                                    for (int i = 0; i < 18; i++) { flist.Add(new GH_Number(sec_f[e][18 * 4 + i].Value)); }
                                }
                                kentei.AppendRange(k4list, new GH_Path(new int[] { 4, ind })); sec_f_new.AppendRange(flist, new GH_Path(new int[] { 4, ind }));

                                if (on_off_12 == 1)
                                {
                                    var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                                    var r1 = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value); var r2 = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value);
                                    var rc = (r1 + r2) / 2.0; var ri = (r1 + rc) / 2.0; var rj = (r2 + rc) / 2.0;
                                    if (on_off_21 == 1)
                                    {
                                        var k1 = Math.Max(k1list[3].Value, k1list[4].Value) + k1list[0].Value;//i端の検定比
                                        var k2 = Math.Max(k2list[3].Value, k2list[4].Value) + k2list[0].Value;
                                        var k3 = Math.Max(k3list[3].Value, k3list[4].Value) + k3list[0].Value;
                                        var k4 = Math.Max(k4list[3].Value, k4list[4].Value) + k4list[0].Value;
                                        var k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, digit));
                                        _p.Add(ri);
                                        var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = Math.Max(k1list[8].Value, k1list[9].Value) + k1list[5].Value;//j端の検定比
                                        k2 = Math.Max(k2list[8].Value, k2list[9].Value) + k2list[5].Value;
                                        k3 = Math.Max(k3list[8].Value, k3list[9].Value) + k3list[5].Value;
                                        k4 = Math.Max(k4list[8].Value, k4list[9].Value) + k4list[5].Value;
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, digit));
                                        _p.Add(rj);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = Math.Max(k1list[13].Value, k1list[14].Value) + k1list[10].Value;//中央の検定比
                                        k2 = Math.Max(k2list[13].Value, k2list[14].Value) + k2list[10].Value;
                                        k3 = Math.Max(k3list[13].Value, k3list[14].Value) + k3list[10].Value;
                                        k4 = Math.Max(k4list[13].Value, k4list[14].Value) + k4list[10].Value;
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, digit));
                                        _p.Add(rc);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                    }
                                    else if (on_off_22 == 1)
                                    {
                                        var k1 = Math.Max(k1list[1].Value, k1list[2].Value);
                                        var k2 = Math.Max(k2list[1].Value, k2list[2].Value);
                                        var k3 = Math.Max(k3list[1].Value, k3list[2].Value);
                                        var k4 = Math.Max(k4list[1].Value, k4list[2].Value);
                                        var k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, digit));
                                        _p.Add(ri);
                                        var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = Math.Max(k1list[6].Value, k1list[7].Value);
                                        k2 = Math.Max(k2list[6].Value, k2list[7].Value);
                                        k3 = Math.Max(k3list[6].Value, k3list[7].Value);
                                        k4 = Math.Max(k4list[6].Value, k4list[7].Value);
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, digit));
                                        _p.Add(rj);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = Math.Max(k1list[11].Value, k1list[12].Value);
                                        k2 = Math.Max(k2list[11].Value, k2list[12].Value);
                                        k3 = Math.Max(k3list[11].Value, k3list[12].Value);
                                        k4 = Math.Max(k4list[11].Value, k4list[12].Value);
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, digit));
                                        _p.Add(rc);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                    }
                                    else if (on_off_23 == 1)
                                    {
                                        var k1 = Math.Max(Math.Max(k1list[3].Value, k1list[4].Value) + k1list[0].Value, Math.Max(Math.Max(k1list[1].Value, k1list[2].Value), k1list[0].Value));
                                        var k2 = Math.Max(Math.Max(k2list[3].Value, k2list[4].Value) + k2list[0].Value, Math.Max(Math.Max(k2list[1].Value, k2list[2].Value), k2list[0].Value));
                                        var k3 = Math.Max(Math.Max(k3list[3].Value, k3list[4].Value) + k3list[0].Value, Math.Max(Math.Max(k3list[1].Value, k3list[2].Value), k3list[0].Value));
                                        var k4 = Math.Max(Math.Max(k4list[3].Value, k4list[4].Value) + k4list[0].Value, Math.Max(Math.Max(k4list[1].Value, k4list[2].Value), k4list[0].Value));
                                        var k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, digit));
                                        _p.Add(ri);
                                        var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = Math.Max(Math.Max(k1list[8].Value, k1list[9].Value) + k1list[5].Value, Math.Max(Math.Max(k1list[6].Value, k1list[7].Value), k1list[5].Value));
                                        k2 = Math.Max(Math.Max(k2list[8].Value, k2list[9].Value) + k2list[5].Value, Math.Max(Math.Max(k2list[6].Value, k2list[7].Value), k2list[5].Value));
                                        k3 = Math.Max(Math.Max(k3list[8].Value, k3list[9].Value) + k3list[5].Value, Math.Max(Math.Max(k3list[6].Value, k3list[7].Value), k3list[5].Value));
                                        k4 = Math.Max(Math.Max(k4list[8].Value, k4list[9].Value) + k4list[5].Value, Math.Max(Math.Max(k4list[6].Value, k4list[7].Value), k4list[5].Value));
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, digit));
                                        _p.Add(rj);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                        k1 = Math.Max(Math.Max(k1list[13].Value, k1list[14].Value) + k1list[10].Value, Math.Max(Math.Max(k1list[11].Value, k1list[12].Value), k1list[10].Value));
                                        k2 = Math.Max(Math.Max(k2list[13].Value, k2list[14].Value) + k2list[10].Value, Math.Max(Math.Max(k2list[11].Value, k2list[12].Value), k2list[10].Value));
                                        k3 = Math.Max(Math.Max(k3list[13].Value, k3list[14].Value) + k3list[10].Value, Math.Max(Math.Max(k3list[11].Value, k3list[12].Value), k3list[10].Value));
                                        k4 = Math.Max(Math.Max(k4list[13].Value, k4list[14].Value) + k4list[10].Value, Math.Max(Math.Max(k4list[11].Value, k4list[12].Value), k4list[10].Value));
                                        k = Math.Max(Math.Max(k1, k2), Math.Max(k3, k4));
                                        _text.Add(k.ToString("F").Substring(0, digit));
                                        _p.Add(rc);
                                        color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        _c.Add(color);
                                    }
                                    else if (on_off_24 == 1)
                                    {
                                        var k = lam;
                                        _text.Add(k.ToString("F").Substring(0, digit));
                                        _p.Add(rc);
                                        var color = Color.Crimson;
                                        _c.Add(color);
                                    }
                                }
                            }
                        }
                        var f_ktree = new GH_Structure<GH_Number>(); var f_ttree = new GH_Structure<GH_Number>(); var f_btree = new GH_Structure<GH_Number>(); var f_stree = new GH_Structure<GH_Number>(); var f_botree = new GH_Structure<GH_Number>(); var f_sotree = new GH_Structure<GH_Number>();
                        var f_klist = new List<GH_Number>(); var f_tlist = new List<GH_Number>(); var f_blist = new List<GH_Number>(); var f_slist = new List<GH_Number>(); var f_bolist = new List<GH_Number>(); var f_solist = new List<GH_Number>();
                        var f_k2list = new List<GH_Number>(); var f_t2list = new List<GH_Number>(); var f_b2list = new List<GH_Number>(); var f_s2list = new List<GH_Number>(); var f_bo2list = new List<GH_Number>(); var f_so2list = new List<GH_Number>();
                        for (int i = 0; i < f_k.Count; i++)
                        {
                            f_klist.Add(new GH_Number(f_k[i])); f_tlist.Add(new GH_Number(f_t[i])); f_blist.Add(new GH_Number(f_b[i])); f_slist.Add(new GH_Number(f_s[i])); f_bolist.Add(new GH_Number(f_bo[i])); f_solist.Add(new GH_Number(f_so[i]));
                            f_k2list.Add(new GH_Number(f_k2[i])); f_t2list.Add(new GH_Number(f_t2[i])); f_b2list.Add(new GH_Number(f_b2[i])); f_s2list.Add(new GH_Number(f_s2[i])); f_bo2list.Add(new GH_Number(f_bo2[i])); f_so2list.Add(new GH_Number(f_so2[i]));
                        }
                        f_ktree.AppendRange(f_klist, new GH_Path(0)); f_ktree.AppendRange(f_k2list, new GH_Path(1));
                        f_ttree.AppendRange(f_tlist, new GH_Path(0)); f_ttree.AppendRange(f_t2list, new GH_Path(1));
                        f_btree.AppendRange(f_blist, new GH_Path(0)); f_btree.AppendRange(f_b2list, new GH_Path(1));
                        f_stree.AppendRange(f_slist, new GH_Path(0)); f_stree.AppendRange(f_s2list, new GH_Path(1));
                        f_botree.AppendRange(f_bolist, new GH_Path(0)); f_botree.AppendRange(f_bo2list, new GH_Path(1));
                        f_sotree.AppendRange(f_solist, new GH_Path(0)); f_sotree.AppendRange(f_so2list, new GH_Path(1));
                        DA.SetDataTree(1, ij_new); DA.SetDataTree(13, sec_f_new); DA.SetDataTree(14, kentei); DA.SetDataList("lambda", Lambda);
                        DA.SetDataTree(6, f_ktree); DA.SetDataTree(7, f_ttree); DA.SetDataTree(8, f_btree); DA.SetDataTree(9, f_stree); DA.SetDataTree(10, f_botree); DA.SetDataTree(11, f_sotree); DA.SetDataList("index", index);
                        var kk = kentei.Branches;
                        var k1max = 0.0; var k2max = 0.0; var ij1 = -1; var ij2 = -1;
                        for (int i = 0; i < kk.Count / index.Count; i++)
                        {
                            for (int j = 0; j < index.Count; j++)
                            {
                                int ii = index.Count * i + j;
                                var k1 = Math.Max(Math.Max(kk[ii][3].Value, kk[ii][4].Value) + kk[ii][0].Value, Math.Max(Math.Max(kk[ii][1].Value, kk[ii][2].Value), kk[ii][0].Value));
                                var k2 = Math.Max(Math.Max(kk[ii][5 + 3].Value, kk[ii][5 + 4].Value) + kk[ii][5 + 0].Value, Math.Max(Math.Max(kk[ii][5 + 1].Value, kk[ii][5 + 2].Value), kk[ii][5 + 0].Value));
                                var k3 = Math.Max(Math.Max(kk[ii][10 + 3].Value, kk[ii][10 + 4].Value) + kk[ii][10 + 0].Value, Math.Max(Math.Max(kk[ii][10 + 1].Value, kk[ii][10 + 2].Value), kk[ii][10 + 0].Value));
                                var k = Math.Max(Math.Max(k1, k2), k3);
                                if (i == 0)
                                {
                                    k1max = Math.Max(k1max, k);
                                    if (k1max == k) { ij1 = j; }
                                }
                                else
                                {
                                    k2max = Math.Max(k2max, k);
                                    if (k2max == k) { ij2 = j; }
                                }
                            }
                        }
                        var llist = new List<GH_Number>(); var slist = new List<GH_Number>();
                        llist.Add(new GH_Number(ij1)); llist.Add(new GH_Number(k1max));
                        slist.Add(new GH_Number(ij2)); slist.Add(new GH_Number(k2max));
                        var kmax = new GH_Structure<GH_Number>();
                        kmax.AppendRange(llist, new GH_Path(0));
                        if (ij2 != -1) { kmax.AppendRange(slist, new GH_Path(1)); }
                        DA.SetDataTree(17, kmax);
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
                return OpenSeesUtility.Properties.Resources.cltcheck;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b435f02e-c278-4d81-a076-7bd442c5cc14"); }
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

                global_rec.Height = radio_rec2_4.Bottom - global_rec.Y;

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