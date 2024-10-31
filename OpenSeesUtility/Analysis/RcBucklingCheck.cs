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
///****************************************
using System.Diagnostics;

using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp;

namespace OpenSeesUtility
{
    public class RcBucklingCheck : GH_Component
    {
        public static int on_off_11 = 1; public static int on_off_12 = 0;
        public static int on_off_21 = 0; public static int on_off_22 = 0; public static int on_off_23 = 0;
        public string unit_of_length = "m"; public string unit_of_force = "kN"; public double fontsize = 20; static int on_off = 0;
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
            else if (s == "1")
            {
                on_off = i;
            }
        }
        public RcBucklingCheck()
          : base("Allowable stress design for RC buickling loads", "RCBucklingCheck",
              "Allowable stress design for RC buickling loads based on Navier equation and AIJ RC design code 2010 P.145",
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
            pManager.AddNumberParameter("Standard allowable stress (compression)[N/mm2]", "Fc", "[...](DataList)[N/mm2]", GH_ParamAccess.list, new List<double> { 24.0 });///
            pManager.AddTextParameter("name", "name", "name of sections", GH_ParamAccess.list, new List<string> { "" });
            pManager.AddNumberParameter("sectional_force", "sec_f", "[[Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("index", "index", "[...](element No. List to show)", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddNumberParameter("fontsize", "FS", "font size for display texts", GH_ParamAccess.item, 12.0);///
            pManager.AddTextParameter("outputname", "outputname", "output file name", GH_ParamAccess.item, "RcBucklingCheck");///
            pManager.AddNumberParameter("P1", "P1", "[■□HL[:B,〇●:R](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///1
            pManager.AddNumberParameter("P2", "P2", "[■□HL[:D,〇:t,●:0](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///2
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("kentei(max)", "kentei(max)", "[[element.No, long-term, short-term],...]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("kmax", "kmax", "[[ele. No.,Long-term max],[ele. No.,Short-term max]](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r); var r = _r.Branches;
            DA.GetDataTree("element_node_relationship", out GH_Structure<GH_Number> _ij); var ij = _ij.Branches;
            DA.GetDataTree("sectional_force", out GH_Structure<GH_Number> _sec_f); var sec_f = _sec_f.Branches;
            var Fc = new List<double>(); DA.GetDataList("Standard allowable stress (compression)[N/mm2]", Fc);
            DA.GetData("fontsize", ref fontsize);
            var P1 = new List<double>(); DA.GetDataList("P1", P1); var P2 = new List<double>(); DA.GetDataList("P2", P2);
            var kentei = new GH_Structure<GH_Number>(); int digit = 4;
            var unitl = 1.0 / 1000.0; var unitf = 1.0 / 1000.0;///単位合わせのための係数
            List<double> index = new List<double>(); DA.GetDataList("index", index);
            var Nod = new List<List<string>>(); var Ele = new List<List<string>>();
            var FC = new List<List<string>>(); var Size = new List<List<string>>();
            var N = new List<List<double>>(); var Nx = new List<List<double>>(); var Ny = new List<List<double>>(); var Nx2 = new List<List<double>>(); var Ny2 = new List<List<double>>();
            var NaL = new List<double>(); var NaS = new List<double>(); var omega = new List<double>(); var H = new List<double>(); var H_per_D = new List<double>(); var kmax1 = new List<double>(); var kmax2 = new List<double>();
            double Omega(double h_bar_D)
            {
                var _omega = 1.0;
                if (15.0 <= h_bar_D && h_bar_D < 20.0)
                {
                    _omega = (1.08-1.00) / 5.0 * (h_bar_D - 15.0) + 1.0;
                }
                else if (20.0 <= h_bar_D && h_bar_D < 25.0)
                {
                    _omega = (1.32-1.08) / 5.0 * (h_bar_D - 20.0) + 1.08;
                }
                else if (25.0 <= h_bar_D && h_bar_D < 30.0)
                {
                    _omega = (1.72 - 1.32) / 5.0 * (h_bar_D - 25.0) + 1.32;
                }
                else if (30.0 <= h_bar_D && h_bar_D < 35.0)
                {
                    _omega = (2.28 - 1.72) / 5.0 * (h_bar_D - 30.0) + 1.72;
                }
                else if (35.0 <= h_bar_D)
                {
                    _omega = (3.00-2.28) / 5.0 * (h_bar_D - 35.0) + 2.28;
                }
                return _omega;
            }
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
            if (r[0][0].Value != -9999 && ij[0][0].Value != -9999 && sec_f[0][0].Value != -9999 && P1[0] != -9999 && P2[0] != -9999)
            {
                for (int ind = 0; ind < index.Count; ind++)
                {
                    int e = (int)index[ind]; int ni = (int)ij[e][0].Value; int nj = (int)ij[e][1].Value; int mat = (int)ij[e][2].Value; Nod.Add(new List<string> { ni.ToString() + "(i端)", "中央", nj.ToString() + "(j端)" }); Ele.Add(new List<string> { e.ToString() });
                    var f = sec_f[e]; var Ni = f[0].Value; var Nj = -f[6].Value; var Nc = f[12].Value;
                    var Ni_x = 0.0; var Nj_x = 0.0; var Nc_x = 0.0; var Ni_y = 0.0; var Nj_y = 0.0; var Nc_y = 0.0;
                    var Ni_x2 = 0.0; var Nj_x2 = 0.0; var Nc_x2 = 0.0; var Ni_y2 = 0.0; var Nj_y2 = 0.0; var Nc_y2 = 0.0;
                    if (sec_f[0].Count / 18 == 3)
                    {
                        Ni_x = f[18 + 0].Value; Nj_x = -f[18 + 6].Value; Nc_x = f[18 + 12].Value; Ni_y = f[18 * 2 + 0].Value; Nj_y = -f[18 * 2 + 6].Value; Nc_y = f[18 * 2 + 12].Value;
                        Ni_x2 = -Ni_x; Nj_x2 = -Nj_x; Nc_x2 = -Nc_x; Ni_y2 = -Ni_y; Nj_y2 = -Nj_y; Nc_y2 = -Nc_y;
                    }
                    else if (sec_f[0].Count / 18 == 5)
                    {
                        Ni_x = f[18 + 0].Value; Nj_x = -f[18 + 6].Value; Nc_x = f[18 + 12].Value; Ni_y = f[18 * 2 + 0].Value; Nj_y = -f[18 * 2 + 6].Value; Nc_y = f[18 * 2 + 12].Value;
                        Ni_x2 = f[18 * 3 + 0].Value; Nj_x2 = -f[18 * 3 + 6].Value; Nc_x2 = f[18 * 3 + 12].Value; Ni_y2 = f[18 * 4 + 0].Value; Nj_y2 = -f[18 * 4 + 6].Value; Nc_y2 = f[18 * 4 + 12].Value;
                    }
                    N.Add(new List<double> { Ni, Nc, Nj }); Nx.Add(new List<double> { Ni_x, Nc_x, Nj_x }); Ny.Add(new List<double> { Ni_y, Nc_y, Nj_y }); Nx2.Add(new List<double> { Ni_x2, Nc_x2, Nj_x2 }); Ny2.Add(new List<double> { Ni_y2, Nc_y2, Nj_y2 });
                    var fcL = Fc[mat] / 3.0; var fcS = fcL * 2.0;
                    FC.Add(new List<string> { fcL.ToString("F10").Substring(0, Math.Max(4, Digit((int)fcL))), "", (fcS).ToString("F10").Substring(0, Math.Max(4, Digit((int)(fcS)))) });
                    var D = P1[(int)ij[e][3].Value] * 1000; var b = P2[(int)ij[e][3].Value] * 1000;
                    Size.Add(new List<string> { ((b).ToString()).Substring(0, Digit((int)b)) + "x" + ((D).ToString()).Substring(0, Digit((int)D)) });
                    var A = D * b; NaL.Add(fcL * A / 1000.0); NaS.Add(fcS * A / 1000.0);
                    var r1 = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value); var r2 = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value);
                    var rc = (r1 + r2) / 2.0; var ri = (r1 + rc) / 2.0; var rj = (r2 + rc) / 2.0;
                    var length = (r2 - r1).Length; H.Add(length);
                    var h_bar_D = length * 1000 / Math.Min(b, D); H_per_D.Add(h_bar_D);
                    omega.Add(Omega(h_bar_D));
                    kmax1.Add(omega[ind] * Nc / NaL[ind]);
                    var ki = omega[ind] * Math.Max(Math.Max(Ni + Ni_x, Ni + Ni_y), Math.Max(Ni + Ni_x2, Ni + Ni_y2)) / NaS[ind];
                    var kj = omega[ind] * Math.Max(Math.Max(Nj + Nj_x, Nj + Nj_y), Math.Max(Nj + Nj_x2, Nj + Nj_y2)) / NaS[ind];
                    var kc = omega[ind] * Math.Max(Math.Max(Nc + Nc_x, Nc + Nc_y), Math.Max(Nc + Nc_x2, Nc + Nc_y2)) / NaS[ind];
                    kmax2.Add(Math.Max(kc, Math.Max(ki, kj)));
                    if (on_off_11 == 1)
                    {
                        if (on_off_21 == 1)
                        {
                            var k = omega[ind] * Ni / NaL[ind];
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(ri);
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = omega[ind] * Nj / NaL[ind];
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rj);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = omega[ind] * Nc / NaL[ind];
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rc);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                        }
                        else if (on_off_22 == 1)
                        {
                            var k = h_bar_D;
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rc);
                            var color = new ColorHSL((1 - Math.Min(h_bar_D / 40.0, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                        }
                        else if (on_off_23 == 1)
                        {
                            var k = omega[ind];
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rc);
                            var color = new ColorHSL((1 - Math.Min(k / 3.0, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                        }
                    }
                    else if (on_off_12 == 1)
                    {
                        if (on_off_21 == 1)
                        {
                            var k = omega[ind] * Math.Max(Math.Max(Ni + Ni_x, Ni + Ni_y), Math.Max(Ni + Ni_x2, Ni + Ni_y2)) / NaS[ind];
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(ri);
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = omega[ind] * Math.Max(Math.Max(Nj + Nj_x, Nj + Nj_y), Math.Max(Nj + Nj_x2, Nj + Nj_y2)) / NaS[ind];
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rj);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = omega[ind] * Math.Max(Math.Max(Nc + Nc_x, Nc + Nc_y), Math.Max(Nc + Nc_x2, Nc + Nc_y2)) / NaS[ind];
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rc);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                        }
                        else if (on_off_22 == 1)
                        {
                            var k = h_bar_D;
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rc);
                            var color = new ColorHSL((1 - Math.Min(h_bar_D / 40.0, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                        }
                        else if (on_off_23 == 1)
                        {
                            var k = omega[ind];
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rc);
                            var color = new ColorHSL((1 - Math.Min(k / 3.0, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                        }
                    }
                }
                for (int ind = 0; ind < index.Count; ind++)
                {
                    int e = (int)index[ind];
                    if (sec_f[0].Count / 18 >= 3)
                    {
                        kentei.AppendRange(new List<GH_Number> { new GH_Number(e), new GH_Number(kmax1[ind]), new GH_Number(kmax2[ind]) }, new GH_Path(ind));
                    }
                    else { kentei.AppendRange(new List<GH_Number> { new GH_Number(e), new GH_Number(kmax1[ind]), new GH_Number(0.0) }, new GH_Path(ind)); }
                }
                DA.SetDataTree(0, kentei);
                var _kentei = kentei.Branches; var kmax = new GH_Structure<GH_Number>(); var Lmax = 0.0; int L = 0; var Smax = 0.0; int S = 0;
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
                    var pdfname = "RcBeamCheck"; DA.GetData("outputname", ref pdfname);
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
                    if (sec_f[0].Count == 18)
                    {
                        var labels = new List<string>
                        {
                            "部材番号","b x D[mm]"," ","コンクリートfc[N/mm2]","h[mm]","h/D","ω","節点番号","","N[kN]","Na[kN]","座屈検定比ωN/Na","判定"
                        };
                        var label_width = 100; var offset_x = 25; var offset_y = 25; var pitchy = 13; var text_width = 45; PdfPage page = new PdfPage(); page.Size = PageSize.A4;
                        for (int ind = 0; ind < index.Count; ind++)//
                        {
                            int e = (int)index[ind];
                            var values = new List<List<string>>();
                            values.Add(Ele[ind]); values.Add(Size[ind]); values.Add(new List<string> { "長期", "", "短期" });
                            values.Add(FC[ind]);
                            values.Add(new List<string> { H[ind].ToString("F10").Substring(0, Math.Max(4, Digit((int)H[ind]))) });
                            values.Add(new List<string> { H_per_D[ind].ToString("F10").Substring(0, Math.Max(4, Digit((int)H_per_D[ind]))) });
                            values.Add(new List<string> { omega[ind].ToString("F10").Substring(0, Math.Max(4, Digit((int)omega[ind]))) }); 
                            values.Add(Nod[ind]);
                            values.Add(new List<string> { "長期検討" });
                            values.Add(new List<string> { N[ind][0].ToString("F10").Substring(0, Math.Max(4, Digit((int)N[ind][0]))), N[ind][1].ToString("F10").Substring(0, Math.Max(4, Digit((int)N[ind][1]))), N[ind][2].ToString("F10").Substring(0, Math.Max(4, Digit((int)N[ind][2]))) });
                            values.Add(new List<string> { NaL[ind].ToString("F10").Substring(0, Math.Max(4, Digit((int)NaL[ind]))) });
                            var ki = omega[ind] * N[ind][0] / NaL[ind]; var kc = omega[ind] * N[ind][1] / NaL[ind]; var kj = omega[ind] * N[ind][2] / NaL[ind];
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            var k_color = new List<XSolidBrush>();
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            if (ki < 1 && kc < 1 && kj < 1)
                            {
                                values.Add(new List<string> { "O.K.", "O.K.", "O.K." });
                            }
                            else { values.Add(new List<string> { "N.G.", "N.G.", "N.G." }); }
                            if (ind % 3 == 0)
                            {
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
                            for (int i = 0; i < values.Count; i++)
                            {
                                var j = ind % 3;
                                gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, offset_x + label_width + text_width * 3 * (j + 1), offset_y + pitchy * i);//横線
                                gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * (i + 1));//縦線
                                if (values[i].Count == 1)
                                {
                                    gfx.DrawString(values[i][0], font, XBrushes.Black, new XRect(offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, text_width * 3, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                                }
                                else
                                {
                                    var color1 = XBrushes.Black; var color2 = XBrushes.Black; var color3 = XBrushes.Black; var f = font;
                                    if (i == 11) { color1 = k_color[0]; color2 = k_color[1]; color3 = k_color[2]; }
                                    gfx.DrawString(values[i][0], f, color1, new XRect(offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, text_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                                    gfx.DrawString(values[i][1], f, color2, new XRect(offset_x + label_width + text_width * 3 * j + text_width, offset_y + pitchy * i, text_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                                    gfx.DrawString(values[i][2], f, color3, new XRect(offset_x + label_width + text_width * 3 * j + text_width * 2, offset_y + pitchy * i, text_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                                }
                                if (i == values.Count - 1)
                                {
                                    i += 1;
                                    gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, offset_x + label_width + text_width * 3 * (j + 1), offset_y + pitchy * i);//横線
                                }
                            }
                        }
                    }
                    else if (sec_f[0].Count == 18 * 3 || sec_f[0].Count == 18 * 5)
                    {
                        var labels = new List<string>
                        {
                            "部材番号","b x D[mm]"," ","コンクリートfc[N/mm2]","h[mm]","h/D","ω","節点番号","","N[kN]","Na[kN]","座屈検定比ωN/Na","判定","","N[kN]","Na[kN]","座屈検定比ωN/Na","判定","","N[kN]","Na[kN]","座屈検定比ωN/Na","判定","","N[kN]","Na[kN]","座屈検定比ωN/Na","判定","","N[kN]","Na[kN]","座屈検定比ωN/Na","判定"
                        };
                        var label_width = 100; var offset_x = 25; var offset_y = 25; var pitchy = 13; var text_width = 45; PdfPage page = new PdfPage(); page.Size = PageSize.A4;
                        for (int ind = 0; ind < index.Count; ind++)//
                        {
                            int e = (int)index[ind];
                            var values = new List<List<string>>();
                            values.Add(Ele[ind]); values.Add(Size[ind]); values.Add(new List<string> { "長期", "", "短期" });
                            values.Add(FC[ind]);
                            values.Add(new List<string> { H[ind].ToString("F10").Substring(0, Math.Max(4, Digit((int)H[ind]))) });
                            values.Add(new List<string> { H_per_D[ind].ToString("F10").Substring(0, Math.Max(4, Digit((int)H_per_D[ind]))) });
                            values.Add(new List<string> { omega[ind].ToString("F10").Substring(0, Math.Max(4, Digit((int)omega[ind]))) });
                            values.Add(Nod[ind]);
                            values.Add(new List<string> { "長期検討" });
                            values.Add(new List<string> { N[ind][0].ToString("F10").Substring(0, Math.Max(4, Digit((int)N[ind][0]))), N[ind][1].ToString("F10").Substring(0, Math.Max(4, Digit((int)N[ind][1]))), N[ind][2].ToString("F10").Substring(0, Math.Max(4, Digit((int)N[ind][2]))) });
                            values.Add(new List<string> { NaL[ind].ToString("F10").Substring(0, Math.Max(4, Digit((int)NaL[ind]))) });
                            var ki = omega[ind] * N[ind][0] / NaL[ind]; var kc = omega[ind] * N[ind][1] / NaL[ind]; var kj = omega[ind] * N[ind][2] / NaL[ind];
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            var k_color = new List<XSolidBrush>();
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            if (ki < 1 && kc < 1 && kj < 1)
                            {
                                values.Add(new List<string> { "O.K.", "O.K.", "O.K." });
                            }
                            else { values.Add(new List<string> { "N.G.", "N.G.", "N.G." }); }
                            values.Add(new List<string> { "短期(L+X)検討" });
                            values.Add(new List<string> { (N[ind][0] + Nx[ind][0]).ToString("F10").Substring(0, Math.Max(4, Digit((int)(N[ind][0] + Nx[ind][0])))), (N[ind][1] + Nx[ind][1]).ToString("F10").Substring(0, Math.Max(4, Digit((int)(N[ind][1] + Nx[ind][1])))), (N[ind][2] + Nx[ind][2]).ToString("F10").Substring(0, Math.Max(4, Digit((int)(N[ind][2] + Nx[ind][2])))) });
                            values.Add(new List<string> { NaS[ind].ToString("F10").Substring(0, Math.Max(4, Digit((int)NaS[ind]))) });
                            ki = omega[ind] * (N[ind][0] + Nx[ind][0]) / NaS[ind]; kc = omega[ind] * (N[ind][1] + Nx[ind][1]) / NaS[ind]; kj = omega[ind] * (N[ind][2] + Nx[ind][2]) / NaS[ind];
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            var k2_color = new List<XSolidBrush>();
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            if (ki < 1 && kc < 1 && kj < 1)
                            {
                                values.Add(new List<string> { "O.K.", "O.K.", "O.K." });
                            }
                            else { values.Add(new List<string> { "N.G.", "N.G.", "N.G." }); }
                            values.Add(new List<string> { "短期(L+Y)検討" });
                            values.Add(new List<string> { (N[ind][0] + Ny[ind][0]).ToString("F10").Substring(0, Math.Max(4, Digit((int)(N[ind][0] + Ny[ind][0])))), (N[ind][1] + Ny[ind][1]).ToString("F10").Substring(0, Math.Max(4, Digit((int)(N[ind][1] + Ny[ind][1])))), (N[ind][2] + Ny[ind][2]).ToString("F10").Substring(0, Math.Max(4, Digit((int)(N[ind][2] + Ny[ind][2])))) });
                            values.Add(new List<string> { NaS[ind].ToString("F10").Substring(0, Math.Max(4, Digit((int)NaS[ind]))) });
                            ki = omega[ind] * (N[ind][0] + Ny[ind][0]) / NaS[ind]; kc = omega[ind] * (N[ind][1] + Ny[ind][1]) / NaS[ind]; kj = omega[ind] * (N[ind][2] + Ny[ind][2]) / NaS[ind];
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            var k3_color = new List<XSolidBrush>();
                            k3_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k3_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k3_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            if (ki < 1 && kc < 1 && kj < 1)
                            {
                                values.Add(new List<string> { "O.K.", "O.K.", "O.K." });
                            }
                            else { values.Add(new List<string> { "N.G.", "N.G.", "N.G." }); }
                            values.Add(new List<string> { "短期(L-X)検討" });
                            values.Add(new List<string> { (N[ind][0] + Nx2[ind][0]).ToString("F10").Substring(0, Math.Max(4, Digit((int)(N[ind][0] + Nx2[ind][0])))), (N[ind][1] + Nx2[ind][1]).ToString("F10").Substring(0, Math.Max(4, Digit((int)(N[ind][1] + Nx2[ind][1])))), (N[ind][2] + Nx2[ind][2]).ToString("F10").Substring(0, Math.Max(4, Digit((int)(N[ind][2] + Nx2[ind][2])))) });
                            values.Add(new List<string> { NaS[ind].ToString("F10").Substring(0, Math.Max(4, Digit((int)NaS[ind]))) });
                            ki = omega[ind] * (N[ind][0] + Nx2[ind][0]) / NaS[ind]; kc = omega[ind] * (N[ind][1] + Nx2[ind][1]) / NaS[ind]; kj = omega[ind] * (N[ind][2] + Nx2[ind][2]) / NaS[ind];
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            var k4_color = new List<XSolidBrush>();
                            k4_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k4_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k4_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            if (ki < 1 && kc < 1 && kj < 1)
                            {
                                values.Add(new List<string> { "O.K.", "O.K.", "O.K." });
                            }
                            else { values.Add(new List<string> { "N.G.", "N.G.", "N.G." }); }
                            values.Add(new List<string> { "短期(L-Y)検討" });
                            values.Add(new List<string> { (N[ind][0] + Ny2[ind][0]).ToString("F10").Substring(0, Math.Max(4, Digit((int)(N[ind][0] + Ny2[ind][0])))), (N[ind][1] + Ny2[ind][1]).ToString("F10").Substring(0, Math.Max(4, Digit((int)(N[ind][1] + Ny2[ind][1])))), (N[ind][2] + Ny2[ind][2]).ToString("F10").Substring(0, Math.Max(4, Digit((int)(N[ind][2] + Ny2[ind][2])))) });
                            values.Add(new List<string> { NaS[ind].ToString("F10").Substring(0, Math.Max(4, Digit((int)NaS[ind]))) });
                            ki = omega[ind] * (N[ind][0] + Ny2[ind][0]) / NaS[ind]; kc = omega[ind] * (N[ind][1] + Ny2[ind][1]) / NaS[ind]; kj = omega[ind] * (N[ind][2] + Ny2[ind][2]) / NaS[ind];
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            var k5_color = new List<XSolidBrush>();
                            k5_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k5_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k5_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            if (ki < 1 && kc < 1 && kj < 1)
                            {
                                values.Add(new List<string> { "O.K.", "O.K.", "O.K." });
                            }
                            else { values.Add(new List<string> { "N.G.", "N.G.", "N.G." }); }
                            if (ind % 3 == 0)
                            {
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
                            for (int i = 0; i < values.Count; i++)
                            {
                                var j = ind % 3;
                                gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, offset_x + label_width + text_width * 3 * (j + 1), offset_y + pitchy * i);//横線
                                gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * (i + 1));//縦線
                                if (values[i].Count == 1)
                                {
                                    gfx.DrawString(values[i][0], font, XBrushes.Black, new XRect(offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, text_width * 3, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                                }
                                else
                                {
                                    var color1 = XBrushes.Black; var color2 = XBrushes.Black; var color3 = XBrushes.Black; var f = font;
                                    if (i == 11) { color1 = k_color[0]; color2 = k_color[1]; color3 = k_color[2]; }
                                    else if (i == 16) { color1 = k2_color[0]; color2 = k2_color[1]; color3 = k2_color[2]; }
                                    else if (i == 21) { color1 = k3_color[0]; color2 = k3_color[1]; color3 = k3_color[2]; }
                                    else if (i == 26) { color1 = k4_color[0]; color2 = k4_color[1]; color3 = k4_color[2]; }
                                    else if (i == 31) { color1 = k5_color[0]; color2 = k5_color[1]; color3 = k5_color[2]; }
                                    gfx.DrawString(values[i][0], f, color1, new XRect(offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, text_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                                    gfx.DrawString(values[i][1], f, color2, new XRect(offset_x + label_width + text_width * 3 * j + text_width, offset_y + pitchy * i, text_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                                    gfx.DrawString(values[i][2], f, color3, new XRect(offset_x + label_width + text_width * 3 * j + text_width * 2, offset_y + pitchy * i, text_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                                }
                                if (i == values.Count - 1)
                                {
                                    i += 1;
                                    gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, offset_x + label_width + text_width * 3 * (j + 1), offset_y + pitchy * i);//横線
                                }
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
                return OpenSeesUtility.Properties.Resources.rcbucklingcheck;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("207205fa-571b-4648-933b-cd777cfee7b9"); }
        }
        ///ここからカスタム関数群********************************************************************************
        private readonly List<string> _text = new List<string>();
        private readonly List<Point3d> _p = new List<Point3d>();
        private readonly List<Color> _c = new List<Color>();
        private readonly List<double> _size = new List<double>();
        protected override void BeforeSolveInstance()
        {
            _text.Clear();
            _c.Clear();
            _p.Clear();
            _size.Clear();
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            args.Viewport.GetFrustumFarPlane(out Plane plane);
            RhinoViewport viewport = args.Viewport;
            for (int i = 0; i < _text.Count; i++)
            {
                double size = _size[i]; Point3d point = _p[i];
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
            private Rectangle radio_rec3_1; private Rectangle text_rec3_1;
            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int height = 120; int radi1 = 7; int radi2 = 4; int titleheight = 20;
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

                radio_rec3 = radio_rec2;
                radio_rec3.Y = radio_rec2.Y + radio_rec2.Height;
                radio_rec3.Height = textheight;

                radio_rec3_1 = radio_rec3;
                radio_rec3_1.X += 5; radio_rec3_1.Y += 5;
                radio_rec3_1.Height = radi1; radio_rec3_1.Width = radi1;

                text_rec3_1 = radio_rec3_1;
                text_rec3_1.X += pitchx; text_rec3_1.Y -= radi2;
                text_rec3_1.Height = textheight; text_rec3_1.Width = width * 3;

                Bounds = global_rec;
            }
            Brush c1 = Brushes.Black; Brush c2 = Brushes.White; Brush c21 = Brushes.White; Brush c22 = Brushes.White; Brush c23 = Brushes.White; Brush c3 = Brushes.White;
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
                    graphics.DrawString("N kentei", GH_FontServer.Standard, Brushes.Black, text_rec2_1);

                    GH_Capsule radio2_2 = GH_Capsule.CreateCapsule(radio_rec2_2, GH_Palette.Black, 5, 5);
                    radio2_2.Render(graphics, Selected, Owner.Locked, false); radio2_2.Dispose();
                    graphics.FillEllipse(c22, radio_rec2_2);
                    graphics.DrawString("h/D", GH_FontServer.Standard, Brushes.Black, text_rec2_2);

                    GH_Capsule radio2_3 = GH_Capsule.CreateCapsule(radio_rec2_3, GH_Palette.Black, 5, 5);
                    radio2_3.Render(graphics, Selected, Owner.Locked, false); radio2_3.Dispose();
                    graphics.FillEllipse(c23, radio_rec2_3);
                    graphics.DrawString("omega", GH_FontServer.Standard, Brushes.Black, text_rec2_3);

                    GH_Capsule radio3 = GH_Capsule.CreateCapsule(radio_rec3, GH_Palette.White, 2, 0);
                    radio3.Render(graphics, Selected, Owner.Locked, false); radio3.Dispose();

                    GH_Capsule radio3_1 = GH_Capsule.CreateCapsule(radio_rec3_1, GH_Palette.Black, 5, 5);
                    radio3_1.Render(graphics, Selected, Owner.Locked, false); radio3_1.Dispose();
                    graphics.FillEllipse(c3, radio_rec3_1);
                    graphics.DrawString("PDF OUTPUT", GH_FontServer.Standard, Brushes.Black, text_rec3_1);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec1 = radio_rec_1; RectangleF rec2 = radio_rec_2;
                    RectangleF rec21 = radio_rec2_1; RectangleF rec22 = radio_rec2_2; RectangleF rec23 = radio_rec2_3; RectangleF rec3 = radio_rec3_1;
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
                        if (c21 == Brushes.White) { c21 = Brushes.Black; SetButton("21", 1); c22 = Brushes.White; SetButton("22", 0); c23 = Brushes.White; SetButton("23", 0); }
                        else { c21 = Brushes.White; SetButton("21", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec22.Contains(e.CanvasLocation))
                    {
                        if (c22 == Brushes.White) { c22 = Brushes.Black; SetButton("22", 1); c21 = Brushes.White; SetButton("21", 0); c23 = Brushes.White; SetButton("23", 0); }
                        else { c22 = Brushes.White; SetButton("22", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec23.Contains(e.CanvasLocation))
                    {
                        if (c23 == Brushes.White) { c23 = Brushes.Black; SetButton("23", 1); c21 = Brushes.White; SetButton("21", 0); c22 = Brushes.White; SetButton("22", 0); }
                        else { c23 = Brushes.White; SetButton("23", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec3.Contains(e.CanvasLocation))
                    {
                        if (c3 == Brushes.Black) { c3 = Brushes.White; SetButton("1", 0); }
                        else
                        { c3 = Brushes.Black; SetButton("1", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}