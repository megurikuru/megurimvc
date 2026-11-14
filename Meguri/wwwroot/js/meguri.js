// SNS Meguri 共通 JavaScript (ES Modules)

import { localizeTimes, initCommonUi } from './meguri/common-ui.js';
import { initTagManager } from './meguri/tag-manager.js';
import { initImageAttachments } from './meguri/image-attachment-manager.js';
import { initReactions } from './meguri/reactions.js';
import { initCommentReply } from './meguri/comment-reply.js';
import { initImageOrder } from './meguri/image-order.js';
import { initMessageUi } from './meguri/message-ui.js';
import { initImageViewer } from './meguri/image-viewer.js';

function onDOMContentLoaded() {
    localizeTimes();
    initCommonUi();
    initTagManager();
    initImageAttachments();
    initImageOrder();
    initReactions();
    initCommentReply();
    initMessageUi();
    initImageViewer();
}

// DOM読み込み完了時に初期化処理を実行
document.addEventListener('DOMContentLoaded', onDOMContentLoaded);