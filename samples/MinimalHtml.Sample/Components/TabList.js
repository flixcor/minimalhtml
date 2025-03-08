class TabList extends HTMLElement {
  /** @type {HTMLElement} */
  #slot
  /** @type {() => void} */
  #destroy
  /** @type {HTMLAnchorElement[]} */
  #anchors = []
  /** @type {HTMLElement[]} */
  #panels = []
  /** @type {number} */
  #current
  connectedCallback() {
    if(this.shadowRoot) {
      this.loopElements()
      return
    }
    const root = this.attachShadow({ mode: 'open' })
    this.#slot = document.createElement('slot')
    root.appendChild(this.#slot)
    const slotchange = this.slotchange.apply(this)
    this.#slot.addEventListener('slotchange', slotchange)
    this.addEventListener('click', this.click)
    this.addEventListener('keydown', this.keydown)

    this.#destroy = () => {
      this.#slot.removeEventListener('slotchange', slotchange)
      this.removeEventListener('click', this.click)
      this.removeEventListener('keydown', this.keydown)
    }
  }
  disconnectedCallback() {
    this.#destroy?.()
  }
  slotchange() {
    const nav = this.querySelector(':scope > nav')
    if(!(nav instanceof HTMLElement)) {
      return
    }        
    nav.role = 'tablist'
    this.#anchors = [...nav.getElementsByTagName('a')]
    this.#panels = [...this.querySelectorAll(':scope > section')]
    this.#current = 0
    this.loopElements()  
  }
  click(e) {
    const i = this.#anchors.indexOf(e.target.closest('a'))
    if (this.isValidIndex(i)) {
      // e.preventDefault()
      this.activate(i)
    }
  }
  /** @param {Event} e */
  keydown(e) {
    if (!(e.target instanceof HTMLAnchorElement && this.#anchors.includes(e.target))) {
      return
    }
    if (e.key === 'ArrowRight') {
      e.preventDefault()
      this.activate(this.#current + 1)
      return
    }
    if (e.key === 'ArrowLeft') {
      e.preventDefault()
      this.activate(this.#current - 1)
      return
    }
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      this.#panels[this.#current]?.focus()
    }
  }
  activate(i = 0) {
    if (i < 0) {
      i = this.#anchors.length - 1
    } else if (i > this.#anchors.length - 1) {
      i = 0
    }
    if (this.#current === i) return

    const oldAnchor = this.#anchors[this.#current]
    const oldPanel = this.#panels[this.#current]
    const newAnchor = this.#anchors[i]
    const newPanel = this.#panels[i]

    oldAnchor.removeAttribute('aria-selected')
    oldAnchor.tabIndex = -1
    oldPanel.hidden = true

    newAnchor.ariaSelected = 'true'
    newAnchor.removeAttribute('tabindex')
    newPanel.hidden = false
    document.location.hash = newPanel.id
    newAnchor.focus()

    this.#current = i
  }
  loopElements() {
    document.startViewTransition(() =>  new Promise(resolve => {
      for (var i = 0; i < this.#anchors.length; i++) {
        const a = this.#anchors[i]
        const panel = this.#panels[i]
        panel.role = 'tabpanel'
        panel.tabIndex = -1
        a.role = 'tab'
        if (i === 0) {
          a.ariaSelected = 'true'
          a.removeAttribute('tabindex')
          panel.hidden = false
        } else {
          a.removeAttribute('aria-selected')
          a.tabIndex = -1
          panel.hidden = true
        }
      }
      resolve()
    }))
  }
  /** 
   * @param {unknown} i 
   * @returns {i is number} */
  isValidIndex(i) {
    return typeof i === 'number' && !isNaN(i) && i !== -1
  }
}

customElements.define('tab-list', TabList)
