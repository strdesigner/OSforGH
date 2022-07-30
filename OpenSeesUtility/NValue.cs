using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.Display;
using Rhino.DocObjects;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
///****************************************
using System.Diagnostics;

using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp;

namespace NValue
{
    public class NValue : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public NValue()
          : base("NValue", "NValue",
              "N Value calculation (based on gray book)",
              "OpenSees", "Analysis")
        {
        }
        public override bool IsPreviewCapable { get { return true; } }
        static List<int> Sou = new List<int> { 0, 0, 0 }; static int N_Value = 0; static int Na_Value = 0; static int N_Name = 0;
        double fontsize = 10.0;
        static int PDF = 0;
        public static PdfCreate.JapaneseFontResolver fontresolver = new PdfCreate.JapaneseFontResolver();
        public static XGraphics gfx;
        public static void SetButton(string s, int i)
        {
            if (s == "c11")
            {
                Sou[0] = i;
            }
            else if (s == "c12")
            {
                Sou[1] = i;
            }
            else if (s == "c13")
            {
                Sou[2] = i;
            }
            else if (s == "c21")
            {
                N_Value = i;
            }
            else if (s == "c22")
            {
                Na_Value = i;
            }
            else if (s == "c23")
            {
                N_Name = i;
            }
            else if (s == "c31")
            {
                PDF = i;
            }
        }
        int Digit(int num)//数字の桁数を求める関数
        {
            // Mathf.Log10(0)はNegativeInfinityを返すため、別途処理する。
            return (num == 0) ? 1 : ((int)Math.Log10(num) + 1);
        }
        XColor RGB(double h, double s, double l)//convert HSL to RGB
        {
            var max = 0.0; var min = 0.0; var R = 0.0; var G = 0.0; var B = 0.0;
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
            var hp = HUE_MAX / 6.0; h *= HUE_MAX; var Q = h / hp;
            if (Q <= 1)
            {
                R = max;
                G = (h / hp) * (max - min) + min;
                B = min;
            }
            else if (Q <= 2)
            {
                R = ((hp * 2 - h) / hp) * (max - min) + min;
                G = max;
                B = min;
            }
            else if (Q <= 3)
            {
                R = min;
                G = max;
                B = ((h - hp * 2) / hp) * (max - min) + min;
            }
            else if (Q <= 4)
            {
                R = min;
                G = ((hp * 4 - h) / hp) * (max - min) + min;
                B = max;
            }
            else if (Q <= 5)
            {
                R = ((h - hp * 4) / hp) * (max - min) + min;
                G = min;
                B = max;
            }
            else
            {
                R = max;
                G = min;
                B = ((HUE_MAX - h) / hp) * (max - min) + min;
            }
            R *= RGB_MAX; G *= RGB_MAX; B *= RGB_MAX;
            return XColor.FromArgb((int)R, (int)G, (int)B);
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree,-9999);///
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("sec_f(L)", "sec_f(L)", "[[Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("sec_f(X)", "sec_f(X)", "[[Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("sec_f(Y)", "sec_f(Y)", "[[Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddIntegerParameter("index1F", "index1F", "[int,int,...](Datalist) column indexes in 1F", GH_ParamAccess.list);
            pManager.AddIntegerParameter("index2F", "index2F", "[int,int,...](Datalist) column indexes in 2F", GH_ParamAccess.list);
            pManager.AddIntegerParameter("index3F", "index3F", "[int,int,...](Datalist) column indexes in 3F", GH_ParamAccess.list);
            pManager.AddNumberParameter("Q", "Q", "shear force", GH_ParamAccess.list);///
            pManager.AddNumberParameter("Qax", "Qax", "allowable shear force for X direction", GH_ParamAccess.list);///
            pManager.AddNumberParameter("Qay", "Qay", "allowable shear force for Y direction", GH_ParamAccess.list);///
            pManager.AddNumberParameter("Bx", "Bx", "safe factor for X direction", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddNumberParameter("By", "By", "safe factor for Y direction", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddNumberParameter("fontsize", "fontsize", "fontsize", GH_ParamAccess.item, 12.0);
            pManager.AddTextParameter("outputname", "outputname", "output file name", GH_ParamAccess.item, "Nvalue");///
            pManager[6].Optional = true; pManager[7].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("jointname", "jointname", "[[name1F1,name1F2...],[name2F1,name2F2...]...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("Tx", "Tx", "[[1FTx1,1FTx2...],[2FTx1,2FTx2...]...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("Ty", "Ty", "[[1FTy1,1FTy2...],[2FTy1,2FTy2...]...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("Tmax", "Tmax", "[[1FTmax1,1FTmax2...],[2FTmax1,2FTmax2...]...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("Ta", "Ta", "[[1FTa1,1FTa2...],[2FTa1,2FTa2...]...](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetDataTree("sec_f(L)", out GH_Structure<GH_Number> _sec_fl); DA.GetDataTree("sec_f(X)", out GH_Structure<GH_Number> _sec_fx); DA.GetDataTree("sec_f(Y)", out GH_Structure<GH_Number> _sec_fy); DA.GetData("fontsize", ref fontsize); var pdfname = "Nvalue"; DA.GetData("outputname", ref pdfname);
            var sec_fl = _sec_fl.Branches; var sec_fx = _sec_fx.Branches; var sec_fy = _sec_fy.Branches;
            List<int> index1 = new List<int>(); if (!DA.GetDataList("index1F", index1)) { };
            List<int> index2 = new List<int>(); if (!DA.GetDataList("index2F", index2)) { };
            List<int> index3 = new List<int>(); if (!DA.GetDataList("index3F", index3)) { };
            List<double> Q = new List<double>(); if (!DA.GetDataList("Q", Q)) { };
            List<double> Qax = new List<double>(); if (!DA.GetDataList("Qax", Qax)) { };
            List<double> Qay = new List<double>(); if (!DA.GetDataList("Qay", Qay)) { };
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r); var r = _r.Branches;
            DA.GetDataTree("element_node_relationship", out GH_Structure<GH_Number> _ij); var ij = _ij.Branches;
            var BX = new List<double>(); var BY = new List<double>(); DA.GetDataList("Bx", BX); DA.GetDataList("By", BY);
            var alphaX = new List<double>(); var alphaY = new List<double>();
            if (BX[0] == -9999)
            {
                BX = new List<double>();
                for (int e = 0; e < ij.Count; e++) { BX.Add(1.0); }
            }
            if (BY[0] == -9999)
            {
                BY = new List<double>();
                for (int e = 0; e < ij.Count; e++) { BY.Add(1.0); }
            }
            var NL = new List<List<double>>(); var NX = new List<List<double>>(); var NY = new List<List<double>>(); var COLOR = new List<List<ColorHSL>>();
            var TX = new GH_Structure<GH_Number>(); var TY = new GH_Structure<GH_Number>(); var TMAX = new GH_Structure<GH_Number>();
            var NAME = new GH_Structure<GH_String>();
            var TA = new GH_Structure<GH_Number>();
            for (int i = 0; i < Q.Count; i++)
            {
                if (Qax[0] != -9999)
                {
                    alphaX.Add(Qax[i] / Q[i]);
                }
                else { alphaX.Add(1.0); }
                if (Qay[0] != -9999)
                {
                    alphaY.Add(Qay[i] / Q[i]);
                }
                else { alphaY.Add(1.0); }
            }
            if (index1.Count != 0)
            {
                var nl = new List<double>(); var nx = new List<double>(); var ny = new List<double>();
                for (int i = 0; i < sec_fl.Count; i++)
                {
                    if (index1.Contains(i))
                    {
                        nl.Add(Math.Abs(sec_fl[i][0].Value)); nx.Add(Math.Abs(sec_fx[i][0].Value)); ny.Add(Math.Abs(sec_fy[i][0].Value));
                    }
                }
                NL.Add(nl); NX.Add(nx); NY.Add(ny);
            }
            if (index2.Count != 0)
            {
                var nl = new List<double>(); var nx = new List<double>(); var ny = new List<double>();
                for (int i = 0; i < sec_fl.Count; i++)
                {
                    if (index2.Contains(i))
                    {
                        nl.Add(Math.Abs(sec_fl[i][0].Value)); nx.Add(Math.Abs(sec_fx[i][0].Value)); ny.Add(Math.Abs(sec_fy[i][0].Value));
                    }
                }
                NL.Add(nl); NX.Add(nx); NY.Add(ny);
            }
            if (index3.Count != 0)
            {
                var nl = new List<double>(); var nx = new List<double>(); var ny = new List<double>();
                for (int i = 0; i < sec_fl.Count; i++)
                {
                    if (index3.Contains(i))
                    {
                        nl.Add(Math.Abs(sec_fl[i][0].Value)); nx.Add(Math.Abs(sec_fx[i][0].Value)); ny.Add(Math.Abs(sec_fy[i][0].Value));
                    }
                }
                NL.Add(nl); NX.Add(nx); NY.Add(ny);
            }
            Tuple<string, double, ColorHSL> Nvalue(double T)//数字の桁数を求める関数
            {
                var Nname = "は"; var Na = 5.88; var color = new ColorHSL((1 - Math.Min(0, 1.0)) * 1.9 / 3.0, 1, 0.5);
                if (T <= 5.88) { }
                else if (T <= 7.5) { Nname = "に"; Na = 7.5; color = new ColorHSL((1 - Math.Min(0.125, 1.0)) * 1.9 / 3.0, 1, 0.5); }
                else if (T <= 8.5) { Nname = "ほ"; Na = 8.5; color = new ColorHSL((1 - Math.Min(0.25, 1.0)) * 1.9 / 3.0, 1, 0.5); }
                else if (T <= 10.0) { Nname = "へ"; Na = 10.0; color = new ColorHSL((1 - Math.Min(0.375, 1.0)) * 1.9 / 3.0, 1, 0.5); }
                else if (T <= 15.0) { Nname = "と"; Na = 15.0; color = new ColorHSL((1 - Math.Min(0.5, 1.0)) * 1.9 / 3.0, 1, 0.5); }
                else if (T <= 20.0) { Nname = "ち"; Na = 20.0; color = new ColorHSL((1 - Math.Min(0.625, 1.0)) * 1.9 / 3.0, 1, 0.5); }
                else if (T <= 25.0) { Nname = "り"; Na = 25.0; color = new ColorHSL((1 - Math.Min(0.75, 1.0)) * 1.9 / 3.0, 1, 0.5); }
                else if (T <= 30.0) { Nname = "ぬ"; Na = 30.0; color = new ColorHSL((1 - Math.Min(0.825, 1.0)) * 1.9 / 3.0, 1, 0.5); }
                else if (T <= 40.0) { Nname = "る"; Na = 40.0; color = new ColorHSL((1 - Math.Min(1.0, 1.0)) * 1.9 / 3.0, 1, 0.5); }
                else if (T <= 50.0) { Nname = "を"; Na = 50.0; color = new ColorHSL((1 - Math.Min(1.125, 1.0)) * 1.9 / 3.0, 1, 0.5); }
                else { Nname = "×"; Na = 50.0; color = new ColorHSL((1 - Math.Min(1.125, 1.0)) * 1.9 / 3.0, 1, 0.5); }
                return new Tuple<string, double, ColorHSL>(Nname, Na, color);
            }
            for (int i = 0; i < Q.Count; i++)
            {
                var bx = 1.0;var by = 1.0;
                var tx = new List<GH_Number>(); var ty = new List<GH_Number>(); var tmax = new List<GH_Number>(); var name = new List<GH_String>(); var ta = new List<GH_Number>(); var colors = new List<ColorHSL>();
                for (int j = 0; j < NL[i].Count; j++)
                {
                    if (i == 0) { bx = BX[index1[j]]; by = BY[index1[j]]; }
                    if (i == 1) { bx = BX[index2[j]]; by = BY[index2[j]]; }
                    if (i == 2) { bx = BX[index3[j]]; by = BY[index3[j]]; }
                    var txi = NX[i][j] * alphaX[i] * bx - NL[i][j]; var tyi = NY[i][j] * alphaY[i] * by - NL[i][j];
                    tx.Add(new GH_Number(txi)); ty.Add(new GH_Number(tyi)); tmax.Add(new GH_Number(Math.Max(txi, tyi)));
                    var N_value = Nvalue(Math.Max(txi, tyi));
                    var namei = N_value.Item1; var na = N_value.Item2; var c = N_value.Item3; ta.Add(new GH_Number(na));
                    name.Add(new GH_String(namei)); colors.Add(c);
                    if (i == 0 && Sou[0] == 1)
                    {
                        int e = index1[j];
                        int ni = (int)ij[e][0].Value; int nj = (int)ij[e][1].Value;
                        var ri = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value);
                        var rj = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value);
                        _pts.Add((ri + rj) / 2.0); _colors.Add(c); _names.Add(namei);
                        _Ns.Add(Math.Max(Math.Max(txi, tyi), 0).ToString("F6").Substring(0, 4));
                        _Nas.Add(na.ToString("F6").Substring(0, 4));
                    }
                    if (i == 1 && Sou[1] == 1)
                    {
                        int e = index2[j];
                        int ni = (int)ij[e][0].Value; int nj = (int)ij[e][1].Value;
                        var ri = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value);
                        var rj = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value);
                        _pts.Add((ri + rj) / 2.0); _colors.Add(c); _names.Add(namei);
                        _Ns.Add(Math.Max(Math.Max(txi, tyi), 0).ToString("F6").Substring(0, 4));
                        _Nas.Add(na.ToString("F6").Substring(0, 4));
                    }
                    if (i == 2 && Sou[2] == 1)
                    {
                        int e = index3[j];
                        int ni = (int)ij[e][0].Value; int nj = (int)ij[e][1].Value;
                        var ri = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value);
                        var rj = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value);
                        _pts.Add((ri + rj) / 2.0); _colors.Add(c); _names.Add(namei);
                        _Ns.Add(Math.Max(Math.Max(txi, tyi), 0).ToString("F6").Substring(0, 4));
                        _Nas.Add(na.ToString("F6").Substring(0, 4));
                    }
                }
                COLOR.Add(colors);
                TX.AppendRange(tx, new GH_Path(i)); TY.AppendRange(ty, new GH_Path(i)); TMAX.AppendRange(tmax, new GH_Path(i)); NAME.AppendRange(name, new GH_Path(i)); TA.AppendRange(ta, new GH_Path(i));
            }
            DA.SetDataTree(0, NAME); DA.SetDataTree(1, TX); DA.SetDataTree(2, TY); DA.SetDataTree(3, TMAX); DA.SetDataTree(4, TA); DA.SetDataList("pts", _pts);
            if (PDF == 1)
            {
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
                            "部材番号","階","Bx","By","Qi[kN]","Qaxi[kN]","Qayi[kN]","αx","αy","NEx[kN]","NEy[kN]","NHx[kN]","NHy[kN]","Nw[kN]","Tx[kN]","Ty[kN]","T[kN]","必要金物","Ta[kN]","T/Ta"
                        };
                if (Qax[0]==-9999 || Qay[0] == -9999)
                {
                    labels = new List<string>
                        {
                            "部材番号","階","Bx","By","NEx[kN]","NEy[kN]","NHx[kN]","NHy[kN]","Nw[kN]","Tx[kN]","Ty[kN]","T[kN]","必要金物","Ta[kN]","T/Ta"
                        };
                }
                var label_width = 75; var offset_x = 25; var offset_y = 25; var pitchy = 12; var text_width = 45; PdfPage page = new PdfPage(); page.Size = PageSize.A4;
                var k = 0;
                for (int i = 0; i< Q.Count; i++)
                {
                    for (int j = 0; j < NL[i].Count; j++)
                    {
                        var values = new List<string>();
                        int e = 0;
                        if (i == 0) { e = index1[j]; }
                        else if (i == 1) { e = index2[j]; }
                        else if (i == 2) { e = index3[j]; }
                        values.Add(e.ToString());
                        values.Add((i + 1).ToString() + "F");
                        values.Add(BX[e].ToString("F6").Substring(0, 3));
                        values.Add(BY[e].ToString("F6").Substring(0, 3));
                        if (Qax[0] != -9999 || Qay[0] != -9999)
                        {
                            values.Add(Q[i].ToString("F6").Substring(0, Math.Max(4, Digit((int)Q[i]) + 2)));
                            values.Add(Qax[i].ToString("F6").Substring(0, Math.Max(4, Digit((int)Qax[i]) + 2)));
                            values.Add(Qay[i].ToString("F6").Substring(0, Math.Max(4, Digit((int)Qay[i]) + 2)));
                            values.Add(alphaX[i].ToString("F6").Substring(0, 4));
                            values.Add(alphaY[i].ToString("F6").Substring(0, 4));
                        }
                        values.Add(NX[i][j].ToString("F6").Substring(0, Math.Max(4, Digit((int)NX[i][j]) + 2)));
                        values.Add(NY[i][j].ToString("F6").Substring(0, Math.Max(4, Digit((int)NY[i][j]) + 2)));
                        values.Add((NX[i][j] * alphaX[i]).ToString("F6").Substring(0, Math.Max(4, Digit((int)(NX[i][j] * alphaX[i]) + 2))));
                        values.Add((NY[i][j] * alphaY[i]).ToString("F6").Substring(0, Math.Max(4, Digit((int)(NY[i][j] * alphaY[i]) + 2))));
                        values.Add(NL[i][j].ToString("F6").Substring(0, Math.Max(4, Digit((int)NL[i][j]) + 2)));
                        values.Add(TX[i][j].Value.ToString("F6").Substring(0, Math.Max(4, Digit((int)TX[i][j].Value) + 2)));
                        values.Add(TY[i][j].Value.ToString("F6").Substring(0, Math.Max(4, Digit((int)TY[i][j].Value) + 2)));
                        values.Add(TMAX[i][j].Value.ToString("F6").Substring(0, Math.Max(4, Digit((int)TMAX[i][j].Value) + 2)));
                        values.Add(NAME[i][j].Value);
                        values.Add(TA[i][j].Value.ToString("F6").Substring(0, Math.Max(4, Digit((int)TA[i][j].Value) + 2)));
                        var text = ":O.K.";
                        if(TMAX[i][j].Value / TA[i][j].Value >= 1.0) { text = ":N.G."; }
                        values.Add(Math.Max((TMAX[i][j].Value / TA[i][j].Value),0.0).ToString("F6").Substring(0, 4) + text);
                        var n = 10;
                        var slide = 0.0;
                        if (n <= k % (n*3) && k % (n*3) < n*2) { slide = pitchy * 21; }
                        if (n*2 <= k % (n*3) && k % (n*3) < n*3) { slide = pitchy * 42; }
                        if (k % n == 0)
                        {
                            if (k % (n * 3) == 0)
                            {
                                page = document.AddPage();// 空白ページを作成。
                                gfx = XGraphics.FromPdfPage(page);// 描画するためにXGraphicsオブジェクトを取得。
                            }
                            for (int ii = 0; ii < labels.Count; ii++)//ラベル列**************************************************************************
                            {
                                gfx.DrawLine(pen, offset_x, offset_y + pitchy * ii + slide, offset_x + label_width, offset_y + pitchy * ii + slide);//横線
                                gfx.DrawLine(pen, offset_x + label_width, offset_y + pitchy * ii + slide, offset_x + label_width, offset_y + pitchy * (ii + 1) + slide);//縦線
                                gfx.DrawLine(pen, offset_x, offset_y + pitchy * ii + slide, offset_x, offset_y + pitchy * (ii + 1) + slide);//縦線
                                gfx.DrawString(labels[ii], font, XBrushes.Black, new XRect(offset_x, offset_y + pitchy * ii + slide, label_width, offset_y + pitchy * (ii + 1) + slide), XStringFormats.TopCenter);
                                if (ii == labels.Count - 1)
                                {
                                    ii += 1;
                                    gfx.DrawLine(pen, offset_x, offset_y + pitchy * ii + slide, offset_x + label_width, offset_y + pitchy * ii + slide);//横線
                                }
                            }//***********************************************************************************************************************
                        }
                        for (int ii = 0; ii < values.Count; ii++)
                        {
                            var jj = k % n;
                            gfx.DrawLine(pen, offset_x + label_width + text_width * jj, offset_y + pitchy * ii + slide, offset_x + label_width + text_width * (jj + 1), offset_y + pitchy * ii + slide);//横線
                            gfx.DrawLine(pen, offset_x + label_width + text_width * (jj+1), offset_y + pitchy * ii + slide, offset_x + label_width + text_width * (jj + 1), offset_y + pitchy * (ii + 1) + slide);//縦線
                            if (ii == values.Count - 1)
                            {
                                gfx.DrawLine(pen, offset_x + label_width + text_width * jj, offset_y + pitchy * (ii + 1) + slide, offset_x + label_width + text_width * (jj + 1), offset_y + pitchy * (ii + 1) + slide);//横線
                            }
                            var color = XBrushes.Black;
                            if ((ii == 17 && Qax[0]!=-9999 && Qay[0]!=-9999) || (ii == 12 && (Qax[0]==-9999 || Qay[0]==-9999)))
                            {
                                color = new XSolidBrush(RGB(COLOR[i][j].H, COLOR[i][j].S, COLOR[i][j].L));
                            }
                            gfx.DrawString(values[ii], font, color, new XRect(offset_x + label_width + text_width * jj, offset_y + pitchy * ii + slide, text_width, offset_y + pitchy * (ii + 1) + slide), XStringFormats.TopCenter);
                        }
                        k += 1;
                    }
                }
                var dir = Path.GetDirectoryName(Rhino.RhinoDoc.ActiveDoc.Path);
                // ドキュメントを保存。
                var filename = dir + "/" + pdfname + ".pdf";
                document.Save(filename);
                // ビューアを起動。
                Process.Start(filename);
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
                return OpenSeesUtility.Properties.Resources.Nvalue;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("49249cb3-07c0-40e7-8b8a-ae6cd2613657"); }
        }
        ///ここからカスタム関数群********************************************************************************
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<string> _names = new List<string>();
        private readonly List<string> _Ns = new List<string>();
        private readonly List<string> _Nas = new List<string>();
        private readonly List<Point3d> _pts = new List<Point3d>();
        protected override void BeforeSolveInstance()
        {
            _colors.Clear();
            _names.Clear();
            _Ns.Clear();
            _Nas.Clear();
            _pts.Clear();
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            args.Viewport.GetFrustumFarPlane(out Plane plane);
            RhinoViewport viewport = args.Viewport;
            ///*************************************************************************************************
            ///結果描画関数
            for (int i = 0; i < _pts.Count; i++)
            {
                double size = fontsize; Point3d point = _pts[i]; plane.Origin = point;
                viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                if (N_Value == 1)
                {
                    args.Display.Draw3dText(_Ns[i], Color.Blue, plane, size, "", false, false, TextHorizontalAlignment.Left, TextVerticalAlignment.Bottom);
                }
                if (Na_Value == 1)
                {
                    args.Display.Draw3dText(_Nas[i], Color.Red, plane, size, "", false, false, TextHorizontalAlignment.Left, TextVerticalAlignment.Top);
                }
                if (N_Name == 1)
                {
                    args.Display.Draw3dText(_names[i], _colors[i], plane, size, "", false, false, TextHorizontalAlignment.Right, TextVerticalAlignment.Middle);
                }
            }
        }
        ///ここまでカスタム関数群********************************************************************************
        ///ここからGUIの作成*****************************************************************************************
        public class CustomGUI : GH_ComponentAttributes
        {
            public CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle title_rec;
            private Rectangle radio_rec; private Rectangle radio_rec2; private Rectangle radio_rec3;
            private Rectangle radio_rec_11; private Rectangle text_rec_11; private Rectangle radio_rec_12; private Rectangle text_rec_12; private Rectangle radio_rec_13; private Rectangle text_rec_13;
            private Rectangle radio_rec_21; private Rectangle text_rec_21; private Rectangle radio_rec_22; private Rectangle text_rec_22; private Rectangle radio_rec_23; private Rectangle text_rec_23;
            private Rectangle radio_rec_31; private Rectangle text_rec_31;

            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int height = 110; int subwidth = 44; int radi1 = 7; int radi2 = 5; int radi3 = 3;
                int pitchx = 6; int textheight = 20;
                global_rec.Height += height;
                title_rec = global_rec;
                title_rec.Y = title_rec.Bottom - height;
                title_rec.Height = 22;

                radio_rec = title_rec;
                radio_rec.Y += title_rec.Height;
                radio_rec2 = radio_rec;
                radio_rec3 = radio_rec;

                radio_rec_11 = radio_rec;
                radio_rec_11.X += radi2 - 1; radio_rec_11.Y = title_rec.Bottom + radi2;
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

                radio_rec_13 = text_rec_12;
                radio_rec_13.X += text_rec_12.Width - radi2; radio_rec_13.Y = radio_rec_12.Y;
                radio_rec_13.Height = radi1; radio_rec_13.Width = radi1;

                text_rec_13 = radio_rec_13;
                text_rec_13.X += pitchx; text_rec_13.Y -= radi2;
                text_rec_13.Height = textheight; text_rec_13.Width = subwidth;

                radio_rec_21 = radio_rec_11;
                radio_rec_21.Y += text_rec_11.Height - radi3;
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
                text_rec_23.Height = textheight; text_rec_23.Width = subwidth;

                radio_rec_31 = radio_rec_21;
                radio_rec_31.Y += text_rec_21.Height - radi3;
                radio_rec_31.Height = radi1; radio_rec_21.Width = radi1;

                text_rec_31 = radio_rec_31;
                text_rec_31.X += pitchx; text_rec_31.Y -= radi2;
                text_rec_31.Height = textheight; text_rec_31.Width = subwidth * 3;

                radio_rec.Height = text_rec_11.Y + textheight - radio_rec.Y - radi3;
                radio_rec2.Y += radio_rec.Height;
                radio_rec2.Height = text_rec_21.Y + textheight - radio_rec2.Y - radi3;
                radio_rec3 = radio_rec2;
                radio_rec3.Y += radio_rec2.Height; radio_rec3.Height = textheight;
                global_rec.Height = radio_rec3.Bottom - global_rec.Y;
                ///******************************************************************************************

                Bounds = global_rec;
            }
            Brush c11 = Brushes.White; Brush c12 = Brushes.White; Brush c13 = Brushes.White; Brush c21 = Brushes.White; Brush c22 = Brushes.White; Brush c23 = Brushes.White; Brush c31 = Brushes.White;
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

                    GH_Capsule radio_11 = GH_Capsule.CreateCapsule(radio_rec_11, GH_Palette.Black, 5, 5);
                    radio_11.Render(graphics, Selected, Owner.Locked, false); radio_11.Dispose();
                    graphics.FillEllipse(c11, radio_rec_11);
                    graphics.DrawString("1F", GH_FontServer.Standard, Brushes.Black, text_rec_11);

                    GH_Capsule radio_12 = GH_Capsule.CreateCapsule(radio_rec_12, GH_Palette.Black, 5, 5);
                    radio_12.Render(graphics, Selected, Owner.Locked, false); radio_12.Dispose();
                    graphics.FillEllipse(c12, radio_rec_12);
                    graphics.DrawString("2F", GH_FontServer.Standard, Brushes.Black, text_rec_12);

                    GH_Capsule radio_13 = GH_Capsule.CreateCapsule(radio_rec_13, GH_Palette.Black, 5, 5);
                    radio_13.Render(graphics, Selected, Owner.Locked, false); radio_13.Dispose();
                    graphics.FillEllipse(c13, radio_rec_13);
                    graphics.DrawString("3F", GH_FontServer.Standard, Brushes.Black, text_rec_13);

                    GH_Capsule radio2 = GH_Capsule.CreateCapsule(radio_rec2, GH_Palette.White, 2, 0);
                    radio2.Render(graphics, Selected, Owner.Locked, false); radio2.Dispose();

                    GH_Capsule radio_21 = GH_Capsule.CreateCapsule(radio_rec_21, GH_Palette.Black, 5, 5);
                    radio_21.Render(graphics, Selected, Owner.Locked, false); radio_21.Dispose();
                    graphics.FillEllipse(c21, radio_rec_21);
                    graphics.DrawString("N", GH_FontServer.Standard, Brushes.Black, text_rec_21);

                    GH_Capsule radio_22 = GH_Capsule.CreateCapsule(radio_rec_22, GH_Palette.Black, 5, 5);
                    radio_22.Render(graphics, Selected, Owner.Locked, false); radio_22.Dispose();
                    graphics.FillEllipse(c22, radio_rec_22);
                    graphics.DrawString("Na", GH_FontServer.Standard, Brushes.Black, text_rec_22);

                    GH_Capsule radio_23 = GH_Capsule.CreateCapsule(radio_rec_23, GH_Palette.Black, 5, 5);
                    radio_23.Render(graphics, Selected, Owner.Locked, false); radio_23.Dispose();
                    graphics.FillEllipse(c23, radio_rec_23);
                    graphics.DrawString("name", GH_FontServer.Standard, Brushes.Black, text_rec_23);

                    GH_Capsule radio3 = GH_Capsule.CreateCapsule(radio_rec3, GH_Palette.White, 2, 0);
                    radio3.Render(graphics, Selected, Owner.Locked, false); radio3.Dispose();

                    GH_Capsule radio_31 = GH_Capsule.CreateCapsule(radio_rec_31, GH_Palette.Black, 5, 5);
                    radio_31.Render(graphics, Selected, Owner.Locked, false); radio_31.Dispose();
                    graphics.FillEllipse(c31, radio_rec_31);
                    graphics.DrawString("PDF OUTPUT", GH_FontServer.Standard, Brushes.Black, text_rec_31);
                    ///******************************************************************************************
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec11 = radio_rec_11; RectangleF rec12 = radio_rec_12; RectangleF rec13 = radio_rec_13;
                    RectangleF rec21 = radio_rec_21; RectangleF rec22 = radio_rec_22; RectangleF rec23 = radio_rec_23;
                    RectangleF rec31 = radio_rec_31; 
                    if (rec11.Contains(e.CanvasLocation))
                    {
                        if (c11 == Brushes.Black) { c11 = Brushes.White; SetButton("c11", 0); }
                        else
                        { c11 = Brushes.Black; SetButton("c11", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec12.Contains(e.CanvasLocation))
                    {
                        if (c12 == Brushes.Black) { c12 = Brushes.White; SetButton("c12", 0); }
                        else
                        { c12 = Brushes.Black; SetButton("c12", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec13.Contains(e.CanvasLocation))
                    {
                        if (c13 == Brushes.Black) { c13 = Brushes.White; SetButton("c13", 0); }
                        else
                        { c13 = Brushes.Black; SetButton("c13", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec21.Contains(e.CanvasLocation))
                    {
                        if (c21 == Brushes.Black) { c21 = Brushes.White; SetButton("c21", 0); }
                        else
                        { c21 = Brushes.Black; SetButton("c21", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec22.Contains(e.CanvasLocation))
                    {
                        if (c22 == Brushes.Black) { c22 = Brushes.White; SetButton("c22", 0); }
                        else
                        { c22 = Brushes.Black; SetButton("c22", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec23.Contains(e.CanvasLocation))
                    {
                        if (c23 == Brushes.Black) { c23 = Brushes.White; SetButton("c23", 0); }
                        else
                        { c23 = Brushes.Black; SetButton("c23", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec31.Contains(e.CanvasLocation))
                    {
                        if (c31 == Brushes.Black) { c31 = Brushes.White; SetButton("c31", 0); }
                        else
                        { c31 = Brushes.Black; SetButton("c31", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    ///*************************************************************************************************************************************************
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}