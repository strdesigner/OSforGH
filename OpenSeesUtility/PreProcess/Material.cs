using System;
using System.IO;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace OpenSeesUtility
{
    public class Material : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public Material()
          : base("Set material parameters", "Material",
              "default=[Cedar,Pine,Fc24,Steel]",
              "OpenSees", "PreProcess")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("filename", "filename", "input csv file path", GH_ParamAccess.item, " ");///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("E", "E", "[young's modulus1, young's modulus2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("poi", "poi", "[poison's ratio1, poison's ratio2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("rho", "rho", "[unit weight1, unit weight2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Fc", "Fc", "for RC or Timber [Fc1,Fc2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Ft", "Ft", "for Timber [Ft1,Ft2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Fb", "Fb", "for Timber [Fb1,Fb2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Fs", "Fs", "for Timber [Fs1,Fs2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("F", "F", "for Steel[N/mm2] [Yield stress1,Yield stress2,...](Datalist)", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var E = new List<double> { 4.5e+6, 6.5e+6, 2.1e+7, 2.05e+8 }; var poi = new List<double> { 0.4, 0.4, 0.2, 0.3 }; var rho = new List<double> { 5.0, 5.0, 24.0, 77.0 };
            var Fc = new List<double> { 17.7, 22.2, 24, 0.0 }; var Ft = new List<double> { 13.5, 17.7, 0.0, 0.0 }; var Fb = new List<double> { 22.2, 28.2, 0.0, 0.0 }; var Fs = new List<double> { 1.8, 2.4, 0.0, 0.0 };
            var F = new List<double> { 0.0, 0.0, 0.0, 235.0 };
            string filename = " "; DA.GetData("filename", ref filename);
            if (filename != " ")
            {
                E = new List<double>(); poi = new List<double>(); rho = new List<double>(); Fc = new List<double>(); Ft = new List<double>(); Fb = new List<double>(); Fs = new List<double>(); F = new List<double>();
                StreamReader sr = new StreamReader(@filename);// 読み込みたいCSVファイルのパスを指定して開く
                int k = 0;
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    if (k != 0)
                    {
                        string[] values = line.Split(',');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                        E.Add(double.Parse(values[1])); poi.Add(double.Parse(values[2])); rho.Add(double.Parse(values[3])); Fc.Add(double.Parse(values[4])); Ft.Add(double.Parse(values[5])); Fb.Add(double.Parse(values[6])); Fs.Add(double.Parse(values[7])); F.Add(double.Parse(values[8]));
                    }
                    k += 1;
                }
            }
            DA.SetDataList("E", E); DA.SetDataList("poi", poi); DA.SetDataList("rho", rho); DA.SetDataList("Fc", Fc); DA.SetDataList("Ft", Ft); DA.SetDataList("Fb", Fb); DA.SetDataList("Fs", Fs); DA.SetDataList("F", F);
        }
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return OpenSeesUtility.Properties.Resources.material;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("20475245-4d68-4ceb-bb8a-79cf90140c44"); }
        }
    }
}