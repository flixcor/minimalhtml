/**  @param {SubmitEvent} e */
function submit(e) {
    if(!(e.target instanceof HTMLFormElement)) return
    const targetQuery = e.target.dataset.target
    if(!targetQuery) return
    const targetElement = document.querySelector(targetQuery)
    if(!targetElement) return
    e.preventDefault()
    const action = e.target.action
    const method = e.target.method?.toUpperCase() || "GET"
    const formData = new FormData(e.target, e.submitter)
    const body = method === "POST"
        ? e.target.enctype === 'multipart/form-data'
            ? formData
            : new URLSearchParams(formData)
        : undefined
    const url = method === 'GET'
        ? action + '?' + new URLSearchParams(formData)
        : action
    return fetch(url, {
        method,
        body
    }).then(async (r) => {
        if(!r.ok) return Promise.reject(r.status)
        const doc = document.implementation.createHTMLDocument()
        const reader = r.body
            .pipeThrough(new TextDecoderStream())
            .getReader()
        let first = true
        while(true) {
            const next = await reader.read()
            if(next.done) {
                return
            }
            doc.write(next.value)
            if(first && doc.body.firstChild) {
                first = false
                targetElement.replaceWith(doc.body.firstChild)
            }
        }
    })
}

class ActiveSearch extends HTMLElement {
    #formController = new AbortController()
    #slotController = new AbortController()
    connectedCallback() {
        const root = this.attachShadow({ mode: 'open' })
        const slot = document.createElement('slot')
        root.appendChild(slot)
        const slotchange = this.slotchange.apply(this)
        slot.addEventListener('slotchange', slotchange, {signal: this.#slotController.signal})
    }
    slotchange() {
        this.#formController.abort()
        this.#formController = new AbortController()
        /** @type {HTMLFormElement} */
        this.querySelectorAll('form').forEach(form => {
            const ms = +(form.dataset.debounce || 100)
            let timeout
            form.addEventListener('submit', submit, {signal: this.#formController.signal})
            form.addEventListener('input', (e) => {
                timeout && clearTimeout(timeout)
                if(!e.target.value) return
                timeout = setTimeout(() => form.requestSubmit(), ms)
            }, {signal: this.#formController.signal})
        })
    }
    disconnectedCallback(){
        this.#formController.abort()
        this.#slotController.abort()
    }
}

customElements.define('active-search', ActiveSearch)

