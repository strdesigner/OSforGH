using System;
using System.Collections.Generic;
using System.IO;
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
using System.Diagnostics;

using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp;
using Rhino;

namespace OpenSeesUtility
{
    public class AnalysisResultPDF : GH_Component
    {
        public static int on_off = 0; public static double fontsize = 9;
        public static PdfCreate.JapaneseFontResolver fontresolver = new PdfCreate.JapaneseFontResolver();
        public static XGraphics gfx;
        public static void SetButton(string s, int i)
        {
            if (s == "1")
            {
                on_off = i;
            }
        }
        public AnalysisResultPDF()
          : base("AnalysisResultPDF", "AnalysisResultPDF",
              "Output Analysis result to pdf",
              "OpenSees", "Utility")
        {
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("R", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("IJ", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("sec_f", "sec_f", "[[element No.,Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree);
            pManager.AddVectorParameter("element axis vector", "l_vec", "element axis vector for each elements", GH_ParamAccess.list, new Vector3d(-9999, -9999, -9999));
            pManager.AddNumberParameter("index", "index", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("KABE_W", "KABE_W", "[[No.i,No.j,No.k,No.l,kabebairitsu],...](DataTree)", GH_ParamAccess.tree, -9999);///9
            pManager.AddNumberParameter("shear_w", "shear_w", "[Q1,Q2,...](DataList)", GH_ParamAccess.list, -9999);///10
            pManager.AddNumberParameter("B", "B", "[[node No.,X,Y,Z,MX,MY,MZ],...](DataTree) 0-1 variable", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("joint", "joint", "[[Ele. No., 0 or 1 or 2(means i or j or both), kx, ky, kz, rx, ry, rz(spring value)],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("spring", "spring", "[[No.i,No.j,kx+,kx-,ky+,ky-,kz+,kz-,mx,my,mz,angle],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddTextParameter("names", "names", "[[layer,wick],[layer,wick],...](Datatree)", GH_ParamAccess.tree, "");
            pManager.AddTextParameter("names(shell)", "names(shell)", "[[layer,wick],[layer,wick],...](Datatree)", GH_ParamAccess.tree, "");
            pManager.AddTextParameter("names(spring)", "names(spring)", "[[layer,wick],[layer,wick],...](Datatree)", GH_ParamAccess.tree, "");
            pManager.AddTextParameter("outputname", "outputname", "output file name", GH_ParamAccess.item, "");///
            pManager.AddNumberParameter("scaling", "scaling", "scale factor of figures", GH_ParamAccess.item, 0.85);///
            pManager.AddNumberParameter("offset", "offset", "offset value of figures", GH_ParamAccess.item, 25.0);///
            pManager.AddNumberParameter("scale_factor_for_N,Q", "NS", "scale factor for N,Q", GH_ParamAccess.item, 0.1);///
            pManager.AddNumberParameter("scale_factor_for_M", "MS", "scale factor for M", GH_ParamAccess.item, 0.15);///
            pManager.AddNumberParameter("scale_factor_for_Qw", "QwS", "scale factor for Qw", GH_ParamAccess.item, 0.1);///
            pManager.AddTextParameter("casename", "casename", "files are named _casename.pdf", GH_ParamAccess.list, new List<string> { "L", "X", "Y" });///
            pManager.AddTextParameter("casememo", "casememo", "load case name in sheets", GH_ParamAccess.list, new List<string> { "(長期荷重時)", "+X荷重時", "+Y荷重時" });///
            pManager.AddNumberParameter("fontsize", "fontsize", "fontsize", GH_ParamAccess.item, 9);///
            pManager.AddNumberParameter("linewidth", "linewidth", "linewidth", GH_ParamAccess.item, 1);///
            pManager.AddNumberParameter("pointsize", "pointsize", "pointsize", GH_ParamAccess.item, 2);///
            pManager.AddNumberParameter("jointsize", "jointsize", "jointsize", GH_ParamAccess.item, 3);///
            pManager.AddIntegerParameter("separate option", "separate option", "0:node number, element number angle, material number, and section number are written same page, 1:written other pages", GH_ParamAccess.item, 0);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (on_off == 1)
            {
                XColor RGB(double h, double s, double l)//convert HSL to RGB
                {
                    var max = 0.0; var min = 0.0; var r = 0.0; var g = 0.0; var b = 0.0;
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
                        r = max;
                        g = (h / hp) * (max - min) + min;
                        b = min;
                    }
                    else if (q <= 2)
                    {
                        r = ((hp * 2 - h) / hp) * (max - min) + min;
                        g = max;
                        b = min;
                    }
                    else if (q <= 3)
                    {
                        r = min;
                        g = max;
                        b = ((h - hp * 2) / hp) * (max - min) + min;
                    }
                    else if (q <= 4)
                    {
                        r = min;
                        g = ((hp * 4 - h) / hp) * (max - min) + min;
                        b = max;
                    }
                    else if (q <= 5)
                    {
                        r = ((h - hp * 4) / hp) * (max - min) + min;
                        g = min;
                        b = max;
                    }
                    else
                    {
                        r = max;
                        g = min;
                        b = ((HUE_MAX - h) / hp) * (max - min) + min;
                    }
                    r *= RGB_MAX; g *= RGB_MAX; b *= RGB_MAX;
                    return XColor.FromArgb((int)r, (int)g, (int)b);
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
                DA.GetDataTree("R", out GH_Structure<GH_Number> _R); var R = _R.Branches; DA.GetDataTree("IJ", out GH_Structure<GH_Number> _ij); var ij = _ij.Branches; DA.GetDataTree("sec_f", out GH_Structure<GH_Number> _sec_f); var sec_f = _sec_f.Branches; DA.GetDataTree("joint", out GH_Structure<GH_Number> _joint); var joint = _joint.Branches; DA.GetDataTree("B", out GH_Structure<GH_Number> _B); var B = _B.Branches;
                var joint_No = new List<int>(); for (int i = 0; i < joint.Count; i++) { joint_No.Add((int)joint[i][0].Value); }
                var B_No = new List<int>(); for (int i = 0; i < B.Count; i++) { B_No.Add((int)B[i][0].Value); }
                var l_vec = new List<Vector3d>(); DA.GetDataList(3, l_vec);
                DA.GetDataTree("KABE_W", out GH_Structure<GH_Number> _kabe_w); var kabe_w = _kabe_w.Branches;
                var shear_w = new List<double>(); DA.GetDataList("shear_w", shear_w);
                DA.GetDataTree("names", out GH_Structure<GH_String> _names); var names = _names.Branches; DA.GetDataTree("names(shell)", out GH_Structure<GH_String> _names2); var names2 = _names2.Branches; DA.GetDataTree("names(spring)", out GH_Structure<GH_String> _names3); var names3 = _names3.Branches;
                var pdfname = "TimberCheck"; DA.GetData("outputname", ref pdfname); var scaling = 0.95; DA.GetData("scaling", ref scaling); var offset = 25.0; DA.GetData("offset", ref offset); var offsety = offset * 2; var lw = 1.0; DA.GetData("linewidth", ref lw); var js = 1.0; DA.GetData("jointsize", ref js); var ps = 1.0; DA.GetData("pointsize", ref ps);
                var nscale = 0.1; DA.GetData("scale_factor_for_N,Q", ref nscale); var mscale = 0.15; DA.GetData("scale_factor_for_M", ref mscale); var qscale = 0.025; DA.GetData("scale_factor_for_Qw", ref qscale);
                var index = new List<double>(); DA.GetDataList("index", index); DA.GetData("fontsize", ref fontsize);
                var layer = new List<string>(); var wick = new List<string>(); var wicks = new List<List<string>>(); var wicks2 = new List<List<string>>(); var wicks3 = new List<List<string>>();
                int separate_option = 0; DA.GetData("separate option", ref separate_option);
                DA.GetDataTree("spring", out GH_Structure<GH_Number> _spring); var spring = _spring.Branches;
                for (int i = 0; i < names.Count; i++)
                {
                    var w = new List<string>();
                    if (layer.Contains(names[i][0].Value) == false) { layer.Add(names[i][0].Value); }
                    if (names[i].Count >= 2)
                    {
                        if (wick.Contains(names[i][1].Value) == false) { wick.Add(names[i][1].Value); }
                        w.Add(names[i][1].Value);
                    }
                    if (names[i].Count >= 3)
                    {
                        if (wick.Contains(names[i][2].Value) == false) { wick.Add(names[i][2].Value); }
                        w.Add(names[i][2].Value);
                    }
                    if (names[i].Count >= 4)
                    {
                        if (wick.Contains(names[i][3].Value) == false) { wick.Add(names[i][3].Value); }
                        w.Add(names[i][3].Value);
                    }
                    wicks.Add(w);
                }
                if (names2[0][0].Value != "")
                {
                    for (int i = 0; i < names2.Count; i++)
                    {
                        var w = new List<string>();
                        if (names2[i].Count >= 2)
                        {
                            w.Add(names2[i][1].Value);
                        }
                        if (names2[i].Count >= 3)
                        {
                            w.Add(names2[i][2].Value);
                        }
                        if (names2[i].Count >= 4)
                        {
                            w.Add(names2[i][3].Value);
                        }
                        wicks2.Add(w);
                    }
                }
                if (names3[0][0].Value != "")
                {
                    for (int i = 0; i < names3.Count; i++)
                    {
                        var w = new List<string>();
                        if (names3[i].Count >= 2)
                        {
                            w.Add(names3[i][1].Value);
                        }
                        if (names3[i].Count >= 3)
                        {
                            w.Add(names3[i][2].Value);
                        }
                        if (names3[i].Count >= 4)
                        {
                            w.Add(names3[i][3].Value);
                        }
                        wicks3.Add(w);
                    }
                }
                var Xmin = 9999.0; var Xmax = -9999.0; var Ymin = 9999.0; var Ymax = -9999.0; var Zmin = 9999.0; var Zmax = -9999.0;
                for (int i = 0; i < R.Count; i++)
                {
                    Xmin = Math.Min(Xmin, R[i][0].Value); Xmax = Math.Max(Xmax, R[i][0].Value);
                    Ymin = Math.Min(Ymin, R[i][1].Value); Ymax = Math.Max(Ymax, R[i][1].Value);
                    Zmin = Math.Min(Zmin, R[i][2].Value); Zmax = Math.Max(Zmax, R[i][2].Value);
                }
                var rangexy = Math.Max(Xmax - Xmin, Ymax - Ymin); var rangez = Zmax - Zmin;//架構の範囲
                var scale = Math.Min(594.0 / rangexy * scaling, 842.0 / rangez * scaling); var pinwidth = 0.04;
                var tri = lw*5;
                // フォントリゾルバーのグローバル登録
                if (PdfCreate.JapaneseFontResolver.fontset == 0) { PdfSharp.Fonts.GlobalFontSettings.FontResolver = fontresolver; PdfCreate.JapaneseFontResolver.fontset = 1; }
                // フォントを作成。
                XFont font = new XFont("Gen Shin Gothic", fontsize, XFontStyle.Regular);
                XFont titlefont = new XFont("Gen Shin Gothic", fontsize * 2, XFontStyle.Regular);
                XFont fontbold = new XFont("Gen Shin Gothic", fontsize, XFontStyle.Bold);
                var pen = new XPen(XColors.Black, lw); var penspring = new XPen(XColors.BlueViolet, lw);
                var pengray = new XPen(XColors.Gray, lw);
                var dir = Path.GetDirectoryName(Rhino.RhinoDoc.ActiveDoc.Path);//カレントディレクトリ
                wick.Sort();
                // PDFドキュメントを作成。
                PdfDocument document = new PdfDocument();
                document.Info.Title = pdfname;
                document.Info.Author = "Shinnosuke Fujita, Assoc. Prof., The Univ. of Kitakyushu shinnosuke@dn-archi.com";
                var figname = new List<string> { "通りモデル図" };
                if (separate_option == 1)
                {
                    figname = new List<string> { "通り節点番号図", "通り部材番号図", "通り材料番号図", "通り断面番号図", "通りコードアングル図", "通り部材番号図(ばね)" };
                }
                for (int j = 0; j < wick.Count; j++)//1; j++)//
                {
                    for (int k = 0; k < figname.Count; k++)
                    {
                        PdfPage page = new PdfPage(); page.Size = PageSize.A4;// 空白ページを作成。width x height = 594 x 842
                        page = document.AddPage();// 描画するためにXGraphicsオブジェクトを取得。
                        gfx = XGraphics.FromPdfPage(page);
                        var ij_new = new List<List<double>>();//その軸・通りの要素節点関係
                        for (int e = 0; e < names.Count; e++)
                        {
                            var list = new List<double>();
                            if (wicks[e].Contains(wick[j]) == true)
                            {
                                list.Add(e);
                                for (int i = 0; i < ij[e].Count; i++)
                                {
                                    list.Add(ij[e][i].Value);
                                }
                                ij_new.Add(list);
                            }
                        }
                        var kabe_w_new = new List<List<double>>();//その軸・通りの耐力壁
                        if (names2[0][0].Value != "" && kabe_w[0][0].Value != -9999)
                        {
                            for (int e = 0; e < names2.Count; e++)
                            {
                                var list = new List<double>();
                                if (wicks2[e].Contains(wick[j]) == true)
                                {
                                    list.Add(e);
                                    for (int i = 0; i < kabe_w[e].Count; i++)
                                    {
                                        list.Add(kabe_w[e][i].Value);
                                    }
                                    kabe_w_new.Add(list);
                                }
                            }
                        }
                        var spring_new = new List<List<double>>();//その軸・通りのばね
                        if (names3[0][0].Value != "" && spring[0][0].Value != -9999)
                        {
                            for (int e = 0; e < names3.Count; e++)
                            {
                                var list = new List<double>();
                                if (wicks3[e].Contains(wick[j]) == true)
                                {
                                    list.Add(e);
                                    for (int i = 0; i < spring[e].Count; i++)
                                    {
                                        list.Add(spring[e][i].Value);
                                    }
                                    spring_new.Add(list);
                                }
                            }
                        }
                        var xmin = 9999.0; var xmax = -9999.0; var ymin = 9999.0; var ymax = -9999.0; var zmin = 9999.0; var zmax = -9999.0;
                        for (int e = 0; e < ij_new.Count; e++)
                        {
                            int ni = (int)ij_new[e][1]; int nj = (int)ij_new[e][2];
                            xmin = Math.Min(xmin, R[ni][0].Value); xmax = Math.Max(xmax, R[ni][0].Value); xmin = Math.Min(xmin, R[nj][0].Value); xmax = Math.Max(xmax, R[nj][0].Value);
                            ymin = Math.Min(ymin, R[ni][1].Value); ymax = Math.Max(ymax, R[ni][1].Value); ymin = Math.Min(ymin, R[nj][1].Value); ymax = Math.Max(ymax, R[nj][1].Value);
                            zmin = Math.Min(zmin, R[ni][2].Value); zmax = Math.Max(zmax, R[ni][2].Value); zmin = Math.Min(zmin, R[nj][2].Value); zmax = Math.Max(zmax, R[nj][2].Value);
                        }
                        var vec = new Vector3d(xmax - xmin, ymax - ymin, 0.0);
                        var cos = (xmax - xmin) / Math.Sqrt(Math.Pow(xmax - xmin, 2) + Math.Pow(ymax - ymin, 2));//(1,0,0)との角度
                        var theta = -Math.Acos(cos) / Math.PI * 180.0;
                        var r0 = new Vector3d(xmin, ymin, Zmin);//左下
                        var r_ij = new List<List<List<double>>>(); var zvec = new Vector3d(0, 0, 1);
                        r0 = rotation(r0, zvec, theta);//回転後の左下
                        if (names2[0][0].Value != "")
                        {
                            for (int e = 0; e < kabe_w_new.Count; e++)
                            {
                                int ni = (int)kabe_w_new[e][1]; int nj = (int)kabe_w_new[e][2]; int nk = (int)kabe_w_new[e][3]; int nl = (int)kabe_w_new[e][4]; int nel = (int)kabe_w_new[e][0];
                                var alpha = kabe_w_new[e][5];
                                if (alpha != 0.0)
                                {
                                    var vi = rotation(new Vector3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value), zvec, theta) - r0;
                                    var vj = rotation(new Vector3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value), zvec, theta) - r0;
                                    var vk = rotation(new Vector3d(R[nk][0].Value, R[nk][1].Value, R[nk][2].Value), zvec, theta) - r0;
                                    var vl = rotation(new Vector3d(R[nl][0].Value, R[nl][1].Value, R[nl][2].Value), zvec, theta) - r0;
                                    var r1 = new List<double>(); r1.Add(offset + vi[0] * scale); r1.Add(842 - offsety - vi[2] * scale);
                                    var r2 = new List<double>(); r2.Add(offset + vj[0] * scale); r2.Add(842 - offsety - vj[2] * scale);
                                    var r3 = new List<double>(); r3.Add(offset + vk[0] * scale); r3.Add(842 - offsety - vk[2] * scale);
                                    var r4 = new List<double>(); r4.Add(offset + vl[0] * scale); r4.Add(842 - offsety - vl[2] * scale);
                                    var rp1 = new List<double>(); rp1.Add(r1[0] + (r3[0] - r1[0]) * pinwidth); rp1.Add(r1[1] + (r3[1] - r1[1]) * pinwidth);
                                    var rp2 = new List<double>(); rp2.Add(r2[0] + (r4[0] - r2[0]) * pinwidth); rp2.Add(r2[1] + (r4[1] - r2[1]) * pinwidth);
                                    var rp3 = new List<double>(); rp3.Add(r3[0] + (r1[0] - r3[0]) * pinwidth); rp3.Add(r3[1] + (r1[1] - r3[1]) * pinwidth);
                                    var rp4 = new List<double>(); rp4.Add(r4[0] + (r2[0] - r4[0]) * pinwidth); rp4.Add(r4[1] + (r2[1] - r4[1]) * pinwidth);
                                    var rc = new List<double> { (r1[0] + r2[0] + r3[0] + r4[0]) / 4.0, (r1[1] + r2[1] + r3[1] + r4[1]) / 4.0 };
                                    gfx.DrawLine(pengray, r1[0], r1[1], r3[0], r3[1]); gfx.DrawLine(pengray, r2[0], r2[1], r4[0], r4[1]);//線材置換トラスの描画
                                    gfx.DrawEllipse(pengray, XBrushes.White, rp1[0] - js / 2.0, rp1[1] - js / 2.0, js, js);//ピン記号
                                    gfx.DrawEllipse(pengray, XBrushes.White, rp2[0] - js / 2.0, rp2[1] - js / 2.0, js, js);//ピン記号
                                    gfx.DrawEllipse(pengray, XBrushes.White, rp3[0] - js / 2.0, rp3[1] - js / 2.0, js, js);//ピン記号
                                    gfx.DrawEllipse(pengray, XBrushes.White, rp4[0] - js / 2.0, rp4[1] - js / 2.0, js, js);//ピン記号
                                    var color = new XSolidBrush(RGB((1 - Math.Min(alpha / 5.0, 1.0)) * 1.9 / 3.0, 1, 0.5));
                                    if (k == 0) { gfx.DrawString(Math.Round(alpha, 2).ToString().Substring(0, Math.Min(4, Math.Round(alpha, 2).ToString().Length)) + "倍", font, color, rc[0], rc[1], XStringFormats.TopCenter); }//壁倍率
                                    if (k == 0) { gfx.DrawString(nel.ToString(), font, XBrushes.Black, rc[0], rc[1], XStringFormats.BottomCenter); }//壁番号
                                }
                            }
                        }
                        if (names3[0][0].Value != "")
                        {
                            for (int e = 0; e < spring_new.Count; e++)
                            {
                                int ni = (int)spring_new[e][1]; int nj = (int)spring_new[e][2]; int nel = (int)spring_new[e][0];
                                var vi = rotation(new Vector3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value), zvec, theta) - r0;
                                var vj = rotation(new Vector3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value), zvec, theta) - r0;
                                var r1 = new List<double>(); r1.Add(offset + vi[0] * scale); r1.Add(842 - offsety - vi[2] * scale);
                                var r2 = new List<double>(); r2.Add(offset + vj[0] * scale); r2.Add(842 - offsety - vj[2] * scale);
                                gfx.DrawLine(penspring, r1[0], r1[1], r2[0], r2[1]);//ばねの描画
                                gfx.DrawEllipse(XBrushes.Black, r1[0] - ps / 2.0, r1[1] - ps / 2.0, ps, ps);//i節点の描画
                                if (k == 0) { gfx.DrawString(ni.ToString(), font, XBrushes.Red, r1[0], r1[1], XStringFormats.BaseLineLeft); }//i節点の節点番号描画
                                gfx.DrawEllipse(XBrushes.Black, r2[0] - ps / 2.0, r2[1] - ps / 2.0, ps, ps);//j節点の描画
                                if (k == 0) { gfx.DrawString(nj.ToString(), font, XBrushes.Red, r2[0], r2[1], XStringFormats.BaseLineLeft); }//j節点の節点番号描画
                                if (figname.Count == 1 || k == 5) { gfx.DrawString(nel.ToString(), font, XBrushes.DarkOrange, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, XStringFormats.BaseLineLeft); }//要素番号描画
                            }
                        }
                        for (int e = 0; e < ij_new.Count; e++)//紙面に平行に回転後の骨組
                        {
                            int ni = (int)ij_new[e][1]; int nj = (int)ij_new[e][2]; int nel = (int)ij_new[e][0];
                            int mat = (int)ij_new[e][3]; int sec = (int)ij_new[e][4]; int angle = 0;//(int)ij_new[e][5];
                            var vi = rotation(new Vector3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value), zvec, theta) - r0;
                            var vj = rotation(new Vector3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value), zvec, theta) - r0;
                            var r1 = new List<double>(); r1.Add(offset + vi[0] * scale); r1.Add(842 - offsety - vi[2] * scale);
                            var r2 = new List<double>(); r2.Add(offset + vj[0] * scale); r2.Add(842 - offsety - vj[2] * scale);
                            r_ij.Add(new List<List<double>> { r1, r2 });
                            gfx.DrawLine(pen, r1[0], r1[1], r2[0], r2[1]);//骨組の描画
                            gfx.DrawEllipse(XBrushes.Black, r1[0] - ps / 2.0, r1[1] - ps / 2.0, ps, ps);//i節点の描画
                            if (k == 0) { gfx.DrawString(ni.ToString(), font, XBrushes.Red, r1[0], r1[1], XStringFormats.BaseLineLeft); }//i節点の節点番号描画
                            gfx.DrawEllipse(XBrushes.Black, r2[0] - ps / 2.0, r2[1] - ps / 2.0, ps, ps);//j節点の描画
                            if (k == 0) { gfx.DrawString(nj.ToString(), font, XBrushes.Red, r2[0], r2[1], XStringFormats.BaseLineLeft); }//j節点の節点番号描画
                            if (figname.Count == 1 || k == 1) { gfx.DrawString(nel.ToString(), font, XBrushes.Blue, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, XStringFormats.BaseLineLeft); }//要素番号描画
                            if (figname.Count == 1 || k == 2) { gfx.DrawString(mat.ToString(), font, XBrushes.Orange, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, XStringFormats.TopRight); }//材料番号描画
                            if (figname.Count == 1) { gfx.DrawString(",", font, XBrushes.Black, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, XStringFormats.TopCenter); }//カンマ描画
                            if (figname.Count == 1 || k == 3) { gfx.DrawString(sec.ToString(), font, XBrushes.Pink, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, XStringFormats.TopLeft); }//断面番号描画
                            if (figname.Count == 1 || k == 4) { gfx.DrawString(((int)ij_new[e][5]).ToString() + "°", font, XBrushes.DarkGreen, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, XStringFormats.BaseLineRight); }//コードアングル描画
                            if (figname.Count == 1) { gfx.DrawString(",", font, XBrushes.Black, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, XStringFormats.BaseLineCenter); }//カンマ描画
                            if (joint_No.Contains(nel) == true)//材端ピン
                            {
                                int i = joint_No.IndexOf(nel);
                                if (joint[i][1].Value == 0 || joint[i][1].Value == 2)
                                {
                                    var rp1 = new List<double>(); rp1.Add(r1[0] + (r2[0] - r1[0]) * pinwidth); rp1.Add(r1[1] + (r2[1] - r1[1]) * pinwidth);
                                    gfx.DrawEllipse(pen, XBrushes.White, rp1[0] - js / 2.0, rp1[1] - js / 2.0, js, js);//ピン記号
                                }
                                if (joint[i][1].Value == 1 || joint[i][1].Value == 2)
                                {
                                    var rp2 = new List<double>(); rp2.Add(r2[0] + (r1[0] - r2[0]) * pinwidth); rp2.Add(r2[1] + (r1[1] - r2[1]) * pinwidth);
                                    gfx.DrawEllipse(pen, XBrushes.White, rp2[0] - js / 2.0, rp2[1] - js / 2.0, js, js);//ピン記号
                                }
                            }
                            if (B_No.Contains(ni) == true)//境界条件
                            {
                                int i = B_No.IndexOf(ni);
                                XPoint[] pts = new XPoint[3]; pts[0].X = r1[0]; pts[0].Y = r1[1]; pts[1].X = r1[0] - tri / 2.0; pts[1].Y = r1[1] + tri / 2.0 * Math.Sqrt(3); pts[2].X = r1[0] + tri / 2.0; pts[2].Y = r1[1] + tri / 2.0 * Math.Sqrt(3);
                                var pc = new XPoint(); pc.X = (pts[1].X + pts[2].X) / 2.0; pc.Y = pts[1].Y;
                                gfx.DrawPolygon(pen, pts);
                                gfx.DrawString(((int)B[i][1].Value).ToString() + ((int)B[i][2].Value).ToString() + ((int)B[i][3].Value).ToString() + ((int)B[i][4].Value).ToString() + ((int)B[i][5].Value).ToString() + ((int)B[i][6].Value).ToString(), font, XBrushes.LightGray, pc.X, pc.Y, XStringFormat.TopCenter);
                            }
                            if (B_No.Contains(nj) == true)//境界条件
                            {
                                int i = B_No.IndexOf(nj);
                                XPoint[] pts = new XPoint[3]; pts[0].X = r2[0]; pts[0].Y = r2[1]; pts[1].X = r2[0] - tri / 2.0; pts[1].Y = r2[1] + tri / 2.0 * Math.Sqrt(3); pts[2].X = r2[0] + tri / 2.0; pts[2].Y = r2[1] + tri / 2.0 * Math.Sqrt(3);
                                var pc = new XPoint(); pc.X = (pts[1].X + pts[2].X) / 2.0; pc.Y = pts[1].Y;
                                gfx.DrawPolygon(pen, pts);
                            }
                        }
                        gfx.DrawString(wick[j] + figname[k], titlefont, XBrushes.Black, offset, 842 - offset, XStringFormats.BaseLineLeft);
                    }
                }
                var filename = dir + "/" + pdfname + "_model.pdf";
                document.Save(filename);// ドキュメントを保存。
                Process.Start(filename);// ビューアを起動。

                //応力図
                if (l_vec[0] != new Vector3d(-9999, -9999, -9999) && sec_f[0][0].Value != -9999)
                {
                    int nf=sec_f[0].Count; var label = new List<string> { "L","X","Y"}; var casememo = new List<string> { "(長期荷重時)", "+X荷重時", "+Y荷重時" };
                    DA.GetDataList("casename", label); DA.GetDataList("casememo", casememo);
                    int div = 30;
                    if (index[0] == -9999)
                    {
                        index = new List<double>();
                        for(int e = 0; e < ij.Count; e++) { index.Add(e); }
                    }
                    var Nmax = 0.0; var Nmin = 0.0; var Mxmax = 0.0; var Mymax = 0.0; var Mzmax = 0.0; var Qymax = 0.0; var Qzmax = 0.0;
                    for (int ii = 0; ii < nf / 18; ii++)
                    {
                        for (int ind = 0; ind < index.Count; ind++)//応力の最大値最小値
                        {
                            int e = (int)index[ind];
                            for (int i = 0; i < 3; i++)
                            {
                                Qymax = Math.Max(Qymax, Math.Abs(sec_f[e][i * 6 + 1 + ii * 18].Value) * nscale); Qzmax = Math.Max(Qzmax, Math.Abs(sec_f[e][i * 6 + 2 + ii * 18].Value) * nscale);
                                Mxmax = Math.Max(Mxmax, Math.Abs(sec_f[e][i * 6 + 3 + ii * 18].Value) * mscale); Mymax = Math.Max(Mymax, Math.Abs(sec_f[e][i * 6 + 4 + ii * 18].Value) * mscale); Mzmax = Math.Max(Mzmax, Math.Abs(sec_f[e][i * 6 + 5 + ii * 18].Value) * mscale);
                            }
                            Nmax = Math.Max(Nmax, Math.Max(sec_f[e][0 + ii * 18].Value, Math.Max(-sec_f[e][6 + ii * 18].Value, sec_f[e][12 + ii * 18].Value)) * nscale);
                            Nmin = Math.Min(Nmin, Math.Min(sec_f[e][0 + ii * 18].Value, Math.Min(-sec_f[e][6 + ii * 18].Value, sec_f[e][12 + ii * 18].Value)) * nscale);
                        }
                    }
                    for (int ii= 0;ii< nf / 18; ii++)
                    {
                        var Qmax = Math.Max(Qymax, Qzmax);
                        var Qwmax = 0.0;
                        for (int j = 0; j < shear_w.Count / 3; j++) { Qwmax = Math.Max(Qwmax, shear_w[(shear_w.Count / 3)*ii + j]); }
                        Qwmax *= qscale * 100 /2.0;
                        //せん断力図/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        document = new PdfDocument();
                        document.Info.Title = pdfname;
                        document.Info.Author = "Shinnosuke Fujita, Assoc. Prof., The Univ. of Kitakyushu";
                        for (int j = 0; j < wick.Count; j++)//1; j++)//
                        {
                            PdfPage page = new PdfPage(); page.Size = PageSize.A4;// 空白ページを作成。width x height = 594 x 842
                            page = document.AddPage();// 描画するためにXGraphicsオブジェクトを取得。
                            gfx = XGraphics.FromPdfPage(page);
                            var ij_new = new List<List<double>>();//その軸・通りの要素節点関係
                            var sec_f_new = new List<List<double>>();//その軸・通りの断面力
                            for (int e = 0; e < names.Count; e++)
                            {
                                var list = new List<double>(); var flist = new List<double>();
                                if (wicks[e].Contains(wick[j]) == true)
                                {
                                    list.Add(e);
                                    for (int i = 0; i < ij[e].Count; i++)
                                    {
                                        list.Add(ij[e][i].Value);
                                    }
                                    ij_new.Add(list);
                                    if (index.Contains(e) == true)
                                    {
                                        for (int i = 0; i < 18; i++)
                                        {
                                            flist.Add(sec_f[e][i + ii * 18].Value);
                                        }
                                    }
                                    else { for (int i = 0; i < 18; i++) { flist.Add(0.0); } }
                                    sec_f_new.Add(flist);
                                }
                            }
                            var kabe_w_new = new List<List<double>>();//その軸・通りの耐力壁
                            var shear_w_new = new List<double>();//その軸・通りのせん断力
                            if (names2[0][0].Value != "" && shear_w[0]!=-9999 && kabe_w[0][0].Value!=-9999)
                            {
                                for (int e = 0; e < names2.Count; e++)
                                {
                                    var list = new List<double>();
                                    if (wicks2[e].Contains(wick[j]) == true)
                                    {
                                        list.Add(e);
                                        for (int i = 0; i < kabe_w[e].Count; i++)
                                        {
                                            list.Add(kabe_w[e][i].Value);
                                        }
                                        kabe_w_new.Add(list);
                                        if (kabe_w.Count < shear_w.Count) { shear_w_new.Add(shear_w[e + kabe_w.Count * ii]); }
                                    }
                                }
                            }
                            var xmin = 9999.0; var xmax = -9999.0; var ymin = 9999.0; var ymax = -9999.0; var zmin = 9999.0; var zmax = -9999.0;
                            for (int e = 0; e < ij_new.Count; e++)
                            {
                                int ni = (int)ij_new[e][1]; int nj = (int)ij_new[e][2];
                                xmin = Math.Min(xmin, R[ni][0].Value); xmax = Math.Max(xmax, R[ni][0].Value); xmin = Math.Min(xmin, R[nj][0].Value); xmax = Math.Max(xmax, R[nj][0].Value);
                                ymin = Math.Min(ymin, R[ni][1].Value); ymax = Math.Max(ymax, R[ni][1].Value); ymin = Math.Min(ymin, R[nj][1].Value); ymax = Math.Max(ymax, R[nj][1].Value);
                                zmin = Math.Min(zmin, R[ni][2].Value); zmax = Math.Max(zmax, R[ni][2].Value); zmin = Math.Min(zmin, R[nj][2].Value); zmax = Math.Max(zmax, R[nj][2].Value);
                            }
                            var vec = new Vector3d(xmax - xmin, ymax - ymin, 0.0);
                            var cos = (xmax - xmin) / Math.Sqrt(Math.Pow(xmax - xmin, 2) + Math.Pow(ymax - ymin, 2));//(1,0,0)との角度
                            var theta = -Math.Acos(cos) / Math.PI * 180.0;
                            var r0 = new Vector3d(xmin, ymin, Zmin);//左下
                            var r_ij = new List<List<List<double>>>(); var zvec = new Vector3d(0, 0, 1);
                            r0 = rotation(r0, zvec, theta);//回転後の左下
                            if (names2[0][0].Value != "")
                            {
                                for (int e = 0; e < kabe_w_new.Count; e++)
                                {
                                    int ni = (int)kabe_w_new[e][1]; int nj = (int)kabe_w_new[e][2]; int nk = (int)kabe_w_new[e][3]; int nl = (int)kabe_w_new[e][4]; int nel = (int)kabe_w_new[e][0];
                                    var alpha = kabe_w_new[e][5];
                                    if (alpha != 0.0)
                                    {
                                        var vi = rotation(new Vector3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value), zvec, theta) - r0;
                                        var vj = rotation(new Vector3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value), zvec, theta) - r0;
                                        var vk = rotation(new Vector3d(R[nk][0].Value, R[nk][1].Value, R[nk][2].Value), zvec, theta) - r0;
                                        var vl = rotation(new Vector3d(R[nl][0].Value, R[nl][1].Value, R[nl][2].Value), zvec, theta) - r0;
                                        var r1 = new List<double>(); r1.Add(offset + vi[0] * scale); r1.Add(842 - offsety - vi[2] * scale);
                                        var r2 = new List<double>(); r2.Add(offset + vj[0] * scale); r2.Add(842 - offsety - vj[2] * scale);
                                        var r3 = new List<double>(); r3.Add(offset + vk[0] * scale); r3.Add(842 - offsety - vk[2] * scale);
                                        var r4 = new List<double>(); r4.Add(offset + vl[0] * scale); r4.Add(842 - offsety - vl[2] * scale);
                                        var rp1 = new List<double>(); rp1.Add(r1[0] + (r3[0] - r1[0]) * pinwidth); rp1.Add(r1[1] + (r3[1] - r1[1]) * pinwidth);
                                        var rp2 = new List<double>(); rp2.Add(r2[0] + (r4[0] - r2[0]) * pinwidth); rp2.Add(r2[1] + (r4[1] - r2[1]) * pinwidth);
                                        var rp3 = new List<double>(); rp3.Add(r3[0] + (r1[0] - r3[0]) * pinwidth); rp3.Add(r3[1] + (r1[1] - r3[1]) * pinwidth);
                                        var rp4 = new List<double>(); rp4.Add(r4[0] + (r2[0] - r4[0]) * pinwidth); rp4.Add(r4[1] + (r2[1] - r4[1]) * pinwidth);
                                        var rc = new List<double> { (r1[0] + r2[0] + r3[0] + r4[0]) / 4.0, (r1[1] + r2[1] + r3[1] + r4[1]) / 4.0 };
                                        gfx.DrawLine(pengray, r1[0], r1[1], r3[0], r3[1]); gfx.DrawLine(pengray, r2[0], r2[1], r4[0], r4[1]);//線材置換トラスの描画
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp1[0] - js / 2.0, rp1[1] - js / 2.0, js, js);//ピン記号
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp2[0] - js / 2.0, rp2[1] - js / 2.0, js, js);//ピン記号
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp3[0] - js / 2.0, rp3[1] - js / 2.0, js, js);//ピン記号
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp4[0] - js / 2.0, rp4[1] - js / 2.0, js, js);//ピン記号
                                        if (ii != 0)//長期荷重時は壁のせん断力は描画しない
                                        {
                                            gfx.DrawString(shear_w_new[e].ToString().Substring(0, Math.Min(shear_w_new[e].ToString().Length, 4)), font, XBrushes.Black, rc[0], rc[1], XStringFormats.BottomCenter);//壁せん断力
                                            var val = shear_w_new[e] * qscale * 100 / 2.0;
                                            var color = RGB((1 - val / Math.Max(1e-10, Qwmax)) * 1.9 / 3.0, 1, 0.5);
                                            var pens = new XPen(color, lw*0.5);
                                            gfx.DrawLine(pens, rc[0] - val, rc[1], rc[0] + val, rc[1]);//せん断力矢印線の描画
                                            gfx.DrawLine(pens, rc[0] - val, rc[1], rc[0] - val + fontsize, rc[1] + fontsize);//せん断力矢印線のarrowの描画
                                            gfx.DrawLine(pens, rc[0] + val, rc[1], rc[0] + val - fontsize, rc[1] - fontsize);//せん断力矢印線のarrowの描画
                                        }
                                    }
                                }
                            }
                            var pen2 = new XPen(XColors.Black, lw*0.25);
                            for (int e = 0; e < ij_new.Count; e++)//紙面に平行に回転後の骨組
                            {
                                int ni = (int)ij_new[e][1]; int nj = (int)ij_new[e][2]; int nel = (int)ij_new[e][0];
                                int mat = (int)ij_new[e][3]; int sec = (int)ij_new[e][4]; int angle = 0;//(int)ij_new[e][5];
                                var ri = new Vector3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value);
                                var rj = new Vector3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value);
                                var vi = rotation(ri, zvec, theta) - r0;
                                var vj = rotation(rj, zvec, theta) - r0;
                                var r1 = new List<double>(); r1.Add(offset + vi[0] * scale); r1.Add(842 - offsety - vi[2] * scale);
                                var r2 = new List<double>(); r2.Add(offset + vj[0] * scale); r2.Add(842 - offsety - vj[2] * scale);
                                r_ij.Add(new List<List<double>> { r1, r2 });
                                gfx.DrawLine(pen, r1[0], r1[1], r2[0], r2[1]);//骨組の描画
                                gfx.DrawEllipse(XBrushes.Black, r1[0] - ps / 2.0, r1[1] - ps / 2.0, ps, ps);//i節点の描画
                                gfx.DrawEllipse(XBrushes.Black, r2[0] - ps / 2.0, r2[1] - ps / 2.0, ps, ps);//j節点の描画

                                var element = new Line(new Point3d(ri), new Point3d(rj));
                                var rc = (ri + rj) / 2.0;
                                ///Qy
                                var l_ve = rotation(l_vec[nel], rj - ri, angle + 90);//Qyの方向
                                var Qyi = sec_f_new[e][1]; var Qyj = sec_f_new[e][7]; var Qyc = sec_f_new[e][13];
                                var p1 = ri - l_ve * Qyi * nscale; var p2 = rc - l_ve * Qyc * nscale; var p3 = rj + l_ve * Qyj * nscale;
                                var curve = NurbsCurve.CreateInterpolatedCurve(new Point3d[] { new Point3d(p1), new Point3d(p2), new Point3d(p3) }, 3);
                                curve.DivideByCount(div, true, out Point3d[] Qy);
                                var Qy_2D = new List<Vector2d>(); var Qy0_2D = new List<Vector2d>();
                                for (int i = 0; i < Qy.Length; i++) { var v = rotation(new Vector3d(Qy[i].X, Qy[i].Y, Qy[i].Z), zvec, theta) - r0; var x = offset + v[0] * scale; var y = 842 - offsety - v[2] * scale; Qy_2D.Add(new Vector2d(x, y)); }//紙面に平行に回転
                                for (int i = 0; i < Qy_2D.Count - 1; i++) { gfx.DrawLine(pen2, Qy_2D[i].X, Qy_2D[i].Y, Qy_2D[i + 1].X, Qy_2D[i + 1].Y); }//せん断力の描画(曲線)

                                var Qy0 = new Point3d[Qy.Length];//せん断力描画点から要素への法線の足の座標
                                for (int i = 0; i < Qy.Length; i++) { Qy0[i] = element.ClosestPoint(Qy[i], true); }
                                for (int i = 0; i < Qy0.Length; i++) { var v = rotation(new Vector3d(Qy0[i].X, Qy0[i].Y, Qy0[i].Z), zvec, theta) - r0; var x = offset + v[0] * scale; var y = 842 - offsety - v[2] * scale; Qy0_2D.Add(new Vector2d(x, y)); }//紙面に平行に回転
                                Qyi = Math.Round(Qyi, 3); Qyc = Math.Round(Qyc, 3); Qyj = Math.Round(Qyj, 3);
                                for (int i = 0; i < Qy_2D.Count; i++)//せん断力の描画(カラー分布)
                                {
                                    var v = Qy[i] - Qy0[i]; var val = v.Length;
                                    var color = RGB((1 - val / Math.Max(1e-10, Qmax)) * 1.9 / 3.0, 1, 0.5);
                                    var pens = new XPen(color, lw*0.25);
                                    gfx.DrawLine(pens, Qy_2D[i].X, Qy_2D[i].Y, Qy0_2D[i].X, Qy0_2D[i].Y);
                                    if (i == 0 && Math.Abs(Qyi) > 0.1)
                                    {
                                        gfx.DrawString(Qyi.ToString().Substring(0, Math.Min(Qyi.ToString().Length, 4)), font, XBrushes.Black, Qy_2D[i].X, Qy_2D[i].Y, XStringFormats.Center);
                                    }
                                    else if (i == (int)(Qy_2D.Count / 2) && Math.Abs(Qyc) > 0.1)
                                    {
                                        gfx.DrawString(Qyc.ToString().Substring(0, Math.Min(Qyc.ToString().Length, 4)), font, XBrushes.Black, Qy_2D[i].X, Qy_2D[i].Y, XStringFormats.Center);
                                    }
                                    else if (i == Qy_2D.Count - 1 && Math.Abs(Qyj) > 0.1)
                                    {
                                        gfx.DrawString(Qyj.ToString().Substring(0, Math.Min(Qyj.ToString().Length, 4)), font, XBrushes.Black, Qy_2D[i].X, Qy_2D[i].Y, XStringFormats.Center);
                                    }
                                }
                                ///Qz
                                var l_ve2 = rotation(l_vec[nel], rj - ri, angle);//Qzの方向
                                var Qzi = sec_f_new[e][2]; var Qzj = sec_f_new[e][8]; var Qzc = sec_f_new[e][14];
                                p1 = ri - l_ve2 * Qzi * nscale; p2 = rc - l_ve2 * Qzc * nscale; p3 = rj + l_ve2 * Qzj * nscale;
                                curve = NurbsCurve.CreateInterpolatedCurve(new Point3d[] { new Point3d(p1), new Point3d(p2), new Point3d(p3) }, 3);
                                curve.DivideByCount(div, true, out Point3d[] Qz);
                                var Qz_2D = new List<Vector2d>(); var Qz0_2D = new List<Vector2d>();
                                for (int i = 0; i < Qz.Length; i++) { var v = rotation(new Vector3d(Qz[i].X, Qz[i].Y, Qz[i].Z), zvec, theta) - r0; var x = offset + v[0] * scale; var y = 842 - offsety - v[2] * scale; Qz_2D.Add(new Vector2d(x, y)); }//紙面に平行に回転
                                for (int i = 0; i < Qz_2D.Count - 1; i++) { gfx.DrawLine(pen2, Qz_2D[i].X, Qz_2D[i].Y, Qz_2D[i + 1].X, Qz_2D[i + 1].Y); }//せん断力の描画(曲線)
                                var Qz0 = new Point3d[Qz.Length];//せん断力描画点から要素への法線の足の座標
                                for (int i = 0; i < Qz.Length; i++) { Qz0[i] = element.ClosestPoint(Qz[i], true); }
                                for (int i = 0; i < Qz0.Length; i++) { var v = rotation(new Vector3d(Qz0[i].X, Qz0[i].Y, Qz0[i].Z), zvec, theta) - r0; var x = offset + v[0] * scale; var y = 842 - offsety - v[2] * scale; Qz0_2D.Add(new Vector2d(x, y)); }//紙面に平行に回転
                                Qzi = Math.Round(Qzi, 3); Qzc = Math.Round(Qzc, 3); Qzj = Math.Round(Qzj, 3);
                                for (int i = 0; i < Qz_2D.Count; i++)//せん断力の描画(カラー分布)
                                {
                                    var v = Qz[i] - Qz0[i]; var val = v.Length;
                                    var color = RGB((1 - val / Math.Max(1e-10, Qmax)) * 1.9 / 3.0, 1, 0.5);
                                    var pens = new XPen(color, lw*0.25);
                                    gfx.DrawLine(pens, Qz_2D[i].X, Qz_2D[i].Y, Qz0_2D[i].X, Qz0_2D[i].Y);
                                    if (i == 0 && Math.Abs(Qzi)>0.1)
                                    {
                                        gfx.DrawString(Qzi.ToString().Substring(0, Math.Min(Qzi.ToString().Length, 4)), font, XBrushes.Black, Qz_2D[i].X, Qz_2D[i].Y, XStringFormats.Center);
                                    }
                                    else if (i == (int)(Qz_2D.Count / 2) && Math.Abs(Qzc) > 0.1)
                                    {
                                        gfx.DrawString(Qzc.ToString().Substring(0, Math.Min(Qzc.ToString().Length, 4)), font, XBrushes.Black, Qz_2D[i].X, Qz_2D[i].Y, XStringFormats.Center);
                                    }
                                    else if (i == Qz_2D.Count - 1 && Math.Abs(Qzj) > 0.1)
                                    {
                                        gfx.DrawString(Qzj.ToString().Substring(0, Math.Min(Qzj.ToString().Length, 4)), font, XBrushes.Black, Qz_2D[i].X, Qz_2D[i].Y, XStringFormats.Center);
                                    }
                                }
                                if (joint_No.Contains(nel) == true)//材端ピン
                                {
                                    int i = joint_No.IndexOf(nel);
                                    if (joint[i][1].Value == 0 || joint[i][1].Value == 2)
                                    {
                                        var rp1 = new List<double>(); rp1.Add(r1[0] + (r2[0] - r1[0]) * pinwidth); rp1.Add(r1[1] + (r2[1] - r1[1]) * pinwidth);
                                        gfx.DrawEllipse(pen, XBrushes.White, rp1[0] - js / 2.0, rp1[1] - js / 2.0, js, js);//ピン記号
                                    }
                                    if (joint[i][1].Value == 1 || joint[i][1].Value == 2)
                                    {
                                        var rp2 = new List<double>(); rp2.Add(r2[0] + (r1[0] - r2[0]) * pinwidth); rp2.Add(r2[1] + (r1[1] - r2[1]) * pinwidth);
                                        gfx.DrawEllipse(pen, XBrushes.White, rp2[0] - js / 2.0, rp2[1] - js / 2.0, js, js);//ピン記号
                                    }
                                }
                                if (B_No.Contains(ni) == true)//境界条件
                                {
                                    int i = B_No.IndexOf(ni);
                                    XPoint[] pts = new XPoint[3]; pts[0].X = r1[0]; pts[0].Y = r1[1]; pts[1].X = r1[0] - tri / 2.0; pts[1].Y = r1[1] + tri / 2.0 * Math.Sqrt(3); pts[2].X = r1[0] + tri / 2.0; pts[2].Y = r1[1] + tri / 2.0 * Math.Sqrt(3);
                                    var pc = new XPoint(); pc.X = (pts[1].X + pts[2].X) / 2.0; pc.Y = pts[1].Y;
                                    gfx.DrawPolygon(pen, pts);
                                    gfx.DrawString(((int)B[i][1].Value).ToString() + ((int)B[i][2].Value).ToString() + ((int)B[i][3].Value).ToString() + ((int)B[i][4].Value).ToString() + ((int)B[i][5].Value).ToString() + ((int)B[i][6].Value).ToString(), font, XBrushes.LightGray, pc.X, pc.Y, XStringFormat.TopCenter);
                                }
                                if (B_No.Contains(nj) == true)//境界条件
                                {
                                    int i = B_No.IndexOf(nj);
                                    XPoint[] pts = new XPoint[3]; pts[0].X = r2[0]; pts[0].Y = r2[1]; pts[1].X = r2[0] - tri / 2.0; pts[1].Y = r2[1] + tri / 2.0 * Math.Sqrt(3); pts[2].X = r2[0] + tri / 2.0; pts[2].Y = r2[1] + tri / 2.0 * Math.Sqrt(3);
                                    var pc = new XPoint(); pc.X = (pts[1].X + pts[2].X) / 2.0; pc.Y = pts[1].Y;
                                    gfx.DrawPolygon(pen, pts);
                                }
                            }
                            gfx.DrawString(wick[j] + "通りせん断力図" + casememo[ii], titlefont, XBrushes.Black, offset, 842 - offset, XStringFormats.BaseLineLeft);
                        }
                        filename = dir + "/" + pdfname + "_Q" + label[ii] + ".pdf";
                        document.Save(filename);// ドキュメントを保存。
                        Process.Start(filename);// ビューアを起動。
                        //曲げモーメント図//////////////////////////////////////////////////////////////////////////////////
                        document = new PdfDocument();
                        document.Info.Title = pdfname;
                        document.Info.Author = "Shinnosuke Fujita, Assoc. Prof., The Univ. of Kitakyushu";
                        for (int j = 0; j < wick.Count; j++)//1; j++)//
                        {
                            PdfPage page = new PdfPage(); page.Size = PageSize.A4;// 空白ページを作成。width x height = 594 x 842
                            page = document.AddPage();// 描画するためにXGraphicsオブジェクトを取得。
                            gfx = XGraphics.FromPdfPage(page);
                            var ij_new = new List<List<double>>();//その軸・通りの要素節点関係
                            var sec_f_new = new List<List<double>>();//その軸・通りの断面力
                            for (int e = 0; e < names.Count; e++)
                            {
                                var list = new List<double>(); var flist = new List<double>();
                                if (wicks[e].Contains(wick[j]) == true)
                                {
                                    list.Add(e);
                                    for (int i = 0; i < ij[e].Count; i++)
                                    {
                                        list.Add(ij[e][i].Value);
                                    }
                                    ij_new.Add(list);
                                    if (index.Contains(e) == true)
                                    {
                                        for (int i = 0; i < 18; i++)
                                        {
                                            flist.Add(sec_f[e][i + ii * 18].Value);
                                        }
                                    }
                                    else { for (int i = 0; i < 18; i++) { flist.Add(0.0); } }
                                    sec_f_new.Add(flist);
                                }
                            }
                            var kabe_w_new = new List<List<double>>();//その軸・通りの耐力壁
                            if (names2[0][0].Value != "" && shear_w[0] != -9999 && kabe_w[0][0].Value != -9999)
                            {
                                for (int e = 0; e < names2.Count; e++)
                                {
                                    var list = new List<double>();
                                    if (wicks2[e].Contains(wick[j]) == true)
                                    {
                                        list.Add(e);
                                        for (int i = 0; i < kabe_w[e].Count; i++)
                                        {
                                            list.Add(kabe_w[e][i].Value);
                                        }
                                        kabe_w_new.Add(list);
                                    }
                                }
                            }
                            var xmin = 9999.0; var xmax = -9999.0; var ymin = 9999.0; var ymax = -9999.0; var zmin = 9999.0; var zmax = -9999.0;
                            for (int e = 0; e < ij_new.Count; e++)
                            {
                                int ni = (int)ij_new[e][1]; int nj = (int)ij_new[e][2];
                                xmin = Math.Min(xmin, R[ni][0].Value); xmax = Math.Max(xmax, R[ni][0].Value); xmin = Math.Min(xmin, R[nj][0].Value); xmax = Math.Max(xmax, R[nj][0].Value);
                                ymin = Math.Min(ymin, R[ni][1].Value); ymax = Math.Max(ymax, R[ni][1].Value); ymin = Math.Min(ymin, R[nj][1].Value); ymax = Math.Max(ymax, R[nj][1].Value);
                                zmin = Math.Min(zmin, R[ni][2].Value); zmax = Math.Max(zmax, R[ni][2].Value); zmin = Math.Min(zmin, R[nj][2].Value); zmax = Math.Max(zmax, R[nj][2].Value);
                            }
                            var vec = new Vector3d(xmax - xmin, ymax - ymin, 0.0);
                            var cos = (xmax - xmin) / Math.Sqrt(Math.Pow(xmax - xmin, 2) + Math.Pow(ymax - ymin, 2));//(1,0,0)との角度
                            var theta = -Math.Acos(cos) / Math.PI * 180.0;
                            var r0 = new Vector3d(xmin, ymin, Zmin);//左下
                            var r_ij = new List<List<List<double>>>(); var zvec = new Vector3d(0, 0, 1);
                            r0 = rotation(r0, zvec, theta);//回転後の左下
                            if (names2[0][0].Value != "")
                            {
                                for (int e = 0; e < kabe_w_new.Count; e++)
                                {
                                    int ni = (int)kabe_w_new[e][1]; int nj = (int)kabe_w_new[e][2]; int nk = (int)kabe_w_new[e][3]; int nl = (int)kabe_w_new[e][4]; int nel = (int)kabe_w_new[e][0];
                                    var alpha = kabe_w_new[e][5];
                                    if (alpha != 0.0)
                                    {
                                        var vi = rotation(new Vector3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value), zvec, theta) - r0;
                                        var vj = rotation(new Vector3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value), zvec, theta) - r0;
                                        var vk = rotation(new Vector3d(R[nk][0].Value, R[nk][1].Value, R[nk][2].Value), zvec, theta) - r0;
                                        var vl = rotation(new Vector3d(R[nl][0].Value, R[nl][1].Value, R[nl][2].Value), zvec, theta) - r0;
                                        var r1 = new List<double>(); r1.Add(offset + vi[0] * scale); r1.Add(842 - offsety - vi[2] * scale);
                                        var r2 = new List<double>(); r2.Add(offset + vj[0] * scale); r2.Add(842 - offsety - vj[2] * scale);
                                        var r3 = new List<double>(); r3.Add(offset + vk[0] * scale); r3.Add(842 - offsety - vk[2] * scale);
                                        var r4 = new List<double>(); r4.Add(offset + vl[0] * scale); r4.Add(842 - offsety - vl[2] * scale);
                                        var rp1 = new List<double>(); rp1.Add(r1[0] + (r3[0] - r1[0]) * pinwidth); rp1.Add(r1[1] + (r3[1] - r1[1]) * pinwidth);
                                        var rp2 = new List<double>(); rp2.Add(r2[0] + (r4[0] - r2[0]) * pinwidth); rp2.Add(r2[1] + (r4[1] - r2[1]) * pinwidth);
                                        var rp3 = new List<double>(); rp3.Add(r3[0] + (r1[0] - r3[0]) * pinwidth); rp3.Add(r3[1] + (r1[1] - r3[1]) * pinwidth);
                                        var rp4 = new List<double>(); rp4.Add(r4[0] + (r2[0] - r4[0]) * pinwidth); rp4.Add(r4[1] + (r2[1] - r4[1]) * pinwidth);
                                        var rc = new List<double> { (r1[0] + r2[0] + r3[0] + r4[0]) / 4.0, (r1[1] + r2[1] + r3[1] + r4[1]) / 4.0 };
                                        gfx.DrawLine(pengray, r1[0], r1[1], r3[0], r3[1]); gfx.DrawLine(pengray, r2[0], r2[1], r4[0], r4[1]);//線材置換トラスの描画
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp1[0] - js / 2.0, rp1[1] - js / 2.0, js, js);//ピン記号
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp2[0] - js / 2.0, rp2[1] - js / 2.0, js, js);//ピン記号
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp3[0] - js / 2.0, rp3[1] - js / 2.0, js, js);//ピン記号
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp4[0] - js / 2.0, rp4[1] - js / 2.0, js, js);//ピン記号
                                    }
                                }
                            }
                            var pen2 = new XPen(XColors.Black, 0.25); //Rhino.RhinoApp.Write("width="+pen2.Width.ToString()+"\n");//
                            for (int e = 0; e < ij_new.Count; e++)//紙面に平行に回転後の骨組
                            {
                                int ni = (int)ij_new[e][1]; int nj = (int)ij_new[e][2]; int nel = (int)ij_new[e][0];
                                int mat = (int)ij_new[e][3]; int sec = (int)ij_new[e][4]; int angle = 0;//(int)ij_new[e][5];
                                var ri = new Vector3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value);
                                var rj = new Vector3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value);
                                var vi = rotation(ri, zvec, theta) - r0;
                                var vj = rotation(rj, zvec, theta) - r0;
                                var r1 = new List<double>(); r1.Add(offset + vi[0] * scale); r1.Add(842 - offsety - vi[2] * scale);
                                var r2 = new List<double>(); r2.Add(offset + vj[0] * scale); r2.Add(842 - offsety - vj[2] * scale);
                                r_ij.Add(new List<List<double>> { r1, r2 });
                                gfx.DrawLine(pen, r1[0], r1[1], r2[0], r2[1]);//骨組の描画
                                gfx.DrawEllipse(XBrushes.Black, r1[0] - ps / 2.0, r1[1] - ps / 2.0, ps, ps);//i節点の描画
                                gfx.DrawEllipse(XBrushes.Black, r2[0] - ps / 2.0, r2[1] - ps / 2.0, ps, ps);//j節点の描画

                                var element = new Line(new Point3d(ri), new Point3d(rj));
                                var rc = (ri + rj) / 2.0;
                                var l_ve = rotation(l_vec[nel], rj - ri, angle);
                                var Myi = sec_f_new[e][4]; var Myj = sec_f_new[e][10]; var Myc = sec_f_new[e][16];
                                var p1 = ri - l_ve * Myi * mscale; var p2 = rc - l_ve * Myc * mscale; var p3 = rj + l_ve * Myj * mscale;
                                var curve = NurbsCurve.CreateInterpolatedCurve(new Point3d[] { new Point3d(p1), new Point3d(p2), new Point3d(p3) }, 3);
                                curve.DivideByCount(div, true, out Point3d[] My);
                                var My_2D = new List<Vector2d>(); var My0_2D = new List<Vector2d>();
                                for (int i = 0; i < My.Length; i++) { var v = rotation(new Vector3d(My[i].X, My[i].Y, My[i].Z), zvec, theta) - r0; var x = offset + v[0] * scale; var y = 842 - offsety - v[2] * scale; My_2D.Add(new Vector2d(x, y)); }//紙面に平行に回転
                                for (int i = 0; i < My_2D.Count - 1; i++) { gfx.DrawLine(pen2, My_2D[i].X, My_2D[i].Y, My_2D[i + 1].X, My_2D[i + 1].Y); }//曲げモーメントの描画(曲線)

                                var My0 = new Point3d[My.Length];//曲げモーメント描画点から要素への法線の足の座標
                                for (int i = 0; i < My.Length; i++) { My0[i] = element.ClosestPoint(My[i], true); }
                                for (int i = 0; i < My0.Length; i++) { var v = rotation(new Vector3d(My0[i].X, My0[i].Y, My0[i].Z), zvec, theta) - r0; var x = offset + v[0] * scale; var y = 842 - offsety - v[2] * scale; My0_2D.Add(new Vector2d(x, y)); }//紙面に平行に回転
                                Myi = Math.Round(Math.Abs(Myi), 3); Myc = Math.Round(Math.Abs(Myc), 3); Myj = Math.Round(Math.Abs(Myj), 3);
                                for (int i = 0; i < My_2D.Count; i++)//曲げモーメントの描画(カラー分布)
                                {
                                    var v = My[i] - My0[i]; var val = v.Length;
                                    //if ((v / v.Length - l_ve).Length < 1e-5) { val = -val; }
                                    var color = RGB((1 - val / Math.Max(1e-10, Mymax)) * 1.9 / 3.0, 1, 0.5);
                                    var pens = new XPen(color, lw*0.25);
                                    gfx.DrawLine(pens, My_2D[i].X, My_2D[i].Y, My0_2D[i].X, My0_2D[i].Y);
                                    if (i == 0 && Math.Abs(Myi) > 0.1)
                                    {
                                        gfx.DrawString(Myi.ToString().Substring(0, Math.Min(Myi.ToString().Length, 4)), font, XBrushes.Black, My_2D[i].X, My_2D[i].Y, XStringFormats.Center);
                                    }
                                    else if (i == (int)(My_2D.Count / 2) && Math.Abs(Myc) > 0.1)
                                    {
                                        gfx.DrawString(Myc.ToString().Substring(0, Math.Min(Myc.ToString().Length, 4)), font, XBrushes.Black, My_2D[i].X, My_2D[i].Y, XStringFormats.Center);
                                    }
                                    else if (i == My_2D.Count - 1 && Math.Abs(Myj) > 0.1)
                                    {
                                        gfx.DrawString(Myj.ToString().Substring(0, Math.Min(Myj.ToString().Length, 4)), font, XBrushes.Black, My_2D[i].X, My_2D[i].Y, XStringFormats.Center);
                                    }
                                }
                                //var v_i = rotation(p1, zvec, theta); var Myi_2D = new Vector2d(offset + v_i[0] * scale, 842 - offsety - v_i[2] * scale);
                                //var v_c = rotation(p2, zvec, theta); var Myc_2D = new Vector2d(offset + v_c[0] * scale, 842 - offsety - v_c[2] * scale);
                                //var v_j = rotation(p3, zvec, theta); var Myj_2D = new Vector2d(offset + v_j[0] * scale, 842 - offsety - v_j[2] * scale);
                                //gfx.DrawString(Math.Abs(Myi).ToString().Substring(0, Math.Min(4, Math.Abs(Myi).ToString().Length)), font, XBrushes.Black, Myi_2D.X, Myi_2D.Y,XStringFormat.BottomCenter);
                                //gfx.DrawString(Math.Abs(Myc).ToString().Substring(0, Math.Min(4, Math.Abs(Myc).ToString().Length)), font, XBrushes.Black, Myc_2D.X, Myc_2D.Y, XStringFormat.BottomCenter);
                                //gfx.DrawString(Math.Abs(Myj).ToString().Substring(0, Math.Min(4, Math.Abs(Myj).ToString().Length)), font, XBrushes.Black, Myj_2D.X, Myj_2D.Y, XStringFormat.BottomCenter);


                                l_ve = rotation(l_vec[nel], rj - ri, angle + 90);
                                var Mzi = sec_f_new[e][5]; var Mzj = sec_f_new[e][11]; var Mzc = sec_f_new[e][17];
                                p1 = ri - l_ve * Mzi * mscale; p2 = rc - l_ve * Mzc * mscale; p3 = rj + l_ve * Mzj * mscale;
                                curve = NurbsCurve.CreateInterpolatedCurve(new Point3d[] { new Point3d(p1), new Point3d(p2), new Point3d(p3) }, 3);
                                curve.DivideByCount(div, true, out Point3d[] Mz);
                                var Mz_2D = new List<Vector2d>(); var Mz0_2D = new List<Vector2d>();
                                for (int i = 0; i < Mz.Length; i++) { var v = rotation(new Vector3d(Mz[i].X, Mz[i].Y, Mz[i].Z), zvec, theta) - r0; var x = offset + v[0] * scale; var y = 842 - offsety - v[2] * scale; Mz_2D.Add(new Vector2d(x, y)); }//紙面に平行に回転
                                for (int i = 0; i < Mz_2D.Count - 1; i++) { gfx.DrawLine(pen2, Mz_2D[i].X, Mz_2D[i].Y, Mz_2D[i + 1].X, Mz_2D[i + 1].Y); }//曲げモーメントの描画(曲線)

                                var Mz0 = new Point3d[Mz.Length];//曲げモーメント描画点から要素への法線の足の座標
                                for (int i = 0; i < Mz.Length; i++) { Mz0[i] = element.ClosestPoint(Mz[i], true); }
                                for (int i = 0; i < Mz0.Length; i++) { var v = rotation(new Vector3d(Mz0[i].X, Mz0[i].Y, Mz0[i].Z), zvec, theta) - r0; var x = offset + v[0] * scale; var y = 842 - offsety - v[2] * scale; Mz0_2D.Add(new Vector2d(x, y)); }//紙面に平行に回転
                                Mzi = Math.Round(Math.Abs(Mzi), 3); Mzc = Math.Round(Math.Abs(Mzc), 3); Mzj = Math.Round(Math.Abs(Mzj), 3);
                                for (int i = 0; i < Mz_2D.Count; i++)//曲げモーメントの描画(カラー分布)
                                {
                                    var v = Mz[i] - Mz0[i]; var val = v.Length;
                                    //if ((v / v.Length - l_ve).Length < 1e-5) { val = -val; }
                                    var color = RGB((1 - val / Math.Max(1e-10, Mzmax)) * 1.9 / 3.0, 1, 0.5);
                                    var pens = new XPen(color, lw*0.25);
                                    gfx.DrawLine(pens, Mz_2D[i].X, Mz_2D[i].Y, Mz0_2D[i].X, Mz0_2D[i].Y);
                                    if (i == 0 && Math.Abs(Mzi) > 0.1)
                                    {
                                        gfx.DrawString(Mzi.ToString().Substring(0, Math.Min(Mzi.ToString().Length, 4)), font, XBrushes.Black, Mz_2D[i].X, Mz_2D[i].Y, XStringFormats.Center);
                                    }
                                    else if (i == (int)(Mz_2D.Count / 2) && Math.Abs(Mzc) > 0.1)
                                    {
                                        gfx.DrawString(Mzc.ToString().Substring(0, Math.Min(Mzc.ToString().Length, 4)), font, XBrushes.Black, Mz_2D[i].X, Mz_2D[i].Y, XStringFormats.Center);
                                    }
                                    else if (i == Mz_2D.Count - 1 && Math.Abs(Mzj) > 0.1)
                                    {
                                        gfx.DrawString(Mzj.ToString().Substring(0, Math.Min(Mzj.ToString().Length, 4)), font, XBrushes.Black, Mz_2D[i].X, Mz_2D[i].Y, XStringFormats.Center);
                                    }
                                }

                                /*var My2 = new List<Point3d>(); var color3 = new ColorHSL(1.9 / 3.0, 1, 0.5); var element = new Line(new Point3d(ri), new Point3d(rj));
                                for (int i = 0; i < My1.Length; i++) { My2.Add(element.ClosestPoint(My1[i], true)); }
                                var My1_2D = new List<List<double>>(); var My2_2D = new List<List<double>>();
                                for (int i = 0; i < My1.Length; i++) { var v = rotation(new Vector3d(My1[i]), zvec, theta) - r0; var x = offset + v[0] * scale; var y = offset + v[1] * scale; My1_2D.Add(new List<double> { x, y }); }//紙面に平行に回転
                                for (int i = 0; i < My2.Count; i++) { var v = rotation(new Vector3d(My2[i]), zvec, theta) - r0; var x = offset + v[0] * scale; var y = offset + v[1] * scale; My2_2D.Add(new List<double> { x, y }); }//紙面に平行に回転
                                var vv = rotation(new Vector3d(l_ve), zvec, theta); var xx = offset + vv[0] * scale; var yy = offset + vv[1] * scale; var l_ve_2D=new Vector2d(xx,yy);
                                for (int i = 0; i < My1_2D.Count - 1; i++)
                                {
                                    var vec1 = new Vector2d( My1_2D[i][0] - My2_2D[i][0], My1_2D[i][1] - My2_2D[i][1]); var val1 = vec1.Length;
                                    if ((vec1 / vec1.Length - l_ve_2D).Length < 1e-5) { val1 = -val1; }
                                    var color1 = new XSolidBrush(RGB((1 - Math.Abs(val1) / Math.Max(1e-10, Mymax)) * 1.9 / 3.0, 1, 0.5));
                                    var vec2 = new Vector2d(My1_2D[i + 1][0] - My2_2D[i + 1][0], My1_2D[i + 1][1] - My2_2D[i + 1][1]); var val2 = vec2.Length;
                                    if ((vec2 / vec2.Length - l_ve_2D).Length < 1e-5) { val2 = -val2; }
                                    var color2 = new XSolidBrush(RGB((1 - Math.Abs(val2) / Math.Max(1e-10, Mymax)) * 1.9 / 3.0, 1, 0.5));
                                    var vertex=new XPoint[4]; vertex[0] = new XPoint(My1_2D[i][0], My1_2D[i][1]); vertex[1] = new XPoint(My2_2D[i][0], My2_2D[i][1]);
                                    vertex[2] = new XPoint(My2_2D[i + 1][0], My2_2D[i + 1][1]); vertex[3] = new XPoint(My1_2D[i + 1][0], My1_2D[i + 1][1]);
                                    //gfx.DrawPolygon()
                                }*/


                                if (joint_No.Contains(nel) == true)//材端ピン
                                {
                                    int i = joint_No.IndexOf(nel);
                                    if (joint[i][1].Value == 0 || joint[i][1].Value == 2)
                                    {
                                        var rp1 = new List<double>(); rp1.Add(r1[0] + (r2[0] - r1[0]) * pinwidth); rp1.Add(r1[1] + (r2[1] - r1[1]) * pinwidth);
                                        gfx.DrawEllipse(pen, XBrushes.White, rp1[0] - js / 2.0, rp1[1] - js / 2.0, js, js);//ピン記号
                                    }
                                    if (joint[i][1].Value == 1 || joint[i][1].Value == 2)
                                    {
                                        var rp2 = new List<double>(); rp2.Add(r2[0] + (r1[0] - r2[0]) * pinwidth); rp2.Add(r2[1] + (r1[1] - r2[1]) * pinwidth);
                                        gfx.DrawEllipse(pen, XBrushes.White, rp2[0] - js / 2.0, rp2[1] - js / 2.0, js, js);//ピン記号
                                    }
                                }
                                if (B_No.Contains(ni) == true)//境界条件
                                {
                                    int i = B_No.IndexOf(ni);
                                    XPoint[] pts = new XPoint[3]; pts[0].X = r1[0]; pts[0].Y = r1[1]; pts[1].X = r1[0] - tri / 2.0; pts[1].Y = r1[1] + tri / 2.0 * Math.Sqrt(3); pts[2].X = r1[0] + tri / 2.0; pts[2].Y = r1[1] + tri / 2.0 * Math.Sqrt(3);
                                    var pc = new XPoint(); pc.X = (pts[1].X + pts[2].X) / 2.0; pc.Y = pts[1].Y;
                                    gfx.DrawPolygon(pen, pts);
                                    gfx.DrawString(((int)B[i][1].Value).ToString() + ((int)B[i][2].Value).ToString() + ((int)B[i][3].Value).ToString() + ((int)B[i][4].Value).ToString() + ((int)B[i][5].Value).ToString() + ((int)B[i][6].Value).ToString(), font, XBrushes.LightGray, pc.X, pc.Y, XStringFormat.TopCenter);
                                }
                                if (B_No.Contains(nj) == true)//境界条件
                                {
                                    int i = B_No.IndexOf(nj);
                                    XPoint[] pts = new XPoint[3]; pts[0].X = r2[0]; pts[0].Y = r2[1]; pts[1].X = r2[0] - tri / 2.0; pts[1].Y = r2[1] + tri / 2.0 * Math.Sqrt(3); pts[2].X = r2[0] + tri / 2.0; pts[2].Y = r2[1] + tri / 2.0 * Math.Sqrt(3);
                                    var pc = new XPoint(); pc.X = (pts[1].X + pts[2].X) / 2.0; pc.Y = pts[1].Y;
                                    gfx.DrawPolygon(pen, pts);
                                }
                            }
                            gfx.DrawString(wick[j] + "通り曲げモーメント図" + casememo[ii], titlefont, XBrushes.Black, offset, 842 - offset, XStringFormats.BaseLineLeft);
                        }
                        filename = dir + "/" + pdfname + "_M" + label[ii] + ".pdf";
                        document.Save(filename);// ドキュメントを保存。
                        Process.Start(filename);// ビューアを起動。
                        //軸力図///////////////////////////////////////////////////////////////////////////////////////////
                        document = new PdfDocument();
                        document.Info.Title = pdfname;
                        document.Info.Author = "Shinnosuke Fujita, Assoc. Prof., The Univ. of Kitakyushu";
                        for (int j = 0; j < wick.Count; j++)//1; j++)//
                        {
                            PdfPage page = new PdfPage(); page.Size = PageSize.A4;// 空白ページを作成。width x height = 594 x 842
                            page = document.AddPage();// 描画するためにXGraphicsオブジェクトを取得。
                            gfx = XGraphics.FromPdfPage(page);
                            var ij_new = new List<List<double>>();//その軸・通りの要素節点関係
                            var sec_f_new = new List<List<double>>();//その軸・通りの断面力
                            for (int e = 0; e < names.Count; e++)
                            {
                                var list = new List<double>(); var flist = new List<double>();
                                if (wicks[e].Contains(wick[j]) == true)
                                {
                                    list.Add(e);
                                    for (int i = 0; i < ij[e].Count; i++)
                                    {
                                        list.Add(ij[e][i].Value);
                                    }
                                    ij_new.Add(list);
                                    if (index.Contains(e) == true)
                                    {
                                        for (int i = 0; i < 18; i++)
                                        {
                                            flist.Add(sec_f[e][i + ii * 18].Value);
                                        }
                                    }
                                    else { for (int i = 0; i < 18; i++) { flist.Add(0.0); } }
                                    sec_f_new.Add(flist);
                                }
                            }
                            var kabe_w_new = new List<List<double>>();//その軸・通りの耐力壁
                            if (names2[0][0].Value != "" && shear_w[0] != -9999 && kabe_w[0][0].Value != -9999)
                            {
                                for (int e = 0; e < names2.Count; e++)
                                {
                                    var list = new List<double>();
                                    if (wicks2[e].Contains(wick[j]) == true)
                                    {
                                        list.Add(e);
                                        for (int i = 0; i < kabe_w[e].Count; i++)
                                        {
                                            list.Add(kabe_w[e][i].Value);
                                        }
                                        kabe_w_new.Add(list);
                                    }
                                }
                            }
                            var xmin = 9999.0; var xmax = -9999.0; var ymin = 9999.0; var ymax = -9999.0; var zmin = 9999.0; var zmax = -9999.0;
                            for (int e = 0; e < ij_new.Count; e++)
                            {
                                int ni = (int)ij_new[e][1]; int nj = (int)ij_new[e][2];
                                xmin = Math.Min(xmin, R[ni][0].Value); xmax = Math.Max(xmax, R[ni][0].Value); xmin = Math.Min(xmin, R[nj][0].Value); xmax = Math.Max(xmax, R[nj][0].Value);
                                ymin = Math.Min(ymin, R[ni][1].Value); ymax = Math.Max(ymax, R[ni][1].Value); ymin = Math.Min(ymin, R[nj][1].Value); ymax = Math.Max(ymax, R[nj][1].Value);
                                zmin = Math.Min(zmin, R[ni][2].Value); zmax = Math.Max(zmax, R[ni][2].Value); zmin = Math.Min(zmin, R[nj][2].Value); zmax = Math.Max(zmax, R[nj][2].Value);
                            }
                            var vec = new Vector3d(xmax - xmin, ymax - ymin, 0.0);
                            var cos = (xmax - xmin) / Math.Sqrt(Math.Pow(xmax - xmin, 2) + Math.Pow(ymax - ymin, 2));//(1,0,0)との角度
                            var theta = -Math.Acos(cos) / Math.PI * 180.0;
                            var r0 = new Vector3d(xmin, ymin, Zmin);//左下
                            var r_ij = new List<List<List<double>>>(); var zvec = new Vector3d(0, 0, 1);
                            r0 = rotation(r0, zvec, theta);//回転後の左下
                            if (names2[0][0].Value != "")
                            {
                                for (int e = 0; e < kabe_w_new.Count; e++)
                                {
                                    int ni = (int)kabe_w_new[e][1]; int nj = (int)kabe_w_new[e][2]; int nk = (int)kabe_w_new[e][3]; int nl = (int)kabe_w_new[e][4]; int nel = (int)kabe_w_new[e][0];
                                    var alpha = kabe_w_new[e][5];
                                    if (alpha != 0.0)
                                    {
                                        var vi = rotation(new Vector3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value), zvec, theta) - r0;
                                        var vj = rotation(new Vector3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value), zvec, theta) - r0;
                                        var vk = rotation(new Vector3d(R[nk][0].Value, R[nk][1].Value, R[nk][2].Value), zvec, theta) - r0;
                                        var vl = rotation(new Vector3d(R[nl][0].Value, R[nl][1].Value, R[nl][2].Value), zvec, theta) - r0;
                                        var r1 = new List<double>(); r1.Add(offset + vi[0] * scale); r1.Add(842 - offsety - vi[2] * scale);
                                        var r2 = new List<double>(); r2.Add(offset + vj[0] * scale); r2.Add(842 - offsety - vj[2] * scale);
                                        var r3 = new List<double>(); r3.Add(offset + vk[0] * scale); r3.Add(842 - offsety - vk[2] * scale);
                                        var r4 = new List<double>(); r4.Add(offset + vl[0] * scale); r4.Add(842 - offsety - vl[2] * scale);
                                        var rp1 = new List<double>(); rp1.Add(r1[0] + (r3[0] - r1[0]) * pinwidth); rp1.Add(r1[1] + (r3[1] - r1[1]) * pinwidth);
                                        var rp2 = new List<double>(); rp2.Add(r2[0] + (r4[0] - r2[0]) * pinwidth); rp2.Add(r2[1] + (r4[1] - r2[1]) * pinwidth);
                                        var rp3 = new List<double>(); rp3.Add(r3[0] + (r1[0] - r3[0]) * pinwidth); rp3.Add(r3[1] + (r1[1] - r3[1]) * pinwidth);
                                        var rp4 = new List<double>(); rp4.Add(r4[0] + (r2[0] - r4[0]) * pinwidth); rp4.Add(r4[1] + (r2[1] - r4[1]) * pinwidth);
                                        var rc = new List<double> { (r1[0] + r2[0] + r3[0] + r4[0]) / 4.0, (r1[1] + r2[1] + r3[1] + r4[1]) / 4.0 };
                                        gfx.DrawLine(pengray, r1[0], r1[1], r3[0], r3[1]); gfx.DrawLine(pengray, r2[0], r2[1], r4[0], r4[1]);//線材置換トラスの描画
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp1[0] - js / 2.0, rp1[1] - js / 2.0, js, js);//ピン記号
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp2[0] - js / 2.0, rp2[1] - js / 2.0, js, js);//ピン記号
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp3[0] - js / 2.0, rp3[1] - js / 2.0, js, js);//ピン記号
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp4[0] - js / 2.0, rp4[1] - js / 2.0, js, js);//ピン記号
                                    }
                                }
                            }
                            for (int e = 0; e < ij_new.Count; e++)//紙面に平行に回転後の骨組
                            {
                                int ni = (int)ij_new[e][1]; int nj = (int)ij_new[e][2]; int nel = (int)ij_new[e][0];
                                int mat = (int)ij_new[e][3]; int sec = (int)ij_new[e][4]; int angle = 0;//(int)ij_new[e][5];
                                var ri = new Vector3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value);
                                var rj = new Vector3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value);
                                var vi = rotation(ri, zvec, theta) - r0;
                                var vj = rotation(rj, zvec, theta) - r0;
                                var r1 = new List<double>(); r1.Add(offset + vi[0] * scale); r1.Add(842 - offsety - vi[2] * scale);
                                var r2 = new List<double>(); r2.Add(offset + vj[0] * scale); r2.Add(842 - offsety - vj[2] * scale);
                                r_ij.Add(new List<List<double>> { r1, r2 });
                                gfx.DrawLine(pen, r1[0], r1[1], r2[0], r2[1]);//骨組の描画
                                gfx.DrawEllipse(XBrushes.Black, r1[0] - ps / 2.0, r1[1] - ps / 2.0, ps, ps);//i節点の描画
                                gfx.DrawEllipse(XBrushes.Black, r2[0] - ps / 2.0, r2[1] - ps / 2.0, ps, ps);//j節点の描画

                                var element = new Line(new Point3d(ri), new Point3d(rj));
                                var rc = (ri + rj) / 2.0;
                                var l_ve = rotation(l_vec[nel], rj - ri, angle);
                                var Ni = sec_f_new[e][0] * nscale; var Nj = -sec_f_new[e][6] * nscale; var Nc = sec_f_new[e][12] * nscale;
                                var p1 = new Vector2d(r1[0], r1[1]); var p4 = new Vector2d(r2[0], r2[1]); var p2 = p1 + (p4 - p1) / 3.0; var p3 = p1 + (p4 - p1) / 3.0 * 2.0;
                                var c1 = RGB((1 - (Ni - Nmin) / Math.Max(1e-10, Nmax - Nmin)) * 1.9 / 3.0, 1, 0.5); var c2 = RGB((1 - (Nc - Nmin) / Math.Max(1e-10, Nmax - Nmin)) * 1.9 / 3.0, 1, 0.5); var c3 = RGB((1 - (Nj - Nmin) / Math.Max(1e-10, Nmax - Nmin)) * 1.9 / 3.0, 1, 0.5);
                                var pen1 = new XPen(c1, Math.Abs(Ni) / Nmax * 20); var pen2 = new XPen(c2, Math.Abs(Nc) / Nmax * 20); var pen3 = new XPen(c3, Math.Abs(Nj) / Nmax * 20);
                                gfx.DrawLine(pen1, p1.X, p1.Y, p2.X, p2.Y); gfx.DrawLine(pen2, p2.X, p2.Y, p3.X, p3.Y); gfx.DrawLine(pen3, p3.X, p3.Y, p4.X, p4.Y);//
                                Ni = Math.Round(sec_f_new[e][0], 3); Nc = Math.Round(sec_f_new[e][12], 3); Nj = Math.Round(-sec_f_new[e][6], 3);
                                if (Math.Abs(Ni) > 0.1) { gfx.DrawString(Ni.ToString().Substring(0, Math.Min(Ni.ToString().Length, 4)), font, XBrushes.Black, (p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0, XStringFormats.Center); }//i端軸力数値
                                if (Math.Abs(Nc) > 0.1) { gfx.DrawString(Nc.ToString().Substring(0, Math.Min(Nc.ToString().Length, 4)), font, XBrushes.Black, (p2.X + p3.X) / 2.0, (p2.Y + p3.Y) / 2.0, XStringFormats.Center); }//中央軸力数値
                                if (Math.Abs(Nj) > 0.1) { gfx.DrawString(Nj.ToString().Substring(0, Math.Min(Nj.ToString().Length, 4)), font, XBrushes.Black, (p3.X + p4.X) / 2.0, (p3.Y + p4.Y) / 2.0, XStringFormats.Center); }//中央軸力数値
                                if (joint_No.Contains(nel) == true)//材端ピン
                                {
                                    int i = joint_No.IndexOf(nel);
                                    if (joint[i][1].Value == 0 || joint[i][1].Value == 2)
                                    {
                                        var rp1 = new List<double>(); rp1.Add(r1[0] + (r2[0] - r1[0]) * pinwidth); rp1.Add(r1[1] + (r2[1] - r1[1]) * pinwidth);
                                        gfx.DrawEllipse(pen, XBrushes.White, rp1[0] - js / 2.0, rp1[1] - js / 2.0, js, js);//ピン記号
                                    }
                                    if (joint[i][1].Value == 1 || joint[i][1].Value == 2)
                                    {
                                        var rp2 = new List<double>(); rp2.Add(r2[0] + (r1[0] - r2[0]) * pinwidth); rp2.Add(r2[1] + (r1[1] - r2[1]) * pinwidth);
                                        gfx.DrawEllipse(pen, XBrushes.White, rp2[0] - js / 2.0, rp2[1] - js / 2.0, js, js);//ピン記号
                                    }
                                }
                                if (B_No.Contains(ni) == true)//境界条件
                                {
                                    int i = B_No.IndexOf(ni);
                                    XPoint[] pts = new XPoint[3]; pts[0].X = r1[0]; pts[0].Y = r1[1]; pts[1].X = r1[0] - tri / 2.0; pts[1].Y = r1[1] + tri / 2.0 * Math.Sqrt(3); pts[2].X = r1[0] + tri / 2.0; pts[2].Y = r1[1] + tri / 2.0 * Math.Sqrt(3);
                                    var pc = new XPoint(); pc.X = (pts[1].X + pts[2].X) / 2.0; pc.Y = pts[1].Y;
                                    gfx.DrawPolygon(pen, pts);
                                    gfx.DrawString(((int)B[i][1].Value).ToString() + ((int)B[i][2].Value).ToString() + ((int)B[i][3].Value).ToString() + ((int)B[i][4].Value).ToString() + ((int)B[i][5].Value).ToString() + ((int)B[i][6].Value).ToString(), font, XBrushes.LightGray, pc.X, pc.Y, XStringFormat.TopCenter);
                                }
                                if (B_No.Contains(nj) == true)//境界条件
                                {
                                    int i = B_No.IndexOf(nj);
                                    XPoint[] pts = new XPoint[3]; pts[0].X = r2[0]; pts[0].Y = r2[1]; pts[1].X = r2[0] - tri / 2.0; pts[1].Y = r2[1] + tri / 2.0 * Math.Sqrt(3); pts[2].X = r2[0] + tri / 2.0; pts[2].Y = r2[1] + tri / 2.0 * Math.Sqrt(3);
                                    var pc = new XPoint(); pc.X = (pts[1].X + pts[2].X) / 2.0; pc.Y = pts[1].Y;
                                    gfx.DrawPolygon(pen, pts);
                                }
                            }
                            gfx.DrawString(wick[j] + "通り軸力図" + casememo[ii], titlefont, XBrushes.Black, offset, 842 - offset, XStringFormats.BaseLineLeft);
                        }
                        filename = dir + "/" + pdfname + "_N" + label[ii] + ".pdf";
                        document.Save(filename);// ドキュメントを保存。
                        Process.Start(filename);// ビューアを起動。
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
                return OpenSeesUtility.Properties.Resources.VisPdf;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("57ad3fa2-a873-4eff-a3fe-b253af650c5f"); }
        }///ここからGUIの作成*****************************************************************************************
        public class CustomGUI : GH_ComponentAttributes
        {
            public CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle radio_rec1;
            private Rectangle radio_rec1_1; private Rectangle text_rec1_1;
            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int height = 17; int radi1 = 7; int radi2 = 4;
                int pitchx = 8; int textheight = 20;
                int width = global_rec.Width;

                radio_rec1 = global_rec; radio_rec1.Y = radio_rec1.Bottom;
                radio_rec1.Height = height;
                global_rec.Height += height;

                radio_rec1_1 = radio_rec1;
                radio_rec1_1.X += 5; radio_rec1_1.Y += 5;
                radio_rec1_1.Height = radi1; radio_rec1_1.Width = radi1;

                text_rec1_1 = radio_rec1_1;
                text_rec1_1.X += pitchx; text_rec1_1.Y -= radi2;
                text_rec1_1.Height = textheight; text_rec1_1.Width = width;

                Bounds = global_rec;
            }
            Brush c1 = Brushes.White;
            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
            {
                base.Render(canvas, graphics, channel);
                if (channel == GH_CanvasChannel.Objects)
                {
                    GH_Capsule radio1 = GH_Capsule.CreateCapsule(radio_rec1, GH_Palette.White, 2, 0);
                    radio1.Render(graphics, Selected, Owner.Locked, false); radio1.Dispose();

                    GH_Capsule radio1_1 = GH_Capsule.CreateCapsule(radio_rec1_1, GH_Palette.Black, 5, 5);
                    radio1_1.Render(graphics, Selected, Owner.Locked, false); radio1_1.Dispose();
                    graphics.FillEllipse(c1, radio_rec1_1);
                    graphics.DrawString("PDF OUTPUT", GH_FontServer.Standard, Brushes.Black, text_rec1_1);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec1 = radio_rec1_1;
                    if (rec1.Contains(e.CanvasLocation))
                    {
                        if (c1 == Brushes.White) { c1 = Brushes.Black; SetButton("1", 1); }
                        else { c1 = Brushes.White; SetButton("1", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}