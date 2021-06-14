using System;
using System.IO;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System.Drawing;
using System.Windows.Forms;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Rhino.DocObjects;
using Rhino;
///****************************************

namespace SectionPerformance
{
    public class SectionPerformanceComponent : GH_Component
    {
        public static int shape = 0; public static int name = 0; public static int render = 0;
        public static void SetButton(string s, int i)
        {
            if (s == "c1")
            {
                shape = i;
            }
            else if (s == "c2")
            {
                name = i;
            }
            else if (s == "c3")
            {
                render = i;
            }
        }
        public SectionPerformanceComponent()
          : base("Calc Section Performance", "Cross-Sec Perf",
              "Calc sectionperformance (A,Iy,Iz,J)",
              "OpenSees", "PreProcess")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("section type", "S", "[■●□〇HL[](DataList)", GH_ParamAccess.list, "");///0
            pManager.AddNumberParameter("parameter 1", "P1", "[■□HL[:B,〇●:R](DataList)", GH_ParamAccess.list, -9999);///1
            pManager.AddNumberParameter("parameter 2", "P2", "[■□HL[:D,〇:t,●:0](DataList)", GH_ParamAccess.list, -9999);///2
            pManager.AddNumberParameter("parameter 3", "P3", "[□HL[:tw,■●〇:0](DataList)", GH_ParamAccess.list, -9999);///3
            pManager.AddNumberParameter("parameter 4", "P4", "[□HL[:tf,■●〇:0](DataList)", GH_ParamAccess.list, -9999);///4
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree, -9999);///5
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree, -9999);///6
            pManager.AddVectorParameter("element axis vector", "l_vec", "element axis vector for each elements", GH_ParamAccess.list, new Vector3d(0, 0, 0));///7
            pManager.AddNumberParameter("fontsize", "FS", "font size for display texts", GH_ParamAccess.item, 10.0);///
            pManager.AddTextParameter("unit_of_length", "LU", "mm,cm,m,...etc. only use for secname", GH_ParamAccess.item, "mm");///
            pManager.AddTextParameter("filename", "filename", "input csv file path", GH_ParamAccess.item," ");///
            pManager.AddNumberParameter("index", "index", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("section area", "A", "[...](DataList)", GH_ParamAccess.list);///0
            pManager.AddNumberParameter("Second moment of area around y-axis", "Iy", "[...](DataList)", GH_ParamAccess.list);///1
            pManager.AddNumberParameter("Second moment of area around z-axis", "Iz", "[...](DataList)", GH_ParamAccess.list);///2
            pManager.AddNumberParameter("St Venant's torsion constant", "J", "[...](DataList)", GH_ParamAccess.list);///3
            pManager.AddNumberParameter("Section modulus around y-axis", "Zy", "[...](DataList)", GH_ParamAccess.list);///4
            pManager.AddNumberParameter("Section modulus around z-axis", "Zz", "[...](DataList)", GH_ParamAccess.list);///5
            pManager.AddNumberParameter("V&A", "V&A", "[volume, surface area](DataList)", GH_ParamAccess.list);///6
            pManager.AddBrepParameter("rendering shape", "ren", "[...](DataList)", GH_ParamAccess.list);
            pManager.AddTextParameter("secname", "secname", "section name", GH_ParamAccess.list);///
            pManager.AddTextParameter("section type", "S", "[■●□〇HL[](DataList)", GH_ParamAccess.list);///0
            pManager.AddNumberParameter("parameter 1", "P1", "[■□HL[:B,〇●:R](DataList)", GH_ParamAccess.list);///1
            pManager.AddNumberParameter("parameter 2", "P2", "[■□HL[:D,〇:t,●:0](DataList)", GH_ParamAccess.list);///2
            pManager.AddNumberParameter("parameter 3", "P3", "[□HL[:tw,■●〇:0](DataList)", GH_ParamAccess.list);///3
            pManager.AddNumberParameter("parameter 4", "P4", "[□HL[:tf,■●〇:0](DataList)", GH_ParamAccess.list);///4
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> sec = new List<string>(); List<double> P1 = new List<double>(); List<double> P2 = new List<double>(); List<double> P3 = new List<double>(); List<double> P4 = new List<double>(); var VA = new List<double> { 0, 0 };
            DA.GetDataList(0, sec); DA.GetDataList(1, P1); DA.GetDataList(2, P2); DA.GetDataList(3, P3); DA.GetDataList(4, P4);
            string filename = " "; DA.GetData("filename", ref filename);
            if (filename != " ")
            {
                sec = new List<string>(); P1 = new List<double>(); P2 = new List<double>(); P3 = new List<double>(); P4 = new List<double>();
                StreamReader sr = new StreamReader(@filename);// 読み込みたいCSVファイルのパスを指定して開く
                int k = 0;
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    if (k != 0)
                    {
                        string[] values = line.Split(',');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                        if (values[1] != "")
                        {
                            sec.Add(values[1]);
                            if (values[1]== "■" || values[1] == "1" || values[1] == "〇" || values[1] == "4" || values[1] == "○")
                            {
                                P1.Add(double.Parse(values[2])); P2.Add(double.Parse(values[3])); P3.Add(0); P4.Add(0);
                            }
                            else if (values[1]== "●" || values[1] == "2")
                            {
                                P1.Add(double.Parse(values[2])); P2.Add(0); P3.Add(0); P4.Add(0);
                            }
                            else
                            {
                                P1.Add(double.Parse(values[2])); P2.Add(double.Parse(values[3])); P3.Add(double.Parse(values[4])); P4.Add(double.Parse(values[5]));
                            }
                        }
                        else { sec.Add("none"); P1.Add(0); P2.Add(0); P3.Add(0); P4.Add(0); }
                    }
                    k += 1;
                }
            }
            DA.SetDataList("section type", sec); DA.SetDataList("parameter 1", P1); DA.SetDataList("parameter 2", P2); DA.SetDataList("parameter 3", P3); DA.SetDataList("parameter 4", P4);
            List<double> A = new List<double>(); List<double> Iy = new List<double>(); List<double> Iz = new List<double>(); List<double> J = new List<double>(); List<double> Zy = new List<double>(); List<double> Zz = new List<double>();
            IList<List<GH_Number>> r; IList<List<GH_Number>> ij; List<Vector3d> l_vec = new List<Vector3d>(); List<Point3d> pts = new List<Point3d>(); var ren = new List<Brep>(); var secname = new List<string>();
            int nsec = sec.Count; int i;
            double fontsize = double.NaN; if (!DA.GetData("fontsize", ref fontsize)) return; string unit_of_length = ""; if (!DA.GetData("unit_of_length", ref unit_of_length)) return;
            var index = new List<double>(); DA.GetDataList("index", index);
            ///断面形状の描画****************************************************************************************
            if (!DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r)) { }
            else if (DA.GetDataTree("element_node_relationship", out GH_Structure<GH_Number> _ij) && DA.GetDataList(7, l_vec))
            {
                r = _r.Branches; ij = _ij.Branches;
                if (r[0][0].Value != -9999 && ij[0][0].Value != -9999 && l_vec[0] != new Vector3d(0, 0, 0))
                {
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
                    if (index[0] == -9999)
                    {
                        index = new List<double>();
                        for (int e = 0; e < ij.Count; e++) { index.Add(e); }
                    }
                    for (int ind = 0; ind < index.Count; ind++)
                    {
                        int e = (int)index[ind];
                        int n1 = (int)ij[e][0].Value; int n2 = (int)ij[e][1].Value; int sec_e = (int)ij[e][3].Value;
                        Point3d r1 = new Point3d(r[n1][0].Value, r[n1][1].Value, r[n1][2].Value); Point3d r2 = new Point3d(r[n2][0].Value, r[n2][1].Value, r[n2][2].Value);
                        Point3d rc = new Point3d((r[n1][0].Value + r[n2][0].Value) / 2.0, (r[n1][1].Value + r[n2][1].Value) / 2.0, (r[n1][2].Value + r[n2][2].Value) / 2.0);
                        Vector3d l1 = l_vec[e]; Vector3d l2 = rotation(l1, r2 - r1, 90);
                        if (sec[sec_e] == "■" || sec[sec_e] == "1")
                        {
                            VA[0] += P1[sec_e] * P2[sec_e] * (r2 - r1).Length; VA[1] += 2 * (P1[sec_e] + P2[sec_e]) * (r2 - r1).Length;
                            if (shape == 1 || render == 1)
                            {
                                Point3d c1 = rc + l1 * P1[sec_e] / 2.0 + l2 * P2[sec_e] / 2.0;
                                Point3d c2 = c1 - l1 * P1[sec_e];
                                Point3d c3 = c2 - l2 * P2[sec_e];
                                Point3d c4 = c3 + l1 * P1[sec_e];
                                if (shape == 1)
                                {
                                    Brep brep = Brep.CreatePlanarBreps(new Polyline(new List<Point3d> { c1, c2, c3, c4, c1 }).ToNurbsCurve(), 0.001)[0];
                                    _rc.Add(brep);
                                    pts.Add(rc);
                                }
                                if (render == 1)
                                {
                                    Brep brep = Brep.CreatePlanarBreps(new Polyline(new List<Point3d> { c1 + (r1 - rc), c2 + (r1 - rc), c3 + (r1 - rc), c4 + (r1 - rc), c1 + (r1 - rc) }).ToNurbsCurve(), 0.001)[0];
                                    var face = brep.Faces[0];
                                    var solid = face.CreateExtrusion(new Line(r1,r2).ToNurbsCurve(), true);
                                    ren.Add(solid);
                                }
                            }
                            if (name == 1)
                            {
                                double scale = 1;
                                if (unit_of_length == "mm") { scale = 1000; }
                                if (unit_of_length == "cm") { scale = 100; }
                                double t1 = P1[sec_e] * scale; double t2 = P2[sec_e] * scale;
                                _text.Add("■-" + t2.ToString() + "x" + t1.ToString());//幅×せい
                                _point.Add(rc);
                                _size.Add(fontsize);
                            }
                        }
                        else if (sec[sec_e] == "●" || sec[sec_e] == "2")
                        {
                            VA[0] += Math.Pow(P1[sec_e],2) * Math.PI/4.0 * (r2 - r1).Length; VA[1] += P1[sec_e] * Math.PI * (r2 - r1).Length;
                            if (shape == 1)
                            {
                                Brep brep = Brep.CreatePlanarBreps(new Circle(rc, P1[sec_e] / 2.0).ToNurbsCurve(), 0.001)[0];
                                _rc.Add(brep);
                            }
                            if (render == 1)
                            {
                                Brep brep = Brep.CreatePlanarBreps(new Circle(r1, P1[sec_e] / 2.0).ToNurbsCurve(), 0.001)[0];
                                var face = brep.Faces[0];
                                var solid = face.CreateExtrusion(new Line(r1, r2).ToNurbsCurve(), true);
                                ren.Add(solid);
                            }
                            if (name == 1)
                            {
                                double scale = 1;
                                if (unit_of_length == "mm") { scale = 1000; }
                                if (unit_of_length == "cm") { scale = 100; }
                                double t1 = P1[sec_e] * scale;
                                _text.Add("●-" + t1.ToString());
                                _point.Add(rc);
                                _size.Add(fontsize);
                            }
                        }
                        else if (sec[sec_e] == "□" || sec[sec_e] == "3" || sec[sec_e] == "▢")
                        {
                            VA[0] += (P1[sec_e] * P2[sec_e] - (P1[sec_e] - 2 * P3[sec_e]) * (P2[sec_e] - 2 * P4[sec_e])) * (r2 - r1).Length; VA[1] += 2 * (P1[sec_e] + P2[sec_e]) * (r2 - r1).Length;
                            if (shape == 1 || render == 1)
                            {
                                Point3d c1 = rc + l1 * (P1[sec_e] - P4[sec_e] * 2) / 2.0 + l2 * (P2[sec_e] - P3[sec_e] * 2) / 2.0;
                                Point3d c2 = rc + l1 * (P1[sec_e] - P4[sec_e] * 2) / 2.0 - l2 * (P2[sec_e] - P3[sec_e] * 2) / 2.0;
                                Point3d c3 = rc - l1 * (P1[sec_e] - P4[sec_e] * 2) / 2.0 - l2 * (P2[sec_e] - P3[sec_e] * 2) / 2.0;
                                Point3d c4 = rc - l1 * (P1[sec_e] - P4[sec_e] * 2) / 2.0 + l2 * (P2[sec_e] - P3[sec_e] * 2) / 2.0;
                                Point3d c5 = rc + l1 * P1[sec_e] / 2.0 + l2 * P2[sec_e] / 2.0;
                                Point3d c6 = rc + l1 * P1[sec_e] / 2.0 - l2 * P2[sec_e] / 2.0;
                                Point3d c7 = rc - l1 * P1[sec_e] / 2.0 - l2 * P2[sec_e] / 2.0;
                                Point3d c8 = rc - l1 * P1[sec_e] / 2.0 + l2 * P2[sec_e] / 2.0;
                                if (shape == 1)
                                {
                                    Brep brep = Brep.CreatePlanarBreps(new Polyline(new List<Point3d> { c1, c2, c3, c4, c8, c7, c6, c5, c1 }).ToNurbsCurve(), 0.001)[0];
                                    _rc.Add(brep);
                                    brep = Brep.CreatePlanarBreps(new Polyline(new List<Point3d> { c1, c4, c8, c5, c1 }).ToNurbsCurve(), 0.001)[0];
                                    _rc.Add(brep);
                                }
                                if (render == 1)
                                {
                                    Brep brep = Brep.CreatePlanarBreps(new Polyline(new List<Point3d> { c1 + (r1 - rc), c2 + (r1 - rc), c3 + (r1 - rc), c4 + (r1 - rc), c8 + (r1 - rc), c7 + (r1 - rc), c6 + (r1 - rc), c5 + (r1 - rc), c1 + (r1 - rc) }).ToNurbsCurve(), 0.001)[0];
                                    var face = brep.Faces[0];
                                    var solid = face.CreateExtrusion(new Line(r1, r2).ToNurbsCurve(), true);
                                    ren.Add(solid);
                                    brep = Brep.CreatePlanarBreps(new Polyline(new List<Point3d> { c1 + (r1 - rc), c4 + (r1 - rc), c8 + (r1 - rc), c5 + (r1 - rc), c1 + (r1 - rc) }).ToNurbsCurve(), 0.001)[0];
                                    face = brep.Faces[0];
                                    solid = face.CreateExtrusion(new Line(r1, r2).ToNurbsCurve(), true);
                                    ren.Add(solid);
                                }
                            }
                            if (name == 1)
                            {
                                double scale = 1;
                                if (unit_of_length == "mm") { scale = 1000; }
                                if (unit_of_length == "cm") { scale = 100; }
                                double t1 = P1[sec_e] * scale; double t2 = P2[sec_e] * scale; double t3 = P3[sec_e] * scale; double t4 = P4[sec_e] * scale;
                                _text.Add("□-" + t1.ToString() + "x" + t2.ToString() + "x" + t3.ToString() + "x" + t4.ToString());//せい×幅×tw×tf
                                _point.Add(rc);
                                _size.Add(fontsize);
                            }
                        }
                        else if (sec[sec_e] == "〇" || sec[sec_e] == "4" || sec[sec_e] == "○")
                        {
                            VA[0] += (Math.Pow(P1[sec_e], 2)- Math.Pow(P1[sec_e]-2*P2[sec_e], 2)) * Math.PI / 4.0 * (r2 - r1).Length; VA[1] += P1[sec_e] * Math.PI * (r2 - r1).Length;
                            if (shape == 1)
                            {
                                Brep brep = new SweepOneRail().PerformSweep(new Arc(rc + l2 * P1[sec_e] / 2.0, rc + l1 * P1[sec_e] / 2.0, rc - l2 * P1[sec_e] / 2.0).ToNurbsCurve(), new Line(rc - l2 * P1[sec_e] / 2.0, rc - l2 * P1[sec_e] / 2.0 + l2 * P2[sec_e]).ToNurbsCurve())[0];
                                _rc.Add(brep);
                                brep = new SweepOneRail().PerformSweep(new Arc(rc + l2 * P1[sec_e] / 2.0, rc - l1 * P1[sec_e] / 2.0, rc - l2 * P1[sec_e] / 2.0).ToNurbsCurve(), new Line(rc - l2 * P1[sec_e] / 2.0, rc - l2 * P1[sec_e] / 2.0 + l2 * P2[sec_e]).ToNurbsCurve())[0];
                                _rc.Add(brep);
                            }
                            if (render == 1)
                            {
                                Brep brep = new SweepOneRail().PerformSweep(new Arc(r1 + l2 * P1[sec_e] / 2.0, r1 + l1 * P1[sec_e] / 2.0, r1 - l2 * P1[sec_e] / 2.0).ToNurbsCurve(), new Line(r1 - l2 * P1[sec_e] / 2.0, r1 - l2 * P1[sec_e] / 2.0 + l2 * P2[sec_e]).ToNurbsCurve())[0];
                                var face = brep.Faces[0];
                                var solid = face.CreateExtrusion(new Line(r1, r2).ToNurbsCurve(), true);
                                ren.Add(solid);
                                brep = new SweepOneRail().PerformSweep(new Arc(r1 + l2 * P1[sec_e] / 2.0, r1 - l1 * P1[sec_e] / 2.0, r1 - l2 * P1[sec_e] / 2.0).ToNurbsCurve(), new Line(r1 - l2 * P1[sec_e] / 2.0, r1 - l2 * P1[sec_e] / 2.0 + l2 * P2[sec_e]).ToNurbsCurve())[0];
                                face = brep.Faces[0];
                                solid = face.CreateExtrusion(new Line(r1, r2).ToNurbsCurve(), true);
                                ren.Add(solid);
                            }
                            if (name == 1)
                            {
                                double scale = 1;
                                if (unit_of_length == "mm") { scale = 1000; }
                                if (unit_of_length == "cm") { scale = 100; }
                                double t1 = P1[sec_e] * scale; double t2 = P2[sec_e] * scale;
                                _text.Add("〇-" + t1.ToString() + "x" + t2.ToString());
                                _point.Add(rc);
                                _size.Add(fontsize);
                            }
                        }
                        else if (sec[sec_e] == "H" || sec[sec_e] == "5")
                        {
                            VA[0] += (P1[sec_e] * P2[sec_e] - (P1[sec_e] - 2 * P4[sec_e]) * (P2[sec_e] * P3[sec_e])) * (r2 - r1).Length; VA[1] += (P2[sec_e] * 2 + P4[sec_e] * 4 + (P2[sec_e] - P3[sec_e]) * 2 + (P1[sec_e] - 2 * P4[sec_e]) * 2) * (r2 - r1).Length;
                            if (shape == 1 || render == 1)
                            {
                                Point3d c1 = rc + l1 * (P1[sec_e] / 2.0 - P4[sec_e]) + l2 * P3[sec_e] / 2.0;
                                Point3d c2 = c1 + l2 * (P2[sec_e] - P3[sec_e]) / 2.0;
                                Point3d c3 = c2 + l1 * P4[sec_e];
                                Point3d c4 = c3 - l2 * P2[sec_e];
                                Point3d c5 = c4 - l1 * P4[sec_e];
                                Point3d c6 = c5 + l2 * (P2[sec_e] - P3[sec_e]) / 2.0;
                                Point3d c7 = c6 - l1 * (P1[sec_e] - 2 * P4[sec_e]);
                                Point3d c8 = c7 - l2 * (P2[sec_e] - P3[sec_e]) / 2.0;
                                Point3d c9 = c8 - l1 * P4[sec_e];
                                Point3d c10 = c9 + l2 * P2[sec_e];
                                Point3d c11 = c10 + l1 * P4[sec_e];
                                Point3d c12 = c11 - l2 * (P2[sec_e] - P3[sec_e]) / 2.0;
                                if (shape == 1)
                                {
                                    Brep brep = Brep.CreatePlanarBreps(new Polyline(new List<Point3d> { c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c1 }).ToNurbsCurve(), 0.001)[0];
                                    _rc.Add(brep);
                                }
                                if (render == 1)
                                {
                                    Brep brep = Brep.CreatePlanarBreps(new Polyline(new List<Point3d> { c1 + (r1 - rc), c2 + (r1 - rc), c3 + (r1 - rc), c4 + (r1 - rc), c5 + (r1 - rc), c6 + (r1 - rc), c7 + (r1 - rc), c8 + (r1 - rc), c9 + (r1 - rc), c10 + (r1 - rc), c11 + (r1 - rc), c12 + (r1 - rc), c1 + (r1 - rc) }).ToNurbsCurve(), 0.001)[0];
                                    var face = brep.Faces[0];
                                    var solid = face.CreateExtrusion(new Line(r1, r2).ToNurbsCurve(), true);
                                    ren.Add(solid);
                                }
                            }
                            if (name == 1)
                            {
                                double scale = 1;
                                if (unit_of_length == "mm") { scale = 1000; }
                                if (unit_of_length == "cm") { scale = 100; }
                                double t1 = P1[sec_e] * scale; double t2 = P2[sec_e] * scale; double t3 = P3[sec_e] * scale; double t4 = P4[sec_e] * scale;
                                _text.Add("H-" + t1.ToString() + "x" + t2.ToString() + "x" + t3.ToString() + "x" + t4.ToString());
                                _point.Add(rc);
                                _size.Add(fontsize);
                            }
                        }
                        else if (sec[sec_e] == "L" || sec[sec_e] == "6")
                        {
                            if (shape == 1 || render == 1)
                            {
                                Point3d c1 = rc - l2 * P2[sec_e] / 2.0 + l1 * P1[sec_e] / 2.0;
                                Point3d c2 = c1 + l2 * P3[sec_e];
                                Point3d c3 = c2 - l1 * (P1[sec_e] - P4[sec_e]);
                                Point3d c4 = c3 + l2 * (P2[sec_e] - P3[sec_e]);
                                Point3d c5 = c4 - l1 * P4[sec_e];
                                Point3d c6 = c5 - l2 * P2[sec_e];
                                if (shape == 1)
                                {
                                    Brep brep = Brep.CreatePlanarBreps(new Polyline(new List<Point3d> { c1, c2, c3, c4, c5, c6, c1 }).ToNurbsCurve(), 0.001)[0];
                                    _rc.Add(brep);
                                }
                                if (render == 1)
                                {
                                    Brep brep = Brep.CreatePlanarBreps(new Polyline(new List<Point3d> { c1 + (r1 - rc), c2 + (r1 - rc), c3 + (r1 - rc), c4 + (r1 - rc), c5 + (r1 - rc), c6 + (r1 - rc), c1 + (r1 - rc) }).ToNurbsCurve(), 0.001)[0];
                                    var face = brep.Faces[0];
                                    var solid = face.CreateExtrusion(new Line(r1, r2).ToNurbsCurve(), true);
                                    ren.Add(solid);
                                }
                            }
                        }
                        else if (sec[sec_e] == "[" || sec[sec_e] == "7" || sec[sec_e] == "コ")
                        {
                            if (shape == 1 || render == 1)
                            {
                                Point3d c1 = rc - l1 * P1[sec_e] / 2.0 + l2 * P2[sec_e] / 2.0;
                                Point3d c2 = c1 + l1 * P4[sec_e];
                                Point3d c3 = c2 - l2 * (P2[sec_e] - P3[sec_e]);
                                Point3d c4 = c3 + l1 * (P1[sec_e] - P4[sec_e]*2);
                                Point3d c5 = c4 + l2 * (P2[sec_e] - P3[sec_e]);
                                Point3d c6 = c5 + l1 * P4[sec_e];
                                Point3d c7 = c6 - l2 * P2[sec_e];
                                Point3d c8 = c7 - l1 * P1[sec_e];
                                if (shape == 1)
                                {
                                    Brep brep = Brep.CreatePlanarBreps(new Polyline(new List<Point3d> { c1, c2, c3, c4, c5, c6, c7, c8, c1 }).ToNurbsCurve(), 0.001)[0];
                                    _rc.Add(brep);
                                }
                                if (render == 1)
                                {
                                    Brep brep = Brep.CreatePlanarBreps(new Polyline(new List<Point3d> { c1 + (r1 - rc), c2 + (r1 - rc), c3 + (r1 - rc), c4 + (r1 - rc), c5 + (r1 - rc), c6 + (r1 - rc), c7 + (r1 - rc), c8 + (r1 - rc), c1 + (r1 - rc) }).ToNurbsCurve(), 0.001)[0];
                                    var face = brep.Faces[0];
                                    var solid = face.CreateExtrusion(new Line(r1, r2).ToNurbsCurve(), true);
                                    ren.Add(solid);
                                }
                            }
                        }
                    }
                    DA.SetDataList(6, VA);
                    DA.SetDataList(7, ren);
                }
            }
            ///******************************************************************************************************

            for (i = 0; i < nsec; i++)
            {
                string ss = sec[i]; double p1 = P1[i]; double p2 = P2[i]; double p3 = P3[i]; double p4 = P4[i];
                if (ss == "■" || ss == "1")
                {
                    double h = p1; double b = p2;
                    A.Add(b * h);
                    Iy.Add(b * Math.Pow(h, 3) / 12.0);
                    Iz.Add(h * Math.Pow(b, 3) / 12.0);
                    if (b < h)
                    {
                        J.Add(Math.Pow(b, 3) * h / 16.0 * (16.0 / 3.0 - 3.360 * b / h * (1.0 - 1.0 / 12.0 * Math.Pow((b / h), 4))));
                    }
                    else
                    {
                        J.Add(Math.Pow(h, 3) * b / 16.0 * (16.0 / 3.0 - 3.360 * h / b * (1.0 - 1.0 / 12.0 * Math.Pow((h / b), 4))));
                    }
                    Zy.Add(b * Math.Pow(h, 2) / 6.0); Zz.Add(h * Math.Pow(b, 2) / 6.0);
                    var hh = ((int)(h * 1000)).ToString(); var bb = ((int)(b * 1000)).ToString();
                    if (Math.Abs(Math.Round(h * 1000, 0) - h * 1000) > 0.05) { hh = (Math.Round((h * 1000), 1)).ToString(); }
                    if (Math.Abs(Math.Round(b * 1000, 0) - b * 1000) > 0.05) { bb = (Math.Round((b * 1000), 1)).ToString(); }
                    secname.Add("■-" + bb + "x" + hh);
                }
                else if (ss == "●" || ss == "2")
                {
                    double d = p1;
                    A.Add(Math.PI * Math.Pow(d, 2) / 4.0);
                    Iy.Add(Math.PI * Math.Pow(d, 4) / 64.0);
                    Iz.Add(Math.PI * Math.Pow(d, 4) / 64.0);
                    J.Add(Math.PI * Math.Pow(d, 4) / 32.0);
                    Zy.Add(Math.PI * Math.Pow(d, 3) / 32.0);
                    Zz.Add(Math.PI * Math.Pow(d, 3) / 32.0);
                    var dd = ((int)(d * 1000)).ToString();
                    if (Math.Abs(Math.Round(d * 1000, 0) - d * 1000) > 0.05) { dd = (Math.Round((d * 1000), 1)).ToString(); }
                    secname.Add("●-" + dd);
                }
                else if (ss == "□" || ss == "3" || ss == "▢")
                {
                    double h = p1; double b = p2; double tw = p3; double tf = p4;
                    double b1 = b - 2 * tw; double h1 = h - 2 * tf;
                    A.Add(b * h - b1 * h1);
                    Iy.Add((b * Math.Pow(h, 3) - b1 * Math.Pow(h1, 3)) / 12.0);
                    Iz.Add((h * Math.Pow(b, 3) - h1 * Math.Pow(b1, 3)) / 12.0);
                    J.Add(2 * tf * tw * Math.Pow((b - tw), 2) * Math.Pow((h - tf), 2) / (tf * (b - tw) + tw * (h - tf)));
                    Zy.Add((b * Math.Pow(h, 3) - b1 * Math.Pow(h1, 3)) / 6.0 / h);
                    Zz.Add((h * Math.Pow(b, 3) - h1 * Math.Pow(b1, 3)) / 6.0 / b);
                    var hh = ((int)(h * 1000)).ToString(); var bb = ((int)(b * 1000)).ToString();
                    var ttw = ((int)(tw * 1000)).ToString(); var ttf = ((int)(tf * 1000)).ToString();
                    if (Math.Abs(Math.Round(h * 1000, 0) - h * 1000) > 0.05) { hh = (Math.Round((h * 1000), 1)).ToString(); }
                    if (Math.Abs(Math.Round(b * 1000, 0) - b * 1000) > 0.05) { bb = (Math.Round((b * 1000), 1)).ToString(); }
                    if (Math.Abs(Math.Round(tw * 1000, 0) - tw * 1000) > 0.05) { ttw = (Math.Round((tw * 1000), 1)).ToString(); }
                    if (Math.Abs(Math.Round(tf * 1000, 0) - tf * 1000) > 0.05) { ttf = (Math.Round((tf * 1000), 1)).ToString(); }
                    secname.Add("□-" + hh + "x" + bb + "x" + ttw + "x" + ttf);
                }
                else if (ss == "〇" || ss == "4" || ss == "○")
                {
                    double d = p1; double t = p2;
                    double d1 = d - t * 2;
                    A.Add(Math.PI * (Math.Pow(d, 2) - Math.Pow(d1, 2)) / 4.0);
                    Iy.Add(Math.PI * (Math.Pow(d, 4) - Math.Pow(d1, 4)) / 64.0);
                    Iz.Add(Math.PI * (Math.Pow(d, 4) - Math.Pow(d1, 4)) / 64.0);
                    J.Add(Math.PI / 32.0 * (Math.Pow(d, 4) - Math.Pow((d - 2 * t), 4)));
                    Zy.Add(Math.PI * (Math.Pow(d, 4) - Math.Pow(d1, 4)) / 32.0 / d);
                    Zz.Add(Math.PI * (Math.Pow(d, 4) - Math.Pow(d1, 4)) / 32.0 / d);
                    var dd = ((int)(d * 1000)).ToString(); var tt = ((int)(t * 1000)).ToString();
                    if (Math.Abs(Math.Round(d * 1000, 0) - d * 1000) > 0.05) { dd = (Math.Round((d * 1000), 1)).ToString(); }
                    if (Math.Abs(Math.Round(t * 1000, 0) - t * 1000) > 0.05) { tt = (Math.Round((t * 1000), 1)).ToString(); }
                    secname.Add("〇-" + dd + "x" + tt);
                }
                else if (ss == "H" || ss == "5")
                {
                    double h = p1; double b = p2; double tw = p3; double tf = p4;
                    A.Add(h * b - (b - tw) * (h - 2 * tf));
                    Iy.Add((b * Math.Pow(h, 3) - (b - tw) * Math.Pow((h - 2 * tf), 3)) / 12.0);
                    Iz.Add((2.0 * tf * Math.Pow(b, 3) + (h - 2 * tf) * Math.Pow(tw, 3)) / 12.0);
                    J.Add((2.0 * b * Math.Pow(tf, 3) + (h - 2 * tf) * Math.Pow(tw, 3)) / 3.0);
                    Zy.Add((b * Math.Pow(h, 3) - (b - tw) * Math.Pow((h - 2 * tf), 3)) / 6.0 / h);
                    Zz.Add((2.0 * tf * Math.Pow(b, 3) + (h - 2 * tf) * Math.Pow(tw, 3)) / 6.0 / b);
                    var hh = ((int)(h * 1000)).ToString(); var bb = ((int)(b * 1000)).ToString();
                    var ttw = ((int)(tw * 1000)).ToString(); var ttf = ((int)(tf * 1000)).ToString();
                    if (Math.Abs(Math.Round(h * 1000, 0) - h * 1000) > 0.05) { hh = (Math.Round((h * 1000), 1)).ToString(); }
                    if (Math.Abs(Math.Round(b * 1000, 0) - b * 1000) > 0.05) { bb = (Math.Round((b * 1000), 1)).ToString(); }
                    if (Math.Abs(Math.Round(tw * 1000, 0) - tw * 1000) > 0.05) { ttw = (Math.Round((tw * 1000), 1)).ToString(); }
                    if (Math.Abs(Math.Round(tf * 1000, 0) - tf * 1000) > 0.05) { ttf = (Math.Round((tf * 1000), 1)).ToString(); }
                    secname.Add("H-" + hh + "x" + bb + "x" + ttw + "x" + ttf);
                }
                else if (ss == "L" || ss == "6")
                {
                    double h = p1; double b = p2; double t1 = p3; double t2 = p4;
                    double e1 = (t1 * Math.Pow(h, 2) + (b - t1) * Math.Pow(t2, 2)) / (2 * (t1 * h + (b - t1) * t2)); double e2 = h - e1;
                    //RhinoApp.WriteLine("e1="+e1.ToString()+" e2="+e2.ToString());
                    A.Add(b * h - (b - t1) * (h - t2));
                    Iy.Add((t1 * Math.Pow(e2, 3) + (b - t1) * Math.Pow(t2 - e1, 3) + b * Math.Pow(e1, 3)) / 3.0);
                    Zy.Add(Iy[i] / e2);////////////////////////////////////
                    J.Add(((h - t2) * Math.Pow(t1, 3) + b * Math.Pow(t2, 3)) / 3.0);
                    h = p2; b = p1; t1 = p4; t2 = p3;
                    e1 = (t1 * Math.Pow(h, 2) + (b - t1) * Math.Pow(t2, 2)) / (2 * (t1 * h + (b - t1) * t2)); e2 = h - e1;
                    Iz.Add((t1 * Math.Pow(e2, 3) + (b - t1) * Math.Pow(t2 - e1, 3) + b * Math.Pow(e1, 3)) / 3.0);
                    Zz.Add(Iz[i] / e2);////////////////////////////////////
                    var hh = ((int)(h * 1000)).ToString(); var bb = ((int)(b * 1000)).ToString();
                    var tt1 = ((int)(t1 * 1000)).ToString(); var tt2 = ((int)(t2 * 1000)).ToString();
                    if (Math.Abs(Math.Round(h * 1000, 0) - h * 1000) > 0.05) { hh = (Math.Round((h * 1000), 1)).ToString(); }
                    if (Math.Abs(Math.Round(b * 1000, 0) - b * 1000) > 0.05) { bb = (Math.Round((b * 1000), 1)).ToString(); }
                    if (Math.Abs(Math.Round(t1 * 1000, 0) - t1 * 1000) > 0.05) { tt1 = (Math.Round((t1 * 1000), 1)).ToString(); }
                    if (Math.Abs(Math.Round(t2 * 1000, 0) - t2 * 1000) > 0.05) { tt2 = (Math.Round((t2 * 1000), 1)).ToString(); }
                    secname.Add("L-" + hh + "x" + bb + "x" + tt1 + "x" + tt2);
                }
                else if (ss == "[" || ss == "7" || ss == "コ")
                {
                    double h = p1; double b = p2; double tw = p3; double tf = p4;
                    A.Add(h * b - (b - tw) * (h - 2 * tf));
                    Iy.Add((b * Math.Pow(h, 3) - (b - tw) * Math.Pow((h - 2 * tf), 3)) / 12.0);
                    Zy.Add((b * Math.Pow(h, 3) - (b - tw) * Math.Pow((h - 2 * tf), 3)) / 6.0 / h);
                    J.Add((2.0 * b * Math.Pow(tf, 3) + (h - 2 * tf) * Math.Pow(tw, 3)) / 3.0);
                    double a = tf / 2.0; double H = b; double B = h; b = B - tf * 2; double t = tw;
                    double e1 = (a * Math.Pow(H, 2) + b * Math.Pow(t, 2)) / (2 * (a * H + b * t)); double e2 = H - e1; h = H - e2 - t;
                    Iz.Add((B * Math.Pow(e1, 3) - b * Math.Pow(h, 3) + a * Math.Pow(e2, 3)) / 3.0);
                    Zz.Add((B * Math.Pow(e1, 3) - b * Math.Pow(h, 3) + a * Math.Pow(e2, 3)) / 3.0 / e1);////////////////////////////////////////////
                    var hh = ((int)(h * 1000)).ToString(); var bb = ((int)(b * 1000)).ToString();
                    var ttw = ((int)(tw * 1000)).ToString(); var ttf = ((int)(tf * 1000)).ToString();
                    if (Math.Abs(Math.Round(h * 1000, 0) - h * 1000) > 0.05) { hh = (Math.Round((h * 1000), 1)).ToString(); }
                    if (Math.Abs(Math.Round(b * 1000, 0) - b * 1000) > 0.05) { bb = (Math.Round((b * 1000), 1)).ToString(); }
                    if (Math.Abs(Math.Round(tw * 1000, 0) - tw * 1000) > 0.05) { ttw = (Math.Round((tw * 1000), 1)).ToString(); }
                    if (Math.Abs(Math.Round(tf * 1000, 0) - tf * 1000) > 0.05) { ttf = (Math.Round((tf * 1000), 1)).ToString(); }
                    secname.Add("[-" + hh + "x" + bb + "x" + ttw + "x" + ttf);
                }
                else
                {
                    A.Add(0.0); Iy.Add(0.0); Iz.Add(0.0); Zy.Add(0.0); Zz.Add(0.0); J.Add(0.0); secname.Add("none");
                }
            }
            DA.SetDataList(0, A); DA.SetDataList(1, Iy); DA.SetDataList(2, Iz); DA.SetDataList(3, J); DA.SetDataList(4, Zy); DA.SetDataList(5, Zz);
            DA.SetDataList(8, secname);
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
                return OpenSeesUtility.Properties.Resources.SecPerf;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("c8dc21bd-76bc-44c3-8861-77372681501f"); }
        }
        ///ここからカスタム関数群********************************************************************************
        private readonly List<string> _text = new List<string>();
        private readonly List<double> _size = new List<double>();
        private readonly List<Point3d> _point = new List<Point3d>();
        private readonly List<Brep> _rc = new List<Brep>();
        protected override void BeforeSolveInstance()
        {
            _text.Clear();
            _size.Clear();
            _point.Clear();
            _rc.Clear();
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            args.Viewport.GetFrustumFarPlane(out Plane plane);
            Rhino.Display.RhinoViewport viewport = args.Viewport;
            for (int i = 0; i < _rc.Count; i++)
            {
                args.Display.DrawBrepShaded(_rc[i], new Rhino.Display.DisplayMaterial(Color.IndianRed));
            }
            ///断面名の描画用関数*********************************************************************************
            for (int i = 0; i < _text.Count; i++)
            {
                double size = _size[i]; Point3d point = _point[i];
                plane.Origin = point;
                viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                Rhino.Display.Text3d drawText = new Rhino.Display.Text3d(_text[i], plane, size);
                args.Display.Draw3dText(_text[i], Color.LightYellow, plane, drawText.Height, "MS UI Gothic", false,false,TextHorizontalAlignment.Left,TextVerticalAlignment.Top);
                drawText.Dispose();
            }
        }///ここからGUIの作成*****************************************************************************************
        public class CustomGUI : GH_ComponentAttributes
        {
            public CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle title_rec;
            private Rectangle radio_rec;
            private Rectangle radio_rec_1; private Rectangle text_rec_1;
            private Rectangle radio_rec_2; private Rectangle text_rec_2;
            private Rectangle radio_rec_3; private Rectangle text_rec_3;
            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int height = 60; int radi1 = 7; int radi2 = 4;
                int pitchx = 8; int pitchy = 11; int textheight = 20;
                global_rec.Height += height;
                int width= global_rec.Width;
                title_rec = global_rec;
                title_rec.Y = title_rec.Bottom - height;
                title_rec.Height = 22;

                radio_rec = title_rec;
                radio_rec.Y += title_rec.Height;
                radio_rec.Height = height - title_rec.Height;

                radio_rec_1 = radio_rec;
                radio_rec_1.X += 5; radio_rec_1.Y += 5;
                radio_rec_1.Height = radi1; radio_rec_1.Width = radi1;

                text_rec_1 = radio_rec_1;
                text_rec_1.X += pitchx; text_rec_1.Y -= radi2;
                text_rec_1.Height = textheight; text_rec_1.Width = width;

                radio_rec_2 = radio_rec_1; radio_rec_2.Y += pitchy;
                text_rec_2 = radio_rec_2;
                text_rec_2.X += pitchx; text_rec_2.Y -= radi2;
                text_rec_2.Height = textheight; text_rec_2.Width = width;

                radio_rec_3 = radio_rec_2; radio_rec_3.Y += pitchy;
                text_rec_3 = radio_rec_3;
                text_rec_3.X += pitchx; text_rec_3.Y -= radi2;
                text_rec_3.Height = textheight; text_rec_3.Width = width;

                Bounds = global_rec;
            }
            Brush c1 = Brushes.White; Brush c2 = Brushes.White; Brush c3 = Brushes.White;
            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
            {
                base.Render(canvas, graphics, channel);
                if (channel == GH_CanvasChannel.Objects)
                {
                    GH_Capsule title = GH_Capsule.CreateCapsule(title_rec, GH_Palette.Pink, 2, 0);
                    title.Render(graphics, Selected, Owner.Locked, false);
                    title.Dispose();

                    StringFormat format = new StringFormat();
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    format.Trimming = StringTrimming.EllipsisCharacter;

                    RectangleF textRectangle = title_rec;
                    textRectangle.Height = 20;
                    graphics.DrawString("Display Option", GH_FontServer.Standard, Brushes.White, textRectangle, format);

                    GH_Capsule radio = GH_Capsule.CreateCapsule(radio_rec, GH_Palette.White, 2, 0);
                    radio.Render(graphics, Selected, Owner.Locked, false); radio.Dispose();

                    GH_Capsule radio_1 = GH_Capsule.CreateCapsule(radio_rec_1, GH_Palette.Black, 5, 5);
                    radio_1.Render(graphics, Selected, Owner.Locked, false); radio_1.Dispose();
                    graphics.FillEllipse(c1, radio_rec_1);
                    graphics.DrawString("Section shape", GH_FontServer.Standard, Brushes.Black, text_rec_1);

                    GH_Capsule radio_2 = GH_Capsule.CreateCapsule(radio_rec_2, GH_Palette.Black, 5, 5);
                    radio_2.Render(graphics, Selected, Owner.Locked, false); radio_2.Dispose();
                    graphics.FillEllipse(c2, radio_rec_2);
                    graphics.DrawString("Section name", GH_FontServer.Standard, Brushes.Black, text_rec_2);

                    GH_Capsule radio_3 = GH_Capsule.CreateCapsule(radio_rec_3, GH_Palette.Black, 5, 5);
                    radio_3.Render(graphics, Selected, Owner.Locked, false); radio_3.Dispose();
                    graphics.FillEllipse(c3, radio_rec_3);
                    graphics.DrawString("Render", GH_FontServer.Standard, Brushes.Black, text_rec_3);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec1 = radio_rec_1; RectangleF rec2 = radio_rec_2; RectangleF rec3 = radio_rec_3;
                    if (rec1.Contains(e.CanvasLocation))
                    {
                        if (c1 == Brushes.White) { c1 = Brushes.Black; SetButton("c1", 1); }
                        else { c1 = Brushes.White; SetButton("c1", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec2.Contains(e.CanvasLocation))
                    {
                        if (c2 == Brushes.White) { c2 = Brushes.Black; SetButton("c2", 1); }
                        else { c2 = Brushes.White; SetButton("c2", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec3.Contains(e.CanvasLocation))
                    {
                        if (c3 == Brushes.White) { c3 = Brushes.Black; SetButton("c3", 1); }
                        else { c3 = Brushes.White; SetButton("c3", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}