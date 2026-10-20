"use strict";

const API = "api";
const HEADER = { "X-Spacebox-Dashboard": "1" };
const state = { view: "overview", timers: [], commands: [], history: [], historyIndex: -1, lastLine: 0, player: null, players: { online: [], saved: [] } };

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
    $("#auth-server").textContent = info.serverName + " · v" + info.version;
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
  overview: () => { every(3000, refreshOverview); },
  players: () => { every(4000, refreshPlayers); },
  player: () => { every(10000, () => state.player && loadPlayer(state.player)); },
  moderation: () => { refreshModeration(); },
  console: () => { every(1000, pollConsole); $("#console-form input").focus(); },
  settings: () => { loadSettings(); }
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

  const history = metrics.history;
  drawChart($("#chart-cpu"), history.map(s => s.cpu), "#ffc428", 100);
  drawChart($("#chart-ram"), history.map(s => s.ramMb), "#6ed2e6");
  drawChart($("#chart-players"), history.map(s => s.players), "#6edc6e", Math.max(4, info.maxPlayers));
  drawChart($("#chart-tps"), history.map(s => s.tps), "#ff9b3d", Math.max(10, Math.ceil(info.targetTps * 1.2)));
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
  const data = await run(() => api("/players"));
  if (!data) return;
  state.players = data;
  $("#online-count").textContent = data.online.length;

  const online = data.online.map(p => el("tr", { class: "clickable", onclick: () => openPlayer(p.name) },
    el("td", {}, el("span", { class: "dot on" }), p.name, p.muted ? el("span", { class: "tag red", text: "muted" }) : null, !p.alive ? el("span", { class: "tag", text: "dead" }) : null),
    el("td", { text: `${p.ping} ms` }),
    el("td", { class: "faint", text: p.position ? p.position.join(", ") : "—" }),
    el("td", { class: "faint", text: p.address }),
    el("td", {}, playerButtons(p.name, true, p.muted))));
  $("#online-table").replaceChildren(
    el("tr", {}, ...["Name", "Ping", "Position", "Address", ""].map(t => el("th", { text: t }))),
    ...(online.length ? online : [el("tr", {}, el("td", { class: "empty", colspan: "5", text: "Nobody is online" }))]));
  renderSaved();
}

function renderSaved() {
  const filter = $("#player-filter").value.trim().toLowerCase();
  const saved = state.players.saved.filter(p => p.name.toLowerCase().includes(filter));
  const rows = saved.map(p => el("tr", { class: "clickable", onclick: () => openPlayer(p.name) },
    el("td", {}, el("span", { class: p.online ? "dot on" : "dot" }), p.name, p.muted ? el("span", { class: "tag red", text: "muted" }) : null),
    el("td", { class: "faint", text: p.lastSeen }),
    el("td", {}, playerButtons(p.name, p.online, p.muted))));
  $("#saved-table").replaceChildren(
    el("tr", {}, ...["Name", "Last save", ""].map(t => el("th", { text: t }))),
    ...(rows.length ? rows : [el("tr", {}, el("td", { class: "empty", colspan: "3", text: "No saved players" }))]));
}

$("#player-filter").addEventListener("input", renderSaved);

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

$("#whitelist-enabled").addEventListener("change", event =>
  run(() => api("/whitelist/enabled", { enabled: event.target.checked }), event.target.checked ? "Whitelist on" : "Whitelist off"));

async function pollConsole() {
  let data;
  try { data = await api(`/console?after=${state.lastLine}`); } catch { return; }
  if (!data.lines.length) return;

  const output = $("#console-output");
  output.append(...data.lines.map(line => el("div", { class: `line ${line.level}` }, el("span", { class: "time", text: line.time }), el("span", { text: line.text }))));
  state.lastLine = data.lines[data.lines.length - 1].id;
  while (output.childElementCount > 2000) output.firstChild.remove();
  if ($("#console-follow").checked) output.scrollTop = output.scrollHeight;
}

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
