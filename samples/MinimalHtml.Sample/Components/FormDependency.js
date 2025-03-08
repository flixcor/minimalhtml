function getComparer(key) {
  switch (key) {
    case 'Equal':
      return (a, b) => a === b
    case 'GreaterThan':
      return (a, b) => a > b
    case 'LessThan':
      return (a, b) => a < b
    case 'NotEqual':
      return (a, b) => a !== b
    case 'GreaterThanOrEqual':
      return (a, b) => a >= b
    case 'LessThanOrEqual':
      return (a, b) => a <= b
    case 'Contains':
      return (a, b) =>
        (Array.isArray(a) || typeof a === 'string') && a.includes(b)
    default:
      return false
  }
}
class FormDependency extends HTMLElement {
  #targetElement
  #value = ''
  #currentValue
  #name = ''
  #operator
  #form
  #numeric = false
  #whenTrue
  #whenFalse
  #elementsSetup = false
  static get observedAttributes() {
    return ['data-is-client']
  }
  handleChange() {
    const hidden = !getComparer(this.#operator)(
      this.#currentValue,
      this.#value
    )
    if (this.#whenTrue instanceof HTMLFieldSetElement && this.#whenTrue.disabled !== hidden) {
      this.#whenTrue.disabled = hidden
    }
    if (this.#whenFalse instanceof HTMLFieldSetElement && this.#whenFalse.disabled === hidden) {
      this.#whenFalse.disabled = !hidden
    }
  }
  parseValue(value) {
    if (!this.#numeric) return value
    const parsed = Number.parseFloat(value)
    if (isNaN(parsed)) return value
    return parsed
  }
  attributeChangedCallback(name, oldValue, newValue) {
    if (name === 'data-is-client' && !newValue) {
      this.setAttribute('data-is-client', 'true')
      setTimeout(() => {
        this.handleChange()
      })
    }
  }
  connectedCallback() {
    this.setAttribute('data-is-client', 'true')
    this.#name = this.getAttribute('name')
    this.#operator = this.getAttribute('operator')
    this.#form = this.closest('form')
    this.#numeric =
      this.hasAttribute('numeric') && this.getAttribute('numeric') !== 'false'
    this.#value = this.parseValue(this.getAttribute('value'))
    this.#targetElement = this.#form?.elements?.[this.#name]
    this.#targetElement.addEventListener('input', this)
    this.#currentValue = this.getValue(this.#targetElement)
    this.setupElements()
    // give the browser a change to parse children
    setTimeout(() => this.setupElements())
  }
  setupElements() {
    if (this.#elementsSetup) return
    const fieldsets = [...this.getElementsByTagName('fieldset')]
    if (!fieldsets.length) return
    if (fieldsets.length === 1) {
      this.#whenTrue = fieldsets[0]
    } else {
      this.#whenFalse = fieldsets.find(
        (x) => x.getAttribute('data-dependency') === 'false'
      )
      this.#whenTrue = fieldsets.find(
        (x) => x.getAttribute('data-dependency') !== 'false'
      )
    }
    this.handleChange()
    this.#elementsSetup = true
  }
  getValue(element) {
    return element instanceof HTMLSelectElement
      ? [...element.selectedOptions].map(x => this.parseValue(x.value))
      : element instanceof HTMLInputElement && element.type === 'checkbox'
        ? [element.form.querySelectorAll(`[name='${this.#name}']:checked`)].map(x => this.parseValue(x.value))
        : this.parseValue(element.value)
  }
  /** @param {Event} e */
  handleEvent(e) {
    if (e.type === 'input' && e.target?.name === this.#name) {
      const newValue = this.getValue(e.target)
      if (this.#currentValue !== newValue) {
        this.#currentValue = newValue
        this.handleChange()
      }
    }
  }
  disconnectedCallback() {
    this.#targetElement?.removeEventListener('input', this)
  }
}

customElements.define('form-dependency', FormDependency)
