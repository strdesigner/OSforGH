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
    public class RcColumnCheck : GH_Component
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
        public RcColumnCheck()
          : base("Allowable stress design for RC columns", "RCColumnCheck",
              "Allowable stress design(danmensantei) for RC columns using Japanese Design Code",
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
            pManager.AddTextParameter("outputname", "outputname", "output file name", GH_ParamAccess.item, "RcColumnCheck");///
            pManager.AddNumberParameter("P1", "P1", "[■□HL[:B,〇●:R](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///1
            pManager.AddNumberParameter("P2", "P2", "[■□HL[:D,〇:t,●:0](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///2
            pManager[14].Optional = true; pManager[15].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("MaL", "MaL", "[[Mai(top),Mac(top),Maj(top),Mai(bottom),Mac(bottom),Maj(bottom),Mai(right),Mac(right),Maj(right),Mai(left),Mac(left),Maj(left)],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("QaL", "QaL", "[[Qai,Qac,Qaj,Qai2,Qac2,Qaj2],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("MaS(L+X)", "MaS(L+X)", "[[Mai(top),Mac(top),Maj(top),Mai(bottom),Mac(bottom),Maj(bottom),Mai(right),Mac(right),Maj(right),Mai(left),Mac(left),Maj(left)],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("MaS(L+Y)", "MaS(L+Y)", "[[Mai(top),Mac(top),Maj(top),Mai(bottom),Mac(bottom),Maj(bottom),Mai(right),Mac(right),Maj(right),Mai(left),Mac(left),Maj(left)],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("MaS(L-X)", "MaS(L-X)", "[[Mai(top),Mac(top),Maj(top),Mai(bottom),Mac(bottom),Maj(bottom),Mai(right),Mac(right),Maj(right),Mai(left),Mac(left),Maj(left)],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("MaS(L-Y)", "MaS(L-Y)", "[[Mai(top),Mac(top),Maj(top),Mai(bottom),Mac(bottom),Maj(bottom),Mai(right),Mac(right),Maj(right),Mai(left),Mac(left),Maj(left)],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("QaS(L+X)", "QaS(L+X)", "[[Qai,Qac,Qaj],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("QaS(L+Y)", "QaS(L+Y)", "[[Qai,Qac,Qaj],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("QaS(L-X)", "QaS(L-X)", "[[Qai,Qac,Qaj],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("QaS(L-Y)", "QaS(L-Y)", "[[Qai,Qac,Qaj],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("kentei(max)", "kentei(max)", "[[ele. No.,for Long-term, for Short-term],...](DataTree)", GH_ParamAccess.tree);///
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
            DA.GetDataTree("barT1", out GH_Structure<GH_Number> _barT1); var barT1 = _barT1.Branches;
            DA.GetDataTree("barT2", out GH_Structure<GH_Number> _barT2); var barT2 = _barT2.Branches;
            DA.GetDataTree("barB1", out GH_Structure<GH_Number> _barB1); var barB1 = _barB1.Branches;
            DA.GetDataTree("barB2", out GH_Structure<GH_Number> _barB2); var barB2 = _barB2.Branches;
            var barNo = new List<double>(); DA.GetDataList("bar", barNo); var Fc = new List<double>(); DA.GetDataList("Standard allowable stress (compression)[N/mm2]", Fc); var N = 2.0; DA.GetData("n", ref N);
            DA.GetData("fontsize", ref fontsize); var barname = new List<string>(); DA.GetDataList("name", barname);
            var P1 = new List<double>(); DA.GetDataList("P1", P1); var P2 = new List<double>(); DA.GetDataList("P2", P2);
            var kentei = new GH_Structure<GH_Number>(); int digit = 4; var kmaxL = new List<double>(); var kmaxS = new List<double>();
            var unitl = 1.0 / 1000.0; var unitf = 1.0 / 1000.0;///単位合わせのための係数
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
                var MaLpX = new GH_Structure<GH_Number>(); var MaLpY = new GH_Structure<GH_Number>(); var MaLmX = new GH_Structure<GH_Number>(); var MaLmY = new GH_Structure<GH_Number>();
                var QaLpX = new GH_Structure<GH_Number>(); var QaLpY = new GH_Structure<GH_Number>(); var QaLmX = new GH_Structure<GH_Number>(); var QaLmY = new GH_Structure<GH_Number>();
                var FtiT = new List<List<string>>(); var FtiB = new List<List<string>>();
                var FtcT = new List<List<string>>(); var FtcB = new List<List<string>>();
                var FtjT = new List<List<string>>(); var FtjB = new List<List<string>>();
                var FtiR = new List<List<string>>(); var FtiL = new List<List<string>>();
                var FtcR = new List<List<string>>(); var FtcL = new List<List<string>>();
                var FtjR = new List<List<string>>(); var FtjL = new List<List<string>>();
                var Fsi = new List<List<string>>(); var Fsc = new List<List<string>>(); var Fsj = new List<List<string>>();
                var FC = new List<List<string>>(); var FS = new List<List<string>>();
                var Nod = new List<List<string>>(); var Ele = new List<List<string>>(); var Name = new List<List<string>>(); var Size = new List<List<string>>();
                var BarT = new List<List<string>>(); var BarB = new List<List<string>>(); var Bars = new List<List<string>>();
                var BarR = new List<List<string>>(); var BarL = new List<List<string>>(); var Bars2 = new List<List<string>>();
                var DT = new List<List<string>>(); var DB = new List<List<string>>(); var DR = new List<List<string>>(); var DL = new List<List<string>>();
                var MyL = new List<List<double>>(); var MzL = new List<List<double>>(); var QyL = new List<List<double>>(); var QzL = new List<List<double>>();
                var MyLpX = new List<List<double>>(); var MzLpX = new List<List<double>>(); var QyLpX = new List<List<double>>(); var QzLpX = new List<List<double>>();
                var MyLmX = new List<List<double>>(); var MzLmX = new List<List<double>>(); var QyLmX = new List<List<double>>(); var QzLmX = new List<List<double>>();
                var MyLpY = new List<List<double>>(); var MzLpY = new List<List<double>>(); var QyLpY = new List<List<double>>(); var QzLpY = new List<List<double>>();
                var MyLmY = new List<List<double>>(); var MzLmY = new List<List<double>>(); var QyLmY = new List<List<double>>(); var QzLmY = new List<List<double>>();
                var My = new List<List<double>>(); var Qy = new List<List<double>>();
                var MT_aL = new List<List<double>>(); var MB_aL = new List<List<double>>(); var MR_aL = new List<List<double>>(); var ML_aL = new List<List<double>>();
                var MT_aLpX = new List<List<double>>(); var MB_aLpX = new List<List<double>>(); var MR_aLpX = new List<List<double>>(); var ML_aLpX = new List<List<double>>();
                var MT_aLmX = new List<List<double>>(); var MB_aLmX = new List<List<double>>(); var MR_aLmX = new List<List<double>>(); var ML_aLmX = new List<List<double>>();
                var MT_aLpY = new List<List<double>>(); var MB_aLpY = new List<List<double>>(); var MR_aLpY = new List<List<double>>(); var ML_aLpY = new List<List<double>>();
                var MT_aLmY = new List<List<double>>(); var MB_aLmY = new List<List<double>>(); var MR_aLmY = new List<List<double>>(); var ML_aLmY = new List<List<double>>();
                var Q_aL = new List<List<double>>(); var Q_aLpX = new List<List<double>>(); var Q_aLpY = new List<List<double>>(); var Q_aLmX = new List<List<double>>(); var Q_aLmY = new List<List<double>>();
                var Q_aL2 = new List<List<double>>(); var Q_aLpX2 = new List<List<double>>(); var Q_aLpY2 = new List<List<double>>(); var Q_aLmX2 = new List<List<double>>(); var Q_aLmY2 = new List<List<double>>(); var Blist = new List<double>(); var Dlist = new List<double>();
                var DTlist = new List<List<int>>(); var DBlist = new List<List<int>>(); var DRlist = new List<List<int>>(); var DLlist = new List<List<int>>();
                var HDlist = new List<List<int>>(); var HBlist = new List<List<int>>();
                for (int ind = 0; ind < index.Count; ind++)
                {
                    int e = (int)index[ind]; int ni = (int)ij[e][0].Value; int nj = (int)ij[e][1].Value; Nod.Add(new List<string> { ni.ToString()+"(i端)", "中央", nj.ToString()+"(j端)" }); Ele.Add(new List<string> { e.ToString() });
                    var f = sec_f[e]; var Ni = f[0].Value; var Nj = f[6].Value; var Nc = f[12].Value; var Myi = f[4].Value; var Myj = f[10].Value; var Myc = f[16].Value; var Mzi = f[5].Value; var Mzj = f[11].Value; var Mzc = f[17].Value; var Qyi = f[1].Value; var Qzi = f[2].Value; var Qyj = f[7].Value; var Qzj = f[8].Value; var Qyc = f[13].Value; var Qzc = f[14].Value;
                    var Ni_x = 0.0; var Nj_x = 0.0; var Nc_x = 0.0; var Myi_x = 0.0; var Myj_x = 0.0; var Myc_x = 0.0; var Mzi_x = 0.0; var Mzj_x = 0.0; var Mzc_x = 0.0; var Qyi_x = 0.0; var Qzi_x = 0.0; var Qyj_x = 0.0; var Qzj_x = 0.0; var Qyc_x = 0.0; var Qzc_x = 0.0; var Ni_y = 0.0; var Nj_y = 0.0; var Nc_y = 0.0; var Myi_y = 0.0; var Myj_y = 0.0; var Myc_y = 0.0; var Mzi_y = 0.0; var Mzj_y = 0.0; var Mzc_y = 0.0; var Qyi_y = 0.0; var Qzi_y = 0.0; var Qyj_y = 0.0; var Qzj_y = 0.0; var Qyc_y = 0.0; var Qzc_y = 0.0;
                    var Ni_x2 = 0.0; var Nj_x2 = 0.0; var Nc_x2 = 0.0; var Myi_x2 = 0.0; var Myj_x2 = 0.0; var Myc_x2 = 0.0; var Mzi_x2 = 0.0; var Mzj_x2 = 0.0; var Mzc_x2 = 0.0; var Qyi_x2 = 0.0; var Qzi_x2 = 0.0; var Qyj_x2 = 0.0; var Qzj_x2 = 0.0; var Qyc_x2 = 0.0; var Qzc_x2 = 0.0; var Ni_y2 = 0.0; var Nj_y2 = 0.0; var Nc_y2 = 0.0; var Myi_y2 = 0.0; var Myj_y2 = 0.0; var Myc_y2 = 0.0; var Mzi_y2 = 0.0; var Mzj_y2 = 0.0; var Mzc_y2 = 0.0; var Qyi_y2 = 0.0; var Qzi_y2 = 0.0; var Qyj_y2 = 0.0; var Qzj_y2 = 0.0; var Qyc_y2 = 0.0; var Qzc_y2 = 0.0;
                    if (sec_f[0].Count / 18 >= 3)
                    {
                        Ni_x = f[18 + 0].Value; Nj_x = f[18 + 6].Value; Nc_x = f[18 + 12].Value;
                        Myi_x = f[18 + 4].Value; Myj_x = f[18 + 10].Value; Myc_x = f[18 + 16].Value; Mzi_x = f[18 + 5].Value; Mzj_x = f[18 + 11].Value; Mzc_x = f[18 + 17].Value; Qyi_x = f[18 + 1].Value; Qzi_x = f[18 + 2].Value; Qyj_x = f[18 + 7].Value; Qzj_x = f[18 + 8].Value; Qyc_x = f[18 + 13].Value; Qzc_x = f[18 + 14].Value;
                        Ni_y = f[36 + 0].Value; Nj_y = f[36 + 6].Value; Nc_y = f[36 + 12].Value;
                        Myi_y = f[36 + 4].Value; Myj_y = f[36 + 10].Value; Myc_y = f[36 + 16].Value; Mzi_y = f[36 + 5].Value; Mzj_y = f[36 + 11].Value; Mzc_y = f[36 + 17].Value; Qyi_y = f[36 + 1].Value; Qzi_y = f[36 + 2].Value; Qyj_y = f[36 + 7].Value; Qzj_y = f[36 + 8].Value; Qyc_y = f[36 + 13].Value; Qzc_y = f[36 + 14].Value;
                        if (sec_f[0].Count / 18 == 5)
                        {
                            Ni_x2 = f[54 + 0].Value; Nj_x2 = f[54 + 6].Value; Nc_x2 = f[54 + 12].Value;
                            Myi_x2 = f[54 + 4].Value; Myj_x2 = f[54 + 10].Value; Myc_x2 = f[54 + 16].Value; Mzi_x2 = f[54 + 5].Value; Mzj_x2 = f[54 + 11].Value; Mzc_x2 = f[54 + 17].Value; Qyi_x2 = f[54 + 1].Value; Qzi_x2 = f[54 + 2].Value; Qyj_x2 = f[54 + 7].Value; Qzj_x2 = f[54 + 8].Value; Qyc_x2 = f[54 + 13].Value; Qzc_x2 = f[54 + 14].Value;
                            Ni_y2 = f[72 + 0].Value; Nj_y2 = f[72 + 6].Value; Nc_y2 = f[72 + 12].Value;
                            Myi_y2 = f[72 + 4].Value; Myj_y2 = f[72 + 10].Value; Myc_y2 = f[72 + 16].Value; Mzi_y2 = f[72 + 5].Value; Mzj_y2 = f[72 + 11].Value; Mzc_y2 = f[72 + 17].Value; Qyi_y2 = f[72 + 1].Value; Qzi_y2 = f[72 + 2].Value; Qyj_y2 = f[72 + 7].Value; Qzj_y2 = f[72 + 8].Value; Qyc_y2 = f[72 + 13].Value; Qzc_y2 = f[72 + 14].Value;
                        }
                        else if(sec_f[0].Count / 18 == 3)
                        {
                            Ni_x2 = -f[18 + 0].Value; Nj_x2 = -f[18 + 6].Value; Nc_x2 = -f[18 + 12].Value;
                            Myi_x2 = -f[18 + 4].Value; Myj_x2 = -f[18 + 10].Value; Myc_x2 = -f[18 + 16].Value; Mzi_x2 = -f[18 + 5].Value; Mzj_x2 = -f[18 + 11].Value; Mzc_x2 = -f[18 + 17].Value; Qyi_x2 = -f[18 + 1].Value; Qzi_x2 = -f[18 + 2].Value; Qyj_x2 = -f[18 + 7].Value; Qzj_x2 = -f[18 + 8].Value; Qyc_x2 = -f[18 + 13].Value; Qzc_x2 = -f[18 + 14].Value;
                            Ni_y2 = -f[36 + 0].Value; Nj_y2 = -f[36 + 6].Value; Nc_y2 = -f[36 + 12].Value;
                            Myi_y2 = -f[36 + 4].Value; Myj_y2 = -f[36 + 10].Value; Myc_y2 = -f[36 + 16].Value; Mzi_y2 = -f[36 + 5].Value; Mzj_y2 = -f[36 + 11].Value; Mzc_y2 = -f[36 + 17].Value; Qyi_y2 = -f[36 + 1].Value; Qzi_y2 = -f[36 + 2].Value; Qyj_y2 = -f[36 + 7].Value; Qzj_y2 = -f[36 + 8].Value; Qyc_y2 = -f[36 + 13].Value; Qzc_y2 = -f[36 + 14].Value;
                        }
                    }
                    int mat = (int)ij[e][2].Value; int bar = (int)barNo[e]; var T1 = barT1[bar]; var T2 = barT2[bar]; var B1 = barB1[bar]; var B2 = barB2[bar];
                    Name.Add(new List<string> { barname[bar] });
                    var fcL = Fc[mat] / 3.0; var fsL = Math.Min(fcL / 10.0, 0.49 + Fc[mat] / 100.0);
                    var fcS = fcL * 2.0; var fsS = fsL * 1.5;
                    FC.Add(new List<string> { fcL.ToString("F10").Substring(0, Math.Max(4, Digit((int)fcL))), "", (fcS).ToString("F10").Substring(0, Math.Max(4, Digit((int)(fcS)))) });
                    FS.Add(new List<string> { fsL.ToString("F10").Substring(0, Math.Max(4, Digit((int)fsL))), "", (fsS).ToString("F10").Substring(0, Math.Max(4, Digit((int)(fsS)))) });
                    var b = T1[16].Value; var D = T1[17].Value;//梁幅,梁せい
                    if (b <= 1e-5 || D <= 1e-5) { D = P1[(int)ij[e][3].Value] * 1000; b = P2[(int)ij[e][3].Value] * 1000; }//梁幅,梁せいの指定がない場合は断面リストの値を採用する
                    if (b <= 1e-5 || D <= 1e-5) { return; }
                    Blist.Add(b); Dlist.Add(D);
                    var kT = T1[15].Value; var kB = T1[15].Value; var kR = B1[15].Value; var kL = B2[15].Value;//上下右左かぶり
                    var n_iT = T1[0].Value; var n_iB = T2[0].Value; var D_iT = T1[1].Value; var D_iB = T2[1].Value;//i端の強軸の引張鉄筋本数ならびに主筋径
                    var n_cT = T1[2].Value; var n_cB = T2[2].Value; var D_cT = T1[3].Value; var D_cB = T2[3].Value;//中央の強軸の引張鉄筋本数ならびに主筋径
                    var n_jT = T1[4].Value; var n_jB = T2[4].Value; var D_jT = T1[5].Value; var D_jB = T2[5].Value;//j端の強軸の引張鉄筋本数ならびに主筋径
                    var s_i = T1[6].Value; var S_i = T1[7].Value; var P_i = T1[8].Value;//i端の強軸方向のSTP本数，主筋径，ピッチ
                    var s_c = T1[9].Value; var S_c = T1[10].Value; var P_c = T1[11].Value;//中央の強軸方向のSTP本数，主筋径，ピッチ
                    var s_j = T1[12].Value; var S_j = T1[13].Value; var P_j = T1[14].Value;//j端の強軸方向のSTP本数，主筋径，ピッチ
                    var s_i2 = B1[6].Value; var S_i2 = B1[7].Value; var P_i2 = B1[8].Value;//i端の弱軸方向のSTP本数，主筋径，ピッチ
                    var s_c2 = B1[9].Value; var S_c2 = B1[10].Value; var P_c2 = B1[11].Value;//中央の弱軸方向のSTP本数，主筋径，ピッチ
                    var s_j2 = B1[12].Value; var S_j2 = B1[13].Value; var P_j2 = B1[14].Value;//j端の弱軸方向のSTP本数，主筋径，ピッチ
                    var n_iR = B1[0].Value; var n_iL = B2[0].Value; var D_iR = B1[1].Value; var D_iL = B2[1].Value;//i端の弱軸の引張鉄筋本数ならびに主筋径
                    var n_cR = B1[2].Value; var n_cL = B2[2].Value; var D_cR = B1[3].Value; var D_cL = B2[3].Value;//中央の弱軸の引張鉄筋本数ならびに主筋径
                    var n_jR = B1[4].Value; var n_jL = B2[4].Value; var D_jR = B1[5].Value; var D_jL = B2[5].Value;//j端の弱軸の引張鉄筋本数ならびに主筋径
                    DTlist.Add(new List<int> { (int)n_iT, (int)n_cT, (int)n_jT }); DBlist.Add(new List<int> { (int)n_iB, (int)n_cB, (int)n_jB }); DRlist.Add(new List<int> { (int)n_iR, (int)n_cR, (int)n_jR }); DLlist.Add(new List<int> { (int)n_iL, (int)n_cL, (int)n_jL }); HDlist.Add(new List<int> { (int)s_i, (int)s_c, (int)s_j }); HBlist.Add(new List<int> { (int)s_i2, (int)s_c2, (int)s_j2 });
                    if (kB == 0) { kB = kT; }
                    if (kL == 0) { kR = kL; }
                    Size.Add(new List<string> { ((b).ToString()).Substring(0, Digit((int)b)) + "x" + ((D).ToString()).Substring(0, Digit((int)D)) });
                    var bartextit = (n_iT.ToString()).Substring(0, Digit((int)n_iT));
                    bartextit += "-D" + (D_iT.ToString()).Substring(0, Digit((int)D_iT));
                    var bartextib = (n_iB.ToString()).Substring(0, Digit((int)n_iB));
                    bartextib += "-D" + (D_iB.ToString()).Substring(0, Digit((int)D_iB));
                    var bartextct = (n_cT.ToString()).Substring(0, Digit((int)n_cT));
                    bartextct += "-D" + (D_cT.ToString()).Substring(0, Digit((int)D_cT));
                    var bartextcb = (n_cB.ToString()).Substring(0, Digit((int)n_cB));
                    bartextcb += "-D" + (D_cB.ToString()).Substring(0, Digit((int)D_cB));
                    var bartextjt = (n_jT.ToString()).Substring(0, Digit((int)n_jT));
                    bartextjt += "-D" + (D_jT.ToString()).Substring(0, Digit((int)D_jT));
                    var bartextjb = (n_jB.ToString()).Substring(0, Digit((int)n_jB));
                    bartextjb += "-D" + (D_jB.ToString()).Substring(0, Digit((int)D_jB));
                    var bartextir = (n_iR.ToString()).Substring(0, Digit((int)n_iR));
                    bartextir += "-D" + (D_iR.ToString()).Substring(0, Digit((int)D_iR));
                    var bartextil = (n_iL.ToString()).Substring(0, Digit((int)n_iL));
                    bartextil += "-D" + (D_iL.ToString()).Substring(0, Digit((int)D_iL));
                    var bartextcr = (n_cR.ToString()).Substring(0, Digit((int)n_cR));
                    bartextcr += "-D" + (D_cR.ToString()).Substring(0, Digit((int)D_cR));
                    var bartextcl = (n_cL.ToString()).Substring(0, Digit((int)n_cL));
                    bartextcl += "-D" + (D_cL.ToString()).Substring(0, Digit((int)D_cL));
                    var bartextjr = (n_jR.ToString()).Substring(0, Digit((int)n_jR));
                    bartextjr += "-D" + (D_jR.ToString()).Substring(0, Digit((int)D_jR));
                    var bartextjl = (n_jL.ToString()).Substring(0, Digit((int)n_jL));
                    bartextjl += "-D" + (D_jL.ToString()).Substring(0, Digit((int)D_jL));
                    BarT.Add(new List<string> { bartextit, bartextct, bartextjt }); BarB.Add(new List<string> { bartextib, bartextcb, bartextjb });
                    BarR.Add(new List<string> { bartextir, bartextcr, bartextjr }); BarL.Add(new List<string> { bartextil, bartextcl, bartextjl });
                    var stptexti = (s_i.ToString()).Substring(0, Digit((int)s_i)) + "-D" + (S_i.ToString()).Substring(0, Digit((int)S_i)) + "@" + (P_i.ToString()).Substring(0, Digit((int)P_i));
                    var stptextc = (s_c.ToString()).Substring(0, Digit((int)s_c)) + "-D" + (S_c.ToString()).Substring(0, Digit((int)S_c)) + "@" + (P_c.ToString()).Substring(0, Digit((int)P_c));
                    var stptextj = (s_j.ToString()).Substring(0, Digit((int)s_j)) + "-D" + (S_j.ToString()).Substring(0, Digit((int)S_j)) + "@" + (P_j.ToString()).Substring(0, Digit((int)P_j));
                    Bars.Add(new List<string> { stptexti, stptextc, stptextj });
                    var stptexti2 = (s_i2.ToString()).Substring(0, Digit((int)s_i2)) + "-D" + (S_i2.ToString()).Substring(0, Digit((int)S_i2)) + "@" + (P_i2.ToString()).Substring(0, Digit((int)P_i2));
                    var stptextc2 = (s_c2.ToString()).Substring(0, Digit((int)s_c2)) + "-D" + (S_c2.ToString()).Substring(0, Digit((int)S_c2)) + "@" + (P_c2.ToString()).Substring(0, Digit((int)P_c2));
                    var stptextj2 = (s_j2.ToString()).Substring(0, Digit((int)s_j2)) + "-D" + (S_j2.ToString()).Substring(0, Digit((int)S_j2)) + "@" + (P_j2.ToString()).Substring(0, Digit((int)P_j2));
                    Bars2.Add(new List<string> { stptexti2, stptextc2, stptextj2 });
                    //************************************************************************************************************************************************************
                    //i端の許容曲げモーメントFtit.Add(new List<string> { "", "", "" });
                    //************************************************************************************************************************************************************
                    var a_iT = n_iT * Math.Pow(D_iT, 2) * Math.PI / 4.0;//i端上端主筋断面積
                    var a_iB = n_iB * Math.Pow(D_iB, 2) * Math.PI / 4.0;//i端下端主筋断面積
                    var a_iR = n_iR * Math.Pow(D_iR, 2) * Math.PI / 4.0;//i端右端主筋断面積
                    var a_iL = n_iL * Math.Pow(D_iL, 2) * Math.PI / 4.0;//i端左端主筋断面積
                    var ft_iTL = 195.0; var ft_iBL = 195.0; var ft_iRL = 195.0; var ft_iLL = 195.0;
                    if (D_iT > 18.9 && D_iT < 28.9) { ft_iTL = 215.0; }//i端上端主筋許容引張応力度
                    if (D_iB > 18.9 && D_iB < 28.9) { ft_iBL = 215.0; }//i端下端主筋許容引張応力度
                    if (D_iR > 18.9 && D_iR < 28.9) { ft_iRL = 215.0; }//i端右端主筋許容引張応力度
                    if (D_iL > 18.9 && D_iL < 28.9) { ft_iLL = 215.0; }//i端左端主筋許容引張応力度
                    var ft_iTS = 295.0; var ft_iBS = 295.0; var ft_iRS = 295.0; var ft_iLS = 295.0;
                    if (D_iT > 18.9 && D_iT < 28.9) { ft_iTS = 345.0; }//i端上端主筋許容引張応力度
                    else if (D_iT > 28.9) { ft_iTS = 390.0; }
                    if (D_iB > 18.9 && D_iB < 28.9) { ft_iBS = 345.0; }//i端下端主筋許容引張応力度
                    else if (D_iB > 28.9) { ft_iBS = 390.0; }
                    if (D_iR > 18.9 && D_iR < 28.9) { ft_iRS = 345.0; }//i端上端主筋許容引張応力度
                    else if (D_iR > 28.9) { ft_iRS = 390.0; }
                    if (D_iL > 18.9 && D_iL < 28.9) { ft_iLS = 345.0; }//i端下端主筋許容引張応力度
                    else if (D_iL > 28.9) { ft_iLS = 390.0; }
                    FtiT.Add(new List<string> { ft_iTL.ToString().Substring(0, Digit((int)ft_iTL)), "", ft_iTS.ToString().Substring(0, Digit((int)ft_iTS)) });
                    var d_iT = kT + S_i + D_iT / 2.0;//i端の上端より鉄筋重心までの距離
                    FtiB.Add(new List<string> { ft_iBL.ToString().Substring(0, Digit((int)ft_iBL)), "", ft_iBS.ToString().Substring(0, Digit((int)ft_iBS)) });
                    var d_iB = kB + S_i + D_iB / 2.0;//i端の下端より鉄筋重心までの距離
                    FtiR.Add(new List<string> { ft_iRL.ToString().Substring(0, Digit((int)ft_iRL)), "", ft_iRS.ToString().Substring(0, Digit((int)ft_iRS)) });
                    var d_iR = kR + S_i2 + D_iR / 2.0;//i端の右端より鉄筋重心までの距離
                    FtiL.Add(new List<string> { ft_iLL.ToString().Substring(0, Digit((int)ft_iLL)), "", ft_iLS.ToString().Substring(0, Digit((int)ft_iLS)) });
                    var d_iL = kL + S_i2 + D_iL / 2.0;//i端の下端より鉄筋重心までの距離
                    var sN_iL = Ni * 1000 / b / D;//N/mm2
                    var sN_iLpX = (Ni + Ni_x) * 1000 / b / D; var sN_iLmX = (Ni + Ni_x2) * 1000 / b / D;
                    var sN_iLpY = (Ni + Ni_y) * 1000 / b / D; var sN_iLmY = (Ni + Ni_y2) * 1000 / b / D;
                    var x_iTL = (1 - d_iB / D) / (1 + ft_iTL / (n * fcL)); var x_iBL = (1 - d_iT / D) / (1 + ft_iBL / (n * fcL));
                    var x_iRL = (1 - d_iL / b) / (1 + ft_iRL / (n * fcL)); var x_iLL = (1 - d_iR / b) / (1 + ft_iLL / (n * fcL));
                    var x_iTS = (1 - d_iB / D) / (1 + ft_iTS / (n * fcS)); var x_iBS = (1 - d_iT / D) / (1 + ft_iBS / (n * fcS));
                    var x_iRS = (1 - d_iL / b) / (1 + ft_iRS / (n * fcS)); var x_iLS = (1 - d_iR / b) / (1 + ft_iLS / (n * fcS));
                    var sN1_iTL = fcL * (0.5 + n * a_iT / b / D); var sN1_iBL = fcL * (0.5 + n * a_iB / b / D); var sN1_iRL = fcL * (0.5 + n * a_iR / b / D); var sN1_iLL = fcL * (0.5 + n * a_iL / b / D);
                    var sN1_iTS = fcS * (0.5 + n * a_iT / b / D); var sN1_iBS = fcS * (0.5 + n * a_iB / b / D); var sN1_iRS = fcS * (0.5 + n * a_iR / b / D); var sN1_iLS = fcS * (0.5 + n * a_iL / b / D);
                    var sN2_iTL = fcL * (x_iTL / 2.0 + n * a_iT / b / D * (2 - 1.0 / x_iTL)); var sN2_iBL = fcL * (x_iBL / 2.0 + n * a_iB / b / D * (2 - 1.0 / x_iBL)); var sN2_iRL = fcL * (x_iRL / 2.0 + n * a_iR / b / D * (2 - 1.0 / x_iRL)); var sN2_iLL = fcL * (x_iLL / 2.0 + n * a_iL / b / D * (2 - 1.0 / x_iLL));
                    var sN2_iTS = fcS * (x_iTS / 2.0 + n * a_iT / b / D * (2 - 1.0 / x_iTS)); var sN2_iBS = fcS * (x_iBS / 2.0 + n * a_iB / b / D * (2 - 1.0 / x_iBS)); var sN2_iRS = fcS * (x_iRS / 2.0 + n * a_iR / b / D * (2 - 1.0 / x_iRS)); var sN2_iLS = fcL * (x_iLS / 2.0 + n * a_iL / b / D * (2 - 1.0 / x_iLS));
                    var sN3_iTL = ft_iTL * a_iT / b / D / (d_iB / D - 1); var sN3_iBL = ft_iBL * a_iB / b / D / (d_iT / D - 1); var sN3_iRL = ft_iRL * a_iR / b / D / (d_iL / b - 1); var sN3_iLL = ft_iLL * a_iL / b / D / (d_iR / b - 1);
                    var sN3_iTS = ft_iTS * a_iT / b / D / (d_iB / D - 1); var sN3_iBS = ft_iBS * a_iB / b / D / (d_iT / D - 1); var sN3_iRS = ft_iRS * a_iR / b / D / (d_iL / b - 1); var sN3_iLS = ft_iLS * a_iL / b / D / (d_iR / b - 1);
                    var Ma_iTL =  0.0; var Ma_iBL = 0.0; var Ma_iRL = 0.0; var Ma_iLL = 0.0;
                    var Ma_iTLpX = 0.0; var Ma_iBLpX = 0.0; var Ma_iRLpX = 0.0; var Ma_iLLpX = 0.0;
                    var Ma_iTLmX = 0.0; var Ma_iBLmX = 0.0; var Ma_iRLmX = 0.0; var Ma_iLLmX = 0.0;
                    var Ma_iTLpY = 0.0; var Ma_iBLpY = 0.0; var Ma_iRLpY = 0.0; var Ma_iLLpY = 0.0;
                    var Ma_iTLmY = 0.0; var Ma_iBLmY = 0.0; var Ma_iRLmY = 0.0; var Ma_iLLmY = 0.0;
                    //長期上端引張/////////////////////////////////////////////////////////////////////////////////////
                    if (sN_iL > sN1_iTL)
                    {
                        var xn1 = (0.5 + n * a_iT / b / D) / (1 + 2 * n * a_iT / b / D - sN_iL / fcL);
                        Ma_iTL = fcL / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iT / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iB / D, 2) - 2 * xn1 - 2 * d_iB / D + 1)) + sN_iL * (0.5 - xn1);
                    }
                    else if (sN_iL > sN2_iTL)
                    {
                        var xn1 = sN_iL / fcL - 2 * n * a_iT / b / D + Math.Sqrt(Math.Pow(2 * n * a_iT / b / D - sN_iL / fcL, 2) + 2 * n * a_iT / b / D);
                        Ma_iTL = fcL / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iB / D, 2) - 2 * d_iB / D + 1)) + sN_iL * (0.5 - xn1);
                    }
                    else if (sN_iL > sN3_iTL)
                    {
                        var xn1 = -(n / ft_iTL * sN_iL + 2 * n * a_iT / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iT / b / D + n / ft_iTL * sN_iL, 2) + 2 * (n * a_iT / b / D + n / ft_iTL * sN_iL * (1 - d_iB / D)));
                        Ma_iTL = ft_iTL / n / (1 - xn1 - d_iB / D) * (Math.Pow(xn1, 3) / 3 + n * a_iT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iB / D, 2) - 2 * d_iB / D + 1)) + sN_iL * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iTL = ft_iTL * a_iT / b / D * (1 - 2 * d_iB / D) + sN_iL * (0.5 - d_iB / D);
                    }
                    //長期下端引張
                    if (sN_iL > sN1_iBL)
                    {
                        var xn1 = (0.5 + n * a_iB / b / D) / (1 + 2 * n * a_iB / b / D - sN_iL / fcL);
                        Ma_iBL = fcL / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iB / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iT / D, 2) - 2 * xn1 - 2 * d_iT / D + 1)) + sN_iL * (0.5 - xn1);
                    }
                    else if (sN_iL > sN2_iBL)
                    {
                        var xn1 = sN_iL / fcL - 2 * n * a_iB / b / D + Math.Sqrt(Math.Pow(2 * n * a_iB / b / D - sN_iL / fcL, 2) + 2 * n * a_iB / b / D);
                        Ma_iBL = fcL / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iT / D, 2) - 2 * d_iT / D + 1)) + sN_iL * (0.5 - xn1);
                    }
                    else if (sN_iL > sN3_iBL)
                    {
                        var xn1 = -(n / ft_iBL * sN_iL + 2 * n * a_iB / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iB / b / D + n / ft_iBL * sN_iL, 2) + 2 * (n * a_iB / b / D + n / ft_iBL * sN_iL * (1 - d_iT / D)));
                        Ma_iBL = ft_iBL / n / (1 - xn1 - d_iT / D) * (Math.Pow(xn1, 3) / 3 + n * a_iB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iT / D, 2) - 2 * d_iT / D + 1)) + sN_iL * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iBL = ft_iBL * a_iB / b / D * (1 - 2 * d_iT / D) + sN_iL * (0.5 - d_iT / D);
                    }
                    //長期右端引張
                    if (sN_iL > sN1_iRL)
                    {
                        var xn1 = (0.5 + n * a_iR / b / D) / (1 + 2 * n * a_iR / b / D - sN_iL / fcL);
                        Ma_iRL = fcL / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iR / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iL / b, 2) - 2 * xn1 - 2 * d_iL / b + 1)) + sN_iL * (0.5 - xn1);
                    }
                    else if (sN_iL > sN2_iRL)
                    {
                        var xn1 = sN_iL / fcL - 2 * n * a_iR / b / D + Math.Sqrt(Math.Pow(2 * n * a_iR / b / D - sN_iL / fcL, 2) + 2 * n * a_iR / b / D);
                        Ma_iRL = fcL / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iL / b, 2) - 2 * d_iL / b + 1)) + sN_iL * (0.5 - xn1);
                    }
                    else if (sN_iL > sN3_iRL)
                    {
                        var xn1 = -(n / ft_iRL * sN_iL + 2 * n * a_iR / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iR / b / D + n / ft_iRL * sN_iL, 2) + 2 * (n * a_iR / b / D + n / ft_iRL * sN_iL * (1 - d_iL / b)));
                        Ma_iRL = ft_iRL / n / (1 - xn1 - d_iL / b) * (Math.Pow(xn1, 3) / 3 + n * a_iR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iL / b, 2) - 2 * d_iL / b + 1)) + sN_iL * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iRL = ft_iRL * a_iR / b / D * (1 - 2 * d_iL / b) + sN_iL * (0.5 - d_iL / b);
                    }
                    //長期左端引張
                    if (sN_iL > sN1_iLL)
                    {
                        var xn1 = (0.5 + n * a_iL / b / D) / (1 + 2 * n * a_iL / b / D - sN_iL / fcL);
                        Ma_iLL = fcL / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iL / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iR / b, 2) - 2 * xn1 - 2 * d_iR / b + 1)) + sN_iL * (0.5 - xn1);
                    }
                    else if (sN_iL > sN2_iLL)
                    {
                        var xn1 = sN_iL / fcL - 2 * n * a_iL / b / D + Math.Sqrt(Math.Pow(2 * n * a_iL / b / D - sN_iL / fcL, 2) + 2 * n * a_iL / b / D);
                        Ma_iLL = fcL / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iR / b, 2) - 2 * d_iR / b + 1)) + sN_iL * (0.5 - xn1);
                    }
                    else if (sN_iL > sN3_iLL)
                    {
                        var xn1 = -(n / ft_iLL * sN_iL + 2 * n * a_iL / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iL / b / D + n / ft_iLL * sN_iL, 2) + 2 * (n * a_iL / b / D + n / ft_iLL * sN_iL * (1 - d_iR / b)));
                        Ma_iLL = ft_iLL / n / (1 - xn1 - d_iR / b) * (Math.Pow(xn1, 3) / 3 + n * a_iL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iR / b, 2) - 2 * d_iR / b + 1)) + sN_iL * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iLL = ft_iLL * a_iL / b / D * (1 - 2 * d_iR / b) + sN_iL * (0.5 - d_iR / b);
                    }

                    //L+X上端引張/////////////////////////////////////////////////////////////////////////////////////
                    if (sN_iLpX > sN1_iTS)
                    {
                        var xn1 = (0.5 + n * a_iT / b / D) / (1 + 2 * n * a_iT / b / D - sN_iLpX / fcS);
                        Ma_iTLpX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iT / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iB / D, 2) - 2 * xn1 - 2 * d_iB / D + 1)) + sN_iLpX * (0.5 - xn1);
                    }
                    else if (sN_iLpX > sN2_iTS)
                    {
                        var xn1 = sN_iLpX / fcS - 2 * n * a_iT / b / D + Math.Sqrt(Math.Pow(2 * n * a_iT / b / D - sN_iLpX / fcS, 2) + 2 * n * a_iT / b / D);
                        Ma_iTLpX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iB / D, 2) - 2 * d_iB / D + 1)) + sN_iLpX * (0.5 - xn1);
                    }
                    else if (sN_iLpX > sN3_iTS)
                    {
                        var xn1 = -(n / ft_iTS * sN_iLpX + 2 * n * a_iT / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iT / b / D + n / ft_iTS * sN_iLpX, 2) + 2 * (n * a_iT / b / D + n / ft_iTS * sN_iLpX * (1 - d_iB / D)));
                        Ma_iTLpX = ft_iTS / n / (1 - xn1 - d_iB / D) * (Math.Pow(xn1, 3) / 3 + n * a_iT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iB / D, 2) - 2 * d_iB / D + 1)) + sN_iLpX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iTLpX = ft_iTS * a_iT / b / D * (1 - 2 * d_iB / D) + sN_iLpX * (0.5 - d_iB / D);
                    }
                    //L+X下端引張
                    if (sN_iLpX > sN1_iBS)
                    {
                        var xn1 = (0.5 + n * a_iB / b / D) / (1 + 2 * n * a_iB / b / D - sN_iLpX / fcS);
                        Ma_iBLpX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iB / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iT / D, 2) - 2 * xn1 - 2 * d_iT / D + 1)) + sN_iLpX * (0.5 - xn1);
                    }
                    else if (sN_iLpX > sN2_iBS)
                    {
                        var xn1 = sN_iLpX / fcS - 2 * n * a_iB / b / D + Math.Sqrt(Math.Pow(2 * n * a_iB / b / D - sN_iLpX / fcS, 2) + 2 * n * a_iB / b / D);
                        Ma_iBLpX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iT / D, 2) - 2 * d_iT / D + 1)) + sN_iLpX * (0.5 - xn1);
                    }
                    else if (sN_iLpX > sN3_iBS)
                    {
                        var xn1 = -(n / ft_iBS * sN_iLpX + 2 * n * a_iB / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iB / b / D + n / ft_iBS * sN_iLpX, 2) + 2 * (n * a_iB / b / D + n / ft_iBS * sN_iLpX * (1 - d_iT / D)));
                        Ma_iBLpX = ft_iBS / n / (1 - xn1 - d_iT / D) * (Math.Pow(xn1, 3) / 3 + n * a_iB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iT / D, 2) - 2 * d_iT / D + 1)) + sN_iLpX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iBLpX = ft_iBS * a_iB / b / D * (1 - 2 * d_iT / D) + sN_iLpX * (0.5 - d_iT / D);
                    }
                    //L+X右端引張
                    if (sN_iLpX > sN1_iRS)
                    {
                        var xn1 = (0.5 + n * a_iR / b / D) / (1 + 2 * n * a_iR / b / D - sN_iLpX / fcS);
                        Ma_iRLpX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iR / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iL / b, 2) - 2 * xn1 - 2 * d_iL / b + 1)) + sN_iLpX * (0.5 - xn1);
                    }
                    else if (sN_iLpX > sN2_iRS)
                    {
                        var xn1 = sN_iLpX / fcS - 2 * n * a_iR / b / D + Math.Sqrt(Math.Pow(2 * n * a_iR / b / D - sN_iLpX / fcS, 2) + 2 * n * a_iR / b / D);
                        Ma_iRLpX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iL / b, 2) - 2 * d_iL / b + 1)) + sN_iLpX * (0.5 - xn1);
                    }
                    else if (sN_iLpX > sN3_iRS)
                    {
                        var xn1 = -(n / ft_iRS * sN_iLpX + 2 * n * a_iR / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iR / b / D + n / ft_iRS * sN_iLpX, 2) + 2 * (n * a_iR / b / D + n / ft_iRS * sN_iLpX * (1 - d_iL / b)));
                        Ma_iRLpX = ft_iRS / n / (1 - xn1 - d_iL / b) * (Math.Pow(xn1, 3) / 3 + n * a_iR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iL / b, 2) - 2 * d_iL / b + 1)) + sN_iLpX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iRLpX = ft_iRS * a_iR / b / D * (1 - 2 * d_iL / b) + sN_iLpX * (0.5 - d_iL / b);
                    }
                    //L+X左端引張
                    if (sN_iLpX > sN1_iLS)
                    {
                        var xn1 = (0.5 + n * a_iL / b / D) / (1 + 2 * n * a_iL / b / D - sN_iLpX / fcS);
                        Ma_iLLpX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iL / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iR / b, 2) - 2 * xn1 - 2 * d_iR / b + 1)) + sN_iLpX * (0.5 - xn1);
                    }
                    else if (sN_iLpX > sN2_iLS)
                    {
                        var xn1 = sN_iLpX / fcS - 2 * n * a_iL / b / D + Math.Sqrt(Math.Pow(2 * n * a_iL / b / D - sN_iLpX / fcS, 2) + 2 * n * a_iL / b / D);
                        Ma_iLLpX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iR / b, 2) - 2 * d_iR / b + 1)) + sN_iLpX * (0.5 - xn1);
                    }
                    else if (sN_iLpX > sN3_iLS)
                    {
                        var xn1 = -(n / ft_iLS * sN_iLpX + 2 * n * a_iL / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iL / b / D + n / ft_iLS * sN_iLpX, 2) + 2 * (n * a_iL / b / D + n / ft_iLS * sN_iLpX * (1 - d_iR / b)));
                        Ma_iLLpX = ft_iLS / n / (1 - xn1 - d_iR / b) * (Math.Pow(xn1, 3) / 3 + n * a_iL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iR / b, 2) - 2 * d_iR / b + 1)) + sN_iLpX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iLLpX = ft_iLS * a_iL / b / D * (1 - 2 * d_iR / b) + sN_iLpX * (0.5 - d_iR / b);
                    }
                    //L-X上端引張/////////////////////////////////////////////////////////////////////////////////////
                    if (sN_iLmX > sN1_iTS)
                    {
                        var xn1 = (0.5 + n * a_iT / b / D) / (1 + 2 * n * a_iT / b / D - sN_iLmX / fcS);
                        Ma_iTLmX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iT / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iB / D, 2) - 2 * xn1 - 2 * d_iB / D + 1)) + sN_iLmX * (0.5 - xn1);
                    }
                    else if (sN_iLmX > sN2_iTS)
                    {
                        var xn1 = sN_iLmX / fcS - 2 * n * a_iT / b / D + Math.Sqrt(Math.Pow(2 * n * a_iT / b / D - sN_iLmX / fcS, 2) + 2 * n * a_iT / b / D);
                        Ma_iTLmX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iB / D, 2) - 2 * d_iB / D + 1)) + sN_iLmX * (0.5 - xn1);
                    }
                    else if (sN_iLmX > sN3_iTS)
                    {
                        var xn1 = -(n / ft_iTS * sN_iLmX + 2 * n * a_iT / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iT / b / D + n / ft_iTS * sN_iLmX, 2) + 2 * (n * a_iT / b / D + n / ft_iTS * sN_iLmX * (1 - d_iB / D)));
                        Ma_iTLmX = ft_iTS / n / (1 - xn1 - d_iB / D) * (Math.Pow(xn1, 3) / 3 + n * a_iT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iB / D, 2) - 2 * d_iB / D + 1)) + sN_iLmX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iTLmX = ft_iTS * a_iT / b / D * (1 - 2 * d_iB / D) + sN_iLmX * (0.5 - d_iB / D);
                    }
                    //L-X下端引張
                    if (sN_iLmX > sN1_iBS)
                    {
                        var xn1 = (0.5 + n * a_iB / b / D) / (1 + 2 * n * a_iB / b / D - sN_iLmX / fcS);
                        Ma_iBLmX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iB / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iT / D, 2) - 2 * xn1 - 2 * d_iT / D + 1)) + sN_iLmX * (0.5 - xn1);
                    }
                    else if (sN_iLmX > sN2_iBS)
                    {
                        var xn1 = sN_iLmX / fcS - 2 * n * a_iB / b / D + Math.Sqrt(Math.Pow(2 * n * a_iB / b / D - sN_iLmX / fcS, 2) + 2 * n * a_iB / b / D);
                        Ma_iBLmX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iT / D, 2) - 2 * d_iT / D + 1)) + sN_iLmX * (0.5 - xn1);
                    }
                    else if (sN_iLmX > sN3_iBS)
                    {
                        var xn1 = -(n / ft_iBS * sN_iLmX + 2 * n * a_iB / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iB / b / D + n / ft_iBS * sN_iLmX, 2) + 2 * (n * a_iB / b / D + n / ft_iBS * sN_iLmX * (1 - d_iT / D)));
                        Ma_iBLmX = ft_iBS / n / (1 - xn1 - d_iT / D) * (Math.Pow(xn1, 3) / 3 + n * a_iB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iT / D, 2) - 2 * d_iT / D + 1)) + sN_iLmX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iBLmX = ft_iBS * a_iB / b / D * (1 - 2 * d_iT / D) + sN_iLmX * (0.5 - d_iT / D);
                    }
                    //L-X右端引張
                    if (sN_iLmX > sN1_iRS)
                    {
                        var xn1 = (0.5 + n * a_iR / b / D) / (1 + 2 * n * a_iR / b / D - sN_iLmX / fcS);
                        Ma_iRLmX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iR / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iL / b, 2) - 2 * xn1 - 2 * d_iL / b + 1)) + sN_iLmX * (0.5 - xn1);
                    }
                    else if (sN_iLmX > sN2_iRS)
                    {
                        var xn1 = sN_iLmX / fcS - 2 * n * a_iR / b / D + Math.Sqrt(Math.Pow(2 * n * a_iR / b / D - sN_iLmX / fcS, 2) + 2 * n * a_iR / b / D);
                        Ma_iRLmX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iL / b, 2) - 2 * d_iL / b + 1)) + sN_iLmX * (0.5 - xn1);
                    }
                    else if (sN_iLmX > sN3_iRS)
                    {
                        var xn1 = -(n / ft_iRS * sN_iLmX + 2 * n * a_iR / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iR / b / D + n / ft_iRS * sN_iLmX, 2) + 2 * (n * a_iR / b / D + n / ft_iRS * sN_iLmX * (1 - d_iL / b)));
                        Ma_iRLmX = ft_iRS / n / (1 - xn1 - d_iL / b) * (Math.Pow(xn1, 3) / 3 + n * a_iR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iL / b, 2) - 2 * d_iL / b + 1)) + sN_iLmX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iRLmX = ft_iRS * a_iR / b / D * (1 - 2 * d_iL / b) + sN_iLmX * (0.5 - d_iL / b);
                    }
                    //L-X左端引張
                    if (sN_iLmX > sN1_iLS)
                    {
                        var xn1 = (0.5 + n * a_iL / b / D) / (1 + 2 * n * a_iL / b / D - sN_iLmX / fcS);
                        Ma_iLLmX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iL / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iR / b, 2) - 2 * xn1 - 2 * d_iR / b + 1)) + sN_iLmX * (0.5 - xn1);
                    }
                    else if (sN_iLmX > sN2_iLS)
                    {
                        var xn1 = sN_iLmX / fcS - 2 * n * a_iL / b / D + Math.Sqrt(Math.Pow(2 * n * a_iL / b / D - sN_iLmX / fcS, 2) + 2 * n * a_iL / b / D);
                        Ma_iLLmX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iR / b, 2) - 2 * d_iR / b + 1)) + sN_iLmX * (0.5 - xn1);
                    }
                    else if (sN_iLmX > sN3_iLS)
                    {
                        var xn1 = -(n / ft_iLS * sN_iLmX + 2 * n * a_iL / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iL / b / D + n / ft_iLS * sN_iLmX, 2) + 2 * (n * a_iL / b / D + n / ft_iLS * sN_iLmX * (1 - d_iR / b)));
                        Ma_iLLmX = ft_iLS / n / (1 - xn1 - d_iR / b) * (Math.Pow(xn1, 3) / 3 + n * a_iL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iR / b, 2) - 2 * d_iR / b + 1)) + sN_iLmX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iLLmX = ft_iLS * a_iL / b / D * (1 - 2 * d_iR / b) + sN_iLmX * (0.5 - d_iR / b);
                    }
                    //L+Y上端引張/////////////////////////////////////////////////////////////////////////////////////
                    if (sN_iLpY > sN1_iTS)
                    {
                        var xn1 = (0.5 + n * a_iT / b / D) / (1 + 2 * n * a_iT / b / D - sN_iLpY / fcS);
                        Ma_iTLpY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iT / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iB / D, 2) - 2 * xn1 - 2 * d_iB / D + 1)) + sN_iLpY * (0.5 - xn1);
                    }
                    else if (sN_iLpY > sN2_iTS)
                    {
                        var xn1 = sN_iLpY / fcS - 2 * n * a_iT / b / D + Math.Sqrt(Math.Pow(2 * n * a_iT / b / D - sN_iLpY / fcS, 2) + 2 * n * a_iT / b / D);
                        Ma_iTLpY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iB / D, 2) - 2 * d_iB / D + 1)) + sN_iLpY * (0.5 - xn1);
                    }
                    else if (sN_iLpY > sN3_iTS)
                    {
                        var xn1 = -(n / ft_iTS * sN_iLpY + 2 * n * a_iT / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iT / b / D + n / ft_iTS * sN_iLpY, 2) + 2 * (n * a_iT / b / D + n / ft_iTS * sN_iLpY * (1 - d_iB / D)));
                        Ma_iTLpY = ft_iTS / n / (1 - xn1 - d_iB / D) * (Math.Pow(xn1, 3) / 3 + n * a_iT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iB / D, 2) - 2 * d_iB / D + 1)) + sN_iLpY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iTLpY = ft_iTS * a_iT / b / D * (1 - 2 * d_iB / D) + sN_iLpY * (0.5 - d_iB / D);
                    }
                    //L+Y下端引張
                    if (sN_iLpY > sN1_iBS)
                    {
                        var xn1 = (0.5 + n * a_iB / b / D) / (1 + 2 * n * a_iB / b / D - sN_iLpY / fcS);
                        Ma_iBLpY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iB / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iT / D, 2) - 2 * xn1 - 2 * d_iT / D + 1)) + sN_iLpY * (0.5 - xn1);
                    }
                    else if (sN_iLpY > sN2_iBS)
                    {
                        var xn1 = sN_iLpY / fcS - 2 * n * a_iB / b / D + Math.Sqrt(Math.Pow(2 * n * a_iB / b / D - sN_iLpY / fcS, 2) + 2 * n * a_iB / b / D);
                        Ma_iBLpY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iT / D, 2) - 2 * d_iT / D + 1)) + sN_iLpY * (0.5 - xn1);
                    }
                    else if (sN_iLpY > sN3_iBS)
                    {
                        var xn1 = -(n / ft_iBS * sN_iLpY + 2 * n * a_iB / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iB / b / D + n / ft_iBS * sN_iLpY, 2) + 2 * (n * a_iB / b / D + n / ft_iBS * sN_iLpY * (1 - d_iT / D)));
                        Ma_iBLpY = ft_iBS / n / (1 - xn1 - d_iT / D) * (Math.Pow(xn1, 3) / 3 + n * a_iB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iT / D, 2) - 2 * d_iT / D + 1)) + sN_iLpY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iBLpY = ft_iBS * a_iB / b / D * (1 - 2 * d_iT / D) + sN_iLpY * (0.5 - d_iT / D);
                    }
                    //L+Y右端引張
                    if (sN_iLpY > sN1_iRS)
                    {
                        var xn1 = (0.5 + n * a_iR / b / D) / (1 + 2 * n * a_iR / b / D - sN_iLpY / fcS);
                        Ma_iRLpY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iR / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iL / b, 2) - 2 * xn1 - 2 * d_iL / b + 1)) + sN_iLpY * (0.5 - xn1);
                    }
                    else if (sN_iLpY > sN2_iRS)
                    {
                        var xn1 = sN_iLpY / fcS - 2 * n * a_iR / b / D + Math.Sqrt(Math.Pow(2 * n * a_iR / b / D - sN_iLpY / fcS, 2) + 2 * n * a_iR / b / D);
                        Ma_iRLpY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iL / b, 2) - 2 * d_iL / b + 1)) + sN_iLpY * (0.5 - xn1);
                    }
                    else if (sN_iLpY > sN3_iRS)
                    {
                        var xn1 = -(n / ft_iRS * sN_iLpY + 2 * n * a_iR / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iR / b / D + n / ft_iRS * sN_iLpY, 2) + 2 * (n * a_iR / b / D + n / ft_iRS * sN_iLpY * (1 - d_iL / b)));
                        Ma_iRLpY = ft_iRS / n / (1 - xn1 - d_iL / b) * (Math.Pow(xn1, 3) / 3 + n * a_iR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iL / b, 2) - 2 * d_iL / b + 1)) + sN_iLpY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iRLpY = ft_iRS * a_iR / b / D * (1 - 2 * d_iL / b) + sN_iLpY * (0.5 - d_iL / b);
                    }
                    //L+Y左端引張
                    if (sN_iLpY > sN1_iLS)
                    {
                        var xn1 = (0.5 + n * a_iL / b / D) / (1 + 2 * n * a_iL / b / D - sN_iLpY / fcS);
                        Ma_iLLpY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iL / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iR / b, 2) - 2 * xn1 - 2 * d_iR / b + 1)) + sN_iLpY * (0.5 - xn1);
                    }
                    else if (sN_iLpY > sN2_iLS)
                    {
                        var xn1 = sN_iLpY / fcS - 2 * n * a_iL / b / D + Math.Sqrt(Math.Pow(2 * n * a_iL / b / D - sN_iLpY / fcS, 2) + 2 * n * a_iL / b / D);
                        Ma_iLLpY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iR / b, 2) - 2 * d_iR / b + 1)) + sN_iLpY * (0.5 - xn1);
                    }
                    else if (sN_iLpY > sN3_iLS)
                    {
                        var xn1 = -(n / ft_iLS * sN_iLpY + 2 * n * a_iL / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iL / b / D + n / ft_iLS * sN_iLpY, 2) + 2 * (n * a_iL / b / D + n / ft_iLS * sN_iLpY * (1 - d_iR / b)));
                        Ma_iLLpY = ft_iLS / n / (1 - xn1 - d_iR / b) * (Math.Pow(xn1, 3) / 3 + n * a_iL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iR / b, 2) - 2 * d_iR / b + 1)) + sN_iLpY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iLLpY = ft_iLS * a_iL / b / D * (1 - 2 * d_iR / b) + sN_iLpY * (0.5 - d_iR / b);
                    }
                    //L-Y上端引張/////////////////////////////////////////////////////////////////////////////////////
                    if (sN_iLmY > sN1_iTS)
                    {
                        var xn1 = (0.5 + n * a_iT / b / D) / (1 + 2 * n * a_iT / b / D - sN_iLmY / fcS);
                        Ma_iTLmY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iT / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iB / D, 2) - 2 * xn1 - 2 * d_iB / D + 1)) + sN_iLmY * (0.5 - xn1);
                    }
                    else if (sN_iLmY > sN2_iTS)
                    {
                        var xn1 = sN_iLmY / fcS - 2 * n * a_iT / b / D + Math.Sqrt(Math.Pow(2 * n * a_iT / b / D - sN_iLmY / fcS, 2) + 2 * n * a_iT / b / D);
                        Ma_iTLmY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iB / D, 2) - 2 * d_iB / D + 1)) + sN_iLmY * (0.5 - xn1);
                    }
                    else if (sN_iLmY > sN3_iTS)
                    {
                        var xn1 = -(n / ft_iTS * sN_iLmY + 2 * n * a_iT / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iT / b / D + n / ft_iTS * sN_iLmY, 2) + 2 * (n * a_iT / b / D + n / ft_iTS * sN_iLmY * (1 - d_iB / D)));
                        Ma_iTLmY = ft_iTS / n / (1 - xn1 - d_iB / D) * (Math.Pow(xn1, 3) / 3 + n * a_iT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iB / D, 2) - 2 * d_iB / D + 1)) + sN_iLmY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iTLmY = ft_iTS * a_iT / b / D * (1 - 2 * d_iB / D) + sN_iLmY * (0.5 - d_iB / D);
                    }
                    //L-Y下端引張
                    if (sN_iLmY > sN1_iBS)
                    {
                        var xn1 = (0.5 + n * a_iB / b / D) / (1 + 2 * n * a_iB / b / D - sN_iLmY / fcS);
                        Ma_iBLmY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iB / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iT / D, 2) - 2 * xn1 - 2 * d_iT / D + 1)) + sN_iLmY * (0.5 - xn1);
                    }
                    else if (sN_iLmY > sN2_iBS)
                    {
                        var xn1 = sN_iLmY / fcS - 2 * n * a_iB / b / D + Math.Sqrt(Math.Pow(2 * n * a_iB / b / D - sN_iLmY / fcS, 2) + 2 * n * a_iB / b / D);
                        Ma_iBLmY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iT / D, 2) - 2 * d_iT / D + 1)) + sN_iLmY * (0.5 - xn1);
                    }
                    else if (sN_iLmY > sN3_iBS)
                    {
                        var xn1 = -(n / ft_iBS * sN_iLmY + 2 * n * a_iB / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iB / b / D + n / ft_iBS * sN_iLmY, 2) + 2 * (n * a_iB / b / D + n / ft_iBS * sN_iLmY * (1 - d_iT / D)));
                        Ma_iBLmY = ft_iBS / n / (1 - xn1 - d_iT / D) * (Math.Pow(xn1, 3) / 3 + n * a_iB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iT / D, 2) - 2 * d_iT / D + 1)) + sN_iLmY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iBLmY = ft_iBS * a_iB / b / D * (1 - 2 * d_iT / D) + sN_iLmY * (0.5 - d_iT / D);
                    }
                    //L-Y右端引張
                    if (sN_iLmY > sN1_iRS)
                    {
                        var xn1 = (0.5 + n * a_iR / b / D) / (1 + 2 * n * a_iR / b / D - sN_iLmY / fcS);
                        Ma_iRLmY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iR / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iL / b, 2) - 2 * xn1 - 2 * d_iL / b + 1)) + sN_iLmY * (0.5 - xn1);
                    }
                    else if (sN_iLmY > sN2_iRS)
                    {
                        var xn1 = sN_iLmY / fcS - 2 * n * a_iR / b / D + Math.Sqrt(Math.Pow(2 * n * a_iR / b / D - sN_iLmY / fcS, 2) + 2 * n * a_iR / b / D);
                        Ma_iRLmY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iL / b, 2) - 2 * d_iL / b + 1)) + sN_iLmY * (0.5 - xn1);
                    }
                    else if (sN_iLmY > sN3_iRS)
                    {
                        var xn1 = -(n / ft_iRS * sN_iLmY + 2 * n * a_iR / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iR / b / D + n / ft_iRS * sN_iLmY, 2) + 2 * (n * a_iR / b / D + n / ft_iRS * sN_iLmY * (1 - d_iL / b)));
                        Ma_iRLmY = ft_iRS / n / (1 - xn1 - d_iL / b) * (Math.Pow(xn1, 3) / 3 + n * a_iR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iL / b, 2) - 2 * d_iL / b + 1)) + sN_iLmY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iRLmY = ft_iRS * a_iR / b / D * (1 - 2 * d_iL / b) + sN_iLmY * (0.5 - d_iL / b);
                    }
                    //L-Y左端引張
                    if (sN_iLmY > sN1_iLS)
                    {
                        var xn1 = (0.5 + n * a_iL / b / D) / (1 + 2 * n * a_iL / b / D - sN_iLmY / fcS);
                        Ma_iLLmY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_iL / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_iR / b, 2) - 2 * xn1 - 2 * d_iR / b + 1)) + sN_iLmY * (0.5 - xn1);
                    }
                    else if (sN_iLmY > sN2_iLS)
                    {
                        var xn1 = sN_iLmY / fcS - 2 * n * a_iL / b / D + Math.Sqrt(Math.Pow(2 * n * a_iL / b / D - sN_iLmY / fcS, 2) + 2 * n * a_iL / b / D);
                        Ma_iLLmY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_iL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iR / b, 2) - 2 * d_iR / b + 1)) + sN_iLmY * (0.5 - xn1);
                    }
                    else if (sN_iLmY > sN3_iLS)
                    {
                        var xn1 = -(n / ft_iLS * sN_iLmY + 2 * n * a_iL / b / D) + Math.Sqrt(Math.Pow(2 * n * a_iL / b / D + n / ft_iLS * sN_iLmY, 2) + 2 * (n * a_iL / b / D + n / ft_iLS * sN_iLmY * (1 - d_iR / b)));
                        Ma_iLLmY = ft_iLS / n / (1 - xn1 - d_iR / b) * (Math.Pow(xn1, 3) / 3 + n * a_iL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_iR / b, 2) - 2 * d_iR / b + 1)) + sN_iLmY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_iLLmY = ft_iLS * a_iL / b / D * (1 - 2 * d_iR / b) + sN_iLmY * (0.5 - d_iR / b);
                    }
                    Ma_iTL = Ma_iTL * b * D * D * unitl * unitf; Ma_iBL = Ma_iBL * b * D * D * unitl * unitf; Ma_iRL = Ma_iRL * b * b * D * unitl * unitf; Ma_iLL = Ma_iLL * b * b * D * unitl * unitf;
                    Ma_iTLpX = Ma_iTLpX * b * D * D * unitl * unitf; Ma_iBLpX = Ma_iBLpX * b * D * D * unitl * unitf; Ma_iRLpX = Ma_iRLpX * b * b * D * unitl * unitf; Ma_iLLpX = Ma_iLLpX * b * b * D * unitl * unitf;
                    Ma_iTLmX = Ma_iTLmX * b * D * D * unitl * unitf; Ma_iBLmX = Ma_iBLmX * b * D * D * unitl * unitf; Ma_iRLmX = Ma_iRLmX * b * b * D * unitl * unitf; Ma_iLLmX = Ma_iLLmX * b * b * D * unitl * unitf;
                    Ma_iTLpY = Ma_iTLpY * b * D * D * unitl * unitf; Ma_iBLpY = Ma_iBLpY * b * D * D * unitl * unitf; Ma_iRLpY = Ma_iRLpY * b * b * D * unitl * unitf; Ma_iLLpY = Ma_iLLpY * b * b * D * unitl * unitf;
                    Ma_iTLmY = Ma_iTLmY * b * D * D * unitl * unitf; Ma_iBLmY = Ma_iBLmY * b * D * D * unitl * unitf; Ma_iRLmY = Ma_iRLmY * b * b * D * unitl * unitf; Ma_iLLmY = Ma_iLLmY * b * b * D * unitl * unitf;
                    //************************************************************************************************************************************************************
                    //中央の許容曲げモーメント
                    //************************************************************************************************************************************************************
                    var a_cT = n_cT * Math.Pow(D_cT, 2) * Math.PI / 4.0;//i端上端主筋断面積
                    var a_cB = n_cB * Math.Pow(D_cB, 2) * Math.PI / 4.0;//i端下端主筋断面積
                    var a_cR = n_cR * Math.Pow(D_cR, 2) * Math.PI / 4.0;//i端右端主筋断面積
                    var a_cL = n_cL * Math.Pow(D_cL, 2) * Math.PI / 4.0;//i端左端主筋断面積
                    var ft_cTL = 195.0; var ft_cBL = 195.0; var ft_cRL = 195.0; var ft_cLL = 195.0;
                    if (D_cT > 18.9 && D_cT < 28.9) { ft_cTL = 215.0; }//i端上端主筋許容引張応力度
                    if (D_cB > 18.9 && D_cB < 28.9) { ft_cBL = 215.0; }//i端下端主筋許容引張応力度
                    if (D_cR > 18.9 && D_cR < 28.9) { ft_cRL = 215.0; }//i端右端主筋許容引張応力度
                    if (D_cL > 18.9 && D_cL < 28.9) { ft_cLL = 215.0; }//i端左端主筋許容引張応力度
                    var ft_cTS = 295.0; var ft_cBS = 295.0; var ft_cRS = 295.0; var ft_cLS = 295.0;
                    if (D_cT > 18.9 && D_cT < 28.9) { ft_cTS = 345.0; }//i端上端主筋許容引張応力度
                    else if (D_cT > 28.9) { ft_cTS = 390.0; }
                    if (D_cB > 18.9 && D_cB < 28.9) { ft_cBS = 345.0; }//i端下端主筋許容引張応力度
                    else if (D_cB > 28.9) { ft_cBS = 390.0; }
                    if (D_cR > 18.9 && D_cR < 28.9) { ft_cRS = 345.0; }//i端上端主筋許容引張応力度
                    else if (D_cR > 28.9) { ft_cRS = 390.0; }
                    if (D_cL > 18.9 && D_cL < 28.9) { ft_cLS = 345.0; }//i端下端主筋許容引張応力度
                    else if (D_cL > 28.9) { ft_cLS = 390.0; }
                    FtiT.Add(new List<string> { ft_cTL.ToString().Substring(0, Digit((int)ft_cTL)), "", ft_cTS.ToString().Substring(0, Digit((int)ft_cTS)) });
                    var d_cT = kT + S_c + D_cT / 2.0;//i端の上端より鉄筋重心までの距離
                    FtiB.Add(new List<string> { ft_cBL.ToString().Substring(0, Digit((int)ft_cBL)), "", ft_cBS.ToString().Substring(0, Digit((int)ft_cBS)) });
                    var d_cB = kB + S_c + D_cB / 2.0;//i端の下端より鉄筋重心までの距離
                    FtiR.Add(new List<string> { ft_cRL.ToString().Substring(0, Digit((int)ft_cRL)), "", ft_cRS.ToString().Substring(0, Digit((int)ft_cRS)) });
                    var d_cR = kR + S_c2 + D_cR / 2.0;//i端の右端より鉄筋重心までの距離
                    FtiL.Add(new List<string> { ft_cLL.ToString().Substring(0, Digit((int)ft_cLL)), "", ft_cLS.ToString().Substring(0, Digit((int)ft_cLS)) });
                    var d_cL = kL + S_c2 + D_cL / 2.0;//i端の下端より鉄筋重心までの距離
                    var sN_cL = Ni * 1000 / b / D;//N/mm2
                    var sN_cLpX = (Ni + Ni_x) * 1000 / b / D; var sN_cLmX = (Ni + Ni_x2) * 1000 / b / D;
                    var sN_cLpY = (Ni + Ni_y) * 1000 / b / D; var sN_cLmY = (Ni + Ni_y2) * 1000 / b / D;
                    var x_cTL = (1 - d_cB / D) / (1 + ft_cTL / (n * fcL)); var x_cBL = (1 - d_cT / D) / (1 + ft_cBL / (n * fcL));
                    var x_cRL = (1 - d_cL / b) / (1 + ft_cRL / (n * fcL)); var x_cLL = (1 - d_cR / b) / (1 + ft_cLL / (n * fcL));
                    var x_cTS = (1 - d_cB / D) / (1 + ft_cTS / (n * fcS)); var x_cBS = (1 - d_cT / D) / (1 + ft_cBS / (n * fcS));
                    var x_cRS = (1 - d_cL / b) / (1 + ft_cRS / (n * fcS)); var x_cLS = (1 - d_cR / b) / (1 + ft_cLS / (n * fcS));
                    var sN1_cTL = fcL * (0.5 + n * a_cT / b / D); var sN1_cBL = fcL * (0.5 + n * a_cB / b / D); var sN1_cRL = fcL * (0.5 + n * a_cR / b / D); var sN1_cLL = fcL * (0.5 + n * a_cL / b / D);
                    var sN1_cTS = fcS * (0.5 + n * a_cT / b / D); var sN1_cBS = fcS * (0.5 + n * a_cB / b / D); var sN1_cRS = fcS * (0.5 + n * a_cR / b / D); var sN1_cLS = fcS * (0.5 + n * a_cL / b / D);
                    var sN2_cTL = fcL * (x_cTL / 2.0 + n * a_cT / b / D * (2 - 1.0 / x_cTL)); var sN2_cBL = fcL * (x_cBL / 2.0 + n * a_cB / b / D * (2 - 1.0 / x_cBL)); var sN2_cRL = fcL * (x_cRL / 2.0 + n * a_cR / b / D * (2 - 1.0 / x_cRL)); var sN2_cLL = fcL * (x_cLL / 2.0 + n * a_cL / b / D * (2 - 1.0 / x_cLL));
                    var sN2_cTS = fcS * (x_cTS / 2.0 + n * a_cT / b / D * (2 - 1.0 / x_cTS)); var sN2_cBS = fcS * (x_cBS / 2.0 + n * a_cB / b / D * (2 - 1.0 / x_cBS)); var sN2_cRS = fcS * (x_cRS / 2.0 + n * a_cR / b / D * (2 - 1.0 / x_cRS)); var sN2_cLS = fcL * (x_cLS / 2.0 + n * a_cL / b / D * (2 - 1.0 / x_cLS));
                    var sN3_cTL = ft_cTL * a_cT / b / D / (d_cB / D - 1); var sN3_cBL = ft_cBL * a_cB / b / D / (d_cT / D - 1); var sN3_cRL = ft_cRL * a_cR / b / D / (d_cL / b - 1); var sN3_cLL = ft_cLL * a_cL / b / D / (d_cR / b - 1);
                    var sN3_cTS = ft_cTS * a_cT / b / D / (d_cB / D - 1); var sN3_cBS = ft_cBS * a_cB / b / D / (d_cT / D - 1); var sN3_cRS = ft_cRS * a_cR / b / D / (d_cL / b - 1); var sN3_cLS = ft_cLS * a_cL / b / D / (d_cR / b - 1);
                    var Ma_cTL = 0.0; var Ma_cBL = 0.0; var Ma_cRL = 0.0; var Ma_cLL = 0.0;
                    var Ma_cTLpX = 0.0; var Ma_cBLpX = 0.0; var Ma_cRLpX = 0.0; var Ma_cLLpX = 0.0;
                    var Ma_cTLmX = 0.0; var Ma_cBLmX = 0.0; var Ma_cRLmX = 0.0; var Ma_cLLmX = 0.0;
                    var Ma_cTLpY = 0.0; var Ma_cBLpY = 0.0; var Ma_cRLpY = 0.0; var Ma_cLLpY = 0.0;
                    var Ma_cTLmY = 0.0; var Ma_cBLmY = 0.0; var Ma_cRLmY = 0.0; var Ma_cLLmY = 0.0;
                    //長期上端引張/////////////////////////////////////////////////////////////////////////////////////
                    if (sN_cL > sN1_cTL)
                    {
                        var xn1 = (0.5 + n * a_cT / b / D) / (1 + 2 * n * a_cT / b / D - sN_cL / fcL);
                        Ma_cTL = fcL / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cT / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cB / D, 2) - 2 * xn1 - 2 * d_cB / D + 1)) + sN_cL * (0.5 - xn1);
                    }
                    else if (sN_cL > sN2_cTL)
                    {
                        var xn1 = sN_cL / fcL - 2 * n * a_cT / b / D + Math.Sqrt(Math.Pow(2 * n * a_cT / b / D - sN_cL / fcL, 2) + 2 * n * a_cT / b / D);
                        Ma_cTL = fcL / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cB / D, 2) - 2 * d_cB / D + 1)) + sN_cL * (0.5 - xn1);
                    }
                    else if (sN_cL > sN3_cTL)
                    {
                        var xn1 = -(n / ft_cTL * sN_cL + 2 * n * a_cT / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cT / b / D + n / ft_cTL * sN_cL, 2) + 2 * (n * a_cT / b / D + n / ft_cTL * sN_cL * (1 - d_cB / D)));
                        Ma_cTL = ft_cTL / n / (1 - xn1 - d_cB / D) * (Math.Pow(xn1, 3) / 3 + n * a_cT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cB / D, 2) - 2 * d_cB / D + 1)) + sN_cL * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cTL = ft_cTL * a_cT / b / D * (1 - 2 * d_cB / D) + sN_cL * (0.5 - d_cB / D);
                    }
                    //長期下端引張
                    if (sN_cL > sN1_cBL)
                    {
                        var xn1 = (0.5 + n * a_cB / b / D) / (1 + 2 * n * a_cB / b / D - sN_cL / fcL);
                        Ma_cBL = fcL / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cB / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cT / D, 2) - 2 * xn1 - 2 * d_cT / D + 1)) + sN_cL * (0.5 - xn1);
                    }
                    else if (sN_cL > sN2_cBL)
                    {
                        var xn1 = sN_cL / fcL - 2 * n * a_cB / b / D + Math.Sqrt(Math.Pow(2 * n * a_cB / b / D - sN_cL / fcL, 2) + 2 * n * a_cB / b / D);
                        Ma_cBL = fcL / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cT / D, 2) - 2 * d_cT / D + 1)) + sN_cL * (0.5 - xn1);
                    }
                    else if (sN_cL > sN3_cBL)
                    {
                        var xn1 = -(n / ft_cBL * sN_cL + 2 * n * a_cB / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cB / b / D + n / ft_cBL * sN_cL, 2) + 2 * (n * a_cB / b / D + n / ft_cBL * sN_cL * (1 - d_cT / D)));
                        Ma_cBL = ft_cBL / n / (1 - xn1 - d_cT / D) * (Math.Pow(xn1, 3) / 3 + n * a_cB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cT / D, 2) - 2 * d_cT / D + 1)) + sN_cL * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cBL = ft_cBL * a_cB / b / D * (1 - 2 * d_cT / D) + sN_cL * (0.5 - d_cT / D);
                    }
                    //長期右端引張
                    if (sN_cL > sN1_cRL)
                    {
                        var xn1 = (0.5 + n * a_cR / b / D) / (1 + 2 * n * a_cR / b / D - sN_cL / fcL);
                        Ma_cRL = fcL / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cR / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cL / b, 2) - 2 * xn1 - 2 * d_cL / b + 1)) + sN_cL * (0.5 - xn1);
                    }
                    else if (sN_cL > sN2_cRL)
                    {
                        var xn1 = sN_cL / fcL - 2 * n * a_cR / b / D + Math.Sqrt(Math.Pow(2 * n * a_cR / b / D - sN_cL / fcL, 2) + 2 * n * a_cR / b / D);
                        Ma_cRL = fcL / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cL / b, 2) - 2 * d_cL / b + 1)) + sN_cL * (0.5 - xn1);
                    }
                    else if (sN_cL > sN3_cRL)
                    {
                        var xn1 = -(n / ft_cRL * sN_cL + 2 * n * a_cR / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cR / b / D + n / ft_cRL * sN_cL, 2) + 2 * (n * a_cR / b / D + n / ft_cRL * sN_cL * (1 - d_cL / b)));
                        Ma_cRL = ft_cRL / n / (1 - xn1 - d_cL / b) * (Math.Pow(xn1, 3) / 3 + n * a_cR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cL / b, 2) - 2 * d_cL / b + 1)) + sN_cL * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cRL = ft_cRL * a_cR / b / D * (1 - 2 * d_cL / b) + sN_cL * (0.5 - d_cL / b);
                    }
                    //長期左端引張
                    if (sN_cL > sN1_cLL)
                    {
                        var xn1 = (0.5 + n * a_cL / b / D) / (1 + 2 * n * a_cL / b / D - sN_cL / fcL);
                        Ma_cLL = fcL / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cL / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cR / b, 2) - 2 * xn1 - 2 * d_cR / b + 1)) + sN_cL * (0.5 - xn1);
                    }
                    else if (sN_cL > sN2_cLL)
                    {
                        var xn1 = sN_cL / fcL - 2 * n * a_cL / b / D + Math.Sqrt(Math.Pow(2 * n * a_cL / b / D - sN_cL / fcL, 2) + 2 * n * a_cL / b / D);
                        Ma_cLL = fcL / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cR / b, 2) - 2 * d_cR / b + 1)) + sN_cL * (0.5 - xn1);
                    }
                    else if (sN_cL > sN3_cLL)
                    {
                        var xn1 = -(n / ft_cLL * sN_cL + 2 * n * a_cL / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cL / b / D + n / ft_cLL * sN_cL, 2) + 2 * (n * a_cL / b / D + n / ft_cLL * sN_cL * (1 - d_cR / b)));
                        Ma_cLL = ft_cLL / n / (1 - xn1 - d_cR / b) * (Math.Pow(xn1, 3) / 3 + n * a_cL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cR / b, 2) - 2 * d_cR / b + 1)) + sN_cL * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cLL = ft_cLL * a_cL / b / D * (1 - 2 * d_cR / b) + sN_cL * (0.5 - d_cR / b);
                    }

                    //L+X上端引張/////////////////////////////////////////////////////////////////////////////////////
                    if (sN_cLpX > sN1_cTS)
                    {
                        var xn1 = (0.5 + n * a_cT / b / D) / (1 + 2 * n * a_cT / b / D - sN_cLpX / fcS);
                        Ma_cTLpX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cT / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cB / D, 2) - 2 * xn1 - 2 * d_cB / D + 1)) + sN_cLpX * (0.5 - xn1);
                    }
                    else if (sN_cLpX > sN2_cTS)
                    {
                        var xn1 = sN_cLpX / fcS - 2 * n * a_cT / b / D + Math.Sqrt(Math.Pow(2 * n * a_cT / b / D - sN_cLpX / fcS, 2) + 2 * n * a_cT / b / D);
                        Ma_cTLpX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cB / D, 2) - 2 * d_cB / D + 1)) + sN_cLpX * (0.5 - xn1);
                    }
                    else if (sN_cLpX > sN3_cTS)
                    {
                        var xn1 = -(n / ft_cTS * sN_cLpX + 2 * n * a_cT / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cT / b / D + n / ft_cTS * sN_cLpX, 2) + 2 * (n * a_cT / b / D + n / ft_cTS * sN_cLpX * (1 - d_cB / D)));
                        Ma_cTLpX = ft_cTS / n / (1 - xn1 - d_cB / D) * (Math.Pow(xn1, 3) / 3 + n * a_cT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cB / D, 2) - 2 * d_cB / D + 1)) + sN_cLpX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cTLpX = ft_cTS * a_cT / b / D * (1 - 2 * d_cB / D) + sN_cLpX * (0.5 - d_cB / D);
                    }
                    //L+X下端引張
                    if (sN_cLpX > sN1_cBS)
                    {
                        var xn1 = (0.5 + n * a_cB / b / D) / (1 + 2 * n * a_cB / b / D - sN_cLpX / fcS);
                        Ma_cBLpX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cB / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cT / D, 2) - 2 * xn1 - 2 * d_cT / D + 1)) + sN_cLpX * (0.5 - xn1);
                    }
                    else if (sN_cLpX > sN2_cBS)
                    {
                        var xn1 = sN_cLpX / fcS - 2 * n * a_cB / b / D + Math.Sqrt(Math.Pow(2 * n * a_cB / b / D - sN_cLpX / fcS, 2) + 2 * n * a_cB / b / D);
                        Ma_cBLpX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cT / D, 2) - 2 * d_cT / D + 1)) + sN_cLpX * (0.5 - xn1);
                    }
                    else if (sN_cLpX > sN3_cBS)
                    {
                        var xn1 = -(n / ft_cBS * sN_cLpX + 2 * n * a_cB / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cB / b / D + n / ft_cBS * sN_cLpX, 2) + 2 * (n * a_cB / b / D + n / ft_cBS * sN_cLpX * (1 - d_cT / D)));
                        Ma_cBLpX = ft_cBS / n / (1 - xn1 - d_cT / D) * (Math.Pow(xn1, 3) / 3 + n * a_cB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cT / D, 2) - 2 * d_cT / D + 1)) + sN_cLpX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cBLpX = ft_cBS * a_cB / b / D * (1 - 2 * d_cT / D) + sN_cLpX * (0.5 - d_cT / D);
                    }
                    //L+X右端引張
                    if (sN_cLpX > sN1_cRS)
                    {
                        var xn1 = (0.5 + n * a_cR / b / D) / (1 + 2 * n * a_cR / b / D - sN_cLpX / fcS);
                        Ma_cRLpX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cR / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cL / b, 2) - 2 * xn1 - 2 * d_cL / b + 1)) + sN_cLpX * (0.5 - xn1);
                    }
                    else if (sN_cLpX > sN2_cRS)
                    {
                        var xn1 = sN_cLpX / fcS - 2 * n * a_cR / b / D + Math.Sqrt(Math.Pow(2 * n * a_cR / b / D - sN_cLpX / fcS, 2) + 2 * n * a_cR / b / D);
                        Ma_cRLpX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cL / b, 2) - 2 * d_cL / b + 1)) + sN_cLpX * (0.5 - xn1);
                    }
                    else if (sN_cLpX > sN3_cRS)
                    {
                        var xn1 = -(n / ft_cRS * sN_cLpX + 2 * n * a_cR / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cR / b / D + n / ft_cRS * sN_cLpX, 2) + 2 * (n * a_cR / b / D + n / ft_cRS * sN_cLpX * (1 - d_cL / b)));
                        Ma_cRLpX = ft_cRS / n / (1 - xn1 - d_cL / b) * (Math.Pow(xn1, 3) / 3 + n * a_cR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cL / b, 2) - 2 * d_cL / b + 1)) + sN_cLpX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cRLpX = ft_cRS * a_cR / b / D * (1 - 2 * d_cL / b) + sN_cLpX * (0.5 - d_cL / b);
                    }
                    //L+X左端引張
                    if (sN_cLpX > sN1_cLS)
                    {
                        var xn1 = (0.5 + n * a_cL / b / D) / (1 + 2 * n * a_cL / b / D - sN_cLpX / fcS);
                        Ma_cLLpX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cL / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cR / b, 2) - 2 * xn1 - 2 * d_cR / b + 1)) + sN_cLpX * (0.5 - xn1);
                    }
                    else if (sN_cLpX > sN2_cLS)
                    {
                        var xn1 = sN_cLpX / fcS - 2 * n * a_cL / b / D + Math.Sqrt(Math.Pow(2 * n * a_cL / b / D - sN_cLpX / fcS, 2) + 2 * n * a_cL / b / D);
                        Ma_cLLpX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cR / b, 2) - 2 * d_cR / b + 1)) + sN_cLpX * (0.5 - xn1);
                    }
                    else if (sN_cLpX > sN3_cLS)
                    {
                        var xn1 = -(n / ft_cLS * sN_cLpX + 2 * n * a_cL / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cL / b / D + n / ft_cLS * sN_cLpX, 2) + 2 * (n * a_cL / b / D + n / ft_cLS * sN_cLpX * (1 - d_cR / b)));
                        Ma_cLLpX = ft_cLS / n / (1 - xn1 - d_cR / b) * (Math.Pow(xn1, 3) / 3 + n * a_cL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cR / b, 2) - 2 * d_cR / b + 1)) + sN_cLpX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cLLpX = ft_cLS * a_cL / b / D * (1 - 2 * d_cR / b) + sN_cLpX * (0.5 - d_cR / b);
                    }
                    //L-X上端引張/////////////////////////////////////////////////////////////////////////////////////
                    if (sN_cLmX > sN1_cTS)
                    {
                        var xn1 = (0.5 + n * a_cT / b / D) / (1 + 2 * n * a_cT / b / D - sN_cLmX / fcS);
                        Ma_cTLmX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cT / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cB / D, 2) - 2 * xn1 - 2 * d_cB / D + 1)) + sN_cLmX * (0.5 - xn1);
                    }
                    else if (sN_cLmX > sN2_cTS)
                    {
                        var xn1 = sN_cLmX / fcS - 2 * n * a_cT / b / D + Math.Sqrt(Math.Pow(2 * n * a_cT / b / D - sN_cLmX / fcS, 2) + 2 * n * a_cT / b / D);
                        Ma_cTLmX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cB / D, 2) - 2 * d_cB / D + 1)) + sN_cLmX * (0.5 - xn1);
                    }
                    else if (sN_cLmX > sN3_cTS)
                    {
                        var xn1 = -(n / ft_cTS * sN_cLmX + 2 * n * a_cT / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cT / b / D + n / ft_cTS * sN_cLmX, 2) + 2 * (n * a_cT / b / D + n / ft_cTS * sN_cLmX * (1 - d_cB / D)));
                        Ma_cTLmX = ft_cTS / n / (1 - xn1 - d_cB / D) * (Math.Pow(xn1, 3) / 3 + n * a_cT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cB / D, 2) - 2 * d_cB / D + 1)) + sN_cLmX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cTLmX = ft_cTS * a_cT / b / D * (1 - 2 * d_cB / D) + sN_cLmX * (0.5 - d_cB / D);
                    }
                    //L-X下端引張
                    if (sN_cLmX > sN1_cBS)
                    {
                        var xn1 = (0.5 + n * a_cB / b / D) / (1 + 2 * n * a_cB / b / D - sN_cLmX / fcS);
                        Ma_cBLmX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cB / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cT / D, 2) - 2 * xn1 - 2 * d_cT / D + 1)) + sN_cLmX * (0.5 - xn1);
                    }
                    else if (sN_cLmX > sN2_cBS)
                    {
                        var xn1 = sN_cLmX / fcS - 2 * n * a_cB / b / D + Math.Sqrt(Math.Pow(2 * n * a_cB / b / D - sN_cLmX / fcS, 2) + 2 * n * a_cB / b / D);
                        Ma_cBLmX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cT / D, 2) - 2 * d_cT / D + 1)) + sN_cLmX * (0.5 - xn1);
                    }
                    else if (sN_cLmX > sN3_cBS)
                    {
                        var xn1 = -(n / ft_cBS * sN_cLmX + 2 * n * a_cB / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cB / b / D + n / ft_cBS * sN_cLmX, 2) + 2 * (n * a_cB / b / D + n / ft_cBS * sN_cLmX * (1 - d_cT / D)));
                        Ma_cBLmX = ft_cBS / n / (1 - xn1 - d_cT / D) * (Math.Pow(xn1, 3) / 3 + n * a_cB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cT / D, 2) - 2 * d_cT / D + 1)) + sN_cLmX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cBLmX = ft_cBS * a_cB / b / D * (1 - 2 * d_cT / D) + sN_cLmX * (0.5 - d_cT / D);
                    }
                    //L-X右端引張
                    if (sN_cLmX > sN1_cRS)
                    {
                        var xn1 = (0.5 + n * a_cR / b / D) / (1 + 2 * n * a_cR / b / D - sN_cLmX / fcS);
                        Ma_cRLmX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cR / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cL / b, 2) - 2 * xn1 - 2 * d_cL / b + 1)) + sN_cLmX * (0.5 - xn1);
                    }
                    else if (sN_cLmX > sN2_cRS)
                    {
                        var xn1 = sN_cLmX / fcS - 2 * n * a_cR / b / D + Math.Sqrt(Math.Pow(2 * n * a_cR / b / D - sN_cLmX / fcS, 2) + 2 * n * a_cR / b / D);
                        Ma_cRLmX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cL / b, 2) - 2 * d_cL / b + 1)) + sN_cLmX * (0.5 - xn1);
                    }
                    else if (sN_cLmX > sN3_cRS)
                    {
                        var xn1 = -(n / ft_cRS * sN_cLmX + 2 * n * a_cR / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cR / b / D + n / ft_cRS * sN_cLmX, 2) + 2 * (n * a_cR / b / D + n / ft_cRS * sN_cLmX * (1 - d_cL / b)));
                        Ma_cRLmX = ft_cRS / n / (1 - xn1 - d_cL / b) * (Math.Pow(xn1, 3) / 3 + n * a_cR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cL / b, 2) - 2 * d_cL / b + 1)) + sN_cLmX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cRLmX = ft_cRS * a_cR / b / D * (1 - 2 * d_cL / b) + sN_cLmX * (0.5 - d_cL / b);
                    }
                    //L-X左端引張
                    if (sN_cLmX > sN1_cLS)
                    {
                        var xn1 = (0.5 + n * a_cL / b / D) / (1 + 2 * n * a_cL / b / D - sN_cLmX / fcS);
                        Ma_cLLmX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cL / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cR / b, 2) - 2 * xn1 - 2 * d_cR / b + 1)) + sN_cLmX * (0.5 - xn1);
                    }
                    else if (sN_cLmX > sN2_cLS)
                    {
                        var xn1 = sN_cLmX / fcS - 2 * n * a_cL / b / D + Math.Sqrt(Math.Pow(2 * n * a_cL / b / D - sN_cLmX / fcS, 2) + 2 * n * a_cL / b / D);
                        Ma_cLLmX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cR / b, 2) - 2 * d_cR / b + 1)) + sN_cLmX * (0.5 - xn1);
                    }
                    else if (sN_cLmX > sN3_cLS)
                    {
                        var xn1 = -(n / ft_cLS * sN_cLmX + 2 * n * a_cL / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cL / b / D + n / ft_cLS * sN_cLmX, 2) + 2 * (n * a_cL / b / D + n / ft_cLS * sN_cLmX * (1 - d_cR / b)));
                        Ma_cLLmX = ft_cLS / n / (1 - xn1 - d_cR / b) * (Math.Pow(xn1, 3) / 3 + n * a_cL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cR / b, 2) - 2 * d_cR / b + 1)) + sN_cLmX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cLLmX = ft_cLS * a_cL / b / D * (1 - 2 * d_cR / b) + sN_cLmX * (0.5 - d_cR / b);
                    }
                    //L+Y上端引張/////////////////////////////////////////////////////////////////////////////////////
                    if (sN_cLpY > sN1_cTS)
                    {
                        var xn1 = (0.5 + n * a_cT / b / D) / (1 + 2 * n * a_cT / b / D - sN_cLpY / fcS);
                        Ma_cTLpY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cT / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cB / D, 2) - 2 * xn1 - 2 * d_cB / D + 1)) + sN_cLpY * (0.5 - xn1);
                    }
                    else if (sN_cLpY > sN2_cTS)
                    {
                        var xn1 = sN_cLpY / fcS - 2 * n * a_cT / b / D + Math.Sqrt(Math.Pow(2 * n * a_cT / b / D - sN_cLpY / fcS, 2) + 2 * n * a_cT / b / D);
                        Ma_cTLpY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cB / D, 2) - 2 * d_cB / D + 1)) + sN_cLpY * (0.5 - xn1);
                    }
                    else if (sN_cLpY > sN3_cTS)
                    {
                        var xn1 = -(n / ft_cTS * sN_cLpY + 2 * n * a_cT / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cT / b / D + n / ft_cTS * sN_cLpY, 2) + 2 * (n * a_cT / b / D + n / ft_cTS * sN_cLpY * (1 - d_cB / D)));
                        Ma_cTLpY = ft_cTS / n / (1 - xn1 - d_cB / D) * (Math.Pow(xn1, 3) / 3 + n * a_cT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cB / D, 2) - 2 * d_cB / D + 1)) + sN_cLpY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cTLpY = ft_cTS * a_cT / b / D * (1 - 2 * d_cB / D) + sN_cLpY * (0.5 - d_cB / D);
                    }
                    //L+Y下端引張
                    if (sN_cLpY > sN1_cBS)
                    {
                        var xn1 = (0.5 + n * a_cB / b / D) / (1 + 2 * n * a_cB / b / D - sN_cLpY / fcS);
                        Ma_cBLpY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cB / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cT / D, 2) - 2 * xn1 - 2 * d_cT / D + 1)) + sN_cLpY * (0.5 - xn1);
                    }
                    else if (sN_cLpY > sN2_cBS)
                    {
                        var xn1 = sN_cLpY / fcS - 2 * n * a_cB / b / D + Math.Sqrt(Math.Pow(2 * n * a_cB / b / D - sN_cLpY / fcS, 2) + 2 * n * a_cB / b / D);
                        Ma_cBLpY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cT / D, 2) - 2 * d_cT / D + 1)) + sN_cLpY * (0.5 - xn1);
                    }
                    else if (sN_cLpY > sN3_cBS)
                    {
                        var xn1 = -(n / ft_cBS * sN_cLpY + 2 * n * a_cB / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cB / b / D + n / ft_cBS * sN_cLpY, 2) + 2 * (n * a_cB / b / D + n / ft_cBS * sN_cLpY * (1 - d_cT / D)));
                        Ma_cBLpY = ft_cBS / n / (1 - xn1 - d_cT / D) * (Math.Pow(xn1, 3) / 3 + n * a_cB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cT / D, 2) - 2 * d_cT / D + 1)) + sN_cLpY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cBLpY = ft_cBS * a_cB / b / D * (1 - 2 * d_cT / D) + sN_cLpY * (0.5 - d_cT / D);
                    }
                    //L+Y右端引張
                    if (sN_cLpY > sN1_cRS)
                    {
                        var xn1 = (0.5 + n * a_cR / b / D) / (1 + 2 * n * a_cR / b / D - sN_cLpY / fcS);
                        Ma_cRLpY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cR / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cL / b, 2) - 2 * xn1 - 2 * d_cL / b + 1)) + sN_cLpY * (0.5 - xn1);
                    }
                    else if (sN_cLpY > sN2_cRS)
                    {
                        var xn1 = sN_cLpY / fcS - 2 * n * a_cR / b / D + Math.Sqrt(Math.Pow(2 * n * a_cR / b / D - sN_cLpY / fcS, 2) + 2 * n * a_cR / b / D);
                        Ma_cRLpY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cL / b, 2) - 2 * d_cL / b + 1)) + sN_cLpY * (0.5 - xn1);
                    }
                    else if (sN_cLpY > sN3_cRS)
                    {
                        var xn1 = -(n / ft_cRS * sN_cLpY + 2 * n * a_cR / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cR / b / D + n / ft_cRS * sN_cLpY, 2) + 2 * (n * a_cR / b / D + n / ft_cRS * sN_cLpY * (1 - d_cL / b)));
                        Ma_cRLpY = ft_cRS / n / (1 - xn1 - d_cL / b) * (Math.Pow(xn1, 3) / 3 + n * a_cR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cL / b, 2) - 2 * d_cL / b + 1)) + sN_cLpY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cRLpY = ft_cRS * a_cR / b / D * (1 - 2 * d_cL / b) + sN_cLpY * (0.5 - d_cL / b);
                    }
                    //L+Y左端引張
                    if (sN_cLpY > sN1_cLS)
                    {
                        var xn1 = (0.5 + n * a_cL / b / D) / (1 + 2 * n * a_cL / b / D - sN_cLpY / fcS);
                        Ma_cLLpY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cL / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cR / b, 2) - 2 * xn1 - 2 * d_cR / b + 1)) + sN_cLpY * (0.5 - xn1);
                    }
                    else if (sN_cLpY > sN2_cLS)
                    {
                        var xn1 = sN_cLpY / fcS - 2 * n * a_cL / b / D + Math.Sqrt(Math.Pow(2 * n * a_cL / b / D - sN_cLpY / fcS, 2) + 2 * n * a_cL / b / D);
                        Ma_cLLpY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cR / b, 2) - 2 * d_cR / b + 1)) + sN_cLpY * (0.5 - xn1);
                    }
                    else if (sN_cLpY > sN3_cLS)
                    {
                        var xn1 = -(n / ft_cLS * sN_cLpY + 2 * n * a_cL / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cL / b / D + n / ft_cLS * sN_cLpY, 2) + 2 * (n * a_cL / b / D + n / ft_cLS * sN_cLpY * (1 - d_cR / b)));
                        Ma_cLLpY = ft_cLS / n / (1 - xn1 - d_cR / b) * (Math.Pow(xn1, 3) / 3 + n * a_cL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cR / b, 2) - 2 * d_cR / b + 1)) + sN_cLpY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cLLpY = ft_cLS * a_cL / b / D * (1 - 2 * d_cR / b) + sN_cLpY * (0.5 - d_cR / b);
                    }
                    //L-Y上端引張/////////////////////////////////////////////////////////////////////////////////////
                    if (sN_cLmY > sN1_cTS)
                    {
                        var xn1 = (0.5 + n * a_cT / b / D) / (1 + 2 * n * a_cT / b / D - sN_cLmY / fcS);
                        Ma_cTLmY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cT / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cB / D, 2) - 2 * xn1 - 2 * d_cB / D + 1)) + sN_cLmY * (0.5 - xn1);
                    }
                    else if (sN_cLmY > sN2_cTS)
                    {
                        var xn1 = sN_cLmY / fcS - 2 * n * a_cT / b / D + Math.Sqrt(Math.Pow(2 * n * a_cT / b / D - sN_cLmY / fcS, 2) + 2 * n * a_cT / b / D);
                        Ma_cTLmY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cB / D, 2) - 2 * d_cB / D + 1)) + sN_cLmY * (0.5 - xn1);
                    }
                    else if (sN_cLmY > sN3_cTS)
                    {
                        var xn1 = -(n / ft_cTS * sN_cLmY + 2 * n * a_cT / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cT / b / D + n / ft_cTS * sN_cLmY, 2) + 2 * (n * a_cT / b / D + n / ft_cTS * sN_cLmY * (1 - d_cB / D)));
                        Ma_cTLmY = ft_cTS / n / (1 - xn1 - d_cB / D) * (Math.Pow(xn1, 3) / 3 + n * a_cT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cB / D, 2) - 2 * d_cB / D + 1)) + sN_cLmY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cTLmY = ft_cTS * a_cT / b / D * (1 - 2 * d_cB / D) + sN_cLmY * (0.5 - d_cB / D);
                    }
                    //L-Y下端引張
                    if (sN_cLmY > sN1_cBS)
                    {
                        var xn1 = (0.5 + n * a_cB / b / D) / (1 + 2 * n * a_cB / b / D - sN_cLmY / fcS);
                        Ma_cBLmY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cB / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cT / D, 2) - 2 * xn1 - 2 * d_cT / D + 1)) + sN_cLmY * (0.5 - xn1);
                    }
                    else if (sN_cLmY > sN2_cBS)
                    {
                        var xn1 = sN_cLmY / fcS - 2 * n * a_cB / b / D + Math.Sqrt(Math.Pow(2 * n * a_cB / b / D - sN_cLmY / fcS, 2) + 2 * n * a_cB / b / D);
                        Ma_cBLmY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cT / D, 2) - 2 * d_cT / D + 1)) + sN_cLmY * (0.5 - xn1);
                    }
                    else if (sN_cLmY > sN3_cBS)
                    {
                        var xn1 = -(n / ft_cBS * sN_cLmY + 2 * n * a_cB / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cB / b / D + n / ft_cBS * sN_cLmY, 2) + 2 * (n * a_cB / b / D + n / ft_cBS * sN_cLmY * (1 - d_cT / D)));
                        Ma_cBLmY = ft_cBS / n / (1 - xn1 - d_cT / D) * (Math.Pow(xn1, 3) / 3 + n * a_cB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cT / D, 2) - 2 * d_cT / D + 1)) + sN_cLmY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cBLmY = ft_cBS * a_cB / b / D * (1 - 2 * d_cT / D) + sN_cLmY * (0.5 - d_cT / D);
                    }
                    //L-Y右端引張
                    if (sN_cLmY > sN1_cRS)
                    {
                        var xn1 = (0.5 + n * a_cR / b / D) / (1 + 2 * n * a_cR / b / D - sN_cLmY / fcS);
                        Ma_cRLmY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cR / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cL / b, 2) - 2 * xn1 - 2 * d_cL / b + 1)) + sN_cLmY * (0.5 - xn1);
                    }
                    else if (sN_cLmY > sN2_cRS)
                    {
                        var xn1 = sN_cLmY / fcS - 2 * n * a_cR / b / D + Math.Sqrt(Math.Pow(2 * n * a_cR / b / D - sN_cLmY / fcS, 2) + 2 * n * a_cR / b / D);
                        Ma_cRLmY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cL / b, 2) - 2 * d_cL / b + 1)) + sN_cLmY * (0.5 - xn1);
                    }
                    else if (sN_cLmY > sN3_cRS)
                    {
                        var xn1 = -(n / ft_cRS * sN_cLmY + 2 * n * a_cR / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cR / b / D + n / ft_cRS * sN_cLmY, 2) + 2 * (n * a_cR / b / D + n / ft_cRS * sN_cLmY * (1 - d_cL / b)));
                        Ma_cRLmY = ft_cRS / n / (1 - xn1 - d_cL / b) * (Math.Pow(xn1, 3) / 3 + n * a_cR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cL / b, 2) - 2 * d_cL / b + 1)) + sN_cLmY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cRLmY = ft_cRS * a_cR / b / D * (1 - 2 * d_cL / b) + sN_cLmY * (0.5 - d_cL / b);
                    }
                    //L-Y左端引張
                    if (sN_cLmY > sN1_cLS)
                    {
                        var xn1 = (0.5 + n * a_cL / b / D) / (1 + 2 * n * a_cL / b / D - sN_cLmY / fcS);
                        Ma_cLLmY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_cL / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_cR / b, 2) - 2 * xn1 - 2 * d_cR / b + 1)) + sN_cLmY * (0.5 - xn1);
                    }
                    else if (sN_cLmY > sN2_cLS)
                    {
                        var xn1 = sN_cLmY / fcS - 2 * n * a_cL / b / D + Math.Sqrt(Math.Pow(2 * n * a_cL / b / D - sN_cLmY / fcS, 2) + 2 * n * a_cL / b / D);
                        Ma_cLLmY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_cL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cR / b, 2) - 2 * d_cR / b + 1)) + sN_cLmY * (0.5 - xn1);
                    }
                    else if (sN_cLmY > sN3_cLS)
                    {
                        var xn1 = -(n / ft_cLS * sN_cLmY + 2 * n * a_cL / b / D) + Math.Sqrt(Math.Pow(2 * n * a_cL / b / D + n / ft_cLS * sN_cLmY, 2) + 2 * (n * a_cL / b / D + n / ft_cLS * sN_cLmY * (1 - d_cR / b)));
                        Ma_cLLmY = ft_cLS / n / (1 - xn1 - d_cR / b) * (Math.Pow(xn1, 3) / 3 + n * a_cL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_cR / b, 2) - 2 * d_cR / b + 1)) + sN_cLmY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_cLLmY = ft_cLS * a_cL / b / D * (1 - 2 * d_cR / b) + sN_cLmY * (0.5 - d_cR / b);
                    }
                    Ma_cTL = Ma_cTL * b * D * D * unitl * unitf; Ma_cBL = Ma_cBL * b * D * D * unitl * unitf; Ma_cRL = Ma_cRL * b * b * D * unitl * unitf; Ma_cLL = Ma_cLL * b * b * D * unitl * unitf;
                    Ma_cTLpX = Ma_cTLpX * b * D * D * unitl * unitf; Ma_cBLpX = Ma_cBLpX * b * D * D * unitl * unitf; Ma_cRLpX = Ma_cRLpX * b * b * D * unitl * unitf; Ma_cLLpX = Ma_cLLpX * b * b * D * unitl * unitf;
                    Ma_cTLmX = Ma_cTLmX * b * D * D * unitl * unitf; Ma_cBLmX = Ma_cBLmX * b * D * D * unitl * unitf; Ma_cRLmX = Ma_cRLmX * b * b * D * unitl * unitf; Ma_cLLmX = Ma_cLLmX * b * b * D * unitl * unitf;
                    Ma_cTLpY = Ma_cTLpY * b * D * D * unitl * unitf; Ma_cBLpY = Ma_cBLpY * b * D * D * unitl * unitf; Ma_cRLpY = Ma_cRLpY * b * b * D * unitl * unitf; Ma_cLLpY = Ma_cLLpY * b * b * D * unitl * unitf;
                    Ma_cTLmY = Ma_cTLmY * b * D * D * unitl * unitf; Ma_cBLmY = Ma_cBLmY * b * D * D * unitl * unitf; Ma_cRLmY = Ma_cRLmY * b * b * D * unitl * unitf; Ma_cLLmY = Ma_cLLmY * b * b * D * unitl * unitf;
                    //************************************************************************************************************************************************************
                    //j端の許容曲げモーメント
                    //************************************************************************************************************************************************************
                    var a_jT = n_jT * Math.Pow(D_jT, 2) * Math.PI / 4.0;//i端上端主筋断面積
                    var a_jB = n_jB * Math.Pow(D_jB, 2) * Math.PI / 4.0;//i端下端主筋断面積
                    var a_jR = n_jR * Math.Pow(D_jR, 2) * Math.PI / 4.0;//i端右端主筋断面積
                    var a_jL = n_jL * Math.Pow(D_jL, 2) * Math.PI / 4.0;//i端左端主筋断面積
                    var ft_jTL = 195.0; var ft_jBL = 195.0; var ft_jRL = 195.0; var ft_jLL = 195.0;
                    if (D_jT > 18.9 && D_jT < 28.9) { ft_jTL = 215.0; }//i端上端主筋許容引張応力度
                    if (D_jB > 18.9 && D_jB < 28.9) { ft_jBL = 215.0; }//i端下端主筋許容引張応力度
                    if (D_jR > 18.9 && D_jR < 28.9) { ft_jRL = 215.0; }//i端右端主筋許容引張応力度
                    if (D_jL > 18.9 && D_jL < 28.9) { ft_jLL = 215.0; }//i端左端主筋許容引張応力度
                    var ft_jTS = 295.0; var ft_jBS = 295.0; var ft_jRS = 295.0; var ft_jLS = 295.0;
                    if (D_jT > 18.9 && D_jT < 28.9) { ft_jTS = 345.0; }//i端上端主筋許容引張応力度
                    else if (D_jT > 28.9) { ft_jTS = 390.0; }
                    if (D_jB > 18.9 && D_jB < 28.9) { ft_jBS = 345.0; }//i端下端主筋許容引張応力度
                    else if (D_jB > 28.9) { ft_jBS = 390.0; }
                    if (D_jR > 18.9 && D_jR < 28.9) { ft_jRS = 345.0; }//i端上端主筋許容引張応力度
                    else if (D_jR > 28.9) { ft_jRS = 390.0; }
                    if (D_jL > 18.9 && D_jL < 28.9) { ft_jLS = 345.0; }//i端下端主筋許容引張応力度
                    else if (D_jL > 28.9) { ft_jLS = 390.0; }
                    FtiT.Add(new List<string> { ft_jTL.ToString().Substring(0, Digit((int)ft_jTL)), "", ft_jTS.ToString().Substring(0, Digit((int)ft_jTS)) });
                    var d_jT = kT + S_j + D_jT / 2.0;//i端の上端より鉄筋重心までの距離
                    FtiB.Add(new List<string> { ft_jBL.ToString().Substring(0, Digit((int)ft_jBL)), "", ft_jBS.ToString().Substring(0, Digit((int)ft_jBS)) });
                    var d_jB = kB + S_j + D_jB / 2.0;//i端の下端より鉄筋重心までの距離
                    FtiR.Add(new List<string> { ft_jRL.ToString().Substring(0, Digit((int)ft_jRL)), "", ft_jRS.ToString().Substring(0, Digit((int)ft_jRS)) });
                    var d_jR = kR + S_j2 + D_jR / 2.0;//i端の右端より鉄筋重心までの距離
                    FtiL.Add(new List<string> { ft_jLL.ToString().Substring(0, Digit((int)ft_jLL)), "", ft_jLS.ToString().Substring(0, Digit((int)ft_jLS)) });
                    var d_jL = kL + S_j2 + D_jL / 2.0;//i端の下端より鉄筋重心までの距離
                    var sN_jL = Ni * 1000 / b / D;//N/mm2
                    var sN_jLpX = (Ni + Ni_x) * 1000 / b / D; var sN_jLmX = (Ni + Ni_x2) * 1000 / b / D;
                    var sN_jLpY = (Ni + Ni_y) * 1000 / b / D; var sN_jLmY = (Ni + Ni_y2) * 1000 / b / D;
                    var x_jTL = (1 - d_jB / D) / (1 + ft_jTL / (n * fcL)); var x_jBL = (1 - d_jT / D) / (1 + ft_jBL / (n * fcL));
                    var x_jRL = (1 - d_jL / b) / (1 + ft_jRL / (n * fcL)); var x_jLL = (1 - d_jR / b) / (1 + ft_jLL / (n * fcL));
                    var x_jTS = (1 - d_jB / D) / (1 + ft_jTS / (n * fcS)); var x_jBS = (1 - d_jT / D) / (1 + ft_jBS / (n * fcS));
                    var x_jRS = (1 - d_jL / b) / (1 + ft_jRS / (n * fcS)); var x_jLS = (1 - d_jR / b) / (1 + ft_jLS / (n * fcS));
                    var sN1_jTL = fcL * (0.5 + n * a_jT / b / D); var sN1_jBL = fcL * (0.5 + n * a_jB / b / D); var sN1_jRL = fcL * (0.5 + n * a_jR / b / D); var sN1_jLL = fcL * (0.5 + n * a_jL / b / D);
                    var sN1_jTS = fcS * (0.5 + n * a_jT / b / D); var sN1_jBS = fcS * (0.5 + n * a_jB / b / D); var sN1_jRS = fcS * (0.5 + n * a_jR / b / D); var sN1_jLS = fcS * (0.5 + n * a_jL / b / D);
                    var sN2_jTL = fcL * (x_jTL / 2.0 + n * a_jT / b / D * (2 - 1.0 / x_jTL)); var sN2_jBL = fcL * (x_jBL / 2.0 + n * a_jB / b / D * (2 - 1.0 / x_jBL)); var sN2_jRL = fcL * (x_jRL / 2.0 + n * a_jR / b / D * (2 - 1.0 / x_jRL)); var sN2_jLL = fcL * (x_jLL / 2.0 + n * a_jL / b / D * (2 - 1.0 / x_jLL));
                    var sN2_jTS = fcS * (x_jTS / 2.0 + n * a_jT / b / D * (2 - 1.0 / x_jTS)); var sN2_jBS = fcS * (x_jBS / 2.0 + n * a_jB / b / D * (2 - 1.0 / x_jBS)); var sN2_jRS = fcS * (x_jRS / 2.0 + n * a_jR / b / D * (2 - 1.0 / x_jRS)); var sN2_jLS = fcL * (x_jLS / 2.0 + n * a_jL / b / D * (2 - 1.0 / x_jLS));
                    var sN3_jTL = ft_jTL * a_jT / b / D / (d_jB / D - 1); var sN3_jBL = ft_jBL * a_jB / b / D / (d_jT / D - 1); var sN3_jRL = ft_jRL * a_jR / b / D / (d_jL / b - 1); var sN3_jLL = ft_jLL * a_jL / b / D / (d_jR / b - 1);
                    var sN3_jTS = ft_jTS * a_jT / b / D / (d_jB / D - 1); var sN3_jBS = ft_jBS * a_jB / b / D / (d_jT / D - 1); var sN3_jRS = ft_jRS * a_jR / b / D / (d_jL / b - 1); var sN3_jLS = ft_jLS * a_jL / b / D / (d_jR / b - 1);
                    var Ma_jTL = 0.0; var Ma_jBL = 0.0; var Ma_jRL = 0.0; var Ma_jLL = 0.0;
                    var Ma_jTLpX = 0.0; var Ma_jBLpX = 0.0; var Ma_jRLpX = 0.0; var Ma_jLLpX = 0.0;
                    var Ma_jTLmX = 0.0; var Ma_jBLmX = 0.0; var Ma_jRLmX = 0.0; var Ma_jLLmX = 0.0;
                    var Ma_jTLpY = 0.0; var Ma_jBLpY = 0.0; var Ma_jRLpY = 0.0; var Ma_jLLpY = 0.0;
                    var Ma_jTLmY = 0.0; var Ma_jBLmY = 0.0; var Ma_jRLmY = 0.0; var Ma_jLLmY = 0.0;
                    //長期上端引張/////////////////////////////////////////////////////////////////////////////////////
                    if (sN_jL > sN1_jTL)
                    {
                        var xn1 = (0.5 + n * a_jT / b / D) / (1 + 2 * n * a_jT / b / D - sN_jL / fcL);
                        Ma_jTL = fcL / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jT / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jB / D, 2) - 2 * xn1 - 2 * d_jB / D + 1)) + sN_jL * (0.5 - xn1);
                    }
                    else if (sN_jL > sN2_jTL)
                    {
                        var xn1 = sN_jL / fcL - 2 * n * a_jT / b / D + Math.Sqrt(Math.Pow(2 * n * a_jT / b / D - sN_jL / fcL, 2) + 2 * n * a_jT / b / D);
                        Ma_jTL = fcL / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jB / D, 2) - 2 * d_jB / D + 1)) + sN_jL * (0.5 - xn1);
                    }
                    else if (sN_jL > sN3_jTL)
                    {
                        var xn1 = -(n / ft_jTL * sN_jL + 2 * n * a_jT / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jT / b / D + n / ft_jTL * sN_jL, 2) + 2 * (n * a_jT / b / D + n / ft_jTL * sN_jL * (1 - d_jB / D)));
                        Ma_jTL = ft_jTL / n / (1 - xn1 - d_jB / D) * (Math.Pow(xn1, 3) / 3 + n * a_jT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jB / D, 2) - 2 * d_jB / D + 1)) + sN_jL * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jTL = ft_jTL * a_jT / b / D * (1 - 2 * d_jB / D) + sN_jL * (0.5 - d_jB / D);
                    }
                    //長期下端引張
                    if (sN_jL > sN1_jBL)
                    {
                        var xn1 = (0.5 + n * a_jB / b / D) / (1 + 2 * n * a_jB / b / D - sN_jL / fcL);
                        Ma_jBL = fcL / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jB / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jT / D, 2) - 2 * xn1 - 2 * d_jT / D + 1)) + sN_jL * (0.5 - xn1);
                    }
                    else if (sN_jL > sN2_jBL)
                    {
                        var xn1 = sN_jL / fcL - 2 * n * a_jB / b / D + Math.Sqrt(Math.Pow(2 * n * a_jB / b / D - sN_jL / fcL, 2) + 2 * n * a_jB / b / D);
                        Ma_jBL = fcL / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jT / D, 2) - 2 * d_jT / D + 1)) + sN_jL * (0.5 - xn1);
                    }
                    else if (sN_jL > sN3_jBL)
                    {
                        var xn1 = -(n / ft_jBL * sN_jL + 2 * n * a_jB / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jB / b / D + n / ft_jBL * sN_jL, 2) + 2 * (n * a_jB / b / D + n / ft_jBL * sN_jL * (1 - d_jT / D)));
                        Ma_jBL = ft_jBL / n / (1 - xn1 - d_jT / D) * (Math.Pow(xn1, 3) / 3 + n * a_jB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jT / D, 2) - 2 * d_jT / D + 1)) + sN_jL * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jBL = ft_jBL * a_jB / b / D * (1 - 2 * d_jT / D) + sN_jL * (0.5 - d_jT / D);
                    }
                    //長期右端引張
                    if (sN_jL > sN1_jRL)
                    {
                        var xn1 = (0.5 + n * a_jR / b / D) / (1 + 2 * n * a_jR / b / D - sN_jL / fcL);
                        Ma_jRL = fcL / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jR / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jL / b, 2) - 2 * xn1 - 2 * d_jL / b + 1)) + sN_jL * (0.5 - xn1);
                    }
                    else if (sN_jL > sN2_jRL)
                    {
                        var xn1 = sN_jL / fcL - 2 * n * a_jR / b / D + Math.Sqrt(Math.Pow(2 * n * a_jR / b / D - sN_jL / fcL, 2) + 2 * n * a_jR / b / D);
                        Ma_jRL = fcL / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jL / b, 2) - 2 * d_jL / b + 1)) + sN_jL * (0.5 - xn1);
                    }
                    else if (sN_jL > sN3_jRL)
                    {
                        var xn1 = -(n / ft_jRL * sN_jL + 2 * n * a_jR / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jR / b / D + n / ft_jRL * sN_jL, 2) + 2 * (n * a_jR / b / D + n / ft_jRL * sN_jL * (1 - d_jL / b)));
                        Ma_jRL = ft_jRL / n / (1 - xn1 - d_jL / b) * (Math.Pow(xn1, 3) / 3 + n * a_jR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jL / b, 2) - 2 * d_jL / b + 1)) + sN_jL * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jRL = ft_jRL * a_jR / b / D * (1 - 2 * d_jL / b) + sN_jL * (0.5 - d_jL / b);
                    }
                    //長期左端引張
                    if (sN_jL > sN1_jLL)
                    {
                        var xn1 = (0.5 + n * a_jL / b / D) / (1 + 2 * n * a_jL / b / D - sN_jL / fcL);
                        Ma_jLL = fcL / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jL / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jR / b, 2) - 2 * xn1 - 2 * d_jR / b + 1)) + sN_jL * (0.5 - xn1);
                    }
                    else if (sN_jL > sN2_jLL)
                    {
                        var xn1 = sN_jL / fcL - 2 * n * a_jL / b / D + Math.Sqrt(Math.Pow(2 * n * a_jL / b / D - sN_jL / fcL, 2) + 2 * n * a_jL / b / D);
                        Ma_jLL = fcL / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jR / b, 2) - 2 * d_jR / b + 1)) + sN_jL * (0.5 - xn1);
                    }
                    else if (sN_jL > sN3_jLL)
                    {
                        var xn1 = -(n / ft_jLL * sN_jL + 2 * n * a_jL / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jL / b / D + n / ft_jLL * sN_jL, 2) + 2 * (n * a_jL / b / D + n / ft_jLL * sN_jL * (1 - d_jR / b)));
                        Ma_jLL = ft_jLL / n / (1 - xn1 - d_jR / b) * (Math.Pow(xn1, 3) / 3 + n * a_jL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jR / b, 2) - 2 * d_jR / b + 1)) + sN_jL * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jLL = ft_jLL * a_jL / b / D * (1 - 2 * d_jR / b) + sN_jL * (0.5 - d_jR / b);
                    }

                    //L+X上端引張/////////////////////////////////////////////////////////////////////////////////////
                    if (sN_jLpX > sN1_jTS)
                    {
                        var xn1 = (0.5 + n * a_jT / b / D) / (1 + 2 * n * a_jT / b / D - sN_jLpX / fcS);
                        Ma_jTLpX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jT / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jB / D, 2) - 2 * xn1 - 2 * d_jB / D + 1)) + sN_jLpX * (0.5 - xn1);
                    }
                    else if (sN_jLpX > sN2_jTS)
                    {
                        var xn1 = sN_jLpX / fcS - 2 * n * a_jT / b / D + Math.Sqrt(Math.Pow(2 * n * a_jT / b / D - sN_jLpX / fcS, 2) + 2 * n * a_jT / b / D);
                        Ma_jTLpX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jB / D, 2) - 2 * d_jB / D + 1)) + sN_jLpX * (0.5 - xn1);
                    }
                    else if (sN_jLpX > sN3_jTS)
                    {
                        var xn1 = -(n / ft_jTS * sN_jLpX + 2 * n * a_jT / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jT / b / D + n / ft_jTS * sN_jLpX, 2) + 2 * (n * a_jT / b / D + n / ft_jTS * sN_jLpX * (1 - d_jB / D)));
                        Ma_jTLpX = ft_jTS / n / (1 - xn1 - d_jB / D) * (Math.Pow(xn1, 3) / 3 + n * a_jT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jB / D, 2) - 2 * d_jB / D + 1)) + sN_jLpX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jTLpX = ft_jTS * a_jT / b / D * (1 - 2 * d_jB / D) + sN_jLpX * (0.5 - d_jB / D);
                    }
                    //L+X下端引張
                    if (sN_jLpX > sN1_jBS)
                    {
                        var xn1 = (0.5 + n * a_jB / b / D) / (1 + 2 * n * a_jB / b / D - sN_jLpX / fcS);
                        Ma_jBLpX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jB / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jT / D, 2) - 2 * xn1 - 2 * d_jT / D + 1)) + sN_jLpX * (0.5 - xn1);
                    }
                    else if (sN_jLpX > sN2_jBS)
                    {
                        var xn1 = sN_jLpX / fcS - 2 * n * a_jB / b / D + Math.Sqrt(Math.Pow(2 * n * a_jB / b / D - sN_jLpX / fcS, 2) + 2 * n * a_jB / b / D);
                        Ma_jBLpX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jT / D, 2) - 2 * d_jT / D + 1)) + sN_jLpX * (0.5 - xn1);
                    }
                    else if (sN_jLpX > sN3_jBS)
                    {
                        var xn1 = -(n / ft_jBS * sN_jLpX + 2 * n * a_jB / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jB / b / D + n / ft_jBS * sN_jLpX, 2) + 2 * (n * a_jB / b / D + n / ft_jBS * sN_jLpX * (1 - d_jT / D)));
                        Ma_jBLpX = ft_jBS / n / (1 - xn1 - d_jT / D) * (Math.Pow(xn1, 3) / 3 + n * a_jB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jT / D, 2) - 2 * d_jT / D + 1)) + sN_jLpX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jBLpX = ft_jBS * a_jB / b / D * (1 - 2 * d_jT / D) + sN_jLpX * (0.5 - d_jT / D);
                    }
                    //L+X右端引張
                    if (sN_jLpX > sN1_jRS)
                    {
                        var xn1 = (0.5 + n * a_jR / b / D) / (1 + 2 * n * a_jR / b / D - sN_jLpX / fcS);
                        Ma_jRLpX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jR / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jL / b, 2) - 2 * xn1 - 2 * d_jL / b + 1)) + sN_jLpX * (0.5 - xn1);
                    }
                    else if (sN_jLpX > sN2_jRS)
                    {
                        var xn1 = sN_jLpX / fcS - 2 * n * a_jR / b / D + Math.Sqrt(Math.Pow(2 * n * a_jR / b / D - sN_jLpX / fcS, 2) + 2 * n * a_jR / b / D);
                        Ma_jRLpX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jL / b, 2) - 2 * d_jL / b + 1)) + sN_jLpX * (0.5 - xn1);
                    }
                    else if (sN_jLpX > sN3_jRS)
                    {
                        var xn1 = -(n / ft_jRS * sN_jLpX + 2 * n * a_jR / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jR / b / D + n / ft_jRS * sN_jLpX, 2) + 2 * (n * a_jR / b / D + n / ft_jRS * sN_jLpX * (1 - d_jL / b)));
                        Ma_jRLpX = ft_jRS / n / (1 - xn1 - d_jL / b) * (Math.Pow(xn1, 3) / 3 + n * a_jR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jL / b, 2) - 2 * d_jL / b + 1)) + sN_jLpX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jRLpX = ft_jRS * a_jR / b / D * (1 - 2 * d_jL / b) + sN_jLpX * (0.5 - d_jL / b);
                    }
                    //L+X左端引張
                    if (sN_jLpX > sN1_jLS)
                    {
                        var xn1 = (0.5 + n * a_jL / b / D) / (1 + 2 * n * a_jL / b / D - sN_jLpX / fcS);
                        Ma_jLLpX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jL / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jR / b, 2) - 2 * xn1 - 2 * d_jR / b + 1)) + sN_jLpX * (0.5 - xn1);
                    }
                    else if (sN_jLpX > sN2_jLS)
                    {
                        var xn1 = sN_jLpX / fcS - 2 * n * a_jL / b / D + Math.Sqrt(Math.Pow(2 * n * a_jL / b / D - sN_jLpX / fcS, 2) + 2 * n * a_jL / b / D);
                        Ma_jLLpX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jR / b, 2) - 2 * d_jR / b + 1)) + sN_jLpX * (0.5 - xn1);
                    }
                    else if (sN_jLpX > sN3_jLS)
                    {
                        var xn1 = -(n / ft_jLS * sN_jLpX + 2 * n * a_jL / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jL / b / D + n / ft_jLS * sN_jLpX, 2) + 2 * (n * a_jL / b / D + n / ft_jLS * sN_jLpX * (1 - d_jR / b)));
                        Ma_jLLpX = ft_jLS / n / (1 - xn1 - d_jR / b) * (Math.Pow(xn1, 3) / 3 + n * a_jL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jR / b, 2) - 2 * d_jR / b + 1)) + sN_jLpX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jLLpX = ft_jLS * a_jL / b / D * (1 - 2 * d_jR / b) + sN_jLpX * (0.5 - d_jR / b);
                    }
                    //L-X上端引張/////////////////////////////////////////////////////////////////////////////////////
                    if (sN_jLmX > sN1_jTS)
                    {
                        var xn1 = (0.5 + n * a_jT / b / D) / (1 + 2 * n * a_jT / b / D - sN_jLmX / fcS);
                        Ma_jTLmX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jT / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jB / D, 2) - 2 * xn1 - 2 * d_jB / D + 1)) + sN_jLmX * (0.5 - xn1);
                    }
                    else if (sN_jLmX > sN2_jTS)
                    {
                        var xn1 = sN_jLmX / fcS - 2 * n * a_jT / b / D + Math.Sqrt(Math.Pow(2 * n * a_jT / b / D - sN_jLmX / fcS, 2) + 2 * n * a_jT / b / D);
                        Ma_jTLmX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jB / D, 2) - 2 * d_jB / D + 1)) + sN_jLmX * (0.5 - xn1);
                    }
                    else if (sN_jLmX > sN3_jTS)
                    {
                        var xn1 = -(n / ft_jTS * sN_jLmX + 2 * n * a_jT / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jT / b / D + n / ft_jTS * sN_jLmX, 2) + 2 * (n * a_jT / b / D + n / ft_jTS * sN_jLmX * (1 - d_jB / D)));
                        Ma_jTLmX = ft_jTS / n / (1 - xn1 - d_jB / D) * (Math.Pow(xn1, 3) / 3 + n * a_jT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jB / D, 2) - 2 * d_jB / D + 1)) + sN_jLmX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jTLmX = ft_jTS * a_jT / b / D * (1 - 2 * d_jB / D) + sN_jLmX * (0.5 - d_jB / D);
                    }
                    //L-X下端引張
                    if (sN_jLmX > sN1_jBS)
                    {
                        var xn1 = (0.5 + n * a_jB / b / D) / (1 + 2 * n * a_jB / b / D - sN_jLmX / fcS);
                        Ma_jBLmX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jB / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jT / D, 2) - 2 * xn1 - 2 * d_jT / D + 1)) + sN_jLmX * (0.5 - xn1);
                    }
                    else if (sN_jLmX > sN2_jBS)
                    {
                        var xn1 = sN_jLmX / fcS - 2 * n * a_jB / b / D + Math.Sqrt(Math.Pow(2 * n * a_jB / b / D - sN_jLmX / fcS, 2) + 2 * n * a_jB / b / D);
                        Ma_jBLmX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jT / D, 2) - 2 * d_jT / D + 1)) + sN_jLmX * (0.5 - xn1);
                    }
                    else if (sN_jLmX > sN3_jBS)
                    {
                        var xn1 = -(n / ft_jBS * sN_jLmX + 2 * n * a_jB / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jB / b / D + n / ft_jBS * sN_jLmX, 2) + 2 * (n * a_jB / b / D + n / ft_jBS * sN_jLmX * (1 - d_jT / D)));
                        Ma_jBLmX = ft_jBS / n / (1 - xn1 - d_jT / D) * (Math.Pow(xn1, 3) / 3 + n * a_jB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jT / D, 2) - 2 * d_jT / D + 1)) + sN_jLmX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jBLmX = ft_jBS * a_jB / b / D * (1 - 2 * d_jT / D) + sN_jLmX * (0.5 - d_jT / D);
                    }
                    //L-X右端引張
                    if (sN_jLmX > sN1_jRS)
                    {
                        var xn1 = (0.5 + n * a_jR / b / D) / (1 + 2 * n * a_jR / b / D - sN_jLmX / fcS);
                        Ma_jRLmX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jR / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jL / b, 2) - 2 * xn1 - 2 * d_jL / b + 1)) + sN_jLmX * (0.5 - xn1);
                    }
                    else if (sN_jLmX > sN2_jRS)
                    {
                        var xn1 = sN_jLmX / fcS - 2 * n * a_jR / b / D + Math.Sqrt(Math.Pow(2 * n * a_jR / b / D - sN_jLmX / fcS, 2) + 2 * n * a_jR / b / D);
                        Ma_jRLmX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jL / b, 2) - 2 * d_jL / b + 1)) + sN_jLmX * (0.5 - xn1);
                    }
                    else if (sN_jLmX > sN3_jRS)
                    {
                        var xn1 = -(n / ft_jRS * sN_jLmX + 2 * n * a_jR / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jR / b / D + n / ft_jRS * sN_jLmX, 2) + 2 * (n * a_jR / b / D + n / ft_jRS * sN_jLmX * (1 - d_jL / b)));
                        Ma_jRLmX = ft_jRS / n / (1 - xn1 - d_jL / b) * (Math.Pow(xn1, 3) / 3 + n * a_jR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jL / b, 2) - 2 * d_jL / b + 1)) + sN_jLmX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jRLmX = ft_jRS * a_jR / b / D * (1 - 2 * d_jL / b) + sN_jLmX * (0.5 - d_jL / b);
                    }
                    //L-X左端引張
                    if (sN_jLmX > sN1_jLS)
                    {
                        var xn1 = (0.5 + n * a_jL / b / D) / (1 + 2 * n * a_jL / b / D - sN_jLmX / fcS);
                        Ma_jLLmX = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jL / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jR / b, 2) - 2 * xn1 - 2 * d_jR / b + 1)) + sN_jLmX * (0.5 - xn1);
                    }
                    else if (sN_jLmX > sN2_jLS)
                    {
                        var xn1 = sN_jLmX / fcS - 2 * n * a_jL / b / D + Math.Sqrt(Math.Pow(2 * n * a_jL / b / D - sN_jLmX / fcS, 2) + 2 * n * a_jL / b / D);
                        Ma_jLLmX = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jR / b, 2) - 2 * d_jR / b + 1)) + sN_jLmX * (0.5 - xn1);
                    }
                    else if (sN_jLmX > sN3_jLS)
                    {
                        var xn1 = -(n / ft_jLS * sN_jLmX + 2 * n * a_jL / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jL / b / D + n / ft_jLS * sN_jLmX, 2) + 2 * (n * a_jL / b / D + n / ft_jLS * sN_jLmX * (1 - d_jR / b)));
                        Ma_jLLmX = ft_jLS / n / (1 - xn1 - d_jR / b) * (Math.Pow(xn1, 3) / 3 + n * a_jL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jR / b, 2) - 2 * d_jR / b + 1)) + sN_jLmX * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jLLmX = ft_jLS * a_jL / b / D * (1 - 2 * d_jR / b) + sN_jLmX * (0.5 - d_jR / b);
                    }
                    //L+Y上端引張/////////////////////////////////////////////////////////////////////////////////////
                    if (sN_jLpY > sN1_jTS)
                    {
                        var xn1 = (0.5 + n * a_jT / b / D) / (1 + 2 * n * a_jT / b / D - sN_jLpY / fcS);
                        Ma_jTLpY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jT / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jB / D, 2) - 2 * xn1 - 2 * d_jB / D + 1)) + sN_jLpY * (0.5 - xn1);
                    }
                    else if (sN_jLpY > sN2_jTS)
                    {
                        var xn1 = sN_jLpY / fcS - 2 * n * a_jT / b / D + Math.Sqrt(Math.Pow(2 * n * a_jT / b / D - sN_jLpY / fcS, 2) + 2 * n * a_jT / b / D);
                        Ma_jTLpY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jB / D, 2) - 2 * d_jB / D + 1)) + sN_jLpY * (0.5 - xn1);
                    }
                    else if (sN_jLpY > sN3_jTS)
                    {
                        var xn1 = -(n / ft_jTS * sN_jLpY + 2 * n * a_jT / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jT / b / D + n / ft_jTS * sN_jLpY, 2) + 2 * (n * a_jT / b / D + n / ft_jTS * sN_jLpY * (1 - d_jB / D)));
                        Ma_jTLpY = ft_jTS / n / (1 - xn1 - d_jB / D) * (Math.Pow(xn1, 3) / 3 + n * a_jT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jB / D, 2) - 2 * d_jB / D + 1)) + sN_jLpY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jTLpY = ft_jTS * a_jT / b / D * (1 - 2 * d_jB / D) + sN_jLpY * (0.5 - d_jB / D);
                    }
                    //L+Y下端引張
                    if (sN_jLpY > sN1_jBS)
                    {
                        var xn1 = (0.5 + n * a_jB / b / D) / (1 + 2 * n * a_jB / b / D - sN_jLpY / fcS);
                        Ma_jBLpY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jB / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jT / D, 2) - 2 * xn1 - 2 * d_jT / D + 1)) + sN_jLpY * (0.5 - xn1);
                    }
                    else if (sN_jLpY > sN2_jBS)
                    {
                        var xn1 = sN_jLpY / fcS - 2 * n * a_jB / b / D + Math.Sqrt(Math.Pow(2 * n * a_jB / b / D - sN_jLpY / fcS, 2) + 2 * n * a_jB / b / D);
                        Ma_jBLpY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jT / D, 2) - 2 * d_jT / D + 1)) + sN_jLpY * (0.5 - xn1);
                    }
                    else if (sN_jLpY > sN3_jBS)
                    {
                        var xn1 = -(n / ft_jBS * sN_jLpY + 2 * n * a_jB / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jB / b / D + n / ft_jBS * sN_jLpY, 2) + 2 * (n * a_jB / b / D + n / ft_jBS * sN_jLpY * (1 - d_jT / D)));
                        Ma_jBLpY = ft_jBS / n / (1 - xn1 - d_jT / D) * (Math.Pow(xn1, 3) / 3 + n * a_jB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jT / D, 2) - 2 * d_jT / D + 1)) + sN_jLpY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jBLpY = ft_jBS * a_jB / b / D * (1 - 2 * d_jT / D) + sN_jLpY * (0.5 - d_jT / D);
                    }
                    //L+Y右端引張
                    if (sN_jLpY > sN1_jRS)
                    {
                        var xn1 = (0.5 + n * a_jR / b / D) / (1 + 2 * n * a_jR / b / D - sN_jLpY / fcS);
                        Ma_jRLpY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jR / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jL / b, 2) - 2 * xn1 - 2 * d_jL / b + 1)) + sN_jLpY * (0.5 - xn1);
                    }
                    else if (sN_jLpY > sN2_jRS)
                    {
                        var xn1 = sN_jLpY / fcS - 2 * n * a_jR / b / D + Math.Sqrt(Math.Pow(2 * n * a_jR / b / D - sN_jLpY / fcS, 2) + 2 * n * a_jR / b / D);
                        Ma_jRLpY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jL / b, 2) - 2 * d_jL / b + 1)) + sN_jLpY * (0.5 - xn1);
                    }
                    else if (sN_jLpY > sN3_jRS)
                    {
                        var xn1 = -(n / ft_jRS * sN_jLpY + 2 * n * a_jR / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jR / b / D + n / ft_jRS * sN_jLpY, 2) + 2 * (n * a_jR / b / D + n / ft_jRS * sN_jLpY * (1 - d_jL / b)));
                        Ma_jRLpY = ft_jRS / n / (1 - xn1 - d_jL / b) * (Math.Pow(xn1, 3) / 3 + n * a_jR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jL / b, 2) - 2 * d_jL / b + 1)) + sN_jLpY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jRLpY = ft_jRS * a_jR / b / D * (1 - 2 * d_jL / b) + sN_jLpY * (0.5 - d_jL / b);
                    }
                    //L+Y左端引張
                    if (sN_jLpY > sN1_jLS)
                    {
                        var xn1 = (0.5 + n * a_jL / b / D) / (1 + 2 * n * a_jL / b / D - sN_jLpY / fcS);
                        Ma_jLLpY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jL / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jR / b, 2) - 2 * xn1 - 2 * d_jR / b + 1)) + sN_jLpY * (0.5 - xn1);
                    }
                    else if (sN_jLpY > sN2_jLS)
                    {
                        var xn1 = sN_jLpY / fcS - 2 * n * a_jL / b / D + Math.Sqrt(Math.Pow(2 * n * a_jL / b / D - sN_jLpY / fcS, 2) + 2 * n * a_jL / b / D);
                        Ma_jLLpY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jR / b, 2) - 2 * d_jR / b + 1)) + sN_jLpY * (0.5 - xn1);
                    }
                    else if (sN_jLpY > sN3_jLS)
                    {
                        var xn1 = -(n / ft_jLS * sN_jLpY + 2 * n * a_jL / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jL / b / D + n / ft_jLS * sN_jLpY, 2) + 2 * (n * a_jL / b / D + n / ft_jLS * sN_jLpY * (1 - d_jR / b)));
                        Ma_jLLpY = ft_jLS / n / (1 - xn1 - d_jR / b) * (Math.Pow(xn1, 3) / 3 + n * a_jL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jR / b, 2) - 2 * d_jR / b + 1)) + sN_jLpY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jLLpY = ft_jLS * a_jL / b / D * (1 - 2 * d_jR / b) + sN_jLpY * (0.5 - d_jR / b);
                    }
                    //L-Y上端引張/////////////////////////////////////////////////////////////////////////////////////
                    if (sN_jLmY > sN1_jTS)
                    {
                        var xn1 = (0.5 + n * a_jT / b / D) / (1 + 2 * n * a_jT / b / D - sN_jLmY / fcS);
                        Ma_jTLmY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jT / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jB / D, 2) - 2 * xn1 - 2 * d_jB / D + 1)) + sN_jLmY * (0.5 - xn1);
                    }
                    else if (sN_jLmY > sN2_jTS)
                    {
                        var xn1 = sN_jLmY / fcS - 2 * n * a_jT / b / D + Math.Sqrt(Math.Pow(2 * n * a_jT / b / D - sN_jLmY / fcS, 2) + 2 * n * a_jT / b / D);
                        Ma_jTLmY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jB / D, 2) - 2 * d_jB / D + 1)) + sN_jLmY * (0.5 - xn1);
                    }
                    else if (sN_jLmY > sN3_jTS)
                    {
                        var xn1 = -(n / ft_jTS * sN_jLmY + 2 * n * a_jT / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jT / b / D + n / ft_jTS * sN_jLmY, 2) + 2 * (n * a_jT / b / D + n / ft_jTS * sN_jLmY * (1 - d_jB / D)));
                        Ma_jTLmY = ft_jTS / n / (1 - xn1 - d_jB / D) * (Math.Pow(xn1, 3) / 3 + n * a_jT / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jB / D, 2) - 2 * d_jB / D + 1)) + sN_jLmY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jTLmY = ft_jTS * a_jT / b / D * (1 - 2 * d_jB / D) + sN_jLmY * (0.5 - d_jB / D);
                    }
                    //L-Y下端引張
                    if (sN_jLmY > sN1_jBS)
                    {
                        var xn1 = (0.5 + n * a_jB / b / D) / (1 + 2 * n * a_jB / b / D - sN_jLmY / fcS);
                        Ma_jBLmY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jB / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jT / D, 2) - 2 * xn1 - 2 * d_jT / D + 1)) + sN_jLmY * (0.5 - xn1);
                    }
                    else if (sN_jLmY > sN2_jBS)
                    {
                        var xn1 = sN_jLmY / fcS - 2 * n * a_jB / b / D + Math.Sqrt(Math.Pow(2 * n * a_jB / b / D - sN_jLmY / fcS, 2) + 2 * n * a_jB / b / D);
                        Ma_jBLmY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jT / D, 2) - 2 * d_jT / D + 1)) + sN_jLmY * (0.5 - xn1);
                    }
                    else if (sN_jLmY > sN3_jBS)
                    {
                        var xn1 = -(n / ft_jBS * sN_jLmY + 2 * n * a_jB / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jB / b / D + n / ft_jBS * sN_jLmY, 2) + 2 * (n * a_jB / b / D + n / ft_jBS * sN_jLmY * (1 - d_jT / D)));
                        Ma_jBLmY = ft_jBS / n / (1 - xn1 - d_jT / D) * (Math.Pow(xn1, 3) / 3 + n * a_jB / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jT / D, 2) - 2 * d_jT / D + 1)) + sN_jLmY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jBLmY = ft_jBS * a_jB / b / D * (1 - 2 * d_jT / D) + sN_jLmY * (0.5 - d_jT / D);
                    }
                    //L-Y右端引張
                    if (sN_jLmY > sN1_jRS)
                    {
                        var xn1 = (0.5 + n * a_jR / b / D) / (1 + 2 * n * a_jR / b / D - sN_jLmY / fcS);
                        Ma_jRLmY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jR / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jL / b, 2) - 2 * xn1 - 2 * d_jL / b + 1)) + sN_jLmY * (0.5 - xn1);
                    }
                    else if (sN_jLmY > sN2_jRS)
                    {
                        var xn1 = sN_jLmY / fcS - 2 * n * a_jR / b / D + Math.Sqrt(Math.Pow(2 * n * a_jR / b / D - sN_jLmY / fcS, 2) + 2 * n * a_jR / b / D);
                        Ma_jRLmY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jL / b, 2) - 2 * d_jL / b + 1)) + sN_jLmY * (0.5 - xn1);
                    }
                    else if (sN_jLmY > sN3_jRS)
                    {
                        var xn1 = -(n / ft_jRS * sN_jLmY + 2 * n * a_jR / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jR / b / D + n / ft_jRS * sN_jLmY, 2) + 2 * (n * a_jR / b / D + n / ft_jRS * sN_jLmY * (1 - d_jL / b)));
                        Ma_jRLmY = ft_jRS / n / (1 - xn1 - d_jL / b) * (Math.Pow(xn1, 3) / 3 + n * a_jR / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jL / b, 2) - 2 * d_jL / b + 1)) + sN_jLmY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jRLmY = ft_jRS * a_jR / b / D * (1 - 2 * d_jL / b) + sN_jLmY * (0.5 - d_jL / b);
                    }
                    //L-Y左端引張
                    if (sN_jLmY > sN1_jLS)
                    {
                        var xn1 = (0.5 + n * a_jL / b / D) / (1 + 2 * n * a_jL / b / D - sN_jLmY / fcS);
                        Ma_jLLmY = fcS / xn1 * (Math.Pow(xn1, 2) - xn1 + 1.0 / 3.0 + n * a_jL / b / D * (2 * Math.Pow(xn1, 2) + 2 * Math.Pow(d_jR / b, 2) - 2 * xn1 - 2 * d_jR / b + 1)) + sN_jLmY * (0.5 - xn1);
                    }
                    else if (sN_jLmY > sN2_jLS)
                    {
                        var xn1 = sN_jLmY / fcS - 2 * n * a_jL / b / D + Math.Sqrt(Math.Pow(2 * n * a_jL / b / D - sN_jLmY / fcS, 2) + 2 * n * a_jL / b / D);
                        Ma_jLLmY = fcS / xn1 * (Math.Pow(xn1, 3) / 3.0 + n * a_jL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jR / b, 2) - 2 * d_jR / b + 1)) + sN_jLmY * (0.5 - xn1);
                    }
                    else if (sN_jLmY > sN3_jLS)
                    {
                        var xn1 = -(n / ft_jLS * sN_jLmY + 2 * n * a_jL / b / D) + Math.Sqrt(Math.Pow(2 * n * a_jL / b / D + n / ft_jLS * sN_jLmY, 2) + 2 * (n * a_jL / b / D + n / ft_jLS * sN_jLmY * (1 - d_jR / b)));
                        Ma_jLLmY = ft_jLS / n / (1 - xn1 - d_jR / b) * (Math.Pow(xn1, 3) / 3 + n * a_jL / b / D * (2 * Math.Pow(xn1, 2) - 2 * xn1 + 2 * Math.Pow(d_jR / b, 2) - 2 * d_jR / b + 1)) + sN_jLmY * (0.5 - xn1);
                    }
                    else
                    {
                        Ma_jLLmY = ft_jLS * a_jL / b / D * (1 - 2 * d_jR / b) + sN_jLmY * (0.5 - d_jR / b);
                    }
                    Ma_jTL = Ma_jTL * b * D * D * unitl * unitf; Ma_jBL = Ma_jBL * b * D * D * unitl * unitf; Ma_jRL = Ma_jRL * b * b * D * unitl * unitf; Ma_jLL = Ma_jLL * b * b * D * unitl * unitf;
                    Ma_jTLpX = Ma_jTLpX * b * D * D * unitl * unitf; Ma_jBLpX = Ma_jBLpX * b * D * D * unitl * unitf; Ma_jRLpX = Ma_jRLpX * b * b * D * unitl * unitf; Ma_jLLpX = Ma_jLLpX * b * b * D * unitl * unitf;
                    Ma_jTLmX = Ma_jTLmX * b * D * D * unitl * unitf; Ma_jBLmX = Ma_jBLmX * b * D * D * unitl * unitf; Ma_jRLmX = Ma_jRLmX * b * b * D * unitl * unitf; Ma_jLLmX = Ma_jLLmX * b * b * D * unitl * unitf;
                    Ma_jTLpY = Ma_jTLpY * b * D * D * unitl * unitf; Ma_jBLpY = Ma_jBLpY * b * D * D * unitl * unitf; Ma_jRLpY = Ma_jRLpY * b * b * D * unitl * unitf; Ma_jLLpY = Ma_jLLpY * b * b * D * unitl * unitf;
                    Ma_jTLmY = Ma_jTLmY * b * D * D * unitl * unitf; Ma_jBLmY = Ma_jBLmY * b * D * D * unitl * unitf; Ma_jRLmY = Ma_jRLmY * b * b * D * unitl * unitf; Ma_jLLmY = Ma_jLLmY * b * b * D * unitl * unitf;
                    //************************************************************************************************************************************************************
                    //i端の許容せん断力
                    //************************************************************************************************************************************************************
                    var d_i = D - (d_iT + d_iB) / 2.0; var j_i = d_i * 7.0 / 8.0; var d_i2 = b - (d_iR + d_iL) / 2.0; var j_i2 = d_i * 7.0 / 8.0;
                    var alpha_iL = Math.Max(Math.Min(4.0 / (Math.Abs(Myi * 1e+6) / (Math.Abs(Qzi * 1e+3) * d_i) + 1.0), 1.5), 1);
                    var alpha_iL2 = Math.Max(Math.Min(4.0 / (Math.Abs(Mzi * 1e+6) / (Math.Abs(Qyi * 1e+3) * d_i2) + 1.0), 1.5), 1);
                    var alpha_iLpX = Math.Max(Math.Min(4.0 / (Math.Abs((Myi + Myi_x) * 1e+6) / (Math.Abs((Qzi + Qzi_x) * 1e+3) * d_i) + 1.0), 1.5), 1);
                    var alpha_iLpY = Math.Max(Math.Min(4.0 / (Math.Abs((Myi + Myi_y) * 1e+6) / (Math.Abs((Qzi + Qzi_y) * 1e+3) * d_i) + 1.0), 1.5), 1);
                    var alpha_iLmX = Math.Max(Math.Min(4.0 / (Math.Abs((Myi + Myi_x2) * 1e+6) / (Math.Abs((Qzi + Qzi_x2) * 1e+3) * d_i) + 1.0), 1.5), 1);
                    var alpha_iLmY = Math.Max(Math.Min(4.0 / (Math.Abs((Myi + Myi_y2) * 1e+6) / (Math.Abs((Qzi + Qzi_y2) * 1e+3) * d_i) + 1.0), 1.5), 1);
                    var alpha_iLpX2 = Math.Max(Math.Min(4.0 / (Math.Abs((Mzi + Mzi_x) * 1e+6) / (Math.Abs((Qyi + Qyi_x) * 1e+3) * d_i2) + 1.0), 1.5), 1);
                    var alpha_iLpY2 = Math.Max(Math.Min(4.0 / (Math.Abs((Mzi + Mzi_y) * 1e+6) / (Math.Abs((Qyi + Qyi_y) * 1e+3) * d_i2) + 1.0), 1.5), 1);
                    var alpha_iLmX2 = Math.Max(Math.Min(4.0 / (Math.Abs((Mzi + Mzi_x2) * 1e+6) / (Math.Abs((Qyi + Qyi_x2) * 1e+3) * d_i2) + 1.0), 1.5), 1);
                    var alpha_iLmY2 = Math.Max(Math.Min(4.0 / (Math.Abs((Mzi + Mzi_y2) * 1e+6) / (Math.Abs((Qyi + Qyi_y2) * 1e+3) * d_i2) + 1.0), 1.5), 1);
                    var wft_iL = 195.0; var wft_iS = 295.0;
                    if (S_i > 18.9 && S_i < 28.9) { wft_iL = 215.0; }
                    if (S_i > 18.9 && S_i < 28.9) { wft_iS = 345.0; }
                    else if (S_i > 28.9) { wft_iS = 390.0; }
                    Fsi.Add(new List<string> { wft_iL.ToString().Substring(0, Digit((int)wft_iL)), "", wft_iS.ToString().Substring(0, Digit((int)wft_iS)) });
                    var aw_i = s_i * Math.Pow(S_i, 2) * Math.PI / 4.0;
                    var pw_i = aw_i / (b * P_i);
                    var aw_i2 = s_i2 * Math.Pow(S_i2, 2) * Math.PI / 4.0;
                    var pw_i2 = aw_i2 / (D * P_i2);
                    var Qa_iL = (b * j_i * alpha_iL * fsL) * unitf;
                    var Qa_iL2 = (D * j_i2 * alpha_iL2 * fsL) * unitf;
                    var Qa_iLpX = (b * j_i * (2.0 / 3.0 * alpha_iLpX * fsS + 0.5 * wft_iS * (Math.Min(pw_i, 0.012) - 0.002))) * unitf;
                    var Qa_iLpY = (b * j_i * (2.0 / 3.0 * alpha_iLpY * fsS + 0.5 * wft_iS * (Math.Min(pw_i, 0.012) - 0.002))) * unitf;
                    var Qa_iLmX = (b * j_i * (2.0 / 3.0 * alpha_iLmX * fsS + 0.5 * wft_iS * (Math.Min(pw_i, 0.012) - 0.002))) * unitf;
                    var Qa_iLmY = (b * j_i * (2.0 / 3.0 * alpha_iLmY * fsS + 0.5 * wft_iS * (Math.Min(pw_i, 0.012) - 0.002))) * unitf;
                    var Qa_iLpX2 = (D * j_i2 * (2.0 / 3.0 * alpha_iLpX2 * fsS + 0.5 * wft_iS * (Math.Min(pw_i2, 0.012) - 0.002))) * unitf;
                    var Qa_iLpY2 = (D * j_i2 * (2.0 / 3.0 * alpha_iLpY2 * fsS + 0.5 * wft_iS * (Math.Min(pw_i2, 0.012) - 0.002))) * unitf;
                    var Qa_iLmX2 = (D * j_i2 * (2.0 / 3.0 * alpha_iLmX2 * fsS + 0.5 * wft_iS * (Math.Min(pw_i2, 0.012) - 0.002))) * unitf;
                    var Qa_iLmY2 = (D * j_i2 * (2.0 / 3.0 * alpha_iLmY2 * fsS + 0.5 * wft_iS * (Math.Min(pw_i2, 0.012) - 0.002))) * unitf;
                    //************************************************************************************************************************************************************
                    //中央の許容せん断力
                    //************************************************************************************************************************************************************
                    var d_c = D - (d_cT + d_cB) / 2.0; var j_c = d_c * 7.0 / 8.0; var d_c2 = b - (d_cR + d_cL) / 2.0; var j_c2 = d_c * 7.0 / 8.0;
                    var alpha_cL = Math.Max(Math.Min(4.0 / (Math.Abs(Myc * 1e+6) / (Math.Abs(Qzc * 1e+3) * d_c) + 1.0), 1.5), 1);
                    var alpha_cL2 = Math.Max(Math.Min(4.0 / (Math.Abs(Mzc * 1e+6) / (Math.Abs(Qyc * 1e+3) * d_c2) + 1.0), 1.5), 1);
                    var alpha_cLpX = Math.Max(Math.Min(4.0 / (Math.Abs((Myc + Myc_x) * 1e+6) / (Math.Abs((Qzc + Qzc_x) * 1e+3) * d_c) + 1.0), 1.5), 1);
                    var alpha_cLpY = Math.Max(Math.Min(4.0 / (Math.Abs((Myc + Myc_y) * 1e+6) / (Math.Abs((Qzc + Qzc_y) * 1e+3) * d_c) + 1.0), 1.5), 1);
                    var alpha_cLmX = Math.Max(Math.Min(4.0 / (Math.Abs((Myc + Myc_x2) * 1e+6) / (Math.Abs((Qzc + Qzc_x2) * 1e+3) * d_c) + 1.0), 1.5), 1);
                    var alpha_cLmY = Math.Max(Math.Min(4.0 / (Math.Abs((Myc + Myc_y2) * 1e+6) / (Math.Abs((Qzc + Qzc_y2) * 1e+3) * d_c) + 1.0), 1.5), 1);
                    var alpha_cLpX2 = Math.Max(Math.Min(4.0 / (Math.Abs((Mzc + Mzc_x) * 1e+6) / (Math.Abs((Qyc + Qyc_x) * 1e+3) * d_c2) + 1.0), 1.5), 1);
                    var alpha_cLpY2 = Math.Max(Math.Min(4.0 / (Math.Abs((Mzc + Mzc_y) * 1e+6) / (Math.Abs((Qyc + Qyc_y) * 1e+3) * d_c2) + 1.0), 1.5), 1);
                    var alpha_cLmX2 = Math.Max(Math.Min(4.0 / (Math.Abs((Mzc + Mzc_x2) * 1e+6) / (Math.Abs((Qyc + Qyc_x2) * 1e+3) * d_c2) + 1.0), 1.5), 1);
                    var alpha_cLmY2 = Math.Max(Math.Min(4.0 / (Math.Abs((Mzc + Mzc_y2) * 1e+6) / (Math.Abs((Qyc + Qyc_y2) * 1e+3) * d_c2) + 1.0), 1.5), 1);
                    var wft_cL = 195.0; var wft_cS = 295.0;
                    if (S_c > 18.9 && S_c < 28.9) { wft_cL = 215.0; }
                    if (S_c > 18.9 && S_c < 28.9) { wft_cS = 345.0; }
                    else if (S_c > 28.9) { wft_cS = 390.0; }
                    Fsi.Add(new List<string> { wft_cL.ToString().Substring(0, Digit((int)wft_cL)), "", wft_cS.ToString().Substring(0, Digit((int)wft_cS)) });
                    var aw_c = s_c * Math.Pow(S_c, 2) * Math.PI / 4.0;
                    var pw_c = aw_c / (b * P_c);
                    var aw_c2 = s_c2 * Math.Pow(S_c2, 2) * Math.PI / 4.0;
                    var pw_c2 = aw_c2 / (D * P_c2);
                    var Qa_cL = (b * j_c * alpha_cL * fsL) * unitf;
                    var Qa_cL2 = (D * j_c2 * alpha_cL2 * fsL) * unitf;
                    var Qa_cLpX = (b * j_c * (2.0 / 3.0 * alpha_cLpX * fsS + 0.5 * wft_cS * (Math.Min(pw_c, 0.012) - 0.002))) * unitf;
                    var Qa_cLpY = (b * j_c * (2.0 / 3.0 * alpha_cLpY * fsS + 0.5 * wft_cS * (Math.Min(pw_c, 0.012) - 0.002))) * unitf;
                    var Qa_cLmX = (b * j_c * (2.0 / 3.0 * alpha_cLmX * fsS + 0.5 * wft_cS * (Math.Min(pw_c, 0.012) - 0.002))) * unitf;
                    var Qa_cLmY = (b * j_c * (2.0 / 3.0 * alpha_cLmY * fsS + 0.5 * wft_cS * (Math.Min(pw_c, 0.012) - 0.002))) * unitf;
                    var Qa_cLpX2 = (D * j_c2 * (2.0 / 3.0 * alpha_cLpX2 * fsS + 0.5 * wft_cS * (Math.Min(pw_c2, 0.012) - 0.002))) * unitf;
                    var Qa_cLpY2 = (D * j_c2 * (2.0 / 3.0 * alpha_cLpY2 * fsS + 0.5 * wft_cS * (Math.Min(pw_c2, 0.012) - 0.002))) * unitf;
                    var Qa_cLmX2 = (D * j_c2 * (2.0 / 3.0 * alpha_cLmX2 * fsS + 0.5 * wft_cS * (Math.Min(pw_c2, 0.012) - 0.002))) * unitf;
                    var Qa_cLmY2 = (D * j_c2 * (2.0 / 3.0 * alpha_cLmY2 * fsS + 0.5 * wft_cS * (Math.Min(pw_c2, 0.012) - 0.002))) * unitf;
                    //************************************************************************************************************************************************************
                    //j端の許容せん断力
                    //************************************************************************************************************************************************************
                    var d_j = D - (d_jT + d_jB) / 2.0; var j_j = d_j * 7.0 / 8.0; var d_j2 = b - (d_jR + d_jL) / 2.0; var j_j2 = d_j * 7.0 / 8.0;
                    var alpha_jL = Math.Max(Math.Min(4.0 / (Math.Abs(Myj * 1e+6) / (Math.Abs(Qzj * 1e+3) * d_j) + 1.0), 1.5), 1);
                    var alpha_jL2 = Math.Max(Math.Min(4.0 / (Math.Abs(Mzj * 1e+6) / (Math.Abs(Qyj * 1e+3) * d_j2) + 1.0), 1.5), 1);
                    var alpha_jLpX = Math.Max(Math.Min(4.0 / (Math.Abs((Myj + Myj_x) * 1e+6) / (Math.Abs((Qzj + Qzj_x) * 1e+3) * d_j) + 1.0), 1.5), 1);
                    var alpha_jLpY = Math.Max(Math.Min(4.0 / (Math.Abs((Myj + Myj_y) * 1e+6) / (Math.Abs((Qzj + Qzj_y) * 1e+3) * d_j) + 1.0), 1.5), 1);
                    var alpha_jLmX = Math.Max(Math.Min(4.0 / (Math.Abs((Myj + Myj_x2) * 1e+6) / (Math.Abs((Qzj + Qzj_x2) * 1e+3) * d_j) + 1.0), 1.5), 1);
                    var alpha_jLmY = Math.Max(Math.Min(4.0 / (Math.Abs((Myj + Myj_y2) * 1e+6) / (Math.Abs((Qzj + Qzj_y2) * 1e+3) * d_j) + 1.0), 1.5), 1);
                    var alpha_jLpX2 = Math.Max(Math.Min(4.0 / (Math.Abs((Mzj + Mzj_x) * 1e+6) / (Math.Abs((Qyj + Qyj_x) * 1e+3) * d_j2) + 1.0), 1.5), 1);
                    var alpha_jLpY2 = Math.Max(Math.Min(4.0 / (Math.Abs((Mzj + Mzj_y) * 1e+6) / (Math.Abs((Qyj + Qyj_y) * 1e+3) * d_j2) + 1.0), 1.5), 1);
                    var alpha_jLmX2 = Math.Max(Math.Min(4.0 / (Math.Abs((Mzj + Mzj_x2) * 1e+6) / (Math.Abs((Qyj + Qyj_x2) * 1e+3) * d_j2) + 1.0), 1.5), 1);
                    var alpha_jLmY2 = Math.Max(Math.Min(4.0 / (Math.Abs((Mzj + Mzj_y2) * 1e+6) / (Math.Abs((Qyj + Qyj_y2) * 1e+3) * d_j2) + 1.0), 1.5), 1);
                    var wft_jL = 195.0; var wft_jS = 295.0;
                    if (S_j > 18.9 && S_j < 28.9) { wft_jL = 215.0; }
                    if (S_j > 18.9 && S_j < 28.9) { wft_jS = 345.0; }
                    else if (S_j > 28.9) { wft_jS = 390.0; }
                    Fsi.Add(new List<string> { wft_jL.ToString().Substring(0, Digit((int)wft_jL)), "", wft_jS.ToString().Substring(0, Digit((int)wft_jS)) });
                    var aw_j = s_j * Math.Pow(S_j, 2) * Math.PI / 4.0;
                    var pw_j = aw_j / (b * P_j);
                    var aw_j2 = s_j2 * Math.Pow(S_j2, 2) * Math.PI / 4.0;
                    var pw_j2 = aw_j2 / (D * P_j2);
                    var Qa_jL = (b * j_j * alpha_jL * fsL) * unitf;
                    var Qa_jL2 = (D * j_j2 * alpha_jL2 * fsL) * unitf;
                    var Qa_jLpX = (b * j_j * (2.0 / 3.0 * alpha_jLpX * fsS + 0.5 * wft_jS * (Math.Min(pw_j, 0.012) - 0.002))) * unitf;
                    var Qa_jLpY = (b * j_j * (2.0 / 3.0 * alpha_jLpY * fsS + 0.5 * wft_jS * (Math.Min(pw_j, 0.012) - 0.002))) * unitf;
                    var Qa_jLmX = (b * j_j * (2.0 / 3.0 * alpha_jLmX * fsS + 0.5 * wft_jS * (Math.Min(pw_j, 0.012) - 0.002))) * unitf;
                    var Qa_jLmY = (b * j_j * (2.0 / 3.0 * alpha_jLmY * fsS + 0.5 * wft_jS * (Math.Min(pw_j, 0.012) - 0.002))) * unitf;
                    var Qa_jLpX2 = (D * j_j2 * (2.0 / 3.0 * alpha_jLpX2 * fsS + 0.5 * wft_jS * (Math.Min(pw_j2, 0.012) - 0.002))) * unitf;
                    var Qa_jLpY2 = (D * j_j2 * (2.0 / 3.0 * alpha_jLpY2 * fsS + 0.5 * wft_jS * (Math.Min(pw_j2, 0.012) - 0.002))) * unitf;
                    var Qa_jLmX2 = (D * j_j2 * (2.0 / 3.0 * alpha_jLmX2 * fsS + 0.5 * wft_jS * (Math.Min(pw_j2, 0.012) - 0.002))) * unitf;
                    var Qa_jLmY2 = (D * j_j2 * (2.0 / 3.0 * alpha_jLmY2 * fsS + 0.5 * wft_jS * (Math.Min(pw_j2, 0.012) - 0.002))) * unitf;
                    //************************************************************************************************************************************************************
                    DT.Add(new List<string> { (d_iT.ToString()).Substring(0, Digit((int)d_iT)), (d_cT.ToString()).Substring(0, Digit((int)d_cT)), (d_jT.ToString()).Substring(0, Digit((int)d_jT)) });
                    DB.Add(new List<string> { (d_iB.ToString()).Substring(0, Digit((int)d_iB)), (d_cB.ToString()).Substring(0, Digit((int)d_cB)), (d_jB.ToString()).Substring(0, Digit((int)d_jB)) });
                    DR.Add(new List<string> { (d_iR.ToString()).Substring(0, Digit((int)d_iR)), (d_cR.ToString()).Substring(0, Digit((int)d_cR)), (d_jR.ToString()).Substring(0, Digit((int)d_jR)) });
                    DL.Add(new List<string> { (d_iL.ToString()).Substring(0, Digit((int)d_iL)), (d_cL.ToString()).Substring(0, Digit((int)d_cL)), (d_jL.ToString()).Substring(0, Digit((int)d_jL))});
                    MT_aL.Add(new List<double> { Ma_iTL, Ma_cTL, Ma_jTL }); MB_aL.Add(new List<double> { Ma_iBL, Ma_cBL, Ma_jBL });
                    MR_aL.Add(new List<double> { Ma_iRL, Ma_cRL, Ma_jRL }); ML_aL.Add(new List<double> { Ma_iLL, Ma_cLL, Ma_jLL });
                    MT_aLpX.Add(new List<double> { Ma_iTLpX, Ma_cTLpX, Ma_jTLpX }); MB_aLpX.Add(new List<double> { Ma_iBLpX, Ma_cBLpX, Ma_jBLpX });
                    MR_aLpX.Add(new List<double> { Ma_iRLpX, Ma_cRLpX, Ma_jRLpX }); ML_aLpX.Add(new List<double> { Ma_iLLpX, Ma_cLLpX, Ma_jLLpX });
                    MT_aLmX.Add(new List<double> { Ma_iTLmX, Ma_cTLmX, Ma_jTLmX }); MB_aLmX.Add(new List<double> { Ma_iBLmX, Ma_cBLmX, Ma_jBLmX });
                    MR_aLmX.Add(new List<double> { Ma_iRLmX, Ma_cRLmX, Ma_jRLmX }); ML_aLmX.Add(new List<double> { Ma_iLLmX, Ma_cLLmX, Ma_jLLmX });
                    MT_aLpY.Add(new List<double> { Ma_iTLpY, Ma_cTLpY, Ma_jTLpY }); MB_aLpY.Add(new List<double> { Ma_iBLpY, Ma_cBLpY, Ma_jBLpY });
                    MR_aLpY.Add(new List<double> { Ma_iRLpY, Ma_cRLpY, Ma_jRLpY }); ML_aLpY.Add(new List<double> { Ma_iLLpY, Ma_cLLpY, Ma_jLLpY });
                    MT_aLmY.Add(new List<double> { Ma_iTLmY, Ma_cTLmY, Ma_jTLmY }); MB_aLmY.Add(new List<double> { Ma_iBLmY, Ma_cBLmY, Ma_jBLmY });
                    MR_aLmY.Add(new List<double> { Ma_iRLmY, Ma_cRLmY, Ma_jRLmY }); ML_aLmY.Add(new List<double> { Ma_iLLmY, Ma_cLLmY, Ma_jLLmY });
                    Q_aL.Add(new List<double> { Qa_iL, Qa_cL, Qa_jL }); Q_aLpX.Add(new List<double> { Qa_iLpX, Qa_cLpX, Qa_jLpX }); Q_aLpY.Add(new List<double> { Qa_iLpY, Qa_cLpY, Qa_jLpY }); Q_aLmX.Add(new List<double> { Qa_iLmX, Qa_cLmX, Qa_jLmX }); Q_aLmY.Add(new List<double> { Qa_iLmY, Qa_cLmY, Qa_jLmY });
                    Q_aL2.Add(new List<double> { Qa_iL2, Qa_cL2, Qa_jL2 }); Q_aLpX2.Add(new List<double> { Qa_iLpX2, Qa_cLpX2, Qa_jLpX2 }); Q_aLpY2.Add(new List<double> { Qa_iLpY2, Qa_cLpY2, Qa_jLpY2 }); Q_aLmX2.Add(new List<double> { Qa_iLmX2, Qa_cLmX2, Qa_jLmX2 }); Q_aLmY2.Add(new List<double> { Qa_iLmY2, Qa_cLmY2, Qa_jLmY2 });
                    MyL.Add(new List<double> { Myi, Myc, Myj }); MzL.Add(new List<double> { Mzi, Mzc, Mzj });
                    QyL.Add(new List<double> { Qyi, Qyc, Qyj }); QzL.Add(new List<double> { Qzi, Qzc, Qzj });
                    MyLpX.Add(new List<double> { Myi+Myi_x, Myc+Myc_x, Myj+Myj_x }); MzLpX.Add(new List<double> { Mzi+Mzi_x, Mzc+Mzc_x, Mzj+Mzj_x });
                    QyLpX.Add(new List<double> { Qyi + N * Qyi_x, Qyc + N * Qyc_x, Qyj + N * Qyj_x }); QzLpX.Add(new List<double> { Qzi+N*Qzi_x, Qzc+N*Qzc_x, Qzj+N*Qzj_x });
                    MyLmX.Add(new List<double> { Myi + Myi_x2, Myc + Myc_x2, Myj + Myj_x2 }); MzLmX.Add(new List<double> { Mzi + Mzi_x2, Mzc + Mzc_x2, Mzj + Mzj_x2 });
                    QyLmX.Add(new List<double> { Qyi + N * Qyi_x2, Qyc + N * Qyc_x2, Qyj + N * Qyj_x2 }); QzLmX.Add(new List<double> { Qzi + N * Qzi_x2, Qzc + N * Qzc_x2, Qzj + N * Qzj_x2 });
                    MyLpY.Add(new List<double> { Myi + Myi_y, Myc + Myc_y, Myj + Myj_y }); MzLpY.Add(new List<double> { Mzi + Mzi_y, Mzc + Mzc_y, Mzj + Mzj_y });
                    QyLpY.Add(new List<double> { Qyi + N * Qyi_y, Qyc + N * Qyc_y, Qyj + N * Qyj_y }); QzLpY.Add(new List<double> { Qzi + N * Qzi_y, Qzc + N * Qzc_y, Qzj + N * Qzj_y });
                    MyLmY.Add(new List<double> { Myi + Myi_y2, Myc + Myc_y2, Myj + Myj_y2 }); MzLmY.Add(new List<double> { Mzi + Mzi_y2, Mzc + Mzc_y2, Mzj + Mzj_y2 });
                    QyLmY.Add(new List<double> { Qyi + N * Qyi_y2, Qyc + N * Qyc_y2, Qyj + N * Qyj_y2 }); QzLmY.Add(new List<double> { Qzi + N * Qzi_y2, Qzc + N * Qzc_y2, Qzj + N * Qzj_y2 });
                    //************************************************************************************************************************************************************
                    var MLlist = new List<GH_Number>(); var MLpXlist = new List<GH_Number>(); var MLmXlist = new List<GH_Number>(); var MLpYlist = new List<GH_Number>(); var MLmYlist = new List<GH_Number>();
                    MLlist.Add(new GH_Number(Ma_iTL)); MLlist.Add(new GH_Number(Ma_cTL)); MLlist.Add(new GH_Number(Ma_jTL)); MLlist.Add(new GH_Number(Ma_iBL)); MLlist.Add(new GH_Number(Ma_cBL)); MLlist.Add(new GH_Number(Ma_jBL));
                    MLlist.Add(new GH_Number(Ma_iRL)); MLlist.Add(new GH_Number(Ma_cRL)); MLlist.Add(new GH_Number(Ma_jRL)); MLlist.Add(new GH_Number(Ma_iLL)); MLlist.Add(new GH_Number(Ma_cLL)); MLlist.Add(new GH_Number(Ma_jLL));
                    MLpXlist.Add(new GH_Number(Ma_iTLpX)); MLpXlist.Add(new GH_Number(Ma_cTLpX)); MLpXlist.Add(new GH_Number(Ma_jTLpX)); MLpXlist.Add(new GH_Number(Ma_iBLpX)); MLpXlist.Add(new GH_Number(Ma_cBLpX)); MLpXlist.Add(new GH_Number(Ma_jBLpX));
                    MLpXlist.Add(new GH_Number(Ma_iRLpX)); MLpXlist.Add(new GH_Number(Ma_cRLpX)); MLpXlist.Add(new GH_Number(Ma_jRLpX)); MLpXlist.Add(new GH_Number(Ma_iLLpX)); MLpXlist.Add(new GH_Number(Ma_cLLpX)); MLpXlist.Add(new GH_Number(Ma_jLLpX));
                    MLpYlist.Add(new GH_Number(Ma_iTLpY)); MLpYlist.Add(new GH_Number(Ma_cTLpY)); MLpYlist.Add(new GH_Number(Ma_jTLpY)); MLpYlist.Add(new GH_Number(Ma_iBLpY)); MLpYlist.Add(new GH_Number(Ma_cBLpY)); MLpYlist.Add(new GH_Number(Ma_jBLpY));
                    MLpYlist.Add(new GH_Number(Ma_iRLpY)); MLpYlist.Add(new GH_Number(Ma_cRLpY)); MLpYlist.Add(new GH_Number(Ma_jRLpY)); MLpYlist.Add(new GH_Number(Ma_iLLpY)); MLpYlist.Add(new GH_Number(Ma_cLLpY)); MLpYlist.Add(new GH_Number(Ma_jLLpY));
                    MLmXlist.Add(new GH_Number(Ma_iTLmX)); MLmXlist.Add(new GH_Number(Ma_cTLmX)); MLmXlist.Add(new GH_Number(Ma_jTLmX)); MLmXlist.Add(new GH_Number(Ma_iBLmX)); MLmXlist.Add(new GH_Number(Ma_cBLmX)); MLmXlist.Add(new GH_Number(Ma_jBLmX));
                    MLmXlist.Add(new GH_Number(Ma_iRLmX)); MLmXlist.Add(new GH_Number(Ma_cRLmX)); MLmXlist.Add(new GH_Number(Ma_jRLmX)); MLmXlist.Add(new GH_Number(Ma_iLLmX)); MLmXlist.Add(new GH_Number(Ma_cLLmX)); MLmXlist.Add(new GH_Number(Ma_jLLmX));
                    MLmYlist.Add(new GH_Number(Ma_iTLmY)); MLmYlist.Add(new GH_Number(Ma_cTLmY)); MLmYlist.Add(new GH_Number(Ma_jTLmY)); MLmYlist.Add(new GH_Number(Ma_iBLmY)); MLmYlist.Add(new GH_Number(Ma_cBLmY)); MLmYlist.Add(new GH_Number(Ma_jBLmY));
                    MLmYlist.Add(new GH_Number(Ma_iRLmY)); MLmYlist.Add(new GH_Number(Ma_cRLmY)); MLmYlist.Add(new GH_Number(Ma_jRLmY)); MLmYlist.Add(new GH_Number(Ma_iLLmY)); MLmYlist.Add(new GH_Number(Ma_cLLmY)); MLmYlist.Add(new GH_Number(Ma_jLLmY));
                    MaL.AppendRange(MLlist, new GH_Path(ind)); MaLpX.AppendRange(MLpXlist, new GH_Path(ind)); MaLpY.AppendRange(MLpYlist, new GH_Path(ind)); MaLmX.AppendRange(MLmXlist, new GH_Path(ind)); MaLmY.AppendRange(MLmYlist, new GH_Path(ind));
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
                    var klist = new List<GH_Number>(); var k2list = new List<GH_Number>(); var klist2 = new List<GH_Number>(); var k2list2 = new List<GH_Number>();
                    if (Myi < 0) { klist.Add(new GH_Number(Math.Abs(Myi) / Ma_iTL)); }
                    else { klist.Add(new GH_Number(Math.Abs(Myi) / Ma_iBL)); }
                    if (Myj > 0) { klist.Add(new GH_Number(Math.Abs(Myj) / Ma_jTL)); }
                    else { klist.Add(new GH_Number(Math.Abs(Myj) / Ma_jBL)); }
                    if (Myc < 0) { klist.Add(new GH_Number(Math.Abs(Myc) / Ma_cTL)); }
                    else { klist.Add(new GH_Number(Math.Abs(Myc) / Ma_cBL)); }
                    if (Mzi < 0) { klist2.Add(new GH_Number(Math.Abs(Mzi) / Ma_iRL)); }
                    else { klist2.Add(new GH_Number(Math.Abs(Mzi) / Ma_iLL)); }
                    if (Mzj > 0) { klist2.Add(new GH_Number(Math.Abs(Mzj) / Ma_jRL)); }
                    else { klist2.Add(new GH_Number(Math.Abs(Mzj) / Ma_jLL)); }
                    if (Mzc < 0) { klist2.Add(new GH_Number(Math.Abs(Mzc) / Ma_cRL)); }
                    else { klist2.Add(new GH_Number(Math.Abs(Mzc) / Ma_cLL)); }
                    klist.Add(new GH_Number(Math.Abs(Qzi) / Qa_iL)); klist.Add(new GH_Number(Math.Abs(Qzj) / Qa_jL)); klist.Add(new GH_Number(Math.Abs(Qzc) / Qa_cL)); klist2.Add(new GH_Number(Math.Abs(Qyi) / Qa_iL2)); klist2.Add(new GH_Number(Math.Abs(Qyj) / Qa_jL2)); klist2.Add(new GH_Number(Math.Abs(Qyc) / Qa_cL2));
                    //kentei.AppendRange(klist, new GH_Path(e,0)); kentei.AppendRange(klist2, new GH_Path(e, 1));
                    var maxval = 0.0;
                    for (int i = 0; i < klist.Count; i++)
                    {
                        maxval = Math.Max(maxval, klist[i].Value);
                    }
                    for (int i = 0; i < klist2.Count; i++)
                    {
                        maxval = Math.Max(maxval, klist2[i].Value);
                    }
                    kmaxL.Add(maxval);
                    var ki = new List<double>(); var kj = new List<double>(); var kc = new List<double>();
                    if (Myi + Myi_x < 0) { ki.Add(Math.Abs(Myi + Myi_x) / Ma_iTLpX); }
                    else { ki.Add(Math.Abs(Myi + Myi_x) / Ma_iBLpX); }
                    if (Myi + Myi_x2 < 0) { ki.Add(Math.Abs(Myi + Myi_x2) / Ma_iTLmX); }
                    else { ki.Add(Math.Abs(Myi + Myi_x2) / Ma_iBLmX); }
                    if (Myi + Myi_y < 0) { ki.Add(Math.Abs(Myi + Myi_y) / Ma_iTLpY); }
                    else { ki.Add(Math.Abs(Myi + Myi_y) / Ma_iBLpY); }
                    if (Myi + Myi_y2 < 0) { ki.Add(Math.Abs(Myi + Myi_y2) / Ma_iTLmY); }
                    else { ki.Add(Math.Abs(Myi + Myi_y2) / Ma_iBLmY); }
                    if (Myj + Myj_x < 0) { kj.Add(Math.Abs(Myj + Myj_x) / Ma_jTLpX); }
                    else { kj.Add(Math.Abs(Myj + Myj_x) / Ma_jBLpX); }
                    if (Myj + Myj_x2 < 0) { kj.Add(Math.Abs(Myj + Myj_x2) / Ma_jTLmX); }
                    else { kj.Add(Math.Abs(Myj + Myj_x2) / Ma_jBLmX); }
                    if (Myj + Myj_y < 0) { kj.Add(Math.Abs(Myj + Myj_y) / Ma_jTLpY); }
                    else { kj.Add(Math.Abs(Myj + Myj_y) / Ma_jBLpY); }
                    if (Myj + Myj_y2 < 0) { kj.Add(Math.Abs(Myj + Myj_y2) / Ma_jTLmY); }
                    else { kj.Add(Math.Abs(Myj + Myj_y2) / Ma_jBLmY); }
                    if (Myc + Myc_x < 0) { kc.Add(Math.Abs(Myc + Myc_x) / Ma_cTLpX); }
                    else { kc.Add(Math.Abs(Myc + Myc_x) / Ma_cBLpX); }
                    if (Myc + Myc_x2 < 0) { kc.Add(Math.Abs(Myc + Myc_x2) / Ma_cTLmX); }
                    else { kc.Add(Math.Abs(Myc + Myc_x2) / Ma_cBLmX); }
                    if (Myc + Myc_y < 0) { kc.Add(Math.Abs(Myc + Myc_y) / Ma_cTLpY); }
                    else { kc.Add(Math.Abs(Myc + Myc_y) / Ma_cBLpY); }
                    if (Myc + Myc_y2 < 0) { kc.Add(Math.Abs(Myc + Myc_y2) / Ma_cTLmY); }
                    else { kc.Add(Math.Abs(Myc + Myc_y2) / Ma_cBLmY); }
                    k2list.Add(new GH_Number(Math.Max(Math.Max(ki[0], ki[1]), Math.Max(ki[2], ki[3]))));
                    k2list.Add(new GH_Number(Math.Max(Math.Max(kj[0], kj[1]), Math.Max(kj[2], kj[3]))));
                    k2list.Add(new GH_Number(Math.Max(Math.Max(kc[0], kc[1]), Math.Max(kc[2], kc[3]))));
                    k2list.Add(new GH_Number(Math.Max(Math.Max(Math.Abs(Qzi + Qzi_x * N) / Qa_iLpX, Math.Abs(Qzi + Qzi_x2 * N) / Qa_iLmX), Math.Max(Math.Abs(Qzi + Qzi_y * N) / Qa_iLpY, Math.Abs(Qzi + Qzi_y2 * N) / Qa_iLpY))));
                    k2list.Add(new GH_Number(Math.Max(Math.Max(Math.Abs(Qzj + Qzj_x * N) / Qa_jLpX, Math.Abs(Qzj + Qzj_x2 * N) / Qa_jLmX), Math.Max(Math.Abs(Qzj + Qzj_y * N) / Qa_jLpY, Math.Abs(Qzj + Qzj_y2 * N) / Qa_jLpY))));
                    k2list.Add(new GH_Number(Math.Max(Math.Max(Math.Abs(Qzc + Qzc_x * N) / Qa_cLpX, Math.Abs(Qzc + Qzc_x2 * N) / Qa_cLmX), Math.Max(Math.Abs(Qzc + Qzc_y * N) / Qa_cLpY, Math.Abs(Qzc + Qzc_y2 * N) / Qa_cLpY))));

                    var ki2 = new List<double>(); var kj2 = new List<double>(); var kc2 = new List<double>();
                    if (Mzi + Mzi_x < 0) { ki2.Add(Math.Abs(Mzi + Mzi_x) / Ma_iRLpX); }
                    else { ki2.Add(Math.Abs(Mzi + Mzi_x) / Ma_iLLpX); }
                    if (Mzi + Mzi_x2 < 0) { ki2.Add(Math.Abs(Mzi + Mzi_x2) / Ma_iRLmX); }
                    else { ki2.Add(Math.Abs(Mzi + Mzi_x2) / Ma_iLLmX); }
                    if (Mzi + Mzi_y < 0) { ki2.Add(Math.Abs(Mzi + Mzi_y) / Ma_iRLpY); }
                    else { ki2.Add(Math.Abs(Mzi + Mzi_y) / Ma_iLLpY); }
                    if (Mzi + Mzi_y2 < 0) { ki2.Add(Math.Abs(Mzi + Mzi_y2) / Ma_iRLmY); }
                    else { ki2.Add(Math.Abs(Mzi + Mzi_y2) / Ma_iLLmY); }
                    if (Mzj + Mzj_x < 0) { kj2.Add(Math.Abs(Mzj + Mzj_x) / Ma_jRLpX); }
                    else { kj2.Add(Math.Abs(Mzj + Mzj_x) / Ma_jLLpX); }
                    if (Mzj + Mzj_x2 < 0) { kj2.Add(Math.Abs(Mzj + Mzj_x2) / Ma_jRLmX); }
                    else { kj2.Add(Math.Abs(Mzj + Mzj_x2) / Ma_jLLmX); }
                    if (Mzj + Mzj_y < 0) { kj2.Add(Math.Abs(Mzj + Mzj_y) / Ma_jRLpY); }
                    else { kj2.Add(Math.Abs(Mzj + Mzj_y) / Ma_jLLpY); }
                    if (Mzj + Mzj_y2 < 0) { kj2.Add(Math.Abs(Mzj + Mzj_y2) / Ma_jRLmY); }
                    else { kj2.Add(Math.Abs(Mzj + Mzj_y2) / Ma_jLLmY); }
                    if (Mzc + Mzc_x < 0) { kc2.Add(Math.Abs(Mzc + Mzc_x) / Ma_cRLpX); }
                    else { kc2.Add(Math.Abs(Mzc + Mzc_x) / Ma_cLLpX); }
                    if (Mzc + Mzc_x2 < 0) { kc2.Add(Math.Abs(Mzc + Mzc_x2) / Ma_cRLmX); }
                    else { kc2.Add(Math.Abs(Mzc + Mzc_x2) / Ma_cLLmX); }
                    if (Mzc + Mzc_y < 0) { kc2.Add(Math.Abs(Mzc + Mzc_y) / Ma_cRLpY); }
                    else { kc2.Add(Math.Abs(Mzc + Mzc_y) / Ma_cLLpY); }
                    if (Mzc + Mzc_y2 < 0) { kc2.Add(Math.Abs(Mzc + Mzc_y2) / Ma_cRLmY); }
                    else { kc2.Add(Math.Abs(Mzc + Mzc_y2) / Ma_cLLmY); }
                    k2list2.Add(new GH_Number(Math.Max(Math.Max(ki2[0], ki2[1]), Math.Max(ki2[2], ki2[3]))));
                    k2list2.Add(new GH_Number(Math.Max(Math.Max(kj2[0], kj2[1]), Math.Max(kj2[2], kj2[3]))));
                    k2list2.Add(new GH_Number(Math.Max(Math.Max(kc2[0], kc2[1]), Math.Max(kc2[2], kc2[3]))));
                    k2list2.Add(new GH_Number(Math.Max(Math.Max(Math.Abs(Qyi + Qyi_x * N) / Qa_iLpX2, Math.Abs(Qyi + Qyi_x2 * N) / Qa_iLmX2), Math.Max(Math.Abs(Qyi + Qyi_y * N) / Qa_iLpY2, Math.Abs(Qyi + Qyi_y2 * N) / Qa_iLpY2))));
                    k2list2.Add(new GH_Number(Math.Max(Math.Max(Math.Abs(Qyj + Qyj_x * N) / Qa_jLpX2, Math.Abs(Qyj + Qyj_x2 * N) / Qa_jLmX2), Math.Max(Math.Abs(Qyj + Qyj_y * N) / Qa_jLpY2, Math.Abs(Qyj + Qyj_y2 * N) / Qa_jLpY2))));
                    k2list2.Add(new GH_Number(Math.Max(Math.Max(Math.Abs(Qyc + Qyc_x * N) / Qa_cLpX2, Math.Abs(Qyc + Qyc_x2 * N) / Qa_cLmX2), Math.Max(Math.Abs(Qyc + Qyc_y * N) / Qa_cLpY2, Math.Abs(Qyc + Qyc_y2 * N) / Qa_cLpY2))));
                    //kentei.AppendRange(k2list, new GH_Path(e, 2)); kentei.AppendRange(k2list2, new GH_Path(e, 3));
                    maxval = 0.0;
                    for (int i = 0; i < k2list.Count; i++)
                    {
                        maxval = Math.Max(maxval, k2list[i].Value);
                    }
                    for (int i = 0; i < k2list2.Count; i++)
                    {
                        maxval = Math.Max(maxval, k2list2[i].Value);
                    }
                    kmaxS.Add(maxval);
                    var r1 = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value); var r2 = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value);
                    var rc = (r1 + r2) / 2.0; var ri = (r1 + rc) / 2.0; var rj = (r2 + rc) / 2.0;
                    if (on_off_11 == 1)
                    {
                        if (on_off_21 == 1)
                        {
                            var k = Math.Max(klist[0].Value, klist2[0].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(ri);
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = Math.Max(klist[1].Value, klist2[1].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rj);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = Math.Max(klist[2].Value, klist2[2].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rc);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                        }
                        else if (on_off_22 == 1)
                        {
                            var k = Math.Max(klist[3].Value, klist2[3].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(ri);
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = Math.Max(klist[4].Value, klist2[4].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rj);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = Math.Max(klist[5].Value, klist2[5].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rc);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                        }
                        else if (on_off_23 == 1)
                        {
                            var k = Math.Max(Math.Max(klist[0].Value, klist[3].Value),Math.Max(klist2[0].Value, klist2[3].Value));
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(ri);
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = Math.Max(Math.Max(klist[1].Value, klist[4].Value), Math.Max(klist2[1].Value, klist2[4].Value));
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rj);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = Math.Max(Math.Max(klist[2].Value, klist[5].Value), Math.Max(klist2[2].Value, klist2[5].Value));
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
                            var k = Math.Max(k2list[0].Value, k2list2[0].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(ri);
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = Math.Max(k2list[1].Value, k2list2[1].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rj);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = Math.Max(k2list[2].Value, k2list2[2].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rc);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                        }
                        else if (on_off_22 == 1)
                        {
                            var k = Math.Max(k2list[3].Value, k2list2[3].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(ri);
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = Math.Max(k2list[4].Value, k2list2[4].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rj);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = Math.Max(k2list[5].Value, k2list2[5].Value);
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rc);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                        }
                        else if (on_off_23 == 1)
                        {
                            var k = Math.Max(Math.Max(k2list[0].Value, k2list[3].Value), Math.Max(k2list2[0].Value, k2list2[3].Value));
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(ri);
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = Math.Max(Math.Max(k2list[1].Value, k2list[4].Value), Math.Max(k2list2[1].Value, k2list2[4].Value));
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rj);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                            k = Math.Max(Math.Max(k2list[2].Value, k2list[5].Value), Math.Max(k2list2[2].Value, k2list2[5].Value));
                            _text.Add(k.ToString("F").Substring(0, digit));
                            _p.Add(rc);
                            color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _c.Add(color); _size.Add(fontsize);
                        }
                    }
                }
                DA.SetDataTree(0, MaL); DA.SetDataTree(2, MaLpX); DA.SetDataTree(3, MaLpY); DA.SetDataTree(4, MaLmX); DA.SetDataTree(5, MaLmY);
                DA.SetDataTree(1, QaL); DA.SetDataTree(6, QaLpX); DA.SetDataTree(7, QaLpY); DA.SetDataTree(8, QaLmX); DA.SetDataTree(9, QaLmY);
                for (int i = 0; i < index.Count; i++)
                {
                    int e = (int)index[i];
                    kentei.AppendRange(new List<GH_Number> { new GH_Number(e), new GH_Number(kmaxL[i]), new GH_Number(kmaxS[i]) }, new GH_Path(i));
                }
                DA.SetDataTree(10, kentei);
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
                DA.SetDataTree(11, kmax);
                if (on_off == 1)
                {
                    var pdfname = "RcColumnCheck"; DA.GetData("outputname", ref pdfname);
                    // フォントリゾルバーのグローバル登録
                    if (PdfCreate.JapaneseFontResolver.fontset == 0) { PdfSharp.Fonts.GlobalFontSettings.FontResolver = fontresolver; PdfCreate.JapaneseFontResolver.fontset = 1; }
                    // PDFドキュメントを作成。
                    PdfDocument document = new PdfDocument();
                    document.Info.Title = pdfname;
                    document.Info.Author = "Shinnosuke Fujita, Assoc. Prof., The Univ. of Kitakyushu";
                    // フォントを作成。
                    XFont font = new XFont("Gen Shin Gothic", 7.0, XFontStyle.Regular);
                    XFont fontbold = new XFont("Gen Shin Gothic", 7.0, XFontStyle.Bold);
                    var pen = XPens.Black;
                    if (sec_f[0].Count == 18)
                    {
                        var labels = new List<string>
                        {
                            "部材番号","配筋符号","断面算定用bxD[mm]"," ","ft[N/mm2]", "fs[N/mm2]","コンクリートfc[N/mm2]","コンクリートfs[N/mm2]", "節点番号","上端主筋(D方向)", "上端-鉄筋重心距離dt[mm]","下端主筋(D方向)","下端-鉄筋重心距離dt[mm]","上端主筋(b方向)", "上端-鉄筋重心距離dt[mm]","下端主筋(b方向)","下端-鉄筋重心距離dt[mm]","HOOP(D方向)","HOOP(b方向)"," ","上端M[kNm](D方向)","下端M[kNm](D方向)","上端M[kNm](b方向)","下端M[kNm](b方向)","上端Ma[kNm](D方向)","下端Ma[kNm](D方向)","上端Ma[kNm](b方向)","下端Ma[kNm](b方向)","曲げ検定比M/Ma","Q=QL[kN](D方向)","Qa[kN](D方向)", "Q=QL[kN](b方向)","Qa[kN](b方向)","せん断検定比Q/Qa","判定"
                        };
                        var label_width = 100; var offset_x = 25; var offset_y = 25; var pitchy = 10; var text_width = 45; PdfPage page = new PdfPage(); page.Size = PageSize.A4;
                        for (int ind = 0; ind < index.Count; ind++)//
                        {
                            int e = (int)index[ind];
                            var values = new List<List<string>>();
                            values.Add(Ele[ind]); values.Add(Name[ind]); values.Add(Size[ind]); values.Add(new List<string> { "長期", "", "短期" });
                            values.Add(FtiT[ind]);
                            values.Add(Fsi[ind]);
                            values.Add(FC[ind]); values.Add(FS[ind]); values.Add(Nod[ind]);
                            values.Add(BarT[ind]); values.Add(DT[ind]); values.Add(BarB[ind]); values.Add(DB[ind]);
                            values.Add(BarR[ind]); values.Add(DR[ind]); values.Add(BarL[ind]); values.Add(DL[ind]);
                            values.Add(Bars[ind]); values.Add(Bars2[ind]);
                            values.Add(new List<string> { "長期検討" });
                            var MiT = 0.0; var MiB = 0.0; var McT = 0.0; var McB = 0.0; var MjT = 0.0; var MjB = 0.0;
                            var MiR = 0.0; var MiL = 0.0; var McR = 0.0; var McL = 0.0; var MjR = 0.0; var MjL = 0.0;
                            var MiT_text = ""; var MiB_text = ""; var McT_text = ""; var McB_text = ""; var MjT_text = ""; var MjB_text = "";
                            if (MyL[ind][0] < 0) { MiT = Math.Abs(MyL[ind][0]); MiT_text = MiT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiT))); }
                            else { MiB = Math.Abs(MyL[ind][0]); MiB_text = MiB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiB))); }
                            if (MyL[ind][1] < 0) { McT = Math.Abs(MyL[ind][1]); McT_text = McT.ToString("F10").Substring(0, Math.Max(5, Digit((int)McT))); }
                            else { McB = Math.Abs(MyL[ind][1]); McB_text = McB.ToString("F10").Substring(0, Math.Max(5, Digit((int)McB))); }
                            if (MyL[ind][2] > 0) { MjT = Math.Abs(MyL[ind][2]); MjT_text = MjT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjT))); }
                            else { MjB = Math.Abs(MyL[ind][2]); MjB_text = MjB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjB))); }
                            var MiR_text = ""; var MiL_text = ""; var McR_text = ""; var McL_text = ""; var MjR_text = ""; var MjL_text = "";
                            if (MzL[ind][0] < 0) { MiR = Math.Abs(MzL[ind][0]); MiR_text = MiR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiR))); }
                            else { MiL = Math.Abs(MzL[ind][0]); MiL_text = MiL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiL))); }
                            if (MzL[ind][1] < 0) { McR = Math.Abs(MzL[ind][1]); McR_text = McR.ToString("F10").Substring(0, Math.Max(5, Digit((int)McR))); }
                            else { McL = Math.Abs(MzL[ind][1]); McL_text = McL.ToString("F10").Substring(0, Math.Max(5, Digit((int)McL))); }
                            if (MzL[ind][2] > 0) { MjR = Math.Abs(MzL[ind][2]); MjR_text = MjR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjR))); }
                            else { MjL = Math.Abs(MzL[ind][2]); MjL_text = MjL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjL))); }
                            var MaiT = MT_aL[ind][0]; var MacT = MT_aL[ind][1]; var MajT = MT_aL[ind][2];
                            var MaiB = MB_aL[ind][0]; var MacB = MB_aL[ind][1]; var MajB = MB_aL[ind][2];
                            var MaiR = MR_aL[ind][0]; var MacR = MR_aL[ind][1]; var MajR = MR_aL[ind][2];
                            var MaiL = ML_aL[ind][0]; var MacL = ML_aL[ind][1]; var MajL = ML_aL[ind][2];
                            var MaiT_text = MaiT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiT)));
                            var MacT_text = MacT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacT)));
                            var MajT_text = MajT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajT)));
                            var MaiB_text = MaiB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiB)));
                            var MacB_text = MacB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacB)));
                            var MajB_text = MajB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajB)));
                            var MaiR_text = MaiR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiR)));
                            var MacR_text = MacR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacR)));
                            var MajR_text = MajR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajR)));
                            var MaiL_text = MaiL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiL)));
                            var MacL_text = MacL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacL)));
                            var MajL_text = MajL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajL)));
                            var ki = Math.Max(MiT / MaiT, MiB / MaiB) + Math.Max(MiR / MaiR, MiL / MaiL); var kc = Math.Max(McT / MacT, McB / MacB) + Math.Max(McR / MacR, McL / MacL); var kj = Math.Max(MjT / MajT, MjT / MajT) + Math.Max(MjR / MajR, MjL / MajL);
                            var Qi = Math.Abs(QzL[ind][0]); var Qc = Math.Abs(QzL[ind][1]); var Qj = Math.Abs(QzL[ind][2]);
                            var Qi_text = Qi.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qi)));
                            var Qc_text = Qc.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qc)));
                            var Qj_text = Qj.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qj)));
                            var Qai = Q_aL[ind][0]; var Qac = Q_aL[ind][1]; var Qaj = Q_aL[ind][2];
                            var Qai_text = Qai.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qai)));
                            var Qac_text = Qac.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qac)));
                            var Qaj_text = Qaj.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qaj)));
                            var Qi2 = Math.Abs(QyL[ind][0]); var Qc2 = Math.Abs(QyL[ind][1]); var Qj2 = Math.Abs(QyL[ind][2]);
                            var Qi2_text = Qi2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qi2)));
                            var Qc2_text = Qc2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qc2)));
                            var Qj2_text = Qj2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qj2)));
                            var Qai2 = Q_aL2[ind][0]; var Qac2 = Q_aL2[ind][1]; var Qaj2 = Q_aL2[ind][2];
                            var Qai2_text = Qai2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qai2)));
                            var Qac2_text = Qac2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qac2)));
                            var Qaj2_text = Qaj2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qaj2)));
                            var ki2 = Math.Abs(Qi / Qai) + Math.Abs(Qi2 / Qai2); var kc2 = Math.Abs(Qc / Qac) + Math.Abs(Qc2 / Qac2); var kj2 = Math.Abs(Qj / Qaj) + Math.Abs(Qj2 / Qaj2);
                            values.Add(new List<string> { MiT_text, McT_text, MjT_text }); values.Add(new List<string> { MiB_text, McB_text, MjB_text });
                            values.Add(new List<string> { MiR_text, McR_text, MjR_text }); values.Add(new List<string> { MiL_text, McL_text, MjL_text });
                            values.Add(new List<string> { MaiT_text, MacT_text, MajT_text }); values.Add(new List<string> { MaiB_text, MacB_text, MajB_text });
                            values.Add(new List<string> { MaiR_text, MacR_text, MajR_text }); values.Add(new List<string> { MaiL_text, MacL_text, MajL_text });
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            values.Add(new List<string> { Qi_text, Qc_text, Qj_text });
                            values.Add(new List<string> { Qai_text, Qac_text, Qaj_text });
                            values.Add(new List<string> { Qi2_text, Qc2_text, Qj2_text });
                            values.Add(new List<string> { Qai2_text, Qac2_text, Qaj2_text });
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
                                    if (i == 28) { color1 = k_color[0]; color2 = k_color[1]; color3 = k_color[2]; }
                                    if (i == 33) { color1 = k2_color[0]; color2 = k2_color[1]; color3 = k2_color[2]; }
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
                            "部材番号","配筋符号","断面算定用bxD[mm]"," ","ft[N/mm2]", "fs[N/mm2]","コンクリートfc[N/mm2]","コンクリートfs[N/mm2]", "節点番号","上端主筋(D方向)", "上端-鉄筋重心距離dt[mm]","下端主筋(D方向)","下端-鉄筋重心距離dt[mm]","上端主筋(b方向)", "上端-鉄筋重心距離dt[mm]","下端主筋(b方向)","下端-鉄筋重心距離dt[mm]","HOOP(D方向)","HOOP(b方向)"," ","上端M[kNm](D方向)","下端M[kNm](D方向)","上端M[kNm](b方向)","下端M[kNm](b方向)","上端Ma[kNm](D方向)","下端Ma[kNm](D方向)","上端Ma[kNm](b方向)","下端Ma[kNm](b方向)","曲げ検定比M/Ma","Q=QL[kN](D方向)","Qa[kN](D方向)", "Q=QL[kN](b方向)","Qa[kN](b方向)","せん断検定比Q/Qa","判定","","上端M[kNm](D方向)","下端M[kNm](D方向)","上端M[kNm](b方向)","下端M[kNm](b方向)","上端Ma[kNm](D方向)","下端Ma[kNm](D方向)","上端Ma[kNm](b方向)","下端Ma[kNm](b方向)","曲げ検定比M/Ma", "Q=QL+"+Math.Round(N,2).ToString()+"QX[kN](D方向)","Qa[kN](D方向)", "Q=QL+"+Math.Round(N,2).ToString()+"QX[kN](b方向)","Qa[kN](b方向)","せん断検定比Q/Qa","判定","","上端M[kNm](D方向)","下端M[kNm](D方向)","上端M[kNm](b方向)","下端M[kNm](b方向)","上端Ma[kNm](D方向)","下端Ma[kNm](D方向)","上端Ma[kNm](b方向)","下端Ma[kNm](b方向)","曲げ検定比M/Ma", "Q=QL+"+Math.Round(N,2).ToString()+"QY[kN](D方向)","Qa[kN](D方向)", "Q=QL+"+Math.Round(N,2).ToString()+"QY[kN](b方向)","Qa[kN](b方向)","せん断検定比Q/Qa","判定","","上端M[kNm](D方向)","下端M[kNm](D方向)","上端M[kNm](b方向)","下端M[kNm](b方向)","上端Ma[kNm](D方向)","下端Ma[kNm](D方向)","上端Ma[kNm](b方向)","下端Ma[kNm](b方向)","曲げ検定比M/Ma", "Q=QL-"+Math.Round(N,2).ToString()+"QX[kN](D方向)","Qa[kN](D方向)","Q=QL-"+Math.Round(N,2).ToString()+"QX[kN](b方向)","Qa[kN](b方向)","せん断検定比Q/Qa","判定","","上端M[kNm](D方向)","下端M[kNm](D方向)","上端M[kNm](b方向)","下端M[kNm](b方向)","上端Ma[kNm](D方向)","下端Ma[kNm](D方向)","上端Ma[kNm](b方向)","下端Ma[kNm](b方向)","曲げ検定比M/Ma", "Q=QL-"+Math.Round(N,2).ToString()+"QY[kN](D方向)","Qa[kN](D方向)","Q=QL-"+Math.Round(N,2).ToString()+"QY[kN](b方向)","Qa[kN](b方向)","せん断検定比Q/Qa","判定"
                        };
                        var label_width = 100; var offset_x = 25; var offset_y = 25; var pitchy = 8.1; var text_width = 45; PdfPage page = new PdfPage(); page.Size = PageSize.A4;
                        for (int ind = 0; ind < index.Count; ind++)//
                        {
                            int e = (int)index[ind];
                            var values = new List<List<string>>();
                            values.Add(Ele[ind]); values.Add(Name[ind]); values.Add(Size[ind]); values.Add(new List<string> { "長期", "", "短期" });
                            values.Add(FtiT[ind]);
                            values.Add(Fsi[ind]);
                            values.Add(FC[ind]); values.Add(FS[ind]); values.Add(Nod[ind]);
                            values.Add(BarT[ind]); values.Add(DT[ind]); values.Add(BarB[ind]); values.Add(DB[ind]);
                            values.Add(BarR[ind]); values.Add(DR[ind]); values.Add(BarL[ind]); values.Add(DL[ind]);
                            values.Add(Bars[ind]); values.Add(Bars2[ind]);
                            values.Add(new List<string> { "長期検討" });
                            var MiT = 0.0; var MiB = 0.0; var McT = 0.0; var McB = 0.0; var MjT = 0.0; var MjB = 0.0;
                            var MiR = 0.0; var MiL = 0.0; var McR = 0.0; var McL = 0.0; var MjR = 0.0; var MjL = 0.0;
                            var MiT_text = ""; var MiB_text = ""; var McT_text = ""; var McB_text = ""; var MjT_text = ""; var MjB_text = "";
                            if (MyL[ind][0] < 0) { MiT = Math.Abs(MyL[ind][0]); MiT_text = MiT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiT))); }
                            else { MiB = Math.Abs(MyL[ind][0]); MiB_text = MiB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiB))); }
                            if (MyL[ind][1] < 0) { McT = Math.Abs(MyL[ind][1]); McT_text = McT.ToString("F10").Substring(0, Math.Max(5, Digit((int)McT))); }
                            else { McB = Math.Abs(MyL[ind][1]); McB_text = McB.ToString("F10").Substring(0, Math.Max(5, Digit((int)McB))); }
                            if (MyL[ind][2] > 0) { MjT = Math.Abs(MyL[ind][2]); MjT_text = MjT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjT))); }
                            else { MjB = Math.Abs(MyL[ind][2]); MjB_text = MjB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjB))); }
                            var MiR_text = ""; var MiL_text = ""; var McR_text = ""; var McL_text = ""; var MjR_text = ""; var MjL_text = "";
                            if (MzL[ind][0] < 0) { MiR = Math.Abs(MzL[ind][0]); MiR_text = MiR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiR))); }
                            else { MiL = Math.Abs(MzL[ind][0]); MiL_text = MiL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiL))); }
                            if (MzL[ind][1] < 0) { McR = Math.Abs(MzL[ind][1]); McR_text = McR.ToString("F10").Substring(0, Math.Max(5, Digit((int)McR))); }
                            else { McL = Math.Abs(MzL[ind][1]); McL_text = McL.ToString("F10").Substring(0, Math.Max(5, Digit((int)McL))); }
                            if (MzL[ind][2] > 0) { MjR = Math.Abs(MzL[ind][2]); MjR_text = MjR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjR))); }
                            else { MjL = Math.Abs(MzL[ind][2]); MjL_text = MjL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjL))); }
                            var MaiT = MT_aL[ind][0]; var MacT = MT_aL[ind][1]; var MajT = MT_aL[ind][2];
                            var MaiB = MB_aL[ind][0]; var MacB = MB_aL[ind][1]; var MajB = MB_aL[ind][2];
                            var MaiR = MR_aL[ind][0]; var MacR = MR_aL[ind][1]; var MajR = MR_aL[ind][2];
                            var MaiL = ML_aL[ind][0]; var MacL = ML_aL[ind][1]; var MajL = ML_aL[ind][2];
                            var MaiT_text = MaiT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiT)));
                            var MacT_text = MacT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacT)));
                            var MajT_text = MajT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajT)));
                            var MaiB_text = MaiB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiB)));
                            var MacB_text = MacB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacB)));
                            var MajB_text = MajB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajB)));
                            var MaiR_text = MaiR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiR)));
                            var MacR_text = MacR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacR)));
                            var MajR_text = MajR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajR)));
                            var MaiL_text = MaiL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiL)));
                            var MacL_text = MacL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacL)));
                            var MajL_text = MajL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajL)));
                            var ki = Math.Max(MiT / MaiT, MiB / MaiB)+ Math.Max(MiR / MaiR, MiL / MaiL); var kc = Math.Max(McT / MacT, McB / MacB)+ Math.Max(McR / MacR, McL / MacL); var kj = Math.Max(MjT / MajT, MjT / MajT)+ Math.Max(MjR / MajR, MjL / MajL);
                            var Qi = Math.Abs(QzL[ind][0]); var Qc = Math.Abs(QzL[ind][1]); var Qj = Math.Abs(QzL[ind][2]);
                            var Qi_text = Qi.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qi)));
                            var Qc_text = Qc.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qc)));
                            var Qj_text = Qj.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qj)));
                            var Qai = Q_aL[ind][0]; var Qac = Q_aL[ind][1]; var Qaj = Q_aL[ind][2];
                            var Qai_text = Qai.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qai)));
                            var Qac_text = Qac.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qac)));
                            var Qaj_text = Qaj.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qaj)));
                            var Qi2 = Math.Abs(QyL[ind][0]); var Qc2 = Math.Abs(QyL[ind][1]); var Qj2 = Math.Abs(QyL[ind][2]);
                            var Qi2_text = Qi2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qi2)));
                            var Qc2_text = Qc2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qc2)));
                            var Qj2_text = Qj2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qj2)));
                            var Qai2 = Q_aL2[ind][0]; var Qac2 = Q_aL2[ind][1]; var Qaj2 = Q_aL2[ind][2];
                            var Qai2_text = Qai2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qai2)));
                            var Qac2_text = Qac2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qac2)));
                            var Qaj2_text = Qaj2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qaj2)));
                            var ki2 = Math.Abs(Qi / Qai)+ Math.Abs(Qi2 / Qai2); var kc2 = Math.Abs(Qc / Qac)+ Math.Abs(Qc2 / Qac2); var kj2 = Math.Abs(Qj / Qaj)+ Math.Abs(Qj2 / Qaj2);
                            values.Add(new List<string> { MiT_text, McT_text, MjT_text }); values.Add(new List<string> { MiB_text, McB_text, MjB_text });
                            values.Add(new List<string> { MiR_text, McR_text, MjR_text }); values.Add(new List<string> { MiL_text, McL_text, MjL_text });
                            values.Add(new List<string> { MaiT_text, MacT_text, MajT_text }); values.Add(new List<string> { MaiB_text, MacB_text, MajB_text });
                            values.Add(new List<string> { MaiR_text, MacR_text, MajR_text }); values.Add(new List<string> { MaiL_text, MacL_text, MajL_text });
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            values.Add(new List<string> { Qi_text, Qc_text, Qj_text });
                            values.Add(new List<string> { Qai_text, Qac_text, Qaj_text });
                            values.Add(new List<string> { Qi2_text, Qc2_text, Qj2_text });
                            values.Add(new List<string> { Qai2_text, Qac2_text, Qaj2_text });
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
                            MiT = 0.0; MiB = 0.0; McT = 0.0; McB = 0.0; MjT = 0.0; MjB = 0.0;
                            MiR = 0.0; MiL = 0.0; McR = 0.0; McL = 0.0; MjR = 0.0; MjL = 0.0;
                            MiT_text = ""; MiB_text = ""; McT_text = ""; McB_text = ""; MjT_text = ""; MjB_text = "";
                            if ((MyLpX[ind][0]) < 0) { MiT = Math.Abs(MyLpX[ind][0]); MiT_text = MiT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiT))); }
                            else { MiB = Math.Abs(MyLpX[ind][0]); MiB_text = MiB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiB))); }
                            if (MyLpX[ind][1] < 0) { McT = Math.Abs(MyLpX[ind][1]); McT_text = McT.ToString("F10").Substring(0, Math.Max(5, Digit((int)McT))); }
                            else { McB = Math.Abs(MyLpX[ind][1]); McB_text = McB.ToString("F10").Substring(0, Math.Max(5, Digit((int)McB))); }
                            if (MyLpX[ind][2] > 0) { MjT = Math.Abs(MyLpX[ind][2]); MjT_text = MjT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjT))); }
                            else { MjB = Math.Abs(MyLpX[ind][2]); MjB_text = MjB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjB))); }
                            MiR_text = ""; MiL_text = ""; McR_text = ""; McL_text = ""; MjR_text = ""; MjL_text = "";
                            if ((MzLpX[ind][0]) < 0) { MiR = Math.Abs(MzLpX[ind][0]); MiR_text = MiR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiR))); }
                            else { MiL = Math.Abs(MzLpX[ind][0]); MiL_text = MiL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiL))); }
                            if (MzLpX[ind][1] < 0) { McR = Math.Abs(MzLpX[ind][1]); McR_text = McR.ToString("F10").Substring(0, Math.Max(5, Digit((int)McR))); }
                            else { McL = Math.Abs(MzLpX[ind][1]); McL_text = McL.ToString("F10").Substring(0, Math.Max(5, Digit((int)McL))); }
                            if (MzLpX[ind][2] > 0) { MjR = Math.Abs(MzLpX[ind][2]); MjR_text = MjR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjR))); }
                            else { MjL = Math.Abs(MzLpX[ind][2]); MjL_text = MjL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjL))); }
                            MaiT = MT_aLpX[ind][0]; MacT = MT_aLpX[ind][1]; MajT = MT_aLpX[ind][2];
                            MaiB = MB_aLpX[ind][0]; MacB = MB_aLpX[ind][1]; MajB = MB_aLpX[ind][2];
                            MaiR = MR_aLpX[ind][0]; MacR = MR_aLpX[ind][1]; MajR = MR_aLpX[ind][2];
                            MaiL = ML_aLpX[ind][0]; MacL = ML_aLpX[ind][1]; MajL = ML_aLpX[ind][2];
                            MaiT_text = MaiT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiT)));
                            MacT_text = MacT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacT)));
                            MajT_text = MajT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajT)));
                            MaiB_text = MaiB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiB)));
                            MacB_text = MacB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacB)));
                            MajB_text = MajB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajB)));
                            MaiR_text = MaiR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiR)));
                            MacR_text = MacR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacR)));
                            MajR_text = MajR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajR)));
                            MaiL_text = MaiL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiL)));
                            MacL_text = MacL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacL)));
                            MajL_text = MajL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajL)));
                            ki = Math.Max(MiT / MaiT, MiB / MaiB)+ Math.Max(MiR / MaiR, MiL / MaiL); kc = Math.Max(McT / MacT, McB / MacB)+ Math.Max(McR / MacR, McL / MacL); kj = Math.Max(MjT / MajT, MjB / MajB)+ Math.Max(MjR / MajR, MjL / MajL);
                            Qi = Math.Abs(QzLpX[ind][0]); Qc = Math.Abs(QzLpX[ind][1]); Qj = Math.Abs(QzLpX[ind][2]);
                            Qi_text = Qi.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qi)));
                            Qc_text = Qc.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qc)));
                            Qj_text = Qj.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qj)));
                            Qai = Q_aLpX[ind][0]; Qac = Q_aLpX[ind][1]; Qaj = Q_aLpX[ind][2];
                            Qai_text = Qai.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qai)));
                            Qac_text = Qac.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qac)));
                            Qaj_text = Qaj.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qaj)));
                            Qi2 = Math.Abs(QyLpX[ind][0]); Qc2 = Math.Abs(QyLpX[ind][1]); Qj2 = Math.Abs(QyLpX[ind][2]);
                            Qi2_text = Qi2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qi2)));
                            Qc2_text = Qc2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qc2)));
                            Qj2_text = Qj2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qj2)));
                            Qai2 = Q_aLpX2[ind][0]; Qac2 = Q_aLpX2[ind][1]; Qaj2 = Q_aLpX2[ind][2];
                            Qai2_text = Qai2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qai2)));
                            Qac2_text = Qac2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qac2)));
                            Qaj2_text = Qaj2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qaj2)));
                            ki2 = Math.Abs(Qi / Qai) + Math.Abs(Qi2 / Qai2); kc2 = Math.Abs(Qc / Qac) + Math.Abs(Qc2 / Qac2); kj2 = Math.Abs(Qj / Qaj) + Math.Abs(Qj2 / Qaj2);
                            values.Add(new List<string> { MiT_text, McT_text, MjT_text }); values.Add(new List<string> { MiB_text, McB_text, MjB_text });
                            values.Add(new List<string> { MiR_text, McR_text, MjR_text }); values.Add(new List<string> { MiL_text, McL_text, MjL_text });
                            values.Add(new List<string> { MaiT_text, MacT_text, MajT_text }); values.Add(new List<string> { MaiB_text, MacB_text, MajB_text });
                            values.Add(new List<string> { MaiR_text, MacR_text, MajR_text }); values.Add(new List<string> { MaiL_text, MacL_text, MajL_text });
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            values.Add(new List<string> { Qi_text, Qc_text, Qj_text });
                            values.Add(new List<string> { Qai_text, Qac_text, Qaj_text });
                            values.Add(new List<string> { Qi2_text, Qc2_text, Qj2_text });
                            values.Add(new List<string> { Qai2_text, Qac2_text, Qaj2_text });
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
                            MiT = 0.0; MiB = 0.0; McT = 0.0; McB = 0.0; MjT = 0.0; MjB = 0.0;
                            MiR = 0.0; MiL = 0.0; McR = 0.0; McL = 0.0; MjR = 0.0; MjL = 0.0;
                            MiT_text = ""; MiB_text = ""; McT_text = ""; McB_text = ""; MjT_text = ""; MjB_text = "";
                            if ((MyLpY[ind][0]) < 0) { MiT = Math.Abs(MyLpY[ind][0]); MiT_text = MiT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiT))); }
                            else { MiB = Math.Abs(MyLpY[ind][0]); MiB_text = MiB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiB))); }
                            if (MyLpY[ind][1] < 0) { McT = Math.Abs(MyLpY[ind][1]); McT_text = McT.ToString("F10").Substring(0, Math.Max(5, Digit((int)McT))); }
                            else { McB = Math.Abs(MyLpY[ind][1]); McB_text = McB.ToString("F10").Substring(0, Math.Max(5, Digit((int)McB))); }
                            if (MyLpY[ind][2] > 0) { MjT = Math.Abs(MyLpY[ind][2]); MjT_text = MjT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjT))); }
                            else { MjB = Math.Abs(MyLpY[ind][2]); MjB_text = MjB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjB))); }
                            MiR_text = ""; MiL_text = ""; McR_text = ""; McL_text = ""; MjR_text = ""; MjL_text = "";
                            if ((MzLpY[ind][0]) < 0) { MiR = Math.Abs(MzLpY[ind][0]); MiR_text = MiR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiR))); }
                            else { MiL = Math.Abs(MzLpY[ind][0]); MiL_text = MiL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiL))); }
                            if (MzLpY[ind][1] < 0) { McR = Math.Abs(MzLpY[ind][1]); McR_text = McR.ToString("F10").Substring(0, Math.Max(5, Digit((int)McR))); }
                            else { McL = Math.Abs(MzLpY[ind][1]); McL_text = McL.ToString("F10").Substring(0, Math.Max(5, Digit((int)McL))); }
                            if (MzLpY[ind][2] > 0) { MjR = Math.Abs(MzLpY[ind][2]); MjR_text = MjR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjR))); }
                            else { MjL = Math.Abs(MzLpY[ind][2]); MjL_text = MjL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjL))); }
                            MaiT = MT_aLpY[ind][0]; MacT = MT_aLpY[ind][1]; MajT = MT_aLpY[ind][2];
                            MaiB = MB_aLpY[ind][0]; MacB = MB_aLpY[ind][1]; MajB = MB_aLpY[ind][2];
                            MaiR = MR_aLpY[ind][0]; MacR = MR_aLpY[ind][1]; MajR = MR_aLpY[ind][2];
                            MaiL = ML_aLpY[ind][0]; MacL = ML_aLpY[ind][1]; MajL = ML_aLpY[ind][2];
                            MaiT_text = MaiT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiT)));
                            MacT_text = MacT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacT)));
                            MajT_text = MajT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajT)));
                            MaiB_text = MaiB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiB)));
                            MacB_text = MacB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacB)));
                            MajB_text = MajB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajB)));
                            MaiR_text = MaiR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiR)));
                            MacR_text = MacR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacR)));
                            MajR_text = MajR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajR)));
                            MaiL_text = MaiL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiL)));
                            MacL_text = MacL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacL)));
                            MajL_text = MajL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajL)));
                            ki = Math.Max(MiT / MaiT, MiB / MaiB) + Math.Max(MiR / MaiR, MiL / MaiL); kc = Math.Max(McT / MacT, McB / MacB) + Math.Max(McR / MacR, McL / MacL); kj = Math.Max(MjT / MajT, MjB / MajB) + Math.Max(MjR / MajR, MjL / MajL);
                            Qi = Math.Abs(QzLpY[ind][0]); Qc = Math.Abs(QzLpY[ind][1]); Qj = Math.Abs(QzLpY[ind][2]);
                            Qi_text = Qi.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qi)));
                            Qc_text = Qc.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qc)));
                            Qj_text = Qj.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qj)));
                            Qai = Q_aLpY[ind][0]; Qac = Q_aLpY[ind][1]; Qaj = Q_aLpY[ind][2];
                            Qai_text = Qai.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qai)));
                            Qac_text = Qac.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qac)));
                            Qaj_text = Qaj.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qaj)));
                            Qi2 = Math.Abs(QyLpY[ind][0]); Qc2 = Math.Abs(QyLpY[ind][1]); Qj2 = Math.Abs(QyLpY[ind][2]);
                            Qi2_text = Qi2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qi2)));
                            Qc2_text = Qc2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qc2)));
                            Qj2_text = Qj2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qj2)));
                            Qai2 = Q_aLpY2[ind][0]; Qac2 = Q_aLpY2[ind][1]; Qaj2 = Q_aLpY2[ind][2];
                            Qai2_text = Qai2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qai2)));
                            Qac2_text = Qac2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qac2)));
                            Qaj2_text = Qaj2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qaj2)));
                            ki2 = Math.Abs(Qi / Qai) + Math.Abs(Qi2 / Qai2); kc2 = Math.Abs(Qc / Qac) + Math.Abs(Qc2 / Qac2); kj2 = Math.Abs(Qj / Qaj) + Math.Abs(Qj2 / Qaj2);
                            values.Add(new List<string> { MiT_text, McT_text, MjT_text }); values.Add(new List<string> { MiB_text, McB_text, MjB_text });
                            values.Add(new List<string> { MiR_text, McR_text, MjR_text }); values.Add(new List<string> { MiL_text, McL_text, MjL_text });
                            values.Add(new List<string> { MaiT_text, MacT_text, MajT_text }); values.Add(new List<string> { MaiB_text, MacB_text, MajB_text });
                            values.Add(new List<string> { MaiR_text, MacR_text, MajR_text }); values.Add(new List<string> { MaiL_text, MacL_text, MajL_text });
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            values.Add(new List<string> { Qi_text, Qc_text, Qj_text });
                            values.Add(new List<string> { Qai_text, Qac_text, Qaj_text });
                            values.Add(new List<string> { Qi2_text, Qc2_text, Qj2_text });
                            values.Add(new List<string> { Qai2_text, Qac2_text, Qaj2_text });
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
                            MiT = 0.0; MiB = 0.0; McT = 0.0; McB = 0.0; MjT = 0.0; MjB = 0.0;
                            MiR = 0.0; MiL = 0.0; McR = 0.0; McL = 0.0; MjR = 0.0; MjL = 0.0;
                            MiT_text = ""; MiB_text = ""; McT_text = ""; McB_text = ""; MjT_text = ""; MjB_text = "";
                            if ((MyLmX[ind][0]) < 0) { MiT = Math.Abs(MyLmX[ind][0]); MiT_text = MiT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiT))); }
                            else { MiB = Math.Abs(MyLmX[ind][0]); MiB_text = MiB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiB))); }
                            if (MyLmX[ind][1] < 0) { McT = Math.Abs(MyLmX[ind][1]); McT_text = McT.ToString("F10").Substring(0, Math.Max(5, Digit((int)McT))); }
                            else { McB = Math.Abs(MyLmX[ind][1]); McB_text = McB.ToString("F10").Substring(0, Math.Max(5, Digit((int)McB))); }
                            if (MyLmX[ind][2] > 0) { MjT = Math.Abs(MyLmX[ind][2]); MjT_text = MjT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjT))); }
                            else { MjB = Math.Abs(MyLmX[ind][2]); MjB_text = MjB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjB))); }
                            MiR_text = ""; MiL_text = ""; McR_text = ""; McL_text = ""; MjR_text = ""; MjL_text = "";
                            if ((MzLmX[ind][0]) < 0) { MiR = Math.Abs(MzLmX[ind][0]); MiR_text = MiR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiR))); }
                            else { MiL = Math.Abs(MzLmX[ind][0]); MiL_text = MiL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiL))); }
                            if (MzLmX[ind][1] < 0) { McR = Math.Abs(MzLmX[ind][1]); McR_text = McR.ToString("F10").Substring(0, Math.Max(5, Digit((int)McR))); }
                            else { McL = Math.Abs(MzLmX[ind][1]); McL_text = McL.ToString("F10").Substring(0, Math.Max(5, Digit((int)McL))); }
                            if (MzLmX[ind][2] > 0) { MjR = Math.Abs(MzLmX[ind][2]); MjR_text = MjR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjR))); }
                            else { MjL = Math.Abs(MzLmX[ind][2]); MjL_text = MjL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjL))); }
                            MaiT = MT_aLmX[ind][0]; MacT = MT_aLmX[ind][1]; MajT = MT_aLmX[ind][2];
                            MaiB = MB_aLmX[ind][0]; MacB = MB_aLmX[ind][1]; MajB = MB_aLmX[ind][2];
                            MaiR = MR_aLmX[ind][0]; MacR = MR_aLmX[ind][1]; MajR = MR_aLmX[ind][2];
                            MaiL = ML_aLmX[ind][0]; MacL = ML_aLmX[ind][1]; MajL = ML_aLmX[ind][2];
                            MaiT_text = MaiT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiT)));
                            MacT_text = MacT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacT)));
                            MajT_text = MajT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajT)));
                            MaiB_text = MaiB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiB)));
                            MacB_text = MacB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacB)));
                            MajB_text = MajB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajB)));
                            MaiR_text = MaiR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiR)));
                            MacR_text = MacR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacR)));
                            MajR_text = MajR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajR)));
                            MaiL_text = MaiL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiL)));
                            MacL_text = MacL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacL)));
                            MajL_text = MajL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajL)));
                            ki = Math.Max(MiT / MaiT, MiB / MaiB) + Math.Max(MiR / MaiR, MiL / MaiL); kc = Math.Max(McT / MacT, McB / MacB) + Math.Max(McR / MacR, McL / MacL); kj = Math.Max(MjT / MajT, MjB / MajB) + Math.Max(MjR / MajR, MjL / MajL);
                            Qi = Math.Abs(QzLmX[ind][0]); Qc = Math.Abs(QzLmX[ind][1]); Qj = Math.Abs(QzLmX[ind][2]);
                            Qi_text = Qi.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qi)));
                            Qc_text = Qc.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qc)));
                            Qj_text = Qj.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qj)));
                            Qai = Q_aLmX[ind][0]; Qac = Q_aLmX[ind][1]; Qaj = Q_aLmX[ind][2];
                            Qai_text = Qai.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qai)));
                            Qac_text = Qac.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qac)));
                            Qaj_text = Qaj.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qaj)));
                            Qi2 = Math.Abs(QyLmX[ind][0]); Qc2 = Math.Abs(QyLmX[ind][1]); Qj2 = Math.Abs(QyLmX[ind][2]);
                            Qi2_text = Qi2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qi2)));
                            Qc2_text = Qc2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qc2)));
                            Qj2_text = Qj2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qj2)));
                            Qai2 = Q_aLmX2[ind][0]; Qac2 = Q_aLmX2[ind][1]; Qaj2 = Q_aLmX2[ind][2];
                            Qai2_text = Qai2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qai2)));
                            Qac2_text = Qac2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qac2)));
                            Qaj2_text = Qaj2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qaj2)));
                            ki2 = Math.Abs(Qi / Qai) + Math.Abs(Qi2 / Qai2); kc2 = Math.Abs(Qc / Qac) + Math.Abs(Qc2 / Qac2); kj2 = Math.Abs(Qj / Qaj) + Math.Abs(Qj2 / Qaj2);
                            values.Add(new List<string> { MiT_text, McT_text, MjT_text }); values.Add(new List<string> { MiB_text, McB_text, MjB_text });
                            values.Add(new List<string> { MiR_text, McR_text, MjR_text }); values.Add(new List<string> { MiL_text, McL_text, MjL_text });
                            values.Add(new List<string> { MaiT_text, MacT_text, MajT_text }); values.Add(new List<string> { MaiB_text, MacB_text, MajB_text });
                            values.Add(new List<string> { MaiR_text, MacR_text, MajR_text }); values.Add(new List<string> { MaiL_text, MacL_text, MajL_text });
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            values.Add(new List<string> { Qi_text, Qc_text, Qj_text });
                            values.Add(new List<string> { Qai_text, Qac_text, Qaj_text });
                            values.Add(new List<string> { Qi2_text, Qc2_text, Qj2_text });
                            values.Add(new List<string> { Qai2_text, Qac2_text, Qaj2_text });
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
                            MiT = 0.0; MiB = 0.0; McT = 0.0; McB = 0.0; MjT = 0.0; MjB = 0.0;
                            MiR = 0.0; MiL = 0.0; McR = 0.0; McL = 0.0; MjR = 0.0; MjL = 0.0;
                            MiT_text = ""; MiB_text = ""; McT_text = ""; McB_text = ""; MjT_text = ""; MjB_text = "";
                            if ((MyLmY[ind][0]) < 0) { MiT = Math.Abs(MyLmY[ind][0]); MiT_text = MiT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiT))); }
                            else { MiB = Math.Abs(MyLmY[ind][0]); MiB_text = MiB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiB))); }
                            if (MyLmY[ind][1] < 0) { McT = Math.Abs(MyLmY[ind][1]); McT_text = McT.ToString("F10").Substring(0, Math.Max(5, Digit((int)McT))); }
                            else { McB = Math.Abs(MyLmY[ind][1]); McB_text = McB.ToString("F10").Substring(0, Math.Max(5, Digit((int)McB))); }
                            if (MyLmY[ind][2] > 0) { MjT = Math.Abs(MyLmY[ind][2]); MjT_text = MjT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjT))); }
                            else { MjB = Math.Abs(MyLmY[ind][2]); MjB_text = MjB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjB))); }
                            MiR_text = ""; MiL_text = ""; McR_text = ""; McL_text = ""; MjR_text = ""; MjL_text = "";
                            if ((MzLmY[ind][0]) < 0) { MiR = Math.Abs(MzLmY[ind][0]); MiR_text = MiR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiR))); }
                            else { MiL = Math.Abs(MzLmY[ind][0]); MiL_text = MiL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MiL))); }
                            if (MzLmY[ind][1] < 0) { McR = Math.Abs(MzLmY[ind][1]); McR_text = McR.ToString("F10").Substring(0, Math.Max(5, Digit((int)McR))); }
                            else { McL = Math.Abs(MzLmY[ind][1]); McL_text = McL.ToString("F10").Substring(0, Math.Max(5, Digit((int)McL))); }
                            if (MzLmY[ind][2] > 0) { MjR = Math.Abs(MzLmY[ind][2]); MjR_text = MjR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjR))); }
                            else { MjL = Math.Abs(MzLmY[ind][2]); MjL_text = MjL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MjL))); }
                            MaiT = MT_aLmY[ind][0]; MacT = MT_aLmY[ind][1]; MajT = MT_aLmY[ind][2];
                            MaiB = MB_aLmY[ind][0]; MacB = MB_aLmY[ind][1]; MajB = MB_aLmY[ind][2];
                            MaiR = MR_aLmY[ind][0]; MacR = MR_aLmY[ind][1]; MajR = MR_aLmY[ind][2];
                            MaiL = ML_aLmY[ind][0]; MacL = ML_aLmY[ind][1]; MajL = ML_aLmY[ind][2];
                            MaiT_text = MaiT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiT)));
                            MacT_text = MacT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacT)));
                            MajT_text = MajT.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajT)));
                            MaiB_text = MaiB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiB)));
                            MacB_text = MacB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacB)));
                            MajB_text = MajB.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajB)));
                            MaiR_text = MaiR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiR)));
                            MacR_text = MacR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacR)));
                            MajR_text = MajR.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajR)));
                            MaiL_text = MaiL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MaiL)));
                            MacL_text = MacL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MacL)));
                            MajL_text = MajL.ToString("F10").Substring(0, Math.Max(5, Digit((int)MajL)));
                            ki = Math.Max(MiT / MaiT, MiB / MaiB) + Math.Max(MiR / MaiR, MiL / MaiL); kc = Math.Max(McT / MacT, McB / MacB) + Math.Max(McR / MacR, McL / MacL); kj = Math.Max(MjT / MajT, MjB / MajB) + Math.Max(MjR / MajR, MjL / MajL);
                            Qi = Math.Abs(QzLmY[ind][0]); Qc = Math.Abs(QzLmY[ind][1]); Qj = Math.Abs(QzLmY[ind][2]);
                            Qi_text = Qi.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qi)));
                            Qc_text = Qc.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qc)));
                            Qj_text = Qj.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qj)));
                            Qai = Q_aLmY[ind][0]; Qac = Q_aLmY[ind][1]; Qaj = Q_aLmY[ind][2];
                            Qai_text = Qai.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qai)));
                            Qac_text = Qac.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qac)));
                            Qaj_text = Qaj.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qaj)));
                            Qi2 = Math.Abs(QyLmY[ind][0]); Qc2 = Math.Abs(QyLmY[ind][1]); Qj2 = Math.Abs(QyLmY[ind][2]);
                            Qi2_text = Qi2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qi2)));
                            Qc2_text = Qc2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qc2)));
                            Qj2_text = Qj2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qj2)));
                            Qai2 = Q_aLmY2[ind][0]; Qac2 = Q_aLmY2[ind][1]; Qaj2 = Q_aLmY2[ind][2];
                            Qai2_text = Qai2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qai2)));
                            Qac2_text = Qac2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qac2)));
                            Qaj2_text = Qaj2.ToString("F10").Substring(0, Math.Max(5, Digit((int)Qaj2)));
                            ki2 = Math.Abs(Qi / Qai) + Math.Abs(Qi2 / Qai2); kc2 = Math.Abs(Qc / Qac) + Math.Abs(Qc2 / Qac2); kj2 = Math.Abs(Qj / Qaj) + Math.Abs(Qj2 / Qaj2);
                            values.Add(new List<string> { MiT_text, McT_text, MjT_text }); values.Add(new List<string> { MiB_text, McB_text, MjB_text });
                            values.Add(new List<string> { MiR_text, McR_text, MjR_text }); values.Add(new List<string> { MiL_text, McL_text, MjL_text });
                            values.Add(new List<string> { MaiT_text, MacT_text, MajT_text }); values.Add(new List<string> { MaiB_text, MacB_text, MajB_text });
                            values.Add(new List<string> { MaiR_text, MacR_text, MajR_text }); values.Add(new List<string> { MaiL_text, MacL_text, MajL_text });
                            values.Add(new List<string> { ki.ToString("F10").Substring(0, 4), kc.ToString("F10").Substring(0, 4), kj.ToString("F10").Substring(0, 4) });
                            values.Add(new List<string> { Qi_text, Qc_text, Qj_text });
                            values.Add(new List<string> { Qai_text, Qac_text, Qaj_text });
                            values.Add(new List<string> { Qi2_text, Qc2_text, Qj2_text });
                            values.Add(new List<string> { Qai2_text, Qac2_text, Qaj2_text });
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
                                    if (i == 28) { color1 = k_color[0]; color2 = k_color[1]; color3 = k_color[2]; }
                                    if (i == 33) { color1 = k2_color[0]; color2 = k2_color[1]; color3 = k2_color[2]; }
                                    if (i == 44) { color1 = k_color[3]; color2 = k_color[4]; color3 = k_color[5]; }
                                    if (i == 49) { color1 = k2_color[3]; color2 = k2_color[4]; color3 = k2_color[5]; }
                                    if (i == 60) { color1 = k_color[6]; color2 = k_color[7]; color3 = k_color[8]; }
                                    if (i == 65) { color1 = k2_color[6]; color2 = k2_color[7]; color3 = k2_color[8]; }
                                    if (i == 76) { color1 = k_color[9]; color2 = k_color[10]; color3 = k_color[11]; }
                                    if (i == 81) { color1 = k2_color[9]; color2 = k2_color[10]; color3 = k2_color[11]; }
                                    if (i == 92) { color1 = k_color[12]; color2 = k_color[13]; color3 = k_color[14]; }
                                    if (i == 97) { color1 = k2_color[12]; color2 = k2_color[13]; color3 = k2_color[14]; }
                                    gfx.DrawString(values[i][0], f, color1, new XRect(offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, text_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                                    gfx.DrawString(values[i][1], f, color2, new XRect(offset_x + label_width + text_width * 3 * j + text_width, offset_y + pitchy * i, text_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                                    gfx.DrawString(values[i][2], f, color3, new XRect(offset_x + label_width + text_width * 3 * j + text_width * 2, offset_y + pitchy * i, text_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                                }
                                if (i == values.Count - 1)
                                {
                                    i += 1;
                                    gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i, offset_x + label_width + text_width * 3 * (j + 1), offset_y + pitchy * i);//横線
                                }
                                var bb = text_width * 0.5 / Math.Max(Blist[ind], Dlist[ind]) * Blist[ind];
                                var dd = text_width * 0.5 / Math.Max(Blist[ind], Dlist[ind]) * Dlist[ind];
                                var box = Math.Max(bb, dd);
                                var setX = offset_x + label_width + text_width * 1.25 + text_width * 3 * j + (box - bb) / 2.0;
                                var setY = offset_y + pitchy * 4 + 1 + (box - dd) / 2.0; ;
                                var setx = setX + bb * 0.1; var sety = setY + dd * 0.1;
                                var circ = box * 0.05;
                                //////////////////////////////////////////////////////////////////////中央
                                gfx.DrawRectangle(XBrushes.LightGray, setX, setY, bb, dd);
                                for (int ii = 0; ii < HDlist[ind][1]; ii++) { gfx.DrawLine(new XPen(XColors.Black, 0.25), setx + bb * 0.8 / (HDlist[ind][1] - 1) * ii, sety, setx + bb * 0.8 / (HDlist[ind][1] - 1) * ii, sety + dd * 0.8); }//HOOP D方向
                                for (int ii = 0; ii < HBlist[ind][1]; ii++) { gfx.DrawLine(new XPen(XColors.Black, 0.25), setx, sety + dd * 0.8 / (HBlist[ind][1] - 1) * ii, setx + bb * 0.8, sety + dd * 0.8 / (HBlist[ind][1] - 1) * ii); }//HOOP B方向
                                for (int ii = 0; ii < DTlist[ind][1]; ii++) { gfx.DrawEllipse(XBrushes.Red, setx + (bb * 0.8 - circ) / (DTlist[ind][1] - 1) * ii, sety, circ, circ); }//D方向上端主筋
                                for (int ii = 0; ii < DBlist[ind][1]; ii++) { gfx.DrawEllipse(XBrushes.Red, setx + (bb * 0.8 - circ) / (DTlist[ind][1] - 1) * ii, sety + (dd * 0.8 - circ), circ, circ); }//D方向下端主筋
                                for (int ii = 0; ii < DRlist[ind][1]; ii++) { gfx.DrawEllipse(XBrushes.Red, setx, sety + (dd * 0.8 - circ) / (DRlist[ind][1] - 1) * ii, circ, circ); }//B方向上端主筋
                                for (int ii = 0; ii < DLlist[ind][1]; ii++) { gfx.DrawEllipse(XBrushes.Red, setx + (bb * 0.8 - circ), sety + (dd * 0.8 - circ) / (DLlist[ind][1] - 1) * ii, circ, circ); }//B方向下端主筋
                                //////////////////////////////////////////////////////////////////////i端
                                setX = setX - box * 1.1; setx = setx - box * 1.1;
                                gfx.DrawRectangle(XBrushes.LightGray, setX, setY, bb, dd);
                                for (int ii = 0; ii < HDlist[ind][0]; ii++) { gfx.DrawLine(new XPen(XColors.Black, 0.25), setx + bb * 0.8 / (HDlist[ind][0] - 1) * ii, sety, setx + bb * 0.8 / (HDlist[ind][0] - 1) * ii, sety + dd * 0.8); }//HOOP D方向
                                for (int ii = 0; ii < HBlist[ind][0]; ii++) { gfx.DrawLine(new XPen(XColors.Black, 0.25), setx, sety + dd * 0.8 / (HBlist[ind][0] - 1) * ii, setx + bb * 0.8, sety + dd * 0.8 / (HBlist[ind][0] - 1) * ii); }//HOOP B方向
                                for (int ii = 0; ii < DTlist[ind][0]; ii++) { gfx.DrawEllipse(XBrushes.Red, setx + (bb * 0.8 - circ) / (DTlist[ind][0] - 1) * ii, sety, circ, circ); }//D方向上端主筋
                                for (int ii = 0; ii < DBlist[ind][0]; ii++) { gfx.DrawEllipse(XBrushes.Red, setx + (bb * 0.8 - circ) / (DTlist[ind][0] - 1) * ii, sety + (dd * 0.8 - circ), circ, circ); }//D方向下端主筋
                                for (int ii = 0; ii < DRlist[ind][0]; ii++) { gfx.DrawEllipse(XBrushes.Red, setx, sety + (dd * 0.8 - circ) / (DRlist[ind][0] - 1) * ii, circ, circ); }//B方向上端主筋
                                for (int ii = 0; ii < DLlist[ind][0]; ii++) { gfx.DrawEllipse(XBrushes.Red, setx + (bb * 0.8 - circ), sety + (dd * 0.8 - circ) / (DLlist[ind][0] - 1) * ii, circ, circ); }//B方向下端主筋
                                //////////////////////////////////////////////////////////////////////j端
                                setX = setX + box * 1.1 * 2; setx = setx + box * 1.1 * 2;
                                gfx.DrawRectangle(XBrushes.LightGray, setX, setY, bb, dd);
                                for (int ii = 0; ii < HDlist[ind][2]; ii++) { gfx.DrawLine(new XPen(XColors.Black, 0.25), setx + bb * 0.8 / (HDlist[ind][2] - 1) * ii, sety, setx + bb * 0.8 / (HDlist[ind][2] - 1) * ii, sety + dd * 0.8); }//HOOP D方向
                                for (int ii = 0; ii < HBlist[ind][2]; ii++) { gfx.DrawLine(new XPen(XColors.Black, 0.25), setx, sety + dd * 0.8 / (HBlist[ind][2] - 1) * ii, setx + bb * 0.8, sety + dd * 0.8 / (HBlist[ind][2] - 1) * ii); }//HOOP B方向
                                for (int ii = 0; ii < DTlist[ind][2]; ii++) { gfx.DrawEllipse(XBrushes.Red, setx + (bb * 0.8 - circ) / (DTlist[ind][2] - 1) * ii, sety, circ, circ); }//D方向上端主筋
                                for (int ii = 0; ii < DBlist[ind][2]; ii++) { gfx.DrawEllipse(XBrushes.Red, setx + (bb * 0.8 - circ) / (DTlist[ind][2] - 1) * ii, sety + (dd * 0.8 - circ), circ, circ); }//D方向下端主筋
                                for (int ii = 0; ii < DRlist[ind][2]; ii++) { gfx.DrawEllipse(XBrushes.Red, setx, sety + (dd * 0.8 - circ) / (DRlist[ind][2] - 1) * ii, circ, circ); }//B方向上端主筋
                                for (int ii = 0; ii < DLlist[ind][2]; ii++) { gfx.DrawEllipse(XBrushes.Red, setx + (bb * 0.8 - circ), sety + (dd * 0.8 - circ) / (DLlist[ind][2] - 1) * ii, circ, circ); }//B方向下端主筋
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
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return OpenSeesUtility.Properties.Resources.rccolumncheck;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("a035f5f7-1c0c-4d1e-b641-8727e1025b5d"); }
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