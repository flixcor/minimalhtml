namespace MinimalHtml.Lit;

internal static class JintShims
{
    internal const string BufferModule = """
        if (typeof TextEncoder === 'undefined') {
            globalThis.TextEncoder = function TextEncoder() { this.encoding = 'utf-8'; };
            globalThis.TextEncoder.prototype.encode = function(str) {
                var bytes = [];
                for (var i = 0; i < str.length; i++) {
                    var code = str.charCodeAt(i);
                    if (code < 0x80) {
                        bytes.push(code);
                    } else if (code < 0x800) {
                        bytes.push(0xC0 | (code >> 6));
                        bytes.push(0x80 | (code & 0x3F));
                    } else {
                        bytes.push(0xE0 | (code >> 12));
                        bytes.push(0x80 | ((code >> 6) & 0x3F));
                        bytes.push(0x80 | (code & 0x3F));
                    }
                }
                return new Uint8Array(bytes);
            };
        }

        function _b64(bytes) {
            const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
            let out = '';
            let i = 0;
            while (i < bytes.length) {
                const a = bytes[i++];
                const b = i < bytes.length ? bytes[i++] : -1;
                const c = i < bytes.length ? bytes[i++] : -1;
                out += chars[a >> 2];
                out += chars[((a & 3) << 4) | (b >= 0 ? b >> 4 : 0)];
                out += b >= 0 ? chars[((b & 15) << 2) | (c >= 0 ? c >> 6 : 0)] : '=';
                out += c >= 0 ? chars[c & 63] : '=';
            }
            return out;
        }

        class ShimBuffer extends Uint8Array {
            static from(input, encoding) {
                if (typeof input === 'string') {
                    if (encoding === 'binary' || encoding === 'latin1') {
                        const bytes = new Uint8Array(input.length);
                        for (let i = 0; i < input.length; i++) {
                            bytes[i] = input.charCodeAt(i) & 0xff;
                        }
                        return new ShimBuffer(bytes.buffer);
                    }
                    const encoded = new TextEncoder().encode(input);
                    return new ShimBuffer(encoded.buffer);
                }
                if (input instanceof Uint8Array) {
                    return new ShimBuffer(input.buffer, input.byteOffset, input.byteLength);
                }
                return new ShimBuffer(input);
            }

            static alloc(size) {
                return new ShimBuffer(size);
            }

            static isBuffer(obj) {
                return obj instanceof ShimBuffer;
            }

            toString(encoding) {
                if (encoding === 'base64') {
                    return _b64(this);
                }
                if (encoding === 'binary' || encoding === 'latin1') {
                    let s = '';
                    for (let i = 0; i < this.length; i++) {
                        s += String.fromCharCode(this[i]);
                    }
                    return s;
                }
                if (encoding === 'utf8' || encoding === 'utf-8' || !encoding) {
                    return new TextDecoder().decode(this);
                }
                return super.toString();
            }
        }

        export const Buffer = ShimBuffer;
        """;
}
