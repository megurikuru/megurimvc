using System;
using System.Collections.Generic;

namespace Meguri.ViewModels.Manage {
    public class PasskeysViewModel {
        public IList<PasskeyItem> Passkeys { get; set; } = new List<PasskeyItem>();

        public string StatusMessage { get; set; }
    }

    public class PasskeyItem {
        public string Id { get; set; }

        public string Name { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
