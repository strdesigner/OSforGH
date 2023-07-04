using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Display;

using System.Drawing;
using System.Windows.Forms;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
///****************************************

namespace SeismicLoads
{
    public class SeismicLoads : GH_Component
    {
        public static int px_load = 0; public static int py_load = 0; public static int pxy_load = 0; public static int pxy2_load = 0; public static int mx_load = 0; public static int my_load = 0; public static int mxy_load = 0; public static int mxy2_load = 0;
        public static void SetButton(string s, int i)
        {
            if (s == "px")
            {
                px_load = i;
            }
            else if (s == "py")
            {
                py_load = i;
            }
            else if (s == "pxy")
            {
                pxy_load = i;
            }
            else if (s == "pxy2")
            {
                pxy2_load = i;
            }
            else if (s == "mx")
            {
                mx_load = i;
            }
            else if (s == "my")
            {
                my_load = i;
            }
            else if (s == "mxy")
            {
                mxy_load = i;
            }
            else if (s == "mxy2")
            {
                mxy2_load = i;
            }
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        public SeismicLoads()
          : base("SeismicLoads", "SeismicLoads",
              "Set seismic loads based on AI distribution",
              "OpenSees", "PreProcess")
        {
        }
        public override bool IsPreviewCapable { get { return true; } }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Rj", "Rj", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("F", "F", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("layer", "layer", "[[zmin,zmax],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("Z", "Z", "Regional coefficient", GH_ParamAccess.item, 1.0);///
            pManager.AddNumberParameter("Rt", "Rt", "Vibration characteristic coefficient", GH_ParamAccess.item, 1.0);///
            pManager.AddNumberParameter("C0", "C0", "Base shear", GH_ParamAccess.list, new List<double> { 0.2 });///
            pManager.AddNumberParameter("T", "T", "Natural period h(0.02+0.01α) default=0.2", GH_ParamAccess.item, 0.2);///
            pManager.AddColourParameter("C", "C", "Display color", GH_ParamAccess.item, Color.MintCream);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("index", "index", "List of node numbers belonging to each layer", GH_ParamAccess.tree);///0
            pManager.AddNumberParameter("Wi", "Wi", "layer weight", GH_ParamAccess.list);///1
            pManager.AddNumberParameter("sumWi", "sumWi", "layer support weight", GH_ParamAccess.list);///2
            pManager.AddNumberParameter("Ai", "Ai", "Ai distribution", GH_ParamAccess.list);///3
            pManager.AddNumberParameter("Ci", "Ci", "shear factor", GH_ParamAccess.list);///4
            pManager.AddNumberParameter("Qi", "Qi", "shear force", GH_ParamAccess.list);///5
            pManager.AddNumberParameter("Pi", "Pi", "horizontal force on layer", GH_ParamAccess.list);///6
            pManager.AddNumberParameter("ki", "ki", "horizontal seismic intensity", GH_ParamAccess.list);///7
            pManager.AddNumberParameter("+FX", "+FX", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);///8
            pManager.AddNumberParameter("+FY", "+FY", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);///9
            pManager.AddNumberParameter("+FXY(45)", "+FXY(45)", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);///10
            pManager.AddNumberParameter("+FXY(135)", "+FXY(135)", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);///11
            pManager.AddNumberParameter("-FX", "-FX", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);///12
            pManager.AddNumberParameter("-FY", "-FY", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);///13
            pManager.AddNumberParameter("-FXY(45)", "-FXY(45)", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);///14
            pManager.AddNumberParameter("-FXY(135)", "-FXY(135)", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);///15
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetDataTree("R", out GH_Structure<GH_Number> _r); DA.GetDataTree("F", out GH_Structure<GH_Number> _f_v); DA.GetDataTree("layer", out GH_Structure<GH_Number> _layer);
            var r= _r.Branches; var f_v = _f_v.Branches; var layer = _layer.Branches; int n = layer.Count;
            var Z = 0.0; DA.GetData("Z", ref Z); var Rt = 0.0; DA.GetData("Rt", ref Rt); var C0 = new List<double>(); DA.GetDataList("C0", C0); var T = 0.0; DA.GetData("T", ref T); var color = Color.MintCream; DA.GetData("C", ref color);
            if (C0.Count != n)
            {
                for (int i = 1; i < n; i++) { C0.Add(C0[0]); }
            }
            var arrow = new List<Line>(); var underground = 0;
            if (r[0][0].Value!=-9999 && f_v[0][0].Value!=-9999 && layer[0][0].Value != -9999)
            {
                var index = new List<List<int>>();//各層に所属する節点番号リスト
                for (int i = 0; i < n; i++)
                {
                    var ind = new List<int>();
                    for (int ii = 0; ii < layer[i].Count / 2.0; ii++)
                    {
                        var zmin = layer[i][0 + ii * 2].Value; var zmax = layer[i][1 + ii * 2].Value;
                        for (int j = 0; j < r.Count; j++)
                        {
                            var z = r[j][2].Value;
                            if (zmin <= z && z <= zmax) { ind.Add(j); }
                        }
                        if (zmin < 0) { underground = 1; }
                    }
                    index.Add(ind);
                }
                var indextree= new GH_Structure<GH_Integer>();
                for (int i = 0; i < n; i++)
                {
                    List<GH_Integer> indtree = new List<GH_Integer>();
                    for (int j = 0; j < index[i].Count; j++) { indtree.Add(new GH_Integer(index[i][j])); }
                    indextree.AppendRange(indtree, new GH_Path(i));
                }
                DA.SetDataTree(0, indextree);
                var Wi = new List<double>();//層重量の計算
                var W = 0.0;
                for (int i = 0; i < n; i++)
                {
                    Wi.Add(0.0);
                    for (int j = 0; j < f_v.Count; j++)
                    {
                        int e = (int)f_v[j][0].Value;
                        if (index[i].Contains(e) == true)
                        {
                            Wi[i] += -f_v[j][3].Value;
                        }
                    }
                    if (underground == 0 || i != 0) { W += Wi[i]; }
                }
                DA.SetDataList("Wi", Wi);
                var sumWi = new List<double>();//各層の負担重量の計算
                var ai= new List<double>();
                var Ai = new List<double>();//Ai分布算定
                var Ci = new List<double>();//せん断力係数算定
                var Qi = new List<double>();//負担せん断力算定
                var Pi = new List<double>();//各層水平力算定
                var ki = new List<double>();//水平震度算定
                for (int i = 0; i < n; i++)
                {
                    sumWi.Add(0.0);
                    for (int j = i; j < n; j++)
                    {
                        sumWi[i] += Wi[j];
                    }
                    if (underground == 0 || i != 0)
                    {
                        ai.Add(sumWi[i] / W);
                        Ai.Add(1.0 + (1.0 / Math.Sqrt(ai[i]) - ai[i]) * 2 * T / (1.0 + 3 * T));
                        Ci.Add(Z * Rt * Ai[i] * C0[i]);
                        Qi.Add(sumWi[i] * Ci[i]);
                    }
                    else
                    {
                        ai.Add(1.0); Ai.Add(1.0); Ci.Add(Z * 0.10); Qi.Add(sumWi[i] * Ci[i]);
                    }
                }
                for (int i = 0; i < n; i++)
                {
                    if (i < n - 1) { Pi.Add(Qi[i] - Qi[i + 1]); }
                    else { Pi.Add(Qi[i]); }
                    ki.Add(Pi[i] / Wi[i]);
                }
                DA.SetDataList("sumWi", sumWi);
                DA.SetDataList("Ai", Ai);
                DA.SetDataList("Ci", Ci);
                DA.SetDataList("Qi", Qi);
                DA.SetDataList("Pi", Pi);
                DA.SetDataList("ki", ki);
                //ここから地震荷重の作成
                int k = 0; var seismic_load = new GH_Structure<GH_Number>();
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < index[i].Count; j++)
                    {
                        var fi = index[i][j]; List<GH_Number> flist = new List<GH_Number>();
                        flist.Add(new GH_Number(fi));
                        var fx = 0.0; var fy = 0.0;
                        fx += -f_v[fi][3].Value * ki[i];
                        flist.Add(new GH_Number(fx)); flist.Add(new GH_Number(fy)); flist.Add(new GH_Number(0.0));
                        flist.Add(new GH_Number(0.0)); flist.Add(new GH_Number(0.0)); flist.Add(new GH_Number(0.0));
                        seismic_load.AppendRange(flist, new GH_Path(k));
                        if (px_load == 1)
                        {
                            var r1 = new Point3d(r[fi][0].Value, r[fi][1].Value, r[fi][2].Value); var r2 = new Point3d(r[fi][0].Value - VisualizeModel.VisualizeModel.arrowsize * fx, r[fi][1].Value, r[fi][2].Value);
                            _arrow.Add(new Line(r2, r1)); arrow.Add(new Line(r2, r1));
                            _text.Add(Math.Round(fx, 2).ToString());
                            _c.Add(color);
                        }
                        k += 1;
                    }
                }
                DA.SetDataTree(8, seismic_load);
                k = 0; seismic_load = new GH_Structure<GH_Number>();
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < index[i].Count; j++)
                    {
                        var fi = index[i][j]; List<GH_Number> flist = new List<GH_Number>();
                        flist.Add(new GH_Number(fi));
                        var fx = 0.0; var fy = 0.0;
                        fy += -f_v[fi][3].Value * ki[i];
                        flist.Add(new GH_Number(fx)); flist.Add(new GH_Number(fy)); flist.Add(new GH_Number(0.0));
                        flist.Add(new GH_Number(0.0)); flist.Add(new GH_Number(0.0)); flist.Add(new GH_Number(0.0));
                        seismic_load.AppendRange(flist, new GH_Path(k));
                        if (py_load == 1)
                        {
                            var r1 = new Point3d(r[fi][0].Value, r[fi][1].Value, r[fi][2].Value); var r2 = new Point3d(r[fi][0].Value, r[fi][1].Value - VisualizeModel.VisualizeModel.arrowsize * fy, r[fi][2].Value);
                            _arrow.Add(new Line(r2, r1)); arrow.Add(new Line(r2, r1));
                            _text.Add(Math.Round(fy, 2).ToString());
                            _c.Add(color);
                        }
                        k += 1;
                    }
                }
                DA.SetDataTree(9, seismic_load);
                k = 0; seismic_load = new GH_Structure<GH_Number>();
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < index[i].Count; j++)
                    {
                        var fi = index[i][j]; List<GH_Number> flist = new List<GH_Number>();
                        flist.Add(new GH_Number(fi));
                        var fx = 0.0; var fy = 0.0;
                        fx += -f_v[fi][3].Value * ki[i] / Math.Sqrt(2); fy += -f_v[fi][3].Value * ki[i] / Math.Sqrt(2);
                        flist.Add(new GH_Number(fx)); flist.Add(new GH_Number(fy)); flist.Add(new GH_Number(0.0));
                        flist.Add(new GH_Number(0.0)); flist.Add(new GH_Number(0.0)); flist.Add(new GH_Number(0.0));
                        seismic_load.AppendRange(flist, new GH_Path(k));
                        if (pxy_load == 1)
                        {
                            var r1 = new Point3d(r[fi][0].Value, r[fi][1].Value, r[fi][2].Value); var r2 = new Point3d(r[fi][0].Value - VisualizeModel.VisualizeModel.arrowsize * fx, r[fi][1].Value - VisualizeModel.VisualizeModel.arrowsize * fy, r[fi][2].Value);
                            _arrow.Add(new Line(r2, r1)); arrow.Add(new Line(r2, r1));
                            _text.Add(Math.Round(Math.Sqrt(Math.Pow(fx,2)+ Math.Pow(fy, 2)), 2).ToString());
                            _c.Add(color);
                        }
                        k += 1;
                    }
                }
                DA.SetDataTree(10, seismic_load);
                k = 0; seismic_load = new GH_Structure<GH_Number>();
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < index[i].Count; j++)
                    {
                        var fi = index[i][j]; List<GH_Number> flist = new List<GH_Number>();
                        flist.Add(new GH_Number(fi));
                        var fx = 0.0; var fy = 0.0;
                        fx -= -f_v[fi][3].Value * ki[i] / Math.Sqrt(2); fy += -f_v[fi][3].Value * ki[i] / Math.Sqrt(2);
                        flist.Add(new GH_Number(fx)); flist.Add(new GH_Number(fy)); flist.Add(new GH_Number(0.0));
                        flist.Add(new GH_Number(0.0)); flist.Add(new GH_Number(0.0)); flist.Add(new GH_Number(0.0));
                        seismic_load.AppendRange(flist, new GH_Path(k));
                        if (pxy2_load == 1)
                        {
                            var r1 = new Point3d(r[fi][0].Value, r[fi][1].Value, r[fi][2].Value); var r2 = new Point3d(r[fi][0].Value - VisualizeModel.VisualizeModel.arrowsize * fx, r[fi][1].Value - VisualizeModel.VisualizeModel.arrowsize * fy, r[fi][2].Value);
                            _arrow.Add(new Line(r2, r1)); arrow.Add(new Line(r2, r1));
                            _text.Add(Math.Round(Math.Sqrt(Math.Pow(fx, 2) + Math.Pow(fy, 2)), 2).ToString());
                            _c.Add(color);
                        }
                        k += 1;
                    }
                }
                DA.SetDataTree(11, seismic_load);
                k = 0; seismic_load = new GH_Structure<GH_Number>();
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < index[i].Count; j++)
                    {
                        var fi = index[i][j]; List<GH_Number> flist = new List<GH_Number>();
                        flist.Add(new GH_Number(fi));
                        var fx = 0.0; var fy = 0.0;
                        fx += f_v[fi][3].Value * ki[i];
                        flist.Add(new GH_Number(fx)); flist.Add(new GH_Number(fy)); flist.Add(new GH_Number(0.0));
                        flist.Add(new GH_Number(0.0)); flist.Add(new GH_Number(0.0)); flist.Add(new GH_Number(0.0));
                        seismic_load.AppendRange(flist, new GH_Path(k));
                        if (mx_load == 1)
                        {
                            var r1 = new Point3d(r[fi][0].Value, r[fi][1].Value, r[fi][2].Value); var r2 = new Point3d(r[fi][0].Value - VisualizeModel.VisualizeModel.arrowsize * fx, r[fi][1].Value, r[fi][2].Value);
                            _arrow.Add(new Line(r2, r1)); arrow.Add(new Line(r2, r1));
                            _text.Add(Math.Round(fx, 2).ToString());
                            _c.Add(color);
                        }
                        k += 1;
                    }
                }
                DA.SetDataTree(12, seismic_load);
                k = 0; seismic_load = new GH_Structure<GH_Number>();
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < index[i].Count; j++)
                    {
                        var fi = index[i][j]; List<GH_Number> flist = new List<GH_Number>();
                        flist.Add(new GH_Number(fi));
                        var fx = 0.0; var fy = 0.0;
                        fy += f_v[fi][3].Value * ki[i];
                        flist.Add(new GH_Number(fx)); flist.Add(new GH_Number(fy)); flist.Add(new GH_Number(0.0));
                        flist.Add(new GH_Number(0.0)); flist.Add(new GH_Number(0.0)); flist.Add(new GH_Number(0.0));
                        seismic_load.AppendRange(flist, new GH_Path(k));
                        if (my_load == 1)
                        {
                            var r1 = new Point3d(r[fi][0].Value, r[fi][1].Value, r[fi][2].Value); var r2 = new Point3d(r[fi][0].Value, r[fi][1].Value - VisualizeModel.VisualizeModel.arrowsize * fy, r[fi][2].Value);
                            _arrow.Add(new Line(r2, r1)); arrow.Add(new Line(r2, r1));
                            _text.Add(Math.Round(fy, 2).ToString());
                            _c.Add(color);
                        }
                        k += 1;
                    }
                }
                DA.SetDataTree(13, seismic_load);
                k = 0; seismic_load = new GH_Structure<GH_Number>();
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < index[i].Count; j++)
                    {
                        var fi = index[i][j]; List<GH_Number> flist = new List<GH_Number>();
                        flist.Add(new GH_Number(fi));
                        var fx = 0.0; var fy = 0.0;
                        fx += f_v[fi][3].Value * ki[i] / Math.Sqrt(2); fy += f_v[fi][3].Value * ki[i] / Math.Sqrt(2);
                        flist.Add(new GH_Number(fx)); flist.Add(new GH_Number(fy)); flist.Add(new GH_Number(0.0));
                        flist.Add(new GH_Number(0.0)); flist.Add(new GH_Number(0.0)); flist.Add(new GH_Number(0.0));
                        seismic_load.AppendRange(flist, new GH_Path(k));
                        if (mxy_load == 1)
                        {
                            var r1 = new Point3d(r[fi][0].Value, r[fi][1].Value, r[fi][2].Value); var r2 = new Point3d(r[fi][0].Value - VisualizeModel.VisualizeModel.arrowsize * fx, r[fi][1].Value - VisualizeModel.VisualizeModel.arrowsize * fy, r[fi][2].Value);
                            _arrow.Add(new Line(r2, r1)); arrow.Add(new Line(r2, r1));
                            _text.Add(Math.Round(Math.Sqrt(Math.Pow(fx, 2) + Math.Pow(fy, 2)), 2).ToString());
                            _c.Add(color);
                        }
                        k += 1;
                    }
                }
                DA.SetDataTree(14, seismic_load);
                k = 0; seismic_load = new GH_Structure<GH_Number>();
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < index[i].Count; j++)
                    {
                        var fi = index[i][j]; List<GH_Number> flist = new List<GH_Number>();
                        flist.Add(new GH_Number(fi));
                        var fx = 0.0; var fy = 0.0;
                        fx -= f_v[fi][3].Value * ki[i] / Math.Sqrt(2); fy += f_v[fi][3].Value * ki[i] / Math.Sqrt(2);
                        flist.Add(new GH_Number(fx)); flist.Add(new GH_Number(fy)); flist.Add(new GH_Number(0.0));
                        flist.Add(new GH_Number(0.0)); flist.Add(new GH_Number(0.0)); flist.Add(new GH_Number(0.0));
                        seismic_load.AppendRange(flist, new GH_Path(k));
                        if (mxy2_load == 1)
                        {
                            var r1 = new Point3d(r[fi][0].Value, r[fi][1].Value, r[fi][2].Value); var r2 = new Point3d(r[fi][0].Value - VisualizeModel.VisualizeModel.arrowsize * fx, r[fi][1].Value - VisualizeModel.VisualizeModel.arrowsize * fy, r[fi][2].Value);
                            _arrow.Add(new Line(r2, r1)); arrow.Add(new Line(r2, r1));
                            _text.Add(Math.Round(Math.Sqrt(Math.Pow(fx, 2) + Math.Pow(fy, 2)), 2).ToString());
                            _c.Add(color);
                        }
                        k += 1;
                    }
                }
                DA.SetDataTree(15, seismic_load);
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon { get { return OpenSeesUtility.Properties.Resources.seismic; } }
        public override Guid ComponentGuid
        { get { return new Guid("01a647c5-6d8f-4de2-88fc-0ad6d057287b"); } }
        ///ここからカスタム関数群********************************************************************************
        private readonly List<Line> _arrow = new List<Line>();
        private readonly List<String> _text = new List<String>();
        private readonly List<Color> _c = new List<Color>();
        protected override void BeforeSolveInstance()
        { _arrow.Clear(); _text.Clear(); _c.Clear(); }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            args.Viewport.GetFrustumFarPlane(out Plane plane);
            RhinoViewport viewport = args.Viewport;
            ///軸外力の描画用関数*******************************************************************************
            for (int i = 0; i < _arrow.Count; i++)
            {
                if (VisualizeModel.VisualizeModel.Value == 1)
                {
                    var size = VisualizeModel.VisualizeModel.fontsize; Point3d point = _arrow[i].From; plane.Origin = point;
                    viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                    Text3d drawText = new Text3d(_text[i] + "kN", plane, size);
                    args.Display.Draw3dText(drawText, _c[i]); drawText.Dispose();
                }
                Line arrow = _arrow[i];
                args.Display.DrawLine(arrow, _c[i], 2);
                args.Display.DrawArrowHead(arrow.To, arrow.Direction, _c[i], 25, 0);
            }
        }
        ///ここからGUIの作成*****************************************************************************************
        internal class CustomGUI : GH_ComponentAttributes
        {
            internal CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle radio_rec; private Rectangle radio_rec2;//パネルx2
            private Rectangle radio_rec_1; private Rectangle text_rec_1;
            private Rectangle radio_rec_2; private Rectangle text_rec_2;
            private Rectangle radio_rec_3; private Rectangle text_rec_3;
            private Rectangle radio_rec_4; private Rectangle text_rec_4;
            private Rectangle radio_rec_5; private Rectangle text_rec_5;
            private Rectangle radio_rec_6; private Rectangle text_rec_6;
            private Rectangle radio_rec_7; private Rectangle text_rec_7;
            private Rectangle radio_rec_8; private Rectangle text_rec_8;
            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int height = 27; int radi1 = 7; int radi2 = 25; int width = 35;
                int pitchy = 15; int offset = 10;
                global_rec.Height += height;

                radio_rec = global_rec;
                radio_rec.Y = radio_rec.Bottom - height;
                radio_rec.Height = height;
                radio_rec2 = radio_rec;
                global_rec.Height += height;
                radio_rec2.Y = radio_rec.Bottom;

                text_rec_1 = radio_rec; text_rec_1.X += offset;
                text_rec_1.Height = radi2; text_rec_1.Width = width;
                text_rec_2 = text_rec_1; text_rec_2.X += radi2;
                text_rec_3 = text_rec_2; text_rec_3.X += radi2;
                text_rec_4 = text_rec_3; text_rec_4.X += radi2;
                text_rec_5 = text_rec_1; text_rec_5.Y += height;
                text_rec_6 = text_rec_5; text_rec_6.X += radi2;
                text_rec_7 = text_rec_6; text_rec_7.X += radi2;
                text_rec_8 = text_rec_7; text_rec_8.X += radi2;

                radio_rec_1 = text_rec_1; radio_rec_1.Height = radi1; radio_rec_1.Width = radi1;
                radio_rec_1.X += 5;
                radio_rec_1.Y += pitchy;
                radio_rec_2 = radio_rec_1; radio_rec_2.X += radi2;
                radio_rec_3 = radio_rec_2; radio_rec_3.X += radi2 + 5;
                radio_rec_4 = radio_rec_3; radio_rec_4.X += radi2;
                radio_rec_5 = radio_rec_1; radio_rec_5.Y += height;
                radio_rec_6 = radio_rec_5; radio_rec_6.X += radi2;
                radio_rec_7 = radio_rec_6; radio_rec_7.X += radi2 + 5;
                radio_rec_8 = radio_rec_7; radio_rec_8.X += radi2;

                Bounds = global_rec;
            }
            Brush c1 = Brushes.White; Brush c2 = Brushes.White; Brush c3 = Brushes.White; Brush c4 = Brushes.White; Brush c5 = Brushes.White; Brush c6 = Brushes.White; Brush c7 = Brushes.White; Brush c8 = Brushes.White;
            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
            {
                base.Render(canvas, graphics, channel);
                if (channel == GH_CanvasChannel.Objects)
                {
                    StringFormat format = new StringFormat();
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    format.Trimming = StringTrimming.EllipsisCharacter;

                    GH_Capsule radio = GH_Capsule.CreateCapsule(radio_rec, GH_Palette.White, 2, 0);
                    radio.Render(graphics, Selected, Owner.Locked, false); radio.Dispose();

                    GH_Capsule radio2 = GH_Capsule.CreateCapsule(radio_rec2, GH_Palette.White, 2, 0);
                    radio2.Render(graphics, Selected, Owner.Locked, false); radio2.Dispose();

                    GH_Capsule radio_1 = GH_Capsule.CreateCapsule(radio_rec_1, GH_Palette.Black, 5, 5);
                    radio_1.Render(graphics, Selected, Owner.Locked, false); radio_1.Dispose();
                    graphics.FillEllipse(c1, radio_rec_1);
                    graphics.DrawString("+X", GH_FontServer.Standard, Brushes.Black, text_rec_1);

                    GH_Capsule radio_2 = GH_Capsule.CreateCapsule(radio_rec_2, GH_Palette.Black, 5, 5);
                    radio_2.Render(graphics, Selected, Owner.Locked, false); radio_2.Dispose();
                    graphics.FillEllipse(c2, radio_rec_2);
                    graphics.DrawString("+Y", GH_FontServer.Standard, Brushes.Black, text_rec_2);

                    GH_Capsule radio_3 = GH_Capsule.CreateCapsule(radio_rec_3, GH_Palette.Black, 5, 5);
                    radio_3.Render(graphics, Selected, Owner.Locked, false); radio_3.Dispose();
                    graphics.FillEllipse(c3, radio_rec_3);
                    graphics.DrawString("+45", GH_FontServer.Standard, Brushes.Black, text_rec_3);

                    GH_Capsule radio_4 = GH_Capsule.CreateCapsule(radio_rec_4, GH_Palette.Black, 5, 5);
                    radio_4.Render(graphics, Selected, Owner.Locked, false); radio_4.Dispose();
                    graphics.FillEllipse(c4, radio_rec_4);
                    graphics.DrawString("+135", GH_FontServer.Standard, Brushes.Black, text_rec_4);

                    GH_Capsule radio_5 = GH_Capsule.CreateCapsule(radio_rec_5, GH_Palette.Black, 5, 5);
                    radio_5.Render(graphics, Selected, Owner.Locked, false); radio_5.Dispose();
                    graphics.FillEllipse(c5, radio_rec_5);
                    graphics.DrawString("-X", GH_FontServer.Standard, Brushes.Black, text_rec_5);

                    GH_Capsule radio_6 = GH_Capsule.CreateCapsule(radio_rec_6, GH_Palette.Black, 5, 5);
                    radio_6.Render(graphics, Selected, Owner.Locked, false); radio_6.Dispose();
                    graphics.FillEllipse(c6, radio_rec_6);
                    graphics.DrawString("-Y", GH_FontServer.Standard, Brushes.Black, text_rec_6);

                    GH_Capsule radio_7 = GH_Capsule.CreateCapsule(radio_rec_7, GH_Palette.Black, 5, 5);
                    radio_7.Render(graphics, Selected, Owner.Locked, false); radio_7.Dispose();
                    graphics.FillEllipse(c7, radio_rec_7);
                    graphics.DrawString("-45", GH_FontServer.Standard, Brushes.Black, text_rec_7);

                    GH_Capsule radio_8 = GH_Capsule.CreateCapsule(radio_rec_8, GH_Palette.Black, 5, 5);
                    radio_8.Render(graphics, Selected, Owner.Locked, false); radio_8.Dispose();
                    graphics.FillEllipse(c8, radio_rec_8);
                    graphics.DrawString("-135", GH_FontServer.Standard, Brushes.Black, text_rec_8);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec1 = radio_rec_1; RectangleF rec2 = radio_rec_2; RectangleF rec3 = radio_rec_3; RectangleF rec4 = radio_rec_4; RectangleF rec5 = radio_rec_5; RectangleF rec6 = radio_rec_6; RectangleF rec7 = radio_rec_7; RectangleF rec8 = radio_rec_8; RectangleF rec = radio_rec;
                    if (rec1.Contains(e.CanvasLocation))
                    {
                        if (c1 == Brushes.White) { c1 = Brushes.Black; SetButton("px", 1); }
                        else { c1 = Brushes.White; SetButton("px", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec2.Contains(e.CanvasLocation))
                    {
                        if (c2 == Brushes.White) { c2 = Brushes.Black; SetButton("py", 1); }
                        else { c2 = Brushes.White; SetButton("py", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec3.Contains(e.CanvasLocation))
                    {
                        if (c3 == Brushes.White) { c3 = Brushes.Black; SetButton("pxy", 1); }
                        else { c3 = Brushes.White; SetButton("pxy", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec4.Contains(e.CanvasLocation))
                    {
                        if (c4 == Brushes.White) { c4 = Brushes.Black; SetButton("pxy2", 1); }
                        else { c4 = Brushes.White; SetButton("pxy2", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec5.Contains(e.CanvasLocation))
                    {
                        if (c5 == Brushes.White) { c5 = Brushes.Black; SetButton("mx", 1); }
                        else { c5 = Brushes.White; SetButton("mx", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec6.Contains(e.CanvasLocation))
                    {
                        if (c6 == Brushes.White) { c6 = Brushes.Black; SetButton("my", 1); }
                        else { c6 = Brushes.White; SetButton("my", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec7.Contains(e.CanvasLocation))
                    {
                        if (c7 == Brushes.White) { c7 = Brushes.Black; SetButton("mxy", 1); }
                        else { c7 = Brushes.White; SetButton("mxy", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec8.Contains(e.CanvasLocation))
                    {
                        if (c8 == Brushes.White) { c8 = Brushes.Black; SetButton("mxy2", 1); }
                        else { c8 = Brushes.White; SetButton("mxy2", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}