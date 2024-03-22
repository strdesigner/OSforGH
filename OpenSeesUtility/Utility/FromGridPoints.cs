using System;
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
///****************************************

namespace OpenSeesUtility
{
    public class FromGridPoints : GH_Component
    {
        public static int Line = 1; public static int Quad = 0; public static int Tri = 0;
        public static void SetButton(string s, int i)
        {
            if (s == "c1")
            {
                Line = i;
            }
            else if (s == "c2")
            {
                Quad = i;
            }
            else if (s == "c3")
            {
                Tri = i;
            }
        }
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public FromGridPoints()
          : base("FromGridPoints", "FromGridPts",
              "Create BEAM or SHELL on B-spline surface from grid points",
              "OpenSees", "Utility")
        {
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("P", "P", "[[[x00,y00,z00],...],[x10,y10,z10],...]](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("lines", "BEAM", "Line of elements", GH_ParamAccess.list);
            pManager.AddSurfaceParameter("shells", "SHELL", "Plate of elements", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!DA.GetDataTree("P", out GH_Structure<GH_Point> _r)) { return; }
            else
            {
                var r = _r.Branches; var U = r.Count; var V = r[0].Count; var lines = new List<Line>(); var shell = new List<Surface>();
                if (Line == 1)
                {
                    for (int i = 0; i < U; i++)
                    {
                        for (int j = 0; j < V - 1; j++)
                        {
                            var r1 = r[i][j].Value; var r2 = r[i][j + 1].Value;
                            lines.Add(new Line(r1, r2));
                        }
                    }
                    for (int i = 0; i < U - 1; i++)
                    {
                        for (int j = 0; j < V; j++)
                        {
                            var r1 = r[i][j].Value; var r2 = r[i + 1][j].Value;
                            lines.Add(new Line(r1, r2));
                        }
                    }
                    DA.SetDataList("lines", lines);
                }
                if (Quad == 1)
                {
                    for (int i = 0; i < U-1; i++)
                    {
                        for (int j = 0; j < V - 1; j++)
                        {
                            var r1 = r[i][j].Value; var r2 = r[i + 1][j].Value; var r3 = r[i + 1][j + 1].Value; var r4 = r[i][j + 1].Value;
                            shell.Add(NurbsSurface.CreateFromCorners(r1,r2,r3,r4));
                        }
                    }
                    DA.SetDataList("shells", shell);
                }
                if (Tri == 1)
                {
                    for (int i = 0; i < U - 1; i++)
                    {
                        for (int j = 0; j < V - 1; j++)
                        {
                            if ((i < (int)(U / 2) && j < (int)(V / 2)) || (i >= (int)(U / 2) && j >= (int)(V / 2)))
                            {
                                var r1 = r[i][j].Value; var r2 = r[i + 1][j].Value; var r3 = r[i + 1][j + 1].Value; var r4 = r[i][j + 1].Value;
                                shell.Add(NurbsSurface.CreateFromCorners(r1, r2, r4, r4));
                                shell.Add(NurbsSurface.CreateFromCorners(r2, r3, r4, r4));
                            }
                            else
                            {
                                var r1 = r[i][j].Value; var r2 = r[i + 1][j].Value; var r3 = r[i + 1][j + 1].Value; var r4 = r[i][j + 1].Value;
                                shell.Add(NurbsSurface.CreateFromCorners(r1, r3, r4, r4));
                                shell.Add(NurbsSurface.CreateFromCorners(r1, r2, r3, r3));
                            }
                        }
                    }
                    DA.SetDataList("shells", shell);
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
                return OpenSeesUtility.Properties.Resources.fromgridpoints;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("afae8499-7cb9-41f4-ab06-e11ea9020927"); }
        }///ここからGUIの作成*****************************************************************************************
        public class CustomGUI : GH_ComponentAttributes
        {
            public CustomGUI(GH_Component owner) : base(owner)
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
                int height = 40; int radi1 = 7; int radi2 = 4;
                int pitchx = 8; int pitchy = 11; int textheight = 20;
                global_rec.Height += height;
                int width = global_rec.Width;

                radio_rec = global_rec;
                radio_rec.Y = radio_rec.Bottom - height;
                radio_rec.Height = height;

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
            Brush c1 = Brushes.Black; Brush c2 = Brushes.White; Brush c3 = Brushes.White;
            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
            {
                base.Render(canvas, graphics, channel);
                if (channel == GH_CanvasChannel.Objects)
                {
                    GH_Capsule radio = GH_Capsule.CreateCapsule(radio_rec, GH_Palette.White, 2, 0);
                    radio.Render(graphics, Selected, Owner.Locked, false); radio.Dispose();

                    GH_Capsule radio_1 = GH_Capsule.CreateCapsule(radio_rec_1, GH_Palette.Black, 5, 5);
                    radio_1.Render(graphics, Selected, Owner.Locked, false); radio_1.Dispose();
                    graphics.FillEllipse(c1, radio_rec_1);
                    graphics.DrawString("Beam", GH_FontServer.Standard, Brushes.Black, text_rec_1);

                    GH_Capsule radio_2 = GH_Capsule.CreateCapsule(radio_rec_2, GH_Palette.Black, 5, 5);
                    radio_2.Render(graphics, Selected, Owner.Locked, false); radio_2.Dispose();
                    graphics.FillEllipse(c2, radio_rec_2);
                    graphics.DrawString("Shell(Quad)", GH_FontServer.Standard, Brushes.Black, text_rec_2);

                    GH_Capsule radio_3 = GH_Capsule.CreateCapsule(radio_rec_3, GH_Palette.Black, 5, 5);
                    radio_3.Render(graphics, Selected, Owner.Locked, false); radio_3.Dispose();
                    graphics.FillEllipse(c3, radio_rec_3);
                    graphics.DrawString("Shell(Tri)", GH_FontServer.Standard, Brushes.Black, text_rec_3);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec1 = radio_rec_1; RectangleF rec2 = radio_rec_2; RectangleF rec3 = radio_rec_3;
                    if (rec1.Contains(e.CanvasLocation))
                    {
                        if (c1 == Brushes.White) { c1 = Brushes.Black; SetButton("c1", 1); c2 = Brushes.White; SetButton("c2", 0); c3 = Brushes.White; SetButton("c3", 0); }
                        else { c1 = Brushes.White; SetButton("c1", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec2.Contains(e.CanvasLocation))
                    {
                        if (c2 == Brushes.White) { c1 = Brushes.White; SetButton("c1", 0); c2 = Brushes.Black; SetButton("c2", 1); c3 = Brushes.White; SetButton("c3", 0); }
                        else { c2 = Brushes.White; SetButton("c2", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec3.Contains(e.CanvasLocation))
                    {
                        if (c3 == Brushes.White) { c1 = Brushes.White; SetButton("c1", 0); c2 = Brushes.White; SetButton("c2", 0); c3 = Brushes.Black; SetButton("c3", 1); }
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