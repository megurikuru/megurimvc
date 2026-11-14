using System.Collections.Generic;

namespace Meguri.ViewModels.Shared {
    public sealed class FileUploadViewModel {
        public required string InputId { get; init; }
        public required string InputName { get; init; }
        public required string LabelText { get; init; }
        public string? IconPath { get; init; }
        public string Accept { get; init; } = ".jpg,.jpeg,.png,image/jpeg,image/png";
        public int MaxImages { get; init; } = 1;
        public bool ShowCount { get; init; }
        public string? PreviewContainerId { get; init; }
        public string? CountDisplayId { get; init; }
        public string? PreviewVariant { get; init; }
        public string? ExistingImageUrl { get; init; }
        public string? RemoveCheckboxId { get; init; }
        public string? RemoveCheckboxName { get; init; }
        public bool RemoveCheckboxChecked { get; init; }
        public string? RemoveLabelText { get; init; }
        public IReadOnlyList<string> Hints { get; init; } = [];
        public bool UseDisclosure { get; init; }

        public string ResolvedPreviewContainerId => PreviewContainerId ?? $"{InputId}PreviewContainer";
        public string ResolvedCountDisplayId => CountDisplayId ?? $"{InputId}CountDisplay";
    }
}
