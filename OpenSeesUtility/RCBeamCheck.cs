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
    public class RCBeamCheck : GH_Component
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
        public RCBeamCheck()
          : base("Allowable stress design for RC beams", "RCBeamCheck",
              "Allowable stress design(danmensantei) for RC beams using Japanese Design Code",
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
            pManager.AddNumberParameter("bar", "bar", "[int,int,...](Datalist)", GH_ParamAccess.list, new List<double> { -9999 });
            pManager.AddNumberParameter("barT1", "barT1", "Steel bars at the top", GH_ParamAccess.tree, -9999);
            pManager.AddNumberParameter("barT2", "barT2", "Steel bars at the second top", GH_ParamAccess.tree, -9999);
            pManager.AddNumberParameter("barB1", "barB1", "Steel bars at the bottom", GH_ParamAccess.tree, -9999);
            pManager.AddNumberParameter("barB2", "barB2", "Steel bars at the second bottom", GH_ParamAccess.tree, -9999);
            pManager.AddTextParameter("name", "name", "name of sections", GH_ParamAccess.list, new List<string> { "" });
            pManager.AddNumberParameter("n", "n", "Premium rate of Q for primary design", GH_ParamAccess.item, 2.0);
            pManager.AddNumberParameter("Standard allowable stress (compression)[N/mm2]", "Fc", "[...](DataList)[N/mm2]", GH_ParamAccess.list, new List<double> { 24.0 });///
            pManager.AddNumberParameter("sectional_force", "sec_f", "[[Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("index", "index", "[...](element No. List to show)", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddNumberParameter("fontsize", "FS", "font size for display texts", GH_ParamAccess.item, 12.0);///
            pManager.AddTextParameter("outputname", "outputname", "output file name", GH_ParamAccess.item, "RcBeamCheck");///
            pManager.AddNumberParameter("P1", "P1", "[■□HL[:B,〇●:R](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///1
            pManager.AddNumberParameter("P2", "P2", "[■□HL[:D,〇:t,●:0](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///2
            pManager[14].Optional = true; pManager[15].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("MaL", "MaL", "[[Mai(top),Mac(top),Maj(top),Mai(bottom),Mac(bottom),Maj(bottom)],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("QaL", "QaL", "[[Qai,Qac,Qaj],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("MaS", "MaS", "[[Mai(top),Mac(top),Maj(top),Mai(bottom),Mac(bottom),Maj(bottom)],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("QaS(L+X)", "QaS(L+X)", "[[Qai,Qac,Qaj],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("QaS(L+Y)", "QaS(L+Y)", "[[Qai,Qac,Qaj],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("QaS(L-X)", "QaS(L-X)", "[[Qai,Qac,Qaj],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("QaS(L-Y)", "QaS(L-Y)", "[[Qai,Qac,Qaj],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("kentei_hi", "kentei", "[[for Myi,for Myj,for Myc, for Qzi,for Qzj,for Qzc],...](DataTree)", GH_ParamAccess.tree);///
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
            DA.GetDataTree("barT1", out GH_Structure<GH_Number> _barT1); var barT1 = _barT1.Branches;
            DA.GetDataTree("barT2", out GH_Structure<GH_Number> _barT2); var barT2 = _barT2.Branches;
            DA.GetDataTree("barB1", out GH_Structure<GH_Number> _barB1); var barB1 = _barB1.Branches;
            DA.GetDataTree("barB2", out GH_Structure<GH_Number> _barB2); var barB2 = _barB2.Branches;
            var barNo = new List<double>(); DA.GetDataList("bar", barNo); var Fc = new List<double>(); DA.GetDataList("Standard allowable stress (compression)[N/mm2]", Fc); var N = 2.0; DA.GetData("n", ref N);
            DA.GetData("fontsize", ref fontsize); var barname = new List<string>(); DA.GetDataList("name", barname);
            var P1 = new List<double>(); DA.GetDataList("P1", P1); var P2 = new List<double>(); DA.GetDataList("P2", P2);
            var kentei = new GH_Structure<GH_Number>(); int digit = 4;
            var unitl = 1.0/1000.0; var unitf = 1.0/1000.0;///単位合わせのための係数
            List<double> index = new List<double>(); DA.GetDataList("index", index);
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
            if (r[0][0].Value != -9999 && ij[0][0].Value != -9999 && sec_f[0][0].Value != -9999 && barT1[0][0].Value != -9999 && barT2[0][0].Value != -9999 && barB1[0][0].Value != -9999 && barB2[0][0].Value != -9999)
            {
                if (barNo[0] == -9999)
                {
                    barNo = new List<double>();
                    for (int e = 0; e < ij.Count; e++)
                    {
                        barNo.Add(0);
                    }
                }
                var MaL = new GH_Structure<GH_Number>(); var QaL = new GH_Structure<GH_Number>(); var n = 15.0;
                var MaS = new GH_Structure<GH_Number>(); var QaLpX = new GH_Structure<GH_Number>(); var QaLpY = new GH_Structure<GH_Number>(); var QaLmX = new GH_Structure<GH_Number>(); var QaLmY = new GH_Structure<GH_Number>();
                var Ftit = new List<List<string>>(); var Ftib = new List<List<string>>();
                var Ftct = new List<List<string>>(); var Ftcb = new List<List<string>>();
                var Ftjt = new List<List<string>>(); var Ftjb = new List<List<string>>();
                var Fsi = new List<List<string>>(); var Fsc = new List<List<string>>(); var Fsj = new List<List<string>>();
                var FC = new List<List<string>>(); var FS = new List<List<string>>();
                var Nod = new List<List<string>>(); var Ele = new List<List<string>>(); var Name = new List<List<string>>(); var Size = new List<List<string>>();
                var Bart = new List<List<string>>(); var Barb = new List<List<string>>(); var Bars = new List<List<string>>();
                var Dt = new List<List<string>>(); var Db = new List<List<string>>();
                var M = new List<List<double>>(); var Q = new List<List<double>>();
                var Mx = new List<List<double>>(); var Qx = new List<List<double>>();
                var My = new List<List<double>>(); var Qy = new List<List<double>>();
                var Mt_aL = new List<List<double>>(); var Mb_aL = new List<List<double>>(); var Mt_aS = new List<List<double>>(); var Mb_aS = new List<List<double>>();
                var Q_aL = new List<List<double>>(); var Q_aLpX = new List<List<double>>(); var Q_aLpY = new List<List<double>>(); var Q_aLmX = new List<List<double>>(); var Q_aLmY = new List<List<double>>();
                for (int ind = 0; ind < index.Count; ind++)
                {
                    int e = (int)index[ind]; int ni = (int)ij[e][0].Value; int nj = (int)ij[e][1].Value; Nod.Add(new List<string> { ni.ToString() + "(i端)", "中央", nj.ToString() + "(j端)" }); Ele.Add(new List<string> { e.ToString() });
                    var f = sec_f[e]; var Mi = f[4].Value; var Mj = f[10].Value; var Mc = f[16].Value; var Qi = f[2].Value; var Qj = f[8].Value; var Qc = f[14].Value;
                    var Mi_x = 0.0; var Mj_x = 0.0; var Mc_x = 0.0; var Qi_x = 0.0; var Qj_x = 0.0; var Qc_x = 0.0; var Mi_y = 0.0; var Mj_y = 0.0; var Mc_y = 0.0; var Qi_y = 0.0; var Qj_y = 0.0; var Qc_y = 0.0;
                    if (sec_f[0].Count / 18 == 3)
                    {
                        Mi_x = f[18 + 4].Value; Mj_x = f[18 + 10].Value; Mc_x = f[18 + 16].Value; Qi_x = f[18 + 2].Value; Qj_x = f[18 + 8].Value; Qc_x = f[18 + 14].Value;
                        Mi_y = f[36 + 4].Value; Mj_y = f[36 + 10].Value; Mc_y = f[36 + 16].Value; Qi_y = f[36 + 2].Value; Qj_y = f[36 + 8].Value; Qc_y = f[36 + 14].Value;
                    }
                    int mat = (int)ij[e][2].Value; int bar = (int)barNo[e]; var T1 = barT1[bar]; var T2 = barT2[bar]; var B1 = barB1[bar]; var B2 = barB2[bar];
                    Name.Add(new List<string> { barname[bar] });
                    var fcL = Fc[mat] / 3.0; var fsL = Math.Min(fcL/10.0,0.49+Fc[mat]/100.0);
                    var fcS= fcL * 2.0; var fsS = fsL * 1.5;
                    FC.Add(new List<string> { fcL.ToString("F10").Substring(0, Math.Max(4, Digit((int)fcL))), "", (fcS).ToString("F10").Substring(0, Math.Max(4,Digit((int)(fcS)))) });
                    FS.Add(new List<string> { fsL.ToString("F10").Substring(0, Math.Max(4, Digit((int)fsL))), "", (fsS).ToString("F10").Substring(0, Math.Max(4, Digit((int)(fsS)))) });
                    var b = T1[16].Value; var D= T1[17].Value;//梁幅,梁せい
                    if (b <= 1e-5 || D <= 1e-5) { D = P1[(int)ij[e][3].Value] * 1000; b = P2[(int)ij[e][3].Value] * 1000; }//梁幅,梁せいの指定がない場合は断面リストの値を採用する
                    if (b <= 1e-5 || D <= 1e-5) { return; }
                    var kT= T1[15].Value; var kB = B1[15].Value;//上下かぶり
                    var n_it1 = T1[0].Value; var n_it2 = T2[0].Value; var D_it1 = T1[1].Value; var D_it2 = T2[1].Value;//i端の1&2段目上端筋本数ならびに主筋径
                    var n_ct1 = T1[2].Value; var n_ct2 = T2[2].Value; var D_ct1 = T1[3].Value; var D_ct2 = T2[3].Value;//中央の1&2段目上端筋本数ならびに主筋径
                    var n_jt1 = T1[4].Value; var n_jt2 = T2[4].Value; var D_jt1 = T1[5].Value; var D_jt2 = T2[5].Value;//j端の1&2段目上端筋本数ならびに主筋径
                    var s_i = T1[6].Value; var S_i = T1[7].Value; var P_i = T1[8].Value;//i端のSTP本数，主筋径，ピッチ
                    var s_c = T1[9].Value; var S_c = T1[10].Value; var P_c = T1[11].Value;//中央のSTP本数，主筋径，ピッチ
                    var s_j = T1[12].Value; var S_j = T1[13].Value; var P_j = T1[14].Value;//j端のSTP本数，主筋径，ピッチ
                    var n_ib1 = B1[0].Value; var n_ib2 = B2[0].Value; var D_ib1 = B1[1].Value; var D_ib2 = B2[1].Value;//i端の1&2段目下端筋本数ならびに主筋径
                    var n_cb1 = B1[2].Value; var n_cb2 = B2[2].Value; var D_cb1 = B1[3].Value; var D_cb2 = B2[3].Value;//中央の1&2段目下端筋本数ならびに主筋径
                    var n_jb1 = B1[4].Value; var n_jb2 = B2[4].Value; var D_jb1 = B1[5].Value; var D_jb2 = B2[5].Value;//j端の1&2段目下端筋本数ならびに主筋径
                    if (D_it2 == 0) { D_it2 = D_it1; }
                    if (D_ib2 == 0) { D_ib2 = D_ib1; }
                    if (D_ct2 == 0) { D_ct2 = D_ct1; }
                    if (D_cb2 == 0) { D_cb2 = D_cb1; }
                    if (D_jt2 == 0) { D_jt2 = D_jt1; }
                    if (D_jb2 == 0) { D_jb2 = D_jb1; }
                    if (D_it1 == 0) { D_it1 = D_it2; }
                    if (D_ib1 == 0) { D_ib1 = D_ib2; }
                    if (D_ct1 == 0) { D_ct1 = D_ct2; }
                    if (D_cb1 == 0) { D_cb1 = D_cb2; }
                    if (D_jt1 == 0) { D_jt1 = D_jt2; }
                    if (D_jb1 == 0) { D_jb1 = D_jb2; }
                    Size.Add(new List<string> { ((b).ToString()).Substring(0, Digit((int)b))+ "x"+ ((D).ToString()).Substring(0, Digit((int)D)) });
                    var bartextit = (n_it1.ToString()).Substring(0, Digit((int)n_it1))+"/"+ (n_it2.ToString()).Substring(0, Digit((int)n_it2));
                    if (n_it2 < 1) { bartextit = (n_it1.ToString()).Substring(0, Digit((int)n_it1)); }
                    bartextit += "-D"+(((D_it1 + D_it2) / 2).ToString()).Substring(0, Digit((int)(D_it1 + D_it2) / 2));
                    var bartextib = (n_ib1.ToString()).Substring(0, Digit((int)n_ib1)) + "/" + (n_ib2.ToString()).Substring(0, Digit((int)n_ib2));
                    if (n_ib2 < 1) { bartextib = (n_ib1.ToString()).Substring(0, Digit((int)n_ib1)); }
                    bartextib += "-D" + (((D_ib1 + D_ib2) / 2).ToString()).Substring(0, Digit((int)(D_ib1 + D_ib2) / 2));
                    var bartextct = (n_ct1.ToString()).Substring(0, Digit((int)n_ct1)) + "/" + (n_ct2.ToString()).Substring(0, Digit((int)n_ct2));
                    if (n_ct2 < 1) { bartextct = (n_ct1.ToString()).Substring(0, Digit((int)n_ct1)); }
                    bartextct += "-D" + (((D_ct1 + D_ct2) / 2).ToString()).Substring(0, Digit((int)(D_ct1 + D_ct2) / 2));
                    var bartextcb = (n_cb1.ToString()).Substring(0, Digit((int)n_cb1)) + "/" + (n_cb2.ToString()).Substring(0, Digit((int)n_cb2));
                    if (n_cb2 < 1) { bartextcb = (n_cb1.ToString()).Substring(0, Digit((int)n_cb1)); }
                    bartextcb += "-D" + (((D_cb1 + D_cb2) / 2).ToString()).Substring(0, Digit((int)(D_cb1 + D_cb2) / 2));
                    var bartextjt = (n_jt1.ToString()).Substring(0, Digit((int)n_jt1)) + "/" + (n_jt2.ToString()).Substring(0, Digit((int)n_jt2));
                    if (n_jt2 < 1) { bartextjt = (n_jt1.ToString()).Substring(0, Digit((int)n_jt1)); }
                    bartextjt += "-D" + (((D_jt1 + D_jt2) / 2).ToString()).Substring(0, Digit((int)(D_jt1 + D_jt2) / 2));
                    var bartextjb = (n_jb1.ToString()).Substring(0, Digit((int)n_jb1)) + "/" + (n_jb2.ToString()).Substring(0, Digit((int)n_jb2));
                    if (n_jb2 < 1) { bartextjb = (n_jb1.ToString()).Substring(0, Digit((int)n_jb1)); }
                    bartextjb += "-D" + (((D_jb1 + D_jb2) / 2).ToString()).Substring(0, Digit((int)(D_jb1 + D_jb2) / 2));
                    Bart.Add(new List<string> { bartextit, bartextct, bartextjt }); Barb.Add(new List<string> { bartextib, bartextcb, bartextjb });
                    var stptexti = (s_i.ToString()).Substring(0, Digit((int)s_i))+"-D"+ (S_i.ToString()).Substring(0, Digit((int)S_i))+"@"+ (P_i.ToString()).Substring(0, Digit((int)P_i));
                    var stptextc = (s_c.ToString()).Substring(0, Digit((int)s_c)) + "-D" + (S_c.ToString()).Substring(0, Digit((int)S_c)) + "@" + (P_c.ToString()).Substring(0, Digit((int)P_c));
                    var stptextj = (s_j.ToString()).Substring(0, Digit((int)s_j)) + "-D" + (S_j.ToString()).Substring(0, Digit((int)S_j)) + "@" + (P_j.ToString()).Substring(0, Digit((int)P_j));
                    Bars.Add(new List<string> { stptexti, stptextc, stptextj });
                    //************************************************************************************************************************************************************
                    //i端の許容曲げモーメントFtit.Add(new List<string> { "", "", "" });
                    //************************************************************************************************************************************************************
                    var a_it1 = n_it1 * Math.Pow(D_it1,2) * Math.PI / 4.0;//i端上端筋1段目主筋断面積
                    var a_it2 = n_it2 * Math.Pow(D_it2,2) * Math.PI / 4.0;//i端上端筋2段目主筋断面積
                    var ft_it1L = 195.0; var ft_it2L = 195.0;
                    if (D_it1 > 18.9 && D_it1 < 28.9) { ft_it1L = 215.0; }//i端上端筋1段目許容引張応力度
                    if (D_it2 > 18.9 && D_it2 < 28.9) { ft_it2L = 215.0; }//i端上端筋2段目許容引張応力度
                    var ft_it1S = 295.0; var ft_it2S = 295.0;
                    if (D_it1 > 18.9 && D_it1 < 28.9) { ft_it1S = 345.0; }//i端上端筋1段目許容引張応力度
                    else if (D_it1 > 28.9) { ft_it1S = 390.0; }
                    if (D_it2 > 18.9 && D_it2 < 28.9) { ft_it2S = 345.0; }//i端上端筋2段目許容引張応力度
                    else if (D_it2 > 28.9) { ft_it2S = 390.0; }
                    Ftit.Add(new List<string> { ft_it1L.ToString().Substring(0, Digit((int)ft_it1L)), "", ft_it1S.ToString().Substring(0, Digit((int)ft_it1S)) });
                    var a_it = a_it1 + a_it2;//i端上端筋主筋断面積
                    var ft_itL = (ft_it1L * a_it1 + ft_it2L * a_it2) / a_it;//i端上端筋主筋許容引張応力度(長期)
                    var ft_itS = (ft_it1S * a_it1 + ft_it2S * a_it2) / a_it;//i端上端筋主筋許容引張応力度(短期)
                    var d_it = ((kT + S_i + D_it1 / 2.0) * a_it1 + (kT + S_i + D_it1 + 25.0 + D_it2 / 2.0) * a_it2) / a_it;//i端の上端より鉄筋重心までの距離

                    var a_ib1 = n_ib1 * Math.Pow(D_ib1,2) * Math.PI / 4.0;//i端下端筋1段目主筋断面積
                    var a_ib2 = n_ib2 * Math.Pow(D_ib2,2) * Math.PI / 4.0;//i端下端筋2段目主筋断面積
                    var ft_ib1L = 195.0; var ft_ib2L = 195.0;
                    if (D_ib1 > 18.9 && D_ib1 < 28.9) { ft_ib1L = 215.0; }//i端上端筋1段目許容引張応力度
                    if (D_ib2 > 18.9 && D_ib2 < 28.9) { ft_ib2L = 215.0; }//i端上端筋2段目許容引張応力度
                    var ft_ib1S = 295.0; var ft_ib2S = 295.0;
                    if (D_ib1 > 18.9 && D_ib1 < 28.9) { ft_ib1S = 345.0; }//i端上端筋1段目許容引張応力度
                    else if (D_ib1 > 28.9) { ft_ib1S = 390.0; }
                    if (D_ib2 > 18.9 && D_ib2 < 28.9) { ft_ib2S = 345.0; }//i端上端筋2段目許容引張応力度
                    else if (D_ib2 > 28.9) { ft_ib2S = 390.0; }
                    Ftib.Add(new List<string> { ft_ib1L.ToString().Substring(0, Digit((int)ft_ib1L)), "", ft_ib1S.ToString().Substring(0, Digit((int)ft_ib1S)) });
                    var a_ib = a_ib1 + a_ib2;//i端下端筋主筋断面積
                    var ft_ibL = (ft_ib1L * a_ib1 + ft_ib2L * a_ib2) / a_ib;//i端上端筋主筋許容引張応力度(長期)
                    var ft_ibS = (ft_ib1S * a_ib1 + ft_ib2S * a_ib2) / a_ib;//i端上端筋主筋許容引張応力度(短期)
                    var d_ib = ((kB + S_i + D_it1 / 2.0) * a_ib1 + (kB + S_i + D_ib1 + 25.0 + D_ib2 / 2.0) * a_ib2) / a_ib;//i端の下端より鉄筋重心までの距離

                    var gamma_it = a_ib / a_it; var D_it = D - d_it; var dc1_it = d_ib / D_it; var pt_it = a_it / (b * D_it);
                    var xn1_it = pt_it * (Math.Sqrt(Math.Pow(n*(1+ gamma_it) - gamma_it, 2)+2.0/ pt_it * (n*(1+ gamma_it * dc1_it) - gamma_it * dc1_it))-(n*(1+ gamma_it) - gamma_it));//RC規準2010 P.123(13.5)
                    var C1_itL = pt_it * fcL / (3 * xn1_it) * (n*(1- xn1_it) *(3- xn1_it) - gamma_it * (n-1)*(xn1_it - dc1_it) *(3* dc1_it - xn1_it));
                    var C2_itL = pt_it * ft_itL / (3 * n * (1 - xn1_it)) * (n * (1 - xn1_it) * (3 - xn1_it) - gamma_it * (n - 1) * (xn1_it - dc1_it) * (3 * dc1_it - xn1_it));
                    var C_itL = Math.Min(C1_itL, C2_itL);
                    var C1_itS = pt_it * fcS / (3 * xn1_it) * (n * (1 - xn1_it) * (3 - xn1_it) - gamma_it * (n - 1) * (xn1_it - dc1_it) * (3 * dc1_it - xn1_it));
                    var C2_itS = pt_it * ft_itS / (3 * n * (1 - xn1_it)) * (n * (1 - xn1_it) * (3 - xn1_it) - gamma_it * (n - 1) * (xn1_it - dc1_it) * (3 * dc1_it - xn1_it));
                    var C_itS = Math.Min(C1_itS, C2_itS);
                    var Ma_itL = C_itL * b * Math.Pow(D_it, 2) * unitl * unitf; var Ma_itS = C_itS * b * Math.Pow(D_it, 2) * unitl * unitf;

                    var gamma_ib = a_it / a_ib; var D_ib = D - d_ib; var dc1_ib = d_it / D_ib; var pt_ib = a_ib / (b * D_ib);
                    var xn1_ib = pt_ib * (Math.Sqrt(Math.Pow(n * (1 + gamma_ib) - gamma_ib, 2) + 2.0 / pt_ib * (n * (1 + gamma_ib * dc1_ib) - gamma_ib * dc1_ib)) - (n * (1 + gamma_ib) - gamma_ib));//RC規準2010 P.123(13.5)
                    var C1_ibL = pt_ib * fcL / (3 * xn1_ib) * (n * (1 - xn1_ib) * (3 - xn1_ib) - gamma_ib * (n - 1) * (xn1_ib - dc1_ib) * (3 * dc1_ib - xn1_ib));
                    var C2_ibL = pt_ib * ft_ibL / (3 * n * (1 - xn1_ib)) * (n * (1 - xn1_ib) * (3 - xn1_ib) - gamma_ib * (n - 1) * (xn1_ib - dc1_ib) * (3 * dc1_ib - xn1_ib));
                    var C_ibL = Math.Min(C1_ibL, C2_ibL);
                    var Ma_ibL = C_ibL * b * Math.Pow(D_ib, 2) * unitl * unitf;
                    var C1_ibS = pt_ib * fcS / (3 * xn1_ib) * (n * (1 - xn1_ib) * (3 - xn1_ib) - gamma_ib * (n - 1) * (xn1_ib - dc1_ib) * (3 * dc1_ib - xn1_ib));
                    var C2_ibS = pt_ib * ft_ibS / (3 * n * (1 - xn1_ib)) * (n * (1 - xn1_ib) * (3 - xn1_ib) - gamma_ib * (n - 1) * (xn1_ib - dc1_ib) * (3 * dc1_ib - xn1_ib));
                    var C_ibS = Math.Min(C1_ibS, C2_ibS);
                    var Ma_ibS = C_ibS * b * Math.Pow(D_ib, 2) * unitl * unitf;
                    //************************************************************************************************************************************************************
                    //中央の許容曲げモーメント
                    //************************************************************************************************************************************************************
                    var a_ct1 = n_ct1 * Math.Pow(D_ct1,2) * Math.PI / 4.0;//i端上端筋1段目主筋断面積
                    var a_ct2 = n_ct2 * Math.Pow(D_ct2,2) * Math.PI / 4.0;//i端上端筋2段目主筋断面積
                    var ft_ct1L = 195.0; var ft_ct2L = 195.0;
                    if (D_ct1 > 18.9 && D_ct1 < 28.9) { ft_ct1L = 215.0; }//i端上端筋1段目許容引張応力度
                    if (D_ct2 > 18.9 && D_ct2 < 28.9) { ft_ct2L = 215.0; }//i端上端筋2段目許容引張応力度
                    var ft_ct1S = 295.0; var ft_ct2S = 295.0;
                    if (D_ct1 > 18.9 && D_ct1 < 28.9) { ft_ct1S = 345.0; }//i端上端筋1段目許容引張応力度
                    else if (D_ct1 > 28.9) { ft_ct1S = 390.0; }
                    if (D_ct2 > 18.9 && D_ct2 < 28.9) { ft_ct2S = 345.0; }//i端上端筋2段目許容引張応力度
                    else if (D_ct2 > 28.9) { ft_ct2S = 390.0; }
                    Ftct.Add(new List<string> { ft_ct1L.ToString().Substring(0, Digit((int)ft_ct1L)), "", ft_ct1S.ToString().Substring(0, Digit((int)ft_ct1S)) });
                    var a_ct = a_ct1 + a_ct2;//i端上端筋主筋断面積
                    var ft_ctL = (ft_ct1L * a_ct1 + ft_ct2L * a_ct2) / a_ct;//中央上端筋主筋許容引張応力度(長期)
                    var ft_ctS = (ft_ct1S * a_ct1 + ft_ct2S * a_ct2) / a_ct;//中央上端筋主筋許容引張応力度(短期)
                    var d_ct = ((kT + S_c + D_ct1 / 2.0) * a_ct1 + (kT + S_c + D_ct1 + 25.0 + D_ct2 / 2.0) * a_ct2) / a_ct;//中央の上端より鉄筋重心までの距離

                    var a_cb1 = n_cb1 * Math.Pow(D_cb1,2) * Math.PI / 4.0;//中央下端筋1段目主筋断面積
                    var a_cb2 = n_cb2 * Math.Pow(D_cb2,2) * Math.PI / 4.0;//中央下端筋2段目主筋断面積
                    var ft_cb1L = 195.0; var ft_cb2L = 195.0;
                    if (D_cb1 > 18.9 && D_cb1 < 28.9) { ft_cb1L = 215.0; }//i端上端筋1段目許容引張応力度
                    if (D_cb2 > 18.9 && D_cb2 < 28.9) { ft_cb2L = 215.0; }//i端上端筋2段目許容引張応力度
                    var ft_cb1S = 295.0; var ft_cb2S = 295.0;
                    if (D_cb1 > 18.9 && D_cb1 < 28.9) { ft_cb1S = 345.0; }//i端上端筋1段目許容引張応力度
                    else if (D_cb1 > 28.9) { ft_cb1S = 390.0; }
                    if (D_cb2 > 18.9 && D_cb2 < 28.9) { ft_cb2S = 345.0; }//i端上端筋2段目許容引張応力度
                    else if (D_cb2 > 28.9) { ft_cb2S = 390.0; }
                    Ftcb.Add(new List<string> { ft_cb1L.ToString().Substring(0, Digit((int)ft_cb1L)), "", ft_cb1S.ToString().Substring(0, Digit((int)ft_cb1S)) });
                    var a_cb = a_cb1 + a_cb2;//中央下端筋主筋断面積
                    var ft_cbL = (ft_cb1L * a_cb1 + ft_cb2L * a_cb2) / a_cb;//中央下端筋主筋許容引張応力度(長期)
                    var ft_cbS = (ft_cb1S * a_cb1 + ft_cb2S * a_cb2) / a_cb;//中央下端筋主筋許容引張応力度(短期)
                    var d_cb = ((kB + S_c + D_ct1 / 2.0) * a_cb1 + (kB + S_c + D_cb1 + 25.0 + D_cb2 / 2.0) * a_cb2) / a_cb;//中央の下端より鉄筋重心までの距離

                    var gamma_ct = a_cb / a_ct; var D_ct = D - d_ct; var dc1_ct = d_cb / D_ct; var pt_ct = a_ct / (b * D_ct);
                    var xn1_ct = pt_ct * (Math.Sqrt(Math.Pow(n * (1 + gamma_ct) - gamma_ct, 2) + 2.0 / pt_ct * (n * (1 + gamma_ct * dc1_ct) - gamma_ct * dc1_ct)) - (n * (1 + gamma_ct) - gamma_ct));//RC規準2010 P.123(13.5)
                    var C1_ctL = pt_ct * fcL / (3 * xn1_ct) * (n * (1 - xn1_ct) * (3 - xn1_ct) - gamma_ct * (n - 1) * (xn1_ct - dc1_ct) * (3 * dc1_ct - xn1_ct));
                    var C2_ctL = pt_ct * ft_ctL / (3 * n * (1 - xn1_ct)) * (n * (1 - xn1_ct) * (3 - xn1_ct) - gamma_ct * (n - 1) * (xn1_ct - dc1_ct) * (3 * dc1_ct - xn1_ct));
                    var C_ctL = Math.Min(C1_ctL, C2_ctL);
                    var C1_ctS = pt_ct * fcS / (3 * xn1_ct) * (n * (1 - xn1_ct) * (3 - xn1_ct) - gamma_ct * (n - 1) * (xn1_ct - dc1_ct) * (3 * dc1_ct - xn1_ct));
                    var C2_ctS = pt_ct * ft_ctS / (3 * n * (1 - xn1_ct)) * (n * (1 - xn1_ct) * (3 - xn1_ct) - gamma_ct * (n - 1) * (xn1_ct - dc1_ct) * (3 * dc1_ct - xn1_ct));
                    var C_ctS = Math.Min(C1_ctS, C2_ctS);
                    var Ma_ctL = C_ctL * b * Math.Pow(D_ct, 2) * unitl * unitf; var Ma_ctS = C_ctS * b * Math.Pow(D_ct, 2) * unitl * unitf;

                    var gamma_cb = a_ct / a_cb; var D_cb = D - d_cb; var dc1_cb = d_ct / D_cb; var pt_cb = a_cb / (b * D_cb);
                    var xn1_cb = pt_cb * (Math.Sqrt(Math.Pow(n * (1 + gamma_cb) - gamma_cb, 2) + 2.0 / pt_cb * (n * (1 + gamma_cb * dc1_cb) - gamma_cb * dc1_cb)) - (n * (1 + gamma_cb) - gamma_cb));//RC規準2010 P.123(13.5)
                    var C1_cbL = pt_cb * fcL / (3 * xn1_cb) * (n * (1 - xn1_cb) * (3 - xn1_cb) - gamma_cb * (n - 1) * (xn1_cb - dc1_cb) * (3 * dc1_cb - xn1_cb));
                    var C2_cbL = pt_cb * ft_cbL / (3 * n * (1 - xn1_cb)) * (n * (1 - xn1_cb) * (3 - xn1_cb) - gamma_cb * (n - 1) * (xn1_cb - dc1_cb) * (3 * dc1_cb - xn1_cb));
                    var C_cbL = Math.Min(C1_cbL, C2_cbL);
                    var Ma_cbL = C_cbL * b * Math.Pow(D_cb, 2) * unitl * unitf;
                    var C1_cbS = pt_cb * fcS / (3 * xn1_cb) * (n * (1 - xn1_cb) * (3 - xn1_cb) - gamma_cb * (n - 1) * (xn1_cb - dc1_cb) * (3 * dc1_cb - xn1_cb));
                    var C2_cbS = pt_cb * ft_cbS / (3 * n * (1 - xn1_cb)) * (n * (1 - xn1_cb) * (3 - xn1_cb) - gamma_cb * (n - 1) * (xn1_cb - dc1_cb) * (3 * dc1_cb - xn1_cb));
                    var C_cbS = Math.Min(C1_cbS, C2_cbS);
                    var Ma_cbS = C_cbS * b * Math.Pow(D_cb, 2) * unitl * unitf;
                    //************************************************************************************************************************************************************
                    //j端の許容曲げモーメント
                    //************************************************************************************************************************************************************
                    var a_jt1 = n_jt1 * Math.Pow(D_jt1,2) * Math.PI / 4.0;//j端上端筋1段目主筋断面積
                    var a_jt2 = n_jt2 * Math.Pow(D_jt2,2) * Math.PI / 4.0;//j端上端筋2段目主筋断面積
                    var ft_jt1L = 195.0; var ft_jt2L = 195.0;
                    if (D_jt1 > 18.9 && D_jt1 < 28.9) { ft_jt1L = 215.0; }//i端上端筋1段目許容引張応力度
                    if (D_jt2 > 18.9 && D_jt2 < 28.9) { ft_jt2L = 215.0; }//i端上端筋2段目許容引張応力度
                    var ft_jt1S = 295.0; var ft_jt2S = 295.0;
                    if (D_jt1 > 18.9 && D_jt1 < 28.9) { ft_jt1S = 345.0; }//i端上端筋1段目許容引張応力度
                    else if (D_jt1 > 28.9) { ft_jt1S = 390.0; }
                    if (D_jt2 > 18.9 && D_jt2 < 28.9) { ft_jt2S = 345.0; }//i端上端筋2段目許容引張応力度
                    else if (D_jt2 > 28.9) { ft_jt2S = 390.0; }
                    Ftjt.Add(new List<string> { ft_jt1L.ToString().Substring(0, Digit((int)ft_jt1L)), "", ft_jt1S.ToString().Substring(0, Digit((int)ft_jt1S)) });
                    var a_jt = a_jt1 + a_jt2;//j端上端筋主筋断面積
                    var ft_jtL = (ft_jt1L * a_jt1 + ft_jt2L * a_jt2) / a_jt;//j端上端筋主筋許容引張応力度(長期)
                    var ft_jtS = (ft_jt1S * a_jt1 + ft_jt2S * a_jt2) / a_jt;//j端上端筋主筋許容引張応力度(短期)
                    var d_jt = ((kT + S_j + D_jt1 / 2.0) * a_jt1 + (kT + S_j + D_jt1 + 25.0 + D_jt2 / 2.0) * a_jt2) / a_jt;//i端の上端より鉄筋重心までの距離

                    var a_jb1 = n_jb1 * Math.Pow(D_jb1,2) * Math.PI / 4.0;//j端下端筋1段目主筋断面積
                    var a_jb2 = n_jb2 * Math.Pow(D_jb2,2) * Math.PI / 4.0;//j端下端筋2段目主筋断面積
                    var ft_jb1L = 195.0; var ft_jb2L = 195.0;
                    if (D_jb1 > 18.9 && D_jb1 < 28.9) { ft_jb1L = 215.0; }//i端上端筋1段目許容引張応力度
                    if (D_jb2 > 18.9 && D_jb2 < 28.9) { ft_jb2L = 215.0; }//i端上端筋2段目許容引張応力度
                    var ft_jb1S = 295.0; var ft_jb2S = 295.0;
                    if (D_jb1 > 18.9 && D_jb1 < 28.9) { ft_jb1S = 345.0; }//i端上端筋1段目許容引張応力度
                    else if (D_jb1 > 28.9) { ft_jb1S = 390.0; }
                    if (D_jb2 > 18.9 && D_jb2 < 28.9) { ft_jb2S = 345.0; }//i端上端筋2段目許容引張応力度
                    else if (D_jb2 > 28.9) { ft_jb2S = 390.0; }
                    Ftjb.Add(new List<string> { ft_jb1L.ToString().Substring(0, Digit((int)ft_jb1L)), "", ft_jb1S.ToString().Substring(0, Digit((int)ft_jb1S)) });
                    var a_jb = a_jb1 + a_jb2;//j端下端筋主筋断面積
                    var ft_jbL = (ft_jb1L * a_jb1 + ft_jb2L * a_jb2) / a_jb;//j端下端筋主筋許容引張応力度(長期)
                    var ft_jbS = (ft_jb1S * a_jb1 + ft_jb2S * a_jb2) / a_jb;//j端下端筋主筋許容引張応力度(短期)
                    var d_jb = ((kB + S_j + D_jt1 / 2.0) * a_jb1 + (kB + S_j + D_jb1 + 25.0 + D_jb2 / 2.0) * a_jb2) / a_jb;//j端の下端より鉄筋重心までの距離

                    var gamma_jt = a_jb / a_jt; var D_jt = D - d_jt; var dc1_jt = d_jb / D_jt; var pt_jt = a_jt / (b * D_jt);
                    var xn1_jt = pt_jt * (Math.Sqrt(Math.Pow(n * (1 + gamma_jt) - gamma_jt, 2) + 2.0 / pt_jt * (n * (1 + gamma_jt * dc1_jt) - gamma_jt * dc1_jt)) - (n * (1 + gamma_jt) - gamma_jt));//RC規準2010 P.123(13.5)
                    var C1_jtL = pt_jt * fcL / (3 * xn1_jt) * (n * (1 - xn1_jt) * (3 - xn1_jt) - gamma_jt * (n - 1) * (xn1_jt - dc1_jt) * (3 * dc1_jt - xn1_jt));
                    var C2_jtL = pt_jt * ft_jtL / (3 * n * (1 - xn1_jt)) * (n * (1 - xn1_jt) * (3 - xn1_jt) - gamma_jt * (n - 1) * (xn1_jt - dc1_jt) * (3 * dc1_jt - xn1_jt));
                    var C_jtL = Math.Min(C1_jtL, C2_jtL);
                    var C1_jtS = pt_jt * fcS / (3 * xn1_jt) * (n * (1 - xn1_jt) * (3 - xn1_jt) - gamma_jt * (n - 1) * (xn1_jt - dc1_jt) * (3 * dc1_jt - xn1_jt));
                    var C2_jtS = pt_jt * ft_jtS / (3 * n * (1 - xn1_jt)) * (n * (1 - xn1_jt) * (3 - xn1_jt) - gamma_jt * (n - 1) * (xn1_jt - dc1_jt) * (3 * dc1_jt - xn1_jt));
                    var C_jtS = Math.Min(C1_jtS, C2_jtS);
                    var Ma_jtL = C_jtL * b * Math.Pow(D_jt, 2) * unitl * unitf; var Ma_jtS = C_jtS * b * Math.Pow(D_jt, 2) * unitl * unitf;

                    var gamma_jb = a_jt / a_jb; var D_jb = D - d_jb; var dc1_jb = d_jt / D_jb; var pt_jb = a_jb / (b * D_jb);
                    var xn1_jb = pt_jb * (Math.Sqrt(Math.Pow(n * (1 + gamma_jb) - gamma_jb, 2) + 2.0 / pt_jb * (n * (1 + gamma_jb * dc1_jb) - gamma_jb * dc1_jb)) - (n * (1 + gamma_jb) - gamma_jb));//RC規準2010 P.123(13.5)
                    var C1_jbL = pt_jb * fcL / (3 * xn1_jb) * (n * (1 - xn1_jb) * (3 - xn1_jb) - gamma_jb * (n - 1) * (xn1_jb - dc1_jb) * (3 * dc1_jb - xn1_jb));
                    var C2_jbL = pt_jb * ft_jbL / (3 * n * (1 - xn1_jb)) * (n * (1 - xn1_jb) * (3 - xn1_jb) - gamma_jb * (n - 1) * (xn1_jb - dc1_jb) * (3 * dc1_jb - xn1_jb));
                    var C_jbL = Math.Min(C1_jbL, C2_jbL);
                    var C1_jbS = pt_jb * fcS / (3 * xn1_jb) * (n * (1 - xn1_jb) * (3 - xn1_jb) - gamma_jb * (n - 1) * (xn1_jb - dc1_jb) * (3 * dc1_jb - xn1_jb));
                    var C2_jbS = pt_jb * ft_jbS / (3 * n * (1 - xn1_jb)) * (n * (1 - xn1_jb) * (3 - xn1_jb) - gamma_jb * (n - 1) * (xn1_jb - dc1_jb) * (3 * dc1_jb - xn1_jb));
                    var C_jbS = Math.Min(C1_jbS, C2_jbS);
                    var Ma_jbL = C_jbL * b * Math.Pow(D_jb, 2) * unitl * unitf; var Ma_jbS = C_jbS * b * Math.Pow(D_jb, 2) * unitl * unitf;
                    //************************************************************************************************************************************************************
                    //i端の許容せん断力
                    //************************************************************************************************************************************************************
                    var d_i = D - (d_it + d_ib) / 2.0; var j_i = d_i * 7.0 / 8.0;
                    var alpha_iL = Math.Max(Math.Min(4.0 / (Math.Abs(Mi * 1e+6) / (Math.Abs(Qi * 1e+3) * d_i) + 1.0), 2), 1);
                    var alpha_iLpX = Math.Max(Math.Min(4.0 / (Math.Abs((Mi + Mi_x) * 1e+6) / (Math.Abs((Qi + Qi_x) * 1e+3) * d_i) + 1.0), 2), 1);
                    var alpha_iLpY = Math.Max(Math.Min(4.0 / (Math.Abs((Mi + Mi_y) * 1e+6) / (Math.Abs((Qi + Qi_y) * 1e+3) * d_i) + 1.0), 2), 1);
                    var alpha_iLmX = Math.Max(Math.Min(4.0 / (Math.Abs((Mi - Mi_x) * 1e+6) / (Math.Abs((Qi - Qi_x) * 1e+3) * d_i) + 1.0), 2), 1);
                    var alpha_iLmY = Math.Max(Math.Min(4.0 / (Math.Abs((Mi - Mi_y) * 1e+6) / (Math.Abs((Qi - Qi_y) * 1e+3) * d_i) + 1.0), 2), 1);
                    var wft_iL = 195.0; var wft_iS = 295.0;
                    if (S_i > 18.9 && S_i < 28.9) { wft_iL = 215.0; }
                    if (S_i > 18.9 && S_i < 28.9) { wft_iS = 345.0; }
                    else if (S_i > 28.9) { wft_iS = 390.0; }
                    Fsi.Add(new List<string> { wft_iL.ToString().Substring(0, Digit((int)wft_iL)), "", wft_iS.ToString().Substring(0, Digit((int)wft_iS)) });
                    var aw_i = s_i * Math.Pow(S_i, 2) * Math.PI / 4.0;
                    var pw_i = aw_i / (b * P_i);
                    var Qa_iL= (b * j_i * (alpha_iL * fsL + 0.5 * wft_iL * (Math.Min(pw_i, 0.006) - 0.002))) * unitf;
                    var Qa_iLpX = (b * j_i * (2.0 / 3.0 * alpha_iLpX * fsS + 0.5 * wft_iS * (Math.Min(pw_i, 0.012) - 0.002))) * unitf;
                    var Qa_iLpY = (b * j_i * (2.0 / 3.0 * alpha_iLpY * fsS + 0.5 * wft_iS * (Math.Min(pw_i, 0.012) - 0.002))) * unitf;
                    var Qa_iLmX = (b * j_i * (2.0 / 3.0 * alpha_iLmX * fsS + 0.5 * wft_iS * (Math.Min(pw_i, 0.012) - 0.002))) * unitf;
                    var Qa_iLmY = (b * j_i * (2.0 / 3.0 * alpha_iLmY * fsS + 0.5 * wft_iS * (Math.Min(pw_i, 0.012) - 0.002))) * unitf;
                    //************************************************************************************************************************************************************
                    //中央の許容せん断力
                    //************************************************************************************************************************************************************
                    var d_c = D - (d_ct + d_cb) / 2.0; var j_c = d_c * 7.0 / 8.0;
                    var alpha_cL = Math.Max(Math.Min(4.0 / (Math.Abs(Mc * 1e+6) / (Math.Abs(Qc * 1e+3) * d_c) + 1.0), 2), 1);
                    var alpha_cLpX = Math.Max(Math.Min(4.0 / (Math.Abs((Mc + Mc_x) * 1e+6) / (Math.Abs((Qc + Qc_x) * 1e+3) * d_c) + 1.0), 2), 1);
                    var alpha_cLpY = Math.Max(Math.Min(4.0 / (Math.Abs((Mc + Mc_y) * 1e+6) / (Math.Abs((Qc + Qc_y) * 1e+3) * d_c) + 1.0), 2), 1);
                    var alpha_cLmX = Math.Max(Math.Min(4.0 / (Math.Abs((Mc - Mc_x) * 1e+6) / (Math.Abs((Qc - Qc_x) * 1e+3) * d_c) + 1.0), 2), 1);
                    var alpha_cLmY = Math.Max(Math.Min(4.0 / (Math.Abs((Mc - Mc_y) * 1e+6) / (Math.Abs((Qc - Qc_y) * 1e+3) * d_c) + 1.0), 2), 1);
                    var wft_cL = 195.0; var wft_cS = 295.0;
                    if (S_c > 18.9 && S_c < 28.9) { wft_cL = 215.0; }
                    if (S_c > 18.9 && S_c < 28.9) { wft_cS = 345.0; }
                    else if (S_c > 28.9) { wft_cS = 390.0; }
                    Fsc.Add(new List<string> { wft_cL.ToString().Substring(0, Digit((int)wft_cL)), "", wft_cS.ToString().Substring(0, Digit((int)wft_cS)) });
                    var aw_c = s_c * Math.Pow(S_c, 2) * Math.PI / 4.0;
                    var pw_c = aw_c / (b * P_c);
                    var Qa_cL = (b * j_c * (alpha_cL * fsL + 0.5 * wft_cL * (Math.Min(pw_c, 0.006) - 0.002))) * unitf;
                    var Qa_cLpX = (b * j_c * (2.0 / 3.0 * alpha_cLpX * fsS + 0.5 * wft_cS * (Math.Min(pw_c, 0.012) - 0.002))) * unitf;
                    var Qa_cLpY = (b * j_c * (2.0 / 3.0 * alpha_cLpY * fsS + 0.5 * wft_cS * (Math.Min(pw_c, 0.012) - 0.002))) * unitf;
                    var Qa_cLmX = (b * j_c * (2.0 / 3.0 * alpha_cLmX * fsS + 0.5 * wft_cS * (Math.Min(pw_c, 0.012) - 0.002))) * unitf;
                    var Qa_cLmY = (b * j_c * (2.0 / 3.0 * alpha_cLmY * fsS + 0.5 * wft_cS * (Math.Min(pw_c, 0.012) - 0.002))) * unitf;
                    //************************************************************************************************************************************************************
                    //j端の許容せん断力
                    //************************************************************************************************************************************************************
                    var d_j = D - (d_jt + d_jb) / 2.0; var j_j = d_j * 7.0 / 8.0;
                    var alpha_jL = Math.Max(Math.Min(4.0 / (Math.Abs(Mj * 1e+6) / (Math.Abs(Qj * 1e+3) * d_j) + 1.0), 2), 1);
                    var alpha_jLpX = Math.Max(Math.Min(4.0 / (Math.Abs((Mj + Mj_x) * 1e+6) / (Math.Abs((Qj + Qj_x) * 1e+3) * d_j) + 1.0), 2), 1);
                    var alpha_jLpY = Math.Max(Math.Min(4.0 / (Math.Abs((Mj + Mj_y) * 1e+6) / (Math.Abs((Qj + Qj_y) * 1e+3) * d_j) + 1.0), 2), 1);
                    var alpha_jLmX = Math.Max(Math.Min(4.0 / (Math.Abs((Mj - Mj_x) * 1e+6) / (Math.Abs((Qj - Qj_x) * 1e+3) * d_j) + 1.0), 2), 1);
                    var alpha_jLmY = Math.Max(Math.Min(4.0 / (Math.Abs((Mj - Mj_y) * 1e+6) / (Math.Abs((Qj - Qj_y) * 1e+3) * d_j) + 1.0), 2), 1);
                    var wft_jL = 195.0; var wft_jS = 295.0;
                    if (S_j > 18.9 && S_j < 28.9) { wft_jL = 215.0; }
                    if (S_j > 18.9 && S_j < 28.9) { wft_jS = 345.0; }
                    else if (S_j > 28.9) { wft_jS = 390.0; }
                    Fsj.Add(new List<string> { wft_jL.ToString().Substring(0, Digit((int)wft_jL)), "", wft_jS.ToString().Substring(0, Digit((int)wft_jS)) });
                    var aw_j = s_j * Math.Pow(S_j, 2) * Math.PI / 4.0;
                    var pw_j = aw_j / (b * P_j);
                    var Qa_jL = (b * j_j * (alpha_jL * fsL + 0.5 * wft_jL * (Math.Min(pw_j, 0.006) - 0.002))) * unitf;
                    var Qa_jLpX = (b * j_j * (2.0 / 3.0 * alpha_jLpX * fsS + 0.5 * wft_jS * (Math.Min(pw_j, 0.012) - 0.002))) * unitf;
                    var Qa_jLpY = (b * j_j * (2.0 / 3.0 * alpha_jLpY * fsS + 0.5 * wft_jS * (Math.Min(pw_j, 0.012) - 0.002))) * unitf;
                    var Qa_jLmX = (b * j_j * (2.0 / 3.0 * alpha_jLmX * fsS + 0.5 * wft_jS * (Math.Min(pw_j, 0.012) - 0.002))) * unitf;
                    var Qa_jLmY = (b * j_j * (2.0 / 3.0 * alpha_jLmY * fsS + 0.5 * wft_jS * (Math.Min(pw_j, 0.012) - 0.002))) * unitf;
                    //************************************************************************************************************************************************************
                    Dt.Add(new List<string> { (d_it.ToString()).Substring(0, Digit((int)d_it)), (d_ct.ToString()).Substring(0, Digit((int)d_ct)), (d_jt.ToString()).Substring(0, Digit((int)d_jt)) });
                    Db.Add(new List<string> { (d_ib.ToString()).Substring(0, Digit((int)d_ib)), (d_cb.ToString()).Substring(0, Digit((int)d_cb)), (d_jb.ToString()).Substring(0, Digit((int)d_jb)) });
                    Mt_aL.Add(new List<double> { Ma_itL, Ma_ctL, Ma_jtL }); Mb_aL.Add(new List<double> { Ma_ibL, Ma_cbL, Ma_jbL });
                    Mt_aS.Add(new List<double> { Ma_itS, Ma_ctS, Ma_jtS }); Mb_aS.Add(new List<double> { Ma_ibS, Ma_cbS, Ma_jbS });
                    Q_aL.Add(new List<double> { Qa_iL, Qa_cL, Qa_jL }); Q_aLpX.Add(new List<double> { Qa_iLpX, Qa_cLpX, Qa_jLpX }); Q_aLpY.Add(new List<double> { Qa_iLpY, Qa_cLpY, Qa_jLpY }); Q_aLmX.Add(new List<double> { Qa_iLmX, Qa_cLmX, Qa_jLmX }); Q_aLmY.Add(new List<double> { Qa_iLmY, Qa_cLmY, Qa_jLmY });
                    M.Add(new List<double> { Mi, Mc, Mj }); Q.Add(new List<double> { Qi, Qc, Qj });
                    Mx.Add(new List<double> { Mi_x, Mc_x, Mj_x }); Qx.Add(new List<double> { Qi_x, Qc_x, Qj_x });
                    My.Add(new List<double> { Mi_y, Mc_y, Mj_y }); Qy.Add(new List<double> { Qi_y, Qc_y, Qj_y });
                    //************************************************************************************************************************************************************
                    List<GH_Number> MLlist = new List<GH_Number>(); List<GH_Number> MSlist = new List<GH_Number>();
                    MLlist.Add(new GH_Number(Ma_itL)); MLlist.Add(new GH_Number(Ma_ctL)); MLlist.Add(new GH_Number(Ma_jtL)); MLlist.Add(new GH_Number(Ma_ibL)); MLlist.Add(new GH_Number(Ma_cbL)); MLlist.Add(new GH_Number(Ma_jbL));
                    MSlist.Add(new GH_Number(Ma_itS)); MSlist.Add(new GH_Number(Ma_ctS)); MSlist.Add(new GH_Number(Ma_jtS)); MSlist.Add(new GH_Number(Ma_ibS)); MSlist.Add(new GH_Number(Ma_cbS)); MSlist.Add(new GH_Number(Ma_jbS));
                    MaL.AppendRange(MLlist, new GH_Path(e)); MaS.AppendRange(MSlist, new GH_Path(e));
                    List<GH_Number> QLlist = new List<GH_Number>(); List<GH_Number> QLpXlist = new List<GH_Number>(); List<GH_Number> QLpYlist = new List<GH_Number>(); List<GH_Number> QLmXlist = new List<GH_Number>(); List<GH_Number> QLmYlist = new List<GH_Number>();
                    QLlist.Add(new GH_Number(Qa_iL)); QLlist.Add(new GH_Number(Qa_cL)); QLlist.Add(new GH_Number(Qa_jL));
                    QaL.AppendRange(QLlist, new GH_Path(e));
                    QLpXlist.Add(new GH_Number(Qa_iLpX)); QLpXlist.Add(new GH_Number(Qa_cLpX)); QLpXlist.Add(new GH_Number(Qa_jLpX));
                    QaLpX.AppendRange(QLpXlist, new GH_Path(e));
                    QLpYlist.Add(new GH_Number(Qa_iLpY)); QLpYlist.Add(new GH_Number(Qa_cLpY)); QLpYlist.Add(new GH_Number(Qa_jLpY));
                    QaLpY.AppendRange(QLpYlist, new GH_Path(e));
                    QLmXlist.Add(new GH_Number(Qa_iLmX)); QLmXlist.Add(new GH_Number(Qa_cLmX)); QLmXlist.Add(new GH_Number(Qa_jLmX));
                    QaLmX.AppendRange(QLmXlist, new GH_Path(e));
                    QLmYlist.Add(new GH_Number(Qa_iLmY)); QLmYlist.Add(new GH_Number(Qa_cLmY)); QLmYlist.Add(new GH_Number(Qa_jLmY));
                    QaLmY.AppendRange(QLmYlist, new GH_Path(e));
                    List<GH_Number> klist = new List<GH_Number>(); List<GH_Number> k2list = new List<GH_Number>();
                    if (Mi < 0) { klist.Add(new GH_Number(Math.Abs(Mi) / Ma_itL)); }
                    else { klist.Add(new GH_Number(Math.Abs(Mi) / Ma_ibL)); }
                    if (Mj > 0) { klist.Add(new GH_Number(Math.Abs(Mj) / Ma_jtL)); }
                    else { klist.Add(new GH_Number(Math.Abs(Mj) / Ma_jbL)); }
                    if (Mc < 0) { klist.Add(new GH_Number(Math.Abs(Mc) / Ma_ctL)); }
                    else { klist.Add(new GH_Number(Math.Abs(Mc) / Ma_cbL)); }
                    klist.Add(new GH_Number(Math.Abs(Qi) / Qa_iL)); klist.Add(new GH_Number(Math.Abs(Qj) / Qa_jL)); klist.Add(new GH_Number(Math.Abs(Qc) / Qa_cL));
                    kentei.AppendRange(klist, new GH_Path(e));

                    var ki = new List<double>(); var kj = new List<double>(); var kc = new List<double>();
                    if (Mi + Mi_x < 0) { ki.Add(Math.Abs(Mi + Mi_x) / Ma_itS); }
                    else { ki.Add(Math.Abs(Mi + Mi_x) / Ma_ibS); }
                    if (Mi - Mi_x < 0) { ki.Add(Math.Abs(Mi - Mi_x) / Ma_itS); }
                    else { ki.Add(Math.Abs(Mi - Mi_x) / Ma_ibS); }
                    if (Mi + Mi_y < 0) { ki.Add(Math.Abs(Mi + Mi_y) / Ma_itS); }
                    else { ki.Add(Math.Abs(Mi + Mi_y) / Ma_ibS); }
                    if (Mi - Mi_y < 0) { ki.Add(Math.Abs(Mi - Mi_y) / Ma_itS); }
                    else { ki.Add(Math.Abs(Mi - Mi_y) / Ma_ibS); }
                    if (Mj + Mj_x < 0) { kj.Add(Math.Abs(Mj + Mj_x) / Ma_jtS); }
                    else { kj.Add(Math.Abs(Mj + Mj_x) / Ma_jbS); }
                    if (Mj - Mj_x < 0) { kj.Add(Math.Abs(Mj - Mj_x) / Ma_jtS); }
                    else { kj.Add(Math.Abs(Mj - Mj_x) / Ma_jbS); }
                    if (Mj + Mj_y < 0) { kj.Add(Math.Abs(Mj + Mj_y) / Ma_jtS); }
                    else { kj.Add(Math.Abs(Mj + Mj_y) / Ma_jbS); }
                    if (Mj - Mj_y < 0) { kj.Add(Math.Abs(Mj - Mj_y) / Ma_jtS); }
                    else { kj.Add(Math.Abs(Mj - Mj_y) / Ma_jbS); }
                    if (Mc + Mc_x < 0) { kc.Add(Math.Abs(Mc + Mc_x) / Ma_ctS); }
                    else { kc.Add(Math.Abs(Mc + Mc_x) / Ma_cbS); }
                    if (Mc - Mc_x < 0) { kc.Add(Math.Abs(Mc - Mc_x) / Ma_ctS); }
                    else { kc.Add(Math.Abs(Mc - Mc_x) / Ma_cbS); }
                    if (Mc + Mc_y < 0) { kc.Add(Math.Abs(Mc + Mc_y) / Ma_ctS); }
                    else { kc.Add(Math.Abs(Mc + Mc_y) / Ma_cbS); }
                    if (Mc - Mc_y < 0) { kc.Add(Math.Abs(Mc - Mc_y) / Ma_ctS); }
                    else { kc.Add(Math.Abs(Mc - Mc_y) / Ma_cbS); }
                    k2list.Add(new GH_Number(Math.Max(Math.Max(ki[0],ki[1]),Math.Max(ki[2],ki[3]))));
                    k2list.Add(new GH_Number(Math.Max(Math.Max(kj[0], kj[1]), Math.Max(kj[2], kj[3]))));
                    k2list.Add(new GH_Number(Math.Max(Math.Max(kc[0], kc[1]), Math.Max(kc[2], kc[3]))));
                    k2list.Add(new GH_Number(Math.Max(Math.Max(Math.Abs(Qi+Qi_x * N) / Qa_iLpX, Math.Abs(Qi - Qi_x * N) / Qa_iLmX), Math.Max(Math.Abs(Qi + Qi_y * N) / Qa_iLpY, Math.Abs(Qi - Qi_y * N) / Qa_iLpY))));
                    k2list.Add(new GH_Number(Math.Max(Math.Max(Math.Abs(Qj + Qj_x * N) / Qa_jLpX, Math.Abs(Qj - Qj_x * N) / Qa_jLmX), Math.Max(Math.Abs(Qj + Qj_y * N) / Qa_jLpY, Math.Abs(Qj - Qj_y * N) / Qa_jLpY))));
                    k2list.Add(new GH_Number(Math.Max(Math.Max(Math.Abs(Qc + Qc_x * N) / Qa_cLpX, Math.Abs(Qc - Qc_x * N) / Qa_cLmX), Math.Max(Math.Abs(Qc + Qc_y * N) / Qa_cLpY, Math.Abs(Qc - Qc_y * N) / Qa_cLpY))));


                    var r1 = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value); var r2 = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value);
                    var rc = (r1 + r2) / 2.0; var ri = (r1 + rc) / 2.0; var rj = (r2 + rc) / 2.0;
                    if (on_off_11 == 1)
                    {
                        if (on_off_21 == 1)
                        {
                            var k = klist[0].Value;
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(ri);
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = klist[1].Value;
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rj);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = klist[2].Value;
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rc);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                        }
                        else if (on_off_22 == 1)
                        {
                            var k = klist[3].Value;
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(ri);
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = klist[4].Value;
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rj);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = klist[5].Value;
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rc);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                        }
                        else if (on_off_23 == 1)
                        {
                            var k = Math.Max(klist[0].Value, klist[3].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(ri);
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = Math.Max(klist[1].Value, klist[4].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rj);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = Math.Max(klist[2].Value, klist[5].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rc);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                        }
                    }
                    else if (on_off_12 == 1)
                    {
                        if (on_off_21 == 1)
                        {
                            var k = k2list[0].Value;
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(ri);
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = k2list[1].Value;
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rj);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = k2list[2].Value;
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rc);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                        }
                        else if (on_off_22 == 1)
                        {
                            var k = k2list[3].Value;
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(ri);
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = k2list[4].Value;
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rj);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = k2list[5].Value;
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rc);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                        }
                        else if (on_off_23 == 1)
                        {
                            var k = Math.Max(k2list[0].Value, k2list[3].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(ri);
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = Math.Max(k2list[1].Value, k2list[4].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rj);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = Math.Max(k2list[2].Value, k2list[5].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rc);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                        }
                    }
                }
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
                            "部材番号","配筋符号","断面算定用bxD[mm]"," ","i端上端ft[N/mm2]","i端下端ft[N/mm2]","中央上端ft[N/mm2]","中央下端ft[N/mm2]","j端上端ft[N/mm2]", "j端下端ft[N/mm2]", "i端fs[N/mm2]", "中央fs[N/mm2]","j端fs[N/mm2]", "コンクリートfc[N/mm2]","コンクリートfs[N/mm2]", "節点番号","上端主筋", "上端-鉄筋重心距離dt[mm]","下端主筋","下端-鉄筋重心距離dt[mm]","せん断補強筋"," ","上端M[kNm]","下端M[kNm]","上端Ma[kNm]","下端Ma[kNm]","曲げ検定比M/Ma", "Q[kN]","Qa[kN]","せん断検定比Q/Qa","判定"
                        };
                        var label_width = 100; var offset_x = 25; var offset_y = 25; var pitchy = 13; var text_width = 45; PdfPage page = new PdfPage(); page.Size = PageSize.A4;
                        for (int ind = 0; ind < index.Count; ind++)//
                        {
                            int e = (int)index[ind];
                            var values = new List<List<string>>();
                            values.Add(Ele[ind]); values.Add(Name[ind]); values.Add(Size[ind]); values.Add(new List<string> { "長期","","短期"});
                            values.Add(Ftit[ind]); values.Add(Ftib[ind]); values.Add(Ftct[ind]); values.Add(Ftcb[ind]); values.Add(Ftjt[ind]); values.Add(Ftjb[ind]);
                            values.Add(Fsi[ind]); values.Add(Fsc[ind]); values.Add(Fsj[ind]);
                            values.Add(FC[ind]); values.Add(FS[ind]); values.Add(Nod[ind]);
                            values.Add(Bart[ind]); values.Add(Dt[ind]); values.Add(Barb[ind]); values.Add(Db[ind]); values.Add(Bars[ind]);
                            values.Add(new List<string> { "長期検討" });
                            var Mit = 0.0; var Mib = 0.0; var Mct = 0.0; var Mcb = 0.0; var Mjt = 0.0; var Mjb = 0.0;
                            var Mit_text = ""; var Mib_text = ""; var Mct_text = ""; var Mcb_text = ""; var Mjt_text = ""; var Mjb_text = "";
                            if (M[ind][0] < 0) { Mit=Math.Abs(M[ind][0]); Mit_text = Mit.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mit))); }
                            else { Mib = Math.Abs(M[ind][0]); Mib_text = Mib.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mib))); }
                            if (M[ind][1] < 0) { Mct = Math.Abs(M[ind][1]); Mct_text = Mct.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mct))); }
                            else { Mcb = Math.Abs(M[ind][1]); Mcb_text = Mcb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mcb))); }
                            if (M[ind][2] > 0) { Mjt = Math.Abs(M[ind][2]); Mjt_text = Mjt.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mjt))); }
                            else { Mjb = Math.Abs(M[ind][2]); Mjb_text = Mjb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mjb))); }
                            var Mait = Mt_aL[ind][0]; var Mact = Mt_aL[ind][1]; var Majt = Mt_aL[ind][2];
                            var Maib = Mb_aL[ind][0]; var Macb = Mb_aL[ind][1]; var Majb = Mb_aL[ind][2];
                            var Mait_text = Mait.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mait)));
                            var Mact_text = Mact.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mact)));
                            var Majt_text = Majt.ToString("F10").Substring(0, Math.Max(4, Digit((int)Majt)));
                            var Maib_text = Maib.ToString("F10").Substring(0, Math.Max(4, Digit((int)Maib)));
                            var Macb_text = Macb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Macb)));
                            var Majb_text = Majb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Majb)));
                            var ki = Math.Max(Mit / Mait, Mib / Maib); var kc = Math.Max(Mct / Mact, Mcb / Macb); var kj = Math.Max(Mjt / Majt, Mjb / Majb);
                            var Qi = Math.Abs(Q[ind][0]); var Qc = Math.Abs(Q[ind][1]); var Qj = Math.Abs(Q[ind][2]);
                            var Qi_text = Qi.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qi)));
                            var Qc_text = Qc.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qc)));
                            var Qj_text = Qj.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qj)));
                            var Qai = Q_aL[ind][0]; var Qac = Q_aL[ind][1]; var Qaj = Q_aL[ind][2];
                            var Qai_text = Qai.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qai)));
                            var Qac_text = Qac.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qac)));
                            var Qaj_text = Qaj.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qaj)));
                            var ki2 = Math.Abs(Qi / Qai); var kc2 = Math.Abs(Qc / Qac); var kj2 = Math.Abs(Qj / Qaj);
                            values.Add(new List<string> { Mit_text, Mct_text, Mjt_text }); values.Add(new List<string> { Mib_text, Mcb_text, Mjb_text });
                            values.Add(new List<string> { Mait_text, Mact_text, Majt_text }); values.Add(new List<string> { Maib_text, Macb_text, Majb_text });
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            values.Add(new List<string> { Qi_text, Qc_text, Qj_text });
                            values.Add(new List<string> { Qai_text, Qac_text, Qaj_text });
                            values.Add(new List<string> { ki2.ToString("F10").Substring(0, 4), kc2.ToString("F10").Substring(0, 4), kj2.ToString("F10").Substring(0, 4) });
                            var k_color = new List<XSolidBrush>(); var k2_color = new List<XSolidBrush>();
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            if (ki < 1 && kc < 1 && kj < 1 && ki2 < 1 && kc2 < 1 && kj2 < 1)
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
                                    if (i == 26) { color1 = k_color[0]; color2 = k_color[1]; color3 = k_color[2]; }
                                    if (i == 29) { color1 = k2_color[0]; color2 = k2_color[1]; color3 = k2_color[2]; }
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
                    else if (sec_f[0].Count == 18 * 3)
                    {
                        var labels = new List<string>
                        {
                            "部材番号","配筋符号","断面算定用bxD[mm]"," ","i端上端ft[N/mm2]","i端下端ft[N/mm2]","中央上端ft[N/mm2]","中央下端ft[N/mm2]","j端上端ft[N/mm2]", "j端下端ft[N/mm2]", "i端fs[N/mm2]", "中央fs[N/mm2]","j端fs[N/mm2]", "コンクリートfc[N/mm2]","コンクリートfs[N/mm2]", "節点番号","上端主筋", "上端-鉄筋重心距離dt[mm]","下端主筋","下端-鉄筋重心距離dt[mm]","せん断補強筋"," ","上端M[kNm]","下端M[kNm]","上端Ma[kNm]","下端Ma[kNm]","曲げ検定比M/Ma", "Q=QL[kN]","Qa[kN]","せん断検定比Q/Qa","判定","","上端M[kNm]","下端M[kNm]","上端Ma[kNm]","下端Ma[kNm]","曲げ検定比M/Ma", "Q=QL+"+Math.Round(N,2).ToString()+"QX[kN]","Qa[kN]","せん断検定比Q/Qa","判定","","上端M[kNm]","下端M[kNm]","上端Ma[kNm]","下端Ma[kNm]","曲げ検定比M/Ma", "Q=QL+"+Math.Round(N,2).ToString()+"QY[kN]","Qa[kN]","せん断検定比Q/Qa","判定","","上端M[kNm]","下端M[kNm]","上端Ma[kNm]","下端Ma[kNm]","曲げ検定比M/Ma", "Q=QL-"+Math.Round(N,2).ToString()+"QX[kN]","Qa[kN]","せん断検定比Q/Qa","判定","","上端M[kNm]","下端M[kNm]","上端Ma[kNm]","下端Ma[kNm]","曲げ検定比M/Ma", "Q=QL-"+Math.Round(N,2).ToString()+"QY[kN]","Qa[kN]","せん断検定比Q/Qa","判定"
                        };
                        var label_width = 100; var offset_x = 25; var offset_y = 25; var pitchy = 10.5; var text_width = 45; PdfPage page = new PdfPage(); page.Size = PageSize.A4;
                        for (int ind = 0; ind < index.Count; ind++)//
                        {
                            int e = (int)index[ind];
                            var values = new List<List<string>>();
                            values.Add(Ele[ind]); values.Add(Name[ind]); values.Add(Size[ind]); values.Add(new List<string> { "長期", "", "短期" });
                            values.Add(Ftit[ind]); values.Add(Ftib[ind]); values.Add(Ftct[ind]); values.Add(Ftcb[ind]); values.Add(Ftjt[ind]); values.Add(Ftjb[ind]);
                            values.Add(Fsi[ind]); values.Add(Fsc[ind]); values.Add(Fsj[ind]);
                            values.Add(FC[ind]); values.Add(FS[ind]); values.Add(Nod[ind]);
                            values.Add(Bart[ind]); values.Add(Dt[ind]); values.Add(Barb[ind]); values.Add(Db[ind]); values.Add(Bars[ind]);
                            values.Add(new List<string> { "長期検討" });
                            var Mit = 0.0; var Mib = 0.0; var Mct = 0.0; var Mcb = 0.0; var Mjt = 0.0; var Mjb = 0.0;
                            var Mit_text = ""; var Mib_text = ""; var Mct_text = ""; var Mcb_text = ""; var Mjt_text = ""; var Mjb_text = "";
                            if (M[ind][0] < 0) { Mit = Math.Abs(M[ind][0]); Mit_text = Mit.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mit))); }
                            else { Mib = Math.Abs(M[ind][0]); Mib_text = Mib.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mib))); }
                            if (M[ind][1] < 0) { Mct = Math.Abs(M[ind][1]); Mct_text = Mct.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mct))); }
                            else { Mcb = Math.Abs(M[ind][1]); Mcb_text = Mcb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mcb))); }
                            if (M[ind][2] > 0) { Mjt = Math.Abs(M[ind][2]); Mjt_text = Mjt.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mjt))); }
                            else { Mjb = Math.Abs(M[ind][2]); Mjb_text = Mjb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mjb))); }
                            var Mait = Mt_aL[ind][0]; var Mact = Mt_aL[ind][1]; var Majt = Mt_aL[ind][2];
                            var Maib = Mb_aL[ind][0]; var Macb = Mb_aL[ind][1]; var Majb = Mb_aL[ind][2];
                            var Mait_text = Mait.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mait)));
                            var Mact_text = Mact.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mact)));
                            var Majt_text = Majt.ToString("F10").Substring(0, Math.Max(4, Digit((int)Majt)));
                            var Maib_text = Maib.ToString("F10").Substring(0, Math.Max(4, Digit((int)Maib)));
                            var Macb_text = Macb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Macb)));
                            var Majb_text = Majb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Majb)));
                            var ki = Math.Max(Mit / Mait, Mib / Maib); var kc = Math.Max(Mct / Mact, Mcb / Macb); var kj = Math.Max(Mjt / Majt, Mjb / Majb);
                            var Qi = Math.Abs(Q[ind][0]); var Qc = Math.Abs(Q[ind][1]); var Qj = Math.Abs(Q[ind][2]);
                            var Qi_text = Qi.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qi)));
                            var Qc_text = Qc.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qc)));
                            var Qj_text = Qj.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qj)));
                            var Qai = Q_aL[ind][0]; var Qac = Q_aL[ind][1]; var Qaj = Q_aL[ind][2];
                            var Qai_text = Qai.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qai)));
                            var Qac_text = Qac.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qac)));
                            var Qaj_text = Qaj.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qaj)));
                            var ki2 = Math.Abs(Qi / Qai); var kc2 = Math.Abs(Qc / Qac); var kj2 = Math.Abs(Qj / Qaj);
                            values.Add(new List<string> { Mit_text, Mct_text, Mjt_text }); values.Add(new List<string> { Mib_text, Mcb_text, Mjb_text });
                            values.Add(new List<string> { Mait_text, Mact_text, Majt_text }); values.Add(new List<string> { Maib_text, Macb_text, Majb_text });
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            values.Add(new List<string> { Qi_text, Qc_text, Qj_text });
                            values.Add(new List<string> { Qai_text, Qac_text, Qaj_text });
                            values.Add(new List<string> { ki2.ToString("F10").Substring(0, 4), kc2.ToString("F10").Substring(0, 4), kj2.ToString("F10").Substring(0, 4) });
                            var k_color = new List<XSolidBrush>(); var k2_color = new List<XSolidBrush>();
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            if (ki < 1 && kc < 1 && kj < 1 && ki2 < 1 && kc2 < 1 && kj2 < 1)
                            {
                                values.Add(new List<string> { "O.K.", "O.K.", "O.K." });
                            }
                            else { values.Add(new List<string> { "N.G.", "N.G.", "N.G." }); }
                            values.Add(new List<string> { "短期(L+X)検討" });
                            Mit = 0.0; Mib = 0.0; Mct = 0.0; Mcb = 0.0; Mjt = 0.0; Mjb = 0.0;
                            Mit_text = ""; Mib_text = ""; Mct_text = ""; Mcb_text = ""; Mjt_text = ""; Mjb_text = "";
                            if ((M[ind][0] + Mx[ind][0]) < 0) { Mit = Math.Abs(M[ind][0] + Mx[ind][0]); Mit_text = Mit.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mit))); }
                            else { Mib = Math.Abs(M[ind][0] + Mx[ind][0]); Mib_text = Mib.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mib))); }
                            if (M[ind][1] + Mx[ind][1] < 0) { Mct = Math.Abs(M[ind][1] + Mx[ind][1]); Mct_text = Mct.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mct))); }
                            else { Mcb = Math.Abs(M[ind][1] + Mx[ind][1]); Mcb_text = Mcb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mcb))); }
                            if (M[ind][2] + Mx[ind][2] > 0) { Mjt = Math.Abs(M[ind][2] + Mx[ind][2]); Mjt_text = Mjt.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mjt))); }
                            else { Mjb = Math.Abs(M[ind][2] + Mx[ind][2]); Mjb_text = Mjb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mjb))); }
                            Mait = Mt_aS[ind][0]; Mact = Mt_aS[ind][1]; Majt = Mt_aS[ind][2];
                            Maib = Mb_aS[ind][0]; Macb = Mb_aS[ind][1]; Majb = Mb_aS[ind][2];
                            Mait_text = Mait.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mait)));
                            Mact_text = Mact.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mact)));
                            Majt_text = Majt.ToString("F10").Substring(0, Math.Max(4, Digit((int)Majt)));
                            Maib_text = Maib.ToString("F10").Substring(0, Math.Max(4, Digit((int)Maib)));
                            Macb_text = Macb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Macb)));
                            Majb_text = Majb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Majb)));
                            ki = Math.Max(Mit / Mait, Mib / Maib); kc = Math.Max(Mct / Mact, Mcb / Macb); kj = Math.Max(Mjt / Majt, Mjb / Majb);
                            Qi = Math.Abs(Q[ind][0] + Qx[ind][0] * N); Qc = Math.Abs(Q[ind][1] + Qx[ind][1] * N); Qj = Math.Abs(Q[ind][2] + Qx[ind][2] * N);
                            Qi_text = Qi.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qi)));
                            Qc_text = Qc.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qc)));
                            Qj_text = Qj.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qj)));
                            Qai = Q_aLpX[ind][0]; Qac = Q_aLpX[ind][1]; Qaj = Q_aLpX[ind][2];
                            Qai_text = Qai.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qai)));
                            Qac_text = Qac.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qac)));
                            Qaj_text = Qaj.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qaj)));
                            ki2 = Math.Abs(Qi / Qai); kc2 = Math.Abs(Qc / Qac); kj2 = Math.Abs(Qj / Qaj);
                            values.Add(new List<string> { Mit_text, Mct_text, Mjt_text }); values.Add(new List<string> { Mib_text, Mcb_text, Mjb_text });
                            values.Add(new List<string> { Mait_text, Mact_text, Majt_text }); values.Add(new List<string> { Maib_text, Macb_text, Majb_text });
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            values.Add(new List<string> { Qi_text, Qc_text, Qj_text });
                            values.Add(new List<string> { Qai_text, Qac_text, Qaj_text });
                            values.Add(new List<string> { ki2.ToString("F10").Substring(0, 4), kc2.ToString("F10").Substring(0, 4), kj2.ToString("F10").Substring(0, 4) });
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            if (ki < 1 && kc < 1 && kj < 1 && ki2 < 1 && kc2 < 1 && kj2 < 1)
                            {
                                values.Add(new List<string> { "O.K.", "O.K.", "O.K." });
                            }
                            else { values.Add(new List<string> { "N.G.", "N.G.", "N.G." }); }
                            values.Add(new List<string> { "短期(L+Y)検討" });
                            Mit = 0.0; Mib = 0.0; Mct = 0.0; Mcb = 0.0; Mjt = 0.0; Mjb = 0.0;
                            Mit_text = ""; Mib_text = ""; Mct_text = ""; Mcb_text = ""; Mjt_text = ""; Mjb_text = "";
                            if ((M[ind][0] + My[ind][0]) < 0) { Mit = Math.Abs(M[ind][0] + My[ind][0]); Mit_text = Mit.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mit))); }
                            else { Mib = Math.Abs(M[ind][0] + My[ind][0]); Mib_text = Mib.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mib))); }
                            if (M[ind][1] + My[ind][1] < 0) { Mct = Math.Abs(M[ind][1] + My[ind][1]); Mct_text = Mct.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mct))); }
                            else { Mcb = Math.Abs(M[ind][1] + My[ind][1]); Mcb_text = Mcb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mcb))); }
                            if (M[ind][2] + My[ind][2] > 0) { Mjt = Math.Abs(M[ind][2] + My[ind][2]); Mjt_text = Mjt.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mjt))); }
                            else { Mjb = Math.Abs(M[ind][2] + My[ind][2]); Mjb_text = Mjb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mjb))); }
                            Mait_text = Mait.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mait)));
                            Mact_text = Mact.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mact)));
                            Majt_text = Majt.ToString("F10").Substring(0, Math.Max(4, Digit((int)Majt)));
                            Maib_text = Maib.ToString("F10").Substring(0, Math.Max(4, Digit((int)Maib)));
                            Macb_text = Macb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Macb)));
                            Majb_text = Majb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Majb)));
                            ki = Math.Max(Mit / Mait, Mib / Maib); kc = Math.Max(Mct / Mact, Mcb / Macb); kj = Math.Max(Mjt / Majt, Mjb / Majb);
                            Qi = Math.Abs(Q[ind][0] + Qy[ind][0] * N); Qc = Math.Abs(Q[ind][1] + Qy[ind][1] * N); Qj = Math.Abs(Q[ind][2] + Qy[ind][2] * N);
                            Qi_text = Qi.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qi)));
                            Qc_text = Qc.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qc)));
                            Qj_text = Qj.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qj)));
                            Qai = Q_aLpY[ind][0]; Qac = Q_aLpY[ind][1]; Qaj = Q_aLpY[ind][2];
                            Qai_text = Qai.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qai)));
                            Qac_text = Qac.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qac)));
                            Qaj_text = Qaj.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qaj)));
                            ki2 = Math.Abs(Qi / Qai); kc2 = Math.Abs(Qc / Qac); kj2 = Math.Abs(Qj / Qaj);
                            values.Add(new List<string> { Mit_text, Mct_text, Mjt_text }); values.Add(new List<string> { Mib_text, Mcb_text, Mjb_text });
                            values.Add(new List<string> { Mait_text, Mact_text, Majt_text }); values.Add(new List<string> { Maib_text, Macb_text, Majb_text });
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            values.Add(new List<string> { Qi_text, Qc_text, Qj_text });
                            values.Add(new List<string> { Qai_text, Qac_text, Qaj_text });
                            values.Add(new List<string> { ki2.ToString("F10").Substring(0, 4), kc2.ToString("F10").Substring(0, 4), kj2.ToString("F10").Substring(0, 4) });
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            if (ki < 1 && kc < 1 && kj < 1 && ki2 < 1 && kc2 < 1 && kj2 < 1)
                            {
                                values.Add(new List<string> { "O.K.", "O.K.", "O.K." });
                            }
                            else { values.Add(new List<string> { "N.G.", "N.G.", "N.G." }); }
                            values.Add(new List<string> { "短期(L-X)検討" });
                            Mit = 0.0; Mib = 0.0; Mct = 0.0; Mcb = 0.0; Mjt = 0.0; Mjb = 0.0;
                            Mit_text = ""; Mib_text = ""; Mct_text = ""; Mcb_text = ""; Mjt_text = ""; Mjb_text = "";
                            if ((M[ind][0] - Mx[ind][0]) < 0) { Mit = Math.Abs(M[ind][0] - Mx[ind][0]); Mit_text = Mit.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mit))); }
                            else { Mib = Math.Abs(M[ind][0] - Mx[ind][0]); Mib_text = Mib.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mib))); }
                            if (M[ind][1] - Mx[ind][1] < 0) { Mct = Math.Abs(M[ind][1] - Mx[ind][1]); Mct_text = Mct.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mct))); }
                            else { Mcb = Math.Abs(M[ind][1] - Mx[ind][1]); Mcb_text = Mcb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mcb))); }
                            if (M[ind][2] - Mx[ind][2] > 0) { Mjt = Math.Abs(M[ind][2] - Mx[ind][2]); Mjt_text = Mjt.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mjt))); }
                            else { Mjb = Math.Abs(M[ind][2] - Mx[ind][2]); Mjb_text = Mjb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mjb))); }
                            Mait_text = Mait.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mait)));
                            Mact_text = Mact.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mact)));
                            Majt_text = Majt.ToString("F10").Substring(0, Math.Max(4, Digit((int)Majt)));
                            Maib_text = Maib.ToString("F10").Substring(0, Math.Max(4, Digit((int)Maib)));
                            Macb_text = Macb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Macb)));
                            Majb_text = Majb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Majb)));
                            ki = Math.Max(Mit / Mait, Mib / Maib); kc = Math.Max(Mct / Mact, Mcb / Macb); kj = Math.Max(Mjt / Majt, Mjb / Majb);
                            Qi = Math.Abs(Q[ind][0] - Qx[ind][0] * N); Qc = Math.Abs(Q[ind][1] - Qx[ind][1] * N); Qj = Math.Abs(Q[ind][2] - Qx[ind][2] * N);
                            Qi_text = Qi.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qi)));
                            Qc_text = Qc.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qc)));
                            Qj_text = Qj.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qj)));
                            Qai = Q_aLmX[ind][0]; Qac = Q_aLmX[ind][1]; Qaj = Q_aLmX[ind][2];
                            Qai_text = Qai.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qai)));
                            Qac_text = Qac.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qac)));
                            Qaj_text = Qaj.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qaj)));
                            ki2 = Math.Abs(Qi / Qai); kc2 = Math.Abs(Qc / Qac); kj2 = Math.Abs(Qj / Qaj);
                            values.Add(new List<string> { Mit_text, Mct_text, Mjt_text }); values.Add(new List<string> { Mib_text, Mcb_text, Mjb_text });
                            values.Add(new List<string> { Mait_text, Mact_text, Majt_text }); values.Add(new List<string> { Maib_text, Macb_text, Majb_text });
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            values.Add(new List<string> { Qi_text, Qc_text, Qj_text });
                            values.Add(new List<string> { Qai_text, Qac_text, Qaj_text });
                            values.Add(new List<string> { ki2.ToString("F10").Substring(0, 4), kc2.ToString("F10").Substring(0, 4), kj2.ToString("F10").Substring(0, 4) });
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            if (ki < 1 && kc < 1 && kj < 1 && ki2 < 1 && kc2 < 1 && kj2 < 1)
                            {
                                values.Add(new List<string> { "O.K.", "O.K.", "O.K." });
                            }
                            else { values.Add(new List<string> { "N.G.", "N.G.", "N.G." }); }
                            values.Add(new List<string> { "短期(L-Y)検討" });
                            Mit = 0.0; Mib = 0.0; Mct = 0.0; Mcb = 0.0; Mjt = 0.0; Mjb = 0.0;
                            Mit_text = ""; Mib_text = ""; Mct_text = ""; Mcb_text = ""; Mjt_text = ""; Mjb_text = "";
                            if ((M[ind][0] - My[ind][0]) < 0) { Mit = Math.Abs(M[ind][0] - My[ind][0]); Mit_text = Mit.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mit))); }
                            else { Mib = Math.Abs(M[ind][0] - My[ind][0]); Mib_text = Mib.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mib))); }
                            if (M[ind][1] - My[ind][1] < 0) { Mct = Math.Abs(M[ind][1] - My[ind][1]); Mct_text = Mct.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mct))); }
                            else { Mcb = Math.Abs(M[ind][1] - My[ind][1]); Mcb_text = Mcb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mcb))); }
                            if (M[ind][2] - My[ind][2] > 0) { Mjt = Math.Abs(M[ind][2] - My[ind][2]); Mjt_text = Mjt.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mjt))); }
                            else { Mjb = Math.Abs(M[ind][2] - My[ind][2]); Mjb_text = Mjb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mjb))); }
                            Mait_text = Mait.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mait)));
                            Mact_text = Mact.ToString("F10").Substring(0, Math.Max(4, Digit((int)Mact)));
                            Majt_text = Majt.ToString("F10").Substring(0, Math.Max(4, Digit((int)Majt)));
                            Maib_text = Maib.ToString("F10").Substring(0, Math.Max(4, Digit((int)Maib)));
                            Macb_text = Macb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Macb)));
                            Majb_text = Majb.ToString("F10").Substring(0, Math.Max(4, Digit((int)Majb)));
                            ki = Math.Max(Mit / Mait, Mib / Maib); kc = Math.Max(Mct / Mact, Mcb / Macb); kj = Math.Max(Mjt / Majt, Mjb / Majb);
                            Qi = Math.Abs(Q[ind][0] - Qy[ind][0] * N); Qc = Math.Abs(Q[ind][1] - Qy[ind][1] * N); Qj = Math.Abs(Q[ind][2] - Qy[ind][2] * N);
                            Qi_text = Qi.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qi)));
                            Qc_text = Qc.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qc)));
                            Qj_text = Qj.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qj)));
                            Qai = Q_aLmY[ind][0]; Qac = Q_aLmY[ind][1]; Qaj = Q_aLmY[ind][2];
                            Qai_text = Qai.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qai)));
                            Qac_text = Qac.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qac)));
                            Qaj_text = Qaj.ToString("F10").Substring(0, Math.Max(4, Digit((int)Qaj)));
                            ki2 = Math.Abs(Qi / Qai); kc2 = Math.Abs(Qc / Qac); kj2 = Math.Abs(Qj / Qaj);
                            values.Add(new List<string> { Mit_text, Mct_text, Mjt_text }); values.Add(new List<string> { Mib_text, Mcb_text, Mjb_text });
                            values.Add(new List<string> { Mait_text, Mact_text, Majt_text }); values.Add(new List<string> { Maib_text, Macb_text, Majb_text });
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            values.Add(new List<string> { Qi_text, Qc_text, Qj_text });
                            values.Add(new List<string> { Qai_text, Qac_text, Qaj_text });
                            values.Add(new List<string> { ki2.ToString("F10").Substring(0, 4), kc2.ToString("F10").Substring(0, 4), kj2.ToString("F10").Substring(0, 4) });
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(ki2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(kc2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            k2_color.Add(new XSolidBrush(RGB((1 - Math.Min(kj2, 1.0)) * 1.9 / 3.0, 1, 0.5)));
                            if (ki < 1 && kc < 1 && kj < 1 && ki2 < 1 && kc2 < 1 && kj2 < 1)
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
                                    if (i == 26) { color1 = k_color[0]; color2 = k_color[1]; color3 = k_color[2]; }
                                    if (i == 29) { color1 = k2_color[0]; color2 = k2_color[1]; color3 = k2_color[2]; }
                                    if (i == 36) { color1 = k_color[3]; color2 = k_color[4]; color3 = k_color[5]; }
                                    if (i == 39) { color1 = k2_color[3]; color2 = k2_color[4]; color3 = k2_color[5]; }
                                    if (i == 46) { color1 = k_color[6]; color2 = k_color[7]; color3 = k_color[8]; }
                                    if (i == 49) { color1 = k2_color[6]; color2 = k2_color[7]; color3 = k2_color[8]; }
                                    if (i == 56) { color1 = k_color[9]; color2 = k_color[10]; color3 = k_color[11]; }
                                    if (i == 59) { color1 = k2_color[9]; color2 = k2_color[10]; color3 = k2_color[11]; }
                                    if (i == 66) { color1 = k_color[12]; color2 = k_color[13]; color3 = k_color[14]; }
                                    if (i == 69) { color1 = k2_color[12]; color2 = k2_color[13]; color3 = k2_color[14]; }
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
                return OpenSeesUtility.Properties.Resources.rcbeamcheck;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5ae985c8-dc74-43b8-ab19-d1fb4d9ff403"); }
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
                int width= global_rec.Width;
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
                    graphics.DrawString("M kentei", GH_FontServer.Standard, Brushes.Black, text_rec2_1);

                    GH_Capsule radio2_2 = GH_Capsule.CreateCapsule(radio_rec2_2, GH_Palette.Black, 5, 5);
                    radio2_2.Render(graphics, Selected, Owner.Locked, false); radio2_2.Dispose();
                    graphics.FillEllipse(c22, radio_rec2_2);
                    graphics.DrawString("Q kentei", GH_FontServer.Standard, Brushes.Black, text_rec2_2);

                    GH_Capsule radio2_3 = GH_Capsule.CreateCapsule(radio_rec2_3, GH_Palette.Black, 5, 5);
                    radio2_3.Render(graphics, Selected, Owner.Locked, false); radio2_3.Dispose();
                    graphics.FillEllipse(c23, radio_rec2_3);
                    graphics.DrawString("MAX kentei", GH_FontServer.Standard, Brushes.Black, text_rec2_3);

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
                        if (c21 == Brushes.White) { c21 = Brushes.Black; SetButton("21", 1); c22 = Brushes.White; SetButton("22", 0); c23 = Brushes.White; SetButton("23", 0);}
                        else { c21 = Brushes.White; SetButton("21", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec22.Contains(e.CanvasLocation))
                    {
                        if (c22 == Brushes.White) { c22 = Brushes.Black; SetButton("22", 1); c21 = Brushes.White; SetButton("21", 0); c23 = Brushes.White; SetButton("23", 0);}
                        else { c22 = Brushes.White; SetButton("22", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec23.Contains(e.CanvasLocation))
                    {
                        if (c23 == Brushes.White) { c23 = Brushes.Black; SetButton("23", 1); c21 = Brushes.White; SetButton("21", 0); c22 = Brushes.White; SetButton("22", 0);}
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