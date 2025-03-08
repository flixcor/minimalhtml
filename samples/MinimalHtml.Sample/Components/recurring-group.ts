function getPrefix(root: RecurringGroups) {
  const firstInput = root.querySelector('[name][id]') || root.querySelector('[name]')
  let namePrefix = firstInput?.getAttribute('name')
  let idPrefix = firstInput?.id || ""
  if (!firstInput || !namePrefix) return { idPrefix: "", namePrefix: "" }
  const distance = getDistance(firstInput, root)
  for (let index = 0; index <= distance; index++) {
    const nameSeperatorIndex = namePrefix.lastIndexOf('[')
    if (nameSeperatorIndex > -1) {
      namePrefix = namePrefix.substring(0, nameSeperatorIndex)
    }
    const idSeperatorIndex = idPrefix.lastIndexOf('__')
    if (idSeperatorIndex > -1) {
      idPrefix = idPrefix.substring(0, idSeperatorIndex)
      idPrefix = idPrefix.substring(0, idPrefix.lastIndexOf('_') + 1)
    }
  }
  namePrefix += '['
  return {
    namePrefix,
    idPrefix
  }
}

function getDistance(el: Element, root: RecurringGroups) {
  let distance = 0
  let parent
  while ((parent = el.parentElement?.closest('recurring-groups')) && parent !== root) {
    distance++
  }
  return distance
}

class AddGroupButton extends HTMLElement {
  #controller?: AbortController;
  #parent?: RecurringGroups;

  connectedCallback() {
    this.#controller = new AbortController();
    this.addEventListener("click", (e) => {
      e.preventDefault();
      this.#parent?.addGroup();
    }, {
      signal: this.#controller.signal,
    });
    this.#parent = undefined
    const parent = this.closest('recurring-groups')
    if (parent && 'initialize' in parent && typeof parent.initialize === 'function') {
      this.#parent = parent as RecurringGroups
      parent.initialize(this)
    }
  }

  disconnectedCallback() {
    this.#controller?.abort();
  }
}

class RemoveGroupButton extends HTMLElement {
  #controller?: AbortController;

  private removeGroup() {
    const group = this.closest("recurring-groups > *");
    const parent = group?.parentElement
    group && parent && group instanceof HTMLElement && parent instanceof RecurringGroups &&
      parent.removeGroup(group)
  }

  connectedCallback() {
    this.#controller = new AbortController();
    this.addEventListener("click", (e) => {
      e.preventDefault();
      this.removeGroup();
    }, {
      signal: this.#controller.signal,
    });
  }

  disconnectedCallback() {
    this.#controller?.abort();
  }
}

class RecurringGroups extends HTMLElement {
  groups: HTMLElement[] = []
  clone: Element | DocumentFragment | null = null
  namePrefix = ""
  idPrefix = ""
  addButton?: AddGroupButton

  initialize(addGroupButton: AddGroupButton) {
    const { namePrefix, idPrefix } = getPrefix(this)
    this.namePrefix = namePrefix
    this.idPrefix = idPrefix
    this.addButton = addGroupButton
    this.groups = []
    let group
    if (addGroupButton.previousElementSibling && addGroupButton.previousElementSibling instanceof HTMLTemplateElement) {
      this.clone = addGroupButton.previousElementSibling.content.firstElementChild
      group = addGroupButton.previousElementSibling.previousElementSibling
    } else {
      this.clone = addGroupButton.previousElementSibling
      group = addGroupButton.previousElementSibling
    }
    while (group && group instanceof HTMLElement) {
      this.groups.push(group)
      group = group.previousElementSibling
    }
    this.handleVisibility()
  }

  addGroup() {
    if (!this.clone || !this.addButton) return
    const newGroup = this.clone.cloneNode(true) as HTMLElement;
    this.handleNewIndex(newGroup, this.groups.length);
    this.addButton.insertAdjacentElement("beforebegin", newGroup);
    this.groups.push(newGroup);
    this.handleVisibility();
  }

  removeGroup(group: HTMLElement) {
    const index = this.groups.indexOf(group)
    if (index < 0) return;
    this.groups.forEach((g, i) => i > index && this.handleNewIndex(g, i - 1));
    this.groups.splice(index, 1);
    this.handleVisibility();
    group.remove();
  }

  handleNewIndex(item: Node, i: number) {
    const namePrefix = this.namePrefix
    const idPrefix = this.idPrefix
    if (!namePrefix || !(item instanceof Element)) return;
    const query = idPrefix
      ? `[name^='${namePrefix}'], [id^='${idPrefix}']`
      : `[name^='${namePrefix}']`
    const inputs = item.querySelectorAll(query);
    const labels = idPrefix ? item.querySelectorAll(`[for^='${idPrefix}'`) : [];
    inputs.forEach((input) => {
      if (input.id && idPrefix) {
        let after = input.id.substring(idPrefix.length)
        after = after.substring(after.indexOf('_'))
        input.id = `${idPrefix}${i}${after}`;
      }
      const inputName = input.getAttribute("name");
      if (inputName) {
        let after = inputName.substring(namePrefix.length)
        after = after.substring(after.indexOf(']'))
        input.setAttribute(
          "name",
          `${namePrefix}${i}${after}`
        );
      }
    });
    labels.forEach((label) => {
      label.setAttribute(
        "for",
        `${idPrefix}${i}${label
          .getAttribute("for")
          ?.substring(idPrefix?.length ?? 0 + 1)}`
      );
    });
  }

  handleVisibility(
  ) {
    const maxAttr = this.getAttribute("maxlength");
    const minAttr = this.getAttribute("minlength");
    this.addButton &&
      (this.addButton.hidden =
        !!maxAttr && +maxAttr <= this.groups.length);
    const removeButtonShouldBeHidden = !!minAttr && +minAttr >= this.groups.length;
    this.groups.forEach((g) => {
      const el = g.querySelector("remove-group-button:not(:scope recurring-groups remove-group-button)");
      el &&
        el instanceof HTMLElement &&
        (el.hidden = removeButtonShouldBeHidden);
    });
  }
}

customElements.define("recurring-groups", RecurringGroups);
customElements.define("remove-group-button", RemoveGroupButton);
customElements.define("add-group-button", AddGroupButton);

async function* hoi() {
  yield 5
  yield 7
  yield 9
}

async function* test() {
  for await (const element of new AsyncPipeline(hoi())
    .filter(x => x > 5)
    .map(Number.toString)) {
    console.log(element)
  }
}

class AsyncPipeline<T> implements AsyncGenerator<T> {
  private g: AsyncGenerator<T, any, any>

  constructor(g: AsyncGenerator<T>) {
    this.g = g
  }

  private async * _map<O>(f: (v: T) => O) {
    for await (const element of this.g) {
      yield f(element)
    }
  }

  private async * _filter(f: (v: T) => boolean) {
    for await (const element of this.g) {
      if (f(element)) {
        yield element
      }
    }
  }

  map<O>(f: (v: T) => O) {
    this.g = this._map(f) as any
    return this as unknown as AsyncPipeline<O>
  }

  filter(f: (v: T) => boolean) {
    this.g == this._filter(f)
    return this
  }

  next(...[value]: [] | [any]): Promise<IteratorResult<T, any>> {
    return this.g.next(value)
  }
  return(value: any): Promise<IteratorResult<T, any>> {
    return this.g.return(value)
  }
  throw(e: any): Promise<IteratorResult<T, any>> {
    return this.g.throw(e)
  }
  [Symbol
    .
    asyncIterator](): AsyncGenerator<T, any, any> {
    return this
  }

}