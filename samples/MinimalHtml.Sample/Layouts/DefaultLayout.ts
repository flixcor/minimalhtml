navigator?.serviceWorker?.addEventListener("message", e => {
    if (e.data?.url !== location.href) return
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