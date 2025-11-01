navigator?.serviceWorker?.addEventListener("message", e => {
    if (e.data?.url !== location.href) return
    console.log("Service worker reported a change:", e.data)
    const toast = document.createElement("div")
    toast.innerText = "This page has changed. Refresh to see the new content."
    toast.setAttribute("popover", "manual")
    document.getElementsByTagName("main")[0]?.append(toast)
    toast.showPopover()
    setTimeout(() => {
        toast.hidePopover()
        toast.remove()
    }, 10000);
})

customElements.define("version-number", class extends HTMLElement {
    constructor() {
        super();
        fetch("/api/version-number")
            .then(r => r.ok ? r.json() : null)
            .then(r => this.innerText = r?.version ?? "")
    }
})