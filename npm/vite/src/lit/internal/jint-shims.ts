declare global {
  // eslint-disable-next-line no-var
  var window: typeof globalThis;
  // eslint-disable-next-line no-var
  var customElements: {
    _registry: Map<string, CustomElementCtor>;
    define(name: string, ctor: CustomElementCtor): void;
    get(name: string): CustomElementCtor | undefined;
  };
}

interface CustomElementCtor {
  observedAttributes?: unknown;
  new (): unknown;
}

const g = globalThis as unknown as {
  window?: unknown;
  customElements?: unknown;
  btoa?: (s: string) => string;
};

g.window ??= globalThis;

interface CustomElementsRegistry {
  _registry: Map<string, CustomElementCtor>;
  define(name: string, ctor: CustomElementCtor): void;
  get(name: string): CustomElementCtor | undefined;
}

const registry: CustomElementsRegistry = {
  _registry: new Map<string, CustomElementCtor>(),
  define(name, ctor) {
    void ctor.observedAttributes;
    this._registry.set(name, ctor);
  },
  get(name) {
    return this._registry.get(name);
  },
};

g.customElements ??= registry;

if (typeof g.btoa !== "function") {
  g.btoa = function (s: string): string {
    const chars =
      "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    let out = "";
    let i = 0;
    while (i < s.length) {
      const a = s.charCodeAt(i++) & 0xff;
      const b = i < s.length ? s.charCodeAt(i++) & 0xff : -1;
      const c = i < s.length ? s.charCodeAt(i++) & 0xff : -1;
      out += chars[a >> 2];
      out += chars[((a & 3) << 4) | (b >= 0 ? b >> 4 : 0)];
      out += b >= 0 ? chars[((b & 15) << 2) | (c >= 0 ? c >> 6 : 0)] : "=";
      out += c >= 0 ? chars[c & 63] : "=";
    }
    return out;
  };
}

export {};
