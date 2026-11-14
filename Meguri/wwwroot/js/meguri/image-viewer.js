/**
 * 投稿・メッセージ添付画像のオリジナルサイズ（最大画面サイズ）ビューアー
 */
class ImageViewer {
    static overlayEl = null;
    static imgEl = null;
    static prevBtn = null;
    static nextBtn = null;
    static closeBtn = null;
    static counterEl = null;
    static images = [];
    static currentIndex = 0;

    static ensureDom() {
        if (this.overlayEl) return;

        const overlay = document.createElement('div');
        overlay.className = 'image-viewer-overlay';
        overlay.hidden = true;
        overlay.setAttribute('role', 'dialog');
        overlay.setAttribute('aria-modal', 'true');

        const prevBtn = document.createElement('button');
        prevBtn.type = 'button';
        prevBtn.className = 'image-viewer-btn image-viewer-prev';
        prevBtn.setAttribute('aria-label', 'Previous image');
        prevBtn.textContent = '‹';

        const img = document.createElement('img');
        img.className = 'image-viewer-img';
        img.alt = '';

        const nextBtn = document.createElement('button');
        nextBtn.type = 'button';
        nextBtn.className = 'image-viewer-btn image-viewer-next';
        nextBtn.setAttribute('aria-label', 'Next image');
        nextBtn.textContent = '›';

        const closeBtn = document.createElement('button');
        closeBtn.type = 'button';
        closeBtn.className = 'image-viewer-btn image-viewer-close';
        closeBtn.setAttribute('aria-label', 'Close');
        closeBtn.textContent = '×';

        const counter = document.createElement('div');
        counter.className = 'image-viewer-counter';
        counter.hidden = true;

        overlay.appendChild(prevBtn);
        overlay.appendChild(img);
        overlay.appendChild(nextBtn);
        overlay.appendChild(closeBtn);
        overlay.appendChild(counter);
        document.body.appendChild(overlay);

        prevBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            this.showPrev();
        });

        nextBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            this.showNext();
        });

        closeBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            this.close();
        });

        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) {
                this.close();
            }
        });

        this.overlayEl = overlay;
        this.imgEl = img;
        this.prevBtn = prevBtn;
        this.nextBtn = nextBtn;
        this.closeBtn = closeBtn;
        this.counterEl = counter;
    }

    static open(images, startIndex = 0) {
        if (!Array.isArray(images) || images.length === 0) return;
        this.ensureDom();

        this.images = images;
        this.currentIndex = Math.max(0, Math.min(startIndex, images.length - 1));
        this.updateView();

        this.overlayEl.hidden = false;
    }

    static close() {
        if (!this.overlayEl || this.overlayEl.hidden) return;
        this.overlayEl.hidden = true;
        if (this.imgEl) {
            this.imgEl.removeAttribute('src');
        }
        this.images = [];
        this.currentIndex = 0;
    }

    static isOpen() {
        return Boolean(this.overlayEl && !this.overlayEl.hidden);
    }

    static showPrev() {
        if (this.currentIndex <= 0) return;
        this.currentIndex -= 1;
        this.updateView();
    }

    static showNext() {
        if (this.currentIndex >= this.images.length - 1) return;
        this.currentIndex += 1;
        this.updateView();
    }

    static updateView() {
        const current = this.images[this.currentIndex];
        if (!current || !this.imgEl) return;

        this.imgEl.src = current.src;
        this.imgEl.alt = current.alt || '';

        const hasMultiple = this.images.length > 1;
        this.prevBtn.hidden = !hasMultiple;
        this.nextBtn.hidden = !hasMultiple;
        this.counterEl.hidden = !hasMultiple;

        if (hasMultiple) {
            this.prevBtn.disabled = this.currentIndex <= 0;
            this.nextBtn.disabled = this.currentIndex >= this.images.length - 1;
            this.counterEl.textContent = `${this.currentIndex + 1} / ${this.images.length}`;
        }
    }
}

function handleDocumentClick(e) {
    if (e.defaultPrevented || e.button !== 0 || e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) {
        return;
    }

    const target = e.target;
    if (!target || typeof target.closest !== 'function') return;

    const finderImg = target.closest('.finder-thumbs img');
    if (finderImg) {
        const finderContainer = finderImg.closest('.finder-thumbs');
        if (!finderContainer) return;

        const imgs = Array.from(finderContainer.querySelectorAll('img'));
        if (imgs.length === 0) return;

        e.preventDefault();

        const images = imgs.map((imgEl) => ({
            src: imgEl.getAttribute('src') || imgEl.src,
            alt: imgEl.alt || ''
        }));

        const startIndex = Math.max(0, imgs.indexOf(finderImg));
        ImageViewer.open(images, startIndex);
        return;
    }

    const link = target.closest('.post-images a[href*="/Image/File/"], .message-images a[href*="/Image/File/"]');
    if (!link) return;

    const container = link.closest('.post-images, .message-images');
    if (!container) return;

    const links = Array.from(container.querySelectorAll('a[href*="/Image/File/"]'));
    if (links.length === 0) return;

    e.preventDefault();

    const images = links.map((anchor) => {
        const thumbImg = anchor.querySelector('img');
        return {
            src: anchor.getAttribute('href') || (thumbImg ? thumbImg.src : ''),
            alt: thumbImg ? thumbImg.alt : ''
        };
    });

    const startIndex = Math.max(0, links.indexOf(link));
    ImageViewer.open(images, startIndex);
}

function handleDocumentKeydown(e) {
    if (!ImageViewer.isOpen()) return;

    if (e.key === 'Escape') {
        e.preventDefault();
        ImageViewer.close();
    } else if (e.key === 'ArrowLeft') {
        e.preventDefault();
        ImageViewer.showPrev();
    } else if (e.key === 'ArrowRight') {
        e.preventDefault();
        ImageViewer.showNext();
    }
}

export function initImageViewer() {
    document.addEventListener('click', handleDocumentClick);
    document.addEventListener('keydown', handleDocumentKeydown);
}
