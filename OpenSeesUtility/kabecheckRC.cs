using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Rhino.Display;
using Rhino.DocObjects;

using System.Drawing;
using System.Windows.Forms;
using System.IO;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
///****************************************
using System.Diagnostics;

using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp;

namespace kabecheckRC
{
    public class KabecheckRC : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public KabecheckRC()
          : base("Checking RCKABERYOU", "KabeCheckRC",
              "RCKABERYOU calculation based on Japanese Design Code (AIJ book)",
              "OpenSees", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("floor 1", "floor 1", "[floor1layername1,floor1layername2,...](Datalist)", GH_ParamAccess.list);//0
            pManager.AddTextParameter("floor 2", "floor 2", "[floor2layername1,floor2layername2,...](Datalist)", GH_ParamAccess.list);//1
            pManager.AddTextParameter("floor 3", "floor 3", "[floor3layername1,floor3layername2,...](Datalist)", GH_ParamAccess.list);//2
            pManager.AddTextParameter("floor 4", "floor 4", "[floor3layername1,floor3layername2,...](Datalist)", GH_ParamAccess.list);//3
            pManager.AddTextParameter("floor 5", "floor 5", "[floor3layername1,floor3layername2,...](Datalist)", GH_ParamAccess.list);//4
            pManager.AddTextParameter("wall 1", "wall 1", "[wall1layername1,wall1layername2,...](Datalist)", GH_ParamAccess.list);//5
            pManager.AddTextParameter("wall 2", "wall 2", "[wall2layername1,wall2layername2,...](Datalist)", GH_ParamAccess.list);//6
            pManager.AddTextParameter("wall 3", "wall 3", "[wall3layername1,wall3layername2,...](Datalist)", GH_ParamAccess.list);//7
            pManager.AddTextParameter("wall 4", "wall 4", "[wall2layername1,wall2layername2,...](Datalist)", GH_ParamAccess.list);//8
            pManager.AddTextParameter("wall 5", "wall 5", "[wall3layername1,wall3layername2,...](Datalist)", GH_ParamAccess.list);//9
            pManager.AddNumberParameter("parameter 1", "P1", "[■□HL[:B,〇●:R](DataList)", GH_ParamAccess.list);///10
            pManager.AddNumberParameter("parameter 2", "P2", "[■□HL[:D,〇:t,●:0](DataList)", GH_ParamAccess.list);///11
            pManager.AddTextParameter("name sec", "name sec", "usertextname for section", GH_ParamAccess.item, "sec");
            pManager.AddTextParameter("name angle", "name angle", "usertextname for code-angle", GH_ParamAccess.item, "angle");
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
            List<string> flayer1 = new List<string>(); if (!DA.GetDataList("floor 1", flayer1)) { flayer1 = new List<string>(); };
            List<string> flayer2 = new List<string>(); if (!DA.GetDataList("floor 2", flayer2)) { flayer2 = new List<string>(); };
            List<string> flayer3 = new List<string>(); if (!DA.GetDataList("floor 3", flayer3)) { flayer3 = new List<string>(); };
            List<string> flayer4 = new List<string>(); if (!DA.GetDataList("floor 4", flayer4)) { flayer4 = new List<string>(); };
            List<string> flayer5 = new List<string>(); if (!DA.GetDataList("floor 5", flayer5)) { flayer5 = new List<string>(); };
            List<string> wlayer1 = new List<string>(); if (!DA.GetDataList("wall 1", wlayer1)) { wlayer1 = new List<string>(); };
            List<string> wlayer2 = new List<string>(); if (!DA.GetDataList("wall 2", wlayer2)) { wlayer2 = new List<string>(); };
            List<string> wlayer3 = new List<string>(); if (!DA.GetDataList("wall 3", wlayer3)) { wlayer3 = new List<string>(); };
            List<string> wlayer4 = new List<string>(); if (!DA.GetDataList("wall 4", wlayer4)) { wlayer4 = new List<string>(); };
            List<string> wlayer5 = new List<string>(); if (!DA.GetDataList("wall 5", wlayer5)) { wlayer5 = new List<string>(); };
            var wall1 = new List<List<double>>(); var wall2 = new List<List<double>>(); var wall3 = new List<List<double>>(); var wall4 = new List<List<double>>(); var wall5 = new List<List<double>>();//t,L,angle
            string name_sec = "sec"; DA.GetData("name sec", ref name_sec);
            if (flayer1.Count != 0 && wlayer1.Count != 0)
            {
                var A = new List<double>();//各階面積
                var doc = RhinoDoc.ActiveDoc;
                var a = 0.0;
                for (int i = 0; i < flayer1.Count; i++)//1F床
                {
                    var shell = doc.Objects.FindByLayer(flayer1[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        a += s.GetArea();//床面積
                    }
                    if (i == flayer1.Count - 1) { A.Add(a); }
                }
                a = 0.0;
                for (int i = 0; i < flayer2.Count; i++)//1F床
                {
                    var shell = doc.Objects.FindByLayer(flayer2[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        a += s.GetArea();//床面積
                    }
                    if (i == flayer2.Count - 1) { A.Add(a); }
                }
                a = 0.0;
                for (int i = 0; i < flayer3.Count; i++)//1F床
                {
                    var shell = doc.Objects.FindByLayer(flayer3[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        a += s.GetArea();//床面積
                    }
                    if (i == flayer3.Count - 1) { A.Add(a); }
                }
                a = 0.0;
                for (int i = 0; i < flayer4.Count; i++)//1F床
                {
                    var shell = doc.Objects.FindByLayer(flayer4[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        a += s.GetArea();//床面積
                    }
                    if (i == flayer4.Count - 1) { A.Add(a); }
                }
                a = 0.0;
                for (int i = 0; i < flayer5.Count; i++)//1F床
                {
                    var shell = doc.Objects.FindByLayer(flayer5[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        a += s.GetArea();//床面積
                    }
                    if (i == flayer5.Count - 1) { A.Add(a); }
                }
                for (int i = 0; i < wlayer1.Count; i++)//1F壁
                {
                    var line = doc.Objects.FindByLayer(wlayer1[i]);
                    for (int j = 0; j < line.Length; j++)
                    {
                        var obj = line[j]; Curve[] l = new Curve[] { (new ObjRef(obj)).Curve() };
                        var text = obj.Attributes.GetUserString(name_sec);//断面情報
                    }
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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("af67fccd-77bc-4e87-8ff9-163bb214f46b"); }
        }
    }
}