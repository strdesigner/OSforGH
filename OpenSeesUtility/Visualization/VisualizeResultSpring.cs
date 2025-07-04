using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Display;
///using MathNet.Numerics.LinearAlgebra.Double;

using System.Drawing;
using System.Windows.Forms;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Rhino;
///****************************************

namespace OpenSeesUtility
{
    public class VisualizeResultSpring : GH_Component
    {
        public static int on_off_11 = 0; public static int on_off_21 = 0;
        public static int on_off2_11 = 0; public static int on_off2_12 = 0; public static int on_off2_13 = 0; public static int on_off2_14 = 0; public static int on_off2_21 = 0; public static int on_off2_22 = 0; public static int on_off2_23 = 0; public static int on_off3_11 = 0;
        public static int Value = 0; public static int Delta = 0;
        double fontsize = double.NaN;
        string unit_of_force = "kN"; string unit_of_length = "m"; int digit = 4;
        public static void SetButton(string s, int i)
        {
            if (s == "c11")
            {
                on_off_11 = i;
            }
            else if (s == "c21")
            {
                on_off_21 = i;
            }
            else if (s == "c211")
            {
                on_off2_11 = i;
            }
            else if (s == "c212")
            {
                on_off2_12 = i;
            }
            else if (s == "c213")
            {
                on_off2_13 = i;
            }
            else if (s == "c214")
            {
                on_off2_14 = i;
            }
            else if (s == "c221")
            {
                on_off2_21 = i;
            }
            else if (s == "c222")
            {
                on_off2_22 = i;
            }
            else if (s == "c223")
            {
                on_off2_23 = i;
            }
            else if (s == "c311")
            {
                on_off3_11 = i;
            }
            else if (s == "Value")
            {
                Value = i;
            }
        }
        public VisualizeResultSpring()
          : base("VisualizeAnalysisResultSpring", "VisualizeResultSpring",
              "Display analysis result by OpenSees",
              "OpenSees", "Visualization")
        {
        }
        public override bool IsPreviewCapable { get { return true; } }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("spring element", "spring", "[[No.i, No.j, kxt, ktc, kyt, kyc, kzt, kzc, rx, ry, rz(spring value)],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("spring_force", "spring_f", "[[N,Qy,Qz,My,Mz],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("spring_allowable_f", "spring_a", "[[Nta,Nca,Qyta,Qyca,Qzta,Qzca,Mxa,Mya,Mza],...](DataTree)", GH_ParamAccess.tree,-9999);///
            pManager.AddNumberParameter("nodal displacement (each node)", "D(R)", "[[dxi,dyi,dzi,theta_xi,theta_yi,theta_zi,dxj,dyj,dzj,theta_xj,theta_yj,theta_zj],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("index(spring)", "index(spring)", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddIntegerParameter("divide_display_points", "Div", "number of display point per element", GH_ParamAccess.item, 30);///
            pManager.AddNumberParameter("scale_factor_for_N,Q", "NS", "scale factor for N,Q", GH_ParamAccess.item, 0.1);///
            pManager.AddNumberParameter("scale_factor_for_M", "MS", "scale factor for M", GH_ParamAccess.item, 0.15);///
            pManager.AddNumberParameter("fontsize", "FS", "font size for display texts", GH_ParamAccess.item, 12.0);///
            pManager.AddNumberParameter("arcsize", "CS", "radius parameter for moment arc with arrow", GH_ParamAccess.item, 0.25);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("spring element", "spring", "[[No.i, No.j, kxt, ktc, kyt, kyc, kzt, kzc, rx, ry, rz(spring value)],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("spring_force", "spring_f", "[[N,Qy,Qz,My,Mz],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("spring_allowable_f", "spring_a", "[[Nta,Nca,Qyta,Qyca,Qzta,Qzca,Mxa,Mya,Mza],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("nodal displacement", "D(spring)", "[[dxi,dyi,dzi,theta_xi,theta_yi,theta_zi,dxj,dyj,dzj,theta_xj,theta_yj,theta_zj],...](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double nscale = double.NaN; if (!DA.GetData("scale_factor_for_N,Q", ref nscale)) return;
            double mscale = double.NaN; if (!DA.GetData("scale_factor_for_M", ref mscale)) return;
            if (!DA.GetData("fontsize", ref fontsize)) return; int div = 10; if (!DA.GetData("divide_display_points", ref div)) return;
            Vector3d rotation(Vector3d a, Vector3d b, double theta)
            {
                double rad = theta * Math.PI / 180;
                double s = Math.Sin(rad); double c = Math.Cos(rad);
                b /= Math.Sqrt(Vector3d.Multiply(b, b));
                double b1 = b[0]; double b2 = b[1]; double b3 = b[2];
                Vector3d v1 = new Vector3d(c + Math.Pow(b1, 2) * (1 - c), b1 * b2 * (1 - c) - b3 * s, b1 * b3 * (1 - c) + b2 * s);
                Vector3d v2 = new Vector3d(b2 * b1 * (1 - c) + b3 * s, c + Math.Pow(b2, 2) * (1 - c), b2 * b3 * (1 - c) - b1 * s);
                Vector3d v3 = new Vector3d(b3 * b1 * (1 - c) - b2 * s, b3 * b2 * (1 - c) + b1 * s, c + Math.Pow(b3, 2) * (1 - c));
                return new Vector3d(Vector3d.Multiply(v1, a), Vector3d.Multiply(v2, a), Vector3d.Multiply(v3, a));
            }
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r); var r = _r.Branches;
            DA.GetDataTree("spring element", out GH_Structure<GH_Number> _spring); var spring = _spring.Branches; var m = spring.Count;
            DA.GetDataTree("spring_force", out GH_Structure<GH_Number> _spring_f); var spring_f = _spring_f.Branches;
            DA.GetDataTree("spring_allowable_f", out GH_Structure<GH_Number> _spring_a); var spring_a = _spring_a.Branches;
            List<double> index = new List<double>(); DA.GetDataList("index(spring)", index);
            if (index[0] == -9999)
            {
                index = new List<double>();
                for (int e = 0; e < m; e++) { index.Add(e); }
            }
            if (_spring.Branches[0][0].Value != -9999)
            {
                DA.SetDataTree(1, _spring);
                var angle = new List<double>();
                for (int ii = 0; ii < m; ii++) { angle.Add(0); }
                List<Vector3d> l_vec = new List<Vector3d>();
                var Nmax = 0.0; var Nmin = 0.0; var Mxmax = 0.0; var Mymax = 0.0; var Mzmax = 0.0; var Qymax = 0.0; var Qzmax = 0.0;
                for (int ind = 0; ind < index.Count; ind++)
                {
                    int e = (int)index[ind];
                    Nmax = Math.Max(Nmax, spring_f[e][0].Value * nscale); Nmin = Math.Max(Nmin, spring_f[e][0].Value * nscale);
                    Qymax = Math.Max(Qymax, Math.Abs(spring_f[e][1].Value) * nscale);
                    Qzmax = Math.Max(Qzmax, Math.Abs(spring_f[e][2].Value) * nscale);
                    Mxmax = Math.Max(Mxmax, Math.Abs(spring_f[e][3].Value) * mscale);
                    Mymax = Math.Max(Mymax, Math.Abs(spring_f[e][4].Value) * mscale);
                    Mzmax = Math.Max(Mzmax, Math.Abs(spring_f[e][5].Value) * mscale);
                }
                for (int ind = 0; ind < index.Count; ind++)
                {
                    int e = (int)index[ind];
                    if (spring[0].Count == 12)
                    {
                        angle[e] = spring[e][11].Value;
                    }
                    int i = (int)spring[e][0].Value; int j = (int)spring[e][1].Value;
                    var r1 = new Point3d(r[i][0].Value, r[i][1].Value, r[i][2].Value); var r2 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value);
                    Vector3d x = new Vector3d(r[j][0].Value - r[i][0].Value, r[j][1].Value - r[i][1].Value, r[j][2].Value - r[i][2].Value);
                    Vector3d y = new Vector3d(0, 1, 0);
                    Vector3d z = new Vector3d(0, 0, 1);
                    if (Math.Abs(x[0]) <= 5e-3 && Math.Abs(x[1]) <= 5e-3)
                    {
                        y = rotation(x, new Vector3d(0, 1, 0), 90);
                        z = rotation(y, x, 90 + angle[e]);
                        Vector3d l = z / Math.Sqrt(Vector3d.Multiply(z, z));
                        l_vec.Add(l);
                    }
                    else
                    {
                        y = rotation(x, new Vector3d(0, 0, 1), 90);
                        y[2] = 0.0;
                        z = rotation(y, x, 90 + angle[e]);
                        Vector3d l = z / Math.Sqrt(Vector3d.Multiply(z, z));
                        l_vec.Add(l);
                    }
                    var kxt = spring[e][2].Value; var kxc = spring[e][3].Value;
                    var kyt = spring[e][4].Value; var kyc = spring[e][5].Value;
                    var kzt = spring[e][6].Value; var kzc = spring[e][7].Value;
                    var kmx = spring[e][8].Value; var kmy = spring[e][9].Value; var kmz = spring[e][10].Value;
                    var N = spring_f[e][0].Value; var Qy = spring_f[e][1].Value; var Qz = spring_f[e][2].Value;
                    var Mx = spring_f[e][3].Value; var My = spring_f[e][4].Value; var Mz = spring_f[e][5].Value;
                    ///断面力の描画****************************************************************************************
                    if (spring_f[0][0].Value != -9999)
                    {
                        if (on_off_11 == 1)
                        {
                            var text = "kxt=" + ((int)kxt).ToString("G") + "kN/m";
                            text += " ";
                            text += "kxc=" + ((int)kxc).ToString("G") + "kN/m";
                            text += "\n";
                            text += "kyt=" + ((int)kyt).ToString("G") + "kN/m";
                            text += " ";
                            text += "kyc=" + ((int)kyc).ToString("G") + "kN/m";
                            text += "\n";
                            text += "kzt=" + ((int)kzt).ToString("G") + "kN/m";
                            text += " ";
                            text += "kzc=" + ((int)kzc).ToString("G") + "kN/m";
                            text += "\n";
                            text += "kmx=" + ((int)kmx).ToString("G") + "kNm/rad";
                            text += " ";
                            text += "kmy=" + ((int)kmy).ToString("G") + "kNm/rad";
                            text += " ";
                            text += "kmz=" + ((int)kmz).ToString("G") + "kNm/rad";
                            _stiffness.Add(text);
                            _pt2.Add((r1 + r2) / 2.0);
                        }
                        if (on_off_21 == 1)
                        {
                            var text = "";
                            if (kxt < 999999 && kxt > 1) { text = "kxt=" + ((int)kxt).ToString("G") + "kN/m"; text += " "; }
                            if (kxc < 999999 && kxc > 1) {text += "kxc=" + ((int)kxc).ToString("G") + "kN/m";}
                            text += "\n";
                            if (kyt < 999999 && kyt > 1) { text += "kyt=" + ((int)kyt).ToString("G") + "kN/m"; text += " "; }
                            if (kyc < 999999 && kyc > 1) { text += "kyc=" + ((int)kyc).ToString("G") + "kN/m"; }
                            text += "\n";
                            if (kzt < 999999 && kzt > 1) { text += "kzt=" + ((int)kzt).ToString("G") + "kN/m"; text += " "; }
                            if (kzc < 999999 && kzc > 1) { text += "kzc=" + ((int)kzc).ToString("G") + "kN/m"; }
                            text += "\n";
                            if (kmx < 999999 && kmx > 1) { text += "kmx=" + ((int)kmx).ToString("G") + "kNm/rad"; text += " "; }
                            if (kmy < 999999 && kmy > 1) { text += "kmy=" + ((int)kmy).ToString("G") + "kNm/rad"; text += " "; }
                            if (kmz < 999999 && kmz > 1) { text += "kmz=" + ((int)kmz).ToString("G") + "kNm/rad"; }
                            _stiffness.Add(text);
                            _pt2.Add((r1 + r2) / 2.0);
                        }
                        if (on_off2_11 == 1 && N<0)
                        {
                            var l_ve = rotation(l_vec[ind], r2 - r1, 90);
                            var p1 = r1 - l_ve * N * nscale; var p2 = r2 - l_ve * N * nscale;
                            var mesh = new Mesh(); mesh.Vertices.Add(r1); mesh.Vertices.Add(r2); mesh.Vertices.Add(p2); mesh.Vertices.Add(p1);
                            var color1 = new ColorHSL((1 - Math.Abs(N * nscale) / Math.Max(1e-10, -Nmin)) * 1.9 / 3.0, 1, 0.5);
                            mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1);
                            mesh.Faces.AddFace(0, 1, 2, 3);
                            _mesh.Add(mesh);
                            if (Value == 1)
                            {
                                _pt.Add((p1 + p2) / 2.0); _value.Add(Math.Abs(N)); _funit.Add("kN"); _lunit.Add("");
                            }
                        }
                        if (on_off2_12 == 1 && N > 0)
                        {
                            var l_ve = rotation(l_vec[ind], r2 - r1, 90);
                            var p1 = r1 - l_ve * N * nscale; var p2 = r2 - l_ve * N * nscale;
                            var mesh = new Mesh(); mesh.Vertices.Add(r1); mesh.Vertices.Add(r2); mesh.Vertices.Add(p2); mesh.Vertices.Add(p1);
                            var color1 = new ColorHSL((1 - Math.Abs(N * nscale) / Math.Max(1e-10, Math.Abs(Nmax))) * 1.9 / 3.0, 1, 0.5);
                            mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1);
                            mesh.Faces.AddFace(0, 1, 2, 3);
                            _mesh.Add(mesh);
                            if (Value == 1)
                            {
                                _pt.Add((p1 + p2) / 2.0); _value.Add(Math.Abs(N)); _funit.Add("kN"); _lunit.Add("");
                            }
                        }
                        if (on_off2_13 == 1)
                        {
                            var l_ve = rotation(l_vec[ind], r2 - r1, 90);
                            var p1 = r1 - l_ve * Qy * nscale; var p2 = r2 - l_ve * Qy * nscale;
                            var mesh = new Mesh(); mesh.Vertices.Add(r1); mesh.Vertices.Add(r2); mesh.Vertices.Add(p2); mesh.Vertices.Add(p1);
                            var color1 = new ColorHSL((1 - Math.Abs(Qy * nscale) / Math.Max(1e-10, Qymax)) * 1.9 / 3.0, 1, 0.5);
                            mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1);
                            mesh.Faces.AddFace(0, 1, 2, 3);
                            _mesh.Add(mesh);
                            if (Value == 1)
                            {
                                _pt.Add((p1 + p2) / 2.0); _value.Add(Math.Abs(Qy)); _funit.Add("kN"); _lunit.Add("");
                            }
                        }
                        if (on_off2_14 == 1)
                        {
                            var l_ve = l_vec[ind];
                            var p1 = r1 - l_ve * Qz * nscale; var p2 = r2 - l_ve * Qz * nscale;
                            var mesh = new Mesh(); mesh.Vertices.Add(r1); mesh.Vertices.Add(r2); mesh.Vertices.Add(p2); mesh.Vertices.Add(p1);
                            var color1 = new ColorHSL((1 - Math.Abs(Qz * nscale) / Math.Max(1e-10, Qzmax)) * 1.9 / 3.0, 1, 0.5);
                            mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1);
                            mesh.Faces.AddFace(0, 1, 2, 3);
                            _mesh.Add(mesh);
                            if (Value == 1)
                            {
                                _pt.Add((p1 + p2) / 2.0); _value.Add(Math.Abs(Qz)); _funit.Add("kN"); _lunit.Add("");
                            }
                        }
                        if (on_off2_22 == 1)
                        {
                            var l_ve = rotation(l_vec[ind], r2 - r1, 90);
                            var p1 = r1 - l_ve * Mz * mscale; var p2 = r2 - l_ve * Mz * mscale;
                            var mesh = new Mesh(); mesh.Vertices.Add(r1); mesh.Vertices.Add(r2); mesh.Vertices.Add(p2); mesh.Vertices.Add(p1);
                            var color1 = new ColorHSL((1 - Math.Abs(Mz * mscale) / Math.Max(1e-10, Mzmax)) * 1.9 / 3.0, 1, 0.5);
                            mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1);
                            mesh.Faces.AddFace(0, 1, 2, 3);
                            _mesh.Add(mesh);
                            if (Value == 1)
                            {
                                _pt.Add((p1 + p2) / 2.0); _value.Add(Math.Abs(Mz)); _funit.Add("kN"); _lunit.Add("m");
                            }
                        }
                        if (on_off2_23 == 1)
                        {
                            var l_ve = l_vec[ind];
                            var p1 = r1 - l_ve * My * mscale; var p2 = r2 - l_ve * My * mscale;
                            var mesh = new Mesh(); mesh.Vertices.Add(r1); mesh.Vertices.Add(r2); mesh.Vertices.Add(p2); mesh.Vertices.Add(p1);
                            var color1 = new ColorHSL((1 - Math.Abs(My * mscale) / Math.Max(1e-10, Mymax)) * 1.9 / 3.0, 1, 0.5);
                            mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1); mesh.VertexColors.Add(color1);
                            mesh.Faces.AddFace(0, 1, 2, 3);
                            _mesh.Add(mesh);
                            if (Value == 1)
                            {
                                _pt.Add((p1 + p2) / 2.0); _value.Add(Math.Abs(My)); _funit.Add("kN"); _lunit.Add("m");
                            }
                        }
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
                return OpenSeesUtility.Properties.Resources.VisualizeResultSpring;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("bf022c72-4803-4f55-ab9b-a0b1e255ca6e"); }
        }
        ///ここからカスタム関数群********************************************************************************
        private readonly List<Mesh> _mesh = new List<Mesh>();
        private readonly List<double> _value = new List<double>();
        private readonly List<Point3d> _pt = new List<Point3d>();
        private readonly List<string> _lunit = new List<string>();
        private readonly List<string> _funit = new List<string>();
        private readonly List<string> _stiffness = new List<string>();
        private readonly List<Point3d> _pt2 = new List<Point3d>();
        protected override void BeforeSolveInstance()
        {
            _value.Clear();
            _pt.Clear();
            _mesh.Clear();
            _lunit.Clear();
            _funit.Clear();
            _stiffness.Clear();
            _pt2.Clear();
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            args.Viewport.GetFrustumFarPlane(out Plane plane);
            RhinoViewport viewport = args.Viewport;
            ///モーメントとせん断力の描画用関数*************************************************************************
            for (int i = 0; i < _mesh.Count; i++)
            {
                args.Display.DrawMeshFalseColors(_mesh[i]);
            }
            for (int i = 0; i < _value.Count; i++)
            {
                double size = fontsize;
                Point3d point = _pt[i];
                plane.Origin = point;
                viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                Text3d drawText = new Text3d(_value[i].ToString("F").Substring(0, digit) + _funit[i] + _lunit[i], plane, size);
                args.Display.Draw3dText(drawText, Color.MediumVioletRed); drawText.Dispose();
            }
            for (int i = 0; i < _stiffness.Count; i++)
            {
                double size = fontsize;
                Point3d point = _pt2[i];
                plane.Origin = point;
                viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                Text3d drawText = new Text3d(_stiffness[i], plane, size);
                args.Display.Draw3dText(drawText, Color.Black); drawText.Dispose();
            }
            ///*************************************************************************************************
        }
        public class CustomGUI : GH_ComponentAttributes
        {
            public CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle title_rec; private Rectangle title1_rec; private Rectangle title2_rec; private Rectangle title3_rec;
            private Rectangle radio_rec; private Rectangle radio2_rec; private Rectangle radio3_rec;
            private Rectangle radio_rec_11; private Rectangle text_rec_11;
            private Rectangle radio_rec_21; private Rectangle text_rec_21;
            private Rectangle radio_rec2_11; private Rectangle text_rec2_11; private Rectangle radio_rec2_12; private Rectangle text_rec2_12; private Rectangle radio_rec2_13; private Rectangle text_rec2_13; private Rectangle radio_rec2_14; private Rectangle text_rec2_14;
            private Rectangle radio_rec2_21; private Rectangle text_rec2_21; private Rectangle radio_rec2_22; private Rectangle text_rec2_22; private Rectangle radio_rec2_23; private Rectangle text_rec2_23;
             private Rectangle radio_rec3_11; private Rectangle text_rec3_11;

            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int height = 187; int subwidth = 36; int radi1 = 7; int radi2 = 4;
                int pitchx = 6; int textheight = 20; int subtitleheight = 18;
                global_rec.Height += height;
                title_rec = global_rec;
                title_rec.Y = title_rec.Bottom - height;
                title_rec.Height = 22;

                radio_rec = title_rec;
                radio_rec.Y += title_rec.Height;

                title1_rec = radio_rec;
                title1_rec.Height = subtitleheight;

                radio_rec_11 = title1_rec;
                radio_rec_11.X += radi2 - 1; radio_rec_11.Y += title1_rec.Height + radi2;
                radio_rec_11.Height = radi1; radio_rec_11.Width = radi1;

                text_rec_11 = radio_rec_11;
                text_rec_11.X += pitchx; text_rec_11.Y -= radi2;
                text_rec_11.Height = textheight; text_rec_11.Width = subwidth * 3;

                radio_rec_21 = radio_rec_11;
                radio_rec_21.Y += text_rec_11.Height - radi1;
                radio_rec_21.Height = radi1; radio_rec_11.Width = radi1;

                text_rec_21 = radio_rec_21;
                text_rec_21.X += pitchx; text_rec_21.Y -= radi2;
                text_rec_21.Height = textheight; text_rec_21.Width = subwidth * 3;

                radio_rec.Height = text_rec_21.Y + textheight - radio_rec.Y - radi2;
                ///******************************************************************************************
                radio2_rec = radio_rec;
                radio2_rec.Y = radio_rec.Y + radio_rec.Height;

                title2_rec = title1_rec;
                title2_rec.Y = radio2_rec.Y;

                radio_rec2_11 = title2_rec;
                radio_rec2_11.X += radi2 - 1; radio_rec2_11.Y += title2_rec.Height + radi2;
                radio_rec2_11.Height = radi1; radio_rec2_11.Width = radi1;

                text_rec2_11 = radio_rec2_11;
                text_rec2_11.X += pitchx; text_rec2_11.Y -= radi2;
                text_rec2_11.Height = textheight; text_rec2_11.Width = subwidth;

                radio_rec2_12 = text_rec2_11;
                radio_rec2_12.X += text_rec2_11.Width - radi2; radio_rec2_12.Y = radio_rec2_11.Y;
                radio_rec2_12.Height = radi1; radio_rec2_12.Width = radi1;

                text_rec2_12 = radio_rec2_12;
                text_rec2_12.X += pitchx; text_rec2_12.Y -= radi2;
                text_rec2_12.Height = textheight; text_rec2_12.Width = subwidth;

                radio_rec2_13 = text_rec2_12;
                radio_rec2_13.X += text_rec2_12.Width - radi2; radio_rec2_13.Y = radio_rec2_12.Y;
                radio_rec2_13.Height = radi1; radio_rec2_13.Width = radi1;

                text_rec2_13 = radio_rec2_13;
                text_rec2_13.X += pitchx; text_rec2_13.Y -= radi2;
                text_rec2_13.Height = textheight; text_rec2_13.Width = subwidth;

                radio_rec2_14 = text_rec2_13;
                radio_rec2_14.X += text_rec2_13.Width - radi2; radio_rec2_14.Y = radio_rec2_13.Y;
                radio_rec2_14.Height = radi1; radio_rec2_14.Width = radi1;

                text_rec2_14 = radio_rec2_14;
                text_rec2_14.X += pitchx; text_rec2_14.Y -= radi2;
                text_rec2_14.Height = textheight; text_rec2_14.Width = subwidth;

                radio_rec2_21 = radio_rec2_11;
                radio_rec2_21.Y += text_rec2_11.Height - radi1;
                radio_rec2_21.Height = radi1; radio_rec2_11.Width = radi1;

                text_rec2_21 = radio_rec2_21;
                text_rec2_21.X += pitchx; text_rec2_21.Y -= radi2;
                text_rec2_21.Height = textheight; text_rec2_21.Width = subwidth;

                radio_rec2_22 = text_rec2_21;
                radio_rec2_22.X += text_rec2_21.Width - radi2; radio_rec2_22.Y = radio_rec2_21.Y;
                radio_rec2_22.Height = radi1; radio_rec2_22.Width = radi1;

                text_rec2_22 = radio_rec2_22;
                text_rec2_22.X += pitchx; text_rec2_22.Y -= radi2;
                text_rec2_22.Height = textheight; text_rec2_22.Width = subwidth;

                radio_rec2_23 = text_rec2_22;
                radio_rec2_23.X += text_rec2_22.Width - radi2; radio_rec2_23.Y = radio_rec2_22.Y;
                radio_rec2_23.Height = radi1; radio_rec2_23.Width = radi1;

                text_rec2_23 = radio_rec2_23;
                text_rec2_23.X += pitchx; text_rec2_23.Y -= radi2;
                text_rec2_23.Height = textheight; text_rec2_23.Width = subwidth + 30;

                radio2_rec.Height = text_rec2_23.Y + textheight - radio2_rec.Y - radi2;
                ///******************************************************************************************
                radio3_rec = radio2_rec; radio3_rec.Height = textheight - radi2;
                radio3_rec.Y += radio2_rec.Height;

                radio_rec3_11 = radio3_rec;
                radio_rec3_11.X += radi2 - 1; radio_rec3_11.Y += radi2;
                radio_rec3_11.Height = radi1; radio_rec3_11.Width = radi1;

                text_rec3_11 = radio_rec3_11;
                text_rec3_11.X += pitchx; text_rec3_11.Y -= radi2;
                text_rec3_11.Height = textheight; text_rec3_11.Width = subwidth + 30;
                global_rec.Height = text_rec3_11.Y + radio3_rec.Height - global_rec.Y;
                Bounds = global_rec;
            }
            Brush c11 = Brushes.White; Brush c21 = Brushes.White;
            Brush c211 = Brushes.White; Brush c212 = Brushes.White; Brush c213 = Brushes.White; Brush c214 = Brushes.White; Brush c221 = Brushes.White; Brush c222 = Brushes.White; Brush c223 = Brushes.White;
            Brush c311 = Brushes.White;
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

                    GH_Capsule title1 = GH_Capsule.CreateCapsule(title1_rec, GH_Palette.Blue, 2, 0);
                    title1.Render(graphics, Selected, Owner.Locked, false);
                    title1.Dispose();

                    RectangleF textRectangle1 = title1_rec;
                    textRectangle1.Height = 20;
                    graphics.DrawString("About spring stiffness", GH_FontServer.Standard, Brushes.White, textRectangle1, format);


                    GH_Capsule radio_11 = GH_Capsule.CreateCapsule(radio_rec_11, GH_Palette.Black, 5, 5);
                    radio_11.Render(graphics, Selected, Owner.Locked, false); radio_11.Dispose();
                    graphics.FillEllipse(c11, radio_rec_11);
                    graphics.DrawString("all", GH_FontServer.Standard, Brushes.Black, text_rec_11);

                    GH_Capsule radio_21 = GH_Capsule.CreateCapsule(radio_rec_21, GH_Palette.Black, 5, 5);
                    radio_21.Render(graphics, Selected, Owner.Locked, false); radio_21.Dispose();
                    graphics.FillEllipse(c21, radio_rec_21);
                    graphics.DrawString("without rigid", GH_FontServer.Standard, Brushes.Black, text_rec_21);
                    ///******************************************************************************************
                    GH_Capsule radio2 = GH_Capsule.CreateCapsule(radio2_rec, GH_Palette.White, 2, 0);
                    radio2.Render(graphics, Selected, Owner.Locked, false); radio2.Dispose();

                    GH_Capsule title2 = GH_Capsule.CreateCapsule(title2_rec, GH_Palette.Blue, 2, 0);
                    title2.Render(graphics, Selected, Owner.Locked, false);
                    title2.Dispose();

                    RectangleF textRectangle2 = title2_rec;
                    textRectangle2.Height = 20;
                    graphics.DrawString("About spring force", GH_FontServer.Standard, Brushes.White, textRectangle2, format);
                    ///******************************************************************************************

                    GH_Capsule radio2_11 = GH_Capsule.CreateCapsule(radio_rec2_11, GH_Palette.Black, 5, 5);
                    radio2_11.Render(graphics, Selected, Owner.Locked, false); radio2_11.Dispose();
                    graphics.FillEllipse(c211, radio_rec2_11);
                    graphics.DrawString("T", GH_FontServer.Standard, Brushes.Black, text_rec2_11);

                    GH_Capsule radio2_12 = GH_Capsule.CreateCapsule(radio_rec2_12, GH_Palette.Black, 5, 5);
                    radio2_12.Render(graphics, Selected, Owner.Locked, false); radio2_12.Dispose();
                    graphics.FillEllipse(c212, radio_rec2_12);
                    graphics.DrawString("C", GH_FontServer.Standard, Brushes.Black, text_rec2_12);

                    GH_Capsule radio2_13 = GH_Capsule.CreateCapsule(radio_rec2_13, GH_Palette.Black, 5, 5);
                    radio2_13.Render(graphics, Selected, Owner.Locked, false); radio2_13.Dispose();
                    graphics.FillEllipse(c213, radio_rec2_13);
                    graphics.DrawString("Qy", GH_FontServer.Standard, Brushes.Black, text_rec2_13);


                    GH_Capsule radio2_14 = GH_Capsule.CreateCapsule(radio_rec2_14, GH_Palette.Black, 5, 5);
                    radio2_14.Render(graphics, Selected, Owner.Locked, false); radio2_14.Dispose();
                    graphics.FillEllipse(c214, radio_rec2_14);
                    graphics.DrawString("Qz", GH_FontServer.Standard, Brushes.Black, text_rec2_14);

                    GH_Capsule radio2_21 = GH_Capsule.CreateCapsule(radio_rec2_21, GH_Palette.Black, 5, 5);
                    radio2_21.Render(graphics, Selected, Owner.Locked, false); radio2_21.Dispose();
                    graphics.FillEllipse(c221, radio_rec2_21);
                    graphics.DrawString("Mx", GH_FontServer.Standard, Brushes.Black, text_rec2_21);

                    GH_Capsule radio2_22 = GH_Capsule.CreateCapsule(radio_rec2_22, GH_Palette.Black, 5, 5);
                    radio2_22.Render(graphics, Selected, Owner.Locked, false); radio2_22.Dispose();
                    graphics.FillEllipse(c222, radio_rec2_22);
                    graphics.DrawString("My", GH_FontServer.Standard, Brushes.Black, text_rec2_22);

                    GH_Capsule radio2_23 = GH_Capsule.CreateCapsule(radio_rec2_23, GH_Palette.Black, 5, 5);
                    radio2_23.Render(graphics, Selected, Owner.Locked, false); radio2_23.Dispose();
                    graphics.FillEllipse(c223, radio_rec2_23);
                    graphics.DrawString("Mz", GH_FontServer.Standard, Brushes.Black, text_rec2_23);
                    ///******************************************************************************************
                    GH_Capsule radio3 = GH_Capsule.CreateCapsule(radio3_rec, GH_Palette.White, 2, 0);
                    radio3.Render(graphics, Selected, Owner.Locked, false); radio3.Dispose();

                    GH_Capsule radio3_11 = GH_Capsule.CreateCapsule(radio_rec3_11, GH_Palette.Black, 5, 5);
                    radio3_11.Render(graphics, Selected, Owner.Locked, false); radio3_11.Dispose();
                    graphics.FillEllipse(c311, radio_rec3_11);
                    graphics.DrawString("Value", GH_FontServer.Standard, Brushes.Black, text_rec3_11);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec11 = radio_rec_11;
                    RectangleF rec21 = radio_rec_21;
                    RectangleF rec211 = radio_rec2_11; RectangleF rec212 = radio_rec2_12; RectangleF rec213 = radio_rec2_13; RectangleF rec214 = radio_rec2_14;
                    RectangleF rec221 = radio_rec2_21; RectangleF rec222 = radio_rec2_22; RectangleF rec223 = radio_rec2_23;
                    RectangleF rec311 = radio_rec3_11;
                    if (rec11.Contains(e.CanvasLocation))
                    {
                        if (c11 == Brushes.Black) { c11 = Brushes.White; SetButton("c11", 0); }
                        else
                        { c11 = Brushes.Black; c21 = Brushes.White; SetButton("c11", 1); SetButton("c21", 0);}
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec21.Contains(e.CanvasLocation))
                    {
                        if (c21 == Brushes.Black) { c21 = Brushes.White; SetButton("c21", 0); }
                        else
                        { c21 = Brushes.Black; c11 = Brushes.White; SetButton("c11", 0); SetButton("c21", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    ///*************************************************************************************************************************************************
                    if (rec211.Contains(e.CanvasLocation))
                    {
                        if (c211 == Brushes.Black) { c211 = Brushes.White; SetButton("c211", 0); }
                        else { c211 = Brushes.Black; SetButton("c211", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec212.Contains(e.CanvasLocation))
                    {
                        if (c212 == Brushes.Black) { c212 = Brushes.White; SetButton("c212", 0); }
                        else { c212 = Brushes.Black; SetButton("c212", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec213.Contains(e.CanvasLocation))
                    {
                        if (c213 == Brushes.Black) { c213 = Brushes.White; SetButton("c213", 0); }
                        else { c213 = Brushes.Black; SetButton("c213", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec214.Contains(e.CanvasLocation))
                    {
                        if (c214 == Brushes.Black) { c214 = Brushes.White; SetButton("c214", 0); }
                        else { c214 = Brushes.Black; SetButton("c214", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec221.Contains(e.CanvasLocation))
                    {
                        if (c221 == Brushes.Black) { c221 = Brushes.White; SetButton("c221", 0); }
                        else { c221 = Brushes.Black; SetButton("c221", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec222.Contains(e.CanvasLocation))
                    {
                        if (c222 == Brushes.Black) { c222 = Brushes.White; SetButton("c222", 0); }
                        else { c222 = Brushes.Black; SetButton("c222", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec223.Contains(e.CanvasLocation))
                    {
                        if (c223 == Brushes.Black) { c223 = Brushes.White; SetButton("c223", 0); }
                        else { c223 = Brushes.Black; SetButton("c223", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    ///*************************************************************************************************************************************************
                    if (rec311.Contains(e.CanvasLocation))
                    {
                        if (c311 == Brushes.Black) { c311 = Brushes.White; SetButton("Value", 0); }
                        else { c311 = Brushes.Black; SetButton("Value", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}