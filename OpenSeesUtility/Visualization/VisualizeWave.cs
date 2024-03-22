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
        public VisualizeWave()
          : base("VisualizeWave", "VisualizeWave",
              "Display valueYeleration dYta",
              "OpenSees", "Visualization")
        {
        }
        public override bool IsPreviewCapable { get { return true; } }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("value X", "value X", "[value X1,value X2,...](DataList)", GH_ParamAccess.list);
            pManager.AddNumberParameter("value Y", "value Y", "[value Y1,value Y2,...](DataList)", GH_ParamAccess.list);
            pManager.AddIntegerParameter("step", "step", "step", GH_ParamAccess.item, 0);///
            pManager.AddNumberParameter("x width", "x width", "x-width of graph", GH_ParamAccess.item, 20);///
            pManager.AddNumberParameter("y width", "y width", "y-width of graph", GH_ParamAccess.item, 8);///
            pManager.AddPointParameter("origin", "origin", "drawing origin", GH_ParamAccess.item, new Point3d(0,0,0));///
            pManager.AddNumberParameter("pitch x", "pitch x", "value X pitch of graph", GH_ParamAccess.item);///
            pManager.AddNumberParameter("pitch y", "pitch y", "value Y pitch of graph", GH_ParamAccess.item);///
            pManager.AddTextParameter("label x", "label x", "label x", GH_ParamAccess.item, "");///
            pManager.AddTextParameter("label y", "label y", "label y", GH_ParamAccess.item, "");///
            pManager.AddTextParameter("title", "title", "graph title", GH_ParamAccess.item, "");///
            pManager.AddNumberParameter("XminXmax", "XminXmax", "[Xmin,Xmax](DataList)", GH_ParamAccess.list);
            pManager.AddNumberParameter("YminYmax", "YminYmax", "[Ymin,Ymax](DataList)", GH_ParamAccess.list);
            pManager.AddColourParameter("color", "color", "graph color", GH_ParamAccess.item, Color.Red);///
            pManager[11].Optional = true; pManager[12].Optional = true; pManager[6].Optional = true; pManager[7].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("xmin", "xmin", "minimum value of value X", GH_ParamAccess.item);
            pManager.AddNumberParameter("xmax", "xmax", "maximum value of value X", GH_ParamAccess.item);
            pManager.AddNumberParameter("ymin", "ymin", "minimum value of value Y", GH_ParamAccess.item);
            pManager.AddNumberParameter("ymax", "ymax", "maximum value of value Y", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var valueX = new List<double>(); var valueY = new List<double>(); int step = 0; DA.GetData("step", ref step);
            if (!DA.GetDataList("value X", valueX)) return; if (!DA.GetDataList("value Y", valueY)) return;
            var X = 0.0; DA.GetData("x width", ref X); var Y = 0.0; DA.GetData("y width", ref Y); var cp = new List<double> { 0, 0, 0 }; var p = new Point3d(); DA.GetData("origin", ref p); cp[0] = p[0]; cp[1] = p[1]; cp[2] = p[2];
            var xmax = valueX.Max(); var xmin = valueX.Min(); var ymax = valueY.Max(); var ymin = valueY.Min();
            var XminXmax = new List<double>(); var YminYmax = new List<double>(); var wavecolor = Color.Red; DA.GetData("color", ref wavecolor); _wavecolor.Add(wavecolor);
            if (!DA.GetDataList("XminXmax", XminXmax)) { }; if (!DA.GetDataList("YminYmax", YminYmax)) { };
            if (XminXmax.Count == 0) { XminXmax = new List<double> { xmin, xmax }; }
            if (YminYmax.Count == 0) { YminYmax = new List<double> { ymin, ymax }; }
            var xlabel = ""; var ylabel = ""; DA.GetData("label x", ref xlabel); DA.GetData("label y", ref ylabel); var title = ""; DA.GetData("title", ref title);
            cp[1] -= Y;
            var xrange = XminXmax[1] - XminXmax[0]; var yrange = YminYmax[1] - YminYmax[0];
            var dx = X / xrange; var dy = Y / yrange;
            var dX = xrange / 5.0; var dY = yrange / 5.0;
            
            if (!DA.GetData("pitch x", ref dX)) { }; if (!DA.GetData("pitch y", ref dY)) { };
            var x = 0.0;
            while (x < XminXmax[1])//グリッド線描画
            {
                _scale.Add(new Line(cp[0] + dx * x, cp[1] + dy * YminYmax[0], cp[2], cp[0] + dx * x, cp[1] + dy * YminYmax[1], cp[2]));
                _xp.Add(new Point3d(cp[0] + dx * x, cp[1] + dy * YminYmax[0], cp[2])); _xtext.Add(x.ToString().Substring(0,Math.Min(5,(x.ToString()).Length)));
                x += dX;
            }
            _frame.Add(new Line(cp[0] + dx * XminXmax[1], cp[1] + dy * YminYmax[0], cp[2], cp[0] + dx * XminXmax[1], cp[1] + dy * YminYmax[1], cp[2]));//外枠の描画(右側の縦線)
            x = -dX;
            while (x > XminXmax[0])//グリッド線描画
            {
                _scale.Add(new Line(cp[0] + dx * x, cp[1] + dy * YminYmax[0], cp[2], cp[0] + dx * x, cp[1] + dy * YminYmax[1], cp[2]));
                _xp.Add(new Point3d(cp[0] + dx * x, cp[1] + dy * YminYmax[0], cp[2])); _xtext.Add(x.ToString().Substring(0, Math.Min(6, (x.ToString()).Length)));
                x -= dX;
            }
            _frame.Add(new Line(cp[0] + dx * XminXmax[0], cp[1] + dy * YminYmax[0], cp[2], cp[0] + dx * XminXmax[0], cp[1] + dy * YminYmax[1], cp[2]));//外枠の描画(左側の縦線)
            _xp.Add(new Point3d(cp[0] + dx * (XminXmax[0] + xrange / 2.0), cp[1] + dy * YminYmax[0], cp[2])); _xtext.Add("\n" + xlabel);//xラベル
            _tp.Add(new Point3d(cp[0] + dx * (XminXmax[0] + xrange / 2.0), cp[1] + dy * YminYmax[1], cp[2])); _ttext.Add(title);//タイトル
            var y = 0.0;
            while (y < YminYmax[1])
            {
                _scale.Add(new Line(cp[0] + dx * XminXmax[0], cp[1] + dy * y, cp[2], cp[0] + dx * XminXmax[1], cp[1] + dy * y, cp[2]));
                _yp.Add(new Point3d(cp[0] + dx * XminXmax[0], cp[1] + dy * y, cp[2])); _ytext.Add(y.ToString().Substring(0, Math.Min(5, (y.ToString()).Length)));
                y += dY;
            }
            _frame.Add(new Line(cp[0] + dx * XminXmax[0], cp[1] + dy * YminYmax[1], cp[2], cp[0] + dx * XminXmax[1], cp[1] + dy * YminYmax[1], cp[2]));//外枠の描画(上側の横線)
            y = -dY;
            while (y > YminYmax[0])
            {
                _scale.Add(new Line(cp[0] + dx * XminXmax[0], cp[1] + dy * y, cp[2], cp[0] + dx * XminXmax[1], cp[1] + dy * y, cp[2]));
                _yp.Add(new Point3d(cp[0] + dx * XminXmax[0], cp[1] + dy * y, cp[2])); _ytext.Add(y.ToString().Substring(0, Math.Min(6, (y.ToString()).Length)));
                y -= dY;
            }
            _frame.Add(new Line(cp[0] + dx * XminXmax[0], cp[1] + dy * YminYmax[0], cp[2], cp[0] + dx * XminXmax[1], cp[1] + dy * YminYmax[0], cp[2]));//外枠の描画(下側の横線)
            _yp.Add(new Point3d(cp[0] + dx * XminXmax[0], cp[1] + dy * (YminYmax[0] + yrange / 2.0), cp[2])); _ytext.Add(ylabel + "       ");//yラベル
            if (step > 0)
            {
                for (int i = 0; i < step; i++)
                {
                    _wave.Add(new Line(cp[0] + valueX[i] * dx, cp[1] + (valueY[i]) * dy, cp[2], cp[0] + valueX[i + 1] * dx, cp[1] + (valueY[i + 1]) * dy, cp[2]));
                }
                _p.Add(_wave[step - 1].To);
            }
            _p.Add(new Point3d(cp[0], cp[1], cp[2]));
            DA.SetData("xmin", valueX.Min()); DA.SetData("xmax", valueX.Max()); DA.SetData("ymin", valueY.Min()); DA.SetData("ymax", valueY.Max());
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and valueYess them like this:
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
        private readonly List<Point3d> _xp = new List<Point3d>();
        private readonly List<string> _xtext = new List<string>();
        private readonly List<Point3d> _yp = new List<Point3d>();
        private readonly List<string> _ytext = new List<string>();
        private readonly List<Point3d> _tp = new List<Point3d>();
        private readonly List<string> _ttext = new List<string>();
        private readonly List<Color> _wavecolor = new List<Color>();
        protected override void BeforeSolveInstance()
        {
            _frame.Clear();
            _scale.Clear();
            _wave.Clear();
            _p.Clear();
            _xp.Clear();
            _xtext.Clear();
            _yp.Clear();
            _ytext.Clear();
            _tp.Clear();
            _ttext.Clear();
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
                args.Display.DrawLine(_wave[i], _wavecolor[0], 2);
            }
            for (int i = 0; i < _p.Count; i++)
            {
                args.Display.DrawPoint(_p[i], PointStyle.RoundSimple, 4, Color.Green);
            }
            for (int i = 0; i < _xp.Count; i++)
            {
                var size = VisualizeModel.VisualizeModel.fontsize; Point3d point = _xp[i]; plane.Origin = point;
                args.Viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                args.Display.Draw3dText(_xtext[i], Color.Black, plane, size, "", false, false, Rhino.DocObjects.TextHorizontalAlignment.Center, Rhino.DocObjects.TextVerticalAlignment.Top);
            }
            for (int i = 0; i < _yp.Count; i++)
            {
                var size = VisualizeModel.VisualizeModel.fontsize; Point3d point = _yp[i]; plane.Origin = point;
                args.Viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                args.Display.Draw3dText(_ytext[i], Color.Black, plane, size, "", false, false, Rhino.DocObjects.TextHorizontalAlignment.Right, Rhino.DocObjects.TextVerticalAlignment.Middle);
            }
            for (int i = 0; i < _tp.Count; i++)
            {
                var size = VisualizeModel.VisualizeModel.fontsize; Point3d point = _tp[i]; plane.Origin = point;
                args.Viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                args.Display.Draw3dText(_ttext[i], Color.Black, plane, size, "", false, false, Rhino.DocObjects.TextHorizontalAlignment.Center, Rhino.DocObjects.TextVerticalAlignment.Bottom);
            }
        }
    }
}