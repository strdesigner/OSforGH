using PdfSharp.Fonts;
using System;
using System.IO;
using System.Reflection;

namespace PdfCreate
{
    // 日本語フォントのためのフォントリゾルバー
    public class JapaneseFontResolver : IFontResolver
    {
        public static int fontset = 0;
        // 源真ゴシック（ http://jikasei.me/font/genshin/）
        private static readonly string GEN_SHIN_GOTHIC_MEDIUM_TTF =
            "OpenSeesUtility.fonts.GenShinGothic-Monospace-Medium.ttf";

        public byte[] GetFont(string faceName)
        {
            switch (faceName)
            {
                case "GenShinGothic#Medium":
                    return LoadFontData(GEN_SHIN_GOTHIC_MEDIUM_TTF);
            }
            return null;
        }

        public FontResolverInfo ResolveTypeface(
                    string familyName, bool isBold, bool isItalic)
        {
            var fontName = familyName.ToLower();

            switch (fontName)
            {
                case "gen shin gothic":
                    return new FontResolverInfo("GenShinGothic#Medium");
            }

            // デフォルトのフォント
            return PlatformFontResolver.ResolveTypeface("Arial", isBold, isItalic);
        }

        // 埋め込みリソースからフォントファイルを読み込む
        private byte[] LoadFontData(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new ArgumentException("No resource with name " + resourceName);

                int count = (int)stream.Length;
                byte[] data = new byte[count];
                stream.Read(data, 0, count);
                return data;
            }
        }
    }
}