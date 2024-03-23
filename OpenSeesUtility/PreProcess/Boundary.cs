using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.Drawing;
using System.Windows.Forms;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using GH_IO.Serialization;
///****************************************

namespace Boundary
{
    public class Boundary : GH_Component
    {
        public int Fx = 1; public int Fy = 1; public int Fz = 1; public int Rx = 0; public int Ry = 0; public int Rz = 0;

        CustomGUI m_customGUI => (CustomGUI)m_attributes;
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        public Boundary()
          : base("SetBoundaryCondition", "Boundary",
              "Set boundary condition into the points with specified coordinates",
              "OpenSees", "PreProcess")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("x coordinates", "x", "boundary point x", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("y coordinates", "y", "boundary point y", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("z coordinates", "z", "boundary point z", GH_ParamAccess.list, -9999);///
        }
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("boundary_condition", "Bounds", "[[node No.,X,Y,Z,MX,MY,MZ],...](DataTree) 0-1 variable", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Number> fix = new GH_Structure<GH_Number>();
            if (!DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r)) { }
            else if (_r.Branches[0][0].Value != -9999)
            {
                var r = _r.Branches; var n = r.Count;
                var x = new List<double>(); DA.GetDataList(1, x); var y = new List<double>(); DA.GetDataList(2, y); var z = new List<double>(); DA.GetDataList(3, z); int e = 0;
                for (int i = 0; i < n; i++)
                {
                    var xi = r[i][0].Value; var yi = r[i][1].Value; var zi = r[i][2].Value;
                    int k = 0;
                    for (int j = 0; j < x.Count; j++)
                    {
                        if (Math.Abs(x[j] - xi) < 5e-3 || x[j] == -9999) { k += 1; break; }
                    }
                    for (int j = 0; j < y.Count; j++)
                    {
                        if (Math.Abs(y[j] - yi) < 5e-3 || y[j] == -9999) { k += 1; break; }
                    }
                    for (int j = 0; j < z.Count; j++)
                    {
                        if (Math.Abs(z[j] - zi) < 5e-3 || z[j] == -9999) { k += 1; break; }
                    }
                    if (k == 3)
                    {
                        List<GH_Number> fixlist = new List<GH_Number>();
                        fixlist.Add(new GH_Number(i));
                        fixlist.Add(new GH_Number(Fx));
                        fixlist.Add(new GH_Number(Fy));
                        fixlist.Add(new GH_Number(Fz));
                        fixlist.Add(new GH_Number(Rx));
                        fixlist.Add(new GH_Number(Ry));
                        fixlist.Add(new GH_Number(Rz));
                        fix.AppendRange(fixlist, new GH_Path(e)); e += 1;
                    }
                }
            }
            DA.SetDataTree(0, fix);
        }

        public override bool Read(GH_IReader reader)
        {
            var fx = 1; var fy = 1; var fz = 1; var rx = 0; var ry = 0; var rz = 0;
            reader.TryGetInt32("Fx", ref fx);
            reader.TryGetInt32("Fy", ref fy);
            reader.TryGetInt32("Fz", ref fz);
            reader.TryGetInt32("Rx", ref rx);
            reader.TryGetInt32("Ry", ref ry);
            reader.TryGetInt32("Rz", ref rz);

            Fx = fx; Fy = fy; Fz = fz; Rx = rx; Ry = ry; Rz = rz;
            return base.Read(reader);
        }
        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("Fx", Fx);
            writer.SetInt32("Fy", Fy);
            writer.SetInt32("Fz", Fz);
            writer.SetInt32("Rx", Rx);
            writer.SetInt32("Ry", Ry);
            writer.SetInt32("Rz", Rz);
            return base.Write(writer);
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
                return OpenSeesUtility.Properties.Resources.boundary;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("d55d8bf1-5462-4b51-94c7-8b12f4b39d4f"); }
        }///ここからGUIの作成*****************************************************************************************
        internal class CustomGUI : GH_ComponentAttributes
        {
            Boundary _ownerBoundary => Owner as Boundary;
            internal CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle radio_rec;
            private Rectangle radio_rec_1; private Rectangle text_rec_1;
            private Rectangle radio_rec_2; private Rectangle text_rec_2;
            private Rectangle radio_rec_3; private Rectangle text_rec_3;
            private Rectangle radio_rec_4; private Rectangle text_rec_4;
            private Rectangle radio_rec_5; private Rectangle text_rec_5;
            private Rectangle radio_rec_6; private Rectangle text_rec_6;
            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int height = 27; int width = 90; int radi1 = 7; int radi2 = 14;
                int pitchy = 15;
                global_rec.Height += height;
                global_rec.Width = width;

                radio_rec = global_rec;
                radio_rec.Y = radio_rec.Bottom - height;
                radio_rec.Height = 27;

                text_rec_1 = radio_rec; text_rec_1.X += 5;
                text_rec_1.Height = radi2; text_rec_1.Width = radi2;
                text_rec_2 = text_rec_1; text_rec_2.X += radi2;
                text_rec_3 = text_rec_2; text_rec_3.X += radi2;
                text_rec_4 = text_rec_3; text_rec_4.X += radi2;
                text_rec_5 = text_rec_4; text_rec_5.X += radi2;
                text_rec_6 = text_rec_5; text_rec_6.X += radi2;

                radio_rec_1 = text_rec_1; radio_rec_1.Height = radi1; radio_rec_1.Width = radi1;
                radio_rec_1.X += 2;
                radio_rec_1.Y += pitchy;
                radio_rec_2 = radio_rec_1; radio_rec_2.X += radi2;
                radio_rec_3 = radio_rec_2; radio_rec_3.X += radi2;
                radio_rec_4 = radio_rec_3; radio_rec_4.X += radi2;
                radio_rec_5 = radio_rec_4; radio_rec_5.X += radi2;
                radio_rec_6 = radio_rec_5; radio_rec_6.X += radi2;

                Bounds = global_rec;
            }
            Brush c1 
            {
                get {
                    if (_ownerBoundary.Fx == 0)
                        return Brushes.White;
                    return Brushes.Black;
                }
            }
            Brush c2
            {
                get
                {
                    if (_ownerBoundary.Fy == 0)
                        return Brushes.White;
                    return Brushes.Black;
                }
            }
            Brush c3
            {
                get
                {
                    if (_ownerBoundary.Fz == 0)
                        return Brushes.White;
                    return Brushes.Black;
                }
            }
            Brush c4
            {
                get
                {
                    if (_ownerBoundary.Rx == 0)
                        return Brushes.White;
                    return Brushes.Black;
                }
            }
            Brush c5
            {
                get
                {
                    if (_ownerBoundary.Ry == 0)
                        return Brushes.White;
                    return Brushes.Black;
                }
            }
            Brush c6
            {
                get
                {
                    if (_ownerBoundary.Rz == 0)
                        return Brushes.White;
                    return Brushes.Black;
                }
            }
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
                    graphics.DrawString("fx", GH_FontServer.Standard, Brushes.Black, text_rec_1);

                    GH_Capsule radio_2 = GH_Capsule.CreateCapsule(radio_rec_2, GH_Palette.Black, 5, 5);
                    radio_2.Render(graphics, Selected, Owner.Locked, false); radio_2.Dispose();
                    graphics.FillEllipse(c2, radio_rec_2);
                    graphics.DrawString("fy", GH_FontServer.Standard, Brushes.Black, text_rec_2);

                    GH_Capsule radio_3 = GH_Capsule.CreateCapsule(radio_rec_3, GH_Palette.Black, 5, 5);
                    radio_3.Render(graphics, Selected, Owner.Locked, false); radio_3.Dispose();
                    graphics.FillEllipse(c3, radio_rec_3);
                    graphics.DrawString("fz", GH_FontServer.Standard, Brushes.Black, text_rec_3);

                    GH_Capsule radio_4 = GH_Capsule.CreateCapsule(radio_rec_4, GH_Palette.Black, 5, 5);
                    radio_4.Render(graphics, Selected, Owner.Locked, false); radio_4.Dispose();
                    graphics.FillEllipse(c4, radio_rec_4);
                    graphics.DrawString("rx", GH_FontServer.Standard, Brushes.Black, text_rec_4);

                    GH_Capsule radio_5 = GH_Capsule.CreateCapsule(radio_rec_5, GH_Palette.Black, 5, 5);
                    radio_5.Render(graphics, Selected, Owner.Locked, false); radio_5.Dispose();
                    graphics.FillEllipse(c5, radio_rec_5);
                    graphics.DrawString("ry", GH_FontServer.Standard, Brushes.Black, text_rec_5);

                    GH_Capsule radio_6 = GH_Capsule.CreateCapsule(radio_rec_6, GH_Palette.Black, 5, 5);
                    radio_6.Render(graphics, Selected, Owner.Locked, false); radio_6.Dispose();
                    graphics.FillEllipse(c6, radio_rec_6);
                    graphics.DrawString("rz", GH_FontServer.Standard, Brushes.Black, text_rec_6);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec1 = radio_rec_1; RectangleF rec2 = radio_rec_2; RectangleF rec3 = radio_rec_3; RectangleF rec4 = radio_rec_4; RectangleF rec5 = radio_rec_5; RectangleF rec6 = radio_rec_6; RectangleF rec = radio_rec;
                    if (rec1.Contains(e.CanvasLocation))
                    {
                        if (_ownerBoundary.Fx == 0) { _ownerBoundary.Fx = 1; }
                        else { _ownerBoundary.Fx = 0; }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec2.Contains(e.CanvasLocation))
                    {
                        if (_ownerBoundary.Fy == 0) { _ownerBoundary.Fy = 1; }
                        else { _ownerBoundary.Fy = 0; }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec3.Contains(e.CanvasLocation))
                    {
                        if (_ownerBoundary.Fz == 0) { _ownerBoundary.Fz = 1; }
                        else { _ownerBoundary.Fz = 0; }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec4.Contains(e.CanvasLocation))
                    {
                        if (_ownerBoundary.Rx == 0) { _ownerBoundary.Rx = 1; }
                        else { _ownerBoundary.Rx = 0; }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec5.Contains(e.CanvasLocation))
                    {
                        if (_ownerBoundary.Ry == 0) { _ownerBoundary.Ry = 1; }
                        else { _ownerBoundary.Ry = 0; }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec6.Contains(e.CanvasLocation))
                    {
                        if (_ownerBoundary.Rz == 0) { _ownerBoundary.Rz = 1; }
                        else { _ownerBoundary.Rz = 0; }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}