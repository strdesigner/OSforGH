using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.DocObjects;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using System.Drawing;
using System.Windows.Forms;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
///****************************************

namespace Wick
{
    public class Wick : GH_Component
    {
        public static int x_guideline = 0; public static int y_guideline = 0; public static int z_guideline = 0; static double fontsize;
        public static void SetButton(string s, int i)
        {
            if (s == "x")
            {
                x_guideline = i;
            }
            else if (s == "y")
            {
                y_guideline = i;
            }
            else if (s == "z")
            {
                z_guideline = i;
            }
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public Wick()
          : base("VisualizeWickLine", "Wick",
              "Display wick line of structural model",
              "OpenSees", "Visualization")
        {
        }
        public override bool IsPreviewCapable { get { return true; } }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("x", "x", "x coordinate of guide lines", GH_ParamAccess.list,new List<double> { 0.0 });///
            pManager.AddNumberParameter("y", "y", "y coordinate of guide lines", GH_ParamAccess.list, new List<double> { 0.0 });///
            pManager.AddNumberParameter("z", "z", "distance between origin and extra guide lines", GH_ParamAccess.list, new List<double> { 0.0 });///
            pManager.AddNumberParameter("h", "h", "h coordinate of guide lines", GH_ParamAccess.list, new List<double> { 0.0 });///
            pManager.AddNumberParameter("angle", "angle", "angle of extra guide lines", GH_ParamAccess.item, 0);///
            pManager.AddNumberParameter("offsetx", "offsetx", "offset for x", GH_ParamAccess.item, 1.0);///
            pManager.AddNumberParameter("offsety", "offsety", "offset for y", GH_ParamAccess.item, 1.0);///
            pManager.AddNumberParameter("offsetz", "offsetz", "offset for z", GH_ParamAccess.item, 1.0);///
            pManager.AddTextParameter("xlabel", "xlabel", "[label1, label2...](if default, X0,X1,..)", GH_ParamAccess.list, new List<string> { "default" });///
            pManager.AddTextParameter("ylabel", "ylabel", "[label1, label2...](if default, Y0,Y1,..)", GH_ParamAccess.list, new List<string> { "default" });///
            pManager.AddTextParameter("zlabel", "zlabel", "[label1, label2...](if default, Z0,Z1,..)", GH_ParamAccess.list, new List<string> { "default" });///
            pManager.AddNumberParameter("fontsize", "FS", "font size for display texts", GH_ParamAccess.item, 20.0);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("l", "l", "wick line", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var x = new List<double>(); var y = new List<double>(); var z = new List<double>(); var h = new List<double>(); var xlabel = new List<string>(); var ylabel = new List<string>(); var zlabel = new List<string>(); var angle = 0.0;
            var offsetx = 1.0; var offsety = 1.0; var offsetz = 1.0;
            DA.GetDataList("x", x); DA.GetDataList("y", y); DA.GetDataList("z", z); DA.GetDataList("h", h); DA.GetDataList("xlabel", xlabel); DA.GetDataList("ylabel", ylabel); DA.GetDataList("zlabel", zlabel); DA.GetData("offsetx", ref offsetx); DA.GetData("offsety", ref offsety); DA.GetData("offsetz", ref offsetz); DA.GetData("fontsize", ref fontsize); DA.GetData("angle", ref angle);
            var xmax = x.Max(); var xmin = x.Min(); var ymax = y.Max(); var ymin = y.Min();
            var normal = new Vector3d(Math.Cos(angle / 180 * Math.PI), Math.Sin(angle / 180 * Math.PI), 0);
            var direction = new Vector3d(Math.Cos((angle + 90) / 180 * Math.PI), Math.Sin((angle + 90) / 180 * Math.PI), 0);
            var vecmin = new Vector3d(xmin, ymin, 0); var vecmax = new Vector3d(xmax, ymax, 0);
            var d1 = (vecmin + Vector3d.Multiply(-vecmin, normal) * normal).Length; var d2 = (vecmax + Vector3d.Multiply(-vecmax, normal) * normal).Length;
            var wicklines = new List<Line>();
            for (int i = 0; i < h.Count; i++)
            {
                for (int j = 0; j < x.Count; j++)
                {
                    var l = new Line(new Point3d(x[j], ymin - offsetx, h[i]), new Point3d(x[j], ymax + offsetx, h[i]));
                    _xline.Add(l);
                    if (xlabel[0] == "default") { _xlabel.Add("X" + j.ToString()); }
                    else { _xlabel.Add(xlabel[j]); }
                    wicklines.Add(l);
                }
                for (int j = 0; j < y.Count; j++)
                {
                    var l = new Line(new Point3d(xmin - offsety, y[j], h[i]), new Point3d(xmax + offsety, y[j], h[i]));
                    _yline.Add(l);
                    if (ylabel[0] == "default") { _ylabel.Add("Y" + j.ToString()); }
                    else { _ylabel.Add(ylabel[j]); }
                    wicklines.Add(l);
                }
                if (angle != 0)
                {
                    for (int j = 0; j < z.Count; j++)
                    {
                        var vec1 = normal * z[j] - direction * (offsetz + d1); var vec2 = normal * z[j] + direction * (offsetz + d2);
                        var l = new Line(new Point3d(vec1[0], vec1[1], h[i]), new Point3d(vec2[0], vec2[1], h[i]));
                        _zline.Add(l);
                        if (zlabel[0] == "default") { _zlabel.Add("Z" + j.ToString()); }
                        else { _zlabel.Add(zlabel[j]); }
                        wicklines.Add(l);
                    }
                }
            }
            DA.SetDataList("l",wicklines);
        }

        protected override System.Drawing.Bitmap Icon { get { return OpenSeesUtility.Properties.Resources.wick; } }
        public override Guid ComponentGuid { get { return new Guid("094e691b-703c-4bab-965e-f787140a4b4f"); } }
        ///ここからカスタム関数群********************************************************************************
        private readonly List<Line> _xline = new List<Line>();
        private readonly List<Line> _yline = new List<Line>();
        private readonly List<Line> _zline = new List<Line>();
        private readonly List<string> _xlabel = new List<string>();
        private readonly List<string> _ylabel = new List<string>();
        private readonly List<string> _zlabel = new List<string>();
        protected override void BeforeSolveInstance()
        {
            _xline.Clear();
            _yline.Clear();
            _zline.Clear();
            _xlabel.Clear();
            _ylabel.Clear();
            _zlabel.Clear();
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            args.Viewport.GetFrustumFarPlane(out Plane plane);
            Rhino.Display.RhinoViewport viewport = args.Viewport;
            //X軸
            for (int i = 0; i < _xline.Count; i++)
            {
                if (x_guideline == 1)
                {
                    args.Display.DrawPatternedLine(_xline[i], Color.Black, 0x00001111, 1);
                    double size = fontsize; Point3d point = _xline[i].From; plane.Origin = point;
                    viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                    Rhino.Display.Text3d drawText = new Rhino.Display.Text3d(_xlabel[i], plane, size);
                    drawText.HorizontalAlignment = TextHorizontalAlignment.Center; drawText.VerticalAlignment = TextVerticalAlignment.Top;
                    args.Display.Draw3dText(drawText, Color.Black);
                    drawText.Dispose();
                }
            }
            //Y軸
            for (int i = 0; i < _yline.Count; i++)
            {
                if (y_guideline == 1)
                {
                    args.Display.DrawPatternedLine(_yline[i], Color.Black, 0x00001111, 1);
                    double size = fontsize; Point3d point = _yline[i].From; plane.Origin = point;
                    viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                    Rhino.Display.Text3d drawText = new Rhino.Display.Text3d(_ylabel[i], plane, size);
                    drawText.HorizontalAlignment = TextHorizontalAlignment.Right; drawText.VerticalAlignment = TextVerticalAlignment.Middle;
                    args.Display.Draw3dText(drawText, Color.Black);
                    drawText.Dispose();
                }
            }
            //Z軸
            for (int i = 0; i < _zline.Count; i++)
            {
                if (z_guideline == 1)
                {
                    args.Display.DrawPatternedLine(_zline[i], Color.Black, 0x00001111, 1);
                    double size = fontsize; Point3d point = _zline[i].From; plane.Origin = point;
                    viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                    Rhino.Display.Text3d drawText = new Rhino.Display.Text3d(_zlabel[i], plane, size);
                    drawText.HorizontalAlignment = TextHorizontalAlignment.Right; drawText.VerticalAlignment = TextVerticalAlignment.Middle;
                    args.Display.Draw3dText(drawText, Color.Black);
                    drawText.Dispose();
                }
            }
        }///ここからGUIの作成*****************************************************************************************
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
                int height = 14; int radi1 = 7; int radi2 = 8;
                int textheight = 14;
                global_rec.Height += height;

                radio_rec = global_rec;
                radio_rec.Y = radio_rec.Bottom - height;
                radio_rec.Height = 14;

                text_rec_1 = radio_rec; text_rec_1.X += 5;
                text_rec_1.Height = textheight; text_rec_1.Width = radi2;
                radio_rec_1 = text_rec_1; radio_rec_1.Width = radi1; radio_rec_1.X += radi2; radio_rec_1.Height = radi1;
                text_rec_2 = radio_rec_1; text_rec_2.Width = radi2; text_rec_2.X += radi1 * 2; text_rec_2.Height = textheight;
                radio_rec_2 = text_rec_2; radio_rec_2.Width = radi1; radio_rec_2.X += radi2; radio_rec_2.Height = radi1;
                text_rec_3 = radio_rec_2; text_rec_3.Width = radi2; text_rec_3.X += radi1 * 2; text_rec_3.Height = textheight;
                radio_rec_3 = text_rec_3; radio_rec_3.Width = radi1; radio_rec_3.X += radi2; radio_rec_3.Height = radi1;
                radio_rec_1.Y += 4; radio_rec_2.Y += 4; radio_rec_3.Y += 4;
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
                    graphics.DrawString("X", GH_FontServer.Standard, Brushes.Black, text_rec_1);

                    GH_Capsule radio_2 = GH_Capsule.CreateCapsule(radio_rec_2, GH_Palette.Black, 5, 5);
                    radio_2.Render(graphics, Selected, Owner.Locked, false); radio_2.Dispose();
                    graphics.FillEllipse(c2, radio_rec_2);
                    graphics.DrawString("Y", GH_FontServer.Standard, Brushes.Black, text_rec_2);

                    GH_Capsule radio_3 = GH_Capsule.CreateCapsule(radio_rec_3, GH_Palette.Black, 5, 5);
                    radio_3.Render(graphics, Selected, Owner.Locked, false); radio_3.Dispose();
                    graphics.FillEllipse(c3, radio_rec_3);
                    graphics.DrawString("Z", GH_FontServer.Standard, Brushes.Black, text_rec_3);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec1 = radio_rec_1; RectangleF rec2 = radio_rec_2; RectangleF rec3 = radio_rec_3;
                    if (rec1.Contains(e.CanvasLocation))
                    {
                        if (c1 == Brushes.White) { c1 = Brushes.Black; SetButton("x", 1); }
                        else { c1 = Brushes.White; SetButton("x", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec2.Contains(e.CanvasLocation))
                    {
                        if (c2 == Brushes.White) { c2 = Brushes.Black; SetButton("y", 1); }
                        else { c2 = Brushes.White; SetButton("y", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec3.Contains(e.CanvasLocation))
                    {
                        if (c3 == Brushes.White) { c3 = Brushes.Black; SetButton("z", 1); }
                        else { c3 = Brushes.White; SetButton("z", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}