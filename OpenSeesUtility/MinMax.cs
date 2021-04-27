using System;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.Drawing;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI.Canvas;
///****************************************

namespace MinMax
{
    public class MinMax : GH_Component
    {
        public static double min = 0; public static double max = 0; public static double absmin = 0; public static double absmax = 0;
        /// <summary>
        /// Initializes a new instance of the MyComponent2 class.
        /// </summary>
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        public MinMax()
          : base("CalcMinMaxDataofTree", "MinMax",
              "Calculate min, max, absmin, absmax value of the components at the specified index in the tree",
              "OpenSees", "Utility")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("DataTree", "Tree", "[[*,*,*],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddIntegerParameter("specified index", "index", "min, max, absmin, absmax value of the components at the specified index in the tree are calculated", GH_ParamAccess.item, 0);///
            ExpireSolution(true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("min value", "min", "min value of the components at the specified index in the tree are calculated", GH_ParamAccess.item);///
            pManager.AddNumberParameter("max value", "max", "max value of the components at the specified index in the tree are calculated", GH_ParamAccess.item);///
            pManager.AddNumberParameter("absmin value", "absmin", "absmin value of the components at the specified index in the tree are calculated", GH_ParamAccess.item);///
            pManager.AddNumberParameter("absmax value", "absmax", "absmax value of the components at the specified index in the tree are calculated", GH_ParamAccess.item);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!DA.GetDataTree("DataTree", out GH_Structure<GH_Number> _tree)) { }
            else if (_tree.Branches[0][0].Value != -9999)
            {
                var tree = _tree.Branches; var n = tree.Count; int ind = 0; DA.GetData("specified index", ref ind);
                for (int i = 0; i < n; i++)
                {
                    min = Math.Min(min, tree[i][ind].Value); max = Math.Max(max, tree[i][ind].Value);
                    absmin = Math.Min(absmin, Math.Abs(tree[i][ind].Value)); absmax = Math.Max(absmax, Math.Abs(tree[i][ind].Value));
                }
                DA.SetData(0, min); DA.SetData(1, max); DA.SetData(2, absmin); DA.SetData(3, absmax);
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
                return OpenSeesUtility.Properties.Resources.minmax;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("3b72ef5f-930f-4089-b606-9e939a185ccb"); }
        }
    }///ここからGUIの作成*****************************************************************************************
    public class CustomGUI : GH_ComponentAttributes
    {
        public CustomGUI(GH_Component owner) : base(owner)
        {
        }
        private Rectangle radio_rec;private Rectangle text_rec_1;private Rectangle text_rec_2;private Rectangle text_rec_3;private Rectangle text_rec_4;
        protected override void Layout()
        {
            base.Layout();
            Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
            int height = 57; int width = 110; int radi1 = 14; int offset = 5;
            global_rec.Height += height;
            global_rec.Width = width;

            radio_rec = global_rec;
            radio_rec.Y = radio_rec.Bottom - height;
            radio_rec.Height = height;

            text_rec_1 = radio_rec; text_rec_1.X += offset;
            text_rec_1.Height = radi1; text_rec_1.Width = width - offset;

            text_rec_2 = text_rec_1; text_rec_2.Y += radi1;
            text_rec_3 = text_rec_2; text_rec_3.Y += radi1;
            text_rec_4 = text_rec_3; text_rec_4.Y += radi1;

            Bounds = global_rec;
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

                graphics.DrawString("Min:" + MinMax.min.ToString(), GH_FontServer.Standard, Brushes.Black, text_rec_1);

                graphics.DrawString("Max:" + MinMax.max.ToString(), GH_FontServer.Standard, Brushes.Black, text_rec_2);

                graphics.DrawString("AbsMin:" + MinMax.absmin.ToString(), GH_FontServer.Standard, Brushes.Black, text_rec_3);

                graphics.DrawString("AbsMax:" + MinMax.absmax.ToString(), GH_FontServer.Standard, Brushes.Black, text_rec_4);
            }
        }
    }
}