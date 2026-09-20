using CRM.infrastructure.data;
using CRM.winforms.Controls;
using CRM.winforms.models;
using CRM.winforms.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRM.winforms.Views;

public partial class NewBookingWizardView : UserControl
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly Func<int> _getCompanyId;
    private readonly Action _onCloseWizard;

    private readonly BookingDraftModel _draft = new();
    private readonly List<IBookingWizardStep> _steps = [];
    private int _currentStepIndex = 0;

    public NewBookingWizardView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId, Action onCloseWizard)
    {
        InitializeComponent();

        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));
        _onCloseWizard = onCloseWizard ?? throw new ArgumentNullException(nameof(onCloseWizard));

        InitializeSteps();
    }

    private void InitializeSteps()
    {
        _steps.Clear();
        _steps.Add(new Step1ClientSelectionView(_contextFactory, _getCompanyId));
        //_steps.Add(new UserControl { BackColor = Color.Transparent }); // Step 2
        //_steps.Add(new UserControl { BackColor = Color.Transparent }); // Step 3
        //_steps.Add(new UserControl { BackColor = Color.Transparent }); // Step 4
        //_steps.Add(new UserControl { BackColor = Color.Transparent }); // Step 5

        ShowStep(0);
    }

    private void ShowStep(int index)
    {
        _currentStepIndex = index;
        wizardStepperControl.CurrentStep = index + 1;

        panelContentHost.SuspendLayout();
        panelContentHost.Controls.Clear();

        var stepControl = (UserControl)_steps[_currentStepIndex];
        stepControl.Dock = DockStyle.Fill;
        panelContentHost.Controls.Add(stepControl);

        _steps[_currentStepIndex].OnStepEnter(_draft);
        panelContentHost.ResumeLayout();

        btnCancel.Enabled = _currentStepIndex > 0;
        btnNext.Text = (_currentStepIndex == _steps.Count - 1) ? "Confirm Booking" : "Next Step →";
    }

    private void BtnNext_Click(object? sender, EventArgs e)
    {
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
    }

    private void BtnBack_Click(object? sender, EventArgs e)
    {
        if (_currentStepIndex > 0)
        {
            ShowStep(_currentStepIndex - 1);
        }
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        _onCloseWizard();
    }

    private void NewBookingWizardView_Load(object sender, EventArgs e)
    {

    }

    private void panelContentHost_Paint(object sender, PaintEventArgs e)
    {

    }
}