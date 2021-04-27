using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Display;
using System.Drawing;
using System.Windows.Forms;

namespace ReadShell
{
    public class ReadShell : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ReadShell()
          : base("ReadShell", "ReadShell",
              "Read surface data from Rhinoceros with selected layer and export shell element information for OpenSees",
              "OpenSees", "Reading from Rhino")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("layer", "layer", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("name mat", "name mat", "usertextname for material", GH_ParamAccess.item, "mat");
            pManager.AddTextParameter("name t", "name t", "usertextname for plate thickness", GH_ParamAccess.item, "thick");
            pManager.AddTextParameter("name Sz", "name Sz", "usertextname for pressure/floor load", GH_ParamAccess.item, "Sz");
            pManager.AddTextParameter("name Sz(for E)", "name Sz(for E)", "usertextname for pressure/floor load(for seismic load)", GH_ParamAccess.item, "Sz2");
            pManager.AddTextParameter("name K", "name K", "usertextname for kabe/yuka bairitsu", GH_ParamAccess.item, "K");
            pManager.AddTextParameter("name rad", "name rad", "usertextname for layer angle", GH_ParamAccess.item, "rad");
            pManager.AddTextParameter("name wick", "name wick", "usertextname for wick1 and wick2", GH_ParamAccess.list, new List<string> { "wickX", "wickY" });
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("shell", "shell", "plate of elements", GH_ParamAccess.list);
            pManager.AddIntegerParameter("mat", "mat", "[int,int,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("thick", "thick", "[float,float,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("bairitsu", "bairitsu", "[float,float,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("rad", "rad", "[float,float,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Sz", "Sz", "[[No.,Sz],...](Datatree)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Sz(for E)", "Sz(for E)", "[[No.,Sz(for E)],...](Datatree)", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("index", "index", "[int,int,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("names", "names", "[[layer,wick],[layer,wick],...](Datatree)", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> layer = new List<string>(); string name_mat = "mat"; string name_t = "thick"; string name_Sz = "Sz"; string name_Sz2 = "Sz(for E)"; string name_K = "K"; string name_rad = "rad"; string name_x = "wickX"; string name_y = "wickY"; 
            DA.GetDataList("layer", layer); DA.GetData("name mat", ref name_mat); DA.GetData("name t", ref name_t); DA.GetData("name Sz", ref name_Sz); DA.GetData("name Sz(for E)", ref name_Sz2); DA.GetData("name K", ref name_K); DA.GetData("name rad", ref name_rad);
            var name_xy = new List<string>(); DA.GetDataList("name wick", name_xy); name_x = name_xy[0]; name_y = name_xy[1];
            List<Brep> shells = new List<Brep>(); List<int> mat = new List<int>(); List<double> t = new List<double>(); List<double> K = new List<double>(); List<double> rad = new List<double>();
            GH_Structure<GH_Number> Sz = new GH_Structure<GH_Number>(); GH_Structure<GH_Number> Sz2 = new GH_Structure<GH_Number>(); List<int> index = new List<int>();
            var names = new GH_Structure<GH_String>();
            var doc = RhinoDoc.ActiveDoc; int e = 0; int k = 0; int kk = 0;
            for (int i = 0; i < layer.Count; i++)
            {
                var shell = doc.Objects.FindByLayer(layer[i]);
                for (int j = 0; j < shell.Length; j++)
                {
                    var obj = shell[j];
                    var s = (new ObjRef(obj)).Brep(); shells.Add(s);
                    var text = obj.Attributes.GetUserString(name_mat);//材料情報
                    if (text == null) { mat.Add(0); }
                    else { mat.Add(int.Parse(text)); }
                    text = obj.Attributes.GetUserString(name_t);//板厚情報
                    if (text == null) { t.Add(0); }
                    else { t.Add(float.Parse(text)); }
                    text = obj.Attributes.GetUserString(name_K);//壁倍率情報
                    if (text == null) { K.Add(0.0); }
                    else { K.Add(float.Parse(text)); }
                    text = obj.Attributes.GetUserString(name_rad);//rad情報
                    if (text == null) { rad.Add(120); }
                    else { rad.Add(float.Parse(text)); }
                    text = obj.Attributes.GetUserString(name_Sz);//面荷重情報
                    if (text != null)
                    {
                        List<GH_Number> slist = new List<GH_Number>();
                        slist.Add(new GH_Number(e)); slist.Add(new GH_Number(float.Parse(text)));
                        Sz.AppendRange(slist, new GH_Path(k));
                        k += 1;
                    }
                    text = obj.Attributes.GetUserString(name_Sz2);//面荷重情報(地震用)
                    if (text != null)
                    {
                        List<GH_Number> slist = new List<GH_Number>();
                        slist.Add(new GH_Number(e)); slist.Add(new GH_Number(float.Parse(text)));
                        Sz2.AppendRange(slist, new GH_Path(kk));
                        kk += 1;
                    }
                    string text1 = obj.Attributes.GetUserString(name_x); string text2 = obj.Attributes.GetUserString(name_y);//軸ラベル
                    index.Add(e);
                    var namelist = new List<GH_String>(); namelist.Add(new GH_String(layer[i]));
                    if (text1 != null) { namelist.Add(new GH_String(text1)); }
                    if (text2 != null) { namelist.Add(new GH_String(text2)); }
                    names.AppendRange(namelist, new GH_Path(e));
                    e += 1;
                }
            }
            DA.SetDataList("shell", shells);
            DA.SetDataList("mat", mat);
            DA.SetDataList("thick", t);
            DA.SetDataList("bairitsu", K);
            DA.SetDataList("rad", rad);
            DA.SetDataTree(5, Sz);
            DA.SetDataTree(6, Sz2);
            DA.SetDataList("index", index);
            DA.SetDataTree(8, names);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon { get { return OpenSeesUtility.Properties.Resources.readshell; } }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid { get { return new Guid("17748853-9e98-4f51-8357-42d9e9335d44"); } }
    }
}