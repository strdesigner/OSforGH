using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Display;
///for GUI*********************************
using System.Windows.Forms;
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
///****************************************

namespace OpenSeesUtility
{
    public class ReadWave : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ReadWave()
          : base("ReadWave", "ReadWave",
              "Set seismic wave acc data for time history analysis",
              "OpenSees", "PreProcess")
        {
        }
        public override bool IsPreviewCapable { get { return true; } }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("filename", "filename", "input csv file path (must be [sec & m/sec^2])", GH_ParamAccess.list,"");///
            pManager.AddNumberParameter("x", "x", "x-width of graph", GH_ParamAccess.item, 20);///
            pManager.AddNumberParameter("y", "y", "y-width of graph", GH_ParamAccess.item, 10);///
            pManager.AddNumberParameter("origin", "origin", "drawing origin", GH_ParamAccess.list, new List<double> { 0,0,0});///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("time", "time", "[[time1,time2,],...](DataTree)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("acc", "acc", "[[acc1,acc2,],...](DataTree)", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var filenames = new List<string>(); if (!DA.GetDataList("filename", filenames)) return; if (filenames[0] == "") return;
            var X = 0.0; DA.GetData("x", ref X); var Y = 0.0; DA.GetData("y", ref Y); var cp = new List<double>(); DA.GetDataList("origin", cp);
            var time = new GH_Structure<GH_Number>(); var acc = new GH_Structure<GH_Number>(); var vel = new GH_Structure<GH_Number>(); var disp = new GH_Structure<GH_Number>();
            var tmax = 0.0; var amax = 0.0; var vmax = 0.0; var dmax = 0.0; var colors = new List<Color> { Color.Red, Color.Blue, Color.DarkOrange, Color.Gold, Color.LightPink };
            for (int e = 0; e < filenames.Count; e++)
            {
                StreamReader sr = new StreamReader(@filenames[0]);// 読み込みたいCSVファイルのパスを指定して開く
                int k = 0;
                var tlist = new List<GH_Number>(); var alist = new List<GH_Number>(); var vlist = new List<GH_Number>(); var dlist = new List<GH_Number>();
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(',');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    if (double.Parse(values[0])!= 0 && k == 0)
                    {
                        tlist.Add(new GH_Number(0)); alist.Add(new GH_Number(0));
                    }
                    else
                    {
                        tlist.Add(new GH_Number(double.Parse(values[0]))); alist.Add(new GH_Number(double.Parse(values[1])));
                        tmax = Math.Max(Math.Abs(double.Parse(values[0])), tmax); amax = Math.Max(Math.Abs(double.Parse(values[1])), amax);
                    }
                    k += 1;
                }
                time.AppendRange(tlist, new GH_Path(e)); acc.AppendRange(alist, new GH_Path(e)); vel.AppendRange(vlist, new GH_Path(e)); disp.AppendRange(dlist, new GH_Path(e));
                    //外枠の描画
                _frame.Add(new Line(cp[0], cp[1], cp[2], cp[0] + X, cp[1], cp[2]));
                _frame.Add(new Line(cp[0], cp[1] + Y, cp[2], cp[0] + X, cp[1] + Y, cp[2]));
                _frame.Add(new Line(cp[0], cp[1], cp[2], cp[0], cp[1] + Y, cp[2]));
                _frame.Add(new Line(cp[0] + X, cp[1], cp[2], cp[0] + X, cp[1] + Y, cp[2]));
                var dx = X / tmax; var dy = Y / (amax * 2); var x = time.Branches; var y = acc.Branches;
                for (int i = 0; i < x.Count; i++)
                {
                    for (int j = 0; j < x[i].Count - 1; j++)
                    {
                        _wave.Add(new Line(cp[0] + x[i][j].Value * dx, cp[1] + (y[i][j].Value + amax) * dy, cp[2], cp[0] + x[i][j + 1].Value * dx, cp[1] + (y[i][j + 1].Value + amax) * dy, cp[2])); _c.Add(colors[e]);
                    }
                }
            }
            DA.SetDataTree(0, time); DA.SetDataTree(1, acc);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return OpenSeesUtility.Properties.Resources.readwave;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2f49506d-90dd-4d50-9109-42ae7a13312f"); }
        }
        ///ここからカスタム関数群********************************************************************************
        private readonly List<Line> _frame = new List<Line>();
        private readonly List<Line> _wave = new List<Line>();
        private readonly List<Color> _c = new List<Color>();
        protected override void BeforeSolveInstance()
        {
            _frame.Clear();
            _wave.Clear();
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            args.Viewport.GetFrustumFarPlane(out Plane plane);
            RhinoViewport viewport = args.Viewport;
            for (int i = 0; i < _frame.Count; i++)
            {
                args.Display.DrawLine(_frame[i], Color.Black);
            }
            for (int i = 0; i < _wave.Count; i++)
            {
                args.Display.DrawLine(_wave[i], _c[i]);
            }
        }
    }
}