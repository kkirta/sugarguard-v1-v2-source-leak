using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaterialSkin;
using SugarGuard.Protector;
using SugarGuard.Protector.Enums;

namespace SugarGuard
{
    public partial class Main : Form
    {
        #region Imports
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        #endregion

        public Main()
        {
            InitializeComponent();

            MaterialSkinManager MSM = MaterialSkinManager.Instance;
            MSM.Theme = MaterialSkinManager.Themes.DARK;
            MSM.ColorScheme = new ColorScheme(Primary.Pink600, Primary.Pink600, Primary.Pink600, Accent.Pink400, TextShade.WHITE);

            Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 5, 5));

            CheckForIllegalCrossThreadCalls = false;
            AllowDrop = true;
        }

        #region Stuffs
        string Target = string.Empty;

        private void Main_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files) Target = file;
            FPath.Text = Path.GetFileName(Target);
        }

        private void Main_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void Main_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, 0xA1, 0x2, 0);
            }
        }

        private void Close_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        #endregion

        private void Protect_Click(object sender, EventArgs e)
        {
            new Task(() =>
            {
                Sugar SGR = new Sugar(Target);

                if (VM.Checked == true)
                    SGR.protections.Add(Protections.VM);

                if (CallConvertion.Checked == true)
                    SGR.protections.Add(Protections.CallConvertion);

                if (AntiDebug.Checked == true)
                    SGR.protections.Add(Protections.AntiDebug);

                if (AntiDump.Checked == true)
                    SGR.protections.Add(Protections.AntiDump);

                if (Constants.Checked == true)
                {
                    SGR.protections.Add(Protections.Constants);
                    SGR.protections.Add(Protections.PosConstants);
                }

                if (Mutation.Checked == true)
                    SGR.protections.Add(Protections.Mutation);

                if (ControlFlow.Checked == true)
                    SGR.protections.Add(Protections.ControlFlow);

                if (ReferenceProxy.Checked == true)
                    SGR.protections.Add(Protections.ReferenceProxy);

                if (InvalidOpcodes.Checked == true)
                    SGR.protections.Add(Protections.InvalidOpcodes);

                if (Renamer.Checked == true)
                    SGR.protections.Add(Protections.Renamer);

                if (FakeAttributes.Checked == true)
                    SGR.protections.Add(Protections.FakeAttributes);

                if (WaterMark.Checked == true)
                    SGR.protections.Add(Protections.WaterMark);

                //if (ReferenceOverload.Checked == true)
                //    SGR.protections.Add(Protections.ReferenceOverload);

                SGR.Protect();
                SGR.Save();

                Console.Beep(500, 500);
            }).Start();
        }

        private void FPath_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if(dialog.ShowDialog() == DialogResult.OK)
            {
                FPath.Text = dialog.FileName;
                Target = FPath.Text;
            }
        }
    }
}