using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace OpenSeesUtility
{
    public class SurfLoad : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent2 class.
        /// </summary>
        public SurfLoad()
          : base("SetSurfLoads2", "SurfLoad2",
              "Set surface or floor load for OpenSees",
              "OpenSees", "PreProcess")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddTextParameter("layer", "layer", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("name Sz", "name Sz", "usertextname for pressure/floor load", GH_ParamAccess.item, "Sz");
            pManager.AddTextParameter("name Sz(for E)", "name Sz(for E)", "usertextname for pressure/floor load (for seismic load)", GH_ParamAccess.item, "Sz2");
            pManager.AddTextParameter("name Wz", "name Wz", "usertextname for 1 drection surf load", GH_ParamAccess.item, "Wz");
            pManager.AddTextParameter("name Wz(for E)", "name Wz(for E)", "usertextname for 1 drection surf load (for seismic load)", GH_ParamAccess.item, "Wz2");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("surface or floor load vector", "sf_load", "[[I,J,K,L,Wz],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("surface or floor load vector (for seismic load)", "sf_load(for E)", "[[I,J,K,L,Wz],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("1 drection surf load vector", "w_load", "[[I,J,K,L,Wz],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("1 drection surf load vector (for seismic load)", "w_load(for E)", "[[I,J,K,L,Wz],...](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var name_Sz = "Sz"; var name_Sz2 = "Sz2"; var name_Wz = "Wz"; var name_Wz2 = "Wz2";
            DA.GetData("name Sz", ref name_Sz); DA.GetData("name Sz(for E)", ref name_Sz2); DA.GetData("name Wz", ref name_Wz); DA.GetData("name Wz(for E)", ref name_Wz2);
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r); var r = _r.Branches;
            var layer = new List<string>(); DA.GetDataList("layer", layer);
            GH_Structure<GH_Number> s_load = new GH_Structure<GH_Number>(); GH_Structure<GH_Number> w_load = new GH_Structure<GH_Number>();
            GH_Structure<GH_Number> s_load2 = new GH_Structure<GH_Number>(); GH_Structure<GH_Number> w_load2 = new GH_Structure<GH_Number>();
            var doc = RhinoDoc.ActiveDoc; var szi = 0; var sz2i = 0; var wzi = 0; var wz2i = 0;
            for (int ii = 0; ii < layer.Count; ii++)
            {
                var shell = doc.Objects.FindByLayer(layer[ii]);
                for (int e = 0; e < shell.Length; e++)
                {
                    var obj = shell[e];
                    var surface = (new ObjRef(obj)).Surface();
                    var p = surface.ToNurbsSurface().Points;
                    Point3d r1; p.GetPoint(0, 0, out r1); Point3d r2; p.GetPoint(1, 0, out r2); Point3d r3; p.GetPoint(1, 1, out r3); Point3d r4; p.GetPoint(0, 1, out r4);
                    int i = 0; int j = 0; int k = 0; int l = -1;
                    for (i = 0; i < r.Count; i++) { if ((new Point3d(r[i][0].Value, r[i][1].Value, r[i][2].Value) - r1).Length <= 5e-3) { break; }; }
                    for (j = 0; j < r.Count; j++) { if ((new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value) - r2).Length <= 5e-3) { break; }; }
                    if (surface.IsSingular(0) == false && surface.IsSingular(1) == false && surface.IsSingular(2) == false && surface.IsSingular(3) == false)
                    {
                        for (k = 0; k < r.Count; k++) { if ((new Point3d(r[k][0].Value, r[k][1].Value, r[k][2].Value) - r3).Length <= 5e-3) { break; }; }
                        for (l = 0; l < r.Count; l++) { if ((new Point3d(r[l][0].Value, r[l][1].Value, r[l][2].Value) - r4).Length <= 5e-3) { break; }; }
                    }
                    else
                    {
                        for (int m = 0; m < r.Count; m++)
                        {
                            if (((new Point3d(r[m][0].Value, r[m][1].Value, r[m][2].Value) - r4).Length <= 5e-3 && (r1 - r4).Length > 5e-3 && (r2 - r4).Length > 5e-3) || ((new Point3d(r[m][0].Value, r[m][1].Value, r[m][2].Value) - r3).Length <= 5e-3 && (r1 - r3).Length > 5e-3 && (r2 - r3).Length > 5e-3))
                            {
                                k = m; break;
                            };
                        }
                    }
                    var text = obj.Attributes.GetUserString(name_Sz);//面荷重情報
                    if (text != null)
                    {
                        var sz = float.Parse(text); var ijkllist = new List<GH_Number>();
                        ijkllist.Add(new GH_Number(i)); ijkllist.Add(new GH_Number(j)); ijkllist.Add(new GH_Number(k)); ijkllist.Add(new GH_Number(l)); ijkllist.Add(new GH_Number(sz));
                        s_load.AppendRange(ijkllist, new GH_Path(szi)); szi += 1;
                    }
                    text = obj.Attributes.GetUserString(name_Sz2);//面荷重情報(地震用)
                    if (text != null)
                    {
                        var sz2 = float.Parse(text); var ijkllist = new List<GH_Number>();
                        ijkllist.Add(new GH_Number(i)); ijkllist.Add(new GH_Number(j)); ijkllist.Add(new GH_Number(k)); ijkllist.Add(new GH_Number(l)); ijkllist.Add(new GH_Number(sz2));
                        s_load2.AppendRange(ijkllist, new GH_Path(sz2i)); sz2i += 1;
                    }
                    text = obj.Attributes.GetUserString(name_Wz);//面荷重情報
                    if (text != null)
                    {
                        var wz = float.Parse(text); var ijkllist = new List<GH_Number>();
                        ijkllist.Add(new GH_Number(i)); ijkllist.Add(new GH_Number(j)); ijkllist.Add(new GH_Number(k)); ijkllist.Add(new GH_Number(l)); ijkllist.Add(new GH_Number(wz));
                        w_load.AppendRange(ijkllist, new GH_Path(wzi)); wzi += 1;
                    }
                    text = obj.Attributes.GetUserString(name_Wz2);//面荷重情報(地震用)
                    if (text != null)
                    {
                        var wz2 = float.Parse(text); var ijkllist = new List<GH_Number>();
                        ijkllist.Add(new GH_Number(i)); ijkllist.Add(new GH_Number(j)); ijkllist.Add(new GH_Number(k)); ijkllist.Add(new GH_Number(l)); ijkllist.Add(new GH_Number(wz2));
                        w_load2.AppendRange(ijkllist, new GH_Path(wz2i)); wz2i += 1;
                    }
                }
            }
            DA.SetDataTree(0, s_load); DA.SetDataTree(1, s_load2); DA.SetDataTree(2, w_load); DA.SetDataTree(3, w_load2);
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
                return OpenSeesUtility.Properties.Resources.surfaceload2;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2d465cfb-bfdc-4eec-84cd-47a522aebd6c"); }
        }
    }
}