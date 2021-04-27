using System;
using System.IO;
using System.Collections.Generic;

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

namespace OpenSeesUtility
{
    public class SteelBar : GH_Component
    {
        public static int bar_No = 0; public static int bar_name = 0; public static int bar_list = 0;
        public static void SetButton(string s, int i)
        {
            if (s == "No.")
            {
                bar_No = i;
            }
            else if (s == "name")
            {
                bar_name = i;
            }
            else if (s == "list")
            {
                bar_list = i;
            }
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        public SteelBar()
          : base("Set reinforcement", "SteelBar",
              "Reinforcement information for RC",
              "OpenSees", "PreProcess")
        {
        }
        public override bool IsPreviewCapable { get { return true; } }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("filename", "filename", "input csv file path", GH_ParamAccess.item, "reinforcementlist.csv");///
            pManager.AddNumberParameter("R", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("IJ", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("bar", "bar", "[int,int,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("index", "index", "[...](element No. List to show)", GH_ParamAccess.list);///
            pManager[1].Optional = true; pManager[2].Optional = true; pManager[3].Optional = true; pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("barT1", "barT1", "Steel bars at the top", GH_ParamAccess.tree);
            pManager.AddNumberParameter("barT2", "barT2", "Steel bars at the second top", GH_ParamAccess.tree);
            pManager.AddNumberParameter("barB1", "barB1", "Steel bars at the bottom", GH_ParamAccess.tree);
            pManager.AddNumberParameter("barB2", "barB2", "Steel bars at the second bottom", GH_ParamAccess.tree);
            pManager.AddTextParameter("name", "name", "name of section", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Number> barT1 = new GH_Structure<GH_Number>(); GH_Structure<GH_Number> barT2 = new GH_Structure<GH_Number>(); GH_Structure<GH_Number> barB1 = new GH_Structure<GH_Number>(); GH_Structure<GH_Number> barB2 = new GH_Structure<GH_Number>();//[[i端主筋本数,主筋径,中央主筋本数,主筋径,j端主筋本数,主筋径],...]
            string filename = "reinforcementlist.csv"; DA.GetData("filename", ref filename); var name = new List<string>();
            if (filename != " ")
            {
                StreamReader sr = new StreamReader(@filename);// 読み込みたいCSVファイルのパスを指定して開く
                int k = 0;
                int k1 = 0; int k2 = 0; int k3 = 0; int k4 = 0;　var similar = 0;
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    if (k != 0)
                    {
                        string[] values = line.Split(',');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                        if ((k - 1) % 4 == 0)//上端筋1段目
                        {
                            var ni = 0.0; var nc = 0.0; var nj = 0.0; var Di = 0.0; var Dc = 0.0; var Dj = 0.0;
                            var si = 0.0; var sc = 0.0; var sj = 0.0; var Si = 0.0; var Sc = 0.0; var Sj = 0.0; var pi = 0.0; var pc = 0.0; var pj = 0.0; var kaburi = 0.0; var B = 0.0; var D= 0.0;
                            if (values[1] != "" && values[1] != "0" && String.IsNullOrEmpty(values[1]) == false) { ni = double.Parse(values[1]); Di = double.Parse(values[2]); }
                            if (values[3] == "" || values[3] == "0" || String.IsNullOrEmpty(values[3]) == true) { nc = ni; nj = ni; Dc = Di; Dj = Di; similar = 1; }//全断面同一配筋の時
                            else { nc = double.Parse(values[3]); Dc = double.Parse(values[4]); nj = double.Parse(values[5]); Dj = double.Parse(values[6]); similar = 0; }
                            if (values[7] != "" && values[7] != "0" && String.IsNullOrEmpty(values[7]) == false)
                            {
                                si = double.Parse(values[7]); Si = double.Parse(values[8]); pi = double.Parse(values[9]);
                                if (values[10] == "" || values[10] == "0" || String.IsNullOrEmpty(values[10]) == false) { sc = si; sj = si; Sc = Si; Sj = Si; pc = pi; pj = pi; }//全断面同一配筋の時
                                else { sc = double.Parse(values[10]); Sc = double.Parse(values[11]); pc = double.Parse(values[12]); sj = double.Parse(values[13]); Sj = double.Parse(values[14]); pj = double.Parse(values[15]); }
                            }
                            kaburi= double.Parse(values[16]); B = double.Parse(values[17]); D = double.Parse(values[18]);
                            var bar = new List<GH_Number>(); bar.Add(new GH_Number(ni)); bar.Add(new GH_Number(Di)); bar.Add(new GH_Number(nc)); bar.Add(new GH_Number(Dc)); bar.Add(new GH_Number(nj)); bar.Add(new GH_Number(Dj)); bar.Add(new GH_Number(si)); bar.Add(new GH_Number(Si)); bar.Add(new GH_Number(pi)); bar.Add(new GH_Number(sc)); bar.Add(new GH_Number(Sc)); bar.Add(new GH_Number(pc)); bar.Add(new GH_Number(sj)); bar.Add(new GH_Number(Sj)); bar.Add(new GH_Number(pj)); bar.Add(new GH_Number(kaburi)); bar.Add(new GH_Number(B)); bar.Add(new GH_Number(D));
                            barT1.AppendRange(bar, new GH_Path(k1)); k1 += 1;
                        }
                        else if((k - 1) % 4 == 1)//上端筋2段目
                        {
                            var ni = 0.0; var nc = 0.0; var nj = 0.0; var Di = 0.0; var Dc = 0.0; var Dj = 0.0;
                            if (values[1] != "" && values[1] != "0" && String.IsNullOrEmpty(values[1]) == false) { ni = double.Parse(values[1]); Di = double.Parse(values[2]); }
                            if (similar == 1) { nc = ni; nj = ni; Dc = Di; Dj = Di; }//全断面同一配筋の時
                            else
                            {
                                if (values[3] != "") { nc = double.Parse(values[3]); }
                                if (values[4] != "") { Dc = double.Parse(values[4]); }
                                if (values[5] != "") { nj = double.Parse(values[5]); }
                                if (values[6] != "") { Dj = double.Parse(values[6]); }
                            }
                            var bar = new List<GH_Number>(); bar.Add(new GH_Number(ni)); bar.Add(new GH_Number(Di)); bar.Add(new GH_Number(nc)); bar.Add(new GH_Number(Dc)); bar.Add(new GH_Number(nj)); bar.Add(new GH_Number(Dj));
                            for (int i = 7; i <= 15; i++) { bar.Add(new GH_Number(0)); }
                            if (values[16] != "") { bar.Add(new GH_Number(double.Parse(values[16]))); }//柱用(下端のかぶりを上端と変える場合)
                            else { bar.Add(barT1[k1-1][15]); }
                            barT2.AppendRange(bar, new GH_Path(k2)); k2 += 1;
                        }
                        else if ((k - 1) % 4 == 2)//下端筋1段目
                        {
                            name.Add(values[0]);
                            var ni = 0.0; var nc = 0.0; var nj = 0.0; var Di = 0.0; var Dc = 0.0; var Dj = 0.0;
                            var si = 0.0; var sc = 0.0; var sj = 0.0; var Si = 0.0; var Sc = 0.0; var Sj = 0.0; var pi = 0.0; var pc = 0.0; var pj = 0.0; var kaburi = 0.0;
                            if (values[1] != "" & values[1] != "0" && String.IsNullOrEmpty(values[1]) == false) { ni = double.Parse(values[1]); Di = double.Parse(values[2]); }
                            if (values[3] == "" || values[3] == "0" || String.IsNullOrEmpty(values[3]) == true) { nc = ni; nj = ni; Dc = Di; Dj = Di; similar = 1; }//全断面同一配筋の時
                            else { nc = double.Parse(values[3]); Dc = double.Parse(values[4]); nj = double.Parse(values[5]); Dj = double.Parse(values[6]); similar = 0; }
                            if (values[7] != "" && values[7] != "0" && String.IsNullOrEmpty(values[7]) == false)
                            {
                                si = double.Parse(values[7]); Si = double.Parse(values[8]); pi = double.Parse(values[9]);
                            }
                            else
                            {
                                si = barT1[k1 - 1][6].Value; Si = barT1[k1 - 1][7].Value; pi = barT1[k1 - 1][8].Value;
                            }
                            if (values[10] == "" || values[10] == "0" || String.IsNullOrEmpty(values[10]) == false) { sc = si; sj = si; Sc = Si; Sj = Si; pc = pi; pj = pi; }//全断面同一配筋の時
                            else { sc = double.Parse(values[10]); Sc = double.Parse(values[11]); pc = double.Parse(values[12]); sj = double.Parse(values[13]); Sj = double.Parse(values[14]); pj = double.Parse(values[15]); }
                            kaburi = double.Parse(values[16]);
                            var bar = new List<GH_Number>(); bar.Add(new GH_Number(ni)); bar.Add(new GH_Number(Di)); bar.Add(new GH_Number(nc)); bar.Add(new GH_Number(Dc)); bar.Add(new GH_Number(nj)); bar.Add(new GH_Number(Dj)); bar.Add(new GH_Number(si)); bar.Add(new GH_Number(Si)); bar.Add(new GH_Number(pi)); bar.Add(new GH_Number(sc)); bar.Add(new GH_Number(Sc)); bar.Add(new GH_Number(pc)); bar.Add(new GH_Number(sj)); bar.Add(new GH_Number(Sj)); bar.Add(new GH_Number(pj)); bar.Add(new GH_Number(kaburi));
                            barB1.AppendRange(bar, new GH_Path(k3)); k3 += 1;
                        }
                        else if ((k - 1) % 4 == 3)//下端筋2段目
                        {
                            var ni = 0.0; var nc = 0.0; var nj = 0.0; var Di = 0.0; var Dc = 0.0; var Dj = 0.0;
                            if (values[1] != "" && values[1] != "0" && String.IsNullOrEmpty(values[1]) == false) { ni = double.Parse(values[1]); Di = double.Parse(values[2]); }
                            if (similar == 1) { nc = ni; nj = ni; Dc = Di; Dj = Di; }//全断面同一配筋の時
                            else
                            {
                                if (values[3] != "") { nc = double.Parse(values[3]); }
                                if (values[4] != "") { Dc = double.Parse(values[4]); }
                                if (values[5] != "") { nj = double.Parse(values[5]); }
                                if (values[6] != "") { Dj = double.Parse(values[6]); }
                            }
                            var bar = new List<GH_Number>(); bar.Add(new GH_Number(ni)); bar.Add(new GH_Number(Di)); bar.Add(new GH_Number(nc)); bar.Add(new GH_Number(Dc)); bar.Add(new GH_Number(nj)); bar.Add(new GH_Number(Dj));
                            for (int i = 7; i <= 15; i++) { bar.Add(new GH_Number(0)); }
                            if (values[16] != "") { bar.Add(new GH_Number(double.Parse(values[16]))); }//柱用(左端のかぶりを右端と変える場合)
                            else { bar.Add(barB1[k3 - 1][15]); }
                            barB2.AppendRange(bar, new GH_Path(k4)); k4 += 1;
                        }
                    }
                    k += 1;
                }
                DA.SetDataTree(0, barT1);
                DA.SetDataTree(1, barT2);
                DA.SetDataTree(2, barB1);
                DA.SetDataTree(3, barB2);
                DA.SetDataList(4, name);
                if (!DA.GetDataTree("R", out GH_Structure<GH_Number> _R)) return; var R = _R.Branches;
                if (!DA.GetDataTree("IJ", out GH_Structure<GH_Number> _IJ)) return; var IJ = _IJ.Branches;
                var barNo = new List<double>(); var index = new List<double>();
                if (!DA.GetDataList("bar", barNo)) return; if (!DA.GetDataList("index", index)) return;
                
                if (index.Count == 0)
                {
                    index = new List<double>();
                    for (int e = 0; e < IJ.Count; e++) { index.Add(e); }
                }
                for (int i = 0; i < index.Count; i++)
                {
                    int e = (int)barNo[(int)index[i]];
                    _bar.Add(e.ToString());
                    _name.Add(name[e]);
                    var ni = (int)IJ[(int)index[i]][0].Value; var nj = (int)IJ[(int)index[i]][1].Value;
                    _pt.Add(new Point3d((R[ni][0].Value + R[nj][0].Value) / 2.0, (R[ni][1].Value + R[nj][1].Value) / 2.0, (R[ni][2].Value + R[nj][2].Value) / 2.0));
                    var color = new ColorHSL((1 - Math.Min((float)e/barNo.Count, 1.0)) * 1.9 / 3.0, 1, 0.5);
                    _c.Add(color);
                    var nl1 = barT1[e][0].Value; var dl1 = barT1[e][1].Value;
                    var nc1 = barT1[e][2].Value; var dc1 = barT1[e][3].Value;
                    var nr1 = barT1[e][4].Value; var dr1 = barT1[e][5].Value;
                    var Nl1 = barT1[e][6].Value; var Dl1 = barT1[e][7].Value; var Pl1 = barT1[e][8].Value;
                    var Nc1 = barT1[e][9].Value; var Dc1 = barT1[e][10].Value; var Pc1 = barT1[e][11].Value;
                    var Nr1 = barT1[e][12].Value; var Dr1 = barT1[e][13].Value; var Pr1 = barT1[e][14].Value;
                    var nl2 = barT2[e][0].Value; var dl2 = barT2[e][1].Value;
                    var nc2 = barT2[e][2].Value; var dc2 = barT2[e][3].Value;
                    var nr2 = barT2[e][4].Value; var dr2 = barT2[e][5].Value;
                    var nl3 = barB1[e][0].Value; var dl3 = barB1[e][1].Value;
                    var nc3 = barB1[e][2].Value; var dc3 = barB1[e][3].Value;
                    var nr3 = barB1[e][4].Value; var dr3 = barB1[e][5].Value;
                    var Nl3 = barB1[e][6].Value; var Dl3 = barB1[e][7].Value; var Pl3 = barB1[e][8].Value;
                    var Nc3 = barB1[e][9].Value; var Dc3 = barB1[e][10].Value; var Pc3 = barB1[e][11].Value;
                    var Nr3 = barB1[e][12].Value; var Dr3 = barB1[e][13].Value; var Pr3 = barB1[e][14].Value;
                    var nl4 = barB2[e][0].Value; var dl4 = barB2[e][1].Value;
                    var nc4 = barB2[e][2].Value; var dc4 = barB2[e][3].Value;
                    var nr4 = barB2[e][4].Value; var dr4 = barB2[e][5].Value;
                    var texttl = ((int)nl1).ToString(); var texttc = ((int)nc1).ToString(); var texttr = ((int)nr1).ToString();
                    if (nl2 == 0) { texttl += "-D" + ((int)dl1).ToString(); } else { texttl += "/" + ((int)nl2).ToString() + "-D" + ((int)dl1).ToString(); }
                    if (nc2 == 0) { texttc += "-D" + ((int)dc1).ToString(); } else { texttc += "/" + ((int)nc2).ToString() + "-D" + ((int)dc1).ToString(); }
                    if (nr2 == 0) { texttr += "-D" + ((int)dr1).ToString(); } else { texttr += "/" + ((int)nr2).ToString() + "-D" + ((int)dr1).ToString(); }
                    var textbl = ((int)nl3).ToString(); var textbc = ((int)nc3).ToString(); var textbr = ((int)nr3).ToString();
                    if (nl4 == 0) { textbl += "-D" + ((int)dl3).ToString(); } else { textbl += "/" + ((int)nl4).ToString() + "-D" + ((int)dl3).ToString(); }
                    if (nc4 == 0) { textbc += "-D" + ((int)dc3).ToString(); } else { textbc += "/" + ((int)nc4).ToString() + "-D" + ((int)dc3).ToString(); }
                    if (nr4 == 0) { textbr += "-D" + ((int)dr3).ToString(); } else { textbr += "/" + ((int)nr4).ToString() + "-D" + ((int)dr3).ToString(); }
                    var texttl2 = ((int)Nl1).ToString() + "-D" + ((int)Dl1).ToString() + "@" + ((int)Pl1).ToString();
                    var texttc2 = ((int)Nc1).ToString() + "-D" + ((int)Dc1).ToString() + "@" + ((int)Pc1).ToString();
                    var texttr2 = ((int)Nr1).ToString() + "-D" + ((int)Dr1).ToString() + "@" + ((int)Pr1).ToString();
                    _list.Add(texttl + "   " + texttc + "   " + texttr + "\n" + textbl + "   " + textbc + "   " + textbr + "\n" + texttl2 + "   " + texttc2 + "   " + texttr2);
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
                return OpenSeesUtility.Properties.Resources.bar;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("c92b76a4-e9b7-4baf-9278-cc6aacdb5de9"); }
        }
        ///ここからカスタム関数群********************************************************************************
        private readonly List<Point3d> _pt = new List<Point3d>();
        private readonly List<String> _bar = new List<String>();
        private readonly List<String> _name = new List<String>();
        private readonly List<String> _list = new List<String>();
        private readonly List<Color> _c = new List<Color>();
        protected override void BeforeSolveInstance()
        { _bar.Clear(); _pt.Clear(); _name.Clear(); _list.Clear(); _c.Clear(); }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            args.Viewport.GetFrustumFarPlane(out Plane plane);
            RhinoViewport viewport = args.Viewport;
            for (int i = 0; i < _pt.Count; i++)
            {
                if (bar_No == 1)
                {
                    var size = VisualizeModel.VisualizeModel.fontsize; Point3d point = _pt[i]; plane.Origin = point;
                    viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                    args.Display.Draw3dText(_bar[i], _c[i], plane, size, "", false, false, Rhino.DocObjects.TextHorizontalAlignment.Center, Rhino.DocObjects.TextVerticalAlignment.Bottom);
                }
                if (bar_name == 1)
                {
                    var size = VisualizeModel.VisualizeModel.fontsize; Point3d point = _pt[i]; plane.Origin = point;
                    viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                    args.Display.Draw3dText(_name[i], _c[i], plane, size, "", false, false, Rhino.DocObjects.TextHorizontalAlignment.Center, Rhino.DocObjects.TextVerticalAlignment.Top);
                }
                if (bar_list == 1)
                {
                    var size = VisualizeModel.VisualizeModel.fontsize; Point3d point = _pt[i]; plane.Origin = point;
                    viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                    args.Display.Draw3dText(_list[i], _c[i], plane, size, "", false, false, Rhino.DocObjects.TextHorizontalAlignment.Center, Rhino.DocObjects.TextVerticalAlignment.Top);
                }
            }
        }
        ///ここからGUIの作成*****************************************************************************************
        internal class CustomGUI : GH_ComponentAttributes
        {
            internal CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle radio_rec;
            private Rectangle radio_rec_1; private Rectangle text_rec_1;
            private Rectangle radio_rec_2; private Rectangle text_rec_2;
            private Rectangle radio_rec_3; private Rectangle text_rec_3;
            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int radi1 = 7; int radi2 = 4; int subwidth = 36; int textheight = 20;
                int pitchx = 6;
                
                radio_rec = global_rec;
                radio_rec.Y = global_rec.Bottom;

                radio_rec_1 = radio_rec; radio_rec_1.Height = radi1; radio_rec_1.Width = radi1; radio_rec_1.Y += radi2; radio_rec_1.X += pitchx / 2;
                text_rec_1 = radio_rec_1; text_rec_1.X += pitchx; text_rec_1.Y -= radi2; text_rec_1.Height = textheight; text_rec_1.Width = subwidth;

                radio_rec_2 = radio_rec_1; radio_rec_2.X += text_rec_1.Width + radi1;
                text_rec_2 = text_rec_1; text_rec_2.X += text_rec_1.Width + radi1;

                radio_rec_3 = radio_rec_2; radio_rec_3.X += text_rec_2.Width + radi1;
                text_rec_3 = text_rec_2; text_rec_3.X += text_rec_2.Width + radi1;


                radio_rec.Height = radio_rec_1.Bottom - global_rec.Bottom + radi2;
                global_rec.Height += radio_rec.Height;

                Bounds = global_rec;
            }
            Brush c1 = Brushes.White; Brush c2 = Brushes.White; Brush c3 = Brushes.White;
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

                    GH_Capsule radio_1 = GH_Capsule.CreateCapsule(radio_rec_1, GH_Palette.Black, 5, 5);
                    radio_1.Render(graphics, Selected, Owner.Locked, false); radio_1.Dispose();
                    graphics.FillEllipse(c1, radio_rec_1);
                    graphics.DrawString("No.", GH_FontServer.Standard, Brushes.Black, text_rec_1);

                    GH_Capsule radio_2 = GH_Capsule.CreateCapsule(radio_rec_2, GH_Palette.Black, 5, 5);
                    radio_2.Render(graphics, Selected, Owner.Locked, false); radio_2.Dispose();
                    graphics.FillEllipse(c2, radio_rec_2);
                    graphics.DrawString("name", GH_FontServer.Standard, Brushes.Black, text_rec_2);

                    GH_Capsule radio_3 = GH_Capsule.CreateCapsule(radio_rec_3, GH_Palette.Black, 5, 5);
                    radio_3.Render(graphics, Selected, Owner.Locked, false); radio_3.Dispose();
                    graphics.FillEllipse(c3, radio_rec_3);
                    graphics.DrawString("list", GH_FontServer.Standard, Brushes.Black, text_rec_3);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec1 = radio_rec_1; RectangleF rec2 = radio_rec_2; RectangleF rec3 = radio_rec_3;
                    if (rec1.Contains(e.CanvasLocation))
                    {
                        if (c1 == Brushes.White) { c1 = Brushes.Black; SetButton("No.", 1); }
                        else { c1 = Brushes.White; SetButton("No.", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec2.Contains(e.CanvasLocation))
                    {
                        if (c2 == Brushes.White) { c2 = Brushes.Black; SetButton("name", 1); }
                        else { c2 = Brushes.White; SetButton("name", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec3.Contains(e.CanvasLocation))
                    {
                        if (c3 == Brushes.White) { c3 = Brushes.Black; SetButton("list", 1); }
                        else { c3 = Brushes.White; SetButton("list", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}