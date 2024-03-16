using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

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

namespace OpenSeesUtility
{
    public class SteelCheckPDF : GH_Component
    {
        public static int on_off = 0;
        public static PdfCreate.JapaneseFontResolver fontresolver = new PdfCreate.JapaneseFontResolver();
        public static XGraphics gfx;
        public static void SetButton(string s, int i)
        {
            if (s == "1")
            {
                on_off = i;
            }
        }
        public SteelCheckPDF()
          : base("SteelCheck PDF Output", "SteelCheck PDF Output",
              "Output SteelCheck result to pdf",
              "OpenSees", "Utility")
        {
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("index", "index", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("lambda", "lambda", "elongation ratio[...](DataList)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("section area", "A", "[...](DataList)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("Section modulus around y-axis", "Zy", "[...](DataList)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("Section modulus around z-axis", "Zz", "[...](DataList)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("fc", "fc", "[...](DataList)[N/mm2]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("ft", "ft", "[...](DataList)[N/mm2]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("fby", "fby", "[...](DataList)[N/mm2]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("fbz", "fbz", "[...](DataList)[N/mm2]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("fs", "fs", "[...](DataList)[N/mm2]", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("sectional_force", "sec_f", "[[Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("kentei_hi", "kentei", "[[for Ni,for Qyi,for Qzi,for Myi,for Mzi,for Nj,for Qyj,for Qzj,for Myj,for Mzj,for Nc,for Qyc,for Qzc,for Myc,for Mzc],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddTextParameter("secname", "secname", "section name", GH_ParamAccess.list, "");///
            pManager.AddTextParameter("outputname", "outputname", "output file name", GH_ParamAccess.item, "SteelCheck");///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("message", "message", "output message", GH_ParamAccess.item);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (on_off == 1)
            {
                var index = new List<double>(); DA.GetDataList("index", index);
                DA.GetDataTree("element_node_relationship", out GH_Structure<GH_Number> _ij); var ij = _ij.Branches;
                DA.GetDataTree("sectional_force", out GH_Structure<GH_Number> _sec_f);
                DA.GetDataTree("fc", out GH_Structure<GH_Number> _fc); DA.GetDataTree("ft", out GH_Structure<GH_Number> _ft); DA.GetDataTree("fby", out GH_Structure<GH_Number> _fby); DA.GetDataTree("fbz", out GH_Structure<GH_Number> _fbz); DA.GetDataTree("fs", out GH_Structure<GH_Number> _fs);
                List<double> Lambda = new List<double>(); DA.GetDataList("lambda", Lambda);
                List<double> A = new List<double>(); DA.GetDataList("section area", A);
                List<double> Zy = new List<double>(); DA.GetDataList("Section modulus around y-axis", Zy);
                List<double> Zz = new List<double>(); DA.GetDataList("Section modulus around z-axis", Zz);
                var fc = _fc.Branches[0]; var ft = _ft.Branches[0]; var fby = _fby.Branches[0]; var fbz = _fbz.Branches[0]; var fs = _fs.Branches[0];
                var fc2 = _fc.Branches[0]; var ft2 = _ft.Branches[0]; var fby2 = _fby.Branches[0]; var fbz2 = _fbz.Branches[0]; var fs2 = _fs.Branches[0];
                if (_sec_f.Branches.Count / ij.Count == 5)
                {
                    fc2 = _fc.Branches[1]; ft2 = _ft.Branches[1]; fby2 = _fby.Branches[1]; fbz2 = _fbz.Branches[1]; fs2 = _fs.Branches[1];
                }
                DA.GetDataTree("kentei_hi", out GH_Structure<GH_Number> _kentei); List<string> secname = new List<string>(); DA.GetDataList("secname", secname); var pdfname = "TimberCheck"; DA.GetData("outputname", ref pdfname);
                int Digit(int num)//数字の桁数を求める関数
                {
                    // Mathf.Log10(0)はNegativeInfinityを返すため、別途処理する。
                    return (num == 0) ? 1 : ((int)Math.Log10(num) + 1);
                }
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
                    "部材番号","部材断面","λ","A[cm2]","Zy[cm3]","Zz[cm3]","","fc[N/mm2]", "ft[N/mm2]", "fby[N/mm2]", "fbz[N/mm2]", "fs[N/mm2]","", "節点番号","", "N[kN]","My[kNm]", "Mz[kNm]","Qy[kN]","Qz[kN]","軸+曲げ検定比","せん断検定比","判定",
                    "", "N[kN]","My[kNm]", "Mz[kNm]","Qy[kN]","Qz[kN]","軸+曲げ検定比","せん断検定比","判定",
                    "", "N[kN]","My[kNm]", "Mz[kNm]","Qy[kN]","Qz[kN]","軸+曲げ検定比","せん断検定比","判定",
                    "", "N[kN]","My[kNm]", "Mz[kNm]","Qy[kN]","Qz[kN]","軸+曲げ検定比","せん断検定比","判定",
                    "", "N[kN]","My[kNm]", "Mz[kNm]","Qy[kN]","Qz[kN]","軸+曲げ検定比","せん断検定比","判定"
                };
                var label_width = 75; var offset_x = 25; var offset_y = 25; var pitchy = 13; var text_width = 25; PdfPage page = new PdfPage(); page.Size = PageSize.A4;
                for (int e = 0; e < index.Count; e++)//
                {
                    int ni = (int)ij[e][0].Value; int nj = (int)ij[e][1].Value; int sec = (int)ij[e][3].Value;
                    var ele_text = ((int)index[e]).ToString(); var ni_text = ni.ToString(); var nj_text = nj.ToString();
                    var name_text = secname[sec]; var lambda_text = Lambda[e].ToString("F").Substring(0, Digit((int)Lambda[e]));
                    var A_text = Math.Round(A[sec] * 1e+4,2).ToString("F").Substring(0, Math.Min(5,(Math.Round(A[sec] * 1e+4, 2).ToString("F")).Length));
                    var Zy_text = Math.Round(Zy[sec] * 1e+6, 2).ToString("F"); Zy_text = Zy_text.Substring(0, Math.Min(5, Zy_text.Length));
                    var Zz_text = Math.Round(Zz[sec] * 1e+6,2).ToString("F"); Zz_text = Zz_text.Substring(0, Math.Min(5, Zz_text.Length));
                    var fc_text = fc[e].Value.ToString("F"); fc_text = fc_text.Substring(0, Math.Min(5, fc_text.Length)); var fc2_text = "";
                    var ft_text = ft[e].Value.ToString("F"); ft_text = ft_text.Substring(0, Math.Min(5, ft_text.Length)); var ft2_text = "";
                    var fby_text = fby[e].Value.ToString("F"); fby_text = fby_text.Substring(0, Math.Min(5, fby_text.Length)); var fby2_text = "";
                    var fbz_text = fbz[e].Value.ToString("F"); fbz_text = fbz_text.Substring(0, Math.Min(5, fbz_text.Length)); var fbz2_text = "";
                    var fs_text = fs[e].Value.ToString("F"); fs_text = fs_text.Substring(0, Math.Min(5, fs_text.Length)); var fs2_text = "";
                    if (_fc.Branches.Count == 2) { fc2_text = fc2[e].Value.ToString("F"); fc2_text = fc2_text.Substring(0, Math.Min(5, fc2_text.Length)); }
                    if (_ft.Branches.Count == 2) { ft2_text = ft2[e].Value.ToString("F"); ft2_text = ft2_text.Substring(0, Math.Min(5, ft2_text.Length)); }
                    if (_fby.Branches.Count == 2) { fby2_text = fby2[e].Value.ToString("F"); fby2_text = fby2_text.Substring(0, Math.Min(5, fby2_text.Length)); }
                    if (_fbz.Branches.Count == 2) { fbz2_text = fbz2[e].Value.ToString("F"); fbz2_text = fbz2_text.Substring(0, Math.Min(5, fbz2_text.Length)); }
                    if (_fs.Branches.Count == 2) { fs2_text = fs2[e].Value.ToString("F"); fs2_text = fs2_text.Substring(0, Math.Min(5, fs2_text.Length)); }
                    var sec_f = _sec_f.get_Branch(new GH_Path(new int[] { 0, e }));
                    var Ni_text = double.Parse(sec_f[0].ToString()).ToString("F"); Ni_text = Ni_text.Substring(0, Math.Min(4, Ni_text.Length));
                    var Qyi_text = Math.Abs(double.Parse(sec_f[1].ToString())).ToString("F"); Qyi_text = Qyi_text.Substring(0, Math.Min(4, Qyi_text.Length));
                    var Qzi_text = Math.Abs(double.Parse(sec_f[2].ToString())).ToString("F"); Qzi_text = Qzi_text.Substring(0, Math.Min(4, Qzi_text.Length));
                    var Myi_text = Math.Abs(double.Parse(sec_f[4].ToString())).ToString("F"); Myi_text = Myi_text.Substring(0, Math.Min(4, Myi_text.Length));
                    var Mzi_text = Math.Abs(double.Parse(sec_f[5].ToString())).ToString("F"); Mzi_text = Mzi_text.Substring(0, Math.Min(4, Mzi_text.Length));
                    var Nj_text = (-double.Parse(sec_f[6].ToString())).ToString("F"); Nj_text = Nj_text.Substring(0, Math.Min(4, Nj_text.Length));
                    var Qyj_text = Math.Abs(double.Parse(sec_f[7].ToString())).ToString("F"); Qyj_text = Qyj_text.Substring(0, Math.Min(4, Qyj_text.Length));
                    var Qzj_text = Math.Abs(double.Parse(sec_f[8].ToString())).ToString("F"); Qzj_text = Qzj_text.Substring(0, Math.Min(4, Qzj_text.Length));
                    var Myj_text = Math.Abs(double.Parse(sec_f[10].ToString())).ToString("F"); Myj_text = Myj_text.Substring(0, Math.Min(4, Myj_text.Length));
                    var Mzj_text = Math.Abs(double.Parse(sec_f[11].ToString())).ToString("F"); Mzj_text = Mzj_text.Substring(0, Math.Min(4, Mzj_text.Length));
                    var Nc_text = (double.Parse(sec_f[12].ToString())).ToString("F"); Nc_text = Nc_text.Substring(0, Math.Min(4, Nc_text.Length));
                    var Qyc_text = Math.Abs(double.Parse(sec_f[13].ToString())).ToString("F"); Qyc_text = Qyc_text.Substring(0, Math.Min(4, Qyc_text.Length));
                    var Qzc_text = Math.Abs(double.Parse(sec_f[14].ToString())).ToString("F"); Qzc_text = Qzc_text.Substring(0, Math.Min(4, Qzc_text.Length));
                    var Myc_text = Math.Abs(double.Parse(sec_f[16].ToString())).ToString("F"); Myc_text = Myc_text.Substring(0, Math.Min(4, Myc_text.Length));
                    var Mzc_text = Math.Abs(double.Parse(sec_f[17].ToString())).ToString("F"); Mzc_text = Mzc_text.Substring(0, Math.Min(4, Mzc_text.Length));
                    var klist = _kentei.get_Branch(new GH_Path(new int[] { 0, e }));
                    var Mki_color = new List<XSolidBrush>(); var Mkj_color = new List<XSolidBrush>(); var Mkc_color = new List<XSolidBrush>();
                    var Qki_color = new List<XSolidBrush>(); var Qkj_color = new List<XSolidBrush>(); var Qkc_color = new List<XSolidBrush>();
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
                    if (Mkj > 1 || Qkj > 1) { OKc_text = "N.G."; }
                    if (Mkc > 1 || Qkc > 1) { OKj_text = "N.G."; }
                    var values = new List<List<string>>();
                    values.Add(new List<string> { ele_text }); values.Add(new List<string> { name_text });
                    values.Add(new List<string> { lambda_text }); values.Add(new List<string> { A_text });
                    values.Add(new List<string> { Zy_text }); values.Add(new List<string> { Zz_text }); values.Add(new List<string> { "長期", "", "短期" });
                    values.Add(new List<string> { fc_text, "", fc2_text });
                    values.Add(new List<string> { ft_text, "", ft2_text }); values.Add(new List<string> { fby_text, "", fby2_text }); values.Add(new List<string> { fbz_text, "", fbz2_text }); values.Add(new List<string> { fs_text, "", fs2_text });
                    values.Add(new List<string> { "i端", "中央", "j端" }); values.Add(new List<string> { ni_text, "", nj_text });
                    values.Add(new List<string> { "長期検討" });
                    values.Add(new List<string> { Ni_text, Nc_text, Nj_text }); values.Add(new List<string> { Myi_text, Myc_text, Myj_text }); values.Add(new List<string> { Mzi_text, Mzc_text, Mzj_text });
                    values.Add(new List<string> { Qyi_text, Qyc_text, Qyj_text }); values.Add(new List<string> { Qzi_text, Qzc_text, Qzj_text });
                    values.Add(new List<string> { Mki_text, Mkc_text, Mkj_text }); values.Add(new List<string> { Qki_text, Qkc_text, Qkj_text });
                    values.Add(new List<string> { OKi_text, OKc_text, OKj_text });
                    int N = _sec_f.Branches.Count / ij.Count;
                    if (N == 5)
                    {
                        var text = new List<string> { "短期(L+X)検討", "短期(L+Y)検討", "短期(L-X)検討", "短期(L-Y)検討" };
                        for (int i = 0; i < 4; i++)
                        {
                            values.Add(new List<string> { text[i] });
                            var sec_f1 = _sec_f.get_Branch(new GH_Path(new int[] { i + 1, e }));
                            Ni_text = (double.Parse(sec_f[0].ToString()) + double.Parse(sec_f1[0].ToString())).ToString("F"); Ni_text = Ni_text.Substring(0, Math.Min(4, Ni_text.Length));
                            Qyi_text = Math.Abs(double.Parse(sec_f[1].ToString()) + double.Parse(sec_f1[1].ToString())).ToString("F"); Qyi_text = Qyi_text.Substring(0, Math.Min(4, Qyi_text.Length));
                            Qzi_text = Math.Abs(double.Parse(sec_f[2].ToString()) + double.Parse(sec_f1[2].ToString())).ToString("F"); Qzi_text = Qzi_text.Substring(0, Math.Min(4, Qzi_text.Length));
                            Myi_text = Math.Abs(double.Parse(sec_f[4].ToString()) + double.Parse(sec_f1[4].ToString())).ToString("F"); Myi_text = Myi_text.Substring(0, Math.Min(4, Myi_text.Length));
                            Mzi_text = Math.Abs(double.Parse(sec_f[5].ToString()) + double.Parse(sec_f1[5].ToString())).ToString("F"); Mzi_text = Mzi_text.Substring(0, Math.Min(4, Mzi_text.Length));
                            Nj_text = (-double.Parse(sec_f[6].ToString()) - double.Parse(sec_f1[6].ToString())).ToString("F"); Nj_text = Nj_text.Substring(0, Math.Min(4, Nj_text.Length));
                            Qyj_text = Math.Abs(double.Parse(sec_f[7].ToString()) + double.Parse(sec_f1[7].ToString())).ToString("F"); Qyj_text = Qyj_text.Substring(0, Math.Min(4, Qyj_text.Length));
                            Qzj_text = Math.Abs(double.Parse(sec_f[8].ToString()) + double.Parse(sec_f1[8].ToString())).ToString("F"); Qzj_text = Qzj_text.Substring(0, Math.Min(4, Qzj_text.Length));
                            Myj_text = Math.Abs(double.Parse(sec_f[10].ToString()) + double.Parse(sec_f1[10].ToString())).ToString("F"); Myj_text = Myj_text.Substring(0, Math.Min(4, Myj_text.Length));
                            Mzj_text = Math.Abs(double.Parse(sec_f[11].ToString()) + double.Parse(sec_f1[11].ToString())).ToString("F"); Mzj_text = Mzj_text.Substring(0, Math.Min(4, Mzj_text.Length));
                            Nc_text = (double.Parse(sec_f[12].ToString()) + double.Parse(sec_f1[12].ToString())).ToString("F"); Nc_text = Nc_text.Substring(0, Math.Min(4, Nc_text.Length));
                            Qyc_text = Math.Abs(double.Parse(sec_f[13].ToString()) + double.Parse(sec_f1[13].ToString())).ToString("F"); Qyc_text = Qyc_text.Substring(0, Math.Min(4, Qyc_text.Length));
                            Qzc_text = Math.Abs(double.Parse(sec_f[14].ToString()) + double.Parse(sec_f1[14].ToString())).ToString("F"); Qzc_text = Qzc_text.Substring(0, Math.Min(4, Qzc_text.Length));
                            Myc_text = Math.Abs(double.Parse(sec_f[16].ToString()) + double.Parse(sec_f1[16].ToString())).ToString("F"); Myc_text = Myc_text.Substring(0, Math.Min(4, Myc_text.Length));
                            Mzc_text = Math.Abs(double.Parse(sec_f[17].ToString()) + double.Parse(sec_f1[17].ToString())).ToString("F"); Mzc_text = Mzc_text.Substring(0, Math.Min(4, Mzc_text.Length));
                            klist = _kentei.get_Branch(new GH_Path(new int[] { i + 1, e }));
                            Mki = (Math.Max(double.Parse(klist[3].ToString()), double.Parse(klist[4].ToString())) + double.Parse(klist[0].ToString()));
                            Mki_text = Mki.ToString("F").Substring(0, 4); Mki_color.Add(new XSolidBrush(RGB((1 - Math.Min(Mki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            Mkj = (Math.Max(double.Parse(klist[8].ToString()), double.Parse(klist[9].ToString())) + double.Parse(klist[5].ToString()));
                            Mkj_text = Mkj.ToString("F").Substring(0, 4); Mkj_color.Add(new XSolidBrush(RGB((1 - Math.Min(Mkj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            Mkc = (Math.Max(double.Parse(klist[13].ToString()), double.Parse(klist[14].ToString())) + double.Parse(klist[10].ToString()));
                            Mkc_text = Mkc.ToString("F").Substring(0, 4); Mkc_color.Add(new XSolidBrush(RGB((1 - Math.Min(Mkc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            Qki = Math.Max(double.Parse(klist[1].ToString()), double.Parse(klist[2].ToString()));
                            Qki_text = Qki.ToString("F").Substring(0, 4); Qki_color.Add(new XSolidBrush(RGB((1 - Math.Min(Qki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            Qkj = Math.Max(double.Parse(klist[6].ToString()), double.Parse(klist[7].ToString()));
                            Qkj_text = Qkj.ToString("F").Substring(0, 4); Qkj_color.Add(new XSolidBrush(RGB((1 - Math.Min(Qkj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            Qkc = Math.Max(double.Parse(klist[11].ToString()), double.Parse(klist[12].ToString()));
                            Qkc_text = Qkc.ToString("F").Substring(0, 4); Qkc_color.Add(new XSolidBrush(RGB((1 - Math.Min(Qkc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            OKi_text = "O.K."; OKj_text = "O.K."; OKc_text = "O.K.";
                            if (Mki > 1 || Qki > 1) { OKi_text = "N.G."; }
                            if (Mkj > 1 || Qkj > 1) { OKc_text = "N.G."; }
                            if (Mkc > 1 || Qkc > 1) { OKj_text = "N.G."; }
                            values.Add(new List<string> { Ni_text, Nc_text, Nj_text }); values.Add(new List<string> { Myi_text, Myc_text, Myj_text }); values.Add(new List<string> { Mzi_text, Mzc_text, Mzj_text });
                            values.Add(new List<string> { Qyi_text, Qyc_text, Qyj_text }); values.Add(new List<string> { Qzi_text, Qzc_text, Qzj_text });
                            values.Add(new List<string> { Mki_text, Mkc_text, Mkj_text }); values.Add(new List<string> { Qki_text, Qkc_text, Qkj_text });
                            values.Add(new List<string> { OKi_text, OKc_text, OKj_text });
                        }
                    }


                    if (e % 6 == 0)
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
                        var j = e % 6;
                        gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, offset_x + label_width + text_width * 3 * (j + 1), offset_y + pitchy * i);//横線
                        gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * (i + 1));//縦線
                        if (values[i].Count == 1)
                        {
                            gfx.DrawString(values[i][0], font, XBrushes.Black, new XRect(offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, text_width * 3, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                        }
                        else
                        {
                            var color1 = XBrushes.Black; var color2 = XBrushes.Black; var color3 = XBrushes.Black; var f = font;
                            if (i == 20) { color1 = Mki_color[0]; color2 = Mkc_color[0]; color3 = Mkj_color[0]; f = fontbold; }
                            else if (i == 21) { color1 = Qki_color[0]; color2 = Qkc_color[0]; color3 = Qkj_color[0]; f = fontbold; }
                            else if (i == 29) { color1 = Mki_color[1]; color2 = Mkc_color[1]; color3 = Mkj_color[1]; f = fontbold; }
                            else if (i == 30) { color1 = Qki_color[1]; color2 = Qkc_color[1]; color3 = Qkj_color[1]; f = fontbold; }
                            else if (i == 38) { color1 = Mki_color[2]; color2 = Mkc_color[2]; color3 = Mkj_color[2]; f = fontbold; }
                            else if (i == 39) { color1 = Qki_color[2]; color2 = Qkc_color[2]; color3 = Qkj_color[2]; f = fontbold; }
                            else if (i == 47) { color1 = Mki_color[3]; color2 = Mkc_color[3]; color3 = Mkj_color[3]; f = fontbold; }
                            else if (i == 48) { color1 = Qki_color[3]; color2 = Qkc_color[3]; color3 = Qkj_color[3]; f = fontbold; }
                            else if (i == 56) { color1 = Mki_color[4]; color2 = Mkc_color[4]; color3 = Mkj_color[4]; f = fontbold; }
                            else if (i == 57) { color1 = Qki_color[4]; color2 = Qkc_color[4]; color3 = Qkj_color[4]; f = fontbold; }
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
                return OpenSeesUtility.Properties.Resources.steelcheckpdf;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9b17a365-2ab6-41bc-8a72-2fb635fea861"); }
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
                int height = 15; int radi1 = 7; int radi2 = 4;
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