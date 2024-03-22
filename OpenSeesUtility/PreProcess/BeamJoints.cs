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
///****************************************


namespace BeamJoints
{
    public class BeamJoints : GH_Component
    {
        public static int I = 1; public static int J = 1;
        public static void SetButton(string s, int i)
        {
            if (s == "I")
            {
                I = i;
            }
            else if (s == "J")
            {
                J = i;
            }
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        public BeamJoints()
          : base("SetBeamJoints", "BeamJoints",
              "Set joint condition of beam element for OpenSees",
              "OpenSees", "PreProcess")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("x coordinates of element center point", "x", "element center point x", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("y coordinates of element center point", "y", "element center point y", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("z coordinates of element center point", "z", "element center point z", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("axial spring for node_i", "ki", "[kxi,kyi,kzi](DataList)(1e-8<=ki<=1e+8)", GH_ParamAccess.list, new List<double> { 1e+15, 1e+15, 1e+15 });///
            pManager.AddNumberParameter("rotational spring for node_i", "ri", "[rxi,ryi,rzi](DataList)(1e-8<=ri<=1e+8)", GH_ParamAccess.list, new List<double> { 1e+15, 0.0, 0.0 });///
            pManager.AddNumberParameter("axial spring for node_j", "kj", "[kxj,kyj,kzj](DataList)(1e-8<=kj<=1e+8)", GH_ParamAccess.list, new List<double> { 1e+15, 1e+15, 1e+15 });///
            pManager.AddNumberParameter("rotational spring for node_j", "rj", "[rxj,ryj,rzj](DataList)(1e-8<=rj<=1e+8)", GH_ParamAccess.list, new List<double> { 1e+15, 0.0, 0.0 });///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("joint condition", "joint", "[[Ele. No., 0 or 1(means i or j), kx, ky, kz, rx, ry, rz(spring value)],...](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Number> joint = new GH_Structure<GH_Number>();
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r);
            DA.GetDataTree("element_node_relationship", out GH_Structure<GH_Number> _ij);
            if (_r.Branches[0][0].Value != -9999 && _ij.Branches[0][0].Value != -9999)
            {
                var r = _r.Branches; var ij = _ij.Branches;
                var x = new List<double>(); DA.GetDataList("x coordinates of element center point", x); var y = new List<double>(); DA.GetDataList("y coordinates of element center point", y); var z = new List<double>(); DA.GetDataList("z coordinates of element center point", z);
                var ki = new List<double>(); DA.GetDataList("axial spring for node_i", ki); var kj = new List<double>(); DA.GetDataList("axial spring for node_j", kj);
                var ri = new List<double>(); DA.GetDataList("rotational spring for node_i", ri); var rj = new List<double>(); DA.GetDataList("rotational spring for node_j", rj);
                var kxi = ki[0]; var kyi = ki[1]; var kzi = ki[2]; var rxi = ri[0]; var ryi = ri[1]; var rzi = ri[2];
                var kxj = kj[0]; var kyj = kj[1]; var kzj = kj[2]; var rxj = rj[0]; var ryj = rj[1]; var rzj = ri[2];
                int k = 0;
                for (int j = 0; j < Math.Max(Math.Max(x.Count, y.Count), z.Count); j++)
                {
                    for (int e = 0; e < ij.Count; e++)
                    {
                        var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                        var xi = r[ni][0].Value; var yi = r[ni][1].Value; var zi = r[ni][2].Value;
                        var xj = r[nj][0].Value; var yj = r[nj][1].Value; var zj = r[nj][2].Value;
                        var xc = (xi + xj) / 2.0; var yc = (yi + yj) / 2.0; var zc = (zi + zj) / 2.0;
                        if (Math.Abs(xc - x[Math.Min(j, x.Count - 1)]) < 1e-8 || x[0] == -9999)
                        {
                            if (Math.Abs(yc - y[Math.Min(j, y.Count - 1)]) < 1e-8 || y[0] == -9999)
                            {
                                if (Math.Abs(zc - z[Math.Min(j, z.Count - 1)]) < 1e-8 || z[0] == -9999)
                                {
                                    List<GH_Number> jlist = new List<GH_Number>();
                                    jlist.Add(new GH_Number(e));
                                    if (I == 1 && J == 0){ jlist.Add(new GH_Number(0)); }
                                    else if (I == 0 && J == 1) { jlist.Add(new GH_Number(1)); }
                                    else if(I == 1 && J == 1){ jlist.Add(new GH_Number(2)); }
                                    else { jlist.Add(new GH_Number(-1)); }
                                    jlist.Add(new GH_Number(kxi)); jlist.Add(new GH_Number(kyi)); jlist.Add(new GH_Number(kzi));
                                    jlist.Add(new GH_Number(rxi)); jlist.Add(new GH_Number(ryi)); jlist.Add(new GH_Number(rzi));
                                    jlist.Add(new GH_Number(kxj)); jlist.Add(new GH_Number(kyj)); jlist.Add(new GH_Number(kzj));
                                    jlist.Add(new GH_Number(rxj)); jlist.Add(new GH_Number(ryj)); jlist.Add(new GH_Number(rzj));
                                    joint.AppendRange(jlist, new GH_Path(k));
                                    k += 1;
                                }
                            }
                        }
                    }
                }
                DA.SetDataTree(0, joint);
            }
        }///ここからGUIの作成*****************************************************************************************
        public class CustomGUI : GH_ComponentAttributes
        {
            public CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle radio_rec;
            private Rectangle radio_rec_1; private Rectangle text_rec_1;
            private Rectangle radio_rec_2; private Rectangle text_rec_2;
            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int height = 27; int radi1 = 7; int radi2 = 40;
                int pitchy = 15;
                global_rec.Height += height;
                ///global_rec.Width = width;

                radio_rec = global_rec;
                radio_rec.Y = radio_rec.Bottom - height;
                radio_rec.Height = 27;

                text_rec_1 = radio_rec; ///text_rec_1.X += 5;
                text_rec_1.Height = radi2; text_rec_1.Width = radi2;
                text_rec_2 = text_rec_1; text_rec_2.X += radi2;

                radio_rec_1 = text_rec_1; radio_rec_1.Height = radi1; radio_rec_1.Width = radi1;
                radio_rec_1.X += 17;
                radio_rec_1.Y += pitchy;
                radio_rec_2 = radio_rec_1; radio_rec_2.X += radi2;

                Bounds = global_rec;
            }
            Brush c1 = Brushes.Black; Brush c2 = Brushes.Black;
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
                    graphics.DrawString("Node-I", GH_FontServer.Standard, Brushes.Black, text_rec_1);

                    GH_Capsule radio_2 = GH_Capsule.CreateCapsule(radio_rec_2, GH_Palette.Black, 5, 5);
                    radio_2.Render(graphics, Selected, Owner.Locked, false); radio_2.Dispose();
                    graphics.FillEllipse(c2, radio_rec_2);
                    graphics.DrawString("Node-J", GH_FontServer.Standard, Brushes.Black, text_rec_2);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec1 = radio_rec_1; RectangleF rec2 = radio_rec_2;
                    if (rec1.Contains(e.CanvasLocation))
                    {
                        if (c1 == Brushes.White) { c1 = Brushes.Black; SetButton("I", 1); }
                        else { c1 = Brushes.White; SetButton("I", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec2.Contains(e.CanvasLocation))
                    {
                        if (c2 == Brushes.White) { c2 = Brushes.Black; SetButton("J", 1); }
                        else { c2 = Brushes.White; SetButton("J", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
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
                return OpenSeesUtility.Properties.Resources.beamjoint;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("685d1648-a87e-4ca2-93aa-33ce9561c5a8"); }
        }
    }
}