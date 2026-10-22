"use strict";

const spaceMap = (() => {
  const FLAG_SHIP = 1, FLAG_LOADED = 2, FLAG_CLAIMED = 4, FLAG_CLAIM_ACTIVE = 8, PERSISTENCE_SHIFT = 4;
  const PERSISTENCE = ["Regenerated", "Modified", "Saved"];
  const COLORS = { regenerated: "#545c6e", regeneratedLoaded: "#d4dae6", modified: "#ff9b3d", saved: "#6ed2e6", player: "#6edc6e", dead: "#ff6b5a", claimActive: "#ffc428", claimInactive: "#aaafb9", grid: "rgba(90,100,120,0.35)", gridStrong: "rgba(120,130,150,0.55)" };
  const FOV = 60 * Math.PI / 180;

  const view = { yaw: 0.7, pitch: 0.45, distance: 3000, target: [0, 0, 0] };
  const filters = { regenerated: true, changed: true, ships: true, claimedOnly: false, labels: true };
  const state = { snapshot: null, origin: null, focus: "", radius: 2000, items: [], hover: null, selected: null, dirty: true, initialized: false, framed: false, loading: false };

  let canvas, ctx, tooltip, info, stats, width = 0, height = 0, dpr = 1, drag = null, touch = null, lastTap = 0;

  function init() {
    if (state.initialized) return;
    state.initialized = true;
    canvas = document.getElementById("map-canvas");
    ctx = canvas.getContext("2d");
    tooltip = document.getElementById("map-tooltip");
    info = document.getElementById("map-info");
    stats = document.getElementById("map-stats");

    new ResizeObserver(resize).observe(canvas.parentElement);
    canvas.addEventListener("mousedown", onMouseDown);
    window.addEventListener("mousemove", onMouseMove);
    window.addEventListener("mouseup", () => { drag = null; });
    canvas.addEventListener("wheel", onWheel, { passive: false });
    canvas.addEventListener("dblclick", () => { state.framed = false; frame(); });
    canvas.addEventListener("contextmenu", event => event.preventDefault());
    canvas.addEventListener("mouseleave", () => { tooltip.classList.add("hidden"); state.hover = null; invalidate(); });
    canvas.addEventListener("touchstart", onTouchStart, { passive: false });
    canvas.addEventListener("touchmove", onTouchMove, { passive: false });
    canvas.addEventListener("touchend", onTouchEnd, { passive: false });
    canvas.addEventListener("touchcancel", () => { touch = null; });
    canvas.addEventListener("gesturestart", event => event.preventDefault());

    document.getElementById("map-radius").addEventListener("change", event => { state.radius = Number(event.target.value); state.framed = false; refresh(); });
    document.getElementById("map-focus").addEventListener("change", event => { state.focus = event.target.value; state.origin = null; state.framed = false; refresh(); });
    for (const input of document.querySelectorAll("[data-map-filter]")) {
      input.checked = filters[input.dataset.mapFilter];
      input.addEventListener("change", () => { filters[input.dataset.mapFilter] = input.checked; invalidate(); });
    }
    document.getElementById("map-reset").addEventListener("click", () => { state.framed = false; frame(); });
  }

  function show() {
    init();
    resize();
  }

  async function refresh() {
    if (state.loading) return;
    state.loading = true;
    try {
      const query = `?radius=${state.radius}` + (state.focus ? `&player=${encodeURIComponent(state.focus)}` : "");
      apply(await api(`/map${query}`));
    } catch (error) {
      if (error.status !== 401) stats.textContent = error.message;
    } finally {
      state.loading = false;
    }
  }

  function apply(snapshot) {
    if (!state.origin) state.origin = snapshot.center.slice();
    const offset = [snapshot.center[0] - state.origin[0], snapshot.center[1] - state.origin[1], snapshot.center[2] - state.origin[2]];
    state.snapshot = snapshot;
    state.items = [];

    snapshot.entities.forEach((row, index) => {
      state.items.push({
        type: (row[4] & FLAG_SHIP) ? "ship" : "asteroid",
        id: snapshot.ids[index],
        p: [row[0] + offset[0], row[1] + offset[1], row[2] + offset[2]],
        r: row[3],
        flags: row[4],
        owner: row[5] >= 0 ? snapshot.owners[row[5]] : null
      });
    });
    for (const player of snapshot.players) {
      state.items.push({ type: "player", name: player.name, alive: player.alive, p: [player.position[0] + offset[0], player.position[1] + offset[1], player.position[2] + offset[2]], r: 0 });
    }

    if (state.selected) state.selected = state.items.find(item => sameItem(item, state.selected)) || null;
    updateFocusOptions(snapshot.players);
    updateStats(snapshot);
    updateInfo();
    if (!state.framed) frame();
    invalidate();
  }

  function sameItem(a, b) {
    return a.type === b.type && (a.type === "player" ? a.name === b.name : a.id === b.id);
  }

  function updateFocusOptions(players) {
    const select = document.getElementById("map-focus");
    const names = players.map(p => p.name).sort((a, b) => a.localeCompare(b));
    const current = Array.from(select.options).slice(1).map(o => o.value);
    if (names.join("\n") === current.join("\n")) return;
    select.replaceChildren(el("option", { value: "", text: "All players" }), ...names.map(name => el("option", { value: name, text: name })));
    select.value = names.includes(state.focus) ? state.focus : "";
  }

  function updateStats(snapshot) {
    let asteroids = 0, ships = 0, claimed = 0, saved = 0, modified = 0, loaded = 0;
    for (const item of state.items) {
      if (item.type === "player") continue;
      if (item.type === "ship") ships++; else asteroids++;
      if (item.flags & FLAG_CLAIMED) claimed++;
      if (item.flags & FLAG_LOADED) loaded++;
      const persistence = item.flags >> PERSISTENCE_SHIFT;
      if (persistence === 1) modified++;
      if (persistence === 2) saved++;
    }
    const parts = [`${snapshot.players.length} players`, `${asteroids} asteroids`, `${ships} ships`, `${loaded} loaded`, `${saved} saved`, `${modified} unsaved changes`, `${claimed} claimed`];
    if (snapshot.truncated > 0) parts.push(`${snapshot.truncated} not shown`);
    stats.textContent = parts.join(" · ");
  }

  function frame() {
    let max = 0;
    for (const item of state.items) {
      if (!isVisible(item)) continue;
      max = Math.max(max, Math.hypot(item.p[0], item.p[1], item.p[2]) + item.r);
    }
    if (state.focus) {
      const player = state.items.find(item => item.type === "player" && item.name === state.focus);
      view.target = player ? player.p.slice() : [0, 0, 0];
    } else {
      view.target = [0, 0, 0];
    }
    view.distance = Math.max(300, Math.min(state.radius * 2.2, (max || state.radius) * 2.2));
    state.framed = state.items.length > 0;
    invalidate();
  }

  function isVisible(item) {
    if (item.type === "player") return true;
    if (filters.claimedOnly && !(item.flags & FLAG_CLAIMED)) return false;
    if (item.type === "ship" && !filters.ships) return false;
    const persistence = item.flags >> PERSISTENCE_SHIFT;
    if (persistence === 0 && !filters.regenerated && !(item.flags & FLAG_CLAIMED)) return false;
    if (persistence !== 0 && !filters.changed) return false;
    return true;
  }

  function resize() {
    if (!canvas) return;
    const rect = canvas.parentElement.getBoundingClientRect();
    dpr = window.devicePixelRatio || 1;
    width = Math.max(100, rect.width);
    height = Math.max(200, rect.height);
    canvas.width = Math.round(width * dpr);
    canvas.height = Math.round(height * dpr);
    canvas.style.width = width + "px";
    canvas.style.height = height + "px";
    invalidate();
  }

  function invalidate() {
    if (state.dirty && state.raf) return;
    state.dirty = true;
    state.raf = requestAnimationFrame(() => { state.raf = 0; state.dirty = false; draw(); });
  }

  function basis() {
    const cy = Math.cos(view.yaw), sy = Math.sin(view.yaw), cp = Math.cos(view.pitch), sp = Math.sin(view.pitch);
    return { cy, sy, cp, sp, focal: height / (2 * Math.tan(FOV / 2)) };
  }

  function project(p, b) {
    const x = p[0] - view.target[0], y = p[1] - view.target[1], z = p[2] - view.target[2];
    const rx = x * b.cy - z * b.sy;
    const rz1 = x * b.sy + z * b.cy;
    const ry = y * b.cp - rz1 * b.sp;
    const rz = y * b.sp + rz1 * b.cp;
    const depth = view.distance + rz;
    if (depth < 1) return null;
    const scale = b.focal / depth;
    return { x: width / 2 - rx * scale, y: height / 2 - ry * scale, depth, scale };
  }

  function draw() {
    if (!ctx) return;
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.clearRect(0, 0, width, height);
    const b = basis();
    drawGrid(b);

    const drawables = [];
    for (const item of state.items) {
      if (!isVisible(item)) continue;
      const s = project(item.p, b);
      if (!s) continue;
      item.screen = s;
      drawables.push(item);
    }
    drawables.sort((a, b2) => b2.screen.depth - a.screen.depth);

    for (const item of drawables) {
      if (item.type === "ship") drawShip(item);
      else if (item.type === "asteroid") drawAsteroid(item);
    }
    for (const item of drawables) if (item.type === "player") drawPlayer(item);

    if (filters.labels) for (const item of drawables) if (item.type === "player") drawLabel(item.name, item.screen.x, item.screen.y - 10, item.alive ? COLORS.player : COLORS.dead);
    const highlight = state.hover || state.selected;
    if (highlight && highlight.screen && isVisible(highlight)) drawHighlight(highlight);
    state.drawn = drawables;
  }

  function drawGrid(b) {
    const step = niceStep(state.radius / 4);
    const extent = Math.ceil(state.radius / step) * step;
    const y = view.target[1];
    ctx.lineWidth = 1;
    for (let r = step; r <= extent; r += step) {
      ctx.strokeStyle = r === extent ? COLORS.gridStrong : COLORS.grid;
      ctx.beginPath();
      let first = true;
      for (let i = 0; i <= 64; i++) {
        const a = i / 64 * Math.PI * 2;
        const s = project([view.target[0] + Math.cos(a) * r, y, view.target[2] + Math.sin(a) * r], b);
        if (!s) { first = true; continue; }
        if (first) ctx.moveTo(s.x, s.y); else ctx.lineTo(s.x, s.y);
        first = false;
      }
      ctx.stroke();
    }
    const axes = [[[extent, 0, 0], "#ff6b5a88", "X"], [[0, 0, extent], "#6ed2e688", "Z"], [[0, extent * 0.5, 0], "#6edc6e88", "Y"]];
    const origin = project([view.target[0], y, view.target[2]], b);
    for (const [dir, color, label] of axes) {
      const end = project([view.target[0] + dir[0], y + dir[1], view.target[2] + dir[2]], b);
      if (!origin || !end) continue;
      ctx.strokeStyle = color;
      ctx.beginPath(); ctx.moveTo(origin.x, origin.y); ctx.lineTo(end.x, end.y); ctx.stroke();
      ctx.fillStyle = color; ctx.font = "11px monospace"; ctx.fillText(label, end.x + 4, end.y);
    }
    ctx.fillStyle = "#828791";
    ctx.font = "11px monospace";
    ctx.fillText(`grid ${step} m`, 10, height - 10);
  }

  function niceStep(value) {
    const pow = Math.pow(10, Math.floor(Math.log10(value)));
    const n = value / pow;
    return (n < 2 ? 1 : n < 5 ? 2 : 5) * pow;
  }

  function colorOf(item) {
    const persistence = item.flags >> PERSISTENCE_SHIFT;
    if (persistence === 1) return COLORS.modified;
    if (persistence === 2) return COLORS.saved;
    return item.flags & FLAG_LOADED ? COLORS.regeneratedLoaded : COLORS.regenerated;
  }

  function drawAsteroid(item) {
    const s = item.screen;
    const radius = Math.max(1.5, item.r * s.scale);
    const fade = Math.max(0.35, Math.min(1, 1.6 - s.depth / (view.distance * 1.6)));
    ctx.globalAlpha = fade;
    ctx.fillStyle = colorOf(item);
    ctx.beginPath();
    ctx.arc(s.x, s.y, radius, 0, Math.PI * 2);
    ctx.fill();
    ctx.globalAlpha = 1;
    if (item.flags & FLAG_CLAIMED) drawClaimRing(item, radius + 3);
  }

  function drawShip(item) {
    const s = item.screen;
    const size = Math.max(6, Math.min(16, item.r * s.scale));
    ctx.fillStyle = colorOf(item);
    ctx.strokeStyle = "#f0f0f0";
    ctx.lineWidth = 1.2;
    ctx.beginPath();
    ctx.moveTo(s.x, s.y - size);
    ctx.lineTo(s.x + size * 0.8, s.y + size * 0.7);
    ctx.lineTo(s.x, s.y + size * 0.3);
    ctx.lineTo(s.x - size * 0.8, s.y + size * 0.7);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
    if (item.flags & FLAG_CLAIMED) drawClaimRing(item, size + 4);
  }

  function drawClaimRing(item, radius) {
    const active = item.flags & FLAG_CLAIM_ACTIVE;
    ctx.strokeStyle = active ? COLORS.claimActive : COLORS.claimInactive;
    ctx.lineWidth = active ? 2 : 1.2;
    ctx.setLineDash(active ? [] : [3, 3]);
    ctx.beginPath();
    ctx.arc(item.screen.x, item.screen.y, radius, 0, Math.PI * 2);
    ctx.stroke();
    ctx.setLineDash([]);
  }

  function drawPlayer(item) {
    const s = item.screen;
    ctx.fillStyle = item.alive ? COLORS.player : COLORS.dead;
    ctx.strokeStyle = "#07090e";
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.arc(s.x, s.y, 5, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();
  }

  function drawLabel(text, x, y, color) {
    ctx.font = "12px system-ui, sans-serif";
    const w = ctx.measureText(text).width;
    ctx.fillStyle = "rgba(7,9,14,0.75)";
    ctx.fillRect(x - w / 2 - 4, y - 13, w + 8, 16);
    ctx.fillStyle = color;
    ctx.fillText(text, x - w / 2, y);
  }

  function drawHighlight(item) {
    const s = item.screen;
    const radius = item.type === "asteroid" ? Math.max(1.5, item.r * s.scale) + 6 : 12;
    ctx.strokeStyle = "#ffffff";
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.arc(s.x, s.y, radius, 0, Math.PI * 2);
    ctx.stroke();
  }

  function pick(x, y) {
    let best = null, bestDistance = Infinity;
    for (const item of state.drawn || []) {
      const s = item.screen;
      const radius = item.type === "asteroid" ? Math.max(4, item.r * s.scale) : 9;
      const d = Math.hypot(s.x - x, s.y - y);
      if (d <= radius && (d < bestDistance || (item.type === "player" && best?.type !== "player"))) {
        best = item;
        bestDistance = d;
      }
    }
    return best;
  }

  function describe(item) {
    if (item.type === "player") return [`Player ${item.name}`, item.alive ? "alive" : "dead", position(item)];
    const lines = [`${item.type === "ship" ? "Ship" : "Asteroid"} ${item.id}`, `radius ${Math.round(item.r)} m`, PERSISTENCE[item.flags >> PERSISTENCE_SHIFT] + (item.flags & FLAG_LOADED ? ", loaded" : ", not loaded")];
    if (item.flags & FLAG_CLAIMED) lines.push(`claim: ${item.owner || "?"} (${item.flags & FLAG_CLAIM_ACTIVE ? "active" : "expired"})`);
    lines.push(position(item));
    return lines;
  }

  function position(item) {
    if (!state.origin) return "";
    return "at " + item.p.map((v, i) => Math.round(v + state.origin[i])).join(", ");
  }

  function updateInfo() {
    if (!state.selected) {
      info.replaceChildren(el("div", { class: "dim", text: "Click or tap a dot for details. Drag to rotate, right drag or two fingers to pan, wheel or pinch to zoom, double click or double tap to reset." }));
      return;
    }
    info.replaceChildren(...describe(state.selected).map((line, i) => el("div", { class: i === 0 ? "" : "dim", text: line })));
  }

  function onMouseDown(event) {
    drag = { x: event.clientX, y: event.clientY, button: event.button, moved: false, pan: event.button === 2 || event.shiftKey };
  }

  function onMouseMove(event) {
    if (!canvas || canvas.offsetParent === null) return;
    if (drag) {
      const dx = event.clientX - drag.x, dy = event.clientY - drag.y;
      if (Math.abs(dx) + Math.abs(dy) > 2) drag.moved = true;
      drag.x = event.clientX;
      drag.y = event.clientY;
      if (drag.pan) pan(dx, dy);
      else {
        view.yaw += dx * 0.008;
        view.pitch = Math.max(-1.5, Math.min(1.5, view.pitch + dy * 0.008));
      }
      invalidate();
      return;
    }
    const rect = canvas.getBoundingClientRect();
    const x = event.clientX - rect.left, y = event.clientY - rect.top;
    if (x < 0 || y < 0 || x > rect.width || y > rect.height) return;
    const hover = pick(x, y);
    if (hover !== state.hover) { state.hover = hover; invalidate(); }
    if (hover) {
      tooltip.replaceChildren(...describe(hover).map((line, i) => el("div", { class: i === 0 ? "" : "dim", text: line })));
      tooltip.style.left = (x + 14) + "px";
      tooltip.style.top = (y + 14) + "px";
      tooltip.classList.remove("hidden");
    } else {
      tooltip.classList.add("hidden");
    }
  }

  canvasClick();
  function canvasClick() {
    window.addEventListener("mouseup", event => {
      if (!canvas || event.target !== canvas || !drag || drag.moved || drag.button !== 0) return;
      selectAt(event.clientX, event.clientY);
    }, true);
  }

  function selectAt(clientX, clientY) {
    const rect = canvas.getBoundingClientRect();
    state.selected = pick(clientX - rect.left, clientY - rect.top);
    updateInfo();
    invalidate();
  }

  function touchPoints(event) {
    return Array.from(event.touches, t => ({ x: t.clientX, y: t.clientY }));
  }

  function onTouchStart(event) {
    event.preventDefault();
    const points = touchPoints(event);
    if (!touch) touch = { moved: 0, multi: false, points };
    touch.points = points;
    if (points.length > 1) touch.multi = true;
  }

  function onTouchMove(event) {
    event.preventDefault();
    if (!touch) return;
    const points = touchPoints(event);
    const previous = touch.points;
    if (points.length === 1 && previous.length === 1) {
      const dx = points[0].x - previous[0].x, dy = points[0].y - previous[0].y;
      touch.moved += Math.abs(dx) + Math.abs(dy);
      view.yaw += dx * 0.008;
      view.pitch = Math.max(-1.5, Math.min(1.5, view.pitch + dy * 0.008));
    } else if (points.length >= 2 && previous.length >= 2) {
      const before = Math.hypot(previous[0].x - previous[1].x, previous[0].y - previous[1].y);
      const after = Math.hypot(points[0].x - points[1].x, points[0].y - points[1].y);
      if (before > 0 && after > 0) zoomBy(before / after);
      pan((points[0].x + points[1].x - previous[0].x - previous[1].x) / 2, (points[0].y + points[1].y - previous[0].y - previous[1].y) / 2);
      touch.moved += 10;
    }
    touch.points = points;
    invalidate();
  }

  function onTouchEnd(event) {
    event.preventDefault();
    if (!touch) return;
    const points = touchPoints(event);
    if (points.length > 0) {
      touch.points = points;
      return;
    }

    const tap = touch.moved < 8 && !touch.multi;
    touch = null;
    if (!tap) return;
    const now = performance.now();
    const point = event.changedTouches[0];
    if (now - lastTap < 350) {
      lastTap = 0;
      state.framed = false;
      frame();
      return;
    }
    lastTap = now;
    selectAt(point.clientX, point.clientY);
  }

  function zoomBy(factor) {
    view.distance = Math.max(20, Math.min(40000, view.distance * factor));
  }

  function pan(dx, dy) {
    const b = basis();
    const unit = view.distance / b.focal;
    const right = [b.cy, 0, -b.sy];
    const up = [-b.sy * b.sp, b.cp, -b.cy * b.sp];
    for (let i = 0; i < 3; i++) view.target[i] += (dx * right[i] + dy * up[i]) * unit;
  }

  function onWheel(event) {
    event.preventDefault();
    zoomBy(Math.pow(1.0015, event.deltaY));
    invalidate();
  }

  return { show, refresh };
})();
