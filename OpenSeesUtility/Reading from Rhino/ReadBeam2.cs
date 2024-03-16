using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using System.Drawing;
using System.Windows.Forms;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using System.Diagnostics;

namespace ReadBeam2
{
    public class ReadBeam2 : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ReadBeam2()
          : base("ReadBeam2", "ReadBeam2",
              "Read non-divided line data from Rhinoceros with selected layer and export elastic beam information for OpenSees",
              "OpenSees", "Reading from Rhino")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("layer(beam)", "layer(beam)", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("name mat", "name mat", "usertextname for material", GH_ParamAccess.item, "mat");
            pManager.AddTextParameter("name sec", "name sec", "usertextname for section", GH_ParamAccess.item, "sec");
            pManager.AddTextParameter("name angle", "name angle", "usertextname for code-angle", GH_ParamAccess.item, "angle");
            pManager.AddTextParameter("name joint", "name joint", "usertextname for pin-joint", GH_ParamAccess.item, "joint");
            pManager.AddTextParameter("name lb", "name lb", "usertextname for buckling length(local-y and z axis)", GH_ParamAccess.list, new List<string> { "lby","lbz" });
            pManager.AddTextParameter("name bar", "name bar", "usertextname for reinforcement number", GH_ParamAccess.item, "bar");
            pManager.AddTextParameter("name wick", "name wick", "usertextname for wick1 and wick2", GH_ParamAccess.list, new List<string> { "wickX","wickY", "wickZ", "wickW" });
            pManager.AddTextParameter("name ele_w", "name ele_w", "element force Wx", GH_ParamAccess.list, new List<string> { "ele_wx", "ele_wy", "ele_wz", "ele_wx2", "ele_wy2", "ele_wz2", "ele_l1", "ele_l2" });
            pManager.AddTextParameter("layer(spring)", "layer(spring)", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("name K", "name K", "[kx+,kx-,ky+,ky-,kz+,kz-,mx,my,mz](Datalist)", GH_ParamAccess.list, new List<string> { "kxt", "kxc", "kyt", "kyc", "kzt", "kzc", "mx", "my", "mz" });
            pManager.AddTextParameter("name angle(spring)", "name angle(spring)", "usertextname for code-angle", GH_ParamAccess.item, "angle");
            pManager.AddTextParameter("name spring_a", "name spring_a", "usertextname for allowable force of spring", GH_ParamAccess.list, new List<string> { "Nta", "Nca", "Qyta", "Qyca", "Qzta", "Qzca", "Mxa", "Mya", "Mza" });
            pManager.AddNumberParameter("rigid & pin value", "rigid & pin value", "[para_rigid,para_pin](Datalist) stiffness of rigid and pin joint", GH_ParamAccess.list, new List<double> { 1.0e+12, 0.001 });
            pManager.AddNumberParameter("accuracy", "accuracy", "oversection accuracy", GH_ParamAccess.item, 5e-3);
            pManager[9].Optional = true;
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
            List<string> layer = new List<string>(); string name_mat = "mat"; string name_sec = "sec"; string name_angle = "angle"; string name_joint = "joint"; string name_lby = "lby"; string name_lbz = "lbz"; string name_bar = "bar"; string name_ele_wx = "ele_wx"; string name_ele_wy = "ele_wy"; string name_ele_wz = "ele_wz"; string name_ele_wx2 = "ele_wx2"; string name_ele_wy2 = "ele_wy2"; string name_ele_wz2 = "ele_wz2"; string name_ele_l1 = "ele_l1"; string name_ele_l2 = "ele_l2"; string name_x = "wickX"; string name_y = "wickY"; string name_z = "wickZ"; string name_w = "wickW";
            DA.GetDataList("layer(beam)", layer); DA.GetData("name mat", ref name_mat); DA.GetData("name sec", ref name_sec); DA.GetData("name angle", ref name_angle); DA.GetData("name joint", ref name_joint); var name_lb = new List<string> {"lby","lbz" }; DA.GetDataList("name lb", name_lb); name_lby = name_lb[0]; name_lbz = name_lb[1]; var acc = 5e-3; DA.GetData("accuracy", ref acc);
            DA.GetData("name bar", ref name_bar);
            var name_ele_w = new List<string>(); DA.GetDataList("name ele_w", name_ele_w); name_ele_wx = name_ele_w[0]; name_ele_wy = name_ele_w[1]; name_ele_wz = name_ele_w[2];
            if (name_ele_w.Count == 8) { name_ele_wx2 = name_ele_w[3]; name_ele_wy2 = name_ele_w[4]; name_ele_wz2 = name_ele_w[5]; name_ele_l1 = name_ele_w[6]; name_ele_l2 = name_ele_w[7]; }
            var name_xyz = new List<string>(); DA.GetDataList("name wick", name_xyz); name_x = name_xyz[0]; name_y = name_xyz[1];
            if (name_xyz.Count >= 3) { name_z = name_xyz[2]; }
            if (name_xyz.Count >= 4) { name_w = name_xyz[3]; }
            var layer2 = new List<string>(); DA.GetDataList("layer(spring)", layer2);
            List<string> Ename = new List<string>(); DA.GetDataList("name K", Ename);
            string name_angle2 = ""; DA.GetData("name angle(spring)", ref name_angle2);
            var name_spring_a = new List<string>(); DA.GetDataList("name spring_a", name_spring_a);
            List<Curve> lines = new List<Curve>(); List<int> mat = new List<int>(); List<int> sec = new List<int>(); List<double> angle = new List<double>();
            List<double> lby = new List<double>(); List<double> lbz = new List<double>(); List<double> bar = new List<double>(); GH_Structure<GH_Number> joint = new GH_Structure<GH_Number>(); GH_Structure<GH_Number> e_load = new GH_Structure<GH_Number>(); var names = new GH_Structure<GH_String>();
            List<Line> lines_new = new List<Line>(); List<int> mat_new = new List<int>(); List<int> sec_new = new List<int>(); List<double> angle_new = new List<double>();
            List<double> lby_new = new List<double>(); List<double> lbz_new = new List<double>(); List<double> bar_new = new List<double>(); GH_Structure<GH_Number> joint_new = new GH_Structure<GH_Number>(); GH_Structure<GH_Number> e_load_new = new GH_Structure<GH_Number>(); List<int> index_new = new List<int>(); var joint_No = new List<int>(); var e_load_No = new List<int>();
            var names_new = new GH_Structure<GH_String>();
            var names2 = new GH_Structure<GH_String>(); List<Curve> lines2 = new List<Curve>(); List<int> index2 = new List<int>(); var E = new GH_Structure<GH_Number>(); var A = new GH_Structure<GH_Number>();
            var doc = RhinoDoc.ActiveDoc; int e = 0; int k = 0; int kk = 0;
            var rigid = 1e+12; var pin = 0.001;//joint stiffness
            var rigidpin = new List<double>(); DA.GetDataList("rigid & pin value", rigidpin); rigid = rigidpin[0]; pin = rigidpin[1];
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
                        if (text == null) { lby.Add(-1); }
                        else { lby.Add(float.Parse(text)); }
                        text = obj.Attributes.GetUserString(name_lbz);//部材z軸方向座屈長さ情報
                        if (text == null) { lbz.Add(-1); }
                        else { lbz.Add(float.Parse(text)); }
                        text = obj.Attributes.GetUserString(name_bar);//配筋情報
                        if (text == null) { bar.Add(0); }
                        else { bar.Add(int.Parse(text)); }
                        text = obj.Attributes.GetUserString(name_joint);//材端ピン情報
                        if (text != null)
                        {
                            List<GH_Number> jlist = new List<GH_Number>();
                            joint_No.Add(e);
                            jlist.Add(new GH_Number(e)); jlist.Add(new GH_Number(int.Parse(text)));
                            jlist.Add(new GH_Number(rigid)); jlist.Add(new GH_Number(rigid)); jlist.Add(new GH_Number(rigid));
                            jlist.Add(new GH_Number(rigid)); jlist.Add(new GH_Number(pin)); jlist.Add(new GH_Number(pin));
                            jlist.Add(new GH_Number(rigid)); jlist.Add(new GH_Number(rigid)); jlist.Add(new GH_Number(rigid));
                            jlist.Add(new GH_Number(rigid)); jlist.Add(new GH_Number(pin)); jlist.Add(new GH_Number(pin));
                            joint.AppendRange(jlist, new GH_Path(k));
                            k += 1;
                        }
                        var t1 = obj.Attributes.GetUserString(name_ele_wx); var t2 = obj.Attributes.GetUserString(name_ele_wy); var t3 = obj.Attributes.GetUserString(name_ele_wz);//分布荷重
                        var t4 = obj.Attributes.GetUserString(name_ele_wx2); var t5 = obj.Attributes.GetUserString(name_ele_wy2); var t6 = obj.Attributes.GetUserString(name_ele_wz2); var t7 = obj.Attributes.GetUserString(name_ele_l1); var t8 = obj.Attributes.GetUserString(name_ele_l2);//台形分布用
                        if (t1 != null || t2 != null || t3 != null)
                        {
                            e_load_No.Add(e);
                            var wx = 0.0; var wy = 0.0; var wz = 0.0;
                            if (t1 != null) { wx = float.Parse(t1); }
                            if (t2 != null) { wy = float.Parse(t2); }
                            if (t3 != null) { wz = float.Parse(t3); }
                            List<GH_Number> flist = new List<GH_Number>();
                            flist.Add(new GH_Number(e)); flist.Add(new GH_Number(wx)); flist.Add(new GH_Number(wy)); flist.Add(new GH_Number(wz));
                            if (t4 != null || t5 != null || t6 != null || t7 != null || t8 != null)
                            {
                                var wx2 = 0.0; var wy2 = 0.0; var wz2 = 0.0; var l1 = 0.0; var l2 = 0.0;
                                if (t4 != null) { wx2 = float.Parse(t4); }
                                if (t5 != null) { wy2 = float.Parse(t5); }
                                if (t6 != null) { wz2 = float.Parse(t6); }
                                if (t7 != null) { l1 = float.Parse(t7); }
                                if (t8 != null) { l2 = float.Parse(t8); }
                                flist.Add(new GH_Number(wx2)); flist.Add(new GH_Number(wy2)); flist.Add(new GH_Number(wz2)); flist.Add(new GH_Number(l1)); flist.Add(new GH_Number(l2));
                            }
                            e_load.AppendRange(flist, new GH_Path(kk));
                            kk += 1;
                        }
                        string text1 = obj.Attributes.GetUserString(name_x); string text2 = obj.Attributes.GetUserString(name_y); string text3 = obj.Attributes.GetUserString(name_z); string text4 = obj.Attributes.GetUserString(name_w);//軸ラベル
                        var namelist = new List<GH_String>(); namelist.Add(new GH_String(layer[i]));
                        if (text1 != null) { namelist.Add(new GH_String(text1)); }
                        if (text2 != null) { namelist.Add(new GH_String(text2)); }
                        if (text3 != null) { namelist.Add(new GH_String(text3)); }
                        if (text4 != null) { namelist.Add(new GH_String(text4)); }
                        names.AppendRange(namelist, new GH_Path(e));
                        e += 1;
                    }
                }
            }
            int e2 = 0;
            for (int i = 0; i < layer2.Count; i++)
            {
                var line = doc.Objects.FindByLayer(layer2[i]);
                for (int j = 0; j < line.Length; j++)
                {
                    var obj = line[j];
                    var l = (new ObjRef(obj)).Curve(); lines2.Add(l);
                    List<GH_Number> Elist = new List<GH_Number>();
                    var text = obj.Attributes.GetUserString(Ename[0]);//kxt
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(float.Parse(text))); }
                    text = obj.Attributes.GetUserString(Ename[1]);//kxc
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(float.Parse(text))); }
                    text = obj.Attributes.GetUserString(Ename[2]);//kyt
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(float.Parse(text))); }
                    text = obj.Attributes.GetUserString(Ename[3]);//kyc
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(float.Parse(text))); }
                    text = obj.Attributes.GetUserString(Ename[4]);//kzt
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(float.Parse(text))); }
                    text = obj.Attributes.GetUserString(Ename[5]);//kzc
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(float.Parse(text))); }
                    text = obj.Attributes.GetUserString(Ename[6]);//mx
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(float.Parse(text))); }
                    text = obj.Attributes.GetUserString(Ename[7]);//my
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(float.Parse(text))); }
                    text = obj.Attributes.GetUserString(Ename[8]);//mz
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(float.Parse(text))); }
                    text = obj.Attributes.GetUserString(name_angle);//angle
                    if (text == null) { Elist.Add(new GH_Number(0)); }
                    else { Elist.Add(new GH_Number(int.Parse(text))); }
                    E.AppendRange(Elist, new GH_Path(e2));
                    var Alist = new List<GH_Number>();
                    for (int jj = 0; jj < name_spring_a.Count; jj++)
                    {
                        text = obj.Attributes.GetUserString(name_spring_a[jj]);//allowable force tag name
                        if (text == null) { Alist.Add(new GH_Number(0)); }
                        else { Alist.Add(new GH_Number(float.Parse(text))); }
                    }
                    A.AppendRange(Alist, new GH_Path(e2));
                    string text1 = obj.Attributes.GetUserString(name_x); string text2 = obj.Attributes.GetUserString(name_y); string text3 = obj.Attributes.GetUserString(name_z); string text4 = obj.Attributes.GetUserString(name_w);//軸ラベル
                    var namelist = new List<GH_String>(); namelist.Add(new GH_String(layer2[i]));
                    if (text1 != null) { namelist.Add(new GH_String(text1)); }
                    if (text2 != null) { namelist.Add(new GH_String(text2)); }
                    if (text3 != null) { namelist.Add(new GH_String(text3)); }
                    if (text4 != null) { namelist.Add(new GH_String(text4)); }
                    names2.AppendRange(namelist, new GH_Path(e2));
                    index2.Add(e2);
                    e2 += 1;
                }
            }
            var xyz = new List<Point3d>();
            for (e = 0; e < lines.Count; e++)//節点生成
            {
                var r1 = lines[e].PointAtStart; var r2 = lines[e].PointAtEnd; var l1 = 10.0; var l2 = 10.0;
                for (int i = 0; i < xyz.Count; i++) { l1 = Math.Min(l1, (xyz[i] - r1).Length); }
                if (l1 > acc) { xyz.Add(r1); }
                for (int i = 0; i < xyz.Count; i++) { l2 = Math.Min(l2, (xyz[i] - r2).Length); }
                if (l2 > acc) { xyz.Add(r2); }
                for (e2 = 0; e2 < lines.Count; e2++)//中間交差点も考慮
                {
                    if (e2 != e)
                    {
                        var cp = Rhino.Geometry.Intersect.Intersection.CurveCurve(lines[e], lines[e2], acc, acc);
                        if (cp != null && cp.Count!=0)
                        {
                            var rc = cp[0].PointA;
                            l1 = 10.0;
                            for (int i = 0; i < xyz.Count; i++) { l1 = Math.Min(l1, (xyz[i] - rc).Length); }
                            if (l1 > acc) { xyz.Add(rc); }
                        }
                    }
                }
            }
            for (e = 0; e < lines2.Count; e++)//節点生成(spring)
            {
                var r1 = lines2[e].PointAtStart; var r2 = lines2[e].PointAtEnd; var l1 = 10.0; var l2 = 10.0;
                for (int i = 0; i < xyz.Count; i++) { l1 = Math.Min(l1, (xyz[i] - r1).Length); }
                if (l1 > acc) { xyz.Add(r1); }
                for (int i = 0; i < xyz.Count; i++) { l2 = Math.Min(l2, (xyz[i] - r2).Length); }
                if (l2 > acc) { xyz.Add(r2); }
            }
            k = -1; kk = 0; int kkk = 0;
            for (e = 0; e < lines.Count; e++)//交差判定を行い交差部で要素分割する
            {
                var r1 = lines[e].PointAtStart; var r2 = lines[e].PointAtEnd; var l0 = r2 - r1; var rc = new List<Point3d>();
                int ind = joint_No.IndexOf(e); int ind2 = e_load_No.IndexOf(e);
                for (int i = 0; i < xyz.Count; i++)
                {
                    var l1 = xyz[i] - r1;
                    if (l1.Length > acc && (r2 - xyz[i]).Length > acc)//線分上に節点がいるかどうかチェック
                    {
                        if ((l0 / l0.Length - l1 / l1.Length).Length < 1e-5 && l0.Length - l1.Length > acc)
                        {
                            rc.Add(xyz[i]);
                        }
                    }
                }
                if (rc.Count != 0)
                {
                    var llist = new List<double>();
                    for (int i = 0; i < rc.Count; i++)
                    {
                        llist.Add((rc[i] - r1).Length);
                    }
                    int[] idx = Enumerable.Range(0, rc.Count).ToArray<int>();//r1とr2の間の点のソート
                    Array.Sort<int>(idx, (a, b) => llist[a].CompareTo(llist[b]));
                    lines_new.Add(new Line(r1, rc[idx[0]])); k += 1;
                    mat_new.Add(mat[e]); sec_new.Add(sec[e]); angle_new.Add(angle[e]); bar_new.Add(bar[e]); names_new.AppendRange(names[e], new GH_Path(k)); index_new.Add(k);
                    if (ind >= 0) { if (joint[ind][1].Value == 0 || joint[ind][1].Value == 2) { joint_new.AppendRange(new List<GH_Number> { new GH_Number(k), new GH_Number(0), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(pin), new GH_Number(pin), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(pin), new GH_Number(pin) }, new GH_Path(kk)); kk += 1; } }
                    if (ind2 >= 0)
                    {
                        if (e_load[ind2].Count == 4)
                        {
                            e_load_new.AppendRange(new List<GH_Number> { new GH_Number(k), e_load[ind2][1], e_load[ind2][2], e_load[ind2][3] }, new GH_Path(kkk)); kkk += 1;
                        }
                        else if (e_load[ind2].Count == 9)
                        {
                            e_load_new.AppendRange(new List<GH_Number> { new GH_Number(k), e_load[ind2][1], e_load[ind2][2], e_load[ind2][3], e_load[ind2][4], e_load[ind2][5], e_load[ind2][6], e_load[ind2][7], e_load[ind2][8] }, new GH_Path(kkk)); kkk += 1;
                        }
                    }
                    if (lby[e] == -1) { lby_new.Add(new Line(r1, rc[idx[0]]).Length); }
                    else { lby_new.Add(lby[e]); }
                    if (lbz[e] == -1) { lbz_new.Add(new Line(r1, rc[idx[0]]).Length); }
                    else { lbz_new.Add(lbz[e]); }
                    for (int i = 0; i < idx.Length - 1; i++)
                    {
                        lines_new.Add(new Line(rc[idx[i]], rc[idx[i + 1]])); k += 1;
                        mat_new.Add(mat[e]); sec_new.Add(sec[e]); angle_new.Add(angle[e]); bar_new.Add(bar[e]); names_new.AppendRange(names[e], new GH_Path(k)); index_new.Add(k);
                        if (ind2 >= 0)
                        {
                            if (e_load[ind2].Count == 4)
                            {
                                e_load_new.AppendRange(new List<GH_Number> { new GH_Number(k), e_load[ind2][1], e_load[ind2][2], e_load[ind2][3] }, new GH_Path(kkk)); kkk += 1;
                            }
                            else if (e_load[ind2].Count == 9)
                            {
                                e_load_new.AppendRange(new List<GH_Number> { new GH_Number(k), e_load[ind2][1], e_load[ind2][2], e_load[ind2][3], e_load[ind2][4], e_load[ind2][5], e_load[ind2][6], e_load[ind2][7], e_load[ind2][8] }, new GH_Path(kkk)); kkk += 1;
                            }
                        }
                        if (lby[e] == -1) { lby_new.Add(new Line(rc[idx[i]], rc[idx[i + 1]]).Length); }
                        else { lby_new.Add(lby[e]); }
                        if (lbz[e] == -1) { lbz_new.Add(new Line(rc[idx[i]], rc[idx[i + 1]]).Length); }
                        else { lbz_new.Add(lbz[e]); }
                    }
                    lines_new.Add(new Line(rc[idx[idx.Length - 1]], r2)); k += 1;
                    if (ind >= 0) { if (joint[ind][1].Value == 1 || joint[ind][1].Value == 2) { joint_new.AppendRange(new List<GH_Number> { new GH_Number(k), new GH_Number(1), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(pin), new GH_Number(pin), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(pin), new GH_Number(pin) }, new GH_Path(kk)); kk += 1; } }
                    if (ind2 >= 0)
                    {
                        if (e_load[ind2].Count == 4)
                        {
                            e_load_new.AppendRange(new List<GH_Number> { new GH_Number(k), e_load[ind2][1], e_load[ind2][2], e_load[ind2][3] }, new GH_Path(kkk)); kkk += 1;
                        }
                        else if (e_load[ind2].Count == 9)
                        {
                            e_load_new.AppendRange(new List<GH_Number> { new GH_Number(k), e_load[ind2][1], e_load[ind2][2], e_load[ind2][3], e_load[ind2][4], e_load[ind2][5], e_load[ind2][6], e_load[ind2][7], e_load[ind2][8] }, new GH_Path(kkk)); kkk += 1;
                        }
                    }
                    mat_new.Add(mat[e]); sec_new.Add(sec[e]); angle_new.Add(angle[e]); bar_new.Add(bar[e]); names_new.AppendRange(names[e], new GH_Path(k)); index_new.Add(k);
                    if (lby[e] == -1) { lby_new.Add(new Line(rc[idx[idx.Length - 1]], r2).Length); }
                    else { lby_new.Add(lby[e]); }
                    if (lbz[e] == -1) { lbz_new.Add(new Line(rc[idx[idx.Length - 1]], r2).Length); }
                    else { lbz_new.Add(lbz[e]); }
                }
                else
                {
                    lines_new.Add(new Line(r1, r2)); k += 1;
                    if (ind >= 0) { joint_new.AppendRange(new List<GH_Number> { new GH_Number(k), new GH_Number(joint[ind][1].Value), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(pin), new GH_Number(pin), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(rigid), new GH_Number(pin), new GH_Number(pin) }, new GH_Path(kk)); kk += 1; }
                    if (ind2 >= 0)
                    {
                        if (e_load[ind2].Count == 4)
                        {
                            e_load_new.AppendRange(new List<GH_Number> { new GH_Number(k), e_load[ind2][1], e_load[ind2][2], e_load[ind2][3] }, new GH_Path(kkk)); kkk += 1;
                        }
                        else if (e_load[ind2].Count == 9)
                        {
                            e_load_new.AppendRange(new List<GH_Number> { new GH_Number(k), e_load[ind2][1], e_load[ind2][2], e_load[ind2][3], e_load[ind2][4], e_load[ind2][5], e_load[ind2][6], e_load[ind2][7], e_load[ind2][8] }, new GH_Path(kkk)); kkk += 1;
                        }
                    }
                    mat_new.Add(mat[e]); sec_new.Add(sec[e]); angle_new.Add(angle[e]); bar_new.Add(bar[e]); names_new.AppendRange(names[e], new GH_Path(k)); index_new.Add(k);
                    if (lby[e] == -1) { lby_new.Add(new Line(r1, r2).Length); }
                    else { lby_new.Add(lby[e]); }
                    if (lbz[e] == -1) { lbz_new.Add(new Line(r1, r2).Length); }
                    else { lbz_new.Add(lbz[e]); }
                }
            }
            DA.SetDataList("beam", lines_new);
            DA.SetDataList("mat", mat_new);
            DA.SetDataList("sec", sec_new);
            DA.SetDataList("angle", angle_new);
            DA.SetDataTree(4, joint_new);
            DA.SetDataList("Lby", lby_new);
            DA.SetDataList("Lbz", lbz_new);
            DA.SetDataList("bar", bar_new);
            DA.SetDataTree(8, e_load_new);
            DA.SetDataList("index", index_new);
            DA.SetDataTree(10, names_new);
            DA.SetDataList("slines", lines2);
            DA.SetDataTree(12, E);
            DA.SetDataTree(13, A);
            DA.SetDataList("index(spring)", index2);
            DA.SetDataTree(15, names2);
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
                return OpenSeesUtility.Properties.Resources.readbeam2;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("bf97c9ec-8b7a-4210-a699-73a3fecd4091"); }
        }
    }
}