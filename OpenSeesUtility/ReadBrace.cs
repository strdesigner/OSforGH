using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace OpenSeesUtility
{
    public class ReadBrace : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ReadBrace()
          : base("ReadBrace", "ReadBrace",
              "Read line data from Rhinoceros with selected layer and export brace element as spring element information for OpenSees",
              "OpenSees", "Reading from Rhino")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("layer", "layer", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("name E,A,F", "name E,A,F", "usertextname for section-area[mm2], young's modulus[kN/m2], and F value[N/mm2]", GH_ParamAccess.list, new List<string> { "E", "A"});
            pManager.AddTextParameter("name wick", "name wick", "usertextname for wick1 and wick2", GH_ParamAccess.list, new List<string> { "wickX", "wickY", "wickZ" });
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("slines", "slines", "Line of spring elements", GH_ParamAccess.list);
            pManager.AddNumberParameter("E", "E", "[[kx+,kx-,ky+,ky-,kz+,kz-,mx,my,mz],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("spring_a", "spring_a", "[[Nta,Nca,Qyta,Qyca,Qzta,Qzca,Mxa,Mya,Mza],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddIntegerParameter("index(spring)", "index(spring)", "[int,int,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("names(spring)", "names(spring)", "[[layer,wick],[layer,wick],...](Datatree)", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name_x = "wickX"; string name_y = "wickY"; string name_z = "wickZ";
            var name_xyz = new List<string>(); DA.GetDataList("name wick", name_xyz); name_x = name_xyz[0]; name_y = name_xyz[1]; name_z = name_xyz[2];
            List<int> index_new = new List<int>();
            List<string> layer = new List<string>(); DA.GetDataList("layer", layer);
            List<string> EAFname = new List<string>(); DA.GetDataList("name E,A,F", EAFname);
            List<Curve> lines = new List<Curve>(); List<int> index = new List<int>();
            GH_Structure<GH_Number> E = new GH_Structure<GH_Number>(); GH_Structure<GH_Number> A = new GH_Structure<GH_Number>();
            var names = new GH_Structure<GH_String>();
            var doc = RhinoDoc.ActiveDoc; int e = 0;
            for (int i = 0; i < layer.Count; i++)
            {
                var line = doc.Objects.FindByLayer(layer[i]);
                for (int j = 0; j < line.Length; j++)
                {
                    var obj = line[j]; Curve[] l = new Curve[] { (new ObjRef(obj)).Curve() };
                    int nl = (new ObjRef(obj)).Curve().SpanCount;//ポリラインのセグメント数
                    if (nl > 1) { l = (new ObjRef(obj)).Curve().DuplicateSegments(); }
                    for (int jj = 0; jj < nl; jj++)
                    {
                        lines.Add(l[jj]);
                        var length = l[jj].GetLength();
                        List<GH_Number> Elist = new List<GH_Number>();
                        var Ei = 2.05e+8; var Ai = 0.0; var Fi = 235.0;
                        var text = obj.Attributes.GetUserString(EAFname[0]);//
                        if (text != null) { Ei = double.Parse(text); }
                        text = obj.Attributes.GetUserString(EAFname[1]);//
                        if (text == null) { RhinoApp.WriteLine("***error no section area is set"); }
                        else { Ai = double.Parse(text); }
                        text = obj.Attributes.GetUserString(EAFname[2]);//
                        if (text != null) { Fi = double.Parse(text); }
                        Elist.Add(new GH_Number(Ei * Ai*1.0e-6 / length));//kxt
                        for (int k = 0; k < 9; k++) { Elist.Add(new GH_Number(0)); }
                        E.AppendRange(Elist, new GH_Path(e));
                        var Alist = new List<GH_Number>();
                        Alist.Add(new GH_Number(Ai*Fi*1e-3));
                        for (int k = 0; k < 8; k++) { Alist.Add(new GH_Number(0)); }
                        A.AppendRange(Alist, new GH_Path(e));
                        string text1 = obj.Attributes.GetUserString(name_x); string text2 = obj.Attributes.GetUserString(name_y); string text3 = obj.Attributes.GetUserString(name_z);//軸ラベル
                        var namelist = new List<GH_String>(); namelist.Add(new GH_String(layer[i]));
                        if (text1 != null) { namelist.Add(new GH_String(text1)); }
                        if (text2 != null) { namelist.Add(new GH_String(text2)); }
                        if (text3 != null) { namelist.Add(new GH_String(text3)); }
                        names.AppendRange(namelist, new GH_Path(e));
                        index.Add(e);
                        e += 1;
                    }
                }
            }
            DA.SetDataList(0, lines);
            DA.SetDataTree(1, E);
            DA.SetDataTree(2, A);
            DA.SetDataList(3, index);
            DA.SetDataTree(4, names);
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
                return OpenSeesUtility.Properties.Resources.readbrace;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("d37797c0-c6f5-4cff-87bb-00d3011ac4da"); }
        }
    }
}