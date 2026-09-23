using CRM.infrastructure.data;
using CRM.winforms.Models;
using CRM.winforms.Services.RentalBookingServices;

namespace CRM.winforms.Views;

public partial class NewBookingWizardView : UserControl
{
    private readonly Func<TenantCrmDbContext>? _contextFactory;
    private readonly Func<int>? _getCompanyId;
    private readonly Action? _onCloseWizard;

    private readonly BookingDraftModel _draft = new();
    private readonly List<IBookingWizardStep> _steps = new();
    private int _currentStepIndex = 0;

    public NewBookingWizardView()
    {
        InitializeComponent();
    }

    public NewBookingWizardView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId, Action? onCloseWizard = null)
    {
        InitializeComponent();

        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));
        _onCloseWizard = onCloseWizard;

        btnNext.BringToFront();
        btnCancel.BringToFront();

        btnNext.Click -= BtnNext_Click;
        btnNext.Click += BtnNext_Click;

        btnCancel.Click -= BtnCancel_Click;
        btnCancel.Click += BtnCancel_Click;

        InitializeSteps();
    }

    private void InitializeSteps()
    {
        if (_contextFactory == null || _getCompanyId == null) return;

        _steps.Clear();

        _steps.Add(new Step1ClientSelectionView(_contextFactory, _getCompanyId));

        try
        {
            _steps.Add(new Step2GarmentDatesView(_contextFactory, _getCompanyId));
        }
        catch
        {
            _steps.Add(new PlaceholderStepView("Step 2: Garment & Dates"));
        }

        _steps.Add(new Step3MeasurementsNotesView());

        _steps.Add(new Step4PaymentDepositView());

        _steps.Add(new Step5ConfirmationView());

        ShowStep(0);
    }

    private void ShowStep(int index)
    {
        if (index < 0 || index >= _steps.Count) return;

        _currentStepIndex = index;
        wizardStepperControl.CurrentStep = index + 1;

        panelContentHost.SuspendLayout();
        panelContentHost.Controls.Clear();

        var stepControl = (UserControl)_steps[_currentStepIndex];
        stepControl.Dock = DockStyle.Fill;
        panelContentHost.Controls.Add(stepControl);

        _steps[_currentStepIndex].OnStepEnter(_draft);
        panelContentHost.ResumeLayout();

        btnNext.BringToFront();
        btnCancel.BringToFront();

        btnCancel.Text = (_currentStepIndex == 0) ? "← Cancel" : "← Back";
        btnNext.Text = (_currentStepIndex == _steps.Count - 1) ? "Confirm Booking" : "Continue →";
    }

    private async void BtnNext_Click(object? sender, EventArgs e)
    {
        if (_steps.Count == 0) return;

        var step = _steps[_currentStepIndex];

        if (!step.ValidateStep(out string error))
        {
            MessageBox.Show(error, "Validation Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        step.OnStepLeave(_draft);

        if (_currentStepIndex < _steps.Count - 1)
        {
            ShowStep(_currentStepIndex + 1);
        }
        else
        {
            try
            {
                btnNext.Enabled = false;
                btnCancel.Enabled = false;
                btnNext.Text = "Saving...";

                var bookingService = new RentalBookingService(_contextFactory!);
                int bookingId = await bookingService.CreateBookingFromDraftAsync(_getCompanyId!(), _draft);
                string bookingCode = $"BKG-{bookingId:D4}";

                ShowSuccessScreen(bookingCode);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save rental booking: {ex.GetBaseException().Message}",
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                btnNext.Enabled = true;
                btnCancel.Enabled = true;
                btnNext.Text = "Confirm Booking";
            }
        }
    }

    private void ShowSuccessScreen(string bookingCode)
    {
        btnNext.Visible = false;
        btnCancel.Visible = false;
        wizardStepperControl.Visible = false;

        panelContentHost.SuspendLayout();
        panelContentHost.Controls.Clear();

        var successView = new BookingSuccessView(bookingCode, () => _onCloseWizard?.Invoke())
        {
            Dock = DockStyle.Fill
        };

        panelContentHost.Controls.Add(successView);
        panelContentHost.ResumeLayout();
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        if (_currentStepIndex > 0)
        {
            ShowStep(_currentStepIndex - 1);
        }
        else
        {
            if (_onCloseWizard != null)
            {
                _onCloseWizard.Invoke();
            }
            else if (Parent != null)
            {
                Parent.Controls.Remove(this);
                Dispose();
            }
        }
    }

    private class PlaceholderStepView : UserControl, IBookingWizardStep
    {
        public string StepTitle { get; }

        public PlaceholderStepView(string title)
        {
            StepTitle = title;
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(249, 241, 241);

            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(38, 22, 24),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(lbl);
        }

        public void OnStepEnter(BookingDraftModel draft) { }
        public void OnStepLeave(BookingDraftModel draft) { }
        public bool ValidateStep(out string errorMessage)
        {
            errorMessage = string.Empty;
            return true;
        }
    }
}