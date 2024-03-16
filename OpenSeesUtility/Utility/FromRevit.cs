using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using System.Drawing;
using System.Windows.Forms;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using System.Diagnostics;
///****************************************

namespace FromRevit
{
    public class FromRevit : GH_Component
    {
        public static int on_off = 0;
        public static void SetButton(string s, int i)
        {
            if (s == "1")
            {
                on_off = i;
            }
        }
        public FromRevit()
          : base("FromRevit", "FromRevit",
              "Import Revit data to Grasshopper and convert for OpenSees",
              "OpenSees", "Utility")
        {
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("directoryname", "directory", "if nothing inputted, same directory of gh file is searched", GH_ParamAccess.item, "default");
            pManager.AddTextParameter("beamfilename", "beamfile", "must be same directory of gh file", GH_ParamAccess.item,"");
            pManager.AddTextParameter("columnfilename", "columnfile", "must be same directory of gh file", GH_ParamAccess.item,"");
            pManager.AddTextParameter("sectionlistname", "sectionlistname", "section list which has specified name will be created", GH_ParamAccess.item, "");
            pManager.AddTextParameter("userstring(sec)", "userstring(sec)", "user string for section number", GH_ParamAccess.item, "sec");
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("lines", "lines", "lines from revit", GH_ParamAccess.list);
            pManager.AddTextParameter("layers", "layers", "layers from revit", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string dir = ""; DA.GetData("directoryname", ref dir);
            string bname = ""; DA.GetData("beamfilename", ref bname); string cname = ""; DA.GetData("columnfilename", ref cname); string seclistname = ""; DA.GetData("sectionlistname", ref seclistname);
            if(seclistname=="none" | seclistname == "None") { seclistname = ""; }
            var secuserstring = "断面"; DA.GetData("userstring(sec)", ref secuserstring);
            var secname = new List<string>(); int nsec = 0; var sec_No = new List<int>();
            var layerlist = new List<string>(); var layerindex = new List<int>(); var read_or_write = 0;//0ならsectionlistを新たに作る，1なら既存のリストを使う
            var linetexts= new List<string>(); var userstrings = new List<string>(); var uservalues = new List<List<string>>();

            if (dir == "default")
            {
                dir = Directory.GetCurrentDirectory();
            }
            var doc = RhinoDoc.ActiveDoc;
            var objs = new List<Guid>(); var lines = new List<Line>(); var xyz = new List<Point3d>();
            StreamWriter w = new StreamWriter(@dir + "/temp",false); StreamReader r;
            if (seclistname != "")
            {
                if (File.Exists(@dir + "/" + seclistname)){ read_or_write = 1; }
                else { w.Close(); w = new StreamWriter(@dir + "/" + seclistname, false); }
            }
            if (seclistname != "" && read_or_write == 0) { w.WriteLine("No.,TYPE,P1,P2,P3,P4"); }
            else if (seclistname != "" && read_or_write == 1)
            {
                r = new StreamReader(@dir + "/" + seclistname);
                int k = 0;
                while (!r.EndOfStream)
                {
                    string line = r.ReadLine(); linetexts.Add(line);// CSVファイルの一行を読み込む
                    string[] values = line.Split(',');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    if (k != 0)
                    {
                        var type = values[1]; var p1 = values[2].ToString(); var p2 = values[3].ToString(); var p3 = values[4].ToString(); var p4 = values[5].ToString();
                        string sec = type + "-" + p1 + "x" + p2 + "x" + p3 + "x" + p4;
                        secname.Add(sec);
                        nsec += 1;
                    }
                    k += 1;
                }
                r.Close();
                w.Close(); w = new StreamWriter(@dir + "/" + seclistname, true);//追記用
            }
            int check = 0;//最初の追加かどうかのflag(追記するsection.csvの一番最初に改行を入れるため)
            if (bname != "")//0:Level,1:Family_Name,2:FamilyType_Name,3:Start_PointX,4:Start_PointY,5:Start_PointZ,6:End_PointX,7:End_PointY,8:End_PointZ,9:bf,10:d,11:kr,12:tf,13:tw,14:mat,15:angle,16:joint,17:Lby,18:Lbz
            {
                var filename = dir + "/" + bname;
                StreamReader sr = new StreamReader(@filename);// 読み込みたいCSVファイルのパスを指定して開く, System.Text.Encoding.GetEncoding("shift_jis")
                int k = 0;
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(',');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    if (k != 0)
                    {
                        var r1 = new Point3d(double.Parse(values[3]) / 1000.0, double.Parse(values[4]) / 1000.0, double.Parse(values[5]) / 1000.0);
                        var r2 = new Point3d(double.Parse(values[6]) / 1000.0, double.Parse(values[7]) / 1000.0, double.Parse(values[8]) / 1000.0);
                        lines.Add(new Line(r1, r2));
                        if (values[1].Contains("角形") == true)
                        {
                            var s1 = (double.Parse(values[9]) / 1000.0).ToString(); var s2 = (double.Parse(values[10]) / 1000.0).ToString(); var s3 = (double.Parse(values[11]) / 1000.0).ToString(); var s4 = (double.Parse(values[12]) / 1000.0).ToString();
                            string sec = "□-" + s1 + "x" + s2 + "x" + s3 + "x" + s4;
                            if (secname.Contains(sec) == false) { secname.Add(sec); if (read_or_write == 1 && check == 0) { w.WriteLine(""); check += 1; } if (seclistname != "") { var text = nsec.ToString() + ",□," + s1 + "," + s2 + "," + s3 + "," + s4; w.WriteLine(text); linetexts.Add(text); } nsec += 1; }
                            sec_No.Add(secname.IndexOf(sec));
                        }
                        else if (values[1].Contains("H") == true)
                        {
                            var s1 = (double.Parse(values[9]) / 1000.0).ToString(); var s2 = (double.Parse(values[10]) / 1000.0).ToString(); var s3 = (double.Parse(values[11]) / 1000.0).ToString(); var s4 = (double.Parse(values[12]) / 1000.0).ToString();
                            var sec = "H-" + s1 + "x" + s2 + "x" + s3 + "x" + s4;
                            if (secname.Contains(sec) == false) { secname.Add(sec); if (read_or_write == 1 && check == 0) { w.WriteLine(""); check += 1; } if (seclistname != "") { var text = nsec.ToString() + ",H," + s1 + "," + s2 + "," + s3 + "," + s4; w.WriteLine(text); linetexts.Add(text); } nsec += 1; }
                            sec_No.Add(secname.IndexOf(sec));
                        }
                        if (layerlist.Contains(values[0]) == false) { layerlist.Add(values[0]); }
                        layerindex.Add(layerlist.IndexOf(values[0]));

                        var uservalue = new List<string>();
                        for (int i = 13; i < values.Length; i++)
                        {
                            if (i == 17 || i == 18)
                            {
                                uservalue.Add((double.Parse(values[i]) / 1000.0).ToString());
                            }
                            else { uservalue.Add(values[i]); }
                        }
                        uservalues.Add(uservalue);
                    }
                    else
                    {
                        if (values.Length >= 14)
                        {
                            for (int i = 13; i< values.Length; i++)
                            {
                                userstrings.Add(values[i]);
                            }
                        }
                    }
                    k += 1;
                }
            }
            if (cname != "")
            {
                var filename = dir + "/" + cname;
                StreamReader sr = new StreamReader(@filename);// 読み込みたいCSVファイルのパスを指定して開く, System.Text.Encoding.GetEncoding("shift_jis")
                int k = 0;
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(',');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    if (k != 0)
                    {
                        var r1 = new Point3d(double.Parse(values[3]) / 1000.0, double.Parse(values[4]) / 1000.0, double.Parse(values[5]) / 1000.0);
                        var r2 = new Point3d(double.Parse(values[6]) / 1000.0, double.Parse(values[7]) / 1000.0, double.Parse(values[8]) / 1000.0);
                        lines.Add(new Line(r1, r2));
                        if (values[1].Contains("角形") == true)
                        {
                            var s1 = (double.Parse(values[9]) / 1000.0).ToString(); var s2 = (double.Parse(values[10]) / 1000.0).ToString(); var s3 = (double.Parse(values[11]) / 1000.0).ToString(); var s4 = (double.Parse(values[12]) / 1000.0).ToString();
                            string sec = "□-" + s1 + "x" + s2 + "x" + s3 + "x" + s4;
                            if (secname.Contains(sec) == false) { secname.Add(sec); if (read_or_write == 1 && check == 0) { w.WriteLine(""); check += 1; } if (seclistname != "") { var text = nsec.ToString() + ",□," + s1 + "," + s2 + "," + s3 + "," + s4; w.WriteLine(text); linetexts.Add(text); } nsec += 1; }
                            sec_No.Add(secname.IndexOf(sec));
                        }
                        else if (values[1].Contains("H") == true)
                        {
                            var s1 = (double.Parse(values[9]) / 1000.0).ToString(); var s2 = (double.Parse(values[10]) / 1000.0).ToString(); var s3 = (double.Parse(values[11]) / 1000.0).ToString(); var s4 = (double.Parse(values[12]) / 1000.0).ToString();
                            var sec = "H-" + s1 + "x" + s2 + "x" + s3 + "x" + s4;
                            if (secname.Contains(sec) == false) { secname.Add(sec); if (read_or_write == 1 && check == 0) { w.WriteLine(""); check += 1; } if (seclistname != "") { var text = nsec.ToString() + ",H," + s1 + "," + s2 + "," + s3 + "," + s4; w.WriteLine(text); linetexts.Add(text); } nsec += 1; }
                            sec_No.Add(secname.IndexOf(sec));
                        }
                        if (layerlist.Contains(values[0]) == false) { layerlist.Add(values[0]); }
                        layerindex.Add(layerlist.IndexOf(values[0]));

                        var uservalue = new List<string>();
                        for (int i = 13; i < values.Length; i++)
                        {
                            if (i == 17 || i == 18)
                            {
                                uservalue.Add((double.Parse(values[i]) / 1000.0).ToString());
                            }
                            else { uservalue.Add(values[i]); }
                        }
                        uservalues.Add(uservalue);
                    }
                    k += 1;
                }
            }
            w.Close();
            //最後の改行を削除///////////////////////////////////////////////////////////////////////////////
            w = new StreamWriter(@dir + "/" + seclistname, false); w.Write("No.,TYPE,P1,P2,P3,P4");
            for (int i = 1; i < linetexts.Count; i++){ w.Write(Environment.NewLine + linetexts[i]); }
            w.Close();
            /////////////////////////////////////////////////////////////////////////////////////////////////

            DA.SetDataList("lines", lines); DA.SetDataList("layers", layerlist);
            if (on_off == 1)
            {
                for (int i = 0; i < layerlist.Count; i++) { doc.Layers.Add(layerlist[i], Color.Black); }//レイヤ生成
                for (int e = 0; e < lines.Count; e++)//節点生成
                {
                    var r1 = lines[e].From; var r2 = lines[e].To; var l1 = 10.0; var l2 = 10.0;
                    for (int i = 0; i < xyz.Count; i++) { l1 = Math.Min(l1, (xyz[i] - r1).Length); }
                    if (l1 > 5e-3) { xyz.Add(r1); }
                    for (int i = 0; i < xyz.Count; i++) { l2 = Math.Min(l2, (xyz[i] - r2).Length); }
                    if (l2 > 5e-3) { xyz.Add(r2); }
                }
                for (int e = 0; e < lines.Count; e++)//交差判定を行い交差部で要素分割する
                {
                    var r1 = lines[e].From; var r2 = lines[e].To; var l0 = r2 - r1; var rc = new List<Point3d>(); var sec = sec_No[e];
                    var att = new ObjectAttributes();
                    att.SetUserString(secuserstring, sec.ToString());
                    for (int i = 0; i < userstrings.Count; i++)
                    {
                        if (uservalues[e][i]!="" && uservalues[e][i] != "rigid") { att.SetUserString(userstrings[i], uservalues[e][i]); }
                    }
                    att.LayerIndex = layerindex[e] + 1;
                    for (int i = 0; i < xyz.Count; i++)
                    {
                        var l1 = xyz[i] - r1;
                        if (l1.Length > 5e-3 && (r2 - xyz[i]).Length > 5e-3)//線分上に節点がいるかどうかチェック
                        {
                            if ((l0 / l0.Length - l1 / l1.Length).Length < 1e-5 && l0.Length- l1.Length>5e-3) { rc.Add(xyz[i]); }
                        }
                    }
                    if (rc.Count != 0)
                    {
                        var llist = new List<double>();
                        for (int i = 0; i < rc.Count; i++)
                        {
                            llist.Add((rc[i] - r1).Length);
                        }
                        int[] idx = Enumerable.Range(0, rc.Count).ToArray<int>();//r1とr2の間の点のソート
                        Array.Sort<int>(idx, (a, b) => llist[a].CompareTo(llist[b]));
                        var obj = doc.Objects.AddLine(r1, rc[idx[0]], att);
                        for (int i = 0; i < idx.Length - 1; i++)
                        {
                            obj = doc.Objects.AddLine(rc[idx[i]], rc[idx[i + 1]], att);
                        }
                        obj = doc.Objects.AddLine(rc[idx[idx.Length - 1]], r2, att);
                    }
                    else
                    {
                        var obj = doc.Objects.AddLine(r1, r2, att);
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
                return OpenSeesUtility.Properties.Resources.fromrevit;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("a53b4d40-1275-4746-b53e-0c737a37df70"); }
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
                    graphics.DrawString("Bake", GH_FontServer.Standard, Brushes.Black, text_rec1_1);
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