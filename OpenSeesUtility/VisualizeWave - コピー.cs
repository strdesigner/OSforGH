using System;
using System.Collections;
using System.Collections.Generic;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.Display;
///using MathNet.Numerics.LinearAlgebra.Double;

using System.Drawing;
using System.Windows.Forms;
using System.Linq;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
///****************************************

namespace OpenSeesUtility
{
    public class VisualizeWave : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public VisualizeWave()
          : base("VisualizeWave", "VisualizeWave",
              "Display acceleration data",
              "OpenSees", "Visualization")
        {
        }
        public override bool IsPreviewCapable { get { return true; } }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("dataX", "dataX", "[dataX1,dataX2,...](DataList)", GH_ParamAccess.list);
            pManager.AddNumberParameter("dataY", "dataY", "[dataY1,dataY2,...](DataList)", GH_ParamAccess.list);
            pManager.AddIntegerParameter("step", "step", "step", GH_ParamAccess.item, 0);///
            pManager.AddNumberParameter("x", "x", "x-width of graph", GH_ParamAccess.item, 20);///
            pManager.AddNumberParameter("y", "y", "y-width of graph", GH_ParamAccess.item, 8);///
            pManager.AddNumberParameter("origin", "origin", "drawing origin", GH_ParamAccess.list, new List<double> { 0, 0, 0 });///
            pManager.AddNumberParameter("pitch x", "pitch x", "time pitch of graph [sec]", GH_ParamAccess.item, 5);///
            pManager.AddNumberParameter("pitch y", "pitch y", "acceleration pitch of graph [gal]", GH_ParamAccess.item, 50);///
            pManager.AddNumberParameter("Xminmax", "Xminmax", "[Xmin,Xmax]", GH_ParamAccess.list);///
            pManager.AddNumberParameter("Yminmax", "Yminmax", "[Ymin,Ymax]", GH_ParamAccess.list);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("accmax", "accmax", "maximum value of acceleration [m/sec^2]", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var time = new List<double>(); var acc = new List<double>(); int step = 0; DA.GetData("step", ref step);
            var dt = 5.0; DA.GetData("pitch x", ref dt); var da = 50.0; DA.GetData("pitch y", ref da);
            if (!DA.GetDataList("time", time)) return; if (!DA.GetDataList("acc", acc)) return;
            var X = 0.0; DA.GetData("x", ref X); var Y = 0.0; DA.GetData("y", ref Y); var cp = new List<double>(); DA.GetDataList("origin", cp);
            var tmax = time.Max(); var amax = Math.Max(Math.Abs(acc.Max()), Math.Abs(acc.Min()));
            cp[1] -= Y;
            //外枠の描画
            _frame.Add(new Line(cp[0], cp[1], cp[2], cp[0] + X, cp[1], cp[2]));
            _frame.Add(new Line(cp[0], cp[1] + Y, cp[2], cp[0] + X, cp[1] + Y, cp[2]));
            _frame.Add(new Line(cp[0], cp[1], cp[2], cp[0], cp[1] + Y, cp[2]));
            _frame.Add(new Line(cp[0] + X, cp[1], cp[2], cp[0] + X, cp[1] + Y, cp[2]));
            _scale.Add(new Line(cp[0], cp[1] + Y / 2.0, cp[2], cp[0] + X, cp[1] + Y / 2.0, cp[2]));
            var dx = X / tmax; var dy = Y / (amax * 2); var x = time; var y = acc;
            var t = 0.0; var a = 0.0;
            while (t < tmax)
            {
                _scale.Add(new Line(cp[0] + dx * t, cp[1], cp[2], cp[0] + dx * t, cp[1] + Y, cp[2]));
                _tp.Add(new Point3d(cp[0] + dx * t, cp[1] - Y * 0.05, cp[2])); _ttext.Add(t.ToString().Substring(0,Math.Min(4,(t.ToString()).Length)));
                t += dt;
            }
            _tp.Add(new Point3d(cp[0] + X / 2.0, cp[1] - Y * 0.15, cp[2])); _ttext.Add("[sec]");
            while (a < amax * 100)
            {
                _scale.Add(new Line(cp[0], cp[1] + dy * a / 100.0 + Y / 2.0, cp[2], cp[0] + X, cp[1] + dy * a / 100.0 + Y / 2.0, cp[2]));
                _scale.Add(new Line(cp[0], cp[1] - dy * a / 100.0 + Y / 2.0, cp[2], cp[0] + X, cp[1] - dy * a / 100.0 + Y / 2.0, cp[2]));
                _ap.Add(new Point3d(cp[0] - X * 0.05, cp[1] + dy * a / 100.0 + Y / 2.0, cp[2])); _atext.Add(a.ToString().Substring(0, Math.Min(4, (a.ToString()).Length)));
                _ap.Add(new Point3d(cp[0] - X * 0.05, cp[1] - dy * a / 100.0 + Y / 2.0, cp[2])); _atext.Add(a.ToString().Substring(0, Math.Min(4, (a.ToString()).Length)));
                a += da;
            }
            _ap.Add(new Point3d(cp[0], cp[1] + Y * 1.05, cp[2])); _atext.Add("[gal]");
            if (step > 0)
            {
                for (int i = 0; i < step; i++)
                {
                    _wave.Add(new Line(cp[0] + x[i] * dx, cp[1] + (y[i] + amax) * dy, cp[2], cp[0] + x[i + 1] * dx, cp[1] + (y[i + 1] + amax) * dy, cp[2]));
                }
                _p.Add(_wave[step - 1].To);
            }
            _p.Add(new Point3d(cp[0], cp[1] + Y / 2.0, cp[2]));
            DA.SetData("accmax", amax);
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
                return OpenSeesUtility.Properties.Resources.visualizewave;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b1d9ac84-7fe7-4e87-b220-239b2a69e5ba"); }
        }
        ///ここからカスタム関数群********************************************************************************
        private readonly List<Line> _frame = new List<Line>();
        private readonly List<Line> _scale = new List<Line>();
        private readonly List<Line> _wave = new List<Line>();
        private readonly List<Point3d> _p = new List<Point3d>();
        private readonly List<Point3d> _tp = new List<Point3d>();
        private readonly List<string> _ttext = new List<string>();
        private readonly List<Point3d> _ap = new List<Point3d>();
        private readonly List<string> _atext = new List<string>();
        protected override void BeforeSolveInstance()
        {
            _frame.Clear();
            _scale.Clear();
            _wave.Clear();
            _p.Clear();
            _tp.Clear();
            _ttext.Clear();
            _ap.Clear();
            _atext.Clear();
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            args.Viewport.GetFrustumFarPlane(out Plane plane);
            for (int i = 0; i < _frame.Count; i++)
            {
                args.Display.DrawLine(_frame[i], Color.Black, 2);
            }
            for (int i = 0; i < _scale.Count; i++)
            {
                args.Display.DrawLine(_scale[i], Color.Black, 1);
            }
            for (int i = 0; i < _wave.Count; i++)
            {
                args.Display.DrawLine(_wave[i], Color.Red, 2);
            }
            for (int i = 0; i < _p.Count; i++)
            {
                args.Display.DrawPoint(_p[i], PointStyle.RoundSimple, 4, Color.Green);
            }
            for (int i = 0; i < _tp.Count; i++)
            {
                var size = VisualizeModel.VisualizeModel.fontsize; Point3d point = _tp[i]; plane.Origin = point;
                args.Viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                args.Display.Draw3dText(_ttext[i], Color.Black, plane, size, "", false, false, Rhino.DocObjects.TextHorizontalAlignment.Center, Rhino.DocObjects.TextVerticalAlignment.Top);
            }
            for (int i = 0; i < _ap.Count; i++)
            {
                var size = VisualizeModel.VisualizeModel.fontsize; Point3d point = _ap[i]; plane.Origin = point;
                args.Viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                if (_atext[i] != "[gal]")
                {
                    args.Display.Draw3dText(_atext[i], Color.Black, plane, size, "", false, false, Rhino.DocObjects.TextHorizontalAlignment.Right, Rhino.DocObjects.TextVerticalAlignment.Middle);
                }
                else
                {
                    args.Display.Draw3dText(_atext[i], Color.Black, plane, size, "", false, false, Rhino.DocObjects.TextHorizontalAlignment.Right, Rhino.DocObjects.TextVerticalAlignment.Bottom);
                }
            }
        }
    }
}