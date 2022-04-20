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
    public class SpringCheck : GH_Component
    {
        public static int on_off_11 = 1; public static int on_off_12 = 0; public static double fontsize;
        public static int on_off_21 = 0; public static int on_off_22 = 0; public static int on_off_23 = 0; public static int on_off_24 = 0; static int on_off = 0;
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
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        public SpringCheck()
           : base("Allowable stress design for spring elements", "SpringCheck",
               "Allowable stress design(danmensantei) for spring elements",
               "OpenSees", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("spring element", "spring", "[[No.i, No.j, kxt, ktc, kyt, kyc, kzt, kzc, rx, ry, rz(spring value)],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("spring_force", "spring_f", "[[N,Qy,Qz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("spring_allowable_f", "spring_a", "[[Nta,Nca,Qyta,Qyca,Qzta,Qzca,Mxa,Mya,Mza],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("index", "index", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("fontsize", "FS", "font size for display texts", GH_ParamAccess.item, 12.0);///
            pManager.AddTextParameter("casename", "casename", "casenames", GH_ParamAccess.list, new List<string> { "長期検討","L+X検討", "L+Y検討", "L-X検討", "L-Y検討" });///
            pManager.AddTextParameter("outputname", "outputname", "output file name", GH_ParamAccess.item, "SpringCheck");///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("kentei_hi", "kentei", "[[for N,for Qy,for Qz,for My,for Mz],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddPointParameter("pts", "pts", "point", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r); var r = _r.Branches;
            DA.GetDataTree("spring element", out GH_Structure<GH_Number> _spring); var spring = _spring.Branches;
            DA.GetDataTree("spring_force", out GH_Structure<GH_Number> _spring_f); var spring_f = _spring_f.Branches;
            DA.GetDataTree("spring_allowable_f", out GH_Structure<GH_Number> _spring_a); var spring_a = _spring_a.Branches;
            List<double> index = new List<double>(); DA.GetDataList("index", index); var pts = new List<Point3d>();
            var casename = new List<string>(); DA.GetDataList("casename", casename);
            var kentei = new GH_Structure<GH_Number>(); int digit = 4; fontsize = 20.0; DA.GetData("fontsize", ref fontsize);
            if (index[0] == -9999)
            {
                index = new List<double>();
                for (int e = 0; e < spring.Count; e++) { index.Add(e); }
            }
            if (on_off == 1)
            {
                XColor RGB(double h, double s, double l)//convert HSL to RGB
                {
                    var max = 0.0; var min = 0.0; var rr = 0.0; var gg = 0.0; var bb = 0.0;
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
                        gg = (h / hp) * (max - min) + min;
                        bb = min;
                    }
                    else if (q <= 2)
                    {
                        rr = ((hp * 2 - h) / hp) * (max - min) + min;
                        gg = max;
                        bb = min;
                    }
                    else if (q <= 3)
                    {
                        rr = min;
                        gg = max;
                        bb = ((h - hp * 2) / hp) * (max - min) + min;
                    }
                    else if (q <= 4)
                    {
                        rr = min;
                        gg = ((hp * 4 - h) / hp) * (max - min) + min;
                        bb = max;
                    }
                    else if (q <= 5)
                    {
                        rr = ((h - hp * 4) / hp) * (max - min) + min;
                        gg = min;
                        bb = max;
                    }
                    else
                    {
                        rr = max;
                        gg = min;
                        bb = ((HUE_MAX - h) / hp) * (max - min) + min;
                    }
                    rr *= RGB_MAX; gg *= RGB_MAX; bb *= RGB_MAX;
                    return XColor.FromArgb((int)rr, (int)gg, (int)bb);
                }
                var pdfname = "SpringCheck"; DA.GetData("outputname", ref pdfname);
                // フォントリゾルバーのグローバル登録
                if (PdfCreate.JapaneseFontResolver.fontset == 0) { PdfSharp.Fonts.GlobalFontSettings.FontResolver = fontresolver; PdfCreate.JapaneseFontResolver.fontset = 1; }
                // PDFドキュメントを作成。
                PdfDocument document = new PdfDocument();
                document.Info.Title = pdfname;
                document.Info.Author = "Shinnosuke Fujita, Assoc. Prof., The Univ. of Kitakyushu";
                // フォントを作成。
                XFont font = new XFont("Gen Shin Gothic", 8, XFontStyle.Regular);
                XFont fontbold = new XFont("Gen Shin Gothic", 8, XFontStyle.Bold);
                var pen = XPens.Black;
                if (spring_f[0].Count == 18 || spring_f[0].Count == 30)
                {
                    var labels = new List<string>
                    {
                        "部材番号","ばね方向","kx[kN/m]","ky[kN/m]","kz[kN/m]","kmy[kNm/rad]","kmz[kNm/rad]","","許容引張軸力Nta[kN]","許容圧縮軸力Nca[kN]","許容せん断力Qya[kN]","許容せん断力Qza[kN]","許容曲げモーメントMya[kN]","許容曲げモーメントMza[kN]","","N[kN]","Qy[kN]　Qz[kN]","My[kN]　Mz[kN]","N/Na","Qy/Qya　Qz/Qza","My/Mya　Mz/Mza","検定比合計","判定","","N[kN]","Qy[kN]　Qz[kN]","My[kN]　Mz[kN]","N/Na","Qy/Qya　Qz/Qza","My/Mya　Mz/Mza","検定比合計","判定","","N[kN]","Qy[kN]　Qz[kN]","My[kN]　Mz[kN]","N/Na","Qy/Qya　Qz/Qza","My/Mya　Mz/Mza","検定比合計","判定","","N[kN]","Qy[kN]　Qz[kN]","My[kN]　Mz[kN]","N/Na","Qy/Qya　Qz/Qza","My/Mya　Mz/Mza","検定比合計","判定","","N[kN]","Qy[kN]　Qz[kN]","My[kN]　Mz[kN]","N/Na","Qy/Qya　Qz/Qza","My/Mya　Mz/Mza","検定比合計","判定"
                    };
                    var label_width = 110; var offset_x = 25; var offset_y = 25; var pitchy = 13; var text_width = 25; PdfPage page = new PdfPage(); page.Size = PageSize.A4;
                    for (int ind = 0; ind < index.Count; ind++)
                    {
                        int e = (int)index[ind];
                        var kx1 = spring[e][2].Value; var kx2 = spring[e][3].Value; var ky1 = spring[e][4].Value; var ky2 = spring[e][5].Value; var kz1 = spring[e][6].Value; var kz2 = spring[e][7].Value;
                        var kmy = spring[e][9].Value; var kmz = spring[e][10].Value;
                        var N = spring_f[e][0].Value; var Qy = spring_f[e][1].Value; var Qz = spring_f[e][2].Value; var My = spring_f[e][4].Value; var Mz = spring_f[e][5].Value;
                        var Na1 = spring_a[e][0].Value; var Na2 = spring_a[e][1].Value; var Qya1 = spring_a[e][2].Value; var Qya2 = spring_a[e][3].Value; var Qza1 = spring_a[e][4].Value; var Qza2 = spring_a[e][5].Value; var Mya = spring_a[e][6].Value; var Mza = spring_a[e][7].Value;
                        var N_x1 = spring_f[e][6 + 0].Value; var Qy_x1 = spring_f[e][6 + 1].Value; var Qz_x1 = spring_f[e][6 + 2].Value; var My_x1 = spring_f[e][6 + 4].Value; var Mz_x1 = spring_f[e][6 + 5].Value;
                        var N_y1 = spring_f[e][12 + 0].Value; var Qy_y1 = spring_f[e][12 + 1].Value; var Qz_y1 = spring_f[e][12 + 2].Value; var My_y1 = spring_f[e][12 + 4].Value; var Mz_y1 = spring_f[e][12 + 5].Value;
                        var N_x2 = -N_x1; var Qy_x2 = -Qy_x1; var Qz_x2 = -Qz_x1; var My_x2 = -My_x1; var Mz_x2 = -Mz_x1; var N_y2 = -N_y1; var Qy_y2 = -Qy_y1; var Qz_y2 = -Qz_y1; var My_y2 = -My_y1; var Mz_y2 = -Mz_y1;
                        if (spring_f[0].Count == 30)
                        {
                            N_x2 = spring_f[e][12 + 6 + 0].Value; Qy_x2 = spring_f[e][12 + 6 + 1].Value; Qz_x2 = spring_f[e][12 + 6 + 2].Value; My_x2 = spring_f[e][12 + 6 + 4].Value; Mz_x2 = spring_f[e][12 + 6 + 5].Value;
                            N_y2 = spring_f[e][12 + 12 + 0].Value; Qy_y2 = spring_f[e][12 + 12 + 1].Value; Qz_y2 = spring_f[e][12 + 12 + 2].Value; My_y2 = spring_f[e][12 + 12 + 4].Value; Mz_y2 = spring_f[e][12 + 12 + 5].Value;
                        }
                        var kN1 = 0.0; var kQy1 = 0.0; var kQz1 = 0.0; var kMy1 = 0.0; var kMz1 = 0.0;
                        var kN2 = 0.0; var kQy2 = 0.0; var kQz2 = 0.0; var kMy2 = 0.0; var kMz2 = 0.0;
                        var kN3 = 0.0; var kQy3 = 0.0; var kQz3 = 0.0; var kMy3 = 0.0; var kMz3 = 0.0;
                        var kN4 = 0.0; var kQy4 = 0.0; var kQz4 = 0.0; var kMy4 = 0.0; var kMz4 = 0.0;
                        var kN5 = 0.0; var kQy5 = 0.0; var kQz5 = 0.0; var kMy5 = 0.0; var kMz5 = 0.0;
                        //L
                        if (N >= 0)
                        {
                            if (Na1 != 0) { kN1 = N / Na1 * 2; };
                        }
                        else
                        {
                            if (Na2 != 0) { kN1 = -N / Na2 * 2; };
                        }
                        if (Qy >= 0)
                        {
                            if (Qya1 != 0) { kQy1 = Qy / Qya1 * 2; };
                        }
                        else
                        {
                            if (Qya2 != 0) { kQy1 = -Qy / Qya2 * 2; };
                        }
                        if (Qz >= 0)
                        {
                            if (Qza1 != 0) { kQz1 = Qz / Qza1 * 2; };
                        }
                        else
                        {
                            if (Qza2 != 0) { kQz1 = -Qz / Qza2 * 2; };
                        }
                        if (Mya != 0) { kMy1 = Math.Abs(My) / Mya * 2; }; if (Mza != 0) { kMz1 = Math.Abs(Mz) / Mza * 2; };
                        //L+X
                        if ((N + N_x1) >= 0)
                        {
                            if (Na1 != 0) { kN2 = (N + N_x1) / Na1; };
                        }
                        else
                        {
                            if (Na2 != 0) { kN2 = -(N + N_x1) / Na2; };
                        }
                        if ((Qy + Qy_x1) >= 0)
                        {
                            if (Qya1 != 0) { kQy2 = (Qy + Qy_x1) / Qya1; };
                        }
                        else
                        {
                            if (Qya2 != 0) { kQy2 = -(Qy + Qy_x1) / Qya2; };
                        }
                        if ((Qz + Qz_x1) >= 0)
                        {
                            if (Qza1 != 0) { kQz2 = (Qz + Qz_x1) / Qza1; };
                        }
                        else
                        {
                            if (Qza2 != 0) { kQz2 = -(Qz + Qz_x1) / Qza2; };
                        }
                        if (Mya != 0) { kMy2 = Math.Abs(My + My_x1) / Mya; }; if (Mza != 0) { kMz2 = Math.Abs(Mz + Mz_x1) / Mza; };
                        //L+Y
                        if ((N + N_y1) >= 0)
                        {
                            if (Na1 != 0) { kN3 = (N + N_y1) / Na1; };
                        }
                        else
                        {
                            if (Na2 != 0) { kN3 = -(N + N_y1) / Na2; };
                        }
                        if ((Qy + Qy_y1) >= 0)
                        {
                            if (Qya1 != 0) { kQy3 = (Qy + Qy_y1) / Qya1; };
                        }
                        else
                        {
                            if (Qya2 != 0) { kQy3 = -(Qy + Qy_y1) / Qya2; };
                        }
                        if ((Qz + Qz_y1) >= 0)
                        {
                            if (Qza1 != 0) { kQz3 = (Qz + Qz_y1) / Qza1; };
                        }
                        else
                        {
                            if (Qza2 != 0) { kQz3 = -(Qz + Qz_y1) / Qza2; };
                        }
                        if (Mya != 0) { kMy3 = Math.Abs(My + My_y1) / Mya; }; if (Mza != 0) { kMz3 = Math.Abs(Mz + Mz_y1) / Mza; };
                        //L-X
                        if ((N + N_x2) >= 0)
                        {
                            if (Na1 != 0) { kN4 = (N + N_x2) / Na1; };
                        }
                        else
                        {
                            if (Na2 != 0) { kN4 = -(N + N_x2) / Na2; };
                        }
                        if ((Qy + Qy_x2) >= 0)
                        {
                            if (Qya1 != 0) { kQy4 = (Qy + Qy_x2) / Qya1; };
                        }
                        else
                        {
                            if (Qya2 != 0) { kQy4 = -(Qy + Qy_x2) / Qya2; };
                        }
                        if ((Qz + Qz_x2) >= 0)
                        {
                            if (Qza1 != 0) { kQz4 = (Qz + Qz_x2) / Qza1; };
                        }
                        else
                        {
                            if (Qza2 != 0) { kQz4 = -(Qz + Qz_x2) / Qza2; };
                        }
                        if (Mya != 0) { kMy4 = Math.Abs(My + My_x2) / Mya; }; if (Mza != 0) { kMz4 = Math.Abs(Mz + Mz_x2) / Mza; };
                        //L-Y
                        if ((N + N_y2) >= 0)
                        {
                            if (Na1 != 0) { kN5 = (N + N_y2) / Na1; };
                        }
                        else
                        {
                            if (Na2 != 0) { kN5 = -(N + N_y2) / Na2; };
                        }
                        if ((Qy + Qy_y2) >= 0)
                        {
                            if (Qya1 != 0) { kQy5 = (Qy + Qy_y2) / Qya1; };
                        }
                        else
                        {
                            if (Qya2 != 0) { kQy5 = -(Qy + Qy_y2) / Qya2; };
                        }
                        if ((Qz + Qz_y2) >= 0)
                        {
                            if (Qza1 != 0) { kQz5 = (Qz + Qz_y2) / Qza1; };
                        }
                        else
                        {
                            if (Qza2 != 0) { kQz5 = -(Qz + Qz_y2) / Qza2; };
                        }
                        if (Mya != 0) { kMy5 = Math.Abs(My + My_y2) / Mya; }; if (Mza != 0) { kMz5 = Math.Abs(Mz + Mz_y2) / Mza; };
                        var values = new List<List<string>>();
                        values.Add(new List<string> { e.ToString() });
                        values.Add(new List<string> { "+","","-" });
                        if (kx1 >= 999999 && kx2>=999999)
                        {
                            values.Add(new List<string> { "---", "", "---" });
                        }
                        else if (kx1 >= 999999 && kx2 < 999999)
                        {
                            values.Add(new List<string> { "---", "", Math.Round(kx2, 0).ToString() });
                        }
                        else if (kx1 < 999999 && kx2 >= 999999)
                        {
                            values.Add(new List<string> { Math.Round(kx1, 0).ToString(), "", "---" });
                        }
                        else
                        {
                            values.Add(new List<string> { Math.Round(kx1, 0).ToString(), "", Math.Round(kx2, 0).ToString() });
                        }
                        if (ky1 >= 999999 && ky2 >= 999999)
                        {
                            values.Add(new List<string> { "---", "", "---" });
                        }
                        else
                        {
                            values.Add(new List<string> { Math.Round(ky1, 0).ToString(), "", Math.Round(ky2, 0).ToString() });
                        }
                        if (kz1 >= 999999 && kz2 >= 999999)
                        {
                            values.Add(new List<string> { "---", "", "---" });
                        }
                        else
                        {
                            values.Add(new List<string> { Math.Round(kz1, 0).ToString(), "", Math.Round(kz2, 0).ToString() });
                        }
                        if (kmy > 999999)
                        {
                            values.Add(new List<string> { "---" });
                        }
                        else
                        {
                            values.Add(new List<string> { Math.Round(kmy, 0).ToString() });
                        }
                        if (kmz > 999999)
                        {
                            values.Add(new List<string> { "---" });
                        }
                        else
                        {
                            values.Add(new List<string> { Math.Round(kmz, 0).ToString() });
                        }
                        values.Add(new List<string> { "長期", "", "短期" });
                        var Na1_text = new List<string> { "-", "", "-" }; var Na2_text = new List<string> { "-", "", "-" };
                        var Qya1_text = new List<string> { "-", "", "-" }; var Qya2_text = new List<string> { "-", "", "-" }; var Qza1_text = new List<string> { "-", "", "-" }; var Qza2_text = new List<string> { "-", "", "-" };
                        var Mya_text = new List<string> { "-" }; var Mza_text = new List<string> { "-" };
                        if (Na1 != 0) { Na1_text = new List<string> { Math.Round(Na1 / 2.0, 2).ToString(), "", Math.Round(Na1, 2).ToString() }; }
                        if (Na2 != 0) { Na2_text = new List<string> { Math.Round(Na2 / 2.0, 2).ToString(), "", Math.Round(Na2, 2).ToString() }; }
                        if (Qya1 != 0) { Qya1_text = new List<string> { Math.Round(Qya1 / 2.0, 2).ToString(), "", Math.Round(Qya1, 2).ToString() }; }
                        if (Qya2 != 0) { Qya2_text = new List<string> { Math.Round(Qya2 / 2.0, 2).ToString(), "", Math.Round(Qya2, 2).ToString() }; }
                        if (Qza1 != 0) { Qza1_text = new List<string> { Math.Round(Qza1 / 2.0, 2).ToString(), "", Math.Round(Qza1, 2).ToString() }; }
                        if (Qza2 != 0) { Qza2_text = new List<string> { Math.Round(Qza2 / 2.0, 2).ToString(), "", Math.Round(Qza2, 2).ToString() }; }
                        if (Mya != 0) { Mya_text = new List<string> { Math.Round(Mya / 2.0, 2).ToString(), "", Math.Round(Mya, 2).ToString() }; }
                        if (Mza != 0) { Mza_text = new List<string> { Math.Round(Mza / 2.0, 2).ToString(), "", Math.Round(Mza, 2).ToString() }; }
                        //Qya+とQya-, Qza+とQza-は同じ値として+の値のみ出力
                        values.Add(Na1_text); values.Add(Na2_text); values.Add(Qya1_text); values.Add(Qza1_text); values.Add(Mya_text); values.Add(Mza_text);
                        values.Add(new List<string> { casename[0] });
                        if (kx1 >= 999999 && kx2 >= 999999) { values.Add(new List<string> { "---" }); }
                        else if (kx1 >= 999999 && N > 0) { values.Add(new List<string> { "---" }); }
                        else if (kx2 >= 999999 && N < 0) { values.Add(new List<string> { "---" }); }
                        else { values.Add(new List<string> { Math.Round(N, 2).ToString() }); }
                        if (ky1 >= 999999 && ky2 >= 999999 && kz1 >= 999999 && kz2 >= 999999) { values.Add(new List<string> { "---", "", "---" }); }
                        else if (ky1 >= 999999 && ky2 >= 999999) { values.Add(new List<string> { "---", "", Math.Round(Qz, 2).ToString() }); }
                        else if (kz1 >= 999999 && kz2 >= 999999) { values.Add(new List<string> { Math.Round(Qy, 2).ToString(), "", "---" }); }
                        else { values.Add(new List<string> { Math.Round(Qy, 2).ToString(), "", Math.Round(Qz, 2).ToString() }); }
                        if (kmy > 999999 && kmz > 999999) { values.Add(new List<string> { "---", "", "---" }); }
                        else { values.Add(new List<string> { Math.Round(My, 2).ToString(), "", Math.Round(Mz, 2).ToString() }); }

                        if (kN1 != 0) { values.Add(new List<string> { Math.Round(kN1, 2).ToString() }); } else { values.Add(new List<string> { "-" }); }
                        var value = new List<string>();
                        if (kQy1 != 0) { value.Add(Math.Round(kQy1, 2).ToString()); } else { value.Add("-"); }
                        value.Add("");
                        if (kQz1 != 0) { value.Add(Math.Round(kQz1, 2).ToString()); } else { value.Add("-"); }
                        values.Add(value);
                        value = new List<string>();
                        if (kMy1 != 0) { value.Add(Math.Round(kMy1, 2).ToString()); } else { value.Add("-"); }
                        value.Add("");
                        if (kMz1 != 0) { value.Add(Math.Round(kMz1, 2).ToString()); } else { value.Add("-"); }
                        values.Add(value);
                        if (kN1 + kQy1 + kQz1 + kMy1 + kMz1 != 0) { values.Add(new List<string> { Math.Round(kN1 + kQy1 + kQz1 + kMy1 + kMz1, 2).ToString() }); } else { values.Add(new List<string> { "-" }); }
                        if (kN1 + kQy1 + kQz1 + kMy1 + kMz1 <= 1.0) { values.Add(new List<string> { "O.K." }); } else { values.Add(new List<string> { "N.G." }); }

                        values.Add(new List<string> { casename[1] });
                        if (kx1 >= 999999 && kx2 >= 999999) { values.Add(new List<string> { "---" }); }
                        else if (kx1 >= 999999 && N + N_x1 > 0) { values.Add(new List<string> { "---" }); }
                        else if (kx2 >= 999999 && N + N_x1 < 0) { values.Add(new List<string> { "---" }); }
                        else { values.Add(new List<string> { Math.Round(N + N_x1, 2).ToString() }); }
                        if (ky1 >= 999999 && ky2 >= 999999 && kz1 >= 999999 && kz2 >= 999999) { values.Add(new List<string> { "---", "", "---" }); }
                        else if (ky1 >= 999999 && ky2 >= 999999) { values.Add(new List<string> { "---", "", Math.Round(Qz + Qz_x1, 2).ToString() }); }
                        else if (kz1 >= 999999 && kz2 >= 999999) { values.Add(new List<string> { Math.Round(Qy + Qy_x1, 2).ToString(), "", "---" }); }
                        else { values.Add(new List<string> { Math.Round(Qy + Qy_x1, 2).ToString(), "", Math.Round(Qz + Qz_x1, 2).ToString() }); }
                        if (kmy > 999999 && kmz > 999999) { values.Add(new List<string> { "---", "", "---" }); }
                        else { values.Add(new List<string> { Math.Round(My + My_x1, 2).ToString(), "", Math.Round(Mz + Mz_x1, 2).ToString() }); }
                        if (kN2 != 0) { values.Add(new List<string> { Math.Round(kN2, 2).ToString() }); } else { values.Add(new List<string> { "-" }); }
                        value = new List<string>();
                        if (kQy2 != 0) { value.Add(Math.Round(kQy2, 2).ToString() ); } else { value.Add("-" ); }
                        value.Add("");
                        if (kQz2 != 0) { value.Add(Math.Round(kQz2, 2).ToString() ); } else { value.Add("-"); }
                        values.Add(value);
                        value = new List<string>();
                        if (kMy2 != 0) { value.Add(Math.Round(kMy2, 2).ToString()); } else { value.Add("-"); }
                        value.Add("");
                        if (kMz2 != 0) { value.Add(Math.Round(kMz2, 2).ToString()); } else { value.Add("-"); }
                        values.Add(value);
                        if (kN2 + kQy2 + kQz2 + kMy2 + kMz2 != 0) { values.Add(new List<string> { Math.Round(kN2 + kQy2 + kQz2 + kMy2 + kMz2, 2).ToString() }); } else { values.Add(new List<string> { "-" }); }
                        if (kN2 + kQy2 + kQz2 + kMy2 + kMz2 <= 1.0) { values.Add(new List<string> { "O.K." }); } else { values.Add(new List<string> { "N.G." }); }

                        values.Add(new List<string> { casename[2] });
                        if (kx1 >= 999999 && kx2 >= 999999) { values.Add(new List<string> { "---" }); }
                        else if (kx1 >= 999999 && N + N_y1 > 0) { values.Add(new List<string> { "---" }); }
                        else if (kx2 >= 999999 && N + N_y1 < 0) { values.Add(new List<string> { "---" }); }
                        else { values.Add(new List<string> { Math.Round(N + N_y1, 2).ToString() }); }
                        if (ky1 >= 999999 && ky2 >= 999999 && kz1 >= 999999 && kz2 >= 999999) { values.Add(new List<string> { "---", "", "---" }); }
                        else if (ky1 >= 999999 && ky2 >= 999999) { values.Add(new List<string> { "---", "", Math.Round(Qz + Qz_y1, 2).ToString() }); }
                        else if (kz1 >= 999999 && kz2 >= 999999) { values.Add(new List<string> { Math.Round(Qy + Qy_y1, 2).ToString(), "", "---" }); }
                        else { values.Add(new List<string> { Math.Round(Qy + Qy_y1, 2).ToString(), "", Math.Round(Qz + Qz_y1, 2).ToString() }); }
                        if (kmy > 999999 && kmz > 999999) { values.Add(new List<string> { "---", "", "---" }); }
                        else { values.Add(new List<string> { Math.Round(My + My_y1, 2).ToString(), "", Math.Round(Mz + Mz_y1, 2).ToString() }); }
                        if (kN3 != 0) { values.Add(new List<string> { Math.Round(kN3, 2).ToString() }); } else { values.Add(new List<string> { "-" }); }
                        value = new List<string>();
                        if (kQy3 != 0) { value.Add(Math.Round(kQy3, 2).ToString()); } else { value.Add("-"); }
                        value.Add("");
                        if (kQz3 != 0) { value.Add(Math.Round(kQz3, 2).ToString()); } else { value.Add("-"); }
                        values.Add(value);
                        value = new List<string>();
                        if (kMy3 != 0) { value.Add(Math.Round(kMy3, 2).ToString()); } else { value.Add("-"); }
                        value.Add("");
                        if (kMz3 != 0) { value.Add(Math.Round(kMz3, 2).ToString()); } else { value.Add("-"); }
                        values.Add(value);
                        if (kN3 + kQy3 + kQz3 + kMy3 + kMz3 != 0) { values.Add(new List<string> { Math.Round(kN3 + kQy3 + kQz3 + kMy3 + kMz3, 2).ToString() }); } else { values.Add(new List<string> { "-" }); }
                        if (kN3 + kQy3 + kQz3 + kMy3 + kMz3 <= 1.0) { values.Add(new List<string> { "O.K." }); } else { values.Add(new List<string> { "N.G." }); }

                        values.Add(new List<string> { casename[3] });
                        if (kx1 >= 999999 && kx2 >= 999999) { values.Add(new List<string> { "---" }); }
                        else if (kx1 >= 999999 && N + N_x2 > 0) { values.Add(new List<string> { "---" }); }
                        else if (kx2 >= 999999 && N + N_x2 < 0) { values.Add(new List<string> { "---" }); }
                        else { values.Add(new List<string> { Math.Round(N + N_x2, 2).ToString() }); }
                        if (ky1 >= 999999 && ky2 >= 999999 && kz1 >= 999999 && kz2 >= 999999) { values.Add(new List<string> { "---", "", "---" }); }
                        else if (ky1 >= 999999 && ky2 >= 999999) { values.Add(new List<string> { "---", "", Math.Round(Qz + Qz_x2, 2).ToString() }); }
                        else if (kz1 >= 999999 && kz2 >= 999999) { values.Add(new List<string> { Math.Round(Qy + Qy_x2, 2).ToString(), "", "---" }); }
                        else { values.Add(new List<string> { Math.Round(Qy + Qy_x2, 2).ToString(), "", Math.Round(Qz + Qz_x2, 2).ToString() }); }
                        if (kmy > 999999 && kmz > 999999) { values.Add(new List<string> { "---", "", "---" }); }
                        else { values.Add(new List<string> { Math.Round(My + My_x2, 2).ToString(), "", Math.Round(Mz + Mz_x2, 2).ToString() }); }
                        if (kN4 != 0) { values.Add(new List<string> { Math.Round(kN4, 2).ToString() }); } else { values.Add(new List<string> { "-" }); }
                        value = new List<string>();
                        if (kQy4 != 0) { value.Add(Math.Round(kQy4, 2).ToString()); } else { value.Add("-"); }
                        value.Add("");
                        if (kQz4 != 0) { value.Add(Math.Round(kQz4, 2).ToString()); } else { value.Add("-"); }
                        values.Add(value);
                        value = new List<string>();
                        if (kMy4 != 0) { value.Add(Math.Round(kMy4, 2).ToString()); } else { value.Add("-"); }
                        value.Add("");
                        if (kMz4 != 0) { value.Add(Math.Round(kMz4, 2).ToString()); } else { value.Add("-"); }
                        values.Add(value);
                        if (kN4 + kQy4 + kQz4 + kMy4 + kMz4 != 0) { values.Add(new List<string> { Math.Round(kN4 + kQy4 + kQz4 + kMy4 + kMz4, 2).ToString() }); } else { values.Add(new List<string> { "-" }); }
                        if (kN4 + kQy4 + kQz4 + kMy4 + kMz4 <= 1.0) { values.Add(new List<string> { "O.K." }); } else { values.Add(new List<string> { "N.G." }); }

                        values.Add(new List<string> { casename[4] });
                        if (kx1 >= 999999 && kx2 >= 999999) { values.Add(new List<string> { "---" }); }
                        else if (kx1 >= 999999 && N + N_y2 > 0) { values.Add(new List<string> { "---" }); }
                        else if (kx2 >= 999999 && N + N_y2 < 0) { values.Add(new List<string> { "---" }); }
                        else { values.Add(new List<string> { Math.Round(N + N_y2, 2).ToString() }); }
                        values.Add(new List<string> { Math.Round(Qy + Qy_y2, 2).ToString(),"",Math.Round(Qz + Qz_y2, 2).ToString() });
                        if (kmy > 999999 && kmz > 999999) { values.Add(new List<string> { "---", "", "---" }); }
                        else { values.Add(new List<string> { Math.Round(My + My_y2, 2).ToString(), "", Math.Round(Mz + Mz_y2, 2).ToString() }); }
                        if (kN5 != 0) { values.Add(new List<string> { Math.Round(kN5, 2).ToString() }); } else { values.Add(new List<string> { "-" }); }
                        value = new List<string>();
                        if (kQy5 != 0) { value.Add(Math.Round(kQy5, 2).ToString()); } else { value.Add("-"); }
                        value.Add("");
                        if (kQz5 != 0) { value.Add(Math.Round(kQz5, 2).ToString()); } else { value.Add("-"); }
                        values.Add(value);
                        value = new List<string>();
                        if (kMy5 != 0) { value.Add(Math.Round(kMy5, 2).ToString()); } else { value.Add("-"); }
                        value.Add("");
                        if (kMz5 != 0) { value.Add(Math.Round(kMz5, 2).ToString()); } else { value.Add("-"); }
                        values.Add(value);
                        if (kN5 + kQy5 + kQz5 + kMy5 + kMz5 != 0) { values.Add(new List<string> { Math.Round(kN5 + kQy5 + kQz5 + kMy5 + kMz5, 2).ToString() }); } else { values.Add(new List<string> { "-" }); }
                        if (kN5 + kQy5 + kQz5 + kMy5 + kMz5 <= 1.0) { values.Add(new List<string> { "O.K." }); } else { values.Add(new List<string> { "N.G." }); }
                        if (ind % 6 == 0)
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
                            var j = ind % 6;
                            gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, offset_x + label_width + text_width * 3 * (j + 1), offset_y + pitchy * i);//横線
                            gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * (i + 1));//縦線
                            if (values[i].Count == 1)
                            {
                                var color1 = XBrushes.Black;
                                if (i == 18) { color1 = new XSolidBrush(RGB((1 - Math.Min(kN1, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 21) { color1 = new XSolidBrush(RGB((1 - Math.Min(kN1+kQy1+kQz1+kMy1+kMz1, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 27) { color1 = new XSolidBrush(RGB((1 - Math.Min(kN2, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 30) { color1 = new XSolidBrush(RGB((1 - Math.Min(kN2 + kQy2 + kQz2 + kMy2 + kMz2, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 36) { color1 = new XSolidBrush(RGB((1 - Math.Min(kN3, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 39) { color1 = new XSolidBrush(RGB((1 - Math.Min(kN3 + kQy3 + kQz3 + kMy3 + kMz3, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 45) { color1 = new XSolidBrush(RGB((1 - Math.Min(kN4, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 48) { color1 = new XSolidBrush(RGB((1 - Math.Min(kN4 + kQy4 + kQz4 + kMy4 + kMz4, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 54) { color1 = new XSolidBrush(RGB((1 - Math.Min(kN5, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 57) { color1 = new XSolidBrush(RGB((1 - Math.Min(kN5 + kQy5 + kQz5 + kMy5 + kMz5, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                gfx.DrawString(values[i][0], font, color1, new XRect(offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, text_width * 3, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                            }
                            else if (values[i].Count == 3)
                            {
                                var color1 = XBrushes.Black; var color2 = XBrushes.Black; var color3 = XBrushes.Black; var f = font;
                                if (i == 19) { color1 = new XSolidBrush(RGB((1 - Math.Min(kQy1, 1.0)) * 1.9 / 3.0, 1, 0.5)); color3 = new XSolidBrush(RGB((1 - Math.Min(kQz1, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 20) { color1 = new XSolidBrush(RGB((1 - Math.Min(kMy1, 1.0)) * 1.9 / 3.0, 1, 0.5)); color3 = new XSolidBrush(RGB((1 - Math.Min(kMz1, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 28) { color1 = new XSolidBrush(RGB((1 - Math.Min(kQy2, 1.0)) * 1.9 / 3.0, 1, 0.5)); color3 = new XSolidBrush(RGB((1 - Math.Min(kQz2, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 29) { color1 = new XSolidBrush(RGB((1 - Math.Min(kMy2, 1.0)) * 1.9 / 3.0, 1, 0.5)); color3 = new XSolidBrush(RGB((1 - Math.Min(kMz2, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 37) { color1 = new XSolidBrush(RGB((1 - Math.Min(kQy3, 1.0)) * 1.9 / 3.0, 1, 0.5)); color3 = new XSolidBrush(RGB((1 - Math.Min(kQz3, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 38) { color1 = new XSolidBrush(RGB((1 - Math.Min(kMy3, 1.0)) * 1.9 / 3.0, 1, 0.5)); color3 = new XSolidBrush(RGB((1 - Math.Min(kMz3, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 46) { color1 = new XSolidBrush(RGB((1 - Math.Min(kQy4, 1.0)) * 1.9 / 3.0, 1, 0.5)); color3 = new XSolidBrush(RGB((1 - Math.Min(kQz4, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 47) { color1 = new XSolidBrush(RGB((1 - Math.Min(kMy4, 1.0)) * 1.9 / 3.0, 1, 0.5)); color3 = new XSolidBrush(RGB((1 - Math.Min(kMz4, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 55) { color1 = new XSolidBrush(RGB((1 - Math.Min(kQy5, 1.0)) * 1.9 / 3.0, 1, 0.5)); color3 = new XSolidBrush(RGB((1 - Math.Min(kQz5, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                if (i == 56) { color1 = new XSolidBrush(RGB((1 - Math.Min(kMy5, 1.0)) * 1.9 / 3.0, 1, 0.5)); color3 = new XSolidBrush(RGB((1 - Math.Min(kMz5, 1.0)) * 1.9 / 3.0, 1, 0.5)); }
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
                Process.Start(filename);
            }
            for (int ind = 0; ind < index.Count; ind++)
            {
                int e = (int)index[ind];
                var ni = (int)spring[e][0].Value; var nj = (int)spring[e][1].Value;
                var r1 = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value); var r2 = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value);
                var rc = (r1 + r2) / 2.0; pts.Add(rc);
                List<GH_Number> klist = new List<GH_Number>();
                var N = spring_f[e][0].Value; var Qy = spring_f[e][1].Value; var Qz = spring_f[e][2].Value; var My = spring_f[e][4].Value; var Mz = spring_f[e][5].Value;
                var Na = spring_a[e][0].Value; var Qya= spring_a[e][2].Value; var Qza = spring_a[e][4].Value; var Mya = spring_a[e][6].Value; var Mza = spring_a[e][7].Value;
                if (N < 0) { Na = spring_a[e][1].Value; }
                if (Qy < 0) { Qya = spring_a[e][3].Value; }
                if (Qz < 0) { Qza = spring_a[e][5].Value; }
                if (Na != 0) { klist.Add(new GH_Number(Math.Abs(N) / Na * 2)); }
                else { klist.Add(new GH_Number(0)); }
                if (Qya != 0) { klist.Add(new GH_Number(Math.Abs(Qy) / Qya * 2)); }
                else { klist.Add(new GH_Number(0)); }
                if (Qza != 0) { klist.Add(new GH_Number(Math.Abs(Qz) / Qza * 2)); }
                else { klist.Add(new GH_Number(0)); }
                if (Mya != 0) { klist.Add(new GH_Number(Math.Abs(My) / Mya * 2)); }
                else { klist.Add(new GH_Number(0)); }
                if (Mza != 0) { klist.Add(new GH_Number(Math.Abs(Mz) / Mza * 2)); }
                else { klist.Add(new GH_Number(0)); }
                kentei.AppendRange(klist, new GH_Path(new int[] { 0, e }));
                if (on_off_11 == 1)
                {
                    if (on_off_21 == 1)
                    {
                        var k = klist[0].Value;
                        var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                        if (k > 0.001) { _text.Add(k.ToString("F").Substring(0, digit)); _p.Add(rc); _c.Add(color); }
                    }
                    else if (on_off_22 == 1)
                    {
                        var ky = klist[1].Value;
                        var kz = klist[2].Value;
                        var k = ky + kz;
                        var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                        if (k > 0.01) { _text.Add(k.ToString("F").Substring(0, digit)); _p.Add(rc); _c.Add(color); }
                    }
                    else if (on_off_23 == 1)
                    {
                        var ky = klist[3].Value;
                        var kz = klist[4].Value;
                        var k = ky + kz;
                        var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                        if (k > 0.01) { _text.Add(k.ToString("F").Substring(0, digit)); _p.Add(rc); _c.Add(color); }
                    }
                    else if (on_off_24 == 1)
                    {
                        //var k = Math.Max(Math.Max(klist[0].Value, klist[1].Value + klist[2].Value), klist[3].Value + klist[4].Value);
                        var k = klist[0].Value + klist[1].Value + klist[2].Value + klist[3].Value + klist[4].Value;
                        var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                        if (k > 0.01) { _text.Add(k.ToString("F").Substring(0, digit)); _p.Add(rc); _c.Add(color); }
                    }
                }
                if (spring_f[0].Count == 18 || spring_f[0].Count == 30)
                {
                    var N_x1 = spring_f[e][6 + 0].Value; var Qy_x1 = spring_f[e][6 + 1].Value; var Qz_x1 = spring_f[e][6 + 2].Value; var My_x1 = spring_f[e][6 + 4].Value; var Mz_x1 = spring_f[e][6 + 5].Value;
                    var N_y1 = spring_f[e][12 + 0].Value; var Qy_y1 = spring_f[e][12 + 1].Value; var Qz_y1 = spring_f[e][12 + 2].Value; var My_y1 = spring_f[e][12 + 4].Value; var Mz_y1 = spring_f[e][12 + 5].Value;
                    var N_x2 = -N_x1; var Qy_x2 = -Qy_x1; var Qz_x2 = -Qz_x1; var My_x2 = -My_x1; var Mz_x2 = -Mz_x1; var N_y2 = -N_y1; var Qy_y2 = -Qy_y1; var Qz_y2 = -Qz_y1; var My_y2 = -My_y1; var Mz_y2 = -Mz_y1;
                    if (spring_f[0].Count == 30)
                    {
                        N_x2 = spring_f[e][12 + 6 + 0].Value; Qy_x2 = spring_f[e][12 + 6 + 1].Value; Qz_x2 = spring_f[e][12 + 6 + 2].Value; My_x2 = spring_f[e][12 + 6 + 4].Value; Mz_x2 = spring_f[e][12 + 6 + 5].Value;
                        N_y2 = spring_f[e][12 + 12 + 0].Value; Qy_y2 = spring_f[e][12 + 12 + 1].Value; Qz_y2 = spring_f[e][12 + 12 + 2].Value; My_y2 = spring_f[e][12 + 12 + 4].Value; Mz_y2 = spring_f[e][12 + 12 + 5].Value;
                    }
                    var k2list = new List<GH_Number>();
                    Na = spring_a[e][0].Value; Qya = spring_a[e][2].Value; Qza = spring_a[e][4].Value;
                    if (N + N_x1 < 0) { Na = spring_a[e][1].Value; }
                    if (Qy + Qy_x1 < 0) { Qya = spring_a[e][3].Value; }
                    if (Qz + Qz_x1 < 0) { Qza = spring_a[e][5].Value; }
                    if (Na != 0) { k2list.Add(new GH_Number(Math.Abs(N+N_x1) / Na)); }
                    else { k2list.Add(new GH_Number(0)); }
                    if (Qya != 0) { k2list.Add(new GH_Number(Math.Abs(Qy+Qy_x1) / Qya)); }
                    else { k2list.Add(new GH_Number(0)); }
                    if (Qza != 0) { k2list.Add(new GH_Number(Math.Abs(Qz+Qz_x1) / Qza)); }
                    else { k2list.Add(new GH_Number(0)); }
                    if (Mya != 0) { k2list.Add(new GH_Number(Math.Abs(My+My_x1) / Mya)); }
                    else { k2list.Add(new GH_Number(0)); }
                    if (Mza != 0) { k2list.Add(new GH_Number(Math.Abs(Mz+Mz_x1) / Mza)); }
                    else { k2list.Add(new GH_Number(0)); }
                    kentei.AppendRange(k2list, new GH_Path(new int[] { 1, e }));
                    var k3list = new List<GH_Number>();
                    Na = spring_a[e][0].Value; Qya = spring_a[e][2].Value; Qza = spring_a[e][4].Value;
                    if (N + N_y1 < 0) { Na = spring_a[e][1].Value; }
                    if (Qy + Qy_y1 < 0) { Qya = spring_a[e][3].Value; }
                    if (Qz + Qz_y1 < 0) { Qza = spring_a[e][5].Value; }
                    if (Na != 0) { k3list.Add(new GH_Number(Math.Abs(N + N_y1) / Na)); }
                    else { k3list.Add(new GH_Number(0)); }
                    if (Qya != 0) { k3list.Add(new GH_Number(Math.Abs(Qy + Qy_y1) / Qya)); }
                    else { k3list.Add(new GH_Number(0)); }
                    if (Qza != 0) { k3list.Add(new GH_Number(Math.Abs(Qz + Qz_y1) / Qza)); }
                    else { k3list.Add(new GH_Number(0)); }
                    if (Mya != 0) { k3list.Add(new GH_Number(Math.Abs(My + My_y1) / Mya)); }
                    else { k3list.Add(new GH_Number(0)); }
                    if (Mza != 0) { k3list.Add(new GH_Number(Math.Abs(Mz + Mz_y1) / Mza)); }
                    else { k3list.Add(new GH_Number(0)); }
                    kentei.AppendRange(k3list, new GH_Path(new int[] { 2, e }));
                    var k4list = new List<GH_Number>();
                    Na = spring_a[e][0].Value; Qya = spring_a[e][2].Value; Qza = spring_a[e][4].Value;
                    if (N + N_x2 < 0) { Na = spring_a[e][1].Value; }
                    if (Qy + Qy_x2 < 0) { Qya = spring_a[e][3].Value; }
                    if (Qz + Qz_x2 < 0) { Qza = spring_a[e][5].Value; }
                    if (Na != 0) { k4list.Add(new GH_Number(Math.Abs(N + N_x2) / Na)); }
                    else { k4list.Add(new GH_Number(0)); }
                    if (Qya != 0) { k4list.Add(new GH_Number(Math.Abs(Qy + Qy_x2) / Qya)); }
                    else { k4list.Add(new GH_Number(0)); }
                    if (Qza != 0) { k4list.Add(new GH_Number(Math.Abs(Qz + Qz_x2) / Qza)); }
                    else { k4list.Add(new GH_Number(0)); }
                    if (Mya != 0) { k4list.Add(new GH_Number(Math.Abs(My + My_x2) / Mya)); }
                    else { k4list.Add(new GH_Number(0)); }
                    if (Mza != 0) { k4list.Add(new GH_Number(Math.Abs(Mz + Mz_x2) / Mza)); }
                    else { k4list.Add(new GH_Number(0)); }
                    kentei.AppendRange(k4list, new GH_Path(new int[] { 3, e }));
                    var k5list = new List<GH_Number>();
                    Na = spring_a[e][0].Value; Qya = spring_a[e][2].Value; Qza = spring_a[e][4].Value;
                    if (N + N_y2 < 0) { Na = spring_a[e][1].Value; }
                    if (Qy + Qy_y2 < 0) { Qya = spring_a[e][3].Value; }
                    if (Qz + Qz_y2 < 0) { Qza = spring_a[e][5].Value; }
                    if (Na != 0) { k5list.Add(new GH_Number(Math.Abs(N + N_y2) / Na)); }
                    else { k5list.Add(new GH_Number(0)); }
                    if (Qya != 0) { k5list.Add(new GH_Number(Math.Abs(Qy + Qy_y2) / Qya)); }
                    else { k5list.Add(new GH_Number(0)); }
                    if (Qza != 0) { k5list.Add(new GH_Number(Math.Abs(Qz + Qz_y2) / Qza)); }
                    else { k5list.Add(new GH_Number(0)); }
                    if (Mya != 0) { k5list.Add(new GH_Number(Math.Abs(My + My_y2) / Mya)); }
                    else { k5list.Add(new GH_Number(0)); }
                    if (Mza != 0) { k5list.Add(new GH_Number(Math.Abs(Mz + Mz_y2) / Mza)); }
                    else { k5list.Add(new GH_Number(0)); }
                    kentei.AppendRange(k5list, new GH_Path(new int[] { 4, e }));

                    if (on_off_12 == 1)
                    {
                        if (on_off_21 == 1)
                        {
                            var k = Math.Max(Math.Max(k2list[0].Value, k3list[0].Value), Math.Max(k4list[0].Value, k5list[0].Value));
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            if (k > 0.01) { _text.Add(k.ToString("F").Substring(0, digit)); _p.Add(rc); _c.Add(color); }
                        }
                        else if (on_off_22 == 1)
                        {
                            var ky2 = k2list[1].Value; var kz2 = k2list[2].Value;
                            var ky3 = k3list[1].Value; var kz3 = k3list[2].Value;
                            var ky4 = k4list[1].Value; var kz4 = k4list[2].Value;
                            var ky5 = k5list[1].Value; var kz5 = k5list[2].Value;
                            var k = Math.Max(Math.Max(ky2 + kz2, ky3 + kz3), Math.Max(ky4 + kz4, ky5 + kz5));
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            if (k > 0.01) { _text.Add(k.ToString("F").Substring(0, digit)); _p.Add(rc); _c.Add(color); }
                        }
                        else if (on_off_23 == 1)
                        {
                            var ky2 = k2list[3].Value; var kz2 = k2list[4].Value;
                            var ky3 = k3list[3].Value; var kz3 = k3list[4].Value;
                            var ky4 = k4list[3].Value; var kz4 = k4list[4].Value;
                            var ky5 = k5list[3].Value; var kz5 = k5list[4].Value;
                            var k = Math.Max(Math.Max(ky2 + kz2, ky3 + kz3), Math.Max(ky4 + kz4, ky5 + kz5));
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            if (k > 0.01) { _text.Add(k.ToString("F").Substring(0, digit)); _p.Add(rc); _c.Add(color); }
                        }
                        else if (on_off_24 == 1)
                        {
                            //var ky2 = k2list[1].Value; var kz2 = k2list[2].Value;
                            //var ky3 = k3list[1].Value; var kz3 = k3list[2].Value;
                            //var ky4 = k4list[1].Value; var kz4 = k4list[2].Value;
                            //var ky5 = k5list[1].Value; var kz5 = k5list[2].Value;
                            //var ky6 = k2list[3].Value; var kz6 = k2list[4].Value;
                            //var ky7 = k3list[3].Value; var kz7 = k3list[4].Value;
                            //var ky8 = k4list[3].Value; var kz8 = k4list[4].Value;
                            //var ky9 = k5list[3].Value; var kz9 = k5list[4].Value;
                            //var k1 = Math.Max(Math.Max(ky2 + kz2, ky3 + kz3), Math.Max(ky4 + kz4, ky5 + kz5));
                            //var k2 = Math.Max(Math.Max(ky6 + kz6, ky7 + kz7), Math.Max(ky8 + kz8, ky9 + kz9));
                            //var k = Math.Max(k1, k2);
                            var k2 = k2list[0].Value + k2list[1].Value + k2list[2].Value + k2list[3].Value + k2list[4].Value;
                            var k3 = k3list[0].Value + k3list[1].Value + k3list[2].Value + k3list[3].Value + k3list[4].Value;
                            var k4 = k4list[0].Value + k4list[1].Value + k4list[2].Value + k4list[3].Value + k4list[4].Value;
                            var k5 = k5list[0].Value + k5list[1].Value + k5list[2].Value + k5list[3].Value + k5list[4].Value;
                            var k = Math.Max(Math.Max(k2, k3), Math.Max(k4, k5));
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            if (k > 0.01) { _text.Add(k.ToString("F").Substring(0, digit)); _p.Add(rc); _c.Add(color); }
                        }
                    }
                }
            }
            DA.SetDataList("pts", pts);
            DA.SetDataTree(0, kentei);
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
                return OpenSeesUtility.Properties.Resources.springcheck;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("a0961368-2869-4e62-8d5e-63459e4ccc99"); }
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
                int radi1 = 7; int radi2 = 4; int titleheight = 20;
                int pitchx = 8; int pitchy = 11; int textheight = 20;
                int width = global_rec.Width;
                title_rec = global_rec;
                title_rec.Y = title_rec.Bottom;
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

                radio_rec3 = radio_rec2;
                radio_rec3.Y = radio_rec2.Y + radio_rec2.Height;
                radio_rec3.Height = textheight;

                radio_rec3_1 = radio_rec3;
                radio_rec3_1.X += 5; radio_rec3_1.Y += 5;
                radio_rec3_1.Height = radi1; radio_rec3_1.Width = radi1;

                text_rec3_1 = radio_rec3_1;
                text_rec3_1.X += pitchx; text_rec3_1.Y -= radi2;
                text_rec3_1.Height = textheight; text_rec3_1.Width = width * 3;

                global_rec.Height += (radio_rec3_1.Bottom - global_rec.Bottom);

                Bounds = global_rec;
            }
            Brush c1 = Brushes.Black; Brush c2 = Brushes.White; Brush c21 = Brushes.White; Brush c22 = Brushes.White; Brush c23 = Brushes.White; Brush c24 = Brushes.White; Brush c3 = Brushes.White;
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
                    graphics.DrawString("Q kentei", GH_FontServer.Standard, Brushes.Black, text_rec2_2);

                    GH_Capsule radio2_3 = GH_Capsule.CreateCapsule(radio_rec2_3, GH_Palette.Black, 5, 5);
                    radio2_3.Render(graphics, Selected, Owner.Locked, false); radio2_3.Dispose();
                    graphics.FillEllipse(c23, radio_rec2_3);
                    graphics.DrawString("M kentei", GH_FontServer.Standard, Brushes.Black, text_rec2_3);

                    GH_Capsule radio2_4 = GH_Capsule.CreateCapsule(radio_rec2_4, GH_Palette.Black, 5, 5);
                    radio2_4.Render(graphics, Selected, Owner.Locked, false); radio2_4.Dispose();
                    graphics.FillEllipse(c24, radio_rec2_4);
                    graphics.DrawString("Sum kentei", GH_FontServer.Standard, Brushes.Black, text_rec2_4);

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
                    RectangleF rec21 = radio_rec2_1; RectangleF rec22 = radio_rec2_2; RectangleF rec23 = radio_rec2_3; RectangleF rec24 = radio_rec2_4; RectangleF rec3 = radio_rec3_1;
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