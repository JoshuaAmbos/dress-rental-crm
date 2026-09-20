using CRM.infrastructure.data;
using CRM.winforms.models;

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

        // 1. Ensure buttons are on top of any panels so mouse clicks aren't blocked
        btnNext.BringToFront();
        btnCancel.BringToFront();

        // 2. Explicitly wire click handlers
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

        // Step 1
        _steps.Add(new Step1ClientSelectionView(_contextFactory, _getCompanyId));

        // Step 2
        try
        {
            _steps.Add(new Step2GarmentDatesView(_contextFactory, _getCompanyId));
        }
        catch
        {
            _steps.Add(new PlaceholderStepView("Step 2: Garment & Dates"));
        }

        // Step 3
        _steps.Add(new Step3MeasurementsNotesView()); 
        _steps.Add(new PlaceholderStepView("Step 4: Payment"));
        _steps.Add(new PlaceholderStepView("Step 5: Confirmation"));

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

        // Ensure buttons stay above the loaded view
        btnNext.BringToFront();
        btnCancel.BringToFront();

        // Button labels
        btnCancel.Text = (_currentStepIndex == 0) ? "← Cancel" : "← Back";
        btnNext.Text = (_currentStepIndex == _steps.Count - 1) ? "Confirm Booking" : "Continue →";
    }

    private void BtnNext_Click(object? sender, EventArgs e)
    {
        if (_steps.Count == 0) return;

        var currentStep = _steps[_currentStepIndex];

        // 1. Validate current step
        if (!currentStep.ValidateStep(out string error))
        {
            MessageBox.Show(error, "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // 2. Save step data to draft
        currentStep.OnStepLeave(_draft);

        // 3. Advance to the next step
        if (_currentStepIndex < _steps.Count - 1)
        {
            ShowStep(_currentStepIndex + 1);
        }
        else
        {
            MessageBox.Show("Booking ready for submission!", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        if (_currentStepIndex > 0)
        {
            // If on Step 2 or later, act as "Back"
            ShowStep(_currentStepIndex - 1);
        }
        else
        {
            // If on Step 1, exit wizard
            if (_onCloseWizard != null)
            {
                _onCloseWizard.Invoke();
            }
            else if (Parent != null)
            {
                // Fallback: Remove this view from parent container if callback was null
                Parent.Controls.Remove(this);
                Dispose();
            }
        }
    }

    // Temporary step placeholder to prevent wizard truncation
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