using System.Reflection;

namespace StepClock;

public sealed class MainForm : Form
{
    private readonly Label _timeLabel;
    private readonly CheckBox _use24Hour;
    private readonly CheckBox _hideMilliseconds;
    private readonly TextBox _log;
    private readonly System.Windows.Forms.Timer _timer;

    public MainForm()
    {
        Text = "StepClock";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(480, 420);
        MinimumSize = new Size(400, 320);
        Font = new Font("Segoe UI", 10F);

        _timeLabel = new ClockLabel
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Consolas", 32F, FontStyle.Bold),
            AutoSize = false,
        };

        _use24Hour = new CheckBox
        {
            Text = "24-hour clock",
            AutoSize = true,
            Checked = true,
            Margin = new Padding(6),
        };
        _use24Hour.CheckedChanged += (_, _) => UpdateClock();

        _hideMilliseconds = new CheckBox
        {
            Text = "Hide milliseconds",
            AutoSize = true,
            Checked = true,
            Margin = new Padding(6),
        };
        _hideMilliseconds.CheckedChanged += (_, _) => UpdateClock();

        var options = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.None,
            WrapContents = false,
        };
        options.Controls.AddRange([_use24Hour, _hideMilliseconds]);

        _log = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = false,
            BackColor = SystemColors.Window,
            Font = new Font("Consolas", 11F),
        };

        var stampButton = MakeButton("Time Stamp", StampClicked);
        var copyButton = MakeButton("Copy", CopyClicked);
        var clearButton = MakeButton("Clear", (_, _) => _log.Clear());

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.None,
            WrapContents = false,
        };
        buttons.Controls.AddRange([stampButton, copyButton, clearButton]);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(_timeLabel, 0, 0);
        layout.Controls.Add(options, 0, 1);
        layout.Controls.Add(_log, 0, 2);
        layout.Controls.Add(buttons, 0, 3);
        Controls.Add(layout);

        var aboutItem = new ToolStripMenuItem("&About StepClock...");
        aboutItem.Click += (_, _) => ShowAbout();
        var helpMenu = new ToolStripMenuItem("&Help");
        helpMenu.DropDownItems.Add(aboutItem);
        var menu = new MenuStrip();
        menu.Items.Add(helpMenu);
        Controls.Add(menu);
        MainMenuStrip = menu;

        AcceptButton = stampButton;

        _timer = new System.Windows.Forms.Timer { Interval = 10 };
        _timer.Tick += (_, _) => UpdateClock();
        _timer.Start();
        UpdateClock();
    }

    private string TimeFormat(bool includeMilliseconds) =>
        (_use24Hour.Checked ? "HH:mm:ss" : "hh:mm:ss")
        + (includeMilliseconds ? ".fff" : "")
        + (_use24Hour.Checked ? "" : " tt");

    private void UpdateClock()
    {
        var text = DateTime.Now.ToString(TimeFormat(!_hideMilliseconds.Checked));
        if (text == _timeLabel.Text) return;

        // Lock painting while the text changes, then unlock and repaint once.
        SendMessage(_timeLabel.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        try
        {
            _timeLabel.Text = text;
        }
        finally
        {
            SendMessage(_timeLabel.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            _timeLabel.Invalidate();
        }
    }

    private const int WM_SETREDRAW = 0x000B;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    /// <summary>Label that paints into an off-screen buffer so updates don't flicker.</summary>
    private sealed class ClockLabel : Label
    {
        public ClockLabel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint, true);
        }
    }

    private void StampClicked(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var index = _log.TextLength == 0 ? 1 : _log.Lines.Length + 1;
        var line = $"Stamp {index}: " + now.ToString(TimeFormat(includeMilliseconds: true));
        _log.AppendText(_log.TextLength == 0 ? line : Environment.NewLine + line);
    }

    private void CopyClicked(object? sender, EventArgs e)
    {
        if (_log.TextLength == 0)
        {
            Clipboard.Clear();
            return;
        }
        Clipboard.SetText(_log.Text);
    }

    private void ShowAbout()
    {
        var buildDate = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "BuildDate")?.Value ?? "unknown";

        MessageBox.Show(this,
            $"StepClock{Environment.NewLine}{Environment.NewLine}" +
            $"Built: {buildDate}{Environment.NewLine}" +
            "By Robert Achmann",
            "About StepClock",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static Button MakeButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(110, 34),
            Margin = new Padding(6),
        };
        button.Click += onClick;
        return button;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}
