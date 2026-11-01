using System;
using System.Collections.Generic;

namespace Meguri.ViewModels.Manage {
    public class PasskeysViewModel {
        public IList<PasskeyItem> Passkeys { get; set; } = new List<PasskeyItem>();

        public bool HasPassword { get; set; }

        public bool Is2faEnabled { get; set; }

        public bool HasAuthenticator { get; set; }

        public bool CanRemovePasswordAnd2fa => Passkeys.Count > 0 && (HasPassword || Is2faEnabled || HasAuthenticator);

        public string StatusMessage { get; set; }
    }

    public class PasskeyItem {
        public string Id { get; set; }

        public string Name { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
