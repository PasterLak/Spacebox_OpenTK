"use strict";

const API = "api";
const HEADER = { "X-Spacebox-Dashboard": "1" };
const state = { view: "overview", timers: [], commands: [], history: [], historyIndex: -1, lastLine: 0, player: null, players: { online: [], saved: [] }, sort: { key: "name", dir: 1 }, claimItems: [] };

function formatDuration(seconds) {
  const total = Math.max(0, Math.round((seconds || 0) / 60));
  const hours = Math.floor(total / 60);
  return hours > 0 ? `${hours} h ${total % 60} min` : `${total} min`;
}

const $ = (selector, root = document) => root.querySelector(selector);
const $$ = (selector, root = document) => Array.from(root.querySelectorAll(selector));

function el(tag, props = {}, ...children) {
  const node = document.createElement(tag);
  for (const [key, value] of Object.entries(props)) {
    if (value === undefined || value === null || value === false) continue;
    if (key === "class") node.className = value;
    else if (key === "text") node.textContent = value;
    else if (key.startsWith("on")) node.addEventListener(key.slice(2), value);
    else node.setAttribute(key, value === true ? "" : value);
  }
  for (const child of children.flat()) {
    if (child === null || child === undefined || child === false) continue;
    node.append(child instanceof Node ? child : document.createTextNode(String(child)));
  }
  return node;
}

class ApiError extends Error {
  constructor(message, status) { super(message); this.status = status; }
}

async function api(path, body) {
  const options = { method: body === undefined ? "GET" : "POST", headers: { ...HEADER }, credentials: "same-origin" };
  if (body !== undefined) {
    options.headers["Content-Type"] = "application/json";
    options.body = JSON.stringify(body);
  }
  const response = await fetch(`${API}${path}`, options);
  if (response.status === 401 && !path.startsWith("/login") && !path.startsWith("/setup")) {
    showAuth();
    throw new ApiError("Not logged in", 401);
  }
  const data = response.headers.get("Content-Type")?.includes("json") ? await response.json() : null;
  if (!response.ok) throw new ApiError(data?.error || `Request failed (${response.status})`, response.status);
  return data;
}

function toast(message, bad = false) {
  const node = $("#toast");
  node.textContent = message;
  node.classList.toggle("bad", bad);
  node.classList.remove("hidden");
  clearTimeout(toast.timer);
  toast.timer = setTimeout(() => node.classList.add("hidden"), 3500);
}

function confirmAction(text) {
  return new Promise(resolve => {
    const modal = $("#confirm");
    $("#confirm-text").textContent = text;
    modal.classList.remove("hidden");
    const close = result => {
      modal.classList.add("hidden");
      $("#confirm-yes").onclick = null;
      $("#confirm-no").onclick = null;
      resolve(result);
    };
    $("#confirm-yes").onclick = () => close(true);
    $("#confirm-no").onclick = () => close(false);
  });
}

async function run(action, success) {
  try {
    const result = await action();
    if (success) toast(success);
    return result;
  } catch (error) {
    if (error.status !== 401) toast(error.message, true);
    return null;
  }
}

function formatDuration(seconds) {
  const d = Math.floor(seconds / 86400), h = Math.floor(seconds % 86400 / 3600), m = Math.floor(seconds % 3600 / 60);
  if (d > 0) return `${d}d ${h}h`;
  if (h > 0) return `${h}h ${m}m`;
  return `${m}m ${Math.floor(seconds % 60)}s`;
}

function formatBytes(bytes) {
  if (bytes < 1024 * 1024) return `${Math.max(1, Math.round(bytes / 1024))} KB`;
  if (bytes < 1024 * 1024 * 1024) return `${(bytes / 1048576).toFixed(1)} MB`;
  return `${(bytes / 1073741824).toFixed(2)} GB`;
}

function formToObject(form) {
  const data = {};
  for (const input of form.elements) {
    if (!input.name) continue;
    if (input.type === "checkbox") data[input.name] = input.checked;
    else if (input.type === "number") data[input.name] = Number(input.value);
    else data[input.name] = input.value;
  }
  return data;
}

function stopTimers() {
  state.timers.forEach(clearInterval);
  state.timers = [];
}

function every(ms, fn) {
  fn();
  state.timers.push(setInterval(fn, ms));
}

async function start() {
  try {
    const info = await api("/state");
    $("#auth-server").textContent = info.version ? info.serverName + " · v" + info.version : info.serverName;
    if (info.loggedIn) showApp(info.user);
    else showAuth(info.setupRequired);
  } catch (error) {
    $("#auth-error").textContent = "The server does not answer";
    showAuth(false);
  }
}

async function showAuth(setupRequired) {
  stopTimers();
  if (setupRequired === undefined) {
    try { setupRequired = (await api("/state")).setupRequired; } catch { setupRequired = false; }
  }
  $("#app").classList.add("hidden");
  $("#auth").classList.remove("hidden");
  $("#setup-form").classList.toggle("hidden", !setupRequired);
  $("#login-form").classList.toggle("hidden", setupRequired);
}

function showApp(user) {
  $("#auth").classList.add("hidden");
  $("#app").classList.remove("hidden");
  $("#who").textContent = "Signed in as " + user;
  loadIcon();
  api("/commands").then(list => state.commands = list).catch(() => {});
  switchView(state.view === "player" ? "players" : state.view);
}

$("#setup-form").addEventListener("submit", async event => {
  event.preventDefault();
  const data = formToObject(event.target);
  if (data.password !== data.password2) { $("#auth-error").textContent = "The passwords do not match"; return; }
  try {
    const result = await api("/setup", { key: data.key, name: data.name, password: data.password });
    $("#auth-error").textContent = "";
    showApp(result.user);
  } catch (error) { $("#auth-error").textContent = error.message; }
});

$("#login-form").addEventListener("submit", async event => {
  event.preventDefault();
  try {
    const result = await api("/login", formToObject(event.target));
    $("#auth-error").textContent = "";
    event.target.reset();
    showApp(result.user);
  } catch (error) { $("#auth-error").textContent = error.message; }
});

$("#logout").addEventListener("click", async () => {
  await run(() => api("/logout", {}));
  showAuth(false);
});

$$("#nav button").forEach(button => button.addEventListener("click", () => switchView(button.dataset.view)));

const views = {
  overview: () => { loadBackups(); every(3000, refreshOverview); },
  players: () => { every(4000, refreshPlayers); },
  map: () => { spaceMap.show(); every(2000, spaceMap.refresh); },
  player: () => { every(10000, () => state.player && loadPlayer(state.player)); },
  moderation: () => { refreshModeration(); },
  console: () => { every(1000, pollConsole); $("#console-form input").focus(); },
  settings: () => { loadSettings(); loadGameplay(); loadClaims(); }
};

function switchView(view) {
  stopTimers();
  state.view = view;
  $$(".view").forEach(section => section.classList.toggle("hidden", section.dataset.view !== view));
  $$("#nav button").forEach(button => button.classList.toggle("active", button.dataset.view === view || (view === "player" && button.dataset.view === "players")));
  views[view]?.();
}

async function loadIcon() {
  const response = await fetch(`${API}/server/icon?ts=${Date.now()}`, { headers: HEADER });
  const ok = response.ok;
  const url = ok ? URL.createObjectURL(await response.blob()) : null;
  for (const image of [$("#brand-icon"), $("#icon-preview")]) {
    image.classList.toggle("hidden", !ok);
    if (url) image.src = url;
  }
}

function card(label, value, tone) {
  return el("div", { class: "card" }, el("div", { class: "label", text: label }), el("div", { class: `value ${tone || ""}`, text: value }));
}

async function refreshOverview() {
  const [info, metrics] = await Promise.all([run(() => api("/server")), run(() => api("/metrics"))]);
  if (!info || !metrics) return;

  $("#brand-name").textContent = info.name;
  const latest = metrics.latest;
  const tpsTone = latest.tps >= info.targetTps * 0.9 ? "good" : latest.tps >= info.targetTps * 0.6 ? "warn" : "bad";
  const cpuTone = latest.cpu < 60 ? "good" : latest.cpu < 85 ? "warn" : "bad";
  $("#cards").replaceChildren(
    card("Players", `${info.players} / ${info.maxPlayers}`),
    ...(info.statistics ? [card("Most today", String(info.statistics.today)), card("Most ever", String(info.statistics.allTime))] : []),
    card("Uptime", formatDuration(metrics.uptimeSeconds)),
    card("CPU", `${latest.cpu}%`, cpuTone),
    card("Memory", `${Math.round(latest.ramMb)} MB`),
    card("Game ticks / s", `${latest.tps} / ${info.targetTps}`, tpsTone),
    card("Update time", `${latest.updateMs} ms`),
    card("World size", formatBytes(info.worldSizeBytes))
  );

  const rows = [
    ["Name", info.name], ["Description", info.description || "—"], ["Version", info.version], ["World", info.world],
    ["Seed", info.seed], ["GameSet", info.modId], ["Game mode", info.gameMode], ["Game port (UDP)", info.port],
    ["Dashboard port (TCP)", info.dashboardPort], ["HTTPS", info.https], ["Whitelist", info.whitelistEnabled ? "on" : "off"],
    ["Registration", info.registrationEnabled ? "on" : "off"], ["Autosave", info.autoSaveInterval > 0 ? `every ${info.autoSaveInterval}s` : "off"],
    ["Server frame rate", `${info.updateRate} per second`],
    ["Started", info.startedAt], ["System", `${info.os}, ${info.processors} cores`], ["Runtime", info.runtime]
  ];
  $("#server-info").replaceChildren(...rows.map(([key, value]) => el("tr", {}, el("td", { text: key }), el("td", { text: String(value) }))));

  renderStatistics(info.statistics);

  const history = metrics.history;
  drawChart($("#chart-cpu"), history.map(s => s.cpu), "#ffc428", 100);
  drawChart($("#chart-ram"), history.map(s => s.ramMb), "#6ed2e6");
  drawChart($("#chart-players"), history.map(s => s.players), "#6edc6e", Math.max(4, info.maxPlayers));
  drawChart($("#chart-tps"), history.map(s => s.tps), "#ff9b3d", Math.max(10, Math.ceil(info.targetTps * 1.2)));
}

function renderStatistics(statistics) {
  const panel = $("#statistics-panel");
  panel.classList.toggle("hidden", !statistics);
  if (!statistics) return;

  const rows = [
    ["Most players today", statistics.today],
    ["Most players ever", statistics.allTimeAt ? `${statistics.allTime} (${statistics.allTimeAt})` : statistics.allTime],
    ["Joins", statistics.joins], ["Different players", statistics.uniquePlayers],
    ["Time played by all players", statistics.playTime], ["Average session", statistics.averageSession],
    ["Average time per player", statistics.averagePerPlayer], ["Server running time", statistics.runningTime]
  ];
  $("#statistics-info").replaceChildren(...rows.map(([key, value]) => el("tr", {}, el("td", { text: key }), el("td", { text: String(value) }))));
  const top = (statistics.topPlayers || []).map((p, i) => el("tr", { class: "clickable", onclick: () => openPlayer(p.name) },
    el("td", { text: `${i + 1}. ${p.name}` }),
    el("td", { "data-label": "Played", text: formatDuration(p.playSeconds) }),
    el("td", { "data-label": "Sessions", class: "faint", text: `${p.sessions} sessions` })));
  $("#top-players").replaceChildren(...(top.length ? top : [el("tr", {}, el("td", { class: "empty", text: "Nobody has played yet" }))]));
  drawBars($("#chart-days"), statistics.days.map(d => d.peak), statistics.days.map(d => d.day.slice(5)));
}

function drawBars(canvas, values, labels) {
  const ratio = window.devicePixelRatio || 1;
  const width = canvas.clientWidth, height = canvas.clientHeight;
  canvas.width = width * ratio;
  canvas.height = height * ratio;
  const g = canvas.getContext("2d");
  g.scale(ratio, ratio);
  g.clearRect(0, 0, width, height);
  const max = Math.max(1, ...values);
  const slot = width / Math.max(1, values.length);
  g.font = "10px system-ui";
  g.textAlign = "center";
  values.forEach((value, i) => {
    const bar = (height - 30) * value / max;
    const x = i * slot + slot * 0.15;
    g.fillStyle = "#6edc6e";
    g.fillRect(x, height - 16 - bar, slot * 0.7, bar);
    g.fillStyle = "#c8ccd4";
    if (value > 0) g.fillText(String(value), x + slot * 0.35, height - 20 - bar);
    g.fillStyle = "#828791";
    if (i % 2 === values.length % 2) g.fillText(labels[i], x + slot * 0.35, height - 3);
  });
}

function drawChart(canvas, values, color, fixedMax) {
  const ratio = window.devicePixelRatio || 1;
  const width = canvas.clientWidth, height = canvas.clientHeight;
  canvas.width = width * ratio;
  canvas.height = height * ratio;
  const g = canvas.getContext("2d");
  g.scale(ratio, ratio);
  g.clearRect(0, 0, width, height);

  const max = fixedMax || Math.max(1, ...values) * 1.15;
  g.strokeStyle = "#ffffff12";
  g.fillStyle = "#828791";
  g.font = "11px system-ui";
  g.lineWidth = 1;
  for (let i = 0; i <= 4; i++) {
    const y = Math.round(8 + (height - 20) * i / 4) + 0.5;
    g.beginPath(); g.moveTo(34, y); g.lineTo(width, y); g.stroke();
    g.fillText(String(Math.round(max * (1 - i / 4))), 0, y + 4);
  }
  if (values.length < 2) return;

  const x = i => 34 + (width - 36) * i / (values.length - 1);
  const y = v => 8 + (height - 20) * (1 - Math.min(v, max) / max);
  const gradient = g.createLinearGradient(0, 0, 0, height);
  gradient.addColorStop(0, color + "55");
  gradient.addColorStop(1, color + "00");

  g.beginPath();
  values.forEach((v, i) => i === 0 ? g.moveTo(x(i), y(v)) : g.lineTo(x(i), y(v)));
  g.lineTo(x(values.length - 1), height - 12);
  g.lineTo(x(0), height - 12);
  g.closePath();
  g.fillStyle = gradient;
  g.fill();

  g.beginPath();
  values.forEach((v, i) => i === 0 ? g.moveTo(x(i), y(v)) : g.lineTo(x(i), y(v)));
  g.strokeStyle = color;
  g.lineWidth = 2;
  g.stroke();
}

$$("[data-action]").forEach(button => button.addEventListener("click", async () => {
  const action = button.dataset.action;
  if (action === "save") return run(() => api("/server/save", {}), "World saved");
  const text = action === "stop" ? "Save the world and stop the server?" : "Save the world and restart the server?";
  if (!(await confirmAction(text))) return;
  await run(() => api(`/server/${action}`, {}), action === "stop" ? "The server is stopping" : "The server is restarting");
  if (action === "restart") setTimeout(() => location.reload(), 8000);
}));

$("#say-form").addEventListener("submit", async event => {
  event.preventDefault();
  const result = await run(() => api("/server/say", formToObject(event.target)), "Message sent");
  if (result) event.target.reset();
});

async function refreshPlayers() {
  if (document.activeElement?.classList.contains("mode-select")) return;
  const data = await run(() => api("/players"));
  if (!data) return;
  state.players = data;
  if (data.gameModes) GAME_MODES = data.gameModes;
  $("#online-count").textContent = data.online.length;

  const online = data.online.map(p => el("tr", { class: "clickable", onclick: () => openPlayer(p.name) },
    el("td", {}, el("span", { class: "dot on" }), p.name, p.muted ? el("span", { class: "tag red", text: "muted" }) : null, !p.alive ? el("span", { class: "tag", text: "dead" }) : null),
    el("td", { "data-label": "Ping", text: `${p.ping} ms` }),
    el("td", { "data-label": "Session", text: formatDuration(p.sessionSeconds) }),
    el("td", { "data-label": "Position", class: "faint", text: p.position ? p.position.join(", ") : "—" }),
    el("td", { "data-label": "Address", class: "faint", text: p.address }),
    el("td", { "data-label": "Game mode" }, gameModeSelect(p.name, p.gameMode)),
    el("td", {}, playerButtons(p.name, true, p.muted))));
  $("#online-table").replaceChildren(
    el("tr", {}, ...["Name", "Ping", "Session", "Position", "Address", "Game mode", ""].map(t => el("th", { text: t }))),
    ...(online.length ? online : [el("tr", {}, el("td", { class: "empty", colspan: "7", text: "Nobody is online" }))]));
  renderSaved();
}

function renderSaved() {
  const filter = $("#player-filter").value.trim().toLowerCase();
  const saved = state.players.saved.filter(p => p.name.toLowerCase().includes(filter));
  const { key, dir } = state.sort;
  saved.sort((a, b) => {
    const x = a[key], y = b[key];
    const order = typeof x === "number" ? x - y : String(x).localeCompare(String(y), undefined, { sensitivity: "base" });
    return order * dir || a.name.localeCompare(b.name);
  });
  const rows = saved.map(p => el("tr", { class: "clickable", onclick: () => openPlayer(p.name) },
    el("td", {}, el("span", { class: p.online ? "dot on" : "dot" }), p.name, p.muted ? el("span", { class: "tag red", text: "muted" }) : null),
    el("td", { "data-label": "Played", text: formatDuration(p.playSeconds) }),
    el("td", { "data-label": "Sessions", class: "faint", text: String(p.sessions) }),
    el("td", { "data-label": "Avg session", class: "faint", text: formatDuration(p.averageSessionSeconds) }),
    el("td", { "data-label": "Last save", class: "faint", text: p.lastSeen }),
    el("td", { "data-label": "Game mode" }, gameModeSelect(p.name, p.gameMode)),
    el("td", {}, playerButtons(p.name, p.online, p.muted))));
  const columns = [["Name", "name", 1], ["Played", "playSeconds", -1], ["Sessions", "sessions", -1], ["Avg session", "averageSessionSeconds", -1], ["Last save", "lastSeen", -1], ["Game mode"], [""]];
  $("#saved-table").replaceChildren(
    el("tr", {}, ...columns.map(([title, field, first]) => field
      ? el("th", { text: title, class: `sortable${key === field ? " sorted" + (dir > 0 ? " up" : "") : ""}`, title: "Sort", onclick: () => sortSaved(field, first) })
      : el("th", { text: title }))),
    ...(rows.length ? rows : [el("tr", {}, el("td", { class: "empty", colspan: "7", text: "No saved players" }))]));
}

function sortSaved(field, firstDirection) {
  state.sort = state.sort.key === field ? { key: field, dir: -state.sort.dir } : { key: field, dir: firstDirection };
  renderSaved();
}

$("#player-filter").addEventListener("input", renderSaved);

let GAME_MODES = [{ id: 0, name: "Survival" }, { id: 1, name: "Creative" }, { id: 2, name: "Spectator" }];

function gameModeName(id) {
  const mode = GAME_MODES.find(m => m.id === id);
  return mode ? mode.name : "Mode " + id;
}

function gameModeSelect(name, mode) {
  const select = el("select", { class: "mode-select", title: "Game mode (kept when the player rejoins)", onclick: event => event.stopPropagation() },
    ...GAME_MODES.map(m => el("option", { value: m.id, text: m.name, selected: m.id === mode })));
  select.addEventListener("change", async event => {
    event.stopPropagation();
    const value = Number(select.value);
    const result = await run(() => api("/players/gamemode", { name, mode: value }));
    if (!result) { select.value = mode; return; }
    mode = value;
    toast(`${name}: ${gameModeName(value)}${result.applied ? "" : " (applied when the player joins)"}`);
  });
  return select;
}

function playerButtons(name, online, muted) {
  const stop = handler => event => { event.stopPropagation(); handler(); };
  return el("div", { class: "actions" },
    online ? el("button", { class: "btn small", text: "Kick", onclick: stop(() => moderate("kick", name)) }) : null,
    el("button", { class: "btn small", text: muted ? "Unmute" : "Mute", onclick: stop(() => mute(name, !muted)) }),
    el("button", { class: "btn small danger", text: "Ban", onclick: stop(() => moderate("ban", name)) }));
}

async function moderate(action, name) {
  if (!(await confirmAction(`${action === "ban" ? "Ban" : "Kick"} ${name}?`))) return;
  await run(() => api(`/players/${action}`, { name }), `${name} was ${action === "ban" ? "banned" : "kicked"}`);
  refreshCurrent();
}

async function mute(name, muted) {
  await run(() => api("/players/mute", { name, muted }), `${name} was ${muted ? "muted" : "unmuted"}`);
  refreshCurrent();
}

function refreshCurrent() {
  if (state.view === "players") refreshPlayers();
  if (state.view === "moderation") refreshModeration();
  if (state.view === "player" && state.player) loadPlayer(state.player);
}

function openPlayer(name) {
  state.player = name;
  switchView("player");
}

async function loadPlayer(name) {
  let data;
  try {
    data = await api(`/players/detail?name=${encodeURIComponent(name)}`);
  } catch (error) {
    if (error.status !== 401) toast(error.message, true);
    return;
  }

  $("#player-title").textContent = name;
  $("#player-actions").replaceChildren(
    el("button", { class: "btn ghost", text: "← Back", onclick: () => switchView("players") }),
    gameModeSelect(name, data.gameMode),
    playerButtons(name, data.online, false));

  const bar = (label, value, max, kind) => el("div", {},
    el("div", { class: "bar-label" }, el("span", { text: label }), el("span", { text: `${Math.round(value)} / ${max}` })),
    (() => { const inner = el("div"); inner.style.width = `${Math.max(0, Math.min(100, value / max * 100))}%`; return el("div", { class: `bar ${kind}` }, inner); })());

  $("#player-status").replaceChildren(el("div", { class: "bars" },
    el("div", { class: "faint", text: data.online ? "Online now · data from the last sync" : "Offline · data from the save file" }),
    bar("Health", data.health, Math.max(100, Math.round(data.health)), "health"),
    bar("Power", data.power, Math.max(100, Math.round(data.power)), "power"),
    bar("Oxygen", data.oxygen, Math.max(100, Math.round(data.oxygen)), "oxygen"),
    el("div", { class: "faint", text: `Position ${data.position.map(v => v.toFixed(1)).join(", ")}` })));

  renderSlots($("#player-equipment"), data.equipment, 6);
  renderSlots($("#player-panel"), data.panel, 10);
  renderSlots($("#player-inventory"), data.inventory, 30);

  const stats = data.statistics && typeof data.statistics === "object" ? Object.entries(data.statistics) : [];
  $("#player-stats").replaceChildren(...(stats.length
    ? stats.filter(([, v]) => typeof v !== "object").map(([k, v]) => el("tr", {}, el("td", { text: k.replace(/([a-z])([A-Z])/g, "$1 $2") }), el("td", { text: String(v) })))
    : [el("tr", {}, el("td", { class: "empty", text: "No statistics" }))]));
}

function renderSlots(container, slots, minimum) {
  const sorted = [...slots].sort((a, b) => a.y - b.y || a.x - b.x);
  const nodes = sorted.map(slot => {
    const initials = el("span", { class: "initials hidden", text: slot.name.split(" ").map(w => w[0]).join("").slice(0, 3).toUpperCase() });
    const image = el("img", { alt: slot.name, src: `${API}/items/icon?id=${encodeURIComponent(slot.id)}` });
    image.addEventListener("error", () => { image.remove(); initials.classList.remove("hidden"); });
    return el("div", { class: "slot", title: `${slot.name} (${slot.id})` }, image, initials, slot.count > 1 ? el("span", { class: "count", text: slot.count }) : null);
  });
  while (nodes.length < minimum) nodes.push(el("div", { class: "slot empty-slot" }));
  container.replaceChildren(...nodes);
}

async function refreshModeration() {
  const data = await run(() => api("/moderation"));
  if (!data) return;

  const chips = (list, values, remove) => list.replaceChildren(...(values.length
    ? values.map(value => el("li", {}, value, el("button", { title: "Remove", text: "×", onclick: () => remove(value) })))
    : [el("li", { class: "empty", text: "Nobody" })]));

  chips($("#banned-names"), data.bannedNames, name => run(() => api("/moderation/unban", { name }), `${name} was unbanned`).then(refreshModeration));
  chips($("#banned-addresses"), data.bannedAddresses, address => run(() => api("/moderation/unban-address", { address }), `${address} was unbanned`).then(refreshModeration));
  chips($("#muted-names"), data.mutedNames, name => mute(name, false));
  chips($("#whitelist"), data.whitelist, name => run(() => api("/whitelist/remove", { name }), `${name} was removed`).then(refreshModeration));
  $("#whitelist-enabled").checked = data.whitelistEnabled;

  const admins = await run(() => api("/admins"));
  if (!admins) return;
  chips($("#admins"), admins.admins, name => run(() => api("/admins/remove", { name }), `${name} is no longer an admin`).then(refreshModeration));
  $("#show-admins").checked = admins.showAdmins;
  $("#admins-warning").classList.toggle("hidden", admins.rightsActive);
}

$("#ban-form").addEventListener("submit", async event => {
  event.preventDefault();
  const name = event.target.name.value.trim();
  if (!(await confirmAction(`Ban ${name}?`))) return;
  await run(() => api("/players/ban", { name }), `${name} was banned`);
  event.target.reset();
  refreshModeration();
});

$("#whitelist-form").addEventListener("submit", async event => {
  event.preventDefault();
  await run(() => api("/whitelist/add", formToObject(event.target)), "Added to the whitelist");
  event.target.reset();
  refreshModeration();
});

$("#admins-form").addEventListener("submit", async event => {
  event.preventDefault();
  await run(() => api("/admins/add", formToObject(event.target)), "Admin added");
  event.target.reset();
  refreshModeration();
});

$("#show-admins").addEventListener("change", event =>
  run(() => api("/admins/show", { enabled: event.target.checked }), event.target.checked ? "Admins are shown" : "Admins are hidden"));

$("#whitelist-enabled").addEventListener("change", event =>
  run(() => api("/whitelist/enabled", { enabled: event.target.checked }), event.target.checked ? "Whitelist on" : "Whitelist off"));

async function pollConsole() {
  let data;
  try { data = await api(`/console?after=${state.lastLine}`); } catch { return; }
  if (!data.lines.length) return;

  const output = $("#console-output");
  output.append(...data.lines.map(line => el("div", { class: `line ${line.level}` }, el("span", { class: "time", text: line.time }), el("span", { text: line.text }))));
  state.lastLine = data.lines[data.lines.length - 1].id;
  const follow = $("#console-follow").checked;
  const before = output.scrollHeight - output.scrollTop;
  while (output.childElementCount > 5000) output.firstChild.remove();
  if (follow) output.scrollTop = output.scrollHeight;
  else output.scrollTop = output.scrollHeight - before;
}

$("#console-output").addEventListener("scroll", () => {
  const output = $("#console-output");
  const atBottom = output.scrollHeight - output.scrollTop - output.clientHeight < 24;
  if ($("#console-follow").checked !== atBottom) $("#console-follow").checked = atBottom;
});

$("#console-follow").addEventListener("change", event => {
  if (event.target.checked) $("#console-output").scrollTop = $("#console-output").scrollHeight;
});

const consoleInput = $("#console-form input");
let suggestIndex = -1;

function suggestions() {
  const text = consoleInput.value.trim().toLowerCase();
  if (!text || text.includes(" ")) return [];
  return state.commands.filter(c => c.name.toLowerCase().startsWith(text)).slice(0, 12);
}

function renderSuggest() {
  const list = suggestions();
  const box = $("#console-suggest");
  box.classList.toggle("hidden", list.length === 0);
  box.replaceChildren(...list.map((c, i) => el("div", { class: i === suggestIndex ? "active" : "", onmousedown: event => { event.preventDefault(); pick(c.name); } },
    el("b", { text: c.name }), el("span", { text: c.description || "" }))));
}

function pick(name) {
  consoleInput.value = name + " ";
  suggestIndex = -1;
  renderSuggest();
  consoleInput.focus();
}

consoleInput.addEventListener("input", () => { suggestIndex = -1; renderSuggest(); });
consoleInput.addEventListener("blur", () => setTimeout(() => $("#console-suggest").classList.add("hidden"), 100));
consoleInput.addEventListener("keydown", event => {
  const list = suggestions();
  if (event.key === "Tab" && list.length) {
    event.preventDefault();
    pick(list[Math.max(0, suggestIndex)].name);
  } else if (event.key === "ArrowUp" || event.key === "ArrowDown") {
    event.preventDefault();
    const step = event.key === "ArrowUp" ? 1 : -1;
    if (list.length && consoleInput.value.trim() && !consoleInput.value.includes(" ")) {
      suggestIndex = (suggestIndex - step + list.length) % list.length;
      renderSuggest();
      return;
    }
    state.historyIndex = Math.max(-1, Math.min(state.history.length - 1, state.historyIndex + step));
    consoleInput.value = state.historyIndex >= 0 ? state.history[state.history.length - 1 - state.historyIndex] : "";
  } else if (event.key === "Escape") {
    $("#console-suggest").classList.add("hidden");
  }
});

$("#console-form").addEventListener("submit", async event => {
  event.preventDefault();
  const list = suggestions();
  if (suggestIndex >= 0 && list[suggestIndex]) { pick(list[suggestIndex].name); return; }
  const command = consoleInput.value.trim();
  if (!command) return;
  state.history.push(command);
  state.historyIndex = -1;
  consoleInput.value = "";
  renderSuggest();
  await run(() => api("/console", { command }));
  pollConsole();
});

async function loadSettings() {
  const info = await run(() => api("/server"));
  if (!info) return;
  const form = $("#settings-form");
  form.name.value = info.name;
  form.description.value = info.description;
  form.maxPlayers.value = info.maxPlayers;
  form.autoSaveInterval.value = info.autoSaveInterval;
  form.updateRate.value = info.updateRate;
  form.lanDiscovery.checked = info.lanDiscovery;
  loadIcon();
}

async function loadBackups() {
  const data = await run(() => api("/backups"));
  if (!data) return;
  $("#backup-info").textContent = data.enabled
    ? `Automatic every ${data.intervalMinutes} min, the last ${data.keepLast} are kept. Files in Worlds/_backups.`
    : "Automatic backups are off (Backups in serverconfig.json). Files in Worlds/_backups.";
  $("#backup-now").disabled = data.running;
  const backupForm = $("#backup-form");
  if (!backupForm.contains(document.activeElement)) {
    backupForm.enabled.checked = data.enabled;
    backupForm.intervalMinutes.value = data.intervalMinutes;
    backupForm.keepLast.value = data.keepLast;
    backupForm.onStop.checked = data.onStop;
  }
  $("#backup-list").replaceChildren(...(data.backups.length
    ? data.backups.map(b => el("tr", {}, el("td", { text: b.name }), el("td", { class: "faint", text: b.created }), el("td", { class: "faint", text: b.sizeMb >= 0.1 ? `${b.sizeMb} MB` : "< 0.1 MB" })))
    : [el("tr", {}, el("td", { class: "empty", text: "No backups yet" }))]));
}

$("#backup-form").addEventListener("submit", async event => {
  event.preventDefault();
  const form = event.target;
  const result = await run(() => api("/backups/settings", {
    enabled: form.enabled.checked,
    intervalMinutes: Number(form.intervalMinutes.value),
    keepLast: Math.round(Number(form.keepLast.value)),
    onStop: form.onStop.checked
  }), "Backup settings saved");
  if (result?.corrected) toast("Some values were out of range and were corrected", true);
  loadBackups();
});

$("#backup-now").addEventListener("click", async () => {
  $("#backup-now").disabled = true;
  await run(() => api("/backups", {}), "Backup started");
  setTimeout(loadBackups, 3000);
});

async function loadGameplay() {
  const data = await run(() => api("/gameplay"));
  if (!data) return;
  const list = $("#rule-list");
  list.replaceChildren(...data.rules.map(ruleRow));
}

function ruleRow(rule) {
  const row = document.createElement("div");
  row.className = "rule-row";
  const text = document.createElement("div");
  text.className = "rule-text";
  const title = document.createElement("b");
  title.textContent = rule.label;
  const hint = document.createElement("span");
  hint.className = "hint";
  hint.textContent = rule.description || "";
  text.append(title, hint);

  const control = document.createElement("div");
  control.className = "rule-control";
  if (rule.kind === "switch") {
    const label = document.createElement("label");
    label.className = "switch";
    const input = document.createElement("input");
    input.type = "checkbox";
    input.checked = !!rule.value;
    input.setAttribute("aria-label", rule.label);
    input.addEventListener("change", () => saveRule(rule.name, input.checked, input));
    label.append(input, document.createElement("span"));
    control.append(label);
  } else {
    const input = document.createElement("input");
    input.type = "number";
    input.min = rule.min;
    input.max = rule.max;
    input.step = 1;
    input.value = rule.value;
    input.setAttribute("aria-label", rule.label);
    const unit = document.createElement("span");
    unit.className = "hint";
    unit.textContent = rule.unit || "";
    input.addEventListener("change", () => saveRule(rule.name, Number(input.value), input));
    control.append(input, unit);
  }

  row.append(text, control);
  return row;
}

async function saveRule(name, value, input) {
  input.disabled = true;
  const ok = await run(() => api("/gameplay", { [name]: value }), "Rule saved");
  input.disabled = false;
  if (!ok) loadGameplay();
}

async function loadClaims() {
  const data = await run(() => api("/claims"));
  if (!data) return;
  const s = data.settings;
  const form = $("#claims-form");
  form.enabled.checked = s.enabled;
  form.radius.value = s.radius;
  form.maxStoredHours.value = Math.round(s.maxStoredMinutes / 60);
  form.warningMinutes.value = s.warningMinutes;
  form.extraClaimCostPercent.value = s.extraClaimCostPercent;
  form.maxClaimsPerPlayer.value = s.maxClaimsPerPlayer;
  form.maxMembers.value = s.maxMembers;
  form.minDistanceFromSpawn.value = s.minDistanceFromSpawn;
  state.claimItems = data.items || [];
  $("#payment-rows").replaceChildren(...Object.entries(s.payments || {}).map(([id, minutes]) => paymentRow(id, minutes)));
  $("#claims-count").textContent = `${data.total} claim(s) in this world, ${data.loaded} loaded.`;
}

function matchItemId(id) {
  const lower = id.toLowerCase();
  const exact = state.claimItems.find(item => item.id.toLowerCase() === lower);
  if (exact) return exact.id;
  const short = state.claimItems.find(item => item.id.toLowerCase().endsWith(":" + lower));
  return short ? short.id : id;
}

function paymentRow(id = "", minutes = 60) {
  const value = id ? matchItemId(id) : (state.claimItems[0]?.id || "");
  const icon = el("img", { alt: "" });
  const select = el("select", { "aria-label": "Resource" },
    ...state.claimItems.map(item => el("option", { value: item.id, text: `${item.name} (${item.id})`, selected: item.id === value })));
  if (value && !state.claimItems.some(item => item.id === value)) select.append(el("option", { value, text: `${value} (unknown)`, selected: true }));
  const showIcon = () => { icon.src = `${API}/items/icon?id=${encodeURIComponent(select.value)}`; };
  select.addEventListener("change", showIcon);
  showIcon();
  const time = el("input", { type: "number", min: "1", step: "1", value: minutes, "aria-label": "Minutes", title: "Protection minutes per item" });
  const row = el("div", { class: "payment-row" }, icon, select, time, el("span", { class: "hint", text: "min" }),
    el("button", { class: "btn small danger", type: "button", text: "Remove", onclick: () => row.remove() }));
  return row;
}

$("#payment-add").addEventListener("click", () => $("#payment-rows").append(paymentRow()));

function readPayments() {
  const payments = {};
  for (const row of $$("#payment-rows .payment-row")) {
    const id = $("select", row).value;
    const minutes = Number($("input", row).value);
    if (id && minutes > 0) payments[id] = Math.round(minutes);
  }
  return payments;
}

$("#claims-form").addEventListener("submit", async event => {
  event.preventDefault();
  const form = event.target;
  const body = {
    enabled: form.enabled.checked,
    radius: Number(form.radius.value),
    maxStoredMinutes: Math.round(Number(form.maxStoredHours.value) * 60),
    warningMinutes: Math.round(Number(form.warningMinutes.value)),
    extraClaimCostPercent: Number(form.extraClaimCostPercent.value),
    maxClaimsPerPlayer: Number(form.maxClaimsPerPlayer.value),
    maxMembers: Number(form.maxMembers.value),
    minDistanceFromSpawn: Number(form.minDistanceFromSpawn.value),
    payments: readPayments()
  };
  const result = await run(() => api("/claims/settings", body), "Claim settings saved");
  if (result?.corrected) toast("Some values were out of range and were reset to defaults", true);
  loadClaims();
});

$("#settings-form").addEventListener("submit", async event => {
  event.preventDefault();
  const result = await run(() => api("/server/settings", formToObject(event.target)), "Settings saved");
  if (result) $("#brand-name").textContent = event.target.name.value;
});

$("#icon-file").addEventListener("change", async event => {
  const file = event.target.files[0];
  event.target.value = "";
  if (!file) return;
  if (file.size > 256 * 1024) { toast("The icon must be 256 KB or smaller", true); return; }
  const bytes = new Uint8Array(await file.arrayBuffer());
  let binary = "";
  for (let i = 0; i < bytes.length; i += 0x8000) binary += String.fromCharCode(...bytes.subarray(i, i + 0x8000));
  const result = await run(() => api("/server/icon", { data: btoa(binary) }), "Icon changed");
  if (result) loadIcon();
});

$("#password-form").addEventListener("submit", async event => {
  event.preventDefault();
  const result = await run(() => api("/password", formToObject(event.target)), "Password changed, log in again");
  if (result) { event.target.reset(); showAuth(false); }
});

start();
