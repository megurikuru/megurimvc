using System.ComponentModel.DataAnnotations;

namespace Meguri.Models.ManageViewModels {
    public class OptionsViewModel {
        [Display(Name = "Manage_Options_Language")]
        public string Culture { get; set; }

        [Display(Name = "Manage_Options_ColorMode")]
        public string ColorMode { get; set; }

        public string StatusMessage { get; set; }
    }
}
