namespace CRM.winforms.models;

public interface IBookingWizardStep
{
    string StepTitle { get; }
    void OnStepEnter(BookingDraftModel draft);
    void OnStepLeave(BookingDraftModel draft);
    bool ValidateStep(out string errorMessage);
}
