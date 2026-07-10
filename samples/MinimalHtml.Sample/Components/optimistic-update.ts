/*
<optimistic-update>
  <form>
    <input type="text" name="name" placeholder="Enter your name" />
    <button type="submit">Submit</button>
  </form>
  <template>
    <p>Hello, <span data-bind="name"></span>!</p>
  </template>
</optimistic-update>
*/

/**
 * @attr {'replace-children' | 'replace' | 'append' | 'prepend' | 'before' | 'after'} mode The mode of how the template should be added to the DOM. Default is 'replace'.
 * @attr {string} target The id of the target element where the template should be added. Default is the optimistic-update element itself.
 * @attr {string} template The id of the template to use. Default is the first template child of the optimistic-update element.
 */
class OptimisticUpdate extends HTMLElement {


    #abortController: AbortController | undefined;
    connectedCallback() {
        this.#abortController = new AbortController();
        this.addEventListener('submit', e => this.#handleSubmit(e), { signal: this.#abortController.signal });
    }
    disconnectedCallback() {
        this.#abortController?.abort();
    }
    #handleSubmit = (event: Event) => {
        if (!(event.target instanceof HTMLFormElement)) return;
        const formData = new FormData(event.target);
        const clone = this.#getClonedTemplate();
        if (!clone) return;
        this.#fillTemplate(clone, formData);
        this.#addTemplateToDOM(clone);
    }
    #getClonedTemplate() {
        const templateId = this.getAttribute('template');
        const template = (templateId && document.getElementById(templateId)) || this.querySelector('template');
        if (template instanceof HTMLTemplateElement) return template.content.cloneNode(true) as DocumentFragment;
    }
    #fillTemplate(template: DocumentFragment, formData: FormData) {
        const bound = template.querySelectorAll('[data-bind]');
        for (const element of bound) {
            const name = element.getAttribute('data-bind');
            if (!name) continue;
            const value = formData.get(name);
            if (typeof value === 'string') {
                element.textContent = value;
            }
        }
    }
    #addTemplateToDOM(clone: DocumentFragment) {
        const mode = this.getAttribute('mode') || 'replace';
        const targetId = this.getAttribute('target');
        const target = (targetId && document.getElementById(targetId)) || this;
        switch (mode) {
            case 'replace-children':
                target.replaceChildren(clone);
                break;
            case 'replace':
                target.replaceWith(clone);
                break;
            case 'append':
                target.append(clone);
                break;
            case 'prepend':
                target.prepend(clone);
                break;
            case 'before':
                target.before(clone);
                break;
            case 'after':
                target.after(clone);
                break;
            default:
                console.warn(`Unknown mode: ${mode}`);
        }
    }
}
customElements.define('optimistic-update', OptimisticUpdate);