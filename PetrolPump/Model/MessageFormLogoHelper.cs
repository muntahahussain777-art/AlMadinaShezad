using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AlMadinaShahzad.Model
{
    /// <summary>
    /// Yes/No, Error, Success message forms pe Al Madina logo (Al Noor replace).
    /// </summary>
    internal static class MessageFormLogoHelper
    {
        private static Image _cached;

        public static void Apply(PictureBox pictureBox)
        {
            if (pictureBox == null || pictureBox.IsDisposed) return;
            try
            {
                Image img = _cached ?? LoadLogo();
                if (img == null) return;
                _cached = img;
                pictureBox.Image = img;
                pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            }
            catch { }
        }

        private static Image LoadLogo()
        {
            string[] names =
            {
                "AlMadinaShahzadLogo.jpg",
                "AlMadinaShahzad Diesel & Cement Logo.jpg"
            };
            var roots = new[]
            {
                AppDomain.CurrentDomain.BaseDirectory,
                Application.StartupPath,
                Directory.GetCurrentDirectory()
            };

            foreach (string root in roots)
            {
                if (string.IsNullOrEmpty(root)) continue;
                var dir = new DirectoryInfo(root);
                for (int up = 0; up < 4 && dir != null; up++, dir = dir.Parent)
                {
                    foreach (string name in names)
                    {
                        string candidate = Path.Combine(dir.FullName, "Assets", name);
                        if (!File.Exists(candidate)) continue;
                        // Copy stream so file lock na rahe
                        byte[] bytes = File.ReadAllBytes(candidate);
                        return Image.FromStream(new MemoryStream(bytes));
                    }
                }
            }
            return null;
        }
    }
}
