namespace FloristHub.Services.DTOs.Onboarding;

public record OnboardingChecklistItemDto(
    string Key,
    string Title,
    string Description,
    bool IsRequired,
    bool IsComplete,
    string? AdminPath,
    string? WizardStep);

public record FloristOnboardingStatusDto(
    bool IsPublished,
    bool CanPublish,
    IReadOnlyList<string> MissingRequired,
    IReadOnlyList<OnboardingChecklistItemDto> Checklist,
    string CurrentWizardStep,
    int WizardProgressPercent);

public record SetWizardStepRequest(string Step);

public record SetDailyCapacityRequest(int DailyOrderLimit);

public record SetPaymentsDisabledByChoiceRequest(bool Disabled);

public record PlatformTenantOnboardingDto(
    Guid TenantId,
    string Name,
    string Slug,
    bool IsPublished,
    bool FloristInviteCompleted,
    string? CurrentWizardStep,
    IReadOnlyList<OnboardingChecklistItemDto> Checklist,
    int CompletedRequiredCount,
    int RequiredCount);
