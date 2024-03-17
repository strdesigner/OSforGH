using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace ReadBeam
{
    public class ReadBeam : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ReadBeam()
          : base("ReadBeam", "ReadBeam",
              "Read line data from Rhinoceros with selected layer and export elastic beam information for OpenSees",
              "OpenSees", "Reading from Rhino")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("layer(all)", "layer(all)", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("name mat", "name mat", "usertextname for material", GH_ParamAccess.item,"mat");
            pManager.AddTextParameter("name sec", "name sec", "usertextname for section", GH_ParamAccess.item, "sec");
            pManager.AddTextParameter("name angle", "name angle", "usertextname for code-angle", GH_ParamAccess.item, "angle");
            pManager.AddTextParameter("name joint", "name joint", "usertextname for pin-joint", GH_ParamAccess.item,"joint");
            pManager.AddTextParameter("name lby", "name lby", "usertextname for buckling length(local-y axis)", GH_ParamAccess.item,"lby");
            pManager.AddTextParameter("name lbz", "name lbz", "usertextname for buckling length(local-z axis)", GH_ParamAccess.item,"lbz");
            pManager.AddTextParameter("name bar", "name bar", "usertextname for reinforcement number", GH_ParamAccess.item, "bar");
            pManager.AddTextParameter("name wick1", "name wick1", "usertextname for wick1", GH_ParamAccess.item, "wickX");
            pManager.AddTextParameter("name wick2", "name wick2", "usertextname for wick2", GH_ParamAccess.item, "wickY");
            pManager.AddTextParameter("name ele_wx", "name ele_wx", "element force Wx", GH_ParamAccess.item, "ele_wx");
            pManager.AddTextParameter("name ele_wy", "name ele_wy", "element force Wy", GH_ParamAccess.item, "ele_wy");
            pManager.AddTextParameter("name ele_wz", "name ele_wz", "element force Wz", GH_ParamAccess.item, "ele_wz");
            pManager.AddTextParameter("on_off", "on_off", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list, "");
            pManager.AddTextParameter("on_off(wick)", "on_off(wick)", "[wickname1,wickname2,...](Datalist)", GH_ParamAccess.list, "");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("beam", "beam", "Line of elements", GH_ParamAccess.list);
            pManager.AddIntegerParameter("mat", "mat", "[int,int,...](Datalist)", GH_ParamAccess.list);
            pManager.AddIntegerParameter("sec", "sec", "[int,int,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("angle", "angle", "[float,float,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("joint", "joint", "[[Ele. No., 0 or 1(means i or j), kx, ky, kz, rx, ry, rz(spring value)],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("Lby", "Lby", "[float,float,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Lbz", "Lbz", "[float,float,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("bar", "bar", "[int,int,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("e_load", "e_load", "[[element No.,Wx,Wy,Wz],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddIntegerParameter("index", "index", "[int,int,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("names", "names", "[[layer,wick],[layer,wick],...](Datatree)", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> layer = new List<string>(); string name_mat = "mat"; string name_sec = "sec"; string name_angle = "angle"; string name_joint = "joint"; string name_lby = "lby"; string name_lbz = "lbz"; string name_bar = "bar"; string name_ele_wx = "ele_wx"; string name_ele_wy = "ele_wy"; string name_ele_wz = "ele_wz"; List<string> on_off = new List<string>(); string name_x = "wickX"; string name_y = "wickY"; List<string> on_off2 = new List<string>();
            DA.GetDataList("layer(all)", layer); DA.GetData("name mat", ref name_mat); DA.GetData("name sec", ref name_sec); DA.GetData("name angle", ref name_angle); DA.GetData("name joint", ref name_joint); DA.GetData("name lby", ref name_lby); DA.GetData("name lbz", ref name_lbz); DA.GetData("name bar", ref name_bar); DA.GetData("name ele_wx", ref name_ele_wx); DA.GetData("name ele_wy", ref name_ele_wy); DA.GetData("name ele_wz", ref name_ele_wz);
            DA.GetData("name wick1", ref name_x); DA.GetData("name wick2", ref name_y); DA.GetDataList("on_off", on_off); DA.GetDataList("on_off(wick)", on_off2);
            List<Curve> lines = new List<Curve>(); List<int> mat =new List<int>(); List<int> sec = new List<int>(); List<double> angle = new List<double>();
            List<double> lby = new List<double>(); List<double> lbz = new List<double>(); List<double> bar = new List<double>(); GH_Structure<GH_Number> joint = new GH_Structure<GH_Number>(); GH_Structure<GH_Number> e_load = new GH_Structure<GH_Number>(); List<int> index = new List<int>();
            var names= new GH_Structure<GH_String>();
            var doc = RhinoDoc.ActiveDoc; int e = 0; int k = 0; int kk = 0;
            var rigid = 1e+12; var pin = 0.001;//joint stiffness
            if (on_off[0] == ""){ on_off = layer; }
            for (int i = 0; i < layer.Count; i++)
            {
                var line = doc.Objects.FindByLayer(layer[i]);
                for (int j = 0; j < line.Length; j++)
                {
                    var obj = line[j];
                    var l = (new ObjRef(obj)).Curve(); lines.Add(l);　var length = l.GetLength();
                    var text = obj.Attributes.GetUserString(name_mat);//材料情報
                    if (text == null) { mat.Add(0); }
                    else { mat.Add(int.Parse(text)); }
                    text = obj.Attributes.GetUserString(name_sec);//断面情報
                    if (text == null) { sec.Add(0); }
                    else { sec.Add(int.Parse(text)); }
                    text = obj.Attributes.GetUserString(name_angle);//コードアングル情報
                    if (text == null) { angle.Add(0.0); }
                    else { angle.Add(float.Parse(text)); }
                    text = obj.Attributes.GetUserString(name_lby);//部材y軸方向座屈長さ情報
                    if (text == null) { lby.Add(length); }
                    else { lby.Add(float.Parse(text)); }
                    text = obj.Attributes.GetUserString(name_lbz);//部材z軸方向座屈長さ情報
                    if (text == null) { lbz.Add(length); }
                    else { lbz.Add(float.Parse(text)); }
                    text = obj.Attributes.GetUserString(name_bar);//配筋情報
                    if (text == null) { bar.Add(0); }
                    else { bar.Add(int.Parse(text)); }
                    text = obj.Attributes.GetUserString(name_joint);//材端ピン情報
                    if (text != null)
                    {
                        List<GH_Number> jlist = new List<GH_Number>();
                        jlist.Add(new GH_Number(e)); jlist.Add(new GH_Number(int.Parse(text)));
                        jlist.Add(new GH_Number(rigid)); jlist.Add(new GH_Number(rigid)); jlist.Add(new GH_Number(rigid));
                        jlist.Add(new GH_Number(rigid)); jlist.Add(new GH_Number(pin)); jlist.Add(new GH_Number(pin));
                        jlist.Add(new GH_Number(rigid)); jlist.Add(new GH_Number(rigid)); jlist.Add(new GH_Number(rigid));
                        jlist.Add(new GH_Number(rigid)); jlist.Add(new GH_Number(pin)); jlist.Add(new GH_Number(pin));
                        joint.AppendRange(jlist, new GH_Path(k));
                        k += 1;
                    }
                    var t1= obj.Attributes.GetUserString(name_ele_wx); var t2 = obj.Attributes.GetUserString(name_ele_wy); var t3 = obj.Attributes.GetUserString(name_ele_wz);//分布荷重
                    if(t1!=null || t2!=null|| t3 != null)
                    {
                        var wx = 0.0; var wy = 0.0; var wz = 0.0;
                        if (t1 != null) { wx = float.Parse(t1); }
                        if (t2 != null) { wy = float.Parse(t2); }
                        if (t3 != null) { wz = float.Parse(t3); }
                        List<GH_Number> flist = new List<GH_Number>();
                        flist.Add(new GH_Number(e)); flist.Add(new GH_Number(wx)); flist.Add(new GH_Number(wy)); flist.Add(new GH_Number(wz));
                        e_load.AppendRange(flist, new GH_Path(kk));
                        kk += 1;
                    }
                    string text1 = obj.Attributes.GetUserString(name_x); string text2 = obj.Attributes.GetUserString(name_y);//軸ラベル
                    if (on_off.Contains(layer[i]) == true)
                    {
                        if (on_off2[0]!="")
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
                    e += 1;
                }
            }
            if (on_off[0] == "" && on_off2[0] == "") { index.Add(-9999); }
            else if (index.Count == 0) { index.Add(9999); }
            DA.SetDataList("beam", lines);
            DA.SetDataList("mat", mat);
            DA.SetDataList("sec", sec);
            DA.SetDataList("angle", angle);
            DA.SetDataTree(4, joint);
            DA.SetDataList("Lby", lby);
            DA.SetDataList("Lbz", lbz);
            DA.SetDataList("bar", bar);
            DA.SetDataTree(8, e_load);
            DA.SetDataList("index", index);
            DA.SetDataTree(10, names);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        { get { return OpenSeesUtility.Properties.Resources.readbeam; } }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid { get { return new Guid("73a62eb2-e4ae-4f79-95e7-fff78ba558e6"); } }
    }
}