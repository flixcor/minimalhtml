
"use strict";
import hasher from "xxhash-wasm"

self.addEventListener("install", () => self.skipWaiting())

const getHasher = () => hasher()
/**
 * 
 * @param {Response} response 
 * @returns 
 */
const getHash = (response) => (hasherPromise ??= getHasher()).then(async hasher => {
    if (!response.body || !hasher) return ""
    if (response.url.includes("localhost")) return response.text().then(t => t.replaceAll(/.*browserLink.*/g, "")).then(hasher.h32ToString)
    const x32 = hasher.create32()
    const reader = response.body.getReader()
    let done = false
    while (!done) {
        const result = await reader.read()
        done = result.done
        if (result.value) {
            x32.update(result.value)
        }
    }
    return x32.digest().toString(16).padStart(8, "0")
})

/**
 * @type {ReturnType<typeof getHasher> | undefined}
 */
let hasherPromise
const ourCachePromise = caches.open("minimalhtml-sample")

self.addEventListener("fetch", (e) => {
    if (e instanceof FetchEvent
        && e.request.method.toLowerCase() === "get"

    ) {
        const url = new URL(e.request.url)
        if (url.origin !== self.location.origin || url.pathname.includes('.') || url.pathname.includes('browserLink')) return;
        e.respondWith(handle(e.request, e.resultingClientId).then(([response, promise]) => {
            promise && e.waitUntil(promise)
            return response.clone()
        }))
    }
})

/**
 * 
 * @param {Request} request 
 * @param {string} clientId 
 * @returns {Promise<[Response, Promise<void> | undefined]>}
 */
async function handle(request, clientId) {
    const cached = await ourCachePromise.then(c => c.match(request))
    const fresh = fetch(request)
    const [response, isFresh] = await prioritize(cached, fresh)
    const cachePromise = handleCaching(request, clientId, cached, fresh, isFresh)
    return [response, cachePromise]
}

/**
 * 
 * @param {Response | undefined} cached 
 * @param {Promise<Response>} fresh 
 * @returns {Promise<readonly [Response, boolean]>}
 */
async function prioritize(cached, fresh) {
    const SENSIBLE_TIMOUT = 750
    if (!cached) return [await fresh, true]
    if (cached.headers.get("x-swr")) return [cached, false]
    /** @type {Promise<void>} */
    const timeoutPromise = new Promise((_, reject) => setTimeout(() => reject(), SENSIBLE_TIMOUT))
    return await Promise.race([fresh, timeoutPromise])
        .then(quick => [quick, true])
        .catch(() => [cached, false])
}

/**
 * @param {Request} request 
 * @param {string} clientId 
 * @param {Response | undefined} cached 
 * @param {Promise<Response>} fresh 
 * @param {boolean} usedFresh 
 * @returns {Promise<undefined>}
 */
async function handleCaching(request, clientId, cached, fresh, usedFresh) {
    const response = await fresh
    const clone = response.clone()
    const contentType = response.headers.get('Content-Type');
    const isHtml = contentType?.includes("html");
    if (!isHtml) return
    const result = await compare(cached, response)
    const ourCache = await ourCachePromise
    switch (result) {
        case comparisonResult.deleted:
            await ourCache.delete(request)
            break;
        case comparisonResult.changed:
        case comparisonResult.created:
            await ourCache.put(request, clone)
            break;
        default:
            break;
    }
    if (!usedFresh &&
        result === comparisonResult.changed &&
        'clients' in self &&
        self.clients instanceof Clients) {
        const client = await self.clients.get(clientId)
        client?.postMessage({
            url: request.url
        })
    }
}

/**
 * @param {Response} response 
 * @returns {string | null}
 */
const getEtag = (response) => response.headers.get('ETag') || response.headers.get('x-swr-etag') || response.headers.get('x-swr-hash')

/**
 * @param {Response} response 
 * @returns {Promise<string>}
 */
const getSwr = async (response) => {
    const etag = getEtag(response)
    if (etag) return etag
    const clone = response.clone()
    const hash = await getHash(clone)
    // response.headers.set('x-swr-hash', hash)
    return hash
}

/**
 * @param {Response | undefined} cached 
 * @param {Response} fresh 
 * @returns 
 */
async function compare(cached, fresh) {
    const [oldHash, newHash] = await Promise.all([cached && getSwr(cached), getSwr(fresh)])
    if (newHash && !oldHash) return comparisonResult.created
    if (oldHash && !newHash) return comparisonResult.deleted
    if (newHash && oldHash && newHash !== oldHash) return comparisonResult.changed
    return comparisonResult.unchanged
}

const comparisonResult = Object.freeze({
    changed: 'changed',
    created: 'created',
    deleted: 'deleted',
    unchanged: 'unchanged'
})
