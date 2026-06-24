#!/usr/bin/env node

import { readFileSync, existsSync, readdirSync } from "node:fs";
import { homedir } from "node:os";
import path from "node:path";
import { pathToFileURL } from "node:url";

const REGISTRY_DIR = path.join(homedir(), ".pie-unity", "instances");
const DEFAULT_WAIT_MS = 600;
const DEFAULT_RETRIES = 30;
const ACTIVE_INSTANCE_MAX_AGE_SEC = 120;
const EXPECTED_SKILL_PROTOCOL_VERSION = "pie-unity-rpc/2";

class InstanceSelectionError extends Error {
	constructor(code, message, details = {}) {
		super(message);
		this.name = "InstanceSelectionError";
		this.code = code;
		this.details = details;
	}
}

export function parseArgs(argv) {
	const args = argv.slice(2);
	const command = args[0] || "instances";
	const flags = {};
	for (let i = 1; i < args.length; i += 1) {
		const item = args[i];
		if (!item.startsWith("--")) continue;
		const key = item.slice(2);
		const next = args[i + 1];
		if (!next || next.startsWith("--")) {
			flags[key] = "true";
			continue;
		}
		flags[key] = next;
		i += 1;
	}
	return { command, flags };
}

export function loadRegistry(directoryPath = REGISTRY_DIR) {
	if (!existsSync(directoryPath)) {
		return [];
	}

	const items = [];
	try {
		const fileNames = readdirSync(directoryPath, { withFileTypes: true })
			.filter((entry) => entry.isFile() && entry.name.endsWith(".json"))
			.map((entry) => path.join(directoryPath, entry.name));
		for (const filePath of fileNames) {
			try {
				const raw = readFileSync(filePath, "utf8").replace(/^\uFEFF/, "");
				if (!raw.trim()) continue;
				const item = JSON.parse(raw);
				if (!item || typeof item !== "object") continue;
				items.push(item);
			} catch {
				// Ignore malformed instance files so one broken host never poisons discovery.
			}
		}
	} catch {
		return [];
	}
	return dedupeInstances(items.filter(Boolean));
}

export function getActiveInstances(instances, nowUnix = Math.floor(Date.now() / 1000)) {
	const recent = instances.filter((item) => {
		const lastSeenUnix = Number(item?.lastSeenUnix || 0);
		return lastSeenUnix > 0 && nowUnix - lastSeenUnix <= ACTIVE_INSTANCE_MAX_AGE_SEC;
	});
	return recent.length > 0 ? recent : instances;
}

function basenameMatch(left, right) {
	return path.basename(left || "") !== ""
		&& path.basename(left || "") === path.basename(right || "");
}

function computeProjectMatchScore(requestedProject, candidateProject) {
	const requested = normalizePath(requestedProject || "");
	const candidate = normalizePath(candidateProject || "");
	if (!requested || !candidate) return 0;
	if (requested === candidate) return 1000;
	if (basenameMatch(requested, candidate)) return 700;

	const requestedWithSlash = requested.endsWith("/") ? requested : `${requested}/`;
	const candidateWithSlash = candidate.endsWith("/") ? candidate : `${candidate}/`;
	if (requestedWithSlash.startsWith(candidateWithSlash) || candidateWithSlash.startsWith(requestedWithSlash)) {
		const distance = Math.abs(requested.length - candidate.length);
		return 500 - Math.min(distance, 400);
	}

	return 0;
}

function formatCandidate(item) {
	const health = item?.__health || {};
	return {
		instanceId: String(item?.instanceId || ""),
		projectPath: normalizePath(item?.projectPath || ""),
		projectName: String(item?.projectName || ""),
		displayName: String(item?.displayName || item?.projectName || item?.productName || ""),
		productName: String(item?.productName || ""),
		applicationIdentifier: String(item?.applicationIdentifier || ""),
		mode: String(item?.mode || ""),
		port: Number(item?.port || 0),
		pid: Number(item?.pid || 0),
		lastSeenUnix: Number(item?.lastSeenUnix || 0),
		version: String(item?.version || item?.packageVersion || ""),
		ready: health.ready === true,
		mainThreadResponsive: health.mainThreadResponsive === true,
		bridgeReady: health.bridgeReady === true,
		scriptHostReady: health.scriptHostReady === true,
		runtimeActive: health.runtimeActive === true,
		editorActive: health.editorActive === true,
		editorSuppressedByRuntime: health.editorSuppressedByRuntime === true,
		playModeActive: health.playModeActive === true,
		isPlayingOrWillChangePlaymode: health.isPlayingOrWillChangePlaymode === true,
		behaviorTreeActiveCount: Number(health.behaviorTreeActiveCount || 0),
	};
}

function buildSelectionError(code, message, flags, candidates, extra = {}) {
	return new InstanceSelectionError(code, message, {
		reason: code === "PIE_UNITY_INSTANCE_AMBIGUOUS" ? "ambiguous" : "no_match",
		requestedProject: normalizePath(flags.project || ""),
		requestedInstance: String(flags.instance || "").trim(),
		requestedPort: Number(flags.port || 0) || 0,
		candidates: candidates.map(formatCandidate),
		...extra,
	});
}

export function selectInstance(instances, flags) {
	const activeInstances = getActiveInstances(instances);
	const project = normalizePath(flags.project || "");
	const instanceId = String(flags.instance || "").trim();
	const explicitPort = Number(flags.port || 0);

	if (explicitPort > 0) {
		const portMatches = activeInstances
			.filter((item) => Number(item?.port || 0) === explicitPort)
			.filter((item) => !instanceId || String(item?.instanceId || "") === instanceId)
			.filter((item) => !project || computeProjectMatchScore(project, item?.projectPath || "") > 0)
			.sort((left, right) => Number(right?.lastSeenUnix || 0) - Number(left?.lastSeenUnix || 0));
		if (portMatches[0]) {
			return portMatches[0];
		}
		return {
			instanceId: instanceId || `port_${explicitPort}`,
			projectPath: project,
			port: explicitPort,
			mode: "unknown",
		};
	}

	if (instanceId) {
		const match = activeInstances.find((item) => String(item.instanceId || "") === instanceId);
		if (match) return match;
		throw buildSelectionError(
			"PIE_UNITY_INSTANCE_NOT_FOUND",
			`No pie-unity instance found for --instance ${instanceId}.`,
			flags,
			activeInstances,
		);
	}

	if (project && activeInstances.length > 0) {
		const scored = activeInstances
			.map((item) => ({ item, score: computeProjectMatchScore(project, item.projectPath || "") }))
			.filter((entry) => entry.score > 0)
			.sort((left, right) => right.score - left.score || Number(right.item.lastSeenUnix || 0) - Number(left.item.lastSeenUnix || 0));

		if (scored.length === 1) {
			return scored[0].item;
		}

		if (scored.length > 1) {
			const bestScore = scored[0].score;
			const best = scored.filter((entry) => entry.score === bestScore).map((entry) => entry.item);
			if (best.length === 1) {
				return best[0];
			}
			throw buildSelectionError(
				"PIE_UNITY_INSTANCE_AMBIGUOUS",
				`Multiple pie-unity instances match project hint ${project}. Pass --instance or --port.`,
				flags,
				best,
			);
		}
	}

	if (activeInstances.length === 1) {
		return activeInstances[0];
	}

	if (activeInstances.length > 1) {
		throw buildSelectionError(
			"PIE_UNITY_INSTANCE_AMBIGUOUS",
			"Multiple pie-unity instances found. Pass --instance, --port, or a more specific --project.",
			flags,
			activeInstances,
		);
	}

	throw buildSelectionError(
		"PIE_UNITY_INSTANCE_NOT_FOUND",
		project
			? `No matching pie-unity instance found for project hint ${project}.`
			: "No matching pie-unity instance found.",
		flags,
		activeInstances,
	);
}

export async function selectReadyInstance(instances, flags) {
	const candidates = getSelectionCandidates(instances, flags);
	const probed = await probeCandidateHealth(candidates, flags);
	const ranked = rankInstancesByHealth(probed);
	const healthy = ranked.filter((item) => item?.__health?.ready === true && item?.__health?.mainThreadResponsive === true);
	if (healthy.length === 1) return healthy[0];
	if (ranked.length === 1) return ranked[0];
	return selectInstance(instances, flags);
}

function getSelectionCandidates(instances, flags) {
	const activeInstances = getActiveInstances(instances);
	const project = normalizePath(flags.project || "");
	const instanceId = String(flags.instance || "").trim();
	const explicitPort = Number(flags.port || 0);

	if (explicitPort > 0) {
		const portMatches = activeInstances
			.filter((item) => Number(item?.port || 0) === explicitPort)
			.filter((item) => !instanceId || String(item?.instanceId || "") === instanceId)
			.filter((item) => !project || computeProjectMatchScore(project, item?.projectPath || "") > 0)
			.sort((left, right) => Number(right?.lastSeenUnix || 0) - Number(left?.lastSeenUnix || 0));
		return portMatches.length > 0 ? portMatches : [{
			instanceId: instanceId || `port_${explicitPort}`,
			projectPath: project,
			port: explicitPort,
			mode: "unknown",
		}];
	}

	if (instanceId) {
		const matches = activeInstances
			.filter((item) => String(item.instanceId || "") === instanceId)
			.filter((item) => !project || computeProjectMatchScore(project, item?.projectPath || "") > 0);
		if (matches.length > 0) return matches;
		throw buildSelectionError(
			"PIE_UNITY_INSTANCE_NOT_FOUND",
			`No pie-unity instance found for --instance ${instanceId}.`,
			flags,
			activeInstances,
		);
	}

	if (project && activeInstances.length > 0) {
		const scored = activeInstances
			.map((item) => ({ item, score: computeProjectMatchScore(project, item.projectPath || "") }))
			.filter((entry) => entry.score > 0)
			.sort((left, right) => right.score - left.score || Number(right.item.lastSeenUnix || 0) - Number(left.item.lastSeenUnix || 0));
		if (scored.length > 0) {
			const bestScore = scored[0].score;
			return scored.filter((entry) => entry.score === bestScore).map((entry) => entry.item);
		}
	}

	return activeInstances;
}

async function probeCandidateHealth(candidates, flags) {
	const next = [];
	for (const instance of candidates) {
		const health = await probeInstanceHealth(instance, flags);
		next.push({ ...instance, __health: health });
	}
	return next;
}

async function probeInstanceHealth(instance, flags) {
	const port = Number(instance?.port || 0);
	if (port <= 0) return { ready: false, mainThreadResponsive: false };
	try {
		return normalizeEnvelope(await requestJson(`http://127.0.0.1:${port}/health`, {}, {
			...flags,
			retries: 1,
			waitMs: 100,
			token: "",
		}));
	} catch (error) {
		return {
			ready: false,
			mainThreadResponsive: false,
			error: error?.message || String(error || ""),
		};
	}
}

function rankInstancesByHealth(instances) {
	return [...instances].sort((left, right) => {
		const leftHealth = left?.__health || {};
		const rightHealth = right?.__health || {};
		const leftReady = leftHealth.ready === true && leftHealth.mainThreadResponsive === true ? 1 : 0;
		const rightReady = rightHealth.ready === true && rightHealth.mainThreadResponsive === true ? 1 : 0;
		if (leftReady !== rightReady) return rightReady - leftReady;
		const leftBridge = leftHealth.bridgeReady === true && leftHealth.scriptHostReady === true ? 1 : 0;
		const rightBridge = rightHealth.bridgeReady === true && rightHealth.scriptHostReady === true ? 1 : 0;
		if (leftBridge !== rightBridge) return rightBridge - leftBridge;
		const leftRuntime = String(left?.mode || "").toLowerCase() === "runtime" ? 1 : 0;
		const rightRuntime = String(right?.mode || "").toLowerCase() === "runtime" ? 1 : 0;
		if (leftRuntime !== rightRuntime) return rightRuntime - leftRuntime;
		return Number(right?.lastSeenUnix || 0) - Number(left?.lastSeenUnix || 0);
	});
}

export async function selectInstanceForCapability(instances, flags, kind, name) {
	const activeInstances = getActiveInstances(instances);
	const hasExplicitSelector = Boolean(flags.project || flags.instance || flags.port);
	if (hasExplicitSelector) {
		const instance = await selectReadyInstance(activeInstances, flags);
		const manifest = await fetchManifestForInstance(instance, flags, { name, retries: 1 });
		if (name && !manifestHasCapability(manifest, kind, name)) {
			throw buildSelectionError(
				"PIE_UNITY_CAPABILITY_NOT_FOUND",
				`Selected pie-unity instance does not expose ${kind} ${name}.`,
				flags,
				[instance],
				{ requestedKind: kind, requestedCapability: name },
			);
		}
		return { instance, manifest };
	}

	const manifests = await fetchCandidateManifests(activeInstances, flags, { name, retries: 1 });
	const matching = manifests.filter((entry) => manifestHasCapability(entry.manifest, kind, name));

	if (matching.length === 1) {
		return matching[0];
	}

	if (matching.length > 1) {
		throw buildSelectionError(
			"PIE_UNITY_CAPABILITY_AMBIGUOUS",
			`Multiple pie-unity instances expose ${kind} ${name}. Pass --instance, --project, or --port.`,
			flags,
			matching.map((entry) => entry.instance),
			{
				reason: "ambiguous",
				requestedKind: kind,
				requestedCapability: name,
				matchingCapabilities: matching.map((entry) => ({
					instance: formatCandidate(entry.instance),
					capabilities: summarizeMatchingCapabilities(entry.manifest, kind, name),
					selection: `--instance ${entry.instance.instanceId || ""}`.trim(),
				})),
			},
		);
	}

	if (activeInstances.length === 1) {
		const instance = activeInstances[0];
		const manifest = manifests.find((entry) => entry.instance === instance)?.manifest || null;
		throw buildSelectionError(
			"PIE_UNITY_CAPABILITY_NOT_FOUND",
			`The only active pie-unity instance does not expose ${kind} ${name}.`,
			flags,
			[instance],
			{ requestedKind: kind, requestedCapability: name, manifestAvailable: Boolean(manifest) },
		);
	}

	throw buildSelectionError(
		"PIE_UNITY_CAPABILITY_NOT_FOUND",
		`No active pie-unity instance exposes ${kind} ${name}.`,
		flags,
		activeInstances,
		{
			requestedKind: kind,
			requestedCapability: name,
			probedInstances: manifests.map((entry) => formatCandidate(entry.instance)),
		},
	);
}

export async function requestJson(url, init = {}, options = {}) {
	const waitMs = Number(options.waitMs || DEFAULT_WAIT_MS);
	let retries = Number(options.retries || DEFAULT_RETRIES);
	let token = String(options.token || "");
	let tokenRefreshAttempted = false;
	let lastError;
	for (let attempt = 0; attempt < retries; attempt += 1) {
		try {
			const requestInit = withPieToken(init, token);
			const response = await fetch(url, requestInit);
			const text = await response.text();
			const json = text ? JSON.parse(text) : {};

			if (!response.ok) {
				if (isRpcUnauthorized(response, json)
					&& !tokenRefreshAttempted
					&& typeof options.refreshToken === "function") {
					tokenRefreshAttempted = true;
					const nextToken = String(options.refreshToken(token) || "");
					if (nextToken && nextToken !== token) {
						token = nextToken;
						options.token = nextToken;
						lastError = new Error(`HTTP ${response.status}: ${text}`);
						if (attempt >= retries - 1) {
							retries += 1;
						}
						continue;
					}
				}
				throw new Error(`HTTP ${response.status}: ${text}`);
			}

			const unavailable = extractAvailability(json);
			if (unavailable) {
				lastError = new Error(unavailable.message || "pie-unity temporarily unavailable");
				if (attempt < retries - 1) {
					await sleep(waitMs);
					continue;
				}
			}

			return json;
		} catch (error) {
			lastError = error;
			if (attempt >= retries - 1) break;
			await sleep(waitMs);
		}
	}
	throw lastError || new Error("Request failed");
}

function isRpcUnauthorized(response, json) {
	return response?.status === 401 || String(json?.errorCode || "") === "RPC_UNAUTHORIZED";
}

function buildTokenRefresh(flags, instance) {
	return (currentToken) => {
		const latest = findMatchingInstance(loadRegistry(), flags, instance);
		const nextToken = String(latest?.token || "");
		return nextToken && nextToken !== currentToken ? nextToken : "";
	};
}

function findMatchingInstance(instances, flags, instance) {
	const instanceId = String(instance?.instanceId || "").trim();
	const port = Number(instance?.port || 0);
	const project = normalizePath(instance?.projectPath || flags?.project || "");
	const matches = instances.filter((item) => {
		if (port > 0 && Number(item?.port || 0) !== port) return false;
		if (project && normalizePath(item?.projectPath || "") !== project) return false;
		if (instanceId && String(item?.instanceId || "") !== instanceId) return false;
		return true;
	});
	return matches.sort((left, right) => Number(right?.lastSeenUnix || 0) - Number(left?.lastSeenUnix || 0))[0] || null;
}

export async function runCli(argv = process.argv) {
	const { command, flags } = parseArgs(argv);
	if (command === "instances") {
		const instances = await probeCandidateHealth(getActiveInstances(loadRegistry()), flags);
		const filtered = flags.project
			? instances.filter((item) => computeProjectMatchScore(flags.project, item.projectPath || "") > 0)
			: instances;
		writeJson({ service: "pie-unity", instances: filtered.map(formatCandidate) });
		return;
	}

	switch (command) {
		case "health": {
			const instance = await selectReadyInstance(loadRegistry(), flags);
			const baseUrl = `http://127.0.0.1:${instance.port}`;
			writeJson(normalizeEnvelope(await requestJson(`${baseUrl}/health`, {}, flags)));
			return;
		}
		case "manifest": {
			const instances = loadRegistry();
			const hasExplicitSelector = Boolean(flags.project || flags.instance || flags.port);
			if (hasExplicitSelector || getActiveInstances(instances).length === 1) {
				const instance = await selectReadyInstance(instances, flags);
				const manifest = await fetchManifestForInstance(instance, flags, {
					namespace: flags.namespace,
					name: flags.name,
				});
				writeJson(addProtocolWarning(normalizeEnvelope(manifest)));
				return;
			}
			const manifests = await fetchCandidateManifests(getActiveInstances(instances), flags, {
				namespace: flags.namespace,
				name: flags.name,
				retries: 1,
			});
			writeJson({
				service: "pie-unity",
				manifests: manifests.map((entry) => ({
					instance: formatCandidate(entry.instance),
					manifest: addProtocolWarning(normalizeEnvelope(entry.manifest)),
				})),
			});
			return;
		}
		case "query": {
			await runToolCommand(flags, "unity_scene_query");
			return;
		}
		case "inspect": {
			await runToolCommand(flags, "unity_scene_object_inspect");
			return;
		}
		case "edit": {
			await runToolCommand(flags, "unity_scene_object_edit");
			return;
		}
		case "log-read": {
			await runToolCommand(flags, "unity_log_read");
			return;
		}
		case "script-run": {
			await runToolCommand(flags, "unity_script_run");
			return;
		}
		case "tool": {
			const name = String(flags.tool || "").trim();
			if (!name) throw new Error("--tool is required");
			await runToolCommand(flags, name);
			return;
		}
		case "rpc": {
			const name = String(flags.method || "").trim();
			if (!name) throw new Error("--method is required");
			const { instance, manifest } = await selectInstanceForCapability(loadRegistry(), flags, "rpc", name);
			const baseUrl = `http://127.0.0.1:${instance.port}`;
			const requestOptions = { ...flags, token: String(flags.token || instance.token || ""), refreshToken: buildTokenRefresh(flags, instance) };
			requireToken(requestOptions, instance);
			const response = attachProtocolWarning(
				normalizeEnvelope(await requestJson(`${baseUrl}/rpc/${encodeURIComponent(name)}`, postJson(parseData(flags.data)), requestOptions)),
				manifest,
			);
			if (name === "pie_chat.set_config") {
				await settleChatConfig(baseUrl, requestOptions, Number(response?.result?.configAppliedVersion || 0));
			}
			writeJson(response);
			return;
		}
		default:
			throw new Error(`Unknown command: ${command}`);
	}
}

async function runToolCommand(flags, name) {
	const { instance, manifest } = await selectInstanceForCapability(loadRegistry(), flags, "tool", name);
	const baseUrl = `http://127.0.0.1:${instance.port}`;
	const requestOptions = { ...flags, token: String(flags.token || instance.token || ""), refreshToken: buildTokenRefresh(flags, instance) };
	requireToken(requestOptions, instance);
	const response = attachProtocolWarning(
		normalizeEnvelope(await requestJson(`${baseUrl}/tool/${encodeURIComponent(name)}`, postJson(parseData(flags.data)), requestOptions)),
		manifest,
	);
	writeJson(response);
}

function extractAvailability(json) {
	const raw = json?.serverAvailability || json?.availability || "";
	if (!raw) return null;
	if (typeof raw === "string") {
		try {
			return JSON.parse(raw);
		} catch {
			return { message: raw };
		}
	}
	return raw;
}

function normalizeEnvelope(json) {
	if (!json || typeof json !== "object") return json;
	if (!("result" in json) && !("serverAvailability" in json)) return json;

	const next = { ...json };
	if (typeof next.result === "string") {
		const trimmed = next.result.trim();
		if (trimmed === "null") {
			next.result = null;
		} else if ((trimmed.startsWith("{") && trimmed.endsWith("}")) || (trimmed.startsWith("[") && trimmed.endsWith("]"))) {
			try {
				next.result = JSON.parse(trimmed);
			} catch {
				// Keep original string.
			}
		}
	}

	if (typeof next.serverAvailability === "string" && next.serverAvailability.trim()) {
		try {
			next.serverAvailability = JSON.parse(next.serverAvailability);
		} catch {
			// Keep original string.
		}
	}

	return next;
}

export function addProtocolWarning(json) {
	if (!json || typeof json !== "object") return json;
	const hostProtocol = String(json.skillProtocolVersion || "");
	if (hostProtocol && hostProtocol !== EXPECTED_SKILL_PROTOCOL_VERSION) {
		return {
			...json,
			skillProtocolWarning: `pie-unity-rpc helper expects ${EXPECTED_SKILL_PROTOCOL_VERSION}, but Unity host reports ${hostProtocol}. Update the helper or com.pie.agent package if calls fail.`,
		};
	}
	if (!hostProtocol) {
		return {
			...json,
			skillProtocolWarning: `Unity host did not report skillProtocolVersion. It may be older than ${EXPECTED_SKILL_PROTOCOL_VERSION}; update com.pie.agent for the full manifest contract.`,
		};
	}
	return json;
}

function attachProtocolWarning(json, manifest) {
	const checked = addProtocolWarning(manifest || {});
	if (!checked?.skillProtocolWarning || !json || typeof json !== "object") return json;
	return { ...json, skillProtocolWarning: checked.skillProtocolWarning };
}

async function fetchManifestForInstance(instance, flags, filters = {}) {
	const requestOptions = {
		...flags,
		token: String(flags.token || instance.token || ""),
		retries: filters.retries || flags.retries,
		refreshToken: buildTokenRefresh(flags, instance),
	};
	requireToken(requestOptions, instance);
	const query = new URLSearchParams();
	if (filters.namespace) query.set("namespace", filters.namespace);
	if (filters.name) query.set("name", filters.name);
	const suffix = query.toString() ? `?${query.toString()}` : "";
	const baseUrl = `http://127.0.0.1:${instance.port}`;
	return requestJson(`${baseUrl}/manifest${suffix}`, {}, requestOptions);
}

async function fetchCandidateManifests(instances, flags, filters = {}) {
	const results = [];
	for (const instance of instances) {
		try {
			results.push({
				instance,
				manifest: await fetchManifestForInstance(instance, flags, filters),
			});
		} catch {
			// Ignore unavailable candidates while probing manifests. A direct
			// selection still reports the request error through fetchManifestForInstance.
		}
	}
	return results;
}

function getManifestCapabilities(manifest) {
	if (Array.isArray(manifest?.capabilities)) return manifest.capabilities;
	return [];
}

function manifestHasCapability(manifest, kind, name) {
	return getManifestCapabilities(manifest).some((capability) =>
		String(capability?.kind || "").toLowerCase() === String(kind || "").toLowerCase()
		&& String(capability?.name || "").toLowerCase() === String(name || "").toLowerCase()
		&& capability?.deprecated !== true);
}

function summarizeMatchingCapabilities(manifest, kind, name) {
	return getManifestCapabilities(manifest)
		.filter((capability) =>
			String(capability?.kind || "").toLowerCase() === String(kind || "").toLowerCase()
			&& String(capability?.name || "").toLowerCase() === String(name || "").toLowerCase())
		.map((capability) => ({
			kind: String(capability?.kind || ""),
			name: String(capability?.name || ""),
			namespace: String(capability?.ns || ""),
			readOnly: capability?.readOnly === true,
			deprecated: capability?.deprecated === true,
			description: String(capability?.description || ""),
		}));
}

function parseData(input) {
	if (!input) return {};
	return JSON.parse(input);
}

function postJson(body) {
	return {
		method: "POST",
		headers: { "Content-Type": "application/json" },
		body: JSON.stringify(body || {}),
	};
}

function withPieToken(init, token) {
	if (!token) return init;
	return {
		...init,
		headers: {
			...(init.headers || {}),
			"X-Pie-Token": token,
		},
	};
}

function requireToken(options, instance) {
	if (options.token) return;
	throw new Error(`Selected pie-unity instance ${instance.instanceId || instance.port} does not include an RPC token. Restart Unity or pass --token for explicit port access.`);
}

async function settleChatConfig(baseUrl, flags, expectedVersion = 0) {
	const settleMs = Math.max(0, Number(flags.configSettleMs || 500));
	const pollMs = Math.max(100, Math.min(settleMs || 200, 250));
	const deadline = Date.now() + Math.max(1500, settleMs * 4 || 1500);

	while (Date.now() < deadline) {
		try {
			const state = normalizeEnvelope(
				await requestJson(`${baseUrl}/tool/${encodeURIComponent("chat_get_state")}`, {}, {
					token: flags.token,
					waitMs: pollMs,
					retries: 1,
				}),
			);
			const appliedVersion = Number(state?.result?.configAppliedVersion || 0);
			const versionApplied = expectedVersion > 0 ? appliedVersion >= expectedVersion : false;
			if (versionApplied || !state?.result?.isBusy) {
				if (settleMs > 0) {
					await sleep(settleMs);
				}
				return;
			}
		} catch {
			// Ignore transient polling failures while the bridge settles.
		}
		await sleep(pollMs);
	}

	if (settleMs > 0) {
		await sleep(settleMs);
	}
}

function sleep(ms) {
	return new Promise((resolve) => setTimeout(resolve, ms));
}

function normalizePath(value) {
	return String(value || "").replace(/\\/g, "/");
}

function writeJson(payload) {
	process.stdout.write(`${JSON.stringify(payload, null, 2)}\n`);
}

function dedupeInstances(items) {
	const sorted = [...items].sort((a, b) => {
		const timeA = Number(a?.lastSeenUnix || 0);
		const timeB = Number(b?.lastSeenUnix || 0);
		return timeB - timeA;
	});
	const seen = new Set();
	const next = [];
	for (const item of sorted) {
		const key = [
			String(item?.instanceId || ""),
			normalizePath(item?.projectPath || ""),
			String(item?.mode || ""),
			Number(item?.pid || 0),
			Number(item?.port || 0),
		].join("|");
		if (seen.has(key)) continue;
		seen.add(key);
		next.push(item);
	}
	return next;
}

function filterDiscoverableInstances(items) {
	const groups = new Map();
	for (const item of items) {
		const key = normalizePath(item?.projectPath || "") || String(item?.instanceId || "");
		if (!groups.has(key)) {
			groups.set(key, []);
		}
		groups.get(key).push(item);
	}

	const next = [];
	for (const group of groups.values()) {
		const sorted = [...group].sort((left, right) => {
			const leftRuntime = String(left?.mode || "").toLowerCase() === "runtime" ? 1 : 0;
			const rightRuntime = String(right?.mode || "").toLowerCase() === "runtime" ? 1 : 0;
			if (leftRuntime !== rightRuntime) return rightRuntime - leftRuntime;
			return Number(right?.lastSeenUnix || 0) - Number(left?.lastSeenUnix || 0);
		});
		if (sorted[0]) next.push(sorted[0]);
	}

	return next.sort((left, right) => Number(right?.lastSeenUnix || 0) - Number(left?.lastSeenUnix || 0));
}

export const __testOnly = {
	findMatchingInstance,
	formatCandidate,
};

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
	runCli().catch((error) => {
		if (error instanceof InstanceSelectionError) {
			process.stderr.write(`${JSON.stringify({
				error: error.message,
				code: error.code,
				...error.details,
			}, null, 2)}\n`);
			process.exitCode = 2;
			return;
		}
		process.stderr.write(`${error?.message || error}\n`);
		process.exitCode = 1;
	});
}
