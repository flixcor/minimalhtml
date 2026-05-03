import { LitElement, css, html } from "lit";
import { customElement, property } from "lit/decorators.js";

@customElement("static-tag")
export class StaticTag extends LitElement {
  @property() label = "static";
  @property({ type: Number }) count = 0;

  render() {
    return html`
      <span class="badge">${this.label}: ${this.count}</span>
      <slot></slot>
    `;
  }

  static styles = css`
    :host {
      display: inline-flex;
      gap: 0.5em;
      align-items: baseline;
      font-family: system-ui, sans-serif;
    }
    .badge {
      padding: 0.2em 0.6em;
      border-radius: 999px;
      background: #1a1a1a;
      color: #fff;
      font-size: 0.85em;
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "static-tag": StaticTag;
  }
}
