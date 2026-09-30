using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using CRM.winforms.Models;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class Step3MeasurementsNotesView : UserControl, IBookingWizardStep
{
    private Label lblClientNameVal = null!;
    private Label lblOnFileSizesVal = null!;
    private NumericUpDown numBust = null!;
    private NumericUpDown numWaist = null!;
    private NumericUpDown numHip = null!;
    private TextBox txtAlterationNotes = null!;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SendMessage(nint hWnd, int msg, nint wParam, string lParam);
    private const int EM_SETCUEBANNER = 0x1501;

    public string StepTitle => "Measurements & Notes";

    public Step3MeasurementsNotesView()
    {
        BuildLayout();
    }

    private void BuildLayout()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        Padding = new Padding(32, 20, 32, 20);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "Measurements & Alteration Notes",
            Font = new Font("Segoe UI Semibold", 15.5f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(0, 0),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        var pnlHeaderSpacer = new Panel { Dock = DockStyle.Top, Height = 20, BackColor = Color.Transparent };

        var pnlClientStrip = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.Transparent
        };

        var lblClientTag = new Label
        {
            Text = "CLIENT",
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = ColorSubtext,
            Location = new Point(0, 0),
            AutoSize = true
        };
        lblClientNameVal = new Label
        {
            Text = "—",
            Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(0, 20),
            AutoSize = true
        };

        var lblOnFileTag = new Label
        {
            Text = "MEASUREMENTS ON FILE",
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = ColorSubtext,
            Location = new Point(220, 0),
            AutoSize = true
        };
        lblOnFileSizesVal = new Label
        {
            Text = "—",
            Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(220, 20),
            AutoSize = true
        };

        pnlClientStrip.Controls.AddRange(new Control[] { lblClientTag, lblClientNameVal, lblOnFileTag, lblOnFileSizesVal });

        var pnlStripSpacer = new Panel { Dock = DockStyle.Top, Height = 18, BackColor = Color.Transparent };

        var pnlMeasurementsRow = new Panel
        {
            Dock = DockStyle.Top,
            Height = 74,
            BackColor = Color.Transparent
        };

        var pnlBustBox = CreateMeasurementBox("Bust (in)", out numBust, 0);
        var pnlWaistBox = CreateMeasurementBox("Waist (in)", out numWaist, 220);
        var pnlHipBox = CreateMeasurementBox("Hip (in)", out numHip, 440);

        pnlMeasurementsRow.Controls.AddRange(new Control[] { pnlBustBox, pnlWaistBox, pnlHipBox });

        var pnlMeasureSpacer = new Panel { Dock = DockStyle.Top, Height = 18, BackColor = Color.Transparent };

        var pnlNotesContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        var lblNotesTag = new Label
        {
            Text = "Alteration Notes",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = ColorSubtext,
            Dock = DockStyle.Top,
            Height = 22
        };

        var pnlNotesBorder = new Panel
        {
            Dock = DockStyle.Top,
            Height = 150,
            BackColor = ColorCardBg,
            Padding = new Padding(14, 12, 14, 12)
        };
        pnlNotesBorder.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, pnlNotesBorder.Width - 1, pnlNotesBorder.Height - 1), 6);
            using var pen = new Pen(ColorBorder, 1.25f);
            e.Graphics.DrawPath(pen, path);
        };

        txtAlterationNotes = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Multiline = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = ColorPrimary,
            BackColor = ColorCardBg,
            ScrollBars = ScrollBars.Vertical
        };

        pnlNotesBorder.Controls.Add(txtAlterationNotes);
        pnlNotesContainer.Controls.Add(pnlNotesBorder);
        pnlNotesContainer.Controls.Add(lblNotesTag);

        Controls.Add(pnlNotesContainer);
        Controls.Add(pnlMeasureSpacer);
        Controls.Add(pnlMeasurementsRow);
        Controls.Add(pnlStripSpacer);
        Controls.Add(pnlClientStrip);
        Controls.Add(pnlHeaderSpacer);
        Controls.Add(pnlHeader);

        txtAlterationNotes.HandleCreated += (s, e) =>
        {
            SendMessage(txtAlterationNotes.Handle, EM_SETCUEBANNER, 1, "e.g. Hem to ankle length, take in waist 1 inch, add bra cups...");
        };
    }

    private Panel CreateMeasurementBox(string labelText, out NumericUpDown numInput, int xPosition)
    {
        var box = new Panel
        {
            Location = new Point(xPosition, 0),
            Size = new Size(196, 70),
            BackColor = Color.Transparent
        };

        var lbl = new Label
        {
            Text = labelText,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = ColorSubtext,
            Location = new Point(0, 0),
            AutoSize = true
        };

        var borderPanel = new Panel
        {
            Location = new Point(0, 24),
            Size = new Size(196, 40),
            BackColor = ColorCardBg,
            Padding = new Padding(12, 9, 8, 8)
        };
        borderPanel.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, borderPanel.Width - 1, borderPanel.Height - 1), 6);
            using var pen = new Pen(ColorBorder, 1.25f);
            e.Graphics.DrawPath(pen, path);
        };

        numInput = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            BackColor = ColorCardBg,
            DecimalPlaces = 1,
            Minimum = 0,
            Maximum = 120,
            Value = 0
        };

        borderPanel.Controls.Add(numInput);
        box.Controls.Add(lbl);
        box.Controls.Add(borderPanel);

        return box;
    }

    public void OnStepEnter(BookingDraftModel draft)
    {
        if (draft.SelectedCustomer is { } customer)
        {
            lblClientNameVal.Text = $"{customer.FirstName} {customer.LastName}".Trim();

            string b = customer.BustSize > 0 ? $"{customer.BustSize:0.#}" : "—";
            string w = customer.WaistSize > 0 ? $"{customer.WaistSize:0.#}" : "—";
            string h = customer.HipSize > 0 ? $"{customer.HipSize:0.#}" : "—";
            lblOnFileSizesVal.Text = $"{b} – {w} – {h} in";

            numBust.Value = draft.FittingBust > 0 ? draft.FittingBust : customer.BustSize;
            numWaist.Value = draft.FittingWaist > 0 ? draft.FittingWaist : customer.WaistSize;
            numHip.Value = draft.FittingHip > 0 ? draft.FittingHip : customer.HipSize;
        }
        else
        {
            lblClientNameVal.Text = "—";
            lblOnFileSizesVal.Text = "—";
        }

        txtAlterationNotes.Text = draft.AlterationNotes ?? string.Empty;
    }

    public void OnStepLeave(BookingDraftModel draft)
    {
        draft.FittingBust = numBust.Value;
        draft.FittingWaist = numWaist.Value;
        draft.FittingHip = numHip.Value;
        draft.AlterationNotes = string.IsNullOrWhiteSpace(txtAlterationNotes.Text)
            ? null
            : txtAlterationNotes.Text.Trim();
    }

    public bool ValidateStep(out string errorMessage)
    {
        if (numBust.Value == 0 && numWaist.Value == 0 && numHip.Value == 0)
        {
            errorMessage = "Please enter the client's fitting measurements.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}