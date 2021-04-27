using System;
using System.IO;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using System.Drawing;
using System.Windows.Forms;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
///****************************************

namespace OpenSeesUtility
{
    public class ToRevit : GH_Component
    {
        public static int on_off = 0;
        public static void SetButton(string s, int i)
        {
            if (s == "1")
            {
                on_off = i;
            }
        }
        public ToRevit()
          : base("ToRevit", "ToRevit",
              "Export Opensees for Grasshopper data to Revit",
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
            pManager.AddTextParameter("beamfilename", "beamfile", "must be same directory of gh file", GH_ParamAccess.item, "");
            pManager.AddTextParameter("columnfilename", "columnfile", "must be same directory of gh file", GH_ParamAccess.item, "");
            pManager.AddTextParameter("beamlayer", "beamlayer", "beam layer names", GH_ParamAccess.list);
            pManager.AddTextParameter("columnlayer", "columnlayer", "column layer names", GH_ParamAccess.list);
            pManager.AddTextParameter("secname", "secname", "section name", GH_ParamAccess.list);///
            pManager.AddTextParameter("name mat", "name mat", "usertextname for material", GH_ParamAccess.item, "mat");
            pManager.AddTextParameter("name sec", "name sec", "usertextname for section", GH_ParamAccess.item, "sec");
            pManager.AddTextParameter("name angle", "name angle", "usertextname for code-angle", GH_ParamAccess.item, "angle");
            pManager.AddTextParameter("name joint", "name joint", "usertextname for pin-joint", GH_ParamAccess.item, "joint");
            pManager.AddTextParameter("name lby", "name lby", "usertextname for buckling length(local-y axis)", GH_ParamAccess.item, "lby");
            pManager.AddTextParameter("name lbz", "name lbz", "usertextname for buckling length(local-z axis)", GH_ParamAccess.item, "lbz");
            pManager.AddNumberParameter("parameter 1", "P1", "[■□HL[:B,〇●:R](DataList)", GH_ParamAccess.list);///1
            pManager.AddNumberParameter("parameter 2", "P2", "[■□HL[:D,〇:t,●:0](DataList)", GH_ParamAccess.list);///2
            pManager.AddNumberParameter("parameter 3", "P3", "[□HL[:tw,■●〇:0](DataList)", GH_ParamAccess.list);///3
            pManager.AddNumberParameter("parameter 4", "P4", "[□HL[:tf,■●〇:0](DataList)", GH_ParamAccess.list);///4
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
            string dir = ""; DA.GetData("directoryname", ref dir);
            string bname = ""; DA.GetData("beamfilename", ref bname); string cname = ""; DA.GetData("columnfilename", ref cname);
            List<string> beamlayer = new List<string>(); DA.GetDataList("beamlayer", beamlayer);
            List<string> columnlayer = new List<string>(); DA.GetDataList("columnlayer", columnlayer);
            List<string> secname = new List<string>(); DA.GetDataList("secname", secname);
            string name_mat = "mat"; string name_sec = "sec"; string name_angle = "angle"; string name_joint = "joint"; string name_lby = "lby"; string name_lbz = "lbz";
            DA.GetData("name mat", ref name_mat); DA.GetData("name sec", ref name_sec); DA.GetData("name angle", ref name_angle); DA.GetData("name joint", ref name_joint); DA.GetData("name lby", ref name_lby); DA.GetData("name lbz", ref name_lbz);
            List<double> P1 = new List<double>(); List<double> P2 = new List<double>(); List<double> P3 = new List<double>(); List<double> P4 = new List<double>();
            DA.GetDataList("parameter 1", P1); DA.GetDataList("parameter 2", P2); DA.GetDataList("parameter 3", P3); DA.GetDataList("parameter 4", P4);
            if (on_off == 1)
            {
                if (dir == "default")
                {
                    dir = Directory.GetCurrentDirectory();
                }
                var doc = RhinoDoc.ActiveDoc;
                var w1 = new StreamWriter(@dir + "/" + bname, false);
                if (bname != "") { w1.WriteLine("Layer,Family_Name,FamilyType_Name,Start_PointX,Start_PointY,Start_PointZ,End_PointX,End_PointY,End_PointZ,Ht,b,tr,tf,tw,mat,angle,joint,Lby,Lbz"); }
                for (int i = 0; i < beamlayer.Count; i++)
                {
                    var line = doc.Objects.FindByLayer(beamlayer[i]);
                    for (int j = 0; j < line.Length; j++)
                    {
                        var obj = line[j];
                        var l = (new ObjRef(obj)).Curve();
                        var r1 = l.PointAtStart; var r2 = l.PointAtEnd;
                        var x1 = r1.X * 1000; var y1 = r1.Y * 1000; var z1 = r1.Z * 1000;
                        var x2 = r2.X * 1000; var y2 = r2.Y * 1000; var z2 = r2.Z * 1000;
                        var mat = "0"; var sec = 0; var angle = "0.0"; var lby = (l.GetLength() * 1000).ToString(); var lbz = (l.GetLength()*1000).ToString(); var joint = "rigid";
                        var text = obj.Attributes.GetUserString(name_mat);//材料情報
                        if (text != null){ mat=text; }
                        text = obj.Attributes.GetUserString(name_sec);//断面情報
                        if (text != null) { sec=int.Parse(text); }
                        text = obj.Attributes.GetUserString(name_angle);//コードアングル情報
                        if (text != null){ angle=text; }
                        text = obj.Attributes.GetUserString(name_lby);//部材y軸方向座屈長さ情報
                        if (text != null){ lby= (float.Parse(text)*1000).ToString(); }
                        text = obj.Attributes.GetUserString(name_lbz);//部材z軸方向座屈長さ情報
                        if (text != null){ lbz= (float.Parse(text) * 1000).ToString(); }
                        text = obj.Attributes.GetUserString(name_joint);//材端ピン情報
                        if (text != null) { joint=text; }
                        var familyname = secname[sec];
                        var familytype = "";
                        if (familyname.Contains("H") == true) { familytype="H形鋼"; }
                        else if (familyname.Contains("□") == true) { familytype = "角形鋼管"; }
                        var p1 = P1[sec] * 1000; var p2 = P2[sec] * 1000; var p3 = P3[sec] * 1000; var p4 = P4[sec] * 1000;
                        w1.WriteLine(beamlayer[i] + "," + familytype + "," + familyname + "," + x1 + "," + y1 + "," + z1 + "," + x2 + "," + y2 + "," + z2 + "," + p2 + "," + p1 + "," + 0 + "," + p4 + "," + p3 + "," + mat + "," + angle + "," + joint + "," + lby + "," + lbz);
                    }
                }
                w1.Flush();
                var w2 = new StreamWriter(@dir + "/" + cname, false);
                if (cname != "") { w2.WriteLine("Layer,Family_Name,FamilyType_Name,Start_PointX,Start_PointY,Start_PointZ,End_PointX,End_PointY,End_PointZ,Ht,b,tr,tf,tw,mat,angle,joint,Lby,Lbz"); }
                for (int i = 0; i < columnlayer.Count; i++)
                {
                    var line = doc.Objects.FindByLayer(columnlayer[i]);
                    for (int j = 0; j < line.Length; j++)
                    {
                        var obj = line[j];
                        var l = (new ObjRef(obj)).Curve();
                        var r1 = l.PointAtStart; var r2 = l.PointAtEnd;
                        var x1 = r1.X * 1000; var y1 = r1.Y * 1000; var z1 = r1.Z * 1000;
                        var x2 = r2.X * 1000; var y2 = r2.Y * 1000; var z2 = r2.Z * 1000;
                        var mat = "0"; var sec = 0; var angle = "0.0"; var lby = (l.GetLength() * 1000).ToString(); var lbz = (l.GetLength() * 1000).ToString(); var joint = "rigid";
                        var text = obj.Attributes.GetUserString(name_mat);//材料情報
                        if (text != null) { mat = text; }
                        text = obj.Attributes.GetUserString(name_sec);//断面情報
                        if (text != null) { sec = int.Parse(text); }
                        text = obj.Attributes.GetUserString(name_angle);//コードアングル情報
                        if (text != null) { angle = text; }
                        text = obj.Attributes.GetUserString(name_lby);//部材y軸方向座屈長さ情報
                        if (text != null) { lby = (float.Parse(text) * 1000).ToString(); }
                        text = obj.Attributes.GetUserString(name_lbz);//部材z軸方向座屈長さ情報
                        if (text != null) { lbz = (float.Parse(text) * 1000).ToString(); }
                        text = obj.Attributes.GetUserString(name_joint);//材端ピン情報
                        if (text != null) { joint = text; }
                        var familyname = secname[sec];
                        var familytype = "";
                        if (familyname.Contains("H") == true) { familytype = "H形鋼"; }
                        else if (familyname.Contains("□") == true) { familytype = "角形鋼管"; }
                        var p1 = P1[sec] * 1000; var p2 = P2[sec] * 1000; var p3 = P3[sec] * 1000; var p4 = P4[sec] * 1000;
                        w2.WriteLine(columnlayer[i] + "," + familytype + "," + familyname + "," + x1 + "," + y1 + "," + z1 + "," + x2 + "," + y2 + "," + z2 + "," + p2 + "," + p1 + "," + 0 + "," + p4 + "," + p3 + "," + mat + "," + angle + "," + joint + "," + lby + "," + lbz);
                    }
                }
                w2.Flush();
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
                return OpenSeesUtility.Properties.Resources.torevit;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("402baece-a417-426a-8aa3-eb1e851df39b"); }
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
                    graphics.DrawString("Export", GH_FontServer.Standard, Brushes.Black, text_rec1_1);
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