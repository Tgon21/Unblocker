using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class UnblockerForm : Form
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool DeleteFile(string lpFileName);

    const int ERROR_FILE_NOT_FOUND = 2;
    const int ERROR_PATH_NOT_FOUND = 3;

    static readonly Color BgColor       = Color.FromArgb(30, 30, 30);
    static readonly Color SurfaceColor  = Color.FromArgb(45, 45, 48);
    static readonly Color BorderColor   = Color.FromArgb(70, 70, 74);
    static readonly Color TextColor     = Color.FromArgb(232, 232, 232);
    static readonly Color MutedTextColor= Color.FromArgb(180, 180, 180);
    static readonly Color AccentColor   = Color.FromArgb(45, 110, 70);
    static readonly Color AccentHover   = Color.FromArgb(55, 135, 85);

    TextBox pathBox;
    TextBox status;
    Button unblockBtn;

    public UnblockerForm()
    {
        Text = "File / Folder Unblocker";
        ClientSize = new Size(610, 330);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = BgColor;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 9);
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        var instructions = new Label {
            Location = new Point(15, 12),
            Size = new Size(580, 75),
            ForeColor = MutedTextColor,
            Text =
                "Removes the \"downloaded from the internet\" block (Zone.Identifier) from any\r\n" +
                "file or folder. For folders, every file inside (recursively) is unblocked.\r\n\r\n" +
                "1. Paste a path below, drag-and-drop a file/folder, or click Browse.\r\n" +
                "2. Click Unblock. Results appear in the box at the bottom."
        };

        var pathLabel = new Label {
            Location = new Point(15, 92),
            Size = new Size(100, 18),
            Text = "Path:",
            ForeColor = TextColor
        };

        pathBox = new TextBox {
            Location = new Point(15, 112),
            Size = new Size(465, 25),
            AllowDrop = true,
            BackColor = SurfaceColor,
            ForeColor = TextColor,
            BorderStyle = BorderStyle.FixedSingle
        };
        pathBox.DragEnter += (s, e) => {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        };
        pathBox.DragDrop += (s, e) => {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0) pathBox.Text = files[0];
        };

        var browseFile = MakeButton("Browse File...", new Point(490, 110), new Size(105, 28), false);
        browseFile.Click += (s, e) => {
            using (var dlg = new OpenFileDialog()) {
                dlg.Filter = "All files (*.*)|*.*";
                if (dlg.ShowDialog() == DialogResult.OK) pathBox.Text = dlg.FileName;
            }
        };

        var browseFolder = MakeButton("Browse Folder...", new Point(490, 144), new Size(105, 28), false);
        browseFolder.Click += (s, e) => {
            using (var dlg = new FolderBrowserDialog()) {
                if (dlg.ShowDialog() == DialogResult.OK) pathBox.Text = dlg.SelectedPath;
            }
        };

        unblockBtn = MakeButton("Unblock", new Point(15, 144), new Size(130, 28), true);
        unblockBtn.Click += OnUnblock;

        status = new TextBox {
            Location = new Point(15, 184),
            Size = new Size(580, 135),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9),
            BackColor = SurfaceColor,
            ForeColor = TextColor,
            BorderStyle = BorderStyle.FixedSingle
        };

        Controls.AddRange(new Control[] {
            instructions, pathLabel, pathBox, browseFile, browseFolder, unblockBtn, status
        });
        AcceptButton = unblockBtn;
    }

    static Button MakeButton(string text, Point loc, Size sz, bool accent)
    {
        var b = new Button {
            Text = text,
            Location = loc,
            Size = sz,
            FlatStyle = FlatStyle.Flat,
            ForeColor = TextColor,
            BackColor = accent ? AccentColor : SurfaceColor,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderColor = accent ? AccentHover : BorderColor;
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.MouseOverBackColor = accent ? AccentHover : BorderColor;
        b.FlatAppearance.MouseDownBackColor = accent ? AccentColor : BgColor;
        return b;
    }

    void Log(string msg) { status.AppendText(msg + "\r\n"); }

    void OnUnblock(object sender, EventArgs e)
    {
        var target = pathBox.Text.Trim().Trim('"').Trim();
        status.Clear();
        if (string.IsNullOrEmpty(target)) { Log("No path provided."); return; }

        Cursor = Cursors.WaitCursor;
        unblockBtn.Enabled = false;
        try {
            if (Directory.Exists(target)) {
                Log("Scanning folder: " + target);
                string[] files;
                try {
                    files = Directory.GetFiles(target, "*", SearchOption.AllDirectories);
                } catch (Exception ex) {
                    Log("Error scanning: " + ex.Message);
                    return;
                }
                Log("Found " + files.Length + " file(s).");
                int ok = 0, clean = 0, fail = 0;
                string err;
                foreach (var f in files) {
                    if (TryUnblock(f, out err)) ok++;
                    else if (err == null) clean++;
                    else { fail++; Log("  ! " + f + " -- " + err); }
                }
                Log("Done. Unblocked: " + ok + ", already clean: " + clean +
                    (fail > 0 ? ", errors: " + fail : "") + ".");
            }
            else if (File.Exists(target)) {
                string err;
                if (TryUnblock(target, out err)) Log("Unblocked: " + target);
                else if (err == null) Log("Already unblocked: " + target);
                else Log("Error: " + err);
            }
            else {
                Log("Path does not exist: " + target);
            }
        }
        finally {
            Cursor = Cursors.Default;
            unblockBtn.Enabled = true;
        }
    }

    static bool TryUnblock(string filePath, out string error)
    {
        error = null;
        if (DeleteFile(filePath + ":Zone.Identifier")) return true;
        int code = Marshal.GetLastWin32Error();
        if (code == ERROR_FILE_NOT_FOUND || code == ERROR_PATH_NOT_FOUND) return false;
        error = "Win32 error " + code;
        return false;
    }

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new UnblockerForm());
    }
}
