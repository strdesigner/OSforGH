using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;

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
///using CenterSpace.NMath.Core;

namespace NLPComponent
{
    public class NLPComponent : GH_Component
    {
        GH_Document doc;
        //private readonly GH_Document GrasshopperDocument;
        IGH_Component Component;
        private static int counter = -1;
        public static int start = 0; public static int agm = 1; public static int cg = 0; public static int constant = 1; public static int golden = 0;
        private static List<double> y = new List<double>();
        private static double t = 1.0;
        private static List<double> minrange = new List<double>();
        private static List<double> maxrange = new List<double>();
        private static double beta = 0.0; private static double[] dk; private static double[] pk;
        public static void SetButton(string s, int i)
        {
            if (s == "c1")
            {
                start = i;
            }
            else if (s == "AGM")
            {
                agm = i;
            }
            else if (s == "CG")
            {
                cg = i;
            }
            else if (s == "Constant")
            {
                constant = i;
            }
            else if (s == "Golden")
            {
                golden = i;
            }
        }
        public NLPComponent()
            : base("Optimization for NLP problem", "NLP",
                "Optimization for NLP problem",
                "OpenSees", "Analysis")
        {
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("f(x)", "f(x)", "objective function value", GH_ParamAccess.item);
            pManager.AddNumberParameter("x", "x", "decision variable", GH_ParamAccess.list);
            pManager.AddIntegerParameter("step", "step", "iteration limit", GH_ParamAccess.item,1000);
            pManager.AddNumberParameter("h", "h", "difference approximation width(>=1e-6)", GH_ParamAccess.item,1e-6);
            pManager.AddNumberParameter("alpha", "alpha", "step width(>=1e-6)", GH_ParamAccess.item, 0.5);
            pManager.AddNumberParameter("eps", "eps", "convergent accuracy(>=1e-4)", GH_ParamAccess.item, 1e-4);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("df(x)", "df(x)", "gradient vector", GH_ParamAccess.list);
            pManager.AddNumberParameter("|df(x)|", "|df(x)|", "gradient vector norm", GH_ParamAccess.item);
            pManager.AddNumberParameter("alpha", "alpha", "step width(>=1e-6)", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Component = this;
            doc = Component.OnPingDocument();
            var x = new List<double>();
            double fx = new double(); double h = new double(); int step = 0; double alpha = new double(); var eps = 1e-4;
            if (!DA.GetData("f(x)", ref fx)) { return; }
            if (!DA.GetDataList("x", x)) { return; }
            if (!DA.GetData("h", ref h)) { return; }
            if (!DA.GetData("alpha", ref alpha)) { return; }
            if (!DA.GetData("step", ref step)) { return; }
            if (!DA.GetData("eps", ref eps)) { return; }
            if (start == 0){ return; }
            if (counter == -1){ y = x; }
            List<Grasshopper.Kernel.Special.GH_NumberSlider> sliders = new List<Grasshopper.Kernel.Special.GH_NumberSlider>();
            for (int i = 0; i < x.Count; i++)
            {
                Grasshopper.Kernel.Special.GH_NumberSlider slider = Params.Input[1].Sources[i] as Grasshopper.Kernel.Special.GH_NumberSlider;
                sliders.Add(slider);
                if (counter == -1) { minrange.Add((double)slider.Slider.Minimum); maxrange.Add((double)slider.Slider.Maximum); }
            }
            doc.NewSolution(false);
            if (agm == 1)
            {
                for (int i = 0; i < y.Count; i++) { y[i] = Math.Min(Math.Max(y[i], minrange[i]), maxrange[i]); }//force y inside slider range
                var df = Sensitivity(sliders, y, h);
                dk = df; for (int i = 0; i < dk.Length; i++) { dk[i] = -dk[i]; }//dk=-df
                if (golden == 1) { alpha = line_search_golden_section(sliders, y, dk); }
                var x_prev = x;
                for (int i = 0; i < x.Count; i++) { x[i] = y[i] + alpha * dk[i]; }//x=y+alpha*dk
                var dk_norm = Math.Sqrt(vec_multiply(dk, dk));
                var t_prev = t;
                var reset_value = 0.0; for (int i = 0; i < dk.Length; i++) { reset_value += -dk[i] * (x[i] - x_prev[i]); } //np.dot(-dk,x-x_prev)
                if (reset_value <= 0.0)
                {
                    t = 0.5 * (1.0 + Math.Sqrt(1.0 + 4.0 * Math.Pow(t_prev, 2)));//t=0.50*(1.0+np.sqrt(1.0+4.0*t**2))
                    for (int i = 0; i < x.Count; i++) { y[i] = x[i] + (t_prev - 1.0) / t * (x[i] - x_prev[i]); }//y=x+(t_prev-1.0)/t*(x-x_prev)
                }
                else { t = 1.0; y = x; }
                DA.SetDataList("df(x)", df);
                DA.SetData("|df(x)|", dk_norm);
                DA.SetData("alpha", alpha);
                counter += 1;
                if (start == 1 && counter < step && dk_norm > eps) { doc.ScheduleSolution(1, ScheduleCallback); }
            }
            else if (cg == 1)
            {
                if (counter == -1) { dk = Sensitivity(sliders, x, h); for (int i = 0; i < dk.Length; i++) { dk[i] = -dk[i]; }; pk = dk; }//dk=-df
                for (int i = 0; i < pk.Length; i++) { pk[i] = dk[i] + beta * pk[i]; }//pk=dk+beta*pk
                if (golden == 1) { alpha = line_search_golden_section(sliders, x, pk); }
                for (int i = 0; i < x.Count; i++) { x[i] = x[i] + alpha * pk[i]; }//x=x+alpha*pk
                var dk_prev = dk;
                dk = Sensitivity(sliders, x, h); for (int i = 0; i < dk.Length; i++) { dk[i] = -dk[i]; }//dk=-df
                beta = vec_multiply(dk, dk) / vec_multiply(dk_prev, dk_prev);
                var dk_norm = Math.Sqrt(vec_multiply(dk, dk));
                DA.SetDataList("df(x)", dk);
                DA.SetData("|df(x)|", dk_norm);
                DA.SetData("alpha", alpha);
                counter += 1;
                if (start == 1 && counter < step && dk_norm > eps) { doc.ScheduleSolution(1, ScheduleCallback); }
            }


        }
        private double vec_multiply(double[] a, double[] b)
        {
            var v = 0.0;
            for (int i = 0; i<a.Length; i++){ v += a[i] * b[i]; }
            return v;
        }
        private double line_search_golden_section(List<Grasshopper.Kernel.Special.GH_NumberSlider> sliders, List<double> x, double[] dk)
        {
            double bl = 0.0; double bu = 1.0; double phi = (1.0 + Math.Sqrt(5.0)) / 2.0; double eps = 1e-5;
            double[] x1 = new double[x.Count]; double[] x2 = new double[x.Count];
            while (bl + eps < bu)
            {
                var alpha1 = (bl * phi + bu) / (1.0 + phi); var alpha2 = (bl + bu * phi) / (1.0 + phi);
                for (int i = 0; i < x.Count; i++){ x1[i] = x[i] + alpha1 * dk[i]; sliders[i].SetSliderValue((decimal)Math.Min(Math.Max(x1[i],minrange[i]), maxrange[i])); CollectData(); }//calc f1
                var f1 = getobjective();
                for (int i = 0; i < x.Count; i++){ x2[i] = x[i] + alpha2 * dk[i]; sliders[i].SetSliderValue((decimal)Math.Min(Math.Max(x2[i], minrange[i]), maxrange[i])); CollectData(); }// calc f2
                var f2 = getobjective();
                if (f1 > f2) { bl = alpha1*1.0; }
                else { bu = alpha2*1.0; }
            }
            return bl;
        }
        private double[] Sensitivity(List<Grasshopper.Kernel.Special.GH_NumberSlider> sliders, List<double> x, double h)
        {
            var df = new double[x.Count];
            for (int i = 0; i < x.Count; i++)//change x value to x+h,x-h
            {
                sliders[i].SetSliderValue((decimal)(x[i] + h)); CollectData();//insert changed x(x+h) to slider
                var fpx = getobjective();
                sliders[i].SetSliderValue((decimal)x[i]); CollectData();
                sliders[i].SetSliderValue((decimal)(x[i] - h)); CollectData();
                var fmx = getobjective();
                df[i]=(fpx - fmx) / 2.0 / h;
                sliders[i].SetSliderValue((decimal)x[i]); CollectData();
            }
            return df;
        }
        private double getobjective()
        {
            var T = Params.Input[0].VolatileData;
            var objective = T.AllData(true).First();
            var f = 0.0;
            objective.CastTo<double>(out f);
            return f;
        }
        private void ScheduleCallback(GH_Document document)
        {
            Component.ExpireSolution(false);
        }
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return OpenSeesUtility.Properties.Resources.nlp;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{50084e0a-caa3-472e-8e9a-a680604444d2}"); }
        }///ここからGUIの作成*****************************************************************************************
        public class CustomGUI : GH_ComponentAttributes
        {
            public CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle radio_rec0; private Rectangle radio_rec1; private Rectangle radio_rec2;
            private Rectangle radio_rec_1; private Rectangle text_rec_1;
            private Rectangle title_rec1; private Rectangle title_rec2;
            private Rectangle radio_rec_11; private Rectangle text_rec_11; private Rectangle radio_rec_12; private Rectangle text_rec_12;
            private Rectangle radio_rec_21; private Rectangle text_rec_21; private Rectangle radio_rec_22; private Rectangle text_rec_22; private Rectangle radio_rec_23; private Rectangle text_rec_23;
            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int height = 19; int width = 90; int radi1 = 7; int radi2 = 4;
                int pitchx = 8; int pitchy = 11; int textheight = 20;

                radio_rec0 = global_rec;
                radio_rec0.Y = radio_rec0.Bottom;
                radio_rec0.Height = height;

                radio_rec_1 = radio_rec0;
                radio_rec_1.X += 5; radio_rec_1.Y += 5;
                radio_rec_1.Height = radi1; radio_rec_1.Width = radi1;

                text_rec_1 = radio_rec_1;
                text_rec_1.X += pitchx; text_rec_1.Y -= radi2;
                text_rec_1.Height = textheight; text_rec_1.Width = width;

                title_rec1 = radio_rec0;
                title_rec1.Y = radio_rec0.Bottom;
                title_rec1.Width = global_rec.Width;
                title_rec1.Height = 22;

                radio_rec1 = title_rec1;
                radio_rec1.Y = title_rec1.Bottom;
                radio_rec1.Height = height;

                radio_rec_11 = radio_rec1;
                radio_rec_11.X += 5; radio_rec_11.Y += 5;
                radio_rec_11.Height = radi1; radio_rec_11.Width = radi1;

                text_rec_11 = radio_rec_11;
                text_rec_11.X += pitchx; text_rec_11.Y -= radi2;
                text_rec_11.Height = textheight; text_rec_11.Width = width;

                radio_rec_12 = radio_rec_11; radio_rec_12.X = text_rec_11.X+45;

                text_rec_12 = radio_rec_12;
                text_rec_12.X += pitchx; text_rec_12.Y -= radi2;
                text_rec_12.Height = textheight; text_rec_12.Width = width;

                title_rec2 = radio_rec1;
                title_rec2.Y = radio_rec1.Bottom;
                title_rec2.Width = global_rec.Width;
                title_rec2.Height = 22;

                radio_rec2 = title_rec2;
                radio_rec2.Y = title_rec2.Bottom;
                radio_rec2.Height = height*2;

                radio_rec_21 = radio_rec2;
                radio_rec_21.X += 5; radio_rec_21.Y += 5;
                radio_rec_21.Height = radi1; radio_rec_21.Width = radi1;

                text_rec_21 = radio_rec_21;
                text_rec_21.X += pitchx; text_rec_21.Y -= radi2;
                text_rec_21.Height = textheight; text_rec_21.Width = width;

                radio_rec_22 = radio_rec_21;
                radio_rec_22.Y += pitchy;

                text_rec_22 = radio_rec_22;
                text_rec_22.X += pitchx; text_rec_22.Y -= radi2;
                text_rec_22.Height = textheight; text_rec_22.Width = width;

                radio_rec_23 = radio_rec_22;
                radio_rec_23.Y += pitchy;

                text_rec_23 = radio_rec_23;
                text_rec_23.X += pitchx; text_rec_23.Y -= radi2;
                text_rec_23.Height = textheight; text_rec_23.Width = width;


                global_rec.Height += radio_rec2.Bottom - radio_rec0.Y;
                Bounds = global_rec;
            }
            static internal Brush c1 = Brushes.White; static internal Brush c11 = Brushes.Black; static internal Brush c12 = Brushes.White;
            static internal Brush c21 = Brushes.Black; static internal Brush c22 = Brushes.White; static internal Brush c23 = Brushes.White;
            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
            {
                base.Render(canvas, graphics, channel);
                if (channel == GH_CanvasChannel.Objects)
                {
                    StringFormat format = new StringFormat();
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    format.Trimming = StringTrimming.EllipsisCharacter;

                    GH_Capsule radio0 = GH_Capsule.CreateCapsule(radio_rec0, GH_Palette.Blue, 2, 0);
                    radio0.Render(graphics, Selected, Owner.Locked, false); radio0.Dispose();

                    GH_Capsule radio_1 = GH_Capsule.CreateCapsule(radio_rec_1, GH_Palette.Black, 5, 5);
                    radio_1.Render(graphics, Selected, Owner.Locked, false); radio_1.Dispose();
                    graphics.FillEllipse(c1, radio_rec_1);
                    graphics.DrawString("START", GH_FontServer.Standard, Brushes.Black, text_rec_1);

                    GH_Capsule title1 = GH_Capsule.CreateCapsule(title_rec1, GH_Palette.Pink, 2, 0);
                    title1.Render(graphics, Selected, Owner.Locked, false);
                    title1.Dispose();
                    RectangleF textRectangle = title_rec1;
                    textRectangle.Height = 20;
                    graphics.DrawString("Algorithm", GH_FontServer.Standard, Brushes.White, textRectangle, format);

                    GH_Capsule radio1 = GH_Capsule.CreateCapsule(radio_rec1, GH_Palette.White, 2, 0);
                    radio1.Render(graphics, Selected, Owner.Locked, false); radio1.Dispose();

                    GH_Capsule radio_11 = GH_Capsule.CreateCapsule(radio_rec_11, GH_Palette.Black, 5, 5);
                    radio_11.Render(graphics, Selected, Owner.Locked, false); radio_11.Dispose();
                    graphics.FillEllipse(c11, radio_rec_11);
                    graphics.DrawString("AGM", GH_FontServer.Standard, Brushes.Black, text_rec_11);

                    GH_Capsule radio_12 = GH_Capsule.CreateCapsule(radio_rec_12, GH_Palette.Black, 5, 5);
                    radio_12.Render(graphics, Selected, Owner.Locked, false); radio_12.Dispose();
                    graphics.FillEllipse(c12, radio_rec_12);
                    graphics.DrawString("CG", GH_FontServer.Standard, Brushes.Black, text_rec_12);

                    GH_Capsule title2 = GH_Capsule.CreateCapsule(title_rec2, GH_Palette.Pink, 2, 0);
                    title2.Render(graphics, Selected, Owner.Locked, false);
                    title2.Dispose();
                    RectangleF textRectangle2 = title_rec2;
                    textRectangle2.Height = 20;
                    graphics.DrawString("Step size", GH_FontServer.Standard, Brushes.White, textRectangle2, format);

                    GH_Capsule radio2 = GH_Capsule.CreateCapsule(radio_rec2, GH_Palette.White, 2, 0);
                    radio2.Render(graphics, Selected, Owner.Locked, false); radio2.Dispose();

                    GH_Capsule radio_21 = GH_Capsule.CreateCapsule(radio_rec_21, GH_Palette.Black, 5, 5);
                    radio_21.Render(graphics, Selected, Owner.Locked, false); radio_21.Dispose();
                    graphics.FillEllipse(c21, radio_rec_21);
                    graphics.DrawString("Constant", GH_FontServer.Standard, Brushes.Black, text_rec_21);

                    GH_Capsule radio_22 = GH_Capsule.CreateCapsule(radio_rec_22, GH_Palette.Black, 5, 5);
                    radio_22.Render(graphics, Selected, Owner.Locked, false); radio_22.Dispose();
                    graphics.FillEllipse(c22, radio_rec_22);
                    graphics.DrawString("Golden section", GH_FontServer.Standard, Brushes.Black, text_rec_22);

                    GH_Capsule radio_23 = GH_Capsule.CreateCapsule(radio_rec_23, GH_Palette.Black, 5, 5);
                    radio_23.Render(graphics, Selected, Owner.Locked, false); radio_23.Dispose();
                    graphics.FillEllipse(c23, radio_rec_23);
                    graphics.DrawString("Armijo", GH_FontServer.Standard, Brushes.Black, text_rec_23);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec1 = radio_rec_1; RectangleF rec11 = radio_rec_11; RectangleF rec12 = radio_rec_12; RectangleF rec21 = radio_rec_21; RectangleF rec22 = radio_rec_22; RectangleF rec23 = radio_rec_23;
                    if (rec1.Contains(e.CanvasLocation))
                    {
                        if (c1 == Brushes.White) { c1 = Brushes.Black; SetButton("c1", 1); counter = -1; }
                        else { c1 = Brushes.White; SetButton("c1", 0); counter = -1; }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec11.Contains(e.CanvasLocation))
                    {
                        if (c11 == Brushes.White) { c11 = Brushes.Black; SetButton("AGM", 1); c12 = Brushes.White; SetButton("CG", 0); counter = -1; }
                        else { c11 = Brushes.White; SetButton("AGM", 0); c12 = Brushes.Black; SetButton("CG", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec12.Contains(e.CanvasLocation))
                    {
                        if (c12 == Brushes.White) { c11 = Brushes.White; SetButton("AGM", 0); c12 = Brushes.Black; SetButton("CG", 1); counter = -1; }
                        else { c11 = Brushes.Black; SetButton("AGM", 1); c12 = Brushes.White; SetButton("CG", 0);}
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec21.Contains(e.CanvasLocation))
                    {
                        if (c21 == Brushes.White) { c21 = Brushes.Black; SetButton("Constant", 1); c22 = Brushes.White; SetButton("Golden", 0); c23 = Brushes.White; SetButton("Armijo", 0); }
                        //else { c21 = Brushes.White; SetButton("Constant", 0); c22 = Brushes.Black; SetButton("Golden", 1); c23 = Brushes.White; SetButton("Armijo", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec22.Contains(e.CanvasLocation))
                    {
                        if (c22 == Brushes.White) { c21 = Brushes.White; SetButton("Constant", 0); c22 = Brushes.Black; SetButton("Golden", 1); c23 = Brushes.White; SetButton("Armijo", 0); }
                        //else { c21 = Brushes.Black; SetButton("Constant", 1); c22 = Brushes.White; SetButton("Golden", 0); c23 = Brushes.White; SetButton("Armijo", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec23.Contains(e.CanvasLocation))
                    {
                        if (c23 == Brushes.White) { c21 = Brushes.White; SetButton("Constant", 0); c22 = Brushes.White; SetButton("Golden", 0); c23 = Brushes.Black; SetButton("Armijo", 1); }
                        //else { c21 = Brushes.Black; SetButton("Constant", 1); c22 = Brushes.White; SetButton("Golden", 0); c23 = Brushes.White; SetButton("Armijo", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}
