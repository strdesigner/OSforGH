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
    public class ReadSpring : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ReadSpring()
          : base("ReadSpring", "ReadSpring",
              "Read line data from Rhinoceros with selected layer and export spring element information for OpenSees",
              "OpenSees", "Reading from Rhino")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("layer", "layer", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("name K", "name K", "[kx+,kx-,ky+,ky-,kz+,kz-,mx,my,mz](Datalist)", GH_ParamAccess.list, new List<string> { "kxt","kxc", "kyt", "kyc", "kzt", "kzc", "mx", "my", "mz" });
            pManager.AddTextParameter("name angle", "name angle", "usertextname for code-angle", GH_ParamAccess.item, "angle");
            pManager.AddTextParameter("name N(for MSS)", "name N(for MSS)", "usertextname of number of spring for MSS(multi shear spring)", GH_ParamAccess.item, "ns");
            pManager.AddTextParameter("on_off", "on_off", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list, "");
            pManager.AddTextParameter("on_off(wick)", "on_off(wick)", "[wickname1,wickname2,...](Datalist)", GH_ParamAccess.list, "");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("slines", "slines", "Line of spring elements", GH_ParamAccess.list);
            pManager.AddNumberParameter("E", "E", "[[kx+,kx-,ky+,ky-,kz+,kz-,mx,my,mz],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddIntegerParameter("index", "index", "[int,int,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("names", "names", "[[layer,wick],[layer,wick],...](Datatree)", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> layer = new List<string>(); DA.GetDataList("layer", layer);
            List<string> Ename = new List<string>(); DA.GetDataList("name K", Ename);
            string name_angle = "angle"; DA.GetData("name angle", ref name_angle);
            string name_N = "ns"; DA.GetData("name N(for MSS)", ref name_N);
            List<string> on_off = new List<string>(); string name_x = "wickX"; string name_y = "wickY"; List<string> on_off2 = new List<string>();
            DA.GetDataList("on_off", on_off); DA.GetDataList("on_off(wick)", on_off2); List<Curve> lines = new List<Curve>(); List<int> index = new List<int>();
            GH_Structure<GH_Number> E = new GH_Structure<GH_Number>();
            var names = new GH_Structure<GH_String>();
            var doc = RhinoDoc.ActiveDoc; int e = 0;
            var rigid = 1e+10; var pin = 0.0001;//joint stiffness
            if (on_off[0] == "") { on_off = layer; }
            for (int i = 0; i < layer.Count; i++)
            {
                var line = doc.Objects.FindByLayer(layer[i]);
                for (int j = 0; j < line.Length; j++)
                {
                    var obj = line[j];
                    var l = (new ObjRef(obj)).Curve(); lines.Add(l);
                    List<GH_Number> Elist = new List<GH_Number>();
                    var text = obj.Attributes.GetUserString(Ename[0]);//kxt
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(int.Parse(text))); }
                    text = obj.Attributes.GetUserString(Ename[1]);//kxc
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(int.Parse(text))); }
                    text = obj.Attributes.GetUserString(Ename[2]);//kyt
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(int.Parse(text))); }
                    text = obj.Attributes.GetUserString(Ename[3]);//kyc
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(int.Parse(text))); }
                    text = obj.Attributes.GetUserString(Ename[4]);//kzt
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(int.Parse(text))); }
                    text = obj.Attributes.GetUserString(Ename[5]);//kzc
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(int.Parse(text))); }
                    text = obj.Attributes.GetUserString(Ename[6]);//mx
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(int.Parse(text))); }
                    text = obj.Attributes.GetUserString(Ename[7]);//my
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(int.Parse(text))); }
                    text = obj.Attributes.GetUserString(Ename[8]);//mz
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(int.Parse(text))); }
                    text = obj.Attributes.GetUserString(name_angle);//angle
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(int.Parse(text))); }
                    text = obj.Attributes.GetUserString(name_N);//N for MSS
                    if (text == null) { }
                    else { Elist.Add(new GH_Number(int.Parse(text))); }
                    string text1 = obj.Attributes.GetUserString(name_x); string text2 = obj.Attributes.GetUserString(name_y);//軸ラベル
                    if (on_off.Contains(layer[i]) == true)
                    {
                        if (on_off2[0] != "")
                        {
                            if (on_off2.Contains(text1) == true || on_off2.Contains(text2) == true)
                            {
                                index.Add(e);//指定軸が含まれていればindexを格納
                            }
                        }
                        else
                        {
                            index.Add(e);
                        }
                    }
                    var namelist = new List<GH_String>(); namelist.Add(new GH_String(layer[i]));
                    if (text1 != null) { namelist.Add(new GH_String(text1)); }
                    if (text2 != null) { namelist.Add(new GH_String(text2)); }
                    names.AppendRange(namelist, new GH_Path(e));
                    E.AppendRange(Elist, new GH_Path(e));
                    e += 1;
                }
            }
            if (on_off[0] == "" && on_off2[0] == "") { index.Add(-9999); }
            else if (index.Count == 0) { index.Add(9999); }
            DA.SetDataList("slines", lines);
            DA.SetDataTree(1, E);
            DA.SetDataList("index", index);
            DA.SetDataTree(3, names);
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
                return OpenSeesUtility.Properties.Resources.readspring;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("568d5f10-0c5a-4cac-8c0a-6620316dbe3f"); }
        }
    }
}