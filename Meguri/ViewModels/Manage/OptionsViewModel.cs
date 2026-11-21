using System.ComponentModel.DataAnnotations;

namespace Meguri.ViewModels.Manage {
    public class OptionsViewModel {
        [Display(Name = "Manage_Options_Language")]
        public string Culture { get; set; }

        [Display(Name = "Manage_Options_ColorMode")]
        public string ColorMode { get; set; }

        [Display(Name = "Manage_Options_ShowSexual")]
        public bool ShowSexual { get; set; }

        [Display(Name = "Manage_Options_ShowViolence")]
        public bool ShowViolence { get; set; }

        public bool IsAdult { get; set; }

        public string StatusMessage { get; set; }
    }
}
